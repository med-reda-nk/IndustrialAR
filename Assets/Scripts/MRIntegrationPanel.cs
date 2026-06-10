using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[ExecuteAlways]
public class MRIntegrationPanel : MonoBehaviour
{
    private static Sprite roundedSprite;

    [Header("References")]
    public BenchSystem bench;
    public NodeRedClient nodeRed;
    public WiringGuideUI wiringGuide;
    public TMP_FontAsset fontMain;

    [Header("Placement")]
    public Vector3 localOffset = new Vector3(0f, 0.95f, 0f);
    public float canvasScale = 0.001f;
    public bool buildInEditor = true;
    public Vector2 screenMargin = new Vector2(24f, 24f);

    private GameObject canvasRoot;
    private RectTransform toolsFrame;
    private RectTransform nodePanel;
    private RectTransform toolbar;
    private RectTransform logTerminal;
    private RectTransform liveTelemetryPanel;
    private RectTransform telemetryPanel;
    private TMP_InputField ipInput;
    private TMP_InputField portInput;
    private TMP_InputField passwordInput;
    private TextMeshProUGUI sourceText;
    private TextMeshProUGUI urlText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI telemetryText;
    private Image logTerminalStatusDot;
    private TextMeshProUGUI logTerminalStatusText;
    private TextMeshProUGUI logTerminalText;
    private Image liveTelemetryStatusDot;
    private TextMeshProUGUI liveTelemetryStatusText;
    private TextMeshProUGUI nodeButtonLabel;
    private TextMeshProUGUI telemetryButtonLabel;
    private TextMeshProUGUI liveButtonLabel;
    private Button nodeButton;
    private Button telemetryButton;
    private Button liveButton;
    private Button simButton;
    private readonly TextMeshProUGUI[] metricValueTexts = new TextMeshProUGUI[6];
    private readonly TextMeshProUGUI[] metricUnitTexts = new TextMeshProUGUI[6];
    private TelemetryChartGraphic voltageChart;
    private TelemetryChartGraphic currentChart;
    private TelemetryChartGraphic powerChart;
    private TelemetryChartGraphic frequencyChart;
    private readonly TextMeshProUGUI[] graphValueTexts = new TextMeshProUGUI[4];
    private readonly TextMeshProUGUI[] graphScaleTexts = new TextMeshProUGUI[4];
    private bool built;
    private bool nodePanelOpen;
    private bool telemetryPanelOpen;
    private const int TelemetryHistoryCapacity = 90;
    private readonly float[] voltageHistory = new float[TelemetryHistoryCapacity];
    private readonly float[] currentHistory = new float[TelemetryHistoryCapacity];
    private readonly float[] powerHistory = new float[TelemetryHistoryCapacity];
    private readonly float[] frequencyHistory = new float[TelemetryHistoryCapacity];
    private int telemetryHistoryCount;
    private float telemetrySampleTimer;

    private static readonly Color PanelBg = new Color(0.055f, 0.06f, 0.065f, 0.96f);
    private static readonly Color ButtonBg = new Color(0.18f, 0.19f, 0.20f, 0.94f);
    private static readonly Color ButtonSelectedBg = new Color(0.30f, 0.32f, 0.33f, 0.96f);
    private static readonly Color InputBg = new Color(0.09f, 0.10f, 0.105f, 0.98f);
    private static readonly Color Accent = new Color(0.72f, 0.76f, 0.78f, 1f);
    private static readonly Color Warn = new Color(1f, 0.62f, 0.08f, 1f);
    private static readonly Color Ok = new Color(0.2f, 0.95f, 0.55f, 1f);
    private static readonly Color Text = new Color(0.92f, 0.94f, 0.95f, 1f);
    private static readonly Color Muted = new Color(0.62f, 0.66f, 0.68f, 1f);

    private void OnEnable()
    {
        if (Application.isPlaying || buildInEditor) Build(false);
    }

    private void Start()
    {
        Build(false);
    }

    private void Update()
    {
        if (!built && (Application.isPlaying || buildInEditor)) Build(false);
        SampleTelemetryHistory();
        UpdatePanelText();
    }

    public void Build(bool force)
    {
        if (built && !force) return;
        built = true;

        ResolveReferences();
        EnsureEventSystem();

        if (canvasRoot == null)
        {
            var existing = transform.Find("MRIntegration_Canvas");
            if (existing != null) canvasRoot = existing.gameObject;
        }

        if (canvasRoot != null)
        {
            if (Application.isPlaying) Destroy(canvasRoot);
            else DestroyImmediate(canvasRoot);
        }

        canvasRoot = new GameObject("MRIntegration_Canvas", typeof(RectTransform));
        canvasRoot.transform.SetParent(transform, false);
        canvasRoot.transform.localPosition = Vector3.zero;
        canvasRoot.transform.localRotation = Quaternion.identity;

        var rect = canvasRoot.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 8000;
        canvas.pixelPerfect = true;
        var scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRoot.AddComponent<GraphicRaycaster>();

        BuildLiveTelemetryPanel(rect);
        BuildLogTerminal(rect);

        toolsFrame = DashboardUIFactory.CreateRect("MRToolsFrame", rect, GetFrameSize(), new Vector2(-screenMargin.x, screenMargin.y), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        AddPanelImage(toolsFrame, PanelBg, Accent);
        DashboardUIFactory.ApplyCyberOverlay(toolsFrame, DashboardUIFactory.CyberStyle.PowerMeterGrey, false, false);

        toolbar = DashboardUIFactory.CreateRect("BottomRightActionDock", toolsFrame, new Vector2(324f, 132f), new Vector2(-14f, 12f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));

        nodeButton = CreatePanelButton("NodeRedButton", toolbar, new Vector2(300f, 36f), new Vector2(0f, 44f), "CONNECT NODE-RED", 12f);
        nodeButtonLabel = GetButtonLabel(nodeButton);
        nodeButton.onClick.AddListener(ToggleNodePanel);

        telemetryButton = CreatePanelButton("TelemetryButton", toolbar, new Vector2(300f, 36f), new Vector2(0f, 0f), "TELEMETRY GRAPHS", 12f);
        telemetryButtonLabel = GetButtonLabel(telemetryButton);
        telemetryButton.onClick.AddListener(ToggleTelemetryPanel);

        var schemaButton = CreatePanelButton("WiringButton", toolbar, new Vector2(300f, 36f), new Vector2(0f, -44f), "WIRING SCHEMA", 12f);
        schemaButton.onClick.AddListener(ToggleWiringGuide);

        nodePanel = DashboardUIFactory.CreateRect("NodeRedPanel", toolsFrame, new Vector2(720f, 500f), new Vector2(-30f, 202f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        AddPanelImage(nodePanel, new Color(0.07f, 0.075f, 0.08f, 0.98f), new Color(0.48f, 0.50f, 0.51f, 0.9f));
        AddPanelMotion(nodePanel);

        var title = CreateText("Title", nodePanel, "NODE-RED CONNECTION", 24f, TextAlignmentOptions.Left, Text);
        DashboardUIFactory.SetRect(title.rectTransform, new Vector2(660f, 38f), new Vector2(0f, 228f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));

        var ipLabel = CreateText("IpLabel", nodePanel, "IP", 14f, TextAlignmentOptions.Left, Muted);
        DashboardUIFactory.SetRect(ipLabel.rectTransform, new Vector2(286f, 24f), new Vector2(-206f, 184f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));

        ipInput = CreateInputField("IpAddressInput", nodePanel, new Vector2(286f, 50f), new Vector2(-206f, 148f), "200.200.200.177", false);

        var portLabel = CreateText("PortLabel", nodePanel, "PORT", 14f, TextAlignmentOptions.Left, Muted);
        DashboardUIFactory.SetRect(portLabel.rectTransform, new Vector2(96f, 24f), new Vector2(4f, 184f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));

        portInput = CreateInputField("PortInput", nodePanel, new Vector2(96f, 50f), new Vector2(4f, 148f), "1880", false);

        var passwordLabel = CreateText("PasswordLabel", nodePanel, "PASSWORD / API TOKEN", 14f, TextAlignmentOptions.Left, Muted);
        DashboardUIFactory.SetRect(passwordLabel.rectTransform, new Vector2(226f, 24f), new Vector2(226f, 184f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));

        passwordInput = CreateInputField("PasswordInput", nodePanel, new Vector2(226f, 50f), new Vector2(226f, 148f), "password", true);

        sourceText = CreateText("Source", nodePanel, "", 16f, TextAlignmentOptions.Left, Ok);
        DashboardUIFactory.SetRect(sourceText.rectTransform, new Vector2(660f, 26f), new Vector2(0f, 96f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));

        urlText = CreateText("Url", nodePanel, "", 13f, TextAlignmentOptions.Left, Muted);
        DashboardUIFactory.SetRect(urlText.rectTransform, new Vector2(660f, 24f), new Vector2(0f, 68f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));
        urlText.overflowMode = TextOverflowModes.Ellipsis;

        telemetryText = CreateMaskedLogText("ConnectionLog", nodePanel, new Vector2(672f, 178f), new Vector2(0f, -34f), 12f);

        statusText = CreateText("Status", nodePanel, "", 13f, TextAlignmentOptions.Left, Warn);
        DashboardUIFactory.SetRect(statusText.rectTransform, new Vector2(660f, 36f), new Vector2(0f, -150f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));
        statusText.overflowMode = TextOverflowModes.Ellipsis;

        liveButton = CreatePanelButton("LiveButton", nodePanel, new Vector2(202f, 44f), new Vector2(-226f, -220f), "CONNECT LIVE", 14f);
        liveButtonLabel = GetButtonLabel(liveButton);
        liveButton.onClick.AddListener(UseLiveData);

        simButton = CreatePanelButton("SimulationButton", nodePanel, new Vector2(202f, 44f), new Vector2(0f, -220f), "SIMULATION", 14f);
        simButton.onClick.AddListener(UseSimulation);

        var openButton = CreatePanelButton("OpenDashboardButton", nodePanel, new Vector2(202f, 44f), new Vector2(226f, -220f), "OPEN UI", 14f);
        openButton.onClick.AddListener(OpenDashboard);

        BuildTelemetryGraphsPanel();

        PopulateNodeRedFields();
        HookConnectionFieldEvents();
        nodePanel.gameObject.SetActive(nodePanelOpen);
        if (telemetryPanel != null) telemetryPanel.gameObject.SetActive(telemetryPanelOpen);
        UpdateFrameLayout();
        UpdatePanelText();
    }

    private void BuildLiveTelemetryPanel(RectTransform root)
    {
        liveTelemetryPanel = DashboardUIFactory.CreateRect("NodeRedLiveTelemetry", root, new Vector2(360f, 188f), new Vector2(screenMargin.x, -screenMargin.y), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        AddPanelImage(liveTelemetryPanel, new Color(0.04f, 0.043f, 0.046f, 0.88f), new Color(0.34f, 0.36f, 0.37f, 0.58f));
        AddPanelMotion(liveTelemetryPanel);

        CreateOverlayImage("TelemetryHeader", liveTelemetryPanel, new Vector2(328f, 26f), new Vector2(16f, -12f), new Color(0.085f, 0.09f, 0.095f, 0.94f), null, new Vector2(0f, 1f));
        var title = CreateText("TelemetryTitle", liveTelemetryPanel, "LIVE TELEMETRY", 10f, TextAlignmentOptions.Left, Accent);
        DashboardUIFactory.SetRect(title.rectTransform, new Vector2(140f, 18f), new Vector2(26f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        liveTelemetryStatusDot = CreateOverlayImage("TelemetryStatusDot", liveTelemetryPanel, new Vector2(8f, 8f), new Vector2(286f, -20f), Muted, DashboardUIFactory.GetCircleSprite(), new Vector2(0f, 1f));
        liveTelemetryStatusText = CreateText("TelemetryStatus", liveTelemetryPanel, "IDLE", 9f, TextAlignmentOptions.Left, Muted);
        DashboardUIFactory.SetRect(liveTelemetryStatusText.rectTransform, new Vector2(48f, 16f), new Vector2(300f, -17f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        CreateMetricWidget(0, "VOLTAGE", "V", MetricIconType.Voltage, new Vector2(14f, -46f), new Color(0.74f, 0.78f, 0.80f, 1f));
        CreateMetricWidget(1, "CURRENT", "A", MetricIconType.Current, new Vector2(184f, -46f), new Color(0.66f, 0.72f, 0.74f, 1f));
        CreateMetricWidget(2, "POWER", "kW", MetricIconType.Power, new Vector2(14f, -90f), new Color(0.82f, 0.84f, 0.84f, 1f));
        CreateMetricWidget(3, "FREQUENCY", "Hz", MetricIconType.Frequency, new Vector2(184f, -90f), new Color(0.70f, 0.76f, 0.78f, 1f));
        CreateMetricWidget(4, "POWER FACTOR", "PF", MetricIconType.PowerFactor, new Vector2(14f, -134f), new Color(0.78f, 0.80f, 0.80f, 1f));
        CreateMetricWidget(5, "ENERGY", "kWh", MetricIconType.Energy, new Vector2(184f, -134f), new Color(0.68f, 0.72f, 0.72f, 1f));
    }

    private void CreateMetricWidget(int index, string label, string unit, MetricIconType iconType, Vector2 position, Color iconColor)
    {
        var card = DashboardUIFactory.CreateRect("Metric_" + label.Replace(" ", ""), liveTelemetryPanel, new Vector2(162f, 36f), position, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        AddPanelImage(card, new Color(0.075f, 0.079f, 0.083f, 0.94f), new Color(0.34f, 0.36f, 0.37f, 0.44f));

        var iconRect = DashboardUIFactory.CreateRect("Icon", card, new Vector2(22f, 22f), new Vector2(10f, -7f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        iconRect.gameObject.AddComponent<CanvasRenderer>();
        var icon = iconRect.gameObject.AddComponent<MetricIconGraphic>();
        icon.iconType = iconType;
        icon.color = iconColor;
        icon.secondaryColor = new Color(iconColor.r, iconColor.g, iconColor.b, 0.32f);
        icon.raycastTarget = false;

        var title = CreateText("Label", card, label, 7.2f, TextAlignmentOptions.Left, Muted);
        DashboardUIFactory.SetRect(title.rectTransform, new Vector2(104f, 12f), new Vector2(38f, -4f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        metricValueTexts[index] = CreateText("Value", card, "--", 13f, TextAlignmentOptions.Left, Text);
        DashboardUIFactory.SetRect(metricValueTexts[index].rectTransform, new Vector2(74f, 18f), new Vector2(38f, -17f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        metricValueTexts[index].fontStyle = FontStyles.Bold;
        metricValueTexts[index].enableAutoSizing = true;
        metricValueTexts[index].fontSizeMin = 8f;
        metricValueTexts[index].fontSizeMax = 13f;

        metricUnitTexts[index] = CreateText("Unit", card, unit, 8.5f, TextAlignmentOptions.Right, Accent);
        DashboardUIFactory.SetRect(metricUnitTexts[index].rectTransform, new Vector2(36f, 14f), new Vector2(118f, -18f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
    }

    private void BuildTelemetryGraphsPanel()
    {
        telemetryPanel = DashboardUIFactory.CreateRect("TelemetryGraphsPanel", toolsFrame, new Vector2(720f, 390f), new Vector2(-30f, 202f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        AddPanelImage(telemetryPanel, new Color(0.06f, 0.064f, 0.068f, 0.98f), new Color(0.50f, 0.52f, 0.53f, 0.86f));
        AddPanelMotion(telemetryPanel);

        var title = CreateText("Title", telemetryPanel, "NODE-RED TELEMETRY", 22f, TextAlignmentOptions.Left, Text);
        DashboardUIFactory.SetRect(title.rectTransform, new Vector2(430f, 34f), new Vector2(-124f, 168f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));

        var source = CreateText("Source", telemetryPanel, "READ-ONLY LIVE VALUES", 12f, TextAlignmentOptions.Right, Accent);
        DashboardUIFactory.SetRect(source.rectTransform, new Vector2(230f, 24f), new Vector2(220f, 166f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));

        voltageChart = CreateGraphCard(0, "VoltageGraph", "VOLTAGE", "V", MetricIconType.Voltage, new Vector2(-178f, 70f), new Color(0.76f, 0.80f, 0.82f, 1f));
        currentChart = CreateGraphCard(1, "CurrentGraph", "CURRENT", "A", MetricIconType.Current, new Vector2(178f, 70f), new Color(0.66f, 0.72f, 0.74f, 1f));
        powerChart = CreateGraphCard(2, "PowerGraph", "ACTIVE POWER", "kW", MetricIconType.Power, new Vector2(-178f, -104f), new Color(0.82f, 0.84f, 0.84f, 1f));
        frequencyChart = CreateGraphCard(3, "FrequencyGraph", "FREQUENCY", "Hz", MetricIconType.Frequency, new Vector2(178f, -104f), new Color(0.70f, 0.76f, 0.78f, 1f));
    }

    private TelemetryChartGraphic CreateGraphCard(int index, string name, string label, string unit, MetricIconType iconType, Vector2 position, Color accentColor)
    {
        var card = DashboardUIFactory.CreateRect(name, telemetryPanel, new Vector2(332f, 146f), position, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));
        AddPanelImage(card, new Color(0.075f, 0.079f, 0.083f, 0.96f), new Color(0.34f, 0.36f, 0.37f, 0.48f));

        var iconRect = DashboardUIFactory.CreateRect("Icon", card, new Vector2(30f, 30f), new Vector2(-140f, 50f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));
        iconRect.gameObject.AddComponent<CanvasRenderer>();
        var icon = iconRect.gameObject.AddComponent<MetricIconGraphic>();
        icon.iconType = iconType;
        icon.color = accentColor;
        icon.secondaryColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.32f);
        icon.raycastTarget = false;

        var title = CreateText("Label", card, label, 12f, TextAlignmentOptions.Left, Text);
        DashboardUIFactory.SetRect(title.rectTransform, new Vector2(150f, 24f), new Vector2(-48f, 52f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));

        graphValueTexts[index] = CreateText("LiveValue", card, "-- " + unit, 16f, TextAlignmentOptions.Right, Text);
        DashboardUIFactory.SetRect(graphValueTexts[index].rectTransform, new Vector2(118f, 26f), new Vector2(92f, 51f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));
        graphValueTexts[index].fontStyle = FontStyles.Bold;
        graphValueTexts[index].enableAutoSizing = true;
        graphValueTexts[index].fontSizeMin = 10f;
        graphValueTexts[index].fontSizeMax = 16f;
        graphValueTexts[index].color = accentColor;

        var chartRect = DashboardUIFactory.CreateRect("Chart", card, new Vector2(292f, 72f), new Vector2(0f, -18f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));
        chartRect.gameObject.AddComponent<CanvasRenderer>();
        var chart = chartRect.gameObject.AddComponent<TelemetryChartGraphic>();
        chart.color = Color.white;
        chart.raycastTarget = false;

        graphScaleTexts[index] = CreateText("Scale", card, "0 - -- " + unit, 8.5f, TextAlignmentOptions.Right, Muted);
        DashboardUIFactory.SetRect(graphScaleTexts[index].rectTransform, new Vector2(292f, 14f), new Vector2(0f, -65f), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0.5f, 0.5f));
        graphScaleTexts[index].overflowMode = TextOverflowModes.Ellipsis;

        return chart;
    }

    private void BuildLogTerminal(RectTransform root)
    {
        logTerminal = DashboardUIFactory.CreateRect("NodeRedLogTerminal", root, new Vector2(430f, 140f), new Vector2(screenMargin.x, screenMargin.y), Vector2.zero, Vector2.zero, Vector2.zero);
        AddPanelImage(logTerminal, new Color(0.035f, 0.038f, 0.04f, 0.94f), new Color(0.34f, 0.36f, 0.37f, 0.72f));
        AddPanelMotion(logTerminal);

        CreateTerminalImage("TerminalHeader", logTerminal, new Vector2(398f, 28f), new Vector2(16f, 96f), new Color(0.085f, 0.09f, 0.095f, 0.98f));
        var title = CreateText("TerminalTitle", logTerminal, "NODE-RED LOG", 10.5f, TextAlignmentOptions.Left, Accent);
        DashboardUIFactory.SetRect(title.rectTransform, new Vector2(180f, 20f), new Vector2(28f, 101f), Vector2.zero, Vector2.zero, Vector2.zero);

        logTerminalStatusDot = CreateTerminalImage("TerminalStatusDot", logTerminal, new Vector2(8f, 8f), new Vector2(322f, 106f), Ok, DashboardUIFactory.GetCircleSprite());
        logTerminalStatusText = CreateText("TerminalStatus", logTerminal, "IDLE", 9.5f, TextAlignmentOptions.Left, Muted);
        DashboardUIFactory.SetRect(logTerminalStatusText.rectTransform, new Vector2(70f, 18f), new Vector2(336f, 101f), Vector2.zero, Vector2.zero, Vector2.zero);

        CreateTerminalImage("TerminalLeftRail", logTerminal, new Vector2(3f, 70f), new Vector2(26f, 18f), new Color(Accent.r, Accent.g, Accent.b, 0.34f));
        logTerminalText = CreateMaskedLogText("TerminalScreen", logTerminal, new Vector2(360f, 78f), new Vector2(42f, 16f), 9.2f, Vector2.zero);
        logTerminalText.text = "Node-RED bridge idle.";
        logTerminalText.lineSpacing = -6f;
    }

    private TextMeshProUGUI CreateMaskedLogText(string name, Transform parent, Vector2 size, Vector2 anchoredPos, float fontSize)
    {
        return CreateMaskedLogText(name, parent, size, anchoredPos, fontSize, Vector2.one * 0.5f);
    }

    private TextMeshProUGUI CreateMaskedLogText(string name, Transform parent, Vector2 size, Vector2 anchoredPos, float fontSize, Vector2 anchor)
    {
        var viewport = DashboardUIFactory.CreateRect(name + "Viewport", parent, size, anchoredPos, anchor, anchor, anchor);
        var image = viewport.gameObject.AddComponent<Image>();
        image.color = new Color(0.006f, 0.008f, 0.009f, 0.96f);
        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        viewport.gameObject.AddComponent<RectMask2D>();

        var text = CreateText(name + "Text", viewport, "", fontSize, TextAlignmentOptions.TopLeft, Text);
        DashboardUIFactory.SetRect(text.rectTransform, new Vector2(size.x - 24f, size.y - 18f), new Vector2(12f, -9f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateTerminalImage(string name, Transform parent, Vector2 size, Vector2 anchoredPos, Color color, Sprite sprite = null)
    {
        var rect = DashboardUIFactory.CreateRect(name, parent, size, anchoredPos, Vector2.zero, Vector2.zero, Vector2.zero);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
        }
        else
        {
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    private static Image CreateOverlayImage(string name, Transform parent, Vector2 size, Vector2 anchoredPos, Color color, Sprite sprite, Vector2 anchor)
    {
        var rect = DashboardUIFactory.CreateRect(name, parent, size, anchoredPos, anchor, anchor, anchor);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
        }
        else
        {
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    private void ResolveReferences()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        if (nodeRed == null) nodeRed = FindAnyObjectByType<NodeRedClient>(FindObjectsInactive.Include);
        if (wiringGuide == null) wiringGuide = FindAnyObjectByType<WiringGuideUI>(FindObjectsInactive.Include);
    }

    private Button CreatePanelButton(string name, Transform parent, Vector2 size, Vector2 pos, string label, float fontSize = 12f)
    {
        var button = DashboardUIFactory.CreateButton(name, parent, size, pos, ButtonBg, label, fontMain, fontSize, Text);
        var image = button.targetGraphic as Image;
        if (image != null)
        {
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
        }
        var outline = button.gameObject.GetComponent<Outline>();
        if (outline == null) outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.7f);
        outline.effectDistance = new Vector2(1f, -1f);
        return button;
    }

    private TMP_InputField CreateInputField(string name, Transform parent, Vector2 size, Vector2 pos, string placeholderText, bool password)
    {
        var rect = DashboardUIFactory.CreateRect(name, parent, size, pos, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = InputBg;
        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.36f);
        outline.effectDistance = new Vector2(1f, -1f);

        var input = rect.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.asteriskChar = '*';

        var text = CreateText("Text", rect, "", size.y >= 48f ? 16f : 12f, TextAlignmentOptions.Left, Text);
        DashboardUIFactory.SetRect(text.rectTransform, new Vector2(size.x - 24f, size.y), new Vector2(0f, 0f), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f);
        text.raycastTarget = false;
        input.textComponent = text;

        var placeholder = CreateText("Placeholder", rect, placeholderText, size.y >= 48f ? 16f : 12f, TextAlignmentOptions.Left, Muted);
        DashboardUIFactory.SetRect(placeholder.rectTransform, new Vector2(size.x - 24f, size.y), new Vector2(0f, 0f), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f);
        placeholder.raycastTarget = false;
        input.placeholder = placeholder;

        var colors = input.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.74f, 0.76f, 0.77f, 1f);
        colors.pressedColor = new Color(0.58f, 0.60f, 0.61f, 1f);
        colors.selectedColor = new Color(0.70f, 0.72f, 0.73f, 1f);
        colors.fadeDuration = 0.08f;
        input.colors = colors;
        return input;
    }

    private static TextMeshProUGUI GetButtonLabel(Button button)
    {
        return button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    private static void AddPanelMotion(RectTransform rect)
    {
        if (rect == null) return;
        var motion = rect.GetComponent<DashboardMotionAnimator>();
        if (motion == null) motion = rect.gameObject.AddComponent<DashboardMotionAnimator>();
        motion.motionType = DashboardMotionAnimator.MotionType.Panel;
        motion.entranceDuration = 0.22f;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions align, Color color)
    {
        var text = DashboardUIFactory.CreateText(name, parent, value, fontMain, size, align, color);
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static void AddPanelImage(RectTransform rect, Color fill, Color outlineColor)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = fill;
        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.58f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private static Sprite GetRoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;

        const int size = 64;
        const int radius = 14;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x < radius ? radius - x : x >= size - radius ? x - (size - radius - 1) : 0;
                int dy = y < radius ? radius - y : y >= size - radius ? y - (size - radius - 1) : 0;
                bool inside = dx == 0 && dy == 0 || (dx * dx + dy * dy) <= radius * radius;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();

        roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedSprite;
    }

    private void ToggleNodePanel()
    {
        nodePanelOpen = !nodePanelOpen;
        if (nodePanelOpen) telemetryPanelOpen = false;
        if (nodePanel != null) nodePanel.gameObject.SetActive(nodePanelOpen);
        if (telemetryPanel != null) telemetryPanel.gameObject.SetActive(telemetryPanelOpen);
        if (nodePanelOpen) PopulateNodeRedFields();
        UpdateFrameLayout();
        UpdatePanelText();
    }

    private void ToggleTelemetryPanel()
    {
        telemetryPanelOpen = !telemetryPanelOpen;
        if (telemetryPanelOpen) nodePanelOpen = false;
        if (nodePanel != null) nodePanel.gameObject.SetActive(nodePanelOpen);
        if (telemetryPanel != null) telemetryPanel.gameObject.SetActive(telemetryPanelOpen);
        UpdateFrameLayout();
        UpdatePanelText();
    }

    private void ToggleWiringGuide()
    {
        EnsureWiringGuide();
        wiringGuide.ToggleManualVisibility();
    }

    private void EnsureWiringGuide()
    {
        ResolveReferences();
        if (wiringGuide == null)
        {
            var host = bench != null ? bench.gameObject : gameObject;
            wiringGuide = host.AddComponent<WiringGuideUI>();
        }

        wiringGuide.bench = bench;
    }

    private void UseLiveData()
    {
        ResolveReferences();
        if (nodeRed != null)
        {
            ApplyNodeRedConnection();
            nodeRed.UseLiveData();
        }
        UpdatePanelText();
    }

    private void UseSimulation()
    {
        ResolveReferences();
        if (nodeRed != null) nodeRed.UseSimulation();
        UpdatePanelText();
    }

    private void OpenDashboard()
    {
        ResolveReferences();
        if (nodeRed != null && !string.IsNullOrEmpty(nodeRed.dashboardUrl))
        {
            ApplyNodeRedConnection();
            Application.OpenURL(nodeRed.dashboardUrl);
        }
    }

    private void UpdatePanelText()
    {
        ResolveReferences();
        if (nodeRed == null)
        {
            if (sourceText != null) sourceText.text = "SOURCE: NODE-RED CLIENT NOT FOUND";
            if (statusText != null) statusText.text = "Add NodeRedClient to the scene to pull dashboard telemetry.";
            if (nodeButtonLabel != null) nodeButtonLabel.text = "NODE-RED MISSING";
            if (logTerminalText != null)
            {
                SetTerminalStatus("MISSING", Warn);
                logTerminalText.color = Warn;
                logTerminalText.text = "[--:--:--] Node-RED client not found";
            }

            if (telemetryButtonLabel != null) telemetryButtonLabel.text = "TELEMETRY";
            SetButtonState(telemetryButton, false);
            UpdateLiveTelemetryWidget("MISSING", Warn);
            SetMetricValues("--", "--", "--", "--", "--", "--");
            UpdateTelemetryCharts();
            return;
        }

        bool live = !nodeRed.useSimulation;
        if (nodeButtonLabel != null) nodeButtonLabel.text = live ? "NODE-RED CONNECTED" : "CONNECT NODE-RED";
        if (telemetryButtonLabel != null) telemetryButtonLabel.text = telemetryPanelOpen ? "HIDE TELEMETRY" : "TELEMETRY GRAPHS";
        if (liveButtonLabel != null) liveButtonLabel.text = live ? "RECONNECT" : "CONNECT LIVE";
        if (sourceText != null)
        {
            sourceText.text = live ? "SOURCE: LIVE NODE-RED TELEMETRY" : "SOURCE: LOCAL SIMULATION";
            sourceText.color = live ? Ok : Warn;
        }

        if (urlText != null)
        {
            urlText.text = nodeRed.nodeRedUrl;
        }

        if (telemetryText != null)
        {
            telemetryText.text = FormatLogForDisplay(nodeRed.connectionLog, 8, 92);
        }

        if (logTerminalText != null)
        {
            bool hasError = !string.IsNullOrEmpty(nodeRed.lastError);
            SetTerminalStatus(hasError ? "ISSUE" : live ? "LIVE" : "IDLE", hasError ? Warn : live ? Ok : Muted);
            logTerminalText.color = hasError ? Warn : Text;
            logTerminalText.text = FormatLogForDisplay(nodeRed.connectionLog, 4, 56);
        }

        if (statusText != null)
        {
            string stamp = string.IsNullOrEmpty(nodeRed.lastSuccessfulPollTime) ? "waiting for first packet" : nodeRed.lastSuccessfulPollTime;
            statusText.text = live
                ? "Connected in Play Mode - last packet: " + stamp
                : "Simulation mode - enter IP/password and press CONNECT LIVE";
        }

        UpdateLiveTelemetryFromBench(live);
        UpdateTelemetryCharts();
        SetButtonState(telemetryButton, telemetryPanelOpen);
        SetButtonState(liveButton, live);
        SetButtonState(simButton, !live);
    }

    private static void SetButtonState(Button button, bool selected)
    {
        if (button == null) return;
        var image = button.targetGraphic as Image;
        if (image != null) image.color = selected ? ButtonSelectedBg : ButtonBg;
    }

    private static string FormatLogForDisplay(string log, int maxLines, int maxCharsPerLine)
    {
        if (string.IsNullOrWhiteSpace(log)) return "[--:--:--] Waiting for Node-RED logs";
        string normalized = log.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] rawLines = normalized.Split('\n');
        int start = Mathf.Max(0, rawLines.Length - Mathf.Max(1, maxLines));
        var builder = new System.Text.StringBuilder();

        for (int i = start; i < rawLines.Length; i++)
        {
            string line = rawLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            line = line.Trim();
            if (line.Length > maxCharsPerLine)
            {
                line = line.Substring(0, Mathf.Max(0, maxCharsPerLine - 3)) + "...";
            }

            if (builder.Length > 0) builder.Append('\n');
            builder.Append(line);
        }

        return builder.Length > 0 ? builder.ToString() : "[--:--:--] Waiting for Node-RED logs";
    }

    private void SetTerminalStatus(string label, Color color)
    {
        if (logTerminalStatusDot != null) logTerminalStatusDot.color = color;
        if (logTerminalStatusText != null)
        {
            logTerminalStatusText.text = label;
            logTerminalStatusText.color = color;
        }
    }

    private void UpdateLiveTelemetryFromBench(bool live)
    {
        if (bench == null)
        {
            UpdateLiveTelemetryWidget("NO DATA", Warn);
            SetMetricValues("--", "--", "--", "--", "--", "--");
            return;
        }

        bool hasError = nodeRed != null && !string.IsNullOrEmpty(nodeRed.lastError);
        bool waiting = nodeRed != null && live && string.IsNullOrEmpty(nodeRed.lastSuccessfulPollTime);
        string status = hasError ? "ISSUE" : waiting ? "WAIT" : live ? "LIVE" : "SIM";
        Color statusColor = hasError || waiting ? Warn : live ? Ok : Muted;

        UpdateLiveTelemetryWidget(status, statusColor);
        SetMetricValues(
            bench.voltage.ToString("0.0"),
            bench.current.ToString("0.00"),
            bench.power.ToString("0.000"),
            bench.frequency.ToString("0.00"),
            bench.powerFactor.ToString("0.00"),
            (bench.energy / 1000f).ToString("0.00"));
    }

    private void UpdateLiveTelemetryWidget(string status, Color color)
    {
        if (liveTelemetryStatusDot != null) liveTelemetryStatusDot.color = color;
        if (liveTelemetryStatusText != null)
        {
            liveTelemetryStatusText.text = status;
            liveTelemetryStatusText.color = color;
        }
    }

    private void SetMetricValues(string voltage, string current, string power, string frequency, string powerFactor, string energy)
    {
        SetMetricText(0, voltage);
        SetMetricText(1, current);
        SetMetricText(2, power);
        SetMetricText(3, frequency);
        SetMetricText(4, powerFactor);
        SetMetricText(5, energy);
    }

    private void SetMetricText(int index, string value)
    {
        if (index < 0 || index >= metricValueTexts.Length || metricValueTexts[index] == null) return;
        metricValueTexts[index].text = value;
    }

    private void SampleTelemetryHistory()
    {
        if (bench == null) return;
        float delta = Application.isPlaying ? Time.deltaTime : 0.12f;
        telemetrySampleTimer += Mathf.Max(0.02f, delta);
        if (telemetrySampleTimer < 0.5f && telemetryHistoryCount > 0) return;
        telemetrySampleTimer = 0f;

        AddHistorySample(voltageHistory, bench.voltage);
        AddHistorySample(currentHistory, bench.current);
        AddHistorySample(powerHistory, bench.power);
        AddHistorySample(frequencyHistory, bench.frequency);
        if (telemetryHistoryCount < TelemetryHistoryCapacity) telemetryHistoryCount++;
    }

    private void AddHistorySample(float[] history, float value)
    {
        if (history == null || history.Length == 0) return;

        if (telemetryHistoryCount < TelemetryHistoryCapacity)
        {
            history[telemetryHistoryCount] = value;
            return;
        }

        for (int i = 1; i < history.Length; i++) history[i - 1] = history[i];
        history[history.Length - 1] = value;
    }

    private void UpdateTelemetryCharts()
    {
        int count = Mathf.Max(telemetryHistoryCount, 0);
        Color grid = new Color(0.72f, 0.76f, 0.78f, 0.10f);

        if (voltageChart != null)
        {
            float max = Mathf.Max(450f, MaxHistory(voltageHistory, count) * 1.12f);
            voltageChart.SetData(voltageHistory, count, 0f, max, new Color(0.76f, 0.80f, 0.82f, 1f), grid);
            SetGraphReadout(0, bench != null ? bench.voltage : 0f, "0.0", "V", 0f, max);
        }

        if (currentChart != null)
        {
            float max = Mathf.Max(5f, MaxHistory(currentHistory, count) * 1.25f);
            currentChart.SetData(currentHistory, count, 0f, max, new Color(0.66f, 0.72f, 0.74f, 1f), grid);
            SetGraphReadout(1, bench != null ? bench.current : 0f, "0.00", "A", 0f, max);
        }

        if (powerChart != null)
        {
            float max = Mathf.Max(2f, MaxHistory(powerHistory, count) * 1.25f);
            powerChart.SetData(powerHistory, count, 0f, max, new Color(0.82f, 0.84f, 0.84f, 1f), grid);
            SetGraphReadout(2, bench != null ? bench.power : 0f, "0.000", "kW", 0f, max);
        }

        if (frequencyChart != null)
        {
            float max = Mathf.Max(60f, MaxHistory(frequencyHistory, count) * 1.12f);
            frequencyChart.SetData(frequencyHistory, count, 0f, max, new Color(0.70f, 0.76f, 0.78f, 1f), grid);
            SetGraphReadout(3, bench != null ? bench.frequency : 0f, "0.00", "Hz", 0f, max);
        }
    }

    private void SetGraphReadout(int index, float value, string valueFormat, string unit, float min, float max)
    {
        if (index < 0 || index >= graphValueTexts.Length) return;

        if (graphValueTexts[index] != null)
        {
            graphValueTexts[index].text = value.ToString(valueFormat) + " " + unit;
        }

        if (graphScaleTexts[index] != null)
        {
            graphScaleTexts[index].text = min.ToString("0.#") + " - " + max.ToString(max >= 10f ? "0" : "0.0") + " " + unit;
        }
    }

    private static float MaxHistory(float[] history, int count)
    {
        if (history == null || count <= 0) return 0f;
        int limit = Mathf.Min(count, history.Length);
        float max = 0f;
        for (int i = 0; i < limit; i++) max = Mathf.Max(max, history[i]);
        return max;
    }

    private void ApplyNodeRedConnection()
    {
        if (nodeRed == null) return;
        string baseAddress = BuildNodeRedAddress();
        nodeRed.ConfigureFromBaseUrl(baseAddress);
        nodeRed.nodeRedPassword = passwordInput != null ? passwordInput.text : string.Empty;
    }

    private void HookConnectionFieldEvents()
    {
        if (ipInput != null)
        {
            ipInput.onEndEdit.RemoveListener(OnConnectionFieldEdited);
            ipInput.onEndEdit.AddListener(OnConnectionFieldEdited);
            ipInput.onSubmit.RemoveListener(OnConnectionFieldEdited);
            ipInput.onSubmit.AddListener(OnConnectionFieldEdited);
        }

        if (passwordInput != null)
        {
            passwordInput.onEndEdit.RemoveListener(OnConnectionFieldEdited);
            passwordInput.onEndEdit.AddListener(OnConnectionFieldEdited);
            passwordInput.onSubmit.RemoveListener(OnConnectionFieldEdited);
            passwordInput.onSubmit.AddListener(OnConnectionFieldEdited);
        }

        if (portInput != null)
        {
            portInput.onEndEdit.RemoveListener(OnConnectionFieldEdited);
            portInput.onEndEdit.AddListener(OnConnectionFieldEdited);
            portInput.onSubmit.RemoveListener(OnConnectionFieldEdited);
            portInput.onSubmit.AddListener(OnConnectionFieldEdited);
        }
    }

    private void OnConnectionFieldEdited(string _)
    {
        ResolveReferences();
        if (nodeRed != null) ApplyNodeRedConnection();
        UpdatePanelText();
    }

    private void PopulateNodeRedFields()
    {
        if (nodeRed == null) return;
        if (ipInput != null && !ipInput.isFocused) ipInput.text = ExtractHost(nodeRed.nodeRedBaseUrl);
        if (portInput != null && !portInput.isFocused) portInput.text = ExtractPort(nodeRed.nodeRedBaseUrl);
        if (passwordInput != null && !passwordInput.isFocused) passwordInput.text = nodeRed.nodeRedPassword;
    }

    private void UpdateFrameLayout()
    {
        if (toolsFrame == null) return;
        toolsFrame.sizeDelta = GetFrameSize();
    }

    private Vector2 GetFrameSize()
    {
        if (nodePanelOpen) return new Vector2(780f, 760f);
        if (telemetryPanelOpen) return new Vector2(780f, 610f);
        return new Vector2(360f, 156f);
    }

    private static string NormalizeNodeRedBaseAddress(string value)
    {
        string trimmed = string.IsNullOrWhiteSpace(value) ? "200.200.200.177:1880" : value.Trim();
        trimmed = trimmed.TrimEnd('/');

        if (!trimmed.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "http://" + trimmed;
        }

        int hostStart = trimmed.IndexOf("://", System.StringComparison.Ordinal) + 3;
        int pathStart = trimmed.IndexOf('/', hostStart);
        if (pathStart >= 0) trimmed = trimmed.Substring(0, pathStart);

        string host = trimmed.Substring(hostStart);
        if (!host.Contains(":")) trimmed += ":1880";
        return trimmed;
    }

    private static string ExtractBaseAddress(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return "200.200.200.177:1880";
        string trimmed = endpoint.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/telemetry", System.StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - "/telemetry".Length);
        }
        else if (trimmed.EndsWith("/ui", System.StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - "/ui".Length);
        }
        return trimmed;
    }

    private string BuildNodeRedAddress()
    {
        string host = ipInput != null ? ipInput.text : string.Empty;
        string port = portInput != null ? portInput.text : string.Empty;
        host = string.IsNullOrWhiteSpace(host) ? "200.200.200.177" : host.Trim();
        port = string.IsNullOrWhiteSpace(port) ? "1880" : port.Trim();

        string normalizedHost = NormalizeNodeRedBaseAddress(host);
        string displayHost = ExtractHost(normalizedHost);
        return NormalizeNodeRedBaseAddress(displayHost + ":" + port);
    }

    private static string ExtractHost(string endpoint)
    {
        string baseAddress = ExtractBaseAddress(endpoint);
        if (baseAddress.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase))
        {
            baseAddress = baseAddress.Substring("http://".Length);
        }
        else if (baseAddress.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            baseAddress = baseAddress.Substring("https://".Length);
        }

        int slash = baseAddress.IndexOf('/');
        if (slash >= 0) baseAddress = baseAddress.Substring(0, slash);
        int colon = baseAddress.LastIndexOf(':');
        if (colon > 0) baseAddress = baseAddress.Substring(0, colon);
        return baseAddress;
    }

    private static string ExtractPort(string endpoint)
    {
        string baseAddress = ExtractBaseAddress(endpoint);
        if (baseAddress.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase))
        {
            baseAddress = baseAddress.Substring("http://".Length);
        }
        else if (baseAddress.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            baseAddress = baseAddress.Substring("https://".Length);
        }

        int slash = baseAddress.IndexOf('/');
        if (slash >= 0) baseAddress = baseAddress.Substring(0, slash);
        int colon = baseAddress.LastIndexOf(':');
        return colon > 0 && colon < baseAddress.Length - 1 ? baseAddress.Substring(colon + 1) : "1880";
    }

    private static void EnsureEventSystem()
    {
        var existing = FindAnyObjectByType<EventSystem>();
        if (existing != null)
        {
            EnsureInputModule(existing.gameObject);
            return;
        }

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        EnsureInputModule(eventSystem);
        if (!Application.isPlaying) eventSystem.hideFlags = HideFlags.DontSaveInEditor;
    }

    private static void EnsureInputModule(GameObject eventSystem)
    {
        if (eventSystem == null || eventSystem.GetComponent<BaseInputModule>() != null) return;
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }
}
