using UnityEngine;
using UnityEngine.Events;

namespace FreeStyle
{
    /// <summary>
    /// Makes any object respond to E without writing code: just drag the method you
    /// want called into the UnityEvent in the Inspector.
    /// </summary>
    [AddComponentMenu("FreeStyle/Interaction/Simple Interactable")]
    public class SimpleInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] string prompt = "Interact";
        [SerializeField] bool enabledForInteraction = true;
        [Tooltip("Can only be used once.")]
        [SerializeField] bool oneShot = false;

        [Tooltip("Drag the method you want called in here from the Inspector.")]
        public UnityEvent onInteract;

        bool _used;

        /// <summary>Who last pressed E on this object, so UnityEvent targets can look it up.</summary>
        public PlayerInteractor LastInteractor { get; private set; }

        public string GetPrompt(PlayerInteractor interactor) => prompt;

        public bool CanInteract(PlayerInteractor interactor) =>
            enabledForInteraction && !(oneShot && _used);

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor)) return;
            _used = true;
            LastInteractor = interactor;
            onInteract?.Invoke();
        }

        public void SetPrompt(string value) => prompt = value;
        public void SetEnabled(bool value) => enabledForInteraction = value;
    }
}
