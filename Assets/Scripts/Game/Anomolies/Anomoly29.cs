using UnityEngine;

// You walk into the left lift and come out of the right one (or vice versa).
// While active, the moment the player is inside a lift cabin they are mirrored
// across the plane between the two cabins - unnoticeable from inside, and then
// they step out of the wrong lift.
//
// Both LiftRoom trigger colliders are disabled around the swap and only re-enabled
// once the player has left both boxes, so the mirror can never re-fire QueryEnter.
public class Anomoly29 : MonoBehaviour, Anomoly
{

    [Tooltip("The 'Normal Door' lift trigger object.")]
    public Transform liftTriggerA;

    [Tooltip("The 'Anomoly Door' lift trigger object.")]
    public Transform liftTriggerB;

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private bool active;
    private bool mirrored;
    private bool collidersDisabled;

    public void Evaluate()
    {
        active = true;
        mirrored = false;
    }

    public void Restore()
    {
        active = false;
        mirrored = false;
        SetTriggersEnabled(true);
    }

    private void Update()
    {
        if (GameManager.Instance == null || liftTriggerA == null || liftTriggerB == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null) return;

        BoxCollider boxA = liftTriggerA.GetComponent<BoxCollider>();
        BoxCollider boxB = liftTriggerB.GetComponent<BoxCollider>();
        if (boxA == null || boxB == null) return;

        // Grow the bounds a little so "outside" really means clear of the trigger.
        Bounds a = boxA.bounds; a.Expand(0.3f);
        Bounds b = boxB.bounds; b.Expand(0.3f);
        Vector3 probe = player.position + Vector3.up * 0.6f;
        bool insideAny = a.Contains(probe) || b.Contains(probe);

        // Re-arm the triggers once the player has walked clear of both cabins.
        if (collidersDisabled && !insideAny) SetTriggersEnabled(true);

        if (!active || mirrored) return;

        // Only mirror a player on this floor.
        float floorY = transform.parent != null ? transform.parent.position.y : transform.position.y;
        if (Mathf.Abs(player.position.y - floorY) > 2.5f) return;

        if (insideAny)
        {
            mirrored = true;
            SetTriggersEnabled(false);

            float midX = (liftTriggerA.position.x + liftTriggerB.position.x) * 0.5f;
            Vector3 pos = player.position;
            pos.x = 2f * midX - pos.x;
            float yaw = -player.eulerAngles.y;

            FirstPersonController fpc = player.GetComponent<FirstPersonController>();
            if (fpc != null)
            {
                fpc.Teleport(pos, yaw);
            }
            else
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                if (cc != null) cc.enabled = true;
            }
        }
    }

    private void SetTriggersEnabled(bool on)
    {
        collidersDisabled = !on;
        BoxCollider boxA = liftTriggerA != null ? liftTriggerA.GetComponent<BoxCollider>() : null;
        BoxCollider boxB = liftTriggerB != null ? liftTriggerB.GetComponent<BoxCollider>() : null;
        if (boxA != null) boxA.enabled = on;
        if (boxB != null) boxB.enabled = on;
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
