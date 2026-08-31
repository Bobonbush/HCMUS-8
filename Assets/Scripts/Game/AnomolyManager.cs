using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using UnityEngine;

public class AnomolyManager : MonoBehaviour
{


    Anomoly currentAnomoly = null;

    
    public List<MonoBehaviour> anomolies;

    /*
     *
     *   Anomly generator here
     * 
    */


    public void RestoreAnomoly()
    {
        // No anomoly was generated this round (the dice in GameManager.NewMap can roll 0).
        if (currentAnomoly == null) return;

        if(currentAnomoly.getType() == Anomoly.EvaluateType.Global
            || currentAnomoly.getType() == Anomoly.EvaluateType.AddUp)
        {
            // AddUp evaluated on a neighbour floor, so restore has to reach every floor too.
            GameManager.Instance.ForceRestoreAll();
        }else
        {
            currentAnomoly.Restore();
            currentAnomoly = null;
        }


    }

    public void ForceRestore()
    {
        if (currentAnomoly != null)
        {
            currentAnomoly.Restore();
            currentAnomoly = null;
        }
    }



    public void ForceAnomoly(int index) {

        if (anomolies[index] is Anomoly anomoly)
        {
            currentAnomoly = anomoly;
        }
        currentAnomoly.Evaluate();
    }


    public void GenerateAnomoly()
    {
        

        int size = anomolies.Count;
        int randomMize = Random.Range(0, size);

        if (anomolies[randomMize] is Anomoly anomoly)
        {

            if (anomoly.getType() == Anomoly.EvaluateType.Global)
            {
                GameManager.Instance.ForceAnomolyAll(randomMize);
            }
            else if (anomoly.getType() == Anomoly.EvaluateType.AddUp)
            {
                currentAnomoly = anomoly;
                int direction = Random.Range(0, 2) == 0 ? 1 : -1;
                GameManager.Instance.ForceAnomoly(direction, randomMize);
            }
            else
            {
                currentAnomoly = anomoly;
                currentAnomoly.Evaluate();
            }
        }

    }
}
