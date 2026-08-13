using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Drives a TMP label from a <see cref="Localization"/> key and keeps it in sync when the player
    /// switches language. Attach it to any static caption — titles, button labels — instead of typing
    /// the text into the label itself.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedLabel : MonoBehaviour
    {
        [Tooltip("Key in Localization, e.g. menu.reset_defaults.")]
        [SerializeField] private string key;

        private TMP_Text label;

        private void OnEnable()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            Localization.OnLanguageChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Localization.OnLanguageChanged -= Apply;
        }

        public void SetKey(string newKey)
        {
            key = newKey;
            Apply();
        }

        private void Apply()
        {
            if (label != null && !string.IsNullOrEmpty(key)) label.text = Localization.Get(key);
        }
    }
}
