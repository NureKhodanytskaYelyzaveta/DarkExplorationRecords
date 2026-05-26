using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void OnPlayButtonClick()
    {
        var saveManager = SaveManager.Instance
            ?? Object.FindFirstObjectByType<SaveManager>();

        if (saveManager != null && saveManager.HasSave)
        {
            PlayerPrefs.SetInt("ShouldLoadSave", 1);
        }
        else
        {
            PlayerPrefs.SetInt("ShouldLoadSave", 0);
        }

        PlayerPrefs.Save();
        LoadingScreen.LoadScene("SampleGame");
    }
}