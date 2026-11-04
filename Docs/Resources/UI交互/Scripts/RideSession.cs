// 单次骑行统计：时长 / 距离 / 最高速 / 均速
public class RideSession
{
    public bool Active { get; private set; }
    public float Elapsed { get; private set; }
    public float Distance { get; private set; }
    public float MaxSpeed { get; private set; }
    public float AvgSpeed => Elapsed > 0.05f ? Distance / Elapsed : 0f;

    public void Start()
    {
        Active = true;
        Elapsed = 0f;
        Distance = 0f;
        MaxSpeed = 0f;
    }

    public void Stop() { Active = false; }

    public void Tick(float dt, float speed)
    {
        if (!Active) return;
        Elapsed += dt;
        Distance += speed * dt;
        if (speed > MaxSpeed) MaxSpeed = speed;
    }
}
