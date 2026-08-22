using UnityEngine;

/// <summary>
/// Footstep, landing and breathing audio for the first person controller.
///
/// The clips are synthesised at startup so this works in a project with no audio assets:
/// a footstep is a bright heel tap plus a delayed toe tap and a soft scuff tail, tuned
/// to read as soft-soled shoes on school tile rather than boots on wood. Drop your own
/// clips into the arrays below and those are used instead.
///
/// Breathing is a single seamless inhale/exhale loop on its own AudioSource. It stays
/// completely silent while walking; sprinting builds exertion (from FirstPersonCameraFeel)
/// and past a threshold the loop fades in, speeding up the more tired you are, then fades
/// back out as you recover. No more per-breath one-shots.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Player/First Person Footsteps")]
[DisallowMultipleComponent]
public class FirstPersonFootsteps : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController controller;
    public FirstPersonCameraFeel cameraFeel;

    [Header("Custom Clips (optional)")]
    [Tooltip("Leave empty to use the synthesised footsteps.")]
    public AudioClip[] footstepClips;
    [Tooltip("Leave empty to use the synthesised landing sounds.")]
    public AudioClip[] landingClips;
    [Tooltip("Leave empty to use the synthesised breathing loop.")]
    public AudioClip breathLoopClip;

    [Header("Footsteps")]
    [Range(0f, 1f)] public float footstepVolume = 0.42f;
    [Tooltip("Extra volume at full sprint, on top of the base volume.")]
    [Range(0f, 1f)] public float sprintVolumeBoost = 0.3f;
    [Tooltip("Random pitch variation. Identical footsteps are the giveaway that it is a game.")]
    [Range(0f, 0.3f)] public float pitchVariation = 0.06f;
    [Tooltip("Sprint footsteps are pitched down slightly so they land heavier.")]
    [Range(0f, 0.3f)] public float sprintPitchDrop = 0.07f;
    [Tooltip("How far left/right each foot is panned.")]
    [Range(0f, 1f)] public float footPan = 0.22f;

    [Header("Landing")]
    [Range(0f, 1f)] public float landingVolume = 0.8f;
    [Tooltip("Impact speed in m/s that produces a full volume landing.")]
    public float landFullImpactSpeed = 12f;
    [Tooltip("Landings softer than this are ignored.")]
    public float minLandImpactSpeed = 2.5f;

    [Header("Breathing")]
    public bool breathingEnabled = true;
    [Tooltip("Volume of the breathing loop when fully out of breath.")]
    [Range(0f, 1f)] public float breathVolume = 0.4f;
    [Tooltip("Exertion (0..1, built by sprinting) above which breathing fades in.")]
    [Range(0f, 1f)] public float breathThreshold = 0.35f;
    [Tooltip("Once audible, breathing keeps playing until exertion drops below threshold times this. Stops the sound popping in and out around the threshold.")]
    [Range(0f, 1f)] public float breathHysteresis = 0.55f;
    [Tooltip("Seconds for the breathing to fade in once past the threshold.")]
    public float breathFadeIn = 1.6f;
    [Tooltip("Seconds for the breathing to fade out while recovering.")]
    public float breathFadeOut = 3.0f;

    // ---------------------------------------------------------------- private

    AudioSource _source;          // one-shots: steps and landings
    AudioSource _breathSource;    // dedicated looping source for breathing
    AudioClip[] _steps;
    AudioClip[] _lands;
    int _lastStepIndex = -1;
    bool _breathAudible;
    float _breathLevel;           // 0..1 smoothed fade

    const int StepVariations = 6;
    const int LandVariations = 3;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;          // your own footsteps are not positional

        // Route through the Sfx group so Settings > Audio > Sound Volume actually reaches these.
        // An AudioSource with no group goes straight to the listener and ignores the mixer entirely.
        if (_source.outputAudioMixerGroup == null)
            _source.outputAudioMixerGroup = Game.UI.SettingsService.FindMixerGroup("Sfx");

        if (controller == null) controller = GetComponent<FirstPersonController>();
        if (cameraFeel == null) cameraFeel = GetComponentInChildren<FirstPersonCameraFeel>();

        _steps = footstepClips != null && footstepClips.Length > 0 ? footstepClips : BuildFootsteps();
        _lands = landingClips != null && landingClips.Length > 0 ? landingClips : BuildLandings();

        if (breathingEnabled)
        {
            _breathSource = gameObject.AddComponent<AudioSource>();
            _breathSource.playOnAwake = false;
            _breathSource.loop = true;
            _breathSource.spatialBlend = 0f;
            _breathSource.volume = 0f;
            _breathSource.outputAudioMixerGroup = _source.outputAudioMixerGroup;
            _breathSource.clip = breathLoopClip != null ? breathLoopClip : BuildBreathLoop();
        }
    }

    void OnEnable()
    {
        if (controller == null) return;
        controller.OnFootstep += HandleFootstep;
        controller.OnLand += HandleLand;
    }

    void OnDisable()
    {
        if (controller == null) return;
        controller.OnFootstep -= HandleFootstep;
        controller.OnLand -= HandleLand;
    }

    void Update()
    {
        if (!breathingEnabled || _breathSource == null || cameraFeel == null) return;

        float exertion = cameraFeel.Exertion;

        // Hysteresis: fade in above the threshold, but once audible keep going until
        // exertion has genuinely recovered, so the loop does not stutter at the edge.
        if (!_breathAudible && exertion >= breathThreshold) _breathAudible = true;
        else if (_breathAudible && exertion < breathThreshold * breathHysteresis) _breathAudible = false;

        float dt = Time.deltaTime;
        float target = _breathAudible ? 1f : 0f;
        float tau = _breathAudible ? Mathf.Max(0.05f, breathFadeIn) : Mathf.Max(0.05f, breathFadeOut);
        // Reaches ~95% of the target in `tau` seconds.
        _breathLevel = Mathf.Lerp(_breathLevel, target, 1f - Mathf.Exp(-3f * dt / tau));

        // How hard the breathing is once audible: 0 right at the threshold, 1 exhausted.
        float depth = Mathf.InverseLerp(breathThreshold * breathHysteresis, 1f, exertion);

        // The recording is left at its natural pitch - only volume responds to exertion.
        _breathSource.volume = breathVolume * _breathLevel * Mathf.Lerp(0.5f, 1f, depth);

        if (_breathLevel > 0.005f && !_breathSource.isPlaying) _breathSource.Play();
        else if (_breathLevel <= 0.005f && _breathSource.isPlaying) _breathSource.Stop();
    }

    // ---------------------------------------------------------------- playback

    void HandleFootstep(FirstPersonController.Foot foot, float intensity)
    {
        if (_steps == null || _steps.Length == 0) return;

        int index = Random.Range(0, _steps.Length);
        if (_steps.Length > 1 && index == _lastStepIndex) index = (index + 1) % _steps.Length;
        _lastStepIndex = index;

        _source.panStereo = foot == FirstPersonController.Foot.Left ? -footPan : footPan;
        _source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation) - sprintPitchDrop * intensity;

        float volume = footstepVolume + sprintVolumeBoost * intensity;
        _source.PlayOneShot(_steps[index], Mathf.Clamp01(volume));
    }

    void HandleLand(float impactSpeed)
    {
        if (_lands == null || _lands.Length == 0) return;
        if (impactSpeed < minLandImpactSpeed) return;

        float strength = Mathf.Clamp01(Mathf.InverseLerp(minLandImpactSpeed, landFullImpactSpeed, impactSpeed));

        _source.panStereo = 0f;
        _source.pitch = 1f - 0.12f * strength + Random.Range(-0.04f, 0.04f);
        _source.PlayOneShot(_lands[Random.Range(0, _lands.Length)],
            Mathf.Clamp01(landingVolume * Mathf.Lerp(0.35f, 1f, strength)));
    }

    // ---------------------------------------------------------------- synthesis

    AudioClip[] BuildFootsteps()
    {
        var clips = new AudioClip[StepVariations];
        for (int i = 0; i < StepVariations; i++)
        {
            float t = StepVariations == 1 ? 0.5f : i / (float)(StepVariations - 1);
            clips[i] = SynthesiseShoeStep(
                name: "ProcFootstep" + i,
                seed: 4400 + i * 97,
                duration: Mathf.Lerp(0.16f, 0.21f, t),
                heelHz: Mathf.Lerp(2400f, 1700f, t),
                toeDelay: Mathf.Lerp(0.052f, 0.075f, t),
                thumpHz: Mathf.Lerp(130f, 100f, t),
                thumpAmount: 0.16f,
                scuffAmount: 0.3f);
        }
        return clips;
    }

    AudioClip[] BuildLandings()
    {
        var clips = new AudioClip[LandVariations];
        for (int i = 0; i < LandVariations; i++)
        {
            float t = LandVariations == 1 ? 0.5f : i / (float)(LandVariations - 1);
            clips[i] = SynthesiseShoeStep(
                name: "ProcLanding" + i,
                seed: 9100 + i * 131,
                duration: Mathf.Lerp(0.34f, 0.42f, t),
                heelHz: Mathf.Lerp(1400f, 1000f, t),
                toeDelay: 0.03f,
                thumpHz: Mathf.Lerp(64f, 50f, t),
                thumpAmount: 0.95f,
                scuffAmount: 0.55f);
        }
        return clips;
    }

    /// <summary>
    /// A shoe step on hard tile: a bright, very short heel tap; a quieter toe tap a few
    /// tens of milliseconds later (real steps are two contacts, and the double transient
    /// is most of what reads as "shoe"); a small low thump; and a soft scuff tail.
    /// </summary>
    AudioClip SynthesiseShoeStep(string name, int seed, float duration, float heelHz,
        float toeDelay, float thumpHz, float thumpAmount, float scuffAmount)
    {
        int rate = SampleRate;
        int count = Mathf.Max(16, Mathf.CeilToInt(duration * rate));
        var data = new float[count];
        var rng = new System.Random(seed);

        // Two resonators (state-variable band-pass) — heel bright, toe slightly brighter.
        float fHeel = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(heelHz, 20f, rate * 0.45f) / rate);
        float fToe = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(heelHz * 1.3f, 20f, rate * 0.45f) / rate);
        float fScuff = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(1500f, 20f, rate * 0.45f) / rate);
        const float qRes = 0.9f;   // resonant taps
        const float qScuff = 1.6f; // broad scuff
        float lowH = 0f, bandH = 0f, lowT = 0f, bandT = 0f, lowS = 0f, bandS = 0f;

        int toeStart = Mathf.RoundToInt(toeDelay * rate);
        float peak = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            // Heel: excite the resonator with a burst only in the first ~8ms.
            float heelExcite = t < 0.008f ? noise : 0f;
            float hiH = heelExcite - lowH - qRes * bandH;
            bandH += fHeel * hiH;
            lowH += fHeel * bandH;
            float heel = bandH * Mathf.Exp(-t * 90f);

            // Toe: a second, quieter burst starting at toeDelay.
            float tt = (i - toeStart) / (float)rate;
            float toeExcite = (tt >= 0f && tt < 0.006f) ? noise : 0f;
            float hiT = toeExcite - lowT - qRes * bandT;
            bandT += fToe * hiT;
            lowT += fToe * bandT;
            float toe = tt >= 0f ? bandT * Mathf.Exp(-tt * 110f) * 0.55f : 0f;

            // Scuff: continuous quiet noise through a broad band, fading over the clip.
            float hiS = noise - lowS - qScuff * bandS;
            bandS += fScuff * hiS;
            lowS += fScuff * bandS;
            float scuff = bandS * scuffAmount * 0.35f * Mathf.Exp(-t * 22f);

            // Thump: small low sine so the step has body without booming.
            float thump = thumpAmount * Mathf.Sin(2f * Mathf.PI * thumpHz * t) * Mathf.Exp(-t * 40f);

            float sample = heel + toe + scuff + thump;
            data[i] = sample;
            peak = Mathf.Max(peak, Mathf.Abs(sample));
        }

        Normalise(data, peak, rate);
        return ToClip(name, data, rate);
    }

    /// <summary>
    /// One seamless breath cycle: inhale (brighter, faster swell), short pause, exhale
    /// (darker, longer), pause. The loop starts and ends in silence so it cannot click.
    /// </summary>
    AudioClip BuildBreathLoop()
    {
        int rate = SampleRate;
        const float cycleSeconds = 3.4f;
        int count = Mathf.CeilToInt(cycleSeconds * rate);
        var data = new float[count];
        var rng = new System.Random(2470);

        // Cycle layout, as fractions: inhale 0.00-0.34, pause to 0.42, exhale 0.42-0.92, pause to 1.
        const float inEnd = 0.34f, exStart = 0.42f, exEnd = 0.92f;

        // Two formant-ish band-passes shape the airflow; inhale sits higher than exhale.
        float fIn = 2f * Mathf.Sin(Mathf.PI * 1350f / rate);
        float fEx = 2f * Mathf.Sin(Mathf.PI * 650f / rate);
        float fBody = 2f * Mathf.Sin(Mathf.PI * 320f / rate);
        const float q = 1.1f;
        float lowI = 0f, bandI = 0f, lowE = 0f, bandE = 0f, lowB = 0f, bandB = 0f;

        float peak = 0f;
        for (int i = 0; i < count; i++)
        {
            float u = i / (float)count;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            float hiI = noise - lowI - q * bandI;
            bandI += fIn * hiI;
            lowI += fIn * bandI;

            float hiE = noise - lowE - q * bandE;
            bandE += fEx * hiE;
            lowE += fEx * bandE;

            float hiB = noise - lowB - q * bandB;
            bandB += fBody * hiB;
            lowB += fBody * bandB;

            // Smooth half-sine envelopes; both ends of each phase are zero.
            float envIn = 0f, envEx = 0f;
            if (u < inEnd)
                envIn = Mathf.Sin(Mathf.PI * (u / inEnd));
            else if (u >= exStart && u < exEnd)
                envEx = Mathf.Sin(Mathf.PI * ((u - exStart) / (exEnd - exStart)));

            // Exhale is slightly louder and carries more low body, like real tired breathing.
            float sample = bandI * envIn * envIn * 0.9f
                         + (bandE + bandB * 0.5f) * envEx * envEx * 1.1f;
            data[i] = sample;
            peak = Mathf.Max(peak, Mathf.Abs(sample));
        }

        Normalise(data, peak, rate);
        return ToClip("ProcBreathLoop", data, rate);
    }

    static int SampleRate
    {
        get
        {
            int rate = AudioSettings.outputSampleRate;
            return rate > 0 ? rate : 48000;
        }
    }

    /// <summary>Scales to full scale and fades the tail so the clip cannot click on release.</summary>
    static void Normalise(float[] data, float peak, int rate)
    {
        float gain = peak > 1e-4f ? 0.92f / peak : 0f;
        int fade = Mathf.Min(data.Length, Mathf.Max(8, rate / 500));
        int fadeStart = data.Length - fade;

        for (int i = 0; i < data.Length; i++)
        {
            float sample = data[i] * gain;
            if (i >= fadeStart) sample *= (data.Length - i) / (float)fade;
            data[i] = sample;
        }
    }

    static AudioClip ToClip(string name, float[] data, int rate)
    {
        var clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
