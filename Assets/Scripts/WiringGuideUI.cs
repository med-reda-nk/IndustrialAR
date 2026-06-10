using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class WiringGuideUI : MonoBehaviour
{
    [Header("Component References")]
    public VFDController vfd;
    public MotorController motor;
    public BenchSystem bench;
    public SignalTower tower;
    public EStopButton eStop;

    [Header("Placement")]
    public Vector3 localOffset = new Vector3(0f, 0.6f, 0f);
    public float canvasScale = 0.001f;
    public bool screenSpaceOverlay = true;
    public bool stickyToSurface;
    public Vector2 screenMargin = new Vector2(24f, 132f);
    public float stickyDistance = 1.25f;
    public float schemaScale = 1.55f;
    [Range(0.45f, 0.95f)] public float stickyViewportFit = 0.78f;

    [Header("Visibility")]
    public bool alwaysShow = false;
    public bool showOnlyWhenIncomplete = true;

    private Canvas canvas;
    private RectTransform rootRect;
    private GameObject guideRoot;
    private bool built;
    private bool manualVisible;

    private const float PanelW = 1040f;
    private const float PanelH = 650f;

    private static readonly Color PanelBg = new Color(0f, 0f, 0f, 0f);
    private static readonly Color Backplate = new Color(0.84f, 0.86f, 0.86f, 1f);
    private static readonly Color DeviceBody = new Color(0.98f, 0.98f, 0.96f, 0.62f);
    private static readonly Color DeviceFace = new Color(0.91f, 0.92f, 0.90f, 0.58f);
    private static readonly Color Terminal = new Color(0.03f, 0.035f, 0.04f, 1f);
    private static readonly Color Rail = new Color(0.68f, 0.70f, 0.70f, 1f);
    private static readonly Color Duct = new Color(0.88f, 0.90f, 0.90f, 1f);
    private static readonly Color Accent = new Color(0.08f, 0.10f, 0.12f, 1f);
    private static readonly Color TextPrimary = new Color(0.05f, 0.06f, 0.07f, 1f);
    private static readonly Color TextMuted = new Color(0.18f, 0.20f, 0.22f, 1f);
    private static readonly Color WireMissing = new Color(0.96f, 0.62f, 0.18f, 1f);
    private static readonly Color WirePower = new Color(0.10f, 0.10f, 0.10f, 1f);
    private static readonly Color WirePhase = new Color(0.02f, 0.02f, 0.02f, 1f);
    private static readonly Color Wire24V = new Color(0.88f, 0.22f, 0.18f, 1f);
    private static readonly Color Wire0V = new Color(0.16f, 0.34f, 0.86f, 1f);
    private static readonly Color WirePE = new Color(0.15f, 0.76f, 0.34f, 1f);
    private static readonly Color WireControl = new Color(0.42f, 0.72f, 0.74f, 1f);
    private static readonly Color WireSafety = new Color(1f, 0.20f, 0.24f, 1f);
    private static readonly Color WireEthernet = new Color(0.24f, 0.85f, 0.45f, 1f);
    private static readonly Color WireModbus = new Color(0.65f, 0.56f, 0.86f, 1f);

    private WireDef[] wireDefs;
    private List<Image>[] wireSegments;
    private Image[] wireDots;
    private TextMeshProUGUI[] wireLabels;
    private TextMeshProUGUI modeText;
    private TextMeshProUGUI stickText;

    private struct WireDef
    {
        public string label;
        public Vector2[] path;
        public Color color;
        public System.Func<bool> check;
    }

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        built = false;
    }

    private void Start()
    {
        Rebuild();
    }

    private void Update()
    {
        if (!built) Rebuild();
        ResolveRefs();
        UpdateVisibility();
        UpdateWireStates();
    }

    private void Rebuild()
    {
        if (built) return;
        built = true;

        ResolveRefs();
        BuildWireDefs();
        EnsureEventSystem();

        DestroyAllGuideRoots();

        guideRoot = new GameObject("WiringGuide_Canvas", typeof(RectTransform));
        guideRoot.transform.SetParent(screenSpaceOverlay ? transform : null, false);

        canvas = guideRoot.AddComponent<Canvas>();
        var scaler = guideRoot.AddComponent<CanvasScaler>();
        guideRoot.AddComponent<GraphicRaycaster>();

        rootRect = guideRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(PanelW, PanelH);

        if (screenSpaceOverlay)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 4990;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            rootRect.anchorMin = new Vector2(1f, 0f);
            rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(1f, 0f);
            rootRect.anchoredPosition = new Vector2(-screenMargin.x, screenMargin.y);
            rootRect.localScale = Vector3.one * Mathf.Max(0.5f, schemaScale);
        }
        else
        {
            canvas.renderMode = RenderMode.WorldSpace;
            PlaceOnSurface();
        }

        BuildPanelSchema();
        UpdateVisibility();
        UpdateWireStates();
    }

    private void DestroyAllGuideRoots()
    {
        var rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
        for (int i = 0; i < rects.Length; i++)
        {
            var rect = rects[i];
            if (rect == null || rect.gameObject == null) continue;
            if (rect.gameObject == gameObject) continue;
            if (rect.name != "WiringGuide_Canvas") continue;

            if (Application.isPlaying) Destroy(rect.gameObject);
            else DestroyImmediate(rect.gameObject);
        }

        guideRoot = null;
        canvas = null;
        rootRect = null;
    }

    private void BuildPanelSchema()
    {
        TMP_FontAsset font = FindFont();

        AddTransparentSheet(rootRect, "QetSheet", Vector2.zero, new Vector2(1000f, 610f));
        AddText(rootRect, "Title", "SCHEMA DE CABLAGE INDUSTRIEL", new Vector2(-300f, 286f), new Vector2(520f, 28f), font, 17f, TextAlignmentOptions.Left, TextPrimary);
        AddText(rootRect, "Subtitle", "QElectroTech-style: folio 1/1 - power, 24VDC control, safety, metering, and communication.", new Vector2(-186f, 262f), new Vector2(760f, 20f), font, 9f, TextAlignmentOptions.Left, TextMuted);

        modeText = AddText(rootRect, "Mode", "", new Vector2(216f, 286f), new Vector2(240f, 24f), font, 8.5f, TextAlignmentOptions.Right, TextMuted);
        CreateStickToggle(font);

        DrawBusLabel(font, "3~ 400 VAC", new Vector2(-448f, 226f));
        DrawBusLabel(font, "24 VDC", new Vector2(-448f, -12f));
        DrawBusLabel(font, "SAFETY", new Vector2(-448f, -116f));
        DrawBusLabel(font, "RS-485 / ETH", new Vector2(250f, -238f));

        DrawBreaker(font, new Vector2(-430f, 150f));
        DrawPowerSupply(font, new Vector2(-270f, 150f));
        DrawVfd(font, new Vector2(-70f, 150f));
        DrawMotor(font, new Vector2(210f, 150f));
        DrawPlc(font, new Vector2(-260f, -80f));
        DrawHmi(font, new Vector2(-40f, -80f));
        DrawSignalTower(font, new Vector2(430f, 82f));
        DrawEStop(font, new Vector2(-430f, -95f));
        DrawPm2200(font, new Vector2(370f, -82f));
        DrawTerminalStrip(font, "X1 POWER", new Vector2(85f, 18f), new Vector2(260f, 58f), new[] { "1 L1", "2 L2", "3 L3", "4 PE", "5 +24", "6 0V" });
        DrawTerminalStrip(font, "X2 CONTROL", new Vector2(-72f, -226f), new Vector2(300f, 58f), new[] { "1 DI2", "2 DQ0", "3 DQ1", "4 DQ2", "5 AI1", "6 AQ0" });
        DrawTerminalStrip(font, "X3 METER/COM", new Vector2(250f, -226f), new Vector2(300f, 58f), new[] { "1 RS+", "2 RS-", "3 ETH", "4 V1", "5 V2", "6 V3" });
        DrawEarthBar(font, new Vector2(462f, -236f));

        BuildWires(font);

    }

    private void CreateStickToggle(TMP_FontAsset font)
    {
        var rect = MakeRect("StickSurfaceToggle", rootRect, new Vector2(154f, 28f), new Vector2(420f, 286f));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.98f, 0.98f, 0.96f, 0.72f);
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = TextPrimary;
        outline.effectDistance = new Vector2(1f, -1f);

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(ToggleSurfaceStick);

        stickText = AddText(rect, "Label", "", Vector2.zero, new Vector2(134f, 20f), font, 8.6f, TextAlignmentOptions.Center, TextPrimary);
    }

    private void BuildWires(TMP_FontAsset font)
    {
        int count = wireDefs != null ? wireDefs.Length : 0;
        wireSegments = new List<Image>[count];
        wireDots = new Image[count * 2];
        wireLabels = new TextMeshProUGUI[count];

        for (int i = 0; i < count; i++)
        {
            var wire = wireDefs[i];
            wireSegments[i] = CreateWirePath("Wire_" + i, wire.path, wire.color, 2.2f);
            wireDots[i * 2] = CreateTerminalDot("WireStart_" + i, wire.path[0], wire.color);
            wireDots[i * 2 + 1] = CreateTerminalDot("WireEnd_" + i, wire.path[wire.path.Length - 1], wire.color);

            Vector2 labelPos = GetWireLabelPosition(i, wire.path);
            wireLabels[i] = AddText(rootRect, "WireLabel_" + i, wire.label, labelPos, new Vector2(170f, 24f), font, 6.4f, TextAlignmentOptions.Center, TextPrimary);
            wireLabels[i].enableAutoSizing = true;
            wireLabels[i].fontSizeMin = 4.6f;
            wireLabels[i].fontSizeMax = 6.4f;
        }
    }

    private void DrawBreaker(TMP_FontAsset font, Vector2 pos)
    {
        var body = AddCadPanel(rootRect, "MainBreaker", pos, new Vector2(96f, 126f), DeviceBody, TextPrimary);
        AddText(body, "Name", "QF1\n3P\nBREAKER", Vector2.zero, new Vector2(78f, 60f), font, 10f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(body, new Vector2(0f, 50f), new[] { "L1", "L2", "L3", "PE" }, font);
        AddTerminalRow(body, new Vector2(0f, -50f), new[] { "T1", "T2", "T3", "PE" }, font);
        AddCadLine("QF1Switch", pos + new Vector2(-18f, -18f), pos + new Vector2(18f, 22f), TextPrimary, 2.2f);
        AddCadLine("QF1ContactTop", pos + new Vector2(-24f, 24f), pos + new Vector2(24f, 24f), TextPrimary, 1.5f);
        AddCadLine("QF1ContactBottom", pos + new Vector2(-24f, -24f), pos + new Vector2(24f, -24f), TextPrimary, 1.5f);
    }

    private void DrawPowerSupply(TMP_FontAsset font, Vector2 pos)
    {
        var body = AddCadPanel(rootRect, "PowerSupply", pos, new Vector2(96f, 126f), DeviceBody, TextPrimary);
        AddText(body, "Name", "PS1\n24 VDC\nSUPPLY", Vector2.zero, new Vector2(78f, 58f), font, 9.5f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(body, new Vector2(0f, 50f), new[] { "L1", "N", "PE" }, font);
        AddTerminalRow(body, new Vector2(0f, -50f), new[] { "+24", "0V" }, font);
        AddPanel(body, "PowerLed", new Vector2(30f, 4f), new Vector2(10f, 10f), Wire24V, DashboardUIFactory.GetCircleSprite());
        AddCadLine("PS1Separator", pos + new Vector2(-34f, -12f), pos + new Vector2(34f, -12f), TextPrimary, 1.3f);
    }

    private void DrawVfd(TMP_FontAsset font, Vector2 pos)
    {
        var body = AddCadPanel(rootRect, "VFD", pos, new Vector2(116f, 148f), DeviceBody, TextPrimary);
        AddText(body, "Name", "VFD\nATV DRIVE", new Vector2(0f, 34f), new Vector2(86f, 40f), font, 10.5f, TextAlignmentOptions.Center, TextPrimary);
        AddCadPanel(body, "Display", new Vector2(0f, 4f), new Vector2(58f, 24f), new Color(0.86f, 0.91f, 0.89f, 1f), TextPrimary);
        AddText(body, "DisplayText", "Hz", new Vector2(0f, 4f), new Vector2(50f, 18f), font, 7f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(body, new Vector2(0f, 62f), new[] { "L1", "L2", "L3" }, font);
        AddTerminalRow(body, new Vector2(0f, -62f), new[] { "U", "V", "W", "STO" }, font);
        AddTerminalRow(body, new Vector2(0f, -36f), new[] { "DI1", "AI1", "AO1" }, font);
    }

    private void DrawMotor(TMP_FontAsset font, Vector2 pos)
    {
        var housing = AddPanel(rootRect, "MotorHousing", pos, new Vector2(84f, 84f), DeviceBody, DashboardUIFactory.GetCircleSprite());
        var outline = housing.gameObject.AddComponent<Outline>();
        outline.effectColor = TextPrimary;
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        var terminalBox = AddCadPanel(rootRect, "MotorTerminalBox", pos + new Vector2(0f, 54f), new Vector2(86f, 26f), DeviceBody, TextPrimary);
        AddText(housing, "Name", "M\n3~", new Vector2(0f, -4f), new Vector2(56f, 42f), font, 14f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(terminalBox, Vector2.zero, new[] { "U", "V", "W", "PTC" }, font);
    }

    private void DrawPlc(TMP_FontAsset font, Vector2 pos)
    {
        var body = AddCadPanel(rootRect, "PLC", pos, new Vector2(150f, 126f), DeviceBody, TextPrimary);
        AddText(body, "Name", "PLC\nS7-1200", new Vector2(0f, 26f), new Vector2(100f, 42f), font, 11f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(body, new Vector2(0f, 52f), new[] { "L+", "M", "DI2", "AI0", "AI1", "PN" }, font);
        AddTerminalRow(body, new Vector2(0f, -52f), new[] { "DQ0", "DQ1", "DQ2", "AQ0" }, font);
        AddPanel(body, "StatusLED", new Vector2(-54f, 8f), new Vector2(10f, 10f), WireControl, DashboardUIFactory.GetCircleSprite());
        AddCadLine("PLCModuleSplit", pos + new Vector2(-36f, -48f), pos + new Vector2(-36f, 48f), TextPrimary, 1f);
    }

    private void DrawHmi(TMP_FontAsset font, Vector2 pos)
    {
        var bezel = AddCadPanel(rootRect, "HMI", pos, new Vector2(122f, 82f), DeviceBody, TextPrimary);
        AddCadPanel(bezel, "Screen", Vector2.zero, new Vector2(92f, 52f), new Color(0.84f, 0.90f, 0.90f, 1f), TextPrimary);
        AddText(bezel, "Name", "HMI\nPN", Vector2.zero, new Vector2(82f, 32f), font, 9.5f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(bezel, new Vector2(0f, -34f), new[] { "PN" }, font);
    }

    private void DrawSignalTower(TMP_FontAsset font, Vector2 pos)
    {
        var basePanel = AddCadPanel(rootRect, "SignalTower", pos, new Vector2(92f, 132f), DeviceBody, TextPrimary);
        AddPanel(basePanel, "Red", new Vector2(0f, 38f), new Vector2(28f, 28f), WireSafety, DashboardUIFactory.GetCircleSprite());
        AddPanel(basePanel, "Amber", new Vector2(0f, 4f), new Vector2(28f, 28f), WireMissing, DashboardUIFactory.GetCircleSprite());
        AddPanel(basePanel, "Green", new Vector2(0f, -30f), new Vector2(28f, 28f), WireEthernet, DashboardUIFactory.GetCircleSprite());
        AddText(basePanel, "Name", "TOWER", new Vector2(0f, -58f), new Vector2(78f, 16f), font, 8.5f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(basePanel, new Vector2(0f, -74f), new[] { "G", "A", "R" }, font);
    }

    private void DrawEStop(TMP_FontAsset font, Vector2 pos)
    {
        var body = AddCadPanel(rootRect, "EStop", pos, new Vector2(108f, 92f), DeviceBody, TextPrimary);
        AddPanel(body, "Button", new Vector2(0f, 12f), new Vector2(44f, 44f), WireSafety, DashboardUIFactory.GetCircleSprite());
        AddText(body, "Name", "E-STOP", new Vector2(0f, -20f), new Vector2(82f, 18f), font, 9f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(body, new Vector2(0f, -38f), new[] { "NC1", "NC2" }, font);
        AddCadLine("EStopNcContact", pos + new Vector2(-20f, 16f), pos + new Vector2(20f, -8f), TextPrimary, 1.5f);
    }

    private void DrawPm2200(TMP_FontAsset font, Vector2 pos)
    {
        var body = AddCadPanel(rootRect, "PM2200", pos, new Vector2(134f, 108f), DeviceBody, TextPrimary);
        AddCadPanel(body, "LCD", new Vector2(0f, 18f), new Vector2(86f, 32f), new Color(0.86f, 0.91f, 0.89f, 1f), TextPrimary);
        AddText(body, "Name", "PM2230\nMETER", new Vector2(0f, 18f), new Vector2(86f, 24f), font, 8.2f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(body, new Vector2(0f, -44f), new[] { "A+", "B-", "V1", "V2", "V3", "I+" }, font);
    }

    private void DrawTerminalStrip(TMP_FontAsset font, string title, Vector2 pos, Vector2 size, string[] terminals)
    {
        var strip = AddCadPanel(rootRect, title.Replace(" ", ""), pos, size, DeviceBody, TextPrimary);
        AddText(strip, "Name", title, new Vector2(0f, 18f), new Vector2(size.x - 18f, 16f), font, 8f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(strip, new Vector2(0f, -10f), terminals, font);
    }

    private void DrawEarthBar(TMP_FontAsset font, Vector2 pos)
    {
        var bar = AddCadPanel(rootRect, "PEBar", pos, new Vector2(116f, 42f), new Color(0.92f, 0.97f, 0.92f, 1f), WirePE);
        AddText(bar, "Name", "PE BAR", new Vector2(0f, 9f), new Vector2(94f, 14f), font, 7.5f, TextAlignmentOptions.Center, TextPrimary);
        AddTerminalRow(bar, new Vector2(0f, -10f), new[] { "PE1", "PE2", "PE3", "PE4" }, font);
    }

    private void DrawLegend(TMP_FontAsset font)
    {
        var legend = AddPanel(rootRect, "Legend", new Vector2(-372f, -244f), new Vector2(210f, 48f), new Color(0.08f, 0.09f, 0.095f, 0.96f));
        AddText(legend, "Title", "WIRE DOMAINS", new Vector2(-50f, 12f), new Vector2(92f, 14f), font, 7.4f, TextAlignmentOptions.Left, TextPrimary);
        AddLegendItem(legend, font, "3~ AC", WirePhase, new Vector2(-78f, -8f));
        AddLegendItem(legend, font, "+24", Wire24V, new Vector2(-18f, -8f));
        AddLegendItem(legend, font, "0V", Wire0V, new Vector2(36f, -8f));
        AddLegendItem(legend, font, "PE", WirePE, new Vector2(88f, -8f));
    }

    private void AddLegendItem(RectTransform parent, TMP_FontAsset font, string label, Color color, Vector2 pos)
    {
        AddPanel(parent, "Swatch_" + label, pos + new Vector2(-16f, 0f), new Vector2(18f, 5f), color);
        AddText(parent, "Label_" + label, label, pos + new Vector2(10f, 0f), new Vector2(44f, 12f), font, 6.5f, TextAlignmentOptions.Left, TextMuted);
    }

    private void AddTerminalRow(RectTransform parent, Vector2 pos, string[] labels, TMP_FontAsset font)
    {
        if (labels == null || labels.Length == 0) return;
        float spacing = Mathf.Min(22f, Mathf.Max(14f, (parent.sizeDelta.x - 18f) / labels.Length));
        float start = -(labels.Length - 1) * spacing * 0.5f;

        for (int i = 0; i < labels.Length; i++)
        {
            Vector2 p = pos + new Vector2(start + i * spacing, 0f);
            AddPanel(parent, "Terminal_" + labels[i], p, new Vector2(12f, 12f), Terminal, DashboardUIFactory.GetCircleSprite());
            var label = AddText(parent, "Label_" + labels[i], labels[i], p + new Vector2(0f, -12f), new Vector2(38f, 11f), font, 5.2f, TextAlignmentOptions.Center, TextMuted);
            label.enableAutoSizing = true;
            label.fontSizeMin = 3.8f;
            label.fontSizeMax = 5.2f;
        }
    }

    private void AddDinRail(Vector2 pos, float width)
    {
        AddPanel(rootRect, "DinRail", pos, new Vector2(width, 10f), Rail);
        for (int i = 0; i < 14; i++)
        {
            float x = -width * 0.45f + i * width * 0.9f / 13f;
            AddPanel(rootRect, "RailSlot" + i, pos + new Vector2(x, 0f), new Vector2(14f, 4f), Backplate);
        }
    }

    private void AddCableDuct(string name, Vector2 pos, Vector2 size)
    {
        var duct = AddPanel(rootRect, name, pos, size, Duct);
        bool vertical = size.y > size.x;
        int count = vertical ? 10 : 22;
        for (int i = 0; i < count; i++)
        {
            float t = -0.45f + i * 0.9f / Mathf.Max(1, count - 1);
            Vector2 slotPos = vertical ? new Vector2(0f, t * size.y) : new Vector2(t * size.x, 0f);
            Vector2 slotSize = vertical ? new Vector2(size.x * 0.55f, 8f) : new Vector2(8f, size.y * 0.55f);
            AddPanel(duct, "DuctSlot" + i, slotPos, slotSize, Backplate);
        }
    }

    private List<Image> CreateWirePath(string name, Vector2[] points, Color color, float thickness)
    {
        var result = new List<Image>();
        if (points == null || points.Length < 2) return result;

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[i + 1];
            Vector2 delta = b - a;
            var rect = MakeRect(name + "_Seg" + i, rootRect, new Vector2(delta.magnitude, thickness), (a + b) * 0.5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = WithMaxAlpha(color, 0.92f);
            image.raycastTarget = false;
            result.Add(image);
        }

        return result;
    }

    private Image CreateTerminalDot(string name, Vector2 pos, Color color)
    {
        var dot = AddPanel(rootRect, name, pos, new Vector2(11f, 11f), color, DashboardUIFactory.GetCircleSprite());
        return dot.GetComponent<Image>();
    }

    private void ResolveRefs()
    {
        if (vfd == null) vfd = FindAnyObjectByType<VFDController>(FindObjectsInactive.Include);
        if (motor == null) motor = FindAnyObjectByType<MotorController>(FindObjectsInactive.Include);
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        if (tower == null) tower = FindAnyObjectByType<SignalTower>(FindObjectsInactive.Include);
        if (eStop == null) eStop = FindAnyObjectByType<EStopButton>(FindObjectsInactive.Include);
    }

    private void BuildWireDefs()
    {
        wireDefs = new WireDef[]
        {
            Wire("W-AC-L123-001\nSUPPLY -> QF1:L1/L2/L3", WirePhase, () => true, new Vector2(-500f, 220f), new Vector2(-500f, 200f), new Vector2(-440f, 200f)),
            Wire("W-AC-L123-002\nQF1:T1/T2/T3 -> VFD:L1/L2/L3", WirePhase, () => vfd != null, new Vector2(-440f, 100f), new Vector2(-440f, 224f), new Vector2(-70f, 224f), new Vector2(-70f, 212f)),
            Wire("W-AC-L1N-003\nQF1:T1/N -> PS1:L1/N", WirePower, () => true, new Vector2(-459f, 100f), new Vector2(-459f, 72f), new Vector2(-292f, 72f), new Vector2(-292f, 200f)),
            Wire("W-MTR-UVW-001\nVFD:U/V/W -> M1:U/V/W", WirePhase, () => vfd != null && motor != null, new Vector2(-91f, 88f), new Vector2(-91f, 248f), new Vector2(199f, 248f), new Vector2(199f, 204f)),
            Wire("W-PE-001\nQF1:PE -> PE BAR", WirePE, () => true, new Vector2(-401f, 100f), new Vector2(-470f, 100f), new Vector2(-470f, -246f), new Vector2(429f, -246f)),
            Wire("W-PE-002\nVFD/MOTOR PE -> PE BAR", WirePE, () => vfd != null && motor != null, new Vector2(-36f, 88f), new Vector2(-36f, -268f), new Vector2(451f, -268f), new Vector2(451f, -246f)),
            Wire("W-24V-001\nPS1:+24 -> X1:5", Wire24V, () => true, new Vector2(-281f, 100f), new Vector2(-281f, 48f), new Vector2(118f, 48f), new Vector2(118f, 8f)),
            Wire("W-0V-001\nPS1:0V -> X1:6", Wire0V, () => true, new Vector2(-259f, 100f), new Vector2(-259f, 28f), new Vector2(140f, 28f), new Vector2(140f, 8f)),
            Wire("W-24V-002\nX1:5 -> PLC L+", Wire24V, () => bench != null, new Vector2(118f, 8f), new Vector2(118f, -40f), new Vector2(-315f, -40f), new Vector2(-315f, -28f)),
            Wire("W-0V-002\nX1:6 -> PLC M", Wire0V, () => bench != null, new Vector2(140f, 8f), new Vector2(140f, -60f), new Vector2(-293f, -60f), new Vector2(-293f, -28f)),
            Wire("W-DI-001\nES1:NC1 -> X2:1 -> PLC:DI2", WireSafety, () => eStop != null && bench != null, new Vector2(-441f, -133f), new Vector2(-441f, -168f), new Vector2(-127f, -168f), new Vector2(-127f, -236f), new Vector2(-271f, -236f), new Vector2(-271f, -28f)),
            Wire("W-STO-001\nES1:NC2 -> VFD:STO", WireSafety, () => eStop != null && vfd != null, new Vector2(-419f, -133f), new Vector2(-488f, -133f), new Vector2(-488f, 264f), new Vector2(-37f, 264f), new Vector2(-37f, 88f)),
            Wire("W-DO-001\nPLC:DQ0 -> X2:2 -> VFD:DI1 RUN", WireControl, () => bench != null && vfd != null, new Vector2(-293f, -132f), new Vector2(-293f, -188f), new Vector2(-105f, -188f), new Vector2(-105f, -236f), new Vector2(-92f, -236f), new Vector2(-92f, 114f)),
            Wire("W-AO-001\nPLC:AQ0 -> X2:6 -> VFD:AI1 SPEED", WireControl, () => bench != null && vfd != null, new Vector2(-227f, -132f), new Vector2(-227f, -260f), new Vector2(-17f, -260f), new Vector2(-17f, -236f), new Vector2(-70f, -236f), new Vector2(-70f, 114f)),
            Wire("W-AI-001\nVFD:AO1 -> PLC:AI1 FEEDBACK", WireControl, () => vfd != null && bench != null, new Vector2(-48f, 114f), new Vector2(-48f, 36f), new Vector2(-227f, 36f), new Vector2(-227f, -28f)),
            Wire("W-AI-002\nM1:PTC -> PLC:AI0", WireControl, () => motor != null && bench != null, new Vector2(243f, 204f), new Vector2(243f, 16f), new Vector2(-249f, 16f), new Vector2(-249f, -28f)),
            Wire("W-DO-002\nPLC:DQ1/DQ2 -> H1:G/A/R", WireControl, () => bench != null && tower != null, new Vector2(-249f, -132f), new Vector2(-249f, -206f), new Vector2(420f, -206f), new Vector2(420f, 8f)),
            Wire("W-MET-V-001\nQF1:T1/T2/T3 -> PM1:V1/V2/V3", WirePhase, () => bench != null, new Vector2(-420f, 100f), new Vector2(-420f, -218f), new Vector2(380f, -218f), new Vector2(380f, -126f)),
            Wire("W-RS485-001\nPM1:A+/B- -> X3:1/2 -> PLC", WireModbus, () => bench != null, new Vector2(332f, -126f), new Vector2(332f, -188f), new Vector2(206f, -188f), new Vector2(206f, -236f), new Vector2(206f, -248f), new Vector2(-205f, -248f), new Vector2(-205f, -28f)),
            Wire("W-ETH-001\nHMI/PLC PN -> Node-RED 200.200.200.177", WireEthernet, () => bench != null, new Vector2(-40f, -114f), new Vector2(-40f, -288f), new Vector2(239f, -288f), new Vector2(239f, -236f)),
        };
    }

    private static WireDef Wire(string label, Color color, System.Func<bool> check, params Vector2[] path)
    {
        return new WireDef { label = label, color = color, check = check, path = path };
    }

    private void UpdateVisibility()
    {
        if (guideRoot == null) return;
        if (manualVisible) { guideRoot.SetActive(true); return; }
        if (alwaysShow) { guideRoot.SetActive(true); return; }
        if (!showOnlyWhenIncomplete) { guideRoot.SetActive(true); return; }
        guideRoot.SetActive(HasMissingConnections());
    }

    public void ToggleManualVisibility()
    {
        if (!built) Rebuild();
        manualVisible = !manualVisible;
        UpdateVisibility();
        UpdateWireStates();
    }

    public void ShowManualVisibility()
    {
        if (!built) Rebuild();
        manualVisible = true;
        UpdateVisibility();
        UpdateWireStates();
    }

    public void HideManualVisibility()
    {
        manualVisible = false;
        UpdateVisibility();
    }

    public void ToggleSurfaceStick()
    {
        stickyToSurface = !stickyToSurface;
        screenSpaceOverlay = !stickyToSurface;
        manualVisible = true;
        built = false;
        Rebuild();
    }

    public bool HasMissingConnections()
    {
        if (wireDefs == null) return true;
        foreach (var wire in wireDefs)
        {
            if (wire.check == null || !wire.check()) return true;
        }
        return false;
    }

    private void UpdateWireStates()
    {
        if (wireDefs == null || wireSegments == null) return;

        int ok = 0;
        for (int i = 0; i < wireDefs.Length; i++)
        {
            bool connected = wireDefs[i].check != null && wireDefs[i].check();
            if (connected) ok++;
            Color color = connected ? wireDefs[i].color : WireMissing;

            if (i < wireSegments.Length && wireSegments[i] != null)
            {
                foreach (var segment in wireSegments[i])
                {
                    if (segment != null) segment.color = WithMaxAlpha(color, 0.92f);
                }
            }

            int dotIndex = i * 2;
            if (dotIndex < wireDots.Length && wireDots[dotIndex] != null) wireDots[dotIndex].color = WithMaxAlpha(color, 0.86f);
            if (dotIndex + 1 < wireDots.Length && wireDots[dotIndex + 1] != null) wireDots[dotIndex + 1].color = WithMaxAlpha(color, 0.86f);
            if (i < wireLabels.Length && wireLabels[i] != null) wireLabels[i].color = connected ? TextPrimary : WireMissing;
        }

        if (modeText != null) modeText.text = stickyToSurface ? "MODE: STUCK TO SURFACE" : "MODE: SCREEN OVERLAY";
        if (stickText != null) stickText.text = stickyToSurface ? "UNSTICK FROM SURFACE" : "STICK TO SURFACE";
    }

    private Vector2 GetWireLabelPosition(int index, Vector2[] path)
    {
        if (path == null || path.Length == 0) return Vector2.zero;
        Vector2 basePos = path[Mathf.Clamp(path.Length / 2, 0, path.Length - 1)];

        switch (index)
        {
            case 0: return new Vector2(-470f, 230f);
            case 1: return new Vector2(-240f, 236f);
            case 2: return new Vector2(-350f, 60f);
            case 3: return new Vector2(40f, 260f);
            case 4: return new Vector2(-110f, -260f);
            case 5: return new Vector2(190f, -282f);
            case 6: return new Vector2(-90f, 58f);
            case 7: return new Vector2(-70f, 36f);
            case 8: return new Vector2(-90f, -44f);
            case 9: return new Vector2(-70f, -72f);
            case 10: return new Vector2(-256f, -180f);
            case 11: return new Vector2(-275f, 278f);
            case 12: return new Vector2(-115f, -178f);
            case 13: return new Vector2(-65f, -274f);
            case 14: return new Vector2(-132f, 48f);
            case 15: return new Vector2(12f, 28f);
            case 16: return new Vector2(94f, -198f);
            case 17: return new Vector2(-10f, -230f);
            case 18: return new Vector2(102f, -172f);
            case 19: return new Vector2(100f, -302f);
            default: return basePos + new Vector2(0f, 16f);
        }
    }

    private void PlaceOnSurface()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            guideRoot.transform.SetParent(transform, false);
            guideRoot.transform.localPosition = localOffset;
            guideRoot.transform.localRotation = Quaternion.identity;
            rootRect.localScale = Vector3.one * canvasScale * Mathf.Max(0.5f, schemaScale);
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        float distance;
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            guideRoot.transform.position = hit.point + hit.normal * 0.02f;
            guideRoot.transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
            distance = Mathf.Max(0.35f, Vector3.Distance(cam.transform.position, guideRoot.transform.position));
        }
        else
        {
            guideRoot.transform.position = cam.transform.position + cam.transform.forward * stickyDistance;
            guideRoot.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
            distance = Mathf.Max(0.35f, stickyDistance);
        }

        rootRect.localScale = Vector3.one * GetWorldScaleToFitView(cam, distance);
    }

    private float GetWorldScaleToFitView(Camera cam, float distance)
    {
        float desiredScale = canvasScale * Mathf.Max(0.5f, schemaScale);
        if (cam == null) return desiredScale;

        float fitRatio = Mathf.Clamp(stickyViewportFit, 0.45f, 0.95f);
        float visibleHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * fitRatio;
        float visibleWidth = visibleHeight * Mathf.Max(0.2f, cam.aspect);
        float fitScale = Mathf.Min(visibleWidth / PanelW, visibleHeight / PanelH);

        return Mathf.Min(desiredScale, Mathf.Max(canvasScale * 0.25f, fitScale));
    }

    private RectTransform AddTransparentSheet(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var rect = MakeRect(name, parent, size, pos);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = PanelBg;
        image.raycastTarget = false;
        return rect;
    }

    private RectTransform AddCadPanel(Transform parent, string name, Vector2 pos, Vector2 size, Color fill, Color outlineColor, float outlineDistance = 1f)
    {
        var rect = MakeRect(name, parent, size, pos);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = WithMaxAlpha(fill, 0.66f);
        image.raycastTarget = false;

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(outlineDistance, -outlineDistance);
        outline.useGraphicAlpha = false;

        return rect;
    }

    private Image AddCadLine(string name, Vector2 a, Vector2 b, Color color, float thickness)
    {
        Vector2 delta = b - a;
        var rect = MakeRect(name, rootRect, new Vector2(delta.magnitude, thickness), (a + b) * 0.5f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var image = rect.gameObject.AddComponent<Image>();
        image.color = WithMaxAlpha(color, 0.92f);
        image.raycastTarget = false;
        return image;
    }

    private void DrawQetGrid()
    {
        Color major = new Color(0.46f, 0.50f, 0.52f, 0.22f);
        Color minor = new Color(0.46f, 0.50f, 0.52f, 0.10f);

        for (float x = -480f; x <= 480f; x += 20f)
        {
            bool isMajor = Mathf.Abs(Mathf.Repeat(x + 480f, 80f)) < 0.1f;
            AddCadLine("GridV_" + x, new Vector2(x, -284f), new Vector2(x, 284f), isMajor ? major : minor, isMajor ? 1.1f : 0.7f);
        }

        for (float y = -280f; y <= 280f; y += 20f)
        {
            bool isMajor = Mathf.Abs(Mathf.Repeat(y + 280f, 80f)) < 0.1f;
            AddCadLine("GridH_" + y, new Vector2(-484f, y), new Vector2(484f, y), isMajor ? major : minor, isMajor ? 1.1f : 0.7f);
        }
    }

    private void DrawBusLabel(TMP_FontAsset font, string label, Vector2 pos)
    {
        var tag = AddCadPanel(rootRect, "BusLabel_" + label.Replace(" ", "_").Replace("/", "_"), pos, new Vector2(122f, 22f), new Color(0.99f, 0.99f, 0.97f, 1f), TextPrimary, 0.9f);
        AddText(tag, "Text", label, Vector2.zero, new Vector2(110f, 14f), font, 7.4f, TextAlignmentOptions.Center, TextPrimary);
    }

    private RectTransform AddPanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
    {
        var rect = MakeRect(name, parent, size, pos);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = sprite != null ? WithMaxAlpha(color, 0.86f) : WithMaxAlpha(color, 0.66f);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
        }
        else
        {
            DashboardUIFactory.ApplyRoundedCorners(image);
        }
        image.raycastTarget = false;
        return rect;
    }

    private static Color WithMaxAlpha(Color color, float alpha)
    {
        color.a = Mathf.Min(color.a, alpha);
        return color;
    }

    private TextMeshProUGUI AddText(Transform parent, string name, string value, Vector2 pos, Vector2 size, TMP_FontAsset font, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var rect = MakeRect(name, parent, size, pos);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        return text;
    }

    private TMP_FontAsset FindFont()
    {
        var existing = GetComponentInChildren<TextMeshProUGUI>(true);
        if (existing != null && existing.font != null) return existing.font;
        return TMP_Settings.defaultFontAsset;
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        if (!Application.isPlaying) eventSystem.hideFlags = HideFlags.DontSaveInEditor;
    }
}
