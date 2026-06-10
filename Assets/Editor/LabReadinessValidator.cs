using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Vuforia;

public static class LabReadinessValidator
{
    private const string LogPrefix = "[IndustrialAR Lab]";

    [MenuItem("Tools/Industrial AR/Run Lab Readiness Check")]
    public static void RunFromMenu()
    {
        RunChecks(false);
    }

    [MenuItem("Tools/Industrial AR/Run Lab Readiness Check + Overlay Simulation")]
    public static void RunWithOverlaySimulationFromMenu()
    {
        RunChecks(true);
    }

    public static void RunBatch()
    {
        bool ok = RunChecks(true);
        if (!ok) EditorApplication.Exit(1);
    }

    private static bool RunChecks(bool simulateOverlays)
    {
        var failures = new List<string>();
        var warnings = new List<string>();

        CheckMissingScripts(failures);
        CheckVuforiaSetup(failures, warnings);
        CheckMrAndNodeRedSetup(failures, warnings);

        if (simulateOverlays)
        {
            CheckOverlaySimulation(failures, warnings);
        }

        var builder = new StringBuilder();
        builder.AppendLine(LogPrefix + " readiness check complete.");
        builder.AppendLine(LogPrefix + " failures=" + failures.Count + " warnings=" + warnings.Count);

        foreach (string failure in failures)
        {
            builder.AppendLine(LogPrefix + " FAIL " + failure);
        }

        foreach (string warning in warnings)
        {
            builder.AppendLine(LogPrefix + " WARN " + warning);
        }

        if (failures.Count == 0)
        {
            builder.AppendLine(LogPrefix + " PASS Unity scene, scripts, Vuforia targets, MR tools, and Node-RED UI wiring are ready for lab testing.");
        }

        string message = builder.ToString();
        if (failures.Count > 0) Debug.LogError(message);
        else if (warnings.Count > 0) Debug.LogWarning(message);
        else Debug.Log(message);

        return failures.Count == 0;
    }

    private static void CheckMissingScripts(List<string> failures)
    {
        int missingSlots = 0;
        foreach (var go in FindSceneObjects())
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count <= 0) continue;

            missingSlots += count;
            failures.Add(go.name + " has " + count + " missing MonoBehaviour script slot(s).");
        }

        foreach (string path in EnumerateSerializedAssetFiles())
        {
            string text = File.ReadAllText(path);
            if (text.Contains("m_Script: {fileID: 0}"))
            {
                failures.Add(path + " contains a broken m_Script reference.");
            }
        }

        if (missingSlots == 0)
        {
            Debug.Log(LogPrefix + " missing-script scan passed.");
        }
    }

    private static void CheckVuforiaSetup(List<string> failures, List<string> warnings)
    {
        string configPath = "Assets/Resources/VuforiaConfiguration.asset";
        if (!File.Exists(configPath))
        {
            failures.Add("Missing " + configPath + ".");
        }
        else
        {
            string config = File.ReadAllText(configPath);
            if (!config.Contains("maxSimultaneousImageTargets: 6"))
            {
                failures.Add("Vuforia maxSimultaneousImageTargets is not set to 6.");
            }

            if (!config.Contains("vuforiaLicenseKey: ") || config.Contains("vuforiaLicenseKey: \n"))
            {
                failures.Add("Vuforia license key appears empty.");
            }
        }

        string xmlPath = "Assets/StreamingAssets/Vuforia/IndustrialBench.xml";
        string datPath = "Assets/StreamingAssets/Vuforia/IndustrialBench.dat";
        if (!File.Exists(xmlPath)) failures.Add("Missing Vuforia database XML: " + xmlPath + ".");
        if (!File.Exists(datPath)) failures.Add("Missing Vuforia database DAT: " + datPath + ".");

        var targets = UnityEngine.Object.FindObjectsByType<ImageTargetBehaviour>(FindObjectsInactive.Include);
        if (targets.Length < 6)
        {
            failures.Add("Expected 6 ImageTargetBehaviour objects, found " + targets.Length + ".");
        }

        var expectedNames = new HashSet<string>
        {
            "PM2200",
            "S7-1200_PLC",
            "ktp700hmi",
            "Signal_tower",
            "motor",
            "Stop_Button"
        };

        foreach (var target in targets)
        {
            if (target == null) continue;

            string targetName = SafeTargetName(target);
            expectedNames.Remove(targetName);

            var safeHandler = target.GetComponent<SafeVuforiaObserverEventHandler>();
            if (safeHandler == null)
            {
                failures.Add(target.gameObject.name + " is missing SafeVuforiaObserverEventHandler.");
            }
            else if (safeHandler.statusFilter != SafeVuforiaObserverEventHandler.TrackingStatusFilter.Tracked)
            {
                failures.Add(target.gameObject.name + " SafeVuforiaObserverEventHandler must use strict TRACKED-only visibility so dashboards disappear when targets are lost.");
            }

            if (!target.isActiveAndEnabled)
            {
                warnings.Add(target.gameObject.name + " ImageTargetBehaviour is not active and enabled.");
            }
        }

        foreach (string missingName in expectedNames)
        {
            failures.Add("Missing Vuforia target named " + missingName + ".");
        }

        var camera = Camera.main;
        if (camera == null)
        {
            failures.Add("No MainCamera/ARCamera is available.");
            return;
        }

        var vuforia = camera.GetComponent<VuforiaBehaviour>();
        if (vuforia == null) failures.Add(camera.name + " is missing VuforiaBehaviour.");
        else if (!vuforia.enabled) failures.Add(camera.name + " VuforiaBehaviour is disabled.");

        if (camera.GetComponent<SafeVuforiaInitializationErrorHandler>() == null)
        {
            failures.Add(camera.name + " is missing SafeVuforiaInitializationErrorHandler.");
        }

        if (camera.GetComponent<VuforiaDetectionDiagnostics>() == null)
        {
            warnings.Add(camera.name + " is missing VuforiaDetectionDiagnostics, so target status logs will be unavailable.");
        }
    }

    private static void CheckMrAndNodeRedSetup(List<string> failures, List<string> warnings)
    {
        var nodeRed = UnityEngine.Object.FindAnyObjectByType<NodeRedClient>(FindObjectsInactive.Include);
        if (nodeRed == null)
        {
            failures.Add("NodeRedClient is missing from the scene.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(nodeRed.nodeRedBaseUrl))
            {
                failures.Add("NodeRedClient nodeRedBaseUrl is empty.");
            }

            if (nodeRed.telemetryPath != "/twin-data")
            {
                warnings.Add("NodeRedClient telemetryPath is " + nodeRed.telemetryPath + "; expected /twin-data for the provided Node-RED flow.");
            }

            if (nodeRed.useSimulation)
            {
                warnings.Add("NodeRedClient starts in simulation mode. Use CONNECT LIVE in play mode for real telemetry.");
            }
        }

        if (UnityEngine.Object.FindAnyObjectByType<MRIntegrationPanel>(FindObjectsInactive.Include) == null)
        {
            failures.Add("MRIntegrationPanel is missing from the scene.");
        }

        if (UnityEngine.Object.FindAnyObjectByType<WiringGuideUI>(FindObjectsInactive.Include) == null)
        {
            failures.Add("WiringGuideUI is missing from the scene.");
        }

        string[] requiredUiObjects =
        {
            "MRToolsFrame",
            "BottomRightActionDock",
            "NodeRedButton",
            "WiringButton",
            "TelemetryButton",
            "IpAddressInput",
            "PortInput",
            "PasswordInput"
        };

        foreach (string name in requiredUiObjects)
        {
            if (FindSceneObjectByName(name) == null)
            {
                failures.Add("MR tool UI object is missing: " + name + ".");
            }
        }
    }

    private static void CheckOverlaySimulation(List<string> failures, List<string> warnings)
    {
        var method = typeof(SafeVuforiaObserverEventHandler).GetMethod("SetTrackingVisible", BindingFlags.Instance | BindingFlags.NonPublic);
        var visibleField = typeof(SafeVuforiaObserverEventHandler).GetField("currentVisible", BindingFlags.Instance | BindingFlags.NonPublic);

        if (method == null)
        {
            failures.Add("Cannot access SafeVuforiaObserverEventHandler.SetTrackingVisible for overlay simulation.");
            return;
        }

        foreach (var target in UnityEngine.Object.FindObjectsByType<ImageTargetBehaviour>(FindObjectsInactive.Include))
        {
            if (target == null) continue;

            var handler = target.GetComponent<SafeVuforiaObserverEventHandler>();
            if (handler == null) continue;

            var snapshot = new OverlaySnapshot(target.gameObject, handler, visibleField);
            method.Invoke(handler, new object[] { true, false });

            int immediateChildren = 0;
            foreach (Transform child in target.transform)
            {
                if (child != null && child.gameObject.activeSelf) immediateChildren++;
            }

            int enabledCanvases = 0;
            foreach (var canvas in target.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas != null && canvas.enabled) enabledCanvases++;
            }

            snapshot.Restore();

            if (immediateChildren == 0)
            {
                failures.Add(target.gameObject.name + " overlay simulation activated no child dashboard objects.");
            }
            else if (enabledCanvases == 0)
            {
                warnings.Add(target.gameObject.name + " overlay simulation activated children but found no enabled Canvas.");
            }
        }
    }

    private static IEnumerable<GameObject> FindSceneObjects()
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go == null || !go.scene.IsValid()) continue;
            yield return go;
        }
    }

    private static GameObject FindSceneObjectByName(string name)
    {
        foreach (var go in FindSceneObjects())
        {
            if (go.name == name) return go;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSerializedAssetFiles()
    {
        foreach (string pattern in new[] { "*.unity", "*.prefab", "*.asset" })
        {
            foreach (string path in Directory.EnumerateFiles("Assets", pattern, SearchOption.AllDirectories))
            {
                yield return path.Replace('\\', '/');
            }
        }
    }

    private static string SafeTargetName(ImageTargetBehaviour target)
    {
        try
        {
            return target.TargetName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed class OverlaySnapshot
    {
        private readonly List<GameObjectState> gameObjects = new List<GameObjectState>();
        private readonly List<BehaviourState> behaviours = new List<BehaviourState>();
        private readonly List<RendererState> renderers = new List<RendererState>();
        private readonly List<ColliderState> colliders = new List<ColliderState>();
        private readonly SafeVuforiaObserverEventHandler handler;
        private readonly FieldInfo visibleField;
        private readonly object currentVisible;

        public OverlaySnapshot(GameObject root, SafeVuforiaObserverEventHandler handler, FieldInfo visibleField)
        {
            this.handler = handler;
            this.visibleField = visibleField;
            currentVisible = visibleField != null ? visibleField.GetValue(handler) : null;

            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform != null) gameObjects.Add(new GameObjectState(transform.gameObject));
            }

            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component is Behaviour behaviour) behaviours.Add(new BehaviourState(behaviour));
                else if (component is Renderer renderer) renderers.Add(new RendererState(renderer));
                else if (component is Collider collider) colliders.Add(new ColliderState(collider));
            }
        }

        public void Restore()
        {
            foreach (var item in behaviours) item.Restore();
            foreach (var item in renderers) item.Restore();
            foreach (var item in colliders) item.Restore();
            foreach (var item in gameObjects) item.Restore();

            if (visibleField != null)
            {
                visibleField.SetValue(handler, currentVisible);
            }
        }
    }

    private readonly struct GameObjectState
    {
        private readonly GameObject target;
        private readonly bool activeSelf;

        public GameObjectState(GameObject target)
        {
            this.target = target;
            activeSelf = target.activeSelf;
        }

        public void Restore()
        {
            if (target != null) target.SetActive(activeSelf);
        }
    }

    private readonly struct BehaviourState
    {
        private readonly Behaviour target;
        private readonly bool enabled;

        public BehaviourState(Behaviour target)
        {
            this.target = target;
            enabled = target.enabled;
        }

        public void Restore()
        {
            if (target != null) target.enabled = enabled;
        }
    }

    private readonly struct RendererState
    {
        private readonly Renderer target;
        private readonly bool enabled;

        public RendererState(Renderer target)
        {
            this.target = target;
            enabled = target.enabled;
        }

        public void Restore()
        {
            if (target != null) target.enabled = enabled;
        }
    }

    private readonly struct ColliderState
    {
        private readonly Collider target;
        private readonly bool enabled;

        public ColliderState(Collider target)
        {
            this.target = target;
            enabled = target.enabled;
        }

        public void Restore()
        {
            if (target != null) target.enabled = enabled;
        }
    }
}
