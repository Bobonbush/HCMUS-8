using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Code-built phone screen. All dimensions use the same 400 x 700 logical display.</summary>
public static class MainMenuPhoneLayout
{
    static readonly Color Ink = new Color(.91f, .94f, .93f);
    static readonly Color Muted = new Color(.53f, .60f, .58f);

    /// <summary>
    /// Resolves the layout already serialized in MainMenu.unity. Runtime deliberately does not
    /// rebuild it, so changes made in the Unity Editor remain visible and survive Play Mode.
    /// </summary>
    public static void BindExisting(MainMenu3DPresentation menu, out Button phonePlay, out TMP_Text mist)
    {
        phonePlay = null; // PLAY belongs to the elevator, not the booking app.
        Transform screen = menu.phoneScreen != null ? menu.phoneScreen.transform : null;
        Transform prompt = menu.doorPrompt != null ? menu.doorPrompt.transform : null;
        if (screen != null)
        {
            menu.settingsButton = Find<Button>(screen, "Settings");
            menu.cancelButton = Find<Button>(screen, "Quit");
            menu.etaLabel = Find<TMP_Text>(screen, "Arrival");
            menu.floorLabel = Find<TMP_Text>(screen, "Location") ?? menu.floorLabel;
        }
        mist = prompt != null ? Find<TMP_Text>(prompt, "MistWrittenPlay") : null;
        menu.playButton = mist != null ? mist.GetComponent<Button>() : menu.playButton;
        Transform identity = menu.transform.Find("GameIdentity");
        if (identity != null)
        {
            menu.selectionLabel = Find<TMP_Text>(identity, "SelectionHint");
            menu.introFloorLabel = Find<TMP_Text>(identity, "FloorIndicator");
        }
    }

    public static void Build(MainMenu3DPresentation menu, out Button phonePlay, out TMP_Text mist)
    {
        Transform screen = menu.phoneScreen.transform;
        Texture route = menu.routeMap;
        if (route == null)
        {
            var oldMap = screen.GetComponentInChildren<RawImage>();
            if (oldMap != null) route = oldMap.texture;
        }
        TMP_FontAsset font = menu.floorLabel != null ? menu.floorLabel.font : TMP_Settings.defaultFontAsset;
        Clear(screen);
        var bg = Panel(screen, "OLED", new Color(.022f, .029f, .028f), new Vector2(400, 700), Vector2.zero);
        Text(bg.transform, font, "Clock", "21:42", 18, new Vector2(100, 26), new Vector2(-126, 317));
        Text(bg.transform, font, "Battery", "LTE   87%", 16, new Vector2(100, 26), new Vector2(128, 317));
        Panel(bg.transform, "Earpiece", new Color(.005f, .007f, .006f), new Vector2(72, 9), new Vector2(0, 320));
        var brand = Text(bg.transform, font, "AppTitle", "GrabBike", 29, new Vector2(344, 45), new Vector2(0, 264));
        brand.fontStyle = FontStyles.Bold;
        brand.color = new Color(.34f, .83f, .54f);
        Text(bg.transform, font, "Subtitle", "YOUR RIDE HOME", 12, new Vector2(344, 24), new Vector2(0, 232)).color = Muted;
        var mapObject = new GameObject("RouteMap", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        mapObject.transform.SetParent(bg.transform, false);
        var map = mapObject.GetComponent<RawImage>();
        map.texture = route;
        map.color = new Color(.77f, .83f, .79f);
        map.rectTransform.sizeDelta = new Vector2(344, 202);
        map.rectTransform.anchoredPosition = new Vector2(0, 112);
        // Crop to the approach to campus. The original whole-city image was too busy.
        map.uvRect = new Rect(.06f, .20f, .52f, .22f);
        var ride = Panel(bg.transform, "RideStatus", new Color(.065f, .092f, .08f), new Vector2(344, 110), new Vector2(0, -52));
        Text(ride.transform, font, "Arrival", "Arriving in 4 min", 24, new Vector2(302, 34), new Vector2(0, 28));
        Text(ride.transform, font, "Driver", "Minh  /  Honda Vision", 17, new Vector2(302, 28), new Vector2(0, -6)).color = Muted;
        menu.floorLabel = Text(ride.transform, font, "Location", "Pickup: HCMUS main entrance", 15, new Vector2(302, 25), new Vector2(0, -34));
        menu.etaLabel = ride.transform.Find("Arrival").GetComponent<TMP_Text>();
        phonePlay = null; // PLAY belongs to the elevator, not the booking app.
        menu.settingsButton = Row(bg.transform, font, "Settings", "Options", "Game settings", -154, false);
        menu.cancelButton = Row(bg.transform, font, "Quit", "Cancel booking", "Quit game", -241, true);
        Panel(bg.transform, "HomeIndicator", new Color(.68f, .72f, .7f), new Vector2(104, 5), new Vector2(0, -330));
        foreach (var graphic in screen.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = graphic.GetComponent<Button>() != null;
        RoundPhone(menu, screen);

        // Reuse the existing world canvas, but replace the entire image with transparent glyphs.
        Transform prompt = menu.doorPrompt.transform;
        Clear(prompt);
        TMP_FontAsset mistFont = menu.mistFontAsset;
        if (mistFont != null)
        {
            mistFont.TryAddCharacters("PLAY?");
            var material = mistFont.material;
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", new Color(.34f, .62f, .49f, .6f));
            material.SetFloat("_UnderlayDilate", .28f);
            material.SetFloat("_UnderlaySoftness", .9f);
            material.SetFloat("_FaceDilate", -.08f);
            material.SetFloat("_OutlineSoftness", .12f);
        }
        mist = Text(prompt, mistFont != null ? mistFont : font, "MistWrittenPlay", "PLAY?", 110, new Vector2(500, 210), Vector2.zero);
        mist.alignment = TextAlignmentOptions.Center;
        mist.characterSpacing = 12;
        mist.raycastTarget = true;
        menu.playButton = mist.gameObject.AddComponent<Button>();
        menu.playButton.targetGraphic = mist;
        menu.playButton.navigation = new Navigation { mode = Navigation.Mode.None };
        // A second, faint glyph surface adds depth without a rectangular texture/background.
        var echo = Text(prompt, mist.font, "MistDepth", "PLAY?", 110, new Vector2(500, 210), new Vector2(2, -3));
        echo.alignment = TextAlignmentOptions.Center;
        echo.characterSpacing = 12;
        echo.color = new Color(.38f, .64f, .5f, .12f);
        echo.raycastTarget = false;
        echo.transform.localPosition += Vector3.forward * 8;
        echo.transform.SetAsFirstSibling();

        BuildTitle(menu, font);
        // Hand meshes and poses are authored by MainMenu3DInstaller and remain editable in-scene.
    }

    static void RoundPhone(MainMenu3DPresentation menu, Transform screen)
    {
        // Assets are baked by the editor installer, so this exact appearance is visible in Edit Mode.
        var sprite = menu.roundedPanelSprite;
        foreach (var panel in screen.GetComponentsInChildren<Image>())
        {
            if (sprite != null) panel.sprite = sprite;
            panel.type = Image.Type.Sliced;
        }
        foreach (string name in new[] { "SleekPhone", "Glass" })
        {
            Transform body = menu.phoneRig.Find(name);
            if (body == null) continue;
            if (menu.roundedHandsetMesh != null)
                body.GetComponent<MeshFilter>().sharedMesh = menu.roundedHandsetMesh;
        }
    }

    public static Mesh CreateRoundedHandsetMesh()
    {
        const int steps = 8, ringSize = 4 * (steps + 1);
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        for (int ring = 0; ring < 4; ring++)
        {
            float z = ring == 0 ? -.5f : ring == 1 ? -.32f : ring == 2 ? .32f : .5f;
            float inset = ring == 0 || ring == 3 ? .012f : 0;
            for (int corner = 0; corner < 4; corner++)
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (corner * 90 + step * 90f / steps) * Mathf.Deg2Rad;
                    float cx = corner == 0 || corner == 3 ? .415f : -.415f;
                    float cy = corner < 2 ? .452f : -.452f;
                    vertices.Add(new Vector3((cx + Mathf.Cos(angle) * .085f) * (1 - inset), (cy + Mathf.Sin(angle) * .048f) * (1 - inset), z));
                }
        }
        for (int ring = 0; ring < 3; ring++)
            for (int i = 0; i < ringSize; i++)
            {
                int a = ring * ringSize + i, b = ring * ringSize + (i + 1) % ringSize;
                triangles.AddRange(new[] { a, b, b + ringSize, a, b + ringSize, a + ringSize });
            }
        // Separate cap vertices keep glass faces flat while the metal bevel catches light.
        for (int face = 0; face < 2; face++)
        {
            int center = vertices.Count;
            vertices.Add(new Vector3(0, 0, face == 0 ? -.5f : .5f));
            for (int i = 0; i < ringSize; i++) vertices.Add(vertices[face * 3 * ringSize + i]);
            for (int i = 0; i < ringSize; i++)
            {
                int a = center + 1 + i, b = center + 1 + (i + 1) % ringSize;
                triangles.AddRange(face == 0 ? new[] { center, b, a } : new[] { center, a, b });
            }
        }
        var mesh = new Mesh { name = "Rounded handset with metal bevel" };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    static void BuildTitle(MainMenu3DPresentation menu, TMP_FontAsset font)
    {
        var identity = new GameObject("GameIdentity", typeof(RectTransform));
        identity.transform.SetParent(menu.transform, false);

        // The title is real world-space lettering above the same elevator as PLAY?. It remains
        // serialized in the scene, so artists can move, rotate, resize, or rewrite it in Edit Mode.
        var wall = new GameObject("WallTitle", typeof(RectTransform), typeof(Canvas));
        wall.transform.SetParent(identity.transform, false);
        var wallCanvas = wall.GetComponent<Canvas>();
        wallCanvas.renderMode = RenderMode.WorldSpace;
        wallCanvas.worldCamera = menu.viewCamera;
        var wallRect = (RectTransform)wall.transform;
        wallRect.sizeDelta = new Vector2(900, 220);
        wall.transform.position = menu.doorPrompt.transform.position + Vector3.up * 1.12f;
        wall.transform.rotation = menu.doorPrompt.transform.rotation;
        wall.transform.localScale = Vector3.one * .003f;
        TMP_FontAsset wallFont = menu.mistFontAsset != null ? menu.mistFontAsset : font;
        var title = Text(wall.transform, wallFont, "Title", "HCMUS 8", 76, new Vector2(860, 110), new Vector2(0, 34));
        title.characterSpacing = 9;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(.58f, .76f, .66f, .82f);
        var hint = Text(wall.transform, font, "GlanceHint", "LOOK DOWN TO CHECK YOUR PHONE", 17, new Vector2(780, 45), new Vector2(0, -54));
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(.45f, .62f, .54f, .72f);

        var go = new GameObject("MenuHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(identity.transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        menu.selectionLabel = Text(go.transform, font, "SelectionHint", "", 16, new Vector2(1100, 40), Vector2.zero);
        menu.selectionLabel.rectTransform.anchorMin = menu.selectionLabel.rectTransform.anchorMax = Vector2.zero;
        menu.selectionLabel.rectTransform.pivot = Vector2.zero;
        menu.selectionLabel.rectTransform.anchoredPosition = new Vector2(56, 32);
        menu.introFloorLabel = Text(go.transform, font, "FloorIndicator", "", 24, new Vector2(220, 58), Vector2.zero);
        menu.introFloorLabel.alignment = TextAlignmentOptions.MidlineRight;
        menu.introFloorLabel.rectTransform.anchorMin = menu.introFloorLabel.rectTransform.anchorMax = Vector2.one;
        menu.introFloorLabel.rectTransform.pivot = Vector2.one;
        menu.introFloorLabel.rectTransform.anchoredPosition = new Vector2(-56, -46);
    }

    static Button Row(Transform parent, TMP_FontAsset font, string name, string title, string subtitle, float y, bool destructive)
    {
        Image image = Panel(parent, name, new Color(.105f, .125f, .118f), new Vector2(344, 78), new Vector2(0, y));
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var heading = Text(image.transform, font, "Label", title, 24, new Vector2(277, 32), new Vector2(5, 13));
        if (destructive) heading.color = new Color(.91f, .51f, .47f);
        Text(image.transform, font, "Detail", subtitle, 14, new Vector2(277, 24), new Vector2(5, -16)).color = Muted;
        Panel(image.transform, "Selection", new Color(.63f, .88f, .75f), new Vector2(3, 38), new Vector2(-156, 0));
        return button;
    }

    static Image Panel(Transform parent, string name, Color color, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text Text(Transform parent, TMP_FontAsset font, string name, string value, float size, Vector2 bounds, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Ink;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = bounds;
        text.rectTransform.anchoredPosition = position;
        return text;
    }

    static void Clear(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;
            if (Application.isPlaying) Object.Destroy(child);
            else Object.DestroyImmediate(child);
        }
    }

    static T Find<T>(Transform root, string name) where T : Component
    {
        foreach (var component in root.GetComponentsInChildren<T>(true))
            if (component.name == name) return component;
        return null;
    }
}
