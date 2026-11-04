using System;
using UnityEngine;
using Unity.XR.PXR;

// 双手离开车把检测（PICO 手部追踪）
// 判定：左右手“掌心”关节与车把左右握把锚点的距离都小于 maxGripDistance 视为握把中；
// 连续 offDelay 秒不满足 → 触发 HandsOff（由 RideUi 暂停训练）。
// 若手部追踪不可用（未开启/设备不支持），自动停用检测，避免误暂停。
public class HandsOnHandlebarDetector : MonoBehaviour
{
    [Header("判定参数")]
    public float maxGripDistance = 0.32f;
    public float offDelay = 1.5f;
    public float resumeGrace = 2.5f;
    public bool detectionEnabled = true;

    public event Action HandsOff;

    Transform gripL;
    Transform gripR;
    Transform trackingRoot;

    float offTimer;
    float graceTimer;
    float noTrackTimer;
    bool fired;

    HandJointLocations leftJoints = new HandJointLocations();
    HandJointLocations rightJoints = new HandJointLocations();

    public void Setup(Transform root, Transform leftGrip, Transform rightGrip)
    {
        trackingRoot = root;
        gripL = leftGrip;
        gripR = rightGrip;
    }

    // 继续训练时给一点缓冲时间，避免立刻再次触发暂停
    public void ResetGrace()
    {
        graceTimer = resumeGrace;
        offTimer = 0f;
        fired = false;
    }

    void Update()
    {
        if (!detectionEnabled || gripL == null || gripR == null) return;

        if (graceTimer > 0f)
        {
            graceTimer -= Time.deltaTime;
            return;
        }

        bool leftOk = TryGetHandPoint(HandType.HandLeft, ref leftJoints, out Vector3 leftPos);
        bool rightOk = TryGetHandPoint(HandType.HandRight, ref rightJoints, out Vector3 rightPos);

        // 两只手都追不到：可能是没开手部追踪或手在视野外，不做判定
        if (!leftOk && !rightOk)
        {
            noTrackTimer += Time.deltaTime;
            if (noTrackTimer > 3f) detectionEnabled = false;
            return;
        }
        noTrackTimer = 0f;

        bool leftOn = leftOk && Near(leftPos, gripL);
        bool rightOn = rightOk && Near(rightPos, gripR);

        if (leftOn && rightOn)
        {
            offTimer = 0f;
            fired = false;
            return;
        }

        offTimer += Time.deltaTime;
        if (offTimer >= offDelay && !fired)
        {
            fired = true;
            Debug.Log("HandsOnHandlebarDetector: 双手离开车把");
            HandsOff?.Invoke();
        }
    }

    bool Near(Vector3 handTrackingPos, Transform grip)
    {
        Transform root = trackingRoot != null ? trackingRoot : transform;
        Vector3 gripLocal = root.InverseTransformPoint(grip.position);
        return (handTrackingPos - gripLocal).sqrMagnitude <= maxGripDistance * maxGripDistance;
    }

    bool TryGetHandPoint(HandType hand, ref HandJointLocations joints, out Vector3 pos)
    {
        pos = Vector3.zero;
        try
        {
            if (!PXR_HandTracking.GetJointLocations(hand, ref joints)) return false;
            if (joints.jointLocations == null) return false;
            if (joints.jointLocations.Length <= (int)HandJoint.JointPalm) return false;
            pos = joints.jointLocations[(int)HandJoint.JointPalm].pose.Position.ToVector3();
            return true;
        }
        catch (Exception)
        {
            // 未创建 PXR_ProjectSetting 或手部追踪被禁用时会抛异常
            detectionEnabled = false;
            return false;
        }
    }
}
