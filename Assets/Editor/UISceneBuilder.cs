using System.Collections.Generic;
using System.IO;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.EditorTools
{
    /// <summary>
    /// Builds the UI scenes and the pause-menu prefab in the Exit 8 layout: a tab bar over a table of
    /// setting rows, Back at the bottom, blurred photo behind everything.
    ///
    /// It exists because wiring ~30 rows by hand in the inspector is miserable and easy to get subtly
    /// wrong. The result is scaffolding, not final art — restyle freely, just keep the component
    /// references intact.
    /// </summary>
    public static class UISceneBuilder
    {
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string BackgroundPath = "Assets/Art/UI/temp_setting_background_blur.png";
        /// <summary>
        /// Font used by every generated label. Assigned directly rather than relying on
        /// Project Settings > TextMesh Pro > Default Font Asset: a project-wide setting is easy to
        /// forget after a fresh clone, and the failure mode is silent (labels quietly fall back to
        /// LiberationSans and Vietnamese turns into empty boxes).
        /// </summary>
        private const string UIFontPath = "Assets/Font/UI Font SDF.asset";

        /// <summary>
        /// Optional second font for the two big titles. A display face — condensed, heavy, all caps —
        /// carries a title well and reads badly in a seven-row table, so games normally pair one with
        /// a plain sans for body text. If this asset does not exist the titles just use the UI font.
        /// </summary>
        private const string DisplayFontPath = "Assets/Font/UI Display SDF.asset";
        private const string SceneFolder = "Assets/Scenes/UI";
        private const string PrefabFolder = "Assets/Prefabs/UI";
        private const string SettingsScenePath = SceneFolder + "/Settings.unity";
        private const string MainMenuScenePath = SceneFolder + "/MainMenu.unity";
        private const string PausePrefabPath = PrefabFolder + "/PauseCanvas.prefab";

        // Colours, text sizes and spacing all live in UITheme so the whole UI stays one system.

        [MenuItem("HCMUS-8/UI/Build Settings + Main Menu scenes")]
        public static void BuildAll()
        {
            if (!CheckTextMeshPro()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(PrefabFolder);
            PrepareBackgroundSprite();

            cachedFont = null;
            cachedDisplayFont = null;
            if (UIFont == null)
                Debug.LogWarning(
                    "UISceneBuilder: no font asset found anywhere in the project, so labels will use " +
                    "the TMP default. Generate one with Window > TextMeshPro > Font Asset Creator, " +
                    "character set from Assets/Font/vietnamese-charset.txt, or Vietnamese text will " +
                    "show as boxes.");

            BuildSettingsScene();
            BuildMainMenuScene();
            BuildPausePrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddScenesToBuildSettings();

            EditorUtility.DisplayDialog(
                "UI scenes built",
                "Created:\n" +
                SettingsScenePath + "\n" +
                MainMenuScenePath + "\n" +
                PausePrefabPath + "\n\n" +
                "Drag PauseCanvas into FirstPerson.unity to get the in-game pause menu.",
                "OK");
        }

        /// <summary>
        /// Drops the pause canvas into whatever scene is open and makes sure the player can receive
        /// the Game-tab settings. Saves dragging two things into FirstPerson.unity by hand.
        /// </summary>
        [MenuItem("HCMUS-8/UI/Add pause menu to open scene")]
        public static void AddPauseMenuToOpenScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog(
                    "No pause prefab",
                    "Run HCMUS-8 > UI > Build Settings + Main Menu scenes first.",
                    "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();

            // Rebuilding the scenes writes a brand-new prefab at the same path, so every object
            // inside it gets fresh file ids. An instance placed before that rebuild still points at
            // the old ids: Unity does not flag it as missing, it just ends up half-wired. Offer to
            // swap it rather than silently leaving the stale one in place.
            PauseMenu existing = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Pause menu already in this scene",
                    "Replace it with the freshly built prefab?\n\n" +
                    "Say Replace if you have just re-run the scene builder. Keep is only safe if the " +
                    "prefab has not been rebuilt since this instance was placed.",
                    "Replace",
                    "Keep");

                if (!replace)
                {
                    EnsureCameraBinder();
                    EditorSceneManager.MarkSceneDirty(scene);
                    return;
                }

                Undo.DestroyObjectImmediate(existing.transform.root.gameObject);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add pause menu");

            CreateEventSystem();

            FirstPersonController player = EnsureCameraBinder();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog(
                "Pause menu added",
                player != null
                    ? "Pause canvas and CameraSettingsBinder are in the scene. Save it to keep them."
                    : "Pause canvas added. No FirstPersonController here, so no camera binder was needed.",
                "OK");
        }

        /// <summary>
        /// The camera settings (invert, sensitivity, shake, FOV) reach the player through this
        /// binder. Adding it is idempotent, so it is safe on every run.
        /// </summary>
        private static FirstPersonController EnsureCameraBinder()
        {
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>(
                FindObjectsInactive.Include);
            if (player != null && player.GetComponent<CameraSettingsBinder>() == null)
                Undo.AddComponent<CameraSettingsBinder>(player.gameObject);
            return player;
        }

        // ---- scenes ----------------------------------------------------------------------------

        private static void BuildSettingsScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMenuCamera();
            CreateEventSystem();
            Transform canvas = CreateCanvas("UICanvas").transform;
            CreateBackground(canvas);

            OptionsScreen options = BuildOptionsScreen(canvas);

            // The doc wants one scene per feature, so this scene needs something to open the screen.
            UIScreenAutoOpen autoOpen = options.gameObject.AddComponent<UIScreenAutoOpen>();
            SetRef(autoOpen, "screen", options);

            EditorSceneManager.SaveScene(scene, SettingsScenePath);
        }

        private static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMenuCamera();
            CreateEventSystem();
            GameObject canvasObject = CreateCanvas("UICanvas");
            Transform canvas = canvasObject.transform;
            CreateBackground(canvas);

            TextMeshProUGUI title = CreateLabel(
                canvas, "Title", "HCMUS 8", UITheme.Display, TextAlignmentOptions.Center, true);
            title.color = UITheme.Ink;
            Anchor(title.rectTransform, new Vector2(0.5f, 0.74f), new Vector2(1000f, 130f));

            GameObject buttons = CreateTransparent(canvas, "MenuButtons", new Vector2(UITheme.TableWidth, 200f));
            Anchor(
                (RectTransform)buttons.transform,
                new Vector2(0.5f, 0.42f),
                new Vector2(UITheme.TableWidth, 200f));
            VerticalLayout(buttons, UITheme.RowGap, 0);

            MenuButtonRow play = CreateMenuRow(buttons.transform, "PlayRow", "menu.play");
            MenuButtonRow settings = CreateMenuRow(buttons.transform, "SettingsRow", "menu.options");
            MenuButtonRow quit = CreateMenuRow(buttons.transform, "QuitRow", "menu.quit_desktop");

            RowMenu rowMenu = buttons.AddComponent<RowMenu>();
            SetRef(rowMenu, "inputActions", LoadInputActions());

            OptionsScreen options = BuildOptionsScreen(canvas);

            MainMenuController controller = canvasObject.AddComponent<MainMenuController>();
            SetRef(controller, "inputActions", LoadInputActions());
            SetRef(controller, "titleObject", title.gameObject);
            SetRef(controller, "optionsScreen", options);
            SetRef(controller, "menuButtons", buttons);
            SetRef(controller, "rowMenu", rowMenu);
            SetColor(controller, "activeTabColor", UITheme.TabActive);
            SetColor(controller, "inactiveTabColor", UITheme.TabInactive);
            SetColor(controller, "activeTabTextColor", UITheme.Ink);
            SetColor(controller, "inactiveTabTextColor", UITheme.InkOnDark);
            SetString(controller, "gameScene", "FirstPerson");

            AddRowListener(play, controller.OnPlay);
            AddRowListener(settings, controller.OnSettings);
            AddRowListener(quit, controller.OnQuit);

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void BuildPausePrefab()
        {
            GameObject canvasObject = CreateCanvas("PauseCanvas");
            // Above gameplay UI but below the brightness overlay.
            canvasObject.GetComponent<Canvas>().sortingOrder = 100;
            Transform canvas = canvasObject.transform;

            GameObject screenObject = CreateTransparent(canvas, "PauseScreen", Vector2.zero);
            Stretch((RectTransform)screenObject.transform);
            screenObject.AddComponent<CanvasGroup>();

            // Frozen, blurred copy of the last gameplay frame — the Exit 8 backdrop. It draws
            // first, then the veil darkens it, then the menu sits on top.
            GameObject blurObject = new GameObject("BlurredFrame", typeof(RectTransform), typeof(RawImage));
            blurObject.transform.SetParent(screenObject.transform, false);
            Stretch((RectTransform)blurObject.transform);
            RawImage blurImage = blurObject.GetComponent<RawImage>();
            blurImage.raycastTarget = false;
            PauseBlur pauseBlur = blurObject.AddComponent<PauseBlur>();
            SetRef(pauseBlur, "target", blurImage);

            GameObject veil = CreatePanel(screenObject.transform, "Veil", Vector2.zero);
            Stretch((RectTransform)veil.transform);
            // Dark rather than light: the light row strips need something to sit against, and a
            // dark veil also separates the menu from the lit game world behind it.
            veil.GetComponent<Image>().color = UITheme.PauseVeil;
            veil.GetComponent<Image>().raycastTarget = false;

            GameObject buttons = CreateTransparent(screenObject.transform, "MenuButtons", new Vector2(UITheme.TableWidth, 200f));
            Anchor(
                (RectTransform)buttons.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(UITheme.TableWidth, 200f));
            VerticalLayout(buttons, UITheme.RowGap, 0);

            MenuButtonRow resume = CreateMenuRow(buttons.transform, "ResumeRow", "menu.resume");
            MenuButtonRow optionsRow = CreateMenuRow(buttons.transform, "OptionsRow", "menu.options");
            MenuButtonRow quitRow = CreateMenuRow(buttons.transform, "QuitRow", "menu.quit_main");

            RowMenu rowMenu = buttons.AddComponent<RowMenu>();
            SetRef(rowMenu, "inputActions", LoadInputActions());

            OptionsScreen options = BuildOptionsScreen(canvas);

            PauseMenu pause = screenObject.AddComponent<PauseMenu>();
            SetRef(pause, "optionsScreen", options);
            SetRef(pause, "menuButtons", buttons);
            SetRef(pause, "rowMenu", rowMenu);
            SetRef(pause, "pauseBlur", pauseBlur);
            SetRef(pause, "inputActions", LoadInputActions());
            SetString(pause, "cancelActionPath", "UI/Cancel");
            SetString(pause, "mainMenuScene", "MainMenu");

            AddRowListener(resume, pause.OnResume);
            AddRowListener(optionsRow, pause.OnOptions);
            AddRowListener(quitRow, pause.OnQuitToMain);

            PrefabUtility.SaveAsPrefabAsset(canvasObject, PausePrefabPath);
            Object.DestroyImmediate(canvasObject);
        }

        // ---- the shared options screen -----------------------------------------------------------

        private static OptionsScreen BuildOptionsScreen(Transform parent)
        {
            GameObject root = CreateTransparent(parent, "OptionsScreen", Vector2.zero);
            Stretch((RectTransform)root.transform);
            root.AddComponent<CanvasGroup>();
            OptionsScreen screen = root.AddComponent<OptionsScreen>();

            // Everything lives under Content, which the screen deactivates while closed so a hidden
            // menu cannot keep reading input.
            GameObject content = CreateTransparent(root.transform, "Content", Vector2.zero);
            Stretch((RectTransform)content.transform);

            // The title rides on its own strip. Floating white text worked in Exit 8 because their
            // corridor is uniformly white; ours sits over a live game world, so it needs its own
            // background to stay readable wherever the player is standing.
            GameObject titlePlate = CreatePanel(content.transform, "TitlePlate", Vector2.zero);
            titlePlate.GetComponent<Image>().color = UITheme.Row;
            RectTransform plateRect = (RectTransform)titlePlate.transform;
            plateRect.anchorMin = new Vector2(0f, 1f);
            plateRect.anchorMax = new Vector2(0f, 1f);
            plateRect.pivot = new Vector2(0f, 1f);
            plateRect.anchoredPosition = new Vector2(UITheme.SpaceHuge * 2f, -UITheme.SpaceHuge * 1.5f);
            plateRect.sizeDelta = new Vector2(320f, 76f);

            TextMeshProUGUI title = CreateLabel(
                titlePlate.transform, "Title", "Options", UITheme.Title, TextAlignmentOptions.Left, true);
            SetString(title.gameObject.AddComponent<LocalizedLabel>(), "key", "menu.options");
            title.color = UITheme.Ink;
            Stretch(title.rectTransform);
            title.margin = new Vector4(UITheme.SpaceLarge, 0f, 0f, 0f);

            // Tall enough for the longest tab. A VerticalLayoutGroup shrinks its children when they
            // do not fit, so a short body silently squashed the Controls rows into slivers rather
            // than overflowing where it would have been obvious.
            GameObject body = CreateTransparent(
                content.transform, "Body", new Vector2(UITheme.TableWidth, 830f));
            Anchor(
                (RectTransform)body.transform,
                new Vector2(0.5f, 0.56f),
                new Vector2(UITheme.TableWidth, 830f));
            // No gap: the active tab should look welded to the table under it.
            VerticalLayout(body, 0f, 0);

            // --- tab bar
            GameObject tabBar = CreateTransparent(
                body.transform, "TabBar", new Vector2(UITheme.TableWidth, UITheme.TabHeight));
            HorizontalLayout(tabBar, 0f, 0);
            LayoutElement tabBarElement = tabBar.AddComponent<LayoutElement>();
            tabBarElement.preferredHeight = UITheme.TabHeight;

            string[] tabTitles = { "tab.game", "tab.video", "tab.graphics", "tab.audio", "tab.controls" };
            Button[] tabButtons = new Button[tabTitles.Length];
            for (int i = 0; i < tabTitles.Length; i++)
            {
                tabButtons[i] = CreateTabButton(tabBar.transform, tabTitles[i]);
                SetString(
                    tabButtons[i].GetComponentInChildren<TextMeshProUGUI>().gameObject
                        .AddComponent<LocalizedLabel>(),
                    "key",
                    tabTitles[i]);
                tabButtons[i].GetComponent<LayoutElement>().preferredWidth =
                    UITheme.TableWidth / tabTitles.Length;
            }

            // --- tab contents
            GameObject contentHost = CreateTransparent(
                body.transform, "Tabs", new Vector2(UITheme.TableWidth, 778f));
            LayoutElement contentElement = contentHost.AddComponent<LayoutElement>();
            contentElement.preferredHeight = 778f;

            GameObject gameTab = BuildGameTab(contentHost.transform);
            GameObject videoTab = BuildVideoTab(contentHost.transform);
            GameObject graphicsTab = BuildGraphicsTab(contentHost.transform);
            GameObject audioTab = BuildAudioTab(contentHost.transform);
            GameObject controlsTab = BuildControlsTab(contentHost.transform);

            // --- back
            MenuButtonRow back = CreateMenuRow(content.transform, "BackRow", "menu.back");
            Anchor((RectTransform)back.transform, new Vector2(0.5f, 0.14f), new Vector2(320f, 56f));

            RowMenu rowMenu = content.AddComponent<RowMenu>();
            SetRef(rowMenu, "inputActions", LoadInputActions());

            OptionsTabController controller = content.AddComponent<OptionsTabController>();
            SetRef(controller, "inputActions", LoadInputActions());
            SetRef(controller, "backRow", back);
            SetRef(controller, "rowMenu", rowMenu);
            SetTabs(controller, tabTitles, tabButtons,
                new[] { gameTab, videoTab, graphicsTab, audioTab, controlsTab });

            SetFloat(screen, "fadeDuration", UITheme.ScreenFade);
            SetRef(screen, "contentRoot", content);
            SetRef(screen, "tabController", controller);
            return screen;
        }

        private static GameObject BuildGameTab(Transform parent)
        {
            GameObject tab = CreateTable(parent, "GameTab");
            GameOptionsTab component = tab.AddComponent<GameOptionsTab>();

            SetRef(component, "languageRow", CreateCycleRow(tab.transform, "Language"));
            SetRef(component, "invertHorizontalRow", CreateCycleRow(tab.transform, "InvertHorizon"));
            SetRef(component, "invertVerticalRow", CreateCycleRow(tab.transform, "InvertVertical"));
            SetRef(component, "sensitivityHorizontalRow", CreateSliderRow(tab.transform, "SensitivityHorizon"));
            SetRef(component, "sensitivityVerticalRow", CreateSliderRow(tab.transform, "SensitivityVertical"));
            SetRef(component, "accelerationRow", CreateSliderRow(tab.transform, "Acceleration"));
            SetRef(component, "shakeRow", CreateSliderRow(tab.transform, "Shake"));
            return tab;
        }

        private static GameObject BuildVideoTab(Transform parent)
        {
            GameObject tab = CreateTable(parent, "VideoTab");
            VideoOptionsTab component = tab.AddComponent<VideoOptionsTab>();

            SetRef(component, "displayModeRow", CreateCycleRow(tab.transform, "DisplayMode"));
            SetRef(component, "resolutionRow", CreateCycleRow(tab.transform, "Resolution"));
            SetRef(component, "fovRow", CreateSliderRow(tab.transform, "Fov"));
            SetRef(component, "frameRateRow", CreateCycleRow(tab.transform, "FrameRate"));
            SetRef(component, "vsyncRow", CreateCycleRow(tab.transform, "VSync"));
            SetRef(component, "motionBlurRow", CreateCycleRow(tab.transform, "MotionBlur"));
            SetRef(component, "brightnessRow", CreateSliderRow(tab.transform, "Brightness"));
            return tab;
        }

        private static GameObject BuildGraphicsTab(Transform parent)
        {
            GameObject tab = CreateTable(parent, "GraphicsTab");
            GraphicsOptionsTab component = tab.AddComponent<GraphicsOptionsTab>();

            SetRef(component, "presetRow", CreateCycleRow(tab.transform, "Preset"));
            SetRef(component, "renderScaleRow", CreateSliderRow(tab.transform, "RenderScale"));
            SetRef(component, "lightingRow", CreateCycleRow(tab.transform, "Lighting"));
            SetRef(component, "reflectionRow", CreateCycleRow(tab.transform, "Reflection"));
            SetRef(component, "antiAliasingRow", CreateCycleRow(tab.transform, "AntiAliasing"));
            SetRef(component, "postProcessingRow", CreateCycleRow(tab.transform, "PostProcessing"));
            return tab;
        }

        private static GameObject BuildAudioTab(Transform parent)
        {
            GameObject tab = CreateTable(parent, "AudioTab");
            AudioOptionsTab component = tab.AddComponent<AudioOptionsTab>();

            SetRef(component, "masterRow", CreateSliderRow(tab.transform, "Master"));
            SetRef(component, "musicRow", CreateSliderRow(tab.transform, "Music"));
            SetRef(component, "sfxRow", CreateSliderRow(tab.transform, "Sfx"));
            return tab;
        }

        private static GameObject BuildControlsTab(Transform parent)
        {
            GameObject tab = CreateTable(parent, "ControlsTab");
            KeyboardRebindManager manager = tab.AddComponent<KeyboardRebindManager>();

            CreateRebindHeader(tab.transform);

            // One row per slot in the table — including the arrow-key and alternate rows, so no
            // binding is left invisible.
            List<RebindEntry> entries = new List<RebindEntry>();
            foreach (InputBindingService.BindingDefinition definition in InputBindingService.Definitions)
                entries.Add(CreateRebindRow(tab.transform, manager, definition));

            TextMeshProUGUI message = CreateLabel(
                tab.transform, "MessageLabel", string.Empty, UITheme.Caption, TextAlignmentOptions.Center);
            message.color = UITheme.InkOnDark;
            message.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            // A row rather than a button, so keyboard and controller reach it like everything else.
            MenuButtonRow reset = CreateMenuRow(tab.transform, "ResetRow", "menu.reset_defaults");
            AddRowListener(reset, manager.ResetToDefaults);

            SetRef(manager, "inputActions", LoadInputActions());
            SetRef(manager, "messageLabel", message);
            SetArray(manager, "entries", entries.ToArray());
            return tab;
        }

        // ---- row construction --------------------------------------------------------------------

        /// <summary>
        /// The shared shell of a row: strip, accent bar, and a caption.
        ///
        /// Value widgets are anchored to the row's right edge by every caller, using the same
        /// measurements from <see cref="UITheme"/>, which is what puts every value in one straight
        /// column. Anchoring them inside a floating "value area" was why the table looked ragged.
        /// </summary>
        private static GameObject CreateRowShell(Transform parent, string name, out TextMeshProUGUI caption)
        {
            GameObject row = CreatePanel(parent, name + "Row", new Vector2(UITheme.TableWidth, UITheme.RowHeight));
            row.GetComponent<Image>().color = UITheme.Row;
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = UITheme.RowHeight;
            // A minimum as well: without it the layout group shrinks rows to fit and the table turns
            // into a stack of hairlines the moment a tab has one row too many.
            rowLayout.minHeight = UITheme.RowHeight;

            GameObject accent = CreatePanel(row.transform, "AccentBar", Vector2.zero);
            RectTransform accentRect = (RectTransform)accent.transform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(UITheme.AccentBarWidth, 0f);
            Image accentImage = accent.GetComponent<Image>();
            accentImage.color = new Color(UITheme.Accent.r, UITheme.Accent.g, UITheme.Accent.b, 0f);
            accentImage.raycastTarget = false;

            caption = CreateLabel(row.transform, "Caption", name, UITheme.Body, TextAlignmentOptions.MidlineLeft);
            RectTransform captionRect = caption.rectTransform;
            captionRect.anchorMin = new Vector2(0f, 0f);
            captionRect.anchorMax = new Vector2(0.6f, 1f);
            captionRect.offsetMin = new Vector2(UITheme.RowPaddingLeft, 0f);
            captionRect.offsetMax = Vector2.zero;
            caption.color = UITheme.Ink;

            return row;
        }

        /// <summary>Anchors a widget a fixed distance in from the row's right edge.</summary>
        private static void AnchorRight(RectTransform rect, float rightInset, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-rightInset, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// The value column plus its two ‹ › marks. Shared by cycle rows and rebind rows so both look
        /// identical in the table — only what the marks do differs.
        ///
        /// The marks sit outside the column, so showing and hiding them never shifts the value.
        /// </summary>
        private static TextMeshProUGUI CreateValueColumn(GameObject row, out Button left, out Button right)
        {
            TextMeshProUGUI value = CreateLabel(
                row.transform, "ValueLabel", "-", UITheme.Body, TextAlignmentOptions.Center);
            AnchorRight(value.rectTransform, UITheme.ValueColumnRight, UITheme.ValueColumnWidth, UITheme.RowHeight);

            right = CreateArrowButton(row.transform, "Next", "\u203A");
            AnchorRight(right.GetComponent<RectTransform>(), UITheme.SpaceMedium, UITheme.ArrowSize, UITheme.ArrowSize);

            left = CreateArrowButton(row.transform, "Prev", "\u2039");
            AnchorRight(
                left.GetComponent<RectTransform>(),
                UITheme.ValueColumnRight + UITheme.ValueColumnWidth + UITheme.SpaceSmall,
                UITheme.ArrowSize,
                UITheme.ArrowSize);

            return value;
        }

        private static CycleSettingsRow CreateCycleRow(Transform parent, string name)
        {
            GameObject row = CreateRowShell(parent, name, out TextMeshProUGUI caption);
            TextMeshProUGUI value = CreateValueColumn(row, out Button left, out Button right);

            CycleSettingsRow component = row.AddComponent<CycleSettingsRow>();
            SetRowRefs(row, component);
            SetRef(component, "labelText", caption);
            SetRef(component, "valueText", value);
            SetRef(component, "leftArrow", left);
            SetRef(component, "rightArrow", right);
            return component;
        }

        private static SliderSettingsRow CreateSliderRow(Transform parent, string name)
        {
            GameObject row = CreateRowShell(parent, name, out TextMeshProUGUI caption);

            // The read-out box is centred on the same column as a cycle row's value, so both kinds of
            // row line up down the table.
            float boxInset = UITheme.ValueColumnRight + (UITheme.ValueColumnWidth - UITheme.ValueBoxWidth) * 0.5f;

            GameObject box = CreatePanel(row.transform, "ValueBox", Vector2.zero);
            box.GetComponent<Image>().color = UITheme.ValueBox;
            AnchorRight((RectTransform)box.transform, boxInset, UITheme.ValueBoxWidth, UITheme.ValueBoxHeight);

            TextMeshProUGUI value = CreateLabel(
                box.transform, "ValueLabel", "0.0", UITheme.Body, TextAlignmentOptions.Center);
            Stretch(value.rectTransform);
            value.color = UITheme.Ink;

            Slider slider = CreateSlider(row.transform, name);
            AnchorRight(
                slider.GetComponent<RectTransform>(),
                boxInset + UITheme.ValueBoxWidth + UITheme.SpaceMedium,
                UITheme.SliderWidth,
                UITheme.ValueBoxHeight);

            SliderSettingsRow component = row.AddComponent<SliderSettingsRow>();
            SetRowRefs(row, component);
            SetRef(component, "labelText", caption);
            SetRef(component, "slider", slider);
            SetRef(component, "valueText", value);
            SetRef(component, "valueBox", box.GetComponent<Image>());
            return component;
        }

        private static RebindEntry CreateRebindRow(
            Transform parent,
            KeyboardRebindManager manager,
            InputBindingService.BindingDefinition definition)
        {
            GameObject row = CreateRowShell(parent, definition.id.ToString(), out TextMeshProUGUI caption);
            caption.text = definition.displayName;

            // Two columns, keyboard then gamepad, matching the header above the table. Each one is a
            // transparent button so it can be clicked directly as well as reached with left/right.
            Button keyboardButton = CreateColumnButton(row.transform, "KeyboardValue", KeyboardColumnInset);
            Button gamepadButton = CreateColumnButton(row.transform, "GamepadValue", UITheme.ValueColumnRight);

            RebindEntry entry = row.AddComponent<RebindEntry>();
            SetRowRefs(row, entry);
            SetRef(entry, "labelText", caption);
            SetRef(entry, "manager", manager);
            SetEnum(entry, "binding", (int)definition.id);
            SetRef(entry, "keyboardLabel", keyboardButton.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(entry, "keyboardButton", keyboardButton);
            SetRef(entry, "gamepadLabel", gamepadButton.GetComponentInChildren<TextMeshProUGUI>());
            SetRef(entry, "gamepadButton", gamepadButton);
            return entry;
        }

        /// <summary>Left edge of the keyboard column, measured from the row's right edge.</summary>
        private const float KeyboardColumnInset =
            UITheme.ValueColumnRight + UITheme.RebindColumnWidth + UITheme.SpaceMedium;

        /// <summary>
        /// A value cell in the Controls tab: text with an invisible button behind it. Invisible
        /// because a boxed button in the middle of the table read as a stray widget next to the plain
        /// values of every other tab — but it still has to be clickable.
        /// </summary>
        private static Button CreateColumnButton(Transform parent, string name, float rightInset)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            AnchorRight(
                (RectTransform)buttonObject.transform,
                rightInset,
                UITheme.RebindColumnWidth,
                UITheme.RowHeight);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);

            TextMeshProUGUI label = CreateLabel(
                buttonObject.transform, "Value", "-", UITheme.Body, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            // "L-Stick / D-Pad" wrapped onto a second line and spilled into the rows above and below.
            // One line, shrink-to-fit if it has to: a value must never change its row's height.
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMin = UITheme.Caption;
            label.fontSizeMax = UITheme.Body;
            return buttonObject.GetComponent<Button>();
        }

        /// <summary>
        /// Column titles over the rebind table. Not a SettingsRow, so keyboard navigation skips it —
        /// a header the highlight can land on is a small but constant annoyance.
        /// </summary>
        private static void CreateRebindHeader(Transform parent)
        {
            float headerHeight = UITheme.RowHeight * 0.66f;

            // On its own strip rather than floating: light text over whatever happens to be behind
            // the menu was unreadable, which is the same rule the rows follow.
            GameObject header = CreatePanel(
                parent, "ColumnHeader", new Vector2(UITheme.TableWidth, headerHeight));
            header.GetComponent<Image>().color = UITheme.TabInactive;
            LayoutElement headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = headerHeight;
            headerLayout.minHeight = headerHeight;

            AddHeaderLabel(
                header.transform, "KeyboardHeader", "controls.keyboard", KeyboardColumnInset, headerHeight);
            AddHeaderLabel(
                header.transform, "GamepadHeader", "controls.gamepad", UITheme.ValueColumnRight, headerHeight);
        }

        private static void AddHeaderLabel(
            Transform parent, string name, string key, float rightInset, float height)
        {
            TextMeshProUGUI label = CreateLabel(
                parent, name, string.Empty, UITheme.Caption, TextAlignmentOptions.Center);
            AnchorRight(label.rectTransform, rightInset, UITheme.RebindColumnWidth, height);
            label.color = UITheme.InkOnDark;
            SetString(label.gameObject.AddComponent<LocalizedLabel>(), "key", key);
        }

        private static MenuButtonRow CreateMenuRow(Transform parent, string name, string labelKey)
        {
            GameObject row = CreateRowShell(parent, name, out TextMeshProUGUI caption);
            caption.alignment = TextAlignmentOptions.Center;
            Stretch(caption.rectTransform);

            MenuButtonRow component = row.AddComponent<MenuButtonRow>();
            SetRowRefs(row, component);
            SetRef(component, "labelText", caption);
            SetString(component, "labelKey", labelKey);
            return component;
        }

        /// <summary>Hooks a row component up to the strip and the accent bar built by the shell.</summary>
        private static void SetRowRefs(GameObject row, SettingsRow component)
        {
            SetRef(component, "background", row.GetComponent<Image>());
            Transform accent = row.transform.Find("AccentBar");
            if (accent != null) SetRef(component, "accentBar", accent.GetComponent<Image>());
        }

        // ---- primitives --------------------------------------------------------------------------

        private static GameObject CreateCanvas(string name)
        {
            GameObject canvasObject = new GameObject(
                name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // Match height: the table is much narrower than the screen, so scaling by height keeps it
            // fully visible on 4:3 and on ultrawide alike.
            scaler.matchWidthOrHeight = 1f;

            // Without this, UI rects land on fractional pixels and every edge — text especially —
            // gets its brightness split across two pixels and reads as blurry at any resolution.
            canvas.pixelPerfect = true;
            return canvasObject;
        }

        /// <summary>
        /// Blurred photo backdrop. The sprite is 1415x860, which matches no common screen, so it is
        /// scaled to cover and cropped rather than stretched — see <see cref="MenuBackground"/>.
        /// </summary>
        private static void CreateBackground(Transform parent)
        {
            GameObject root = CreateTransparent(parent, "Background", Vector2.zero);
            Stretch((RectTransform)root.transform);
            root.transform.SetAsFirstSibling();
            root.AddComponent<RectMask2D>();

            GameObject imageObject = new GameObject("Photo", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            imageObject.transform.SetParent(root.transform, false);
            RectTransform imageRect = (RectTransform)imageObject.transform;
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            image.raycastTarget = false;

            // A null sprite is not obvious on screen — the Image just becomes a flat colour — so say
            // so loudly rather than letting it look like a styling choice.
            if (image.sprite == null)
                Debug.LogError(
                    $"UISceneBuilder: no Sprite at {BackgroundPath}. Select the file and set " +
                    "Texture Type = Sprite (2D and UI) with Sprite Mode = Single, then run this " +
                    "menu again.");

            GameObject scrimObject = CreatePanel(root.transform, "Scrim", Vector2.zero);
            Stretch((RectTransform)scrimObject.transform);
            Image scrim = scrimObject.GetComponent<Image>();
            scrim.color = UITheme.PhotoScrim;
            scrim.raycastTarget = false;

            MenuBackground background = root.AddComponent<MenuBackground>();
            SetRef(background, "image", image);
            SetRef(background, "fitter", imageObject.GetComponent<AspectRatioFitter>());
            SetRef(background, "scrim", scrim);
            background.Apply();
        }

        /// <summary>
        /// A Screen Space Overlay canvas draws without a camera, but a scene with none still leaves
        /// the framebuffer uncleared (garbage or flicker in a build) and has no AudioListener, so the
        /// Audio tab would be untestable. One black camera solves both.
        /// </summary>
        private static void CreateMenuCamera()
        {
            if (Camera.main != null) return;

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            // Nothing 3D to see in a menu scene; skip the work entirely.
            camera.cullingMask = 0;
            camera.orthographic = true;
        }

        private static TMP_FontAsset cachedFont;
        private static TMP_FontAsset cachedDisplayFont;

        /// <summary>
        /// The project's UI font. Looks at the expected path first, then falls back to searching the
        /// project for any font asset that is not the one TextMeshPro ships with.
        ///
        /// The fallback exists because "save it exactly here under exactly this name" is a rule people
        /// break constantly and for good reasons — Font Asset Creator defaults to a different folder
        /// and strips the spaces out of the name. Failing over a filename, after someone has just done
        /// the fiddly part of generating an atlas, is a bad trade.
        /// </summary>
        private static TMP_FontAsset UIFont
        {
            get
            {
                if (cachedFont != null) return cachedFont;

                cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UIFontPath);
                if (cachedFont == null) cachedFont = FindProjectFont();
                return cachedFont;
            }
        }

        /// <summary>
        /// Any font asset in the project other than TMP's bundled default. When several exist, the
        /// one whose name mentions the UI wins, then simply the first — with a line in the console so
        /// nobody has to guess which was picked.
        /// </summary>
        private static TMP_FontAsset FindProjectFont()
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            TMP_FontAsset best = null;
            string bestPath = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Skip what TextMeshPro installed; that is the fallback we are trying to replace.
                if (path.Contains("TextMesh Pro/Resources")) continue;

                TMP_FontAsset candidate = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (candidate == null) continue;

                bool namedForUI = candidate.name.IndexOf("UI", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (best == null || (namedForUI && bestPath != null && !bestPath.Contains("UI")))
                {
                    best = candidate;
                    bestPath = path;
                    if (namedForUI) break;
                }
            }

            if (best != null)
                Debug.Log($"UISceneBuilder: using font asset '{bestPath}'.");

            return best;
        }

        /// <summary>Title font, falling back to the UI font when none has been made.</summary>
        private static TMP_FontAsset DisplayFont
        {
            get
            {
                if (cachedDisplayFont == null)
                    cachedDisplayFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
                return cachedDisplayFont != null ? cachedDisplayFont : UIFont;
            }
        }

        private static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule module = eventSystem.AddComponent<InputSystemUIInputModule>();

            // The project is set to the Input System package only, so the old StandaloneInputModule
            // would not work here.
            InputActionAsset actions = LoadInputActions();
            if (actions != null) module.actionsAsset = actions;
        }

        private static GameObject CreateTable(Transform parent, string name)
        {
            GameObject table = CreateTransparent(parent, name, new Vector2(UITheme.TableWidth, 778f));
            Stretch((RectTransform)table.transform);
            VerticalLayout(table, UITheme.RowGap, 0);
            return table;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            ((RectTransform)panel.transform).sizeDelta = size;
            return panel;
        }

        private static GameObject CreateTransparent(Transform parent, string name, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            ((RectTransform)panel.transform).sizeDelta = size;
            return panel;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            string text,
            float size,
            TextAlignmentOptions alignment,
            bool display = false)
        {
            GameObject label = new GameObject(name, typeof(RectTransform));
            label.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = display ? DisplayFont : UIFont;
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            ((RectTransform)label.transform).sizeDelta = new Vector2(400f, size + 12f);
            return tmp;
        }

        private static Button CreateTabButton(Transform parent, string text)
        {
            GameObject buttonObject = new GameObject(
                text.Replace(".", "_") + "Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<LayoutElement>().preferredHeight = UITheme.TabHeight;
            buttonObject.GetComponent<Image>().color = UITheme.TabInactive;

            TextMeshProUGUI label = CreateLabel(
                buttonObject.transform, "Label", text, UITheme.Body, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            return buttonObject.GetComponent<Button>();
        }

        private static Button CreateArrowButton(Transform parent, string name, string glyph)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f); // invisible hit target, the glyph is the visual

            TextMeshProUGUI label = CreateLabel(
                buttonObject.transform, "Glyph", glyph, UITheme.Body * 1.25f, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.color = UITheme.InkMuted;
            return buttonObject.GetComponent<Button>();
        }

        /// <summary>
        /// A slider built from Unity's built-in UI sprites rather than bare rectangles: a rounded
        /// track, a filled portion in the accent colour and a round knob. Hand-made rectangles gave a
        /// hairline track and a knob that read as a stray dark line.
        ///
        /// The fill area is inset by half the knob so the coloured part ends under the knob's centre
        /// instead of sliding out from under it at the ends.
        /// </summary>
        private static Slider CreateSlider(Transform parent, string name)
        {
            // UISprite is a sliced rounded rectangle, so the knob keeps clean corners at any size.
            // Knob.psd is a circle and would show as a squashed ellipse once the handle is a pill.
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject sliderObject = new GameObject(name + "Slider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);

            float inset = UITheme.SliderKnobWidth * 0.5f;

            GameObject background = CreatePanel(sliderObject.transform, "Background", Vector2.zero);
            StretchHorizontalBar(
                (RectTransform)background.transform, UITheme.SliderTrackHeight, inset);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = rounded;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = UITheme.SliderTrack;

            GameObject fillArea = CreateTransparent(sliderObject.transform, "Fill Area", Vector2.zero);
            StretchHorizontalBar((RectTransform)fillArea.transform, UITheme.SliderTrackHeight, inset);

            GameObject fill = CreatePanel(fillArea.transform, "Fill", Vector2.zero);
            Stretch((RectTransform)fill.transform);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = rounded;
            fillImage.type = Image.Type.Sliced;
            // The filled part is the one place the accent colour appears besides the selection bar.
            fillImage.color = UITheme.Accent;

            GameObject handleArea = CreateTransparent(sliderObject.transform, "Handle Slide Area", Vector2.zero);
            RectTransform handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.offsetMin = new Vector2(inset, 0f);
            handleAreaRect.offsetMax = new Vector2(-inset, 0f);

            GameObject handle = CreatePanel(
                handleArea.transform, "Handle",
                new Vector2(UITheme.SliderKnobWidth, UITheme.SliderKnobHeight));
            RectTransform handleRect = (RectTransform)handle.transform;
            // Fixed height: without this the handle stretches to fill the row.
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(UITheme.SliderKnobWidth, UITheme.SliderKnobHeight);

            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = rounded;
            handleImage.type = Image.Type.Sliced;
            handleImage.color = UITheme.SliderHandle;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = (RectTransform)handle.transform;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        /// <summary>A bar of fixed height, centred vertically, inset equally on both ends.</summary>
        private static void StretchHorizontalBar(RectTransform rect, float height, float inset)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, -height * 0.5f);
            rect.offsetMax = new Vector2(-inset, height * 0.5f);
        }

        // ---- helpers ---------------------------------------------------------------------------

        private static void VerticalLayout(GameObject target, float spacing, int padding)
        {
            VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
        }

        private static void HorizontalLayout(GameObject target, float spacing, int padding)
        {
            HorizontalLayoutGroup layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddClick(Button button, UnityAction call)
        {
            UnityEventTools.AddPersistentListener(button.onClick, call);
        }

        private static void AddRowListener(MenuButtonRow row, UnityAction call)
        {
            UnityEventTools.AddPersistentListener(row.Activated, call);
        }

        private static InputActionAsset LoadInputActions()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
                Debug.LogWarning($"UISceneBuilder: could not find {InputActionsPath}.");
            return actions;
        }

        /// <summary>
        /// The background PNG imports as a plain texture; the UI needs it as a sprite.
        ///
        /// Both settings matter. Sprite Mode must be Single as well as the type being Sprite: in
        /// Multiple mode Unity treats the file as an unsliced sprite sheet, produces no sprite at all,
        /// and the Image silently renders as a flat coloured box instead of the photo.
        /// </summary>
        private static void PrepareBackgroundSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"UISceneBuilder: {BackgroundPath} not found; menus will have no backdrop.");
                return;
            }

            bool needsReimport = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                needsReimport = true;
            }

            if (importer.mipmapEnabled)
            {
                // A full-screen UI image is never minified, so mipmaps only cost memory and blur it.
                importer.mipmapEnabled = false;
                needsReimport = true;
            }

            if (needsReimport) importer.SaveAndReimport();
        }

        private static void SetTabs(
            OptionsTabController controller, string[] titles, Button[] buttons, GameObject[] contents)
        {
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty tabs = so.FindProperty("tabs");
            tabs.arraySize = titles.Length;
            for (int i = 0; i < titles.Length; i++)
            {
                SerializedProperty element = tabs.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("title").stringValue = titles[i];
                element.FindPropertyRelative("tabButton").objectReferenceValue = buttons[i];
                element.FindPropertyRelative("content").objectReferenceValue = contents[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRef(Component target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning($"UISceneBuilder: {target.GetType().Name} has no field '{field}'.");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Component target, string field, string value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) return;
            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Component target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) return;
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Component target, string field, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) return;
            property.enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(Component target, string field, Color value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) return;
            property.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Component target, string field, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool CheckTextMeshPro()
        {
            if (TMP_Settings.defaultFontAsset != null) return true;

            EditorUtility.DisplayDialog(
                "TextMeshPro not set up",
                "Import TMP Essential Resources first:\n" +
                "Window > TextMeshPro > Import TMP Essential Resources.\n\n" +
                "Without them every generated label would be invisible.",
                "OK");
            return false;
        }

        /// <summary>
        /// Registers the two scenes. MainMenu must end up at index 0 — that is the scene a built game
        /// boots into, and booting straight into the Settings test scene would be nonsense.
        /// </summary>
        private static void AddScenesToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            AddSceneIfMissing(scenes, SettingsScenePath);
            AddSceneIfMissing(scenes, MainMenuScenePath);

            // Move MainMenu to the front, wherever it currently sits.
            int menuIndex = scenes.FindIndex(scene => scene.path == MainMenuScenePath);
            if (menuIndex > 0)
            {
                EditorBuildSettingsScene menu = scenes[menuIndex];
                scenes.RemoveAt(menuIndex);
                scenes.Insert(0, menu);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddSceneIfMissing(List<EditorBuildSettingsScene> scenes, string path)
        {
            foreach (EditorBuildSettingsScene scene in scenes)
                if (scene.path == path) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
