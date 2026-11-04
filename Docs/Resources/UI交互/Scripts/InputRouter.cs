using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// 统一输入入口：键盘（Input System）+ PICO 双手柄（XR 原生接口）
// 手柄现在只用于界面操作（选择/确认/暂停/返回），不参与骑行速度与转向。
// 左右手柄按钮都有效，摇杆优先使用位移更大的那只。
public class InputRouter : MonoBehaviour
{
    public static InputRouter I;

    public float Pedal { get; private set; }   // 仅开发模拟源使用（W/S）
    public float Steer { get; private set; }   // 仅编辑器调试使用（A/D）

    public int NavY { get; private set; }
    public int NavX { get; private set; }
    public bool Confirm { get; private set; }
    public bool Back { get; private set; }

    // 手柄控制模式（调试用）
    public float StickX { get; private set; }
    public float StickY { get; private set; }
    public float TriggerValue { get; private set; }

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
    bool prevTrigger, prevA, prevB;

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

        // ---- 离散菜单导航 ----
        bool up = stick.y > 0.55f, down = stick.y < -0.55f, leftHeld = stick.x < -0.55f, rightHeld = stick.x > 0.55f;
        if (k != null)
        {
            up |= k.wKey.isPressed || k.upArrowKey.isPressed;
            down |= k.sKey.isPressed || k.downArrowKey.isPressed;
            leftHeld |= k.aKey.isPressed || k.leftArrowKey.isPressed;
            rightHeld |= k.dKey.isPressed || k.rightArrowKey.isPressed;
        }
        int u = rUp.Step(up);
        int d = rDown.Step(down);
        int l = rLeft.Step(leftHeld);
        int r = rRight.Step(rightHeld);
        NavY = u - d;
        NavX = r - l;

        bool kbConfirm = k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
        bool kbBack = k != null && k.escapeKey.wasPressedThisFrame;

        bool rTrig = false, rA = false, rB = false;
        if (right.isValid)
        {
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out rTrig);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out rA);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out rB);
        }
        bool lTrig = false, lA = false, lB = false;
        if (left.isValid)
        {
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out lTrig);
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out lA);
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out lB);
        }

        bool trig = rTrig || lTrig;
        bool a = rA || lA;
        bool b = rB || lB;

        Confirm = kbConfirm || (trig && !prevTrigger) || (a && !prevA);
        Back = kbBack || (b && !prevB);
        prevTrigger = trig; prevA = a; prevB = b;
    }
}
