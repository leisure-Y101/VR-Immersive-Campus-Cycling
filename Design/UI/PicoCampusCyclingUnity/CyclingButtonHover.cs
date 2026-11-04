using UnityEngine;

/// <summary>Focus feedback for stick navigation. No ray pointer is required.</summary>
public class CyclingButtonHover : MonoBehaviour
{
    [SerializeField] private float hoverScale = 1.12f;
    [SerializeField] private float forwardOffset = 0.02f;
    [SerializeField] private float speed = 12f;

    private RectTransform rect;
    private Vector3 baseScale;
    private Vector3 basePosition;
    private bool focused;

    private void Awake()
    {
        rect = transform as RectTransform;
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
    }

    public void SetScale(float ignored) { }

    private void OnEnable()
    {
        if (rect == null) rect = transform as RectTransform;
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
    }

    public void SetFocused(bool value) { focused = value; }

    private void Update()
    {
        Vector3 targetScale = baseScale * (focused ? hoverScale : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * speed);

        // The panel's local +Z faces the viewer after it is placed toward the camera.
        Vector3 targetPosition = basePosition + (focused ? Vector3.forward * forwardOffset : Vector3.zero);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.unscaledDeltaTime * speed);
    }
}
