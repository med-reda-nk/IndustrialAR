using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[ExecuteAlways]
public class SignalTowerDashboard : MonoBehaviour
{
    [Header("References")]
    public BenchSystem bench;

    [Header("Tower Modules")]
    public Image moduleRed;
    public Image moduleOrange;
    public Image moduleGreen;
    public Image glowRed;
    public Image glowOrange;
    public Image glowGreen;

    [Header("Legacy / Builder Support")]
    public TextMeshProUGUI textPill;
    public TextMeshProUGUI textTitle;

    [Header("AR Overlays (Floating)")]
    public TextMeshProUGUI textStateBadge;
    public TextMeshProUGUI textStopwatch;
    public TextMeshProUGUI textStateHistory;
    public TextMeshProUGUI textReason;
    public TextMeshProUGUI textNote;

    private string lastState;
    private float stateTimer;
    private readonly List<string> history = new List<string>();

    private static readonly Color RunOn = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    private static readonly Color OrangeOn = new Color32(0xFF, 0x6D, 0x00, 0xFF);
    private static readonly Color RedOn = new Color32(0xD5, 0x00, 0x00, 0xFF);
    private static readonly Color OffColor = new Color32(0x1A, 0x1A, 0x1A, 0x96);

    public void Start()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
    }

    public void Update()
    {
        if (bench == null) return;
        if (!Application.isPlaying && !bench.previewInEditor) return;

        UpdateTowerVisuals();
        UpdateAROverlays();
    }

    // Drive the stack light colors and glow based on the bench state.
    public void UpdateTowerVisuals()
    {
        bool redOn = bench.lightRed;
        bool orangeOn = bench.lightOrange;
        bool greenOn = bench.lightGreen;
        float timeNow = bench.simTime;

        if (moduleRed != null) moduleRed.color = redOn ? RedOn : OffColor;
        if (moduleGreen != null) moduleGreen.color = greenOn ? RunOn : OffColor;

        if (moduleOrange != null)
        {
            if (orangeOn && bench.lightBlinking)
            {
                float alpha = 0.3f + Mathf.PingPong(timeNow * 2f, 0.7f);
                moduleOrange.color = new Color(OrangeOn.r, OrangeOn.g, OrangeOn.b, alpha);
            }
            else
            {
                moduleOrange.color = orangeOn ? OrangeOn : OffColor;
            }
        }

        UpdateGlow(glowRed, redOn, RedOn, false, timeNow);
        UpdateGlow(glowOrange, orangeOn, OrangeOn, bench.lightBlinking, timeNow);
        UpdateGlow(glowGreen, greenOn, RunOn, false, timeNow);
    }

    // Update the overlay badges and state history.
    public void UpdateAROverlays()
    {
        if (bench.towerState != lastState)
        {
            if (!string.IsNullOrEmpty(lastState))
            {
                history.Insert(0, lastState + " [" + System.DateTime.Now.ToString("HH:mm") + "]");
                if (history.Count > 3) history.RemoveAt(3);
            }
            lastState = bench.towerState;
            stateTimer = 0f;
            if (textStateHistory != null) textStateHistory.text = string.Join("\n", history);
        }
        stateTimer += bench.simDeltaTime;

        if (textStateBadge != null) textStateBadge.text = bench.towerState;
        if (textStopwatch != null) textStopwatch.text = stateTimer.ToString("F1") + "s";
        if (textNote != null) textNote.text = "Controlled by S7-1200 - Q0.1/Q0.2/Q0.3";

        if (textReason != null)
        {
            if (bench.eStopPressed) textReason.text = "EMERGENCY STOP ACTIVE";
            else if (bench.faultCode > 0) textReason.text = "SYSTEM FAULT: " + bench.faultCode;
            else if (bench.motorTemp > 70f) textReason.text = "MOTOR TEMP WARNING";
            else if (bench.vibration > 2.8f) textReason.text = "VIBRATION WARNING";
            else if (bench.towerState == "TRANSITION") textReason.text = "SYSTEM STARTING";
            else if (bench.motorRPM > 100f) textReason.text = "SYSTEM RUNNING";
            else textReason.text = "SYSTEM IDLE";
        }
    }

    public void UpdateGlow(Image glow, bool isOn, Color color, bool blinking, float timeNow)
    {
        if (glow == null) return;
        if (!isOn)
        {
            glow.color = new Color(color.r, color.g, color.b, 0f);
            return;
        }

        float alpha = blinking ? (0.3f + Mathf.PingPong(timeNow * 2f, 0.7f)) : 0.8f;
        glow.color = new Color(color.r, color.g, color.b, alpha);
    }
}
