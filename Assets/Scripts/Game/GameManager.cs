using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{


    [SerializeField] private GameObject FloorObject;
    [SerializeField] private Transform playerTransform;


    
    public static GameManager Instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private int CurrentFloor = 11;

    private float offset = 5.664f;

    private int prevAnomoly = -1;
    
    private int maxFloor = 14;


    private int offsetFloor = 3;

    [Tooltip("Floors further than this many floors from the current one are deactivated (not rendered, not simulated).")]
    [SerializeField] private int renderRadius = 4;

    List<int> DecisionPoints = new List<int>(){ 1, 1 };

    List<GameObject> createdFloor = new List<GameObject>() ;

    public bool AlwaysAnomoly = false;
    private int anomoly_flag = 0;

    private bool infinityMode = false;

    private int sleepQuery = 1;

    float minQueryListenerTiming = 0.0f;

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
        RestoreAnomoly();
        RestoreNPC();
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

        MaintainRender();
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

    // Only floors within renderRadius of CurrentFloor stay active. The player never leaves
    // createdFloor[CurrentFloor - 1] (MaintainInfinity shifts the stack around them), so floors
    // further away than that are never visible and do not need their lights, NPCs and
    // anomaly scripts running. Cheap to call every frame: SetActive only runs on a change.
    private void MaintainRender()
    {
        for (int i = 0; i < createdFloor.Count; i++)
        {
            bool shouldRender = Mathf.Abs((i + 1) - CurrentFloor) <= renderRadius;
            if (createdFloor[i].activeSelf != shouldRender)
            {
                createdFloor[i].SetActive(shouldRender);
            }
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
        Debug.Log("Called");
        prevAnomoly =  createdFloor[CurrentFloor - 1].GetComponent<AnomolyManager>().GenerateAnomoly(prevAnomoly);
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

    private static int level = 0;
    private void NewMap()
    {
        // Manual testing owns anomaly selection; a hidden random anomaly must not
        // run while the overlay says the floor is clean or another test is selected.
        var tester = FindFirstObjectByType<AnomolyTester>();
        
        if (tester != null && tester.IsActive)
        {
            ForceRestoreAll();
            anomoly_flag = 0;
            prevAnomoly = -1;
            EvaluateNPC();
            return;
        }

       
        // Generate Anomoly here

        
        int total = 0;
        for(int i = 0; i < DecisionPoints.Count; i++)
        {
            total += DecisionPoints[i];
        }

        int dice = 0;
        int chosenPoint = Random.Range(0, total);
        int chosenIndex = 0;

        for (int i = 0; i < DecisionPoints.Count; i++)
        {
            chosenIndex = i;
            if (chosenPoint < DecisionPoints[i])
            {
                
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
            prevAnomoly = -1;
        }
        else
        {
            EvaluateAnomoly();
            anomoly_flag = 1;
            
        }

        EvaluateNPC();

        Debug.Log("New Mapped");
    }

    public void QueryEnter(int anomoly)
    {
        if (CurrentFloor - offsetFloor == 0) return;
        if (sleepQuery > 0)
        {
            sleepQuery--;
            return;
        }
        if (minQueryListenerTiming > 0.0f) return;
        
        minQueryListenerTiming = 3.0f;
        RestoreAnomoly();
        RestoreNPC();
        
        if (anomoly_flag == anomoly)
        {
            //Debug.Log("Correct Anomoly");
            
            if (!infinityMode)
            {
                if(prevAnomoly >= 0)
                {
                    AnomolyManager.Correct(prevAnomoly);
                }
                CurrentFloor--;
                if (CheckEndGame()) return;
            }
        }else
        {
            //Debug.Log("Wrong Anomoly");
            CurrentFloor = 11;
        }


        
        NewMap();


    }

    private bool CheckEndGame()
    {
        if(CurrentFloor - offsetFloor == 0)
        {
            enabled = false;
            SceneManager.LoadScene("End");
            return true;
        }
        return false;
    }

    void Update()
    {
        MaintainRender();
        MaintainInfinity();

        minQueryListenerTiming -= Time.deltaTime;
    }

    // For anomolies that need to know where the player is (looker, doppelganger, lift swap...)
    public Transform GetPlayerTransform()
    {
        return playerTransform;
    }

    // For global settings
    public void ForceAnomolyAll(int anomoly_index)
    {
        int i = CurrentFloor;
        int j = CurrentFloor-1;
        while (true)
        {
            if (i == 0 && j == createdFloor.Count - 1) break;
            if (i > 0)
            {
                i--;
                createdFloor[i].GetComponent<AnomolyManager>().ForceAnomoly(anomoly_index);
            }

            if (j < createdFloor.Count - 1)
            {
                j++;
                createdFloor[j].GetComponent<AnomolyManager>().ForceAnomoly(anomoly_index);
            }

        }
    }

    public void ForceAnomolyAllExceptCurrent(int anomoly_index)
    {
        int i = CurrentFloor - 1;
        int j = CurrentFloor - 1;
        while(true)
        {
            if (i == 0 && j == createdFloor.Count-1) break;
            if(i > 0)
            {
                i--;
                createdFloor[i].GetComponent<AnomolyManager>().ForceAnomoly(anomoly_index);
            }
            
            if(j < createdFloor.Count - 1)
            {
                j++;
                createdFloor[j].GetComponent<AnomolyManager>().ForceAnomoly(anomoly_index);
            }

        }
    }

    public void ForceRestoreAllExceptCurrent()
    {
        int i = CurrentFloor - 1;
        int j = CurrentFloor -1;
        while (true)
        {
            if (i == 0 && j == createdFloor.Count - 1) break;
            if (i > 0)
            {
                i--;
                createdFloor[i].GetComponent<AnomolyManager>().ForceRestore();
            }

            if (j < createdFloor.Count - 1)
            {
                j++;
                createdFloor[j].GetComponent<AnomolyManager>().ForceRestore();
            }

        }
    }


    // add_up_floor mean if it is the upper floor then call 1 if lower floow then call -1.
    public void ForceAnomoly(int add_up_floor, int anomoly_index)
    {
        int new_floor = CurrentFloor + add_up_floor - offsetFloor;
        // An AddUp anomaly needs a real neighbour floor to appear on. If the chosen
        // side is past the 1..8 range (e.g. CurrentFloor pinned at 8 after a wrong
        // answer, direction +1 -> floor 9), fall back to the opposite side instead
        // of silently showing nothing. Since CurrentFloor is always 1..8, at least
        // one neighbour is always valid.
        if (new_floor < 1 || new_floor > 8) new_floor = CurrentFloor - add_up_floor;
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
        sleepQuery++;
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


    public void AddUpQueryQueue()
    {
        sleepQuery++;
    }
    public int GetCurrentFloor()
    {
        return CurrentFloor - offsetFloor;
    }

    public AnomolyManager GetCurrentAnomolyManager()
    {
        int index = CurrentFloor - 1;
        return index >= 0 && index < createdFloor.Count
            ? createdFloor[index].GetComponent<AnomolyManager>() : null;
    }
}
