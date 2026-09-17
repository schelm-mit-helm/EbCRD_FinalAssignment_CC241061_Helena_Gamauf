using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private SettingsPanelUI settingsPanel;
    [SerializeField] private InputActionReference menuAction;
    [SerializeField] private GameObject firstSelectedButton;
    [SerializeField] private Selectable settingsFirstSelected;
    [SerializeField] private string gameSceneName = SceneNames.Bakery;
    [SerializeField] private bool startsOpen;
    [SerializeField] private bool canClose;

    private bool isOpen;
    private Coroutine focusCoroutine;

    private void OnEnable()
    {
        if (menuAction != null)
        {
            menuAction.action.Enable();
            menuAction.action.performed += Toggle;
        }
    }

    private void OnDisable()
    {
        if (menuAction != null)
        {
            menuAction.action.performed -= Toggle;
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
        MenuInputEnsure.EnsureEventSystem();

        if (settingsPanel == null)
            settingsPanel = menuCanvas != null
                ? menuCanvas.GetComponentInChildren<SettingsPanelUI>(true)
                : GetComponentInChildren<SettingsPanelUI>(true);

        if (settingsFirstSelected == null && settingsPanel != null)
            settingsFirstSelected = settingsPanel.GetComponentInChildren<Slider>(true);

        WireVerticalNavigation(mainPanel);
        if (settingsPanel != null)
            WireVerticalNavigation(settingsPanel.gameObject);

        FixMenuLayout();
        WireMenuButtons();

        if (canClose && string.IsNullOrWhiteSpace(gameSceneName))
            gameSceneName = SceneNames.Bakery;

        if (startsOpen)
            Open();
        else
            Close();
    }

    private void Toggle(InputAction.CallbackContext context)
    {
        if (isOpen && IsSettingsOpen())
        {
            ShowMainPanel();
            return;
        }

        if (!canClose)
            return;

        if (isOpen)
            Resume();
        else
            Open();
    }

    private void Update()
    {
        if (!isOpen || !IsSettingsOpen())
            return;

        if (WasBackInputPressed())
            ShowMainPanel();
    }

    static bool WasBackInputPressed()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;

        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            return true;

        return false;
    }

    bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.gameObject.activeSelf;
    }

    private void Open()
    {
        isOpen = true;

        if (menuCanvas != null)
            menuCanvas.SetActive(true);

        ShowMainPanel();

        if (canClose)
            Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        if (!isOpen)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Close()
    {
        isOpen = false;

        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        ShowMainPanel();

        Time.timeScale = 1f;

        if (canClose)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        ClearSelectedButton();
    }

    public void OpenSettings()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.Show();

        FocusSelectable(settingsFirstSelected);
    }

    public void ShowMainPanel()
    {
        if (settingsPanel != null)
            settingsPanel.Hide();

        if (mainPanel != null)
            mainPanel.SetActive(true);

        FocusSelectable(firstSelectedButton != null ? firstSelectedButton.GetComponent<Selectable>() : null);
    }

    public void StartGame()
    {
        Restart();
    }

    public void Resume()
    {
        if (canClose)
            Close();
        else
            Restart();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        var sceneToLoad = canClose ? SceneNames.Bakery : gameSceneName;

        GameBootstrap.ResetRunState();

        SceneManager.LoadScene(sceneToLoad);
    }

    public void SoundClick()
    {
        AkUnitySoundEngine.PostEvent("Play_Click", gameObject);
    }
    public void SoundStartClick()
    {
        AkUnitySoundEngine.PostEvent("Play_click_enter", gameObject);
    }

    public void ReturnToStartMenu()
    {
        Time.timeScale = 1f;
        GameBootstrap.ResetRunState();
        SceneManager.LoadScene(SceneNames.StartMenu);
    }

    public void Quit()
    {
        Time.timeScale = 1f;

        if (canClose)
        {
            ReturnToStartMenu();
            return;
        }

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void FocusSelectable(Selectable selectable)
    {
        if (focusCoroutine != null)
            StopCoroutine(focusCoroutine);

        focusCoroutine = StartCoroutine(FocusSelectableNextFrame(selectable));
    }

    IEnumerator FocusSelectableNextFrame(Selectable selectable)
    {
        yield return null;

        if (EventSystem.current == null || selectable == null)
            yield break;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        focusCoroutine = null;
    }

    private void ClearSelectedButton()
    {
        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    static void WireVerticalNavigation(GameObject panel)
    {
        if (panel == null)
            return;

        var selectables = panel.GetComponentsInChildren<Selectable>(false);
        if (selectables.Length == 0)
            return;

        System.Array.Sort(selectables, (a, b) =>
            b.transform.position.y.CompareTo(a.transform.position.y));

        for (var i = 0; i < selectables.Length; i++)
        {
            var navigation = selectables[i].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            navigation.selectOnUp = i > 0 ? selectables[i - 1] : null;
            navigation.selectOnDown = i < selectables.Length - 1 ? selectables[i + 1] : null;
            selectables[i].navigation = navigation;
        }
    }

    void FixMenuLayout()
    {
        if (mainPanel == null)
            return;

        var panel = mainPanel.transform.Find("Panel");
        if (panel != null)
        {
            panel.SetAsFirstSibling();

            var panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.raycastTarget = false;
        }

        var title = mainPanel.transform.Find("TitleText");
        if (title != null)
            title.SetSiblingIndex(panel != null ? 1 : 0);
    }

    void WireMenuButtons()
    {
        WireButton(mainPanel, "Resume_B", Resume);
        WireButton(mainPanel, "Restart_B", Restart);
        WireButton(mainPanel, "Quit_B", Quit);
        WireButton(mainPanel, "Settings_B", OpenSettings);
        WireButton(mainPanel, "Start_B", StartGame);
        WireButton(mainPanel, "Exit_B", Quit);
        WireButton(menuCanvas, "GiveUpButton", ReturnToStartMenu);

        if (settingsPanel != null)
            WireButton(settingsPanel.gameObject, "Back_B", ShowMainPanel);
    }

    static void WireButton(GameObject panel, string buttonName, UnityEngine.Events.UnityAction action)
    {
        if (panel == null || action == null)
            return;

        var buttonTransform = panel.transform.Find(buttonName);
        if (buttonTransform == null)
            return;

        var button = buttonTransform.GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}