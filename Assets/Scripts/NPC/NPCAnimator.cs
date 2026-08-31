using UnityEngine;

public class NPCAnimator :  NPCComponent
{
   

    // Update is called once per frame
    void Update()
    {
        npc.animator.SetFloat("Speed", npc.CurrentSpeed);
    }
}
