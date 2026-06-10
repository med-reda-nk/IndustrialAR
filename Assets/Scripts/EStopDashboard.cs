using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class EStopDashboard : MonoBehaviour
{
    [Header("References")]
    public BenchSystem bench;

    [Header("Visual Components")]
    public Image housingPanel;
    public RectTransform mushroomButton;
    public Image mushroomImage;
    public GameObject highlightOverlay;
    public GameObject pressedShadow;
    public Image statusLED;
    public GameObject releaseButtonRoot;

    [Header("Legacy / Builder Support")]
    public TextMeshProUGUI textPill;
    public TextMeshProUGUI textTitle;

    [Header("AR Overlays (Floating)")]
    public TextMeshProUGUI textStateBadge;
    public TextMeshProUGUI textPressCount;
    public TextMeshProUGUI textHaltTimer;
    public TextMeshProUGUI textLastEvent;

    private static readonly Color HousingGrey = new Color32(42, 42, 42, 0xFF);
    private static readonly Color HousingGreyActive = new Color32(62, 62, 62, 0xFF);
    private static readonly Color ButtonRed = new Color32(0xD5, 0x00, 0x00, 0xFF);
    private static readonly Color ButtonRedPressed = new Color32(0x8B, 0x00, 0x00, 0xFF);
    private static readonly Color ReadyCyan = new Color32(0x00, 0xE5, 0xFF, 0xFF);

    public void Start()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
    }

    public void Update()
    {
        if (bench == null) return;
        if (!Application.isPlaying && !bench.previewInEditor) return;

        UpdateButtonVisuals();
        UpdateHousingPulse();
        UpdateAROverlays();
    }

    // Toggle pressed vs released visuals on the mushroom button.
    public void UpdateButtonVisuals()
    {
        if (mushroomButton == null || mushroomImage == null) return;

        if (bench.eStopPressed)
        {
            mushroomButton.localScale = new Vector3(0.95f, 0.95f, 1f);
            mushroomImage.color = ButtonRedPressed;
            if (highlightOverlay != null) highlightOverlay.SetActive(false);
            if (pressedShadow != null) pressedShadow.SetActive(true);
            if (statusLED != null) statusLED.color = Color.red;
            if (releaseButtonRoot != null) releaseButtonRoot.SetActive(true);
        }
        else
        {
            mushroomButton.localScale = Vector3.one;
            mushroomImage.color = ButtonRed;
            if (highlightOverlay != null) highlightOverlay.SetActive(true);
            if (pressedShadow != null) pressedShadow.SetActive(false);
            if (statusLED != null) statusLED.color = ReadyCyan;
            if (releaseButtonRoot != null) releaseButtonRoot.SetActive(false);
        }
    }

    // Pulse the housing alpha while E-Stop is active.
    public void UpdateHousingPulse()
    {
        if (housingPanel == null) return;

        if (bench.eStopPressed)
        {
            float t = (Mathf.Sin(bench.simTime * 2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(200f / 255f, 1f, t);
            housingPanel.color = new Color(HousingGreyActive.r / 255f, HousingGreyActive.g / 255f, HousingGreyActive.b / 255f, alpha);
        }
        else
        {
            housingPanel.color = HousingGrey;
        }
    }

    // Update the floating AR overlays for status and timers.
    public void UpdateAROverlays()
    {
        if (textStateBadge != null)
        {
            textStateBadge.text = bench.eStopPressed ? "HALTED" : "READY";
            textStateBadge.color = bench.eStopPressed ? Color.red : ReadyCyan;
        }

        if (textPressCount != null) textPressCount.text = "ACTUATIONS: " + bench.eStopPressCount;

        if (textHaltTimer != null)
        {
            if (bench.eStopPressed)
            {
                int minutes = Mathf.FloorToInt(bench.haltDuration / 60f);
                int seconds = Mathf.FloorToInt(bench.haltDuration % 60f);
                textHaltTimer.text = "DOWNTIME: " + string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            else
            {
                textHaltTimer.text = "DOWNTIME: --:--";
            }
        }

        if (textLastEvent != null) textLastEvent.text = "LAST EVENT: " + bench.lastEStopTime;
    }

    public void OnPressButton() { if (bench != null && !bench.eStopPressed) bench.EStopPress(); }
    public void OnReleaseButton() { if (bench != null && bench.eStopPressed) bench.EStopRelease(); }
}
