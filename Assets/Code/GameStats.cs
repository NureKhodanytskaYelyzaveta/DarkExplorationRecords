using UnityEngine;
using System.Collections;

public class GameStats : MonoBehaviour
{
    public static GameStats Instance;

    [Header("Stats")]
    public int fear = 0;
    public int trustSoleim = 50;

    [Header("Pulse Settings")]
    [Tooltip("Базовий пульс у спокої (BPM)")]
    public int baseBPM = 72;
    [Tooltip("Поточний BPM — читається PulseMonitor")]
    public int currentBPM = 72;

    [Tooltip("Скільки секунд до початку спаду після підйому")]
    public float decayCooldown = 3f;
    [Tooltip("Скільки BPM падає за секунду")]
    public float decayRate = 5f;
    [Tooltip("Плавність зміни BPM (сек)")]
    public float bpmSmoothTime = 0.6f;

    // pulse (0–100) — стара шкала, залишаємо для сумісності зі слайдером
    public int pulse
    {
        get => Mathf.RoundToInt(Mathf.InverseLerp(40f, 160f, currentBPM) * 100f);
        set => SetBPM(Mathf.RoundToInt(Mathf.Lerp(40f, 160f, value / 100f)));
    }

    private float targetBPM;
    private float smoothBPM;
    private float decayTimer = 0f;
    private bool decaying = false;
    private Coroutine decayCoroutine;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        targetBPM = baseBPM;
        smoothBPM = baseBPM;
        currentBPM = baseBPM;
    }

    /// <summary>Додає до пульсу і запускає таймер спаду.</summary>
    public void AddBPM(int amount)
    {
        targetBPM = Mathf.Clamp(targetBPM + amount, 40f, 160f);

        if (decayCoroutine != null) StopCoroutine(decayCoroutine);
        decayCoroutine = StartCoroutine(DecayRoutine());
    }

    public void SetBPM(int bpm)
    {
        targetBPM = Mathf.Clamp(bpm, 40f, 160f);

        if (decayCoroutine != null) StopCoroutine(decayCoroutine);
        decayCoroutine = StartCoroutine(DecayRoutine());
    }

    void Update()
    {
        // Визначаємо швидкість: якщо ми піднімаємось (target > smooth) — швидкість велика
        // Якщо падаємо (target < smooth) — швидкість дуже маленька і фіксована
        float step;
        if (targetBPM > smoothBPM)
        {
            // Швидкий підйом (використовуємо твій коефіцієнт)
            step = (Mathf.Abs(targetBPM - smoothBPM) / bpmSmoothTime + 1f) * Time.deltaTime;
        }
        else
        {
            // Плавний спад (ігноруємо SmoothTime, просто повільно пливемо за ціллю)
            step = decayRate * Time.deltaTime;
        }

        smoothBPM = Mathf.MoveTowards(smoothBPM, targetBPM, step);
        currentBPM = Mathf.RoundToInt(smoothBPM);

        var monitor = PulseMonitor.Instance;
        if (monitor != null) monitor.BPM = currentBPM;
    }

    private IEnumerator DecayRoutine()
    {
        yield return new WaitForSeconds(decayCooldown);

        // Корутина просто каже, КУДИ ми хочемо прийти (до базового пульсу)
        targetBPM = baseBPM;

        // Оскільки в Update тепер фіксована швидкість на спад, 
        // воно буде опускатися рівно зі швидкістю decayRate ударів/сек
        yield return null;
    }
}