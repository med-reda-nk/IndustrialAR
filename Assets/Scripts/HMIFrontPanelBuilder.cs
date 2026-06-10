using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class HMIFrontPanelBuilder : MonoBehaviour
{
    public BenchSystem bench;
    public TMP_FontAsset fontMain;
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

        var size = new Vector2(2120f, 1430f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(gameObject, size, canvasScale);
        var canvas = DashboardUIFactory.EnsureWorldCanvas(gameObject, size, scale);
        if (canvas == null) return;

        if (alignToTargetTop) DashboardUIFactory.AlignToImageTargetTop(transform, topOffset);

        DashboardUIFactory.ClearChildren(transform);

        var root = DashboardUIFactory.CreateRect("HMI_Root", transform, new Vector2(2120f, 1430f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        DashboardUIFactory.CreateImage("Bezel", root, new Vector2(2120f, 1430f), Vector2.zero, new Color32(45, 45, 45, 255));

        var screen = DashboardUIFactory.CreateImage("Screen", root, new Vector2(1800f, 1100f), new Vector2(0f, 80f), new Color32(20, 20, 20, 255));
        screen.gameObject.AddComponent<RectMask2D>();
        var topBar = DashboardUIFactory.CreateImage("TopBar", screen.transform, new Vector2(1760f, 80f), new Vector2(0f, 470f), new Color32(35, 35, 35, 255));
        var project = DashboardUIFactory.CreateText("Project", topBar.transform, "AutomationLab", fontMain, 22f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(project.rectTransform, new Vector2(400f, 40f), new Vector2(-620f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        var dateTime = DashboardUIFactory.CreateText("Time", topBar.transform, "00:00:00", fontMain, 22f, TextAlignmentOptions.Right, Color.white);
        DashboardUIFactory.SetRect(dateTime.rectTransform, new Vector2(300f, 40f), new Vector2(620f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        var connIcon = DashboardUIFactory.CreateImage("Conn", topBar.transform, new Vector2(18f, 18f), new Vector2(540f, 0f), new Color32(0, 229, 255, 255), DashboardUIFactory.GetCircleSprite());

        var title = DashboardUIFactory.CreateText("ScreenTitle", screen.transform, "MAIN", fontMain, 26f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(title.rectTransform, new Vector2(600f, 40f), new Vector2(-600f, 400f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

        var alarmBar = DashboardUIFactory.CreateImage("AlarmBar", screen.transform, new Vector2(1760f, 60f), new Vector2(0f, -470f), new Color32(102, 102, 102, 255));
        var alarmText = DashboardUIFactory.CreateText("AlarmText", alarmBar.transform, "ALARMS CLEAR", fontMain, 20f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(alarmText.rectTransform, new Vector2(600f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var content = DashboardUIFactory.CreateRect("Content", screen.transform, new Vector2(1500f, 560f), new Vector2(0f, 20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        content.gameObject.AddComponent<RectMask2D>();

        var screenMain = DashboardUIFactory.CreateRect("ScreenMain", content, new Vector2(1500f, 560f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textMain = DashboardUIFactory.CreateText("MainStatus", screenMain, "System: RUNNING", fontMain, 28f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textMain.rectTransform, new Vector2(900f, 92f), new Vector2(-260f, 160f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var screenMotor = DashboardUIFactory.CreateRect("ScreenMotor", content, new Vector2(1500f, 560f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textMotorRPM = DashboardUIFactory.CreateText("MotorRPM", screenMotor, "RPM: 0", fontMain, 28f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textMotorRPM.rectTransform, new Vector2(700f, 40f), new Vector2(-300f, 150f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textMotorTarget = DashboardUIFactory.CreateText("MotorTarget", screenMotor, "FREQ: 0.0 Hz", fontMain, 24f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textMotorTarget.rectTransform, new Vector2(700f, 40f), new Vector2(-300f, 98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var screenPower = DashboardUIFactory.CreateRect("ScreenPower", content, new Vector2(1500f, 560f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textPowerVoltage = DashboardUIFactory.CreateText("PowerVoltage", screenPower, "V: 0.0 V", fontMain, 28f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textPowerVoltage.rectTransform, new Vector2(700f, 40f), new Vector2(-300f, 150f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textPowerCurrent = DashboardUIFactory.CreateText("PowerCurrent", screenPower, "I: 0.0 A", fontMain, 28f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textPowerCurrent.rectTransform, new Vector2(700f, 40f), new Vector2(-300f, 98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textPowerWatts = DashboardUIFactory.CreateText("PowerWatts", screenPower, "P: 0.000 kW", fontMain, 28f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textPowerWatts.rectTransform, new Vector2(760f, 40f), new Vector2(-270f, 46f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var screenDiag = DashboardUIFactory.CreateRect("ScreenDiagnostics", content, new Vector2(1500f, 560f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textDiagFault = DashboardUIFactory.CreateText("DiagFault", screenDiag, "Fault: NONE", fontMain, 24f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textDiagFault.rectTransform, new Vector2(900f, 40f), new Vector2(-220f, 150f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textDiagCPU = DashboardUIFactory.CreateText("DiagCPU", screenDiag, "CPU: 0%", fontMain, 24f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textDiagCPU.rectTransform, new Vector2(900f, 40f), new Vector2(-220f, 98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textDiagIO = DashboardUIFactory.CreateText("DiagIO", screenDiag, "DI: 00000000\nDQ: 00000000", fontMain, 22f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textDiagIO.rectTransform, new Vector2(900f, 80f), new Vector2(-220f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var screenAlarms = DashboardUIFactory.CreateRect("ScreenAlarms", content, new Vector2(1500f, 560f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var textAlarmList = DashboardUIFactory.CreateText("AlarmList", screenAlarms, "No active alarms", fontMain, 24f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(textAlarmList.rectTransform, new Vector2(900f, 200f), new Vector2(-220f, 130f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var ackBtn = DashboardUIFactory.CreateButton("AckButton", screenAlarms, new Vector2(140f, 40f), new Vector2(520f, -210f), new Color32(120, 0, 0, 255), "ACK", fontMain, 18f, Color.white);

        var fKeyRow = DashboardUIFactory.CreateRect("FKeys", screen.transform, new Vector2(1660f, 74f), new Vector2(0f, -388f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var f1 = DashboardUIFactory.CreateButton("F1", fKeyRow, new Vector2(150f, 52f), new Vector2(-560f, 0f), new Color32(60, 60, 60, 255), "F1", fontMain, 18f, Color.white);
        var f2 = DashboardUIFactory.CreateButton("F2", fKeyRow, new Vector2(150f, 52f), new Vector2(-400f, 0f), new Color32(60, 60, 60, 255), "F2", fontMain, 18f, Color.white);
        var f3 = DashboardUIFactory.CreateButton("F3", fKeyRow, new Vector2(150f, 52f), new Vector2(-240f, 0f), new Color32(60, 60, 60, 255), "F3", fontMain, 18f, Color.white);
        var f4 = DashboardUIFactory.CreateButton("F4", fKeyRow, new Vector2(150f, 52f), new Vector2(-80f, 0f), new Color32(60, 60, 60, 255), "F4", fontMain, 18f, Color.white);
        var f5 = DashboardUIFactory.CreateButton("F5", fKeyRow, new Vector2(150f, 52f), new Vector2(80f, 0f), new Color32(60, 60, 60, 255), "F5", fontMain, 18f, Color.white);
        var f6 = DashboardUIFactory.CreateButton("F6", fKeyRow, new Vector2(150f, 52f), new Vector2(240f, 0f), new Color32(60, 60, 60, 255), "F6", fontMain, 18f, Color.white);
        var f7 = DashboardUIFactory.CreateButton("F7", fKeyRow, new Vector2(150f, 52f), new Vector2(400f, 0f), new Color32(60, 60, 60, 255), "F7", fontMain, 18f, Color.white);
        var f8 = DashboardUIFactory.CreateButton("F8", fKeyRow, new Vector2(150f, 52f), new Vector2(560f, 0f), new Color32(60, 60, 60, 255), "F8", fontMain, 18f, Color.white);

        var navLeft = DashboardUIFactory.CreateRect("NavLeft", screen.transform, new Vector2(90f, 250f), new Vector2(-820f, -20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        DashboardUIFactory.CreateButton("Up", navLeft, new Vector2(80f, 60f), new Vector2(0f, 80f), new Color32(60, 60, 60, 255), "^", fontMain, 18f, Color.white);
        DashboardUIFactory.CreateButton("Enter", navLeft, new Vector2(80f, 60f), new Vector2(0f, 0f), new Color32(60, 60, 60, 255), "OK", fontMain, 18f, Color.white);
        DashboardUIFactory.CreateButton("Down", navLeft, new Vector2(80f, 60f), new Vector2(0f, -80f), new Color32(60, 60, 60, 255), "v", fontMain, 18f, Color.white);

        var navRight = DashboardUIFactory.CreateRect("NavRight", screen.transform, new Vector2(90f, 250f), new Vector2(820f, -20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        DashboardUIFactory.CreateButton("ESC", navRight, new Vector2(80f, 60f), new Vector2(0f, 80f), new Color32(60, 60, 60, 255), "ESC", fontMain, 16f, Color.white);
        DashboardUIFactory.CreateImage("Led1", navRight, new Vector2(16f, 16f), new Vector2(0f, 0f), new Color32(26, 26, 26, 255), DashboardUIFactory.GetCircleSprite());
        DashboardUIFactory.CreateImage("Led2", navRight, new Vector2(16f, 16f), new Vector2(0f, -40f), new Color32(26, 26, 26, 255), DashboardUIFactory.GetCircleSprite());

        DashboardUIFactory.ApplyCyberFuturisticSkin(root, DashboardUIFactory.CyberStyle.HMI);

        var dashboard = GetComponent<HMIDashboard>();
        if (dashboard == null) dashboard = gameObject.AddComponent<HMIDashboard>();
        dashboard.bench = bench;
        dashboard.screenMain = screenMain.gameObject;
        dashboard.screenMotor = screenMotor.gameObject;
        dashboard.screenPower = screenPower.gameObject;
        dashboard.screenDiagnostics = screenDiag.gameObject;
        dashboard.screenAlarms = screenAlarms.gameObject;
        dashboard.textScreenTitle = title;
        dashboard.textDateTime = dateTime;
        dashboard.connectionIcon = connIcon;
        dashboard.alarmBar = alarmBar.gameObject;
        dashboard.alarmBarImage = alarmBar;
        dashboard.textAlarmCount = alarmText;
        dashboard.textMainStatus = textMain;
        dashboard.textMotorRPM = textMotorRPM;
        dashboard.textMotorTarget = textMotorTarget;
        dashboard.textPowerVoltage = textPowerVoltage;
        dashboard.textPowerCurrent = textPowerCurrent;
        dashboard.textPowerWatts = textPowerWatts;
        dashboard.textDiagFault = textDiagFault;
        dashboard.textDiagCPU = textDiagCPU;
        dashboard.textDiagIO = textDiagIO;
        dashboard.textAlarmList = textAlarmList;

        f1.onClick.RemoveAllListeners();
        f1.onClick.AddListener(dashboard.OnF1);
        f2.onClick.RemoveAllListeners();
        f2.onClick.AddListener(dashboard.OnF2);
        f3.onClick.RemoveAllListeners();
        f3.onClick.AddListener(dashboard.OnF3);
        f4.onClick.RemoveAllListeners();
        f4.onClick.AddListener(dashboard.OnF4);
        f5.onClick.RemoveAllListeners();
        f5.onClick.AddListener(dashboard.OnF5);
        f6.onClick.RemoveAllListeners();
        f6.onClick.AddListener(dashboard.OnF6);
        f7.onClick.RemoveAllListeners();
        f7.onClick.AddListener(dashboard.OnF7);
        f8.onClick.RemoveAllListeners();
        f8.onClick.AddListener(dashboard.OnF8);
        ackBtn.onClick.RemoveAllListeners();
        ackBtn.onClick.AddListener(dashboard.OnAckAlarms);
    }
}
