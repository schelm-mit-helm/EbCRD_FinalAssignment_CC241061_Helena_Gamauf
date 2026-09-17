using UnityEngine;
using UnityEngine.SceneManagement;

public static class MazeThemeEnvironmentEffects
{
    struct SavedFogState
    {
        public bool fogEnabled;
        public Color fogColor;
        public FogMode fogMode;
        public float fogDensity;
        public float linearFogStart;
        public float linearFogEnd;
    }

    static SavedFogState savedFogState;
    static bool hasSavedFogState;
    static PlayerMovement slipperyPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SubscribeToSceneLoads()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        HandleScene(SceneManager.GetActiveScene().name);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HandleScene(scene.name);

    static void HandleScene(string sceneName)
    {
        if (SceneNames.IsGatheringScene(sceneName))
            ApplyForCurrentTheme();
        else
            ClearEffects();
    }

    public static void ApplyForCurrentTheme()
    {
        ClearEffects();

        var theme = MazeThemeManager.CurrentTheme;
        if (theme == null)
            return;

        switch (theme.environmentEffect)
        {
            case MazeThemeEnvironmentEffect.SlipperyFloor:
                ApplySlipperyFloor();
                break;
            case MazeThemeEnvironmentEffect.Fog:
                ApplyFog();
                break;
        }
    }

    public static void ClearEffects()
    {
        ClearSlipperyFloor();
        ClearFog();
    }

    static void ApplySlipperyFloor()
    {
        var playerTransform = SceneTransformFinder.FindPlayer();
        if (playerTransform == null)
            return;

        slipperyPlayer = playerTransform.GetComponent<PlayerMovement>();
        if (slipperyPlayer == null)
            slipperyPlayer = playerTransform.GetComponentInChildren<PlayerMovement>();

        slipperyPlayer?.SetSlipperyFloor(true);
    }

    static void ClearSlipperyFloor()
    {
        if (slipperyPlayer != null)
            slipperyPlayer.SetSlipperyFloor(false);

        slipperyPlayer = null;

        var playerTransform = SceneTransformFinder.FindPlayer();
        if (playerTransform == null)
            return;

        playerTransform.GetComponent<PlayerMovement>()?.SetSlipperyFloor(false);
        playerTransform.GetComponentInChildren<PlayerMovement>()?.SetSlipperyFloor(false);
    }

    static void ApplyFog()
    {
        if (!hasSavedFogState)
        {
            savedFogState = new SavedFogState
            {
                fogEnabled = RenderSettings.fog,
                fogColor = RenderSettings.fogColor,
                fogMode = RenderSettings.fogMode,
                fogDensity = RenderSettings.fogDensity,
                linearFogStart = RenderSettings.fogStartDistance,
                linearFogEnd = RenderSettings.fogEndDistance,
            };
            hasSavedFogState = true;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.78f, 0.82f, 0.86f, 1f);
        RenderSettings.fogDensity = 0.15f;
    }

    static void ClearFog()
    {
        if (!hasSavedFogState)
            return;

        RenderSettings.fog = savedFogState.fogEnabled;
        RenderSettings.fogColor = savedFogState.fogColor;
        RenderSettings.fogMode = savedFogState.fogMode;
        RenderSettings.fogDensity = savedFogState.fogDensity;
        RenderSettings.fogStartDistance = savedFogState.linearFogStart;
        RenderSettings.fogEndDistance = savedFogState.linearFogEnd;
        hasSavedFogState = false;
    }
}
