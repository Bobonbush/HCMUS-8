using UnityEngine;

namespace FreeStyle
{
    /// <summary>
    /// Everything about how a door *feels*, kept off the GameObject.
    /// Create one via: Assets > Create > FreeStyle > Door Settings.
    /// Point many doors at the same asset and one edit retunes the whole map;
    /// give a door its own asset when it needs to differ. No code changes either way.
    /// </summary>
    [CreateAssetMenu(fileName = "DoorSettings", menuName = "FreeStyle/Door Settings", order = 0)]
    public class DoorSettings : ScriptableObject
    {
        [Header("Speed")]
        [Min(0.01f)] public float openDuration = 0.6f;
        [Tooltip("<= 0 means reuse openDuration.")]
        public float closeDuration = -1f;

        [Header("Motion curve (0 = closed, 1 = open)")]
        public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Behaviour")]
        [Tooltip("Swing shut again on its own after opening.")]
        public bool autoClose = false;
        [Min(0f)] public float autoCloseDelay = 4f;
        [Tooltip("Once open it can never be closed again.")]
        public bool oneWayOnly = false;

        [Header("HUD text")]
        public string openPrompt = "Open door";
        public string closePrompt = "Close door";
        public string lockedPrompt = "The door is locked";
        public string unlockPrompt = "Unlock with key";

        [Header("Audio (optional)")]
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip lockedSound;
        [Range(0f, 1f)] public float volume = 1f;

        public float ResolvedCloseDuration => closeDuration > 0f ? closeDuration : openDuration;
    }
}
