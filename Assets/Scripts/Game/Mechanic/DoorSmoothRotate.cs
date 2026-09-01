using UnityEngine;

public class DoorSmoothRotate : MonoBehaviour
{
    private float angleY = 90.0f;
    private float time = 5.0f;

    AudioSource audioP;

    private float startY;
    private float elapsed;

    private bool opening = false;


    private void Awake()
    {
        audioP = GetComponent<AudioSource>();
    }
    void Start()
    {
        startY = transform.localEulerAngles.y;
    }

    void Update()
    {
        if (!opening) return;
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / time);

        // Smooth interpolation
        t = Mathf.SmoothStep(0f, 1f, t);

        float currentY = Mathf.LerpAngle(startY, angleY, t);

        transform.localRotation = Quaternion.Euler(
            transform.localEulerAngles.x,
            currentY,
            transform.localEulerAngles.z
        );

        if (t >= 1f)
            opening = false;
    }

    public void Rotate(float y)
    {
        startY = transform.localEulerAngles.y;
        angleY = y;
        elapsed = 0f;
        opening = true;
        audioP.Play();
    }

    public void ImmediateRotate(float y)
    {
        transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.x, y, transform.rotation.z));
    }
}
