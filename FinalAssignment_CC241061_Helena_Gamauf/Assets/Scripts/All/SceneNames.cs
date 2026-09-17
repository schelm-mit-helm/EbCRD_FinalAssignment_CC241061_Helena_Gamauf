public static class SceneNames
{
    public const string StartMenu = "Start_Menu_Scene";
    public const string Bakery = "Bakery_Scene";
    public const string Test = "Test_Scene";
    public const string Pantry = "Pantry_Scene";
    public const string GameOver = "Game_Over_Scene";

    /// <summary>
    /// Scene loaded when leaving the bakery through the back door / elevator.
    /// Change this one constant to switch between Test_Scene and Pantry_Scene.
    /// </summary>
    public const string GatheringDestination = Pantry;

    public static bool IsGatheringScene(string sceneName) =>
        sceneName == Test || sceneName == Pantry;
}
