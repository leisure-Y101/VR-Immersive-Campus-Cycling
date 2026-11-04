using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum RideState { Boot, Menu, Settings, Quit, Countdown, Ride, Pause, Results, Notice }

// 应用主控：自动生成整套世界空间 UI 与屏幕流（启动页→主菜单→设置→倒计时→骑行→暂停→结算→退出）
// 场景零改动：进入 Play 时自动生成 RideUI 对象
[RequireComponent(typeof(InputRouter))]
public class RideUi : MonoBehaviour
{
    [Header("布局微调")]
    public float uiForward = 1.5f;     // 面板在正前方的距离(米)
    public float uiDownOffset = 0.03f; // 面板中心比眼高略低
    public float hudDown = 200f;       // HUD 比中心低多少(像素)

    [Header("调试开关")]
    [Tooltip("双手离开车把自动暂停。开发阶段先关闭，稳定后再打开。")]
    public bool handsPauseEnabled = false;

    Camera cam;
    BikeKeyboardRide ride;
    InputRouter input;
    RideSession session = new RideSession();
    RideSettings settings;

    RectTransform canvasRt;
    Image fadeImg;
    MenuPanel menu, settingsPanel, pausePanel, resultsPanel, quitPanel, noticePanel, controllerPrompt;
    GameObject bootPanel;
    Text countdownText;
    GameObject hudPanel;
    Text hudSpeed, hudTime, hudDist, hudPower;

    IBikeDataSource bike;
    BikeVisualRig bikeVisual;
    HandsOnHandlebarDetector handsDetector;
    HeadPoseDriver headPose;
    KinectSteeringReceiver kinect;
    ControllerSteeringSource controllerSteer;

    Vector3 spawnPos;
    Quaternion spawnRot;
    RideState state = RideState.Boot;
    bool busy = true;
    string lastMenuSubtitle;

    bool controllerMode;
    bool promptVisible;
    bool promptDismissed;
    float menuTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapSpawn()
    {
        if (FindAnyObjectByType<RideUi>() != null) return;
        if (FindAnyObjectByType<CampusRideApp>() != null) return;
        var go = new GameObject("RideUI");
        go.AddComponent<RideUi>();
    }

    void Start()
    {
        ride = FindAnyObjectByType<BikeKeyboardRide>();
        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (ride == null || cam == null)
        {
            Debug.LogWarning("RideUi: 找不到骑行脚本或相机，UI 未生成");
            enabled = false;
            return;
        }

        input = GetComponent<InputRouter>();
        settings = RideSettings.Load();

        // 清晰度：关闭纹理 mip 限制，强制各向异性过滤
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

        // 车架原点贴地；真实眼高由头显姿态提供（编辑器里用 1.6m 模拟）
        ride.groundOffset = 0f;
        settings.ApplyTo(ride);

        headPose = cam.GetComponent<HeadPoseDriver>();
        if (headPose == null) headPose = cam.gameObject.AddComponent<HeadPoseDriver>();
        headPose.eyeHeightOffset = settings.heightAdjust;

        // 数据源：Android 用 ThinkRider BLE；编辑器用键盘模拟
        bike = CreateBikeSource();
        ride.telemetry = bike;

        // 第一人称占位自行车（可后续 BindExternal 无缝替换）
        var visGo = new GameObject("BikeVisual");
        visGo.transform.SetParent(ride.transform, false);
        bikeVisual = visGo.AddComponent<BikeVisualRig>();
        bikeVisual.ride = ride;

        // 双手离开车把检测（手部追踪）；开发阶段默认关闭，避免误触发
        if (handsPauseEnabled)
        {
            handsDetector = ride.gameObject.AddComponent<HandsOnHandlebarDetector>();
            handsDetector.Setup(ride.transform, bikeVisual.GripLeft, bikeVisual.GripRight);
            handsDetector.HandsOff += OnHandsOff;
        }

        // Kinect 转向服务（PC 端 UDP 广播）
        kinect = FindAnyObjectByType<KinectSteeringReceiver>();

        // 手柄只负责转向：摇杆转向源常驻，Kinect 有信号时优先；前进动力只来自训练台功率
        controllerSteer = gameObject.AddComponent<ControllerSteeringSource>();

        spawnPos = ride.transform.position;
        spawnRot = ride.transform.rotation;
        ride.ridingEnabled = false;

        BuildCanvas();
        StartCoroutine(BootSeq());
    }

    IBikeDataSource CreateBikeSource()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            var go = new GameObject("ThinkRiderBLE");
            var src = go.AddComponent<ThinkRiderBleSource>();
            src.StartScan();
            return src;
        }

        var sim = new GameObject("SimulatedBike");
        return sim.AddComponent<SimulatedBikeSource>();
    }

    void OnHandsOff()
    {
        if (state != RideState.Ride || busy) return;
        PauseRide("双手已离开车把，训练已暂停");
    }

    string DeviceStatusText()
    {
        string kinectText = (kinect != null && kinect.Available) ? " · Kinect 转向已连接" : "";

        if (controllerMode)
            return "手柄控制模式（调试）· 扳机踩踏 · 摇杆转向 · B 暂停" + kinectText;
        if (bike == null) return "";
        if (bike.IsConnected)
        {
            var t = bike.Snapshot();
            return "ThinkRider A1 已连接 · " + Mathf.RoundToInt(t.powerWatts) + " W · " +
                   Mathf.RoundToInt(t.cadenceRpm) + " RPM";
        }
        switch (bike.State)
        {
            case BikeConnectionState.Scanning:
                return ThinkRiderBleSource.LocationPermissionGranted()
                    ? "正在扫描 ThinkRider A1…"
                    : "正在扫描…缺少定位权限，请允许定位并打开系统定位";
            case BikeConnectionState.Connecting: return "正在连接 ThinkRider A1…";
            case BikeConnectionState.Error:
            {
                var ble = bike as ThinkRiderBleSource;
                return "设备连接异常" + (ble != null && !string.IsNullOrEmpty(ble.LastError)
                    ? "：" + ble.LastError
                    : "，请确认训练台电源与蓝牙");
            }
            default: return "ThinkRider A1 未连接";
        }
    }

    void BuildCanvas()
    {
        var canvasGo = new GameObject("RideUI_Canvas", typeof(Canvas));
        canvasRt = (RectTransform)canvasGo.transform;
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvasRt.SetParent(ride.transform, false);
        canvasRt.sizeDelta = new Vector2(1200, 700);
        canvasRt.localScale = Vector3.one * 0.002f;
        // 头显姿态在首帧才生效，这里用测量值；测量异常时回退到 1.6m 基准
        float measuredEye = cam.transform.position.y - ride.transform.position.y;
        float eyeLocalY = measuredEye > 0.8f
            ? measuredEye
            : 1.6f + (headPose != null ? headPose.eyeHeightOffset : 0f);
        canvasRt.localPosition = new Vector3(0, eyeLocalY - uiDownOffset, uiForward);
        canvasRt.localRotation = Quaternion.identity;

        // 相机跟随的黑幕（淡入淡出用，不是头部锁定 UI 的替代品，只是转场遮罩）
        var fadeGo = new GameObject("RideUI_Fade", typeof(Canvas));
        var frt = (RectTransform)fadeGo.transform;
        var fc = fadeGo.GetComponent<Canvas>();
        fc.renderMode = RenderMode.WorldSpace;
        fc.worldCamera = cam;
        frt.SetParent(cam.transform, false);
        frt.sizeDelta = new Vector2(1000, 1000);
        frt.localScale = Vector3.one * 0.001f;
        frt.localPosition = new Vector3(0, 0, 0.35f);
        fadeImg = UiTheme.CreatePanel(frt, "Black", new Vector2(4000, 4000), Vector2.zero, Color.black);

        // 启动页
        bootPanel = UiTheme.CreatePanel(canvasRt, "Boot", new Vector2(620, 260), new Vector2(0, 20), UiTheme.PanelBg).gameObject;
        var t1 = UiTheme.CreateText(bootPanel.transform, "T", "FreeBike 骑行", 72, UiTheme.TextMain, TextAnchor.MiddleCenter);
        ((RectTransform)t1.transform).sizeDelta = new Vector2(600, 100);
        ((RectTransform)t1.transform).anchoredPosition = new Vector2(0, 30);
        var t2 = UiTheme.CreateText(bootPanel.transform, "S", "低模城市 · 沉浸式骑行健身", 26, UiTheme.TextDim, TextAnchor.MiddleCenter);
        ((RectTransform)t2.transform).sizeDelta = new Vector2(600, 40);
        ((RectTransform)t2.transform).anchoredPosition = new Vector2(0, -40);
        var t3 = UiTheme.CreateText(bootPanel.transform, "V", "版本 0.1", 20, UiTheme.TextDim, TextAnchor.MiddleCenter);
        ((RectTransform)t3.transform).sizeDelta = new Vector2(600, 30);
        ((RectTransform)t3.transform).anchoredPosition = new Vector2(0, -100);

        // 倒计时大字
        countdownText = UiTheme.CreateText(canvasRt, "Countdown", "", 120, UiTheme.Accent, TextAnchor.MiddleCenter);
        var cr = (RectTransform)countdownText.transform;
        cr.sizeDelta = new Vector2(600, 200);
        cr.anchoredPosition = new Vector2(0, 40);
        countdownText.gameObject.SetActive(false);

        BuildMenus();
        BuildHud();
    }

    void BuildMenus()
    {
        menu = new MenuPanel(canvasRt, "FreeBike 骑行", "摇杆/W S 选择 · A/回车确认", new Vector2(460, 364), Vector2.zero);
        menu.AddAction("开始骑行", StartRidePressed);
        menu.AddAction("训练设置", () => ShowScreen(RideState.Settings));
        menu.AddAction("退出应用", () => ShowScreen(RideState.Quit));

        settingsPanel = new MenuPanel(canvasRt, "训练设置", "←→ 调值 · 回车切换 · Esc 返回", new Vector2(460, 508), Vector2.zero);
        settingsPanel.AddValue("最高速度", () => $"{settings.speedKmh:F0} km/h", d =>
        {
            settings.speedKmh = Mathf.Clamp(settings.speedKmh + d * 2f, 10f, 60f);
            OnSettingChanged();
        });
        settingsPanel.AddValue("转向灵敏度", () => $"{settings.steerSens * 100f:F0}%", d =>
        {
            settings.steerSens = Mathf.Clamp(settings.steerSens + d * 0.05f, 0.5f, 1.5f);
            OnSettingChanged();
        });
        settingsPanel.AddToggle("空气墙", () => settings.airWall ? "开" : "关", () =>
        {
            settings.airWall = !settings.airWall;
            OnSettingChanged();
        });
        settingsPanel.AddValue("视角高度", () => $"{settings.heightAdjust:+0.00;-0.00;0.00} m", d =>
        {
            settings.heightAdjust = Mathf.Clamp(settings.heightAdjust + d * 0.05f, -0.5f, 0.5f);
            OnSettingChanged();
        });
        settingsPanel.AddAction("返回", () => ShowScreen(RideState.Menu));

        noticePanel = new MenuPanel(canvasRt, "设备未连接", "", new Vector2(460, 440), Vector2.zero, true);
        noticePanel.AddAction("重新扫描", () =>
        {
            bike?.StartScan();
            noticePanel.SetBody(DeviceStatusText() + "\n\n正在重新扫描…");
        });
        noticePanel.AddAction("返回", () => ShowScreen(RideState.Menu));

        // 无传感器时的小提示（在主菜单右侧）
        controllerPrompt = new MenuPanel(canvasRt, "调试模式", "",
            new Vector2(320, 260), new Vector2(400, 0), false, 260f, 48f);
        controllerPrompt.SetSubtitle("未检测到训练台传感器，进入手柄控制模式？");
        controllerPrompt.AddAction("进入手柄模式", EnterControllerMode);
        controllerPrompt.AddAction("忽略", () =>
        {
            promptDismissed = true;
            HideControllerPrompt();
        });

        pausePanel = new MenuPanel(canvasRt, "已暂停", "回车确认 · Esc 继续", new Vector2(460, 364), Vector2.zero);
        pausePanel.AddAction("继续骑行", Resume);
        pausePanel.AddAction("结束本次", EndRideToResults);
        pausePanel.AddAction("返回主菜单", EndRideToMenu);

        resultsPanel = new MenuPanel(canvasRt, "训练完成", "", new Vector2(460, 442), Vector2.zero, true);
        resultsPanel.AddAction("再来一次", StartRidePressed);
        resultsPanel.AddAction("返回主菜单", EndRideToMenu);

        quitPanel = new MenuPanel(canvasRt, "确认退出？", "回车确认 · Esc 取消", new Vector2(420, 292), Vector2.zero);
        quitPanel.AddAction("确认退出", QuitApp);
        quitPanel.AddAction("取消", () => ShowScreen(RideState.Menu));
    }

    void BuildHud()
    {
        var img = UiTheme.CreatePanel(canvasRt, "HUD", new Vector2(480, 170),
            new Vector2(0, -hudDown), new Color(0.07f, 0.07f, 0.09f, 0.72f));
        hudPanel = img.gameObject;
        ((RectTransform)img.transform).localRotation = Quaternion.Euler(22f, 0f, 0f);

        hudSpeed = UiTheme.CreateText(img.transform, "Speed", "0.0", 76, UiTheme.TextMain, TextAnchor.MiddleCenter);
        ((RectTransform)hudSpeed.transform).sizeDelta = new Vector2(240, 90);
        ((RectTransform)hudSpeed.transform).anchoredPosition = new Vector2(-100, 18);

        var unit = UiTheme.CreateText(img.transform, "Unit", "km/h", 24, UiTheme.TextDim, TextAnchor.MiddleCenter);
        ((RectTransform)unit.transform).sizeDelta = new Vector2(240, 30);
        ((RectTransform)unit.transform).anchoredPosition = new Vector2(-100, -46);

        hudTime = UiTheme.CreateText(img.transform, "Time", "时间 00:00", 28, UiTheme.TextMain, TextAnchor.MiddleCenter);
        ((RectTransform)hudTime.transform).sizeDelta = new Vector2(200, 36);
        ((RectTransform)hudTime.transform).anchoredPosition = new Vector2(95, 32);

        hudDist = UiTheme.CreateText(img.transform, "Dist", "距离 0 m", 28, UiTheme.TextMain, TextAnchor.MiddleCenter);
        ((RectTransform)hudDist.transform).sizeDelta = new Vector2(200, 36);
        ((RectTransform)hudDist.transform).anchoredPosition = new Vector2(95, -32);

        hudPower = UiTheme.CreateText(img.transform, "Power", "功率 0 W · 踏频 0 RPM", 22, UiTheme.TextDim, TextAnchor.MiddleCenter);
        ((RectTransform)hudPower.transform).sizeDelta = new Vector2(440, 30);
        ((RectTransform)hudPower.transform).anchoredPosition = new Vector2(0, -64);

        hudPanel.SetActive(false);
    }

    // ---------------- 屏幕流 ----------------

    IEnumerator BootSeq()
    {
        SetFade(1f);
        countdownText.gameObject.SetActive(false);
        yield return FadeTo(0f, 0.6f);
        yield return new WaitForSeconds(1.6f);
        yield return FadeTo(1f, 0.35f);
        bootPanel.SetActive(false);
        menu.Show();
        state = RideState.Menu;
        yield return FadeTo(0f, 0.35f);
        busy = false;
    }

    void HideAllScreens()
    {
        menu.Hide();
        settingsPanel.Hide();
        pausePanel.Hide();
        resultsPanel.Hide();
        quitPanel.Hide();
        noticePanel.Hide();
        controllerPrompt.Hide();
        promptVisible = false;
        if (bootPanel) bootPanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        hudPanel.SetActive(false);
    }

    void ShowScreen(RideState s)
    {
        if (busy) return;
        StartCoroutine(ShowScreenCo(s));
    }

    IEnumerator ShowScreenCo(RideState s)
    {
        busy = true;
        yield return FadeTo(1f, 0.18f);
        HideAllScreens();
        state = s;
        switch (s)
        {
            case RideState.Menu: menu.Show(); break;
            case RideState.Settings: settingsPanel.Show(); break;
            case RideState.Quit: quitPanel.Show(); break;
            case RideState.Pause: pausePanel.Show(); break;
            case RideState.Results: resultsPanel.Show(); break;
            case RideState.Notice: noticePanel.Show(); break;
        }
        yield return FadeTo(0f, 0.18f);
        busy = false;
    }

    void StartRidePressed()
    {
        if (busy) return;

        // 前进必须由脚蹬（训练台功率）驱动：没连上就不让开始，只提示重扫
        if (bike == null || !bike.IsConnected)
        {
            var bleSrc = bike as ThinkRiderBleSource;
            string diag = bleSrc != null ? "\n\n诊断：" + bleSrc.Diagnostics : "";
            noticePanel.SetBody(DeviceStatusText() + "\n\n请踩两圈唤醒训练台，或点“重新扫描”。" + diag);
            ShowScreen(RideState.Notice);
            return;
        }

        StartCoroutine(StartRideCo());
    }

    void ShowControllerPrompt()
    {
        promptVisible = true;
        promptDismissed = false;
        controllerPrompt.Show();
    }

    void HideControllerPrompt()
    {
        promptVisible = false;
        controllerPrompt.Hide();
    }

    void EnterControllerMode()
    {
        HideControllerPrompt();
        controllerMode = true;
        promptDismissed = true;

        // 断开真实设备（如果存在），切换到调试数据源
        if (bike is ThinkRiderBleSource ble) ble.Disconnect();

        var go = new GameObject("ControllerBikeSource");
        var src = go.AddComponent<ControllerBikeSource>();
        bike = src;
        ride.telemetry = src;

        // 右摇杆转向（Kinect 有信号时优先用 Kinect）；Awake 已创建，避免重复添加
        if (controllerSteer == null) controllerSteer = gameObject.AddComponent<ControllerSteeringSource>();

        lastMenuSubtitle = null;
        StartRidePressed();
    }

    IEnumerator StartRideCo()
    {
        busy = true;
        yield return FadeTo(1f, 0.3f);
        HideAllScreens();
        ride.ridingEnabled = false;
        ride.transform.SetPositionAndRotation(spawnPos, spawnRot);
        state = RideState.Countdown;
        countdownText.gameObject.SetActive(true);
        yield return FadeTo(0f, 0.3f);

        string[] seq = { "3", "2", "1", "GO!" };
        foreach (var s in seq)
        {
            countdownText.text = s;
            yield return PopText(countdownText, 0.55f);
        }

        countdownText.gameObject.SetActive(false);
        session.Start();
        handsDetector?.ResetGrace();
        ride.ridingEnabled = true;
        hudPanel.SetActive(true);
        state = RideState.Ride;
        busy = false;
    }

    void PauseRide(string reason = null)
    {
        if (state != RideState.Ride || busy) return;
        ride.ridingEnabled = false;
        hudPanel.SetActive(false);
        pausePanel.SetSubtitle(reason);
        StartCoroutine(ShowScreenCo(RideState.Pause));
    }

    void Resume()
    {
        if (busy) return;
        StartCoroutine(ResumeCo());
    }

    IEnumerator ResumeCo()
    {
        busy = true;
        yield return FadeTo(1f, 0.18f);
        HideAllScreens();
        handsDetector?.ResetGrace();
        ride.ridingEnabled = true;
        hudPanel.SetActive(true);
        state = RideState.Ride;
        yield return FadeTo(0f, 0.18f);
        busy = false;
    }

    void EndRideToResults()
    {
        if (busy) return;
        session.Stop();
        ride.ridingEnabled = false;
        hudPanel.SetActive(false);
        resultsPanel.SetBody(
            $"时长    {FmtTime(session.Elapsed)}\n" +
            $"距离    {FmtDist(session.Distance)}\n" +
            $"均速    {session.AvgSpeed * 3.6f:F1} km/h\n" +
            $"最高速  {session.MaxSpeed * 3.6f:F1} km/h");
        StartCoroutine(ShowScreenCo(RideState.Results));
    }

    void EndRideToMenu()
    {
        if (busy) return;
        session.Stop();
        ride.ridingEnabled = false;
        hudPanel.SetActive(false);
        StartCoroutine(BackToMenuCo());
    }

    IEnumerator BackToMenuCo()
    {
        busy = true;
        yield return FadeTo(1f, 0.25f);
        HideAllScreens();
        ride.transform.SetPositionAndRotation(spawnPos, spawnRot);
        menu.Show();
        state = RideState.Menu;
        yield return FadeTo(0f, 0.25f);
        busy = false;
    }

    void OnSettingChanged()
    {
        settings.ApplyTo(ride);
        if (headPose != null) headPose.eyeHeightOffset = settings.heightAdjust;
        settings.Save();
    }

    void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------- 每帧 ----------------

    void Update()
    {
        // 转向来源优先级：Kinect > 手柄调试 > 无
        if (kinect != null && kinect.Available)
            SteeringHub.Source = kinect;
        else
            SteeringHub.Source = controllerSteer;   // 没有 Kinect 时用右摇杆转向

        if (state == RideState.Ride && !busy)
        {
            // 训练中设备断开：立即暂停，避免数据失真
            if (bike != null && !bike.IsConnected && ride.Speed > 0.6f)
                PauseRide("设备连接已断开，训练已暂停");

            session.Tick(Time.deltaTime, ride.Speed);
            string s = (ride.Speed * 3.6f).ToString("F1");
            if (s != hudSpeed.text) hudSpeed.text = s;
            string tm = "时间 " + FmtTime(session.Elapsed);
            if (tm != hudTime.text) hudTime.text = tm;
            string ds = "距离 " + FmtDist(session.Distance);
            if (ds != hudDist.text) hudDist.text = ds;
            if (hudPower != null && bike != null)
            {
                var t = bike.Snapshot();
                string pw = "功率 " + Mathf.RoundToInt(t.powerWatts) + " W · 踏频 " +
                            Mathf.RoundToInt(t.cadenceRpm) + " RPM";
                if (pw != hudPower.text) hudPower.text = pw;
            }
        }

        if (state == RideState.Menu && !busy)
        {
            string sub = DeviceStatusText();
            if (sub != lastMenuSubtitle)
            {
                lastMenuSubtitle = sub;
                menu.SetSubtitle(sub);
            }

            menuTime += Time.deltaTime;
            // 不再自动弹“手柄踩踏”调试提示：手柄只用于转向与界面，前进必须靠踩踏
        }
        else
        {
            menuTime = 0f;
        }

        if (busy || input == null) return;
        int ny = input.NavY, nx = input.NavX;

        switch (state)
        {
            case RideState.Menu:
                if (promptVisible)
                {
                    if (ny != 0) controllerPrompt.Move(ny);
                    if (input.Confirm) controllerPrompt.Confirm();
                    if (input.Back)
                    {
                        promptDismissed = true;
                        HideControllerPrompt();
                    }
                }
                else
                {
                    if (ny != 0) menu.Move(ny);
                    if (input.Confirm) menu.Confirm();
                }
                break;
            case RideState.Notice:
                if (ny != 0) noticePanel.Move(ny);
                if (input.Confirm) noticePanel.Confirm();
                if (input.Back) ShowScreen(RideState.Menu);
                break;
            case RideState.Settings:
                if (ny != 0) settingsPanel.Move(ny);
                if (nx != 0) settingsPanel.Adjust(nx);
                if (input.Confirm) settingsPanel.Confirm();
                if (input.Back) ShowScreen(RideState.Menu);
                break;
            case RideState.Quit:
                if (ny != 0) quitPanel.Move(ny);
                if (input.Confirm) quitPanel.Confirm();
                if (input.Back) ShowScreen(RideState.Menu);
                break;
            case RideState.Pause:
                if (ny != 0) pausePanel.Move(ny);
                if (input.Confirm) pausePanel.Confirm();
                if (input.Back) Resume();
                break;
            case RideState.Results:
                if (ny != 0) resultsPanel.Move(ny);
                if (input.Confirm) resultsPanel.Confirm();
                if (input.Back) EndRideToMenu();
                break;
            case RideState.Ride:
                if (input.Back) PauseRide();
                break;
        }
    }

    // ---------------- 转场与工具 ----------------

    void SetFade(float a) { fadeImg.color = new Color(0f, 0f, 0f, a); }

    IEnumerator FadeTo(float target, float dur)
    {
        float start = fadeImg.color.a;
        if (dur <= 0f) { SetFade(target); yield break; }
        float e = 0f;
        while (e < dur)
        {
            e += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(start, target, Mathf.Clamp01(e / dur)));
            yield return null;
        }
        SetFade(target);
    }

    IEnumerator PopText(Text t, float dur)
    {
        var rt = (RectTransform)t.transform;
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / dur);
            float s = 1f - Mathf.Pow(1f - k, 3f);
            rt.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, s);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    static string FmtTime(float sec)
    {
        int t = Mathf.FloorToInt(sec);
        return $"{t / 60:00}:{t % 60:00}";
    }

    static string FmtDist(float m)
    {
        return m < 1000f ? $"{m:F0} m" : $"{m / 1000f:F2} km";
    }
}
