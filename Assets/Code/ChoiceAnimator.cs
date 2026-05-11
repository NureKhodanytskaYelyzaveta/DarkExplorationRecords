using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Button))]
public class ChoiceAnimator : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Entrance")]
    public float riseDistance = 60f;
    public float riseDuration = 0.45f;
    public float entranceDelay = 0f;

    [Header("Hover / Press")]
    public float hoverScale = 1.06f;
    public float pressScale = 0.96f;
    public float scaleDuration = 0.12f;

    [Header("Glow")]
    public Image buttonImage;
    public Color normalColor = new Color(1f, 1f, 1f, 0.08f);
    public Color hoverColor = new Color(1f, 1f, 1f, 0.22f);
    public Image glowImage;
    public float glowAlphaIdle = 0f;
    public float glowAlphaHover = 0.55f;

    private RectTransform rt;
    private CanvasGroup cg;
    private Coroutine scaleCoroutine;
    private Coroutine colorCoroutine;
    private Coroutine glowCoroutine;
    private bool isHovered = false;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // Ховаємо одразу, але НЕ зміщуємо — чекаємо поки layout розставить кнопки
        cg.alpha = 0f;
        StartCoroutine(EntranceRoutine());
    }

    private IEnumerator EntranceRoutine()
    {
        // Чекаємо 2 frames — layout group встигне розставити кнопки
        yield return null;
        yield return null;

        if (entranceDelay > 0f)
            yield return new WaitForSeconds(entranceDelay);

        // Тільки ТЕПЕР читаємо реальну позицію після layout
        Vector2 anchoredTarget = rt.anchoredPosition;
        rt.anchoredPosition = anchoredTarget + Vector2.down * riseDistance;

        float elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            float eased = EaseOutCubic(t);

            rt.anchoredPosition = Vector2.Lerp(startPos, anchoredTarget, eased);
            cg.alpha = eased;
            yield return null;
        }

        rt.anchoredPosition = anchoredTarget;
        cg.alpha = 1f;
    }

    public void OnPointerEnter(PointerEventData _)
    {
        isHovered = true;
        AnimateScale(hoverScale);
        AnimateColor(hoverColor);
        AnimateGlow(glowAlphaHover);
    }

    public void OnPointerExit(PointerEventData _)
    {
        isHovered = false;
        AnimateScale(1f);
        AnimateColor(normalColor);
        AnimateGlow(glowAlphaIdle);
    }

    public void OnPointerDown(PointerEventData _) => AnimateScale(pressScale);
    public void OnPointerUp(PointerEventData _) => AnimateScale(isHovered ? hoverScale : 1f);

    private void AnimateScale(float target)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(target, scaleDuration));
    }

    private void AnimateColor(Color target)
    {
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(ColorTo(target, scaleDuration));
    }

    private void AnimateGlow(float targetAlpha)
    {
        if (glowImage == null) return;
        if (glowCoroutine != null) StopCoroutine(glowCoroutine);
        glowCoroutine = StartCoroutine(GlowTo(targetAlpha, scaleDuration));
    }

    private IEnumerator ScaleTo(float target, float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.one * target;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale,
                EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        transform.localScale = endScale;
    }

    private IEnumerator ColorTo(Color target, float duration)
    {
        if (buttonImage == null) yield break;
        Color startColor = buttonImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            buttonImage.color = Color.Lerp(startColor, target,
                EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        buttonImage.color = target;
    }

    private IEnumerator GlowTo(float targetAlpha, float duration)
    {
        Color startColor = glowImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            glowImage.color = Color.Lerp(startColor, endColor,
                EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        glowImage.color = endColor;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    public void ResetState()
    {
        transform.localScale = Vector3.one;
        if (buttonImage) buttonImage.color = normalColor;
        if (glowImage)
        {
            Color c = glowImage.color;
            glowImage.color = new Color(c.r, c.g, c.b, 0f);
        }
    }
}