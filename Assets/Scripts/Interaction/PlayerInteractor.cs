using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FreeStyle
{
    /// <summary>
    /// Scans around the character for the nearest / best-aligned IInteractable,
    /// raises an event for the UI, and calls Interact when the key is pressed.
    /// </summary>
    [AddComponentMenu("FreeStyle/Interaction/Player Interactor")]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Scan volume")]
        [SerializeField] float radius = 2.2f;
        [Tooltip("Origin of the scan (usually chest height). Leave empty to use transform + heightOffset.")]
        [SerializeField] Transform origin;
        [SerializeField] float heightOffset = 1f;
        [SerializeField] LayerMask interactableMask = ~0;

        [Header("Filtering")]
        [Tooltip("Maximum angle between the character's facing and the object (degrees). 180 scans all around.")]
        [SerializeField, Range(10f, 180f)] float maxAngle = 120f;
        [Tooltip("Require a clear line of sight to the object.")]
        [SerializeField] bool requireLineOfSight = true;
        [SerializeField] LayerMask lineOfSightBlockers = ~0;

        [Header("Optional")]
        [SerializeField] KeyRing keyRing;
        [Tooltip("Play the interact animation every time the key is pressed.")]
        [SerializeField] bool playInteractAnimation = true;

        public InputActionAsset inputActions;

        readonly Collider[] _hits = new Collider[16];

        InputAction _interactAction;
        IInteractable _current;

        /// <summary>Raised whenever the target changes (null means nothing is in range).</summary>
        public event Action<IInteractable> TargetChanged;

        public IInteractable Current => _current;
        public KeyRing Keys => keyRing;
        public GameObject Owner => gameObject;

        void Awake()
        {
            if (keyRing == null) keyRing = GetComponent<KeyRing>();
        }

        void ResolveInputActions()
        {
            if (inputActions == null)
            {
                inputActions = Game.UI.InputBindingService.Instance != null
                    ? Game.UI.InputBindingService.Instance.actions
                    : null;
            }

            if (inputActions == null)
            {
                enabled = false;
                Debug.LogError(
                    "FirstPersonController: no InputActionAsset assigned. Drag " +
                    "Assets/InputSystem_Actions.inputactions onto the Input Actions field.", this);
                return;
            }

            // Registering the asset first means saved key overrides are already applied by the time
            // the actions below are read.
            Game.UI.InputBindingService.EnsureExists(inputActions);


            _interactAction = inputActions.FindAction("Player/Interact");

            if (_interactAction == null)
            {
                enabled = false;
                Debug.LogError(
                    "FirstPersonController: the assigned asset is missing one of Player/Move, " +
                    "Player/Look, Player/Sprint or Player/Jump.", this);
            }
        }

        void Update()
        {
            IInteractable best = FindBest();
            if (!ReferenceEquals(best, _current))
            {
                _current = best;
                TargetChanged?.Invoke(_current);
            }

            if (_current != null && _interactAction.WasPressedThisFrame())
            {
                _current.Interact(this);
            }
        }

        Vector3 OriginPosition => origin != null ? origin.position : transform.position + Vector3.up * heightOffset;

        IInteractable FindBest()
        {
            Vector3 from = OriginPosition;
            int count = Physics.OverlapSphereNonAlloc(from, radius, _hits, interactableMask, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (col == null) continue;

                var candidate = col.GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract(this)) continue;

                var component = candidate as Component;
                if (component == null) continue;

                Vector3 toTarget = component.transform.position - from;
                Vector3 flat = Vector3.ProjectOnPlane(toTarget, Vector3.up);
                float angle = flat.sqrMagnitude < 0.0001f ? 0f : Vector3.Angle(transform.forward, flat);
                if (angle > maxAngle * 0.5f) continue;

                if (requireLineOfSight && IsBlocked(from, col)) continue;

                // Prefer whatever is close and roughly in front of the character.
                float score = toTarget.magnitude + angle * 0.02f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        bool IsBlocked(Vector3 from, Collider target)
        {
            Vector3 point = target.bounds.center;
            Vector3 dir = point - from;
            float dist = dir.magnitude;
            if (dist < 0.05f) return false;

            if (Physics.Raycast(from, dir / dist, out RaycastHit hit, dist, lineOfSightBlockers, QueryTriggerInteraction.Ignore))
                return hit.collider != target && !hit.collider.transform.IsChildOf(target.transform.root);

            return false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
            Gizmos.DrawWireSphere(OriginPosition, radius);
        }
    }
}
