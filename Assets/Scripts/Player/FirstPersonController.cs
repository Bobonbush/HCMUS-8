using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CharacterController-driven first person motor.
///
/// The "not gliding" feel comes from three things in here:
///  1. Velocity is accelerated/decelerated, never assigned directly.
///  2. The step cycle is driven by distance actually travelled (not by a timer),
///     so footfalls line up with the ground even when you are slowed, on a slope,
///     or pushing into a wall.
///  3. Grounding uses coyote time + a ground snap so slopes and small ledges do
///     not make you go momentarily airborne (which would kill the footfalls).
///
/// Camera feel and audio subscribe to the events below; this script never touches
/// the camera other than applying pitch to <see cref="cameraPivot"/>.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[AddComponentMenu("Player/First Person Controller")]
[DisallowMultipleComponent]
public class FirstPersonController : MonoBehaviour
{
    public enum Foot { Left, Right }

    [Header("References")]
    [Tooltip("Empty transform at eye height. Pitch is applied here, the camera lives under it.")]
    public Transform cameraPivot;

    [Header("Look")]
    [Tooltip("Degrees of rotation per pixel of mouse movement.")]
    [Range(0.01f, 0.5f)] public float mouseSensitivity = 0.09f;
    [Tooltip("Degrees per second at full gamepad stick deflection.")]
    public float gamepadLookSpeed = 200f;
    public float pitchMin = -88f;
    public float pitchMax = 88f;
    public bool invertY = false;
    public bool invertX = false;

    [Tooltip("Player-facing multiplier on top of the base sensitivity (Settings > Game).")]
    [Range(0.1f, 3f)] public float lookSensitivityX = 1f;
    [Range(0.1f, 3f)] public float lookSensitivityY = 1f;
    [Tooltip("How fast the view catches up to the input. High is raw and immediate; low is heavy " +
             "and smoothed. 15 is roughly indistinguishable from raw.")]
    [Range(1f, 30f)] public float lookAcceleration = 15f;

    [Header("Speed")]
    public float walkSpeed = 3.2f;
    public float sprintSpeed = 6.0f;
    [Tooltip("Multiplier applied when moving backwards.")]
    [Range(0.3f, 1f)] public float backwardMultiplier = 0.78f;
    [Tooltip("Multiplier applied when strafing.")]
    [Range(0.3f, 1f)] public float strafeMultiplier = 0.9f;

    [Header("Acceleration")]
    [Tooltip("m/s^2 when speeding up on the ground. Lower = heavier.")]
    public float groundAcceleration = 55f;
    [Tooltip("m/s^2 when slowing down on the ground.")]
    public float groundDeceleration = 42f;
    [Tooltip("m/s^2 available while airborne. Low values give committed jumps.")]
    public float airAcceleration = 14f;

    [Header("Sprint")]
    public bool holdToSprint = true;
    [Tooltip("Sprint only engages when there is forward input, like most immersive sims.")]
    public bool sprintRequiresForward = true;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.0f;
    public float gravity = -20f;
    [Tooltip("Gravity is multiplied by this while falling, so the arc is snappy rather than floaty.")]
    public float fallGravityMultiplier = 1.45f;
    [Tooltip("Grace period after walking off a ledge during which a jump still works.")]
    public float coyoteTime = 0.12f;
    [Tooltip("A jump pressed this long before landing still fires on touchdown.")]
    public float jumpBufferTime = 0.14f;
    public float terminalVelocity = -55f;

    [Header("Ground")]
    public LayerMask groundMask = ~0;
    [Tooltip("Pulls you back onto the ground when walking down slopes or off small lips.")]
    public bool snapToGround = true;

    [Header("Step Cycle")]
    [Tooltip("Metres travelled between footfalls while walking.")]
    public float walkStrideLength = 1.5f;
    [Tooltip("Metres travelled between footfalls while sprinting.")]
    public float sprintStrideLength = 2.0f;
    [Tooltip("Below this speed no footsteps are produced.")]
    public float minStepSpeed = 0.55f;

    [Header("Input")]
    [Tooltip("Assets/InputSystem_Actions.inputactions. Every key this motor reads comes from here " +
             "so the Settings > Keyboard tab can rebind it; nothing is hardcoded.")]
    public InputActionAsset inputActions;

    [Header("Cursor")]
    public bool lockCursorOnStart = true;

    [Header("VR")]
    [Tooltip("Set by VRRig when a headset drives the view. Look input is ignored (the HMD owns " +
             "the camera) and movement directions follow Move Reference instead of the body.")]
    public bool vrMode;
    [Tooltip("In VR, the head camera. Move input is relative to where the player is looking.")]
    public Transform moveReference;

    // ---------------------------------------------------------------- state

    /// <summary>Horizontal speed in m/s, measured from the distance actually covered.</summary>
    public float HorizontalSpeed { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsMoving => HorizontalSpeed > minStepSpeed;
    public Vector3 Velocity => _velocity;
    public Vector2 MoveInput => _moveInput;
    /// <summary>Degrees of yaw (x) and pitch (y) applied by look input this frame.</summary>
    public Vector2 LookDeltaDegrees => _lookDeltaDegrees;
    /// <summary>0..1 where 1 is full sprint speed. Drives bob amplitude and FOV.</summary>
    public float SpeedRatio => Mathf.Clamp01(HorizontalSpeed / Mathf.Max(0.01f, sprintSpeed));
    /// <summary>Radians. Advances by PI for every footfall, so a full stride is 2 PI.</summary>
    public float StepPhase => _stepPhase;
    public Foot NextFoot => _nextFoot;

    /// <summary>Fired on every footfall. Args: which foot, impact strength 0..1.</summary>
    public event Action<Foot, float> OnFootstep;
    /// <summary>Fired on touchdown. Arg: downward speed at impact in m/s (positive).</summary>
    public event Action<float> OnLand;
    public event Action OnJumped;

    // ---------------------------------------------------------------- private

    CharacterController _cc;
    Vector3 _velocity;
    Vector2 _moveInput;
    Vector2 _lookDeltaDegrees;
    Vector2 _smoothedLook;
    float _yaw;
    float _pitch;

    float _coyoteTimer;
    float _jumpBufferTimer;
    bool _wasGrounded;
    float _fallSpeed;          // downward speed carried into the landing event
    bool _sprintToggleState;

    float _stepPhase;
    float _stepAccumulator;
    Foot _nextFoot = Foot.Left;
    Vector3 _lastPosition;

    readonly RaycastHit[] _groundHits = new RaycastHit[8];

    InputActionMap _playerMap;
    InputAction _moveAction;
    InputAction _lookAction;
    InputAction _sprintAction;
    InputAction _jumpAction;
    bool _inputEnabled = true;

    // ---------------------------------------------------------------- lifecycle

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _lastPosition = transform.position;
        _yaw = transform.eulerAngles.y;
        if (cameraPivot != null) _pitch = NormalizeAngle(cameraPivot.localEulerAngles.x);

        ResolveInputActions();
    }

    void Start()
    {
        if (lockCursorOnStart) SetCursorLocked(true);
    }

    void OnEnable()
    {
        _playerMap?.Enable();
    }

    void OnDisable()
    {
        _playerMap?.Disable();
    }

    /// <summary>
    /// Pulls the actions out of the shared asset. They are NOT built in code any more: a key that is
    /// created here would not appear in Settings > Keyboard, so rebinding it would look like it
    /// worked while the old key kept firing. See the rule at the top of Game.UI.InputBindingService.
    /// </summary>
    void ResolveInputActions()
    {
        if (inputActions == null)
        {
            inputActions = Game.UI.InputBindingService.Instance != null
                ? Game.UI.InputBindingService.Instance.actions
                : null;
        }

        if (inputActions == null)
        {
            enabled = false;
            Debug.LogError(
                "FirstPersonController: no InputActionAsset assigned. Drag " +
                "Assets/InputSystem_Actions.inputactions onto the Input Actions field.", this);
            return;
        }

        // Registering the asset first means saved key overrides are already applied by the time
        // the actions below are read.
        Game.UI.InputBindingService.EnsureExists(inputActions);

        _playerMap = inputActions.FindActionMap("Player", false);
        _moveAction = inputActions.FindAction("Player/Move", false);
        _lookAction = inputActions.FindAction("Player/Look", false);
        _sprintAction = inputActions.FindAction("Player/Sprint", false);
        _jumpAction = inputActions.FindAction("Player/Jump", false);

        if (_playerMap == null || _moveAction == null || _lookAction == null ||
            _sprintAction == null || _jumpAction == null)
        {
            enabled = false;
            Debug.LogError(
                "FirstPersonController: the assigned asset is missing one of Player/Move, " +
                "Player/Look, Player/Sprint or Player/Jump.", this);
        }
    }

    // ---------------------------------------------------------------- update

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        ReadInput();
        UpdateLook(dt);
        UpdateGroundState(dt);
        UpdateMovement(dt);
        ApplyMotion(dt);
        UpdateStepCycle();
    }

    /// <summary>
    /// Suspends look/move input while a menu is up. The pause menu owns this — and owns the cursor —
    /// because Escape belongs to exactly one system.
    /// </summary>
    public void SetInputEnabled(bool value)
    {
        _inputEnabled = value;
        if (value) return;

        _moveInput = Vector2.zero;
        _lookDeltaDegrees = Vector2.zero;
        _smoothedLook = Vector2.zero;
        _sprintToggleState = false;
        _jumpBufferTimer = 0f;
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void ReadInput()
    {
        if (!_inputEnabled)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = _moveAction.ReadValue<Vector2>();
        if (_moveInput.sqrMagnitude > 1f) _moveInput.Normalize();

        bool sprintHeld = _sprintAction.IsPressed();
        if (holdToSprint)
        {
            _sprintToggleState = sprintHeld;
        }
        else
        {
            if (_sprintAction.WasPressedThisFrame()) _sprintToggleState = !_sprintToggleState;
            if (_moveInput.sqrMagnitude < 0.01f) _sprintToggleState = false;
        }

        //if (_jumpAction.WasPressedThisFrame()) _jumpBufferTimer = jumpBufferTime;
    }

    /// <summary>VR snap turn: rotates the body without fighting the look pipeline.</summary>
    public void AddYaw(float degrees)
    {
        _yaw += degrees;
    }

    void UpdateLook(float dt)
    {
        if (vrMode)
        {
            // The HMD owns the view. Body yaw still applies (snap turn via AddYaw);
            // the camera pivot is left alone for the TrackedPoseDriver-driven camera.
            _lookDeltaDegrees = Vector2.zero;
            transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            return;
        }

        Vector2 mouse = Vector2.zero;
        Vector2 stick = Vector2.zero;

        if (_inputEnabled)
        {
            // One Look action covers both devices in the asset, so the source decides the scaling:
            // mouse delta is already a per-frame value (it must NOT be scaled by deltaTime) while the
            // stick is a -1..1 axis.
            Vector2 raw = _lookAction.ReadValue<Vector2>();
            bool fromGamepad = _lookAction.activeControl != null &&
                               _lookAction.activeControl.device is Gamepad;

            if (fromGamepad)
                // Squared response gives fine control near centre without losing top speed.
                stick = raw * raw.magnitude * gamepadLookSpeed * dt;
            else
                mouse = raw * mouseSensitivity;
        }

        // Player-facing sensitivity from Settings > Game, applied to both devices.
        mouse.x *= lookSensitivityX;
        mouse.y *= lookSensitivityY;
        stick.x *= lookSensitivityX;
        stick.y *= lookSensitivityY;

        float yawDelta = mouse.x + stick.x;
        float pitchDelta = mouse.y + stick.y;
        if (!invertY) pitchDelta = -pitchDelta;
        if (invertX) yawDelta = -yawDelta;

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            // Ignore mouse while the cursor is free so editor clicks do not fling the view.
            yawDelta = invertX ? -stick.x : stick.x;
            pitchDelta = invertY ? stick.y : -stick.y;
        }

        // Camera Acceleration: how quickly the view catches up to the raw input. At the default of
        // 15 the smoothing is imperceptible, so Khoa's original feel is unchanged unless the player
        // deliberately turns it down.
        if (lookAcceleration < 29.5f && dt > 0f)
        {
            float catchUp = 1f - Mathf.Exp(-lookAcceleration * dt);
            _smoothedLook = Vector2.Lerp(_smoothedLook, new Vector2(yawDelta, pitchDelta), catchUp);
            yawDelta = _smoothedLook.x;
            pitchDelta = _smoothedLook.y;
        }
        else
        {
            _smoothedLook = new Vector2(yawDelta, pitchDelta);
        }

        _yaw += yawDelta;
        float newPitch = Mathf.Clamp(_pitch + pitchDelta, pitchMin, pitchMax);
        // Report the pitch actually applied, so the camera lag does not fight the clamp.
        _lookDeltaDegrees = new Vector2(yawDelta, newPitch - _pitch);
        _pitch = newPitch;

        transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    void UpdateGroundState(float dt)
    {
        IsGrounded = _cc.isGrounded;

        if (IsGrounded) _coyoteTimer = coyoteTime;
        else _coyoteTimer -= dt;

        _jumpBufferTimer -= dt;

        if (!IsGrounded && _velocity.y < 0f) _fallSpeed = -_velocity.y;

        if (IsGrounded && !_wasGrounded)
        {
            OnLand?.Invoke(_fallSpeed);
            _fallSpeed = 0f;
            // Reset the stride so the first step after landing is a full one.
            _stepAccumulator = 0f;
        }
        _wasGrounded = IsGrounded;
    }

    void UpdateMovement(float dt)
    {
        // In VR, "forward" is where the head looks; on desktop it is the body.
        Transform reference = vrMode && moveReference != null ? moveReference : transform;
        Vector3 wishDir = reference.right * _moveInput.x + reference.forward * _moveInput.y;
        wishDir.y = 0f;
        float inputMagnitude = Mathf.Clamp01(wishDir.magnitude);
        if (inputMagnitude > 0.001f) wishDir /= inputMagnitude;
        else wishDir = Vector3.zero;

        bool wantsSprint = _sprintToggleState && (!sprintRequiresForward || _moveInput.y > 0.5f);
        IsSprinting = wantsSprint && inputMagnitude > 0.1f && IsGrounded;

        float baseSpeed = wantsSprint ? sprintSpeed : walkSpeed;
        // Directional penalties: backing up and strafing are slower, like a real body.
        float directional = 1f;
        if (_moveInput.y < -0.01f) directional = Mathf.Lerp(1f, backwardMultiplier, -_moveInput.y);
        else if (Mathf.Abs(_moveInput.x) > Mathf.Abs(_moveInput.y)) directional = Mathf.Lerp(1f, strafeMultiplier, Mathf.Abs(_moveInput.x));

        Vector3 targetVelocity = wishDir * (baseSpeed * directional * inputMagnitude);

        Vector3 horizontal = new Vector3(_velocity.x, 0f, _velocity.z);
        float accel;
        if (IsGrounded)
            accel = targetVelocity.sqrMagnitude >= horizontal.sqrMagnitude ? groundAcceleration : groundDeceleration;
        else
            accel = airAcceleration;

        horizontal = Vector3.MoveTowards(horizontal, targetVelocity, accel * dt);
        _velocity.x = horizontal.x;
        _velocity.z = horizontal.z;

        // Jump: fires from coyote time as well as from true ground contact.
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            _velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            IsGrounded = false;
            _wasGrounded = false;
            OnJumped?.Invoke();
        }
        else if (IsGrounded && _velocity.y < 0f)
        {
            // Small constant push keeps isGrounded stable instead of flickering.
            _velocity.y = -2f;
        }
        else
        {
            float g = _velocity.y < 0f ? gravity * fallGravityMultiplier : gravity;
            _velocity.y = Mathf.Max(_velocity.y + g * dt, terminalVelocity);
        }
    }

    void ApplyMotion(float dt)
    {
        bool groundedBeforeMove = IsGrounded;
        _cc.Move(_velocity * dt);

        if (snapToGround && groundedBeforeMove && !_cc.isGrounded && _velocity.y <= 0f)
            TrySnapToGround();
    }

    /// <summary>
    /// When walking down a slope the controller would otherwise leave the surface for a
    /// frame or two, which reads as floating. Pull it back down onto walkable ground.
    /// </summary>
    void TrySnapToGround()
    {
        // Cast from the centre of the capsule's lower hemisphere: from there a sphere of the
        // capsule radius touching the ground means the capsule is exactly standing on it,
        // so the hit distance is the drop we need.
        float radius = Mathf.Max(0.01f, _cc.radius - 0.01f);
        float halfHeight = Mathf.Max(_cc.height * 0.5f, _cc.radius);
        Vector3 origin = transform.TransformPoint(_cc.center) + Vector3.down * (halfHeight - _cc.radius);
        float maxDistance = _cc.stepOffset + 0.15f;

        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, _groundHits,
            maxDistance, groundMask, QueryTriggerInteraction.Ignore);

        float drop = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _groundHits[i];
            if (hit.collider == null || hit.collider == _cc) continue;
            if (hit.distance <= 0f) continue;                      // started overlapping geometry
            if (Vector3.Angle(hit.normal, Vector3.up) > _cc.slopeLimit) continue;
            if (hit.distance < drop) drop = hit.distance;
        }

        if (drop >= float.MaxValue || drop > maxDistance) return;

        _cc.Move(Vector3.down * drop);
        IsGrounded = _cc.isGrounded;
        if (IsGrounded) _wasGrounded = true;          // suppress a spurious landing event
    }

    /// <summary>
    /// Advances the stride from the distance the body actually covered this frame.
    /// Walking into a wall covers no distance, so it produces no footsteps.
    /// </summary>
    void UpdateStepCycle()
    {
        Vector3 delta = transform.position - _lastPosition;
        _lastPosition = transform.position;
        delta.y = 0f;

        float distance = delta.magnitude;
        HorizontalSpeed = distance / Mathf.Max(Time.deltaTime, 1e-5f);

        if (!IsGrounded || HorizontalSpeed < minStepSpeed)
        {
            // Settle onto the nearest footfall so the bob returns to a neutral pose
            // instead of freezing halfway through a stride.
            float settled = Mathf.Round(_stepPhase / Mathf.PI) * Mathf.PI;
            _stepPhase = Mathf.Lerp(_stepPhase, settled, 1f - Mathf.Exp(-8f * Time.deltaTime));
            return;
        }

        float stride = Mathf.Lerp(walkStrideLength, sprintStrideLength,
            Mathf.InverseLerp(walkSpeed, sprintSpeed, HorizontalSpeed));
        stride = Mathf.Max(0.2f, stride);

        // One footfall per PI radians of phase.
        _stepPhase += (distance / stride) * Mathf.PI;
        if (_stepPhase > Mathf.PI * 2f) _stepPhase -= Mathf.PI * 2f;

        _stepAccumulator += distance;
        while (_stepAccumulator >= stride)
        {
            _stepAccumulator -= stride;
            float intensity = Mathf.Clamp01(Mathf.InverseLerp(minStepSpeed, sprintSpeed, HorizontalSpeed));
            OnFootstep?.Invoke(_nextFoot, intensity);
            _nextFoot = _nextFoot == Foot.Left ? Foot.Right : Foot.Left;
        }
    }

    static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    /// <summary>Teleports the body without the controller fighting the move.</summary>
    public void Teleport(Vector3 position, float yawDegrees)
    {
        _cc.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
        _cc.enabled = true;
        _yaw = yawDegrees;
        _velocity = Vector3.zero;
        _lastPosition = position;
    }
}
