using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using UnityEngine;

public class AnomolyManager : MonoBehaviour
{


    Anomoly currentAnomoly = null;

    [SerializeField]
    private GameObject npc;

    
    public List<MonoBehaviour> anomolies;


    public static List<int> usingAnomolies = new List<int>();

    private static int anomolizeSize;

    private Anomoly.EvaluateType _currentType;

    public Anomoly.EvaluateType currentType {
        private set
        {
            _currentType = value;
        }

        get
        {
            return _currentType;
        }
    }
    /*
     *
     *   Anomly generator here
     * 
    */

    private void Start()
    {
        anomolizeSize = anomolies.Count;
        if (usingAnomolies.Count == 0) {
            ResetUsingAnomolies();
        }
        RestoreNPC();
    }

    private static void ResetUsingAnomolies()
    {
        for(int i = 0; i < anomolizeSize; i++)
        {
            usingAnomolies.Add(i);
        }
    }


    public void RestoreNPC()
    {
        npc.GetComponent<NPC>().Reset();
        DisableNPC();
    }

    public void ActiveNPC()
    {
        
        Invoke(nameof(EnableNPC), 3.0f);

    }

    private void DisableNPC()
    {
        npc.SetActive(false);
    }

    private void EnableNPC()
    {
        npc.SetActive(true);
    }



    public void RestoreAnomoly()
    {
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
            currentType = Anomoly.EvaluateType.Single;
        }


    }

    public void ForceRestore()
    {
        if (currentAnomoly != null)
        {
            currentAnomoly.Restore();
            currentAnomoly = null;
        }
        currentType = Anomoly.EvaluateType.Single;
    }



    public void ForceAnomoly(int index) {
        if (index < 0 || index >= anomolies.Count || !(anomolies[index] is Anomoly anomoly)) return;
        // Never lose the reference to a still-running follow/chase anomaly.
        ForceRestore();
        currentAnomoly = anomoly;
        currentType = anomoly.getType();
        currentAnomoly.Evaluate();
    }

    public static void Correct(int ano)
    {
        usingAnomolies.Remove(ano);
        if(usingAnomolies.Count == 0 )
        {
            ResetUsingAnomolies();
        }
    }


    public int GenerateAnomoly(int prevv = -1)
    {
        ForceRestore();
        

        int size = usingAnomolies.Count;
        int randomMized = Random.Range(0, size);
        int randomMize = usingAnomolies[randomMized];

        Debug.Log("Anomoly #" + randomMize);


        
        if (anomolies[randomMize] is Anomoly anomoly)
        {

            currentType = anomoly.getType();
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

        return randomMize;

    }
}
