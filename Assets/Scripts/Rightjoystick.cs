using UnityEngine;
using UnityEngine.InputSystem;

public class Rightjoystick : MonoBehaviour
{
    float z, zr;
    public Vector2 combined => new Vector2(z, zr);

    public void OnZAxis(InputAction.CallbackContext ctx)
    {
        z = ctx.canceled ? 0f : ctx.ReadValue<float>();
    }

    public void OnZRAxis(InputAction.CallbackContext ctx)
    {
        zr = ctx.canceled ? 0f : ctx.ReadValue<float>();
    }

    void Update()
    {
        Vector2 v = combined;
        // pakai v di sini
    }
}