using System;
using UnityEngine;

public static class GameSettings
{
    const string SensitivityKey = "Panik_MouseSensitivity";
    const string VolumeKey = "Panik_MasterVolume";

    public const float DefaultSensitivity = 1f;
    public const float MinSensitivity = 0.25f;
    public const float MaxSensitivity = 3f;
    public const float DefaultVolume = 1f;

    public static float MouseSensitivity { get; private set; } = DefaultSensitivity;
    public static float MasterVolume { get; private set; } = DefaultVolume;

    public static event Action SettingsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadSavedSettings()
    {
        MouseSensitivity = Mathf.Clamp(
            PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity),
            MinSensitivity,
            MaxSensitivity);

        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));

        ApplyMasterVolume();
    }

    public static void SetMouseSensitivity(float value, bool save = true)
    {
        MouseSensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);

        if (save)
            PlayerPrefs.SetFloat(SensitivityKey, MouseSensitivity);

        SettingsChanged?.Invoke();
    }

    public static void SetMasterVolume(float value, bool save = true)
    {
        MasterVolume = Mathf.Clamp01(value);

        if (save)
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);

        ApplyMasterVolume();
        SettingsChanged?.Invoke();
    }

    public static float SensitivityToSliderValue(float sensitivity) =>
        Mathf.InverseLerp(MinSensitivity, MaxSensitivity, sensitivity);

    public static float SliderValueToSensitivity(float sliderValue) =>
        Mathf.Lerp(MinSensitivity, MaxSensitivity, Mathf.Clamp01(sliderValue));

    public static void ApplyMasterVolume()
    {
        if (!AkUnitySoundEngine.IsInitialized())
            return;

        AkUnitySoundEngine.SetRTPCValue("MasterVolume", MasterVolume * 100f);
    }
}
