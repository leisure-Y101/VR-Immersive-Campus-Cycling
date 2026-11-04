using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Adds an adjustable traffic-light and low-poly pedestrian overlay to a 360 video.
/// All positions are expressed as yaw/pitch angles around the video sphere, so the
/// overlay remains fixed in the video world while the rider looks around.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(120)]
public class VideoTrafficOverlay : MonoBehaviour
{
    public enum TrafficState { Off, Red, Yellow, Green }

    [Header("Video timing")]
    [Tooltip("Seconds from the beginning of the video when the traffic event starts.")]
    [Min(0f)] public float eventStartTime = 20f;
    [Min(0.1f)] public float redDuration = 6f;
    [Min(0.1f)] public float yellowDuration = 2f;
    [Min(0.1f)] public float greenDuration = 6f;
    [Tooltip("Repeat the light cycle while the video continues. Turn off for a one-shot event.")]
    public bool repeatCycle = false;
    [Tooltip("Keep the overlay visible while the video is paused so it can be positioned in the Inspector.")]
    public bool previewWhenPaused = true;
    [Min(0f)] public float previewTimeSeconds = 20f;

    [Header("360 anchor")]
    [Tooltip("Optional stable world anchor. If empty, the first VideoPlayer/PanoSphere is used.")]
    public Transform videoWorldAnchor;
    [Tooltip("Distance from the video viewpoint. Increase if the model is hidden by the panorama sphere.")]
    [Min(0.1f)] public float overlayRadius = 8f;
    [Tooltip("Yaw is in degrees: 0 = forward, negative = left, positive = right.")]
    public float trafficLightYaw = 0f;
    [Tooltip("Positive pitch is above the forward horizon.")]
    public float trafficLightPitch = 16f;
    [Min(0.01f)] public float trafficLightSize = 0.42f;
    [Tooltip("Yaw/pitch of the center of the crossing line.")]
    public float crossingCenterYaw = 0f;
    public float crossingPitch = -14f;
    [Tooltip("Half width of the crossing line in degrees.")]
    [Range(1f, 60f)] public float crossingHalfWidth = 22f;
    [Min(0.1f)] public float pedestrianRadius = 7.9f;

    [Header("Pedestrians")]
    [Range(1, 12)] public int pedestrianCount = 4;
    [Min(0.1f)] public float pedestrianHeight = 0.55f;
    [Min(0.1f)] public float pedestrianWalkDuration = 5.2f;
    [Min(0f)] public float pedestrianSpawnInterval = 0.45f;
    [Tooltip("Move from left to right when true; reverse the crossing when false.")]
    public bool pedestriansLeftToRight = true;
    [Tooltip("Allow people to finish crossing after the light turns yellow/green.")]
    public bool pedestriansFinishAfterRed = true;
    [Min(0.01f)] public float pedestrianStride = 0.18f;
    public Color pedestrianColor = new Color(0.08f, 0.55f, 0.78f, 1f);
    public Color pedestrianAccentColor = new Color(0.15f, 0.9f, 1f, 1f);
    public bool useFootShadow = true;

    [Header("Editor preview")]
    public bool showGizmos = true;
    public bool forcePreviewInEditor = false;

    [Header("Runtime debug")]
    [SerializeField] bool showRuntimeLabel = true;
    [SerializeField] bool logResolvedVideo = false;

    VideoPlayer video;
    Transform anchor;
    GameObject overlayRoot;
    GameObject trafficRoot;
    Renderer redBulb, yellowBulb, greenBulb;
    Material redMaterial, yellowMaterial, greenMaterial;
    readonly List<Walker> walkers = new List<Walker>();
    float lastVideoTime = -1f;
    bool wasPlaying;
    TrafficState currentState;
    GUIStyle labelStyle;

    class Walker
    {
        public GameObject root;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform leftArm;
        public Transform rightArm;
        public Transform head;
        public Transform shadow;
        public Material bodyMaterial;
        public float spawnTime;
        public int index;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallAtRuntime()
    {
        // Traffic-light and pedestrian overlays were removed from the minimal
        // demo flow. Keep this legacy component available for old scenes, but
        // never install it implicitly into the current application.
    }

    void OnEnable()
    {
        ResolveReferences();
        EnsureOverlay();
        RebuildWalkers();
        UpdateVisuals(GetCurrentTime(), true);
    }

    void Start()
    {
        ResolveReferences();
        EnsureOverlay();
        RebuildWalkers();
        UpdateVisuals(GetCurrentTime(), true);
    }

    void Update()
    {
        ResolveReferences();
        if (video == null && !forcePreviewInEditor) return;
        if (overlayRoot != null && anchor != null)
        {
            overlayRoot.transform.position = anchor.position;
            overlayRoot.transform.rotation = Quaternion.identity;
        }
        float time = GetCurrentTime();
        bool looped = lastVideoTime >= 0f && time + 0.25f < lastVideoTime;
        if (looped)
        {
            ResetWalkers();
            currentState = TrafficState.Off;
        }

        bool playing = video != null && video.isPlaying;
        if (playing || previewWhenPaused || forcePreviewInEditor)
            UpdateVisuals(time, looped);
        else
            SetOverlayVisible(false);

        lastVideoTime = time;
        wasPlaying = playing;
    }

    void ResolveReferences()
    {
        if (video == null)
        {
            video = GetComponent<VideoPlayer>();
            if (video == null) video = FindAnyObjectByType<VideoPlayer>();
        }

        if (videoWorldAnchor != null)
        {
            anchor = videoWorldAnchor;
        }
        else if (anchor == null)
        {
            var pano = GameObject.Find("PanoSphere");
            if (pano != null) anchor = pano.transform;
            else if (video != null) anchor = video.transform;
            else anchor = transform;
        }

        if (logResolvedVideo && video != null)
        {
            logResolvedVideo = false;
            Debug.Log("VideoTrafficOverlay: using VideoPlayer on " + video.gameObject.name);
        }
    }

    float GetCurrentTime()
    {
        if (forcePreviewInEditor || video == null) return previewTimeSeconds;
        if (!Application.isPlaying && previewWhenPaused) return previewTimeSeconds;
        if (!video.isPlaying && previewWhenPaused) return previewTimeSeconds;
        return (float)video.time;
    }

    void EnsureOverlay()
    {
        if (anchor == null) return;
        if (overlayRoot == null)
        {
            overlayRoot = new GameObject("VideoTrafficOverlay");
            // Keep the overlay out of the panorama sphere hierarchy. The sphere
            // is often scaled very large, which would otherwise scale the UI and
            // low-poly people with it.
            overlayRoot.transform.SetParent(null, false);
            overlayRoot.transform.position = anchor.position;
            overlayRoot.transform.rotation = Quaternion.identity;
            overlayRoot.transform.localScale = Vector3.one;
        }

        if (trafficRoot == null)
        {
            trafficRoot = new GameObject("TrafficLight");
            trafficRoot.transform.SetParent(overlayRoot.transform, false);
            BuildTrafficLight();
        }
    }

    void BuildTrafficLight()
    {
        if (trafficRoot == null) return;
        foreach (Transform child in trafficRoot.transform) DestroySafe(child.gameObject);

        var frame = CreatePrimitive(PrimitiveType.Cube, "TrafficLightFrame", trafficRoot.transform);
        frame.transform.localScale = new Vector3(0.18f, 0.58f, 0.13f);
        frame.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(0.015f, 0.025f, 0.05f, 1f));

        redBulb = CreateBulb("Red", trafficRoot.transform, new Vector3(0f, 0.18f, -0.09f), Color.red, out redMaterial);
        yellowBulb = CreateBulb("Yellow", trafficRoot.transform, new Vector3(0f, 0f, -0.09f), new Color(1f, 0.65f, 0.02f), out yellowMaterial);
        greenBulb = CreateBulb("Green", trafficRoot.transform, new Vector3(0f, -0.18f, -0.09f), Color.green, out greenMaterial);
        trafficRoot.transform.localScale = Vector3.one * trafficLightSize;
    }

    Renderer CreateBulb(string name, Transform parent, Vector3 localPosition, Color color, out Material material)
    {
        var bulb = CreatePrimitive(PrimitiveType.Sphere, name, parent);
        bulb.transform.localPosition = localPosition;
        bulb.transform.localScale = Vector3.one * 0.13f;
        material = MakeMaterial(color);
        bulb.GetComponent<Renderer>().sharedMaterial = material;
        return bulb.GetComponent<Renderer>();
    }

    void RebuildWalkers()
    {
        if (overlayRoot == null) return;
        for (int i = walkers.Count - 1; i >= 0; i--)
        {
            if (walkers[i].root != null) DestroySafe(walkers[i].root);
        }
        walkers.Clear();

        var peopleRoot = new GameObject("LowPolyPedestrians");
        peopleRoot.transform.SetParent(overlayRoot.transform, false);
        for (int i = 0; i < pedestrianCount; i++)
            walkers.Add(CreateWalker(peopleRoot.transform, i));
    }

    Walker CreateWalker(Transform parent, int index)
    {
        var walker = new Walker { index = index };
        walker.root = new GameObject("Pedestrian_" + (index + 1));
        walker.root.transform.SetParent(parent, false);
        walker.root.transform.localScale = Vector3.one * pedestrianHeight;
        walker.bodyMaterial = MakeMaterial(Color.Lerp(pedestrianColor, pedestrianAccentColor, index / Mathf.Max(1f, pedestrianCount - 1f)));

        var torso = CreatePrimitive(PrimitiveType.Capsule, "Body", walker.root.transform);
        torso.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        torso.transform.localScale = new Vector3(0.32f, 0.55f, 0.22f);
        torso.GetComponent<Renderer>().sharedMaterial = walker.bodyMaterial;

        walker.head = CreatePrimitive(PrimitiveType.Sphere, "Head", walker.root.transform).transform;
        walker.head.localPosition = new Vector3(0f, 1.72f, 0f);
        walker.head.localScale = Vector3.one * 0.26f;
        walker.head.GetComponent<Renderer>().sharedMaterial = walker.bodyMaterial;

        walker.leftLeg = CreateLimb(walker.root.transform, "LeftLeg", new Vector3(-0.11f, 0.38f, 0f), new Vector3(0.11f, 0.42f, 0.11f), walker.bodyMaterial);
        walker.rightLeg = CreateLimb(walker.root.transform, "RightLeg", new Vector3(0.11f, 0.38f, 0f), new Vector3(0.11f, 0.42f, 0.11f), walker.bodyMaterial);
        walker.leftArm = CreateLimb(walker.root.transform, "LeftArm", new Vector3(-0.25f, 1.08f, 0f), new Vector3(0.09f, 0.36f, 0.09f), walker.bodyMaterial);
        walker.rightArm = CreateLimb(walker.root.transform, "RightArm", new Vector3(0.25f, 1.08f, 0f), new Vector3(0.09f, 0.36f, 0.09f), walker.bodyMaterial);

        if (useFootShadow)
        {
            walker.shadow = CreatePrimitive(PrimitiveType.Quad, "FootShadow", walker.root.transform).transform;
            walker.shadow.localPosition = new Vector3(0f, 0.015f, 0f);
            walker.shadow.localRotation = Quaternion.Euler(90f, 0f, 0f);
            walker.shadow.localScale = new Vector3(0.45f, 0.18f, 1f);
            walker.shadow.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(0f, 0.03f, 0.08f, 0.45f));
        }

        walker.spawnTime = redDuration <= 0f ? 0f : index * pedestrianSpawnInterval;
        walker.root.SetActive(false);
        return walker;
    }

    Transform CreateLimb(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var limb = CreatePrimitive(PrimitiveType.Cube, name, parent);
        limb.transform.localPosition = position;
        limb.transform.localScale = scale;
        limb.GetComponent<Renderer>().sharedMaterial = material;
        return limb.transform;
    }

    void UpdateVisuals(float videoTime, bool reset)
    {
        if (anchor == null || overlayRoot == null) return;
        if (reset) ResetWalkers();

        float elapsed = videoTime - eventStartTime;
        if (elapsed < 0f)
        {
            currentState = TrafficState.Off;
            SetOverlayVisible(previewWhenPaused || forcePreviewInEditor);
            PlaceTrafficLight();
            DeactivateWalkers();
            return;
        }

        float cycleLength = redDuration + yellowDuration + greenDuration;
        float cycleTime = repeatCycle && cycleLength > 0.01f ? Mathf.Repeat(elapsed, cycleLength) : elapsed;
        currentState = cycleTime < redDuration ? TrafficState.Red :
            cycleTime < redDuration + yellowDuration ? TrafficState.Yellow :
            cycleTime < cycleLength || repeatCycle ? TrafficState.Green : TrafficState.Off;

        PlaceTrafficLight();
        SetOverlayVisible(currentState != TrafficState.Off || previewWhenPaused || forcePreviewInEditor);
        UpdateWalkers(elapsed);
    }

    void PlaceTrafficLight()
    {
        if (trafficRoot == null) return;
        trafficRoot.transform.position = SphericalPosition(trafficLightYaw, trafficLightPitch, overlayRadius);
        trafficRoot.transform.rotation = LookOutward(trafficRoot.transform.position);
        trafficRoot.transform.localScale = Vector3.one * trafficLightSize;
        ApplyBulb(redBulb, redMaterial, Color.red, currentState == TrafficState.Red);
        ApplyBulb(yellowBulb, yellowMaterial, new Color(1f, 0.65f, 0.02f), currentState == TrafficState.Yellow);
        ApplyBulb(greenBulb, greenMaterial, Color.green, currentState == TrafficState.Green);
    }

    void UpdateWalkers(float elapsed)
    {
        bool allow = elapsed >= 0f && (currentState == TrafficState.Red || pedestriansFinishAfterRed);
        for (int i = 0; i < walkers.Count; i++)
        {
            var walker = walkers[i];
            float local = elapsed - walker.spawnTime;
            bool active = allow && local >= 0f && local < pedestrianWalkDuration;
            if (!active)
            {
                if (walker.root != null) walker.root.SetActive(false);
                continue;
            }

            walker.root.SetActive(true);
            float t = Mathf.Clamp01(local / Mathf.Max(0.1f, pedestrianWalkDuration));
            float startYaw = crossingCenterYaw - crossingHalfWidth;
            float endYaw = crossingCenterYaw + crossingHalfWidth;
            if (!pedestriansLeftToRight)
            {
                float temp = startYaw;
                startYaw = endYaw;
                endYaw = temp;
            }
            Vector3 start = SphericalPosition(startYaw, crossingPitch, pedestrianRadius);
            Vector3 end = SphericalPosition(endYaw, crossingPitch, pedestrianRadius);
            walker.root.transform.position = Vector3.Lerp(start, end, t);
            walker.root.transform.rotation = Quaternion.LookRotation((end - start).normalized, Vector3.up);
            float stride = Mathf.Sin((local / Mathf.Max(0.1f, pedestrianStride)) * Mathf.PI * 2f) * 28f;
            walker.leftLeg.localRotation = Quaternion.Euler(stride, 0f, 0f);
            walker.rightLeg.localRotation = Quaternion.Euler(-stride, 0f, 0f);
            walker.leftArm.localRotation = Quaternion.Euler(-stride * 0.7f, 0f, 0f);
            walker.rightArm.localRotation = Quaternion.Euler(stride * 0.7f, 0f, 0f);
            walker.head.localRotation = Quaternion.Euler(0f, Mathf.Sin(local * 2f) * 3f, 0f);

            float fade = Mathf.Clamp01(Mathf.Min(t / 0.12f, (1f - t) / 0.14f) * 5f);
            // The project can use either built-in or URP materials. A small
            // scale fade keeps the entrance/exit soft even when the selected
            // shader does not support transparent alpha.
            float visibleScale = Mathf.Lerp(0.18f, 1f, fade);
            walker.root.transform.localScale = Vector3.one * pedestrianHeight * visibleScale;
            SetWalkerAlpha(walker, fade);
        }
    }

    Vector3 SphericalPosition(float yaw, float pitch, float radius)
    {
        Transform basis = anchor != null ? anchor : transform;
        Vector3 forward = basis.forward;
        Vector3 right = basis.right;
        Vector3 up = basis.up;
        Quaternion yawRotation = Quaternion.AngleAxis(yaw, up);
        Vector3 yawForward = yawRotation * forward;
        Quaternion pitchRotation = Quaternion.AngleAxis(-pitch, right);
        Vector3 direction = pitchRotation * yawForward;
        return basis.position + direction.normalized * radius;
    }

    Quaternion LookOutward(Vector3 worldPosition)
    {
        Transform basis = anchor != null ? anchor : transform;
        Vector3 outward = (worldPosition - basis.position).normalized;
        return Quaternion.LookRotation(-outward, basis.up);
    }

    void SetWalkerAlpha(Walker walker, float alpha)
    {
        if (walker.bodyMaterial == null) return;
        Color color = Color.Lerp(pedestrianColor, pedestrianAccentColor, walker.index / Mathf.Max(1f, pedestrianCount - 1f));
        color.a = Mathf.Clamp01(alpha);
        walker.bodyMaterial.color = color;
        if (walker.shadow != null)
        {
            var renderer = walker.shadow.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Color shadow = renderer.sharedMaterial.color;
                shadow.a = 0.45f * alpha;
                renderer.sharedMaterial.color = shadow;
            }
        }
    }

    void SetOverlayVisible(bool visible)
    {
        if (overlayRoot != null) overlayRoot.SetActive(visible);
    }

    void ResetWalkers()
    {
        for (int i = 0; i < walkers.Count; i++)
        {
            if (walkers[i].root != null) walkers[i].root.SetActive(false);
        }
    }

    void DeactivateWalkers()
    {
        for (int i = 0; i < walkers.Count; i++)
        {
            if (walkers[i].root != null) walkers[i].root.SetActive(false);
        }
    }

    GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        var collider = go.GetComponent<Collider>();
        if (collider != null) DestroySafe(collider);
        return go;
    }

    Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        var material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }

    void ApplyBulb(Renderer bulb, Material material, Color color, bool on)
    {
        if (bulb == null || material == null) return;
        Color c = on ? color : color * 0.12f;
        c.a = 1f;
        material.color = c;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", on ? color * 1.7f : Color.black);
            if (on) material.EnableKeyword("_EMISSION");
        }
    }

    void OnGUI()
    {
        if (!showRuntimeLabel || !Application.isPlaying || !enabled) return;
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
        }
        GUI.Label(new Rect(18f, 205f, 700f, 28f),
            "Traffic overlay  " + currentState + "  |  video " + GetCurrentTime().ToString("F1") + "s", labelStyle);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        ResolveReferences();
        if (anchor == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(SphericalPosition(trafficLightYaw, trafficLightPitch, overlayRadius), 0.14f * trafficLightSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(SphericalPosition(crossingCenterYaw - crossingHalfWidth, crossingPitch, pedestrianRadius),
            SphericalPosition(crossingCenterYaw + crossingHalfWidth, crossingPitch, pedestrianRadius));
    }

    void OnValidate()
    {
        pedestrianCount = Mathf.Clamp(pedestrianCount, 1, 12);
        eventStartTime = Mathf.Max(0f, eventStartTime);
        redDuration = Mathf.Max(0.1f, redDuration);
        yellowDuration = Mathf.Max(0.1f, yellowDuration);
        greenDuration = Mathf.Max(0.1f, greenDuration);
        overlayRadius = Mathf.Max(0.1f, overlayRadius);
        pedestrianRadius = Mathf.Max(0.1f, pedestrianRadius);
        pedestrianWalkDuration = Mathf.Max(0.1f, pedestrianWalkDuration);
        if (!Application.isPlaying && isActiveAndEnabled)
        {
            ResolveReferences();
            EnsureOverlay();
            RebuildWalkers();
            UpdateVisuals(previewTimeSeconds, true);
        }
    }

    void OnDestroy()
    {
        if (overlayRoot != null) DestroySafe(overlayRoot);
    }

    void DestroySafe(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
