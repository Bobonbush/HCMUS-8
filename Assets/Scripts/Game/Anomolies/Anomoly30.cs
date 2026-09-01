using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using System.Collections.Generic;

// You walk into the left lift and come out of the right one (or vice versa).
// While active, the moment the player is inside a lift cabin they are mirrored
// across the plane between the two cabins - unnoticeable from inside, and then
// they step out of the wrong lift.
//
// Both LiftRoom trigger colliders are disabled around the swap and only re-enabled
// once the player has left both boxes, so the mirror can never re-fire QueryEnter.
public class Anomoly30 : MonoBehaviour, Anomoly
{
    Anomoly.EvaluateType type = Anomoly.EvaluateType.NPCInvolve;

    [SerializeField]
    private GameObject Agent;

    private NPCTrajectory trajectory;
    private NPC npc;


    [SerializeField]
    List<Transform> customPoints;

    private void Awake()
    {
        npc = Agent.GetComponent<NPC>();
        trajectory = Agent.GetComponent<NPCTrajectory>();
        
    }

    bool restored = true;
    public void Evaluate()
    {
        restored = false;
        
        Invoke(nameof(EnableChase), 3.0f);
    }

    private void EnableChase()
    {
        if(restored)
        {
            return;
        }
        trajectory.MoveCustomPoints(customPoints);
        Agent.SetActive(true);
    }


    public void Restore()
    {
        restored = true;


        // Reset by default on anomoly manager 


    }

    


    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
