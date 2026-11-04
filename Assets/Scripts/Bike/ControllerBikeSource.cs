using UnityEngine;

// 调试用：无训练台传感器时，用手柄模拟踩踏
// 右扳机（或右摇杆向上）= 踩踏；摇杆向下 = 刹车
public class ControllerBikeSource : MonoBehaviour, IBikeDataSource
{
    public float maxPower = 260f;
    public float maxCadence = 95f;

    float power;
    float cadence;
    BikeTelemetry telemetry;

    public string DisplayName => "手柄控制（调试）";
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
        var r = InputRouter.I;

        float pedal = 0f;
        float brake = 0f;
        if (r != null)
        {
            pedal = Mathf.Max(0f, r.TriggerValue);
            pedal = Mathf.Max(pedal, Mathf.Max(0f, r.StickY));
            brake = Mathf.Max(0f, -r.StickY);
        }

        if (brake > 0.2f)
        {
            power = Mathf.MoveTowards(power, 0f, 300f * dt);
            cadence = Mathf.MoveTowards(cadence, 0f, 120f * dt);
        }
        else if (pedal > 0.05f)
        {
            cadence = Mathf.MoveTowards(cadence, maxCadence * Mathf.Clamp01(0.4f + pedal * 0.6f), 70f * dt);
            power = Mathf.MoveTowards(power, maxPower * pedal, 180f * dt);
        }
        else
        {
            cadence = Mathf.MoveTowards(cadence, 0f, 90f * dt);
            power = Mathf.MoveTowards(power, 0f, 220f * dt);
        }

        telemetry.powerWatts = power;
        telemetry.cadenceRpm = cadence;
        telemetry.speedKph = 0f;
    }

    public void StartScan() { }
    public void Disconnect() { }
}
