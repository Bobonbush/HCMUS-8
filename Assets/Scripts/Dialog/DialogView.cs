using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The "face" of the dialog — only handles DISPLAY.
// You will have two of these sharing this script:
//   - a rectangular box at the bottom  -> followTarget = false
//   - a speech bubble above a character -> followTarget = true
public class DialogView : MonoBehaviour
{
    [Header("UI References")]
    public GameObject root;               // Root object of this view (toggles the whole group)
    public TMP_Text speakerNameText;      // Can be left empty if the bubble style needs no name
    public TMP_Text dialogText;
    public Transform choicesContainer;
    public Button choiceButtonPrefab;
    public bool speakNameLastRender = true; 

    [Header("Bubble style (follows a character)")]
    public bool followTarget = false;     // true = bubble follows the head; false = fixed box
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f);  // how high above the head (world units)

    [Header("Open / Close animation")]
    public bool useFade = true;           // fade the alpha in/out
    public bool useSlide = true;          // slide horizontally (left -> right). Tick both = combined.
    public float animDuration = 0.25f;    // seconds
    public float slideDistance = 200f;    // how far to the left it starts, in pixels

    [Header("Typewriter (reveal text left to right)")]
    public bool useTypewriter = true;     // reveal the body text char by char, left to right
    public float typeSpeed = 30f;         // characters revealed per second
    public float typeFadeChars = 3f;      // width of the per-char alpha fade (0 = hard pop)
    public float newLinePause = 0.01f;

    private Transform target;
    private readonly List<Button> spawnedButtons = new List<Button>();

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Vector2 shownPos;              // resting anchoredPosition (set in the editor)
    private Vector2 slideOffset;           // current animation offset, in pixels
    private bool isOpen;
    private bool initialized;
    private Coroutine animRoutine;

    private Coroutine typeRoutine;

    private bool skipRequested;

    int listenTypeRountineCnt = 0;   // use to listen to the number of time tyepRountine is called to indicates whether the animation is done or not.
    int maxTypeRountineCnt = 0;

    public void SkipTypewriter()
    {
        if (typeRoutine == null)
            return;

        skipRequested = true;
    }

    public bool AnimationDone
    {
        get { return listenTypeRountineCnt == maxTypeRountineCnt; }
    }

    public void SetTarget(Transform t) => target = t;

    private void Start()
    {
        Init();

        if (!isOpen) HideInstant();
    }

    private void Init()
    {
        if (initialized) return;
        rect = root.GetComponent<RectTransform>();
        canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = root.AddComponent<CanvasGroup>();
        shownPos = rect.anchoredPosition;
        initialized = true;
    }

    public void Open()
    {
        Init();
        isOpen = true;

        // Set the starting state BEFORE showing so there is no 1-frame flash.
        canvasGroup.alpha = useFade ? 0f : 1f;
        slideOffset = useSlide ? new Vector2(-slideDistance, 0f) : Vector2.zero;
        root.SetActive(true);
        ApplyTransform();

        StartAnim(AnimateIn());
    }

    public void Close()
    {
        Init();
        if (!isOpen)                       // never opened -> just make sure it's hidden
        {
            HideInstant();
            return;
        }
        isOpen = false;
        StartAnim(AnimateOut());
    }

    public void SetText(string speaker, string body)
    {
        listenTypeRountineCnt = 0;
        if (speakNameLastRender && useTypewriter)
        {
            maxTypeRountineCnt = 2; // body first then speaker
        } else if(useTypewriter)
        {
            maxTypeRountineCnt = 1; // only body
        }else
        {
            maxTypeRountineCnt = 0;
        }
        if (speakerNameText != null)
        {
            if (speakNameLastRender == false)
                speakerNameText.text = speaker;
            else
                speakerNameText.text = "";
        }

        
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }


        if (speakNameLastRender)
        {
            typeRoutine = StartCoroutine(RunDialogueSequence(speaker, body));
        }
        else if (useTypewriter && isActiveAndEnabled && gameObject.activeInHierarchy)
            typeRoutine = StartCoroutine(TypeText(dialogText, body));
        else
            dialogText.text = body;   

        
    }

    private IEnumerator RunDialogueSequence(string speaker, string body)
    {

        if (useTypewriter && isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            yield return StartCoroutine(TypeText(dialogText, body));
        }
        else
        {
            dialogText.text = body;
        }

        if (speakerNameText != null)
        {
            if (useTypewriter && isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(TypeText(speakerNameText, speaker));
            }
            else
            {
                speakerNameText.text = speaker;
            }
        }
    }



    // Reveals the body text character by character, left to right, each char fading in via alpha.
    private IEnumerator TypeText( TMP_Text dialog , string body)
    {
        dialog.text = body;

        skipRequested = false;

        dialog.ForceMeshUpdate();
        TMP_TextInfo info = dialog.textInfo;
        int count = info.characterCount;
        if (count == 0) yield break;

        float fade = Mathf.Max(typeFadeChars, 0.0001f);
        float head = 0f;                  // current reveal position, in characters

        int lastIndex = -1;

        while (head < count + fade)
        {
            if (skipRequested)
            {
                ApplyReveal(dialog, info, count + fade, fade);

                skipRequested = false;
                typeRoutine = null;
                listenTypeRountineCnt = maxTypeRountineCnt;


                yield break;
            }
            head += Mathf.Max(typeSpeed, 0.01f) * Time.unscaledDeltaTime;
            int currentIndex = Mathf.FloorToInt(head);
            if (currentIndex > lastIndex &&
                currentIndex < body.Length &&
                body[currentIndex] == '\n')
            {
                yield return new WaitForSecondsRealtime(newLinePause);
            }

            // Typewriter blip per revealed char; the cue's own cooldown turns
            // this into a steady tick instead of one blip per frame.

            lastIndex = currentIndex;
            ApplyReveal(dialog, info, head, fade);
            yield return null;
        }
        ApplyReveal(dialog, info, count + fade, fade);   // make sure everything ends fully visible
        typeRoutine = null;

        listenTypeRountineCnt++;
    }

    // Sets each character's alpha based on how far the reveal "head" has passed it.
    private void ApplyReveal(TMP_Text dialog, TMP_TextInfo info, float head, float fade)
    {
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            
            if (!c.isVisible) continue;           // skip spaces / line breaks

            byte alpha = (byte)(Mathf.Clamp01((head - i) / fade) * 255f);

            int vi = c.vertexIndex;
            Color32[] cols = info.meshInfo[c.materialReferenceIndex].colors32;
            cols[vi + 0].a = alpha;
            cols[vi + 1].a = alpha;
            cols[vi + 2].a = alpha;
            cols[vi + 3].a = alpha;
        }
        dialog.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32); // For all
    }

    public void SpawnChoice(string label, UnityAction onClick)
    {
        /*
        Button button = Instantiate(choiceButtonPrefab, choicesContainer);
        button.GetComponentInChildren<TMP_Text>().text = label;
        button.onClick.AddListener(onClick);
        spawnedButtons.Add(button);
        */
    }

    public void ClearChoices()
    {
        foreach (Button b in spawnedButtons)
            Destroy(b.gameObject);
        spawnedButtons.Clear();
    }

    private IEnumerator AnimateIn()
    {
        float t = 0f;
        while (animDuration > 0f && t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / animDuration));
            canvasGroup.alpha = useFade ? k : 1f;
            slideOffset = useSlide ? new Vector2(Mathf.Lerp(-slideDistance, 0f, k), 0f) : Vector2.zero;
            ApplyTransform();
            yield return null;
        }
        canvasGroup.alpha = 1f;
        slideOffset = Vector2.zero;
        ApplyTransform();
    }

    private IEnumerator AnimateOut()
    {
        float startAlpha = canvasGroup.alpha;
        float t = 0f;

        while(typeRoutine != null)
        {
            yield return null;
        }

        while (animDuration > 0f && t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / animDuration));
            canvasGroup.alpha = useFade ? Mathf.Lerp(startAlpha, 0f, k) : 1f;
            slideOffset = useSlide ? new Vector2(Mathf.Lerp(0f, -slideDistance, k), 0f) : Vector2.zero;
            ApplyTransform();
            yield return null;
        }
        HideInstant();
    }

    private void HideInstant()
    {
        if (animRoutine != null) { StopCoroutine(animRoutine); animRoutine = null; }
        if (typeRoutine != null) { StopCoroutine(typeRoutine); typeRoutine = null; }
        ClearChoices();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        slideOffset = Vector2.zero;
        root.SetActive(false);
    }

    private void StartAnim(IEnumerator routine)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(routine);
    }

    private void LateUpdate()
    {
        ApplyTransform();
    }

    // Puts the view where it should be this frame: bubble follows the character,
    // box sits at its resting spot. The current animation slide offset is added on top of both.
    private void ApplyTransform()
    {
        if (!initialized) return;

        if (followTarget && target != null && Camera.main != null)
        {
            // (Same approach UIManager uses for floating damage text. Needs a Screen Space - Overlay canvas.)
            Vector3 screen = Camera.main.WorldToScreenPoint(target.position + worldOffset);
            root.transform.position = screen + (Vector3)slideOffset;
        }
        else
        {
            rect.anchoredPosition = shownPos + slideOffset;
        }
    }
}
