using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class SignalTowerFrontPanelBuilder : MonoBehaviour
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

        var size = new Vector2(600f, 800f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(gameObject, size, canvasScale);
        var canvas = DashboardUIFactory.EnsureWorldCanvas(gameObject, size, scale);
        if (canvas == null) return;

        if (alignToTargetTop) DashboardUIFactory.AlignToImageTargetTop(transform, topOffset);

        DashboardUIFactory.ClearChildren(transform);

        var root = DashboardUIFactory.CreateRect("Tower_Root", transform, new Vector2(600f, 800f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var red = DashboardUIFactory.CreateImage("RedModule", root, new Vector2(128f, 128f), new Vector2(0f, 180f), new Color32(30, 30, 30, 110), DashboardUIFactory.GetCircleSprite());
        var orange = DashboardUIFactory.CreateImage("OrangeModule", root, new Vector2(128f, 128f), new Vector2(0f, 40f), new Color32(30, 30, 30, 110), DashboardUIFactory.GetCircleSprite());
        var green = DashboardUIFactory.CreateImage("GreenModule", root, new Vector2(128f, 128f), new Vector2(0f, -100f), new Color32(30, 30, 30, 110), DashboardUIFactory.GetCircleSprite());

        var glowRed = DashboardUIFactory.CreateImage("GlowRed", root, new Vector2(96f, 96f), new Vector2(0f, 180f), new Color32(213, 0, 0, 120), DashboardUIFactory.GetCircleSprite());
        var glowOrange = DashboardUIFactory.CreateImage("GlowOrange", root, new Vector2(96f, 96f), new Vector2(0f, 40f), new Color32(255, 109, 0, 120), DashboardUIFactory.GetCircleSprite());
        var glowGreen = DashboardUIFactory.CreateImage("GlowRun", root, new Vector2(96f, 96f), new Vector2(0f, -100f), new Color32(0, 229, 255, 120), DashboardUIFactory.GetCircleSprite());

        var arRoot = DashboardUIFactory.CreateRect("AR_Overlays", root, new Vector2(300f, 320f), new Vector2(250f, 20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var badge = DashboardUIFactory.CreateText("StateBadge", arRoot, "IDLE", fontMain, 26f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(badge.rectTransform, new Vector2(240f, 40f), new Vector2(0f, 120f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        var timer = DashboardUIFactory.CreateText("Timer", arRoot, "0.0s", fontMain, 20f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(timer.rectTransform, new Vector2(240f, 30f), new Vector2(0f, 80f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        var history = DashboardUIFactory.CreateText("History", arRoot, "", fontMain, 18f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(history.rectTransform, new Vector2(240f, 90f), new Vector2(0f, 20f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        var reason = DashboardUIFactory.CreateText("Reason", arRoot, "", fontMain, 18f, TextAlignmentOptions.Left, Color.white);
        DashboardUIFactory.SetRect(reason.rectTransform, new Vector2(240f, 60f), new Vector2(0f, -60f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        var note = DashboardUIFactory.CreateText("Note", arRoot, "Controlled by S7-1200 - Q0.1/Q0.2/Q0.3", fontMain, 14f, TextAlignmentOptions.Left, new Color32(200, 200, 200, 255));
        DashboardUIFactory.SetRect(note.rectTransform, new Vector2(260f, 40f), new Vector2(0f, -120f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

        var dashboard = GetComponent<SignalTowerDashboard>();
        if (dashboard == null) dashboard = gameObject.AddComponent<SignalTowerDashboard>();
        dashboard.bench = bench;
        dashboard.moduleRed = red;
        dashboard.moduleOrange = orange;
        dashboard.moduleGreen = green;
        dashboard.glowRed = glowRed;
        dashboard.glowOrange = glowOrange;
        dashboard.glowGreen = glowGreen;
        dashboard.textStateBadge = badge;
        dashboard.textStopwatch = timer;
        dashboard.textStateHistory = history;
        dashboard.textReason = reason;
        dashboard.textNote = note;
    }
}
