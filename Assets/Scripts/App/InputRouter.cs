using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// 统一输入入口：键盘（编辑器调试）+ PICO 双手柄（XR 原生接口）。
// 菜单使用摇杆四向选择，确认只使用食指扳机；Grip 用于面板调整。
[DefaultExecutionOrder(-100)]
public class InputRouter : MonoBehaviour
{
    public static InputRouter I;

    public float Pedal { get; private set; }   // 仅开发模拟源使用（W/S）
    public float Steer { get; private set; }   // 仅编辑器调试使用（A/D）

    public int NavY { get; private set; }
    public int NavX { get; private set; }
    public bool Confirm { get; private set; }
    public bool Back { get; private set; }
    // B 键当前是否按住（新版用来做“按住 B 暂停”）。
    public bool BackHeld { get; private set; }

    // 手柄控制模式（调试用）
    public float StickX { get; private set; }
    public float StickY { get; private set; }
    public float TriggerValue { get; private set; }
    public bool Grip { get; private set; }
    public bool GripLeft { get; private set; }
    public bool GripRight { get; private set; }
    public bool GripLeftDown { get; private set; }
    public bool GripRightDown { get; private set; }
    public bool GripLeftUp { get; private set; }
    public bool GripRightUp { get; private set; }
    public Vector3 LeftHandPosition { get; private set; }
    public Vector3 RightHandPosition { get; private set; }
    public Quaternion LeftHandRotation { get; private set; } = Quaternion.identity;
    public Quaternion RightHandRotation { get; private set; } = Quaternion.identity;

    class RepeatLane
    {
        float next;
        public int Step(bool held, float delay = 0.42f, float rate = 0.13f)
        {
            if (!held) { next = 0f; return 0; }
            if (next <= 0f || Time.unscaledTime >= next)
            {
                next = Time.unscaledTime + (next <= 0f ? delay : rate);
                return 1;
            }
            return 0;
        }
    }

    readonly RepeatLane rUp = new RepeatLane();
    readonly RepeatLane rDown = new RepeatLane();
    readonly RepeatLane rLeft = new RepeatLane();
    readonly RepeatLane rRight = new RepeatLane();
    bool prevTrigger, prevB, prevLeftGrip, prevRightGrip;

    void Awake() { I = this; }
    void OnDestroy() { if (I == this) I = null; }

    void Update()
    {
        var k = Keyboard.current;

        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        Vector2 rs = Vector2.zero, ls = Vector2.zero;
        if (right.isValid) right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out rs);
        if (left.isValid) left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out ls);
        Vector3 lp = Vector3.zero, rp = Vector3.zero;
        Quaternion lq = Quaternion.identity, rq = Quaternion.identity;
        if (left.isValid) left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out lp);
        if (right.isValid) right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out rp);
        if (left.isValid) left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out lq);
        if (right.isValid) right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out rq);
        LeftHandPosition = lp;
        RightHandPosition = rp;
        LeftHandRotation = lq;
        RightHandRotation = rq;
        Vector2 stick = rs.sqrMagnitude >= ls.sqrMagnitude ? rs : ls;
        if (stick.sqrMagnitude < 0.04f) stick = Vector2.zero;
        StickX = stick.x;
        StickY = stick.y;

        float rTriggerAxis = 0f, lTriggerAxis = 0f;
        if (right.isValid) right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out rTriggerAxis);
        if (left.isValid) left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out lTriggerAxis);
        TriggerValue = Mathf.Max(rTriggerAxis, lTriggerAxis);

        // ---- 开发模拟轴（键盘）----
        float kPedal = 0f, kSteer = 0f;
        if (k != null)
        {
            if (k.wKey.isPressed || k.upArrowKey.isPressed) kPedal += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) kPedal -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) kSteer += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) kSteer -= 1f;
        }
        Pedal = kPedal;
        Steer = kSteer;

        // ---- 离散菜单导航：键盘 + PICO 摇杆四向选择 ----
        bool up = false, down = false, leftHeld = false, rightHeld = false;
        if (k != null)
        {
            up |= k.wKey.isPressed || k.upArrowKey.isPressed;
            down |= k.sKey.isPressed || k.downArrowKey.isPressed;
            leftHeld |= k.aKey.isPressed || k.leftArrowKey.isPressed;
            rightHeld |= k.dKey.isPressed || k.rightArrowKey.isPressed;
        }
        const float stickDeadzone = 0.45f;
        up |= stick.y > stickDeadzone;
        down |= stick.y < -stickDeadzone;
        leftHeld |= stick.x < -stickDeadzone;
        rightHeld |= stick.x > stickDeadzone;
        int u = rUp.Step(up);
        int d = rDown.Step(down);
        int l = rLeft.Step(leftHeld);
        int r = rRight.Step(rightHeld);
        NavY = u - d;
        NavX = r - l;

        bool kbConfirm = k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
        bool kbBack = k != null && k.escapeKey.wasPressedThisFrame;

        bool rTrigButton = false, rB = false, rGripButton = false;
        float rGripAxis = 0f;
        if (right.isValid)
        {
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out rTrigButton);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out rB);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out rGripButton);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out rGripAxis);
        }
        bool lTrigButton = false, lB = false, lGripButton = false;
        float lGripAxis = 0f;
        if (left.isValid)
        {
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out lTrigButton);
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out lB);
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out lGripButton);
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out lGripAxis);
        }

        // PICO runtimes normally expose these as buttons, but some OpenXR
        // mappings expose only the analog value. Treat a firm squeeze/press as
        // held so Trigger remains the sole confirm input and Grip stays usable.
        bool rTrig = rTrigButton || rTriggerAxis >= 0.75f;
        bool lTrig = lTrigButton || lTriggerAxis >= 0.75f;
        bool rGrip = rGripButton || rGripAxis >= 0.75f;
        bool lGrip = lGripButton || lGripAxis >= 0.75f;

        Grip = rGrip || lGrip;
        GripLeft = lGrip;
        GripRight = rGrip;
        GripLeftDown = lGrip && !prevLeftGrip;
        GripRightDown = rGrip && !prevRightGrip;
        GripLeftUp = !lGrip && prevLeftGrip;
        GripRightUp = !rGrip && prevRightGrip;
        bool trig = rTrig || lTrig;
        bool b = rB || lB;

        // Edge-trigger the confirm event so holding the trigger cannot activate
        // a menu entry repeatedly.
        Confirm = kbConfirm || (trig && !prevTrigger);
        Back = kbBack || (b && !prevB);
        BackHeld = b || (k != null && k.escapeKey.isPressed);
        prevTrigger = trig; prevB = b;
        prevLeftGrip = lGrip;
        prevRightGrip = rGrip;
    }
}
