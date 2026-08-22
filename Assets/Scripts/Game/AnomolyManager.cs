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
        
        if(currentAnomoly.getType() == Anomoly.EvaluateType.Global)
        {
            GameManager.Instance.ForceRestoreAll();
        }else
        {
            if (currentAnomoly != null)
            {
                currentAnomoly.Restore();
            }
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
            else
            {
                currentAnomoly = anomoly;
                currentAnomoly.Evaluate();
            }
        }

    }
}
