using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>Every rebindable slot shown in the Keyboard tab.</summary>
    public enum MenuBindingId
    {
        MoveForward, MoveBack, MoveLeft, MoveRight,
        MoveForwardAlt, MoveBackAlt, MoveLeftAlt, MoveRightAlt,
        Jump, Sprint, Interact
    }

    /// <summary>Which physical device family a rebindable slot targets.</summary>
    public enum BindingDevice
    {
        Keyboard, Mouse, Gamepad
    }

    /// <summary>
    /// Persists binding overrides on the InputActionAsset the gameplay code uses, and mirrors them
    /// onto every other copy of that asset in play (live PlayerInput clones and anything registered
    /// through <see cref="RegisterMirror"/>).
    ///
    /// RULE FOR THE WHOLE PROJECT — this is the fix for the "one key, two jobs" bug in the 2D build:
    /// every key the player can press must exist exactly ONCE, in InputSystem_Actions.inputactions,
    /// and must be reachable from the table below. No script may read a key directly
    /// (Keyboard.current.xxxKey / new InputAction("...")) — the only place allowed to do that is the
    /// rebind capture in this file. If a key is hardcoded somewhere else, rebinding it in Settings
    /// silently does nothing and the old key keeps working.
    ///
    /// That is also why the arrow keys get their own "…Alt" rows: in the asset each Move direction
    /// carries two keyboard bindings (W and Up arrow). Hiding the second one is what made keys look
    /// like they had a life of their own, so both are listed and both are rebindable.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public sealed class InputBindingService : MonoBehaviour
    {
        /// <summary>One rebindable row: which asset action, composite part and device slot it maps to.</summary>
        public sealed class BindingDefinition
        {
            public readonly MenuBindingId id;
            public readonly string displayName;
            public readonly string action;
            public readonly string part;
            /// <summary>Which binding of this action on that device: 0 = first, 1 = the alternate.</summary>
            public readonly int slot;
            public readonly BindingDevice device;
            public readonly bool gamepadRebindable;

            public BindingDefinition(
                MenuBindingId id,
                string displayName,
                string action,
                string part,
                int slot,
                BindingDevice device,
                bool gamepadRebindable)
            {
                this.id = id;
                this.displayName = displayName;
                this.action = action;
                this.part = part;
                this.slot = slot;
                this.device = device;
                this.gamepadRebindable = gamepadRebindable;
            }
        }

        // Mirrors the Player action map of Assets/InputSystem_Actions.inputactions — but only the
        // actions something actually reads.
        //
        // The asset came from Unity's default template and still carries Attack, Crouch, Previous and
        // Next, which no script in this game listens to. This is a walking-sim like Exit 8: you move,
        // you look, you open doors. Listing a key that does nothing is its own kind of lie, so those
        // rows are deliberately absent. If Crouch or anything else gets implemented later, add the row
        // back here in the same breath as the gameplay code.
        //
        // Move is a Dpad composite whose parts are named up/down/left/right; on a gamepad it is a
        // whole stick, so the directional rows have nothing to rebind there.
        private static readonly BindingDefinition[] definitions =
        {
            new BindingDefinition(MenuBindingId.MoveForward, "Forward", "Move", "up", 0, BindingDevice.Keyboard, false),
            new BindingDefinition(MenuBindingId.MoveBack, "Back", "Move", "down", 0, BindingDevice.Keyboard, false),
            new BindingDefinition(MenuBindingId.MoveLeft, "Left", "Move", "left", 0, BindingDevice.Keyboard, false),
            new BindingDefinition(MenuBindingId.MoveRight, "Right", "Move", "right", 0, BindingDevice.Keyboard, false),
            new BindingDefinition(MenuBindingId.MoveForwardAlt, "Forward (alt)", "Move", "up", 1, BindingDevice.Keyboard, false),
            new BindingDefinition(MenuBindingId.MoveBackAlt, "Back (alt)", "Move", "down", 1, BindingDevice.Keyboard, false),
            new BindingDefinition(MenuBindingId.MoveLeftAlt, "Left (alt)", "Move", "left", 1, BindingDevice.Keyboard, false),
            new BindingDefinition(MenuBindingId.MoveRightAlt, "Right (alt)", "Move", "right", 1, BindingDevice.Keyboard, false),
            new BindingDefinition(MenuBindingId.Jump, "Jump", "Jump", null, 0, BindingDevice.Keyboard, true),
            new BindingDefinition(MenuBindingId.Sprint, "Sprint", "Sprint", null, 0, BindingDevice.Keyboard, true),
            new BindingDefinition(MenuBindingId.Interact, "Interact", "Interact", null, 0, BindingDevice.Keyboard, true)
        };

        public static IReadOnlyList<BindingDefinition> Definitions => definitions;

        public const string StickBindingLabel = "L-Stick / D-Pad";

        /// <summary>
        /// Paths the player may never take, because each one is how a capture is cancelled. Binding
        /// one to an action would make that action uncapturable and would fight the pause menu.
        /// </summary>
        private static readonly string[] reservedPaths =
        {
            "<Keyboard>/escape",
            "<Gamepad>/buttonEast"
        };

        private const string OverridesKey = "set_key_overrides";

        public static InputBindingService Instance { get; private set; }

        /// <summary>Raised whenever bindings are saved, reset, or (re)loaded, so UI can refresh live.</summary>
        public event Action OnBindingsChanged;

        public InputActionAsset actions;
        private readonly List<InputActionAsset> mirrors = new List<InputActionAsset>();
        private InputActionRebindingExtensions.RebindingOperation operation;

        /// <summary>
        /// True while a key capture is waiting for input. The pause menu checks this: Escape both
        /// cancels the capture and toggles pause, and only one of them may act on the press.
        /// </summary>
        public bool IsRebinding => operation != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists(null);
        }

        public static InputBindingService EnsureExists(InputActionAsset inputActions)
        {
            if (Instance == null)
            {
                GameObject serviceObject = new GameObject(nameof(InputBindingService));
                Instance = serviceObject.AddComponent<InputBindingService>();
            }

            Instance.Configure(inputActions);
            return Instance;
        }

        public static BindingDefinition GetDefinition(MenuBindingId id)
        {
            foreach (BindingDefinition definition in definitions)
                if (definition.id == id)
                    return definition;
            return null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            operation?.Dispose();
            operation = null;
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        public void Configure(InputActionAsset inputActions)
        {
            if (inputActions == null) return;
            actions = inputActions;
            LoadSavedOverrides();
        }

        /// <summary>
        /// Registers another copy of the action asset that should receive the same overrides.
        /// Needed because anything that instantiates the asset separately gets baked-in defaults.
        /// </summary>
        public void RegisterMirror(InputActionAsset mirror)
        {
            if (mirror == null || mirror == actions) return;
            if (!mirrors.Contains(mirror)) mirrors.Add(mirror);
            ApplyOverridesToMirrors();
        }

        public void UnregisterMirror(InputActionAsset mirror)
        {
            if (mirror == null) return;
            mirrors.Remove(mirror);
        }

        public string GetDisplayString(MenuBindingId id)
        {
            BindingDefinition definition = GetDefinition(id);
            return definition == null ? "UNBOUND" : GetDisplayString(id, definition.device);
        }

        public string GetDisplayString(MenuBindingId id, BindingDevice device)
        {
            if (device == BindingDevice.Gamepad)
            {
                BindingDefinition definition = GetDefinition(id);
                if (definition != null && !definition.gamepadRebindable) return Localization.Get("rebind.stick");
            }

            InputAction action;
            int index;
            if (!TryResolve(id, device, out action, out index)) return Localization.Get("rebind.unbound");
            return action.GetBindingDisplayString(
                index,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        public void StartInteractiveRebind(MenuBindingId id, BindingDevice device, Action<bool, string> completed)
        {
            CancelRebind();

            BindingDefinition definition = GetDefinition(id);
            if (device == BindingDevice.Gamepad && (definition == null || !definition.gamepadRebindable))
            {
                completed?.Invoke(false, "rebind.not_rebindable");
                return;
            }

            InputAction action;
            int bindingIndex;
            if (!TryResolve(id, device, out action, out bindingIndex))
            {
                completed?.Invoke(false, "rebind.not_found");
                return;
            }

            Rebind(action, bindingIndex, device, id, completed);
        }

        /// <summary>
        /// Rebinds an arbitrary action/binding pair, for rows that address a binding directly by
        /// <see cref="InputActionReference"/> instead of going through the table. Both paths share one
        /// rebind implementation, one conflict rule and one save location.
        /// </summary>
        public void StartInteractiveRebind(
            InputAction action,
            int bindingIndex,
            BindingDevice device,
            Action<bool, string> completed)
        {
            CancelRebind();

            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                completed?.Invoke(false, "rebind.not_found");
                return;
            }

            // The caller may hold a different instance of the same asset. Rebind our own instance
            // instead, otherwise the override would land on an object SaveOverrides never reads.
            InputActionAsset sourceAsset = action.actionMap != null ? action.actionMap.asset : null;
            if (actions != null && sourceAsset != null && sourceAsset != actions)
            {
                InputAction owned = actions.FindAction(action.id);
                if (owned == null || bindingIndex >= owned.bindings.Count)
                {
                    completed?.Invoke(false, "rebind.not_found");
                    return;
                }
                action = owned;
            }

            MenuBindingId? conflictScope = null;
            MenuBindingId matched;
            if (TryGetBindingId(action, bindingIndex, device, out matched)) conflictScope = matched;

            Rebind(action, bindingIndex, device, conflictScope, completed);
        }

        /// <summary>Maps a raw action/binding pair back to its table row, if it has one.</summary>
        public bool TryGetBindingId(InputAction action, int bindingIndex, BindingDevice device, out MenuBindingId id)
        {
            id = default;
            if (action == null) return false;

            foreach (BindingDefinition definition in definitions)
            {
                InputAction candidate;
                int candidateIndex;
                if (!TryResolve(definition.id, device, out candidate, out candidateIndex)) continue;
                if (candidate.id == action.id && candidateIndex == bindingIndex)
                {
                    id = definition.id;
                    return true;
                }
            }
            return false;
        }

        private void Rebind(
            InputAction action,
            int bindingIndex,
            BindingDevice device,
            MenuBindingId? conflictScope,
            Action<bool, string> completed)
        {
            string previousOverride = action.bindings[bindingIndex].overridePath;
            bool wasEnabled = action.enabled;
            action.Disable();

            InputActionRebindingExtensions.RebindingOperation pending =
                action.PerformInteractiveRebinding(bindingIndex);

            switch (device)
            {
                case BindingDevice.Gamepad:
                    pending = pending
                        .WithControlsHavingToMatchPath("<Gamepad>")
                        .WithExpectedControlType("Button");
                    break;
                case BindingDevice.Mouse:
                    // Buttons only: the pointer axes would complete the capture the instant the
                    // player twitches the mouse.
                    pending = pending
                        .WithControlsExcluding("<Mouse>/position")
                        .WithControlsExcluding("<Mouse>/delta")
                        .WithControlsExcluding("<Mouse>/scroll")
                        .WithControlsHavingToMatchPath("<Mouse>")
                        .WithExpectedControlType("Button");
                    break;
                default:
                    pending = pending
                        .WithControlsExcluding("<Mouse>/position")
                        .WithControlsExcluding("<Mouse>/delta")
                        .WithControlsExcluding("<Mouse>/scroll")
                        .WithControlsHavingToMatchPath("<Keyboard>");
                    break;
            }

            // Cancel on the same device being captured. Cancelling only through the keyboard — as
            // this did — left a controller-only player stuck in the prompt with no way out, since a
            // gamepad capture ignores every key they could press.
            string cancelPath = device == BindingDevice.Gamepad
                ? "<Gamepad>/buttonEast"
                : "<Keyboard>/escape";

            operation = pending
                .WithCancelingThrough(cancelPath)
                .OnCancel(op =>
                {
                    FinishOperation(op, action, wasEnabled);
                    completed?.Invoke(false, "rebind.cancelled");
                })
                .OnComplete(op =>
                {
                    string newPath = action.bindings[bindingIndex].effectivePath;

                    string rejection = null;
                    if (IsReserved(newPath)) rejection = "rebind.reserved";
                    else if (conflictScope.HasValue &&
                             HasConflict(conflictScope.Value, device, action, bindingIndex, newPath))
                        rejection = "rebind.in_use";

                    if (rejection != null)
                    {
                        if (string.IsNullOrEmpty(previousOverride))
                            action.RemoveBindingOverride(bindingIndex);
                        else
                            action.ApplyBindingOverride(bindingIndex, previousOverride);
                        FinishOperation(op, action, wasEnabled);
                        completed?.Invoke(false, rejection);
                        return;
                    }

                    FinishOperation(op, action, wasEnabled);
                    ApplyOverridesToMirrors();
                    completed?.Invoke(true, action.GetBindingDisplayString(
                        bindingIndex,
                        InputBinding.DisplayStringOptions.DontIncludeInteractions));
                })
                .Start();
        }

        public void CancelRebind()
        {
            if (operation == null) return;
            operation.Cancel();
        }

        /// <summary>
        /// Escape hatch that works on either device while a capture is running.
        ///
        /// The operation itself only cancels through the device being captured — a keyboard capture
        /// listens for Escape, a gamepad capture for B. That leaves one trap: a player holding only a
        /// gamepad who starts a KEYBOARD capture has no key to press and no way out but killing the
        /// game. This watches both, so whichever controller is in their hands can always back out.
        ///
        /// Reading devices directly is banned everywhere else in the project; the rebind capture in
        /// this file is the documented exception, and this is part of that capture.
        /// </summary>
        private void Update()
        {
            if (operation == null) return;

            bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool padCancel = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

            if (escape || padCancel) CancelRebind();
        }

        public void SaveOverrides()
        {
            if (actions != null)
                PlayerPrefs.SetString(OverridesKey, actions.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
            ApplyOverridesToMirrors();
            OnBindingsChanged?.Invoke();
        }

        public void RevertUnsaved()
        {
            LoadSavedOverrides();
        }

        public void ResetToDefaults()
        {
            CancelRebind();
            if (actions != null)
                foreach (InputActionMap map in actions.actionMaps)
                    map.RemoveAllBindingOverrides();

            PlayerPrefs.DeleteKey(OverridesKey);
            PlayerPrefs.Save();
            ApplyOverridesToMirrors();
            OnBindingsChanged?.Invoke();
        }

        public bool WasPressedThisFrame(MenuBindingId id, BindingDevice device)
        {
            InputAction action;
            int bindingIndex;
            if (!TryResolve(id, device, out action, out bindingIndex)) return false;
            string path = action.bindings[bindingIndex].effectivePath;
            ButtonControl control = InputSystem.FindControl(path) as ButtonControl;
            return control != null && control.wasPressedThisFrame;
        }

        private void LoadSavedOverrides()
        {
            CancelRebind();
            if (actions != null)
            {
                foreach (InputActionMap map in actions.actionMaps)
                    map.RemoveAllBindingOverrides();
                string json = PlayerPrefs.GetString(OverridesKey, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    try { actions.LoadBindingOverridesFromJson(json); }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"InputBindingService: ignored invalid saved overrides. {exception.Message}");
                        PlayerPrefs.DeleteKey(OverridesKey);
                    }
                }
            }

            ApplyOverridesToMirrors();
            OnBindingsChanged?.Invoke();
        }

        private bool TryResolve(MenuBindingId id, BindingDevice device, out InputAction action, out int bindingIndex)
        {
            action = null;
            bindingIndex = -1;

            BindingDefinition definition = GetDefinition(id);
            if (actions == null || definition == null) return false;

            action = actions.FindAction(definition.action, false);
            if (action == null) return false;

            string devicePrefix = DevicePrefix(device);
            bool wantsPart = definition.part != null;
            // The slot only means anything on the row's own device; on a gamepad an action has a
            // single button, so an "(alt)" row would otherwise resolve to nothing.
            int wantedSlot = device == definition.device ? definition.slot : 0;
            int seen = 0;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];

                // Composite headers ("WASD") carry no path of their own.
                if (binding.isComposite) continue;
                if (binding.isPartOfComposite != wantsPart) continue;
                if (wantsPart &&
                    !string.Equals(binding.name, definition.part, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Which device a slot belongs to is decided by the AUTHORED path, never by the
                // overridden one. An override cannot move a binding to another device — the capture
                // is filtered per device — but it can briefly leave the effective path empty, and a
                // row whose position in the list depends on a changing value ends up resolving to
                // nothing. On screen that looked like a row stuck on "UNBOUND" that also refused to
                // be rebound, because the rebind needs the very lookup that was failing.
                string path = binding.path;
                if (string.IsNullOrEmpty(path)) path = binding.effectivePath;
                if (string.IsNullOrEmpty(path) ||
                    !path.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase)) continue;

                if (seen++ != wantedSlot) continue;

                bindingIndex = i;
                return true;
            }

            action = null;
            return false;
        }

        private static string DevicePrefix(BindingDevice device)
        {
            switch (device)
            {
                case BindingDevice.Gamepad: return "<Gamepad>";
                case BindingDevice.Mouse: return "<Mouse>";
                default: return "<Keyboard>";
            }
        }

        private static bool IsReserved(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            foreach (string reserved in reservedPaths)
                if (string.Equals(reserved, path, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>
        /// True when another row on the same device already uses this path. This is what stops two
        /// actions quietly sharing a key — including the arrow-key rows, which is exactly the case
        /// the 2D build got wrong.
        ///
        /// The identity check matters as much as the path check. Two rows can legitimately resolve to
        /// the same physical binding: Attack has a mouse row and a keyboard row, and when a row is
        /// asked to resolve on a device that is not its own it falls back to that device's first
        /// binding. Without comparing action + index, the row being edited finds *itself* through its
        /// sibling row and every rebind is refused with "KEY ALREADY IN USE".
        /// </summary>
        private bool HasConflict(
            MenuBindingId changedId,
            BindingDevice device,
            InputAction changedAction,
            int changedIndex,
            string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            foreach (BindingDefinition definition in definitions)
            {
                if (definition.id == changedId) continue;
                // A keyboard key never conflicts with a mouse or gamepad button.
                InputAction otherAction;
                int otherIndex;
                if (!TryResolve(definition.id, device, out otherAction, out otherIndex)) continue;

                // Same binding seen from another row: not a conflict with itself.
                if (changedAction != null && otherAction.id == changedAction.id && otherIndex == changedIndex)
                    continue;

                if (string.Equals(
                    otherAction.bindings[otherIndex].effectivePath,
                    path,
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void FinishOperation(
            InputActionRebindingExtensions.RebindingOperation completedOperation,
            InputAction action,
            bool reEnable)
        {
            completedOperation.Dispose();
            operation = null;
            if (reEnable) action.Enable();
        }

        private void ApplyOverridesToMirrors()
        {
            if (actions == null) return;
            string json = actions.SaveBindingOverridesAsJson();

            for (int i = mirrors.Count - 1; i >= 0; i--)
            {
                InputActionAsset mirror = mirrors[i];
                if (mirror == null)
                {
                    mirrors.RemoveAt(i);
                    continue;
                }
                ApplyJson(mirror, json, mirror.name);
            }

            // PlayerInput clones the asset when more than one player exists, so live clones still
            // need the sweep even though the registered mirrors cover everything else.
            foreach (PlayerInput playerInput in FindObjectsByType<PlayerInput>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
            {
                if (playerInput.actions == null || playerInput.actions == actions) continue;
                if (mirrors.Contains(playerInput.actions)) continue;
                ApplyJson(playerInput.actions, json, playerInput.name);
            }
        }

        private static void ApplyJson(InputActionAsset target, string json, string label)
        {
            try { target.LoadBindingOverridesFromJson(json); }
            catch (Exception exception)
            {
                Debug.LogWarning($"InputBindingService: could not update '{label}'. {exception.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (actions == null)
            {
                PlayerInput playerInput = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
                if (playerInput != null && playerInput.actions != null)
                {
                    actions = playerInput.actions;
                    LoadSavedOverrides();
                    return;
                }
            }
            ApplyOverridesToMirrors();
        }
    }
}
