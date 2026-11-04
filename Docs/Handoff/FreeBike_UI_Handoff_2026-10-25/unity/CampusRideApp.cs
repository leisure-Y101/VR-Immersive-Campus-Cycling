using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[Serializable]
public class RideReport
{
    public string date;
    public string mode;
    public float durationSec;
    public float distanceKm;
    public float avgHeartRate;
    public float maxHeartRate;
    public float caloriesKcal;
    public bool simulatedHeartRate;
}

/// <summary>
/// Self-contained demo app for the PanoTest scene:
/// campus menu -> immersive / sport ride -> HR alerts, reminders, summary and report.
/// </summary>
public class CampusRideApp : MonoBehaviour
{
    enum RideMode { Immersive, Sport }
    enum UiState { Menu, Settings, Help, Reports, Riding, Paused, AdjustHud, Summary }

    const int HeartAlertThreshold = 130;
    const float DemoHydrationTime = 60f;
    const float HeartAlertShow = 5f;
    const float HeartAlertCooldown = 20f;
    const string ReportDirectory = @"D:\freebikeapk\CyclingReport";

    class Row
    {
        public GameObject go;
        public Image bg;
        public Text label;
        public Text value;
        public Action action;
        public bool isLanguage;
    }

    class RowSpec
    {
        public string labelKey;
        public string valueKey;
        public Action action;
        public bool isLanguage;
    }

    static readonly string[] LangCodes = { "zh", "en", "es", "fr", "pt" };
    static readonly string[] LangNames = { "中文", "English", "Español", "Français", "Português" };
    static readonly Dictionary<string, Dictionary<string, string>> Loc = BuildLocalization();

    InputRouter input;
    PanoBikeSpeed bike;
    VideoPlayer video;
    GameObject panoSphere;
    Camera cam;
    Font font;

    Canvas canvas;
    RectTransform panel;
    Text titleText, subtitleText, bodyText;
    RectTransform rowRoot;
    readonly List<Row> rows = new List<Row>();
    int selected;

    Canvas hudCanvas;
    Text hudHrValue, hudTimeValue, hudDistValue;
    Text hudHrLabel, hudTimeLabel, hudDistLabel;
    Canvas danmakuCanvas;
    Text danmakuText;
    Canvas alertCanvas;
    Text alertText;

    UiState state = UiState.Menu;
    RideMode mode = RideMode.Immersive;
    int languageIndex;
    float sessionTime;
    float distanceKm;
    float workJoules;
    float heartRate = 75f;
    float maxHeartRate = 75f;
    float heartSum;
    float heartSeconds;
    float cardiacDrift;
    float nextHydrationTime;
    float alertUntil;
    float nextHeartAlert;
    float danmakuStart;
    float danmakuDuration;
    bool danmakuActive;
    float hudX, hudY, hudZ;
    Material menuMaterial;
    Material rideMaterial;
    GameObject menuSphere;
    Texture2D menuTexture;

    void Start()
    {
        Application.runInBackground = true;
        input = GetComponent<InputRouter>();
        if (input == null) input = gameObject.AddComponent<InputRouter>();

        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            Debug.LogError("CampusRideApp: no camera");
            enabled = false;
            return;
        }

        bike = FindAnyObjectByType<PanoBikeSpeed>();
        if (bike != null)
        {
            video = bike.video;
            panoSphere = bike.gameObject;
        }
        if (video == null) video = FindAnyObjectByType<VideoPlayer>();
        if (panoSphere == null) panoSphere = GameObject.Find("PanoSphere");
        if (panoSphere != null && video == null) video = panoSphere.GetComponent<VideoPlayer>();
        if (panoSphere != null)
        {
            var panoRenderer = panoSphere.GetComponent<Renderer>();
            if (panoRenderer != null) rideMaterial = panoRenderer.sharedMaterial;
        }

        foreach (var look in FindObjectsByType<SimpleLook>(FindObjectsInactive.Exclude))
            look.enabled = false;

        var oldBike = GameObject.Find("Bikerig");
        if (oldBike != null) oldBike.SetActive(false);

        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 32);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        languageIndex = Mathf.Clamp(PlayerPrefs.GetInt("FreeBike.Language", 0), 0, LangCodes.Length - 1);
        hudX = PlayerPrefs.GetFloat("FreeBike.HudX", -0.42f);
        hudY = PlayerPrefs.GetFloat("FreeBike.HudY", 0.22f);
        hudZ = PlayerPrefs.GetFloat("FreeBike.HudZ", 1.4f);

        BuildUi();
        if (bike != null) bike.enabled = false;
        if (video != null)
        {
            video.playOnAwake = false;
            video.Pause();
        }
        ShowMenu();
    }

    void Update()
    {
        if (input == null) return;

        if (state == UiState.Riding)
        {
            UpdateRide(Time.deltaTime);
            if (input.Back) PauseRide();
            return;
        }

        if (state == UiState.AdjustHud)
        {
            UpdateHudAdjust(Time.deltaTime);
            return;
        }

        if (input.NavY != 0) MoveSelection(input.NavY);
        if (input.NavX != 0 && rows.Count > 0 && selected < rows.Count && rows[selected].isLanguage)
            CycleLanguage(input.NavX);
        if (input.Confirm) ActivateSelection();
        if (input.Back) GoBack();
    }

    // ------------------------------------------------------------------ UI

    void BuildUi()
    {
        var canvasGo = new GameObject("CampusRideUI", typeof(RectTransform), typeof(Canvas));
        canvasGo.transform.SetParent(cam.transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvas.sortingOrder = 100;
        var crt = canvasGo.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(1600, 900);
        crt.localScale = Vector3.one * 0.001f;
        crt.localPosition = new Vector3(0, 0, 1.65f);
        crt.localRotation = Quaternion.identity;

        panel = CreatePanel(canvas.transform, "MenuPanel", new Vector2(1000, 760), new Vector2(-250, 0),
            new Color(0.015f, 0.025f, 0.04f, 0.9f)).rectTransform;
        titleText = CreateText(panel, "Title", "", 48, new Vector2(0, 310), new Vector2(900, 60),
            TextAnchor.MiddleCenter, Color.white);
        subtitleText = CreateText(panel, "Subtitle", "", 22, new Vector2(0, 265), new Vector2(900, 36),
            TextAnchor.MiddleCenter, new Color(0.65f, 0.8f, 0.95f));
        bodyText = CreateText(panel, "Body", "", 24, new Vector2(0, -100), new Vector2(900, 320),
            TextAnchor.UpperCenter, new Color(0.82f, 0.88f, 0.94f));
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        rowRoot = new GameObject("Rows", typeof(RectTransform)).GetComponent<RectTransform>();
        rowRoot.SetParent(panel, false);
        rowRoot.anchorMin = rowRoot.anchorMax = new Vector2(0.5f, 0.5f);
        rowRoot.sizeDelta = new Vector2(920, 560);
        rowRoot.anchoredPosition = Vector2.zero;

        // HUD: head-relative, upper-left.
        var hudGo = new GameObject("RideHUD", typeof(RectTransform), typeof(Canvas));
        hudGo.transform.SetParent(cam.transform, false);
        hudCanvas = hudGo.GetComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.WorldSpace;
        hudCanvas.worldCamera = cam;
        hudCanvas.sortingOrder = 110;
        var hrt = hudGo.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(760, 260);
        hrt.localScale = Vector3.one * 0.001f;
        hrt.localPosition = new Vector3(hudX, hudY, hudZ);
        hrt.localRotation = Quaternion.identity;

        CreatePanel(hudGo.transform, "HudBg", new Vector2(740, 250), Vector2.zero,
            new Color(0.015f, 0.035f, 0.06f, 0.7f));
        hudHrLabel = CreateText(hudGo.transform, "HrLabel", L("hrSim"), 22, new Vector2(-250, 66), new Vector2(220, 40),
            TextAnchor.MiddleLeft, new Color(0.55f, 0.72f, 0.85f));
        hudHrValue = CreateText(hudGo.transform, "HrValue", "0 bpm", 38, new Vector2(120, 66), new Vector2(470, 56),
            TextAnchor.MiddleRight, Color.white);
        AddOutline(hudHrValue, new Color(0, 0, 0, 0.8f));

        hudTimeLabel = CreateText(hudGo.transform, "TimeLabel", L("time"), 22, new Vector2(-250, -14), new Vector2(220, 40),
            TextAnchor.MiddleLeft, new Color(0.55f, 0.72f, 0.85f));
        hudTimeValue = CreateText(hudGo.transform, "TimeValue", "00:00", 36, new Vector2(120, -14), new Vector2(470, 52),
            TextAnchor.MiddleRight, Color.white);
        AddOutline(hudTimeValue, new Color(0, 0, 0, 0.8f));

        hudDistLabel = CreateText(hudGo.transform, "DistLabel", L("dist"), 22, new Vector2(-250, -94), new Vector2(220, 40),
            TextAnchor.MiddleLeft, new Color(0.55f, 0.72f, 0.85f));
        hudDistValue = CreateText(hudGo.transform, "DistValue", "0.00 km", 36, new Vector2(120, -94), new Vector2(470, 52),
            TextAnchor.MiddleRight, Color.white);
        AddOutline(hudDistValue, new Color(0, 0, 0, 0.8f));

        hudCanvas.gameObject.SetActive(false);

        // Danmaku: head-relative, on a spherical shell in front.
        var danGo = new GameObject("Danmaku", typeof(RectTransform), typeof(Canvas));
        danGo.transform.SetParent(cam.transform, false);
        danmakuCanvas = danGo.GetComponent<Canvas>();
        danmakuCanvas.renderMode = RenderMode.WorldSpace;
        danmakuCanvas.worldCamera = cam;
        danmakuCanvas.sortingOrder = 120;
        var drt = danGo.GetComponent<RectTransform>();
        drt.sizeDelta = new Vector2(1200, 100);
        drt.localScale = Vector3.one * 0.001f;
        drt.localPosition = new Vector3(0, 0.18f, 2.2f);
        drt.localRotation = Quaternion.identity;
        danmakuText = CreateText(danGo.transform, "Text", "", 34, Vector2.zero, new Vector2(1000, 80),
            TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.65f));
        AddOutline(danmakuText, new Color(0, 0, 0, 0.95f));
        danmakuCanvas.gameObject.SetActive(false);

        // Center HR alert for immersive mode.
        var alertGo = new GameObject("HeartAlert", typeof(RectTransform), typeof(Canvas));
        alertGo.transform.SetParent(cam.transform, false);
        alertCanvas = alertGo.GetComponent<Canvas>();
        alertCanvas.renderMode = RenderMode.WorldSpace;
        alertCanvas.worldCamera = cam;
        alertCanvas.sortingOrder = 130;
        var art = alertGo.GetComponent<RectTransform>();
        art.sizeDelta = new Vector2(900, 120);
        art.localScale = Vector3.one * 0.001f;
        art.localPosition = new Vector3(0, 0.12f, 1.6f);
        art.localRotation = Quaternion.identity;
        CreatePanel(alertGo.transform, "AlertBg", new Vector2(900, 120), Vector2.zero,
            new Color(0.22f, 0.02f, 0.02f, 0.55f));
        alertText = CreateText(alertGo.transform, "Text", "", 30, Vector2.zero, new Vector2(820, 90),
            TextAnchor.MiddleCenter, new Color(1f, 0.32f, 0.32f));
        AddOutline(alertText, new Color(0, 0, 0, 0.95f));
        alertCanvas.gameObject.SetActive(false);
    }

    Image CreatePanel(Transform parent, string name, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text CreateText(Transform parent, string name, string text, int size, Vector2 pos, Vector2 dims,
        TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = dims;
        rt.anchoredPosition = pos;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.text = text;
        t.alignment = anchor;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void AddOutline(Text text, Color color)
    {
        var outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    // -------------------------------------------------------------- Menu

    void ShowMenu()
    {
        state = UiState.Menu;
        panel.gameObject.SetActive(true);
        bodyText.gameObject.SetActive(false);
        titleText.text = L("title");
        subtitleText.text = L("subtitle");
        BuildRows(new[]
        {
            Def(L("immersive"), () => StartRide(RideMode.Immersive), L("immersiveDesc")),
            Def(L("sport"), () => StartRide(RideMode.Sport), L("sportDesc")),
            Def(L("help"), ShowHelp),
            Def(L("settings"), ShowSettings),
            Def(L("reports"), ShowReports),
            Def(L("exit"), () => Application.Quit())
        });
        SetHud(false);
        SetAlert(false);
        SetDanmaku(false);
        SetupMenuBackground();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    void ShowSettings()
    {
        state = UiState.Settings;
        panel.gameObject.SetActive(true);
        bodyText.gameObject.SetActive(false);
        titleText.text = L("settings");
        subtitleText.text = L("subtitle");
        BuildRows(new[]
        {
            LanguageRow(),
            Def(L("resetHud"), ResetHudPosition),
            Def(L("back"), ShowMenu)
        });
    }

    void ShowHelp()
    {
        state = UiState.Help;
        panel.gameObject.SetActive(true);
        bodyText.gameObject.SetActive(true);
        bodyText.text = L("helpText");
        titleText.text = L("help");
        subtitleText.text = L("subtitle");
        BuildRows(new[] { Def(L("back"), ShowMenu) });
    }

    void ShowReports()
    {
        state = UiState.Reports;
        panel.gameObject.SetActive(true);
        bodyText.gameObject.SetActive(true);
        titleText.text = L("reports");
        subtitleText.text = L("subtitle");
        bodyText.text = BuildReportList();
        BuildRows(new[] { Def(L("back"), ShowMenu) });
    }

    RowSpec LanguageRow()
    {
        var row = Def(L("language"), null, LangNames[languageIndex]);
        row.isLanguage = true;
        return row;
    }

    RowSpec Def(string label, Action action, string value = null)
    {
        return new RowSpec { labelKey = label, valueKey = value, action = action, isLanguage = false };
    }

    void BuildRows(RowSpec[] defs)
    {
        foreach (var row in rows)
            if (row.go != null) Destroy(row.go);
        rows.Clear();
        selected = 0;

        for (int i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            var go = new GameObject("Row_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(rowRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(900, 56);
            rt.anchoredPosition = new Vector2(0, 178 - i * 72);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.11f, 0.16f, 0.85f);
            bg.raycastTarget = false;

            var label = CreateText(go.transform, "Label", def.labelKey, 27, new Vector2(-180, 0),
                new Vector2(560, 44), TextAnchor.MiddleLeft, Color.white);
            label.raycastTarget = false;

            Text value = null;
            if (def.valueKey != null)
            {
                value = CreateText(go.transform, "Value", def.valueKey, 25, new Vector2(250, 0),
                    new Vector2(300, 44), TextAnchor.MiddleRight, new Color(1f, 0.78f, 0.35f));
            }

            rows.Add(new Row
            {
                go = go,
                bg = bg,
                label = label,
                value = value,
                action = def.action,
                isLanguage = def.isLanguage
            });
        }
        RefreshRows();
    }

    void RefreshRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            bool sel = i == selected;
            row.bg.color = sel ? new Color(0.18f, 0.42f, 0.62f, 0.95f) : new Color(0.08f, 0.11f, 0.16f, 0.85f);
            row.label.color = sel ? Color.white : new Color(0.82f, 0.88f, 0.94f);
            if (row.value != null)
                row.value.color = sel ? Color.white : new Color(1f, 0.78f, 0.35f);
        }
    }

    void MoveSelection(int dir)
    {
        if (rows.Count == 0) return;
        selected = (selected + dir + rows.Count) % rows.Count;
        RefreshRows();
    }

    void ActivateSelection()
    {
        if (rows.Count == 0) return;
        var row = rows[selected];
        if (row.isLanguage) CycleLanguage(1);
        else row.action?.Invoke();
    }

    void CycleLanguage(int dir)
    {
        languageIndex = (languageIndex + dir + LangCodes.Length) % LangCodes.Length;
        PlayerPrefs.SetInt("FreeBike.Language", languageIndex);
        PlayerPrefs.Save();
        ApplyLanguage();
    }

    void ApplyLanguage()
    {
        titleText.text = L("title");
        subtitleText.text = L("subtitle");
        hudHrLabel.text = L("hrSim");
        hudTimeLabel.text = L("time");
        hudDistLabel.text = L("dist");

        if (state == UiState.Menu) ShowMenu();
        else if (state == UiState.Settings) ShowSettings();
        else if (state == UiState.Help) ShowHelp();
        else if (state == UiState.Reports) ShowReports();
        else if (state == UiState.Paused) ShowPause();
        else if (state == UiState.Summary) ShowSummary();
    }

    void GoBack()
    {
        if (state == UiState.Menu) return;
        if (state == UiState.Paused) ResumeRide();
        else if (state == UiState.Summary) Home();
        else ShowMenu();
    }

    // -------------------------------------------------------------- Ride

    void StartRide(RideMode newMode)
    {
        mode = newMode;
        sessionTime = 0f;
        distanceKm = 0f;
        workJoules = 0f;
        heartRate = 75f;
        maxHeartRate = 75f;
        heartSum = 0f;
        heartSeconds = 0f;
        cardiacDrift = 0f;
        nextHydrationTime = DemoHydrationTime;
        alertUntil = 0f;
        nextHeartAlert = 0f;
        danmakuActive = false;

        panel.gameObject.SetActive(false);
        SetAlert(false);
        SetDanmaku(false);
        SetHud(newMode == RideMode.Sport);
        if (panoSphere != null) panoSphere.SetActive(true);
        if (bike != null)
        {
            bike.enabled = true;
            bike.power = 0f;
        }
        if (video != null)
        {
            video.playOnAwake = false;
            video.Pause();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        state = UiState.Riding;
    }

    void UpdateRide(float dt)
    {
        float power = bike != null ? Mathf.Max(0f, bike.power) : 0f;
        bool active = power > 5f;

        float load = Mathf.Clamp01(power / 250f);
        if (active) cardiacDrift = Mathf.Min(8f, cardiacDrift + dt * 0.04f);
        else cardiacDrift = Mathf.Max(0f, cardiacDrift - dt * 0.02f);

        float target = 70f + 120f * Mathf.Pow(load, 0.85f) + cardiacDrift;
        float tau = target > heartRate ? 25f : 45f;
        heartRate = Mathf.Lerp(heartRate, target, 1f - Mathf.Exp(-dt / tau));
        heartRate = Mathf.Clamp(heartRate, 70f, 190f);

        sessionTime += dt;
        if (active)
        {
            float speedKmh = 8f + 22f * Mathf.Pow(load, 0.75f);
            distanceKm += speedKmh / 3600f * dt;
            workJoules += power * dt;
            heartSum += heartRate * dt;
            heartSeconds += dt;
        }

        maxHeartRate = Mathf.Max(maxHeartRate, heartRate);

        if (menuSphere != null && menuSphere.activeSelf && video != null && video.isPlaying)
            menuSphere.SetActive(false);

        UpdateHud();
        UpdateNotifications();

        if (Time.time > alertUntil) SetAlert(false);
        if (danmakuActive && Time.time - danmakuStart > danmakuDuration) SetDanmaku(false);
        UpdateDanmaku();
    }

    void UpdateHud()
    {
        if (hudHrValue == null) return;
        hudHrValue.text = $"{heartRate:F0} bpm";
        hudTimeValue.text = FormatTime(sessionTime);
        hudDistValue.text = $"{distanceKm:F2} km";

        bool alert = Time.time < alertUntil && heartRate >= HeartAlertThreshold;
        if (alert)
        {
            hudHrValue.color = Mathf.Sin(Time.time * 16f) > 0f ? new Color(1f, 0.2f, 0.2f) : Color.white;
        }
        else
        {
            hudHrValue.color = Color.white;
        }
    }

    void UpdateNotifications()
    {
        if (heartRate >= HeartAlertThreshold && Time.time >= nextHeartAlert)
        {
            string msg = string.Format(L("hrAlert"), Mathf.RoundToInt(heartRate));
            alertUntil = Time.time + HeartAlertShow;
            nextHeartAlert = Time.time + HeartAlertShow + HeartAlertCooldown;

            if (mode == RideMode.Sport)
            {
                ShowDanmaku(msg, HeartAlertShow);
                danmakuText.color = new Color(1f, 0.35f, 0.35f, 1f);
            }
            else
            {
                ShowCenterAlert(msg, HeartAlertShow);
            }
        }

        if (Time.time >= alertUntil && sessionTime >= nextHydrationTime)
        {
            ShowDanmaku(L("drink"), 5f);
            nextHydrationTime = Mathf.Ceil((sessionTime + 0.01f) / 600f) * 600f;
        }
    }

    void ShowDanmaku(string text, float duration)
    {
        danmakuText.text = text;
        danmakuStart = Time.time;
        danmakuDuration = duration;
        danmakuActive = true;
        danmakuCanvas.gameObject.SetActive(true);
        danmakuText.color = new Color(1f, 0.9f, 0.65f, 1f);
    }

    void UpdateDanmaku()
    {
        if (!danmakuActive) return;
        float t = Mathf.Clamp01((Time.time - danmakuStart) / danmakuDuration);
        var rt = (RectTransform)danmakuText.transform;
        rt.anchoredPosition = new Vector2(Mathf.Lerp(560f, -560f, t), 0f);
        float alpha = t < 0.12f ? t / 0.12f : (t > 0.88f ? (1f - t) / 0.12f : 1f);
        var c = danmakuText.color;
        danmakuText.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
        if (t >= 1f) SetDanmaku(false);
    }

    void ShowCenterAlert(string text, float duration)
    {
        alertText.text = text;
        alertCanvas.gameObject.SetActive(true);
        alertUntil = Time.time + duration;
    }

    void SetDanmaku(bool on)
    {
        danmakuActive = on;
        if (danmakuCanvas != null) danmakuCanvas.gameObject.SetActive(on);
    }

    void SetAlert(bool on)
    {
        if (alertCanvas != null) alertCanvas.gameObject.SetActive(on);
    }

    void SetHud(bool on)
    {
        if (hudCanvas != null) hudCanvas.gameObject.SetActive(on);
    }

    void PauseRide()
    {
        state = UiState.Paused;
        if (bike != null) bike.enabled = false;
        if (video != null) video.Pause();
        ShowPause();
    }

    void ShowPause()
    {
        panel.gameObject.SetActive(true);
        bodyText.gameObject.SetActive(false);
        titleText.text = L("paused");
        subtitleText.text = L("riding");
        BuildRows(new[]
        {
            Def(L("resume"), ResumeRide),
            Def(L("adjust"), BeginHudAdjust),
            Def(L("end"), EndRide)
        });
    }

    void ResumeRide()
    {
        panel.gameObject.SetActive(false);
        SetHud(mode == RideMode.Sport);
        if (bike != null) bike.enabled = true;
        state = UiState.Riding;
    }

    void BeginHudAdjust()
    {
        state = UiState.AdjustHud;
        panel.gameObject.SetActive(false);
        SetHud(true);
        alertText.text = L("adjustHint");
        alertCanvas.gameObject.SetActive(true);
    }

    void UpdateHudAdjust(float dt)
    {
        if (hudCanvas == null) return;
        float sx = Mathf.Abs(input.StickX) > 0.15f ? input.StickX : 0f;
        float sy = Mathf.Abs(input.StickY) > 0.15f ? input.StickY : 0f;
        var p = hudCanvas.transform.localPosition;
        if (input.Grip)
        {
            p.z = Mathf.Clamp(p.z + sy * 0.6f * dt, 0.7f, 2.4f);
        }
        else
        {
            p.x = Mathf.Clamp(p.x + sx * 0.35f * dt, -1.0f, 0.5f);
            p.y = Mathf.Clamp(p.y + sy * 0.35f * dt, -0.35f, 0.55f);
        }
        hudCanvas.transform.localPosition = p;

        if (input.Confirm || input.Back)
        {
            hudX = p.x;
            hudY = p.y;
            hudZ = p.z;
            PlayerPrefs.SetFloat("FreeBike.HudX", hudX);
            PlayerPrefs.SetFloat("FreeBike.HudY", hudY);
            PlayerPrefs.SetFloat("FreeBike.HudZ", hudZ);
            PlayerPrefs.Save();
            SetAlert(false);
            ShowPause();
            state = UiState.Paused;
        }
    }

    void EndRide()
    {
        if (bike != null) bike.enabled = false;
        if (video != null) video.Pause();
        state = UiState.Summary;
        ShowSummary();
    }

    void ShowSummary()
    {
        SetHud(false);
        SetAlert(false);
        SetDanmaku(false);
        panel.gameObject.SetActive(true);
        bodyText.gameObject.SetActive(true);
        titleText.text = L("summary");
        subtitleText.text = L("riding");
        bodyText.text =
            $"{L("duration")}  {FormatTime(sessionTime)}\n" +
            $"{L("maxHr")}  {maxHeartRate:F0} bpm\n" +
            $"{L("distance")}  {distanceKm:F2} km";

        if (mode == RideMode.Sport)
        {
            BuildRows(new[]
            {
                Def(L("saveReport"), SaveReport),
                Def(L("home"), Home)
            });
        }
        else
        {
            BuildRows(new[] { Def(L("home"), Home) });
        }
    }

    void SaveReport()
    {
        try
        {
            Directory.CreateDirectory(ReportDirectory);
            var report = new RideReport
            {
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                mode = mode.ToString(),
                durationSec = sessionTime,
                distanceKm = distanceKm,
                avgHeartRate = heartSeconds > 0f ? heartSum / heartSeconds : heartRate,
                maxHeartRate = maxHeartRate,
                caloriesKcal = workJoules / 4184f / 0.24f,
                simulatedHeartRate = true
            };
            string path = Path.Combine(ReportDirectory, "ride_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            bodyText.text = L("reportSaved") + "\n" + path;
        }
        catch (Exception e)
        {
            bodyText.text = "Report error: " + e.Message;
        }
        Invoke(nameof(Home), 1.6f);
    }

    void Home()
    {
        CancelInvoke();
        state = UiState.Menu;
        if (bike != null) bike.enabled = false;
        if (video != null) video.Pause();
        SetHud(false);
        SetAlert(false);
        SetDanmaku(false);
        SetupMenuBackground();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        ShowMenu();
    }

    void ResetHudPosition()
    {
        hudX = -0.42f;
        hudY = 0.22f;
        hudZ = 1.4f;
        hudCanvas.transform.localPosition = new Vector3(hudX, hudY, hudZ);
        PlayerPrefs.SetFloat("FreeBike.HudX", hudX);
        PlayerPrefs.SetFloat("FreeBike.HudY", hudY);
        PlayerPrefs.SetFloat("FreeBike.HudZ", hudZ);
        PlayerPrefs.Save();
    }

    string BuildReportList()
    {
        try
        {
            if (!Directory.Exists(ReportDirectory)) return L("noReports");
            var files = Directory.GetFiles(ReportDirectory, "*.json");
            if (files.Length == 0) return L("noReports");
            Array.Sort(files);
            Array.Reverse(files);
            var lines = new List<string>();
            for (int i = 0; i < files.Length && i < 5; i++)
                lines.Add(Path.GetFileNameWithoutExtension(files[i]));
            return string.Join("\n", lines);
        }
        catch
        {
            return L("noReports");
        }
    }

    string FormatTime(float seconds)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{s / 60:00}:{s % 60:00}";
    }

    void SetupMenuBackground()
    {
        if (panoSphere == null) return;
        if (menuTexture == null) menuTexture = Resources.Load<Texture2D>("homepage");
        if (menuTexture == null) return;

        if (menuSphere == null)
        {
            if (rideMaterial == null) return;
            menuSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            menuSphere.name = "MenuPanoSphere";
            var col = menuSphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            menuMaterial = new Material(rideMaterial);
            menuMaterial.SetTexture("_BaseMap", menuTexture);
            menuMaterial.SetTexture("_MainTex", menuTexture);
            menuMaterial.SetColor("_BaseColor", Color.white);
            menuSphere.GetComponent<Renderer>().sharedMaterial = menuMaterial;
            menuSphere.transform.position = panoSphere.transform.position;
            menuSphere.transform.rotation = panoSphere.transform.rotation;
            menuSphere.transform.localScale = panoSphere.transform.localScale * 0.98f;
        }

        menuSphere.SetActive(true);
        panoSphere.SetActive(false);
    }

    // ------------------------------------------------------------ Language

    void OnDestroy()
    {
        if (menuSphere != null) Destroy(menuSphere);
        if (menuMaterial != null) Destroy(menuMaterial);
    }

    string L(string key)
    {
        var dict = Loc[LangCodes[languageIndex]];
        if (dict.TryGetValue(key, out var value)) return value;
        var en = Loc["en"];
        return en.TryGetValue(key, out var fallback) ? fallback : key;
    }

    static Dictionary<string, Dictionary<string, string>> BuildLocalization()
    {
        var d = new Dictionary<string, Dictionary<string, string>>();
        d["zh"] = new Dictionary<string, string>
        {
            {"title","沉浸式校园骑行"},
            {"subtitle","PICO 4 Pro · 360° 校园骑行"},
            {"immersive","纯沉浸式体验"},
            {"immersiveDesc","只看风景，不显示数据"},
            {"sport","运动骑行"},
            {"sportDesc","记录心率、时长、距离"},
            {"help","操作说明"},
            {"settings","设置"},
            {"reports","数据报告"},
            {"exit","退出"},
            {"back","返回"},
            {"resume","继续骑行"},
            {"adjust","调整 HUD"},
            {"end","结束骑行"},
            {"saveReport","生成并保存报告"},
            {"home","返回主页"},
            {"language","语言"},
            {"resetHud","重置 HUD 位置"},
            {"summary","骑行总结"},
            {"duration","骑行时长"},
            {"maxHr","最高心率"},
            {"distance","骑行距离"},
            {"calories","消耗"},
            {"avgHr","平均心率"},
            {"reportSaved","报告已保存"},
            {"noReports","暂无运动报告"},
            {"helpText","手柄操作：\n摇杆上下选择，Trigger 确认，B/返回键返回。\n骑行时按 B 暂停；暂停菜单可以调整 HUD。\n调整 HUD：摇杆移动位置，按住 Grip + 摇杆前后调远近，Trigger 确认。"},
            {"paused","已暂停"},
            {"riding","骑行中"},
            {"hr","心率"},
            {"hrSim","心率（模拟）"},
            {"time","时长"},
            {"dist","距离"},
            {"hrAlert","心率 {0}，慢一点"},
            {"drink","喝口水吧，别等渴了"},
            {"adjustHint","摇杆移动，Grip + 摇杆调远近，Trigger 确认"},
            {"sim","模拟心率"}
        };
        d["en"] = new Dictionary<string, string>
        {
            {"title","Immersive Campus Cycling"},
            {"subtitle","PICO 4 Pro · 360° campus ride"},
            {"immersive","Pure immersion"},
            {"immersiveDesc","Scenery only, no data HUD"},
            {"sport","Sport ride"},
            {"sportDesc","Heart rate, time, distance"},
            {"help","How to use"},
            {"settings","Settings"},
            {"reports","Ride reports"},
            {"exit","Exit"},
            {"back","Back"},
            {"resume","Resume"},
            {"adjust","Adjust HUD"},
            {"end","End ride"},
            {"saveReport","Save report"},
            {"home","Home"},
            {"language","Language"},
            {"resetHud","Reset HUD position"},
            {"summary","Ride summary"},
            {"duration","Duration"},
            {"maxHr","Max heart rate"},
            {"distance","Distance"},
            {"calories","Calories"},
            {"avgHr","Average HR"},
            {"reportSaved","Report saved"},
            {"noReports","No ride reports yet"},
            {"helpText","Controller:\nJoystick up/down to select, Trigger to confirm, B/Back to return.\nPress B while riding to pause; adjust HUD from the pause menu.\nAdjust HUD: joystick moves, hold Grip + joystick for depth, Trigger confirms."},
            {"paused","Paused"},
            {"riding","Riding"},
            {"hr","Heart rate"},
            {"hrSim","Heart rate (sim)"},
            {"time","Time"},
            {"dist","Distance"},
            {"hrAlert","Heart rate {0} — ease off"},
            {"drink","Take a drink before you get thirsty"},
            {"adjustHint","Joystick to move, Grip + joystick for depth, Trigger to confirm"},
            {"sim","Simulated HR"}
        };
        d["es"] = new Dictionary<string, string>
        {
            {"title","Ciclismo inmersivo en el campus"},
            {"subtitle","PICO 4 Pro · recorrido 360°"},
            {"immersive","Inmersión pura"},
            {"immersiveDesc","Solo paisaje, sin datos"},
            {"sport","Ciclismo deportivo"},
            {"sportDesc","Ritmo cardíaco, tiempo, distancia"},
            {"help","Cómo usar"},
            {"settings","Ajustes"},
            {"reports","Informes"},
            {"exit","Salir"},
            {"back","Volver"},
            {"resume","Continuar"},
            {"adjust","Ajustar HUD"},
            {"end","Terminar"},
            {"saveReport","Guardar informe"},
            {"home","Inicio"},
            {"language","Idioma"},
            {"resetHud","Restablecer HUD"},
            {"summary","Resumen"},
            {"duration","Duración"},
            {"maxHr","FC máxima"},
            {"distance","Distancia"},
            {"calories","Calorías"},
            {"avgHr","FC media"},
            {"reportSaved","Informe guardado"},
            {"noReports","Sin informes"},
            {"helpText","Mando:\nJoystick arriba/abajo para elegir, Trigger para confirmar, B/Volver para atrás.\nPulsa B durante el recorrido para pausar; ajusta el HUD desde el menú.\nHUD: joystick mueve, Grip + joystick cambia la profundidad, Trigger confirma."},
            {"paused","Pausa"},
            {"riding","En ruta"},
            {"hr","Ritmo cardíaco"},
            {"hrSim","FC (simulada)"},
            {"time","Tiempo"},
            {"dist","Distancia"},
            {"hrAlert","Pulso {0} — baja el ritmo"},
            {"drink","Bebe un poco antes de tener sed"},
            {"adjustHint","Joystick para mover, Grip + joystick para profundidad, Trigger confirma"},
            {"sim","FC simulada"}
        };
        d["fr"] = new Dictionary<string, string>
        {
            {"title","Cyclisme immersif sur le campus"},
            {"subtitle","PICO 4 Pro · parcours 360°"},
            {"immersive","Immersion pure"},
            {"immersiveDesc","Paysage seul, sans données"},
            {"sport","Cyclisme sportif"},
            {"sportDesc","Fréquence cardiaque, durée, distance"},
            {"help","Utilisation"},
            {"settings","Réglages"},
            {"reports","Rapports"},
            {"exit","Quitter"},
            {"back","Retour"},
            {"resume","Reprendre"},
            {"adjust","Régler le HUD"},
            {"end","Terminer"},
            {"saveReport","Enregistrer le rapport"},
            {"home","Accueil"},
            {"language","Langue"},
            {"resetHud","Réinitialiser le HUD"},
            {"summary","Résumé"},
            {"duration","Durée"},
            {"maxHr","FC max"},
            {"distance","Distance"},
            {"calories","Calories"},
            {"avgHr","FC moyenne"},
            {"reportSaved","Rapport enregistré"},
            {"noReports","Aucun rapport"},
            {"helpText","Manette :\nJoystick haut/bas pour choisir, Trigger pour valider, B/Retour pour revenir.\nAppuyez sur B en roulant pour pause ; réglez le HUD dans le menu.\nHUD : joystick pour déplacer, Grip + joystick pour la profondeur, Trigger pour valider."},
            {"paused","En pause"},
            {"riding","En cours"},
            {"hr","Fréquence cardiaque"},
            {"hrSim","FC (simulée)"},
            {"time","Durée"},
            {"dist","Distance"},
            {"hrAlert","Pouls {0} — ralentissez"},
            {"drink","Buvez un peu avant d'avoir soif"},
            {"adjustHint","Joystick pour déplacer, Grip + joystick pour la profondeur, Trigger pour valider"},
            {"sim","FC simulée"}
        };
        d["pt"] = new Dictionary<string, string>
        {
            {"title","Ciclismo imersivo no campus"},
            {"subtitle","PICO 4 Pro · percurso 360°"},
            {"immersive","Imersão pura"},
            {"immersiveDesc","Apenas paisagem, sem dados"},
            {"sport","Ciclismo esportivo"},
            {"sportDesc","Frequência cardíaca, tempo, distância"},
            {"help","Como usar"},
            {"settings","Configurações"},
            {"reports","Relatórios"},
            {"exit","Sair"},
            {"back","Voltar"},
            {"resume","Continuar"},
            {"adjust","Ajustar HUD"},
            {"end","Encerrar"},
            {"saveReport","Salvar relatório"},
            {"home","Início"},
            {"language","Idioma"},
            {"resetHud","Redefinir HUD"},
            {"summary","Resumo"},
            {"duration","Duração"},
            {"maxHr","FC máxima"},
            {"distance","Distância"},
            {"calories","Calorias"},
            {"avgHr","FC média"},
            {"reportSaved","Relatório salvo"},
            {"noReports","Nenhum relatório"},
            {"helpText","Controle:\nJoystick cima/baixo para escolher, Trigger para confirmar, B/Voltar para retornar.\nPressione B durante o percurso para pausar; ajuste o HUD no menu.\nHUD: joystick move, Grip + joystick altera a profundidade, Trigger confirma."},
            {"paused","Pausado"},
            {"riding","Em percurso"},
            {"hr","Frequência cardíaca"},
            {"hrSim","FC (simulada)"},
            {"time","Tempo"},
            {"dist","Distância"},
            {"hrAlert","FC {0} — diminua o ritmo"},
            {"drink","Beba um pouco antes da sede"},
            {"adjustHint","Joystick para mover, Grip + joystick para profundidade, Trigger confirma"},
            {"sim","FC simulada"}
        };
        return d;
    }
}
