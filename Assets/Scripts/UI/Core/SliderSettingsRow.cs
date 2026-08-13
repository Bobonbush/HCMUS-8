using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A row with a slider and the numeric value in a box to its right ("FOV — 90.0"), matching the
    /// Exit 8 layout. Dragging the slider and pressing left/right both edit the same value.
    /// </summary>
    public class SliderSettingsRow : SettingsRow
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Image valueBox;

        private Action<float> onChanged;
        private float step = 0.1f;
        private string format = "0.0";

        public float Value => slider != null ? slider.value : 0f;

        protected override void Awake()
        {
            base.Awake();
            if (slider != null) slider.onValueChanged.AddListener(HandleSliderChanged);
        }

        public void Configure(
            string label,
            float min,
            float max,
            float stepSize,
            string valueFormat,
            float value,
            Action<float> changed)
        {
            Label = label;
            step = Mathf.Max(0.0001f, stepSize);
            format = string.IsNullOrEmpty(valueFormat) ? "0.0" : valueFormat;
            onChanged = null; // don't fire while initialising

            if (slider != null)
            {
                slider.minValue = min;
                slider.maxValue = max;
                slider.wholeNumbers = false;
                slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
            }

            RefreshValueText();
            onChanged = changed;
        }

        /// <summary>Updates the shown value without firing the callback.</summary>
        public void SetValueSilently(float value)
        {
            if (slider == null) return;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, slider.minValue, slider.maxValue));
            RefreshValueText();
        }

        public override void OnMoveHorizontal(int direction)
        {
            if (slider == null || direction == 0) return;
            float next = Mathf.Clamp(slider.value + step * Mathf.Sign(direction), slider.minValue, slider.maxValue);
            if (Mathf.Approximately(next, slider.value)) return;

            slider.SetValueWithoutNotify(next);
            RefreshValueText();
            onChanged?.Invoke(next);
        }

        public override void Refresh() => RefreshValueText();

        protected override void OnColorsApplied(Color foreground)
        {
            // The value box keeps its own light background (like the reference), so only the row's
            // caption follows the highlight. Nothing to tint here beyond the fallback case.
            if (valueBox == null && valueText != null) valueText.color = foreground;
        }

        private void HandleSliderChanged(float value)
        {
            RefreshValueText();
            onChanged?.Invoke(value);
        }

        private void RefreshValueText()
        {
            if (valueText == null || slider == null) return;
            valueText.text = slider.value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
