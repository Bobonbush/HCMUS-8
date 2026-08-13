using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Video tab: display mode, resolution, FOV, frame rate limit, VSync, motion blur and brightness.
    ///
    /// Brightness is not in the Exit 8 reference but the team doc lists it as required, and the
    /// service already implements it through a full-screen overlay.
    /// </summary>
    public class VideoOptionsTab : MonoBehaviour
    {
        [SerializeField] private CycleSettingsRow displayModeRow;
        [SerializeField] private CycleSettingsRow resolutionRow;
        [SerializeField] private SliderSettingsRow fovRow;
        [SerializeField] private CycleSettingsRow frameRateRow;
        [SerializeField] private CycleSettingsRow vsyncRow;
        [SerializeField] private CycleSettingsRow motionBlurRow;
        [SerializeField] private SliderSettingsRow brightnessRow;

        private readonly List<Resolution> resolutions = new List<Resolution>();
        private readonly List<string> resolutionNames = new List<string>();

        private void OnEnable()
        {
            SettingsService settings = SettingsService.Instance;
            if (settings == null) return;

            if (displayModeRow != null)
                displayModeRow.Configure(
                    Localization.Get("video.display_mode"),
                    Localization.GetArray("opt.display_mode"),
                    settings.GetDisplayModeIndex(),
                    settings.SetDisplayMode);

            BuildResolutions(settings);
            if (resolutionRow != null)
                resolutionRow.Configure(
                    Localization.Get("video.resolution"),
                    resolutionNames,
                    CurrentResolutionIndex(settings),
                    OnResolutionChanged);

            if (fovRow != null)
                fovRow.Configure(
                    Localization.Get("video.fov"), 60f, 110f, 1f, "0.0",
                    settings.GetFov(), settings.SetFov);

            if (frameRateRow != null)
                frameRateRow.Configure(
                    Localization.Get("video.frame_rate"),
                    Localization.GetArray("opt.frame_rate"),
                    settings.GetFrameRateIndex(),
                    settings.SetFrameRateIndex);

            if (vsyncRow != null)
                vsyncRow.Configure(
                    Localization.Get("video.vsync"),
                    Localization.GetArray("opt.vsync"),
                    settings.GetVSync(),
                    settings.SetVSync);

            if (motionBlurRow != null)
                motionBlurRow.Configure(
                    Localization.Get("video.motion_blur"),
                    Localization.GetArray("opt.on_off"),
                    settings.GetMotionBlur() ? 1 : 0,
                    index => settings.SetMotionBlur(index == 1));

            if (brightnessRow != null)
                brightnessRow.Configure(
                    Localization.Get("video.brightness"), 0f, 1f, 0.05f, "0.00",
                    settings.GetBrightness(), settings.SetBrightness);
        }

        private void BuildResolutions(SettingsService settings)
        {
            resolutions.Clear();
            resolutionNames.Clear();

            HashSet<string> seen = new HashSet<string>();
            foreach (Resolution res in Screen.resolutions)
            {
                string label = $"{res.width} x {res.height}";
                if (!seen.Add(label)) continue; // dedupe refresh-rate variants

                resolutions.Add(res);
                resolutionNames.Add(label);
            }

            // A monitor list can miss the saved size (someone moved the game to another screen).
            if (resolutions.Count == 0)
            {
                resolutions.Add(Screen.currentResolution);
                resolutionNames.Add($"{Screen.currentResolution.width} x {Screen.currentResolution.height}");
            }
        }

        private int CurrentResolutionIndex(SettingsService settings)
        {
            for (int i = 0; i < resolutions.Count; i++)
                if (resolutions[i].width == settings.GetResolutionWidth() &&
                    resolutions[i].height == settings.GetResolutionHeight())
                    return i;
            return 0;
        }

        private void OnResolutionChanged(int index)
        {
            SettingsService settings = SettingsService.Instance;
            if (settings == null || index < 0 || index >= resolutions.Count) return;

            Resolution res = resolutions[index];
            settings.ApplyVideoMode(res.width, res.height, settings.GetFullScreenMode(), res.refreshRateRatio);
        }
    }
}
