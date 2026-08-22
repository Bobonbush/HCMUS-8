using UnityEngine;

// Spins the hour and minute hands of the wall clock. The clock runs faster than real
// time so the player can actually see it move: the minute hand does a lap a minute.
// Anomoly23 flips direction to -1 to make it run backwards.
public class ClockHands : MonoBehaviour
{
    public Transform hourHand;
    public Transform minuteHand;

    [Tooltip("1 runs forward, -1 runs backwards (the anomoly).")]
    public float direction = 1f;

    [Tooltip("Degrees per second for the minute hand. 6 means one lap per minute.")]
    public float minuteDegreesPerSecond = 6f;

    private void Update()
    {
        float step = minuteDegreesPerSecond * direction * Time.deltaTime;
        // Hands rotate around their local Z; the hour hand moves at 1/12 the rate.
        if (minuteHand != null) minuteHand.Rotate(0f, 0f, -step);
        if (hourHand != null) hourHand.Rotate(0f, 0f, -step / 12f);
    }
}
