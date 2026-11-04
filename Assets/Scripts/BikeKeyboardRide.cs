using UnityEngine;

// 骑行控制：
// - 正式模式：速度由 ThinkRider A1 的功率数据驱动（IBikeDataSource）
// - 转向：预留给车把转向传感器（SteeringHub），没有传感器时直行
// - 开发模式（编辑器）：键盘 W/S 模拟踩踏
public class BikeKeyboardRide : MonoBehaviour
{
    [Header("骑行参数")]
    public float maxSpeed = 10f;        // m/s
    public float accel = 3f;            // 加速平滑
    public float coastDecel = 2f;       // 功率下降时减速
    public float turnSpeedDeg = 35f;    // 满舵转向角速度（50→35，降低转向冲击感）

    [Header("地形贴合")]
    public bool followGround = true;
    public float groundOffset = 0f;     // 车架原点贴地（真实眼高来自头显）
    public float groundSmooth = 12f;
    public float maxVerticalSpeed = 1.2f;  // 垂直最大变化速度(m/s)：禁止瞬移，消除过坡顶/接缝的"一沉"
    public float minGroundNormalY = 0.5f;  // 只认朝上的面；低于此值视为墙面/侧面，不当作地面
    public LayerMask groundMask = ~0;

    [Header("空气墙")]
    public bool airWall = true;
    public Transform roadRoot;

    [Header("状态")]
    public bool ridingEnabled = false;

    [System.NonSerialized] public IBikeDataSource telemetry;

    [SerializeField] float speed;
    [SerializeField] float powerSmoothed;
    [SerializeField] bool wallHit;
    [SerializeField] bool onRoad;

    float steerSmoothed;

    public float Speed => speed;
    public float Power => powerSmoothed;
    public float Cadence => telemetry != null ? telemetry.Snapshot().cadenceRpm : 0f;

    void Start()
    {
        if (roadRoot == null)
        {
            var map = GameObject.Find("Map");
            if (map != null) roadRoot = map.transform;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (!ridingEnabled)
        {
            speed = Mathf.MoveTowards(speed, 0f, 8f * dt);
            powerSmoothed = Mathf.MoveTowards(powerSmoothed, 0f, 300f * dt);
            return;
        }

        float target = TargetSpeed(dt);
        float rate = target > speed ? accel : coastDecel;
        speed = Mathf.MoveTowards(speed, target, rate * dt);

        float steer = SteeringHub.Steer;
#if UNITY_EDITOR
        if (Mathf.Approximately(steer, 0f) && InputRouter.I != null)
            steer = InputRouter.I.Steer; // 编辑器调试
#endif

        // 转向平滑：降低输入抖动带来的视角晃动
        steerSmoothed = Mathf.Lerp(steerSmoothed, steer, 1f - Mathf.Exp(-10f * dt));

        ApplyMotion(dt, steerSmoothed);
    }

    // 功率 -> 目标速度（第一版简单映射，后续可替换成功率/坡度模型）
    float TargetSpeed(float dt)
    {
        if (telemetry == null || !telemetry.IsConnected) return 0f;

        var t = telemetry.Snapshot();
        float power = Mathf.Max(0f, t.powerWatts);
        powerSmoothed = Mathf.Lerp(powerSmoothed, power, 1f - Mathf.Exp(-6f * dt));

        if (powerSmoothed < 5f) return 0f;

        float kph = 8f + powerSmoothed * 0.16f;
        return Mathf.Min(kph / 3.6f, maxSpeed);
    }

    // 基础筛选：只认朝上的水平承受面，排除建筑立面 / 桥侧面
    bool TryRaycastGround(Vector3 pos, out RaycastHit hit)
    {
        hit = default;
        if (!Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out var h, 30f, groundMask, QueryTriggerInteraction.Ignore))
            return false;
        // 用绝对值兼容法线朝向反了的碰撞体
        if (Mathf.Abs(h.normal.y) < minGroundNormalY) return false;
        hit = h;
        return true;
    }

    // 严格：在基础筛选之上，若配置了 roadRoot 则只认道路（P1）
    bool TryGetGround(Vector3 pos, out RaycastHit hit)
    {
        if (!TryRaycastGround(pos, out hit)) return false;
        if (roadRoot != null && !hit.collider.transform.IsChildOf(roadRoot)) return false;
        return true;
    }

    bool HasGround(Vector3 pos)
    {
        return TryRaycastGround(pos, out _);
    }

    bool IsOnRoad(Vector3 pos)
    {
        if (!TryRaycastGround(pos, out var hit)) return false;
        if (Mathf.Abs(hit.point.y - (transform.position.y - groundOffset)) > 3f) return false;
        return roadRoot == null || hit.collider.transform.IsChildOf(roadRoot);
    }

    void ApplyMotion(float dt, float steer)
    {
        // 视角跟随行进方向：车把转向即转动整车/相机
        transform.Rotate(0f, steer * turnSpeedDeg * dt, 0f);

        var next = transform.position + transform.forward * (speed * dt);

        onRoad = IsOnRoad(transform.position);
        wallHit = airWall && (onRoad ? !IsOnRoad(next) : !HasGround(next));
        if (wallHit)
            speed = Mathf.MoveTowards(speed, 0f, 15f * dt);
        else
            transform.position = next;

        if (followGround && TryGetGround(transform.position, out var hit))
        {
            var targetY = hit.point.y + groundOffset;
            var p = transform.position;
            // P0：不再瞬移。先指数平滑，再按最大垂直速度限幅，避免落差处"一沉/一抬"
            var smoothed = Mathf.Lerp(p.y, targetY, 1f - Mathf.Exp(-groundSmooth * dt));
            p.y = Mathf.MoveTowards(p.y, smoothed, maxVerticalSpeed * dt);
            transform.position = p;
        }
    }
}
