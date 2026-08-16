namespace FreeStyle
{
    /// <summary>
    /// Anything the player can press E on implements this interface.
    /// PlayerInteractor only ever talks to the interface, so adding a new kind of
    /// interactable never requires touching the player code.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Text shown on the HUD, e.g. "Open door".</summary>
        string GetPrompt(PlayerInteractor interactor);

        /// <summary>False hides the prompt and blocks the interaction (mid-animation, broken, ...).</summary>
        bool CanInteract(PlayerInteractor interactor);

        void Interact(PlayerInteractor interactor);
    }
}
