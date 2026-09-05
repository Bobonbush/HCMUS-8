using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;

public class A33Trigger : MonoBehaviour
{

    private bool Triggered = false;

    [SerializeField]
    private List<LightCondition> lightConditions;

    [SerializeField]
    GameObject AdditionalText;

    [SerializeField]
    AudioSource closeDoor;

    [SerializeField]
    Transform closeDoorTransform;

    [SerializeField]
    private GameObject ghost;

    [SerializeField]
    DoorSmoothRotate openDoor;

    float time = -10.0f;

    Color LightColor = Color.black;

    bool playSound = false;

    private void Evaluate()
    {
        
        Triggered = true;
        closeDoorTransform.rotation = Quaternion.Euler(new Vector3(closeDoorTransform.rotation.x, -90.0f, closeDoorTransform.rotation.z));
        closeDoor.Play();
        // The door banged shut hard enough to rattle the floor - kick the camera.
        FirstPersonCameraFeel feel = FindFirstObjectByType<FirstPersonCameraFeel>();
        if (feel != null) feel.SlamShake(2f);
        AdditionalText.SetActive(true);
        
        TurnoffLight();
        time = 7.0f;
        ghost.SetActive(true);


    }

    private void Update()
    {
        if (time < 0.0f) return;
        time -= Time.deltaTime;
        if(time < 5.0f && playSound == false)
        {
            ghost.GetComponent<AudioSource>().Play();
            playSound = true;
        }
        if(time < 3.0f)
        {
            ghost.SetActive(false);
        }
        if (time > 0.0f) return;

        
        openDoor.Rotate(-10.0f);
    }

    public void Restore()
    {
        Triggered = false;
        playSound = false;
        closeDoorTransform.rotation = Quaternion.Euler(new Vector3(closeDoorTransform.rotation.x, -145.0f, closeDoorTransform.rotation.z));
        openDoor.ImmediateRotate(-90.0f);
        AdditionalText.SetActive(false);
        ghost.SetActive(false);
        TurnonLight();
    }


    private void TurnoffLight()
    {
        LightColor = lightConditions[0].GetComponent<Light>().color;
        lightConditions[0].GetComponent<Light>().color = new Vector4(0.75f, 0.0f, 0.0f);
        bool skipFirst = false;
        foreach (LightCondition lightB in lightConditions)
        {
            if (skipFirst == false)
            {
                skipFirst = true;
                continue;
            }
            lightB.TurnOff();
        }

        GameManager.Instance.ForceAnomolyAllExceptCurrent(1);
    }

    private void TurnonLight()
    {
        lightConditions[0].GetComponent<Light>().color = LightColor;
        bool skipFirst = false;
        foreach (LightCondition lightB in lightConditions)
        {
            if(skipFirst == false)
            {
                skipFirst = true;
                continue;
            }
            lightB.TurnOn();
        }

        GameManager.Instance.ForceRestoreAllExceptCurrent();

    }
    private void OnTriggerEnter(Collider other)
    {
        if(Triggered)
        {
            return;
        }

        Evaluate();


    }
}
