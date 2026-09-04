using UnityEngine;
using UnityEngine.AI;

// Patrol normally until a close encounter, then follow at the same walking speed.
public class Anomoly15 : MonoBehaviour, Anomoly
{
    [Tooltip("The floor NPC managed by AnomolyManager.")]
    public GameObject agentObject;
    public float followSpeed = 1.75f;
    [Min(0.1f)] public float detectionRadius = 3.5f;
    private bool following;
    public float stoppingDistance = 1.7f;
    public float repathInterval = 0.2f;

    private readonly Anomoly.EvaluateType type = Anomoly.EvaluateType.NPCInvolve;
    private NavMeshAgent navAgent;
    private NPCTrajectory trajectory;
    private NPCChasing chasing;
    private bool active;
    private bool captured;
    private bool wasActive;
    private bool trajectoryWasEnabled;
    private bool chasingWasEnabled;
    private float originalSpeed;
    private float originalStoppingDistance;
    private bool originalStopped;
    private UnityEngine.AI.NavMeshPath originalPath;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float nextRepath;

    public void Evaluate()
    {
        if (agentObject == null || active) return;
        CacheComponents();
        CaptureState();
        active = true;
        agentObject.SetActive(true);
        following = false;
        if (trajectory != null) trajectory.enabled = true;
        if (chasing != null) chasing.enabled = false;
        if (navAgent != null)
        {
            navAgent.speed = followSpeed;
            if (navAgent.isOnNavMesh) navAgent.isStopped = false;
        }
        nextRepath = 0f;
    }

    public void Restore()
    {
        active = false;
        following = false;
        if (agentObject == null || !captured) return;
        CacheComponents();
        if (navAgent != null)
        {
            if (navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
                navAgent.Warp(originalPosition);
                if (originalPath != null && originalPath.corners.Length > 1) navAgent.SetPath(originalPath);
                navAgent.isStopped = originalStopped;
            }
            else agentObject.transform.position = originalPosition;
            navAgent.speed = originalSpeed;
            navAgent.stoppingDistance = originalStoppingDistance;
        }
        agentObject.transform.rotation = originalRotation;
        if (trajectory != null) trajectory.enabled = trajectoryWasEnabled;
        if (chasing != null) chasing.enabled = chasingWasEnabled;
        agentObject.SetActive(wasActive);
        captured = false;
    }

    private void Update()
    {
        if (!active || agentObject == null || GameManager.Instance == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null) return;

        CacheComponents();
        if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh) return;

        navAgent.speed = followSpeed;
        if (!following)
        {
            Vector3 origin = agentObject.transform.position + Vector3.up;
            Vector3 target = player.position + Vector3.up;
            if ((player.position - agentObject.transform.position).sqrMagnitude > detectionRadius * detectionRadius
                || !HasLineOfSight(target, origin - target)) return;
            following = true;
            if (trajectory != null) trajectory.enabled = false;
            navAgent.stoppingDistance = stoppingDistance;
        }

        navAgent.isStopped = false;
        if (Time.time < nextRepath) return;
        nextRepath = Time.time + repathInterval;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(player.position, out hit, 2f, NavMesh.AllAreas))
            navAgent.SetDestination(hit.position);
    }

    private bool HasLineOfSight(Vector3 eyePosition, Vector3 toNpc)
    {
        float distance = toNpc.magnitude;
        if (distance <= 0.01f) return true;
        RaycastHit hit;
        if (!Physics.Raycast(eyePosition, toNpc / distance, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
            return true;
        return hit.transform == agentObject.transform || hit.transform.IsChildOf(agentObject.transform);
    }

    private void CacheComponents()
    {
        if (agentObject == null) return;
        if (navAgent == null) navAgent = agentObject.GetComponent<NavMeshAgent>();
        if (trajectory == null) trajectory = agentObject.GetComponent<NPCTrajectory>();
        if (chasing == null) chasing = agentObject.GetComponent<NPCChasing>();
    }

    private void CaptureState()
    {
        if (captured) return;
        captured = true;
        wasActive = agentObject.activeSelf;
        originalPosition = agentObject.transform.position;
        originalRotation = agentObject.transform.rotation;
        trajectoryWasEnabled = trajectory != null && trajectory.enabled;
        chasingWasEnabled = chasing != null && chasing.enabled;
        if (navAgent != null)
        {
            originalSpeed = navAgent.speed;
            originalStoppingDistance = navAgent.stoppingDistance;
            originalStopped = navAgent.isOnNavMesh && navAgent.isStopped;
            originalPath = navAgent.isOnNavMesh && navAgent.hasPath ? navAgent.path : null;
        }
    }

    public Anomoly.EvaluateType getType() { return type; }
}
