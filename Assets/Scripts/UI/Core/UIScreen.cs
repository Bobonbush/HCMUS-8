using System.Collections;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Base class for any full-screen menu surface (pause menu, options, main menu...).
    /// Wraps a CanvasGroup so screens can fade in/out and be toggled without enabling/disabling
    /// the GameObject (which would kill running coroutines). Background-agnostic on purpose:
    /// the same screen prefab can sit on the in-game pause canvas or the main-menu canvas.
    ///
    /// Fading uses unscaled time so it still animates while the game is paused.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0f;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;

        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            SetVisible(false, true);
        }

        public virtual void Open()
        {
            IsOpen = true;
            SetVisible(true);
            OnOpened();
        }

        public virtual void Close()
        {
            IsOpen = false;
            SetVisible(false);
            OnClosed();
        }

        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }

        private void SetVisible(bool visible, bool instant = false)
        {
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (instant || fadeDuration <= 0f)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                return;
            }

            fadeRoutine = StartCoroutine(Fade(visible ? 1f : 0f));
        }

        private IEnumerator Fade(float target)
        {
            float speed = 1f / fadeDuration;

            while (!Mathf.Approximately(canvasGroup.alpha, target))
            {
                canvasGroup.alpha = Mathf.MoveTowards(
                    canvasGroup.alpha,
                    target,
                    speed * Time.unscaledDeltaTime);

                yield return null;
            }

            canvasGroup.alpha = target;
            fadeRoutine = null;
        }
    }
}
