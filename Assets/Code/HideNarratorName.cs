using UnityEngine;
using Yarn.Unity;

public class HideNarratorName : MonoBehaviour
{
    public GameObject nameBox;
    private LinePresenter linePresenter;

    void Awake()
    {
        linePresenter = GetComponent<LinePresenter>();
    }

    void Update()
    {
        if (nameBox == null) return;
        // перевіряємо текст в NameText напряму
        var nameText = nameBox.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (nameText == null) return;

        string name = nameText.text.Trim();
        nameBox.SetActive(!string.IsNullOrEmpty(name) && name != "_");
    }
}