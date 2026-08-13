using UnityEngine;
using UnityEngine.Events;

namespace Game.UI
{
    /// <summary>
    /// A plain menu entry — "Resume", "Options", "Quit To Main" — drawn as a row so it gets the same
    /// full-width highlight and the same navigation as the settings tables.
    /// </summary>
    public class MenuButtonRow : SettingsRow
    {
        [Tooltip("Localisation key for the label, e.g. menu.resume. Leave empty to keep the text as authored.")]
        [SerializeField] private string labelKey;
        [SerializeField] private UnityEvent activated = new UnityEvent();

        public UnityEvent Activated => activated;

        private void OnEnable()
        {
            Localization.OnLanguageChanged += ApplyLabel;
            ApplyLabel();
        }

        private void OnDisable()
        {
            Localization.OnLanguageChanged -= ApplyLabel;
        }

        private void ApplyLabel()
        {
            if (!string.IsNullOrEmpty(labelKey)) Label = Localization.Get(labelKey);
        }

        public override void Activate() => activated.Invoke();
    }
}
