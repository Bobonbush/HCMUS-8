using System.Collections.Generic;
using UnityEngine;

public class Anomoly32 : MonoBehaviour, Anomoly
{



    Anomoly.EvaluateType type = Anomoly.EvaluateType.NPCInvolve;



    public float speed = 3.0f;
    private float defaultSpeed = 0.0f;

    [SerializeField]
    private GameObject Agent;

    private NPCTrajectory trajectory;
    private NPCChasing chasing;
    private NPC npc;

    private void Awake()
    {
        npc = Agent.GetComponent<NPC>();
        chasing = Agent.GetComponent<NPCChasing>();
        trajectory = Agent.GetComponent<NPCTrajectory>();
    }

    public void Evaluate()
    {

        defaultSpeed = npc.GetSpeed();
        trajectory.enabled = false;
        chasing.enabled = true;
        npc.SetSpeed(speed);

        Invoke(nameof(EnableChase), 1.0f);
    }

    private void EnableChase()
    {
        Agent.SetActive(true);
    }

    public void Restore()
    {
        npc.SetSpeed(defaultSpeed);
        trajectory.enabled = true;
        chasing.enabled = false;

        // Restore automaticially
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
