using System.Collections.Generic;
using UnityEngine;

public class Anomoly31 : MonoBehaviour, Anomoly
{




    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;


    [SerializeField]
    List<BoxCollider> TriggerSpawn;


    public void Evaluate()
    {
        FallingListener.Activated = false;
        foreach(BoxCollider box in TriggerSpawn)
        {
            box.enabled = true;
        }
    }

    public void Restore()
    {
        foreach (BoxCollider box in TriggerSpawn)
        {
            box.enabled = false;
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
