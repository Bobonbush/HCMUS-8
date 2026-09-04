using UnityEngine;
using UnityEngine.AI;

// Visual-only teacher gait; patrol and anomaly behaviours still own navigation.
public class NPCVisualAnimator : MonoBehaviour
{
    public Animation motion;
    public NavMeshAgent agent;
    public Renderer visual;
    public float referenceWalkSpeed = 1.4f;
    string current;
    float speed;
    void OnEnable() { current = null; speed = 0; }
    void Update()
    {
        if (motion == null || agent == null) return;
        if (visual != null && !visual.enabled) { motion.Stop(); current = null; return; }
        Vector3 velocity = agent.velocity; velocity.y = 0;
        speed = Mathf.Lerp(speed, velocity.magnitude, 1 - Mathf.Exp(-12 * Time.deltaTime));
        bool walking = velocity.magnitude > (current == "Walk" ? 0.04f : 0.12f);
        string next = walking ? "Walk" : "Idle";
        if (motion[next] == null) return;
        if (current != next) { motion.CrossFade(next, 0.2f); current = next; }
        motion[next].speed = walking ? Mathf.Clamp(speed / Mathf.Max(0.1f,referenceWalkSpeed),0.15f,3f) : 1;
    }
    void OnDisable() { if (motion != null) motion.Stop(); }
}
