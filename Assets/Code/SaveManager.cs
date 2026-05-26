using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SaveFileName = "save.json";

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public bool HasSave
    {
        get
        {
            if (!File.Exists(SavePath)) return false;
            SaveData data = LoadRaw();
            return data != null && data.hasSave;
        }
    }

    public void Save(string yarnNodeName, string currentBackground = "", string currentMusic = "")
    {
        var stats = GameStats.Instance;

        SaveData data = new SaveData
        {
            yarnNodeName = yarnNodeName,
            fear = stats != null ? stats.fear : 0,
            trustSoleim = stats != null ? stats.trustSoleim : 50,
            currentBPM = stats != null ? stats.currentBPM : 72,
            currentBackground = currentBackground,
            currentMusic = currentMusic,
            savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            hasSave = true
        };

        string json = JsonUtility.ToJson(data, prettyPrint: true);

        File.WriteAllText(SavePath, json);

        Debug.Log($"[SaveManager] Гру збережено: вузол '{yarnNodeName}', файл: {SavePath}");
    }

    public SaveData Load()
    {
        SaveData data = LoadRaw();

        if (data == null || !data.hasSave)
        {
            Debug.LogWarning("[SaveManager] Збереження не знайдено або пошкоджено.");
            return null;
        }

        var stats = GameStats.Instance;
        if (stats != null)
        {
            stats.fear = data.fear;
            stats.trustSoleim = data.trustSoleim;
            stats.SetBPM(data.currentBPM);
        }

        Debug.Log($"[SaveManager] Завантажено збереження від {data.savedAt}, вузол: '{data.yarnNodeName}'");

        return data;
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveManager] Збереження видалено.");
        }
    }

    // =========================================================
    // PRIVATE
    // =========================================================

    private SaveData LoadRaw()
    {
        if (!File.Exists(SavePath))
            return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Помилка читання збереження: {e.Message}");
            return null;
        }
    }
}
