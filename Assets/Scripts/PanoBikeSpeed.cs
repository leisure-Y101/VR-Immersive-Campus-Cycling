using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

/// <summary>
/// 键盘模拟踩踏功率，或接收 PC BLE 桥接发来的 A1 功率。
/// UDP 50506 收到数据时优先使用 A1；收不到时回退到键盘 W/S。
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class PanoBikeSpeed : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer video;

    [Header("A1 BLE UDP")]
    public int udpPort = 50506;
    public float externalTimeout = 2.0f;

    [Header("Power")]
    public float power = 0f;
    public float maxPower = 300f;
    public float increasePerSecond = 150f;
    public float decreasePerSecond = 250f;
    public float coastDownPerSecond = 80f;

    [Header("Playback")]
    [Tooltip("没有有效自行车数据时的播放速度；设为 0 表示静止。")]
    public float idlePlayback = 1f;
    [Tooltip("没有真实自行车数据时是否允许键盘 W/S 回退。正式骑行建议关闭。")]
    public bool allowKeyboardFallback = false;
    public float maxPlayback = 1.25f;
    public float response = 3f;
    public bool showDebugHud = false;
    [Tooltip("由本组件根据真实自行车功率自动暂停/播放。")]
    public bool controlPlaybackState = false;

    public bool ExternalSource => hasExternal && Time.time - lastPacketTime <= externalTimeout;

    private float smoothedPower;
    private UdpClient udp;
    private IPEndPoint remote;
    private float externalPower;
    private float lastPacketTime = -999f;
    private bool hasExternal;

    void Start()
    {
        if (video == null) video = GetComponent<VideoPlayer>();
        if (video == null)
        {
            Debug.LogError("PanoBikeSpeed: 没有找到 VideoPlayer");
            enabled = false;
            return;
        }

        video.playOnAwake = false;
        video.playbackSpeed = idlePlayback;
        smoothedPower = 0f;
        power = 0f;

        try
        {
            udp = new UdpClient(udpPort);
            udp.Client.Blocking = false;
            remote = new IPEndPoint(IPAddress.Any, 0);
            Debug.Log($"PanoBikeSpeed: UDP {udpPort} listening. Keyboard fallback: W/S.");
        }
        catch (Exception exc)
        {
            udp = null;
            Debug.LogWarning($"PanoBikeSpeed: UDP {udpPort} 打开失败，使用键盘。{exc.Message}");
        }
    }

    void Update()
    {
        ReadUdp();

        bool external = hasExternal && Time.time - lastPacketTime <= externalTimeout;

        if (external)
        {
            power = externalPower;
        }
        else if (allowKeyboardFallback)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed)
                {
                    power += increasePerSecond * Time.deltaTime;
                }
                else if (kb.sKey.isPressed)
                {
                    power -= decreasePerSecond * Time.deltaTime;
                }
                else
                {
                    power = Mathf.MoveTowards(power, 0f, coastDownPerSecond * Time.deltaTime);
                }
            }
        }
        else
        {
            // 没有真实自行车数据时保持静止，不能靠键盘或旧功率继续播放。
            power = 0f;
        }

        power = Mathf.Clamp(power, 0f, maxPower);
        smoothedPower = Mathf.Lerp(smoothedPower, power, Time.deltaTime * response);

        float curve = Mathf.Sqrt(Mathf.Clamp01(smoothedPower / maxPower));
        float targetPlayback = Mathf.Lerp(idlePlayback, maxPlayback, curve);
        video.playbackSpeed = Mathf.Lerp(video.playbackSpeed, targetPlayback, Time.deltaTime * response);

        // 没有有效的真实自行车功率时保持暂停；有功率后才开始播放。
        if (controlPlaybackState)
        {
            bool hasRideData = external && smoothedPower >= 5f;
            if (!hasRideData)
            {
                if (video.isPlaying) video.Pause();
            }
            else if (!video.isPlaying)
            {
                video.Play();
            }
        }
    }

    void ReadUdp()
    {
        if (udp == null) return;

        while (udp.Available > 0)
        {
            try
            {
                byte[] data = udp.Receive(ref remote);
                string text = Encoding.ASCII.GetString(data).Trim();
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    externalPower = Mathf.Clamp(value, 0f, maxPower);
                    lastPacketTime = Time.time;
                    hasExternal = true;
                }
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception exc)
            {
                Debug.LogWarning($"PanoBikeSpeed UDP: {exc.Message}");
                break;
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugHud) return;

        bool external = hasExternal && Time.time - lastPacketTime <= externalTimeout;
        string source = external ? "A1 BLE" : "Keyboard";
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 10, 900, 30),
            $"Power {power:F0} W   Speed x{video.playbackSpeed:F2}   Source {source}");
    }

    void OnDestroy()
    {
        if (udp != null)
        {
            udp.Close();
            udp = null;
        }
    }
}
