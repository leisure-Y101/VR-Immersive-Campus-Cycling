using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Grip-based HUD adjustment. Grip starts the grab, the stick changes depth,
/// controller yaw moves the panel sideways, Trigger confirms, and B cancels.
/// Confirm controller feature mappings on the target PICO firmware.
/// </summary>
public class CyclingHudGrabber : MonoBehaviour
{
    private CyclingVRApp owner;
    private Camera vrCamera;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private Vector3 savedScale;
    private bool active;
    private bool dragging;
    private float dragDepth = 1.25f;
    private Vector3 grabOffset;
    private InputDevice right;
    private InputDevice left;
    private bool previousGrip;
    private bool previousB;
    private bool previousTrigger;
    private float grabYaw;

    [Header("HUD grab limits")]
    [SerializeField] private float minDepth = 0.65f;
    [SerializeField] private float maxDepth = 2.5f;
    [SerializeField] private float maxSideOffset = 0.85f;
    [SerializeField] private float depthSpeed = 0.5f;
    [SerializeField] private float yawToSideScale = 0.0125f;

    public bool IsActive => active;

    public void Begin(CyclingVRApp app, Camera camera, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        owner = app;
        vrCamera = camera;
        savedPosition = pos;
        savedRotation = rot;
        savedScale = scale;
        active = true;
        dragging = false;
        transform.localPosition = savedPosition;
        transform.localRotation = savedRotation;
        transform.localScale = savedScale;
        RefreshDevices();
        previousGrip = ReadButton(CommonUsages.gripButton);
        previousTrigger = ReadButton(CommonUsages.triggerButton);
        previousB = ReadButton(CommonUsages.secondaryButton);
    }

    private void RefreshDevices()
    {
        right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    }

    private void Update()
    {
        if (!active || vrCamera == null) return;
        if (!right.isValid || !left.isValid) RefreshDevices();

        bool gripRight = right.isValid && right.TryGetFeatureValue(CommonUsages.gripButton, out bool gr) && gr;
        bool gripLeft = left.isValid && left.TryGetFeatureValue(CommonUsages.gripButton, out bool gl) && gl;
        bool grip = gripRight || (!gripRight && gripLeft);

        bool bRight = right.isValid && right.TryGetFeatureValue(CommonUsages.secondaryButton, out bool br) && br;
        bool bLeft = left.isValid && left.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bl) && bl;
        bool b = bRight || bLeft;

        if (b && !previousB)
        {
            active = false;
            dragging = false;
            owner.OnHudAdjustmentFinished(false, savedPosition, savedRotation, savedScale);
            previousB = b;
            return;
        }
        previousB = b;

        bool trigger = ReadButton(CommonUsages.triggerButton);
        if (trigger && !previousTrigger)
        {
            active = false;
            dragging = false;
            owner.OnHudAdjustmentFinished(true, transform.localPosition, savedRotation, savedScale);
            previousTrigger = trigger;
            return;
        }
        previousTrigger = trigger;

        if (grip && !previousGrip)
        {
            dragging = true;
            dragDepth = Mathf.Clamp(Vector3.Distance(vrCamera.transform.position, transform.position), minDepth, maxDepth);
            Vector3 target = vrCamera.transform.position + vrCamera.transform.forward * dragDepth;
            grabOffset = transform.position - target;
            grabYaw = ReadControllerYaw();
        }
        if (!grip && previousGrip)
        {
            dragging = false;
        }
        previousGrip = grip;

        if (dragging)
        {
            Vector3 target = vrCamera.transform.position + vrCamera.transform.forward * dragDepth;
            float yawDelta = Mathf.DeltaAngle(grabYaw, ReadControllerYaw());
            Vector3 side = vrCamera.transform.right * Mathf.Clamp(yawDelta * yawToSideScale, -maxSideOffset, maxSideOffset);
            transform.position = target + grabOffset + side;
            // Keep the user's chosen orientation and scale stable while moving.
            transform.localRotation = savedRotation;
            transform.localScale = savedScale;

            // Grip + stick: up pushes the HUD away, down pulls it closer.
            Vector2 axis = ReadLargestStick();
            if (Mathf.Abs(axis.y) > 0.1f)
            {
                dragDepth = Mathf.Clamp(dragDepth + axis.y * Time.deltaTime * depthSpeed, minDepth, maxDepth);
            }
        }
    }

    private Vector2 ReadLargestStick()
    {
        Vector2 r = Vector2.zero, l = Vector2.zero;
        if (right.isValid) right.TryGetFeatureValue(CommonUsages.primary2DAxis, out r);
        if (left.isValid) left.TryGetFeatureValue(CommonUsages.primary2DAxis, out l);
        Vector2 axis = r.sqrMagnitude >= l.sqrMagnitude ? r : l;
        return axis.sqrMagnitude < 0.04f ? Vector2.zero : axis;
    }

    private float ReadControllerYaw()
    {
        Quaternion rotation;
        if (right.isValid && right.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            return rotation.eulerAngles.y;
        if (left.isValid && left.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            return rotation.eulerAngles.y;
        return 0f;
    }

    private bool ReadButton(InputFeatureUsage<bool> usage)
    {
        bool r = right.isValid && right.TryGetFeatureValue(usage, out bool rb) && rb;
        bool l = left.isValid && left.TryGetFeatureValue(usage, out bool lb) && lb;
        return r || l;
    }
}
