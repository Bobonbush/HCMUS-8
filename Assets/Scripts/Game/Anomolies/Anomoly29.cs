using UnityEngine;

// You walk into the left lift and come out of the right one (or vice versa).
//
// The intended flow: the player boards a lift on floor n-1, which starts the ride
// and evaluates floor n. If floor n rolls this anomaly, Evaluate() fires while the
// player is still inside the cabin, Update() sees them there and mirrors them
// across the plane between the two cabins - unnoticeable from inside - and the
// opposite cabin adopts the ride in progress (doors shut, same timer, the arrival
// ding plays there) while the boarded cabin quietly resets. When the doors open,
// the player steps out of the wrong lift. That swap IS the anomaly to spot.
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

    private BoxCollider _boxA, _boxB;

    private void Update()
    {
        // 12 floor instances run this every frame - bail before any real work unless
        // this instance actually has something to do.
        if (!active && !collidersDisabled) return;
        if (GameManager.Instance == null || liftTriggerA == null || liftTriggerB == null) return;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null) return;

        if (_boxA == null) _boxA = liftTriggerA.GetComponent<BoxCollider>();
        if (_boxB == null) _boxB = liftTriggerB.GetComponent<BoxCollider>();
        BoxCollider boxA = _boxA;
        BoxCollider boxB = _boxB;
        if (boxA == null || boxB == null) return;

        // Grow the bounds a little so "outside" really means clear of the trigger.
        Bounds a = boxA.bounds; a.Expand(0.3f);
        Bounds b = boxB.bounds; b.Expand(0.3f);
        Vector3 probe = player.position + Vector3.up * 0.6f;
        bool insideA = a.Contains(probe);
        bool insideAny = insideA || b.Contains(probe);

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

            // Hand the ride over: the cabin the player boarded is mid-sequence
            // (doors shut, ding pending); the cabin they now occupy must continue
            // it so nothing looks or sounds out of place.
            ElevatorSounds boarded = NearestCabin(insideA ? liftTriggerA.position : liftTriggerB.position);
            ElevatorSounds destination = NearestCabin(insideA ? liftTriggerB.position : liftTriggerA.position);
            if (destination != null) destination.AdoptRideFrom(boarded);
        }
    }

    // The cabin (ElevatorSounds) on this floor closest to a lift trigger.
    private ElevatorSounds NearestCabin(Vector3 position)
    {
        Transform floorRoot = transform.parent != null ? transform.parent : transform;
        ElevatorSounds best = null;
        float bestDistance = float.MaxValue;
        foreach (ElevatorSounds cabin in floorRoot.GetComponentsInChildren<ElevatorSounds>(true))
        {
            float d = (cabin.transform.position - position).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = cabin; }
        }
        return best;
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
