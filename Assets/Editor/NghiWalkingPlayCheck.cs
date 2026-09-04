using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class NghiWalkingPlayCheck
{
    static Anomoly17 target;
    static NPCVisualAnimator teacher;
    static float started, firstPhase, maxSpeed, maxRotation;
    static Transform calf;
    static Quaternion firstRotation;
    static bool female;
    static List<string> log=new List<string>();
    static NghiWalkingPlayCheck()
    {
        EditorApplication.delayCall += Request;
        EditorApplication.playModeStateChanged += Changed;
    }
    static void Request()
    {
        if (!File.Exists("Temp/NghiWalkPlay.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Temp/NghiWalkPlay.request");
        SessionState.SetBool("NghiWalkPlay",true);
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool("NghiWalkPlay",false)) return;
        if (state==PlayModeStateChange.EnteredPlayMode)
        { Application.runInBackground=true;Time.timeScale=1;started=Time.time;target=null;female=false;maxSpeed=0;maxRotation=0;log.Clear();EditorApplication.update += Tick; }
        if (state==PlayModeStateChange.EnteredEditMode)
        { SessionState.SetBool("NghiWalkPlay",false);EditorApplication.update -= Tick; }
    }
    static void Bind(Transform root,Animation animation)
    {
        calf=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Bip01 L Calf");
        firstRotation=calf.localRotation;firstPhase=animation["Walk"].time;maxSpeed=0;maxRotation=0;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float time=Time.time-started;
        try
        {
            if(time<2)return;
            if(target==null)
            {
                var field=typeof(GameManager).GetField("createdFloor",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var floors=(List<GameObject>)field.GetValue(GameManager.Instance);
                target=floors[GameManager.Instance.GetCurrentFloor()-1].GetComponentInChildren<Anomoly17>(true);
                target.Restore();target.npc.SetActive(true);
                teacher=target.npc.GetComponentInChildren<NPCVisualAnimator>(true);
                Bind(teacher.transform,teacher.motion);
            }
            maxSpeed=Mathf.Max(maxSpeed,target.npc.GetComponent<NavMeshAgent>().velocity.magnitude);
            maxRotation=Mathf.Max(maxRotation,Quaternion.Angle(firstRotation,calf.localRotation));
            if(!female && time>=7)
            {
                Check("Normal teacher",teacher.motion);
                target.Evaluate();female=true;Bind(target.alternateAppearance.transform,target.alternateAnimation);
                if(target.normalRenderers.Any(r=>r.enabled))throw new Exception("Teacher visible during #17");
            }
            if(time<13)return;
            Check("Female anomaly #17",target.alternateAnimation);
            var agent=target.npc.GetComponent<NavMeshAgent>();
            log.Add("Navigation: onMesh="+agent.isOnNavMesh+", path="+agent.hasPath+", remaining="+agent.remainingDistance+", enabled="+target.npc.GetComponent<NPCTrajectory>().enabled);
            target.Restore();
            if(target.normalRenderers.Any(r=>!r.enabled))throw new Exception("Teacher not restored");
            log.Add("PASS: teacher/female visibility swap and restore during live patrol");
            log.Add("Floor display components="+UnityEngine.Object.FindObjectsByType<ElevatorDisplay>(FindObjectsSortMode.None).Length);
            Finish();
        }
        catch(Exception e){log.Add("FAIL: "+e);Finish();}
    }
    static void Check(string name,Animation animation)
    {
        float phase=animation["Walk"].time;
        bool pass=maxSpeed>.2f && maxRotation>10 && phase!=firstPhase;
        log.Add((pass?"PASS: ":"FAIL: ")+name+", speed="+maxSpeed+", knee excursion="+maxRotation+", walk time="+phase);
    }
    static void Finish()
    {
        if(target!=null)target.Restore();
        File.WriteAllLines("Temp/NghiWalkPlay.txt",log);
        EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
    }
}
