using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleLook : MonoBehaviour
{
    public float sensitivity = 0.15f;

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            transform.Rotate(-delta.y * sensitivity, delta.x * sensitivity, 0f, Space.Self);
        }
    }
}