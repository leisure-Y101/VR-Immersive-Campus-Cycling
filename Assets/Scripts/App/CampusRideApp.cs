using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// FreeBike 极简 Demo 主控（2026-10-27）。
/// 主页：8K 全景背景 + 一个“开始”按钮。
/// 骑行：只播放 6K 360 视频，不显示任何数据或提示。
/// 暂停：骑行中按住 B 约 0.25 秒，出现“继续 / 退出”两个按钮；退出回主页。
/// </summary>
[DefaultExecutionOrder(100)]
public class CampusRideApp : MonoBehaviour
{
    enum UiState { Home, Riding, Paused }

    [Header("主页")]
    [Tooltip("Resources 里的 8K 等距柱状全景图名（不带扩展名）。")]
    [SerializeField] string panoramaResource = "homepage";
    [SerializeField] Vector3 startButtonOffset = new Vector3(0f, -0.12f, 2.2f);
    [SerializeField] Vector2 startButtonSize = new Vector2(560f, 132f);
    [SerializeField, Range(0.0005f, 0.002f)] float uiScale = 0.001f;
    [SerializeField] Color panelColor = new Color(0.008f, 0.082f, 0.145f, 0.92f);
    [SerializeField] Color panelSelectedColor = new Color(0.031f, 0.43f, 0.52f, 0.96f);
    [SerializeField] Color accentColor = new Color(0.125f, 0.906f, 1f, 0.95f);
    [SerializeField] Color labelColor = new Color(0.949f, 0.988f, 1f, 1f);
    [SerializeField] Color labelDimColor = new Color(0.68f, 0.82f, 0.88f, 1f);
    [SerializeField] int startFontSize = 54;

    [Header("暂停")]
    [Tooltip("按住 B 多久后暂停（秒）。设小一点更接近“按下即暂停”。")]
    [SerializeField, Min(0.05f)] float pauseHoldSeconds = 0.25f;
    [SerializeField] Vector3 pausePanelOffset = new Vector3(0f, 0f, 1.6f);
    [SerializeField] Vector2 pausePanelSize = new Vector2(720f, 460f);
    [SerializeField] Vector2 pauseButtonSize = new Vector2(520f, 110f);
    [SerializeField] float pauseButtonSpacing = 150f;
    [SerializeField, Range(0f, 1f)] float pauseDimAlpha = 0.62f;
    [SerializeField] int pauseFontSize = 44;
    [Tooltip("可选：在暂停面板底部显示“摇杆选择 · Trigger 确认”。默认关闭。")]
    [SerializeField] bool showControlHint = false;

    [Header("视频")]
    [Tooltip("主视频在 StreamingAssets 下的相对路径。")]
    [SerializeField] string packagedVideoRelativePath = "6k60fps_hevc.mp4";
    [Tooltip("主视频的外部回退路径（开发机）。")]
    [SerializeField] string externalVideoPath = @"D:\freebike\Media\Video\6k60fps_hevc.mp4";
    [Tooltip("主视频解不了时使用的兼容视频路径。")]
    [SerializeField] string fallbackPackagedRelativePath = "6k60fps_hevc.mp4";
    [Tooltip("兼容视频的外部回退路径（开发机）。")]
    [SerializeField] string fallbackExternalVideoPath = @"D:\freebike\Media\Video\6k60fps_hevc.mp4";
    [Tooltip("进入骑行后多久还没播起来，就切换到兼容视频。")]
    [SerializeField, Min(1f)] float fallbackAfterSeconds = 6f;
    [SerializeField] bool loopVideo = true;
    [SerializeField] bool skipOnDrop = true;

    InputRouter input;
    PanoBikeSpeed bike;
    VideoPlayer video;
    GameObject panoSphere;
    Camera cam;
    Font font;

    GameObject menuSphere;
    Material menuMaterial;
    Texture2D menuTexture;

    Canvas homeCanvas;
    Canvas pauseCanvas;
    readonly List<MenuButton> pauseButtons = new List<MenuButton>();
    int pauseIndex;

    UiState state = UiState.Home;
    float backHold;
    bool backArmed = true;

    string primaryUrl;
    string fallbackUrl;
    bool usingFallback;
    float rideStartTime = -1f;

    public bool IsRiding => state == UiState.Riding;

    class MenuButton
    {
        public RectTransform rect;
        public Image background;
        public Text label;
        public Action action;
    }

    void Awake()
    {
        // 场景里有一个完全重复的 PanoSphere (1)（同样带 VideoPlayer + PanoBikeSpeed）。
        // 在 Start 之前关掉：避免两个 800m 球重叠、两个 PanoBikeSpeed 抢 UDP 50506。
        var duplicate = GameObject.Find("PanoSphere (1)");
        if (duplicate != null)
        {
            duplicate.SetActive(false);
            Debug.Log("CampusRideApp: 已关闭重复的 PanoSphere (1)。");
        }
    }

    void Start()
    {
        Application.runInBackground = true;

        input = GetComponent<InputRouter>();
        if (input == null) input = gameObject.AddComponent<InputRouter>();

        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            Debug.LogError("CampusRideApp: 场景里没有相机，脚本停止。");
            enabled = false;
            return;
        }

        panoSphere = GameObject.Find("PanoSphere");
        if (panoSphere != null) video = panoSphere.GetComponent<VideoPlayer>();
        bike = FindAnyObjectByType<PanoBikeSpeed>();
        if (video == null && bike != null) video = bike.video;
        if (video == null) video = FindAnyObjectByType<VideoPlayer>();
        if (panoSphere == null && bike != null) panoSphere = bike.gameObject;

#if UNITY_EDITOR
        var editorHead = cam.GetComponent<HeadPoseDriver>();
        if (editorHead != null) editorHead.enabled = false;
        cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cam.transform.localRotation = Quaternion.identity;
        var editorLook = cam.GetComponent<SimpleLook>();
        if (editorLook == null) editorLook = cam.gameObject.AddComponent<SimpleLook>();
        editorLook.enabled = true;
        editorLook.sensitivity = 2f;
#else
        foreach (var look in FindObjectsByType<SimpleLook>(FindObjectsInactive.Exclude))
            look.enabled = false;
#endif

        var oldBike = GameObject.Find("Bikerig");
        if (oldBike != null) oldBike.SetActive(false);

        // 这一版不用手柄射线，全部关掉。
        foreach (var rays in FindObjectsByType<FreeBikeControllerRays>(FindObjectsInactive.Include))
        {
            if (rays == null) continue;
            rays.SetUiVisible(false);
            rays.enabled = false;
        }

        if (bike != null)
        {
            bike.showDebugHud = false;
            // 没有真实自行车功率数据时保持 0 速并暂停。
            bike.idlePlayback = 1f;
            bike.allowKeyboardFallback = false;
            bike.controlPlaybackState = true;
        }

        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 32);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        ConfigureVideo();
        BuildHomeUi();
        BuildPauseUi();
        GoHome();
    }

    // ---------------------------------------------------------------- 视频

    void ConfigureVideo()
    {
        if (video == null)
        {
            Debug.LogError("CampusRideApp: 没有找到 VideoPlayer。");
            return;
        }

        primaryUrl = ResolveVideoUrl(packagedVideoRelativePath, externalVideoPath, "主视频");
        fallbackUrl = ResolveVideoUrl(fallbackPackagedRelativePath, fallbackExternalVideoPath, null);

        if (string.IsNullOrEmpty(primaryUrl))
        {
            primaryUrl = fallbackUrl;
            usingFallback = true;
        }
        if (string.IsNullOrEmpty(primaryUrl))
        {
            Debug.LogError("CampusRideApp: 找不到任何可用视频。");
            return;
        }

        video.source = VideoSource.Url;
        video.url = primaryUrl;
        video.playOnAwake = false;
        video.isLooping = loopVideo;
        video.skipOnDrop = skipOnDrop;
        video.renderMode = VideoRenderMode.MaterialOverride;
        if (panoSphere != null)
        {
            var target = panoSphere.GetComponent<Renderer>();
            if (target != null)
            {
                video.targetMaterialRenderer = target;
                video.targetMaterialProperty = target.sharedMaterial != null && target.sharedMaterial.HasProperty("_BaseMap")
                    ? "_BaseMap"
                    : "_MainTex";
            }
        }

        video.errorReceived += OnVideoError;
        video.Prepare();
        Debug.Log("CampusRideApp: 主视频 = " + primaryUrl + "；兼容视频 = " + (fallbackUrl ?? "无"));
    }

    string ResolveVideoUrl(string packagedRelative, string external, string label)
    {
        if (!string.IsNullOrEmpty(packagedRelative))
        {
            string packaged = Path.Combine(Application.streamingAssetsPath, packagedRelative);
            if (File.Exists(packaged)) return packaged;

            string packagedRoot = Path.Combine(Application.streamingAssetsPath, Path.GetFileName(packagedRelative));
            if (File.Exists(packagedRoot)) return packagedRoot;
        }

        if (!string.IsNullOrEmpty(external) && File.Exists(external)) return external;

        if (label != null)
            Debug.LogWarning("CampusRideApp: 找不到" + label + "；请放到 StreamingAssets/" + packagedRelative + " 或 " + external);
        return null;
    }

    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("CampusRideApp: 视频错误: " + message);
        if (!usingFallback) SwitchToFallback();
    }

    void SwitchToFallback()
    {
        if (usingFallback || string.IsNullOrEmpty(fallbackUrl) || video == null) return;

        usingFallback = true;
        Debug.LogWarning("CampusRideApp: 主视频播不了，切换到兼容视频 " + fallbackUrl);
        if (video.isPrepared) video.Stop();
        video.url = fallbackUrl;
        video.Prepare();
        // 兼容视频也必须等待真实自行车数据，不能因切换回退视频而自动播放。
        if (state == UiState.Riding) video.Pause();
    }

    // ---------------------------------------------------------------- 页面

    void GoHome()
    {
        state = UiState.Home;
        rideStartTime = -1f;
        backHold = 0f;
        backArmed = true;

        if (video != null) video.Pause();
        if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(false);
        if (homeCanvas != null) homeCanvas.gameObject.SetActive(true);

        EnsureMenuSphere();
        if (panoSphere != null) panoSphere.SetActive(false);
        if (menuSphere != null) menuSphere.SetActive(true);

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    void StartRide()
    {
        if (state != UiState.Home) return;

        state = UiState.Riding;
        rideStartTime = Time.unscaledTime;
        backHold = 0f;
        backArmed = true;

        if (homeCanvas != null) homeCanvas.gameObject.SetActive(false);
        if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(false);
        if (menuSphere != null) menuSphere.SetActive(false);
        if (panoSphere != null) panoSphere.SetActive(true);

        if (video != null)
        {
            if (video.isPrepared && video.time > 0.05f) video.time = 0f;
            // 先暂停：等 A1 有有效功率（PanoBikeSpeed 里 >=5W）后再播放；停踩自动暂停。
            video.Pause();
        }
    }

    void PauseRide()
    {
        if (state != UiState.Riding) return;

        state = UiState.Paused;
        backHold = 0f;
        backArmed = false; // 必须松开 B 才能再次触发，避免刚暂停又触发

        if (video != null) video.Pause();

        pauseIndex = 0;
        RefreshPause();
        if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(true);
    }

    void ResumeRide()
    {
        if (state != UiState.Paused) return;

        state = UiState.Riding;
        backHold = 0f;
        backArmed = false;

        if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(false);
        // 恢复界面后仍需等待有效自行车数据，避免无数据自动播放。
        if (video != null) video.Pause();
    }

    void ExitToHome()
    {
        // 已取消主页；暂停菜单的“退出”直接退出应用。
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- 输入

    void Update()
    {
        if (input == null) return;

        switch (state)
        {
            case UiState.Home:
                if (input.Confirm) StartRide();
                break;

            case UiState.Riding:
                UpdatePauseHold();
                if (!usingFallback && video != null && !video.isPlaying &&
                    rideStartTime >= 0f && Time.unscaledTime - rideStartTime > fallbackAfterSeconds)
                {
                    SwitchToFallback();
                }
                break;

            case UiState.Paused:
                if (input.NavY != 0) MovePause(input.NavY > 0 ? -1 : 1);
                if (input.Confirm) ActivatePause();
                break;
        }
    }

    void UpdatePauseHold()
    {
        if (!input.BackHeld)
        {
            backHold = 0f;
            backArmed = true;
            return;
        }

        if (!backArmed) return;

        backHold += Time.unscaledDeltaTime;
        if (backHold >= pauseHoldSeconds) PauseRide();
    }

    void MovePause(int direction)
    {
        if (pauseButtons.Count == 0) return;
        pauseIndex = Mathf.Clamp(pauseIndex + direction, 0, pauseButtons.Count - 1);
        RefreshPause();
    }

    void ActivatePause()
    {
        if (pauseIndex < 0 || pauseIndex >= pauseButtons.Count) return;
        pauseButtons[pauseIndex].action?.Invoke();
    }

    // ---------------------------------------------------------------- UI

    void BuildHomeUi()
    {
        homeCanvas = CreateWorldCanvas("HomeUI", 100, startButtonOffset);
        var start = CreateButton(homeCanvas.transform, "开始", startButtonSize, Vector2.zero, startFontSize, StartRide);
        SetButtonVisual(start, true);
    }

    void BuildPauseUi()
    {
        pauseCanvas = CreateWorldCanvas("PauseUI", 120, pausePanelOffset);
        pauseCanvas.gameObject.SetActive(false);

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimGo.transform.SetParent(pauseCanvas.transform, false);
        var dimRect = dimGo.GetComponent<RectTransform>();
        dimRect.anchorMin = dimRect.anchorMax = new Vector2(0.5f, 0.5f);
        dimRect.sizeDelta = new Vector2(4400f, 3600f);
        dimRect.anchoredPosition = Vector2.zero;
        var dim = dimGo.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, pauseDimAlpha);
        dim.raycastTarget = false;

        var panelGo = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(pauseCanvas.transform, false);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = pausePanelSize;
        panelRect.anchoredPosition = Vector2.zero;
        var panel = panelGo.GetComponent<Image>();
        panel.color = panelColor;
        panel.raycastTarget = false;
        AddFrame(panelGo.transform, 28f, 3f);

        var resume = CreateButton(panelGo.transform, "继续", pauseButtonSize,
            new Vector2(0f, pauseButtonSpacing * 0.5f), pauseFontSize, ResumeRide);
        var quit = CreateButton(panelGo.transform, "退出", pauseButtonSize,
            new Vector2(0f, -pauseButtonSpacing * 0.5f), pauseFontSize, ExitToHome);
        pauseButtons.Add(resume);
        pauseButtons.Add(quit);

        if (showControlHint)
        {
            CreateText(pauseCanvas.transform, "ControlHint", "摇杆选择 · Trigger 确认", 18,
                new Vector2(0f, -(pausePanelSize.y * 0.5f) - 55f),
                new Vector2(700f, 40f), labelDimColor);
        }
    }

    Canvas CreateWorldCanvas(string name, int sortingOrder, Vector3 localPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(cam.transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvas.sortingOrder = sortingOrder;
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(1200f, 800f);
        rect.localScale = Vector3.one * uiScale;
        rect.localPosition = localPosition;
        rect.localRotation = Quaternion.identity;
        return canvas;
    }

    MenuButton CreateButton(Transform parent, string label, Vector2 size, Vector2 position, int fontSize, Action action)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var image = go.GetComponent<Image>();
        image.color = panelColor;
        image.raycastTarget = false;

        AddFrame(go.transform, 24f, 3f);
        var text = CreateText(go.transform, "Label", label, fontSize, Vector2.zero, size, labelColor);

        return new MenuButton { rect = rect, background = image, label = text, action = action };
    }

    void AddFrame(Transform owner, float cornerCut, float thickness)
    {
        var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer));
        frameGo.transform.SetParent(owner, false);
        var rect = frameGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var frame = frameGo.AddComponent<CampusRideCutFrame>();
        frame.cornerCut = cornerCut;
        frame.thickness = thickness;
        frame.color = accentColor;
        frame.raycastTarget = false;
    }

    Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.text = content;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    void SetButtonVisual(MenuButton button, bool selected)
    {
        if (button == null) return;
        if (button.background != null) button.background.color = selected ? panelSelectedColor : panelColor;
        if (button.label != null) button.label.color = selected ? labelColor : labelDimColor;
        if (button.rect != null) button.rect.localScale = Vector3.one * (selected ? 1.03f : 1f);
    }

    void RefreshPause()
    {
        for (int i = 0; i < pauseButtons.Count; i++)
            SetButtonVisual(pauseButtons[i], i == pauseIndex);
    }

    // ---------------------------------------------------------------- 8K 主页背景

    void EnsureMenuSphere()
    {
        if (menuSphere != null || panoSphere == null) return;

        if (menuTexture == null) menuTexture = Resources.Load<Texture2D>(panoramaResource);
        if (menuTexture == null)
        {
            Debug.LogWarning("CampusRideApp: 找不到 8K 全景 " + panoramaResource);
            return;
        }

        var source = panoSphere.GetComponent<Renderer>();
        if (source == null || source.sharedMaterial == null)
        {
            Debug.LogWarning("CampusRideApp: PanoSphere 上没有材质，主页只有黑底。");
            return;
        }

        menuSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        menuSphere.name = "HomePanoramaSphere";
        var collider = menuSphere.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        menuMaterial = new Material(source.sharedMaterial);
        menuMaterial.SetTexture("_BaseMap", menuTexture);
        menuMaterial.SetTexture("_MainTex", menuTexture);
        menuMaterial.SetColor("_BaseColor", Color.white);
        menuSphere.GetComponent<Renderer>().sharedMaterial = menuMaterial;

        menuSphere.transform.position = panoSphere.transform.position;
        menuSphere.transform.rotation = panoSphere.transform.rotation;
        menuSphere.transform.localScale = panoSphere.transform.localScale * 0.98f;
    }

    void OnDestroy()
    {
        if (menuSphere != null) Destroy(menuSphere);
        if (menuMaterial != null) Destroy(menuMaterial);
    }
}

