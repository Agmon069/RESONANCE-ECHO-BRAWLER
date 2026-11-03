using UnityEngine;
using UnityEngine.InputSystem;

public class Rightjoystick : MonoBehaviour
{
    float z, zr;

    // Tetap mempertahankan properti asli (x = z, y = zr) agar kompatibel dengan scene dosen
    public Vector2 combined => new Vector2(z, zr);

    [Header("Resonance Echo - Right Stick Connection")]
    public float deadzone = 0.15f;
    public EcholocationGame.EcholocationPlayerController echoPlayer;

    private InputAction _zAxisAction;
    private InputAction _zrAxisAction;
    private bool _hasCallbackZ;
    private bool _hasCallbackZR;

    private void Awake()
    {
        if (echoPlayer == null)
        {
            echoPlayer = GetComponent<EcholocationGame.EcholocationPlayerController>();
        }

        // Otomatis membuat binding axis Z (vertikal) dan ZR/RZ (horizontal) untuk Twin USB Gamepad / Generic Joystick
        _zAxisAction = new InputAction("RightStick_Z", InputActionType.Value);
        _zAxisAction.AddBinding("<HID::Twin USB Gamepad      >/z");
        _zAxisAction.AddBinding("<Joystick>/z");
        _zAxisAction.AddBinding("*/{Z}");

        _zrAxisAction = new InputAction("RightStick_ZR", InputActionType.Value);
        _zrAxisAction.AddBinding("<HID::Twin USB Gamepad      >/rz");
        _zrAxisAction.AddBinding("<Joystick>/rz");
        _zrAxisAction.AddBinding("*/{RZ}");
    }

    private void OnEnable()
    {
        _zAxisAction?.Enable();
        _zrAxisAction?.Enable();
    }

    private void OnDisable()
    {
        _zAxisAction?.Disable();
        _zrAxisAction?.Disable();
    }

    private void OnDestroy()
    {
        _zAxisAction?.Dispose();
        _zrAxisAction?.Dispose();
    }

    public void OnZAxis(InputAction.CallbackContext ctx)
    {
        _hasCallbackZ = true;
        z = ctx.canceled ? 0f : ctx.ReadValue<float>();
    }

    public void OnZRAxis(InputAction.CallbackContext ctx)
    {
        _hasCallbackZR = true;
        zr = ctx.canceled ? 0f : ctx.ReadValue<float>();
    }

    public void OnZrAxis(InputAction.CallbackContext ctx)
    {
        OnZRAxis(ctx);
    }

    /// <summary>
    /// Mengembalikan input analog kanan (x = horizontal / ZR, y = vertikal / -Z)
    /// baik dari Twin USB Gamepad (Z & RZ) maupun Gamepad standar (Right Stick).
    /// </summary>
    public Vector2 GetRightStickLook(float customDeadzone = -1f)
    {
        float dz = customDeadzone >= 0f ? customDeadzone : deadzone;

        // 1. Baca dari Z dan ZR (Twin USB Gamepad: zr = horizontal, -z = vertikal seperti di CrosshairController)
        Vector2 twinStick = new Vector2(zr, -z);
        if (twinStick.magnitude < dz)
        {
            twinStick = Vector2.zero;
        }

        // 2. Baca juga dari Gamepad standar (<Gamepad>/rightStick) jika terhubung
        Vector2 gamepadStick = Vector2.zero;
        if (Gamepad.current != null)
        {
            Vector2 rs = Gamepad.current.rightStick.ReadValue();
            if (rs.magnitude >= dz)
            {
                gamepadStick = rs;
            }
        }

        return twinStick.sqrMagnitude >= gamepadStick.sqrMagnitude ? twinStick : gamepadStick;
    }

    void Update()
    {
        // Jika tidak dipanggil lewat event PlayerInput eksternal, baca langsung dari InputAction Z & ZR
        if (!_hasCallbackZ && _zAxisAction != null)
        {
            z = ReadCenteredAxis(_zAxisAction);
        }
        if (!_hasCallbackZR && _zrAxisAction != null)
        {
            zr = ReadCenteredAxis(_zrAxisAction);
        }

        Vector2 v = combined;

        // Hubungkan langsung ke kamera Resonance Echo (EcholocationPlayerController) jika ada
        if (echoPlayer == null)
        {
            echoPlayer = GetComponent<EcholocationGame.EcholocationPlayerController>();
        }
        if (echoPlayer != null)
        {
            echoPlayer.SetRightJoystickInput(GetRightStickLook(deadzone));
        }
    }

    private static float ReadCenteredAxis(InputAction action)
    {
        if (action == null || action.activeControl == null) return 0f;
        float val = action.ReadValue<float>();
        return Mathf.Abs(val) < 0.08f ? 0f : Mathf.Clamp(val, -1f, 1f);
    }
}
