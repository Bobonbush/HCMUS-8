using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Every persistent player setting: what it is worth, where it is stored, and how it reaches the
    /// engine. The main menu and the pause menu share this one service, and saved values are applied
    /// again whenever the game starts or a scene loads.
    ///
    /// Storage is PlayerPrefs, one key per value. Anything the UI can change is readable through a
    /// Get* and writable through a Set*; the Set* both persists and applies. Values that need a
    /// camera or the player (FOV, look sensitivity, camera shake) are stored here but applied by
    /// <c>CameraSettingsBinder</c>, which listens to <see cref="OnSettingsChanged"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SettingsService : MonoBehaviour
    {
        // ---- defaults --------------------------------------------------------------------------

        public const int DefaultResolutionWidth = 1920;
        public const int DefaultResolutionHeight = 1080;
        public const FullScreenMode DefaultFullScreenMode = FullScreenMode.FullScreenWindow;
        public const float DefaultBrightness = 0.5f;
        public const float DefaultVolume = 1f;
        public const int DefaultVSync = 1;
        public const float DefaultFov = 90f;
        public const int DefaultFrameRateIndex = 1;       // 60
        public const bool DefaultMotionBlur = true;
        public const int DefaultGraphicsPreset = (int)GraphicsPreset.High;
        public const float DefaultRenderScale = 100f;     // percent
        public const int DefaultQualityLevel = 2;         // High
        public const float DefaultSensitivity = 1f;
        public const float DefaultCameraAcceleration = 15f;
        public const float DefaultCameraShake = 1f;

        public enum GraphicsPreset { Low, Medium, High, Custom }

        /// <summary>Frame rate cap per option index; -1 means uncapped.</summary>
        public static readonly int[] FrameRateOptions = { 30, 60, 120, 144, 240, -1 };

        /// <summary>Low / Medium / High.</summary>
        public const int QualityLevelCount = 3;
        /// <summary>Low / Medium / High / Custom.</summary>
        public const int PresetCount = 4;

        public static readonly FullScreenMode[] DisplayModes =
        {
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.FullScreenWindow,
            FullScreenMode.Windowed
        };

        /// <summary>Name of the mixer looked up under a Resources folder when nothing is assigned.</summary>
        public const string MixerResourceName = "MainMixer";

        public static SettingsService Instance { get; private set; }

        /// <summary>Raised after any setting changes, so gameplay-side binders can re-apply.</summary>
        public event Action OnSettingsChanged;

        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterParam = "MasterVolume";
        [SerializeField] private string musicParam = "MusicVolume";
        [SerializeField] private string sfxParam = "SfxVolume";

        [Header("Video")]
        [SerializeField] private CanvasGroup brightnessOverlay;

        // ---- keys ------------------------------------------------------------------------------

        private const string KeyMaster = "set_vol_master";
        private const string KeyMusic = "set_vol_music";
        private const string KeySfx = "set_vol_sfx";
        private const string KeyBrightness = "set_brightness";
        private const string KeyFullscreen = "set_fullscreen";
        private const string KeyResWidth = "set_res_w";
        private const string KeyResHeight = "set_res_h";
        private const string KeyRefreshNumerator = "set_res_rate_num";
        private const string KeyRefreshDenominator = "set_res_rate_den";
        private const string KeyVSync = "set_vsync";
        private const string KeyFov = "set_fov";
        private const string KeyFrameRate = "set_fps_limit";
        private const string KeyMotionBlur = "set_motion_blur";
        private const string KeyPreset = "set_gfx_preset";
        private const string KeyRenderScale = "set_render_scale";
        private const string KeyLighting = "set_lighting";
        private const string KeyReflection = "set_reflection";
        private const string KeyAntiAliasing = "set_aa";
        private const string KeyPostProcessing = "set_post";
        private const string KeyInvertX = "set_invert_x";
        private const string KeyInvertY = "set_invert_y";
        private const string KeySensitivityX = "set_sens_x";
        private const string KeySensitivityY = "set_sens_y";
        private const string KeyCameraAcceleration = "set_cam_accel";
        private const string KeyCameraShake = "set_cam_shake";
        private const string KeyLanguage = "set_language";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists(Resources.Load<AudioMixer>(MixerResourceName));
        }

        public static SettingsService EnsureExists(AudioMixer mixer)
        {
            if (Instance == null)
            {
                GameObject serviceObject = new GameObject(nameof(SettingsService));
                Instance = serviceObject.AddComponent<SettingsService>();
            }

            Instance.ConfigureMixer(mixer);
            Instance.EnsureBrightnessOverlay();
            Instance.ApplyAll();
            return Instance;
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
            EnsureBrightnessOverlay();
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyAll();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Hand the original asset back and drop the clone, so leaving Play mode leaves the
            // project exactly as it was found.
            if (runtimePipeline != null)
            {
                QualitySettings.renderPipeline = null;
                Destroy(runtimePipeline);
                runtimePipeline = null;
            }

            Instance = null;
        }

        public void ConfigureMixer(AudioMixer mixer)
        {
            if (mixer != null) audioMixer = mixer;
        }

        /// <summary>
        /// Looks up a group ("Master", "Music", "Sfx") on the game's mixer.
        ///
        /// Any AudioSource that wants to obey the volume sliders has to be routed through one of
        /// these; a source left on the default output bypasses the mixer completely and its volume
        /// slider does nothing.
        /// </summary>
        public static AudioMixerGroup FindMixerGroup(string groupName)
        {
            AudioMixer mixer = Instance != null && Instance.audioMixer != null
                ? Instance.audioMixer
                : Resources.Load<AudioMixer>(MixerResourceName);

            if (mixer == null) return null;

            AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
            return groups != null && groups.Length > 0 ? groups[0] : null;
        }

        public void ApplyAll()
        {
            Localization.SetLanguage(GetLanguage());
            ApplyVolume(masterParam, GetMasterVolume());
            ApplyVolume(musicParam, GetMusicVolume());
            ApplyVolume(sfxParam, GetSfxVolume());
            ApplyBrightness(GetBrightness());
            ApplyVSync(GetVSync());
            ApplyFrameRate(GetFrameRateIndex());
            ApplyGraphics();
            ApplyVideoMode(
                GetResolutionWidth(),
                GetResolutionHeight(),
                GetFullScreenMode(),
                GetRefreshRate(),
                false);

            OnSettingsChanged?.Invoke();
        }

        private void NotifyChanged()
        {
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        // ---- Audio ---------------------------------------------------------------------------

        public float GetMasterVolume() => PlayerPrefs.GetFloat(KeyMaster, DefaultVolume);
        public float GetMusicVolume() => PlayerPrefs.GetFloat(KeyMusic, DefaultVolume);
        public float GetSfxVolume() => PlayerPrefs.GetFloat(KeySfx, DefaultVolume);

        public void SetMasterVolume(float value) => StoreVolume(KeyMaster, masterParam, value);
        public void SetMusicVolume(float value) => StoreVolume(KeyMusic, musicParam, value);
        public void SetSfxVolume(float value) => StoreVolume(KeySfx, sfxParam, value);

        public void ResetAudio()
        {
            SetMasterVolume(DefaultVolume);
            SetMusicVolume(DefaultVolume);
            SetSfxVolume(DefaultVolume);
        }

        private void StoreVolume(string prefKey, string mixerParameter, float value)
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(prefKey, value);
            ApplyVolume(mixerParameter, value);
            NotifyChanged();
        }

        private void ApplyVolume(string mixerParameter, float linear)
        {
            if (audioMixer == null || string.IsNullOrEmpty(mixerParameter)) return;
            float decibels = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
            if (!audioMixer.SetFloat(mixerParameter, decibels))
                Debug.LogWarning($"SettingsService: AudioMixer parameter '{mixerParameter}' is not exposed.", this);
        }

        // ---- Video: screen ---------------------------------------------------------------------

        public float GetBrightness() => PlayerPrefs.GetFloat(KeyBrightness, DefaultBrightness);
        public int GetResolutionWidth() => PlayerPrefs.GetInt(KeyResWidth, DefaultResolutionWidth);
        public int GetResolutionHeight() => PlayerPrefs.GetInt(KeyResHeight, DefaultResolutionHeight);
        public int GetVSync() => Mathf.Clamp(PlayerPrefs.GetInt(KeyVSync, DefaultVSync), 0, 2);

        public FullScreenMode GetFullScreenMode()
        {
            FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt(KeyFullscreen, (int)DefaultFullScreenMode);
            // MaximizedWindow is macOS-only and the UI never offers it, so only the three modes the
            // Display Mode row can produce are accepted.
            if (mode == FullScreenMode.ExclusiveFullScreen ||
                mode == FullScreenMode.FullScreenWindow ||
                mode == FullScreenMode.Windowed)
                return mode;
            return DefaultFullScreenMode;
        }

        /// <summary>Index into <see cref="DisplayModes"/> / <see cref="DisplayModeNames"/>.</summary>
        public int GetDisplayModeIndex()
        {
            FullScreenMode mode = GetFullScreenMode();
            for (int i = 0; i < DisplayModes.Length; i++)
                if (DisplayModes[i] == mode) return i;
            return 1;
        }

        /// <summary>
        /// Refresh rate saved alongside the resolution. Falls back to whatever the screen is
        /// currently running at, which is what Unity would have picked anyway.
        /// </summary>
        public RefreshRate GetRefreshRate()
        {
            uint numerator = (uint)PlayerPrefs.GetInt(KeyRefreshNumerator, 0);
            uint denominator = (uint)PlayerPrefs.GetInt(KeyRefreshDenominator, 0);
            if (numerator == 0 || denominator == 0) return Screen.currentResolution.refreshRateRatio;
            return new RefreshRate { numerator = numerator, denominator = denominator };
        }

        public void SetBrightness(float brightness)
        {
            brightness = Mathf.Clamp01(brightness);
            PlayerPrefs.SetFloat(KeyBrightness, brightness);
            ApplyBrightness(brightness);
            NotifyChanged();
        }

        public void SetVSync(int count)
        {
            count = Mathf.Clamp(count, 0, 2);
            PlayerPrefs.SetInt(KeyVSync, count);
            ApplyVSync(count);
            NotifyChanged();
        }

        public void SetDisplayMode(int index)
        {
            FullScreenMode mode = index >= 0 && index < DisplayModes.Length
                ? DisplayModes[index]
                : DefaultFullScreenMode;
            ApplyVideoMode(GetResolutionWidth(), GetResolutionHeight(), mode, GetRefreshRate(), true);
            NotifyChanged();
        }

        public void ApplyVideoMode(int width, int height, FullScreenMode mode, RefreshRate rate)
        {
            ApplyVideoMode(width, height, mode, rate, true);
            NotifyChanged();
        }

        private void ApplyVideoMode(int width, int height, FullScreenMode mode, RefreshRate rate, bool persist)
        {
            width = Mathf.Max(640, width);
            height = Mathf.Max(360, height);
            if (rate.numerator == 0 || rate.denominator == 0)
                rate = Screen.currentResolution.refreshRateRatio;

            if (persist)
            {
                PlayerPrefs.SetInt(KeyResWidth, width);
                PlayerPrefs.SetInt(KeyResHeight, height);
                PlayerPrefs.SetInt(KeyFullscreen, (int)mode);
                PlayerPrefs.SetInt(KeyRefreshNumerator, (int)rate.numerator);
                PlayerPrefs.SetInt(KeyRefreshDenominator, (int)rate.denominator);
            }

            if (Screen.width == width && Screen.height == height && Screen.fullScreenMode == mode)
                return;

            // Passing the refresh rate matters for ExclusiveFullScreen: without it Unity picks the
            // first mode matching the size, which on a high-refresh monitor is usually the wrong one.
            Screen.SetResolution(width, height, mode, rate);
        }

        private void ApplyVSync(int count) => QualitySettings.vSyncCount = Mathf.Clamp(count, 0, 2);

        // ---- Video: camera & frame rate ----------------------------------------------------------

        public float GetFov() => PlayerPrefs.GetFloat(KeyFov, DefaultFov);
        public int GetFrameRateIndex() =>
            Mathf.Clamp(PlayerPrefs.GetInt(KeyFrameRate, DefaultFrameRateIndex), 0, FrameRateOptions.Length - 1);
        public bool GetMotionBlur() => PlayerPrefs.GetInt(KeyMotionBlur, DefaultMotionBlur ? 1 : 0) == 1;

        public void SetFov(float fov)
        {
            PlayerPrefs.SetFloat(KeyFov, Mathf.Clamp(fov, 60f, 110f));
            NotifyChanged();
        }

        public void SetFrameRateIndex(int index)
        {
            index = Mathf.Clamp(index, 0, FrameRateOptions.Length - 1);
            PlayerPrefs.SetInt(KeyFrameRate, index);
            ApplyFrameRate(index);
            NotifyChanged();
        }

        public void SetMotionBlur(bool enabled)
        {
            PlayerPrefs.SetInt(KeyMotionBlur, enabled ? 1 : 0);
            bool applied = ApplyMotionBlur(enabled);
            NotifyChanged();

            // Only worth saying when the player just used the row and nothing happened. Saying it on
            // every scene load buried the console in a note aimed at whoever dresses the scene.
            if (!applied && enabled && !motionBlurWarningShown)
            {
                motionBlurWarningShown = true;
                Debug.LogWarning(
                    "SettingsService: Motion Blur has nothing to affect — this scene has no Volume " +
                    "carrying a MotionBlur override. Scene setup, not a settings bug.");
            }
        }

        private void ApplyFrameRate(int index)
        {
            Application.targetFrameRate = FrameRateOptions[Mathf.Clamp(index, 0, FrameRateOptions.Length - 1)];
        }

        // ---- Graphics ----------------------------------------------------------------------------

        public int GetGraphicsPreset() =>
            Mathf.Clamp(PlayerPrefs.GetInt(KeyPreset, DefaultGraphicsPreset), 0, PresetCount - 1);
        public float GetRenderScale() => PlayerPrefs.GetFloat(KeyRenderScale, DefaultRenderScale);
        public int GetLightingQuality() => ClampQuality(PlayerPrefs.GetInt(KeyLighting, DefaultQualityLevel));
        public int GetReflectionQuality() => ClampQuality(PlayerPrefs.GetInt(KeyReflection, DefaultQualityLevel));
        public int GetAntiAliasing() => ClampQuality(PlayerPrefs.GetInt(KeyAntiAliasing, DefaultQualityLevel));
        public int GetPostProcessingQuality() => ClampQuality(PlayerPrefs.GetInt(KeyPostProcessing, DefaultQualityLevel));

        private static int ClampQuality(int value) => Mathf.Clamp(value, 0, QualityLevelCount - 1);

        public void SetRenderScale(float percent) => StoreGraphicsFloat(KeyRenderScale, Mathf.Clamp(percent, 50f, 100f));
        public void SetLightingQuality(int level) => StoreGraphicsInt(KeyLighting, level);
        public void SetReflectionQuality(int level) => StoreGraphicsInt(KeyReflection, level);
        public void SetAntiAliasing(int level) => StoreGraphicsInt(KeyAntiAliasing, level);
        public void SetPostProcessingQuality(int level) => StoreGraphicsInt(KeyPostProcessing, level);

        /// <summary>
        /// Applies a whole preset. Picking Custom keeps the current values — Custom is what a preset
        /// becomes once a single row underneath it is touched, exactly like the reference UI.
        /// </summary>
        public void SetGraphicsPreset(int preset)
        {
            preset = Mathf.Clamp(preset, 0, PresetCount - 1);
            PlayerPrefs.SetInt(KeyPreset, preset);

            if (preset != (int)GraphicsPreset.Custom)
            {
                int level = preset; // Low/Medium/High line up with the quality levels
                PlayerPrefs.SetFloat(KeyRenderScale, level == 0 ? 65f : level == 1 ? 85f : 100f);
                PlayerPrefs.SetInt(KeyLighting, level);
                PlayerPrefs.SetInt(KeyReflection, level);
                PlayerPrefs.SetInt(KeyAntiAliasing, level);
                PlayerPrefs.SetInt(KeyPostProcessing, level);
            }

            ApplyGraphics();
            NotifyChanged();
        }

        private void StoreGraphicsInt(string key, int level)
        {
            PlayerPrefs.SetInt(key, ClampQuality(level));
            MarkPresetCustom();
            ApplyGraphics();
            NotifyChanged();
        }

        private void StoreGraphicsFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            MarkPresetCustom();
            ApplyGraphics();
            NotifyChanged();
        }

        private void MarkPresetCustom() => PlayerPrefs.SetInt(KeyPreset, (int)GraphicsPreset.Custom);

        /// <summary>
        /// Pushes the graphics values onto the URP asset, the quality settings and the main camera.
        /// Camera-dependent parts are re-applied on every scene load, since each scene has its own.
        /// </summary>
        /// <summary>
        /// The URP asset this service is allowed to write to: a runtime clone, never the file in
        /// Assets/Settings. Editing the original would work, but in the editor the pipeline asset is
        /// the real project file — dropping Resolution Scale to 50 during Play would leave
        /// PC_RPAsset.asset modified on disk and, if committed, would ship a blurry game to everyone.
        /// Cloning costs one small ScriptableObject (the renderer data underneath stays shared).
        /// </summary>
        private UniversalRenderPipelineAsset runtimePipeline;

        private UniversalRenderPipelineAsset GetWritablePipeline()
        {
            if (runtimePipeline != null) return runtimePipeline;

            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset source))
                return null;

            runtimePipeline = Instantiate(source);
            runtimePipeline.name = source.name + " (Runtime)";
            // Overrides the pipeline for the active quality level only, which is what the player's
            // graphics settings should do.
            QualitySettings.renderPipeline = runtimePipeline;
            return runtimePipeline;
        }

        public void ApplyGraphics()
        {
            int lighting = GetLightingQuality();
            int antiAliasing = GetAntiAliasing();

            UniversalRenderPipelineAsset urp = GetWritablePipeline();
            if (urp != null)
            {
                urp.renderScale = Mathf.Clamp(GetRenderScale() / 100f, 0.5f, 1f);
                // MSAA takes 1/2/4/8; 1 means off. See the note in ApplyCameraGraphics for why the
                // two AA mechanisms are never both enabled.
                urp.msaaSampleCount = antiAliasing == 0 ? 1 : antiAliasing == 1 ? 2 : 4;
                urp.shadowDistance = lighting == 0 ? 25f : lighting == 1 ? 50f : 80f;
                urp.shadowCascadeCount = lighting == 0 ? 1 : lighting == 1 ? 2 : 4;
            }

            QualitySettings.realtimeReflectionProbes = GetReflectionQuality() > 0;
            // Fully qualified: URP declares a ShadowQuality of its own.
            QualitySettings.shadows = lighting == 0
                ? UnityEngine.ShadowQuality.HardOnly
                : UnityEngine.ShadowQuality.All;

            ApplyCameraGraphics();
            ApplyMotionBlur(GetMotionBlur());
        }

        private void ApplyCameraGraphics()
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            if (data == null) return;

            // URP refuses to run TAA while MSAA is on ("Disabling TAA because MSAA is on") and would
            // silently leave the frame with no anti-aliasing at all. Since the levels above raise
            // MSAA, the camera stays on the post-process AA modes that coexist with it:
            //   Low    = FXAA, no MSAA        (cheapest)
            //   Medium = SMAA + MSAA 2x
            //   High   = SMAA + MSAA 4x
            // Switching High to TAA later would mean setting msaaSampleCount back to 1 first.
            switch (GetAntiAliasing())
            {
                case 0: data.antialiasing = AntialiasingMode.FastApproximateAntialiasing; break;
                default: data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; break;
            }
            data.antialiasingQuality = GetAntiAliasing() == 0
                ? AntialiasingQuality.Low
                : AntialiasingQuality.High;

            // Low turns post processing off outright. Previously this row only gated motion blur,
            // which meant two of its three settings did nothing the player could see.
            data.renderPostProcessing = GetPostProcessingQuality() > 0;
        }

        /// <summary>
        /// Toggles the MotionBlur override on every volume in the scene. Uses <c>volume.profile</c>
        /// (a runtime copy) rather than <c>sharedProfile</c>, so this never writes into the asset.
        /// </summary>
        /// <summary>
        /// Switches the MotionBlur override on every Volume in the scene.
        /// Returns false when the scene has no such override, i.e. the setting had nothing to do.
        /// </summary>
        private bool ApplyMotionBlur(bool enabled)
        {
            bool allowedByQuality = GetPostProcessingQuality() > 0;
            bool found = false;

            foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (volume.sharedProfile == null) continue;
                if (!volume.profile.TryGet(out MotionBlur motionBlur)) continue;
                motionBlur.active = enabled && allowedByQuality;
                found = true;
            }

            return found;
        }

        private bool motionBlurWarningShown;

        // ---- Game: camera feel -------------------------------------------------------------------

        /// <summary>
        /// Menu language. Stored here with everything else so it survives between sessions and is
        /// applied before any screen builds its labels.
        /// </summary>
        public Language GetLanguage()
        {
            int stored = PlayerPrefs.GetInt(KeyLanguage, (int)Language.English);
            return stored == (int)Language.Vietnamese ? Language.Vietnamese : Language.English;
        }

        public void SetLanguage(Language language)
        {
            PlayerPrefs.SetInt(KeyLanguage, (int)language);
            Localization.SetLanguage(language);
            NotifyChanged();
        }

        public bool GetInvertX() => PlayerPrefs.GetInt(KeyInvertX, 0) == 1;
        public bool GetInvertY() => PlayerPrefs.GetInt(KeyInvertY, 0) == 1;
        public float GetSensitivityX() => PlayerPrefs.GetFloat(KeySensitivityX, DefaultSensitivity);
        public float GetSensitivityY() => PlayerPrefs.GetFloat(KeySensitivityY, DefaultSensitivity);
        public float GetCameraAcceleration() => PlayerPrefs.GetFloat(KeyCameraAcceleration, DefaultCameraAcceleration);
        public float GetCameraShake() => PlayerPrefs.GetFloat(KeyCameraShake, DefaultCameraShake);

        public void SetInvertX(bool inverted) => StoreCameraInt(KeyInvertX, inverted ? 1 : 0);
        public void SetInvertY(bool inverted) => StoreCameraInt(KeyInvertY, inverted ? 1 : 0);
        public void SetSensitivityX(float value) => StoreCameraFloat(KeySensitivityX, Mathf.Clamp(value, 0.1f, 3f));
        public void SetSensitivityY(float value) => StoreCameraFloat(KeySensitivityY, Mathf.Clamp(value, 0.1f, 3f));
        public void SetCameraAcceleration(float value) => StoreCameraFloat(KeyCameraAcceleration, Mathf.Clamp(value, 1f, 30f));
        public void SetCameraShake(float value) => StoreCameraFloat(KeyCameraShake, Mathf.Clamp(value, 0f, 2f));

        private void StoreCameraInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            NotifyChanged();
        }

        private void StoreCameraFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            NotifyChanged();
        }

        // ---- brightness overlay --------------------------------------------------------------

        private void ApplyBrightness(float brightness)
        {
            EnsureBrightnessOverlay();
            if (brightnessOverlay != null)
                brightnessOverlay.alpha = Mathf.Lerp(0.65f, 0f, Mathf.Clamp01(brightness));
        }

        /// <summary>
        /// Unity has no portable hardware-gamma API, so brightness is faked with a full-screen black
        /// overlay on its own canvas, sorted above everything else.
        /// </summary>
        private void EnsureBrightnessOverlay()
        {
            if (brightnessOverlay != null) return;

            GameObject canvasObject = new GameObject(
                "BrightnessOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject shadeObject = new GameObject("Shade", typeof(RectTransform), typeof(Image));
            shadeObject.transform.SetParent(canvasObject.transform, false);
            RectTransform shadeRect = (RectTransform)shadeObject.transform;
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = Vector2.zero;
            shadeRect.offsetMax = Vector2.zero;
            Image shade = shadeObject.GetComponent<Image>();
            shade.color = Color.black;
            shade.raycastTarget = false;

            brightnessOverlay = canvasObject.GetComponent<CanvasGroup>();
            brightnessOverlay.interactable = false;
            brightnessOverlay.blocksRaycasts = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyVolume(masterParam, GetMasterVolume());
            ApplyVolume(musicParam, GetMusicVolume());
            ApplyVolume(sfxParam, GetSfxVolume());
            ApplyBrightness(GetBrightness());
            ApplyVSync(GetVSync());
            ApplyFrameRate(GetFrameRateIndex());
            // The camera and the volumes belong to the new scene, so these must be re-applied.
            ApplyGraphics();

            OnSettingsChanged?.Invoke();
        }
    }
}
