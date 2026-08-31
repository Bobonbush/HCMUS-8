using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
public class NPCTrajectory : NPCComponent
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    List<Transform> points;

    int index = 0;
    private void Start()
    {
        SetDestination();
    }

    bool hasArrived()
    {
        return npc.Agent.remainingDistance <= npc.Agent.stoppingDistance;
    }

    void SetDestination()
    {
        if (index >= points.Count) return;
        Vector3 randomPoint = points[index].position;

        NavMeshHit hit;
        Vector3 finalPosition = transform.position;
        if(NavMesh.SamplePosition(randomPoint, out hit, 2f, 1))
        {
            finalPosition = hit.position;
        }
        npc.Agent.SetDestination(finalPosition);
        index++;
    }

    // Update is called once per frame
    void Update()
    {
        if(hasArrived())
        {
            SetDestination();
        }    
    }

    public void Reset()
    {
        index = 0;
        //SetDestination();
    }
}
