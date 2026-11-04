using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR;

/// <summary>
/// Runtime-generated UI starter for the PICO 4 Pro campus-cycling app.
/// Requires TextMeshPro and a scene containing a camera/XR Origin.
/// Buttons are selected with the controller stick and activated with the index trigger.
/// </summary>
public class CyclingVRApp : MonoBehaviour
{
    public enum Page
    {
        Home, SportPreparation, Help, Settings, Reports, ReportDetail,
        ExitConfirmation, Pause, SportSummary, ImmersiveSummary
    }

    [Header("Scene references")]
    [SerializeField] private Camera vrCamera;
    [SerializeField] private Texture2D panoramaOverride;

    [Header("World-space panel")]
    [SerializeField] private float homeDistance = 2.2f;
    [SerializeField] private float menuDistance = 1.8f;
    [SerializeField] private Vector2 panelSize = new Vector2(1200, 650);

    [Header("Background overlay / visual debug")]
    [Range(0f, 1f)]
    [SerializeField] private float backgroundOverlayOpacity = 0.62f;
    [Range(0f, 1f)]
    [SerializeField] private float pauseOverlayOpacity = 0.80f;
    [SerializeField] private bool showOverlayDebugLabel = false;

    [Header("Cycling demo")]
    [SerializeField] private bool demoMode = true;
    [SerializeField] private float heartRateThreshold = 130f;
    [SerializeField] private float demoHeartRate = 136f;

    [Header("Power-based speed estimate")]
    [SerializeField] private bool generateDemoPower = true;
    [SerializeField] private float demoPowerWatts = 120f;
    [SerializeField] private float demoPowerVariationWatts = 35f;
    [SerializeField] private float demoPowerPeriodSeconds = 24f;
    [SerializeField] private float riderAndBikeMassKg = 85f;
    [SerializeField] private float rollingResistance = 0.006f;
    [SerializeField] private float dragAreaCdA = 0.45f;
    [SerializeField] private float airDensityKgPerM3 = 1.225f;
    [Range(0.5f, 1f)]
    [SerializeField] private float drivetrainEfficiency = 0.95f;
    [SerializeField] private float roadGradePercent;
    [SerializeField] private float maxEstimatedSpeedKmh = 60f;
    [SerializeField] private float powerInputTimeoutSeconds = 2f;

    [Header("Debug 360 video")]
    [SerializeField] private bool useDebugVideo = true;
    [SerializeField] private string debugVideoPath = @"D:\freebike\Media\Video\6k60fps.mp4";
    [SerializeField] private VideoClip debugVideoClip;
    [SerializeField] private bool loopDebugVideo = true;
    [SerializeField] private bool playDebugVideoOnStart = true;
    [Range(0f, 1f)]
    [SerializeField] private float debugVideoVolume;

    private const string SaveKey = "PicoCampusCycling.Reports";
    private Canvas canvas;
    private RectTransform panel;
    private Graphic panelImage;
    private Canvas overlayCanvas;
    private Image overlayImage;
    private GameObject overlayRoot;
    private TMP_Text pageTitle;
    private Transform contentRoot;
    private GameObject panelRoot;
    private GameObject hudRoot;
    private TMP_Text heartRateText;
    private TMP_Text durationText;
    private TMP_Text speedText;
    private TMP_Text distanceText;
    private TMP_Text powerText;
    private TMP_Text countdownText;
    private TMP_Text warningText;
    private TMP_Text heartAlertText;
    private GameObject heartAlertBadge;
    private GameObject heartWarningPanel;
    private TMP_Text hydrationText;
    private GameObject bottomHudObject;
    private GameObject heartPanelObject;
    private VideoPlayer debugVideoPlayer;
    private RenderTexture debugVideoTexture;
    private Material panoramaMaterial;
    private Page currentPage = Page.Home;
    private Page hudReturnPage = Page.SportPreparation;
    private string currentLanguage = "中文";
    private bool immersiveMode;
    private bool riding;
    private bool paused;
    private bool countdownActive;
    private bool hudAdjustmentActive;
    private bool reportSaved;
    private float rideElapsed;
    private float distanceKm;
    private float smoothedSpeed;
    private float highestHeartRate;
    private float heartRateSum;
    private int heartRateSamples;
    private float nextHeartWarning;
    private float nextHydrationReminder = 60f;
    private float warningUntil;
    private float hydrationUntil;
    private float lastHeartRate;
    private float currentPowerWatts;
    private float externalPowerWatts;
    private float lastExternalPowerTime = float.NegativeInfinity;
    private float peakPowerWatts;
    private float powerSum;
    private int powerSamples;
    private float maxEstimatedSpeed;
    private Vector3 hudSavedPosition;
    private Quaternion hudSavedRotation;
    private Vector3 hudSavedScale;
    private readonly List<RideReport> reports = new List<RideReport>();

    private sealed class NavItem
    {
        public Button button;
        public RectTransform rect;
        public Graphic image;
        public Outline outline;
        public TMP_Text label;
        public CyclingButtonHover focusEffect;
        public bool selectedStyle;
        public bool disabled;
    }

    private readonly List<NavItem> navItems = new List<NavItem>();
    private int navIndex;
    private int heldDirection;
    private float nextNavRepeat;
    private bool previousTrigger;
    private bool previousBack;
    private InputDevice rightController;
    private InputDevice leftController;

    [Serializable]
    public class RideReport
    {
        public string id;
        public string date;
        public float durationSeconds;
        public float maxHeartRate;
        public float averageHeartRate;
        public float distanceKm;
        public float calories;
        public float averageSpeed;
        public float maxSpeed;
        public float averagePowerWatts;
        public float maxPowerWatts;
        public string heartRateSource = "模拟";
    }

    private void Awake()
    {
        if (vrCamera == null) vrCamera = Camera.main;
        if (vrCamera == null)
        {
            Camera[] activeCameras = Camera.allCameras;
            if (activeCameras.Length == 1)
            {
                vrCamera = activeCameras[0];
                Debug.LogWarning("CyclingVRApp: 未找到 MainCamera 标签，已使用场景中唯一启用的相机。请在 Inspector 指定 VR Camera。", this);
            }
        }
        if (vrCamera == null)
            Debug.LogError("CyclingVRApp: 没有可用的 VR Camera，主菜单无法放置。请把 XR Camera 指定到 Inspector 的 Vr Camera。", this);
        LoadReports();
        ApplyPanorama();
        SetupDebugVideo();
        CreateWorldPanel();
        CreateHud();
        ShowPage(Page.Home);
    }

    private void Update()
    {
        if (countdownActive || (riding && !paused))
        {
            // Demo heart rate: replace this block with the actual heart-rate provider when available.
            lastHeartRate = demoMode ? demoHeartRate + Mathf.Sin(Time.time * 0.25f) * 3f : demoHeartRate;
            if (riding && !paused)
            {
                rideElapsed += Time.deltaTime;
                currentPowerWatts = ReadPowerWatts();
                float targetSpeed = EstimateSpeedKmh(currentPowerWatts);
                smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, 1f - Mathf.Exp(-Time.deltaTime * 2.5f));
                maxEstimatedSpeed = Mathf.Max(maxEstimatedSpeed, smoothedSpeed);
                distanceKm += smoothedSpeed * Time.deltaTime / 3600f;
                peakPowerWatts = Mathf.Max(peakPowerWatts, currentPowerWatts);
                powerSum += currentPowerWatts;
                powerSamples++;
                highestHeartRate = Mathf.Max(highestHeartRate, lastHeartRate);
                heartRateSum += lastHeartRate;
                heartRateSamples++;
                UpdateHudValues();
                HandleHeartWarning();
                HandleHydrationReminder();
            }
        }

        if (warningText != null)
        {
            bool warningVisible = Time.time < warningUntil;
            warningText.gameObject.SetActive(warningVisible && !immersiveMode);
            if (heartAlertBadge != null) heartAlertBadge.SetActive(warningVisible && immersiveMode);
            if (heartWarningPanel != null) heartWarningPanel.SetActive(warningVisible && immersiveMode);
        }
        if (hydrationText != null)
        {
            hydrationText.gameObject.SetActive(Time.time < hydrationUntil);
            if (Time.time >= hydrationUntil) hydrationText.gameObject.SetActive(false);
        }

        UpdateControllerNavigation();

        bool hudAdjusting = hudRoot != null && hudRoot.TryGetComponent<CyclingHudGrabber>(out CyclingHudGrabber activeGrabber) && activeGrabber.IsActive;
        if (!hudAdjusting && WasBackPressedThisFrame())
        {
            if (currentPage == Page.Pause) ResumeRide();
            else if (currentPage == Page.SportPreparation || currentPage == Page.Settings ||
                     currentPage == Page.Help || currentPage == Page.Reports ||
                     currentPage == Page.ReportDetail || currentPage == Page.ExitConfirmation)
                ShowPage(Page.Home);
            else if (riding && !paused && !countdownActive)
                PauseRide();
        }
    }

    private void ApplyPanorama()
    {
        Texture2D texture = panoramaOverride != null ? panoramaOverride : Resources.Load<Texture2D>("homepage");
        Shader shader = Shader.Find("Skybox/Panoramic");
        if (shader == null) return;
        Material sky = new Material(shader);
        if (texture != null) sky.SetTexture("_MainTex", texture);
        sky.SetFloat("_Mapping", 1f);
        sky.SetFloat("_ImageType", 0f);
        panoramaMaterial = sky;
        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();
    }

    private void SetupDebugVideo()
    {
        if (!useDebugVideo || panoramaMaterial == null) return;

        VideoSource source;
        if (debugVideoClip != null)
        {
            source = VideoSource.VideoClip;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(debugVideoPath) || !File.Exists(debugVideoPath))
            {
                Debug.LogWarning($"CyclingVRApp: 调试视频不存在，继续使用全景图片背景：{debugVideoPath}");
                return;
            }
            source = VideoSource.Url;
        }

        debugVideoPlayer = gameObject.AddComponent<VideoPlayer>();
        debugVideoPlayer.playOnAwake = false;
        debugVideoPlayer.waitForFirstFrame = true;
        debugVideoPlayer.skipOnDrop = true;
        debugVideoPlayer.isLooping = loopDebugVideo;
        debugVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        debugVideoPlayer.SetDirectAudioMute(0, debugVideoVolume <= 0f);
        debugVideoPlayer.SetDirectAudioVolume(0, debugVideoVolume);
        debugVideoPlayer.source = source;
        if (source == VideoSource.VideoClip)
            debugVideoPlayer.clip = debugVideoClip;
        else
            debugVideoPlayer.url = new System.Uri(Path.GetFullPath(debugVideoPath)).AbsoluteUri;

        debugVideoPlayer.prepareCompleted += OnDebugVideoPrepared;
        debugVideoPlayer.errorReceived += OnDebugVideoError;
        debugVideoPlayer.Prepare();
    }

    private void OnDebugVideoPrepared(VideoPlayer player)
    {
        if (player.texture == null || panoramaMaterial == null) return;
        if (debugVideoTexture != null)
        {
            debugVideoTexture.Release();
            Destroy(debugVideoTexture);
        }

        int width = Mathf.Max(2, (int)player.width);
        int height = Mathf.Max(2, (int)player.height);
        debugVideoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "Cycling_Debug360Video_RT",
            useMipMap = false,
            autoGenerateMips = false
        };
        debugVideoTexture.Create();
        player.targetTexture = debugVideoTexture;
        panoramaMaterial.SetTexture("_MainTex", debugVideoTexture);
        DynamicGI.UpdateEnvironment();
        if (playDebugVideoOnStart) player.Play();
    }

    private void OnDebugVideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning($"CyclingVRApp: 调试视频无法播放，继续使用当前背景：{message}");
    }

    private void OnDestroy()
    {
        if (debugVideoPlayer != null)
        {
            debugVideoPlayer.prepareCompleted -= OnDebugVideoPrepared;
            debugVideoPlayer.errorReceived -= OnDebugVideoError;
        }
        if (debugVideoTexture != null)
        {
            debugVideoTexture.Release();
            Destroy(debugVideoTexture);
        }
        if (panoramaMaterial != null) Destroy(panoramaMaterial);
    }

    private void CreateWorldPanel()
    {
        if (vrCamera == null) return;
        CreateOverlay();
        panelRoot = new GameObject("Cycling_WorldSpace_MainPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas = panelRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = vrCamera;
        canvas.sortingOrder = 10;

        var scaler = panelRoot.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        scaler.referencePixelsPerUnit = 100;

        panel = panelRoot.GetComponent<RectTransform>();
        panel.sizeDelta = panelSize;
        panel.localScale = Vector3.one * 0.0015f;

        panelImage = panelRoot.AddComponent<Image>();
        panelImage.color = PanelBlue;

        var border = panelRoot.AddComponent<Outline>();
        border.effectColor = new Color(0.20f, 0.85f, 0.98f, 0.8f);
        border.effectDistance = new Vector2(2, -2);

        var header = CreateText(panelRoot.transform, "Header", "", 36, Color.white, TextAlignmentOptions.Center);
        var headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0.05f, 0.80f);
        headerRect.anchorMax = new Vector2(0.95f, 0.95f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;
        pageTitle = header;
        pageTitle.fontSize = 38;
        pageTitle.fontStyle = FontStyles.Bold;
        pageTitle.color = BrightCyan;
        CreatePanel(panelRoot.transform, "HeaderAccentLeft", new Vector2(-450, 246), new Vector2(230, 4), Cyan);
        CreatePanel(panelRoot.transform, "HeaderAccentRight", new Vector2(450, 246), new Vector2(230, 4), Cyan);

        var content = new GameObject("PageContent", typeof(RectTransform));
        content.transform.SetParent(panelRoot.transform, false);
        contentRoot = content.transform;
        var cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.04f, 0.04f);
        cr.anchorMax = new Vector2(0.96f, 0.80f);
        cr.offsetMin = Vector2.zero;
        cr.offsetMax = Vector2.zero;

        PlaceWorldPanel(homeDistance);
    }

    private void CreateOverlay()
    {
        if (vrCamera == null) return;
        overlayRoot = new GameObject("Cycling_Background_Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        overlayRoot.transform.SetParent(vrCamera.transform, false);
        overlayRoot.transform.localPosition = new Vector3(0f, 0f, 1.95f);
        overlayRoot.transform.localRotation = Quaternion.identity;
        overlayRoot.transform.localScale = Vector3.one * 0.001f;

        overlayCanvas = overlayRoot.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.WorldSpace;
        overlayCanvas.worldCamera = vrCamera;
        overlayCanvas.sortingOrder = 5;

        CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        scaler.referencePixelsPerUnit = 100;
        RectTransform rect = overlayRoot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920f, 1080f);

        overlayImage = overlayRoot.AddComponent<Image>();
        overlayImage.raycastTarget = false;
        overlayImage.color = Color.clear;
        overlayRoot.SetActive(false);
    }

    private void UpdateBackgroundOverlay(Page page)
    {
        if (overlayImage == null || overlayRoot == null) return;
        bool visible = page != Page.Home && !(riding && !paused && page != Page.Pause);
        overlayRoot.SetActive(visible);
        if (!visible) return;
        float opacity = page == Page.Pause ? pauseOverlayOpacity : backgroundOverlayOpacity;
        overlayImage.color = new Color(0.00f, 0.05f, 0.11f, Mathf.Clamp01(opacity));
    }

    private void PlaceWorldPanel(float distance)
    {
        if (vrCamera == null || panelRoot == null) return;
        Transform t = panelRoot.transform;
        t.position = vrCamera.transform.position + vrCamera.transform.forward * distance;
        t.rotation = Quaternion.LookRotation(vrCamera.transform.position - t.position, Vector3.up);
    }

    private void CreateHud()
    {
        if (vrCamera == null) return;
        hudRoot = new GameObject("Cycling_HUD_Group", typeof(RectTransform));
        hudRoot.transform.SetParent(vrCamera.transform, false);
        hudRoot.transform.localPosition = new Vector3(0, -0.18f, 1.25f);
        hudRoot.transform.localRotation = Quaternion.identity;
        hudRoot.transform.localScale = Vector3.one * 0.001f;
        hudSavedPosition = hudRoot.transform.localPosition;
        hudSavedRotation = hudRoot.transform.localRotation;
        hudSavedScale = hudRoot.transform.localScale;

        var hudCanvas = hudRoot.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.WorldSpace;
        hudCanvas.worldCamera = vrCamera;
        hudCanvas.sortingOrder = 30;

        RectTransform rootRect = hudRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1050, 260);

        var bottom = CreatePanel(hudRoot.transform, "BottomHUD", new Vector2(0, -65), new Vector2(980, 125),
            new Color(0.02f, 0.08f, 0.14f, 0.72f));
        bottomHudObject = bottom.gameObject;
        AddOutline(bottom.gameObject, BrightCyan);

        CreateText(bottom, "DurationLabel", "时长", 22, Cyan, TextAlignmentOptions.Center,
            new Vector2(-320, 18), new Vector2(190, 40));
        durationText = CreateText(bottom, "DurationValue", "00:00", 34, Color.white, TextAlignmentOptions.Center,
            new Vector2(-320, -22), new Vector2(190, 48));

        CreateText(bottom, "SpeedLabel", "速度", 22, Cyan, TextAlignmentOptions.Center,
            new Vector2(0, 30), new Vector2(220, 32));
        speedText = CreateText(bottom, "SpeedValue", "12.0 km/h", 48, Color.white, TextAlignmentOptions.Center,
            new Vector2(0, -12), new Vector2(300, 64));

        CreateText(bottom, "DistanceLabel", "距离", 22, Cyan, TextAlignmentOptions.Center,
            new Vector2(320, 18), new Vector2(190, 40));
        distanceText = CreateText(bottom, "DistanceValue", "0.00 km", 34, Color.white, TextAlignmentOptions.Center,
            new Vector2(120, -22), new Vector2(190, 48));
        CreateText(bottom, "PowerLabel", "功率", 22, Cyan, TextAlignmentOptions.Center,
            new Vector2(360, 18), new Vector2(190, 40));
        powerText = CreateText(bottom, "PowerValue", "0 W", 34, Color.white, TextAlignmentOptions.Center,
            new Vector2(360, -22), new Vector2(190, 48));
        // Shift speed left to make room for the measured power value.
        bottom.Find("SpeedLabel").GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, 30);
        bottom.Find("SpeedValue").GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, -12);
        bottom.Find("DistanceLabel").GetComponent<RectTransform>().anchoredPosition = new Vector2(120, 18);

        var heart = CreatePanel(hudRoot.transform, "HeartRatePanel", new Vector2(-360, 90), new Vector2(280, 90),
            new Color(0.02f, 0.08f, 0.14f, 0.70f));
        heartPanelObject = heart.gameObject;
        AddOutline(heart.gameObject, Cyan);
        CreateText(heart, "HeartTitle", "心率", 20, Cyan, TextAlignmentOptions.Center,
            new Vector2(0, 18), new Vector2(240, 28));
        heartRateText = CreateText(heart, "HeartValue", "136 bpm", 30, Color.white, TextAlignmentOptions.Center,
            new Vector2(0, -8), new Vector2(240, 36));
        CreateText(heart, "HeartSource", "模拟心率", 16, new Color(0.60f, 0.82f, 0.88f), TextAlignmentOptions.Center,
            new Vector2(0, -32), new Vector2(240, 24));

        countdownText = CreateText(hudRoot.transform, "Countdown", "", 110, Color.white, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(600, 220));
        countdownText.gameObject.SetActive(false);

        heartAlertBadge = CreatePanel(hudRoot.transform, "ImmersiveHeartAlert", new Vector2(-350, 170), new Vector2(300, 72),
            new Color(0.20f, 0.02f, 0.04f, 0.68f)).gameObject;
        AddOutline(heartAlertBadge, WarningRed);
        CreateText(heartAlertBadge.transform, "AlertIcon", "♡", 34, WarningRed, TextAlignmentOptions.Center,
            new Vector2(-105, 0), new Vector2(55, 55));
        heartAlertText = CreateText(heartAlertBadge.transform, "AlertText", "心率超限", 24, WarningRed, TextAlignmentOptions.Left,
            new Vector2(20, 0), new Vector2(180, 45));

        heartWarningPanel = CreatePanel(hudRoot.transform, "ImmersiveHeartWarning", new Vector2(310, 160), new Vector2(390, 112),
            new Color(0.20f, 0.02f, 0.04f, 0.58f)).gameObject;
        AddOutline(heartWarningPanel, WarningRed);
        CreateText(heartWarningPanel.transform, "WarningIcon", "!", 36, WarningRed, TextAlignmentOptions.Center,
            new Vector2(-140, 0), new Vector2(60, 72));
        CreateText(heartWarningPanel.transform, "WarningTitle", "心率过高", 26, new Color(1f, 0.67f, 0.70f), TextAlignmentOptions.Left,
            new Vector2(30, 20), new Vector2(230, 35));
        CreateText(heartWarningPanel.transform, "WarningBody", "请注意休息", 20, new Color(1f, 0.49f, 0.55f), TextAlignmentOptions.Left,
            new Vector2(30, -17), new Vector2(230, 30));

        warningText = CreateText(hudRoot.transform, "HeartWarning", "", 30, WarningRed, TextAlignmentOptions.Center,
            new Vector2(0, 145), new Vector2(800, 80));
        warningText.gameObject.SetActive(false);
        heartAlertBadge.SetActive(false);
        heartWarningPanel.SetActive(false);

        hydrationText = CreateText(hudRoot.transform, "HydrationReminder", "记得补水", 28, Cyan, TextAlignmentOptions.Center,
            new Vector2(420, 15), new Vector2(360, 60));
        hydrationText.gameObject.SetActive(false);
        UpdateHudVisible(false);
    }

    private void ShowPage(Page page)
    {
        currentPage = page;
        if (panelRoot == null) return;
        panelRoot.SetActive(true);
        ClearContent();
        navItems.Clear();
        navIndex = 0;
        heldDirection = 0;
        PlaceWorldPanel(page == Page.Home ? homeDistance : menuDistance);
        panelImage.color = page == Page.Home
            ? new Color(0f, 0f, 0f, 0f)
            : new Color(0.01f, 0.08f, 0.14f, page == Page.Pause ? 0.96f : 0.92f);
        UpdateBackgroundOverlay(page);
        if (showOverlayDebugLabel && page != Page.Home)
            CreateText(contentRoot, "OverlayDebug", $"遮罩强度 {(page == Page.Pause ? pauseOverlayOpacity : backgroundOverlayOpacity):0.00}", 15,
                new Color(0.45f, 0.75f, 0.84f), TextAlignmentOptions.Right,
                new Vector2(470, -275), new Vector2(260, 28));
        UpdateHudVisible(page == Page.Pause || page == Page.SportPreparation || page == Page.SportSummary ? false : riding && !paused && !immersiveMode);

        switch (page)
        {
            case Page.Home: BuildHome(); break;
            case Page.SportPreparation: BuildSportPreparation(); break;
            case Page.Help: BuildHelp(); break;
            case Page.Settings: BuildSettings(); break;
            case Page.Reports: BuildReports(); break;
            case Page.ReportDetail: BuildReportDetail(); break;
            case Page.ExitConfirmation: BuildExitConfirmation(); break;
            case Page.Pause: BuildPause(); break;
            case Page.SportSummary: BuildSummary(false); break;
            case Page.ImmersiveSummary: BuildSummary(true); break;
        }
        RefreshNavigationVisuals();
    }

    private void ClearContent()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }

    private void BuildHome()
    {
        pageTitle.text = "沉浸式校园骑行";
        CreateText(contentRoot, "Subtitle", "PICO 4 Pro · 360° 校园骑行", 22, Cyan, TextAlignmentOptions.Center,
            new Vector2(0, 170), new Vector2(900, 45));

        string[] labels = { "纯沉浸式体验", "运动骑行", "操作说明", "设置", "数据报告", "退出" };
        string[] subtitles = { "只看风景", "记录数据", "", "", "", "" };
        Action[] actions = {
            () => StartRide(true),
            () => ShowPage(Page.SportPreparation),
            () => ShowPage(Page.Help),
            () => ShowPage(Page.Settings),
            () => ShowPage(Page.Reports),
            () => ShowPage(Page.ExitConfirmation)
        };

        float[] xs = { -470, -282, -94, 94, 282, 470 };
        float[] ys = { -15, -2, 7, 7, -2, -15 };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var b = CreateButton(contentRoot, labels[i], actions[i], new Vector2(xs[i], ys[i]), new Vector2(172, 120), i == 0);
            if (!string.IsNullOrEmpty(subtitles[i]))
                CreateText(b.transform, "Subtitle", subtitles[i], 16, new Color(0.58f, 0.83f, 0.89f), TextAlignmentOptions.Center,
                    new Vector2(0, -30), new Vector2(150, 25));
        }
    }

    private void BuildSportPreparation()
    {
        pageTitle.text = "运动骑行";
        float previewPower = ReadPowerWatts();
        float previewSpeed = EstimateSpeedKmh(previewPower);
        CreateText(contentRoot, "Description", "调整 HUD 到舒服的位置，然后开始", 26, Cyan,
            TextAlignmentOptions.Center, new Vector2(0, 165), new Vector2(900, 50));
        CreateText(contentRoot, "PreviewHeart", "心率  136 bpm  ·  模拟心率", 22, Color.white,
            TextAlignmentOptions.Center, new Vector2(-300, 55), new Vector2(400, 45));
        CreateText(contentRoot, "PreviewHud", $"功率  {previewPower:0} W       估算速度  {previewSpeed:0.0} km/h", 28, Color.white,
            TextAlignmentOptions.Center, new Vector2(100, -5), new Vector2(800, 55));
        CreateButton(contentRoot, "调整 HUD", () => BeginHudAdjustment(Page.SportPreparation),
            new Vector2(-300, -125), new Vector2(230, 76));
        CreateButton(contentRoot, "开始", () => StartRide(false),
            new Vector2(0, -125), new Vector2(230, 76), true);
        CreateButton(contentRoot, "返回主页", () => ShowPage(Page.Home),
            new Vector2(300, -125), new Vector2(230, 76));
    }

    private void BuildPause()
    {
        pageTitle.text = "已暂停";
        var data = CreatePanel(contentRoot, "PauseRideData", new Vector2(0, 92), new Vector2(850, 108),
            new Color(0.02f, 0.10f, 0.16f, 0.92f));
        AddOutline(data.gameObject, Cyan);
        CreateText(data, "PauseDataTitle", "当前骑行数据", 20, Cyan, TextAlignmentOptions.Center,
            new Vector2(0, 30), new Vector2(760, 28));
        CreateText(data, "PauseDataValues",
            $"时长  {FormatDuration(rideElapsed)}    ·    功率  {currentPowerWatts:0} W    ·    速度  {smoothedSpeed:0.0} km/h    ·    距离  {distanceKm:0.00} km",
            23, Color.white, TextAlignmentOptions.Center, new Vector2(0, -13), new Vector2(790, 42));
        CreateButton(contentRoot, "继续骑行", ResumeRide, new Vector2(-300, -20), new Vector2(250, 90), true);
        CreateButton(contentRoot, "调整 HUD", () => BeginHudAdjustment(Page.Pause), new Vector2(0, -20), new Vector2(250, 90));
        CreateButton(contentRoot, "结束骑行", EndSportRide, new Vector2(300, -20), new Vector2(250, 90));
    }

    private void BuildSummary(bool immersive)
    {
        pageTitle.text = immersive ? "骑行已结束" : "骑行数据";
        bool hasRideData = rideElapsed > 0.1f || distanceKm > 0.001f;
        string duration = hasRideData ? FormatDuration(rideElapsed) : "32:17";
        int peakHeartRate = hasRideData ? Mathf.RoundToInt(highestHeartRate) : (immersive ? 186 : 168);
        string distance = hasRideData ? distanceKm.ToString("0.00") + " km" : "12.8 km";
        CreateText(contentRoot, "SummaryIcon", immersive ? "◇" : "▥", 44, BrightCyan, TextAlignmentOptions.Center,
            new Vector2(0, 185), new Vector2(100, 70));
        CreateStatCard("骑行时长", duration, new Vector2(-330, 35));
        CreateStatCard("最高心率", peakHeartRate + " bpm", new Vector2(0, 35));
        CreateStatCard("骑行距离", distance, new Vector2(330, 35));

        if (!immersive)
        {
            CreateButton(contentRoot, reportSaved ? "报告已保存" : "生成并保存报告",
                SaveCurrentReport, new Vector2(0, -100), new Vector2(340, 70), true, reportSaved);
            CreateButton(contentRoot, "返回主页", () => ShowPage(Page.Home),
                new Vector2(0, -190), new Vector2(280, 60));
        }
        else
        {
            CreateButton(contentRoot, "返回主页", () => ShowPage(Page.Home),
                new Vector2(0, -125), new Vector2(300, 70), true);
        }
    }

    private void BuildHelp()
    {
        pageTitle.text = "操作说明";
        var helpPanel = CreatePanel(contentRoot, "ControllerHelpPanel", new Vector2(0, 20), new Vector2(920, 360),
            new Color(0.01f, 0.08f, 0.14f, 0.78f));
        AddOutline(helpPanel.gameObject, Cyan);
        CreateText(helpPanel, "LeftControllerIcon", "◌", 70, Cyan, TextAlignmentOptions.Center,
            new Vector2(-250, 105), new Vector2(160, 90));
        CreateText(helpPanel, "RightControllerIcon", "◌", 70, Cyan, TextAlignmentOptions.Center,
            new Vector2(250, 105), new Vector2(160, 90));
        CreateText(contentRoot, "LeftHand", "左手柄\n\n摇杆：上下左右选择\nGrip：抓取 HUD\nB：返回 / 取消", 25, Color.white,
            TextAlignmentOptions.Center, new Vector2(-260, -15), new Vector2(400, 250));
        CreateText(contentRoot, "RightHand", "右手柄\n\n摇杆：上下左右选择\nTrigger：确认\nGrip：抓取 HUD", 25, Color.white,
            TextAlignmentOptions.Center, new Vector2(260, -15), new Vector2(400, 250));
        CreateButton(contentRoot, "返回主页", () => ShowPage(Page.Home), new Vector2(0, -190), new Vector2(280, 64));
    }

    private void BuildSettings()
    {
        pageTitle.text = T("设置", "Settings", "Ajustes", "Paramètres", "Configurações");
        CreateText(contentRoot, "LanguageLabel", T("语言", "Language", "Idioma", "Langue", "Idioma"),
            24, Cyan, TextAlignmentOptions.Left, new Vector2(250, 155), new Vector2(360, 40));
        string[] languages = { "中文", "English", "Español", "Français", "Português" };
        float y = 90;
        foreach (string language in languages)
        {
            string captured = language;
            CreateButton(contentRoot, language + (currentLanguage == language ? "  ✓" : ""),
                () => { currentLanguage = captured; ShowPage(Page.Settings); }, new Vector2(300, y), new Vector2(360, 48), currentLanguage == language);
            y -= 55;
        }
        CreateButton(contentRoot, T("重置 HUD 位置", "Reset HUD Position", "Restablecer HUD", "Réinitialiser le HUD", "Redefinir HUD"),
            ResetHud, new Vector2(-290, 80), new Vector2(360, 66));
        CreateButton(contentRoot, T("返回主页", "Back to Home", "Volver al inicio", "Retour à l'accueil", "Voltar ao início"),
            () => ShowPage(Page.Home), new Vector2(-290, -165), new Vector2(300, 58));
    }

    private void BuildReports()
    {
        pageTitle.text = T("数据报告", "Reports", "Informes", "Rapports", "Relatórios");
        if (reports.Count == 0)
        {
            CreateText(contentRoot, "Empty", T("暂无骑行报告", "No reports yet", "No hay informes", "Aucun rapport", "Sem relatórios"),
                28, new Color(0.68f, 0.83f, 0.88f), TextAlignmentOptions.Center, Vector2.zero, new Vector2(700, 80));
        }
        else
        {
            int visible = Mathf.Min(5, reports.Count);
            for (int i = 0; i < visible; i++)
            {
                RideReport report = reports[reports.Count - 1 - i];
                int index = reports.Count - 1 - i;
                string row = $"▥  {report.date}     {FormatDuration(report.durationSeconds)}     {report.distanceKm:0.0} km     {report.maxHeartRate:0} bpm     ›";
                CreateButton(contentRoot, row, () => { selectedReportIndex = index; ShowPage(Page.ReportDetail); },
                    new Vector2(0, 140 - i * 62), new Vector2(950, 52));
            }
        }
        CreateButton(contentRoot, T("返回主页", "Back to Home", "Volver al inicio", "Retour à l'accueil", "Voltar ao início"),
            () => ShowPage(Page.Home), new Vector2(0, -245), new Vector2(280, 52));
    }

    private int selectedReportIndex = -1;
    private void BuildReportDetail()
    {
        pageTitle.text = "报告详情";
        if (selectedReportIndex < 0 || selectedReportIndex >= reports.Count)
        {
            CreateText(contentRoot, "Missing", "未找到报告", 26, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(500, 60));
        }
        else
        {
            RideReport r = reports[selectedReportIndex];
            string details =
                $"日期：{r.date}\n" +
                $"骑行时长：{FormatDuration(r.durationSeconds)}\n" +
                $"最高心率：{r.maxHeartRate:0} bpm\n" +
                $"平均心率：{r.averageHeartRate:0} bpm\n" +
                $"骑行距离：{r.distanceKm:0.00} km\n" +
                $"卡路里：{r.calories:0} kcal（估算）\n" +
                $"平均速度：{r.averageSpeed:0.0} km/h\n" +
                $"最高速度：{r.maxSpeed:0.0} km/h\n" +
                $"平均功率：{r.averagePowerWatts:0} W\n" +
                $"最高功率：{r.maxPowerWatts:0} W\n" +
                $"心率来源：{r.heartRateSource}";
            CreateText(contentRoot, "ReportFields", details, 23, Color.white, TextAlignmentOptions.Left,
                new Vector2(0, 20), new Vector2(800, 350));
        }
        CreateButton(contentRoot, "返回报告列表", () => ShowPage(Page.Reports), new Vector2(-150, -210), new Vector2(280, 55));
        CreateButton(contentRoot, "返回主页", () => ShowPage(Page.Home), new Vector2(150, -210), new Vector2(240, 55));
    }

    private void BuildExitConfirmation()
    {
        pageTitle.text = "确定退出应用吗？";
        CreateText(contentRoot, "ExitDesc", "退出后将关闭 PICO 4 Pro 应用。", 24, Color.white,
            TextAlignmentOptions.Center, new Vector2(0, 55), new Vector2(800, 60));
        CreateButton(contentRoot, "取消", () => ShowPage(Page.Home), new Vector2(-160, -90), new Vector2(240, 70));
        CreateButton(contentRoot, "确认", QuitApplication, new Vector2(160, -90), new Vector2(240, 70), true);
    }

    private void CreateStatCard(string label, string value, Vector2 position)
    {
        var card = CreatePanel(contentRoot, label + "Card", position, new Vector2(270, 150),
            new Color(0.03f, 0.12f, 0.19f, 0.92f));
        AddOutline(card.gameObject, new Color(0.20f, 0.85f, 0.98f, 0.8f));
        CreateText(card, "Label", label, 22, Cyan, TextAlignmentOptions.Center,
            new Vector2(0, 32), new Vector2(240, 35));
        CreateText(card, "Value", value, 32, Color.white, TextAlignmentOptions.Center,
            new Vector2(0, -15), new Vector2(250, 55));
    }

    private void StartRide(bool immersive)
    {
        immersiveMode = immersive;
        riding = false;
        paused = false;
        countdownActive = false;
        rideElapsed = 0;
        distanceKm = 0;
        highestHeartRate = 0;
        heartRateSum = 0;
        heartRateSamples = 0;
        peakPowerWatts = 0f;
        powerSum = 0f;
        powerSamples = 0;
        maxEstimatedSpeed = 0f;
        currentPowerWatts = 0f;
        smoothedSpeed = 0;
        reportSaved = false;
        nextHeartWarning = 0;
        nextHydrationReminder = 60f;
        warningUntil = 0;
        hydrationUntil = 0;
        panelRoot.SetActive(false);
        if (overlayRoot != null) overlayRoot.SetActive(false);
        UpdateHudVisible(!immersive);
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        countdownActive = true;
        if (countdownText != null) countdownText.gameObject.SetActive(true);
        UpdateHudVisible(!immersiveMode);
        string[] steps = { "3", "2", "1", "GO" };
        foreach (string step in steps)
        {
            if (countdownText != null) countdownText.text = step;
            yield return new WaitForSeconds(1f);
        }
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        countdownActive = false;
        riding = true;
        paused = false;
        currentPowerWatts = ReadPowerWatts();
        smoothedSpeed = EstimateSpeedKmh(currentPowerWatts);
        UpdateHudVisible(!immersiveMode);
    }

    private void PauseRide()
    {
        if (!riding || immersiveMode) return;
        paused = true;
        riding = true;
        smoothedSpeed = 0;
        UpdateHudVisible(false);
        ShowPage(Page.Pause);
    }

    private void ResumeRide()
    {
        paused = false;
        riding = true;
        panelRoot.SetActive(false);
        if (overlayRoot != null) overlayRoot.SetActive(false);
        UpdateHudVisible(!immersiveMode);
    }

    private void EndSportRide()
    {
        riding = false;
        paused = false;
        countdownActive = false;
        UpdateHudVisible(false);
        ShowPage(Page.SportSummary);
    }

    public void EndImmersiveRide()
    {
        if (!immersiveMode || !riding) return;
        riding = false;
        paused = false;
        UpdateHudVisible(false);
        ShowPage(Page.ImmersiveSummary);
    }

    private void BeginHudAdjustment(Page source)
    {
        if (riding && !paused)
        {
            Debug.Log("CyclingVRApp: 骑行进行中暂不允许调整 HUD，请先暂停。");
            return;
        }
        hudReturnPage = source;
        hudAdjustmentActive = true;
        panelRoot.SetActive(false);
        if (overlayRoot != null) overlayRoot.SetActive(false);
        UpdateHudVisible(true);
        // CyclingHudGrabber handles Grip movement and invokes this component when released/cancelled.
        CyclingHudGrabber grabber = hudRoot.GetComponent<CyclingHudGrabber>();
        if (grabber == null) grabber = hudRoot.AddComponent<CyclingHudGrabber>();
        grabber.Begin(this, vrCamera, hudSavedPosition, hudSavedRotation, hudSavedScale);
    }

    public void OnHudAdjustmentFinished(bool saved, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        hudAdjustmentActive = false;
        if (saved)
        {
            hudSavedPosition = pos;
            hudSavedRotation = rot;
            hudSavedScale = scale;
        }
        else
        {
            hudRoot.transform.localPosition = hudSavedPosition;
            hudRoot.transform.localRotation = hudSavedRotation;
            hudRoot.transform.localScale = hudSavedScale;
        }
        ShowPage(hudReturnPage);
    }

    private void ResetHud()
    {
        hudSavedPosition = new Vector3(0, -0.18f, 1.25f);
        hudSavedRotation = Quaternion.identity;
        hudSavedScale = Vector3.one * 0.001f;
        if (hudRoot != null)
        {
            hudRoot.transform.localPosition = hudSavedPosition;
            hudRoot.transform.localRotation = hudSavedRotation;
            hudRoot.transform.localScale = hudSavedScale;
        }
        PlayerPrefs.SetFloat("HUD_X", hudSavedPosition.x);
        PlayerPrefs.SetFloat("HUD_Y", hudSavedPosition.y);
        PlayerPrefs.SetFloat("HUD_Z", hudSavedPosition.z);
    }

    private void UpdateHudVisible(bool visible)
    {
        if (hudRoot == null) return;
        bool ridingActive = riding && !paused && !countdownActive;
        bool showDataHud = visible && !immersiveMode && (ridingActive || hudAdjustmentActive);
        if (!hudAdjustmentActive && (currentPage == Page.SportPreparation || currentPage == Page.Pause ||
            currentPage == Page.SportSummary || currentPage == Page.ImmersiveSummary))
            showDataHud = false;

        // Keep the camera-space overlay alive during immersive riding so warning/countdown can render,
        // while hiding the regular heart-rate and bottom data panels.
        bool showOverlay = countdownActive || (riding && !paused) || showDataHud || hudAdjustmentActive;
        hudRoot.SetActive(showOverlay);
        if (bottomHudObject != null) bottomHudObject.SetActive(showDataHud);
        if (heartPanelObject != null) heartPanelObject.SetActive(showDataHud);
        if (countdownText != null) countdownText.gameObject.SetActive(countdownActive);
        if (warningText != null) warningText.gameObject.SetActive(Time.time < warningUntil);
        if (heartAlertBadge != null) heartAlertBadge.SetActive(immersiveMode && Time.time < warningUntil);
        if (heartWarningPanel != null) heartWarningPanel.SetActive(immersiveMode && Time.time < warningUntil);
        if (hydrationText != null) hydrationText.gameObject.SetActive(showDataHud && Time.time < hydrationUntil);
    }

    private void UpdateHudValues()
    {
        if (heartRateText != null) heartRateText.text = Mathf.RoundToInt(lastHeartRate) + " bpm";
        if (durationText != null) durationText.text = FormatDuration(rideElapsed);
        if (speedText != null) speedText.text = (paused ? 0f : smoothedSpeed).ToString("0.0") + " km/h";
        if (distanceText != null) distanceText.text = distanceKm.ToString("0.00") + " km";
        if (powerText != null) powerText.text = currentPowerWatts.ToString("0") + " W";
    }

    /// <summary>Feed the latest bike-trainer power reading in watts.</summary>
    public void SetPowerWatts(float watts)
    {
        externalPowerWatts = Mathf.Clamp(watts, 0f, 3000f);
        lastExternalPowerTime = Time.time;
    }

    private float ReadPowerWatts()
    {
        if (Time.time - lastExternalPowerTime <= Mathf.Max(0.1f, powerInputTimeoutSeconds))
            return externalPowerWatts;
        if (!float.IsNegativeInfinity(lastExternalPowerTime)) return 0f;
        if (!generateDemoPower) return 0f;

        float period = Mathf.Max(1f, demoPowerPeriodSeconds);
        float wave = Mathf.Sin(Time.time * (2f * Mathf.PI / period));
        return Mathf.Clamp(demoPowerWatts + wave * demoPowerVariationWatts, 0f, 2000f);
    }

    private float EstimateSpeedKmh(float powerWatts)
    {
        if (powerWatts <= 0f) return 0f;

        float mass = Mathf.Max(1f, riderAndBikeMassKg);
        float gravity = 9.80665f;
        float grade = roadGradePercent * 0.01f;
        float rollingAndGradeForce = mass * gravity * (Mathf.Max(0f, rollingResistance) + grade);
        float aero = 0.5f * Mathf.Max(0.1f, airDensityKgPerM3) * Mathf.Max(0.05f, dragAreaCdA);
        float wheelPower = powerWatts * Mathf.Clamp(drivetrainEfficiency, 0.5f, 1f);
        float low = 0f;
        float high = Mathf.Max(1f, maxEstimatedSpeedKmh / 3.6f);

        // Solve P = (rolling/grade force + aerodynamic drag) * speed by bisection.
        for (int i = 0; i < 24; i++)
        {
            float speed = (low + high) * 0.5f;
            float requiredPower = (rollingAndGradeForce + aero * speed * speed) * speed;
            if (requiredPower < wheelPower) low = speed;
            else high = speed;
        }

        return (low + high) * 0.5f * 3.6f;
    }

    private void HandleHeartWarning()
    {
        if (lastHeartRate > heartRateThreshold && Time.time >= nextHeartWarning)
        {
            warningUntil = Time.time + 5f;
            nextHeartWarning = Time.time + 20f;
            if (immersiveMode)
            {
                if (heartAlertText != null) heartAlertText.text = $"心率超限  {Mathf.RoundToInt(lastHeartRate)}";
                if (warningText != null) warningText.text = "心率过高\n请注意休息";
            }
            else
            {
                if (warningText != null) warningText.text = $"心率 {Mathf.RoundToInt(lastHeartRate)}，慢一点";
                if (heartRateText != null) heartRateText.color = new Color(1f, 0.35f, 0.3f);
            }
        }
        else if (lastHeartRate <= heartRateThreshold && heartRateText != null)
        {
            heartRateText.color = Color.white;
            nextHeartWarning = Mathf.Max(nextHeartWarning, Time.time);
        }
    }

    private void HandleHydrationReminder()
    {
        if (rideElapsed >= nextHydrationReminder)
        {
            hydrationUntil = Time.time + 5f;
            nextHydrationReminder = demoMode ? float.MaxValue : rideElapsed + 600f;
        }
    }

    private void SaveCurrentReport()
    {
        if (reportSaved) return;
        float avgHeart = heartRateSamples > 0 ? heartRateSum / heartRateSamples : lastHeartRate;
        float avgSpeed = rideElapsed > 0 ? distanceKm / (rideElapsed / 3600f) : 0f;
        RideReport report = new RideReport
        {
            id = Guid.NewGuid().ToString("N"),
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            durationSeconds = rideElapsed,
            maxHeartRate = highestHeartRate,
            averageHeartRate = avgHeart,
            distanceKm = distanceKm,
            calories = distanceKm * 35f, // illustrative estimate only
            averageSpeed = avgSpeed,
            maxSpeed = maxEstimatedSpeed,
            averagePowerWatts = powerSamples > 0 ? powerSum / powerSamples : 0f,
            maxPowerWatts = peakPowerWatts,
            heartRateSource = "模拟"
        };
        reports.Add(report);
        SaveReports();
        reportSaved = true;
        ShowPage(Page.SportSummary);
    }

    private void LoadReports()
    {
        reports.Clear();
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json))
        {
            AddDemoReports();
            return;
        }
        ReportListWrapper wrapper = JsonUtility.FromJson<ReportListWrapper>(json);
        if (wrapper != null && wrapper.items != null) reports.AddRange(wrapper.items);
        if (reports.Count == 0) AddDemoReports();
    }

    private void AddDemoReports()
    {
        reports.Add(new RideReport { id = "demo-0524", date = "2025-05-24", durationSeconds = 1937f, distanceKm = 12.8f, maxHeartRate = 168f, averageHeartRate = 142f, calories = 448f, averageSpeed = 23.8f, maxSpeed = 26.4f });
        reports.Add(new RideReport { id = "demo-0520", date = "2025-05-20", durationSeconds = 1725f, distanceKm = 10.3f, maxHeartRate = 162f, averageHeartRate = 138f, calories = 361f, averageSpeed = 21.5f, maxSpeed = 24.2f });
        reports.Add(new RideReport { id = "demo-0518", date = "2025-05-18", durationSeconds = 2472f, distanceKm = 15.6f, maxHeartRate = 174f, averageHeartRate = 146f, calories = 546f, averageSpeed = 22.7f, maxSpeed = 27.1f });
        reports.Add(new RideReport { id = "demo-0515", date = "2025-05-15", durationSeconds = 1597f, distanceKm = 9.8f, maxHeartRate = 158f, averageHeartRate = 134f, calories = 343f, averageSpeed = 22.1f, maxSpeed = 24.8f });
    }

    private void SaveReports()
    {
        ReportListWrapper wrapper = new ReportListWrapper { items = reports };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    [Serializable] private class ReportListWrapper { public List<RideReport> items = new List<RideReport>(); }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static string FormatDuration(float seconds)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
        if (s >= 3600) return $"{s / 3600:00}:{(s % 3600) / 60:00}:{s % 60:00}";
        return $"{s / 60:00}:{s % 60:00}";
    }

    private string T(string zh, string en, string es, string fr, string pt)
    {
        switch (currentLanguage)
        {
            case "English": return en;
            case "Español": return es;
            case "Français": return fr;
            case "Português": return pt;
            default: return zh;
        }
    }

    private static readonly Color Cyan = new Color(0.20f, 0.85f, 0.96f, 1f);
    private static readonly Color BrightCyan = new Color(0.42f, 0.95f, 1f, 1f);
    private static readonly Color PanelBlue = new Color(0.01f, 0.08f, 0.14f, 0.94f);
    private static readonly Color WarningRed = new Color(1f, 0.32f, 0.38f, 1f);

    private TMP_Text CreateText(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align, Vector2 anchoredPos = default, Vector2 sizeDelta = default)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.font = TMP_Settings.defaultFontAsset;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta == Vector2.zero ? new Vector2(300, 50) : sizeDelta;
        return tmp;
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private void AddOutline(GameObject target, Color color)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null) outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2, -2);
    }

    private Button CreateButton(Transform parent, string label, Action action, Vector2 pos, Vector2 size,
        bool selected = false, bool disabled = false)
    {
        GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = disabled ? new Color(0.06f, 0.16f, 0.22f, 0.8f) :
            selected ? new Color(0.04f, 0.30f, 0.40f, 0.95f) : new Color(0.03f, 0.12f, 0.20f, 0.92f);
        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = selected ? new Color(0.25f, 0.95f, 1f, 1f) : new Color(0.15f, 0.63f, 0.78f, 0.75f);
        outline.effectDistance = new Vector2(2, -2);

        string icon = GetButtonIcon(label);
        if (!string.IsNullOrEmpty(icon))
            CreateText(go.transform, "Icon", icon, Mathf.Min(30f, size.y * 0.28f), selected ? BrightCyan : Cyan,
                TextAlignmentOptions.Center, new Vector2(0, size.y * 0.18f), new Vector2(size.x - 18f, size.y * 0.32f));
        TMP_Text text = CreateText(go.transform, "Label", label, Mathf.Clamp(size.y * 0.19f, 18f, 24f), Color.white,
            TextAlignmentOptions.Center, new Vector2(0, string.IsNullOrEmpty(icon) ? 0f : -size.y * 0.22f),
            new Vector2(size.x - 16f, string.IsNullOrEmpty(icon) ? size.y - 12f : size.y * 0.35f));
        text.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.75f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.55f, 0.9f, 1f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.interactable = !disabled;
        button.onClick.AddListener(() => action?.Invoke());

        CyclingButtonHover focusEffect = go.AddComponent<CyclingButtonHover>();
        var nav = new NavItem
        {
            button = button,
            rect = rect,
            image = image,
            outline = outline,
            label = text,
            focusEffect = focusEffect,
            selectedStyle = selected,
            disabled = disabled
        };
        navItems.Add(nav);
        return button;
    }

    private static string GetButtonIcon(string label)
    {
        if (label.Contains("纯沉浸")) return "◇";
        if (label.Contains("运动骑行")) return "△";
        if (label.Contains("操作说明")) return "▤";
        if (label.Contains("设置") || label.Contains("重置 HUD")) return "◎";
        if (label.Contains("数据报告") || label.Contains("报告")) return "▥";
        if (label.Contains("退出") || label.Contains("确认")) return "↗";
        if (label.Contains("开始") || label.Contains("继续")) return "▶";
        if (label.Contains("结束")) return "■";
        if (label.Contains("返回")) return "↶";
        if (label.Contains("调整 HUD")) return "◈";
        if (label.Contains("保存") || label.Contains("生成")) return "▣";
        if (label.Contains("取消")) return "×";
        return string.Empty;
    }

    private void UpdateControllerNavigation()
    {
        if (panelRoot == null || !panelRoot.activeSelf || navItems.Count == 0) return;

        RefreshControllers();
        Vector2 axis = ReadLargestStick();
        int direction = 0;
        if (Mathf.Abs(axis.x) >= Mathf.Abs(axis.y) && Mathf.Abs(axis.x) > 0.55f)
            direction = axis.x > 0f ? 1 : -1;
        else if (Mathf.Abs(axis.y) > 0.55f)
            direction = axis.y > 0f ? 2 : -2;

        if (direction == 0)
        {
            heldDirection = 0;
            nextNavRepeat = 0f;
        }
        else if (direction != heldDirection || Time.unscaledTime >= nextNavRepeat)
        {
            MoveFocus(direction);
            bool firstPress = direction != heldDirection;
            heldDirection = direction;
            nextNavRepeat = Time.unscaledTime + (firstPress ? 0.38f : 0.20f);
        }

        bool trigger = ReadButton(CommonUsages.triggerButton);
        bool keyboardConfirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        if ((trigger && !previousTrigger) || keyboardConfirm)
        {
            if (navIndex >= 0 && navIndex < navItems.Count && navItems[navIndex].button.interactable)
                navItems[navIndex].button.onClick.Invoke();
        }
        previousTrigger = trigger;
    }

    private void MoveFocus(int direction)
    {
        if (navItems.Count < 2) return;
        Vector2 current = navItems[navIndex].rect.anchoredPosition;
        float bestScore = float.MaxValue;
        int best = navIndex;
        for (int i = 0; i < navItems.Count; i++)
        {
            if (i == navIndex || navItems[i].disabled) continue;
            Vector2 delta = navItems[i].rect.anchoredPosition - current;
            bool inDirection = direction == 1 ? delta.x > 8f :
                direction == -1 ? delta.x < -8f :
                direction == 2 ? delta.y > 8f : delta.y < -8f;
            if (!inDirection) continue;
            float primary = direction == 1 || direction == -1 ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
            float lateral = direction == 1 || direction == -1 ? Mathf.Abs(delta.y) : Mathf.Abs(delta.x);
            float score = primary + lateral * 2.2f;
            if (score < bestScore) { bestScore = score; best = i; }
        }
        if (best != navIndex)
        {
            navIndex = best;
            RefreshNavigationVisuals();
        }
    }

    private void RefreshNavigationVisuals()
    {
        for (int i = 0; i < navItems.Count; i++)
        {
            NavItem item = navItems[i];
            bool focused = i == navIndex && !item.disabled;
            item.image.color = item.disabled
                ? new Color(0.06f, 0.16f, 0.22f, 0.80f)
                : focused || item.selectedStyle
                    ? new Color(0.04f, 0.30f, 0.40f, focused ? 0.98f : 0.82f)
                    : new Color(0.03f, 0.12f, 0.20f, 0.92f);
            item.outline.effectColor = focused
                ? new Color(0.25f, 0.95f, 1f, 1f)
                : new Color(0.15f, 0.63f, 0.78f, 0.75f);
            item.label.fontStyle = focused ? FontStyles.Bold : FontStyles.Normal;
            if (item.focusEffect != null) item.focusEffect.SetFocused(focused);
        }
    }

    private void RefreshControllers()
    {
        if (!rightController.isValid) rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!leftController.isValid) leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    }

    private Vector2 ReadLargestStick()
    {
        Vector2 right = Vector2.zero, left = Vector2.zero;
        if (rightController.isValid) rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out right);
        if (leftController.isValid) leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out left);
        Vector2 result = right.sqrMagnitude >= left.sqrMagnitude ? right : left;
        if (result.sqrMagnitude < 0.04f) result = Vector2.zero;
        if (Input.GetKey(KeyCode.LeftArrow)) result.x = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) result.x = 1f;
        if (Input.GetKey(KeyCode.UpArrow)) result.y = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) result.y = -1f;
        return result;
    }

    private bool ReadButton(InputFeatureUsage<bool> usage)
    {
        bool right = rightController.isValid && rightController.TryGetFeatureValue(usage, out bool r) && r;
        bool left = leftController.isValid && leftController.TryGetFeatureValue(usage, out bool l) && l;
        return right || left;
    }

    private bool WasBackPressedThisFrame()
    {
        bool back = ReadButton(CommonUsages.secondaryButton) || Input.GetKeyDown(KeyCode.Escape);
        bool edge = back && !previousBack;
        previousBack = back;
        return edge;
    }

}
