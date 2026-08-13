using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The Options shell: a tab bar across the top, one table of <see cref="SettingsRow"/> below it,
    /// and Back at the bottom — the Exit 8 layout.
    ///
    /// Selection and editing are handled by the shared <see cref="RowMenu"/>; this class only owns
    /// the tabs, handing the new tab's rows over on every switch. Tab switching uses the
    /// Previous/Next actions that already exist in the asset, so no new key is invented (see the rule
    /// at the top of <see cref="InputBindingService"/>).
    /// </summary>
    public class OptionsTabController : MonoBehaviour
    {
        [Serializable]
        public sealed class Tab
        {
            [Tooltip("Localisation key, e.g. tab.video.")]
            public string title;
            public Button tabButton;
            public GameObject content;
        }

        [SerializeField] private List<Tab> tabs = new List<Tab>();
        [Tooltip("Back sits below the table but navigates as part of it.")]
        [SerializeField] private MenuButtonRow backRow;
        [SerializeField] private RowMenu rowMenu;

        [Header("Tab visuals")]
        [SerializeField] private Color activeTabColor = new Color(0.93f, 0.93f, 0.95f, 1f);
        [SerializeField] private Color inactiveTabColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color activeTabTextColor = new Color(0.10f, 0.10f, 0.12f, 1f);
        [SerializeField] private Color inactiveTabTextColor = new Color(0.93f, 0.93f, 0.95f, 1f);

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string previousTabActionPath = "Player/Previous";
        [SerializeField] private string nextTabActionPath = "Player/Next";
        [Tooltip("Escape / gamepad B backs out of Options.")]
        [SerializeField] private string cancelActionPath = "UI/Cancel";

        /// <summary>Raised when Back is pressed.</summary>
        public event Action BackRequested;

        private InputAction previousTabAction;
        private InputAction nextTabAction;
        private InputAction cancelAction;
        private int activeTab;

        public int ActiveTab => activeTab;

        private void Awake()
        {
            if (rowMenu == null) rowMenu = GetComponentInChildren<RowMenu>(true);

            InputActionAsset asset = inputActions != null
                ? inputActions
                : InputBindingService.Instance?.actions;

            if (asset != null)
            {
                previousTabAction = asset.FindAction(previousTabActionPath, false);
                nextTabAction = asset.FindAction(nextTabActionPath, false);
                cancelAction = asset.FindAction(cancelActionPath, false);
            }

            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i;
                if (tabs[i].tabButton != null)
                    tabs[i].tabButton.onClick.AddListener(() => SelectTab(index));
            }

            if (backRow != null) backRow.Activated.AddListener(() => BackRequested?.Invoke());
        }

        private void OnEnable()
        {
            previousTabAction?.Enable();
            nextTabAction?.Enable();
            cancelAction?.Enable();

            Localization.OnLanguageChanged += Relocalize;
            ApplyLabels();
            SelectTab(activeTab);
        }

        private void OnDisable()
        {
            previousTabAction?.Disable();
            nextTabAction?.Disable();
            cancelAction?.Disable();
            Localization.OnLanguageChanged -= Relocalize;
        }

        /// <summary>Writes the tab and Back captions in the current language.</summary>
        private void ApplyLabels()
        {
            foreach (Tab tab in tabs)
            {
                if (tab.tabButton == null || string.IsNullOrEmpty(tab.title)) continue;
                TMP_Text label = tab.tabButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = Localization.Get(tab.title);
            }

            // The Back row localises itself through its own MenuButtonRow key.
        }

        /// <summary>
        /// Rebuilds every caption after the language changes. The rows get their text inside their
        /// tab's OnEnable, and re-enabling an object that is already active does not fire OnEnable —
        /// hence the deliberate off/on flick on the active tab.
        /// </summary>
        private void Relocalize()
        {
            ApplyLabels();

            if (tabs.Count == 0) return;
            GameObject content = tabs[activeTab].content;
            if (content == null) return;

            content.SetActive(false);
            content.SetActive(true);
            if (rowMenu != null) rowMenu.CollectFrom(content, backRow);
        }

        private void Update()
        {
            if (previousTabAction != null && previousTabAction.WasPressedThisFrame()) PreviousTab();
            if (nextTabAction != null && nextTabAction.WasPressedThisFrame()) NextTab();

            // Cancel belongs to whichever screen is on top. This controller only exists while Options
            // is open, so it owns Escape then — the pause menu deliberately stands down.
            if (cancelAction != null && cancelAction.WasPressedThisFrame())
            {
                // Escape also cancels a rebind prompt; that press must not close the screen too.
                if (InputBindingService.Instance != null && InputBindingService.Instance.IsRebinding)
                    return;
                BackRequested?.Invoke();
            }
        }

        public void SelectTab(int index)
        {
            if (tabs.Count == 0) return;
            activeTab = Mathf.Clamp(index, 0, tabs.Count - 1);

            for (int i = 0; i < tabs.Count; i++)
            {
                bool active = i == activeTab;
                if (tabs[i].content != null) tabs[i].content.SetActive(active);
                PaintTab(tabs[i], active);
            }

            // The tab's OnEnable has already re-read its values from the service by this point.
            // Back is appended last so it is one press past the bottom of the table.
            if (rowMenu != null) rowMenu.CollectFrom(tabs[activeTab].content, backRow);
        }

        public void NextTab() => SelectTab(WrapTab(activeTab + 1));
        public void PreviousTab() => SelectTab(WrapTab(activeTab - 1));

        private int WrapTab(int index)
        {
            if (tabs.Count == 0) return 0;
            return ((index % tabs.Count) + tabs.Count) % tabs.Count;
        }

        private void PaintTab(Tab tab, bool active)
        {
            if (tab.tabButton == null) return;

            Image image = tab.tabButton.GetComponent<Image>();
            if (image != null) image.color = active ? activeTabColor : inactiveTabColor;

            TMP_Text label = tab.tabButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = active ? activeTabTextColor : inactiveTabTextColor;
        }
    }
}
