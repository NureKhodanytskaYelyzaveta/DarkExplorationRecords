using UnityEngine;

public class SettingsBtnHandler : MonoBehaviour
{
    [SerializeField] private SettingsManager settingsManager;

    void Start()
    {
        if (settingsManager == null)
            settingsManager = FindAnyObjectByType<SettingsManager>(FindObjectsInactive.Include);

        if (settingsManager != null)
            settingsManager.ApplyStoredValuesToGame();
        else
            Debug.LogError("[SettingsBtnHandler] SettingsManager не знайдено!");
    }

    public void OnSettingsBtnClick()
    {
        if (settingsManager != null)
            settingsManager.OpenSettings();
    }
}