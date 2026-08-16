using UnityEngine;
using UnityEngine.Events;

namespace FreeStyle
{
    /// <summary>
    /// Door logic: open / close / lock. It deliberately knows nothing about how the
    /// door moves — that is delegated to DoorMotion.
    ///
    /// How to modify:
    ///  - Speed / curve / audio / HUD text        -> edit the DoorSettings asset.
    ///  - Kind of movement (swing -> slide)       -> swap the DoorMotion component.
    ///  - Extra behaviour (lights, spawns, quest) -> hook the UnityEvents below.
    ///  - Open from a button / trigger / script   -> call Open(), Close() or Toggle().
    /// </summary>
    [SelectionBase]
    [AddComponentMenu("FreeStyle/Door/Door")]
    public class Door : MonoBehaviour, IInteractable
    {
        [Header("Configuration")]
        [Tooltip("Leave empty to fall back to the defaults defined in code.")]
        [SerializeField] DoorSettings settings;
        [Tooltip("Leave empty to find a DoorMotion on this object or its children.")]
        [SerializeField] DoorMotion motion;
        [SerializeField] AudioSource audioSource;

        [Header("Initial state")]
        [SerializeField] bool startOpen = false;
        [SerializeField] bool locked = false;
        [Tooltip("Key ID needed to open this door. Empty means it can never be unlocked with a key.")]
        [SerializeField] string requiredKeyId = "";
        [SerializeField] bool consumeKeyOnUnlock = false;

        [Header("Events")]
        public UnityEvent onOpened;
        public UnityEvent onClosed;
        public UnityEvent onUnlocked;
        public UnityEvent onLockedAttempt;

        DoorSettings _runtimeSettings;
        float _raw;          // 0 = closed, 1 = open (before the curve is applied)
        float _target;
        bool _moving;
        float _autoCloseTimer;

        public bool IsOpen => _target > 0.5f;
        public bool IsMoving => _moving;
        public bool IsLocked => locked;
        public DoorSettings Settings => _runtimeSettings;

        void Awake()
        {
            _runtimeSettings = settings != null ? settings : ScriptableObject.CreateInstance<DoorSettings>();

            if (motion == null) motion = GetComponentInChildren<DoorMotion>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            if (motion == null)
            {
                Debug.LogError($"[Door] '{name}' has no DoorMotion, so it will never move.", this);
                return;
            }

            motion.CaptureClosedState();
            _raw = _target = startOpen ? 1f : 0f;
            motion.Apply(Evaluate(_raw, startOpen));
        }

        void Update()
        {
            if (motion == null) return;

            if (_moving)
            {
                bool opening = _target > _raw;
                float duration = opening ? _runtimeSettings.openDuration : _runtimeSettings.ResolvedCloseDuration;
                _raw = Mathf.MoveTowards(_raw, _target, Time.deltaTime / Mathf.Max(0.01f, duration));
                motion.Apply(Evaluate(_raw, opening));

                if (Mathf.Approximately(_raw, _target))
                {
                    _moving = false;
                    if (IsOpen)
                    {
                        _autoCloseTimer = _runtimeSettings.autoCloseDelay;
                        onOpened?.Invoke();
                    }
                    else
                    {
                        onClosed?.Invoke();
                    }
                }
                return;
            }

            if (IsOpen && _runtimeSettings.autoClose && !_runtimeSettings.oneWayOnly)
            {
                _autoCloseTimer -= Time.deltaTime;
                if (_autoCloseTimer <= 0f) Close();
            }
        }

        float Evaluate(float raw, bool opening)
        {
            var curve = opening ? _runtimeSettings.openCurve : _runtimeSettings.closeCurve;
            return curve != null && curve.length > 0 ? curve.Evaluate(raw) : raw;
        }

        // ---------------- IInteractable ----------------

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (locked)
            {
                bool hasKey = interactor != null && interactor.Keys != null && interactor.Keys.Has(requiredKeyId);
                return hasKey ? _runtimeSettings.unlockPrompt : _runtimeSettings.lockedPrompt;
            }
            return IsOpen ? _runtimeSettings.closePrompt : _runtimeSettings.openPrompt;
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            if (motion == null || _moving) return false;
            if (IsOpen && (_runtimeSettings.oneWayOnly || _runtimeSettings.autoClose)) return false;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor)) return;

            if (locked)
            {
                bool hasKey = interactor != null && interactor.Keys != null && interactor.Keys.Has(requiredKeyId);
                if (!hasKey)
                {
                    PlaySound(_runtimeSettings.lockedSound);
                    onLockedAttempt?.Invoke();
                    return;
                }

                if (consumeKeyOnUnlock) interactor.Keys.Remove(requiredKeyId);
                SetLocked(false);
            }

            Toggle(interactor != null ? interactor.transform.position : transform.position + transform.forward);
        }

        // ---------------- API for other scripts / UnityEvents ----------------

        public void Toggle() => Toggle(transform.position - transform.forward);

        public void Toggle(Vector3 interactorPosition)
        {
            if (IsOpen) Close();
            else Open(interactorPosition);
        }

        public void Open() => Open(transform.position - transform.forward);

        public void Open(Vector3 interactorPosition)
        {
            if (motion == null || IsOpen) return;
            motion.OnOpenRequested(interactorPosition);
            _target = 1f;
            _moving = true;
            PlaySound(_runtimeSettings.openSound);
        }

        public void Close()
        {
            if (motion == null || !IsOpen || _runtimeSettings.oneWayOnly) return;
            _target = 0f;
            _moving = true;
            PlaySound(_runtimeSettings.closeSound);
        }

        public void SetLocked(bool value)
        {
            bool wasLocked = locked;
            locked = value;
            if (wasLocked && !value) onUnlocked?.Invoke();
        }

        void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource != null) audioSource.PlayOneShot(clip, _runtimeSettings.volume);
            else AudioSource.PlayClipAtPoint(clip, transform.position, _runtimeSettings.volume);
        }

#if UNITY_EDITOR
        [ContextMenu("Preview: OPEN")]
        void PreviewOpen()
        {
            var m = motion != null ? motion : GetComponentInChildren<DoorMotion>();
            if (m != null) m.Apply(1f);
        }

        [ContextMenu("Preview: CLOSED")]
        void PreviewClose()
        {
            var m = motion != null ? motion : GetComponentInChildren<DoorMotion>();
            if (m != null) m.Apply(0f);
        }
#endif
    }
}
