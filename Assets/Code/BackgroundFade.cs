using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SimpleBackgroundFade : MonoBehaviour
{
    [Header("UI")]
    public Image background;     // Основний фон
    public Image fadeOverlay;    // Чорний екран поверх

    [Header("Settings")]
    public float fadeTime = 1.5f;

    private bool isFading = false;

    public void ChangeBackground(Sprite newBackground)
    {
        if (!isFading)
            StartCoroutine(FadeRoutine(newBackground));
    }

    private IEnumerator FadeRoutine(Sprite newBackground)
    {
        isFading = true;

        float t = 0;

        // Затемнення
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.SmoothStep(0, 1, t / fadeTime);

            fadeOverlay.color = new Color(0, 0, 0, a);
            yield return null;
        }

        // Міняємо фон у темряві
        background.sprite = newBackground;

        t = 0;

        // Повернення світла
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.SmoothStep(1, 0, t / fadeTime);

            fadeOverlay.color = new Color(0, 0, 0, a);
            yield return null;
        }

        fadeOverlay.color = new Color(0, 0, 0, 0);
        isFading = false;
    }
}
