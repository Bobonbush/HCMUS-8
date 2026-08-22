using UnityEngine;

public class LiftRoom : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    private int anomolyDoor = 0;


    private int collisionCnt = 0;

    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            GameManager.Instance.QueryEnter(anomolyDoor);
            collisionCnt++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        collisionCnt--;
    }
}
