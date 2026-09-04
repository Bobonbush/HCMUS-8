using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
public class CutTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    enum TriggerType : int {
        Instant = 0,
        HitBox = 1,
        NeedTrigger = 2
    }

    bool startPlaying = false;

    
    [SerializeField]
    CutSceneInfo info;

    [SerializeField]
    TriggerType type = TriggerType.Instant;

    public DialogNode startNode;                 // First node
    public DialogStyle style = DialogStyle.Box;  // Box or Bubble
    public Transform bubbleTarget;               // Who the bubble follows (empty = this object)

    
    bool played = false;

    bool NoCutSceneRemain { get { return cnt == 0; } }

    float time = 0.0f;
    int cnt = 0;

    private bool isDone = false;


    private List<GameObject> activeObjectList = new List<GameObject>();


    bool MulthiThreadingLock = false;
    bool firstAnimatorTrigger = false;

    private IEnumerator AcquireLock()
    {
        while(MulthiThreadingLock)
        {
            yield return null;
        }
        MulthiThreadingLock = true;
    }

    private void ReleaseLock()
    {

        MulthiThreadingLock = false;
    }

    private IEnumerator ModifyCount(int x)
    {
        yield return AcquireLock();
        cnt += x;
        if(cnt > 0)
        {
            firstAnimatorTrigger = true;
        }
        ReleaseLock();
    }



    void Start()
    {
        if (type == TriggerType.Instant)
        {
            CutSceneManager manager = CutSceneManager.Instance;
            manager.OnCutSceneStart(info);
            startPlaying = true;
            if (info.RealTimeAnimation)
            {
                
                Trigger();
            }
        }

        for (int i = 0; i < info.EnableObjects.Count; i++)
        {
            CutSceneInfo.ActiveObject data = info.EnableObjects[i];
            GameObject ob = GameObject.Find(data.gameobject);
            activeObjectList.Add(ob);
            ob.SetActive(false);
            
        }

    }


    private void Trigger()
    {
        if (type == TriggerType.Instant)
        {
            CutSceneManager manager = CutSceneManager.Instance;
            manager.OnCutSceneStart(info);
            startPlaying = true;
        }
    }




    private IEnumerator PerformActiveObjects()
    {
        yield return ModifyCount(1);
        for (int i = 0; i < info.EnableObjects.Count; i++)
        {
            CutSceneInfo.ActiveObject data = info.EnableObjects[i];
            GameObject ob = activeObjectList[i];
            yield return new WaitForSeconds(data.delayDuration);

            ob.SetActive(true);

            if (data.TurnOnForever == false)
            {
                yield return new WaitForSeconds(data.TurnOnDuration);

                ob.SetActive(false);

            }
        }

        yield return ModifyCount(-1);
    }



    private void BigDialogAnimation()
    {
        if (time <= info.delayTime)
        {
            time += Time.deltaTime;
            return;
        }
        if (!played)
        {
            Transform target = bubbleTarget != null ? bubbleTarget : transform;
            CutSceneDialogManager.Instance.StartDialog(startNode, style, target);
            played = true;
        }

        if (!CutSceneDialogManager.Instance.AnimationDone())
        {
            return;
        }
        time += Time.deltaTime;
        if (time <= info.showTime + info.delayTime)
        {
            return;
        }

        CutSceneDialogManager.Instance.EndDialog();

        if (time <= info.fadingTime + info.delayTime + info.showTime)
        {
            return;
        }

        CutSceneManager manager = CutSceneManager.Instance;
        manager.OnCutSceneEnd(info);
        Destroy(this.gameObject);
    }

    private void Update()
    {
        if(!startPlaying)
        {
            return;
        }

        // Text only CutScene
        if(info.useBigDialog)
        {
            BigDialogAnimation();
            return;
        }

        if(NoCutSceneRemain && startPlaying && firstAnimatorTrigger)
        {
            isDone = true;
            AbandoneTrigger();
        }
    }




    private void AbandoneTrigger()
    {


        CutSceneManager manager = CutSceneManager.Instance;
        manager.OnCutSceneEnd(info);
        this.enabled = false;
    }

}
