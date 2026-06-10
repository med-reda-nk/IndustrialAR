using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class CyberLayerAnimator : MonoBehaviour
{
    public enum AnimationMode
    {
        Pulse,
        DriftPulse,
        Scanlines
    }

    public AnimationMode mode = AnimationMode.Pulse;
    public float speed = 1f;
    public float pulseMin = 0.45f;
    public float pulseMax = 1f;
    public float driftAmount = 6f;
    public Vector2 driftAxis = Vector2.right;

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Graphic graphic;
    private CyberScanlineGraphic scanlines;
    private Vector2 basePosition;
    private bool initialized;

    private void OnEnable()
    {
        Initialize();
    }

    private void Update()
    {
        Initialize();
        if (DashboardMotionAnimator.ReducedMotion)
        {
            ApplyAlpha(pulseMax);
            if (scanlines != null) scanlines.SetOffset(0f);
            if (rect != null) rect.anchoredPosition = basePosition;
            return;
        }

        float time = Time.realtimeSinceStartup;
        float pulse = Mathf.Lerp(pulseMin, pulseMax, 0.5f + 0.5f * Mathf.Sin(time * Mathf.PI * 2f * speed));

        if (mode == AnimationMode.Scanlines)
        {
            if (scanlines != null) scanlines.SetOffset(time * speed * 14f);
            ApplyAlpha(pulse);
            return;
        }

        ApplyAlpha(pulse);

        if (mode == AnimationMode.DriftPulse && rect != null)
        {
            Vector2 axis = driftAxis.sqrMagnitude > 0.001f ? driftAxis.normalized : Vector2.right;
            rect.anchoredPosition = basePosition + axis * (Mathf.Sin(time * Mathf.PI * 2f * speed) * driftAmount);
        }
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        rect = GetComponent<RectTransform>();
        if (rect != null) basePosition = rect.anchoredPosition;

        graphic = GetComponent<Graphic>();
        scanlines = GetComponent<CyberScanlineGraphic>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void ApplyAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        if (canvasGroup != null) canvasGroup.alpha = alpha;

        if (graphic != null)
        {
            Color color = graphic.color;
            color.a = Mathf.Clamp(color.a, 0.02f, 1f);
            graphic.color = color;
        }
    }
}
