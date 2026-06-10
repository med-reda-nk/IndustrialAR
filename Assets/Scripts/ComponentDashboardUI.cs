using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class ComponentDashboardUI : MonoBehaviour
{
    public enum DashboardType
    {
        Auto,
        PM2200,
        Motor,
        PLC,
        SignalTower,
        EStop,
        HMI
    }

    [Header("Type")]
    public DashboardType dashboardType = DashboardType.Auto;

    [Header("References")]
    public VFDController vfd;
    public BenchSystem bench;
    public MotorController motor;
    public SignalTower tower;
    public EStopButton eStop;

    [Header("Header")]
    public TextMeshProUGUI pillText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public Image statusDot;
    public Button wiringGuideButton;

    [Header("Meta")]
    public TextMeshProUGUI metaNodeValue;
    public TextMeshProUGUI metaProtocolValue;
    public TextMeshProUGUI metaPhaseValue;
    public TextMeshProUGUI metaRefreshValue;

    [Header("Telemetry")]
    public TextMeshProUGUI[] metricLabels = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] metricValues = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] metricUnits  = new TextMeshProUGUI[3];
    public Image[] metricBars = new Image[3];

    [Header("Cards")]
    public TextMeshProUGUI card1Label;
    public TextMeshProUGUI card1Value;
    public TextMeshProUGUI card1Unit;
    public TextMeshProUGUI card2Label;
    public TextMeshProUGUI card2Value;
    public TextMeshProUGUI card2Unit;

    [Header("I/O Mapping")]
    public TextMeshProUGUI ioInputsText;
    public TextMeshProUGUI ioOutputsText;

    [Header("Wiring Status")]
    public TextMeshProUGUI wiringStatusText;
    public Image wiringStatusDot;

    [Header("Controls")]
    public GameObject powerControlSection;
    public TextMeshProUGUI powerControlTitle;
    public Button powerOnButton;
    public Button powerOffButton;
    public TextMeshProUGUI powerOnLabel;
    public TextMeshProUGUI powerOffLabel;

    public GameObject frequencyControlSection;
    public TextMeshProUGUI frequencyControlTitle;
    public Button freqDownButton;
    public Button freqUpButton;
    public TextMeshProUGUI freqDownLabel;
    public TextMeshProUGUI freqUpLabel;
    public TextMeshProUGUI freqDisplay;

    [Header("PM2200 Specific")]
    public TextMeshProUGUI pmModbusAddrText;
    public TextMeshProUGUI pmGatewayIPText;
    public TextMeshProUGUI pmPeakDemandText;
    public TextMeshProUGUI pmVUnbalanceText;

    [Header("Generic Controls")]
    public Button genericBtn1;
    public Button genericBtn2;
    public Button genericBtn3;
    public Button genericBtn4;
    public Button genericBtn5;
    public Button genericBtn6;
    public TextMeshProUGUI genericLbl1;
    public TextMeshProUGUI genericLbl2;
    public TextMeshProUGUI genericLbl3;
    public TextMeshProUGUI genericLbl4;
    public TextMeshProUGUI genericLbl5;
    public TextMeshProUGUI genericLbl6;

    [Header("Footer")]
    public TextMeshProUGUI footerLeftText;
    public TextMeshProUGUI footerRightText;
    public Image footerDot;
    public Button dataModeButton;
    public TextMeshProUGUI dataModeLabel;

    private float energy = 161f;
    private bool configured;

    private static readonly Color StatusRunning = new Color(0.2f, 0.9f, 0.7f, 1f);
    private static readonly Color StatusIdle    = new Color(0.55f, 0.55f, 0.55f, 1f);
    private static readonly Color StatusFault   = new Color(0.9f, 0.25f, 0.25f, 1f);
    private static readonly Color WiredOk       = new Color(0.2f, 0.9f, 0.5f, 1f);
    private static readonly Color WiredMissing  = new Color(0.95f, 0.6f, 0.1f, 1f);

    private void OnEnable()  { ResolveReferences(); Configure(); }
    private void OnValidate(){ configured = false; ResolveReferences(); Configure(); }
    private void Start()     { ResolveReferences(); Configure(); }
    private void Update()
    {
        if (Application.isPlaying)
        {
            UpdateDisplay();
            UpdateWiringStatus();
            UpdateDataMode();
            UpdateControlsLockout();
        }
        else
        {
            // In editor, only update if configured or once in a while
            if (!configured) Configure();
        }
    }

    private void UpdateControlsLockout()
    {
        if (bench == null) return;

        bool interactable = !bench.isLockedOut;
        
        // E-Stop dashboard buttons should remain interactable to allow reset/monitoring
        if (dashboardType == DashboardType.EStop) interactable = true;

        if (powerOnButton != null) powerOnButton.interactable = interactable;
        if (powerOffButton != null) powerOffButton.interactable = interactable;
        if (freqDownButton != null) freqDownButton.interactable = interactable;
        if (freqUpButton != null) freqUpButton.interactable = interactable;
        
        if (genericBtn1 != null) genericBtn1.interactable = interactable;
        if (genericBtn2 != null) genericBtn2.interactable = interactable;
        if (genericBtn3 != null) genericBtn3.interactable = interactable;
        if (genericBtn4 != null) genericBtn4.interactable = interactable;
        if (genericBtn5 != null) genericBtn5.interactable = interactable;
        if (genericBtn6 != null) genericBtn6.interactable = interactable;
    }

    private void UpdateDataMode()
    {
        if (dataModeLabel != null && NodeRedClient.Instance != null)
        {
            dataModeLabel.text = NodeRedClient.Instance.useSimulation ? "SIMULATION" : "LIVE DATA";
        }
    }

    private void ResolveReferences()
    {
        if (vfd   == null) vfd   = FindAnyObjectByType<VFDController>(FindObjectsInactive.Include);
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        if (motor == null) motor = FindAnyObjectByType<MotorController>(FindObjectsInactive.Include);
        if (tower == null) tower = FindAnyObjectByType<SignalTower>(FindObjectsInactive.Include);
        if (eStop == null) eStop = FindAnyObjectByType<EStopButton>(FindObjectsInactive.Include);
    }

    private void Configure()
    {
        if (configured) return;
        configured = true;
        var type = ResolveType();
        ApplyMetaDefaults(type);
        ConfigureControls(type);
        ConfigureFooter(type);
        ConfigureTitles(type);
        ConfigureTelemetryLabels(type);
        ConfigureIO(type);
        ConfigureExtraButtons();
    }

    private void ConfigureExtraButtons()
    {
        if (wiringGuideButton != null)
        {
            wiringGuideButton.onClick.RemoveAllListeners();
            wiringGuideButton.onClick.AddListener(() => {
                var guide = FindAnyObjectByType<WiringGuideUI>(FindObjectsInactive.Include);
                if (guide != null) {
                    guide.gameObject.SetActive(!guide.gameObject.activeSelf);
                }
            });
        }
        if (dataModeButton != null)
        {
            dataModeButton.onClick.RemoveAllListeners();
            dataModeButton.onClick.AddListener(() => {
                if (NodeRedClient.Instance != null) {
                    NodeRedClient.Instance.ToggleSimulation();
                }
            });
        }
    }

    private DashboardType ResolveType()
    {
        if (dashboardType != DashboardType.Auto) return dashboardType;
        var parent = transform.parent != null ? transform.parent.name : string.Empty;
        if (parent.Contains("PLC"))    return DashboardType.PLC;
        if (parent.Contains("HMI"))    return DashboardType.HMI;
        if (parent.Contains("Motor"))  return DashboardType.Motor;
        if (parent.Contains("Signal")) return DashboardType.SignalTower;
        if (parent.Contains("Estop") || parent.Contains("EStop")) return DashboardType.EStop;
        return DashboardType.PM2200;
    }

    // ── Meta ──────────────────────────────────────────────
    private void ApplyMetaDefaults(DashboardType type)
    {
        switch (type)
        {
            case DashboardType.PM2200:      SetMeta("01", "MODBUS RTU",  "3~ 400V AC", "10 Hz"); break;
            case DashboardType.Motor:       SetMeta("02", "VFD DRIVE",   "3~ 400V AC", "20 Hz"); break;
            case DashboardType.PLC:         SetMeta("03", "PROFINET",    "24V DC",     "OB1 Cyclic"); break;
            case DashboardType.HMI:         SetMeta("04", "PROFINET",    "24V DC",     "1 s"); break;
            case DashboardType.SignalTower: SetMeta("05", "DIGITAL I/O", "24V DC",     "Instant"); break;
            case DashboardType.EStop:       SetMeta("06", "HARDWIRED",   "Safety 24V", "Instant"); break;
        }
    }

    // ── Titles ────────────────────────────────────────────
    private void ConfigureTitles(DashboardType type)
    {
        if (pillText == null || titleText == null) return;
        switch (type)
        {
            case DashboardType.PM2200:      pillText.text = "PWR ANALYZER";  titleText.text = "SCHNEIDER PM2200"; break;
            case DashboardType.Motor:       pillText.text = "DRIVE TRAIN";   titleText.text = "ABB 3~ ASYNC MOTOR"; break;
            case DashboardType.PLC:         pillText.text = "LOGIC CTRL";    titleText.text = "SIEMENS SIMATIC S7-1200"; break;
            case DashboardType.HMI:         pillText.text = "HMI PANEL";     titleText.text = "SIEMENS SIMATIC KTP700"; break;
            case DashboardType.SignalTower: pillText.text = "VISUAL FB";     titleText.text = "WERMA KOMPAKT 37"; break;
            case DashboardType.EStop:       pillText.text = "SAFETY RELAY";  titleText.text = "PILZ PNOZ s4"; break;
        }

        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(pillText);
            UnityEditor.EditorUtility.SetDirty(titleText);
        }
        #endif
    }

    private void ConfigureFooter(DashboardType type)
    {
        if (footerLeftText  != null) footerLeftText.text  = "IndustrialAR  v2.0  2025";
        if (footerRightText != null) footerRightText.text = "LIVE DATA STREAM";
    }

    // ── Controls ──────────────────────────────────────────
    private void ConfigureControls(DashboardType type)
    {
        if (powerControlSection     != null) powerControlSection.SetActive(true);
        if (frequencyControlSection != null) frequencyControlSection.SetActive(true);

        switch (type)
        {
            case DashboardType.Motor:
                SetPowerControl("MOTOR CONTROL", "START", "STOP",
                    bench != null ? bench.VFDPowerOn : null,
                    bench != null ? bench.VFDPowerOff : null);
                SetFrequencyControl("SPEED CONTROL", "- 5 Hz", "+ 5 Hz",
                    bench != null ? bench.VFDFrequencyDown : null,
                    bench != null ? bench.VFDFrequencyUp   : null);
                break;
            case DashboardType.PLC:
                SetPowerControl("CPU CONTROL", "RUN", "STOP",
                    bench != null ? bench.PLCSetRun : null,
                    bench != null ? bench.PLCSetStop : null);
                SetFrequencyControl("MANUAL SETPOINT", "- 5 Hz", "+ 5 Hz",
                    bench != null ? bench.VFDFrequencyDown : null,
                    bench != null ? bench.VFDFrequencyUp   : null);
                break;
            case DashboardType.HMI:
                SetPowerControl("SYSTEM COMMANDS", "START", "STOP",
                    bench != null ? bench.VFDPowerOn : null,
                    bench != null ? bench.VFDPowerOff : null);
                SetFrequencyControl("SETPOINT ADJ", "- 5 Hz", "+ 5 Hz",
                    bench != null ? bench.VFDFrequencyDown : null,
                    bench != null ? bench.VFDFrequencyUp   : null);
                break;
            case DashboardType.PM2200:
                if (powerControlSection != null) powerControlSection.SetActive(false);
                if (frequencyControlSection != null) frequencyControlSection.SetActive(false);
                break;
            case DashboardType.EStop:
                SetPowerControl("SAFETY CONTROL", "PRESS", "RELEASE",
                    bench != null ? bench.EStopPress : null,
                    bench != null ? bench.EStopRelease : null);
                if (frequencyControlSection != null) frequencyControlSection.SetActive(false);
                break;
            case DashboardType.SignalTower:
                if (powerControlSection     != null) powerControlSection.SetActive(false);
                if (frequencyControlSection != null) frequencyControlSection.SetActive(false);
                break;
        }
    }

    // ── Telemetry Labels ──────────────────────────────────
    private void ConfigureTelemetryLabels(DashboardType type)
    {
        switch (type)
        {
            case DashboardType.PM2200:
                SetMetricLabels(0, "V  L1-L2-L3");
                SetMetricLabels(1, "I  CT1-CT2-CT3");
                SetMetricLabels(2, "P  ACTIVE TOTAL");
                SetCards("ENERGY (kWh)", "FREQUENCY");
                break;
            case DashboardType.Motor:
                SetMetricLabels(0, "SPEED");
                SetMetricLabels(1, "LOAD / CURRENT");
                SetMetricLabels(2, "TORQUE");
                SetCards("WINDING TEMP", "STATUS");
                break;
            case DashboardType.PLC:
                SetMetricLabels(0, "DI  START CMD");
                SetMetricLabels(1, "DQ  RUN OUTPUT");
                SetMetricLabels(2, "DI  FAULT FB");
                SetCards("AQ0 FREQ REF", "SCAN CYCLE");
                break;
            case DashboardType.HMI:
                SetMetricLabels(0, "SP  VOLTAGE");
                SetMetricLabels(1, "SP  CURRENT");
                SetMetricLabels(2, "SP  POWER");
                SetCards("SETPOINT Hz", "ACTUAL Hz");
                break;
            case DashboardType.SignalTower:
                SetMetricLabels(0, "DQ1  CYAN");
                SetMetricLabels(1, "DQ2  AMBER");
                SetMetricLabels(2, "DQ3  RED");
                SetCards("LAMP MODE", "BUZZER DQ4");
                break;
            case DashboardType.EStop:
                SetMetricLabels(0, "NC1  SAFETY LOOP");
                SetMetricLabels(1, "NC2  VFD STO");
                SetMetricLabels(2, "AUX  INTERLOCK");
                SetCards("CIRCUIT", "RESET REQ");
                break;
        }
    }

    // ── I/O Mapping ───────────────────────────────────────
    private void ConfigureIO(DashboardType type)
    {
        if (ioInputsText == null || ioOutputsText == null) return;

        switch (type)
        {
            case DashboardType.PM2200:
                ioInputsText.text =
                    "- V1 V2 V3  Voltage probes L1-L2-L3\n" +
                    "- CT1 CT2 CT3  Current transformers 5A\n" +
                    "- RS-485 A(+) B(-)  Modbus RTU\n" +
                    "- 24V DC  Aux supply";
                ioOutputsText.text =
                    "- Reg 3000  V_avg (V)\n" +
                    "- Reg 3002  I_avg (A)\n" +
                    "- Reg 3054  P_total (kW)\n" +
                    "- Reg 3060  PF\n" +
                    "- Reg 3110  Energy (kWh)\n" +
                    "- Reg 3010  Frequency (Hz)";
                break;

            case DashboardType.Motor:
                ioInputsText.text =
                    "- U V W  3-phase from VFD output\n" +
                    "- PE  Protective earth\n" +
                    "- Rated: 400V / 3.2A / 1.5kW\n" +
                    "- Poles: 4  Slip: 4%";
                ioOutputsText.text =
                    "- Shaft  1430 RPM nominal\n" +
                    "- PTC / PT100  Winding temp sensor\n" +
                    "- Vibration pickup (optional)\n" +
                    "- Bearing temp sensor (optional)";
                break;

            case DashboardType.PLC:
                ioInputsText.text =
                    "- DI0  Start pushbutton (NO)\n" +
                    "- DI1  Stop pushbutton (NC)\n" +
                    "- DI2  E-Stop feedback (NC)\n" +
                    "- DI3  VFD Run feedback\n" +
                    "- AI0  Motor temp 4-20mA\n" +
                    "- AI1  VFD freq FB 0-10V";
                ioOutputsText.text =
                    "- DQ0  VFD Run command\n" +
                    "- DQ1  Signal tower CYAN\n" +
                    "- DQ2  Signal tower AMBER\n" +
                    "- DQ3  Signal tower RED\n" +
                    "- DQ4  Buzzer\n" +
                    "- AQ0  VFD freq ref 0-10V";
                break;

            case DashboardType.HMI:
                ioInputsText.text =
                    "- PROFINET  From S7-1200 PLC\n" +
                    "- DB10  System status word\n" +
                    "- DB11  Actual frequency\n" +
                    "- DB12  Motor telemetry";
                ioOutputsText.text =
                    "- DB20  Start / Stop command\n" +
                    "- DB21  Frequency setpoint\n" +
                    "- DB22  Mode AUTO / MAN\n" +
                    "- DB23  Alarm acknowledge";
                break;

            case DashboardType.SignalTower:
                ioInputsText.text =
                    "- Terminal 1  Cyan   (+24V from DQ1)\n" +
                    "- Terminal 2  Amber  (+24V from DQ2)\n" +
                    "- Terminal 3  Red    (+24V from DQ3)\n" +
                    "- Terminal 4  Buzzer (+24V from DQ4)\n" +
                    "- Terminal 5  Common (0V)";
                ioOutputsText.text =
                    "- Cyan LED    System running\n" +
                    "- Amber LED   Warning / standby\n" +
                    "- Red LED     Fault / E-Stop\n" +
                    "- Piezo       Audible alarm";
                break;

            case DashboardType.EStop:
                ioInputsText.text =
                    "- Manual push  Mushroom head\n" +
                    "- Twist-to-release mechanism\n" +
                    "- IP65 rated enclosure";
                ioOutputsText.text =
                    "- NC1 -> PLC DI2  Safety loop\n" +
                    "- NC2 -> VFD STO  Safe Torque Off\n" +
                    "- AUX NO -> Indicator lamp\n" +
                    "- EN ISO 13850 compliant";
                break;
        }
    }

    // ── Runtime Updates ───────────────────────────────────
    private void UpdateDisplay()
    {
        var type = ResolveType();
        switch (type)
        {
            case DashboardType.PM2200:      UpdatePM2200();      break;
            case DashboardType.Motor:       UpdateMotor();       break;
            case DashboardType.PLC:         UpdatePLC();         break;
            case DashboardType.HMI:         UpdateHMI();         break;
            case DashboardType.SignalTower: UpdateSignalTower(); break;
            case DashboardType.EStop:       UpdateEStop();       break;
        }
    }

    private void UpdatePM2200()
    {
        if (bench == null && vfd == null) return;
        float voltage = bench != null ? bench.voltage : vfd.voltage;
        float current = bench != null ? bench.current : vfd.current;
        float power = bench != null ? bench.power : vfd.power;
        float frequency = bench != null ? bench.frequency : vfd.frequency;
        bool powered = bench != null ? bench.vfdPowered : vfd.isPowered;
        energy += power * Time.deltaTime / 3.6f;
        SetMetric(0, voltage, "V",  voltage / 400f,            "F2");
        SetMetric(1, current, "A",  current / 3.2f,            "F4");
        SetMetric(2, power,   "kW", Mathf.Clamp01(power / 2f), "F3");
        SetCardValues(card1Value, card1Unit, energy,        "Wh", "F1");
        SetCardValues(card2Value, card2Unit, frequency, "Hz", "F1");
        UpdateStatus(powered && frequency > 1f ? "METERING" : "IDLE",
            powered && frequency > 1f ? StatusRunning : StatusIdle);
        if (freqDisplay != null) freqDisplay.text = frequency.ToString("F1");
    }

    private void UpdateMotor()
    {
        if (bench == null && vfd == null) return;
        float frequency = bench != null ? bench.frequency : vfd.frequency;
        float current = bench != null ? bench.current : vfd.current;
        bool powered = bench != null ? bench.vfdPowered : vfd.isPowered;
        var rpm = bench != null && bench.motorRPM > 0f ? bench.motorRPM : (60f * frequency / 2f) * 0.96f;
        var load = Mathf.Clamp01(current / 3.2f);
        var torque = bench != null ? bench.torque : current * 3f;
        var temp = bench != null ? bench.motorTemp : 30f + 50f * load;
        SetMetric(0, rpm,       "RPM", rpm / 1500f,                    "F0");
        SetMetric(1, frequency, "Hz",  Mathf.Clamp01(frequency / 60f), "F1");
        SetMetric(2, torque,    "Nm",  Mathf.Clamp01(torque / 15f),    "F1");
        SetCardValues(card1Value, card1Unit, temp, "°C", "F1");
        SetCardText(card2Value, card2Unit, powered ? "RUN" : "STOP", "");
        UpdateStatus(powered && frequency > 1f ? "RUNNING" : "IDLE",
            powered && frequency > 1f ? StatusRunning : StatusIdle);
        if (freqDisplay != null) freqDisplay.text = frequency.ToString("F1");
    }

    private void UpdatePLC()
    {
        if (bench == null && vfd == null) return;
        float targetFrequency = bench != null ? bench.targetFrequency : (vfd != null ? vfd.frequency : 0f);
        float actualFrequency = bench != null ? bench.frequency : vfd.frequency;
        bool powered = bench != null ? bench.vfdPowered : vfd.isPowered;
        var startCmd = targetFrequency > 0.1f;
        var runFb    = powered && actualFrequency > 1f;
        var fault    = startCmd && !runFb;
        SetMetricBinary(0, startCmd);
        SetMetricBinary(1, runFb);
        SetMetricBinary(2, fault);
        SetCardValues(card1Value, card1Unit, targetFrequency, "Hz", "F1");
        string cycle = runFb ? "12 ms" : "-- ms";
        SetCardText(card2Value, card2Unit, cycle, "");
        UpdateStatus(startCmd ? "AUTO RUN" : "STANDBY", startCmd ? StatusRunning : StatusIdle);
        if (freqDisplay != null) freqDisplay.text = targetFrequency.ToString("F1");
    }

    private void UpdateHMI()
    {
        if (bench == null && vfd == null) return;
        float voltage = bench != null ? bench.voltage : vfd.voltage;
        float current = bench != null ? bench.current : vfd.current;
        float power = bench != null ? bench.power : vfd.power;
        float targetFrequency = bench != null ? bench.targetFrequency : (vfd != null ? vfd.frequency : 0f);
        float actualFrequency = bench != null ? bench.frequency : vfd.frequency;
        energy += power * Time.deltaTime / 3.6f;
        SetMetric(0, voltage, "V",  voltage / 400f,            "F2");
        SetMetric(1, current, "A",  current / 3.2f,            "F3");
        SetMetric(2, power,   "kW", Mathf.Clamp01(power / 2f), "F3");
        SetCardValues(card1Value, card1Unit, targetFrequency, "Hz", "F1");
        SetCardValues(card2Value, card2Unit, actualFrequency, "Hz", "F1");
        UpdateStatus(targetFrequency > 0.1f ? "ACTIVE" : "IDLE",
            targetFrequency > 0.1f ? StatusRunning : StatusIdle);
        if (freqDisplay != null) freqDisplay.text = targetFrequency.ToString("F1");
    }

    private void UpdateSignalTower()
    {
        var state = bench != null
            ? bench.towerState.ToLowerInvariant()
            : tower != null ? tower.CurrentState : "idle";
        SetMetricBinary(0, state == "running");
        SetMetricBinary(1, state == "warning");
        SetMetricBinary(2, state == "fault" || state == "estop");
        SetCardText(card1Value, card1Unit, state.ToUpperInvariant(), "");
        SetCardText(card2Value, card2Unit,
            (state == "fault" || state == "estop") ? "ON" : "OFF", "");
        var c = (state == "fault" || state == "estop") ? StatusFault
              : state == "running" ? StatusRunning : StatusIdle;
        UpdateStatus(state.ToUpperInvariant(), c);
    }

    private void UpdateEStop()
    {
        var pressed = bench != null ? bench.eStopPressed : eStop != null && eStop.IsPressed;
        var powered = bench != null ? bench.vfdPowered : vfd != null && vfd.isPowered;
        SetMetricBinary(0, pressed);
        SetMetricBinary(1, !pressed && powered);
        SetMetricBinary(2, !pressed && powered);
        SetCardText(card1Value, card1Unit, pressed ? "OPEN" : "CLOSED", "");
        SetCardText(card2Value, card2Unit, pressed ? "YES"  : "NO",     "");
        UpdateStatus(pressed ? "TRIPPED" : "READY", pressed ? StatusFault : StatusRunning);
    }

    // ── Wiring Status ─────────────────────────────────────
    private void UpdateWiringStatus()
    {
        if (wiringStatusText == null) return;
        var type = ResolveType();
        bool ok = CheckWiring(type);
        wiringStatusText.text  = ok ? "ALL CONNECTED" : "WIRING INCOMPLETE";
        wiringStatusText.color = ok ? WiredOk : WiredMissing;
        if (wiringStatusDot != null)
            wiringStatusDot.color = ok ? WiredOk : WiredMissing;
    }

    private bool CheckWiring(DashboardType type)
    {
        switch (type)
        {
            case DashboardType.PM2200:      return vfd != null;
            case DashboardType.Motor:       return vfd != null && motor != null;
            case DashboardType.PLC:         return vfd != null && bench != null && tower != null && eStop != null;
            case DashboardType.HMI:         return bench != null && vfd != null;
            case DashboardType.SignalTower: return tower != null;
            case DashboardType.EStop:       return eStop != null && vfd != null && tower != null;
            default: return true;
        }
    }

    // ── Helpers ───────────────────────────────────────────
    private void SetMeta(string node, string protocol, string phase, string refresh)
    {
        if (metaNodeValue    != null) metaNodeValue.text    = node;
        if (metaProtocolValue!= null) metaProtocolValue.text= protocol;
        if (metaPhaseValue   != null) metaPhaseValue.text   = phase;
        if (metaRefreshValue != null) metaRefreshValue.text = refresh;
    }

    private void SetMetricLabels(int i, string label)
    {
        if (metricLabels != null && i < metricLabels.Length && metricLabels[i] != null)
            metricLabels[i].text = label;
    }

    private void SetCards(string l1, string l2)
    {
        if (card1Label != null) card1Label.text = l1;
        if (card2Label != null) card2Label.text = l2;
    }

    private void SetMetric(int i, float value, string unit, float fill, string fmt)
    {
        if (metricValues != null && i < metricValues.Length && metricValues[i] != null)
            metricValues[i].text = value.ToString(fmt);
        if (metricUnits  != null && i < metricUnits.Length  && metricUnits[i]  != null)
            metricUnits[i].text = unit;
        if (metricBars   != null && i < metricBars.Length   && metricBars[i]   != null)
            metricBars[i].fillAmount = Mathf.Clamp01(fill);
    }

    private void SetMetricBinary(int i, bool on)
    {
        if (metricValues != null && i < metricValues.Length && metricValues[i] != null)
            metricValues[i].text = on ? "ON" : "OFF";
        if (metricUnits  != null && i < metricUnits.Length  && metricUnits[i]  != null)
            metricUnits[i].text = "";
        if (metricBars   != null && i < metricBars.Length   && metricBars[i]   != null)
            metricBars[i].fillAmount = on ? 1f : 0f;
    }

    private void SetCardValues(TextMeshProUGUI v, TextMeshProUGUI u, float val, string unit, string fmt)
    {
        if (v != null) v.text = val.ToString(fmt);
        if (u != null) u.text = unit;
    }

    private void SetCardText(TextMeshProUGUI v, TextMeshProUGUI u, string val, string unit)
    {
        if (v != null) v.text = val;
        if (u != null) u.text = unit;
    }

    private void SetPowerControl(string title, string onLbl, string offLbl,
        UnityEngine.Events.UnityAction onAct, UnityEngine.Events.UnityAction offAct)
    {
        if (powerControlTitle != null) powerControlTitle.text = title;
        if (powerOnLabel  != null) powerOnLabel.text  = onLbl;
        if (powerOffLabel != null) powerOffLabel.text = offLbl;
        if (powerOnButton != null)  { powerOnButton.onClick.RemoveAllListeners();  if (onAct  != null) powerOnButton.onClick.AddListener(onAct);  }
        if (powerOffButton != null) { powerOffButton.onClick.RemoveAllListeners(); if (offAct != null) powerOffButton.onClick.AddListener(offAct); }
    }

    private void SetFrequencyControl(string title, string downLbl, string upLbl,
        UnityEngine.Events.UnityAction downAct, UnityEngine.Events.UnityAction upAct)
    {
        if (frequencyControlTitle != null) frequencyControlTitle.text = title;
        if (freqDownLabel != null) freqDownLabel.text = downLbl;
        if (freqUpLabel   != null) freqUpLabel.text   = upLbl;
        if (freqDownButton != null) { freqDownButton.onClick.RemoveAllListeners(); if (downAct != null) freqDownButton.onClick.AddListener(downAct); }
        if (freqUpButton   != null) { freqUpButton.onClick.RemoveAllListeners();   if (upAct   != null) freqUpButton.onClick.AddListener(upAct);   }
    }

    private void UpdateStatus(string text, Color color)
    {
        if (statusText != null) { statusText.text = text; statusText.color = color; }
        if (statusDot  != null) statusDot.color  = color;
        if (footerDot  != null) footerDot.color  = color;
    }
}
