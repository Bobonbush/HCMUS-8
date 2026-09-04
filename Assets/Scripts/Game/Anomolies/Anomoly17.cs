using UnityEngine;
using UnityEngine.AI;

// Swap visuals only: preserve the sole NPC's navigation, collider and behaviour.
public class Anomoly17 : MonoBehaviour, Anomoly
{
    public GameObject npc;
    public Renderer[] normalRenderers;
    public GameObject alternateAppearance;
    public Animation alternateAnimation;
    public string idleClip;
    public string walkClip;
    [Min(0.1f)] public float referenceWalkSpeed = 1.4f;
    private string currentClip;
    private float smoothSpeed;
    private bool[] rendererStates;
    private bool wasActive;
    private bool alternateWasActive;
    private bool active;
    private NavMeshAgent agent;
    public void Evaluate()
    {
        if (active || npc == null || alternateAppearance == null) return;
        wasActive = npc.activeSelf;
        alternateWasActive = alternateAppearance.activeSelf;
        normalRenderers = normalRenderers ?? new Renderer[0];
        rendererStates = new bool[normalRenderers.Length];
        for (int i = 0; i < normalRenderers.Length; i++)
            if (normalRenderers[i] != null) { rendererStates[i] = normalRenderers[i].enabled; normalRenderers[i].enabled = false; }
        npc.SetActive(true);
        alternateAppearance.SetActive(true);
        agent = npc.GetComponent<NavMeshAgent>();
        if (alternateAnimation != null)
        {
            alternateAnimation.enabled = true;
            alternateAnimation.cullingType = AnimationCullingType.AlwaysAnimate;
        }
        currentClip = null;
        smoothSpeed = 0;
        active = true;
        Update();
    }
    private void Update()
    {
        if (!active || alternateAnimation == null) return;
        var velocity = agent != null ? agent.velocity : Vector3.zero;
        velocity.y = 0;
        smoothSpeed = Mathf.Lerp(smoothSpeed, velocity.magnitude, 1 - Mathf.Exp(-12 * Time.deltaTime));
        // Hysteresis prevents restarting the step cycle as navigation settles at a waypoint.
        bool walking = velocity.magnitude > (currentClip == walkClip ? 0.04f : 0.12f);
        string clip = walking ? walkClip : idleClip;
        if (!string.IsNullOrEmpty(clip) && alternateAnimation.GetClip(clip) != null)
        {
            if (currentClip != clip)
            {
                alternateAnimation.CrossFade(clip, 0.2f);
                currentClip = clip;
            }
            // Root translation belongs to the NavMeshAgent; the in-place cycle supplies the steps.
            alternateAnimation[clip].speed = walking
                ? Mathf.Clamp(smoothSpeed / referenceWalkSpeed, 0.15f, 2.5f) : 1;
        }
    }
    public void Restore()
    {
        if (!active) return;
        for (int i = 0; i < normalRenderers.Length; i++)
            if (normalRenderers[i] != null) normalRenderers[i].enabled = rendererStates[i];
        if (alternateAnimation != null) alternateAnimation.Stop();
        if (alternateAppearance != null) alternateAppearance.SetActive(alternateWasActive);
        if (npc != null) npc.SetActive(wasActive);
        active = false;
        currentClip = null;
    }
    public Anomoly.EvaluateType getType() { return Anomoly.EvaluateType.NPCInvolve; }
}
