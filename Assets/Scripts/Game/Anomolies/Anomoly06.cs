using UnityEngine;

// Looking into the toilet mirror, something is looking back at you. The figure
// starts sunk into the wall behind the glass; when the player walks up to the
// mirror it slowly pushes out through the surface until it sits pressed against
// the glass - then it lunges straight at the camera, filling the screen for a
// beat before vanishing. One scare per evaluation.
public class Anomoly06 : MonoBehaviour, Anomoly
{

    [Tooltip("The figure inside the mirror. Hidden until Evaluate.")]
    public ShadowFigure figure;

    [Header("Emergence")]
    [Tooltip("Player distance to the mirror that triggers the emergence.")]
    public float revealDistance = 2.4f;
    [Tooltip("Seconds for the figure to slide fully out of the mirror.")]
    public float emergeDuration = 4f;
    [Tooltip("Offset (in the figure parent's space) the figure hides at before emerging - just far enough behind the glass to be occluded. Keep it shallow: everything deeper than the glass is invisible travel. Set by the wiring tooling.")]
    public Vector3 emergeOffset = new Vector3(0f, 0f, 0.08f);

    [Header("Jumpscare")]
    [Tooltip("Pause between full emergence and the lunge.")]
    public float scareDelay = 0.35f;
    [Tooltip("Seconds the lunge takes to reach the camera.")]
    public float lungeDuration = 0.22f;
    [Tooltip("Seconds the figure holds glued to the screen.")]
    public float lungeHold = 0.4f;
    [Tooltip("Distance in front of the camera the figure flies to.")]
    public float lungeDistance = 0.65f;
    [Tooltip("Scale multiplier at the end of the lunge.")]
    public float lungeScale = 2.2f;
    [Tooltip("Scream played as the figure lunges. Kept quiet on purpose.")]
    public AudioClip scareClip;
    [Range(0f, 1f)] public float scareVolume = 0.35f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    // 0 waiting for the player, 1 emerging, 2 emerged (waiting to pounce),
    // 3 lunging, 4 holding on screen, 5 done (vanished)
    private int phase;
    private bool active;
    private float progress;         // emergence 0..1
    private float phaseTimer;
    private Vector3 lungeFrom;

    private Vector3 shownPos;       // authored pose = fully emerged
    private Quaternion baseRot;
    private Vector3 baseScale;
    private bool basePoseCaptured;

    private FirstPersonCameraFeel cameraFeel;
    private AudioSource scareSource;

    public void Evaluate()
    {
        if (figure == null) return;
        CaptureBasePose();
        active = true;
        phase = 0;
        progress = 0f;
        phaseTimer = 0f;
        // Visible but parked behind the glass; the wall and mirror backing occlude it,
        // so nothing shows until the emergence slides it through the surface.
        ResetFigurePose();
        figure.transform.localPosition = shownPos + emergeOffset;
        figure.Show(true);
    }

    public void Restore()
    {
        active = false;
        phase = 0;
        progress = 0f;
        phaseTimer = 0f;
        if (figure == null) return;
        CaptureBasePose();
        ResetFigurePose();
        figure.Show(false);
        if (scareSource != null) scareSource.Stop();
    }

    private void CaptureBasePose()
    {
        if (basePoseCaptured) return;
        shownPos = figure.transform.localPosition;
        baseRot = figure.transform.localRotation;
        baseScale = figure.transform.localScale;
        basePoseCaptured = true;
    }

    private void ResetFigurePose()
    {
        figure.transform.localPosition = shownPos;
        figure.transform.localRotation = baseRot;
        figure.transform.localScale = baseScale;
    }

    private void Update()
    {
        // 12 floor instances tick this every frame - bail before any real work.
        if (!active || figure == null || phase >= 5) return;

        Transform player = GameManager.Instance != null ? GameManager.Instance.GetPlayerTransform() : null;
        if (player == null) return;
        float playerDistance = Vector3.Distance(player.position, figure.transform.position);

        if (phase == 0)
        {
            if (playerDistance > revealDistance) return;
            phase = 1;
        }

        if (phase == 1)
        {
            progress = Mathf.Min(1f, progress + Time.deltaTime / Mathf.Max(0.1f, emergeDuration));
            float eased = progress * progress * (3f - 2f * progress);   // smoothstep: slow start, slow settle
            Vector3 pos = shownPos + emergeOffset * (1f - eased);
            // A faint shudder while it pushes through the glass, gone once it is out.
            pos.x += Mathf.Sin(Time.time * 27f) * 0.004f * Mathf.Sin(progress * Mathf.PI);
            figure.transform.localPosition = pos;
            if (progress >= 1f) { phase = 2; phaseTimer = 0f; }
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        if (phase == 2)
        {
            // Pounce only while the player is actually near the mirror; otherwise it
            // stays pressed against the glass, waiting.
            if (playerDistance > revealDistance + 0.8f) return;
            phaseTimer += Time.deltaTime;
            if (phaseTimer < scareDelay) return;
            phase = 3;
            phaseTimer = 0f;
            lungeFrom = figure.transform.position;
            if (cameraFeel == null) cameraFeel = FindFirstObjectByType<FirstPersonCameraFeel>();
            if (cameraFeel != null && cameraFeel.enabled) cameraFeel.ExternalImpulse(0.6f, 4f);
            if (scareClip != null)
            {
                if (scareSource == null)
                {
                    scareSource = gameObject.AddComponent<AudioSource>();
                    scareSource.playOnAwake = false;
                    scareSource.spatialBlend = 0f;   // right in your face, not positional
                    scareSource.outputAudioMixerGroup = Game.UI.SettingsService.FindMixerGroup("Sfx");
                }
                scareSource.PlayOneShot(scareClip, scareVolume);
            }
        }

        // Where "filling the screen" is this frame; recomputed so it tracks the view.
        Vector3 screenPoint = cam.transform.position + cam.transform.forward * lungeDistance
                            - cam.transform.up * 0.04f;
        Quaternion faceCam = Quaternion.LookRotation(cam.transform.position - screenPoint, cam.transform.up);

        if (phase == 3)
        {
            phaseTimer += Time.deltaTime;
            float t = Mathf.Clamp01(phaseTimer / Mathf.Max(0.05f, lungeDuration));
            t = t * t;                                   // accelerating rush
            figure.transform.position = Vector3.Lerp(lungeFrom, screenPoint, t);
            figure.transform.rotation = Quaternion.Slerp(figure.transform.rotation, faceCam, t);
            figure.transform.localScale = baseScale * Mathf.Lerp(1f, lungeScale, t);
            if (t >= 1f) { phase = 4; phaseTimer = 0f; }
            return;
        }

        if (phase == 4)
        {
            // Glued to the camera so it stays fullscreen even if the view whips around.
            figure.transform.position = screenPoint;
            figure.transform.rotation = faceCam;
            phaseTimer += Time.deltaTime;
            if (phaseTimer >= lungeHold)
            {
                phase = 5;
                ResetFigurePose();
                figure.Show(false);                      // gone - the mirror is empty again
                // The scream clip is much longer than the scare; let its tail ring
                // briefly, then fade it out instead of cutting or playing on forever.
                if (scareSource != null && scareSource.isPlaying) StartCoroutine(FadeOutScream());
            }
        }
    }

    private System.Collections.IEnumerator FadeOutScream()
    {
        yield return new WaitForSeconds(1.2f);
        float t = 0f;
        while (t < 0.8f && scareSource != null && scareSource.isPlaying)
        {
            t += Time.deltaTime;
            scareSource.volume = Mathf.Lerp(1f, 0f, t / 0.8f);
            yield return null;
        }
        if (scareSource != null)
        {
            scareSource.Stop();
            scareSource.volume = 1f;
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
