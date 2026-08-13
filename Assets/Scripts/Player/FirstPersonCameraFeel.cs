using UnityEngine;

/// <summary>
/// Everything that makes the view feel like it is attached to a body instead of a tripod.
/// Lives on the Camera, which is a child of the controller's camera pivot: the pivot owns
/// pitch, the player owns yaw, and this script only ever writes the camera's *local*
/// position and rotation, so nothing fights over the same value.
///
/// Layers, from slowest to fastest:
///   breathing      - always on, deeper and faster the more you have been exerting yourself
///   postural drift - slow Perlin wander, so the view is never perfectly still
///   head bob       - driven by the controller's distance-based step phase
///   step impulses  - a damped spring kicked on every footfall; this is the "weight"
///   landing        - a much larger version of the same spring, scaled by impact speed
///   look lag       - the view trails fast turns slightly and leans into them
/// </summary>
[AddComponentMenu("Player/First Person Camera Feel")]
[DisallowMultipleComponent]
public class FirstPersonCameraFeel : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController controller;
    [Tooltip("Left empty, the Camera on this GameObject is used (for the FOV effects).")]
    public Camera targetCamera;

    [Header("Breathing")]
    [Tooltip("Breaths per second while rested. 0.22 is roughly 13 breaths a minute.")]
    public float breathRateRested = 0.22f;
    [Tooltip("Breaths per second when fully out of breath.")]
    public float breathRateExhausted = 0.62f;
    [Tooltip("Vertical travel of a rested breath, in metres.")]
    public float breathAmplitude = 0.0075f;
    [Tooltip("Breathing gets this many times deeper when fully out of breath.")]
    public float breathExhaustionDepth = 2.6f;
    [Tooltip("Degrees of pitch added by a full breath.")]
    public float breathPitch = 0.22f;
    [Tooltip("Breathing is masked by the head bob while moving, so it is scaled down.")]
    [Range(0f, 1f)] public float breathWhileMoving = 0.35f;

    [Header("Exertion")]
    [Tooltip("Seconds of continuous sprinting to become fully out of breath.")]
    public float exertionBuildTime = 9f;
    [Tooltip("Seconds of rest to fully recover.")]
    public float exertionRecoverTime = 14f;

    [Header("Postural Drift")]
    [Tooltip("Degrees of slow involuntary wander. This is what stops the view feeling locked off.")]
    public float driftAmount = 0.32f;
    public float driftSpeed = 0.13f;
    [Tooltip("Drift is scaled by this while moving.")]
    [Range(0f, 1f)] public float driftWhileMoving = 0.4f;

    [Header("Head Bob")]
    [Tooltip("Vertical travel at full sprint, in metres. The head dips on each footfall.")]
    public float bobVertical = 0.055f;
    [Tooltip("Sideways travel over a full stride, in metres.")]
    public float bobHorizontal = 0.042f;
    [Tooltip("Degrees of roll over a full stride.")]
    public float bobRoll = 0.75f;
    [Tooltip("Degrees of pitch over a full stride.")]
    public float bobPitch = 0.45f;
    [Tooltip("Bob amplitude at walking speed relative to sprinting.")]
    [Range(0f, 1f)] public float bobWalkScale = 0.62f;
    [Tooltip("How quickly the bob fades in when you start moving and out when you stop.")]
    public float bobBlendSpeed = 7f;

    [Header("Footfall Impact")]
    [Tooltip("Downward velocity kick given to the camera spring on each footfall.")]
    public float stepImpulse = 0.6f;
    [Tooltip("Extra kick at full sprint, on top of the base impulse.")]
    public float stepImpulseSprintBonus = 0.65f;
    [Tooltip("Degrees per second of pitch kick on each footfall.")]
    public float stepPitchImpulse = 9f;
    [Tooltip("Degrees per second of roll kick, alternating with the foot.")]
    public float stepRollImpulse = 5f;

    [Header("Landing")]
    [Tooltip("Impact speed in m/s that produces a full-strength landing.")]
    public float landFullImpactSpeed = 12f;
    public float landImpulse = 2.4f;
    public float landPitchImpulse = 55f;
    [Tooltip("Downward kick given when the jump leaves the ground.")]
    public float jumpImpulse = 0.35f;

    [Header("Spring")]
    [Tooltip("Higher is stiffer and snappier.")]
    public float springStiffness = 190f;
    [Tooltip("Higher settles faster with less overshoot.")]
    public float springDamping = 17f;
    [Tooltip("Clamps total vertical camera travel so extreme values cannot clip through the floor.")]
    public float maxVerticalOffset = 0.22f;

    [Header("Look Lag")]
    [Tooltip("Fraction of a fast turn the view trails behind by.")]
    [Range(0f, 1f)] public float lookLag = 0.35f;
    public float lookLagRecovery = 11f;
    public float maxLookLagDegrees = 2.5f;
    [Tooltip("Degrees of roll leaned into a turn, per degree per second of yaw.")]
    public float turnRoll = 0.012f;
    public float maxTurnRoll = 1.6f;

    [Header("Strafe Roll")]
    [Tooltip("Degrees of roll when strafing at full speed.")]
    public float strafeRoll = 0.9f;
    public float strafeRollSpeed = 6f;

    [Header("Player Setting")]
    [Tooltip("Master multiplier for head bob and every footstep/land/jump kick. Driven by " +
             "Settings > Game > Camera Shake; 0 gives a completely still camera.")]
    [Range(0f, 2f)] public float shakeScale = 1f;

    [Header("Field of View")]
    [Tooltip("Leave at 0 to capture the camera's FOV at startup. Settings > Video > FOV writes here.")]
    public float baseFieldOfView = 0f;
    [Tooltip("Degrees added at full sprint.")]
    public float sprintFovBoost = 7f;
    public float fovBlendSpeed = 6f;

    // ---------------------------------------------------------------- private

    Vector3 _restLocalPosition;
    float _breathPhase;
    float _driftSeed;
    float _exertion;          // 0..1
    float _bobWeight;         // 0..1, fades the bob in and out
    float _strafeRoll;
    float _lagYaw, _lagPitch;
    float _turnRoll;
    float _fov;
    float _sprintBlend;

    Spring _springY;
    Spring _springPitch;
    Spring _springRoll;

    struct Spring
    {
        public float Value;
        public float Velocity;

        /// <summary>Substepped so high stiffness stays stable at low frame rates.</summary>
        public void Step(float dt, float stiffness, float damping)
        {
            const float maxStep = 1f / 120f;
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / maxStep), 1, 8);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float accel = -Value * stiffness - Velocity * damping;
                Velocity += accel * h;
                Value += Velocity * h;
            }
        }

        public void Impulse(float velocity) => Velocity += velocity;
    }

    // ---------------------------------------------------------------- lifecycle

    void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (controller == null) controller = GetComponentInParent<FirstPersonController>();

        _restLocalPosition = transform.localPosition;
        _driftSeed = Random.Range(0f, 100f);

        if (targetCamera != null)
        {
            if (baseFieldOfView <= 0f) baseFieldOfView = targetCamera.fieldOfView;
            _fov = baseFieldOfView;
        }
    }

    void OnEnable()
    {
        if (controller == null) return;
        controller.OnFootstep += HandleFootstep;
        controller.OnLand += HandleLand;
        controller.OnJumped += HandleJump;
    }

    void OnDisable()
    {
        if (controller == null) return;
        controller.OnFootstep -= HandleFootstep;
        controller.OnLand -= HandleLand;
        controller.OnJumped -= HandleJump;
    }

    // ---------------------------------------------------------------- events

    void HandleFootstep(FirstPersonController.Foot foot, float intensity)
    {
        intensity *= shakeScale;
        float impulse = (stepImpulse + stepImpulseSprintBonus * intensity) * shakeScale;
        _springY.Impulse(-impulse);
        _springPitch.Impulse(stepPitchImpulse * intensity);
        _springRoll.Impulse((foot == FirstPersonController.Foot.Left ? -1f : 1f) * stepRollImpulse * intensity);
    }

    void HandleLand(float impactSpeed)
    {
        float strength = Mathf.Clamp01(impactSpeed / Mathf.Max(0.01f, landFullImpactSpeed));
        if (strength <= 0.01f) return;
        // Squared so gentle step-downs stay subtle and real falls really land.
        strength *= strength;
        strength *= shakeScale;
        _springY.Impulse(-landImpulse * strength);
        _springPitch.Impulse(landPitchImpulse * strength);
        _springRoll.Impulse(Random.Range(-1f, 1f) * landPitchImpulse * 0.12f * strength);
    }

    void HandleJump()
    {
        _springY.Impulse(-jumpImpulse * shakeScale);
        _springPitch.Impulse(-jumpImpulse * 12f * shakeScale);
    }

    // ---------------------------------------------------------------- update

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || controller == null) return;

        float speedRatio = controller.SpeedRatio;
        bool moving = controller.IsMoving && controller.IsGrounded;

        UpdateExertion(dt);

        // ---- breathing -------------------------------------------------
        float breathRate = Mathf.Lerp(breathRateRested, breathRateExhausted, _exertion);
        _breathPhase += dt * breathRate * Mathf.PI * 2f;
        if (_breathPhase > Mathf.PI * 2f) _breathPhase -= Mathf.PI * 2f;

        // Real breathing is asymmetric: a quicker inhale than exhale. Skewing the sine
        // keeps it from reading as a mechanical oscillation.
        float breath = Mathf.Sin(_breathPhase - 0.45f * Mathf.Sin(_breathPhase));
        float breathScale = Mathf.Lerp(1f, breathWhileMoving, _bobWeight)
                            * Mathf.Lerp(1f, breathExhaustionDepth, _exertion);
        float breathY = breath * breathAmplitude * breathScale;
        float breathPitchDeg = breath * breathPitch * breathScale;

        // ---- postural drift --------------------------------------------
        float driftTime = Time.time * driftSpeed + _driftSeed;
        float driftScale = driftAmount * Mathf.Lerp(1f, driftWhileMoving, _bobWeight)
                           * Mathf.Lerp(1f, 1.8f, _exertion);
        float driftYaw = (Mathf.PerlinNoise(driftTime, 0f) - 0.5f) * 2f * driftScale;
        float driftPitch = (Mathf.PerlinNoise(0f, driftTime * 0.87f + 11.3f) - 0.5f) * 2f * driftScale;
        float driftRoll = (Mathf.PerlinNoise(driftTime * 0.61f + 27.1f, 5f) - 0.5f) * 2f * driftScale * 0.7f;

        // ---- head bob ---------------------------------------------------
        _bobWeight = Mathf.Lerp(_bobWeight, moving ? 1f : 0f, 1f - Mathf.Exp(-bobBlendSpeed * dt));

        float phase = controller.StepPhase;
        // shakeScale is the player-facing "Camera Shake" slider: 0 removes the gait entirely,
        // 1 is the tuned default.
        float amplitude = _bobWeight * Mathf.Lerp(bobWalkScale, 1f, speedRatio) * shakeScale;

        // Vertical dips once per footfall (phase advances PI per step), the sideways sway
        // and roll take a whole stride, which is how a real gait works.
        float bobY = -Mathf.Cos(phase) * bobVertical * amplitude;
        float bobX = Mathf.Sin(phase * 0.5f) * bobHorizontal * amplitude;
        float bobRollDeg = -Mathf.Sin(phase * 0.5f) * bobRoll * amplitude;
        float bobPitchDeg = Mathf.Sin(phase) * bobPitch * amplitude;

        // ---- springs ----------------------------------------------------
        _springY.Step(dt, springStiffness, springDamping);
        _springPitch.Step(dt, springStiffness * 0.8f, springDamping * 0.9f);
        _springRoll.Step(dt, springStiffness * 0.7f, springDamping * 0.9f);

        // ---- look lag and turn lean -------------------------------------
        Vector2 look = controller.LookDeltaDegrees;
        _lagYaw = Mathf.Clamp(_lagYaw - look.x * lookLag, -maxLookLagDegrees, maxLookLagDegrees);
        _lagPitch = Mathf.Clamp(_lagPitch - look.y * lookLag, -maxLookLagDegrees, maxLookLagDegrees);
        float recovery = 1f - Mathf.Exp(-lookLagRecovery * dt);
        _lagYaw = Mathf.Lerp(_lagYaw, 0f, recovery);
        _lagPitch = Mathf.Lerp(_lagPitch, 0f, recovery);

        float yawRate = look.x / dt;
        float targetTurnRoll = Mathf.Clamp(-yawRate * turnRoll, -maxTurnRoll, maxTurnRoll);
        _turnRoll = Mathf.Lerp(_turnRoll, targetTurnRoll, 1f - Mathf.Exp(-8f * dt));

        // ---- strafe roll -------------------------------------------------
        float targetStrafeRoll = -controller.MoveInput.x * strafeRoll * _bobWeight;
        _strafeRoll = Mathf.Lerp(_strafeRoll, targetStrafeRoll, 1f - Mathf.Exp(-strafeRollSpeed * dt));

        // ---- compose ------------------------------------------------------
        float offsetY = Mathf.Clamp(bobY + breathY + _springY.Value, -maxVerticalOffset, maxVerticalOffset);
        transform.localPosition = _restLocalPosition + new Vector3(bobX, offsetY, 0f);

        float pitchDeg = bobPitchDeg + breathPitchDeg + driftPitch + _springPitch.Value + _lagPitch;
        float yawDeg = driftYaw + _lagYaw;
        float rollDeg = bobRollDeg + driftRoll + _springRoll.Value + _strafeRoll + _turnRoll;
        transform.localRotation = Quaternion.Euler(pitchDeg, yawDeg, rollDeg);

        // ---- field of view -------------------------------------------------
        if (targetCamera != null)
        {
            float sprintTarget = controller.IsSprinting ? Mathf.Clamp01(speedRatio) : 0f;
            _sprintBlend = Mathf.Lerp(_sprintBlend, sprintTarget, 1f - Mathf.Exp(-fovBlendSpeed * dt));
            float targetFov = baseFieldOfView + sprintFovBoost * _sprintBlend;
            _fov = Mathf.Lerp(_fov, targetFov, 1f - Mathf.Exp(-fovBlendSpeed * dt));
            targetCamera.fieldOfView = _fov;
        }
    }

    void UpdateExertion(float dt)
    {
        if (controller.IsSprinting)
            _exertion += dt / Mathf.Max(0.01f, exertionBuildTime);
        else
            _exertion -= dt / Mathf.Max(0.01f, exertionRecoverTime);
        _exertion = Mathf.Clamp01(_exertion);
    }

    /// <summary>0..1 how out of breath the player is. Useful for stamina UI or audio.</summary>
    public float Exertion => _exertion;
}
