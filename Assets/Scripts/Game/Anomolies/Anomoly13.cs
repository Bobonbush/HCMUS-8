using System.Collections.Generic;
using UnityEngine;

// A classroom that should be dark has its light on. It does not come on with
// you watching - the lamp is armed on Evaluate and switches a few seconds later,
// so the room is simply lit the next time you glance that way.
//
// Some classrooms are lit in the normal floor, so this caches every lamp's state
// and puts it back rather than blanket-darkening the floor on Restore.
public class Anomoly13 : MonoBehaviour, Anomoly
{

    [Tooltip("Every classroom lamp on the floor. One of the dark ones is picked at random.")]
    public List<Light> classroomLights = new List<Light>();

    [Tooltip("Shortest delay before the lamp comes on, in seconds.")]
    public float minDelay = 4f;

    [Tooltip("Longest delay before the lamp comes on, in seconds.")]
    public float maxDelay = 8f;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    // A lamp is dark if its component is switched off OR its object is inactive, and
    // both states occur in the map, so both have to be remembered and both restored.
    private readonly List<bool> originalEnabled = new List<bool>();
    private readonly List<bool> originalActive = new List<bool>();
    private int chosen = -1;
    private float coolDown = 10000f;
    private float delay;

    public void Evaluate()
    {
        if (originalEnabled.Count > 0) return;   // already armed

        // Only rooms that are dark right now are worth lighting up.
        List<int> candidates = new List<int>();
        for (int i = 0; i < classroomLights.Count; i++)
        {
            Light lamp = classroomLights[i];
            originalEnabled.Add(lamp != null && lamp.enabled);
            originalActive.Add(lamp != null && lamp.gameObject.activeSelf);
            if (lamp != null && !IsLit(lamp)) candidates.Add(i);
        }
        if (candidates.Count == 0) return;

        chosen = candidates[Random.Range(0, candidates.Count)];
        coolDown = 0f;
        delay = Random.Range(minDelay, maxDelay);
    }

    public void Restore()
    {
        // Disarm first, otherwise a restore before the lamp fired still lights it later.
        coolDown = 10000f;
        chosen = -1;
        for (int i = 0; i < classroomLights.Count && i < originalEnabled.Count; i++)
        {
            if (classroomLights[i] == null) continue;
            classroomLights[i].enabled = originalEnabled[i];
            classroomLights[i].gameObject.SetActive(originalActive[i]);
        }
        originalEnabled.Clear();
        originalActive.Clear();
    }

    private static bool IsLit(Light lamp)
    {
        return lamp.enabled && lamp.gameObject.activeSelf;
    }

    private void Update()
    {
        if (coolDown > delay) return;

        // Every floor runs this Update; only the one the player stands on should act.
        float floorY = transform.parent != null ? transform.parent.position.y : transform.position.y;
        if (GameManager.Instance == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null || Mathf.Abs(player.position.y - floorY) > 3f) return;

        coolDown += Time.deltaTime;
        if (coolDown > delay && chosen >= 0 && chosen < classroomLights.Count)
        {
            Light lamp = classroomLights[chosen];
            // Switch on both ways, or a lamp whose object was inactive stays dark.
            if (lamp != null)
            {
                lamp.gameObject.SetActive(true);
                lamp.enabled = true;
            }
        }
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
