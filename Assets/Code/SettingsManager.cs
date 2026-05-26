using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Висить на SettingsPanel (прихований при старті).
/// - Awake/Start тут НЕ використовуються — вони не викликаються на неактивному об'єкті.
/// - OnEnable викликається щоразу, коли панель відкривається (SetActive true).
/// - Значення до DialogueUI застосовуються через статичний InitFromOutside(),
///   який викликає SettingsBtnHandler при старті сцени.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingsPanel;

    [Header("UI")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider textSpeedSlider;
    public Toggle autoplayToggle;

    [Header("Dialogue UI")]
    public DialogueUI dialogueUI;

    // Статичне посилання — щоб SettingsBtnHandler міг знайти нас
    // навіть поки панель прихована.
    public static SettingsManager Instance { get; private set; }

    void Awake()
    {
        // Awake НЕ викликається, якщо об'єкт неактивний при старті.
        // Але якщо раптом об'єкт активний — зберігаємо Instance.
        Instance = this;
    }

    void OnEnable()
    {
        // Зберігаємо Instance щоразу при відкритті (на випадок якщо Awake не спрацював).
        Instance = this;

        RemoveListeners();

        // Встановлюємо збережені значення БЕЗ виклику listeners.
        if (musicSlider) musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 1f));
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", 1f));
        if (textSpeedSlider) textSpeedSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("TextSpeed", 0.03f));
        if (autoplayToggle) autoplayToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("AutoPlay", 0) == 1);

        AddListeners();
    }

    void OnDisable()
    {
        RemoveListeners();
    }

    // -------------------------------------------------------
    // Викликається ззовні (з SettingsBtnHandler.Start)
    // щоб застосувати збережені налаштування до DialogueUI
    // ще до першого відкриття панелі.
    // -------------------------------------------------------
    public void ApplyStoredValuesToGame()
    {
        if (dialogueUI == null) return;

        dialogueUI.SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
        dialogueUI.SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
        dialogueUI.SetTextSpeed(PlayerPrefs.GetFloat("TextSpeed", 0.03f));
        dialogueUI.SetAutoPlay(PlayerPrefs.GetInt("AutoPlay", 0) == 1);
    }

    // -------------------------------------------------------
    // Listeners
    // -------------------------------------------------------

    private void AddListeners()
    {
        if (musicSlider) musicSlider.onValueChanged.AddListener(SetMusic);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFX);
        if (textSpeedSlider) textSpeedSlider.onValueChanged.AddListener(SetTextSpeed);
        if (autoplayToggle) autoplayToggle.onValueChanged.AddListener(SetAutoPlay);
    }

    private void RemoveListeners()
    {
        if (musicSlider) musicSlider.onValueChanged.RemoveListener(SetMusic);
        if (sfxSlider) sfxSlider.onValueChanged.RemoveListener(SetSFX);
        if (textSpeedSlider) textSpeedSlider.onValueChanged.RemoveListener(SetTextSpeed);
        if (autoplayToggle) autoplayToggle.onValueChanged.RemoveListener(SetAutoPlay);
    }

    // -------------------------------------------------------
    // Панель
    // -------------------------------------------------------

    public void OpenSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    // -------------------------------------------------------
    // Setters
    // -------------------------------------------------------

    public void SetMusic(float value)
    {
        if (dialogueUI) dialogueUI.SetMusicVolume(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFX(float value)
    {
        if (dialogueUI) dialogueUI.SetSFXVolume(value);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    public void SetTextSpeed(float value)
    {
        if (dialogueUI) dialogueUI.SetTextSpeed(value);
        PlayerPrefs.SetFloat("TextSpeed", value);
    }

    public void SetAutoPlay(bool value)
    {
        if (dialogueUI) dialogueUI.SetAutoPlay(value);
        PlayerPrefs.SetInt("AutoPlay", value ? 1 : 0);
    }
}