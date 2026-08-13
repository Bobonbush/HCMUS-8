using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// One row of the Controls tab: an action and the two things that trigger it — a key and a
    /// gamepad button — side by side, the way a console options screen lays them out.
    ///
    /// Left/right moves between the two columns, Submit (or a click) captures a new input for the
    /// focused one, and the focused column is the dark one while the other greys back. Which column
    /// starts focused follows <see cref="ActiveInputDevice"/>: a player who just pressed a gamepad
    /// button lands on the gamepad column without having to aim for it.
    ///
    /// A row addresses slots in <see cref="InputBindingService.Definitions"/> rather than an action
    /// asset directly. That is deliberate: the table is the one place that knows every input the
    /// player can use, so a row can never point at a binding the conflict check does not also see.
    /// </summary>
    public class RebindEntry : SettingsRow
    {
        [SerializeField] private KeyboardRebindManager manager;

        [Header("Binding")]
        [SerializeField] private MenuBindingId binding = MenuBindingId.Jump;

        [Header("Keyboard column")]
        [SerializeField] private TMP_Text keyboardLabel;
        [SerializeField] private Button keyboardButton;

        [Header("Gamepad column")]
        [SerializeField] private TMP_Text gamepadLabel;
        [SerializeField] private Button gamepadButton;

        public MenuBindingId Binding => binding;

        private bool listening;
        private BindingDevice listeningDevice;
        /// <summary>0 = keyboard column, 1 = gamepad column.</summary>
        private int column;

        /// <summary>
        /// False for the movement rows: on a gamepad those are one analogue stick, not four buttons,
        /// so there is nothing per-direction to capture. The cell still shows what moves the player,
        /// it just cannot be edited.
        /// </summary>
        private bool GamepadEditable
        {
            get
            {
                InputBindingService.BindingDefinition definition =
                    InputBindingService.GetDefinition(binding);
                return definition == null || definition.gamepadRebindable;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (keyboardButton != null) keyboardButton.onClick.AddListener(() => ActivateColumn(0));

            if (gamepadButton != null)
            {
                gamepadButton.onClick.AddListener(() => ActivateColumn(1));
                // Locked cells stop taking clicks entirely. Letting one accept a click and then do
                // nothing is worse than not reacting: it reads as a broken button.
                gamepadButton.interactable = GamepadEditable;
            }

            ApplyLabel();
        }

        private void OnEnable()
        {
            Localization.OnLanguageChanged += ApplyLabel;
            ApplyLabel();
            Refresh();
        }

        private void OnDisable()
        {
            Localization.OnLanguageChanged -= ApplyLabel;

            // Leaving the tab mid-capture must not leave the prompt armed.
            if (!listening) return;
            InputBindingService.Instance?.CancelRebind();
            listening = false;
        }

        private void ApplyLabel()
        {
            Label = Localization.Get("binding." + binding);
        }

        public override void Refresh()
        {
            InputBindingService service = InputBindingService.Instance;

            if (keyboardLabel != null && !(listening && listeningDevice == BindingDevice.Keyboard))
                keyboardLabel.text = service != null
                    ? service.GetDisplayString(binding, BindingDevice.Keyboard)
                    : Localization.Get("rebind.unbound");

            if (gamepadLabel != null && !(listening && listeningDevice == BindingDevice.Gamepad))
                gamepadLabel.text = service != null
                    ? service.GetDisplayString(binding, BindingDevice.Gamepad)
                    : Localization.Get("rebind.unbound");

            PaintColumns();
        }

        protected override void OnSelectionChanged(bool selected)
        {
            // Land on the column for whatever the player is holding right now.
            if (selected && !listening) column = ActiveInputDevice.UsingGamepad ? 1 : 0;
            PaintColumns();
        }

        protected override void OnColorsApplied(Color foreground)
        {
            PaintColumns();
        }

        /// <summary>Left/right walks between the keyboard and gamepad columns.</summary>
        public override void OnMoveHorizontal(int direction)
        {
            if (direction == 0) return;

            // A capture in progress swallows the input rather than moving focus out from under it.
            if (listening) return;

            int next = Mathf.Clamp(column + (direction > 0 ? 1 : -1), 0, 1);
            // Do not park the cursor on a cell that cannot be edited.
            if (next == 1 && !GamepadEditable) return;
            if (next == column) return;

            column = next;
            PaintColumns();
        }

        /// <summary>Submit: capture a new input for the focused column.</summary>
        public override void Activate() => ActivateColumn(column);

        private void ActivateColumn(int index)
        {
            InputBindingService service = InputBindingService.Instance;
            if (service == null) return;

            // Activating a row that is already waiting cancels it. Ignoring the second press left the
            // player staring at "…" with no idea the menu was still armed, and the next input they
            // used for anything else got captured.
            if (listening)
            {
                service.CancelRebind();
                return;
            }

            index = Mathf.Clamp(index, 0, 1);
            if (index == 1 && !GamepadEditable)
            {
                // Say why rather than flashing "…" for one frame and snapping back.
                manager?.ReportRejection(Localization.Get("rebind.stick_locked"));
                return;
            }

            column = index;
            listeningDevice = column == 1 ? BindingDevice.Gamepad : BindingDevice.Keyboard;
            listening = true;
            PaintColumns();

            TMP_Text label = column == 1 ? gamepadLabel : keyboardLabel;
            if (label != null) label.text = Localization.Get("rebind.waiting");
            manager?.ReportListening(listeningDevice);

            service.StartInteractiveRebind(
                binding,
                listeningDevice,
                (success, messageKey) =>
                {
                    listening = false;
                    Refresh();

                    if (success)
                    {
                        manager?.SaveOverrides();
                        return;
                    }

                    // A cancel is not a complaint: just clear the prompt.
                    manager?.ReportRejection(
                        messageKey == "rebind.cancelled" ? null : Localization.Get(messageKey));
                });
        }

        /// <summary>
        /// The focused column is the dark one. Doing this with colour rather than a box keeps the
        /// table calm — a second highlight rectangle inside an already highlighted row is noise.
        /// </summary>
        private void PaintColumns()
        {
            bool showFocus = IsSelected;

            if (keyboardLabel != null)
                keyboardLabel.color = !showFocus || column == 0 ? UITheme.Ink : UITheme.InkMuted;

            if (gamepadLabel == null) return;

            // A locked cell stays greyed whatever the selection is doing, so it reads as information
            // rather than as something you failed to click.
            gamepadLabel.color = !GamepadEditable
                ? UITheme.InkMuted
                : (!showFocus || column == 1 ? UITheme.Ink : UITheme.InkMuted);
        }
    }
}
