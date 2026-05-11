using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransition
{
    private static string nextScene = "";

    public static void LoadSceneThroughLoading(string targetScene)
    {
        nextScene = targetScene;
        SceneManager.LoadScene("LoadingScene");
    }

    public static string GetNextScene()
    {
        return nextScene;
    }
}