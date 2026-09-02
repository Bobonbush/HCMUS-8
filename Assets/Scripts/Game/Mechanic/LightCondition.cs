using UnityEngine;



// Custom Light Turn On and Turn off due to Mesh Rendering
public class LightCondition : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Light lightComponent;
    [SerializeField] private GameObject MeshLight;

    private Flickering flicker;


    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        flicker = GetComponent<Flickering>();
    }
    
    public void TurnOff()
    {
        flicker.enabled = false;
        lightComponent.enabled = false;

        MeshLight.SetActive(false);
    }

    public void TurnOn()
    {
        flicker.enabled = true;
        lightComponent.enabled = true;

        MeshLight.SetActive(true);
    }

    public void TurnOffOnlyMesh()
    {
        lightComponent.enabled = true;
        MeshLight.SetActive(false);
    }

    public void TurnOnOnlyMesh()
    {
        lightComponent.enabled = true;
        MeshLight.SetActive(true);
    }
}
