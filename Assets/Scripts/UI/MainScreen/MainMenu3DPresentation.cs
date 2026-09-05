using System.Collections;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Mouse glance and independent, ordered menu navigation over the real Floor prefab.</summary>
[DefaultExecutionOrder(-50)]
public sealed class MainMenu3DPresentation : MonoBehaviour
{
    [HideInInspector] public int presentationRevision;
    public MainMenuController backend;
    public OptionsScreen options;
    public GameObject legacyButtons, legacyTitle, legacyBackground;
    public Camera viewCamera;
    public Transform viewPivot, phoneRig;
    [Tooltip("The single right arm/hand that holds the phone and operates it with its thumb.")]
    public Transform rightHand;
    public CanvasGroup phoneScreen, doorPrompt, blackout, hologramAura;
    public Volume hologramBlur;
    // Names describe screen sides, not world X. The camera faces world -Z.
    public Transform leftDoorA, leftDoorB, rightDoorA, rightDoorB;
    public Transform rightInnerDoorA, rightInnerDoorB;
    public Transform leftInnerDoorA, leftInnerDoorB;
    public Button playButton, settingsButton, cancelButton;
    public TMP_Text etaLabel, floorLabel;
    public Renderer phoneGlass;
    public Font mistFontSource;
    [Tooltip("Persistent editor-baked font used by the elevator PLAY? writing.")]
    public TMP_FontAsset mistFontAsset;
    [Tooltip("Persistent editor-baked rounded panel sprite.")]
    public Sprite roundedPanelSprite;
    [Tooltip("Persistent editor-baked rounded phone body mesh.")]
    public Mesh roundedHandsetMesh;
    public InputActionAsset inputActions;
    public Texture2D routeMap;
    [HideInInspector] public TMP_Text selectionLabel, introFloorLabel;
    [Header("Mouse look and phone pose (tweak in the Inspector)")]
    [Tooltip("Mouse height (0 = bottom, 1 = top) that begins the look-up / phone-lowered pose.")]
    [Range(0.5f, 1f)] public float lookUpMouseThreshold = .82f;
    [Tooltip("Mouse height that begins the look-down / phone-raised pose.")]
    [Range(0f, .5f)] public float lookAtPhoneMouseThreshold = .3f;
    [Tooltip("Camera pitch in degrees while looking down at the phone. Positive pitches downward.")]
    [Range(0f, 35f)] public float phoneLookDownPitch = 14f;
    [Tooltip("Camera pitch in degrees while looking above the phone.")]
    [Range(-20f, 15f)] public float lookUpPitch = -2f;
    [Tooltip("Mouse delta to camera rotation multiplier.")]
    public float lookSensitivity = .035f;
    public float poseSmoothTime = .18f;

    [Header("Natural holding-thumb taps (tweak in the Inspector)")]
    [Tooltip("Extra thumb rotation used for the Options row on the phone screen.")]
    public Vector3 settingsThumbRotation = new Vector3(8f, -7f, 13f);
    [Tooltip("Extra thumb rotation used for the Cancel booking row.")]
    public Vector3 cancelThumbRotation = new Vector3(13f, -11f, 19f);
    [Tooltip("Extra thumb rotation used for the physical side power button.")]
    public Vector3 powerThumbRotation = new Vector3(-8f, 12f, -14f);
    [Range(.2f, .8f)] public float thumbTapDuration = .44f;

    InputAction navigate, submit;
    Button phonePlay;
    TMP_Text mistText;
    Transform thumbTrapez, thumbMeta, thumbProx, thumbDist;
    Vector3 phoneVelocity;
    Quaternion baseRotation;
    float pitch, yaw, targetPose, pose, nextRepeat;
    int selected, heldDirection;
    bool transitioning, screenOn = true, wasOptionsOpen;
    bool originalNavigation;
    EventSystem eventSystem;
    Vector3 mistOrigin;
    PauseBlur settingsBlur;
    readonly System.Collections.Generic.List<Object> ownedAssets = new System.Collections.Generic.List<Object>();
    public void Own(Object asset) => ownedAssets.Add(asset);

    void Awake()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewPivot == null) viewPivot = viewCamera.transform;
        baseRotation = viewPivot.localRotation;
        HideLegacy();
        MainMenuPhoneLayout.BindExisting(this, out phonePlay, out mistText);
        ResolveThumbBones();
        if (playButton == null || settingsButton == null || cancelButton == null || mistText == null)
        {
            Debug.LogError("Main menu presentation is not baked into the scene. Run HCMUS-8/UI/Rebuild editable 3D main menu.", this);
            enabled = false;
            return;
        }
        mistOrigin = mistText.rectTransform.localPosition;
        playButton.onClick.AddListener(Play);
        if (phonePlay != null) phonePlay.onClick.AddListener(Play);
        settingsButton.onClick.AddListener(OpenSettings);
        cancelButton.onClick.AddListener(CancelBooking);
        BindHover(playButton, 0); BindHover(settingsButton, 1); BindHover(cancelButton, 2);
        if (blackout != null) { blackout.alpha = 0; blackout.blocksRaycasts = false; }
        if (hologramAura != null) { hologramAura.alpha = 0; hologramAura.blocksRaycasts = false; }
        phoneRig.localPosition = PhoneShown();
        phoneRig.localRotation = Quaternion.identity;
        var optionsCanvas = options.GetComponent<Canvas>();
        if (optionsCanvas == null) optionsCanvas = options.gameObject.AddComponent<Canvas>();
        optionsCanvas.overrideSorting = true;
        optionsCanvas.sortingOrder = 40;
        if (options.GetComponent<GraphicRaycaster>() == null) options.gameObject.AddComponent<GraphicRaycaster>();
        if (hologramAura != null)
        {
            settingsBlur = hologramAura.GetComponentInChildren<PauseBlur>(true);
            if (settingsBlur == null)
            {
                var blurImage = new GameObject("CapturedBackgroundBlur", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                blurImage.transform.SetParent(hologramAura.transform, false);
                blurImage.transform.SetAsFirstSibling();
                var rect = (RectTransform)blurImage.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                blurImage.GetComponent<RawImage>().raycastTarget = false;
                settingsBlur = blurImage.AddComponent<PauseBlur>();
            }
        }
        StyleHolographicOptions();
        SetSelected(0);
    }

    void StyleHolographicOptions()
    {
        if (options == null) return;
        Color ink = new Color(.72f, 1f, .84f, 1f);
        Color accent = new Color(.28f, 1f, .62f, 1f);
        Color row = new Color(.018f, .075f, .058f, .88f);
        Color rowAlt = new Color(.024f, .095f, .07f, .9f);
        Color selectedRow = new Color(.055f, .27f, .17f, .96f);
        foreach (var settingRow in options.GetComponentsInChildren<SettingsRow>(true))
            settingRow.SetVisualTheme(row, rowAlt, selectedRow, ink, accent);

        var tabs = options.GetComponentInChildren<OptionsTabController>(true);
        if (tabs != null) tabs.SetVisualTheme(
            new Color(.09f, .35f, .22f, .96f), new Color(.012f, .035f, .03f, .94f),
            new Color(.75f, 1f, .85f), new Color(.48f, .7f, .57f));

        foreach (var image in options.GetComponentsInChildren<Image>(true))
        {
            switch (image.name)
            {
                case "TitlePlate": image.color = new Color(.018f, .09f, .065f, .86f); break;
                case "ValueBox": image.color = new Color(.055f, .20f, .13f, .95f); break;
                case "Fill": image.color = accent; break;
                case "Handle": image.color = new Color(.65f, 1f, .79f); break;
                case "Background": image.color = new Color(.06f, .13f, .1f, .9f); break;
                case "ColumnHeader": image.color = new Color(.012f, .035f, .03f, .96f); break;
            }
        }
        foreach (var text in options.GetComponentsInChildren<TMP_Text>(true)) text.color = ink;

        if (hologramAura == null) return;
        Transform frame = hologramAura.transform.Find("HologramFrame");
        if (frame != null)
        {
            var image = frame.GetComponent<Image>();
            if (image != null) image.enabled = false;
            var outline = frame.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
            var rect = frame as RectTransform;
            if (rect != null) rect.sizeDelta = new Vector2(1040, 920);
            Transform scanLine = frame.Find("ScanLine");
            if (scanLine != null) scanLine.gameObject.SetActive(false);
            Transform duplicateTitle = frame.Find("ProjectionTitle");
            if (duplicateTitle != null) duplicateTitle.gameObject.SetActive(false);
        }
        Transform glow = hologramAura.transform.Find("ProjectionGlow");
        if (glow != null && glow.TryGetComponent<Image>(out var glowImage))
            glowImage.enabled = false;
        Transform veil = hologramAura.transform.Find("WorldBlur");
        if (veil != null && veil.TryGetComponent<Image>(out var veilImage))
            veilImage.color = new Color(.005f, .035f, .028f, .2f);
    }

    void Start()
    {
        foreach (var row in backend.GetComponentsInChildren<RowMenu>(true))
            if (!row.transform.IsChildOf(options.transform)) row.enabled = false;
        var asset = inputActions != null ? inputActions : InputBindingService.Instance?.actions;
        navigate = asset?.FindAction("UI/Navigate", false);
        submit = asset?.FindAction("UI/Submit", false);
        eventSystem = EventSystem.current;
        if (eventSystem != null) originalNavigation = eventSystem.sendNavigationEvents;
    }

    void LateUpdate() => HideLegacy();

    void BindHover(Button button, int index)
    {
        var trigger = button.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => { if (!transitioning && !(options != null && options.IsOpen)) SetSelected(index); });
        trigger.triggers.Add(entry);
    }

    void OnEnable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    void OnDestroy()
    {
        foreach (var asset in ownedAssets) if (asset != null) Destroy(asset);
        if (eventSystem != null) eventSystem.sendNavigationEvents = originalNavigation;
    }

    void ResolveThumbBones()
    {
        if (rightHand == null) return;
        foreach (var bone in rightHand.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name == "thumb_trapez") thumbTrapez = bone;
            else if (bone.name == "thumb_meta") thumbMeta = bone;
            else if (bone.name == "thumb_prox") thumbProx = bone;
            else if (bone.name == "thumb_dist") thumbDist = bone;
        }
    }

    void HideLegacy()
    {
        if (legacyButtons != null) legacyButtons.SetActive(false);
        if (legacyTitle != null) legacyTitle.SetActive(false);
        if (legacyBackground != null) legacyBackground.SetActive(false);
    }

    void Update()
    {
        HideLegacy();
        bool settingsOpen = options != null && options.IsOpen;
        // The settings screen owns navigation only while open. Prevent double Submit via uGUI.
        if (eventSystem != null) eventSystem.sendNavigationEvents = settingsOpen && originalNavigation;
        if (transitioning) return;
        if (settingsOpen)
        {
            wasOptionsOpen = true;
            UpdatePose();
            return;
        }
        if (wasOptionsOpen)
        {
            wasOptionsOpen = false;
            heldDirection = 0;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
            SetSelected(1);
            settingsBlur?.Clear();
            UpdatePose();
            return; // The key closing Settings must not immediately activate a menu choice.
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            if (delta.sqrMagnitude > .01f && delta.sqrMagnitude < 2500f && !mouse.leftButton.isPressed)
            {
                float y = mouse.position.ReadValue().y / Mathf.Max(1, Screen.height);
                if (y < lookAtPhoneMouseThreshold) targetPose = 0;
                else if (y > lookUpMouseThreshold) targetPose = 1;
                yaw = Mathf.Clamp(yaw + delta.x * lookSensitivity, -16, 16);
                pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -6, 8);
            }
        }

        // Read the project's existing action bindings, including D-pad and stick, without
        // assigning navigation input any camera/phone-pose side effects.
        navigate?.Enable();
        submit?.Enable();
        float vertical = navigate != null ? navigate.ReadValue<Vector2>().y : 0;
        int direction = vertical > .5f ? -1 : vertical < -.5f ? 1 : 0;
        if (direction == 0) heldDirection = 0;
        else if (direction != heldDirection || Time.unscaledTime >= nextRepeat)
        {
            SetSelected(selected + direction);
            nextRepeat = Time.unscaledTime + (direction != heldDirection ? .4f : .14f);
            heldDirection = direction;
        }
        if (submit != null && submit.WasPressedThisFrame()) ActivateSelected();
        UpdatePose();
    }

    Vector3 PhoneShown()
    {
        // Fit phone AND hands on narrow windows too. Height at 58 degrees stays ~64% of view.
        float distance = Mathf.Max(.69f, .44f / (2 * Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad / 2) * Mathf.Max(.5f, viewCamera.aspect)));
        return new Vector3(0, -.015f, distance);
    }

    void UpdatePose()
    {
        pose = Mathf.MoveTowards(pose, targetPose, Time.unscaledDeltaTime / Mathf.Max(.01f, poseSmoothTime));
        float eased = Mathf.SmoothStep(0, 1, pose);
        Vector3 target = Vector3.Lerp(PhoneShown(), new Vector3(.12f, -.67f, .78f), eased);
        target.y += Mathf.Sin(Time.unscaledTime * 1.35f) * .002f;
        phoneRig.localPosition = Vector3.SmoothDamp(phoneRig.localPosition, target, ref phoneVelocity, poseSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        phoneRig.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(25, -8, 5), eased);
        viewPivot.localRotation = baseRotation * Quaternion.Euler(pitch + Mathf.Lerp(phoneLookDownPitch, lookUpPitch, eased), yaw, 0);
        phoneScreen.alpha = screenOn ? 1 : 0;
        phoneScreen.blocksRaycasts = screenOn && eased < .65f;
        phoneScreen.interactable = phoneScreen.blocksRaycasts;
        // PLAY remains legible while glancing at the phone, and is always keyboard selectable.
        doorPrompt.alpha = Mathf.Lerp(.55f, 1, eased);
        doorPrompt.interactable = !transitioning;
        doorPrompt.blocksRaycasts = !transitioning;
        if (mistText != null)
            mistText.rectTransform.localPosition = mistOrigin + new Vector3(Mathf.Sin(Time.unscaledTime * .41f) * 1.8f, Mathf.Sin(Time.unscaledTime * .63f) * 2.4f, 0);
        if (hologramAura != null)
        {
            float targetAlpha = options != null && options.IsOpen ? 1 : 0;
            hologramAura.alpha = Mathf.MoveTowards(hologramAura.alpha, targetAlpha, Time.unscaledDeltaTime * 4);
            if (hologramBlur != null) hologramBlur.weight = hologramAura.alpha;
        }
    }

    void SetSelected(int value)
    {
        selected = (value % 3 + 3) % 3;
        Button[] buttons = { phonePlay, settingsButton, cancelButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            ColorBlock colors = buttons[i].colors;
            colors.normalColor = i == selected ? new Color(.28f, .47f, .41f) : Color.white;
            colors.highlightedColor = new Color(.43f, .6f, .53f);
            colors.selectedColor = colors.normalColor;
            buttons[i].colors = colors;
            var marker = buttons[i].transform.Find("Selection");
            if (marker != null) marker.gameObject.SetActive(i == selected);
        }
        if (mistText != null) mistText.color = selected == 0 ? new Color(.76f, .92f, .84f) : new Color(.49f, .65f, .59f);
        if (selectionLabel != null)
        {
            string choice = selected == 0 ? "PLAY?  /  elevator" : selected == 1 ? "OPTIONS  /  settings" : "CANCEL BOOKING  /  quit";
            selectionLabel.text = choice + "     |     Up/Down or D-pad  select    Enter / A  confirm";
        }
    }

    void ActivateSelected() { if (selected == 0) Play(); else if (selected == 1) OpenSettings(); else CancelBooking(); }
    public void OpenSettings()
    {
        if (transitioning) return;
        SetSelected(1);
        StartCoroutine(ProjectSettings());
    }
    public void Play() { if (!transitioning) StartCoroutine(Leave(true)); }
    public void CancelBooking() { if (!transitioning) StartCoroutine(Leave(false)); }

    enum ThumbTarget { Settings, Cancel, Power }

    IEnumerator TapThen(System.Action action, ThumbTarget target)
    {
        transitioning = true;
        Quaternion trapez = thumbTrapez != null ? thumbTrapez.localRotation : Quaternion.identity;
        Quaternion meta = thumbMeta != null ? thumbMeta.localRotation : Quaternion.identity;
        Quaternion proximal = thumbProx != null ? thumbProx.localRotation : Quaternion.identity;
        Quaternion distal = thumbDist != null ? thumbDist.localRotation : Quaternion.identity;
        Vector3 rotation = target == ThumbTarget.Settings ? settingsThumbRotation
            : target == ThumbTarget.Cancel ? cancelThumbRotation : powerThumbRotation;
        float duration = Mathf.Max(.2f, thumbTapDuration);
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float progress = Mathf.Clamp01(t / duration);
            // A soft press with a short hold reads like muscle-driven motion. Only actual thumb
            // joints rotate: the arm never translates or scales, so it cannot stretch unnaturally.
            float reach = progress < .55f ? Mathf.SmoothStep(0, 1, progress / .55f)
                : progress < .68f ? 1f : Mathf.SmoothStep(1, 0, (progress - .68f) / .32f);
            if (thumbTrapez != null) thumbTrapez.localRotation = trapez * Quaternion.Euler(rotation * (.22f * reach));
            if (thumbMeta != null) thumbMeta.localRotation = meta * Quaternion.Euler(rotation * (.42f * reach));
            if (thumbProx != null) thumbProx.localRotation = proximal * Quaternion.Euler(rotation * (.72f * reach));
            if (thumbDist != null) thumbDist.localRotation = distal * Quaternion.Euler(rotation * (.48f * reach));
            yield return null;
        }
        if (thumbTrapez != null) thumbTrapez.localRotation = trapez;
        if (thumbMeta != null) thumbMeta.localRotation = meta;
        if (thumbProx != null) thumbProx.localRotation = proximal;
        if (thumbDist != null) thumbDist.localRotation = distal;
        action?.Invoke();
        transitioning = false;
    }

    IEnumerator ProjectSettings()
    {
        yield return TapThen(null, ThumbTarget.Settings);
        transitioning = true;
        bool captured = settingsBlur == null;
        if (settingsBlur != null) settingsBlur.Capture(() => captured = true);
        while (!captured) yield return null;
        backend.OnSettings();
        var rect = options.transform as RectTransform;
        Vector3 position = rect.localPosition, scale = rect.localScale;
        Vector2 origin;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect.parent as RectTransform,
            viewCamera.WorldToScreenPoint(phoneRig.position), null, out origin);
        for (float t = 0; t < .38f; t += Time.unscaledDeltaTime)
        {
            float p = Mathf.SmoothStep(0, 1, t / .38f);
            rect.localPosition = Vector3.Lerp(new Vector3(origin.x, origin.y, position.z), position, p);
            rect.localScale = Vector3.Lerp(scale * .12f, scale, p);
            if (hologramAura != null) hologramAura.alpha = p;
            yield return null;
        }
        rect.localPosition = position; rect.localScale = scale;
        transitioning = false;
    }

    IEnumerator Leave(bool enterElevator)
    {
        transitioning = true;
        if (!enterElevator) yield return TapThen(() => { if (etaLabel != null) etaLabel.text = "Booking cancelled"; }, ThumbTarget.Cancel);
        yield return TapThen(null, ThumbTarget.Power);
        transitioning = true;
        screenOn = false;
        phoneScreen.alpha = 0;
        phoneScreen.blocksRaycasts = false;
        doorPrompt.blocksRaycasts = false;
        Vector3 cameraStart = viewPivot.position;
        Vector3 phoneStart = phoneRig.localPosition;
        Quaternion cameraRotation = viewPivot.rotation;
        Transform[] leaves = enterElevator
            ? new[] { leftDoorA, leftDoorB, leftInnerDoorA, leftInnerDoorB }
            : new[] { rightDoorA, rightDoorB, rightInnerDoorA, rightInnerDoorB };
        var positions = new Vector3[leaves.Length];
        for (int i = 0; i < leaves.Length; i++) if (leaves[i] != null) positions[i] = leaves[i].position;
        // PLAY now uses the left screen elevator; Quit uses the right one.
        Vector3 entrance = leaves[0] != null && leaves[1] != null ? (positions[0] + positions[1]) * .5f : cameraStart + viewPivot.forward * 3;
        Vector3 destination = new Vector3(entrance.x, cameraStart.y, entrance.z - 1.2f);
        for (float t = 0; t < 1.65f; t += Time.unscaledDeltaTime)
        {
            float p = Mathf.SmoothStep(0, 1, t / 1.65f);
            phoneRig.localPosition = Vector3.Lerp(phoneStart, new Vector3(.4f, -.9f, .4f), p);
            {
                float open = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .55f, p));
                for (int i = 0; i < leaves.Length; i++)
                    if (leaves[i] != null) leaves[i].position = positions[i] + Vector3.right * (i % 2 == 0 ? .8f : -.8f) * open;
                float walk = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, 1, p));
                viewPivot.position = Vector3.Lerp(cameraStart, destination, walk);
                viewPivot.rotation = Quaternion.Slerp(cameraRotation, baseRotation, p);
            }
            if (!enterElevator && blackout != null) blackout.alpha = Mathf.InverseLerp(.65f, 1, p);
            yield return null;
        }
        if (enterElevator)
        {
            yield return RideToFloorEight(leaves, positions);
            backend.OnPlay(); // Existing serialized destination is Intro; its text/backend stays intact.
        }
        else backend.OnQuit();
    }

    IEnumerator RideToFloorEight(Transform[] leaves, Vector3[] closedPositions)
    {
        if (selectionLabel != null) selectionLabel.text = "";
        Vector3 position = viewPivot.position;
        Quaternion rotation = viewPivot.rotation;
        float shake = SettingsService.Instance != null ? SettingsService.Instance.GetCameraShake() : 1;
        for (float t = 0; t < .9f; t += Time.unscaledDeltaTime)
        {
            float p = Mathf.SmoothStep(0, 1, t / .9f);
            for (int i = 0; i < leaves.Length; i++)
                if (leaves[i] != null) leaves[i].position = closedPositions[i] + Vector3.right * (i % 2 == 0 ? .8f : -.8f) * (1 - p);
            yield return null;
        }
        foreach (int floor in new[] { 11, 10, 9, 8 })
        {
            if (introFloorLabel != null) introFloorLabel.text = "FLOOR  /  " + floor.ToString("00");
            for (float t = 0; t < .85f; t += Time.unscaledDeltaTime)
            {
                viewPivot.position = position + new Vector3(Mathf.Sin(t * 29) * .0015f, Mathf.Sin(t * 41) * .0025f, 0) * shake;
                yield return null;
            }
        }
        var nearby = new System.Collections.Generic.List<Light>();
        var intensities = new System.Collections.Generic.List<float>();
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.isActiveAndEnabled && Vector3.Distance(light.transform.position, position) < 7)
            { nearby.Add(light); intensities.Add(light.intensity); }
        // A short mechanical stop and irregular power failure; then hand off to existing Intro.
        for (float t = 0; t < 1.15f; t += Time.unscaledDeltaTime)
        {
            float envelope = Mathf.Exp(-t * 3.5f) * shake;
            viewPivot.position = position + new Vector3(Mathf.Sin(t * 61) * .018f, Mathf.Sin(t * 43) * .026f, 0) * envelope;
            viewPivot.rotation = rotation * Quaternion.Euler(Mathf.Sin(t * 35) * .9f * envelope, 0, Mathf.Sin(t * 47) * .6f * envelope);
            bool outage = (t > .08f && t < .17f) || (t > .24f && t < .31f) || t > .58f;
            for (int i = 0; i < nearby.Count; i++) nearby[i].intensity = outage ? 0 : intensities[i];
            if (blackout != null) blackout.alpha = Mathf.InverseLerp(.65f, 1.1f, t);
            yield return null;
        }
        viewPivot.SetPositionAndRotation(position, rotation);
        for (int i = 0; i < nearby.Count; i++) if (nearby[i] != null) nearby[i].intensity = intensities[i];
    }
}
