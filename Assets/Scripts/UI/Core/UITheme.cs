using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Every visual decision for the menus in one place: colours, text sizes, spacing.
    ///
    /// Why a file instead of numbers typed where they are needed: a menu looks designed when the same
    /// few values repeat everywhere, and looks improvised when each screen invents its own. Before
    /// this, font sizes ran 22/24/26/27/28/32/34/54/82 with no relationship between them.
    ///
    /// Rules that come with it:
    ///  - Text is only ever one of the four sizes below.
    ///  - Gaps are only ever multiples of 8 (4 is allowed for hairlines between rows).
    ///  - Dark ink on light strips. A label must carry its own contrast and never depend on whatever
    ///    happens to be behind it — the game world moves, the text has to stay readable anyway.
    /// </summary>
    public static class UITheme
    {
        // ---- colour ------------------------------------------------------------------------------

        /// <summary>Primary text on light strips.</summary>
        public static readonly Color Ink = Hex(0x16171B);
        /// <summary>Secondary text: hints, messages, disabled values.</summary>
        public static readonly Color InkMuted = Hex(0x5A5C63);
        /// <summary>Text on dark surfaces (inactive tabs).</summary>
        public static readonly Color InkOnDark = Hex(0xE8E8EA);

        /// <summary>Row strip. Nearly opaque on purpose: this is what makes labels readable.</summary>
        public static readonly Color Row = Hex(0xE4E4E7, 0.92f);
        /// <summary>Zebra stripe, one step darker.</summary>
        public static readonly Color RowAlt = Hex(0xDBDBDF, 0.92f);
        /// <summary>The selected row: brightest thing on screen.</summary>
        public static readonly Color RowSelected = Hex(0xFFFFFF, 0.98f);

        public static readonly Color TabInactive = Hex(0x1A1B1F, 0.94f);
        public static readonly Color TabActive = RowSelected;

        /// <summary>The single accent. Used only for the selection bar and the slider fill.</summary>
        public static readonly Color Accent = Hex(0x2F6BD8);

        public static readonly Color ValueBox = Hex(0xF5F5F7);
        public static readonly Color SliderTrack = Hex(0x9A9AA2);
        public static readonly Color SliderHandle = Hex(0x2B2C31);
        public static readonly Color ButtonFace = Hex(0xEDEDF0, 0.96f);

        /// <summary>Veil over the live game behind the pause menu.</summary>
        public static readonly Color PauseVeil = Hex(0x101114, 0.55f);
        /// <summary>Veil over the blurred photo in the menu scenes.</summary>
        public static readonly Color PhotoScrim = Hex(0xE0E0E4, 0.30f);

        // ---- type --------------------------------------------------------------------------------

        /// <summary>Game title on the main menu.</summary>
        public const float Display = 64f;
        /// <summary>Screen title ("Options").</summary>
        public const float Title = 40f;
        /// <summary>Row labels, values, tabs, buttons — almost everything.</summary>
        public const float Body = 24f;
        /// <summary>Status messages and hints.</summary>
        public const float Caption = 18f;

        // ---- spacing (multiples of 8) --------------------------------------------------------------

        public const float SpaceHair = 4f;
        public const float SpaceSmall = 8f;
        public const float SpaceMedium = 16f;
        public const float SpaceLarge = 24f;
        public const float SpaceHuge = 32f;

        // ---- table metrics -----------------------------------------------------------------------

        public const float TableWidth = 900f;
        public const float RowHeight = 52f;
        /// <summary>Hairline between rows, so the strips read as a table rather than one slab.</summary>
        public const float RowGap = 2f;
        public const float TabHeight = 52f;

        public const float RowPaddingLeft = 24f;
        /// <summary>
        /// Distance from the row's right edge to the value column. Leaves room for the ▶ arrow, which
        /// sits outside the column so the values themselves stay in one straight line.
        /// </summary>
        public const float ValueColumnRight = 64f;
        public const float ValueColumnWidth = 190f;
        public const float ValueBoxWidth = 110f;
        public const float ValueBoxHeight = 34f;
        /// <summary>
        /// Width of each column in the Controls tab. That tab shows two values per row — the key and
        /// the gamepad button — so it uses its own narrower columns instead of the single wide one.
        /// </summary>
        public const float RebindColumnWidth = 190f;

        public const float SliderWidth = 200f;
        public const float ArrowSize = 34f;

        /// <summary>Thickness of the slider track. Thin enough to be quiet, thick enough to aim at.</summary>
        public const float SliderTrackHeight = 5f;
        /// <summary>
        /// Knob size. Wider than it is tall: a short pill keeps the slider reading as one quiet
        /// horizontal line, where a circle sits on top of the track like a separate object.
        /// </summary>
        public const float SliderKnobWidth = 12f;
        public const float SliderKnobHeight = 5f;

        /// <summary>Width of the accent bar down the left edge of the selected row.</summary>
        public const float AccentBarWidth = 4f;

        // ---- motion ------------------------------------------------------------------------------

        /// <summary>Seconds for the highlight to move between rows. Short enough to feel instant.</summary>
        public const float SelectionFade = 0.12f;
        /// <summary>Seconds for a whole screen to fade in or out.</summary>
        public const float ScreenFade = 0.12f;

        // ---- helpers -----------------------------------------------------------------------------

        /// <summary>0xRRGGBB to a Color, so the palette above reads like a design document.</summary>
        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                alpha);
        }
    }
}
