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
    public float offX = 25f;   // Позиція X, коли вимкнено
    public float onX = -25f;  // Позиція X, коли увімкнено
    public float speed = 10f;  // Швидкість анімації

    private float targetX;
    private bool initialized = false;

    void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();
    }

    void OnEnable()
    {
        // OnEnable викликається щоразу, коли об'єкт стає активним,
        // тому підписка/відписка тут надійніша, ніж лише в Start().
        if (toggle != null)
            toggle.onValueChanged.AddListener(OnToggleChanged);

        // Ініціалізуємо позицію без анімації при першому вмиканні.
        if (!initialized && toggle != null)
        {
            ForceState(toggle.isOn);
            initialized = true;
        }
        else if (toggle != null)
        {
            // Панель відкрили повторно — синхронізуємо без анімації.
            ForceState(toggle.isOn);
        }
    }

    void OnDisable()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    // Встановлює стан миттєво, без плавного руху.
    private void ForceState(bool isOn)
    {
        targetX = isOn ? onX : offX;

        if (backgroundImage != null)
            backgroundImage.sprite = isOn ? onSprite : offSprite;

        if (handle != null)
            handle.anchoredPosition = new Vector2(targetX, handle.anchoredPosition.y);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (backgroundImage != null)
            backgroundImage.sprite = isOn ? onSprite : offSprite;

        targetX = isOn ? onX : offX;
    }

    void Update()
    {
        if (handle == null) return;

        float currentX = handle.anchoredPosition.x;

        if (Mathf.Abs(currentX - targetX) > 0.1f)
        {
            float newX = Mathf.Lerp(currentX, targetX, Time.deltaTime * speed);
            handle.anchoredPosition = new Vector2(newX, handle.anchoredPosition.y);
        }
    }
}