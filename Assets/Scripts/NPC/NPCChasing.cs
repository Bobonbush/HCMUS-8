using System;
using UnityEngine;
using UnityEngine.AI;

public class NPCChasing : NPCComponent
{
    [SerializeField]
    private Transform playerTransform;

    float timeIdle = -5.0f;
    public float maxtimeIdle = 6.0f;


    bool hasArrived()
    {
        return npc.Agent.remainingDistance <= npc.Agent.stoppingDistance;
    }

    void Update()
    {
        timeIdle -= Time.deltaTime;
        if(timeIdle > 0.0f)
        {
            return;
        }

        if(playerTransform == null)
        {
            playerTransform = GameManager.Instance.GetPlayerTransform();
        }



        SetDestination();
    }

    public void WaitForSeconds(float t)
    {
        timeIdle = t;
    }

    void SetDestination()
    {
        Vector3 randomPoint = playerTransform.position;

        NavMeshHit hit;
        Vector3 finalPosition = transform.position;
        if (NavMesh.SamplePosition(randomPoint, out hit, 2f, 1))
        {
            finalPosition = hit.position;
        }
        npc.Agent.SetDestination(finalPosition);
    }
}
