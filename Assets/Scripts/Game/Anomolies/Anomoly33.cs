using System.Collections.Generic;
using UnityEngine;

public class Anomoly33 : MonoBehaviour, Anomoly
{



    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;



    [SerializeField]
    A33Trigger trigger;



    public void Evaluate()
    {
        trigger.enabled = true;
        
    }



    public void Restore()
    {
        if(trigger.enabled)
        {
            trigger.Restore();
            trigger.enabled = false;
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
