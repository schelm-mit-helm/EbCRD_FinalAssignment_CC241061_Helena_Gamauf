using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text sensitivityValueText;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField] private Menu menu;

    bool suppressEvents;

    void Awake()
    {
        if (menu == null)
            menu = GetComponentInParent<Menu>();

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }

    void OnEnable()
    {
        RefreshFromSettings();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshFromSettings();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void RefreshFromSettings()
    {
        suppressEvents = true;

        if (sensitivitySlider != null)
            sensitivitySlider.SetValueWithoutNotify(GameSettings.SensitivityToSliderValue(GameSettings.MouseSensitivity));

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);

        suppressEvents = false;
        UpdateValueLabels();
    }

    public void OnBackPressed()
    {
        if (menu != null)
            menu.ShowMainPanel();
        else
            Hide();
    }

    void OnSensitivitySliderChanged(float sliderValue)
    {
        if (suppressEvents)
            return;

        GameSettings.SetMouseSensitivity(GameSettings.SliderValueToSensitivity(sliderValue));
        UpdateValueLabels();
    }

    void OnVolumeSliderChanged(float sliderValue)
    {
        if (suppressEvents)
            return;

        GameSettings.SetMasterVolume(sliderValue);
        UpdateValueLabels();
    }

    void UpdateValueLabels()
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = $"{GameSettings.MouseSensitivity:0.00}";

        if (volumeValueText != null)
            volumeValueText.text = $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
    }
}
