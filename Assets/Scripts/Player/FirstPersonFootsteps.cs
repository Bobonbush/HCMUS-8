using UnityEngine;

/// <summary>
/// Footstep, landing and breathing audio for the first person controller.
///
/// The clips are synthesised at startup so this works in a project with no audio assets:
/// a footstep is a short noise burst through a resonant low-pass plus a low sine thump,
/// which is what gives the "heel hitting the floor" transient. Drop your own clips into
/// the arrays below and those are used instead.
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

    [Header("Footsteps")]
    [Range(0f, 1f)] public float footstepVolume = 0.5f;
    [Tooltip("Extra volume at full sprint, on top of the base volume.")]
    [Range(0f, 1f)] public float sprintVolumeBoost = 0.35f;
    [Tooltip("Random pitch variation. Identical footsteps are the giveaway that it is a game.")]
    [Range(0f, 0.3f)] public float pitchVariation = 0.07f;
    [Tooltip("Sprint footsteps are pitched down slightly so they land heavier.")]
    [Range(0f, 0.3f)] public float sprintPitchDrop = 0.08f;
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
    [Range(0f, 1f)] public float breathVolume = 0.32f;
    [Tooltip("Breathing is silent below this exertion, then fades in.")]
    [Range(0f, 1f)] public float breathThreshold = 0.12f;
    [Tooltip("Seconds between breaths when fully out of breath.")]
    public float breathIntervalExhausted = 0.85f;
    [Tooltip("Seconds between breaths at the exertion threshold.")]
    public float breathIntervalRested = 2.6f;

    // ---------------------------------------------------------------- private

    AudioSource _source;
    AudioClip[] _steps;
    AudioClip[] _lands;
    AudioClip[] _breathsIn;
    AudioClip[] _breathsOut;
    int _lastStepIndex = -1;
    float _breathTimer;
    bool _breathInhale = true;

    const int StepVariations = 6;
    const int LandVariations = 3;
    const int BreathVariations = 3;

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
        if (breathingEnabled) BuildBreaths();
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
        if (!breathingEnabled || cameraFeel == null || _breathsIn == null) return;

        float exertion = cameraFeel.Exertion;
        if (exertion < breathThreshold)
        {
            _breathTimer = 0f;
            return;
        }

        float t = Mathf.InverseLerp(breathThreshold, 1f, exertion);
        _breathTimer -= Time.deltaTime;
        if (_breathTimer > 0f) return;

        // Inhale and exhale alternate, so the interval is half a breath cycle.
        _breathTimer = Mathf.Lerp(breathIntervalRested, breathIntervalExhausted, t) * 0.5f;

        AudioClip[] bank = _breathInhale ? _breathsIn : _breathsOut;
        _breathInhale = !_breathInhale;

        _source.panStereo = 0f;
        _source.pitch = Random.Range(0.94f, 1.06f);
        _source.PlayOneShot(bank[Random.Range(0, bank.Length)], breathVolume * Mathf.Lerp(0.35f, 1f, t));
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
            clips[i] = SynthesiseImpact(
                name: "ProcFootstep" + i,
                seed: 4400 + i * 97,
                duration: Mathf.Lerp(0.19f, 0.26f, t),
                noiseDecay: Mathf.Lerp(34f, 26f, t),
                cutoffHz: Mathf.Lerp(2600f, 1500f, t),
                resonance: 1.5f,
                thumpHz: Mathf.Lerp(96f, 68f, t),
                thumpDecay: 34f,
                thumpAmount: 0.5f,
                clickAmount: 0.35f);
        }
        return clips;
    }

    AudioClip[] BuildLandings()
    {
        var clips = new AudioClip[LandVariations];
        for (int i = 0; i < LandVariations; i++)
        {
            float t = LandVariations == 1 ? 0.5f : i / (float)(LandVariations - 1);
            clips[i] = SynthesiseImpact(
                name: "ProcLanding" + i,
                seed: 9100 + i * 131,
                duration: Mathf.Lerp(0.36f, 0.44f, t),
                noiseDecay: Mathf.Lerp(18f, 14f, t),
                cutoffHz: Mathf.Lerp(1300f, 900f, t),
                resonance: 1.3f,
                thumpHz: Mathf.Lerp(62f, 48f, t),
                thumpDecay: 17f,
                thumpAmount: 1.0f,
                clickAmount: 0.25f);
        }
        return clips;
    }

    void BuildBreaths()
    {
        _breathsIn = new AudioClip[BreathVariations];
        _breathsOut = new AudioClip[BreathVariations];
        for (int i = 0; i < BreathVariations; i++)
        {
            _breathsIn[i] = SynthesiseBreath("ProcInhale" + i, 1700 + i * 53, 0.62f, 900f, 2200f, 0.42f);
            _breathsOut[i] = SynthesiseBreath("ProcExhale" + i, 3100 + i * 71, 0.78f, 600f, 1200f, 0.62f);
        }
    }

    /// <summary>
    /// A percussive impact: noise through a resonant low-pass for the "scuff", a decaying
    /// sine for the "thud", and a very short bright click for the initial heel contact.
    /// </summary>
    AudioClip SynthesiseImpact(string name, int seed, float duration, float noiseDecay,
        float cutoffHz, float resonance, float thumpHz, float thumpDecay,
        float thumpAmount, float clickAmount)
    {
        int rate = SampleRate;
        int count = Mathf.Max(16, Mathf.CeilToInt(duration * rate));
        var data = new float[count];
        var rng = new System.Random(seed);

        // State variable filter coefficients.
        float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(cutoffHz, 20f, rate * 0.45f) / rate);
        float q = 1f / Mathf.Max(0.5f, resonance);
        float low = 0f, band = 0f;

        float peak = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            float high = noise - low - q * band;
            band += f * high;
            low += f * band;

            float envelope = Mathf.Exp(-t * noiseDecay);
            float click = clickAmount * high * Mathf.Exp(-t * 260f);
            float thump = thumpAmount * Mathf.Sin(2f * Mathf.PI * thumpHz * t) * Mathf.Exp(-t * thumpDecay);

            float sample = low * envelope + click + thump;
            data[i] = sample;
            peak = Mathf.Max(peak, Mathf.Abs(sample));
        }

        Normalise(data, peak, rate);
        return ToClip(name, data, rate);
    }

    /// <summary>Band-passed noise with a slow swell: reads as air moving, not as static.</summary>
    AudioClip SynthesiseBreath(string name, int seed, float duration, float lowCutHz, float highCutHz, float attackFraction)
    {
        int rate = SampleRate;
        int count = Mathf.Max(16, Mathf.CeilToInt(duration * rate));
        var data = new float[count];
        var rng = new System.Random(seed);

        float fHigh = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(highCutHz, 20f, rate * 0.45f) / rate);
        float fLow = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(lowCutHz, 20f, rate * 0.45f) / rate);
        const float q = 1.4f;
        float lowA = 0f, bandA = 0f, lowB = 0f, bandB = 0f;

        int attackSamples = Mathf.Max(1, Mathf.RoundToInt(count * Mathf.Clamp01(attackFraction)));
        float peak = 0f;

        for (int i = 0; i < count; i++)
        {
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            float highA = noise - lowA - q * bandA;
            bandA += fHigh * highA;
            lowA += fHigh * bandA;

            // Subtracting a second, lower low-pass leaves a band: the vocal-tract-ish part.
            float highB = lowA - lowB - q * bandB;
            bandB += fLow * highB;
            lowB += fLow * bandB;

            float banded = lowA - lowB;

            float envelope = i < attackSamples
                ? Mathf.Sin(Mathf.PI * 0.5f * (i / (float)attackSamples))
                : Mathf.Cos(Mathf.PI * 0.5f * ((i - attackSamples) / (float)Mathf.Max(1, count - attackSamples)));

            float sample = banded * envelope * envelope;
            data[i] = sample;
            peak = Mathf.Max(peak, Mathf.Abs(sample));
        }

        Normalise(data, peak, rate);
        return ToClip(name, data, rate);
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
