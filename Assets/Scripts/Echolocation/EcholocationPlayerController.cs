using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace EcholocationGame
{
    [RequireComponent(typeof(CharacterController))]
    public class EcholocationPlayerController : MonoBehaviour
#if ENABLE_INPUT_SYSTEM
        , InputSystem_Actions.IPlayerActions
#endif
    {
        public enum ThrowableShape
        {
            BalokKecil = 0,
            BolaKecil = 1,
            SegitigaKecil = 2
        }

        [Header("Resonance Echo - Input System Connection")]
        [Tooltip("Terhubung otomatis dengan script Rightjoystick.cs dan InputSystem_Actions.cs")]
        public Rightjoystick rightJoystick;

        [Header("Movement Settings")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.5f;
        public float jumpHeight = 1.45f;
        public float gravity = -19.6f;

        [Header("Camera Look Settings (Mouse & Gamepad)")]
        public Transform playerCamera;
        public float mouseSensitivity = 0.16f;
        [Tooltip("Kecepatan putar kamera saat menggunakan Analog Kanan Gamepad / Twin USB Joystick (derajat/detik)")]
        public float gamepadLookSensitivity = 130f;
        public float gamepadDeadzone = 0.15f;
        public bool invertGamepadY = false;
        public float maxLookPitch = 85f;

        [Header("Footstep Echolocation Settings")]
        public float walkStepDistance = 1.75f;
        public float sprintStepDistance = 2.15f;

        [Header("Throw Object Settings (Lebih Kecil dari Objek Map)")]
        public ThrowableShape currentThrowShape = ThrowableShape.BalokKecil;
        [Tooltip("Ukuran objek yang dilempar (lebih kecil dari objek di map yang berukuran 1.5 - 4.0)")]
        public float thrownObjectScale = 0.24f;
        public float minThrowForce = 12f;
        public float maxThrowForce = 24f;
        public float maxChargeTime = 0.85f;
        public float throwCooldown = 0.22f;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _cameraPitch;
        private float _stepCycleDistance;
        private float _idleTime = 1f;

        // Jump & landing state (echolocation lompat hanya muncul saat menyentuh tanah)
        private bool _wasGrounded = true;
        private float _airborneTimer = 0f;
        private float _maxDownwardSpeedInAir = 0f;

        // Throw state
        private bool _isChargingThrow;
        private float _chargeStartTime;
        private float _lastThrowTime = -10f;
        private PhysicsMaterial _throwablePhysicsMat;

        // Viewmodel (bottom-right hand & held object)
        private Transform _handViewModelRoot;
        private Transform _heldPreviewObject;
        private Vector3 _handBaseLocalPos = new Vector3(0.35f, -0.27f, 0.56f);
        private float _throwAnimTimer;

        // UI & Indicator Bar smoothing
        private bool _showHud = true;
        private float _displayedIndicatorValue = 0f;

#if ENABLE_INPUT_SYSTEM
        private InputSystem_Actions _inputActions;
#endif
        // State dari InputSystem_Actions & Rightjoystick
        private Vector2 _actionMoveInput;
        private Vector2 _actionLookInput;
        private bool _actionLookIsMouse;
        private Vector2 _rightJoystickInput;
        private bool _actionSprintHeld;
        private bool _actionJumpQueued;
        private bool _actionThrowPressedThisFrame;
        private bool _actionThrowHeld;
        private bool _actionThrowReleasedThisFrame;
        private bool _actionPingQueued;
        private float _lastShapeSwitchTime = -10f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam == null && Camera.main != null)
                {
                    cam = Camera.main;
                    cam.transform.SetParent(transform, false);
                    cam.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                    cam.transform.localRotation = Quaternion.identity;
                }
                if (cam != null)
                {
                    playerCamera = cam.transform;
                }
            }

            // Hubungkan dengan script Rightjoystick.cs dari package dosen
            if (rightJoystick == null)
            {
                rightJoystick = GetComponent<Rightjoystick>();
                if (rightJoystick == null)
                {
                    rightJoystick = gameObject.AddComponent<Rightjoystick>();
                }
            }
            rightJoystick.echoPlayer = this;
            rightJoystick.deadzone = gamepadDeadzone;

#if ENABLE_INPUT_SYSTEM
            // Hubungkan dengan script InputSystem_Actions.cs dari package dosen
            _inputActions = new InputSystem_Actions();
            _inputActions.Player.AddCallbacks(this);
#endif

            // Pantulan tinggi agar objek yang dilempar bisa memantul berkali-kali dan echolocationnya makin mengecil
            _throwablePhysicsMat = new PhysicsMaterial("ThrowableBounce")
            {
                bounciness = 0.74f,
                dynamicFriction = 0.25f,
                staticFriction = 0.25f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };

            BuildHandViewModel();
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _inputActions?.Player.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            _inputActions?.Player.Disable();
#endif
        }

        private void OnDestroy()
        {
#if ENABLE_INPUT_SYSTEM
            if (_inputActions != null)
            {
                _inputActions.Player.RemoveCallbacks(this);
                _inputActions.Dispose();
                _inputActions = null;
            }
#endif
        }

        private void Start()
        {
            LockCursor(true);

            // Pancarkan 1 langkah awal kecil saat mulai
            if (EcholocationManager.Instance != null)
            {
                EcholocationManager.Instance.EmitFootstepPulse(GetFootPosition(), false);
            }
        }

        private void Update()
        {
            HandleCursorAndHudInput();
            HandleCameraLook();
            HandleMovementAndFootsteps();
            HandleThrowAndPingInput();
            UpdateHandViewModelAnimation();

            if (EcholocationManager.Instance != null)
            {
                _displayedIndicatorValue = Mathf.Lerp(
                    _displayedIndicatorValue,
                    EcholocationManager.Instance.indicatorValue,
                    Time.deltaTime * 12f);
            }

            // Reset one-frame action flags
            _actionJumpQueued = false;
            _actionThrowPressedThisFrame = false;
            _actionThrowReleasedThisFrame = false;
            _actionPingQueued = false;
        }

        /// <summary>
        /// Dipanggil oleh Rightjoystick.cs setiap frame untuk mengirimkan input analog kanan (Z & ZR / RightStick).
        /// </summary>
        public void SetRightJoystickInput(Vector2 lookVector)
        {
            _rightJoystickInput = lookVector;
        }

        #region InputSystem_Actions.IPlayerActions & PlayerInput Callbacks

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputAction.CallbackContext context)
        {
            _actionMoveInput = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (context.canceled)
            {
                _actionLookInput = Vector2.zero;
                return;
            }

            _actionLookInput = context.ReadValue<Vector2>();
            _actionLookIsMouse = context.control?.device is Pointer;
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _actionThrowPressedThisFrame = true;
                _actionThrowHeld = true;
            }
            else if (context.performed && !_actionThrowHeld)
            {
                _actionThrowPressedThisFrame = true;
                _actionThrowHeld = true;
            }
            else if (context.canceled)
            {
                _actionThrowHeld = false;
                _actionThrowReleasedThisFrame = true;
            }
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started || context.performed)
            {
                _actionPingQueued = true;
            }
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                _showHud = !_showHud;
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started || context.performed)
            {
                _actionJumpQueued = true;
            }
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
            if (context.performed && Time.time - _lastShapeSwitchTime > 0.15f)
            {
                _lastShapeSwitchTime = Time.time;
                CycleThrowableShape(-1);
            }
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            if (context.performed && Time.time - _lastShapeSwitchTime > 0.15f)
            {
                _lastShapeSwitchTime = Time.time;
                CycleThrowableShape(1);
            }
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            _actionSprintHeld = !context.canceled && context.ReadValueAsButton();
        }

        // Alias callback agar juga kompatibel jika dihubungkan lewat komponen PlayerInput seperti di PlayerMovement.cs / Rightjoystick.cs
        public void OnAksi(InputAction.CallbackContext context) => OnJump(context);
        public void OnTembak(InputAction.CallbackContext context) => OnAttack(context);

        public void OnZAxis(InputAction.CallbackContext context)
        {
            if (rightJoystick != null) rightJoystick.OnZAxis(context);
        }

        public void OnZRAxis(InputAction.CallbackContext context)
        {
            if (rightJoystick != null) rightJoystick.OnZRAxis(context);
        }

        public void OnZrAxis(InputAction.CallbackContext context)
        {
            if (rightJoystick != null) rightJoystick.OnZRAxis(context);
        }
#endif

        #endregion

        private void CycleThrowableShape(int direction)
        {
            int next = ((int)currentThrowShape + (direction > 0 ? 1 : 2)) % 3;
            SetThrowableShape((ThrowableShape)next);
        }

        private void HandleCursorAndHudInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    LockCursor(Cursor.lockState != CursorLockMode.Locked);
                }
                if (kb.hKey.wasPressedThisFrame)
                {
                    _showHud = !_showHud;
                }
                if (kb.digit1Key.wasPressedThisFrame) SetThrowableShape(ThrowableShape.BalokKecil);
                if (kb.digit2Key.wasPressedThisFrame) SetThrowableShape(ThrowableShape.BolaKecil);
                if (kb.digit3Key.wasPressedThisFrame) SetThrowableShape(ThrowableShape.SegitigaKecil);
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.1f && Time.time - _lastShapeSwitchTime > 0.12f)
                {
                    _lastShapeSwitchTime = Time.time;
                    CycleThrowableShape(scroll > 0 ? 1 : -1);
                }
            }
#endif
        }

        private void HandleCameraLook()
        {
            if (playerCamera == null) return;

            float yaw = 0f;
            float pitch = 0f;

#if ENABLE_INPUT_SYSTEM
            // 1. Input Mouse (ketika kursor terkunci)
            if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                yaw += mouseDelta.x * mouseSensitivity;
                pitch += mouseDelta.y * mouseSensitivity;
            }

            // 2. Input Analog Kanan dari Rightjoystick.cs (Twin USB Gamepad Z/ZR & Gamepad RightStick)
            //    serta OnLook dari InputSystem_Actions.cs
            Vector2 stickLook = _rightJoystickInput;
            if (rightJoystick != null)
            {
                Vector2 rj = rightJoystick.GetRightStickLook(gamepadDeadzone);
                if (rj.sqrMagnitude > stickLook.sqrMagnitude)
                {
                    stickLook = rj;
                }
            }

            if (!_actionLookIsMouse && _actionLookInput.magnitude >= gamepadDeadzone)
            {
                if (_actionLookInput.sqrMagnitude > stickLook.sqrMagnitude)
                {
                    stickLook = _actionLookInput;
                }
            }

            if (stickLook.sqrMagnitude > 0.0001f)
            {
                float ySign = invertGamepadY ? -1f : 1f;
                yaw += stickLook.x * gamepadLookSensitivity * Time.deltaTime;
                pitch += (stickLook.y * ySign) * gamepadLookSensitivity * Time.deltaTime;
            }
#endif

            _cameraPitch = Mathf.Clamp(_cameraPitch - pitch, -maxLookPitch, maxLookPitch);
            playerCamera.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            transform.Rotate(Vector3.up * yaw);
        }

        private void HandleMovementAndFootsteps()
        {
            bool isGrounded = _controller.isGrounded;

            if (!isGrounded)
            {
                _airborneTimer += Time.deltaTime;
                if (_velocity.y < 0f)
                {
                    _maxDownwardSpeedInAir = Mathf.Max(_maxDownwardSpeedInAir, -_velocity.y);
                }
            }

            // Saat lompat: echolocation BARU muncul ketika player kembali menyentuh tanah (mendarat)
            if (isGrounded && !_wasGrounded && _airborneTimer > 0.12f)
            {
                if (EcholocationManager.Instance != null)
                {
                    EcholocationManager.Instance.EmitJumpLandingPulse(GetFootPosition(), Mathf.Max(4.5f, _maxDownwardSpeedInAir));
                }
                _airborneTimer = 0f;
                _maxDownwardSpeedInAir = 0f;
                _stepCycleDistance = 0f;
            }
            _wasGrounded = isGrounded;

            if (isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }

            // Ambil input gerak dari InputSystem_Actions (OnMove: Gamepad Left Stick, Twin USB Stick, atau WASD)
            Vector2 moveInput = _actionMoveInput;
            bool isSprinting = _actionSprintHeld;
            bool jumpPressed = _actionJumpQueued;

#if ENABLE_INPUT_SYSTEM
            // Fallback langsung ke Keyboard, Gamepad, dan Joystick jika ada kontroler tambahan
            var kb = Keyboard.current;
            if (kb != null)
            {
                Vector2 kbMove = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) kbMove.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) kbMove.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) kbMove.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) kbMove.x -= 1f;
                if (kbMove.sqrMagnitude > moveInput.sqrMagnitude) moveInput = kbMove;

                isSprinting |= kb.leftShiftKey.isPressed;
                jumpPressed |= kb.spaceKey.wasPressedThisFrame;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 gpMove = gp.leftStick.ReadValue();
                if (gpMove.magnitude >= gamepadDeadzone && gpMove.sqrMagnitude > moveInput.sqrMagnitude)
                {
                    moveInput = gpMove;
                }
                isSprinting |= gp.leftStickButton.isPressed;
                jumpPressed |= gp.buttonSouth.wasPressedThisFrame;
            }

            var js = Joystick.current;
            if (js != null)
            {
                Vector2 jsMove = js.stick.ReadValue();
                if (jsMove.magnitude >= gamepadDeadzone && jsMove.sqrMagnitude > moveInput.sqrMagnitude)
                {
                    moveInput = jsMove;
                }
                isSprinting |= IsJoystickButtonPressed(js, "button6") || IsJoystickButtonPressed(js, "button5");
                jumpPressed |= WasJoystickButtonPressedThisFrame(js, "button2");
            }
#endif

            if (moveInput.magnitude < gamepadDeadzone)
            {
                moveInput = Vector2.zero;
            }
            else if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 moveWorld = (transform.right * moveInput.x + transform.forward * moveInput.y) * targetSpeed;

            // Saat mulai melompat (lepas landas), TIDAK memancarkan echolocation (baru muncul saat menyentuh tanah)
            if (jumpPressed && isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _airborneTimer = 0.01f;
                _maxDownwardSpeedInAir = 0f;
            }

            _velocity.y += gravity * Time.deltaTime;

            Vector3 beforePos = transform.position;
            _controller.Move((moveWorld + _velocity) * Time.deltaTime);
            Vector3 afterPos = transform.position;

            Vector2 horizDelta = new Vector2(afterPos.x - beforePos.x, afterPos.z - beforePos.z);
            float stepDelta = horizDelta.magnitude;

            if (_controller.isGrounded && moveInput.sqrMagnitude > 0.01f && stepDelta > 0.0005f)
            {
                if (_idleTime > 0.45f)
                {
                    _stepCycleDistance = 0f;
                    if (EcholocationManager.Instance != null)
                    {
                        EcholocationManager.Instance.EmitFootstepPulse(GetFootPosition(), isSprinting);
                    }
                }
                else
                {
                    _stepCycleDistance += stepDelta;
                    float threshold = isSprinting ? sprintStepDistance : walkStepDistance;
                    if (_stepCycleDistance >= threshold)
                    {
                        _stepCycleDistance -= threshold;
                        if (EcholocationManager.Instance != null)
                        {
                            EcholocationManager.Instance.EmitFootstepPulse(GetFootPosition(), isSprinting);
                        }
                    }
                }
                _idleTime = 0f;
            }
            else
            {
                _idleTime += Time.deltaTime;
            }
        }

        private void HandleThrowAndPingInput()
        {
            bool throwDown = _actionThrowPressedThisFrame;
            bool throwHeld = _actionThrowHeld;
            bool throwUp = _actionThrowReleasedThisFrame;
            bool manualPingDown = _actionPingQueued;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            var js = Joystick.current;

            if (mouse != null)
            {
                if (Cursor.lockState != CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame)
                {
                    LockCursor(true);
                    return;
                }

                throwDown |= mouse.leftButton.wasPressedThisFrame;
                throwHeld |= mouse.leftButton.isPressed;
                throwUp |= mouse.leftButton.wasReleasedThisFrame;
                manualPingDown |= mouse.rightButton.wasPressedThisFrame;
            }

            if (kb != null)
            {
                throwDown |= kb.fKey.wasPressedThisFrame;
                throwHeld |= kb.fKey.isPressed;
                throwUp |= kb.fKey.wasReleasedThisFrame;
                manualPingDown |= kb.qKey.wasPressedThisFrame;
            }

            if (gp != null)
            {
                bool gpAttackDown = gp.buttonWest.wasPressedThisFrame || gp.rightTrigger.wasPressedThisFrame;
                bool gpAttackHeld = gp.buttonWest.isPressed || gp.rightTrigger.isPressed;
                bool gpAttackUp = gp.buttonWest.wasReleasedThisFrame || gp.rightTrigger.wasReleasedThisFrame;

                throwDown |= gpAttackDown;
                throwHeld |= gpAttackHeld;
                throwUp |= gpAttackUp;
                manualPingDown |= gp.buttonNorth.wasPressedThisFrame;
            }

            if (js != null)
            {
                bool jsAttackDown = js.trigger.wasPressedThisFrame
                    || WasJoystickButtonPressedThisFrame(js, "button3")
                    || WasJoystickButtonPressedThisFrame(js, "button4")
                    || WasJoystickButtonPressedThisFrame(js, "button8");
                bool jsAttackHeld = js.trigger.isPressed
                    || IsJoystickButtonPressed(js, "button3")
                    || IsJoystickButtonPressed(js, "button4")
                    || IsJoystickButtonPressed(js, "button8");
                bool jsAttackUp = js.trigger.wasReleasedThisFrame
                    || WasJoystickButtonReleasedThisFrame(js, "button3")
                    || WasJoystickButtonReleasedThisFrame(js, "button4")
                    || WasJoystickButtonReleasedThisFrame(js, "button8");

                throwDown |= jsAttackDown;
                throwHeld |= jsAttackHeld;
                throwUp |= jsAttackUp;
                manualPingDown |= WasJoystickButtonPressedThisFrame(js, "button1");
            }
#endif

            if (throwDown && Time.time - _lastThrowTime >= throwCooldown)
            {
                _isChargingThrow = true;
                _chargeStartTime = Time.time;
            }

            if (_isChargingThrow && (!throwHeld || throwUp))
            {
                float chargeRatio = Mathf.Clamp01((Time.time - _chargeStartTime) / maxChargeTime);
                float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);
                ThrowSmallObject(throwForce);
                _isChargingThrow = false;
                _lastThrowTime = Time.time;
            }

            if (manualPingDown && EcholocationManager.Instance != null)
            {
                EcholocationManager.Instance.EmitFootstepPulse(GetFootPosition(), true);
            }
        }

#if ENABLE_INPUT_SYSTEM
        private static bool IsJoystickButtonPressed(Joystick js, string buttonName)
        {
            return js != null && js.TryGetChildControl<ButtonControl>(buttonName) is ButtonControl btn && btn.isPressed;
        }

        private static bool WasJoystickButtonPressedThisFrame(Joystick js, string buttonName)
        {
            return js != null && js.TryGetChildControl<ButtonControl>(buttonName) is ButtonControl btn && btn.wasPressedThisFrame;
        }

        private static bool WasJoystickButtonReleasedThisFrame(Joystick js, string buttonName)
        {
            return js != null && js.TryGetChildControl<ButtonControl>(buttonName) is ButtonControl btn && btn.wasReleasedThisFrame;
        }
#endif

        private void ThrowSmallObject(float force)
        {
            if (playerCamera == null) return;

            Vector3 spawnPos = playerCamera.position + playerCamera.forward * 0.65f + playerCamera.right * 0.22f - playerCamera.up * 0.12f;
            Vector3 throwDir = (playerCamera.forward + Vector3.up * 0.08f).normalized;

            Vector3 scale = Vector3.one * thrownObjectScale;
            Quaternion rot = Random.rotation;

            GameObject thrownObj;
            switch (currentThrowShape)
            {
                case ThrowableShape.BolaKecil:
                    thrownObj = EcholocationMeshBuilder.CreateSphere("Thrown_SmallSphere", spawnPos, scale, null, 0.25f);
                    break;
                case ThrowableShape.SegitigaKecil:
                    thrownObj = EcholocationMeshBuilder.CreatePyramid("Thrown_SmallTriangle", spawnPos, scale * 1.15f, rot, null, true, 0.25f);
                    break;
                default:
                    thrownObj = EcholocationMeshBuilder.CreateBox("Thrown_SmallBox", spawnPos, scale, rot, null, 0.25f);
                    break;
            }

            Collider col = thrownObj.GetComponent<Collider>();
            if (col != null)
            {
                col.sharedMaterial = _throwablePhysicsMat;
                if (_controller != null)
                {
                    Physics.IgnoreCollision(col, _controller, true);
                }
            }

            Rigidbody rb = thrownObj.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearVelocity = throwDir * force;
            rb.angularVelocity = Random.insideUnitSphere * 8f;

            thrownObj.AddComponent<ThrowableEchoObject>();

            // HANYA suara lemparan, TIDAK ADA echolocation saat dilempar (echolocation baru muncul saat objek tertabrak!)
            if (EcholocationManager.Instance != null)
            {
                EcholocationManager.Instance.PlayThrowWhoosh(spawnPos);
            }

            _throwAnimTimer = 0.22f;
        }

        private void BuildHandViewModel()
        {
            if (playerCamera == null) return;

            Transform existing = playerCamera.Find("HandViewModel");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            GameObject root = new GameObject("HandViewModel");
            root.transform.SetParent(playerCamera, false);
            root.transform.localPosition = _handBaseLocalPos;
            root.transform.localRotation = Quaternion.Euler(12f, -18f, 8f);
            _handViewModelRoot = root.transform;

            GameObject arm = EcholocationMeshBuilder.CreateBox(
                "ViewModel_Arm",
                Vector3.zero,
                new Vector3(0.11f, 0.10f, 0.32f),
                Quaternion.identity,
                _handViewModelRoot,
                0.92f);
            arm.transform.localPosition = new Vector3(0.05f, -0.06f, -0.12f);
            arm.transform.localRotation = Quaternion.Euler(14f, -8f, -12f);
            RemoveCollider(arm);

            GameObject grip = EcholocationMeshBuilder.CreateBox(
                "ViewModel_Grip",
                Vector3.zero,
                new Vector3(0.08f, 0.08f, 0.16f),
                Quaternion.identity,
                _handViewModelRoot,
                0.92f);
            grip.transform.localPosition = new Vector3(0.0f, -0.01f, 0.02f);
            grip.transform.localRotation = Quaternion.Euler(-10f, 12f, 0f);
            RemoveCollider(grip);

            RebuildHeldPreviewObject();
        }

        private void SetThrowableShape(ThrowableShape shape)
        {
            if (currentThrowShape == shape) return;
            currentThrowShape = shape;
            RebuildHeldPreviewObject();
        }

        private void RebuildHeldPreviewObject()
        {
            if (_handViewModelRoot == null) return;

            if (_heldPreviewObject != null)
            {
                Destroy(_heldPreviewObject.gameObject);
            }

            Vector3 previewScale = Vector3.one * 0.095f;
            GameObject preview;
            switch (currentThrowShape)
            {
                case ThrowableShape.BolaKecil:
                    preview = EcholocationMeshBuilder.CreateSphere("ViewModel_HeldSphere", Vector3.zero, previewScale, _handViewModelRoot, 0.95f);
                    break;
                case ThrowableShape.SegitigaKecil:
                    preview = EcholocationMeshBuilder.CreatePyramid("ViewModel_HeldTriangle", Vector3.zero, previewScale * 1.1f, Quaternion.identity, _handViewModelRoot, true, 0.95f);
                    break;
                default:
                    preview = EcholocationMeshBuilder.CreateBox("ViewModel_HeldBox", Vector3.zero, previewScale, Quaternion.identity, _handViewModelRoot, 0.95f);
                    break;
            }

            preview.transform.localPosition = new Vector3(-0.02f, 0.055f, 0.11f);
            preview.transform.localRotation = Quaternion.Euler(18f, 32f, 10f);
            RemoveCollider(preview);
            _heldPreviewObject = preview.transform;
        }

        private void UpdateHandViewModelAnimation()
        {
            if (_handViewModelRoot == null) return;

            float bobX = Mathf.Sin(_stepCycleDistance * Mathf.PI * 1.8f) * 0.014f;
            float bobY = Mathf.Abs(Mathf.Cos(_stepCycleDistance * Mathf.PI * 1.8f)) * 0.012f;

            Vector3 chargeOffset = Vector3.zero;
            if (_isChargingThrow)
            {
                float ratio = Mathf.Clamp01((Time.time - _chargeStartTime) / maxChargeTime);
                chargeOffset = new Vector3(0.03f * ratio, -0.02f * ratio, -0.08f * ratio);
            }

            Vector3 throwPush = Vector3.zero;
            if (_throwAnimTimer > 0f)
            {
                _throwAnimTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(_throwAnimTimer / 0.22f);
                throwPush = new Vector3(-0.03f, 0.02f, 0.12f) * Mathf.Sin(t * Mathf.PI);
            }

            _handViewModelRoot.localPosition = Vector3.Lerp(
                _handViewModelRoot.localPosition,
                _handBaseLocalPos + new Vector3(bobX, bobY, 0f) + chargeOffset + throwPush,
                Time.deltaTime * 16f);

            if (_heldPreviewObject != null)
            {
                _heldPreviewObject.Rotate(Vector3.up, 35f * Time.deltaTime, Space.Self);
            }
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }

        private Vector3 GetFootPosition()
        {
            return new Vector3(transform.position.x, transform.position.y - (_controller.height * 0.5f) + 0.05f, transform.position.z);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private string GetConnectedControllerStatus()
        {
#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current != null)
            {
                return $"Gamepad Aktif ({Gamepad.current.displayName})";
            }
            if (Joystick.current != null)
            {
                return $"USB Gamepad Aktif ({Joystick.current.displayName})";
            }
#endif
            return "Keyboard & Mouse (Colok Gamepad untuk langsung pakai)";
        }

        private void OnGUI()
        {
            DrawEcholocationIndicatorBar();

            // Red flash overlay when caught by the Monster
            float timeSinceCatch = Time.time - EcholocationEnemy.LastCatchTime;
            if (timeSinceCatch < 1.8f)
            {
                float alpha = (1f - timeSinceCatch / 1.8f) * 0.55f;
                GUI.color = new Color(0.9f, 0.05f, 0.05f, alpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

                GUIStyle alertStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                GUI.color = Color.white;
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 60f), "TERTANGKAP MONSTER! POSISI DIRESET", alertStyle);
            }

            if (!_showHud) return;

            string shapeName = currentThrowShape switch
            {
                ThrowableShape.BolaKecil => "Bola Kecil (Sphere)",
                ThrowableShape.SegitigaKecil => "Segitiga Kecil (Pyramid)",
                _ => "Balok Kecil (Cube)"
            };

            string info =
                "<b>RESONANCE ECHO</b>  <color=#88FFAA>[" + GetConnectedControllerStatus() + "]</color>\n" +
                "• <b>Analog Kiri / WASD</b> : Melangkah | <b>L3 / R1 (Btn6) / Shift</b> : Lari\n" +
                "• <b>Analog Kanan (Z/ZR) / Mouse</b> : Putar Kamera\n" +
                "• <b>Tombol Bawah (Cross/A/Btn2) / Space</b> : Lompat (Echo Saat Mendarat)\n" +
                "• <b>Kotak (X/Btn3) / R2 / Klik Kiri / F</b> : Tahan & Lepas Lempar Objek\n" +
                "• <b>D-Pad Kiri-Kanan / L1-R1 / 1-2-3</b> : Ganti Objek [" + shapeName + "]\n" +
                "• <b>Segitiga (Y/Btn1) / Q</b> : Ping Manual | <b>Bulat (B) / H</b> : HUD";

            GUI.color = new Color(0f, 0f, 0f, 0.74f);
            GUI.Box(new Rect(14, 14, 555, 158), "");
            GUI.color = Color.white;
            GUI.Label(new Rect(24, 20, 540, 148), info, new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 13,
                normal = { textColor = new Color(0.92f, 0.92f, 0.92f, 0.92f) }
            });

            // Crosshair dot in center
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.DrawTexture(new Rect(cx - 2f, cy - 2f, 4f, 4f), Texture2D.whiteTexture);

            // Throw charge bar
            if (_isChargingThrow)
            {
                float ratio = Mathf.Clamp01((Time.time - _chargeStartTime) / maxChargeTime);
                float barW = 120f;
                float barH = 8f;
                GUI.color = new Color(1f, 1f, 1f, 0.25f);
                GUI.DrawTexture(new Rect(cx - barW * 0.5f, cy + 18f, barW, barH), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(cx - barW * 0.5f, cy + 18f, barW * ratio, barH), Texture2D.whiteTexture);
            }
        }

        private void DrawEcholocationIndicatorBar()
        {
            float maxVal = EcholocationManager.Instance != null ? EcholocationManager.Instance.maxIndicatorValue : 100f;
            float norm = Mathf.Clamp01(_displayedIndicatorValue / Mathf.Max(1f, maxVal));

            float barWidth = 380f;
            float barHeight = 16f;
            float x = (Screen.width - barWidth) * 0.5f;
            float y = 24f;

            // Background frame
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);
            GUI.DrawTexture(new Rect(x - 4f, y - 4f, barWidth + 8f, barHeight + 34f), Texture2D.whiteTexture);

            // Outer border
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.DrawTexture(new Rect(x - 1f, y - 1f, barWidth + 2f, barHeight + 2f), Texture2D.whiteTexture);

            // Inner dark track
            GUI.color = new Color(0.02f, 0.02f, 0.02f, 1f);
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

            // Fill color based on echolocation intensity
            Color fillCol;
            string statusText;
            if (norm < 0.15f)
            {
                fillCol = new Color(0.85f, 0.85f, 0.85f, 0.9f);
                statusText = "RESONANCE ECHO: TENANG (Monster Patroli)";
            }
            else if (norm < 0.55f)
            {
                fillCol = Color.Lerp(new Color(1f, 0.85f, 0.2f, 1f), new Color(1f, 0.45f, 0.1f, 1f), (norm - 0.15f) / 0.4f);
                statusText = "RESONANCE ECHO: WASPADA (Monster Mendekati Sumber Suara!)";
            }
            else
            {
                fillCol = new Color(1f, 0.12f, 0.12f, 1f);
                statusText = "RESONANCE ECHO: BAHAYA! (Monster Mengejar Titik Suara!)";
            }

            GUI.color = fillCol;
            GUI.DrawTexture(new Rect(x, y, barWidth * norm, barHeight), Texture2D.whiteTexture);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = fillCol }
            };
            GUI.color = Color.white;
            GUI.Label(new Rect(x - 20f, y + barHeight + 2f, barWidth + 40f, 22f), statusText, labelStyle);
        }
    }
}
