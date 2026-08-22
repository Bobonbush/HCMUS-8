using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static Anomoly;

// Light Flickering manipulatation
public class Anomoly01 : MonoBehaviour, Anomoly
{

    [SerializeField]
    private List<GameObject> LightGameObject;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    Flickering lightObject = null;
    
    private float coolDownBeforeStart = 3.0f;
    private float coolDown = 10000.0f;
    public void Restore()
    {
        lightObject.Activate(false);
        lightObject = null;
        
    }

    public void Evaluate()
    {
        coolDown = 0.0f;
        int randomSeed = Random.Range(0, LightGameObject.Count);
        lightObject = LightGameObject[randomSeed].GetComponent<Flickering>();
        
    }

    private void Update()
    {
        if(coolDown > coolDownBeforeStart)
        {
            return;
        }
        coolDown += Time.deltaTime;
        if(coolDown > coolDownBeforeStart)
        {
            if(lightObject)
            {
                lightObject.Activate(true);
            }
        }
    }

    public EvaluateType getType()
    {
        return type;
    }



}
