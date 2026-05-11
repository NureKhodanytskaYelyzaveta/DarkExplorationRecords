using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class PulseMonitor : MaskableGraphic
{
    public static PulseMonitor Instance;

    [Header("UI")]
    public TextMeshProUGUI bpmText;
    public TextMeshProUGUI pulseLabel;
    public TextMeshProUGUI statusText;

    [Header("Налаштування")]
    public float lineWidth = 0.8f;
    public int pointCount = 300;

    [Header("Відступи")]
    public float leftPadding = 10f;
    public float rightPadding = 10f;
    public float topPadding = 35f;
    public float bottomPadding = 10f;

    private int _bpm = 72;
    private float offset = 0f;
    private float lastOffset = -999f;

    private List<UIVertex> verts = new List<UIVertex>();

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        // Переконуємось що використовується правильний material
        if (material == null || material == defaultMaterial)
        {
            material = defaultMaterial;
        }

        // Вимикаємо raycast
        raycastTarget = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // Форсуємо оновлення при увімкненні
        SetAllDirty();
    }

    public override Material material
    {
        get
        {
            if (m_CustomMaterial != null)
                return m_CustomMaterial;
            return defaultMaterial;
        }
        set
        {
            if (m_CustomMaterial == value)
                return;
            m_CustomMaterial = value;
            SetMaterialDirty();
        }
    }

    private Material m_CustomMaterial;

    public int BPM
    {
        get => _bpm;
        set
        {
            _bpm = Mathf.Clamp(value, 40, 160);
            SetVerticesDirty();
        }
    }

    Color GetLineColor()
    {
        float t = Mathf.Clamp01((_bpm - 60f) / 100f);

        if (t < 0.5f)
        {
            float f = t / 0.5f;
            return new Color(f, 1f - f * 0.13f, 0.5f * (1f - f));
        }
        else
        {
            float f = (t - 0.5f) / 0.5f;
            return new Color(1f - f * 0.29f, (1f - f) * 0.87f, 0f);
        }
    }

    float GetECGSample(float t)
    {
        float amp = (rectTransform.rect.height - topPadding - bottomPadding) * 0.35f;

        if (t < 0.35f) return 0;
        if (t < 0.40f) return -(t - 0.35f) / 0.05f * amp * 0.22f;
        if (t < 0.44f) return -amp * 0.22f + (t - 0.40f) / 0.04f * amp * 0.95f;
        if (t < 0.47f) return amp * 0.73f - (t - 0.44f) / 0.03f * amp * 1.65f;
        if (t < 0.50f) return -amp * 0.92f + (t - 0.47f) / 0.03f * amp * 1.15f;
        if (t < 0.55f) return amp * 0.23f - (t - 0.50f) / 0.05f * amp * 0.23f;
        if (t < 0.65f) return -(t - 0.55f) / 0.10f * amp * 0.28f;
        if (t < 0.75f) return -amp * 0.28f + (t - 0.65f) / 0.10f * amp * 0.28f;

        return 0;
    }

    void AddSegment(Vector2 p0, Vector2 p1, Color col, float width = -1f)
    {
        float w = width < 0 ? lineWidth : width;

        Vector2 dir = p1 - p0;
        if (dir.magnitude < 0.001f) return;

        dir.Normalize();
        Vector2 perp = new Vector2(-dir.y, dir.x) * w * 0.5f;

        verts.Add(MakeVert(p0 - perp, col));
        verts.Add(MakeVert(p0 + perp, col));
        verts.Add(MakeVert(p1 + perp, col));
        verts.Add(MakeVert(p1 - perp, col));
    }

    UIVertex MakeVert(Vector2 pos, Color col)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = pos;
        v.color = col;
        return v;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        verts.Clear();

        float w = rectTransform.rect.width;
        float h = rectTransform.rect.height;

        // Робоча область з урахуванням відступів
        float workWidth = w - leftPadding - rightPadding;
        float workHeight = h - topPadding - bottomPadding;
        float workLeft = -w / 2f + leftPadding;
        float workRight = w / 2f - rightPadding;
        float workTop = h / 2f - topPadding;
        float workBottom = -h / 2f + bottomPadding;
        float centerY = (workTop + workBottom) / 2f;

        Color lineCol = GetLineColor();

        // Сітка (в межах робочої області)
        Color gridCol = new Color(lineCol.r, lineCol.g, lineCol.b, 0.07f);
        float gridStep = workHeight / 4f;

        for (float gy = workBottom; gy <= workTop; gy += gridStep)
            AddSegment(new Vector2(workLeft, gy), new Vector2(workRight, gy), gridCol, 0.5f);

        for (float gx = workLeft; gx <= workRight; gx += gridStep)
            AddSegment(new Vector2(gx, workBottom), new Vector2(gx, workTop), gridCol, 0.5f);

        // ECG крива
        float period = workWidth * (60f / _bpm) * 0.15f;
        float firstPhase = (offset % period + period) % period;

        Vector2 prev = new Vector2(workLeft, centerY + GetECGSample(firstPhase / period));

        for (int i = 1; i <= pointCount; i++)
        {
            float x = workLeft + (float)i / pointCount * workWidth;

            float fade = 0.12f + (float)i / pointCount * 0.88f;

            float phase = ((x - workLeft + offset) % period + period) % period;
            float t = phase / period;

            float y = centerY + GetECGSample(t);

            Color c = new Color(
                lineCol.r * fade,
                lineCol.g * fade,
                lineCol.b * fade,
                1f
            );

            Vector2 curr = new Vector2(x, y);

            AddSegment(prev, curr, c);
            prev = curr;
        }

        for (int i = 0; i < verts.Count - 3; i += 4)
        {
            vh.AddUIVertexQuad(new UIVertex[]
            {
                verts[i],
                verts[i + 1],
                verts[i + 2],
                verts[i + 3]
            });
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        offset += _bpm / 1800f * Time.deltaTime * 60f;

        if (Mathf.Abs(offset - lastOffset) > 0.15f)
        {
            lastOffset = offset;
            SetVerticesDirty();
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        Color col = GetLineColor();

        if (bpmText)
        {
            bpmText.text = $"{_bpm} <size=55%>BPM</size>";
            bpmText.color = col;
        }

        if (pulseLabel)
            pulseLabel.color = col;

        if (statusText)
        {
            statusText.color = new Color(col.r, col.g, col.b, 0.55f);
            statusText.text = _bpm < 60 ? "BRADY" :
                              _bpm > 100 ? "TACHY" : "NORMAL";
        }
    }

    public void AddPulse(int amount)
    {
        BPM = _bpm + amount;
    }
}