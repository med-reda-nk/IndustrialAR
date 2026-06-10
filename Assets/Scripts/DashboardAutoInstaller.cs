using UnityEngine;
using TMPro;
using Vuforia;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class DashboardAutoInstaller : MonoBehaviour
{
    public enum DashboardType
    {
        None,
        PM2200,
        PLC,
        HMI,
        SignalTower,
        EStop,
        Motor
    }

    [Header("References")]
    public BenchSystem bench;
    public TMP_FontAsset fontMain;
    public TMP_FontAsset fontMono;

    [Header("Behavior")]
    public bool buildInEditor = true;
    public bool buildInPlayMode = true;
    public bool disableLegacyDashboards = true;
    public float canvasScale = 0.0001f;
    public bool forceRebuild;
    private const float FloatingPanelTopOffset = 0.04f;

#if UNITY_EDITOR
    [System.NonSerialized] private bool editorBuildQueued;
#endif

    public void OnEnable()
    {
        if (ShouldBuild()) EnsureDashboards(false);
    }

    public void Start()
    {
        if (Application.isPlaying && buildInPlayMode) EnsureDashboards(false);
    }

    public void OnValidate()
    {
        if (!Application.isPlaying && buildInEditor) QueueEditorBuild(false);
    }

    public void Update()
    {
        if (!forceRebuild) return;
        forceRebuild = false;
        EnsureDashboards(true);
    }

    [ContextMenu("Rebuild Dashboards")]
    public void RebuildDashboards()
    {
        EnsureDashboards(true);
    }

    public bool ShouldBuild()
    {
        return Application.isPlaying ? buildInPlayMode : buildInEditor;
    }

    public void EnsureDashboards(bool force)
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        SetupMRSupport(force);

        var targets = Object.FindObjectsByType<ImageTargetBehaviour>(FindObjectsInactive.Include);
        if (targets == null) return;

        foreach (var target in targets)
        {
            var type = ResolveType(target);
            if (type == DashboardType.None) continue;

            if (disableLegacyDashboards) DisableLegacyDashboards(target.transform);

            switch (type)
            {
                case DashboardType.PM2200:
                    SetupPM2200(target.transform, force);
                    break;
                case DashboardType.PLC:
                    SetupPLC(target.transform, force);
                    break;
                case DashboardType.HMI:
                    SetupHMI(target.transform, force);
                    break;
                case DashboardType.SignalTower:
                    SetupSignalTower(target.transform, force);
                    break;
                case DashboardType.EStop:
                    SetupEStop(target.transform, force);
                    break;
                case DashboardType.Motor:
                    SetupMotor(target.transform, force);
                    break;
            }
        }
    }

    private void SetupMRSupport(bool force)
    {
        var host = bench != null ? bench.gameObject : gameObject;

        var nodeRed = FindAnyObjectByType<NodeRedClient>(FindObjectsInactive.Include);
        if (nodeRed == null) nodeRed = host.AddComponent<NodeRedClient>();

        var wiringGuide = FindAnyObjectByType<WiringGuideUI>(FindObjectsInactive.Include);
        if (wiringGuide == null) wiringGuide = host.AddComponent<WiringGuideUI>();
        wiringGuide.bench = bench;
        wiringGuide.screenSpaceOverlay = true;
        wiringGuide.alwaysShow = false;
        wiringGuide.showOnlyWhenIncomplete = true;

        var integrationPanel = FindAnyObjectByType<MRIntegrationPanel>(FindObjectsInactive.Include);
        if (integrationPanel == null) integrationPanel = host.AddComponent<MRIntegrationPanel>();
        integrationPanel.bench = bench;
        integrationPanel.nodeRed = nodeRed;
        integrationPanel.wiringGuide = wiringGuide;
        integrationPanel.fontMain = fontMain;
        if (force) integrationPanel.Build(true);
    }

    public DashboardType ResolveType(ImageTargetBehaviour target)
    {
        if (target == null) return DashboardType.None;

        string name = target.gameObject.name.ToLowerInvariant();
        string trackable = string.Empty;
        try
        {
            trackable = target.TargetName != null ? target.TargetName.ToLowerInvariant() : string.Empty;
        }
        catch
        {
            trackable = string.Empty;
        }

        string token = name + " " + trackable;
        if (token.Contains("pm2200")) return DashboardType.PM2200;
        if (token.Contains("s7-1200") || token.Contains("plc")) return DashboardType.PLC;
        if (token.Contains("ktp") || token.Contains("hmi")) return DashboardType.HMI;
        if (token.Contains("signal")) return DashboardType.SignalTower;
        if (token.Contains("estop") || token.Contains("e-stop")) return DashboardType.EStop;
        if (token.Contains("motor")) return DashboardType.Motor;

        return DashboardType.None;
    }

    public void DisableLegacyDashboards(Transform target)
    {
        if (target == null) return;
        DisableLegacyDashboardsRecursive(target);
    }

    private void DisableLegacyDashboardsRecursive(Transform target)
    {
        foreach (Transform child in target)
        {
            if (child == null) continue;

            bool legacyDashboard = child.name.Contains("PM2200_Dashboard")
                || child.GetComponent<ComponentDashboardUI>() != null
                || child.GetComponent<PM2200DashboardBuilder>() != null;

            if (legacyDashboard)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            DisableLegacyDashboardsRecursive(child);
        }
    }

    public void SetupPM2200(Transform target, bool force)
    {
        var host = GetOrCreateChild(target, "PM2200_FrontPanel");
        var size = new Vector2(960f, 960f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(host, size, canvasScale);
        DashboardUIFactory.EnsureWorldCanvas(host, size, scale);
        DashboardUIFactory.EnsureCameraFacingDashboard(host);
        host.transform.localPosition = Vector3.zero;
        host.transform.localRotation = Quaternion.identity;

        var builder = host.GetComponent<PM2200FrontPanelBuilder>();
        if (builder == null) builder = host.AddComponent<PM2200FrontPanelBuilder>();
        builder.bench = bench;
        builder.fontHeader = fontMain;
        builder.fontMono = fontMono;
        builder.canvasScale = canvasScale;
        builder.topOffset = FloatingPanelTopOffset;
        builder.Build(force);
    }

    public void SetupPLC(Transform target, bool force)
    {
        var host = GetOrCreateChild(target, "PLC_FrontPanel");
        var size = new Vector2(1100f, 1000f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(host, size, canvasScale);
        DashboardUIFactory.EnsureWorldCanvas(host, size, scale);
        DashboardUIFactory.EnsureCameraFacingDashboard(host);
        host.transform.localPosition = Vector3.zero;
        host.transform.localRotation = Quaternion.identity;

        var builder = host.GetComponent<PLCFrontPanelBuilder>();
        if (builder == null) builder = host.AddComponent<PLCFrontPanelBuilder>();
        builder.bench = bench;
        builder.fontMain = fontMain;
        builder.canvasScale = canvasScale;
        builder.topOffset = FloatingPanelTopOffset;
        builder.Build(force);
    }

    public void SetupHMI(Transform target, bool force)
    {
        var host = GetOrCreateChild(target, "HMI_FrontPanel");
        var size = new Vector2(2120f, 1430f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(host, size, canvasScale);
        DashboardUIFactory.EnsureWorldCanvas(host, size, scale);
        DashboardUIFactory.EnsureCameraFacingDashboard(host);
        host.transform.localPosition = Vector3.zero;
        host.transform.localRotation = Quaternion.identity;

        var builder = host.GetComponent<HMIFrontPanelBuilder>();
        if (builder == null) builder = host.AddComponent<HMIFrontPanelBuilder>();
        builder.bench = bench;
        builder.fontMain = fontMain;
        builder.canvasScale = canvasScale;
        builder.topOffset = FloatingPanelTopOffset;
        builder.Build(force);
    }

    public void SetupSignalTower(Transform target, bool force)
    {
        var host = GetOrCreateChild(target, "SignalTower_FrontPanel");
        var size = new Vector2(600f, 800f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(host, size, canvasScale);
        DashboardUIFactory.EnsureWorldCanvas(host, size, scale);
        DashboardUIFactory.EnsureCameraFacingDashboard(host);
        host.transform.localPosition = Vector3.zero;
        host.transform.localRotation = Quaternion.identity;

        var builder = host.GetComponent<SignalTowerFrontPanelBuilder>();
        if (builder == null) builder = host.AddComponent<SignalTowerFrontPanelBuilder>();
        builder.bench = bench;
        builder.fontMain = fontMain;
        builder.canvasScale = canvasScale;
        builder.topOffset = FloatingPanelTopOffset;
        builder.Build(force);
    }

    public void SetupEStop(Transform target, bool force)
    {
        var host = GetOrCreateChild(target, "EStop_FrontPanel");
        var size = new Vector2(800f, 800f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(host, size, canvasScale);
        DashboardUIFactory.EnsureWorldCanvas(host, size, scale);
        DashboardUIFactory.EnsureCameraFacingDashboard(host);
        host.transform.localPosition = Vector3.zero;
        host.transform.localRotation = Quaternion.identity;

        var builder = host.GetComponent<EStopFrontPanelBuilder>();
        if (builder == null) builder = host.AddComponent<EStopFrontPanelBuilder>();
        builder.bench = bench;
        builder.fontMain = fontMain;
        builder.canvasScale = canvasScale;
        builder.topOffset = FloatingPanelTopOffset;
        builder.Build(force);
    }

    public void SetupMotor(Transform target, bool force)
    {
        var host = GetOrCreateChild(target, "Motor_FrontPanel");
        var size = new Vector2(2000f, 1800f);
        float scale = DashboardUIFactory.ComputeScaleForImageTarget(host, size, canvasScale);
        DashboardUIFactory.EnsureWorldCanvas(host, size, scale);
        DashboardUIFactory.EnsureCameraFacingDashboard(host);
        host.transform.localPosition = Vector3.zero;
        host.transform.localRotation = Quaternion.identity;

        var builder = host.GetComponent<MotorFrontPanelBuilder>();
        if (builder == null) builder = host.AddComponent<MotorFrontPanelBuilder>();
        builder.bench = bench;
        builder.fontMain = fontMain;
        builder.fontMono = fontMono;
        builder.canvasScale = canvasScale;
        builder.topOffset = FloatingPanelTopOffset;
        builder.Build(force);
    }

    public GameObject GetOrCreateChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing.gameObject;
        }
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private void QueueEditorBuild(bool force)
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            EnsureDashboards(force);
            return;
        }

        if (editorBuildQueued) return;
        editorBuildQueued = true;

        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            editorBuildQueued = false;
            if (!Application.isPlaying && buildInEditor) EnsureDashboards(force);
        };
#else
        EnsureDashboards(force);
#endif
    }
}
