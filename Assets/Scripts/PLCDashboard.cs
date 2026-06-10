using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class PLCDashboard : MonoBehaviour
{
    [Header("References")]
    public BenchSystem bench;

    [Header("Status LEDs")]
    public Image ledRunStop;
    public Image ledError;
    public Image ledMaint;
    public Image[] ledDI = new Image[8];
    public Image[] ledDQ = new Image[6];

    [Header("Legacy / Builder Support")]
    public TextMeshProUGUI textPill;
    public TextMeshProUGUI textTitle;
    public TextMeshProUGUI textCPUMode;
    public TextMeshProUGUI textCycleJitter;

    [Header("Fault Info")]
    public GameObject faultPanel;
    public TextMeshProUGUI textFaultDetails;
    public Button buttonResetFault;
    public Image resetFaultGlow;

    [Header("AR Overlays (Floating)")]
    public TextMeshProUGUI textScanCycle;
    public TextMeshProUGUI textFrequency;
    public Image barCPULoad;
    public TextMeshProUGUI textMemoryUsed;
    public TextMeshProUGUI textCycleCount;

    private static readonly Color RunColor = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    private static readonly Color StopColor = new Color32(0xFF, 0x6D, 0x00, 0xFF);
    private static readonly Color FaultColor = new Color32(0xD5, 0x00, 0x00, 0xFF);
    private static readonly Color InactiveColor = new Color32(0x1A, 0x1A, 0x1A, 0xFF);

    public void Start()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
    }

    public void Update()
    {
        if (bench == null) return;
        if (!Application.isPlaying && !bench.previewInEditor) return;

        UpdateLEDs();
        UpdateFaults();
        UpdateAROverlays();
    }

    // Drive the LED strip colors based on PLC state and I/O.
    public void UpdateLEDs()
    {
        if (ledRunStop != null)
        {
            if (bench.faultCode > 0) ledRunStop.color = FaultColor;
            else if (bench.plcRunning) ledRunStop.color = RunColor;
            else ledRunStop.color = StopColor;
        }

        if (ledError != null)
        {
            ledError.color = bench.faultCode > 0 ? FaultColor : InactiveColor;
        }

        if (ledMaint != null)
        {
            ledMaint.color = bench.plcMaintenanceRequired ? StopColor : InactiveColor;
        }

        for (int i = 0; i < ledDI.Length && i < bench.digitalInputs.Length; i++)
        {
            if (ledDI[i] != null)
                ledDI[i].color = bench.digitalInputs[i] ? RunColor : InactiveColor;
        }

        for (int i = 0; i < ledDQ.Length && i < bench.digitalOutputs.Length; i++)
        {
            if (ledDQ[i] != null)
                ledDQ[i].color = bench.digitalOutputs[i] ? RunColor : InactiveColor;
        }
    }

    // Show fault info and only enable reset when fault active.
    public void UpdateFaults()
    {
        bool hasFault = bench.faultCode > 0;
        if (faultPanel != null) faultPanel.SetActive(hasFault);
        if (textFaultDetails != null)
        {
            textFaultDetails.text = hasFault ? bench.faultDescription : string.Empty;
        }
        if (buttonResetFault != null) buttonResetFault.gameObject.SetActive(hasFault);

        if (resetFaultGlow != null)
        {
            float alpha = hasFault ? (0.5f + Mathf.PingPong(bench.simTime * 2f, 0.5f)) : 0f;
            resetFaultGlow.color = new Color(FaultColor.r, FaultColor.g, FaultColor.b, alpha);
        }
        else if (buttonResetFault != null && buttonResetFault.image != null)
        {
            float alpha = hasFault ? (0.6f + Mathf.PingPong(bench.simTime * 2f, 0.4f)) : 1f;
            buttonResetFault.image.color = new Color(FaultColor.r, FaultColor.g, FaultColor.b, alpha);
        }
    }

    // Update floating AR overlay values.
    public void UpdateAROverlays()
    {
        if (textCPUMode != null) textCPUMode.text = bench.cpuMode;
        if (textScanCycle != null) textScanCycle.text = bench.scanCycleMs.ToString("F2") + " ms";
        if (textFrequency != null) textFrequency.text = "FREQ " + bench.frequency.ToString("F1") + " Hz";
        if (textCycleJitter != null) textCycleJitter.text = (bench.scanCycleMs * 0.05f).ToString("F3") + " ms";
        if (barCPULoad != null) barCPULoad.fillAmount = Mathf.Clamp01(bench.cpuLoadPercent / 100f);
        if (textMemoryUsed != null) textMemoryUsed.text = bench.memoryUsedKB.ToString("F1") + " KB";
        if (textCycleCount != null) textCycleCount.text = bench.programCycleCount.ToString();
    }

    public void OnRunSwitch() { if (bench != null) bench.PLCSetRun(); }
    public void OnStopSwitch() { if (bench != null) bench.PLCSetStop(); }
    public void OnResetFault() { if (bench != null) bench.ResetFaults(); }
}
