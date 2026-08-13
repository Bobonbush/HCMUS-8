using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Freezes the last frame of gameplay behind the pause menu and blurs it, the way Exit 8 does.
    ///
    /// No shader and no renderer feature: the frame is captured once, then bounced through a few
    /// progressively smaller RenderTextures. Each step halves the size with bilinear filtering, which
    /// averages four pixels into one — do that three or four times and the result is a genuine blur.
    /// Drawing the small texture back at full size finishes the job, because the upscale is bilinear
    /// too. Total cost is a handful of blits on the single frame the player pauses, and nothing at
    /// all while the menu is open, since the game is frozen and the image never changes.
    ///
    /// The capture has to happen before the menu is drawn, so <see cref="Capture"/> waits for the end
    /// of the current frame and calls back afterwards. That costs one frame between pressing Escape
    /// and seeing the menu, which reads as intentional rather than as lag.
    /// </summary>
    public class PauseBlur : MonoBehaviour
    {
        [Tooltip("Where the frozen frame is drawn. Should sit behind the menu and fill the screen.")]
        [SerializeField] private RawImage target;

        [Tooltip("How many halvings to apply. Each one roughly doubles the softness.")]
        [Range(1, 6)][SerializeField] private int downsamples = 4;

        [Tooltip("Never shrink below this width, or the image turns to mush at low resolutions.")]
        [SerializeField] private int minimumWidth = 96;

        private RenderTexture blurred;

        private void Awake()
        {
            if (target == null) target = GetComponent<RawImage>();
            if (target != null) target.enabled = false;
        }

        private void OnDisable()
        {
            Release();
        }

        private void OnDestroy()
        {
            Release();
        }

        /// <summary>
        /// Grabs the screen at the end of this frame, blurs it, shows it, then calls
        /// <paramref name="onReady"/>. The callback runs even if capturing is impossible, so a caller
        /// can always rely on it.
        /// </summary>
        public void Capture(Action onReady)
        {
            if (target == null || !isActiveAndEnabled)
            {
                onReady?.Invoke();
                return;
            }

            StartCoroutine(CaptureRoutine(onReady));
        }

        /// <summary>Hides the frozen frame and frees the texture.</summary>
        public void Clear()
        {
            if (target != null) target.enabled = false;
            Release();
        }

        private IEnumerator CaptureRoutine(Action onReady)
        {
            // Wait for the frame to finish drawing; the menu is still hidden at this point, so it
            // does not end up baked into its own backdrop.
            yield return new WaitForEndOfFrame();

            Render();
            onReady?.Invoke();
        }

        private void Render()
        {
            Release();

            int width = Mathf.Max(2, Screen.width);
            int height = Mathf.Max(2, Screen.height);

            RenderTexture full = RenderTexture.GetTemporary(width, height, 0);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(full);

            RenderTexture current = full;
            for (int i = 0; i < downsamples; i++)
            {
                int nextWidth = Mathf.Max(minimumWidth, current.width / 2);
                int nextHeight = Mathf.Max(2, current.height / 2);
                if (nextWidth >= current.width) break;

                RenderTexture smaller = RenderTexture.GetTemporary(nextWidth, nextHeight, 0);
                smaller.filterMode = FilterMode.Bilinear;
                Graphics.Blit(current, smaller);

                if (current != full) RenderTexture.ReleaseTemporary(current);
                current = smaller;
            }

            // Keep the final small texture; everything else was scratch.
            blurred = new RenderTexture(current.width, current.height, 0)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Graphics.Blit(current, blurred);

            if (current != full) RenderTexture.ReleaseTemporary(current);
            RenderTexture.ReleaseTemporary(full);

            target.texture = blurred;
            // CaptureScreenshotIntoRenderTexture hands back a bottom-up image on the platforms where
            // textures start at the top, so flip it through the UV rect rather than a blit material.
            target.uvRect = SystemInfo.graphicsUVStartsAtTop
                ? new Rect(0f, 1f, 1f, -1f)
                : new Rect(0f, 0f, 1f, 1f);
            target.enabled = true;
        }

        private void Release()
        {
            if (blurred == null) return;
            if (target != null && ReferenceEquals(target.texture, blurred)) target.texture = null;
            blurred.Release();
            Destroy(blurred);
            blurred = null;
        }
    }
}
