using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI")]
    public Slider loadingBar;
    public TextMeshProUGUI percentText;

    [Header("Налаштування")]
    public float minLoadTime = 3f;

    private static string nextSceneName = "MainMenuScene";

    public static void LoadScene(string sceneName)
    {
        nextSceneName = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }

    void Start()
    {
        StartCoroutine(LoadSceneCoroutine());
    }

    IEnumerator LoadSceneCoroutine()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        float displayProgress = 0f;

        while (!op.isDone)
        {
            elapsed += Time.deltaTime;
            float realProgress = Mathf.Clamp01(op.progress / 0.9f);
            float timeProgress = elapsed / minLoadTime;
            float target = Mathf.Min(realProgress, timeProgress);

            displayProgress = Mathf.MoveTowards(
                displayProgress, target, Time.deltaTime * 0.5f);

            if (loadingBar != null)
                loadingBar.value = displayProgress;

            if (percentText != null)
                percentText.text = $"{Mathf.RoundToInt(displayProgress * 100)}%";

            if (displayProgress >= 0.999f)
            {
                yield return new WaitForSeconds(0.3f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}