using UnityEngine;

// 开发用模拟训练台：电脑上没有 A1 时，用键盘 W/S 模拟踩踏（仅编辑器/开发使用）
public class SimulatedBikeSource : MonoBehaviour, IBikeDataSource
{
    public float maxPower = 260f;
    public float maxCadence = 95f;

    float power;
    float cadence;
    BikeTelemetry telemetry;

    public string DisplayName => "模拟训练台（开发）";
    public BikeConnectionState State => BikeConnectionState.Connected;
    public bool IsConnected => true;

    public BikeTelemetry Snapshot()
    {
        telemetry.timestamp = Time.unscaledTime;
        return telemetry;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float pedal = InputRouter.I != null ? InputRouter.I.Pedal : 0f;

        if (pedal > 0f)
        {
            cadence = Mathf.MoveTowards(cadence, maxCadence * Mathf.Clamp01(pedal + 0.35f), 60f * dt);
            power = Mathf.MoveTowards(power, maxPower * Mathf.Clamp01(pedal), 160f * dt);
        }
        else
        {
            cadence = Mathf.MoveTowards(cadence, 0f, 90f * dt);
            power = Mathf.MoveTowards(power, 0f, 220f * dt);
        }

        telemetry.powerWatts = power;
        telemetry.cadenceRpm = cadence;
        telemetry.speedKph = 0f; // 速度由骑行逻辑按功率推算，这里不填
    }

    public void StartScan() { }
    public void Disconnect() { }
}
