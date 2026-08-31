using UnityEngine;

public class NPCComponent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected NPC npc;

    protected virtual void Awake()
    {
        
        npc = GetComponent<NPC>();
        
    }
}
