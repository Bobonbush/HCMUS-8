using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Game tab: language, camera inversion, sensitivity, acceleration and shake.
    ///
    /// Values are written straight into <see cref="SettingsService"/>; the player object picks them
    /// up through <c>CameraSettingsBinder</c>, so this panel never touches gameplay code.
    /// </summary>
    public class GameOptionsTab : MonoBehaviour
    {
        [SerializeField] private CycleSettingsRow languageRow;
        [SerializeField] private CycleSettingsRow invertHorizontalRow;
        [SerializeField] private CycleSettingsRow invertVerticalRow;
        [SerializeField] private SliderSettingsRow sensitivityHorizontalRow;
        [SerializeField] private SliderSettingsRow sensitivityVerticalRow;
        [SerializeField] private SliderSettingsRow accelerationRow;
        [SerializeField] private SliderSettingsRow shakeRow;

        private void OnEnable()
        {
            SettingsService settings = SettingsService.Instance;
            if (settings == null) return;

            if (languageRow != null)
                languageRow.Configure(
                    Localization.Get("game.language"),
                    Localization.LanguageNames,
                    (int)settings.GetLanguage(),
                    index => settings.SetLanguage((Language)index));

            if (invertHorizontalRow != null)
                invertHorizontalRow.Configure(
                    Localization.Get("game.invert_horizontal"),
                    Localization.GetArray("opt.invert"),
                    settings.GetInvertX() ? 1 : 0,
                    index => settings.SetInvertX(index == 1));

            if (invertVerticalRow != null)
                invertVerticalRow.Configure(
                    Localization.Get("game.invert_vertical"),
                    Localization.GetArray("opt.invert"),
                    settings.GetInvertY() ? 1 : 0,
                    index => settings.SetInvertY(index == 1));

            if (sensitivityHorizontalRow != null)
                sensitivityHorizontalRow.Configure(
                    Localization.Get("game.sensitivity_horizontal"), 0.1f, 3f, 0.1f, "0.0",
                    settings.GetSensitivityX(), settings.SetSensitivityX);

            if (sensitivityVerticalRow != null)
                sensitivityVerticalRow.Configure(
                    Localization.Get("game.sensitivity_vertical"), 0.1f, 3f, 0.1f, "0.0",
                    settings.GetSensitivityY(), settings.SetSensitivityY);

            if (accelerationRow != null)
                accelerationRow.Configure(
                    Localization.Get("game.acceleration"), 1f, 30f, 1f, "0.0",
                    settings.GetCameraAcceleration(), settings.SetCameraAcceleration);

            if (shakeRow != null)
                shakeRow.Configure(
                    Localization.Get("game.shake"), 0f, 2f, 0.1f, "0.0",
                    settings.GetCameraShake(), settings.SetCameraShake);
        }
    }
}
