using UnityEngine;

public class FallingListener : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    

    public static bool Activated = false;

    [SerializeField]
    private GameObject Figure;

    [SerializeField]
    private Transform FallPoint;
    void Activate()
    {
        if (Activated)
        {
            return;
        }
        Activated = true;
        Figure.transform.position = FallPoint.position;
        Figure.SetActive(true);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == 6)
        {
            Activate();
        }
    }
}
