using UnityEngine;

// 调试用：手柄控制模式下，右摇杆 X 控制转向（-1 左 ~ +1 右）
public class ControllerSteeringSource : MonoBehaviour, ISteeringSource
{
    public float deadzone = 0.15f;

    public bool Available => InputRouter.I != null;

    public float Steer
    {
        get
        {
            var r = InputRouter.I;
            if (r == null) return 0f;
            float x = r.StickX;
            if (Mathf.Abs(x) < deadzone) return 0f;
            float sign = Mathf.Sign(x);
            return sign * Mathf.InverseLerp(deadzone, 1f, Mathf.Abs(x));
        }
    }
}
