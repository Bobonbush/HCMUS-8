using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Main menu shell: Play / Options / Quit, plus the boot point for the two persistent services.
    ///
    /// The entries are <see cref="MenuButtonRow"/>s driven by a <see cref="RowMenu"/>, so the main
    /// menu, the pause menu and the settings tables all navigate identically.
    ///
    /// Only the wiring lives here — artwork, layout and animation are the other half of the UI task
    /// (Nghi), and can be replaced as long as the references survive.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Backend services")]
        [Tooltip("Mixer with exposed MasterVolume / MusicVolume / SfxVolume parameters.")]
        [SerializeField] private AudioMixer audioMixer;
        [Tooltip("Assets/InputSystem_Actions.inputactions — the same asset the player uses.")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Screens")]
        [SerializeField] private OptionsScreen optionsScreen;
        [Tooltip("Container holding the menu rows, hidden while Options is open.")]
        [SerializeField] private GameObject menuButtons;
        [Tooltip("Game title. Hidden with the rows, or it shows through the Options table.")]
        [SerializeField] private GameObject titleObject;
        [SerializeField] private RowMenu rowMenu;

        [Header("Play")]
        [Tooltip("Scene loaded by Play. Must be in Build Settings.")]
        [SerializeField] private string gameScene = "FirstPerson";

        private void Awake()
        {
            // Both services survive scene loads and re-apply saved values on the way in, so the
            // menu is the natural place to make sure they exist.
            SettingsService.EnsureExists(audioMixer);
            InputBindingService.EnsureExists(inputActions);

            if (rowMenu == null) rowMenu = GetComponentInChildren<RowMenu>(true);
        }

        private void Start()
        {
            // The menu is mouse-driven; whatever gameplay did to the cursor should not leak in here.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (optionsScreen != null) optionsScreen.Closed += ShowButtons;
            ShowButtons();
        }

        private void OnDestroy()
        {
            if (optionsScreen != null) optionsScreen.Closed -= ShowButtons;
        }

        private void ShowButtons()
        {
            SetMenuVisible(true);
            if (rowMenu != null && menuButtons != null) rowMenu.CollectFrom(menuButtons);
        }

        private void SetMenuVisible(bool visible)
        {
            if (menuButtons != null) menuButtons.SetActive(visible);
            if (titleObject != null) titleObject.SetActive(visible);
        }

        // ---- Row hooks ----------------------------------------------------------------------------

        public void OnPlay() => SceneManager.LoadScene(gameScene);

        public void OnSettings()
        {
            SetMenuVisible(false);
            if (optionsScreen != null) optionsScreen.Open();
        }

        /// <summary>Quit to desktop. Only the main menu offers this; in-game it is "Quit To Main".</summary>
        public void OnQuit()
        {
            // Application.Quit does nothing in the editor, so stop play mode there instead.
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
