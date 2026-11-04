using UnityEngine;

/// <summary>
/// PicoCampusCyclingUnity 风格的空间按钮反馈。
/// 射线选择由 CampusRideApp 管理，这个组件只负责视觉反馈，
/// 因此不依赖 EventSystem 或特定 PICO SDK。
/// </summary>
public sealed class PicoCampusCyclingButtonHover : MonoBehaviour
{
    [SerializeField, Range(1.0f, 1.15f)] float focusScale = 1.06f;
    [SerializeField, Range(0f, 0.04f)] float focusForwardOffset = 0.012f;
    [SerializeField, Range(4f, 24f)] float smoothing = 14f;

    Vector3 baseScale;
    Vector3 basePosition;
    bool focused;

    void Awake()
    {
        CaptureBaseTransform();
    }

    void OnEnable()
    {
        CaptureBaseTransform();
    }

    void CaptureBaseTransform()
    {
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
    }

    /// <summary>由射线悬停或键盘选中状态调用。</summary>
    public void SetFocused(bool value)
    {
        focused = value;
    }

    void Update()
    {
        Vector3 targetScale = baseScale * (focused ? focusScale : 1f);
        Vector3 targetPosition = basePosition + (focused ? Vector3.back * focusForwardOffset : Vector3.zero);
        float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, t);
    }
}
