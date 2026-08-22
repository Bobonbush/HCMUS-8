using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static Anomoly;

// Light Flickering manipulatation
public class Anomoly02 : MonoBehaviour, Anomoly
{

    [SerializeField]
    private List<GameObject> LightGameObject;
    Anomoly.EvaluateType type = Anomoly.EvaluateType.Global;

    private float coolDownBeforeStart = 3.0f;
    private float coolDown = 10000.0f;
    public void Restore()
    {
        TurnAllLight(true);
    }

    public void Evaluate()
    {
        coolDown = 0.0f;
        coolDownBeforeStart = Random.Range(5.0f, 10.0f);

    }


    private void TurnAllLight(bool on)
    {
        foreach(GameObject lightObject in LightGameObject)
        {
            LightCondition lightCondition = lightObject.GetComponent<LightCondition>();
            if(on)
            {
                lightCondition.TurnOn();
            }else
            {
                lightCondition.TurnOff();
            }
        }
    }

    

    private void Update()
    {
        if(coolDown > coolDownBeforeStart)
        {
            return;
        }
        coolDown += Time.deltaTime;

        if (coolDown > coolDownBeforeStart)
        {
            TurnAllLight(false);
        }

    }

    public EvaluateType getType()
    {
        return type;
    }




}
