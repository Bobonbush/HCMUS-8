using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// One line in a settings table: a caption on the left, a value widget on the right, and a
    /// highlight that follows the current selection (the Exit 8 look).
    ///
    /// The row does not know what it edits. A tab hands it a label, the current value and a callback
    /// through <c>Configure</c>, which keeps every tab a few lines long and every row reusable.
    ///
    /// Mouse and keyboard share one notion of "selected": hovering raises <see cref="Selected"/> and
    /// the owning table moves the highlight, exactly as the arrow keys would.
    ///
    /// Colours come from <see cref="UITheme"/>. The strip is nearly opaque and the text is dark, so a
    /// row stays readable over a bright sky or a dark corridor alike — the earlier translucent strips
    /// made readability depend on where the player happened to be standing.
    /// </summary>
    public abstract class SettingsRow : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] protected TMP_Text labelText;
        [SerializeField] protected Image background;
        [Tooltip("Optional accent stripe down the left edge, shown only while selected.")]
        [SerializeField] protected Image accentBar;

        /// <summary>Raised when the row wants to become the selected one (hover or click).</summary>
        public event Action<SettingsRow> Selected;

        public bool IsSelected { get; private set; }

        /// <summary>Rows that cannot be edited are skipped by keyboard navigation.</summary>
        public virtual bool Navigable => isActiveAndEnabled;

        private bool useAlternateColor;
        /// <summary>0 = resting, 1 = selected. Animated so the highlight slides instead of snapping.</summary>
        private float highlight;

        public string Label
        {
            get => labelText != null ? labelText.text : string.Empty;
            set { if (labelText != null) labelText.text = value; }
        }

        protected virtual void Awake()
        {
            highlight = IsSelected ? 1f : 0f;
            Repaint();
        }

        /// <summary>Zebra striping, so a long table stays readable. Called by the table when built.</summary>
        public void SetAlternate(bool alternate)
        {
            useAlternateColor = alternate;
            Repaint();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            OnSelectionChanged(selected);
        }

        protected virtual void OnSelectionChanged(bool selected) { }

        /// <summary>Left/right input, or the ◀ ▶ buttons. Default does nothing.</summary>
        public virtual void OnMoveHorizontal(int direction) { }

        /// <summary>Submit / click on the row itself. Default does nothing.</summary>
        public virtual void Activate() { }

        /// <summary>Re-reads the value from wherever it lives and repaints.</summary>
        public virtual void Refresh() { }

        private void Update()
        {
            float target = IsSelected ? 1f : 0f;
            if (Mathf.Approximately(highlight, target)) return;

            // Unscaled: the menu runs while the game is paused at timeScale 0.
            highlight = Mathf.MoveTowards(
                highlight, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, UITheme.SelectionFade));
            Repaint();
        }

        private void Repaint()
        {
            Color resting = useAlternateColor ? UITheme.RowAlt : UITheme.Row;

            if (background != null)
                background.color = Color.Lerp(resting, UITheme.RowSelected, highlight);

            // The ink does not change — it is the strip that brightens. Swapping text colour as well
            // would make the selection flash and hurt to read down a list.
            Color ink = UITheme.Ink;
            if (labelText != null) labelText.color = ink;

            if (accentBar != null)
            {
                Color accent = UITheme.Accent;
                accent.a *= highlight;
                accentBar.color = accent;
            }

            OnColorsApplied(ink);
        }

        /// <summary>Lets a subclass tint its own value widgets to match the row state.</summary>
        protected virtual void OnColorsApplied(Color foreground) { }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Navigable) Selected?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Navigable) return;
            Selected?.Invoke(this);
            Activate();
        }
    }
}
