using System.Collections.Generic;
using UnityEngine;

// Phantom footsteps: something walks behind you, matching your pace. Every real
// footstep is echoed a moment later from a point a couple of metres behind the
// player, using the same recorded clips as the real steps. Because each echo is
// scheduled from a snapshot taken at the moment you stepped, stopping dead lets
// you hear it take exactly one more step before the corridor goes quiet.
//
// Purely audio - nothing is rendered, so there is nothing to clip, z-fight or
// see through. The echoes reuse the player's own recorded footstep clips at
// their natural pitch.
public class Anomoly16 : MonoBehaviour, Anomoly
{

    [Tooltip("Seconds between your step and its echo behind you.")]
    public float echoDelay = 0.45f;

    [Tooltip("How far behind the player the echo sounds, in metres.")]
    public float followDistance = 2.2f;

    [Tooltip("Echo loudness relative to the real footstep volume.")]
    [Range(0f, 1f)] public float echoVolume = 0.55f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private struct Echo
    {
        public AudioClip clip;
        public float due;
        public float volume;
        public Vector3 position;
    }

    private bool active;
    private FirstPersonController controller;
    private FirstPersonFootsteps footsteps;
    private AudioSource echoSource;
    private readonly Queue<Echo> pending = new Queue<Echo>();
    private int lastClipIndex = -1;

    public void Evaluate()
    {
        if (active) return;
        controller = FindFirstObjectByType<FirstPersonController>();
        footsteps = controller != null ? controller.GetComponent<FirstPersonFootsteps>() : null;
        if (controller == null || footsteps == null
            || footsteps.footstepClips == null || footsteps.footstepClips.Length == 0) return;

        if (echoSource == null)
        {
            GameObject go = new GameObject("PhantomStepSource");
            go.transform.SetParent(transform, false);
            echoSource = go.AddComponent<AudioSource>();
            echoSource.playOnAwake = false;
            echoSource.spatialBlend = 1f;               // it is *somewhere*, behind you
            echoSource.rolloffMode = AudioRolloffMode.Linear;
            echoSource.minDistance = 1.5f;
            echoSource.maxDistance = 18f;
            echoSource.dopplerLevel = 0f;
            echoSource.outputAudioMixerGroup = Game.UI.SettingsService.FindMixerGroup("Sfx");
        }

        controller.OnFootstep += HandleFootstep;
        active = true;
    }

    public void Restore()
    {
        if (controller != null) controller.OnFootstep -= HandleFootstep;
        active = false;
        pending.Clear();
        if (echoSource != null) echoSource.Stop();
    }

    private void HandleFootstep(FirstPersonController.Foot foot, float intensity)
    {
        AudioClip[] clips = footsteps.footstepClips;
        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && index == lastClipIndex) index = (index + 1) % clips.Length;
        lastClipIndex = index;

        // Snapshot where "behind the player" is right now; by the time the echo
        // plays the player has moved on, so the sound trails them - and if they
        // stop, the last echo still lands behind them, one step too late.
        Vector3 behind = controller.transform.position - controller.transform.forward * followDistance;
        float volume = (footsteps.footstepVolume + footsteps.sprintVolumeBoost * intensity) * echoVolume;
        pending.Enqueue(new Echo
        {
            clip = clips[index],
            due = Time.time + echoDelay,
            volume = Mathf.Clamp01(volume),
            position = behind,
        });
    }

    private void Update()
    {
        // 12 floor instances tick this every frame - bail unless this one is live.
        if (!active || pending.Count == 0) return;
        while (pending.Count > 0 && pending.Peek().due <= Time.time)
        {
            Echo e = pending.Dequeue();
            if (echoSource == null) break;
            echoSource.transform.position = e.position;
            echoSource.PlayOneShot(e.clip, e.volume);
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
