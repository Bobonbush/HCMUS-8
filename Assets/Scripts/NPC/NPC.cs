using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NPC : MonoBehaviour
{
    [HideInInspector]
    public NavMeshAgent Agent;

    [SerializeField]
    Transform robospawn;

    [HideInInspector]
    public Animator animator;
    private NPCTrajectory trajectory;


    public float CurrentSpeed
    {
        get { return Agent.velocity.magnitude; }
    }
    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        trajectory = GetComponent<NPCTrajectory>();
    }

    public void SetSpeed(float speed)
    {
        Agent.speed = speed;
    }

    public float GetSpeed()
    {
        return Agent.speed;
    }

    public void Reset()
    {
        transform.position = robospawn.position;
        trajectory.Reset();
    }


}
