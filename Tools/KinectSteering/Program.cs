using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;

namespace KinectSteering
{
    // PC 端：Azure Kinect 骨骼 -> 转向量 -> UDP 广播给 PICO
    // 模式：HANDS（双手连线相对肩膀连线的偏角，默认）/ LEAN（身体侧倾）
    // 快捷键：C=校零  M=切换模式  I=反向  Q=退出
    // USB 断线会自动重试，不会退出程序。
    internal static class Program
    {
        const int Port = 50505;
        const float HandMaxAngleDeg = 45f;
        const float LeanFullScale = 0.18f;
        const float Deadzone = 0.05f;
        const float Smoothing = 0.25f;
        const double HoldSeconds = 0.6;

        static readonly string ToolsDir = @"C:\Program Files\Azure Kinect Body Tracking SDK\tools";
        static readonly string ModelPath = Path.Combine(ToolsDir, "dnn_model_2_0_op11.onnx");
        static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "run.log");

        static bool useLeanMode;
        static bool invert = true;
        static float neutralAngle = float.NaN;
        static float neutralLean = float.NaN;
        static float lastGoodAngle;
        static double lastGoodTime = -999;
        static float smoothedSteer;
        static bool quit;
        static UdpClient udp;
        static int calibrationFrames;
        static readonly List<IPEndPoint> endpoints = new List<IPEndPoint>();

        static void Log(string s)
        {
            try
            {
                string line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + s;
                Console.WriteLine(line);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch { }
        }

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            try { File.WriteAllText(LogPath, ""); } catch { }

            Log("FreeBike Kinect Steering");
            Log("C=校零  M=模式(手/侧倾)  I=反向  Q=退出");

            udp = new UdpClient { EnableBroadcast = true };
            BuildEndpoints();
            Log("UDP 广播端口 " + Port + "，目标数 " + endpoints.Count);

            while (!quit)
            {
                try
                {
                    RunSession();
                }
                catch (Exception e)
                {
                    Log("Kinect 不可用: " + e.Message + "；2 秒后重试（检查 USB 线/接口）");
                    Thread.Sleep(2000);
                }
            }

            udp.Dispose();
        }

        static void RunSession()
        {
            HandleKeys();
            if (quit) return;

            using Device device = Device.Open();
            device.StartCameras(new DeviceConfiguration
            {
                ColorResolution = ColorResolution.Off,
                DepthMode = DepthMode.NFOV_Unbinned,
                CameraFPS = FPS.FPS30,
                SynchronizedImagesOnly = false
            });
            Log("相机已启动");

            Calibration calibration = device.GetCalibration(DepthMode.NFOV_Unbinned, ColorResolution.Off);
            var trackerConfig = new TrackerConfiguration
            {
                ProcessingMode = TrackerProcessingMode.DirectML,
                SensorOrientation = SensorOrientation.Default,
                ModelPath = ModelPath
            };

            using Tracker tracker = Tracker.Create(calibration, trackerConfig);
            Log("骨骼追踪已就绪（DirectML）");

            var clock = System.Diagnostics.Stopwatch.StartNew();
            int printCounter = 0;
            int noBodyCounter = 0;

            while (!quit)
            {
                HandleKeys();
                if (quit) break;

                using Capture capture = device.GetCapture(TimeSpan.FromMilliseconds(1000));
                if (capture == null) continue;

                tracker.EnqueueCapture(capture);
                using Frame frame = tracker.PopResult(TimeSpan.FromMilliseconds(1000));
                if (frame == null) continue;

                float steer = 0f;
                string mode = useLeanMode ? "LEAN" : "HANDS";
                float lean = 0f, handAngle = 0f;
                string handState = "none";

                if (frame.NumberOfBodies > 0)
                {
                    noBodyCounter = 0;
                    Skeleton skeleton = frame.GetBodySkeleton(0);

                    Joint neck = skeleton.GetJoint(JointId.Neck);
                    Joint pelvis = skeleton.GetJoint(JointId.Pelvis);
                    Joint hl = skeleton.GetJoint(JointId.HandLeft);
                    Joint hr = skeleton.GetJoint(JointId.HandRight);
                    Joint sl = skeleton.GetJoint(JointId.ShoulderLeft);
                    Joint sr = skeleton.GetJoint(JointId.ShoulderRight);

                    float torso = Vector3.Distance(neck.Position, pelvis.Position);
                    if (torso > 1f)
                        lean = (neck.Position.X - pelvis.Position.X) / torso;
                    if (float.IsNaN(neutralLean)) neutralLean = lean;
                    lean -= neutralLean;

                    bool jointsOk =
                        hl.ConfidenceLevel != JointConfidenceLevel.None &&
                        hr.ConfidenceLevel != JointConfidenceLevel.None &&
                        sl.ConfidenceLevel != JointConfidenceLevel.None &&
                        sr.ConfidenceLevel != JointConfidenceLevel.None;

                    Vector3 hv = hr.Position - hl.Position;
                    Vector3 sv = sr.Position - sl.Position;
                    float handAxis = MathF.Atan2(hv.Z, hv.X) * (180f / MathF.PI);
                    float shoulderAxis = MathF.Atan2(sv.Z, sv.X) * (180f / MathF.PI);
                    handAngle = NormalizeDeg(handAxis - shoulderAxis);
                    handState = jointsOk
                        ? "ok"
                        : hl.ConfidenceLevel.ToString() + "/" + hr.ConfidenceLevel.ToString();

                    // 置信度抖动保护：短时间内沿用上次有效角度
                    double now = clock.Elapsed.TotalSeconds;
                    if (jointsOk)
                    {
                        lastGoodAngle = handAngle;
                        lastGoodTime = now;
                    }
                    else if (now - lastGoodTime <= HoldSeconds)
                    {
                        handAngle = lastGoodAngle;
                    }
                    else
                    {
                        handAngle = neutralAngle;
                    }

                    if (!useLeanMode && !float.IsNaN(neutralAngle))
                    {
                        float delta = NormalizeDeg(handAngle - neutralAngle);
                        steer = Math.Clamp(delta / HandMaxAngleDeg, -1f, 1f);
                    }
                    else if (useLeanMode)
                    {
                        steer = Math.Clamp(lean / LeanFullScale, -1f, 1f);
                    }
                }
                else
                {
                    handState = "no body";
                    noBodyCounter++;
                    if (noBodyCounter == 30)
                        Log("30 帧未检测到人体，请确认镜头能看到上半身");
                }

                if (MathF.Abs(steer) < Deadzone) steer = 0f;
                smoothedSteer += (steer - smoothedSteer) * Smoothing;

                float outSteer = invert ? -smoothedSteer : smoothedSteer;
                Send(outSteer, mode, lean, handAngle, handState);

                if (++printCounter % 10 == 0)
                {
                    Log($"[{mode}] steer={outSteer:F2} lean={lean:F3} handAngle={handAngle:F1} hands={handState} t={clock.Elapsed.TotalSeconds:F0}s");
                }
            }
        }

        static void HandleKeys()
        {
            try
            {
                while (Console.KeyAvailable)
                {
                    ConsoleKeyInfo k = Console.ReadKey(true);
                    switch (k.Key)
                    {
                        case ConsoleKey.Q:
                            quit = true;
                            break;
                        case ConsoleKey.M:
                            useLeanMode = !useLeanMode;
                            Log("模式 -> " + (useLeanMode ? "LEAN" : "HANDS"));
                            break;
                        case ConsoleKey.I:
                            invert = !invert;
                            Log("反向 -> " + invert);
                            break;
                        case ConsoleKey.C:
                            neutralAngle = float.NaN;
                            neutralLean = float.NaN;
                            Log("请求重新校零…");
                            break;
                    }
                }
            }
            catch (InvalidOperationException) { }
        }

        static void Send(float steer, string mode, float lean, float handAngle, string state)
        {
            if (float.IsNaN(neutralAngle) && !useLeanMode && state == "ok")
            {
                if (++calibrationFrames > 45)
                {
                    neutralAngle = handAngle;
                    calibrationFrames = 0;
                    Log("已自动校零: " + handAngle.ToString("F1"));
                }
            }
            else
            {
                calibrationFrames = 0;
            }

            string msg = string.Format(CultureInfo.InvariantCulture,
                "FBSTEER|{0:F3}|{1}|{2:F3}|{3:F1}|{4}", steer, mode, lean, handAngle, state);
            byte[] data = Encoding.UTF8.GetBytes(msg);

            foreach (IPEndPoint ep in endpoints)
            {
                try { udp.Send(data, data.Length, ep); }
                catch (SocketException) { }
            }
        }

        static void BuildEndpoints()
        {
            endpoints.Add(new IPEndPoint(IPAddress.Broadcast, Port));
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (ua.IPv4Mask == null) continue;

                        byte[] ip = ua.Address.GetAddressBytes();
                        byte[] m = ua.IPv4Mask.GetAddressBytes();
                        byte[] b = new byte[4];
                        for (int i = 0; i < 4; i++) b[i] = (byte)(ip[i] | ~m[i]);
                        endpoints.Add(new IPEndPoint(new IPAddress(b), Port));
                    }
                }
            }
            catch (Exception) { }
        }

        static float NormalizeDeg(float deg)
        {
            while (deg > 180f) deg -= 360f;
            while (deg < -180f) deg += 360f;
            return deg;
        }
    }
}
