using System.Collections.Generic;
using UnityEngine;

// The school crest by the lifts belongs to a different university - the HCMUS
// logo is quietly replaced by UEH's. Same plaque, same size, same place on the
// wall; only the crest is wrong.
public class Anomoly25 : MonoBehaviour, Anomoly
{



    Anomoly.EvaluateType type = Anomoly.EvaluateType.EntireFloor;

    public void Evaluate()
    {
        GameManager.Instance.Flip();
    }

    public void Restore()
    {
        GameManager.Instance.Flip();
    }


    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
