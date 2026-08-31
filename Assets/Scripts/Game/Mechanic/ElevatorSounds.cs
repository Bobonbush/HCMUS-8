using UnityEngine;

// The elevator's ride sequence. Doors stand open by default; when the player steps
// into the cabin they slide shut behind them, the cabin "travels" (departure jolt,
// vibration, arrival jolt - direction randomised per ride so it sometimes goes up
// and sometimes down), the arrival ding plays and the doors open again. The world
// never actually moves; the jolts and rumble sell the trip from inside.
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

    [Header("Travel feel")]
    [Tooltip("Seconds after the close starts before the cab departs (doors need to shut first).")]
    public float departDelay = 1.0f;
    [Tooltip("Camera spring kick at departure/arrival. Sign flips with travel direction.")]
    public float jolt = 0.55f;
    [Tooltip("Cabin vibration amplitude while travelling, in metres. Keep tiny.")]
    public float rumbleAmplitude = 0.004f;
    [Tooltip("Cabin vibration frequency in Hz.")]
    public float rumbleFrequency = 9f;

    private AudioSource source;
    private float timer = -1f;
    private int stage;              // 0 idle (doors open), 1 closing/travelling, 2 arrived, waiting for open
    private bool playerInside;
    private bool doorsOpen = true;
    private Vector3 closedA, closedB, openA, openB;
    private Vector3 closedA2, closedB2, openA2, openB2;

    // travel state
    private float travelDirection;      // +1 the cab "goes up", -1 "goes down", 0 = not travelling
    private bool departed;
    private Vector3 cabinBasePosition;
    private FirstPersonCameraFeel cameraFeel;

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

        cabinBasePosition = transform.localPosition;

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
            if (block != null) block.enabled = true;
            stage = 1;
            timer = dingDelay;
            // pick this ride's direction and reset travel state
            travelDirection = Random.Range(0, 2) == 0 ? 1f : -1f;
            departed = false;
            if (cameraFeel == null) cameraFeel = FindFirstObjectByType<FirstPersonCameraFeel>();
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

        if (stage == 1)
        {
            float elapsed = dingDelay - timer;

            // Departure: once the doors have had a moment to shut, the cab sets off with
            // a jolt. Going up presses you down (camera dips); going down lifts you.
            if (!departed && elapsed >= departDelay)
            {
                departed = true;
                if (cameraFeel != null && cameraFeel.enabled)
                    cameraFeel.ExternalImpulse(-jolt * travelDirection, 6f * travelDirection);
            }

            // In transit: the cabin trembles around its rest pose.
            if (departed)
            {
                float t = Time.time * rumbleFrequency * Mathf.PI * 2f;
                transform.localPosition = cabinBasePosition + new Vector3(
                    Mathf.Sin(t * 0.83f) * rumbleAmplitude * 0.5f,
                    Mathf.Sin(t) * rumbleAmplitude,
                    0f);
            }

            if (timer <= 0f)
            {
                // Arrival: opposite jolt as the cab brakes, then the ding.
                transform.localPosition = cabinBasePosition;
                if (cameraFeel != null && cameraFeel.enabled)
                    cameraFeel.ExternalImpulse(jolt * 0.8f * travelDirection, -5f * travelDirection);
                if (dingClip != null) source.PlayOneShot(dingClip, volume);
                stage = 2;
                timer = openDelay;
            }
        }
        else if (stage == 2 && timer <= 0f)
        {
            if (block != null) block.enabled = false;
            if (doorOpenClip != null) source.PlayOneShot(doorOpenClip, volume);
            doorsOpen = true;
            travelDirection = 0f;
            stage = 0;
            timer = -1f;
        }
    }
}
