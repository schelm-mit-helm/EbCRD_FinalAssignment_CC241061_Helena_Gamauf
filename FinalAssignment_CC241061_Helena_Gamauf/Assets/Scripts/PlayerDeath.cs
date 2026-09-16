using UnityEngine.SceneManagement;

public static class PlayerDeath
{
    public static void KillPlayer()
    {
        SceneManager.LoadScene(SceneNames.GameOver);
    }
}
