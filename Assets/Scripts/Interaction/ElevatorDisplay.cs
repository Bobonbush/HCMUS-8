using TMPro;
using UnityEngine;

public class ElevatorDisplay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    TextMeshPro text;
    private float timing = 0.0f;
    
    [SerializeField]
    private float timeMax = 2.0f;

    [HideInInspector]
    public int setAnomoly = 0;
    int state = 0;
    
    void Start()
    {
        text = GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        timing -= Time.deltaTime;
        if(timing > 0)
        {
            return;
        }
        timing = timeMax;

        state ^= 1;

        if (GameManager.Instance != null)
        {

            text.text = "" + (GameManager.Instance.GetCurrentFloor() + setAnomoly);
        }else
        {
            text.text = "" + (8 + setAnomoly);
        }
        text.enabled = (state > 0);
    }
}
