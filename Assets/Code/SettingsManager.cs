using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public GameObject settingsPanel;

    public void OpenSettings()
    {
        Debug.Log("OPEN");
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void SetMusic(float value)
    {
        Debug.Log("Music: " + value);
    }

    public void SetSFX(float value)
    {
        Debug.Log("SFX: " + value);
    }

    public void SetTextSpeed(float value)
    {
        Debug.Log("Text Speed: " + value);
    }

    public void SetAutoPlay(bool value)
    {
        Debug.Log("AutoPlay: " + value);
    }
}