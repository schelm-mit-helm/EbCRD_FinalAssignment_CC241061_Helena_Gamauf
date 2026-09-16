using System.Collections.Generic;
using UnityEngine;

public class MazeThemeManager : MonoBehaviour
{
    public static MazeThemeData CurrentTheme { get; private set; }
    public static int MazeLayoutSeed { get; private set; } = System.Environment.TickCount;

    [SerializeField] private MazeThemeData[] themes;
    [SerializeField] private MazeThemeData defaultTheme;
    [SerializeField] private bool chooseRandomThemeOnAwake = true;
    [SerializeField] private int selectedThemeIndex;

    private static MazeThemeManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (CurrentTheme != null)
            return;

        if (chooseRandomThemeOnAwake && HasValidThemes())
        {
            RandomizeGlobalTheme();
            return;
        }

        if (themes != null && themes.Length > 0)
        {
            SetThemeByIndex(selectedThemeIndex);
            return;
        }

        if (defaultTheme != null)
            CurrentTheme = defaultTheme;
    }

    public static MazeThemeManager EnsureInstance(
        MazeThemeData[] runtimeThemes = null,
        MazeThemeData runtimeDefault = null)
    {
        if (instance == null)
            instance = FindFirstObjectByType<MazeThemeManager>(FindObjectsInactive.Include);

        if (instance == null)
        {
            var managerObject = new GameObject(nameof(MazeThemeManager));
            DontDestroyOnLoad(managerObject);
            instance = managerObject.AddComponent<MazeThemeManager>();
        }

        instance.ApplyRuntimeThemes(runtimeThemes, runtimeDefault);
        return instance;
    }

    void ApplyRuntimeThemes(MazeThemeData[] runtimeThemes, MazeThemeData runtimeDefault)
    {
        if (runtimeThemes != null && runtimeThemes.Length > 0)
            themes = runtimeThemes;

        if (runtimeDefault != null)
            defaultTheme = runtimeDefault;
    }

    public static MazeThemeData GetCurrentOrDefault(MazeThemeData fallbackTheme)
    {
        if (CurrentTheme != null)
            return CurrentTheme;

        if (instance != null)
        {
            var validThemes = GetValidThemes(instance.themes);
            if (validThemes.Count > 0)
                return validThemes[0];

            if (instance.defaultTheme != null)
                return instance.defaultTheme;
        }

        return fallbackTheme;
    }

    public static void PrepareForNewRun()
    {
        CurrentTheme = null;
        PrepareForNewOrder();
    }

    public static void PrepareForNewOrder()
    {
        RefreshMazeLayoutSeed();
        EnsureInstance();
        CurrentTheme = null;
        RandomizeGlobalTheme();
    }

    static void RefreshMazeLayoutSeed()
    {
        MazeLayoutSeed = unchecked((int)(System.DateTime.UtcNow.Ticks ^ System.Environment.TickCount));
    }

    public static void BumpMazeLayoutSeed()
    {
        MazeLayoutSeed = unchecked(MazeLayoutSeed * 1664525 + 1013904223);
    }

    public static MazeThemeData RandomizeGlobalTheme()
    {
        EnsureInstance();

        if (instance == null)
            return CurrentTheme;

        var validThemes = GetValidThemes(instance.themes);
        if (validThemes.Count == 0)
        {
            if (instance.defaultTheme != null)
                CurrentTheme = instance.defaultTheme;

            return CurrentTheme;
        }

        int newIndex = Random.Range(0, validThemes.Count);
        CurrentTheme = validThemes[newIndex];

        for (int i = 0; i < validThemes.Count; i++)
        {
            if (validThemes[i] == CurrentTheme)
            {
                instance.selectedThemeIndex = i;
                break;
            }
        }

        return CurrentTheme;
    }

    public void SetThemeByIndex(int index)
    {
        var validThemes = GetValidThemes(themes);
        if (validThemes.Count == 0)
            return;

        selectedThemeIndex = Mathf.Clamp(index, 0, validThemes.Count - 1);
        CurrentTheme = validThemes[selectedThemeIndex];
    }

    bool HasValidThemes()
    {
        return GetValidThemes(themes).Count > 0;
    }

    static List<MazeThemeData> GetValidThemes(MazeThemeData[] sourceThemes)
    {
        var validThemes = new List<MazeThemeData>();
        if (sourceThemes == null)
            return validThemes;

        for (int i = 0; i < sourceThemes.Length; i++)
        {
            if (sourceThemes[i] != null)
                validThemes.Add(sourceThemes[i]);
        }

        return validThemes;
    }
}
