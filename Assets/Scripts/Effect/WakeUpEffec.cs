using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class WakeUpEffec : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private float duration = 3f;

    private float delayTime = 2.0f;

    private void Start()
    {
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(delayTime);
        Color color = image.color;
        float startAlpha = color.a;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            color.a = Mathf.Lerp(startAlpha, 0f, t);
            image.color = color;

            yield return null;
        }

        color.a = 0f;
        image.color = color;
    }
}
