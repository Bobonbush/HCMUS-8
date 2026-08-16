using UnityEngine;
using UnityEngine.Events;

namespace FreeStyle
{
    /// <summary>A pickup that unlocks doors. Press E to drop its keyId into the player's KeyRing.</summary>
    [AddComponentMenu("FreeStyle/Interaction/Key Pickup")]
    public class KeyPickup : MonoBehaviour, IInteractable
    {
        [Tooltip("Must match 'requiredKeyId' on the Door.")]
        [SerializeField] string keyId = "RustyKey";
        [SerializeField] string prompt = "Pick up key";
        [Tooltip("Remove the pickup once it has been collected.")]
        [SerializeField] bool destroyOnPickup = true;
        [SerializeField] float spinSpeed = 60f;

        public UnityEvent onPickedUp;

        bool _taken;

        void Update()
        {
            if (spinSpeed != 0f) transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        public string GetPrompt(PlayerInteractor interactor) => prompt;

        public bool CanInteract(PlayerInteractor interactor) =>
            !_taken && interactor != null && interactor.Keys != null;

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor)) return;

            _taken = true;
            interactor.Keys.Add(keyId);
            onPickedUp?.Invoke();

            if (destroyOnPickup) Destroy(gameObject);
        }
    }
}
