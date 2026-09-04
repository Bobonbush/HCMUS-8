using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "NewCutsceneInfo", menuName = "Cutscenes/Cutscene Info")]
public class CutSceneInfo : ScriptableObject
{

    public bool HideUI = true;
    public bool StillHideUI = false;
    [System.Serializable]
    public class CameraForceData
    {
        public Vector3 position = Vector3.zero;   // TargetPosition 
        public float delaybeforeMove = 0.0f;
        public float moveDuration = 1.0f;  // move time
        public float stayDuration = 1.0f; // stay time till the next move

        public enum Effect
        {
            None,
            Shake,
            EarthWake,
            Zoom
        }

        public Effect effect;
        public float effectDuration = 0.0f;
        public float lensImplitude = 0.0f; // 8 for enemy fight
        public bool effectStays = false;
    };

    [System.Serializable]
    public class ActiveObject
    {
        public float delayDuration = 0.0f;
        public string gameobject;
        public float TurnOnDuration = 0.0f;
        public bool TurnOnForever = false;
    }

    
    

    [System.Serializable]
    public class PlayerForceData
    {
        public float forceX = 0.0f;
        public float scaleX = 0.0f;
        public float MoveDuration = 0.0f;
        public float stayDuration = 0.0f;

        public bool useTrigger = false;
        public string triggerName = "";
    };

    [System.Serializable]
    public class ForceMovement
    {
        public Vector2 playerForce;
        public Vector2 cameraForce;
        public float duration;
    }

    public bool useDialog = false;

    
    public bool useBigDialog = true;

    public float delayTime = 0.0f;
    
    public float showTime = 0.0f;
    
    public float fadingTime = 0.0f;

    
    public bool newScene = false;

    /// <summary>Kết thúc cutscene thì kết thúc luôn lượt chơi và quay về main menu
    /// (dùng cho scene "hard" khi chết Steel mode và scene "End" khi phá đảo).
    /// Không dùng chung đường với newScene vì ForceTransitionToScene chạy logic
    /// spawn point / SetRoom vốn vô nghĩa ở menu.</summary>
    public bool returnToMainMenu = false;

    public string SceneID = "";

    public string SpawnID = "";

    public Vector3 SpawnOffset = Vector2.zero;

    public bool RealTimeAnimation = false;

    public bool UsedCamera = false;

    public List<ActiveObject> EnableObjects;
    public List<CameraForceData> ForceCameraMovement;
    public List<PlayerForceData> PlayerForceDatas;

    public bool ComeBackCamera = true;
    public float timeCameraComeBack = 1.0f;


    

}
