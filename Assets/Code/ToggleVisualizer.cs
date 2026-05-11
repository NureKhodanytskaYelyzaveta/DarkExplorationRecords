using UnityEngine;
using UnityEngine.UI;

public class ToggleAdvancedSwitcher : MonoBehaviour
{
    [Header("Компоненти")]
    public Toggle toggle;
    public Image backgroundImage;
    public RectTransform handle;

    [Header("Зображення фону")]
    public Sprite onSprite;
    public Sprite offSprite;

    [Header("Налаштування руху кульки")]
    public float offX = 25f; // Позиція X, коли вимкнено
    public float onX = -25f;   // Позиція X, коли увімкнено
    public float speed = 10f; // Швидкість анімації

    private float targetX;

    void Start()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();

        // Підписуємося на зміну стану
        toggle.onValueChanged.AddListener(OnToggleChanged);

        // Встановлюємо початковий стан без анімації
        UpdateState(toggle.isOn);
        targetX = toggle.isOn ? onX : offX;
        handle.anchoredPosition = new Vector2(targetX, handle.anchoredPosition.y);
    }

    void OnToggleChanged(bool isOn)
    {
        UpdateState(isOn);
    }

    void UpdateState(bool isOn)
    {
        // 1. Міняємо картинку фону
        backgroundImage.sprite = isOn ? onSprite : offSprite;

        // 2. Встановлюємо ціль для руху кульки
        targetX = isOn ? onX : offX;
    }

    void Update()
    {
        // Плавно рухаємо кульку до цілі кожен кадр
        float currentX = handle.anchoredPosition.x;
        if (Mathf.Abs(currentX - targetX) > 0.1f)
        {
            float newX = Mathf.Lerp(currentX, targetX, Time.deltaTime * speed);
            handle.anchoredPosition = new Vector2(newX, handle.anchoredPosition.y);
        }
    }
}