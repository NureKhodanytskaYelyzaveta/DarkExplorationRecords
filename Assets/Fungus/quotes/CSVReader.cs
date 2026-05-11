using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using TMPro;  // або UnityEngine.UI якщо використовуєш звичайний Text

public class CSVReader : MonoBehaviour
{
    public TextAsset csvFile;
    public float autoDelay = 2f;

    [Header("UI елементи — перетягни з Hierarchy")]
    public TextMeshProUGUI storyText;   // StoryText об'єкт
    public TextMeshProUGUI nameText;    // NameText об'єкт
    public GameObject dialogueCanvas;   // DialogueCanvas об'єкт

    private string[] lines;
    private int index = 0;
    private Coroutine autoCoroutine;

    void Start()
    {
        // Перевірки
        if (csvFile == null) { Debug.LogError("[CSVReader] CSV не призначено!"); return; }
        if (storyText == null) { Debug.LogError("[CSVReader] StoryText не призначено!"); return; }
        if (nameText == null) { Debug.LogError("[CSVReader] NameText не призначено!"); return; }

        lines = csvFile.text.Split('\n');
        Debug.Log($"[CSVReader] CSV завантажено, рядків: {lines.Length}");

        if (dialogueCanvas != null) dialogueCanvas.SetActive(true);

        index = 1;
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return null;
        Debug.Log("[CSVReader] DelayedStart викликано");
        ShowNext();
        autoCoroutine = StartCoroutine(AutoPlay());
    }

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                SkipOrNext();
        }
#if UNITY_EDITOR
        else if (Input.GetMouseButtonDown(0))
        {
            SkipOrNext();
        }
#endif
    }

    void SkipOrNext()
    {
        if (autoCoroutine != null) StopCoroutine(autoCoroutine);
        ShowNext();
        autoCoroutine = StartCoroutine(AutoPlay());
    }

    IEnumerator AutoPlay()
    {
        while (index < lines.Length)
        {
            yield return new WaitForSeconds(autoDelay);
            ShowNext();
        }
        Debug.Log("[CSVReader] Діалог завершено");
    }

    void ShowNext()
    {
        if (index >= lines.Length)
        {
            Debug.Log("[CSVReader] Кінець файлу");
            return;
        }

        string line = lines[index].Trim();
        Debug.Log($"[CSVReader] Рядок {index}: '{line}'");

        if (string.IsNullOrEmpty(line))
        {
            index++;
            ShowNext(); // пропускаємо порожні
            return;
        }

        string[] cols = line.Split(';');

        if (cols.Length >= 3)
        {
            string character = cols[0].Trim().Trim('"');
            string text = cols[2].Trim().Trim('"');

            Debug.Log($"[CSVReader] Персонаж='{character}' Текст='{text}'");

            nameText.text = character;
            storyText.text = text;
        }
        else
        {
            Debug.LogWarning($"[CSVReader] Рядок {index} має лише {cols.Length} колонки: '{line}'");
        }

        index++;
    }
}