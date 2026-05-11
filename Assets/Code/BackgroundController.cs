using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using System.Collections;

public class BackgroundController : MonoBehaviour
{
    public Image backgroundImage;
    public Image fadeOverlay;

    public CanvasGroup dialogueGroup; // 👈 замість GameObject

    public float fadeTime = 1f;
    public float uiFadeTime = 0.4f;

    public float preDelay = 0.2f;
    public float holdTime = 0.4f;

    public Sprite street;
    public Sprite hall_one;
    public Sprite hall_two;
    public Sprite market_in;
    public Sprite market_out;
    public Sprite train_one;
    public Sprite train_two;

    void Start()
    {
        var runner = FindObjectOfType<DialogueRunner>();
        runner.AddCommandHandler<string>("bg", ChangeBackgroundWithFade);
    }

    public void ChangeBackgroundWithFade(string bgName)
    {
        StartCoroutine(FadeRoutine(bgName));
    }

    IEnumerator FadeRoutine(string bgName)
    {
        // 👻 плавно ховаємо UI
        yield return StartCoroutine(FadeUI(1f, 0f));
        dialogueGroup.interactable = false;

        // ⏳ пауза
        yield return new WaitForSeconds(preDelay);

        // 🔴 затемнення
        yield return StartCoroutine(Fade(0f, 1f));

        // ⏳ тримаємо чорний
        yield return new WaitForSeconds(holdTime);

        // 🔁 зміна фону
        backgroundImage.sprite = GetSprite(bgName);

        // 🟢 повертаємо сцену
        yield return StartCoroutine(Fade(1f, 0f));

        // ⏳ маленька пауза
        yield return new WaitForSeconds(0.15f);

        // 👁 плавно повертаємо UI
        dialogueGroup.interactable = true;
        yield return StartCoroutine(FadeUI(0f, 1f));
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / fadeTime);

            Color c = fadeOverlay.color;
            c.a = a;
            fadeOverlay.color = c;

            yield return null;
        }
    }

    IEnumerator FadeUI(float from, float to)
    {
        float t = 0f;

        while (t < uiFadeTime)
        {
            t += Time.deltaTime;
            dialogueGroup.alpha = Mathf.Lerp(from, to, t / uiFadeTime);
            yield return null;
        }

        dialogueGroup.alpha = to;
    }

    Sprite GetSprite(string name)
    {
        switch (name)
        {
            case "street": return street;
            case "hall_one": return hall_one;
            case "hall_two": return hall_two;
            case "market_in": return market_in;
            case "market_out": return market_out;
            case "train_one": return train_one;
            case "train_two": return train_two;
        }
        return null;
    }
}