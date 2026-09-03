using System.Collections.Generic;
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
///  - The camera is re-parented from the pitch pivot to a tracking-offset node on the
///    player root and driven by a TrackedPoseDriver (the HMD provides the full head
///    pose including eye height).
///  - The motor switches to vrMode: look input ignored, movement follows the head, and
///    the body capsule chases the tracked head with collisions (room-scale walking).
///  - VR controller bindings are added to the shared input actions AT RUNTIME only:
///      left thumbstick = move, left thumbstick click = sprint,
///      right primary button = jump, right trigger = interact,
///      right thumbstick = snap turn.
///    The keyboard bindings of Move/Sprint/Jump are muted for the session because the
///    XR Device Simulator itself uses WASD/Shift/Space.
///
/// Deactivates cleanly (camera back on the pivot, keyboard unmuted, feel re-enabled)
/// when the headset goes away in Auto mode - e.g. HCMUS > VR > Stop Mock VR.
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
    [Tooltip("Auto activates when an XR headset device (real or simulated) appears, and " +
             "deactivates when it disappears again.")]
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
    private Transform _originalCameraParent;
    private CharacterController _cc;
    private bool _snapArmed = true;
    // action -> binding indices we muted, so deactivation can restore them exactly
    private readonly List<KeyValuePair<InputAction, int>> _mutedBindings = new List<KeyValuePair<InputAction, int>>();
    private readonly List<KeyValuePair<InputAction, int>> _addedBindings = new List<KeyValuePair<InputAction, int>>();

    private void Awake()
    {
        if (controller == null) controller = GetComponent<FirstPersonController>();
        if (cameraFeel == null) cameraFeel = GetComponentInChildren<FirstPersonCameraFeel>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        // Self-heal: a rebind saved during an old VR session may have persisted our
        // "mute" overrides (empty paths) into the keyboard bindings via the settings
        // system. A desktop session must never start with dead movement keys.
        if (!VRActive) RemoveEmptyKeyboardOverrides();
    }

    private void Update()
    {
        if (!VRActive)
        {
            if (mode == Mode.ForceOff) return;
            if (mode == Mode.ForceOn || HeadsetPresent()) Activate();
            return;
        }

        // Auto mode lets go when the headset is gone (Stop Mock VR, simulator disabled).
        if (mode == Mode.Auto && !HeadsetPresent())
        {
            Deactivate();
            return;
        }

        // Frozen game = frozen rig: otherwise snap turn and body-follow keep acting
        // behind the pause menu (CharacterController.Move ignores timescale).
        if (Time.timeScale <= 0f) return;

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

    private static bool HeadsetPresent()
    {
        foreach (InputDevice device in InputSystem.devices)
        {
            if (device is UnityEngine.InputSystem.XR.XRHMD) return true;
        }
        return false;
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
        // The slight downward bias keeps ground contact during the sweep - a purely
        // horizontal Move leaves CharacterController.isGrounded false, which silenced
        // footsteps and let fall gravity build into a phantom landing thud.
        delta.y = -0.05f;
        _cc.Move(delta);
        Vector3 moved = controller.transform.position - before;
        moved.y = 0f;
        // pull the tracking space back so the camera does not get dragged along
        _trackingOffset.position -= moved;
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
        _originalCameraParent = camT.parent;
        camT.SetParent(_trackingOffset, false);
        camT.localPosition = Vector3.zero;
        camT.localRotation = Quaternion.identity;

        _poseDriver = camT.gameObject.GetComponent<TrackedPoseDriver>();
        if (_poseDriver == null) _poseDriver = camT.gameObject.AddComponent<TrackedPoseDriver>();
        var posAction = new InputAction(binding: "<XRHMD>/centerEyePosition");
        var rotAction = new InputAction(binding: "<XRHMD>/centerEyeRotation");
        _poseDriver.positionInput = new InputActionProperty(posAction);
        _poseDriver.rotationInput = new InputActionProperty(rotAction);
        _poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        _poseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

        // 3. Motor into VR mode.
        controller.vrMode = true;
        controller.moveReference = camT;

        // 4. Runtime-only controller bindings on the shared actions, and keyboard mutes
        //    (the XR Device Simulator itself uses WASD, LeftShift and Space).
        if (controller.inputActions != null)
        {
            AddVRBinding("Player/Move", "<XRController>{LeftHand}/thumbstick");
            AddVRBinding("Player/Sprint", "<XRController>{LeftHand}/thumbstickClicked");
            AddVRBinding("Player/Jump", "<XRController>{RightHand}/primaryButton");
            AddVRBinding("Player/Interact", "<XRController>{RightHand}/trigger");
            MuteKeyboardBindings(controller.inputActions.FindAction("Player/Move", false));
            MuteKeyboardBindings(controller.inputActions.FindAction("Player/Sprint", false));
            MuteKeyboardBindings(controller.inputActions.FindAction("Player/Jump", false));
        }

        _snapTurnAction = new InputAction(binding: "<XRController>{RightHand}/thumbstick");
        _snapTurnAction.Enable();

        Debug.Log("VRRig: headset detected - VR mode active.");
    }

    /// <summary>Puts everything back the way the desktop game expects it.</summary>
    private void Deactivate()
    {
        if (!VRActive) return;
        VRActive = false;

        if (_poseDriver != null) Destroy(_poseDriver);
        if (playerCamera != null && _originalCameraParent != null)
        {
            Transform camT = playerCamera.transform;
            camT.SetParent(_originalCameraParent, false);
            camT.localPosition = Vector3.zero;
            camT.localRotation = Quaternion.identity;
        }
        if (_trackingOffset != null) Destroy(_trackingOffset.gameObject);
        _trackingOffset = null;

        controller.vrMode = false;
        controller.moveReference = null;
        if (cameraFeel != null) cameraFeel.enabled = true;

        foreach (var muted in _mutedBindings)
            muted.Key.RemoveBindingOverride(muted.Value);
        _mutedBindings.Clear();
        // Runtime-added XR bindings are erased so a later rebind save cannot persist them.
        for (int i = _addedBindings.Count - 1; i >= 0; i--)
            _addedBindings[i].Key.ChangeBinding(_addedBindings[i].Value).Erase();
        _addedBindings.Clear();

        _snapTurnAction?.Dispose();
        _snapTurnAction = null;

        Debug.Log("VRRig: headset gone - back to desktop mode.");
    }

    private void AddVRBinding(string actionPath, string bindingPath)
    {
        InputAction action = controller.inputActions.FindAction(actionPath, false);
        if (action == null) return;
        action.AddBinding(bindingPath);
        _addedBindings.Add(new KeyValuePair<InputAction, int>(action, action.bindings.Count - 1));
    }

    /// <summary>
    /// Session-only overrides (removed again on deactivation): empties every keyboard and
    /// mouse binding path so only gamepad and XR controller bindings stay live in VR.
    /// </summary>
    private void MuteKeyboardBindings(InputAction action)
    {
        if (action == null) return;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            string path = action.bindings[i].path;
            if (!string.IsNullOrEmpty(path) &&
                (path.StartsWith("<Keyboard>") || path.StartsWith("<Mouse>")))
            {
                action.ApplyBindingOverride(i, string.Empty);
                _mutedBindings.Add(new KeyValuePair<InputAction, int>(action, i));
            }
        }
    }

    /// <summary>Strips empty-path overrides off keyboard bindings of the movement actions.</summary>
    private void RemoveEmptyKeyboardOverrides()
    {
        if (controller == null || controller.inputActions == null) return;
        foreach (string name in new[] { "Player/Move", "Player/Sprint", "Player/Jump" })
        {
            InputAction action = controller.inputActions.FindAction(name, false);
            if (action == null) continue;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].overridePath == string.Empty &&
                    action.bindings[i].path.StartsWith("<Keyboard>"))
                {
                    action.RemoveBindingOverride(i);
                }
            }
        }
    }

    private void OnDestroy()
    {
        _snapTurnAction?.Dispose();
    }
}
