using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The Options surface. Identical whether it is reached from the pause menu or from the main
    /// menu — only the background behind it differs, so this component is background-agnostic and is
    /// reused by both, which is what the design doc asks for.
    ///
    /// Layout follows Exit 8: a tab bar (Game / Video / Graphics / Audio / Controls) over a table of
    /// setting rows, with Back at the bottom. Tabs and selection live in
    /// <see cref="OptionsTabController"/> and <see cref="RowMenu"/>; this class is the wrapper the
    /// menus talk to.
    ///
    /// Closing deactivates <see cref="contentRoot"/> rather than just fading the CanvasGroup. A faded
    /// screen is still a running screen: its RowMenu would keep reading Navigate/Submit and could
    /// start a rebind that nobody can see.
    /// </summary>
    public class OptionsScreen : UIScreen
    {
        [Tooltip("Everything below the screen root: tab bar, tables, Back. Deactivated while closed.")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private OptionsTabController tabController;

        /// <summary>Raised when the screen fully closes (so the caller can refocus its own menu).</summary>
        public System.Action Closed;

        protected override void Awake()
        {
            base.Awake();

            if (tabController == null) tabController = GetComponentInChildren<OptionsTabController>(true);
            if (contentRoot == null && tabController != null) contentRoot = tabController.gameObject;
            if (tabController != null) tabController.BackRequested += Back;

            SetContentActive(false);
        }

        private void OnDestroy()
        {
            if (tabController != null) tabController.BackRequested -= Back;
        }

        protected override void OnOpened() => SetContentActive(true);

        protected override void OnClosed()
        {
            // Never leave a rebind prompt armed behind a closed screen.
            InputBindingService.Instance?.CancelRebind();
            SetContentActive(false);
        }

        /// <summary>Back / Cancel: close the screen and hand control back to whoever opened it.</summary>
        public void Back()
        {
            Close();
            Closed?.Invoke();
        }

        private void SetContentActive(bool active)
        {
            if (contentRoot != null && contentRoot != gameObject) contentRoot.SetActive(active);
        }
    }
}
