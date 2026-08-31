using UnityEngine;

// The elevator's sound-and-door sequence. Doors stand open by default; when the
// player steps into the cabin they slide shut behind them, the arrival ding plays,
// and they slide open again - selling the floor loop without any real travel.
// Expects a trigger BoxCollider on the same object covering the cabin interior.
public class ElevatorSounds : MonoBehaviour
{
    public AudioClip doorCloseClip;
    public AudioClip dingClip;
    public AudioClip doorOpenClip;

    [Header("Door leaves")]
    public Transform doorLeafA;   // outer landing door, slides toward +z of the cabin model
    public Transform doorLeafB;   // outer landing door, slides toward -z
    public Transform doorLeafA2;  // inner cabin door, slides with A
    public Transform doorLeafB2;  // inner cabin door, slides with B
    [Tooltip("How far each leaf slides sideways when opening, in local metres.")]
    public float slideDistance = 0.8f;
    [Tooltip("Leaf movement speed in metres per second.")]
    public float slideSpeed = 0.9f;

    [Header("Timing")]
    [Range(0f, 1f)] public float volume = 0.8f;
    [Tooltip("Seconds between the doors starting to close and the arrival ding.")]
    public float dingDelay = 2.8f;
    [Tooltip("Seconds between the ding and the doors opening.")]
    public float openDelay = 0.8f;

    private AudioSource source;
    private float timer = -1f;
    private int stage;              // 0 idle (doors open), 1 closing/waiting for ding, 2 waiting for open
    private bool playerInside;
    private bool doorsOpen = true;
    private Vector3 closedA, closedB, openA, openB;
    private Vector3 closedA2, closedB2, openA2, openB2;

    [SerializeField]
    BoxCollider block;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;                     // comes from the cabin, not from everywhere
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1.5f;
        source.maxDistance = 16f;
        source.dopplerLevel = 0f;
        if (source.outputAudioMixerGroup == null)
            source.outputAudioMixerGroup = Game.UI.SettingsService.FindMixerGroup("Sfx");

        // The model imports with the doors shut; that pose is "closed", open is slid aside.
        if (doorLeafA != null)
        {
            closedA = doorLeafA.localPosition;
            openA = closedA + LocalSlideAxis(doorLeafA) * slideDistance;
            doorLeafA.localPosition = openA;
        }
        if (doorLeafB != null)
        {
            closedB = doorLeafB.localPosition;
            openB = closedB - LocalSlideAxis(doorLeafB) * slideDistance;
            doorLeafB.localPosition = openB;
        }
        if (doorLeafA2 != null)
        {
            closedA2 = doorLeafA2.localPosition;
            openA2 = closedA2 + LocalSlideAxis(doorLeafA2) * slideDistance;
            doorLeafA2.localPosition = openA2;
        }
        if (doorLeafB2 != null)
        {
            closedB2 = doorLeafB2.localPosition;
            openB2 = closedB2 - LocalSlideAxis(doorLeafB2) * slideDistance;
            doorLeafB2.localPosition = openB2;
        }
    }

    // The leaves slide along the cabin model's z axis, expressed in the leaf's parent space.
    private Vector3 LocalSlideAxis(Transform leaf)
    {
        Vector3 worldAxis = transform.rotation * Vector3.forward;
        return (leaf.parent != null ? leaf.parent.InverseTransformDirection(worldAxis) : worldAxis).normalized;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        if (playerInside) return;
        playerInside = true;

        if (stage == 0)
        {
            if (doorCloseClip != null) source.PlayOneShot(doorCloseClip, volume);
            doorsOpen = false;
            block.enabled = true;
            stage = 1;
            timer = dingDelay;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        playerInside = false;
    }

    private void Update()
    {
        // Slide the leaves toward their current target pose.
        float step = slideSpeed * Time.deltaTime;
        if (doorLeafA != null) doorLeafA.localPosition = Vector3.MoveTowards(doorLeafA.localPosition, doorsOpen ? openA : closedA, step);
        if (doorLeafB != null) doorLeafB.localPosition = Vector3.MoveTowards(doorLeafB.localPosition, doorsOpen ? openB : closedB, step);
        if (doorLeafA2 != null) doorLeafA2.localPosition = Vector3.MoveTowards(doorLeafA2.localPosition, doorsOpen ? openA2 : closedA2, step);
        if (doorLeafB2 != null) doorLeafB2.localPosition = Vector3.MoveTowards(doorLeafB2.localPosition, doorsOpen ? openB2 : closedB2, step);

        if (stage == 0) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (stage == 1)
        {
            
            if (dingClip != null) source.PlayOneShot(dingClip, volume);
            stage = 2;
            timer = openDelay;

        }
        else if (stage == 2)
        {
            block.enabled = false;
            if (doorOpenClip != null) source.PlayOneShot(doorOpenClip, volume);
            doorsOpen = true;
            stage = 0;
            timer = -1f;
            block.enabled = false;
        }
    }
}
