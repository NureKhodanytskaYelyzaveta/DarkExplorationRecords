using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Yarn.Unity;

public class DialogueUI : MonoBehaviour
{
    // =========================================================
    // ПОРТРЕТИ
    // =========================================================

    [System.Serializable]
    public struct EmotionPortrait
    {
        public string emotionName;
        public Sprite portraitSprite;
    }

    [System.Serializable]
    public struct CharacterPortrait
    {
        public string yarnCharacterName;
        public List<EmotionPortrait> emotions;
    }

    [Header("Портрети")]
    [SerializeField] private Image portraitImageHolder;
    [SerializeField] private List<CharacterPortrait> characterPortraits;
    [SerializeField] private bool autoHidePortraitIfUnknown = true;

    // =========================================================
    // UI
    // =========================================================

    [Header("UI Elements")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private GameObject nextIcon;
    [SerializeField] private GameObject nameBox;
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private Image fadeOverlay;


    [Header("Choice Elements")]
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private CanvasGroup overlayCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float typeSpeed = 0.03f;
    [SerializeField] private float choiceStaggerDelay = 0.08f;
    [SerializeField] private float portraitPreloadDelay = 0.05f;

    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menu;
    [SerializeField] private AudioClip street;
    [SerializeField] private AudioClip shop;
    [SerializeField] private AudioClip hall;
    [SerializeField] private AudioClip tense;
    [SerializeField] private AudioClip start;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip subway;
    [SerializeField] private AudioClip subway_noise;
    [SerializeField] private AudioClip door_bell;
    [SerializeField] private AudioClip street_birds;
    [SerializeField] private AudioClip applause;
    [SerializeField] private AudioClip convenience_store;
    [SerializeField] private AudioClip heart;
    [SerializeField] private AudioClip spin;
    [SerializeField] private AudioClip click;
    [SerializeField] private AudioClip scream;
    [SerializeField] private AudioClip splash;

    // =========================================================
    // INTERNAL
    // =========================================================

    private Coroutine fadeCoroutine;
    private Coroutine typeCoroutine;
    private Coroutine autoPlayCoroutine;

    private bool isTyping = false;
    private bool isWaitingForChoice = false;

    private string currentFullText = "";
    private string currentEmotion = "base";
    private string currentCharacter = "";

    private StringBuilder stringBuilder = new StringBuilder();
    private List<Button> choiceButtons = new List<Button>();

    private DialogueRunner dialogueRunner;
    private LinePresenter linePresenter;

    private const float MinLettersPerSec = 10f;
    private const float MaxLettersPerSec = 200f;

    [Header("AutoPlay Settings")]
    [SerializeField] private float autoPlayDelay = 2f;
    public bool isAutoPlayEnabled = false;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        dialogueRunner = Object.FindFirstObjectByType<DialogueRunner>();
        linePresenter = Object.FindFirstObjectByType<LinePresenter>();
        dialogueRunner.AddCommandHandler("gotomenu", GoToMainMenu);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (sfxSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();

            if (sources.Length > 1 && sources[0] == musicSource)
                sfxSource = sources[1];
            else if (sources.Length > 1 && sources[1] == musicSource)
                sfxSource = sources[0];
            else
                sfxSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (dialogueBox)
            dialogueBox.GetComponent<CanvasGroup>().alpha = 0f;

        if (nextIcon)
            nextIcon.SetActive(true);

        if (choicesPanel)
            choicesPanel.SetActive(false);

        if (overlayCanvasGroup)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;
        }

        if (portraitImageHolder)
            portraitImageHolder.gameObject.SetActive(false);

        if (musicSource)
            musicSource.volume = 1f;

        if (sfxSource)
            sfxSource.volume = 1f;

        StartCoroutine(InitDialogue());
    }

    private IEnumerator InitDialogue()
    {
        // Чекаємо один кадр щоб всі компоненти ініціалізувались
        yield return null;

        bool shouldLoad = PlayerPrefs.GetInt("ShouldLoadSave", 0) == 1;
        PlayerPrefs.SetInt("ShouldLoadSave", 0);
        PlayerPrefs.Save();

        var saveManager = SaveManager.Instance
            ?? Object.FindFirstObjectByType<SaveManager>();

        if (shouldLoad && saveManager != null && saveManager.HasSave)
        {
            SaveData data = saveManager.Load();

            if (data != null && !string.IsNullOrEmpty(data.yarnNodeName))
            {
                if (!string.IsNullOrEmpty(data.currentMusic))
                    PlayMusicInternal(data.currentMusic);

                dialogueRunner?.StartDialogue(data.yarnNodeName);
                yield break;
            }
        }

        // Нова гра
        dialogueRunner?.StartDialogue("Chapter1");
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = volume;
    }

    public void SetTextSpeed(float sliderValue)
    {
        if (linePresenter != null)
            linePresenter.lettersPerSecond = Mathf.RoundToInt(Mathf.Lerp(MinLettersPerSec, MaxLettersPerSec, sliderValue));
    }

    public void SetAutoPlay(bool state)
    {
        isAutoPlayEnabled = state;

        if (linePresenter != null)
        {
            linePresenter.autoAdvance = state;
            linePresenter.autoAdvanceDelay = autoPlayDelay;
        }

        if (!state && autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
    }

    // =========================================================
    // MUSIC
    // =========================================================

    [YarnCommand("music")]
    public static void PlayMusic(string musicName)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        if (instance == null || !instance.enabled) return;

        instance.PlayMusicInternal(musicName);
    }

    private void PlayMusicInternal(string musicName)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        AudioClip clip = null;

        switch (musicName.ToLower())
        {
            case "menu":   clip = menu;   break;
            case "street": clip = street; break;
            case "shop":   clip = shop;   break;
            case "hall":   clip = hall;   break;
            case "tense":  clip = tense;  break;
            case "start":  clip = start;  break;
        }

        if (clip == null)
        {
            Debug.LogWarning($"[DialogueUI] Music clip '{musicName}' not found!");
            return;
        }

        if (musicSource == null)
        {
            Debug.LogError("[DialogueUI] MusicSource is NULL!");
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();

        Debug.Log($"[DialogueUI] Playing music: {musicName}");
    }

    [YarnCommand("stopmusic")]
    public static void StopMusic()
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        instance?.StopMusicInternal();
    }

    private void StopMusicInternal()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (musicSource != null)
            musicSource.Stop();
    }

    [YarnCommand("fadeoutmusic")]
    public static void FadeOutMusic(float duration)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        instance?.FadeOutMusicInternal(duration);
    }

    private void FadeOutMusicInternal(float duration)
    {
        if (musicSource == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeVolume(0f, duration));
    }

    [YarnCommand("fadeinmusic")]
    public static void FadeInMusic(float duration)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        instance?.FadeInMusicInternal(duration);
    }

    private void FadeInMusicInternal(float duration)
    {
        if (musicSource == null) return;

        if (!musicSource.isPlaying && musicSource.clip != null)
            musicSource.Play();

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeVolume(1f, duration));
    }

    private IEnumerator FadeVolume(float targetVolume, float duration)
    {
        if (musicSource == null)
            yield break;

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;

        if (targetVolume == 0f)
            musicSource.Stop();
    }

    // =========================================================
    // SFX
    // =========================================================

    [YarnCommand("sfx")]
    public static void PlaySFX(string soundName)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();

        if (instance == null || !instance.enabled)
            return;

        instance.PlaySFXInternal(soundName);
    }

    private void PlaySFXInternal(string soundName)
    {
        if (sfxSource == null)
        {
            Debug.LogError("[SFX] sfxSource is NULL!");
            return;
        }

        if (soundName.ToLower() == "null")
            return;

        AudioClip clip = null;

        switch (soundName.ToLower())
        {
            case "subway":             clip = subway;             break;
            case "subway_noise":       clip = subway_noise;       break;
            case "door_bell":          clip = door_bell;          break;
            case "street_birds":       clip = street_birds;       break;
            case "applause":           clip = applause;           break;
            case "convenience_store":  clip = convenience_store;  break;
            case "heart":              clip = heart; break;
            case "spin":               clip = spin; break;
            case "click":              clip = click; break;
            case "scream":             clip = scream; break;
            case "splash":             clip = splash; break;
        }

        if (clip == null)
        {
            Debug.LogError($"[SFX] Clip '{soundName}' is NULL!");
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    // =========================================================
    // DIALOGUE
    // =========================================================

    public void ShowLine(string character, string text)
    {
        if (dialogueBox)
            dialogueBox.GetComponent<CanvasGroup>().alpha = 1f;

        if (nameText)
            nameText.text = character;

        currentCharacter = character?.Trim() ?? "";
        currentFullText = text;

        UpdatePortrait(character);
        ForcePortraitRender();

        if (typeCoroutine != null)
            StopCoroutine(typeCoroutine);

        typeCoroutine = StartCoroutine(TypeTextWithDelay(text, portraitPreloadDelay));
    }

    private IEnumerator TypeTextWithDelay(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(TypeText(text));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;

        if (nextIcon)
            nextIcon.SetActive(false);

        storyText.text = "";
        stringBuilder.Clear();

        foreach (char c in text)
        {
            stringBuilder.Append(c);
            storyText.text = stringBuilder.ToString();
            yield return new WaitForSeconds(typeSpeed);
        }

        FinishTyping();
    }

    private void FinishTyping()
    {
        isTyping = false;
        storyText.text = currentFullText;

        if (nextIcon)
            nextIcon.SetActive(true);

        if (typeCoroutine != null)
            StopCoroutine(typeCoroutine);

        if (isAutoPlayEnabled && !isWaitingForChoice)
            StartAutoPlay();
    }

    public void OnScreenTap()
    {
        if (isAutoPlayEnabled)
            StopAutoPlay();

        if (isTyping)
            FinishTyping();
        else if (!isWaitingForChoice && dialogueBox.activeSelf)
            dialogueRunner?.RequestNextLine();
    }

    // =========================================================
    // PORTRAITS
    // =========================================================

    [YarnCommand("emotion")]
    public static void SetEmotion(string emotion)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();

        if (instance == null || !instance.enabled)
            return;

        instance.currentEmotion = emotion?.Trim() ?? "base";

        if (!string.IsNullOrEmpty(instance.currentCharacter))
        {
            instance.UpdatePortrait(instance.currentCharacter);
            instance.ForcePortraitRender();
        }
    }

    [YarnCommand("character")]
    public static void SetCharacter(string characterName)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();

        if (instance == null || !instance.enabled)
            return;

        instance.currentCharacter = characterName?.Trim() ?? "";
        instance.UpdatePortrait(instance.currentCharacter);
        instance.ForcePortraitRender();
    }

    [YarnCommand("hideportrait")]
    public static void HidePortrait()
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();

        if (instance == null || !instance.enabled)
            return;

        instance.currentCharacter = "";
        instance.currentEmotion = "base";

        if (instance.portraitImageHolder != null)
            instance.portraitImageHolder.gameObject.SetActive(false);
    }

    private void UpdatePortrait(string characterName)
    {
        if (portraitImageHolder == null)
            return;

        if (string.IsNullOrWhiteSpace(characterName) || characterName == "_")
        {
            portraitImageHolder.gameObject.SetActive(false);
            return;
        }

        string searchCharacter = characterName.Trim().ToLower();
        string searchEmotion = currentEmotion.Trim().ToLower();

        foreach (var character in characterPortraits)
        {
            if (character.yarnCharacterName.Trim().ToLower() == searchCharacter)
            {
                foreach (var emotion in character.emotions)
                {
                    if (emotion.emotionName.Trim().ToLower() == searchEmotion)
                    {
                        if (emotion.portraitSprite == null)
                            continue;

                        portraitImageHolder.gameObject.SetActive(true);
                        portraitImageHolder.sprite = emotion.portraitSprite;
                        portraitImageHolder.SetNativeSize();
                        return;
                    }
                }
            }
        }

        Debug.LogWarning($"[DialogueUI] Portrait not found for '{characterName}'");

        if (autoHidePortraitIfUnknown)
            portraitImageHolder.gameObject.SetActive(false);
    }

    private void ForcePortraitRender()
    {
        if (portraitImageHolder != null && portraitImageHolder.gameObject.activeSelf)
        {
            Canvas.ForceUpdateCanvases();
            portraitImageHolder.SetVerticesDirty();
        }
    }

    // =========================================================
    // CHOICES
    // =========================================================

    [YarnCommand("showchoices")]
    public static IEnumerator ShowChoices(string[] options)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();

        if (instance == null)
            yield break;

        instance.isWaitingForChoice = true;

        if (instance.isTyping)
            instance.FinishTyping();

        if (instance.nameText)
            instance.nameText.gameObject.SetActive(false);

        if (instance.storyText)
            instance.storyText.gameObject.SetActive(false);

        if (instance.nextIcon)
            instance.nextIcon.SetActive(false);

        if (instance.overlayCanvasGroup)
        {
            instance.overlayCanvasGroup.alpha = 1f;
            instance.overlayCanvasGroup.blocksRaycasts = true;
        }

        if (instance.choicesPanel)
            instance.choicesPanel.SetActive(true);

        instance.ClearChoices();

        for (int i = 0; i < options.Length; i++)
        {
            if (!string.IsNullOrEmpty(options[i]))
                instance.CreateChoiceButton(options[i], i, i * instance.choiceStaggerDelay);
        }

        while (instance.isWaitingForChoice)
            yield return null;
    }

    private void CreateChoiceButton(string optionText, int index, float delay)
    {
        GameObject choiceGO = Instantiate(choiceButtonPrefab, choicesContainer);

        TextMeshProUGUI choiceText = choiceGO.GetComponentInChildren<TextMeshProUGUI>();

        if (choiceText)
            choiceText.text = optionText;

        Button choiceButton = choiceGO.GetComponent<Button>();
        choiceButton.onClick.AddListener(() => OnChoiceSelected(index));

        choiceButtons.Add(choiceButton);
    }

    private void OnChoiceSelected(int index)
    {
        dialogueRunner?.VariableStorage?.SetValue("$selectedChoice", index);

        HideChoices();

        if (nameText)
            nameText.gameObject.SetActive(true);

        if (storyText)
            storyText.gameObject.SetActive(true);

        isWaitingForChoice = false;
    }

    private void ClearChoices()
    {
        foreach (Button button in choiceButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        choiceButtons.Clear();
    }

    public void HideChoices()
    {
        if (choicesPanel)
            choicesPanel.SetActive(false);

        if (overlayCanvasGroup)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;
        }

        ClearChoices();
    }

    // =========================================================
    // AUTOPLAY
    // =========================================================

    private void StartAutoPlay()
    {
        if (autoPlayCoroutine != null)
            StopCoroutine(autoPlayCoroutine);

        autoPlayCoroutine = StartCoroutine(AutoPlayRoutine());
    }

    private IEnumerator AutoPlayRoutine()
    {
        // Чекаємо autoPlayDelay секунд після завершення друку,
        // потім просто симулюємо тап — найпростіше і надійне рішення.
        yield return new WaitForSeconds(autoPlayDelay);

        if (isAutoPlayEnabled && !isWaitingForChoice)
            OnScreenTap();
    }

    private void StopAutoPlay()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        ClearChoices();
    }

    [YarnCommand("addpulse")]
    public static void AddPulse(int amount)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        instance?.AddPulseInternal(amount);
    }

    private void AddPulseInternal(int amount)
    {
        var stats = GameStats.Instance ?? Object.FindFirstObjectByType<GameStats>();
        if (stats != null) stats.AddBPM(amount);
        else Debug.LogWarning("GameStats.Instance не знайдено!");
    }

    [YarnCommand("addtrust")]
    public static void AddTrust(int amount)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        instance?.AddTrustInternal(amount);
    }

    private void AddTrustInternal(int amount)
    {
        var stats = GameStats.Instance ?? Object.FindFirstObjectByType<GameStats>();
        if (stats != null) stats.trustSoleim = Mathf.Clamp(stats.trustSoleim + amount, 0, 100);
        else Debug.LogWarning("GameStats.Instance не знайдено!");
    }

    private void GoToMainMenu()
    {
        StartCoroutine(GoToMainMenuRoutine());
    }

    private IEnumerator GoToMainMenuRoutine()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float t = 0f;
            float fadeDuration = 4f;
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, t / fadeDuration); // ← 1f замість 5f
                fadeOverlay.color = c;
                yield return null;
            }

            c.a = 1f;
            fadeOverlay.color = c;
        }

        yield return new WaitForSeconds(0.3f);
        LoadingScreen.LoadScene("MainMenuScene");
    }

    [YarnCommand("hidedialog")]
    public static void HideDialog()
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        if (instance == null) return;

        var img = instance.dialogueBox?.GetComponent<Image>();
        if (img != null) img.enabled = false;

        if (instance.storyText)
            instance.storyText.gameObject.SetActive(false);
        if (instance.nameText)
            instance.nameText.gameObject.SetActive(false);
        if (instance.nameBox)
            instance.nameBox.gameObject.SetActive(false);
        if (instance.nextIcon)
            instance.nextIcon.SetActive(false);
        if (instance.portraitImageHolder)
            instance.portraitImageHolder.gameObject.SetActive(false);
    }

    [YarnCommand("showdialog")]
    public static void ShowDialog()
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        if (instance == null) return;

        var img = instance.dialogueBox?.GetComponent<Image>();
        if (img != null) img.enabled = true;

        if (instance.storyText)
            instance.storyText.gameObject.SetActive(true);
        if (instance.nameText)
            instance.nameText.gameObject.SetActive(true);
        if (instance.nameBox)
            instance.nameBox.gameObject.SetActive(true);
        if (instance.nextIcon)
            instance.nextIcon.SetActive(true);
    }
}