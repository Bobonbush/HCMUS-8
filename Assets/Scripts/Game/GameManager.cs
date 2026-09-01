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

    List<int> DecisionPoints = new List<int>(){ 1, 1 };

    List<GameObject> createdFloor = new List<GameObject>() ;

    public bool AlwaysAnomoly = false;
    private int anomoly_flag = 0;

    private bool infinityMode = true;

    private bool sleepQuery = false;


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
        NewMap();
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

        if(currentFloorY + offset / 2.0f < playerTransform.position.y )
        {
            ShiftAllFloorUpXUnit(1);
        }else if(currentFloorY - offset /2.0f > playerTransform.position.y)
        {
            ShiftAllFloorUpXUnit(-1);
        }
    }

    private void RestoreAnomoly()
    {
        createdFloor[CurrentFloor - 1].GetComponent<AnomolyManager>().RestoreAnomoly();
    }
    private void EvaluateAnomoly()
    {
        createdFloor[CurrentFloor - 1].GetComponent<AnomolyManager>().GenerateAnomoly();
    }

    private void RestoreNPC()
    {
        createdFloor[CurrentFloor - 1].GetComponent<AnomolyManager>().RestoreNPC();
    }

    private void EvaluateNPC()
    {
        if (createdFloor[CurrentFloor - 1].GetComponent<AnomolyManager>().currentType == Anomoly.EvaluateType.NPCInvolve)
        {
            return;
        }
        createdFloor[CurrentFloor - 1].GetComponent<AnomolyManager>().ActiveNPC();
    }


    private void NewMap()
    {
        // Generate Anomoly here
        int total = 0;
        for(int i = 0; i < DecisionPoints.Count; i++)
        {
            total += DecisionPoints[i];
        }

        int dice = 0;
        int chosenPoint = Random.Range(0, total);
        int chosenIndex = 0;
        for(int i = 0; i < DecisionPoints.Count; i++)
        {
            if(chosenPoint < DecisionPoints[i])
            {
                chosenIndex = i;
                break;
            }
            chosenPoint -= DecisionPoints[i];
        }

        DecisionPoints[chosenIndex] = 1;
        DecisionPoints[chosenIndex ^ 1] += 1;

        dice = chosenIndex;


        
        if (AlwaysAnomoly) dice = 1;

        
        if (dice == 0)
        {
            anomoly_flag = 0;
        }else
        {
            EvaluateAnomoly();
            anomoly_flag = 1;
        }

        EvaluateNPC();
    }

    public void QueryEnter(int anomoly)
    {
        if(sleepQuery)
        {
            sleepQuery = false;
            return;
        }
        RestoreAnomoly();
        RestoreNPC();
        
        if (anomoly_flag == anomoly)
        {
            Debug.Log("Correct Anomoly");
            if (!infinityMode)
            {
                
                CurrentFloor--;
            }
            
        }else
        {
            Debug.Log("Wrong Anomoly");
            CurrentFloor = 8;
        }


        
        NewMap();


    }

    void Update()
    {
        MaintainInfinity();
    }

    // For anomolies that need to know where the player is (looker, doppelganger, lift swap...)
    public Transform GetPlayerTransform()
    {
        return playerTransform;
    }

    // For global settings
    public void ForceAnomolyAll(int anomoly_index)
    {
        for(int i = 0; i < createdFloor.Count ;i++)
        {
            createdFloor[i].GetComponent<AnomolyManager>().ForceAnomoly(anomoly_index);
        }
    }


    // add_up_floor mean if it is the upper floor then call 1 if lower floow then call -1.
    public void ForceAnomoly(int add_up_floor, int anomoly_index)
    {
        int new_floor = CurrentFloor + add_up_floor;
        if (new_floor < 1 || new_floor > 8) return;
        createdFloor[new_floor - 1].GetComponent<AnomolyManager>().ForceAnomoly(anomoly_index);
    }

    public void ForceRestoreAll()
    {
        for (int i = 0; i < createdFloor.Count; i++)
        {
            createdFloor[i].GetComponent<AnomolyManager>().ForceRestore();
        }
    }

    public void Flip()
    {
        sleepQuery = true;
        Transform floorTransform = createdFloor[CurrentFloor -1].transform;

        floorTransform.localScale = new Vector3( - floorTransform.localScale.x, floorTransform.localScale.y, floorTransform.localScale.z);
        if (floorTransform.localScale.x < 0.0f)
        {
            floorTransform.position = new Vector3(42.33f + (89.28f - 49.09616f), floorTransform.position.y, -99.65f);
            
        }else
        {
            floorTransform.position = new Vector3(42.33f, floorTransform.position.y, -99.65f);
        }
    }
}
