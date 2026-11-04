using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// 接收 PC 端 Kinect 转向服务广播的转向数据
// 包格式：FBSTEER|steer|mode|lean|angle|state
// 无数据超过 timeout 秒视为不可用
public class KinectSteeringReceiver : MonoBehaviour, ISteeringSource
{
    public int port = 50505;
    public float timeout = 0.5f;

    readonly object gate = new object();
    UdpClient udp;
    Thread thread;
    volatile bool running;

    float steer;
    long lastPacketMs = long.MinValue / 2;
    string mode = "";
    float lean;
    float angle;
    string state = "";

    public bool Available
    {
        get
        {
            lock (gate)
            {
                return DateTime.UtcNow.Ticks - lastPacketMs < (long)(timeout * 10000000f);
            }
        }
    }

    public float Steer
    {
        get { lock (gate) { return steer; } }
    }

    public string Mode { get { lock (gate) { return mode; } } }
    public float Lean { get { lock (gate) { return lean; } } }
    public float Angle { get { lock (gate) { return angle; } } }
    public string RemoteState { get { lock (gate) { return state; } } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<KinectSteeringReceiver>() != null) return;
        var go = new GameObject("KinectSteeringReceiver");
        go.AddComponent<KinectSteeringReceiver>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        try
        {
            udp = new UdpClient(port);
            running = true;
            thread = new Thread(Loop) { IsBackground = true, Name = "KinectSteeringRx" };
            thread.Start();
            Debug.Log("KinectSteeringReceiver: 监听 UDP " + port);
        }
        catch (Exception e)
        {
            Debug.LogWarning("KinectSteeringReceiver 初始化失败: " + e.Message);
        }
    }

    void Loop()
    {
        var remote = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref remote);
                string msg = Encoding.UTF8.GetString(data);
                var p = msg.Split('|');
                if (p.Length < 6 || p[0] != "FBSTEER") continue;

                if (!float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float s))
                    continue;

                float l = 0f, a = 0f;
                float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out l);
                float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out a);

                lock (gate)
                {
                    steer = Math.Clamp(s, -1f, 1f);
                    mode = p[2];
                    lean = l;
                    angle = a;
                    state = p[5];
                    lastPacketMs = DateTime.UtcNow.Ticks;
                }
            }
            catch (SocketException) { Thread.Sleep(10); }
            catch (ObjectDisposedException) { break; }
            catch (Exception) { Thread.Sleep(10); }
        }
    }

    void OnDestroy()
    {
        running = false;
        try { udp?.Close(); } catch { }
    }
}
