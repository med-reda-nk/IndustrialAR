using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[ExecuteAlways]
public class HMIDashboard : MonoBehaviour
{
    [Header("References")]
    public BenchSystem bench;

    [Header("Screen Content Roots")]
    public GameObject screenMain;
    public GameObject screenMotor;
    public GameObject screenPower;
    public GameObject screenDiagnostics;
    public GameObject screenAlarms;

    [Header("Common UI Elements")]
    public TextMeshProUGUI textScreenTitle;
    public TextMeshProUGUI textDateTime;
    public Image connectionIcon;
    public GameObject alarmBar;
    public Image alarmBarImage;
    public TextMeshProUGUI textAlarmCount;

    [Header("Main Screen Elements")]
    public TextMeshProUGUI textMainStatus;

    [Header("Motor Screen Elements")]
    public TextMeshProUGUI textMotorRPM;
    public TextMeshProUGUI textMotorTarget;

    [Header("Power Screen Elements")]
    public TextMeshProUGUI textPowerVoltage;
    public TextMeshProUGUI textPowerCurrent;
    public TextMeshProUGUI textPowerWatts;

    [Header("Diagnostics Elements")]
    public TextMeshProUGUI textDiagFault;
    public TextMeshProUGUI textDiagCPU;
    public TextMeshProUGUI textDiagIO;

    [Header("Alarm Screen Elements")]
    public TextMeshProUGUI textAlarmList;

    [Header("Legacy / Builder Support")]
    public TextMeshProUGUI textPill;
    public TextMeshProUGUI textTitle;

    private float timeTimer;
    private string cachedTime;

    private const string ScreenMain = "MAIN";
    private const string ScreenMotor = "MOTOR CONTROL";
    private const string ScreenPower = "POWER MONITOR";
    private const string ScreenDiagnostics = "DIAGNOSTICS";
    private const string ScreenAlarms = "ALARMS";
    private const string ScreenTrends = "TRENDS";
    private const string ScreenSettings = "SETTINGS";
    private const string ScreenHelp = "HELP";

    public void Start()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        cachedTime = DateTime.Now.ToString("HH:mm:ss");
    }

    public void Update()
    {
        if (bench == null) return;
        if (!Application.isPlaying && !bench.previewInEditor) return;

        UpdateCommonUI();
        UpdateActiveScreen();
    }

    // Common UI elements: time, connection, alarm bar.
    public void UpdateCommonUI()
    {
        timeTimer += bench.simDeltaTime;
        if (timeTimer >= 1f)
        {
            timeTimer = 0f;
            cachedTime = DateTime.Now.ToString("HH:mm:ss");
        }
        if (textDateTime != null) textDateTime.text = cachedTime;

        if (connectionIcon != null)
        {
            if (bench.plcConnected)
            {
                float alpha = 0.5f + Mathf.PingPong(bench.simTime * 2f, 0.5f);
                connectionIcon.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                connectionIcon.color = new Color(1f, 1f, 1f, 0.2f);
            }
        }

        bool hasAlarms = (bench.alarmCount + bench.warningCount) > 0;
        if (alarmBar != null) alarmBar.SetActive(true);
        if (alarmBarImage == null && alarmBar != null) alarmBarImage = alarmBar.GetComponent<Image>();
        if (alarmBarImage != null)
        {
            alarmBarImage.color = hasAlarms ? new Color32(0xD5, 0x00, 0x00, 0xFF) : new Color32(0x66, 0x66, 0x66, 0xFF);
        }
        if (textAlarmCount != null)
        {
            textAlarmCount.text = hasAlarms ? (bench.alarmCount + bench.warningCount).ToString() + " ALARMS ACTIVE" : "ALARMS CLEAR";
        }
    }

    // Switch between the HMI screens based on the active screen name.
    public void UpdateActiveScreen()
    {
        string screen = NormalizeScreenName(bench.activeScreen);
        if (textScreenTitle != null) textScreenTitle.text = screen;

        if (screenMain) screenMain.SetActive(false);
        if (screenMotor) screenMotor.SetActive(false);
        if (screenPower) screenPower.SetActive(false);
        if (screenDiagnostics) screenDiagnostics.SetActive(false);
        if (screenAlarms) screenAlarms.SetActive(false);

        switch (screen)
        {
            case ScreenMain:
                if (screenMain) screenMain.SetActive(true);
                if (textMainStatus != null)
                {
                    string plcState = bench.plcRunning ? "RUN" : "STOP";
                    string motorState = bench.motorRPM > 100f ? "RUNNING" : "STOPPED";
                    string estopState = bench.eStopPressed ? "ESTOP" : "SAFE";
                    string pmState = bench.pm2200DataUpdating ? "UPDATING" : "IDLE";
                    textMainStatus.text = "PLC: " + plcState + "  MOTOR: " + motorState + "  ESTOP: " + estopState
                                          + "\nTOWER: " + bench.towerState + "  PM2200: " + pmState;
                }
                break;
            case ScreenMotor:
                if (screenMotor) screenMotor.SetActive(true);
                if (textMotorRPM != null) textMotorRPM.text = "RPM: " + bench.motorRPM.ToString("F0") + "  DIR: " + bench.motorDirection;
                if (textMotorTarget != null) textMotorTarget.text = "FREQ: " + bench.frequency.ToString("F1") + " Hz  TARGET: " + bench.targetFrequency.ToString("F1") + " Hz";
                break;
            case ScreenPower:
                if (screenPower) screenPower.SetActive(true);
                if (textPowerVoltage != null) textPowerVoltage.text = "V: " + bench.voltage.ToString("F1") + " V";
                if (textPowerCurrent != null) textPowerCurrent.text = "I: " + bench.current.ToString("F2") + " A";
                if (textPowerWatts != null) textPowerWatts.text = "P: " + bench.power.ToString("F3") + " kW  PF: " + bench.powerFactor.ToString("F2");
                break;
            case ScreenDiagnostics:
                if (screenDiagnostics) screenDiagnostics.SetActive(true);
                if (textDiagFault != null) textDiagFault.text = "Fault: " + (bench.faultCode == 0 ? "NONE" : bench.faultDescription);
                if (textDiagCPU != null) textDiagCPU.text = "CPU: " + bench.cpuLoadPercent.ToString("F1") + "%  Scan: " + bench.scanCycleMs.ToString("F2") + " ms";
                if (textDiagIO != null)
                {
                    textDiagIO.text = "DI: " + FormatIO(bench.digitalInputs) + "\nDQ: " + FormatIO(bench.digitalOutputs);
                }
                break;
            case ScreenAlarms:
                if (screenAlarms) screenAlarms.SetActive(true);
                if (textAlarmList != null)
                {
                    if (bench.faultCode == 0 && bench.warningCount == 0) textAlarmList.text = "No active alarms";
                    else
                    {
                        string now = DateTime.Now.ToString("HH:mm:ss");
                        string lines = string.Empty;
                        if (bench.faultCode > 0) lines = "[" + now + "] FAULT " + bench.faultCode + ": " + bench.faultDescription;
                        if (bench.warningCount > 0)
                        {
                            if (!string.IsNullOrEmpty(lines)) lines += "\n";
                            lines += "[" + now + "] MAINTENANCE REQUIRED";
                        }
                        textAlarmList.text = lines;
                    }
                }
                break;
            case ScreenTrends:
                ShowPlaceholder(ScreenTrends);
                break;
            case ScreenSettings:
                ShowPlaceholder(ScreenSettings);
                break;
            case ScreenHelp:
                ShowPlaceholder(ScreenHelp);
                break;
            default:
                ShowPlaceholder(screen);
                break;
        }
    }

    public void OnF1() { if (bench != null) bench.activeScreen = ScreenMain; }
    public void OnF2() { if (bench != null) bench.activeScreen = ScreenMotor; }
    public void OnF3() { if (bench != null) bench.activeScreen = ScreenPower; }
    public void OnF4() { if (bench != null) bench.activeScreen = ScreenDiagnostics; }
    public void OnF5() { if (bench != null) bench.activeScreen = ScreenAlarms; }
    public void OnF6() { if (bench != null) bench.activeScreen = ScreenTrends; }
    public void OnF7() { if (bench != null) bench.activeScreen = ScreenSettings; }
    public void OnF8() { if (bench != null) bench.activeScreen = ScreenHelp; }

    public void OnAckAlarms() { if (bench != null) bench.AcknowledgeAlarm(); }

    public void OnMotorStart() { if (bench != null) bench.VFDPowerOn(); }
    public void OnMotorStop() { if (bench != null) bench.VFDPowerOff(); }
    public void OnFreqUp() { if (bench != null) bench.VFDFrequencyUp(); }
    public void OnFreqDown() { if (bench != null) bench.VFDFrequencyDown(); }

    public string NormalizeScreenName(string name)
    {
        if (string.IsNullOrEmpty(name)) return ScreenMain;
        string upper = name.Trim().ToUpperInvariant();
        if (upper == "MAIN DASHBOARD") return ScreenMain;
        if (upper == "MOTOR CONTROL") return ScreenMotor;
        if (upper == "POWER MONITOR") return ScreenPower;
        if (upper == "DIAGNOSTICS") return ScreenDiagnostics;
        if (upper == "ALARMS") return ScreenAlarms;
        if (upper == "TRENDS") return ScreenTrends;
        if (upper == "SETTINGS") return ScreenSettings;
        if (upper == "HELP") return ScreenHelp;
        return upper;
    }

    public void ShowPlaceholder(string title)
    {
        if (screenMain) screenMain.SetActive(true);
        if (textMainStatus != null) textMainStatus.text = title + " (PLACEHOLDER)";
    }

    public string FormatIO(bool[] values)
    {
        if (values == null || values.Length == 0) return "-";
        string[] parts = new string[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            parts[i] = values[i] ? "1" : "0";
        }
        return string.Join("", parts);
    }
}
