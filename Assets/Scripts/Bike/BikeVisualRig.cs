using UnityEngine;

// 第一人称自行车占位模型：
// - 运行时用基础几何体拼出车架/车把/前叉/双轮/曲柄/脚踏
// - 暴露 gripLeft / gripRight / frontWheel / rearWheel / crank 锚点
// - 后续导入真实模型时调用 BindExternal(root)，按名称自动接管这些部件
// - 轮子按速度旋转，曲柄/脚踏按踏频旋转
public class BikeVisualRig : MonoBehaviour
{
    [Header("数据源")]
    public BikeKeyboardRide ride;

    [Header("尺寸")]
    public float wheelRadius = 0.335f;
    public float handlebarHeight = 0.98f;
    public float handlebarForward = 0.52f;
    public float gripHalfWidth = 0.21f;

    [Header("外部模型（可选，导入后绑定）")]
    public Transform visualRoot;
    public Transform frontWheel;
    public Transform rearWheel;
    public Transform crank;
    public Transform gripLeft;
    public Transform gripRight;

    public Transform GripLeft => gripLeft;
    public Transform GripRight => gripRight;

    Transform placeholderRoot;
    Material frameMat;
    Material darkMat;
    Material metalMat;
    float wheelAngle;
    float crankAngle;

    void Awake()
    {
        BuildPlaceholder();
    }

    void Update()
    {
        float speedKph = ride != null ? ride.Speed * 3.6f : 0f;
        float cadence = ride != null ? ride.Cadence : 0f;
        float dt = Time.deltaTime;

        float wheelDegPerSec = speedKph / 3.6f / Mathf.Max(0.05f, wheelRadius) * Mathf.Rad2Deg;
        wheelAngle = Mathf.Repeat(wheelAngle + wheelDegPerSec * dt, 360f);
        if (frontWheel != null) frontWheel.localRotation = Quaternion.Euler(wheelAngle, 0f, 0f);
        if (rearWheel != null) rearWheel.localRotation = Quaternion.Euler(wheelAngle, 0f, 0f);

        crankAngle = Mathf.Repeat(crankAngle + cadence * 6f * dt, 360f);
        if (crank != null) crank.localRotation = Quaternion.Euler(crankAngle, 0f, 0f);
    }

    // ============ 外部模型替换 ============
    // 约定名称（不区分大小写，包含即可）：
    //   frontwheel / wheel_front / front_wheel
    //   rearwheel / wheel_rear / rear_wheel
    //   crank
    //   gripl / grip_left / handl
    //   gripr / grip_right / handr
    // 未找到的部件会保留占位模型对应部件，避免动画失效。
    public void BindExternal(Transform root)
    {
        if (root == null) return;
        visualRoot = root;

        Transform fw = FindByKeywords(root, "frontwheel", "wheel_front", "front_wheel", "front wheel");
        Transform rw = FindByKeywords(root, "rearwheel", "wheel_rear", "rear_wheel", "rear wheel");
        Transform ck = FindByKeywords(root, "crank");
        Transform gl = FindByKeywords(root, "gripl", "grip_left", "grip left", "handl");
        Transform gr = FindByKeywords(root, "gripr", "grip_right", "grip right", "handr");

        if (fw != null) frontWheel = fw;
        if (rw != null) rearWheel = rw;
        if (ck != null) crank = ck;
        if (gl != null) gripLeft = gl;
        if (gr != null) gripRight = gr;

        if (placeholderRoot != null) placeholderRoot.gameObject.SetActive(false);
        Debug.Log("BikeVisualRig: 已绑定外部模型 " + root.name +
                  "（前轮:" + (fw != null) + " 后轮:" + (rw != null) +
                  " 曲柄:" + (ck != null) + " 车把:" + (gl != null && gr != null) + "）");
    }

    static Transform FindByKeywords(Transform root, params string[] keys)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant().Replace(" ", "");
            foreach (var k in keys)
            {
                if (n.Contains(k.Replace(" ", ""))) return t;
            }
        }
        return null;
    }

    // ============ 占位模型构建 ============
    void BuildPlaceholder()
    {
        frameMat = MakeMat(new Color(0.16f, 0.35f, 0.55f));
        darkMat = MakeMat(new Color(0.10f, 0.10f, 0.11f));
        metalMat = MakeMat(new Color(0.62f, 0.64f, 0.68f));

        var root = new GameObject("BikePlaceholder");
        root.transform.SetParent(transform, false);
        placeholderRoot = root.transform;

        // 双轮
        frontWheel = MakeWheel("FrontWheel", new Vector3(0f, wheelRadius, 0.95f));
        rearWheel = MakeWheel("RearWheel", new Vector3(0f, wheelRadius, -0.55f));

        // 车架
        Tube("SeatTube", new Vector3(0f, 0.30f, 0.02f), new Vector3(0f, 1.00f, -0.26f), 0.022f, frameMat);
        Tube("TopTube", new Vector3(0f, 0.99f, -0.24f), new Vector3(0f, 0.95f, 0.46f), 0.020f, frameMat);
        Tube("DownTube", new Vector3(0f, 0.30f, 0.02f), new Vector3(0f, 0.50f, 0.48f), 0.024f, frameMat);
        Tube("HeadTube", new Vector3(0f, 0.50f, 0.48f), new Vector3(0f, 0.95f, 0.46f), 0.026f, frameMat);
        Tube("Fork", new Vector3(0f, 0.50f, 0.48f), new Vector3(0f, wheelRadius, 0.95f), 0.016f, metalMat);
        Tube("ChainStayL", new Vector3(-0.03f, 0.30f, 0.02f), new Vector3(-0.03f, wheelRadius, -0.55f), 0.012f, frameMat);
        Tube("ChainStayR", new Vector3(0.03f, 0.30f, 0.02f), new Vector3(0.03f, wheelRadius, -0.55f), 0.012f, frameMat);
        Tube("SeatStayL", new Vector3(-0.03f, 0.98f, -0.24f), new Vector3(-0.03f, wheelRadius, -0.55f), 0.010f, frameMat);
        Tube("SeatStayR", new Vector3(0.03f, 0.98f, -0.24f), new Vector3(0.03f, wheelRadius, -0.55f), 0.010f, frameMat);

        // 坐垫
        Part("Saddle", placeholderRoot, PrimitiveType.Cube,
            new Vector3(0f, 1.02f, -0.28f), new Vector3(0.12f, 0.035f, 0.26f),
            Quaternion.identity, darkMat);

        // 车把 + 把立
        Tube("Stem", new Vector3(0f, 0.95f, 0.45f), new Vector3(0f, handlebarHeight, handlebarForward), 0.018f, metalMat);
        Part("Handlebar", placeholderRoot, PrimitiveType.Cylinder,
            new Vector3(0f, handlebarHeight, handlebarForward),
            new Vector3(0.022f, gripHalfWidth + 0.06f, 0.022f),
            Quaternion.Euler(0f, 0f, 90f), metalMat);

        gripLeft = new GameObject("GripL").transform;
        gripLeft.SetParent(placeholderRoot, false);
        gripLeft.localPosition = new Vector3(-gripHalfWidth, handlebarHeight, handlebarForward);
        gripRight = new GameObject("GripR").transform;
        gripRight.SetParent(placeholderRoot, false);
        gripRight.localPosition = new Vector3(gripHalfWidth, handlebarHeight, handlebarForward);

        Part("GripLMesh", gripLeft, PrimitiveType.Cylinder,
            Vector3.zero, new Vector3(0.045f, 0.055f, 0.045f),
            Quaternion.Euler(0f, 0f, 90f), darkMat);
        Part("GripRMesh", gripRight, PrimitiveType.Cylinder,
            Vector3.zero, new Vector3(0.045f, 0.055f, 0.045f),
            Quaternion.Euler(0f, 0f, 90f), darkMat);

        // 曲柄 + 脚踏（绕本地 X 轴旋转）
        var crankRoot = new GameObject("Crank");
        crankRoot.transform.SetParent(placeholderRoot, false);
        crankRoot.transform.localPosition = new Vector3(0f, 0.30f, 0.02f);
        crank = crankRoot.transform;

        Part("CrankAxle", crank, PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(0.05f, 0.06f, 0.05f), Quaternion.Euler(0f, 0f, 90f), metalMat);
        Part("CrankArmL", crank, PrimitiveType.Cube, new Vector3(0f, 0.085f, 0f),
            new Vector3(0.025f, 0.17f, 0.025f), Quaternion.identity, metalMat);
        Part("CrankArmR", crank, PrimitiveType.Cube, new Vector3(0f, -0.085f, 0f),
            new Vector3(0.025f, 0.17f, 0.025f), Quaternion.identity, metalMat);
        Part("PedalL", crank, PrimitiveType.Cube, new Vector3(-0.10f, 0.17f, 0f),
            new Vector3(0.10f, 0.02f, 0.06f), Quaternion.identity, darkMat);
        Part("PedalR", crank, PrimitiveType.Cube, new Vector3(0.10f, -0.17f, 0f),
            new Vector3(0.10f, 0.02f, 0.06f), Quaternion.identity, darkMat);
    }

    Transform MakeWheel(string name, Vector3 localPos)
    {
        var spin = new GameObject(name).transform;
        spin.SetParent(placeholderRoot, false);
        spin.localPosition = localPos;

        Part(name + "_Tire", spin, PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(wheelRadius * 2f, 0.02f, wheelRadius * 2f),
            Quaternion.Euler(0f, 0f, 90f), darkMat);
        Part(name + "_Rim", spin, PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(wheelRadius * 1.86f, 0.012f, wheelRadius * 1.86f),
            Quaternion.Euler(0f, 0f, 90f), metalMat);
        return spin;
    }

    void Tube(string name, Vector3 a, Vector3 b, float radius, Material mat)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 1e-4f) return;
        Part(name, placeholderRoot, PrimitiveType.Cylinder, (a + b) * 0.5f,
            new Vector3(radius * 2f, len * 0.5f, radius * 2f),
            Quaternion.FromToRotation(Vector3.up, d.normalized), mat);
    }

    Transform Part(string name, Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = rot;
        t.localScale = scale;
        var r = go.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;
        return t;
    }

    static Material MakeMat(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.color = c;
        return m;
    }
}
