using UnityEngine;

/// <summary>
/// Footstep, landing and breathing audio for the first person controller.
///
/// All sounds come from the recorded clips assigned in the inspector (sliced from the
/// team's chosen recordings) - nothing is synthesised and nothing is pitch shifted.
/// With no clips assigned a channel simply stays silent.
///
/// Breathing is a single seamless inhale/exhale loop on its own AudioSource. It stays
/// completely silent while walking; sprinting builds exertion (from FirstPersonCameraFeel)
/// and past a threshold the loop fades in at its natural pitch, then fades back out as
/// you recover.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Player/First Person Footsteps")]
[DisallowMultipleComponent]
public class FirstPersonFootsteps : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController controller;
    public FirstPersonCameraFeel cameraFeel;

    [Header("Clips")]
    [Tooltip("One-shot footstep clips; one is chosen per step (never the same twice in a row).")]
    public AudioClip[] footstepClips;
    [Tooltip("One-shot landing clips, cut from the same recording as the footsteps.")]
    public AudioClip[] landingClips;
    [Tooltip("Seamless breathing loop.")]
    public AudioClip breathLoopClip;

    [Header("Footsteps")]
    [Range(0f, 1f)] public float footstepVolume = 0.42f;
    [Tooltip("Extra volume at full sprint, on top of the base volume.")]
    [Range(0f, 1f)] public float sprintVolumeBoost = 0.3f;
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

        _steps = footstepClips;
        _lands = landingClips;

        if (breathingEnabled)
        {
            _breathSource = gameObject.AddComponent<AudioSource>();
            _breathSource.playOnAwake = false;
            _breathSource.loop = true;
            _breathSource.spatialBlend = 0f;
            _breathSource.volume = 0f;
            _breathSource.outputAudioMixerGroup = _source.outputAudioMixerGroup;
            _breathSource.clip = breathLoopClip;
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
        if (!breathingEnabled || _breathSource == null || _breathSource.clip == null || cameraFeel == null) return;

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

        // Clips play at their natural pitch - variety comes from the different slices,
        // never from pitch shifting.
        _source.panStereo = foot == FirstPersonController.Foot.Left ? -footPan : footPan;
        _source.pitch = 1f;

        float volume = footstepVolume + sprintVolumeBoost * intensity;
        _source.PlayOneShot(_steps[index], Mathf.Clamp01(volume));
    }

    void HandleLand(float impactSpeed)
    {
        if (_lands == null || _lands.Length == 0) return;
        if (impactSpeed < minLandImpactSpeed) return;

        float strength = Mathf.Clamp01(Mathf.InverseLerp(minLandImpactSpeed, landFullImpactSpeed, impactSpeed));

        _source.panStereo = 0f;
        _source.pitch = 1f;
        _source.PlayOneShot(_lands[Random.Range(0, _lands.Length)],
            Mathf.Clamp01(landingVolume * Mathf.Lerp(0.35f, 1f, strength)));
    }

}
