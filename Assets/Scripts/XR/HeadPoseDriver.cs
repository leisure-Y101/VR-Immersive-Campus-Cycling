using UnityEngine;
using UnityEngine.XR;

// 头部姿态驱动：把 PICO 的头部 6DoF 姿态应用到相机
// 背景：场景里的 TrackedPoseDriver 没有任何输入绑定（m_SingletonActionBindings 为空），
// 头显姿态从未生效，导致画面“贴脸”、转头整个世界跟着转。
// 本脚本在运行时接管相机姿态，并禁用未正确配置的 TrackedPoseDriver。
public class HeadPoseDriver : MonoBehaviour
{
    public bool applyPosition = true;
    public bool applyRotation = true;

    // 视角高度微调（米），叠加在真实头高之上；编辑器里以 1.6m 为基准
    public float eyeHeightOffset = 0f;

    InputDevice head;
    bool hasHead;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (cam == null) return;

        foreach (var mb in cam.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            string n = mb.GetType().FullName;
            if (n == "UnityEngine.InputSystem.XR.TrackedPoseDriver" ||
                n == "UnityEngine.SpatialTracking.TrackedPoseDriver")
            {
                mb.enabled = false;
                Debug.Log("HeadPoseDriver: 已禁用未配置的 " + n);
            }
        }

        if (cam.GetComponent<HeadPoseDriver>() == null)
            cam.gameObject.AddComponent<HeadPoseDriver>();
    }

    void Update()
    {
        if (!hasHead || !head.isValid)
        {
            head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            hasHead = head.isValid;
        }

        if (!hasHead)
        {
            // 没有头显（桌面编辑器）：模拟 1.6m 眼高，方便 UI 与场景调试
            if (applyPosition)
                transform.localPosition = new Vector3(0f, 1.6f + eyeHeightOffset, 0f);
            return;
        }

        if (applyPosition &&
            head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 pos))
        {
            transform.localPosition = pos + Vector3.up * eyeHeightOffset;
        }

        if (applyRotation &&
            head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rot))
        {
            transform.localRotation = rot;
        }
    }
}
