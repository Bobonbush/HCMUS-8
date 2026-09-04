using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static CutSceneManager Instance { get; private set; }
    public GameObject gameplayObject;
    public GameObject UIObject;




    bool inCutScene = false;

    public bool IsInCutScene
    {
        get { return inCutScene; }
    }
    private void Awake()
    {
        // Ensure only one instance of this manager ever exists (Singleton)
        if (Instance == null)
        {
            Instance = this;

            GameObject player = gameplayObject = GameObject.Find("Player");
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    
    /// <summary>Về main menu: huỷ manager này để lượt chơi sau dựng lại sạch từ StartGame.
    /// Phải gán Instance = null ngay thay vì đợi Destroy, nếu không Awake của bản mới
    /// có thể thấy bản cũ chưa huỷ xong và tự destroy chính nó.</summary>
    public static void TeardownForMenu()
    {
        if (Instance == null)
            return;

        Destroy(Instance.gameObject);
        Instance = null;
    }

    public void ForceHideGamePlay(bool settings = false)
    {
        if (gameplayObject == null)
        {
            gameplayObject = GameObject.Find("Gameplay");
        }
        if (gameplayObject != null)
        {


                gameplayObject.SetActive(settings);
            
        }
        else
        {
            Debug.LogError("[CutScene] gameplayObject is MISSING/NULL in the inspector!");
        }

        if (UIObject != null)
        {
            UIObject.SetActive(settings);
        }
    }

    public void OnCutSceneStart(CutSceneInfo info)
    {

        // Turn off Hub

        inCutScene = true;
        if(gameplayObject == null)
        {
            gameplayObject = GameObject.Find("Gameplay");
        }
        if (gameplayObject != null)
        {
            
            if (info.useBigDialog)
            {
                gameplayObject.SetActive(false);
            }
        }

        if(UIObject != null && info.HideUI)
        {
            UIObject.SetActive(false);
        }
    }

    public void OnCutSceneEnd(CutSceneInfo info)
    {

        if (info.returnToMainMenu)
        {
            // Hết lượt chơi (chết Steel mode hoặc phá đảo) — về menu qua SaveManager
            // để currentlyInGame được reset, nếu không lần chọn slot sau sẽ không vào được game.
            // Bật lại Hub ở đây là vô nghĩa vì đang rời khỏi game, nên return sớm.
            inCutScene = false;
            return;
        }

        // Turn on Hub Again
        if (info.useBigDialog && info.StillHideUI == false && gameplayObject != null)
        {
            gameplayObject.SetActive(true);
        }

        if (UIObject != null && info.StillHideUI == false)
        {
            UIObject.SetActive(true);
        }

        if (info.newScene && info.HideUI)
        {

            SceneManager.LoadScene(info.SceneID);
        }

        inCutScene = false;
    }

}
