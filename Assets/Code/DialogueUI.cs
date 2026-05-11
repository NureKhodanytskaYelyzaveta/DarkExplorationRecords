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
    [SerializeField] private GameObject choicesPanel;

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

    // =========================================================
    // MUSIC & FADE
    // =========================================================

    private Coroutine fadeCoroutine;

    [YarnCommand("music")]
    public static void PlayMusic(string musicName)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        if (instance == null || !instance.enabled) return;
        instance.PlayMusicInternal(musicName);
    }

    private void PlayMusicInternal(string musicName)
    {
        // 🔥 ВАЖЛИВО: Зупиняємо будь-яке активне затухання, щоб воно не вимикало нову музику
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        AudioClip clip = null;
        switch (musicName.ToLower())
        {
            case "menu": clip = menu; break;
            case "street": clip = street; break;
            case "shop": clip = shop; break;
            case "hall": clip = hall; break;
            case "tense": clip = tense; break;
            case "start": clip = start; break;
        }

        if (clip == null)
        {
            Debug.LogWarning($"[DialogueUI] Music clip '{musicName}' not found! Check Inspector assignments.");
            return;
        }

        if (musicSource == null)
        {
            Debug.LogError("[DialogueUI] MusicSource is not assigned!");
            return;
        }

        // Якщо трек той самий, не перезапускаємо
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = 1f; //  Гарантовано вмикаємо гучність на максимум
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
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (musicSource != null) musicSource.Stop();
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
        if (!musicSource.isPlaying && musicSource.clip != null) musicSource.Play(); // Запускаємо, якщо раптом стоїть

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
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
        if (!musicSource.isPlaying && musicSource.clip != null) musicSource.Play();

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeVolume(1f, duration));
    }

    private IEnumerator FadeVolume(float targetVolume, float duration)
    {
        if (musicSource == null) yield break;

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Lerp може працювати некоректно, якщо startVolume і targetVolume однакові, тому додаємо перевірку
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;

        // Якщо затухнули до 0, можна зупинити джерело, щоб не вантажити CPU
        if (targetVolume == 0f)
        {
            musicSource.Stop();
        }
    }

    // =========================================================
    // SFX
    // =========================================================

    [YarnCommand("sfx")]
    public static void PlaySFX(string soundName)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        if (instance == null || !instance.enabled) return;
        instance.PlaySFXInternal(soundName);
    }

    private void PlaySFXInternal(string soundName)
    {
        Debug.Log($"[SFX] Attempting to play: {soundName}");

        if (sfxSource == null)
        {
            Debug.LogError("[SFX] sfxSource is NULL! Assign it in Inspector or add AudioSource component.");
            return;
        }

        AudioClip clip = null;
        switch (soundName.ToLower())
        {
            case "subway": clip = subway; break;
            case "subway_noise": clip = subway_noise; break;
            case "door_bell": clip = door_bell; break;
            case "street_birds": clip = street_birds; break;
            case "applause": clip = applause; break;
            case "convenience_store": clip = convenience_store; break;
        }

        if (clip == null)
        {
            Debug.LogError($"[SFX] Clip '{soundName}' is NULL! Check Inspector assignment.");
            return;
        }

        Debug.Log($"[SFX] Clip found: {clip.name}, Length: {clip.length}s");
        Debug.Log($"[SFX] sfxSource volume: {sfxSource.volume}, mute: {sfxSource.mute}");

        sfxSource.PlayOneShot(clip);
        Debug.Log($"[SFX] Playing {soundName}");
    }

    // =========================================================
    // INTERNAL
    // =========================================================

    private Coroutine typeCoroutine;
    private bool isTyping = false;
    private bool isWaitingForChoice = false;
    private string currentFullText = "";
    private string currentEmotion = "base";
    private string currentCharacter = "";
    private StringBuilder stringBuilder = new StringBuilder();
    private List<Button> choiceButtons = new List<Button>();
    private DialogueRunner dialogueRunner;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        dialogueRunner = Object.FindFirstObjectByType<DialogueRunner>();

        //  АВТО-ПОШУК, ЯКЩО НЕ ПРИЗНАЧЕНО ВРУЧНУ
        if (musicSource == null) musicSource = GetComponent<AudioSource>();

        if (sfxSource == null)
        {
            // Шукаємо інший AudioSource на цьому ж об'єкті
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 1 && sources[0] == musicSource)
                sfxSource = sources[1];
            else if (sources.Length > 1 && sources[1] == musicSource)
                sfxSource = sources[0];
            else
                sfxSource = gameObject.AddComponent<AudioSource>(); // Додаємо новий, якщо немає
        }
    }

    private void Start()
    {
        if (nextIcon) nextIcon.SetActive(false);
        if (choicesPanel) choicesPanel.SetActive(false);
        if (overlayCanvasGroup)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;
        }
        if (portraitImageHolder) portraitImageHolder.gameObject.SetActive(false);

        if (musicSource) musicSource.volume = 1f;
        if (sfxSource) sfxSource.volume = 1f;
    }

    // =========================================================
    // SHOW LINE & TYPEWRITER
    // =========================================================

    public void ShowLine(string character, string text)
    {
        if (nameText) nameText.text = character;
        currentCharacter = character?.Trim() ?? "";
        currentFullText = text;

        UpdatePortrait(character);
        ForcePortraitRender();

        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
        typeCoroutine = StartCoroutine(TypeTextWithDelay(text, portraitPreloadDelay));
    }

    private void UpdatePortrait(string characterName)
    {
        if (portraitImageHolder == null) return;
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
                        if (emotion.portraitSprite == null) continue;

                        portraitImageHolder.gameObject.SetActive(true);
                        portraitImageHolder.sprite = emotion.portraitSprite;
                        portraitImageHolder.SetNativeSize();

                        RectTransform rt = portraitImageHolder.rectTransform;
                        rt.anchorMin = new Vector2(1f, 0.5f);
                        rt.anchorMax = new Vector2(1f, 0.5f);
                        rt.pivot = new Vector2(1f, 0.5f);
                        rt.anchoredPosition = new Vector2(-20f, 0f);

                        AspectRatioFitter fitter = portraitImageHolder.GetComponent<AspectRatioFitter>();
                        if (fitter != null)
                            fitter.aspectRatio = (float)emotion.portraitSprite.rect.width / emotion.portraitSprite.rect.height;

                        return;
                    }
                }
                Debug.LogWarning($"[DialogueUI] Не знайдено емоцію '{currentEmotion}' для персонажа '{characterName}'");
                return;
            }
        }

        Debug.LogWarning($"[DialogueUI] Не знайдено персонажа '{characterName}'");
        if (autoHidePortraitIfUnknown) portraitImageHolder.gameObject.SetActive(false);
    }

    private void ForcePortraitRender()
    {
        if (portraitImageHolder != null && portraitImageHolder.gameObject.activeSelf)
        {
            Canvas.ForceUpdateCanvases();
            portraitImageHolder.SetVerticesDirty();
            portraitImageHolder.gameObject.SetActive(false);
            portraitImageHolder.gameObject.SetActive(true);
        }
    }

    private IEnumerator TypeTextWithDelay(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(TypeText(text));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        if (nextIcon) nextIcon.SetActive(false);
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
        if (nextIcon) nextIcon.SetActive(true);
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
    }

    // =========================================================
    // YARN COMMANDS (Emotion/Character/Portrait)
    // =========================================================

    [YarnCommand("emotion")]
    public static void SetEmotion(string emotion)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        if (instance == null || !instance.enabled) return;
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
        if (instance == null || !instance.enabled) return;
        instance.currentCharacter = characterName?.Trim() ?? "";
        instance.UpdatePortrait(instance.currentCharacter);
        instance.ForcePortraitRender();
    }

    [YarnCommand("hideportrait")]
    public static void HidePortrait()
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        if (instance?.portraitImageHolder != null)
        {
            var image = instance.portraitImageHolder.GetComponent<Image>();
            if (image != null)
                image.enabled = false;
        }
    }

    [YarnCommand("showportrait")]
    public static void ShowPortrait()
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        if (instance?.portraitImageHolder != null)
        {
            var image = instance.portraitImageHolder.GetComponent<Image>();
            if (image != null)
                image.enabled = true;
        }
    }

    // =========================================================
    // INPUT & CHOICES
    // =========================================================

    public void OnScreenTap()
    {
        if (isTyping) FinishTyping();
        else if (!isWaitingForChoice && dialogueBox.activeSelf) dialogueRunner?.RequestNextLine();
    }

    [YarnCommand("showchoices")]
    public static IEnumerator ShowChoices(string[] options)
    {
        var instance = Object.FindFirstObjectByType<DialogueUI>();
        if (instance == null) yield break;

        instance.isWaitingForChoice = true;
        if (instance.isTyping) instance.FinishTyping();
        if (instance.nameText) instance.nameText.gameObject.SetActive(false);
        if (instance.storyText) instance.storyText.gameObject.SetActive(false);
        if (instance.nextIcon) instance.nextIcon.SetActive(false);
        if (instance.overlayCanvasGroup)
        {
            instance.overlayCanvasGroup.alpha = 1f;
            instance.overlayCanvasGroup.blocksRaycasts = true;
        }
        if (instance.choicesPanel) instance.choicesPanel.SetActive(true);
        instance.ClearChoices();

        for (int i = 0; i < options.Length; i++)
        {
            if (!string.IsNullOrEmpty(options[i]))
                instance.CreateChoiceButton(options[i], i, i * instance.choiceStaggerDelay);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(instance.choicesContainer.GetComponent<RectTransform>());

        while (instance.isWaitingForChoice) yield return null;
    }

    private void CreateChoiceButton(string optionText, int index, float delay)
    {
        GameObject choiceGO = Instantiate(choiceButtonPrefab, choicesContainer);
        TextMeshProUGUI choiceText = choiceGO.GetComponentInChildren<TextMeshProUGUI>();
        if (choiceText) choiceText.text = optionText;

        ChoiceAnimator animator = choiceGO.GetComponent<ChoiceAnimator>();
        if (animator != null)
        {
            animator.entranceDelay = delay;
            animator.ResetState();
        }

        Button choiceButton = choiceGO.GetComponent<Button>();
        choiceButton.onClick.AddListener(() => OnChoiceSelected(index));
        choiceButtons.Add(choiceButton);
    }

    private void OnChoiceSelected(int index)
    {
        dialogueRunner?.VariableStorage?.SetValue("$selectedChoice", index);
        HideChoices();
        if (nameText) nameText.gameObject.SetActive(true);
        if (storyText) storyText.gameObject.SetActive(true);
        isWaitingForChoice = false;
    }

    private void ClearChoices()
    {
        foreach (Button button in choiceButtons)
            if (button != null) Destroy(button.gameObject);
        choiceButtons.Clear();
    }

    public void HideChoices()
    {
        if (choicesPanel) choicesPanel.SetActive(false);
        if (overlayCanvasGroup)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;
        }
        ClearChoices();
    }

    // =========================================================
    // STATS
    // =========================================================

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

    private void OnDestroy() => ClearChoices();
}