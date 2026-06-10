using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[ExecuteAlways]
public class PM2200FrontPanelBuilder : MonoBehaviour
{
    private static readonly Color32 FaceLightGrey = new Color32(72, 72, 72, 255);
    private static readonly Color32 DarkGrey = new Color32(24, 24, 24, 255);
    private static readonly Color32 MidGrey = new Color32(42, 42, 42, 255);
    private static readonly Color32 LcdGrey = new Color32(24, 52, 56, 255);
    private static readonly Color32 LcdText = new Color32(213, 238, 232, 255);
    private static readonly Color32 OverlayGrey = new Color32(36, 36, 36, 220);

    public BenchSystem bench;
    public TMP_FontAsset fontHeader;
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

        var size = new Vector2(960f, 960f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(gameObject, size, canvasScale);
        var canvas = DashboardUIFactory.EnsureWorldCanvas(gameObject, size, scale);
        if (canvas == null) return;

        if (alignToTargetTop) DashboardUIFactory.AlignToImageTargetTop(transform, topOffset);

        DashboardUIFactory.ClearChildren(transform);

        var root = DashboardUIFactory.CreateRect("PM2200_Root", transform, new Vector2(960f, 960f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var face = DashboardUIFactory.CreateImage("Face", root, new Vector2(960f, 960f), Vector2.zero, FaceLightGrey);

        var header = DashboardUIFactory.CreateImage("Header", root, new Vector2(960f, 80f), new Vector2(0f, 440f), DarkGrey);
        var logoText = DashboardUIFactory.CreateText("LogoText", header.transform, "Schneider Electric", fontHeader, 22f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(logoText.rectTransform, new Vector2(450f, 60f), new Vector2(-210f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        var modelText = DashboardUIFactory.CreateText("ModelText", header.transform, "EasyLogic PM2200", fontHeader, 22f, TextAlignmentOptions.Right, Color.white);
        DashboardUIFactory.SetRect(modelText.rectTransform, new Vector2(450f, 60f), new Vector2(210f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));

        var lcdFrame = DashboardUIFactory.CreateImage("LCDFrame", root, new Vector2(916f, 596f), new Vector2(0f, 154f), DarkGrey);
        var lcd = DashboardUIFactory.CreateImage("LCD", lcdFrame.transform, new Vector2(892f, 572f), Vector2.zero, LcdGrey);
        lcd.gameObject.AddComponent<RectMask2D>();
        DashboardUIFactory.ApplyCyberOverlay(lcd.rectTransform, DashboardUIFactory.CyberStyle.PowerMeterGrey, true, false);

        var title = DashboardUIFactory.CreateText("LCD_Title", lcd.transform, "Recapitulatif", fontMono, 22f, TextAlignmentOptions.Center, LcdText);
        DashboardUIFactory.SetRect(title.rectTransform, new Vector2(820f, 34f), new Vector2(0f, 236f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        var row1Label = DashboardUIFactory.CreateText("Row1Label", lcd.transform, "Vmoy", fontMono, 24f, TextAlignmentOptions.Left, LcdText);
        var row1Value = DashboardUIFactory.CreateText("Row1Value", lcd.transform, "0.0000 V", fontMono, 24f, TextAlignmentOptions.Right, LcdText);
        PositionLCDRow(row1Label, row1Value, 142f);

        var row2Label = DashboardUIFactory.CreateText("Row2Label", lcd.transform, "Imoy", fontMono, 24f, TextAlignmentOptions.Left, LcdText);
        var row2Value = DashboardUIFactory.CreateText("Row2Value", lcd.transform, "0.0000 mA", fontMono, 24f, TextAlignmentOptions.Right, LcdText);
        PositionLCDRow(row2Label, row2Value, 82f);

        var row3Label = DashboardUIFactory.CreateText("Row3Label", lcd.transform, "Ptot", fontMono, 24f, TextAlignmentOptions.Left, LcdText);
        var row3Value = DashboardUIFactory.CreateText("Row3Value", lcd.transform, "0.00000 kW", fontMono, 24f, TextAlignmentOptions.Right, LcdText);
        PositionLCDRow(row3Label, row3Value, 22f);

        var row4Label = DashboardUIFactory.CreateText("Row4Label", lcd.transform, "E.Fni", fontMono, 24f, TextAlignmentOptions.Left, LcdText);
        var row4Value = DashboardUIFactory.CreateText("Row4Value", lcd.transform, "161.0 Wh", fontMono, 24f, TextAlignmentOptions.Right, LcdText);
        PositionLCDRow(row4Label, row4Value, -38f);

        var navBar = DashboardUIFactory.CreateText("LCD_Nav", lcd.transform, "I    U-U    POS    >", fontMono, 20f, TextAlignmentOptions.Center, LcdText);
        DashboardUIFactory.SetRect(navBar.rectTransform, new Vector2(820f, 24f), new Vector2(0f, -250f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        float buttonY = -322f;
        float spacing = 150f;
        float buttonSize = 96f;
        var btnPrev = DashboardUIFactory.CreateCircleButton("BtnPrev", root, buttonSize, new Vector2(-1.5f * spacing, buttonY), MidGrey, "<", fontHeader, 30f, Color.white);
        var btnDown = DashboardUIFactory.CreateCircleButton("BtnDown", root, buttonSize, new Vector2(-0.5f * spacing, buttonY), MidGrey, "v", fontHeader, 30f, Color.white);
        var btnSelect = DashboardUIFactory.CreateCircleButton("BtnSelect", root, buttonSize, new Vector2(0.5f * spacing, buttonY), MidGrey, "o", fontHeader, 30f, Color.white);
        var btnNext = DashboardUIFactory.CreateCircleButton("BtnNext", root, buttonSize, new Vector2(1.5f * spacing, buttonY), MidGrey, ">", fontHeader, 30f, Color.white);

        var led = DashboardUIFactory.CreateImage("StatusLED", root, new Vector2(22f, 22f), new Vector2(420f, -322f), new Color32(0, 229, 255, 255), DashboardUIFactory.GetCircleSprite());

        var arRoot = DashboardUIFactory.CreateRect("AR_Overlays", lcd.transform, new Vector2(300f, 176f), new Vector2(258f, 44f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var arPanel = DashboardUIFactory.CreateImage("ARPanel", arRoot, new Vector2(300f, 176f), Vector2.zero, OverlayGrey);
        var badge = DashboardUIFactory.CreateImage("BadgeBG", arRoot, new Vector2(120f, 30f), new Vector2(0f, 58f), new Color32(210, 210, 210, 180));
        var badgeText = DashboardUIFactory.CreateText("BadgeText", badge.transform, "GOOD", fontHeader, 20f, TextAlignmentOptions.Center, Color.black);
        DashboardUIFactory.SetRect(badgeText.rectTransform, new Vector2(120f, 30f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var thdText = DashboardUIFactory.CreateText("THD", arRoot, "THD: 2.5%", fontHeader, 17f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(thdText.rectTransform, new Vector2(240f, 24f), new Vector2(0f, 20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var pfText = DashboardUIFactory.CreateText("PF", arRoot, "PF: 0.95", fontHeader, 17f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(pfText.rectTransform, new Vector2(240f, 24f), new Vector2(0f, -18f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var demandText = DashboardUIFactory.CreateText("Demand", arRoot, "Demand: 0.000 kW", fontHeader, 17f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(demandText.rectTransform, new Vector2(240f, 24f), new Vector2(0f, -56f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var dashboard = GetComponent<PM2200Dashboard>();
        if (dashboard == null) dashboard = gameObject.AddComponent<PM2200Dashboard>();
        dashboard.bench = bench;
        dashboard.textScreenTitle = title;
        dashboard.textRow1Label = row1Label;
        dashboard.textRow1Value = row1Value;
        dashboard.textRow2Label = row2Label;
        dashboard.textRow2Value = row2Value;
        dashboard.textRow3Label = row3Label;
        dashboard.textRow3Value = row3Value;
        dashboard.textRow4Label = row4Label;
        dashboard.textRow4Value = row4Value;
        dashboard.textNavBar = navBar;
        dashboard.statusLED = led;
        dashboard.lcdBackground = lcd;
        dashboard.textBadgeQuality = badgeText;
        dashboard.textTHD = thdText;
        dashboard.textPowerFactor = pfText;
        dashboard.textDemand = demandText;

        btnPrev.onClick.RemoveAllListeners();
        btnPrev.onClick.AddListener(dashboard.OnPrevButton);
        btnNext.onClick.RemoveAllListeners();
        btnNext.onClick.AddListener(dashboard.OnNextButton);
        btnSelect.onClick.RemoveAllListeners();
        btnSelect.onClick.AddListener(dashboard.OnSelectButton);

        DashboardUIFactory.AddPointerEvent(btnDown.gameObject, EventTriggerType.PointerDown, _ => dashboard.OnDownButtonPress());
        DashboardUIFactory.AddPointerEvent(btnDown.gameObject, EventTriggerType.PointerUp, _ => dashboard.OnDownButtonRelease());

        DashboardUIFactory.ApplyCyberOverlay(root, DashboardUIFactory.CyberStyle.PowerMeterGrey, false, false);
    }

    public void PositionLCDRow(TextMeshProUGUI label, TextMeshProUGUI value, float y)
    {
        if (label != null)
            DashboardUIFactory.SetRect(label.rectTransform, new Vector2(220f, 30f), new Vector2(-304f, y), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        if (value != null)
            DashboardUIFactory.SetRect(value.rectTransform, new Vector2(280f, 30f), new Vector2(-78f, y), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    }
}
