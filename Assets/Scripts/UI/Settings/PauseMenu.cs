using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// In-game pause menu: Cancel (Escape) pauses and shows <b>Resume / Options / Quit To Main</b>,
    /// laid out as highlighted rows like Exit 8. "Delete Play Data" from the reference is not part of
    /// this game, and Quit goes back to the main menu rather than out to the desktop.
    ///
    /// Pausing uses Time.timeScale = 0; the menu runs on unscaled time so it still animates. The pause
    /// key comes from the UI/Cancel action, never from Keyboard.current — see the rule at the top of
    /// <see cref="InputBindingService"/>. This screen is also the only owner of the cursor lock while
    /// in gameplay, so the player controller never fights it over Escape.
    /// </summary>
    public class PauseMenu : UIScreen
    {
        [Header("Screens")]
        [SerializeField] private OptionsScreen optionsScreen;
        [Tooltip("Container holding the Resume/Options/Quit rows. Hidden while Options is open.")]
        [SerializeField] private GameObject menuButtons;
        [SerializeField] private RowMenu rowMenu;
        [Tooltip("Optional. Freezes and blurs the last gameplay frame behind the menu.")]
        [SerializeField] private PauseBlur pauseBlur;

        [Header("Input")]
        [Tooltip("Assets/InputSystem_Actions.inputactions. Falls back to whatever the binding " +
                 "service already holds.")]
        [SerializeField] private InputActionAsset inputActions;
        [Tooltip("Action used to pause / back out, as 'Map/Action'.")]
        [SerializeField] private string cancelActionPath = "UI/Cancel";

        [Header("Gameplay")]
        [Tooltip("Optional. Found automatically in the scene if left empty.")]
        [SerializeField] private FirstPersonController player;

        [Header("Quit")]
        [Tooltip("Scene loaded by 'Quit To Main'. Must be in Build Settings.")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        public bool IsPaused { get; private set; }

        private InputAction cancelAction;

        protected override void Awake()
        {
            base.Awake();

            InputActionAsset asset = inputActions != null
                ? inputActions
                : InputBindingService.Instance?.actions;

            if (asset != null)
            {
                InputBindingService.EnsureExists(asset);
                cancelAction = asset.FindAction(cancelActionPath, false);
            }

            if (cancelAction == null)
                Debug.LogError(
                    $"PauseMenu: could not find action '{cancelActionPath}'. Assign the " +
                    "InputSystem_Actions asset — the pause key is not allowed to be hardcoded.",
                    this);

            if (rowMenu == null) rowMenu = GetComponentInChildren<RowMenu>(true);
            if (pauseBlur == null) pauseBlur = GetComponentInChildren<PauseBlur>(true);
            if (player == null) player = FindFirstObjectByType<FirstPersonController>();

            // The game starts unpaused, so the rows must start switched off.
            HideButtons();
        }

        private void Start()
        {
            // When Options closes (via Back/Cancel), bring the pause rows back.
            if (optionsScreen != null) optionsScreen.Closed += ShowButtons;
        }

        private void OnEnable() => cancelAction?.Enable();
        private void OnDisable() => cancelAction?.Disable();

        private void OnDestroy()
        {
            if (optionsScreen != null) optionsScreen.Closed -= ShowButtons;
        }

        /// <summary>
        /// Shows the pause rows and hands them to the navigator. Deactivating (not just hiding) the
        /// container matters: a live RowMenu behind a closed menu would still act on Submit.
        /// </summary>
        private void ShowButtons()
        {
            if (menuButtons != null) menuButtons.SetActive(true);
            if (rowMenu != null && menuButtons != null) rowMenu.CollectFrom(menuButtons);
        }

        private void HideButtons()
        {
            if (menuButtons != null) menuButtons.SetActive(false);
        }

        private void Update()
        {
            if (cancelAction == null || !cancelAction.WasPressedThisFrame()) return;

            // A rebind prompt swallows Escape as its own cancel; pausing on the same press would
            // close the screen the player is still using.
            if (InputBindingService.Instance != null && InputBindingService.Instance.IsRebinding) return;

            // While Options is up it owns Cancel — see OptionsTabController. Handling it here too
            // would back out twice on a single press.
            if (optionsScreen != null && optionsScreen.IsOpen) return;

            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            SetGameplayInput(false);

            // The blur has to grab the screen before the menu is drawn, so opening waits a frame for
            // it. Without a blur component the callback runs immediately and nothing changes.
            if (pauseBlur != null) pauseBlur.Capture(ShowMenu);
            else ShowMenu();
        }

        private void ShowMenu()
        {
            // Escape may have been pressed again during the one-frame wait.
            if (!IsPaused) return;
            Open();
            ShowButtons();
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (optionsScreen != null && optionsScreen.IsOpen) optionsScreen.Close();
            HideButtons();
            Close();
            if (pauseBlur != null) pauseBlur.Clear();
            SetGameplayInput(true);
        }

        /// <summary>Freezes the motor and frees the cursor while a menu is up.</summary>
        private void SetGameplayInput(bool enabled)
        {
            if (player == null) player = FindFirstObjectByType<FirstPersonController>();
            if (player == null)
            {
                Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !enabled;
                return;
            }

            player.SetInputEnabled(enabled);
            player.SetCursorLocked(enabled);
        }

        // ---- Row hooks -------------------------------------------------------------------------

        public void OnResume() => Resume();

        public void OnOptions()
        {
            HideButtons();
            if (optionsScreen != null) optionsScreen.Open();
        }

        /// <summary>"Quit To Main": back to the main menu, not out to the desktop.</summary>
        public void OnQuitToMain()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}
