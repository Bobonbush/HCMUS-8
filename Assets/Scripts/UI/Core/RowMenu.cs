using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>
    /// Keyboard/gamepad driving for a vertical list of <see cref="SettingsRow"/>: up/down moves the
    /// highlight, left/right edits the selected row, Submit activates it. Mouse hover feeds the same
    /// selection, so the highlight never disagrees with what the player is pointing at.
    ///
    /// Used by both the pause menu and the Options tabs so the two feel identical.
    /// Input comes from the action asset only — never from Keyboard.current (see the rule at the top
    /// of <see cref="InputBindingService"/>).
    /// </summary>
    public class RowMenu : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string navigateActionPath = "UI/Navigate";
        [SerializeField] private string submitActionPath = "UI/Submit";
        [Tooltip("Seconds a direction must be held before it starts repeating.")]
        [SerializeField] private float repeatDelay = 0.4f;
        [SerializeField] private float repeatRate = 0.12f;

        [Header("Appearance")]
        [Tooltip("Tint every other row slightly differently so a long table stays readable.")]
        [SerializeField] private bool zebraStripe = true;

        private readonly List<SettingsRow> rows = new List<SettingsRow>();
        private int selected = -1;

        private InputAction navigateAction;
        private InputAction submitAction;
        private Vector2 lastDirection;
        private float nextRepeatTime;

        public SettingsRow SelectedRow => selected >= 0 && selected < rows.Count ? rows[selected] : null;

        private void Awake()
        {
            InputActionAsset asset = inputActions != null
                ? inputActions
                : InputBindingService.Instance?.actions;

            if (asset == null)
            {
                Debug.LogWarning(
                    "RowMenu: no InputActionAsset, so this menu is mouse-only. Assign " +
                    "Assets/InputSystem_Actions.inputactions.", this);
                return;
            }

            navigateAction = asset.FindAction(navigateActionPath, false);
            submitAction = asset.FindAction(submitActionPath, false);
        }

        private void OnEnable()
        {
            navigateAction?.Enable();
            submitAction?.Enable();
        }

        private void OnDisable()
        {
            navigateAction?.Disable();
            submitAction?.Disable();
        }

        /// <summary>Takes over a set of rows, selecting the first one.</summary>
        public void SetRows(IList<SettingsRow> newRows)
        {
            Unsubscribe();
            rows.Clear();

            if (newRows != null)
                foreach (SettingsRow row in newRows)
                    if (row != null && row.Navigable)
                        rows.Add(row);

            for (int i = 0; i < rows.Count; i++)
            {
                if (zebraStripe) rows[i].SetAlternate(i % 2 == 1);
                rows[i].Selected += HandleRowSelected;
            }

            Select(rows.Count > 0 ? 0 : -1);
        }

        /// <summary>
        /// Take every row under <paramref name="root"/>, in hierarchy order, then append any extra
        /// rows that live elsewhere in the hierarchy — the Back row sits outside the tab it belongs
        /// to, and a footer nobody can reach with a controller is a footer that does not exist.
        /// </summary>
        public void CollectFrom(GameObject root, params SettingsRow[] extra)
        {
            List<SettingsRow> found = new List<SettingsRow>();
            if (root != null) root.GetComponentsInChildren(true, found);

            if (extra != null)
                foreach (SettingsRow row in extra)
                    if (row != null && !found.Contains(row))
                        found.Add(row);

            SetRows(found);
        }

        public void Refresh()
        {
            foreach (SettingsRow row in rows)
                row.Refresh();
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            foreach (SettingsRow row in rows)
                if (row != null) row.Selected -= HandleRowSelected;
        }

        private void HandleRowSelected(SettingsRow row)
        {
            int index = rows.IndexOf(row);
            if (index >= 0) Select(index);
        }

        private void Select(int index)
        {
            selected = index;
            for (int i = 0; i < rows.Count; i++)
                rows[i].SetSelected(i == selected);
        }

        private void Update()
        {
            if (rows.Count == 0) return;

            if (submitAction != null && submitAction.WasPressedThisFrame())
                SelectedRow?.Activate();

            HandleNavigate();
        }

        /// <summary>
        /// Reads Navigate as discrete steps with hold-to-repeat, so a held stick or arrow key does
        /// not fly through the whole list in a single frame.
        /// </summary>
        private void HandleNavigate()
        {
            if (navigateAction == null) return;

            Vector2 raw = navigateAction.ReadValue<Vector2>();
            Vector2 stepped = new Vector2(Step(raw.x), Step(raw.y));

            if (stepped == Vector2.zero)
            {
                lastDirection = Vector2.zero;
                return;
            }

            if (stepped != lastDirection)
            {
                lastDirection = stepped;
                nextRepeatTime = Time.unscaledTime + repeatDelay;
                Apply(stepped);
                return;
            }

            if (Time.unscaledTime < nextRepeatTime) return;
            nextRepeatTime = Time.unscaledTime + repeatRate;
            Apply(stepped);
        }

        private void Apply(Vector2 direction)
        {
            // Vertical wins: a diagonal stick push should move the highlight, not nudge a slider.
            if (!Mathf.Approximately(direction.y, 0f)) Move(direction.y > 0f ? -1 : 1);
            else SelectedRow?.OnMoveHorizontal((int)direction.x);
        }

        private void Move(int delta)
        {
            if (rows.Count == 0) return;
            int next = selected < 0 ? 0 : selected + delta;
            next = ((next % rows.Count) + rows.Count) % rows.Count;
            Select(next);
        }

        private static float Step(float value)
        {
            const float deadzone = 0.5f;
            if (value > deadzone) return 1f;
            if (value < -deadzone) return -1f;
            return 0f;
        }
    }
}
