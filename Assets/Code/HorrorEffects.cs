using UnityEngine;
using Yarn.Unity;

public class HorrorEffects : MonoBehaviour
{
    public static HorrorEffects Instance { get; private set; }

    // =========================================================
    // INSPECTOR
    // =========================================================

    [Header("Scene References")]
    [UnityEngine.SerializeField] private UnityEngine.RectTransform shakeTarget;

    [Header("BPM Thresholds")]
    [UnityEngine.SerializeField] private int mildThreshold = 120;
    [UnityEngine.SerializeField] private int peakThreshold = 140;

    [Header("Screen Dimming")]
    [UnityEngine.SerializeField] private float dimMaxAlpha = 0.30f;
    [UnityEngine.SerializeField] private float dimFadeSpeed = 2.0f;

    [Header("Vignette — Mild")]
    [UnityEngine.SerializeField] private float vignetteMildAlpha = 0.45f;
    [UnityEngine.SerializeField] private float mildPulseSpeed = 0.65f;
    [UnityEngine.SerializeField] private float mildPulseDepth = 0.055f;

    [Header("Vignette — Peak")]
    [UnityEngine.SerializeField] private float vignettePeakAlpha = 0.82f;
    [UnityEngine.SerializeField] private float peakPulseSpeed = 3.0f;
    [UnityEngine.SerializeField] private float peakPulseDepth = 0.22f;
    [UnityEngine.SerializeField] private float heartbeatAsymmetry = 0.55f;
    [UnityEngine.SerializeField] private float vignetteFadeSpeed = 1.8f;

    [Header("Vignette Shape")]
    [UnityEngine.SerializeField] private int vignetteTexSize = 512;
    [UnityEngine.SerializeField] private float vignettePower = 1.5f;
    [UnityEngine.SerializeField] private float vignetteAspect = 1.45f;
    [UnityEngine.SerializeField] private float vignetteInnerRadius = 0.45f;

    [Header("TV Static Noise — тільки на peak")]
    [Tooltip("Макс alpha шуму на піку")]
    [UnityEngine.SerializeField] private float noiseMaxAlpha = 0.20f;
    [UnityEngine.SerializeField] private float noiseFadeSpeed = 8f;
    [Tooltip("Роздільність статики. 64–128 = грубий TV, 256 = дрібніший")]
    [UnityEngine.SerializeField] private int noiseTexSize = 80;
    [Tooltip("Яскравість білих пікселів (0 = суто чорно-сірий, 1 = є яскраві спалахи)")]
    [UnityEngine.Range(0f, 1f)]
    [UnityEngine.SerializeField] private float staticBrightness = 0.55f;
    [Tooltip("Пульсація шуму разом із серцебиттям")]
    [UnityEngine.SerializeField] private bool staticPulsesWithHeartbeat = true;

    [Header("Screen Shake — тільки на peak")]
    [UnityEngine.SerializeField] private float peakShakeIntensity = 7f;
    [UnityEngine.SerializeField] private float shakeFrequency = 16f;

    [Header("Audio (optional)")]
    [UnityEngine.SerializeField] private AudioSource audioSource;
    [UnityEngine.SerializeField] private AudioClip heartbeatClip;
    [UnityEngine.SerializeField] private float heartbeatMinInterval = 0.45f;

    // =========================================================
    // PRIVATE STATE
    // =========================================================

    private bool horrorEnabled = true;
    private float currentDimAlpha = 0f;
    private float currentVigAlpha = 0f;
    private float currentNoiseAlpha = 0f;
    private Vector2 shakeOrigin;
    private float shakeTimer = 0f;
    private float heartbeatTimer = 0f;
    private float vignettePhase = 0f;

    private float heartbeatPulse = 0f;

    private Color dimColor = new Color(0f, 0f, 0f, 0f);
    private Color vignetteColor = new Color(0f, 0f, 0f, 0f);
    private Color noiseColor = new Color(1f, 1f, 1f, 0f);

    private Texture2D vignetteTex;
    private Texture2D noiseTex;
    private Texture2D whiteTex;

    private PulseMonitor pulseMonitor;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Instance = this;
        pulseMonitor = Object.FindFirstObjectByType<PulseMonitor>();

        if (shakeTarget != null)
            shakeOrigin = shakeTarget.anchoredPosition;

        BuildWhiteTex();
        BuildVignetteTex();
        BuildNoiseTex();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        int bpm = pulseMonitor != null ? pulseMonitor.BPM : 0;

        if (!horrorEnabled || bpm < mildThreshold)
        {
            FadeAlpha(ref currentDimAlpha, 0f, dimFadeSpeed);
            FadeAlpha(ref currentVigAlpha, 0f, vignetteFadeSpeed);
            FadeAlpha(ref currentNoiseAlpha, 0f, noiseFadeSpeed);
            dimColor = new Color(0f, 0f, 0f, currentDimAlpha);
            vignetteColor = new Color(0f, 0f, 0f, currentVigAlpha);
            noiseColor = new Color(1f, 1f, 1f, currentNoiseAlpha);
            heartbeatPulse = 0f;
            ResetShake();
            return;
        }

        float t = Mathf.Clamp01((float)(bpm - mildThreshold) / (peakThreshold - mildThreshold));

        UpdateDim(t);
        UpdateVignette(t);
        UpdateNoise(t);
        UpdateShake(t);
        UpdateHeartbeat(bpm, t);

        if (currentNoiseAlpha > 0.002f)
            RefreshNoiseTex();
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        Rect fs = new Rect(0, 0, Screen.width, Screen.height);

        if (currentDimAlpha > 0.002f)
        {
            GUI.color = dimColor;
            GUI.DrawTexture(fs, whiteTex, ScaleMode.StretchToFill);
        }

        if (vignetteTex != null && currentVigAlpha > 0.005f)
        {
            GUI.color = vignetteColor;
            GUI.DrawTexture(fs, vignetteTex, ScaleMode.StretchToFill);
        }

        if (noiseTex != null && currentNoiseAlpha > 0.002f)
        {
            GUI.color = noiseColor;
            GUI.DrawTexture(fs, noiseTex, ScaleMode.StretchToFill);
        }

        GUI.color = Color.white;
    }

    private void OnDestroy()
    {
        if (shakeTarget != null)
            shakeTarget.anchoredPosition = shakeOrigin;

        if (vignetteTex != null) Destroy(vignetteTex);
        if (noiseTex != null) Destroy(noiseTex);
        if (whiteTex != null) Destroy(whiteTex);
    }

    // =========================================================
    // BUILD TEXTURES
    // =========================================================

    private void BuildWhiteTex()
    {
        whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();
    }

    private void BuildVignetteTex()
    {
        int size = vignetteTexSize;
        vignetteTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        vignetteTex.wrapMode = TextureWrapMode.Clamp;
        vignetteTex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - half) / half / vignetteAspect;
                float ny = (y - half) / half;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float remapped = Mathf.Clamp01((dist - vignetteInnerRadius) / (1f - vignetteInnerRadius));
                float a = Mathf.Pow(remapped, vignettePower);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        vignetteTex.SetPixels(pixels);
        vignetteTex.Apply();
    }

    private void BuildNoiseTex()
    {
        noiseTex = new Texture2D(noiseTexSize, noiseTexSize, TextureFormat.RGBA32, false);
        noiseTex.wrapMode = TextureWrapMode.Repeat;
        noiseTex.filterMode = FilterMode.Point;
        RefreshNoiseTex();
    }

    private void RefreshNoiseTex()
    {
        int size = noiseTexSize;
        Color[] px = new Color[size * size];

        float pulseBoost = staticPulsesWithHeartbeat
            ? heartbeatPulse * 0.35f
            : 0f;

        float maxBright = Mathf.Clamp01(staticBrightness + pulseBoost);

        for (int i = 0; i < px.Length; i++)
        {
            float v = Random.value * maxBright;
            px[i] = new Color(v, v, v, 1f);
        }

        noiseTex.SetPixels(px);
        noiseTex.Apply();
    }

    // =========================================================
    // UPDATES
    // =========================================================

    private void UpdateDim(float t)
    {
        float targetDim = Mathf.Lerp(0f, dimMaxAlpha, t);
        FadeAlpha(ref currentDimAlpha, targetDim, dimFadeSpeed);
        dimColor = new Color(0f, 0f, 0f, currentDimAlpha);
    }

    private void UpdateVignette(float t)
    {
        float baseAlpha = Mathf.Lerp(vignetteMildAlpha, vignettePeakAlpha, t);
        float pulseSpeed = Mathf.Lerp(mildPulseSpeed, peakPulseSpeed, t);
        float pulseDepth = Mathf.Lerp(mildPulseDepth, peakPulseDepth, t);

        vignettePhase += Time.deltaTime * pulseSpeed;

        float pulse;
        if (t < 0.5f)
        {
            pulse = Mathf.Sin(vignettePhase * Mathf.PI * 2f) * pulseDepth;
        }
        else
        {
            float raw = Mathf.Sin(vignettePhase * Mathf.PI * 2f);
            float shaped = raw > 0f
                ? Mathf.Pow(raw, 1f - heartbeatAsymmetry * 0.7f)
                : Mathf.Pow(-raw, 1f + heartbeatAsymmetry) * -1f;
            pulse = shaped * pulseDepth;

            heartbeatPulse = Mathf.Clamp01(shaped);
        }

        float targetAlpha = Mathf.Clamp01(baseAlpha + pulse);
        FadeAlpha(ref currentVigAlpha, targetAlpha, vignetteFadeSpeed);

        float r = Mathf.Lerp(0f, 0.08f, t);
        float b = Mathf.Lerp(0.03f, 0f, t);
        vignetteColor = new Color(r, 0f, b, currentVigAlpha);
    }

    private void UpdateNoise(float t)
    {
        float noiseT = Mathf.Clamp01((t - 0.5f) / 0.5f);
        float intensity = Mathf.SmoothStep(0f, 1f, noiseT);

        float baseAlpha = Mathf.Lerp(0.04f, noiseMaxAlpha, intensity);
        float pulseAlpha = staticPulsesWithHeartbeat
            ? baseAlpha + heartbeatPulse * noiseMaxAlpha * 0.6f
            : baseAlpha;

        float targetAlpha = Mathf.Clamp01(pulseAlpha);
        FadeAlpha(ref currentNoiseAlpha, targetAlpha, noiseFadeSpeed);

        noiseColor = new Color(1f, 1f, 1f, currentNoiseAlpha);
    }

    // =========================================================
    // SCREEN SHAKE
    // =========================================================

    private void UpdateShake(float t)
    {
        if (shakeTarget == null) return;

        float shakeT = Mathf.Clamp01((t - 0.8f) / 0.2f);
        float intensity = Mathf.Lerp(0f, peakShakeIntensity, shakeT);

        if (intensity < 0.01f) { ResetShake(); return; }

        shakeTimer += Time.deltaTime * shakeFrequency;
        float jolt = 1f + heartbeatPulse * 0.5f;
        float offsetX = Mathf.Sin(shakeTimer * 1.3f) * intensity * jolt;
        float offsetY = Mathf.Cos(shakeTimer * 1.7f) * intensity * jolt * 0.55f;
        shakeTarget.anchoredPosition = shakeOrigin + new Vector2(offsetX, offsetY);
    }

    private void ResetShake()
    {
        if (shakeTarget == null) return;
        shakeTarget.anchoredPosition = Vector2.Lerp(
            shakeTarget.anchoredPosition, shakeOrigin, Time.deltaTime * 10f);
    }

    // =========================================================
    // HEARTBEAT SFX
    // =========================================================

    private void UpdateHeartbeat(int bpm, float t)
    {
        if (audioSource == null || heartbeatClip == null) return;
        if (t <= 0f) { heartbeatTimer = 0f; return; }

        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0f)
        {
            float interval = Mathf.Lerp(60f / mildThreshold, heartbeatMinInterval, t);
            heartbeatTimer = interval;
            audioSource.PlayOneShot(heartbeatClip, t);
        }
    }

    // =========================================================
    // YARN COMMANDS
    // =========================================================

    [YarnCommand("horror_effect")]
    public static void SetHorrorEffect(string state)
    {
        if (Instance == null) { Debug.LogWarning("[HorrorEffects] No instance found."); return; }
        switch (state.Trim().ToLower())
        {
            case "on": Instance.horrorEnabled = true; Debug.Log("[HorrorEffects] ENABLED."); break;
            case "off": Instance.horrorEnabled = false; Debug.Log("[HorrorEffects] DISABLED."); break;
            default: Debug.LogWarning($"[HorrorEffects] Unknown state '{state}'. Use 'on'/'off'."); break;
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private void FadeAlpha(ref float current, float target, float speed)
        => current = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
}