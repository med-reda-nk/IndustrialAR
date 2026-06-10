using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class EStopFrontPanelBuilder : MonoBehaviour
{
    private static readonly Color32 HousingGrey = new Color32(42, 42, 42, 255);
    private static readonly Color32 HousingInsetGrey = new Color32(58, 58, 58, 130);
    private static readonly Color32 PlateGrey = new Color32(28, 28, 28, 255);
    private static readonly Color32 PanelGrey = new Color32(24, 24, 24, 235);

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

        var size = new Vector2(800f, 800f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(gameObject, size, canvasScale);
        var canvas = DashboardUIFactory.EnsureWorldCanvas(gameObject, size, scale);
        if (canvas == null) return;

        if (alignToTargetTop) DashboardUIFactory.AlignToImageTargetTop(transform, topOffset);

        DashboardUIFactory.ClearChildren(transform);

        var root = DashboardUIFactory.CreateRect("EStop_Root", transform, new Vector2(800f, 800f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var housingShadow = DashboardUIFactory.CreateImage("HousingShadow", root, new Vector2(772f, 772f), new Vector2(10f, -12f), new Color32(0, 0, 0, 140));
        var housing = DashboardUIFactory.CreateImage("Housing", root, new Vector2(760f, 760f), Vector2.zero, HousingGrey);
        DashboardUIFactory.CreateImage("HousingInset", root, new Vector2(700f, 700f), Vector2.zero, HousingInsetGrey);

        var topPlate = DashboardUIFactory.CreateImage("TopPlate", root, new Vector2(310f, 58f), new Vector2(190f, 248f), PlateGrey);
        var topLabel = DashboardUIFactory.CreateText("TopLabel", topPlate.transform, "EMERGENCY STOP", fontMain, 18f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(topLabel.rectTransform, new Vector2(292f, 40f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var buttonRoot = DashboardUIFactory.CreateRect("MushroomRoot", root, new Vector2(320f, 320f), new Vector2(-190f, 20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var shadow = DashboardUIFactory.CreateImage("Shadow", buttonRoot, new Vector2(320f, 320f), new Vector2(0f, -12f), new Color32(0, 0, 0, 110), DashboardUIFactory.GetCircleSprite());
        var outer = DashboardUIFactory.CreateImage("Outer", buttonRoot, new Vector2(320f, 320f), Vector2.zero, new Color32(120, 0, 0, 255), DashboardUIFactory.GetCircleSprite());
        var innerRing = DashboardUIFactory.CreateImage("InnerRing", buttonRoot, new Vector2(300f, 300f), Vector2.zero, new Color32(150, 0, 0, 255), DashboardUIFactory.GetCircleSprite());
        var mushroom = DashboardUIFactory.CreateImage("Mushroom", buttonRoot, new Vector2(270f, 270f), Vector2.zero, new Color32(0xD5, 0x00, 0x00, 0xFF), DashboardUIFactory.GetCircleSprite());
        var highlight = DashboardUIFactory.CreateImage("Highlight", buttonRoot, new Vector2(120f, 80f), new Vector2(-60f, 60f), new Color32(255, 255, 255, 120), DashboardUIFactory.GetCircleSprite());

        var bottomLabel = DashboardUIFactory.CreateText("BottomLabel", root, "PUSH TO STOP", fontMain, 18f, TextAlignmentOptions.Center, Color.white);
        DashboardUIFactory.SetRect(bottomLabel.rectTransform, new Vector2(300f, 40f), new Vector2(190f, 190f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var led = DashboardUIFactory.CreateImage("StatusLED", root, new Vector2(18f, 18f), new Vector2(320f, -300f), new Color32(0, 229, 255, 255), DashboardUIFactory.GetCircleSprite());

        var releaseRoot = DashboardUIFactory.CreateRect("ReleaseRoot", root, new Vector2(160f, 50f), new Vector2(190f, -230f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var releaseBtn = DashboardUIFactory.CreateButton("Release", releaseRoot, new Vector2(160f, 50f), Vector2.zero, new Color32(80, 80, 80, 255), "UNLOCK", fontMain, 16f, Color.white);

        var arRoot = DashboardUIFactory.CreateRect("AR_Overlays", root, new Vector2(320f, 260f), new Vector2(190f, 20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        DashboardUIFactory.CreateImage("StatusDisplay", arRoot, new Vector2(320f, 260f), Vector2.zero, PanelGrey);
        var stateText = DashboardUIFactory.CreateText("StateBadge", arRoot, "READY", fontMain, 30f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(stateText.rectTransform, new Vector2(280f, 44f), new Vector2(0f, 86f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var pressCount = DashboardUIFactory.CreateText("PressCount", arRoot, "ACTUATIONS: 0", fontMain, 18f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(pressCount.rectTransform, new Vector2(280f, 30f), new Vector2(0f, 34f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var haltTimer = DashboardUIFactory.CreateText("HaltTimer", arRoot, "DOWNTIME: --:--", fontMain, 18f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(haltTimer.rectTransform, new Vector2(280f, 30f), new Vector2(0f, -4f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var lastEvent = DashboardUIFactory.CreateText("LastEvent", arRoot, "LAST EVENT: NEVER", fontMain, 15f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(lastEvent.rectTransform, new Vector2(280f, 44f), new Vector2(0f, -54f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var dashboard = GetComponent<EStopDashboard>();
        if (dashboard == null) dashboard = gameObject.AddComponent<EStopDashboard>();
        dashboard.bench = bench;
        dashboard.housingPanel = housing;
        dashboard.mushroomButton = buttonRoot;
        dashboard.mushroomImage = mushroom;
        dashboard.highlightOverlay = highlight.gameObject;
        dashboard.pressedShadow = shadow.gameObject;
        dashboard.statusLED = led;
        dashboard.releaseButtonRoot = releaseRoot.gameObject;
        dashboard.textStateBadge = stateText;
        dashboard.textPressCount = pressCount;
        dashboard.textHaltTimer = haltTimer;
        dashboard.textLastEvent = lastEvent;

        var pressButton = buttonRoot.gameObject.AddComponent<Button>();
        pressButton.onClick.RemoveAllListeners();
        pressButton.onClick.AddListener(dashboard.OnPressButton);
        releaseBtn.onClick.RemoveAllListeners();
        releaseBtn.onClick.AddListener(dashboard.OnReleaseButton);
    }

}
