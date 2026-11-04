using UnityEngine;

// 训练设置：存 PlayerPrefs，应用到骑行脚本
public class RideSettings
{
    public float speedKmh = 36f;     // 最高速度 10~60
    public float steerSens = 1f;     // 转向灵敏度 0.5~1.5
    public bool airWall = true;      // 道路空气墙
    public float heightAdjust = 0f;  // 视角高度微调（1.6m 基准，±0.5m）

    const string KSpeed = "fb.speedKmh";
    const string KSteer = "fb.steerSens";
    const string KWall = "fb.airWall";
    const string KHeight = "fb.heightAdjust";

    public static RideSettings Load()
    {
        return new RideSettings
        {
            speedKmh = PlayerPrefs.GetFloat(KSpeed, 36f),
            steerSens = PlayerPrefs.GetFloat(KSteer, 1f),
            airWall = PlayerPrefs.GetInt(KWall, 1) == 1,
            heightAdjust = PlayerPrefs.GetFloat(KHeight, 0f),
        };
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(KSpeed, speedKmh);
        PlayerPrefs.SetFloat(KSteer, steerSens);
        PlayerPrefs.SetInt(KWall, airWall ? 1 : 0);
        PlayerPrefs.SetFloat(KHeight, heightAdjust);
        PlayerPrefs.Save();
    }

    public void ApplyTo(BikeKeyboardRide ride)
    {
        if (ride == null) return;
        ride.maxSpeed = speedKmh / 3.6f;
        ride.turnSpeedDeg = 50f * steerSens;
        ride.airWall = airWall;
    }
}
