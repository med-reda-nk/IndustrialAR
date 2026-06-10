using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class MotorFrontPanelBuilder : MonoBehaviour
{
    public BenchSystem bench;
    public TMP_FontAsset fontMain;
    public TMP_FontAsset fontMono;
    public float canvasScale = 0.0001f;
    public bool rebuildOnEnable = true;
    public bool rebuildInEditor = true;
    public bool alignToTargetTop = true;
    public float topOffset = 0.04f;
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

        var size = new Vector2(2000f, 1800f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(gameObject, size, canvasScale);
        var canvas = DashboardUIFactory.EnsureWorldCanvas(gameObject, size, scale);
        if (canvas == null) return;

        if (alignToTargetTop) DashboardUIFactory.AlignToImageTargetTop(transform, topOffset);

        DashboardUIFactory.ClearChildren(transform);

        var root = DashboardUIFactory.CreateRect("Motor_Root", transform, new Vector2(2000f, 1800f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        float motorOffsetX = -160f;
        DashboardUIFactory.CreateImage("Body", root, new Vector2(1460f, 760f), new Vector2(motorOffsetX, 40f), new Color32(0x6B, 0x72, 0x80, 255));
        DashboardUIFactory.CreateImage("RearCap", root, new Vector2(240f, 620f), new Vector2(420f + motorOffsetX, 40f), new Color32(0x5A, 0x61, 0x6F, 255));

        for (int i = 0; i < 10; i++)
        {
            float y = 300f - i * 64f;
            DashboardUIFactory.CreateImage("Fin" + i, root, new Vector2(1020f, 10f), new Vector2(-130f + motorOffsetX, y), new Color32(0x4B, 0x55, 0x63, 255));
        }

        var bearingRoot = DashboardUIFactory.CreateRect("Bearing", root, new Vector2(360f, 360f), new Vector2(-540f + motorOffsetX, 40f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var outer = DashboardUIFactory.CreateImage("OuterRing", bearingRoot, new Vector2(360f, 360f), Vector2.zero, new Color32(0x37, 0x41, 0x51, 255), DashboardUIFactory.GetCircleSprite());
        var inner = DashboardUIFactory.CreateImage("InnerRing", bearingRoot, new Vector2(270f, 270f), Vector2.zero, new Color32(0x4B, 0x55, 0x63, 255), DashboardUIFactory.GetCircleSprite());
        var shaftHole = DashboardUIFactory.CreateImage("ShaftHole", bearingRoot, new Vector2(80f, 80f), Vector2.zero, new Color32(0x1F, 0x29, 0x37, 255), DashboardUIFactory.GetCircleSprite());
        DashboardUIFactory.CreateImage("ShaftProjection", root, new Vector2(180f, 34f), new Vector2(-720f + motorOffsetX, 40f), new Color32(0x4B, 0x55, 0x63, 255));

        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            float radius = 150f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            DashboardUIFactory.CreateImage("Bolt" + i, bearingRoot, new Vector2(20f, 20f), new Vector2(x, y), new Color32(0x1F, 0x29, 0x37, 255), DashboardUIFactory.GetCircleSprite());
        }

        var tempArc = DashboardUIFactory.CreateFilledImage("TempArc", bearingRoot, new Vector2(460f, 460f), Vector2.zero, new Color32(0, 229, 255, 200), Image.FillMethod.Radial360);

        var shaftVisual = DashboardUIFactory.CreateRect("ShaftVisual", bearingRoot, new Vector2(40f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        DashboardUIFactory.CreateImage("ShaftLineA", shaftVisual, new Vector2(40f, 6f), Vector2.zero, Color.white);
        DashboardUIFactory.CreateImage("ShaftLineB", shaftVisual, new Vector2(6f, 40f), Vector2.zero, Color.white);

        var directionArrow = DashboardUIFactory.CreateText("DirectionArrow", bearingRoot, ">", fontMain, 40f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(directionArrow.rectTransform, new Vector2(60f, 60f), new Vector2(0f, -120f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var rpmText = DashboardUIFactory.CreateText("RPM", bearingRoot, "0000", fontMono, 48f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(rpmText.rectTransform, new Vector2(200f, 60f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var terminal = DashboardUIFactory.CreateImage("TerminalBox", root, new Vector2(260f, 120f), new Vector2(140f + motorOffsetX, 410f), new Color32(0x4B, 0x55, 0x63, 255));
        var terminalLabel = DashboardUIFactory.CreateText("TerminalLabel", terminal.transform, "3~ 400V 50Hz", fontMain, 17f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(terminalLabel.rectTransform, new Vector2(238f, 30f), new Vector2(0f, 22f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        DashboardUIFactory.CreateImage("L1", terminal.transform, new Vector2(16f, 16f), new Vector2(-40f, -30f), new Color32(118, 148, 152, 255), DashboardUIFactory.GetCircleSprite());
        DashboardUIFactory.CreateImage("L2", terminal.transform, new Vector2(16f, 16f), new Vector2(0f, -30f), new Color32(0, 0, 0, 255), DashboardUIFactory.GetCircleSprite());
        DashboardUIFactory.CreateImage("L3", terminal.transform, new Vector2(16f, 16f), new Vector2(40f, -30f), new Color32(90, 90, 90, 255), DashboardUIFactory.GetCircleSprite());

        var nameplate = DashboardUIFactory.CreateImage("Nameplate", root, new Vector2(540f, 70f), new Vector2(-40f + motorOffsetX, -250f), new Color32(40, 40, 40, 255));
        var nameText = DashboardUIFactory.CreateText("NameplateText", nameplate.transform, "ETS 0.37 kW 400 V 50 Hz 1450 rpm D/Y", fontMain, 16f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(nameText.rectTransform, new Vector2(510f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var torqueText = DashboardUIFactory.CreateText("Torque", root, "0.0 Nm", fontMain, 22f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(torqueText.rectTransform, new Vector2(240f, 34f), new Vector2(-540f + motorOffsetX, 260f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var vibrationText = DashboardUIFactory.CreateText("Vibration", root, "Vibration: 0.0 mm/s", fontMain, 20f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(vibrationText.rectTransform, new Vector2(360f, 30f), new Vector2(-20f + motorOffsetX, -120f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var faultBorder = DashboardUIFactory.CreateImage("FaultBorder", root, new Vector2(1480f, 780f), new Vector2(motorOffsetX, 40f), new Color32(213, 0, 0, 60));
        var outline = faultBorder.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(213, 0, 0, 255);
        outline.effectDistance = new Vector2(6f, -6f);

        var arPanel = DashboardUIFactory.CreateRect("AR_Controls", root, new Vector2(320f, 392f), new Vector2(390f, 16f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        DashboardUIFactory.CreateImage("VFDEnclosure", arPanel, new Vector2(320f, 392f), Vector2.zero, new Color32(34, 40, 48, 245));
        var vfdTitle = DashboardUIFactory.CreateText("VFDTitle", arPanel, "VARIATEUR", fontMain, 20f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(vfdTitle.rectTransform, new Vector2(280f, 34f), new Vector2(0f, 160f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var frequencyText = DashboardUIFactory.CreateText("VfdFrequency", arPanel, "Freq: 0.0 Hz", fontMain, 18f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(frequencyText.rectTransform, new Vector2(280f, 28f), new Vector2(0f, 132f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var startBtn = DashboardUIFactory.CreateButton("Start", arPanel, new Vector2(124f, 38f), new Vector2(-68f, 106f), new Color32(54, 78, 82, 255), "START", fontMain, 15f, Color.white);
        var stopBtn = DashboardUIFactory.CreateButton("Stop", arPanel, new Vector2(124f, 38f), new Vector2(68f, 106f), new Color32(120, 0, 0, 255), "STOP", fontMain, 15f, Color.white);
        var fwdBtn = DashboardUIFactory.CreateButton("FWD", arPanel, new Vector2(124f, 38f), new Vector2(-68f, 50f), new Color32(60, 60, 60, 255), "FWD", fontMain, 15f, Color.white);
        var revBtn = DashboardUIFactory.CreateButton("REV", arPanel, new Vector2(124f, 38f), new Vector2(68f, 50f), new Color32(60, 60, 60, 255), "REV", fontMain, 15f, Color.white);
        var upBtn = DashboardUIFactory.CreateButton("FreqUp", arPanel, new Vector2(124f, 38f), new Vector2(-68f, -6f), new Color32(60, 60, 60, 255), "+5Hz", fontMain, 15f, Color.white);
        var downBtn = DashboardUIFactory.CreateButton("FreqDown", arPanel, new Vector2(124f, 38f), new Vector2(68f, -6f), new Color32(60, 60, 60, 255), "-5Hz", fontMain, 15f, Color.white);

        var statusText = DashboardUIFactory.CreateText("MotorStatus", arPanel, "IDLE", fontMain, 20f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(statusText.rectTransform, new Vector2(280f, 30f), new Vector2(0f, -72f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var effBar = DashboardUIFactory.CreateImage("EffBar", arPanel, new Vector2(252f, 14f), new Vector2(0f, -108f), new Color32(0, 229, 255, 255));
        effBar.type = Image.Type.Filled;
        effBar.fillMethod = Image.FillMethod.Horizontal;
        effBar.fillAmount = 0.3f;

        DashboardUIFactory.ApplyCyberFuturisticSkin(root, DashboardUIFactory.CyberStyle.Motor, false);

        var dashboard = GetComponent<MotorDashboard>();
        if (dashboard == null) dashboard = gameObject.AddComponent<MotorDashboard>();
        dashboard.bench = bench;
        dashboard.shaftVisual = shaftVisual;
        dashboard.temperatureArc = tempArc;
        dashboard.directionArrow = directionArrow.rectTransform;
        dashboard.textRPM = rpmText;
        dashboard.textTorque = torqueText;
        dashboard.textVibration = vibrationText;
        dashboard.textFrequency = frequencyText;
        dashboard.motorFaultBorder = faultBorder.gameObject;
        dashboard.textMotorStatus = statusText;
        dashboard.barEfficiency = effBar;

        startBtn.onClick.RemoveAllListeners();
        startBtn.onClick.AddListener(dashboard.OnStartMotor);
        stopBtn.onClick.RemoveAllListeners();
        stopBtn.onClick.AddListener(dashboard.OnStopMotor);
        upBtn.onClick.RemoveAllListeners();
        upBtn.onClick.AddListener(dashboard.OnFreqUp);
        downBtn.onClick.RemoveAllListeners();
        downBtn.onClick.AddListener(dashboard.OnFreqDown);
        fwdBtn.onClick.RemoveAllListeners();
        fwdBtn.onClick.AddListener(dashboard.OnToggleDirection);
        revBtn.onClick.RemoveAllListeners();
        revBtn.onClick.AddListener(dashboard.OnToggleDirection);
    }
}
