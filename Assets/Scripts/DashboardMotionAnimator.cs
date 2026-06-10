using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class DashboardMotionAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public static bool ReducedMotion;

    public enum MotionType
    {
        Panel,
        Button,
        Indicator
    }

    public MotionType motionType = MotionType.Panel;
    public float entranceDelay;
    public float entranceDuration = 0.32f;
    public float hoverScale = 1.035f;
    public float pressScale = 0.965f;
    public float pulseAmount = 0.018f;
    public float pulseSpeed = 1.2f;
    public bool pulse;

    private RectTransform rect;
    private CanvasGroup group;
    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private float targetAlpha = 1f;
    private float startTime;
    private bool initialized;
    private bool hovered;
    private bool pressed;

    private void OnEnable()
    {
        Initialize();
        startTime = Time.realtimeSinceStartup;

        if (Application.isPlaying && motionType == MotionType.Panel)
        {
            rect.localScale = baseScale * 0.94f;
            if (group != null) group.alpha = 0f;
        }
        else
        {
            rect.localScale = baseScale;
            if (group != null) group.alpha = 1f;
        }
    }

    private void Update()
    {
        Initialize();
        if (rect == null) return;

        if (!Application.isPlaying || ReducedMotion)
        {
            rect.localScale = baseScale;
            if (group != null) group.alpha = 1f;
            return;
        }

        UpdateTargets();
        float scaleEase = motionType == MotionType.Button ? 16f : 8f;
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, UnscaledDamp(scaleEase));

        if (group != null)
        {
            group.alpha = Mathf.Lerp(group.alpha, targetAlpha, UnscaledDamp(10f));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        rect = GetComponent<RectTransform>();
        baseScale = rect != null ? rect.localScale : Vector3.one;
        targetScale = baseScale;

        group = GetComponent<CanvasGroup>();
        if (motionType == MotionType.Panel && group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    private void UpdateTargets()
    {
        float entrance = 1f;
        if (motionType == MotionType.Panel)
        {
            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - startTime - entranceDelay);
            entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, entranceDuration)));
            targetAlpha = entrance;
        }

        float scale = Mathf.Lerp(0.94f, 1f, entrance);
        if (motionType == MotionType.Button)
        {
            if (pressed) scale = pressScale;
            else if (hovered) scale = hoverScale;
        }

        if (pulse)
        {
            scale += Mathf.Sin(Time.realtimeSinceStartup * Mathf.PI * 2f * pulseSpeed) * pulseAmount;
        }

        targetScale = baseScale * scale;
    }

    private static float UnscaledDamp(float speed)
    {
        return 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
    }
}
