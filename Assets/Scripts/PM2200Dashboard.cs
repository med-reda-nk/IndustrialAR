using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class PM2200Dashboard : MonoBehaviour
{
    [Header("References")]
    public BenchSystem bench;

    [Header("LCD Display Rows")]
    public TextMeshProUGUI textScreenTitle;
    public TextMeshProUGUI textRow1Label;
    public TextMeshProUGUI textRow1Value;
    public TextMeshProUGUI textRow2Label;
    public TextMeshProUGUI textRow2Value;
    public TextMeshProUGUI textRow3Label;
    public TextMeshProUGUI textRow3Value;
    public TextMeshProUGUI textRow4Label;
    public TextMeshProUGUI textRow4Value;
    public TextMeshProUGUI textNavBar;

    [Header("Indicators")]
    public Image statusLED;
    public Image lcdBackground;

    [Header("AR Overlays (Floating)")]
    public TextMeshProUGUI textBadgeQuality;
    public TextMeshProUGUI textTHD;
    public TextMeshProUGUI textPowerFactor;
    public TextMeshProUGUI textDemand;

    [Header("Legacy / Builder Support")]
    public TextMeshProUGUI textPill;
    public TextMeshProUGUI textTitle;
    public TextMeshProUGUI textVoltageL1L2;
    public TextMeshProUGUI textCurrentL1;
    public TextMeshProUGUI textActivePower;
    public TextMeshProUGUI textEnergyCost;
    public TextMeshProUGUI textCarbonFootprint;
    public TextMeshProUGUI textEventLog;
    public TextMeshProUGUI textModbusAddr;
    public TextMeshProUGUI textGatewayIP;
    public GameObject alertBanner;

    private float resetTimer;
    private bool isHoldingReset;

    private static readonly Color LCDBackgroundColor = new Color32(24, 52, 56, 255);
    private static readonly Color LCDTextColor = new Color32(213, 238, 232, 255);

    public void Start()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        ApplyLCDTheme();
    }

    public void OnValidate()
    {
        ApplyLCDTheme();
    }

    public void Update()
    {
        if (bench == null) return;

        ApplyLCDTheme();
        if (!Application.isPlaying && !bench.previewInEditor) return;

        UpdateLCD();
        UpdateLED();
        UpdateAROverlays();
        HandleResetHold();
    }

    // Apply LCD colors to match the PM2200 screen style.
    public void ApplyLCDTheme()
    {
        if (lcdBackground != null) lcdBackground.color = LCDBackgroundColor;

        SetTextColor(textScreenTitle, LCDTextColor);
        SetTextColor(textRow1Label, LCDTextColor);
        SetTextColor(textRow1Value, LCDTextColor);
        SetTextColor(textRow2Label, LCDTextColor);
        SetTextColor(textRow2Value, LCDTextColor);
        SetTextColor(textRow3Label, LCDTextColor);
        SetTextColor(textRow3Value, LCDTextColor);
        SetTextColor(textRow4Label, LCDTextColor);
        SetTextColor(textRow4Value, LCDTextColor);
        SetTextColor(textNavBar, LCDTextColor);
    }

    public void UpdateLCD()
    {
        if (textScreenTitle == null || bench.pm2200Screens == null || bench.pm2200Screens.Length == 0) return;

        int index = Mathf.Clamp(bench.pm2200ScreenIndex, 0, bench.pm2200Screens.Length - 1);
        string currentScreen = bench.pm2200Screens[index];
        textScreenTitle.text = currentScreen;

        switch (index)
        {
            case 0: // Récapitulatif
                SetRow(1, "Vmoy", FormatValue(bench.voltage, 4, "V", 8));
                SetRow(2, "Imoy", FormatValue(bench.current * 1000f, 4, "mA", 8));
                SetRow(3, "Ptot", FormatValue(bench.power, 5, "kW", 8));
                SetRow(4, "E.Fni", FormatValue(bench.energy, 1, "Wh", 8));
                break;
            case 1: // U-U
                SetRow(1, "Vab", FormatValue(PhaseVoltage(bench.voltageAB), 1, "V", 8));
                SetRow(2, "Vbc", FormatValue(PhaseVoltage(bench.voltageBC), 1, "V", 8));
                SetRow(3, "Vca", FormatValue(PhaseVoltage(bench.voltageCA), 1, "V", 8));
                SetRow(4, "Vmoy", FormatValue(bench.voltage, 1, "V", 8));
                break;
            case 2: // I
                SetRow(1, "I1", FormatValue(bench.current * 1.02f, 2, "A", 8));
                SetRow(2, "I2", FormatValue(bench.current * 0.98f, 2, "A", 8));
                SetRow(3, "I3", FormatValue(bench.current, 2, "A", 8));
                SetRow(4, "Imoy", FormatValue(bench.current, 2, "A", 8));
                break;
            case 3: // POS
                SetRow(1, "Ptot", FormatValue(bench.power, 3, "kW", 8));
                SetRow(2, "Qtot", FormatValue(bench.power * 0.4f, 3, "kVAR", 8));
                SetRow(3, "Stot", FormatValue(bench.power * 1.1f, 3, "kVA", 8));
                SetRow(4, "Fact. puiss.", bench.powerFactor.ToString("F2"));
                break;
        }

        if (textNavBar != null) textNavBar.text = "I    U-U    POS    >";
    }

    public void SetRow(int row, string label, string value)
    {
        switch (row)
        {
            case 1:
                if (textRow1Label != null) textRow1Label.text = label;
                if (textRow1Value != null) textRow1Value.text = value;
                break;
            case 2:
                if (textRow2Label != null) textRow2Label.text = label;
                if (textRow2Value != null) textRow2Value.text = value;
                break;
            case 3:
                if (textRow3Label != null) textRow3Label.text = label;
                if (textRow3Value != null) textRow3Value.text = value;
                break;
            case 4:
                if (textRow4Label != null) textRow4Label.text = label;
                if (textRow4Value != null) textRow4Value.text = value;
                break;
        }
    }

    public void UpdateLED()
    {
        if (statusLED == null) return;

        if (bench.pm2200DataUpdating)
        {
            float alpha = 0.4f + Mathf.PingPong(bench.simTime * 2f, 0.6f);
            statusLED.color = new Color(0f, 0.9f, 1f, alpha);
        }
        else
        {
            statusLED.color = new Color(0f, 0.9f, 1f, 1f);
        }
    }

    public void UpdateAROverlays()
    {
        if (textBadgeQuality != null)
        {
            float pf = bench.powerFactor;
            float thd = bench.harmonicDistortionPM;
            if (pf >= 0.95f && thd <= 3.0f) textBadgeQuality.text = "GOOD";
            else if (pf >= 0.85f && thd <= 5.0f) textBadgeQuality.text = "FAIR";
            else textBadgeQuality.text = "POOR";
            textBadgeQuality.color = Color.black;
        }

        if (textTHD != null) textTHD.text = "THD: " + bench.harmonicDistortionPM.ToString("F1") + "%";
        if (textPowerFactor != null) textPowerFactor.text = "Facteur puissance: " + bench.powerFactor.ToString("F2");
        if (textDemand != null) textDemand.text = "Demand: " + bench.demandkW.ToString("F3") + " kW";

        if (textVoltageL1L2 != null) textVoltageL1L2.text = bench.voltage.ToString("F1") + " V";
        if (textCurrentL1 != null) textCurrentL1.text = bench.current.ToString("F2") + " A";
        if (textActivePower != null) textActivePower.text = bench.power.ToString("F3") + " kW";
        if (textEnergyCost != null) textEnergyCost.text = "$" + (bench.energy * 0.00015f).ToString("F2");
        if (textCarbonFootprint != null) textCarbonFootprint.text = (bench.energy * 0.00045f).ToString("F2") + " kg";
        if (textModbusAddr != null) textModbusAddr.text = bench.modbusAddress.ToString();
        if (textGatewayIP != null) textGatewayIP.text = bench.gatewayIP;
        if (textEventLog != null) textEventLog.text = string.Join("\n", bench.pmEventLog);
        if (alertBanner != null) alertBanner.SetActive(bench.eStopPressed || bench.pm2200Register3204 == 1);
    }

    public void HandleResetHold()
    {
        if (!isHoldingReset) return;

        resetTimer += bench.simDeltaTime;
        if (resetTimer >= 3.0f)
        {
            bench.ResetEnergy();
            resetTimer = 0f;
            isHoldingReset = false;
        }
    }

    public void OnPrevButton() { if (bench != null) bench.PM2200PrevScreen(); }
    public void OnNextButton() { if (bench != null) bench.PM2200NextScreen(); }
    public void OnSelectButton() { if (bench != null) bench.PM2200Select(); }
    public void OnDownButtonPress() { isHoldingReset = true; resetTimer = 0f; }
    public void OnDownButtonRelease() { isHoldingReset = false; }

    public string FormatValue(float value, int decimals, string unit, int padWidth)
    {
        string num = value.ToString("F" + decimals);
        if (padWidth > 0) num = num.PadLeft(padWidth);
        return num + " " + unit;
    }

    public void SetTextColor(TextMeshProUGUI text, Color color)
    {
        if (text != null) text.color = color;
    }

    private float PhaseVoltage(float phaseVoltage)
    {
        return Mathf.Abs(phaseVoltage) > 0.0001f ? phaseVoltage : bench.voltage;
    }
}
