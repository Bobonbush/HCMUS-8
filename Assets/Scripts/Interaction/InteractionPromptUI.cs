using UnityEngine;
using UnityEngine.UI;

namespace FreeStyle
{
    /// <summary>
    /// Shows the "[E] Open door" hint while something interactable is in range.
    /// Listens to PlayerInteractor's event, so the UI stays out of the gameplay logic.
    /// </summary>
    [AddComponentMenu("FreeStyle/Interaction/Interaction Prompt UI")]
    public class InteractionPromptUI : MonoBehaviour
    {
        [Tooltip("Leave empty to find the PlayerInteractor in the scene.")]
        [SerializeField] PlayerInteractor interactor;
        [Tooltip("Wrapper object that gets shown/hidden.")]
        [SerializeField] GameObject panel;
        [SerializeField] Text label;
        [SerializeField] string format = "[E] {0}";

        void Start()
        {
            if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
            if (label == null) label = GetComponentInChildren<Text>(true);
            if (panel == null && label != null) panel = label.gameObject;

            if (interactor != null) interactor.TargetChanged += OnTargetChanged;
            Show(null);
        }

        void OnDestroy()
        {
            if (interactor != null) interactor.TargetChanged -= OnTargetChanged;
        }

        void Update()
        {
            // A door's prompt changes with its state (Open/Close/Locked), so refresh it every frame.
            if (interactor != null && interactor.Current != null) Show(interactor.Current);
        }

        void OnTargetChanged(IInteractable target) => Show(target);

        void Show(IInteractable target)
        {
            bool visible = target != null;
            if (panel != null && panel.activeSelf != visible) panel.SetActive(visible);
            if (visible && label != null) label.text = string.Format(format, target.GetPrompt(interactor));
        }
    }
}
