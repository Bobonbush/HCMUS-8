using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Audio tab. Exit 8 has a single Volume row, but the team doc requires Master / Music / Sound,
    /// and the mixer in Assets/Resources/MainMixer.mixer already exposes exactly those three.
    ///
    /// Shown on a 0–10 scale like the reference; the service stores 0–1.
    /// </summary>
    public class AudioOptionsTab : MonoBehaviour
    {
        [SerializeField] private SliderSettingsRow masterRow;
        [SerializeField] private SliderSettingsRow musicRow;
        [SerializeField] private SliderSettingsRow sfxRow;

        private const float DisplayScale = 10f;

        private void OnEnable()
        {
            SettingsService settings = SettingsService.Instance;
            if (settings == null) return;

            Configure(masterRow, Localization.Get("audio.master"), settings.GetMasterVolume(), settings.SetMasterVolume);
            Configure(musicRow, Localization.Get("audio.music"), settings.GetMusicVolume(), settings.SetMusicVolume);
            Configure(sfxRow, Localization.Get("audio.sfx"), settings.GetSfxVolume(), settings.SetSfxVolume);
        }

        private static void Configure(
            SliderSettingsRow row, string label, float linearValue, System.Action<float> setter)
        {
            if (row == null) return;
            row.Configure(
                label, 0f, DisplayScale, 0.5f, "0.0",
                linearValue * DisplayScale,
                shown => setter(shown / DisplayScale));
        }
    }
}
