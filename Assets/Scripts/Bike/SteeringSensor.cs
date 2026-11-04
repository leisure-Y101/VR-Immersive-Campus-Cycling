using UnityEngine;

// 转向数据源接口：为后续“车把转向传感器”预留
// 约定：Steer 取值 -1（左）~ +1（右），0 表示直行
public interface ISteeringSource
{
    bool Available { get; }
    float Steer { get; }
}

// 全局转向入口：未来把 BLE/串口转向传感器注册到这里即可，
// 骑行逻辑（BikeKeyboardRide）只读取 SteeringHub.Source，不再关心硬件细节。
public static class SteeringHub
{
    public static ISteeringSource Source;

    public static float Steer
    {
        get
        {
            if (Source == null || !Source.Available) return 0f;
            return Mathf.Clamp(Source.Steer, -1f, 1f);
        }
    }
}
