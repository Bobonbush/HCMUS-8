using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A row that steps through a fixed list of choices: "Display Mode  ◀ Borderless ▶".
    ///
    /// As in Exit 8 the arrows only appear on the row that is currently selected — an unselected row
    /// shows just its value, which is what keeps the table from looking noisy.
    /// </summary>
    public class CycleSettingsRow : SettingsRow
    {
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Button leftArrow;
        [SerializeField] private Button rightArrow;
        [Tooltip("When off, the value wraps around from the last option back to the first.")]
        [SerializeField] private bool clampAtEnds;

        private readonly List<string> options = new List<string>();
        private Action<int> onChanged;
        private int index;

        public int Index => index;

        protected override void Awake()
        {
            base.Awake();
            if (leftArrow != null) leftArrow.onClick.AddListener(() => OnMoveHorizontal(-1));
            if (rightArrow != null) rightArrow.onClick.AddListener(() => OnMoveHorizontal(1));
            SetArrowsVisible(false);
        }

        public void Configure(string label, IList<string> choices, int currentIndex, Action<int> changed)
        {
            Label = label;
            options.Clear();
            if (choices != null) options.AddRange(choices);

            onChanged = changed;
            index = options.Count == 0 ? 0 : Mathf.Clamp(currentIndex, 0, options.Count - 1);
            RefreshValueText();
        }

        /// <summary>Updates the shown value without firing the callback (e.g. a preset changed it).</summary>
        public void SetIndexSilently(int newIndex)
        {
            if (options.Count == 0) return;
            index = Mathf.Clamp(newIndex, 0, options.Count - 1);
            RefreshValueText();
        }

        public override void OnMoveHorizontal(int direction)
        {
            if (options.Count == 0 || direction == 0) return;

            int next = index + (direction > 0 ? 1 : -1);
            if (clampAtEnds) next = Mathf.Clamp(next, 0, options.Count - 1);
            else next = ((next % options.Count) + options.Count) % options.Count;

            if (next == index) return;

            index = next;
            RefreshValueText();
            onChanged?.Invoke(index);
        }

        public override void Refresh() => RefreshValueText();

        protected override void OnSelectionChanged(bool selected)
        {
            SetArrowsVisible(selected && options.Count > 1);
        }

        protected override void OnColorsApplied(Color foreground)
        {
            if (valueText != null) valueText.color = foreground;
        }

        private void SetArrowsVisible(bool visible)
        {
            if (leftArrow != null) leftArrow.gameObject.SetActive(visible);
            if (rightArrow != null) rightArrow.gameObject.SetActive(visible);
        }

        private void RefreshValueText()
        {
            if (valueText == null) return;
            valueText.text = options.Count == 0 ? "-" : options[Mathf.Clamp(index, 0, options.Count - 1)];
        }
    }
}
