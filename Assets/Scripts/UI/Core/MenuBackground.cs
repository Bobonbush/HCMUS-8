using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Full-screen menu backdrop that never distorts and never leaves a gap.
    ///
    /// The temp background photo is 1415x860 (about 1.65:1), so it does not match 16:9 — let alone
    /// 16:10, 4:3 or ultrawide. Stretching it to the screen would visibly squash the elevator doors,
    /// and fitting it inside would letterbox. This uses <see cref="AspectRatioFitter"/> in
    /// EnvelopeParent mode instead: the image keeps its aspect and is scaled up until it covers the
    /// screen, overflowing equally on the two sides that do not fit, with the centre staying put.
    ///
    /// A RectMask2D on the parent clips the overflow, and the scrim on top is what makes menu text
    /// readable over a photograph.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MenuBackground : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private AspectRatioFitter fitter;
        [Tooltip("Dark/!light veil drawn over the photo. Alpha 0 disables it.")]
        [SerializeField] private Image scrim;

        [Header("Safe area")]
        [Tooltip("Fraction of the screen kept free of cropping at the top and bottom. The menu " +
                 "content should stay inside this so nothing important is ever cut off.")]
        [Range(0f, 0.2f)][SerializeField] private float safeMargin = 0.06f;

        private void Reset()
        {
            image = GetComponentInChildren<Image>();
            fitter = GetComponentInChildren<AspectRatioFitter>();
        }

        private void OnEnable()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Apply() writes to an Image and an AspectRatioFitter, and touching a Graphic marks the
            // canvas dirty — which Unity refuses to do from inside OnValidate ("SendMessage cannot be
            // called during Awake, CheckConsistency, or OnValidate"). Defer a frame so the edit lands
            // outside the validation pass.
            UnityEditor.EditorApplication.delayCall += ApplyDeferred;
        }

        private void ApplyDeferred()
        {
            UnityEditor.EditorApplication.delayCall -= ApplyDeferred;
            // The object can be deleted, or a domain reload can happen, before the call arrives.
            if (this == null || !isActiveAndEnabled) return;
            Apply();
        }
#endif

        /// <summary>Recomputes the fitter from the sprite currently assigned.</summary>
        public void Apply()
        {
            if (image == null || fitter == null) return;

            Sprite sprite = image.sprite;
            if (sprite == null) return;

            Rect rect = sprite.rect;
            if (rect.height <= 0f) return;

            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = rect.width / rect.height;

            // preserveAspect would fight the fitter; the fitter already owns the shape.
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            if (scrim != null) scrim.raycastTarget = false;
        }

        /// <summary>How much vertical room the menu should leave untouched, in normalized units.</summary>
        public float SafeMargin => safeMargin;
    }
}
