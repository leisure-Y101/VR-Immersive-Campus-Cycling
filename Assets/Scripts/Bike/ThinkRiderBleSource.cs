using System;
using UnityEngine;

// ThinkRider A1 BLE 数据源（Android 端通过 ThinkRiderBle.jar 桥接）
// 未安装插件或不在 Android 时自动降级为不可用，交由 RideUi 决定是否允许训练。
public class ThinkRiderBleSource : MonoBehaviour, IBikeDataSource
{
    public string deviceNamePrefix = "Think A1";
    public bool autoConnect = true;
    public float staleSeconds = 2.0f;

    static event Action<string> NativeMessage;

    AndroidJavaClass plugin;
    bool available;
    BikeConnectionState state = BikeConnectionState.Disconnected;
    BikeTelemetry telemetry;
    float lastDataTime = -999f;
    string lastError;
    float retryTimer;

    // 最近收到的原生消息（文字里能看到卡在哪一步，不接 USB 也能读）
    readonly System.Collections.Generic.Queue<string> recent = new System.Collections.Generic.Queue<string>();
    bool notifyLogged;
    public string Diagnostics
    {
        get
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in recent) { if (sb.Length > 0) sb.Append(" | "); sb.Append(s); }
            return sb.ToString();
        }
    }
    readonly ThinkRiderParser.CadenceFromCrank cadenceCalc = new ThinkRiderParser.CadenceFromCrank();
    readonly ThinkRiderParser.SpeedFromWheel speedCalc = new ThinkRiderParser.SpeedFromWheel();

    public string DisplayName => "ThinkRider A1";
    public BikeConnectionState State
    {
        get
        {
            if (state == BikeConnectionState.Connected && Time.unscaledTime - lastDataTime > staleSeconds)
                return BikeConnectionState.Error;
            return state;
        }
    }
    public bool IsConnected => State == BikeConnectionState.Connected;
    public bool IsAvailable => available;
    public float DataAge => Time.unscaledTime - lastDataTime;
    public string LastError => lastError;
    public static bool HasNewData => true;

    public BikeTelemetry Snapshot()
    {
        telemetry.timestamp = lastDataTime;
        return telemetry;
    }

    void Awake()
    {
        var existing = GameObject.Find("ThinkRiderBleCallback");
        if (existing == null)
        {
            var go = new GameObject("ThinkRiderBleCallback");
            go.AddComponent<BleCallbackReceiver>();
            DontDestroyOnLoad(go);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            plugin = new AndroidJavaClass("com.freebike.ble.ThinkRiderBlePlugin");
            plugin.CallStatic("init", "ThinkRiderBleCallback");
            available = true;
        }
        catch (Exception e)
        {
            available = false;
            Debug.LogWarning("ThinkRiderBleSource: BLE 插件未安装或加载失败: " + e.Message);
        }
#endif
    }

    void OnDestroy()
    {
        try { plugin?.CallStatic("disconnect"); } catch { }
    }

    // 扫描无果 / 出错时每 6 秒重扫一次：
    // Android 10 扫 BLE 必须先拿到定位权限，而权限弹窗是异步的，首帧那次扫描通常已经失败
    void Update()
    {
        if (!available) return;
        if (state == BikeConnectionState.Connected || state == BikeConnectionState.Connecting) return;
        retryTimer += Time.unscaledDeltaTime;
        if (retryTimer < 6f) return;
        retryTimer = 0f;
        try { plugin.CallStatic("stopScan"); } catch { }
        StartScan();
    }

    public static bool LocationPermissionGranted()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { return UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.ACCESS_FINE_LOCATION"); }
        catch { return false; }
#else
        return true;
#endif
    }

    public void StartScan()
    {
        if (!available)
        {
            state = BikeConnectionState.Error;
            lastError = "BLE 插件不可用";
            return;
        }

        RequestPermissions();
        state = BikeConnectionState.Scanning;
        try { plugin.CallStatic("startScan", deviceNamePrefix); }
        catch (Exception e) { lastError = e.Message; state = BikeConnectionState.Error; }
    }

    public void Connect(string address)
    {
        if (!available || string.IsNullOrEmpty(address)) return;
        state = BikeConnectionState.Connecting;
        try { plugin.CallStatic("connect", address); }
        catch (Exception e) { lastError = e.Message; state = BikeConnectionState.Error; }
    }

    public void Disconnect()
    {
        if (available)
        {
            try { plugin.CallStatic("disconnect"); } catch { }
        }
        state = BikeConnectionState.Disconnected;
    }

    static void RequestPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            UnityEngine.Android.Permission.RequestUserPermission("android.permission.BLUETOOTH_SCAN");
            UnityEngine.Android.Permission.RequestUserPermission("android.permission.BLUETOOTH_CONNECT");
            UnityEngine.Android.Permission.RequestUserPermission("android.permission.ACCESS_FINE_LOCATION");
        }
        catch (Exception) { }
#endif
    }

    void HandleMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        Record(msg);
        var p = msg.Split('|');
        switch (p[0])
        {
            case "SCAN":
                if (p.Length >= 3)
                {
                    Debug.Log("ThinkRiderBleSource: 发现设备 " + p[1] + " " + p[2]);
                    if (autoConnect && state != BikeConnectionState.Connected)
                        Connect(p[2]);
                }
                break;

            case "STATE":
                if (p.Length >= 2)
                {
                    if (p[1] == "CONNECTED") state = BikeConnectionState.Connected;
                    else if (p[1] == "DISCONNECTED")
                    {
                        state = BikeConnectionState.Disconnected;
                        lastError = p.Length >= 3 ? ("disconnect status=" + p[2]) : null;
                    }
                    else if (p[1] == "CONNECTING") state = BikeConnectionState.Connecting;
                }
                break;

            case "NOTIFY":
                if (p.Length >= 3) HandleNotify(p[1], p[2]);
                break;

            case "ERR":
                lastError = msg.Substring(4);
                Debug.LogWarning("ThinkRiderBleSource: " + lastError);
                break;
        }
    }

    void Record(string msg)
    {
        if (msg.StartsWith("NOTIFY"))
        {
            if (notifyLogged) return;
            notifyLogged = true;
            msg = "NOTIFY 收到数据";
        }
        if (msg.Length > 58) msg = msg.Substring(0, 58) + "…";
        if (recent.Count >= 6) recent.Dequeue();
        recent.Enqueue(msg);
    }

    void HandleNotify(string uuid, string hex)    {
        byte[] data = HexToBytes(hex);
        if (data == null || data.Length == 0) return;
        string u = uuid.ToLowerInvariant();

        if (u.Contains("2ad2"))
        {
            if (ThinkRiderParser.TryParseIndoorBikeData(data, data.Length, ref telemetry))
                Touch();
        }
        else if (u.Contains("2a63"))
        {
            var frame = ThinkRiderParser.ParseCyclingPower(data, data.Length);
            if (!frame.valid) return;
            telemetry.powerWatts = frame.powerWatts;
            if (frame.hasCrankData)
                telemetry.cadenceRpm = cadenceCalc.Update(frame.crankRevs, frame.crankTime);
            if (frame.hasWheelData)
                telemetry.speedKph = speedCalc.Update(frame.wheelRevs, frame.wheelTime);
            Touch();
        }
    }

    void Touch()
    {
        lastDataTime = Time.unscaledTime;
        telemetry.timestamp = lastDataTime;
        if (state != BikeConnectionState.Connected) state = BikeConnectionState.Connected;
    }

    static byte[] HexToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex) || (hex.Length & 1) != 0) return null;
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            int hi = HexVal(hex[i * 2]);
            int lo = HexVal(hex[i * 2 + 1]);
            if (hi < 0 || lo < 0) return null;
            bytes[i] = (byte)((hi << 4) | lo);
        }
        return bytes;
    }

    static int HexVal(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'a' && c <= 'f') return c - 'a' + 10;
        if (c >= 'A' && c <= 'F') return c - 'A' + 10;
        return -1;
    }

    internal static void OnNativeMessage(string msg)
    {
        NativeMessage?.Invoke(msg);
    }

    void OnEnable() { NativeMessage += HandleMessage; }
    void OnDisable() { NativeMessage -= HandleMessage; }
}

// Java -> Unity 消息接收器
public class BleCallbackReceiver : MonoBehaviour
{
    void OnBleMessage(string msg)
    {
        ThinkRiderBleSource.OnNativeMessage(msg);
    }
}
