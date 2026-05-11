using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CardCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Картки")]
    public RectTransform[] cards;
    public float cardSpacing = 640f;
    public float firstCardX = 300f; // відступ першої картки від лівого краю

    [Header("Вигляд")]
    public float scaleActive = 1f;
    public float scaleInactive = 0.85f;
    public float offsetY = 40f;
    public float alphaInactive = 0.6f;

    [Header("Свайп")]
    public float snapSpeed = 8f;
    public float swipeThreshold = 50f;

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public RectTransform[] dots;
    public float dotSizeNormal = 20f;
    public float dotSizeActive = 30f;

    [Header("Дані")]
    public string[] titles;
    public string[] descriptions;

    private int currentIndex = 0;
    private float dragStartX;
    private float offsetX = 0f;
    private float targetOffsetX = 0f;
    private bool isDragging = false;

    void Start()
    {
        offsetX = 0f;
        targetOffsetX = 0f;
        PositionCards(true);
        UpdateUI();
    }

    void Update()
    {
        if (!isDragging)
        {
            offsetX = Mathf.Lerp(offsetX, targetOffsetX, Time.deltaTime * snapSpeed);
            PositionCards(false);
        }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        isDragging = true;
        dragStartX = e.position.x;
    }

    public void OnDrag(PointerEventData e)
    {
        float delta = (e.position.x - dragStartX) / GetComponentInParent<Canvas>().scaleFactor;
        offsetX = targetOffsetX + delta;
        PositionCards(false);
    }

    public void OnEndDrag(PointerEventData e)
    {
        isDragging = false;
        float delta = (e.position.x - dragStartX) / GetComponentInParent<Canvas>().scaleFactor;

        if (Mathf.Abs(delta) > swipeThreshold)
        {
            if (delta < 0 && currentIndex < cards.Length - 1)
                currentIndex++;
            else if (delta > 0 && currentIndex > 0)
                currentIndex--;
        }

        targetOffsetX = -currentIndex * cardSpacing;
        UpdateUI();
    }

    void PositionCards(bool instant)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            float cardX = firstCardX + i * cardSpacing + offsetX;
            float distFromCenter = Mathf.Abs(cardX - firstCardX);
            float normalizedDist = distFromCenter / cardSpacing;

            float targetScale = Mathf.Lerp(scaleActive, scaleInactive,
                Mathf.Clamp01(normalizedDist));
            float targetY = i == currentIndex ? 0f : -offsetY;
            float targetAlpha = i == currentIndex ? 1f : alphaInactive;

            float t = instant ? 1f : Time.deltaTime * snapSpeed * 2f;

            cards[i].anchoredPosition = new Vector2(
                cardX,
                Mathf.Lerp(cards[i].anchoredPosition.y, targetY, t)
            );

            cards[i].localScale = Vector3.Lerp(
                cards[i].localScale,
                Vector3.one * targetScale,
                t
            );

            Image img = cards[i].GetComponent<Image>();
            if (img)
            {
                Color c = img.color;
                c.a = Mathf.Lerp(c.a, targetAlpha, t);
                img.color = c;
            }

            // порядок шарів — активна поверх
            cards[i].SetSiblingIndex(i == currentIndex ? cards.Length : i);
        }
    }

    void UpdateUI()
    {
        if (titleText && titles.Length > currentIndex)
            titleText.text = titles[currentIndex];
        if (descText && descriptions.Length > currentIndex)
            descText.text = descriptions[currentIndex];

        for (int i = 0; i < dots.Length; i++)
        {
            float size = i == currentIndex ? dotSizeActive : dotSizeNormal;
            dots[i].sizeDelta = new Vector2(size, size);
            Image img = dots[i].GetComponent<Image>();
            if (img) img.color = i == currentIndex ?
                Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
        }
    }
}