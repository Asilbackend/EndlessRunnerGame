using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void Load(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        // Use loading screen if available, otherwise fall back to direct load
        if (UI.LoadingScreen.Instance != null)
            UI.LoadingScreen.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
