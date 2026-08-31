using UnityEngine;

// Spins the hour and minute hands of the wall clock. The clock runs faster than real
// time so the player can actually see it move: the minute hand does a lap a minute.
// Anomoly23 flips direction to -1 to make it run backwards.
public class ClockHands : MonoBehaviour
{
    public Transform hourHand;
    public Transform minuteHand;
    public Transform secondHand;

    [Tooltip("1 runs forward, -1 runs backwards (the anomoly).")]
    public float direction = 1f;

    [Tooltip("Degrees per second for the minute hand. 12 means two laps per minute.")]
    public float minuteDegreesPerSecond = 12f;

    [Tooltip("Degrees per second for the second hand. 36 means one lap every 10 seconds, so direction reads at a glance.")]
    public float secondDegreesPerSecond = 36f;

    private void Update()
    {
        float step = minuteDegreesPerSecond * direction * Time.deltaTime;
        // Hands rotate around their local Z; the hour hand moves at 1/12 the minute rate.
        // Positive spin here reads as clockwise from the corridor (playtest-verified;
        // the sign was flipped before).
        if (secondHand != null) secondHand.Rotate(0f, 0f, secondDegreesPerSecond * direction * Time.deltaTime);
        if (minuteHand != null) minuteHand.Rotate(0f, 0f, step);
        if (hourHand != null) hourHand.Rotate(0f, 0f, step / 12f);
    }
}
