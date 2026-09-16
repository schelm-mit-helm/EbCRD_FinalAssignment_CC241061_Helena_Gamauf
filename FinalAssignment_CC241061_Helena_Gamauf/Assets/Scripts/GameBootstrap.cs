using UnityEngine;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        EnsureCoreServices();
    }

    public static void EnsureCoreServices()
    {
        CustomerOrderService.EnsurePersistentInstance();
        CustomerOrderUI.EnsureInstance();
        ComicSpeechBoxUI.EnsureInstance();
        ScoreService.EnsurePersistentInstance();
        BakeryCashierDialogue.EnsureInstance();
        IpadScoreDisplay.EnsureInstance();
        MazeThemeManager.EnsureInstance();
    }

    public static void ResetRunState()
    {
        ScoreService.Instance?.ResetScore();
        CustomerOrderService.Instance?.ResetToDefaults();
        BakeryCashierDialogue.Instance?.ResetForNewRun();
        CustomerOrderUI.Instance?.ClearAndHide();
        MazeThemeManager.PrepareForNewRun();
    }
}
