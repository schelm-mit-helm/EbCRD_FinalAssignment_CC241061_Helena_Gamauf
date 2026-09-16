#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuUIBuilder
{
    const string StartMenuScenePath = "Assets/Scenes/Start_Menu_Scene.unity";

    static MenuUIBuilder()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsReloaded()
    {
        TryBuildActiveStartMenuScene();
        TryFixPauseMenuIfNeeded(SceneManager.GetActiveScene());
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == StartMenuScenePath)
            TryBuildActiveStartMenuScene();
        else
            TryFixPauseMenuIfNeeded(scene);
    }

    static void TryBuildActiveStartMenuScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != StartMenuScenePath)
            return;

        if (Object.FindFirstObjectByType<Menu>() != null)
            return;

        BuildStartMenuInCurrentScene();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Start menu UI was added to Start_Menu_Scene. Save the scene (Ctrl+S) to keep it.");
    }

    static void TryFixPauseMenuIfNeeded(Scene scene)
    {
        if (!IsGameplayScene(scene.path))
            return;

        var menu = Object.FindFirstObjectByType<Menu>();
        if (menu == null)
            return;

        var so = new SerializedObject(menu);
        var settingsPanel = so.FindProperty("settingsPanel").objectReferenceValue;
        var mainPanel = so.FindProperty("mainPanel").objectReferenceValue as GameObject;

        if (settingsPanel == null)
        {
            AddSettingsToPauseMenu();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Pause menu settings were added to {scene.name}. Save the scene (Ctrl+S) to keep them.");
            return;
        }

        if (mainPanel != null)
        {
            EnsurePauseMenuTitle(mainPanel.transform);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    static bool IsGameplayScene(string scenePath) =>
        scenePath == "Assets/Scenes/Bakery_Scene.unity"
        || scenePath == "Assets/Scenes/Pantry_Scene.unity"
        || scenePath == "Assets/Scenes/Test_Scene.unity";

    public static void BuildAllMenus()
    {
        BuildStartMenuScene();
        AddSettingsToAllGameScenes();
    }

    [MenuItem("Panik/Build All Menus")]
    static void BuildAllMenusMenuItem() => BuildAllMenus();

    [MenuItem("Panik/Build Start Menu Scene")]
    public static void BuildStartMenuScene()
    {
        var scene = EditorSceneManager.OpenScene(StartMenuScenePath, OpenSceneMode.Single);
        BuildStartMenuInCurrentScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Start menu scene built. Style the UI under Start_Menu_Canvas in the Hierarchy.");
    }

    public static void BuildStartMenuInCurrentScene()
    {
        if (Object.FindFirstObjectByType<Menu>() != null)
            return;

        EnsureEventSystem();
        RemoveExistingMenuRoots();

        var menuManager = new GameObject("StartMenuManager");
        var menu = menuManager.AddComponent<Menu>();

        var canvas = CreateMenuCanvas("Start_Menu_Canvas");
        SetUiLayer(canvas);
        var background = CreatePanel(canvas.transform, "Panel", new Color(1f, 1f, 1f, 0.392f));
        StretchFull(background.GetComponent<RectTransform>());

        var mainPanel = CreateRectObject("MainPanel", canvas.transform);
        StretchFull(mainPanel.GetComponent<RectTransform>());

        CreateTitleText(mainPanel.transform, "Panik in Bakery", 48f, new Vector2(0f, 120f));
        var startButton = CreateMenuButton(mainPanel.transform, "Start_B", "Start", new Vector2(0f, 20f), menu, nameof(Menu.StartGame));
        CreateMenuButton(mainPanel.transform, "Settings_B", "Settings", new Vector2(0f, -40f), menu, nameof(Menu.OpenSettings));
        CreateMenuButton(mainPanel.transform, "Exit_B", "Exit", new Vector2(0f, -100f), menu, nameof(Menu.Quit));

        var settingsPanel = CreateSettingsPanel(canvas.transform, menu);
        settingsPanel.SetActive(false);

        var so = new SerializedObject(menu);
        so.FindProperty("menuCanvas").objectReferenceValue = canvas;
        so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel.GetComponent<SettingsPanelUI>();
        so.FindProperty("firstSelectedButton").objectReferenceValue = startButton;
        so.FindProperty("gameSceneName").stringValue = SceneNames.Bakery;
        so.FindProperty("startsOpen").boolValue = true;
        so.FindProperty("canClose").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Panik/Fix Bakery Pause Menu")]
    public static void FixBakeryPauseMenu()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Bakery_Scene.unity", OpenSceneMode.Single);
        AddSettingsToPauseMenu();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Bakery pause menu updated: title is Menu, Settings button added.");
    }

    [MenuItem("Panik/Add Settings To All Game Scenes")]
      public static void AddSettingsToAllGameScenes()
    {
        var scenePaths = new[]
        {
            "Assets/Scenes/Pantry_Scene.unity",
            "Assets/Scenes/Bakery_Scene.unity",
            "Assets/Scenes/Test_Scene.unity",
        };

        var originalScene = SceneManager.GetActiveScene().path;
        foreach (var scenePath in scenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AddSettingsToPauseMenu();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        if (!string.IsNullOrEmpty(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        Debug.Log("Settings added to all game pause menus.");
    }

    [MenuItem("Panik/Add Settings To Pause Menu")]
    static void AddSettingsToPauseMenu()
    {
        var menu = Object.FindFirstObjectByType<Menu>();
        if (menu == null)
        {
            Debug.LogError("No Menu component found in the open scene.");
            return;
        }

        var so = new SerializedObject(menu);
        var menuCanvas = so.FindProperty("menuCanvas").objectReferenceValue as GameObject;
        if (menuCanvas == null)
        {
            Debug.LogError("Menu.menuCanvas is not assigned.");
            return;
        }

        var canvasTransform = menuCanvas.transform;
        var existingSettings = menuCanvas.GetComponentInChildren<SettingsPanelUI>(true);
        if (existingSettings == null)
            existingSettings = CreateSettingsPanel(canvasTransform, menu).GetComponent<SettingsPanelUI>();

        var mainPanel = so.FindProperty("mainPanel").objectReferenceValue as GameObject;
        if (mainPanel == null)
            mainPanel = CreateOrWrapMainPanel(canvasTransform, menu);

        var settingsButton = FindChildByName(mainPanel.transform, "Settings_B");
        if (settingsButton == null)
            settingsButton = CreateMenuButton(mainPanel.transform, "Settings_B", "Settings", new Vector2(0f, -76f), menu, nameof(Menu.OpenSettings));

        EnsurePauseMenuTitle(mainPanel.transform);
        EnsureMainPanelLayout(mainPanel.transform);
        WirePauseMenuButtons(menu, mainPanel.transform);

        so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        so.FindProperty("settingsPanel").objectReferenceValue = existingSettings;
        so.FindProperty("gameSceneName").stringValue = SceneNames.Bakery;
        so.ApplyModifiedPropertiesWithoutUndo();

        existingSettings.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Settings added to pause menu. Adjust button positions under MainPanel as needed.");
    }

    static void EnsurePauseMenuTitle(Transform mainPanel)
    {
        var titleObject = FindChildByName(mainPanel, "TitleText")
            ?? FindChildByName(mainPanel, "Text (TMP)");

        if (titleObject == null)
        {
            CreateTitleText(mainPanel, "Menu", 36f, new Vector2(0f, 0f));
            return;
        }

        titleObject.name = "TitleText";
        var titleText = titleObject.GetComponent<TMP_Text>();
        if (titleText != null)
            titleText.text = "Menu";
    }

    static void RemoveExistingMenuRoots()
    {
        DestroyIfExists("Start_Menu_Canvas");
        DestroyIfExists("StartMenuManager");
        DestroyIfExists("EventSystem");
    }

    static void DestroyIfExists(string objectName)
    {
        var existing = GameObject.Find(objectName);
        if (existing != null)
            Object.DestroyImmediate(existing);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    static void SetUiLayer(GameObject root)
    {
        if (root == null)
            return;

        root.layer = 5;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = 5;
    }

    static GameObject CreateMenuCanvas(string canvasName)
    {
        var canvasObject = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var rect = canvasObject.GetComponent<RectTransform>();
        StretchFull(rect);
        return canvasObject;
    }

    static GameObject CreateOrWrapMainPanel(Transform canvasTransform, Menu menu)
    {
        var existing = FindChildByName(canvasTransform, "MainPanel");
        if (existing != null)
            return existing;

        var mainPanel = CreateRectObject("MainPanel", canvasTransform);
        StretchFull(mainPanel.GetComponent<RectTransform>());

        for (var i = canvasTransform.childCount - 1; i >= 0; i--)
        {
            var child = canvasTransform.GetChild(i);
            if (child.name == "Panel" || child.name.EndsWith("_B") || child.name == "Text (TMP)" || child.name == "TitleText")
                child.SetParent(mainPanel.transform, true);
        }

        EnsureMainPanelLayout(mainPanel.transform);

        return mainPanel;
    }

    static void EnsureMainPanelLayout(Transform mainPanel)
    {
        var panel = FindChildByName(mainPanel, "Panel");
        if (panel != null)
        {
            panel.transform.SetAsFirstSibling();
            var panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.raycastTarget = false;
        }

        var title = FindChildByName(mainPanel, "TitleText") ?? FindChildByName(mainPanel, "Text (TMP)");
        if (title != null)
            title.transform.SetSiblingIndex(panel != null ? 1 : 0);
    }

    static void WirePauseMenuButtons(Menu menu, Transform mainPanel)
    {
        WireExistingButton(mainPanel, "Resume_B", menu, nameof(Menu.Resume));
        WireExistingButton(mainPanel, "Restart_B", menu, nameof(Menu.Restart));
        WireExistingButton(mainPanel, "Quit_B", menu, nameof(Menu.Quit));
        WireExistingButton(mainPanel, "Settings_B", menu, nameof(Menu.OpenSettings));

        var settingsPanel = menu.GetComponentInChildren<SettingsPanelUI>(true);
        if (settingsPanel != null)
            WireExistingButton(settingsPanel.transform, "Back_B", menu, nameof(Menu.ShowMainPanel));
    }

    static void WireExistingButton(Transform parent, string buttonName, Menu menu, string methodName)
    {
        var buttonObject = FindChildByName(parent, buttonName);
        if (buttonObject == null)
            return;

        var button = buttonObject.GetComponent<Button>();
        if (button == null)
            return;

        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, 0);

        WireButton(button, menu, methodName);
        EditorUtility.SetDirty(button);
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var panel = CreateRectObject(name, parent);
        var image = panel.AddComponent<Image>();
        image.color = color;
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return panel;
    }

    static GameObject CreateTitleText(Transform parent, string text, float fontSize, Vector2 anchoredPosition)
    {
        var titleObject = CreateRectObject("TitleText", parent);
        var rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(500f, 80f);
        rect.anchoredPosition = anchoredPosition;

        var label = titleObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        return titleObject;
    }

    static GameObject CreateMenuButton(Transform parent, string name, string label, Vector2 anchoredPosition, Menu menu, string methodName)
    {
        var buttonObject = CreateRectObject(name, parent);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(160f, 30f);
        rect.anchoredPosition = anchoredPosition;

        var image = buttonObject.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;

        var button = buttonObject.AddComponent<Button>();
        ApplyDefaultButtonColors(button);
        WireButton(button, menu, methodName);

        var textObject = CreateRectObject("Text (TMP)", buttonObject.transform);
        StretchFull(textObject.GetComponent<RectTransform>());
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;

        return buttonObject;
    }

    static GameObject CreateSettingsPanel(Transform parent, Menu menu)
    {
        var panelRoot = CreatePanel(parent, "Settings_Panel", new Color(0.15f, 0.15f, 0.15f, 0.92f));
        StretchFull(panelRoot.GetComponent<RectTransform>());

        var settings = panelRoot.AddComponent<SettingsPanelUI>();
        CreateTitleText(panelRoot.transform, "Settings", 36f, new Vector2(0f, 150f));

        var sensitivityRow = CreateSliderRow(panelRoot.transform, "Sensitivity", new Vector2(0f, 60f), out var sensitivitySlider, out var sensitivityValue);
        var volumeRow = CreateSliderRow(panelRoot.transform, "Sound", new Vector2(0f, 0f), out var volumeSlider, out var volumeValue);
        var backButton = CreateMenuButton(panelRoot.transform, "Back_B", "Back", new Vector2(0f, -120f), menu, nameof(Menu.ShowMainPanel));
        backButton.GetComponent<Button>().onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            backButton.GetComponent<Button>().onClick,
            settings.OnBackPressed);

        var so = new SerializedObject(settings);
        so.FindProperty("sensitivitySlider").objectReferenceValue = sensitivitySlider;
        so.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
        so.FindProperty("sensitivityValueText").objectReferenceValue = sensitivityValue;
        so.FindProperty("volumeValueText").objectReferenceValue = volumeValue;
        so.FindProperty("menu").objectReferenceValue = menu;
        so.ApplyModifiedPropertiesWithoutUndo();

        return panelRoot;
    }

    static GameObject CreateSliderRow(Transform parent, string label, Vector2 anchoredPosition, out Slider slider, out TMP_Text valueText)
    {
        var row = CreateRectObject($"{label}Row", parent);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(420f, 40f);
        rowRect.anchoredPosition = anchoredPosition;

        var labelObject = CreateRectObject("Label", row.transform);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(120f, 30f);
        labelRect.anchoredPosition = new Vector2(60f, 0f);
        var labelText = labelObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 24f;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;

        var sliderObject = CreateRectObject("Slider", row.transform);
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.sizeDelta = new Vector2(-180f, 20f);
        sliderRect.anchoredPosition = new Vector2(90f, 0f);

        var background = CreateRectObject("Background", sliderObject.transform);
        StretchFull(background.GetComponent<RectTransform>());
        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(1f, 1f, 1f, 0.35f);
        backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        backgroundImage.type = Image.Type.Sliced;

        var fillArea = CreateRectObject("Fill Area", sliderObject.transform);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5f, 0f);
        fillAreaRect.offsetMax = new Vector2(-5f, 0f);

        var fill = CreateRectObject("Fill", fillArea.transform);
        StretchFull(fill.GetComponent<RectTransform>());
        var fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.8f, 0.55f, 1f, 1f);
        fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fillImage.type = Image.Type.Sliced;

        var handleSlideArea = CreateRectObject("Handle Slide Area", sliderObject.transform);
        StretchFull(handleSlideArea.GetComponent<RectTransform>());

        var handle = CreateRectObject("Handle", handleSlideArea.transform);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 20f);
        var handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        slider = sliderObject.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;

        var valueObject = CreateRectObject("Value", row.transform);
        var valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.sizeDelta = new Vector2(60f, 30f);
        valueRect.anchoredPosition = new Vector2(-30f, 0f);
        valueText = valueObject.AddComponent<TextMeshProUGUI>();
        valueText.fontSize = 20f;
        valueText.alignment = TextAlignmentOptions.MidlineRight;

        return row;
    }

    static GameObject CreateRectObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static void ApplyDefaultButtonColors(Button button)
    {
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.54f, 0.99f, 1f);
        colors.pressedColor = new Color(0.67f, 0.46f, 0.99f, 1f);
        colors.selectedColor = new Color(0.57f, 0.92f, 0.97f, 1f);
        button.colors = colors;
    }

    static void WireButton(Button button, Menu menu, string methodName)
    {
        switch (methodName)
        {
            case nameof(Menu.StartGame):
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, menu.StartGame);
                break;
            case nameof(Menu.OpenSettings):
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, menu.OpenSettings);
                break;
            case nameof(Menu.Quit):
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, menu.Quit);
                break;
            case nameof(Menu.ShowMainPanel):
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, menu.ShowMainPanel);
                break;
            case nameof(Menu.Resume):
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, menu.Resume);
                break;
            case nameof(Menu.Restart):
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, menu.Restart);
                break;
        }
    }

    static GameObject FindChildByName(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child.gameObject;

            var nested = FindChildByName(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
#endif
