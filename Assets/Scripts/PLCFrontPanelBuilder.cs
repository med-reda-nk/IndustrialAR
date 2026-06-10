using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class PLCFrontPanelBuilder : MonoBehaviour
{
    public BenchSystem bench;
    public TMP_FontAsset fontMain;
    public float canvasScale = 0.0001f;
    public bool rebuildOnEnable = true;
    public bool rebuildInEditor = true;
    public bool alignToTargetTop = true;
    public float topOffset = 0.04f;
    public bool showFaultPanel = false;
    public bool showExtendedOverlay = false;
    public bool built;

    public void OnEnable()
    {
        if (rebuildOnEnable && (Application.isPlaying || rebuildInEditor)) Build(true);
    }

    public void OnValidate()
    {
        built = false;
    }

    public void Build(bool force)
    {
        if (built && !force) return;
        built = true;

        var size = new Vector2(1100f, 1000f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(gameObject, size, canvasScale);
        var canvas = DashboardUIFactory.EnsureWorldCanvas(gameObject, size, scale);
        if (canvas == null) return;

        if (alignToTargetTop) DashboardUIFactory.AlignToImageTargetTop(transform, topOffset);

        DashboardUIFactory.ClearChildren(transform);

        var root = DashboardUIFactory.CreateRect("PLC_Root", transform, new Vector2(1100f, 1000f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        DashboardUIFactory.CreateImage("Face", root, new Vector2(1100f, 1000f), Vector2.zero, new Color32(70, 70, 70, 255));

        var header = DashboardUIFactory.CreateImage("Header", root, new Vector2(1100f, 110f), new Vector2(0f, 445f), new Color32(50, 50, 50, 255));
        var siemens = DashboardUIFactory.CreateText("SIEMENS", header.transform, "SIEMENS", fontMain, 26f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(siemens.rectTransform, new Vector2(400f, 40f), new Vector2(-320f, 20f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        var simatic = DashboardUIFactory.CreateText("SIMATIC", header.transform, "SIMATIC S7-1200", fontMain, 24f, TextAlignmentOptions.Right, Color.white);
        DashboardUIFactory.SetRect(simatic.rectTransform, new Vector2(500f, 40f), new Vector2(320f, 20f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        int total = 17;
        float ledSize = 26f;
        float spacing = 55f;
        float startX = -((total - 1) * spacing) * 0.5f;
        float rowY = 250f;

        var runLed = CreateLed(root, "RUN", startX + spacing * 0, rowY, ledSize, fontMain);
        var errLed = CreateLed(root, "ERR", startX + spacing * 1, rowY, ledSize, fontMain);
        var maintLed = CreateLed(root, "MAINT", startX + spacing * 2, rowY, ledSize, fontMain);

        var di = new Image[8];
        for (int i = 0; i < 8; i++)
        {
            di[i] = CreateLed(root, "I0." + i, startX + spacing * (3 + i), rowY, ledSize, fontMain);
        }

        var dq = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            dq[i] = CreateLed(root, "Q0." + i, startX + spacing * (11 + i), rowY, ledSize, fontMain);
        }

        var ether = DashboardUIFactory.CreateImage("Ethernet", root, new Vector2(200f, 80f), new Vector2(-360f, 120f), new Color32(60, 60, 60, 255));
        DashboardUIFactory.CreateImage("LinkLed", ether.transform, new Vector2(14f, 14f), new Vector2(60f, -20f), new Color32(198, 214, 214, 255), DashboardUIFactory.GetCircleSprite());
        DashboardUIFactory.CreateImage("ActLed", ether.transform, new Vector2(14f, 14f), new Vector2(80f, -20f), new Color32(118, 148, 152, 255), DashboardUIFactory.GetCircleSprite());

        var memory = DashboardUIFactory.CreateImage("Memory", root, new Vector2(160f, 80f), new Vector2(360f, 120f), new Color32(60, 60, 60, 255));

        var terminal = DashboardUIFactory.CreateImage("TerminalBlock", root, new Vector2(900f, 140f), new Vector2(0f, -330f), new Color32(45, 45, 45, 255));
        for (int i = 0; i < 12; i++)
        {
            float x = -400f + i * 70f;
            DashboardUIFactory.CreateImage("Term" + i, terminal.transform, new Vector2(20f, 20f), new Vector2(x, 20f), new Color32(30, 30, 30, 255));
            DashboardUIFactory.CreateImage("TermB" + i, terminal.transform, new Vector2(20f, 20f), new Vector2(x, -20f), new Color32(30, 30, 30, 255));
        }

        var switchRoot = DashboardUIFactory.CreateRect("ModeSwitch", root, new Vector2(260f, 80f), new Vector2(350f, -210f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var runBtn = DashboardUIFactory.CreateButton("RunBtn", switchRoot, new Vector2(80f, 60f), new Vector2(-90f, 0f), new Color32(54, 78, 82, 255), "RUN", fontMain, 16f, Color.white);
        var stopBtn = DashboardUIFactory.CreateButton("StopBtn", switchRoot, new Vector2(80f, 60f), new Vector2(0f, 0f), new Color32(42, 42, 42, 255), "STOP", fontMain, 16f, Color.white);
        var mprstBtn = DashboardUIFactory.CreateButton("MPRST", switchRoot, new Vector2(80f, 60f), new Vector2(90f, 0f), new Color32(120, 0, 0, 255), "MPRST", fontMain, 14f, Color.white);

        GameObject faultPanel = null;
        TextMeshProUGUI faultText = null;
        Button resetBtn = null;
        Image resetGlow = null;
        if (showFaultPanel)
        {
            faultPanel = DashboardUIFactory.CreateImage("FaultPanel", root, new Vector2(900f, 70f), new Vector2(0f, -250f), new Color32(30, 30, 30, 255)).gameObject;
            faultText = DashboardUIFactory.CreateText("FaultText", faultPanel.transform, "No fault", fontMain, 18f, TextAlignmentOptions.Left, new Color32(255, 200, 200, 255));
            DashboardUIFactory.SetRect(faultText.rectTransform, new Vector2(600f, 40f), new Vector2(-180f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            resetBtn = DashboardUIFactory.CreateButton("ResetFault", faultPanel.transform, new Vector2(160f, 40f), new Vector2(320f, 0f), new Color32(80, 0, 0, 255), "RESET", fontMain, 16f, Color.white);
            resetGlow = DashboardUIFactory.CreateImage("ResetGlow", faultPanel.transform, new Vector2(180f, 50f), new Vector2(320f, 0f), new Color32(213, 0, 0, 120));
            resetGlow.transform.SetAsFirstSibling();
        }

        var arRoot = DashboardUIFactory.CreateRect("AR_Overlays", root, new Vector2(260f, 86f), new Vector2(345f, 350f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        DashboardUIFactory.CreateImage("ScanDisplay", arRoot, new Vector2(260f, 86f), Vector2.zero, new Color32(30, 30, 30, 255));
        var scanText = DashboardUIFactory.CreateText("Scan", arRoot, "0.00 ms", fontMain, 18f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(scanText.rectTransform, new Vector2(230f, 28f), new Vector2(0f, 16f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var frequencyText = DashboardUIFactory.CreateText("Frequency", arRoot, "FREQ 0.0 Hz", fontMain, 18f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(frequencyText.rectTransform, new Vector2(230f, 28f), new Vector2(0f, -18f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        TextMeshProUGUI memText = null;
        TextMeshProUGUI cycleText = null;
        Image loadBar = null;

        DashboardUIFactory.ApplyCyberFuturisticSkin(root, DashboardUIFactory.CyberStyle.PLC);

        var dashboard = GetComponent<PLCDashboard>();
        if (dashboard == null) dashboard = gameObject.AddComponent<PLCDashboard>();
        dashboard.bench = bench;
        dashboard.ledRunStop = runLed;
        dashboard.ledError = errLed;
        dashboard.ledMaint = maintLed;
        dashboard.ledDI = di;
        dashboard.ledDQ = dq;
        dashboard.faultPanel = faultPanel;
        dashboard.textFaultDetails = faultText;
        dashboard.buttonResetFault = resetBtn;
        dashboard.resetFaultGlow = resetGlow;
        dashboard.textScanCycle = scanText;
        dashboard.textFrequency = frequencyText;
        dashboard.barCPULoad = loadBar;
        dashboard.textMemoryUsed = memText;
        dashboard.textCycleCount = cycleText;

        runBtn.onClick.RemoveAllListeners();
        runBtn.onClick.AddListener(dashboard.OnRunSwitch);
        stopBtn.onClick.RemoveAllListeners();
        stopBtn.onClick.AddListener(dashboard.OnStopSwitch);
        mprstBtn.onClick.RemoveAllListeners();
        mprstBtn.onClick.AddListener(dashboard.OnResetFault);
        if (resetBtn != null)
        {
            resetBtn.onClick.RemoveAllListeners();
            resetBtn.onClick.AddListener(dashboard.OnResetFault);
        }
    }

    public Image CreateLed(RectTransform parent, string label, float x, float y, float size, TMP_FontAsset font)
    {
        var holder = DashboardUIFactory.CreateRect("Led_" + label, parent, new Vector2(size, size + 22f), new Vector2(x, y), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var led = DashboardUIFactory.CreateImage("LED", holder, new Vector2(size, size), new Vector2(0f, 8f), new Color32(26, 26, 26, 255), DashboardUIFactory.GetCircleSprite());
        var text = DashboardUIFactory.CreateText("Label", holder, label, font, 12f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(text.rectTransform, new Vector2(60f, 18f), new Vector2(0f, -12f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        return led;
    }
}
