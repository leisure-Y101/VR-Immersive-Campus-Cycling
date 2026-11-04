using UnityEngine;

/// <summary>
/// PICO/XR 原生手柄可视化与 UI 射线层。
/// 不依赖未确认的 PICO 私有 API：位姿来自 UnityEngine.XR.InputDevices，
/// 射线由 CampusRideApp 处理悬停和 Trigger 点击。
/// </summary>
[DefaultExecutionOrder(110)]
public sealed class FreeBikeControllerRays : MonoBehaviour
{
    [Header("射线显示")]
    [SerializeField] bool showDuringMenus = true;
    [SerializeField] bool hideDuringRide = true;
    [SerializeField, Range(1f, 10f)] float rayLength = 5f;
    [SerializeField, Range(0.001f, 0.02f)] float rayWidth = 0.006f;
    [SerializeField] Color leftRayColor = new Color(0.15f, 0.75f, 1f, 0.9f);
    [SerializeField] Color rightRayColor = new Color(0.45f, 1f, 0.82f, 0.9f);
    [SerializeField, Range(0.01f, 0.08f)] float controllerMarkerSize = 0.035f;

    InputRouter input;
    CampusRideApp app;
    Camera cam;
    LineRenderer leftLine;
    LineRenderer rightLine;
    Transform leftMarker;
    Transform rightMarker;
    Material leftMaterial;
    Material rightMaterial;

    void Awake()
    {
        input = GetComponent<InputRouter>() ?? FindAnyObjectByType<InputRouter>();
        app = GetComponent<CampusRideApp>() ?? FindAnyObjectByType<CampusRideApp>();
        cam = Camera.main ?? FindAnyObjectByType<Camera>();
        if (cam == null) { enabled = false; return; }

        leftMaterial = MakeMaterial(leftRayColor);
        rightMaterial = MakeMaterial(rightRayColor);
        leftLine = MakeLine("LeftControllerRay", leftMaterial);
        rightLine = MakeLine("RightControllerRay", rightMaterial);
        leftMarker = MakeMarker("LeftControllerMarker", leftMaterial);
        rightMarker = MakeMarker("RightControllerMarker", rightMaterial);
    }

    LineRenderer MakeLine(string name, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = rayWidth;
        line.endWidth = rayWidth * 0.35f;
        line.material = material;
        line.numCapVertices = 4;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = 300;
        return line;
    }

    Transform MakeMarker(string name, Material material)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        marker.name = name;
        marker.SetParent(transform, false);
        marker.localScale = Vector3.one * controllerMarkerSize;
        var collider = marker.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        var renderer = marker.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return marker;
    }

    Material MakeMaterial(Color color)
    {
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader) { color = color };
        return mat;
    }

    void Update()
    {
        if (input == null || cam == null) return;
        bool visible = showDuringMenus && (app == null || !app.IsRiding || !hideDuringRide);
        SetVisible(leftLine, visible);
        SetVisible(rightLine, visible);
        SetVisible(leftMarker, visible);
        SetVisible(rightMarker, visible);
        if (!visible) return;

        UpdateHand(input.LeftHandPosition, input.LeftHandRotation, leftLine, leftMarker);
        UpdateHand(input.RightHandPosition, input.RightHandRotation, rightLine, rightMarker);
    }

    void UpdateHand(Vector3 localPosition, Quaternion localRotation, LineRenderer line, Transform marker)
    {
        bool valid = localPosition.sqrMagnitude > 0.0001f;
        line.enabled = valid;
        marker.gameObject.SetActive(valid);
        if (!valid) return;

        Vector3 origin = cam.transform.TransformPoint(localPosition);
        Vector3 direction = cam.transform.TransformDirection(localRotation * Vector3.forward).normalized;
        marker.position = origin;
        marker.rotation = cam.transform.rotation * localRotation;
        line.SetPosition(0, origin);
        line.SetPosition(1, origin + direction * rayLength);
    }

    public void SetUiVisible(bool visible)
    {
        SetVisible(leftLine, visible);
        SetVisible(rightLine, visible);
        SetVisible(leftMarker, visible);
        SetVisible(rightMarker, visible);
    }

    void SetVisible(LineRenderer line, bool visible)
    {
        if (line != null) line.enabled = visible;
    }

    void SetVisible(Transform marker, bool visible)
    {
        if (marker != null && marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
    }

    void OnDestroy()
    {
        if (leftMaterial != null) Destroy(leftMaterial);
        if (rightMaterial != null) Destroy(rightMaterial);
    }
}
