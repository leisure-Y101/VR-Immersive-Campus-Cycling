using System;

// 骑行遥测数据的统一结构：真实设备、模拟设备、手柄都通过它进入游戏逻辑
public enum BikeConnectionState
{
    Disconnected,
    Scanning,
    Connecting,
    Connected,
    Error
}

[Serializable]
public struct BikeTelemetry
{
    public float powerWatts;      // 功率 W
    public float cadenceRpm;      // 踏频 RPM
    public float speedKph;        // 速度 km/h
    public int resistanceLevel;   // 阻力等级（如果设备提供）
    public float heartRate;       // 心率（如果设备提供）
    public double timestamp;      // 采样时间（Time.unscaledTime）
}

// 数据源接口：A1 BLE、开发模拟、手柄都实现它，骑行逻辑不关心数据来自哪里
public interface IBikeDataSource
{
    string DisplayName { get; }
    BikeConnectionState State { get; }
    bool IsConnected { get; }
    BikeTelemetry Snapshot();
    void StartScan();
    void Disconnect();
}
