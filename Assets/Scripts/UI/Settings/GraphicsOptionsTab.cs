using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Graphics tab: a preset row on top and the individual knobs under it.
    ///
    /// Picking a preset pushes values into every row below; touching any of those rows flips the
    /// preset to "Custom" — <see cref="SettingsService"/> does that bookkeeping, this panel only has
    /// to repaint the rows afterwards.
    /// </summary>
    public class GraphicsOptionsTab : MonoBehaviour
    {
        [SerializeField] private CycleSettingsRow presetRow;
        [SerializeField] private SliderSettingsRow renderScaleRow;
        [SerializeField] private CycleSettingsRow lightingRow;
        [SerializeField] private CycleSettingsRow reflectionRow;
        [SerializeField] private CycleSettingsRow antiAliasingRow;
        [SerializeField] private CycleSettingsRow postProcessingRow;

        private void OnEnable()
        {
            SettingsService settings = SettingsService.Instance;
            if (settings == null) return;

            if (presetRow != null)
                presetRow.Configure(
                    Localization.Get("graphics.preset"),
                    Localization.GetArray("opt.preset"),
                    settings.GetGraphicsPreset(),
                    OnPresetChanged);

            if (renderScaleRow != null)
                renderScaleRow.Configure(
                    Localization.Get("graphics.render_scale"), 50f, 100f, 5f, "0.0",
                    settings.GetRenderScale(),
                    value => ChangeAndMarkCustom(() => settings.SetRenderScale(value)));

            ConfigureQualityRow(lightingRow, "graphics.lighting",
                settings.GetLightingQuality(), settings.SetLightingQuality);
            ConfigureQualityRow(reflectionRow, "graphics.reflection",
                settings.GetReflectionQuality(), settings.SetReflectionQuality);
            ConfigureQualityRow(antiAliasingRow, "graphics.anti_aliasing",
                settings.GetAntiAliasing(), settings.SetAntiAliasing);
            ConfigureQualityRow(postProcessingRow, "graphics.post",
                settings.GetPostProcessingQuality(), settings.SetPostProcessingQuality);
        }

        private void ConfigureQualityRow(
            CycleSettingsRow row, string labelKey, int current, System.Action<int> setter)
        {
            if (row == null) return;
            row.Configure(
                Localization.Get(labelKey),
                Localization.GetArray("opt.quality"),
                current,
                index => ChangeAndMarkCustom(() => setter(index)));
        }

        private void ChangeAndMarkCustom(System.Action change)
        {
            change();
            RefreshPresetRow();
        }

        private void OnPresetChanged(int index)
        {
            SettingsService settings = SettingsService.Instance;
            if (settings == null) return;

            settings.SetGraphicsPreset(index);
            RefreshRowsFromSettings(settings);
        }

        private void RefreshPresetRow()
        {
            SettingsService settings = SettingsService.Instance;
            if (settings == null || presetRow == null) return;
            presetRow.SetIndexSilently(settings.GetGraphicsPreset());
        }

        private void RefreshRowsFromSettings(SettingsService settings)
        {
            renderScaleRow?.SetValueSilently(settings.GetRenderScale());
            lightingRow?.SetIndexSilently(settings.GetLightingQuality());
            reflectionRow?.SetIndexSilently(settings.GetReflectionQuality());
            antiAliasingRow?.SetIndexSilently(settings.GetAntiAliasing());
            postProcessingRow?.SetIndexSilently(settings.GetPostProcessingQuality());
        }
    }
}
