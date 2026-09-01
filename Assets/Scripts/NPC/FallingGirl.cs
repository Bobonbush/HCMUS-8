using UnityEngine;

public class FallingGirl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    float timeAlive = 2.0f;
    [SerializeField]
    AudioSource audioSource;


    Rigidbody rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = new Vector3(0, -2.0f, 0.0f);
    }


    // Update is called once per frame
    void Update()
    {
        if(timeAlive < 0.0f)
        {
            timeAlive = 2.0f;
            return;
        }
        timeAlive -= Time.deltaTime;
        if(timeAlive < 0)
        {
            audioSource.Play();
            rb.linearVelocity = new Vector3(0, -2.0f, 0.0f);
            this.gameObject.SetActive(false);
        }
    }
}
