using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip menuClip;
    [SerializeField] private float fadeDuration = 1.5f;

    private AudioSource audioSource;
    private static MusicManager instance;

    void Awake()
    {
        // Singleton
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (menuClip != null)
        {
            audioSource.clip = menuClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // У грі — вимикаємо menu music
        if (scene.name == "SampleGame")
        {
            if (audioSource.isPlaying)
                StartCoroutine(FadeOut());
        }
        // У всіх меню/лоадінгах — menu music має грати
        else
        {
            if (!audioSource.isPlaying && menuClip != null)
            {
                audioSource.clip = menuClip;
                audioSource.loop = true;
                audioSource.volume = 0f;
                audioSource.Play();

                StartCoroutine(FadeIn());
            }
        }
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        audioSource.volume = 1f;
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startVolume = audioSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = 1f; // скидаємо для наступного разу
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }
}