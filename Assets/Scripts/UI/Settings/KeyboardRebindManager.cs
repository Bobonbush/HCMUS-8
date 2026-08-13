using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>
    /// Controls tab: owns the grid's action asset reference, the row list, and the status line.
    ///
    /// Persistence, conflict detection and override propagation all live in
    /// <see cref="InputBindingService"/>; this component only forwards to it. It deliberately does
    /// NOT touch PlayerPrefs itself — in the 2D build it wrote the same "set_key_overrides" key as
    /// the service, so whichever saved last won.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class KeyboardRebindManager : MonoBehaviour
    {
        [Tooltip("Assets/InputSystem_Actions.inputactions — the same asset the player uses.")]
        [SerializeField] private InputActionAsset inputActions;
        [Tooltip("Leave empty to pick up every RebindEntry under this object automatically.")]
        [SerializeField] private RebindEntry[] entries;

        [Header("Feedback")]
        [Tooltip("Status line: the press-a-key prompt and rejection messages.")]
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private float messageDuration = 2f;

        public InputActionAsset Actions => inputActions;

        private Coroutine messageRoutine;

        private void Awake()
        {
            if (entries == null || entries.Length == 0)
                entries = GetComponentsInChildren<RebindEntry>(true);

            // Hands the grid's asset to the service if nothing has claimed one yet, and pulls the
            // saved overrides back onto it.
            InputBindingService.EnsureExists(inputActions);
        }

        private void OnEnable()
        {
            InputBindingService service = InputBindingService.Instance;
            if (service != null) service.OnBindingsChanged += RefreshAllLabels;
            Localization.OnLanguageChanged += RefreshAllLabels;

            RefreshAllLabels();
            ShowMessage(string.Empty, false);
        }

        private void OnDisable()
        {
            InputBindingService service = InputBindingService.Instance;
            if (service != null) service.OnBindingsChanged -= RefreshAllLabels;
            Localization.OnLanguageChanged -= RefreshAllLabels;
        }

        public void SaveOverrides()
        {
            InputBindingService.Instance?.SaveOverrides();
            ShowMessage(string.Empty, false);
        }

        /// <summary>Footer "Reset Defaults": clear every override and persist the cleared state.</summary>
        public void ResetToDefaults()
        {
            InputBindingService.Instance?.ResetToDefaults();
            RefreshAllLabels();
            ShowMessage(Localization.Get("rebind.restored"), true);
        }

        public void RefreshAllLabels()
        {
            if (entries == null) return;
            foreach (RebindEntry entry in entries)
                if (entry != null) entry.Refresh();
        }

        /// <summary>
        /// Called by a row the moment it starts waiting for a key. Without this the only sign that
        /// the menu is armed is a row showing "...", which players read as the menu being broken.
        /// The prompt stays up until the capture ends, so it is not on the timed path.
        /// </summary>
        public void ReportListening(BindingDevice device)
        {
            ShowMessage(
                Localization.Get(device == BindingDevice.Gamepad
                    ? "rebind.prompt_gamepad"
                    : "rebind.prompt"),
                false);
        }

        /// <summary>Called by a row when the service refused the captured key.</summary>
        public void ReportRejection(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                ShowMessage(string.Empty, false);
                return;
            }

            ShowMessage(message, true);
        }

        private void ShowMessage(string message, bool autoClear)
        {
            if (messageLabel == null) return;

            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
                messageRoutine = null;
            }

            messageLabel.text = message;
            if (autoClear && !string.IsNullOrEmpty(message) && isActiveAndEnabled)
                messageRoutine = StartCoroutine(ClearMessageAfterDelay());
        }

        private IEnumerator ClearMessageAfterDelay()
        {
            // Unscaled: the options screen is usually open with the game paused.
            yield return new WaitForSecondsRealtime(messageDuration);
            if (messageLabel != null) messageLabel.text = string.Empty;
            messageRoutine = null;
        }
    }
}
