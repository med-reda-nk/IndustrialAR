using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class MotorDashboard : MonoBehaviour
{
    [Header("References")]
    public BenchSystem bench;

    [Header("Visual Components")]
    public RectTransform shaftVisual;
    public Image temperatureArc;
    public RectTransform directionArrow;

    [Header("Legacy / Builder Support")]
    public TextMeshProUGUI textPill;
    public TextMeshProUGUI textTitle;
    public Image barRPM;
    public TextMeshProUGUI textEfficiency;
    public TextMeshProUGUI textPowerFactor;
    public TextMeshProUGUI textFrequency;

    [Header("Face Overlays")]
    public TextMeshProUGUI textRPM;
    public TextMeshProUGUI textTorque;
    public TextMeshProUGUI textVibration;
    public GameObject motorFaultBorder;

    [Header("AR Overlays (Floating)")]
    public TextMeshProUGUI textMotorStatus;
    public Image barEfficiency;

    private static readonly Color TempCool = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    private static readonly Color TempOrange = new Color32(0xFF, 0x6D, 0x00, 0xFF);
    private static readonly Color TempRed = new Color32(0xD5, 0x00, 0x00, 0xFF);

    public void Start()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
    }

    public void Update()
    {
        if (bench == null) return;
        if (!Application.isPlaying && !bench.previewInEditor) return;

        UpdateShaftAnimation();
        UpdateFaceOverlays();
        UpdateAROverlays();
    }

    // Rotate the shaft and direction arrow based on RPM and direction.
    public void UpdateShaftAnimation()
    {
        float rotationAmount = (bench.motorRPM / 60f) * 360f * bench.simDeltaTime;
        float directionSign = bench.motorDirection == "REV" ? 1f : -1f;

        if (shaftVisual != null)
        {
            shaftVisual.Rotate(0f, 0f, directionSign * rotationAmount);
        }

        if (directionArrow != null)
        {
            directionArrow.Rotate(0f, 0f, directionSign * rotationAmount);

            if (bench.motorRPM > 10f)
            {
                float scale = 1.0f + Mathf.PingPong(bench.simTime * 2f, 0.2f);
                directionArrow.localScale = Vector3.one * scale;
            }
            else
            {
                directionArrow.localScale = Vector3.one;
            }
        }
    }

    // Update the motor face overlays with live telemetry.
    public void UpdateFaceOverlays()
    {
        if (textRPM != null) textRPM.text = bench.motorRPM.ToString("F0");
        if (barRPM != null) barRPM.fillAmount = Mathf.Clamp01(bench.motorRPM / 1475f);

        if (temperatureArc != null)
        {
            float tempT = Mathf.InverseLerp(20f, 100f, bench.motorTemp);
            temperatureArc.fillAmount = tempT;
            if (tempT < 0.5f) temperatureArc.color = Color.Lerp(TempCool, TempOrange, tempT * 2f);
            else temperatureArc.color = Color.Lerp(TempOrange, TempRed, (tempT - 0.5f) * 2f);
        }

        if (textTorque != null) textTorque.text = bench.torque.ToString("F1") + " Nm";
        if (textFrequency != null) textFrequency.text = "Freq: " + bench.frequency.ToString("F1") + " Hz";
        if (textEfficiency != null) textEfficiency.text = bench.efficiency.ToString("F1") + " %";
        if (textPowerFactor != null) textPowerFactor.text = bench.powerFactor.ToString("F2");
        if (textVibration != null) textVibration.text = bench.vibration.ToString("F2") + " mm/s";

        if (motorFaultBorder != null) motorFaultBorder.SetActive(bench.motorFault);
    }

    // Floating AR overlay status and efficiency bar.
    public void UpdateAROverlays()
    {
        if (textMotorStatus != null)
        {
            if (bench.motorFault) { textMotorStatus.text = "FAULT"; textMotorStatus.color = TempRed; }
            else if (bench.motorOverheat) { textMotorStatus.text = "OVERHEAT"; textMotorStatus.color = TempOrange; }
            else if (bench.motorRPM > 100f) { textMotorStatus.text = "RUNNING"; textMotorStatus.color = TempCool; }
            else { textMotorStatus.text = "IDLE"; textMotorStatus.color = Color.gray; }
        }

        if (barEfficiency != null) barEfficiency.fillAmount = Mathf.Clamp01(bench.efficiency / 100f);
    }

    public void OnStartMotor() { if (bench != null) bench.VFDPowerOn(); }
    public void OnStopMotor() { if (bench != null) bench.VFDPowerOff(); }
    public void OnFreqUp() { if (bench != null) bench.VFDFrequencyUp(); }
    public void OnFreqDown() { if (bench != null) bench.VFDFrequencyDown(); }
    public void OnToggleDirection()
    {
        if (bench == null) return;
        if (bench.motorRPM < 10f)
        {
            if (bench.motorDirection == "FWD") bench.MotorSetREV();
            else bench.MotorSetFWD();
        }
    }
}
