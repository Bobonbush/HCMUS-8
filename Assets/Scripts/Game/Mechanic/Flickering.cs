using UnityEngine;

public class Flickering : MonoBehaviour
{


    private Light lightComponent;

    private LightCondition lightCondition;



    private float minIntenstity = 0.5f;
    private float maxIntensity = 25f;

    private float flickerSpeed = 0.1f;

    private float flickerTiming = 0.0f;

    private bool active = false;

    void Start()
    {
        lightComponent = GetComponent<Light>();
        lightCondition = GetComponent<LightCondition>();
    }


    private void Update()
    {
        if (active == false) return;
        
        flickerTiming += Time.deltaTime;
        if(flickerTiming >= flickerSpeed)
        {
            flickerTiming -= flickerSpeed;
            Flicker();
        }
        
    }

    public void Activate(bool active)
    {
        this.active = active;
        if (active == false)
        {
            lightComponent.intensity = maxIntensity;
            lightCondition.TurnOn();
        }
    }

    private void Flicker()
    {
        float randomIntensity = Random.Range(minIntenstity, maxIntensity);
        if(randomIntensity <= (minIntenstity + maxIntensity) /4.0f)
        {
            lightCondition.TurnOffOnlyMesh();
        }else
        {
            lightCondition.TurnOnOnlyMesh();
        }

        lightComponent.intensity = randomIntensity;
    }

    
}
