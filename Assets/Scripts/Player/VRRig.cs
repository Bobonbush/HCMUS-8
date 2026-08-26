using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Turns the desktop first person player into a VR player when a headset (real or the
/// XR Device Simulator) is present. Dormant otherwise, so the desktop game is unaffected.
///
/// On activation:
///  - FirstPersonCameraFeel is disabled (head bob and camera sway are motion-sickness
///    poison in VR - the player's real head does the bobbing).
///  - The camera is re-parented from the pitch pivot to the player root and driven by a
///    TrackedPoseDriver, so the HMD provides the full head pose including eye height.
///  - The motor switches to vrMode: look input ignored, movement follows the head.
///  - VR controller bindings are added to the shared input actions AT RUNTIME only, so
///    they never pollute the rebinding UI in desktop sessions:
///      left thumbstick = move, left thumbstick click = sprint,
///      right controller primary button = jump, right thumbstick = snap turn.
/// </summary>
[AddComponentMenu("Player/VR Rig")]
[DisallowMultipleComponent]
public class VRRig : MonoBehaviour
{
    public enum Mode { Auto, ForceOn, ForceOff }

    [Header("References")]
    public FirstPersonController controller;
    public FirstPersonCameraFeel cameraFeel;
    [Tooltip("The player camera (normally under the camera pivot).")]
    public Camera playerCamera;

    [Header("Activation")]
    [Tooltip("Auto activates when an XR headset device (real or simulated) appears.")]
    public Mode mode = Mode.Auto;

    [Header("Comfort")]
    [Tooltip("Degrees per snap turn on the right thumbstick.")]
    public float snapTurnDegrees = 45f;
    [Tooltip("Stick deflection that triggers a snap turn.")]
    [Range(0.3f, 0.95f)] public float snapTurnThreshold = 0.7f;

    /// <summary>True once the VR rig has taken over the player.</summary>
    public bool VRActive { get; private set; }

    private InputAction _snapTurnAction;
    private TrackedPoseDriver _poseDriver;
    private Transform _trackingOffset;
    private CharacterController _cc;
    private bool _snapArmed = true;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<FirstPersonController>();
        if (cameraFeel == null) cameraFeel = GetComponentInChildren<FirstPersonCameraFeel>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (!VRActive)
        {
            if (mode == Mode.ForceOff) return;
            if (mode == Mode.ForceOn || HeadsetPresent()) Activate();
            return;
        }

        // ---- snap turn ----
        float x = _snapTurnAction != null ? _snapTurnAction.ReadValue<Vector2>().x : 0f;
        if (_snapArmed && Mathf.Abs(x) >= snapTurnThreshold)
        {
            controller.AddYaw(Mathf.Sign(x) * snapTurnDegrees);
            _snapArmed = false;
        }
        else if (Mathf.Abs(x) < 0.3f)
        {
            _snapArmed = true;
        }

        BodyFollowsHead();
    }

    /// <summary>
    /// Room-scale correctness: when the tracked head moves away from the body (physically
    /// walking, or steering the simulator's HMD), the body capsule chases it WITH collisions,
    /// and the tracking space shifts back by however far the body actually moved so the head
    /// stays where the player put it. Anomoly triggers, lifts and walls all key off the body,
    /// so without this a simulator head could ghost through the whole floor.
    /// </summary>
    private void BodyFollowsHead()
    {
        if (_cc == null || _trackingOffset == null || playerCamera == null) return;

        Vector3 delta = playerCamera.transform.position - controller.transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.0004f) return;   // within 2cm - close enough

        Vector3 before = controller.transform.position;
        _cc.Move(delta);
        Vector3 moved = controller.transform.position - before;
        moved.y = 0f;
        // pull the tracking space back so the camera does not get dragged along
        _trackingOffset.position -= moved;
    }

    private static bool HeadsetPresent()
    {
        foreach (InputDevice device in InputSystem.devices)
        {
            if (device is UnityEngine.InputSystem.XR.XRHMD) return true;
        }
        return false;
    }

    private void Activate()
    {
        if (VRActive || controller == null || playerCamera == null) return;
        VRActive = true;

        // 1. Camera feel off - the real head does all of this in VR.
        if (cameraFeel != null) cameraFeel.enabled = false;

        // 2. Head-tracked camera under a tracking-offset node on the player root (the root
        //    is the XR origin: body yaw rotates it, the tracked pose supplies eye height,
        //    and the offset node lets BodyFollowsHead recentre the space over the body).
        _cc = controller.GetComponent<CharacterController>();
        GameObject offsetGo = new GameObject("VRTrackingOffset");
        _trackingOffset = offsetGo.transform;
        _trackingOffset.SetParent(controller.transform, false);
        _trackingOffset.localPosition = Vector3.zero;
        _trackingOffset.localRotation = Quaternion.identity;

        Transform camT = playerCamera.transform;
        camT.SetParent(_trackingOffset, false);
        camT.localPosition = Vector3.zero;
        camT.localRotation = Quaternion.identity;

        _poseDriver = camT.gameObject.GetComponent<TrackedPoseDriver>();
        if (_poseDriver == null) _poseDriver = camT.gameObject.AddComponent<TrackedPoseDriver>();
        var posAction = new InputAction(binding: "<XRHMD>/centerEyePosition");
        posAction.AddBinding("<HandheldARInputDevice>/devicePosition");
        var rotAction = new InputAction(binding: "<XRHMD>/centerEyeRotation");
        rotAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
        _poseDriver.positionInput = new InputActionProperty(posAction);
        _poseDriver.rotationInput = new InputActionProperty(rotAction);
        _poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        _poseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

        // 3. Motor into VR mode.
        controller.vrMode = true;
        controller.moveReference = camT;

        // 4. Runtime-only controller bindings on the shared actions. The keyboard bindings
        //    of these actions are muted for the session: the XR Device Simulator uses WASD,
        //    LeftShift and Space itself, so leaving them live makes the player run and jump
        //    while you are just steering the simulator.
        if (controller.inputActions != null)
        {
            InputAction move = controller.inputActions.FindAction("Player/Move", false);
            InputAction sprint = controller.inputActions.FindAction("Player/Sprint", false);
            InputAction jump = controller.inputActions.FindAction("Player/Jump", false);
            if (move != null) move.AddBinding("<XRController>{LeftHand}/thumbstick");
            if (sprint != null) sprint.AddBinding("<XRController>{LeftHand}/thumbstickClicked");
            if (jump != null) jump.AddBinding("<XRController>{RightHand}/primaryButton");
            MuteKeyboardBindings(move);
            MuteKeyboardBindings(sprint);
            MuteKeyboardBindings(jump);
        }

        _snapTurnAction = new InputAction(binding: "<XRController>{RightHand}/thumbstick");
        _snapTurnAction.Enable();

        Debug.Log("VRRig: headset detected - VR mode active.");
    }

    /// <summary>
    /// Session-only overrides (never saved to the asset): empties every keyboard/mouse
    /// binding path so only gamepad and XR controller bindings stay live in VR.
    /// </summary>
    private static void MuteKeyboardBindings(InputAction action)
    {
        if (action == null) return;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            string path = action.bindings[i].path;
            if (!string.IsNullOrEmpty(path) &&
                (path.StartsWith("<Keyboard>") || path.StartsWith("<Mouse>")))
            {
                action.ApplyBindingOverride(i, string.Empty);
            }
        }
    }

    private void OnDestroy()
    {
        _snapTurnAction?.Dispose();
    }
}
