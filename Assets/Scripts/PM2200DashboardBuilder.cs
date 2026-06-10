using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuforia;
using Image = UnityEngine.UI.Image;

[ExecuteAlways]
public class PM2200DashboardBuilder : MonoBehaviour
{
    [Header("References")]
    public VFDController vfd;
    public BenchSystem bench;

    [Header("Behavior")]
    public ComponentDashboardUI.DashboardType dashboardType = ComponentDashboardUI.DashboardType.PM2200;
    public bool hideExistingChildren = true;
    public bool removeExistingChildren = true;
    public bool disableExistingPowerMeter = true;

    private const float CanvasWidth = 480f; // More compact
    private const float CanvasHeight = 750f;
    private const float HeaderHeight = 40f;
    private const float MetaHeight = 18f;
    private const float SectionTitleHeight = 22f;
    private const float MetricHeight = 48f; 
    private const float MetricBarHeight = 8f; // Increased for health bar look
    private const float CardHeight = 50f; 
    private const float ControlRowHeight = 32f;
    private const float IORowHeight = 64f; 
    private const float WiringRowHeight = 12f; 
    private const float DividerHeight = 1f;
    private const float FooterHeight = 18f; 
    private const float Spacing = 2f; 
    private const int Padding = 10; 
    private const string RootName = "PM2200_Dashboard";

    private static readonly Color PanelColor = new Color32(30, 30, 30, 240);
    private static readonly Color AccentColor = new Color32(205, 214, 214, 255);
    private static readonly Color AccentPower = new Color32(126, 166, 170, 255);
    private static readonly Color TextPrimary = new Color32(236, 240, 240, 255);
    private static readonly Color TextMuted = new Color32(170, 180, 180, 255);
    private static readonly Color TextDim = new Color32(112, 120, 120, 255);
    private static readonly Color DividerColor = new Color32(190, 198, 198, 52);
    
    private static readonly Color ButtonOnGhost = new Color(34f/255f, 197f/255f, 94f/255f, 0.2f);
    private static readonly Color ButtonOnOutline = new Color(34f/255f, 197f/255f, 94f/255f, 0.65f);
    private static readonly Color ButtonOnText = new Color(0.72f, 1f, 0.82f, 1f);
    
    private static readonly Color ButtonOffGhost = new Color(1f, 0.2f, 0.3f, 0.15f); // Cyber Red Ghost
    private static readonly Color ButtonOffOutline = new Color(1f, 0.2f, 0.3f, 0.6f);
    private static readonly Color ButtonOffText = new Color(1f, 0.2f, 0.3f, 1f);
    
    private static readonly Color ButtonGhost = new Color(0.18f, 0.19f, 0.20f, 0.92f);
    private static readonly Color ButtonOutline = new Color(0.62f, 0.66f, 0.68f, 0.42f);
    private static readonly Color PM2200ButtonGrey = new Color32(34, 34, 34, 235);
    private static readonly Color PM2200ButtonText = new Color32(238, 238, 238, 255);

    private static readonly Color DisplayColor = new Color32(24, 52, 56, 210);
    private static readonly Color BarBackColor = new Color32(18, 18, 18, 180);
    private static readonly Color PillBackColor = new Color32(56, 56, 56, 185);
    private static readonly Color AlertBannerColor = new Color(0.8f, 0f, 0f, 0.9f); // Industrial Red

    private bool built;

    private struct MetricRow
    {
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
        public TextMeshProUGUI unit;
        public Image bar;
    }

    private struct CardRow
    {
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
        public TextMeshProUGUI unit;
    }

    private void OnEnable()
    {
        EnsureBuilt();
    }

    private bool isDirty;

    private void OnValidate()
    {
        isDirty = true;
    }

    private void Update()
    {
        if (isDirty)
        {
            isDirty = false;
            ForceRebuild();
        }
    }

    private void Start()
    {
        EnsureBuilt();
    }

    [ContextMenu("Force Rebuild Dashboard")]
    public void ForceRebuild()
    {
        built = false;
        Build(true);
    }

    private void EnsureBuilt()
    {
        if (built) return;
        Build();
    }

    public void Build(bool force = false)
    {
        if (built && !force) return;
        built = true;

        var canvas = GetComponent<Canvas>();
        var rectTransform = GetComponent<RectTransform>();
        if (canvas == null || rectTransform == null) return;

        ApplyCanvasPlacement(rectTransform);

        if (disableExistingPowerMeter)
        {
            var oldDisplay = GetComponentInParent<PowerMeterDisplay>();
            if (oldDisplay != null) oldDisplay.enabled = false;
        }

        if (vfd == null)
        {
            var oldDisplay = GetComponentInParent<PowerMeterDisplay>();
            if (oldDisplay != null) vfd = oldDisplay.vfd;
            if (vfd == null) vfd = FindAnyObjectByType<VFDController>(FindObjectsInactive.Include);
        }

        var font = FindFontAsset();

        var existingRoot = rectTransform.Find(RootName);
        if (existingRoot != null)
        {
            if (force || NeedsRebuild(existingRoot))
            {
                if (Application.isPlaying)
                    Destroy(existingRoot.gameObject);
                else
                    DestroyImmediate(existingRoot.gameObject);
            }
            else
            {
                existingRoot.gameObject.SetActive(true);
                RemoveOrHideChildren(rectTransform, existingRoot);
                return;
            }
        }

        RemoveOrHideChildren(rectTransform, null);

        var root = CreateUIObject(RootName, rectTransform).GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // ── Wire references to ComponentDashboardUI ──
        var componentUI = GetComponent<ComponentDashboardUI>();
        if (componentUI == null) componentUI = gameObject.AddComponent<ComponentDashboardUI>();
        componentUI.vfd = vfd;
        componentUI.bench = bench;

        // Ensure everything stays within bounds
        root.gameObject.AddComponent<RectMask2D>();

        var fitter = EnsureComponent<ContentSizeFitter>(root.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var verticalLayout = EnsureComponent<VerticalLayoutGroup>(root.gameObject);
        verticalLayout.padding = new RectOffset(Padding, Padding, Padding, Padding + 40);
        verticalLayout.spacing = Spacing;
        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        var background = CreateUIObject("Background", root);
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImage = background.AddComponent<Image>();
        bgImage.color = PanelColor;
        DashboardUIFactory.ApplyRoundedCorners(bgImage);
        bgImage.raycastTarget = false;

        var bgLayout = background.AddComponent<LayoutElement>();
        bgLayout.ignoreLayout = true;

        // ── Emergency Stop Alert Banner (New) ──
        var alertBanner = CreateRow("AlertBanner", root, 30f);
        var alertBg = alertBanner.AddComponent<Image>();
        alertBg.color = AlertBannerColor;
        DashboardUIFactory.ApplyRoundedCorners(alertBg);
        var alertText = CreateText("AlertText", alertBanner.transform, "EMERGENCY STOP ACTIVE", font, 12f, TextAlignmentOptions.Center, Color.white);
        alertText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        alertBanner.SetActive(false); // Hidden by default

        var header = CreateRow("Header", root, HeaderHeight);
        var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.spacing = 10f;
        headerLayout.childControlWidth = true; // Changed to true for better layout management
        headerLayout.childForceExpandWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandHeight = true;

        string pillLabel = "SYSTEM";
        string titleLabel = "INDUSTRIAL DEVICE";
        if (transform.parent != null)
        {
            titleLabel = transform.parent.name.ToUpper().Replace("IMAGETARGET", "").Trim('_').Trim();
        }

        switch (dashboardType)
        {
            case ComponentDashboardUI.DashboardType.PM2200: pillLabel = "PWR ANALYZER"; if (string.IsNullOrEmpty(titleLabel) || titleLabel == "INDUSTRIAL DEVICE") titleLabel = "SCHNEIDER PM2200"; break;
            case ComponentDashboardUI.DashboardType.Motor: pillLabel = "DRIVE TRAIN"; if (string.IsNullOrEmpty(titleLabel) || titleLabel == "INDUSTRIAL DEVICE") titleLabel = "ABB 3~ ASYNC MOTOR"; break;
            case ComponentDashboardUI.DashboardType.PLC: pillLabel = "LOGIC CTRL"; if (string.IsNullOrEmpty(titleLabel) || titleLabel == "INDUSTRIAL DEVICE") titleLabel = "SIEMENS SIMATIC S7-1200"; break;
            case ComponentDashboardUI.DashboardType.HMI: pillLabel = "HMI PANEL"; if (string.IsNullOrEmpty(titleLabel) || titleLabel == "INDUSTRIAL DEVICE") titleLabel = "SIEMENS SIMATIC KTP700"; break;
            case ComponentDashboardUI.DashboardType.SignalTower: pillLabel = "VISUAL FB"; if (string.IsNullOrEmpty(titleLabel) || titleLabel == "INDUSTRIAL DEVICE") titleLabel = "WERMA KOMPAKT 37"; break;
            case ComponentDashboardUI.DashboardType.EStop: pillLabel = "SAFETY RELAY"; if (string.IsNullOrEmpty(titleLabel) || titleLabel == "INDUSTRIAL DEVICE") titleLabel = "PILZ PNOZ s4"; break;
        }

        var pillText = CreatePill("Pill", header.transform, font, pillLabel);

        var titleText = CreateText("Title", header.transform, titleLabel, font, 18f, TextAlignmentOptions.Left, TextPrimary);
        var titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        var btnWiring = CreateButton("Btn_Wiring", header.transform, "WIRING", font, ButtonGhost, ButtonOutline, TextMuted, 60f, 24f, out TextMeshProUGUI btnWiringLabel);
        btnWiringLabel.fontSize = 9f;

        var statusGroup = CreateStatusGroup("StatusGroup", header.transform, font, out Image statusDot, out TextMeshProUGUI statusText);

        var metaRow = CreateRow("MetaRow", root, MetaHeight);
        var metaLayout = metaRow.AddComponent<HorizontalLayoutGroup>();
        metaLayout.childAlignment = TextAnchor.MiddleLeft;
        metaLayout.spacing = 12f;
        metaLayout.childControlWidth = true;
        metaLayout.childForceExpandWidth = true;
        metaLayout.childControlHeight = true;
        metaLayout.childForceExpandHeight = true;

        string lNode = "NODE ID", lProto = "PROTOCOL", lPhase = "PHASE", lRef = "REFRESH";
        string vNode = "MB-001", vProto = "MODBUS TCP", vPhase = "3P4W", vRef = "10 Hz";

        if (dashboardType == ComponentDashboardUI.DashboardType.PM2200)
        {
            vNode = "ID: " + (bench != null ? bench.modbusAddress : 1);
            vProto = bench != null ? bench.gatewayIP : "192.168.1.150";
            lNode = "SLAVE ID";
            lProto = "GATEWAY IP";
        }
        else if (dashboardType == ComponentDashboardUI.DashboardType.Motor)
        {
            vNode = "VFD-01";
            vProto = "PROFINET";
        }

        var metaNodeValue = CreateMetaItem("Node", metaRow.transform, font, lNode, vNode);
        var metaProtocolValue = CreateMetaItem("Protocol", metaRow.transform, font, lProto, vProto);
        var metaPhaseValue = CreateMetaItem("Phase", metaRow.transform, font, lPhase, vPhase);
        var metaRefreshValue = CreateMetaItem("Refresh", metaRow.transform, font, lRef, vRef);

        var telemetryTitle = CreateSectionTitle("TelemetryTitle", root, "LIVE TELEMETRY", font);

        string m1 = "VOLTAGE", m2 = "CURRENT", m3 = "POWER";
        string c1 = "METRIC A", c2 = "METRIC B";

        switch (dashboardType)
        {
            case ComponentDashboardUI.DashboardType.PM2200:
                m1 = "VOLTAGE L1-L2"; m2 = "CURRENT L1"; m3 = "ACTIVE POWER";
                c1 = "ENERGY COST"; c2 = "CARBON FOOTPRINT";
                break;
            case ComponentDashboardUI.DashboardType.Motor:
                m1 = "MOTOR SPEED"; m2 = "OUTPUT TORQUE"; m3 = "EFFICIENCY";
                c1 = "POWER FACTOR"; c2 = "MTBF STATUS";
                break;
            case ComponentDashboardUI.DashboardType.PLC:
                m1 = "SCAN CYCLE"; m2 = "CPU LOAD"; m3 = "MEMORY USAGE";
                c1 = "CYCLE COUNT"; c2 = "JITTER";
                break;
            case ComponentDashboardUI.DashboardType.HMI:
                m1 = "SESSION TIME"; m2 = "ALARM COUNT"; m3 = "WARNING COUNT";
                c1 = "CONNECTION"; c2 = "ACTIVE SCREEN";
                break;
            case ComponentDashboardUI.DashboardType.SignalTower:
                m1 = "STATE TIMER"; m2 = "CYAN LAMP"; m3 = "AMBER LAMP";
                c1 = "RED LAMP"; c2 = "HISTORY";
                break;
            case ComponentDashboardUI.DashboardType.EStop:
                m1 = "HALT DURATION"; m2 = "PRESS COUNT"; m3 = "SAFETY STATE";
                c1 = "LAST EVENT"; c2 = "CAUSE";
                break;
        }

        var metricVoltage = CreateMetricRow("Metric_Voltage", root, font, AccentColor, m1);
        var metricCurrent = CreateMetricRow("Metric_Current", root, font, AccentColor, m2);
        var metricPower = CreateMetricRow("Metric_Power", root, font, AccentPower, m3);

        var cardsRow = CreateRow("CardsRow", root, CardHeight);
        var cardsLayout = cardsRow.AddComponent<HorizontalLayoutGroup>();
        cardsLayout.childAlignment = TextAnchor.MiddleLeft;
        cardsLayout.spacing = 8f;
        cardsLayout.childControlWidth = true;
        cardsLayout.childForceExpandWidth = true;
        cardsLayout.childControlHeight = true;
        cardsLayout.childForceExpandHeight = true;
        
        var cardsRowLayout = cardsRow.GetComponent<LayoutElement>();
        cardsRowLayout.preferredHeight = CardHeight;
        cardsRowLayout.flexibleHeight = 0f;

        var card1 = CreateCard("Card_One", cardsRow.transform, font, c1);
        var card2 = CreateCard("Card_Two", cardsRow.transform, font, c2);

        // ── Advanced PQ Row (New) ──
        
        var controlContainer = CreateUIObject("ControlContainer", root);
        var containerLayout = controlContainer.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 4f; // Tighter spacing
        containerLayout.childControlWidth = true;
        containerLayout.childForceExpandWidth = true;
        containerLayout.childControlHeight = true; // Changed to true to respect children heights
        containerLayout.childForceExpandHeight = false;

        var controlRow1 = CreateRow("ControlRow1", controlContainer.transform, ControlRowHeight);
        var layout1 = controlRow1.AddComponent<HorizontalLayoutGroup>();
        layout1.childAlignment = TextAnchor.MiddleCenter;
        layout1.spacing = 8f;
        layout1.childControlWidth = true;
        layout1.childForceExpandWidth = false;
        layout1.childControlHeight = true;
        layout1.childForceExpandHeight = true;

        var controlRow2 = CreateRow("ControlRow2", controlContainer.transform, ControlRowHeight);
        var layout2 = controlRow2.AddComponent<HorizontalLayoutGroup>();
        layout2.childAlignment = TextAnchor.MiddleCenter;
        layout2.spacing = 8f;
        layout2.childControlWidth = true;
        layout2.childForceExpandWidth = false;
        layout2.childControlHeight = true;
        layout2.childForceExpandHeight = true;

        Button btn1 = null; TextMeshProUGUI lbl1 = null;
        Button btn2 = null; TextMeshProUGUI lbl2 = null;
        Button btn3 = null; TextMeshProUGUI lbl3 = null;
        Button btn4 = null; TextMeshProUGUI lbl4 = null;
        Button btn5 = null; TextMeshProUGUI lbl5 = null;
        Button btn6 = null; TextMeshProUGUI lbl6 = null;

        switch (dashboardType)
        {
            case ComponentDashboardUI.DashboardType.Motor:
                btn1 = CreateButton("Btn_Start", controlRow1.transform, "VFD START", font, ButtonOnGhost, ButtonOnOutline, ButtonOnText, 100f, 30f, out lbl1);
                btn2 = CreateButton("Btn_Stop", controlRow1.transform, "VFD STOP", font, ButtonOffGhost, ButtonOffOutline, ButtonOffText, 100f, 30f, out lbl2);
                btn3 = CreateButton("Btn_Up", controlRow2.transform, "SPEED +", font, ButtonGhost, ButtonOutline, TextPrimary, 80f, 30f, out lbl3);
                btn4 = CreateButton("Btn_Down", controlRow2.transform, "SPEED -", font, ButtonGhost, ButtonOutline, TextPrimary, 80f, 30f, out lbl4);
                btn5 = CreateButton("Btn_Dir", controlRow2.transform, "FWD/REV", font, ButtonGhost, ButtonOutline, TextPrimary, 80f, 30f, out lbl5);
                break;
            case ComponentDashboardUI.DashboardType.PLC:
                btn1 = CreateButton("Btn_ToggleRUN", controlRow1.transform, "CPU RUN", font, ButtonOnGhost, ButtonOnOutline, ButtonOnText, 100f, 30f, out lbl1);
                btn2 = CreateButton("Btn_ToggleSTOP", controlRow1.transform, "CPU STOP", font, ButtonOffGhost, ButtonOffOutline, ButtonOffText, 100f, 30f, out lbl2);
                btn3 = CreateButton("Btn_Reset", controlRow2.transform, "FAULT RESET", font, ButtonGhost, ButtonOutline, TextPrimary, 120f, 30f, out lbl3);
                btn4 = CreateButton("Btn_Force", controlRow2.transform, "FORCE Q0.4", font, ButtonGhost, ButtonOutline, TextPrimary, 120f, 30f, out lbl4);
                break;
            case ComponentDashboardUI.DashboardType.PM2200:
                btn1 = CreateButton("Btn_ResetEnergy", controlRow1.transform, "CLEAR ENERGY", font, ButtonGhost, ButtonOutline, TextPrimary, 150f, 30f, out lbl1);
                btn2 = CreateButton("Btn_ResetDemand", controlRow1.transform, "CLEAR DEMAND", font, ButtonGhost, ButtonOutline, TextPrimary, 150f, 30f, out lbl2);
                btn3 = CreateButton("Btn_Relay1", controlRow2.transform, "RELAY 1 (K1)", font, ButtonGhost, ButtonOutline, TextPrimary, 120f, 30f, out lbl3);
                btn4 = CreateButton("Btn_Relay2", controlRow2.transform, "RELAY 2 (K2)", font, ButtonGhost, ButtonOutline, TextPrimary, 120f, 30f, out lbl4);
                break;
            case ComponentDashboardUI.DashboardType.EStop:
                btn1 = CreateButton("Btn_ToggleEstop", controlRow1.transform, "SAFETY TRIP", font, ButtonOffGhost, ButtonOffOutline, ButtonOffText, 150f, 30f, out lbl1);
                btn2 = CreateButton("Btn_ResetSafety", controlRow1.transform, "SAFETY RESET", font, ButtonOnGhost, ButtonOnOutline, ButtonOnText, 150f, 30f, out lbl2);
                break;
            case ComponentDashboardUI.DashboardType.HMI:
                btn1 = CreateButton("Btn_Ack", controlRow1.transform, "ACK ALARMS", font, ButtonGhost, ButtonOutline, TextPrimary, 150f, 30f, out lbl1);
                btn2 = CreateButton("Btn_Next", controlRow1.transform, "NEXT PAGE", font, ButtonGhost, ButtonOutline, TextPrimary, 150f, 30f, out lbl2);
                break;
            case ComponentDashboardUI.DashboardType.SignalTower:
                btn1 = CreateButton("Btn_Test", controlRow1.transform, "LAMP TEST", font, ButtonGhost, ButtonOutline, TextPrimary, 150f, 30f, out lbl1);
                btn2 = CreateButton("Btn_Clear", controlRow1.transform, "CLR HISTORY", font, ButtonGhost, ButtonOutline, TextPrimary, 150f, 30f, out lbl2);
                break;
        }

        // Hide second row if not used
        if (controlRow2.transform.childCount == 0) controlRow2.SetActive(false);

        // ── I/O Mapping Section ──
        var ioTitle = CreateSectionTitle("IOTitle", root, "I/O TERMINAL MAPPING", font);

        if (dashboardType == ComponentDashboardUI.DashboardType.PM2200)
        {
            var eventTitle = CreateSectionTitle("EventTitle", root, "MODBUS EVENT LOG", font);
            var eventBox = CreateRow("EventBox", root, 40f); // Reduced from 60f
            var eventText = CreateText("Events", eventBox.transform, "WAITING FOR DATA...", font, 7.5f, TextAlignmentOptions.TopLeft, TextMuted);
            eventText.textWrappingMode = TextWrappingModes.Normal;
            
            var pmDash = GetComponent<PM2200Dashboard>();
            if (pmDash != null) pmDash.textEventLog = eventText;
        }

        var ioRow = CreateRow("IORow", root, IORowHeight);
        var ioRowLayout = ioRow.GetComponent<LayoutElement>();
        ioRowLayout.flexibleHeight = 0f; // Fixed height to prevent overlapping
        
        var ioLayout = ioRow.AddComponent<HorizontalLayoutGroup>();
        ioLayout.childAlignment = TextAnchor.UpperLeft;
        ioLayout.spacing = 15f;
        ioLayout.childControlWidth = true;
        ioLayout.childForceExpandWidth = true;
        ioLayout.childControlHeight = true;
        ioLayout.childForceExpandHeight = true;

        var ioInputCol = CreateIOColumn("IOInputs", ioRow.transform, font, "INPUTS");
        var ioOutputCol = CreateIOColumn("IOOutputs", ioRow.transform, font, "OUTPUTS");

        if (dashboardType == ComponentDashboardUI.DashboardType.PM2200)
        {
            ioInputCol.text = "V1/V2/V3: 480V L-L DIRECT\nI1/I2/I3: CT 5A SECONDARY\nDI1: BREAKER STATUS (MOD)\nDI2: WATER PULSE (EXT)";
            ioOutputCol.text = "K1: SHUNT TRIP RELAY\nK2: ALARM SIGNAL RELAY\nLED: ENERGY PULSE (AMBER)\nCOM: RS-485 MODBUS RTU";
        }
        else if (dashboardType == ComponentDashboardUI.DashboardType.Motor)
        {
            ioInputCol.text = "L1/L2/L3: 400V VFD OUT\nPE: PROTECTIVE EARTH\nPTC: THERMAL SENSOR\nENC: OPTICAL ENCODER";
            ioOutputCol.text = "U/V/W: MOTOR TERMINALS\nBRK: 24V BRAKE (NC)\nFAN: 230V COOLING\nAUX: STATUS FEEDBACK";
        }
        else if (dashboardType == ComponentDashboardUI.DashboardType.PLC)
        {
            ioInputCol.text = "DI 0.0: START PB\nDI 0.1: STOP PB\nDI 0.2: LIMIT SW\nAI 0: 0-10V SENSOR";
            ioOutputCol.text = "DQ 0.0: RUN LAMP\nDQ 0.1: FAULT LAMP\nDQ 0.2: CONTACTOR\nAQ 0: 4-20mA VFD";
        }
        else if (dashboardType == ComponentDashboardUI.DashboardType.EStop)
        {
            ioInputCol.text = "CH1: DUAL SAFETY IN\nCH2: DUAL SAFETY IN\nRES: RESET BUTTON\nEDM: MONITORING";
            ioOutputCol.text = "13/14: SAFETY CONTACT 1\n23/24: SAFETY CONTACT 2\n33/34: AUX CONTACT\nY32: SEMI-COND OUT";
        }

        // ── Wiring Status Row ──
        CreateRow("IOSpacer", root, 12f);
        var wiringRow = CreateRow("WiringRow", root, WiringRowHeight);
        var wiringLayout = wiringRow.AddComponent<HorizontalLayoutGroup>();
        wiringLayout.childAlignment = TextAnchor.MiddleLeft;
        wiringLayout.spacing = 6f;
        wiringLayout.childControlWidth = false;
        wiringLayout.childForceExpandWidth = false;
        wiringLayout.childControlHeight = true;
        wiringLayout.childForceExpandHeight = true;

        var wiringLabel = CreateText("WiringLabel", wiringRow.transform, "WIRING:", font, 9f, TextAlignmentOptions.Left, TextDim);
        var wiringDot = CreateDot("WiringDot", wiringRow.transform, AccentColor);
        var wiringStatusTxt = CreateText("WiringStatus", wiringRow.transform, "CHECKING...", font, 10f, TextAlignmentOptions.Left, AccentColor);

        // ── Footer ──
        var footerSpacerRow = CreateRow("FooterSpacerRow", root, 16f);
        var footer = CreateRow("Footer", root, FooterHeight);
        var footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
        footerLayout.childAlignment = TextAnchor.MiddleLeft;
        footerLayout.spacing = 8f;
        footerLayout.childControlWidth = false;
        footerLayout.childForceExpandWidth = false;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandHeight = true;

        var footerLeft = CreateText("FooterLeft", footer.transform, "IndustrialAR  v2.0  2025", font, 9f, TextAlignmentOptions.Left, TextDim);
        var footerSpacer = CreateUIObject("FooterSpacer", footer.transform);
        var footerSpacerLayout = footerSpacer.AddComponent<LayoutElement>();
        footerSpacerLayout.flexibleWidth = 1f;

        var footerRight = CreateUIObject("FooterRight", footer.transform);
        var footerRightLayout = footerRight.AddComponent<HorizontalLayoutGroup>();
        footerRightLayout.childAlignment = TextAnchor.MiddleRight;
        footerRightLayout.spacing = 6f;
        footerRightLayout.childControlWidth = false;
        footerRightLayout.childForceExpandWidth = false;
        footerRightLayout.childControlHeight = true;
        footerRightLayout.childForceExpandHeight = true;

        var btnDataMode = CreateButton("Btn_DataMode", footerRight.transform, "TOGGLE", font, ButtonGhost, ButtonOutline, TextMuted, 50f, 16f, out TextMeshProUGUI btnDataModeLabel);
        btnDataModeLabel.fontSize = 8f;
        var footerDot = CreateDot("FooterDot", footerRight.transform, AccentColor);
        var footerRightText = CreateText("FooterRightText", footerRight.transform, "SIMULATION", font, 9f, TextAlignmentOptions.Right, TextMuted);

        // ── Wire references to ComponentDashboardUI ──
        componentUI.pillText = pillText;
        componentUI.titleText = titleText;
        componentUI.statusText = statusText;
        componentUI.statusDot = statusDot;
        componentUI.metaNodeValue = metaNodeValue;
        componentUI.metaProtocolValue = metaProtocolValue;
        componentUI.metaPhaseValue = metaPhaseValue;
        componentUI.metaRefreshValue = metaRefreshValue;
        componentUI.metricLabels = new[] { metricVoltage.label, metricCurrent.label, metricPower.label };
        componentUI.metricValues = new[] { metricVoltage.value, metricCurrent.value, metricPower.value };
        componentUI.metricUnits = new[] { metricVoltage.unit, metricCurrent.unit, metricPower.unit };
        componentUI.metricBars = new[] { metricVoltage.bar, metricCurrent.bar, metricPower.bar };
        componentUI.card1Label = card1.label;
        componentUI.card1Value = card1.value;
        componentUI.card1Unit = card1.unit;
        componentUI.card2Label = card2.label;
        componentUI.card2Value = card2.value;
        componentUI.card2Unit = card2.unit;
        componentUI.ioInputsText = ioInputCol;
        componentUI.ioOutputsText = ioOutputCol;
        componentUI.wiringStatusText = wiringStatusTxt;
        componentUI.wiringStatusDot = wiringDot;
        
        // Controls removed for PM2200
        componentUI.powerControlSection = null;
        componentUI.frequencyControlSection = null;

        componentUI.footerLeftText = footerLeft;
        componentUI.footerRightText = footerRightText;
        componentUI.footerDot = footerDot;
        componentUI.wiringGuideButton = btnWiring;
        componentUI.dataModeButton = btnDataMode;
        componentUI.dataModeLabel = footerRightText;

        if (dashboardType == ComponentDashboardUI.DashboardType.PM2200)
        {
            componentUI.pmModbusAddrText = metaNodeValue;
            componentUI.pmGatewayIPText = metaProtocolValue;
        }

        componentUI.genericBtn1 = btn1;
        componentUI.genericBtn2 = btn2;
        componentUI.genericBtn3 = btn3;
        componentUI.genericBtn4 = btn4;
        componentUI.genericBtn5 = btn5;
        componentUI.genericBtn6 = btn6;
        componentUI.genericLbl1 = lbl1;
        componentUI.genericLbl2 = lbl2;
        componentUI.genericLbl3 = lbl3;
        componentUI.genericLbl4 = lbl4;
        componentUI.genericLbl5 = lbl5;
        componentUI.genericLbl6 = lbl6;

        if (dashboardType == ComponentDashboardUI.DashboardType.PM2200)
        {
            DashboardUIFactory.ApplyCyberOverlay(root, DashboardUIFactory.CyberStyle.PowerMeterGrey, true, false);
        }
        else if (dashboardType == ComponentDashboardUI.DashboardType.EStop)
        {
            DashboardUIFactory.ApplyCyberOverlay(root, DashboardUIFactory.CyberStyle.PowerMeterGrey, true, false);
        }
        else
        {
            DashboardUIFactory.ApplyCyberFuturisticSkin(root, DashboardUIFactory.CyberStyle.PowerMeter);
        }

        var legacyDashboard = GetComponent<DashboardUI>();
        if (legacyDashboard != null) legacyDashboard.enabled = false;

        var pmDashboard = GetComponent<PM2200Dashboard>();
        if (pmDashboard != null)
        {
            pmDashboard.textModbusAddr = metaNodeValue;
            pmDashboard.textGatewayIP = metaProtocolValue;
            pmDashboard.alertBanner = alertBanner;
        }
    }

    private TMP_FontAsset FindFontAsset()
    {
        var existing = GetComponentInChildren<TextMeshProUGUI>(true);
        if (existing != null && existing.font != null) return existing.font;
        return TMP_Settings.defaultFontAsset;
    }

    private TextMeshProUGUI CreateIOColumn(string name, Transform parent, TMP_FontAsset font, string header)
    {
        var col = CreateUIObject(name, parent);
        var layout = col.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0); // No padding for minimal size
        layout.spacing = 1f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        var headerText = CreateText("Header", col.transform, header, font, 7f, TextAlignmentOptions.Left, AccentColor);
        headerText.fontStyle = FontStyles.UpperCase | FontStyles.Bold;
        
        var contentText = CreateText("Content", col.transform, "...", font, 6.5f, TextAlignmentOptions.Left, TextMuted);
        contentText.textWrappingMode = TextWrappingModes.Normal;
        contentText.overflowMode = TextOverflowModes.Truncate;
        contentText.lineSpacing = -6f;

        var contentLayout = contentText.gameObject.AddComponent<LayoutElement>();
        contentLayout.preferredHeight = Mathf.Max(40f, IORowHeight - 12f);

        return contentText;
    }

    private GameObject CreateRow(string name, Transform parent, float height)
    {
        var row = CreateUIObject(name, parent);
        var layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;
        return row;
    }

    private GameObject CreateSection(string name, Transform parent)
    {
        var section = CreateUIObject(name, parent);
        var layout = section.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        return section;
    }

    private TextMeshProUGUI CreateSectionTitle(string name, Transform parent, string text, TMP_FontAsset font)
    {
        var titleRow = CreateRow(name, parent, SectionTitleHeight);
        var titleText = CreateText("Text", titleRow.transform, text, font, 10f, TextAlignmentOptions.Left, TextMuted);
        titleText.fontStyle = FontStyles.UpperCase | FontStyles.Bold;
        return titleText;
    }

    private MetricRow CreateMetricRow(string name, Transform parent, TMP_FontAsset font, Color barColor, string label)
    {
        var row = CreateUIObject(name, parent);
        var layout = row.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2f; // Tighter
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        var rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = MetricHeight;
        rowLayout.flexibleHeight = 0f;

        var topRow = CreateUIObject("Top", row.transform);
        var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
        topLayout.childAlignment = TextAnchor.LowerLeft;
        topLayout.spacing = 8f;
        topLayout.childControlWidth = false;
        topLayout.childForceExpandWidth = false;
        topLayout.childControlHeight = true;
        topLayout.childForceExpandHeight = true;

        var labelText = CreateText("Label", topRow.transform, label, font, 11f, TextAlignmentOptions.Left, TextMuted);
        labelText.fontStyle = FontStyles.UpperCase;
        labelText.textWrappingMode = TextWrappingModes.NoWrap; // Metrics should stay one line if possible
        
        var labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        var valueGroup = CreateUIObject("ValueGroup", topRow.transform);
        var valueLayoutGroup = valueGroup.AddComponent<HorizontalLayoutGroup>();
        valueLayoutGroup.childAlignment = TextAnchor.LowerRight;
        valueLayoutGroup.spacing = 4f;
        valueLayoutGroup.childControlWidth = false;
        valueLayoutGroup.childForceExpandWidth = false;
        valueLayoutGroup.childControlHeight = true;
        valueLayoutGroup.childForceExpandHeight = true;

        var valueText = CreateText("Value", valueGroup.transform, "0.0", font, 24f, TextAlignmentOptions.Right, TextPrimary);
        var unitText = CreateText("Unit", valueGroup.transform, "", font, 11f, TextAlignmentOptions.Right, AccentColor);

        var barRow = CreateUIObject("Bar", row.transform);
        var barLayout = barRow.AddComponent<LayoutElement>();
        barLayout.preferredHeight = MetricBarHeight;

        var barBack = barRow.AddComponent<Image>();
        barBack.color = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Dark background for the bar
        DashboardUIFactory.ApplyRoundedCorners(barBack);
        barBack.raycastTarget = false;
        
        // Add a slight outline to the bar container
        var outline = barRow.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.1f);
        outline.effectDistance = new Vector2(1f, -1f);

        var barFill = CreateUIObject("Fill", barRow.transform);
        var barFillRect = barFill.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;

        var barFillImage = barFill.AddComponent<Image>();
        barFillImage.color = barColor;
        barFillImage.sprite = DashboardUIFactory.GetRoundedRectSprite();
        barFillImage.type = Image.Type.Filled;
        barFillImage.fillMethod = Image.FillMethod.Horizontal;
        barFillImage.fillOrigin = 0;
        barFillImage.fillAmount = 0.5f; // Set a default for visibility during build
        barFillImage.raycastTarget = false;

        return new MetricRow
        {
            label = labelText,
            value = valueText,
            unit = unitText,
            bar = barFillImage
        };
    }

    private CardRow CreateCard(string name, Transform parent, TMP_FontAsset font, string label)
    {
        var card = CreateUIObject(name, parent);
        var cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(10, 8, 6, 6);
        cardLayout.spacing = 2f;
        cardLayout.childAlignment = TextAnchor.UpperLeft;
        cardLayout.childControlWidth = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandHeight = false;

        var layoutElem = card.AddComponent<LayoutElement>();
        layoutElem.flexibleWidth = 1f;
        layoutElem.minWidth = 100f; // Allow shrinking but keep a baseline
        layoutElem.preferredHeight = CardHeight;

        var labelText = CreateText("Label", card.transform, label, font, 10f, TextAlignmentOptions.Left, TextMuted);
        labelText.fontStyle = FontStyles.UpperCase;
        labelText.textWrappingMode = TextWrappingModes.Normal;
        labelText.overflowMode = TextOverflowModes.Truncate;
        labelText.lineSpacing = -10f;
        
        var labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 24f;

        var valueRow = CreateUIObject("ValueRow", card.transform);
        var valueLayout = valueRow.AddComponent<HorizontalLayoutGroup>();
        valueLayout.childAlignment = TextAnchor.MiddleLeft;
        valueLayout.spacing = 4f;
        valueLayout.childControlWidth = false;
        valueLayout.childForceExpandWidth = false;
        valueLayout.childControlHeight = true;
        valueLayout.childForceExpandHeight = true;

        var valueText = CreateText("Value", valueRow.transform, "0.0", font, 16f, TextAlignmentOptions.Left, TextPrimary);
        valueText.fontStyle = FontStyles.Bold;
        var unitText = CreateText("Unit", valueRow.transform, "", font, 10f, TextAlignmentOptions.Left, AccentColor);

        return new CardRow
        {
            label = labelText,
            value = valueText,
            unit = unitText
        };
    }

    private TextMeshProUGUI CreatePill(string name, Transform parent, TMP_FontAsset font, string text)
    {
        var pill = CreateUIObject(name, parent);
        var pillLayout = pill.AddComponent<LayoutElement>();
        pillLayout.preferredWidth = 78f;
        pillLayout.preferredHeight = 20f;

        var image = pill.AddComponent<Image>();
        image.color = PillBackColor;
        DashboardUIFactory.ApplyRoundedCorners(image);
        image.raycastTarget = false;

        var outline = pill.AddComponent<Outline>();
        outline.effectColor = AccentColor;
        outline.effectDistance = new Vector2(1f, -1f);

        var pillText = CreateText("Text", pill.transform, text, font, 9f, TextAlignmentOptions.Center, AccentColor);
        pillText.fontStyle = FontStyles.UpperCase;
        return pillText;
    }

    private GameObject CreateStatusGroup(string name, Transform parent, TMP_FontAsset font, out Image dot, out TextMeshProUGUI text)
    {
        var group = CreateUIObject(name, parent);
        var layout = group.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.spacing = 6f;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;

        dot = CreateDot("Dot", group.transform, AccentColor);
        text = CreateText("Text", group.transform, "RUNNING", font, 11f, TextAlignmentOptions.Right, AccentColor);
        
        var groupLayout = group.AddComponent<LayoutElement>();
        groupLayout.preferredWidth = 80f;
        
        return group;
    }

    private TextMeshProUGUI CreateMetaItem(string name, Transform parent, TMP_FontAsset font, string label, string value)
    {
        var item = CreateUIObject(name, parent);
        var layout = item.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;

        var labelText = CreateText("Label", item.transform, label, font, 8f, TextAlignmentOptions.Left, TextDim);
        labelText.fontStyle = FontStyles.UpperCase;
        var valueText = CreateText("Value", item.transform, value, font, 11f, TextAlignmentOptions.Left, AccentColor);
        return valueText;
    }

    private Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, Color ghostColor, Color outlineColor, Color textColor, float width, float height,
        out TextMeshProUGUI labelText)
    {
        bool neutralPM2200 = dashboardType == ComponentDashboardUI.DashboardType.PM2200;
        bool flatEStop = dashboardType == ComponentDashboardUI.DashboardType.EStop;
        if (neutralPM2200)
        {
            ghostColor = PM2200ButtonGrey;
            outlineColor = Color.clear;
            textColor = PM2200ButtonText;
        }
        else if (flatEStop)
        {
            bool tripButton = name.ToLowerInvariant().Contains("estop") || name.ToLowerInvariant().Contains("trip");
            ghostColor = tripButton ? new Color32(96, 0, 0, 235) : new Color32(34, 34, 34, 235);
            outlineColor = Color.clear;
            textColor = Color.white;
        }

        var buttonObject = CreateUIObject(name, parent);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        var image = buttonObject.AddComponent<Image>();
        image.color = ghostColor;
        DashboardUIFactory.ApplyRoundedCorners(image);

        if (outlineColor.a > 0.001f)
        {
            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = neutralPM2200 || flatEStop ? Selectable.Transition.None : Selectable.Transition.ColorTint;
        var motion = buttonObject.AddComponent<DashboardMotionAnimator>();
        motion.motionType = DashboardMotionAnimator.MotionType.Button;
        motion.hoverScale = 1.045f;
        motion.pressScale = 0.955f;

        var colors = button.colors;
        colors.normalColor = neutralPM2200 || flatEStop ? ghostColor : Color.white;
        colors.highlightedColor = neutralPM2200 || flatEStop ? ghostColor : textColor;
        colors.pressedColor = neutralPM2200 || flatEStop ? ghostColor : Color.gray;
        colors.selectedColor = neutralPM2200 || flatEStop ? ghostColor : Color.white;
        colors.disabledColor = new Color(1, 1, 1, 0.4f);
        button.colors = colors;

        var layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        labelText = CreateText("Label", buttonObject.transform, label, font, 14f, TextAlignmentOptions.Center, textColor);
        labelText.fontStyle = FontStyles.UpperCase | FontStyles.Bold;
        labelText.raycastTarget = false;

        return button;
    }

    private TextMeshProUGUI CreateDisplayBox(string name, Transform parent, TMP_FontAsset font, string text)
    {
        var display = CreateUIObject(name, parent);
        var rect = display.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140f, ControlRowHeight);

        var image = display.AddComponent<Image>();
        image.color = DisplayColor;
        DashboardUIFactory.ApplyRoundedCorners(image);
        image.raycastTarget = false;

        var outline = display.AddComponent<Outline>();
        outline.effectColor = AccentColor;
        outline.effectDistance = new Vector2(1f, -1f);

        var layout = display.AddComponent<LayoutElement>();
        layout.preferredWidth = 140f;
        layout.preferredHeight = ControlRowHeight;

        var textObj = CreateText("Value", display.transform, text, font, 16f, TextAlignmentOptions.Center, AccentColor);
        textObj.fontStyle = FontStyles.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textObj.raycastTarget = false;

        return textObj;
    }

    private Image CreateDot(string name, Transform parent, Color color)
    {
        var dot = CreateUIObject(name, parent);
        var rect = dot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(6f, 6f);

        var image = dot.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, TMP_FontAsset font, float size,
        TextAlignmentOptions alignment, Color color)
    {
        var textObject = CreateUIObject(name, parent);
        var tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        var existing = target.GetComponent<T>();
        if (existing != null) return existing;
        return target.AddComponent<T>();
    }

    private void ApplyCanvasPlacement(RectTransform rectTransform)
    {
        // Use world-unit sizing so the canvas maps directly to target meters
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0f); // Pivot at bottom

        float targetWidth = 0f;
        float targetHeight = 0f;
        var imageTarget = GetComponentInParent<ImageTargetBehaviour>();
        if (imageTarget != null)
        {
            var size = imageTarget.GetSize();
            targetWidth = size.x;
            targetHeight = size.y;
        }

        // Desired canvas world width should be a fraction of the target width (so it sits above)
        float desiredFraction = 0.9f; // keep the dashboard slightly narrower than the target
        float desiredWorldWidth = Mathf.Max(0.08f, targetWidth * desiredFraction);
        float aspect = CanvasHeight / CanvasWidth;
        float desiredWorldHeight = desiredWorldWidth * aspect;

        // Set scale to 1 and size directly in world units so it's easier to reason about
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = new Vector2(desiredWorldWidth, desiredWorldHeight);

        // Position directly above target image with proportional gap
        float gap = Mathf.Max(0.02f, targetHeight * 0.06f); // use a small constant floor for very small targets
        float yOffset = (targetHeight * 0.5f) + gap; // Bottom is exactly at gap above target
        rectTransform.localPosition = new Vector3(0f, yOffset, 0f);
    }

    private void RemoveOrHideChildren(RectTransform parent, Transform keep)
    {
        if (removeExistingChildren)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (keep != null && child == keep) continue;
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
            return;
        }

        if (hideExistingChildren)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (keep != null && child == keep) continue;
                child.gameObject.SetActive(false);
            }
        }
    }

    private static bool NeedsRebuild(Transform root)
    {
        return root.Find("Header/Pill") == null || root.Find("TelemetryTitle") == null || root.Find("Metric_Voltage") == null || root.Find("IOTitle") == null;
    }
}
