using System.Collections.Generic;
using UnityEngine;
using System;

public class Anomoly08 : MonoBehaviour, Anomoly
{
    

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;


    [SerializeField]
    private List<GameObject> doors;

    [SerializeField]
    private List<AudioSource> audioSources;

    int currentIndexInUse = -1;

    public void Evaluate()
    {
        currentIndexInUse = UnityEngine.Random.Range(0, 2);
        doors[currentIndexInUse].transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        audioSources[currentIndexInUse].enabled = true;
    }

    public void Restore()
    {
        if (currentIndexInUse != -1)
        {
            doors[currentIndexInUse].transform.rotation = Quaternion.Euler(0.0f, -90.0f, 0.0f);
            audioSources[currentIndexInUse].enabled = false;
        }
        currentIndexInUse = -1;
    }



    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
