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
        active = true;
        Update();
    }
    private void Update()
    {
        if (!active || alternateAnimation == null) return;
        string clip = agent != null && agent.velocity.sqrMagnitude > 0.02f ? walkClip : idleClip;
        if (!string.IsNullOrEmpty(clip) && alternateAnimation.GetClip(clip) != null && !alternateAnimation.IsPlaying(clip))
            alternateAnimation.CrossFade(clip, 0.15f);
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
    }
    public Anomoly.EvaluateType getType() { return Anomoly.EvaluateType.NPCInvolve; }
}
