using UnityEngine;
using System.Collections.Generic;
public class GameManager : MonoBehaviour
{


    [SerializeField] private GameObject FloorObject;
    [SerializeField] private Transform playerTransform;

    public static GameManager Instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private int CurrentFloor = 4;
    
    private float offset = 5.664f;
    
    private int maxFloor = 12;

    List<GameObject> createdFloor = new List<GameObject>() ;


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        LoadFloor();
    }

    public void LoadFloor()
    {
        for (int i = 1; i <= maxFloor; i++)
        {
            GameObject floor =  Instantiate(FloorObject);
            
            floor.transform.position = new Vector3(42.33f, (i - CurrentFloor) * offset, -99.65f);
            createdFloor.Add(floor);
        }
    }

    // should be one or minus one only
    private void ShiftAllFloorUpXUnit(int unit)
    {
        for(int i = 0; i < createdFloor.Count; i++)
        {
            Vector3 position = createdFloor[i].transform.position;
            position.y += offset * unit;
            createdFloor[i].transform.position = position;
        }  
    }

    private void MaintainInfinity()
    {
        float currentFloorY = createdFloor[CurrentFloor - 1].transform.position.y;

        if(currentFloorY + offset /1.5f < playerTransform.position.y )
        {
            ShiftAllFloorUpXUnit(1);
        }else if(currentFloorY - offset /1.5f > playerTransform.position.y)
        {
            ShiftAllFloorUpXUnit(-1);
        }

    }





    void Update()
    {
        MaintainInfinity();
    }
}
