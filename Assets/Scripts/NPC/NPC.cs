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

    private Vector3 resetPosition = Vector3.zero;

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
        resetPosition = transform.localPosition;
    }

    public void Reset()
    {
        transform.position = robospawn.position;
        trajectory.Reset();
    }


}
