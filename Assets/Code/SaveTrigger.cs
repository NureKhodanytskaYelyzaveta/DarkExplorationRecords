using UnityEngine;
using Yarn.Unity;

public class SaveTrigger : MonoBehaviour
{
    [Header("Посилання")]
    [SerializeField] private DialogueRunner dialogueRunner;

    public static string CurrentBackground = "";
    public static string CurrentMusic = "";
    public static string LastSavedNode = "Start"; // ← з великої, єдиний варіант

    // =========================================================
    // YARN КОМАНДА
    // =========================================================

    [YarnCommand("save")]
    public static void SaveFromYarn(string nodeName, string bg = "", string music = "")
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[SaveTrigger] SaveManager не знайдено!");
            return;
        }

        LastSavedNode = nodeName;

        string bgToSave = string.IsNullOrEmpty(bg) ? CurrentBackground : bg;
        string musicToSave = string.IsNullOrEmpty(music) ? CurrentMusic : music;

        SaveManager.Instance.Save(nodeName, bgToSave, musicToSave);
        Debug.Log($"[SaveTrigger] Збережено через Yarn: вузол '{nodeName}'");
    }

    // =========================================================
    // КНОПКА "ПОВЕРНУТИСЬ ДО МЕНЮ"
    // =========================================================

    public void SaveAndGoToMenu()
    {
        if (SaveManager.Instance != null)
        {
            string node = string.IsNullOrEmpty(LastSavedNode) ? "Start" : LastSavedNode;
            SaveManager.Instance.Save(node, CurrentBackground, CurrentMusic); // ← це було відсутнє
            Debug.Log($"[SaveTrigger] Збережено перед виходом: вузол '{node}'");
        }

        LoadingScreen.LoadScene("MainMenuScene");
    }

    // =========================================================
    // ВІДСТЕЖЕННЯ СТАНУ
    // =========================================================

    public static void SetCurrentBackground(string bgName)
    {
        CurrentBackground = bgName;
    }

    public static void SetCurrentMusic(string musicName)
    {
        CurrentMusic = musicName;
    }
}