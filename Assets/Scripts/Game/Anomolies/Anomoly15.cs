using UnityEngine;
using UnityEngine.AI;

// The floor's only NPC follows the player, but freezes whenever the player looks
// at it. This uses the existing NavMeshAgent and does not allocate every frame.
public class Anomoly15 : MonoBehaviour, Anomoly
{
    [Tooltip("The floor NPC managed by AnomolyManager.")]
    public GameObject agentObject;
    public float followSpeed = 2.4f;
    [Range(10f, 160f)] public float noticeAngle = 72f;
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
        if (trajectory != null) trajectory.enabled = false;
        if (chasing != null) chasing.enabled = false;
        if (navAgent != null)
        {
            navAgent.speed = followSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            if (navAgent.isOnNavMesh) navAgent.isStopped = false;
        }
        nextRepath = 0f;
    }

    public void Restore()
    {
        active = false;
        if (agentObject == null || !captured) return;
        CacheComponents();
        if (navAgent != null)
        {
            if (navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
                navAgent.isStopped = true;
                navAgent.Warp(originalPosition);
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

        Transform eyes = Camera.main != null ? Camera.main.transform : player;
        Vector3 toNpc = agentObject.transform.position - eyes.position;
        bool insideViewCone = Vector3.Angle(eyes.forward, toNpc) <= noticeAngle * 0.5f;
        bool visible = insideViewCone && HasLineOfSight(eyes.position, toNpc);
        if (visible)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
            FacePlayer(player.position);
            return;
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

    private void FacePlayer(Vector3 playerPosition)
    {
        Vector3 direction = playerPosition - agentObject.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            agentObject.transform.rotation = Quaternion.LookRotation(direction);
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
        }
    }

    public Anomoly.EvaluateType getType() { return type; }
}
