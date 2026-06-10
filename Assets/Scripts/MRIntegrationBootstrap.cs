using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class MRIntegrationBootstrap
{
    private const string OverlayHostName = "MR_ScreenOverlay";

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorInitialize()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying) return;

            EnsureScreenOverlay(true);

            var installer = Object.FindAnyObjectByType<DashboardAutoInstaller>(FindObjectsInactive.Include);
            if (installer != null)
            {
                installer.RebuildDashboards();
                EditorUtility.SetDirty(installer);
            }
        };
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeInitialize()
    {
        EnsureScreenOverlay(false);
    }

    public static MRIntegrationPanel EnsureScreenOverlay(bool forceBuild)
    {
        var bench = Object.FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        var existingPanel = Object.FindAnyObjectByType<MRIntegrationPanel>(FindObjectsInactive.Include);
        var host = existingPanel != null
            ? existingPanel.gameObject
            : bench != null
                ? bench.gameObject
                : GameObject.Find(OverlayHostName);

        if (host == null) host = new GameObject(OverlayHostName);
        CleanupStrayOverlayHost(host);

        var nodeRed = Object.FindAnyObjectByType<NodeRedClient>(FindObjectsInactive.Include);
        if (nodeRed == null) nodeRed = host.AddComponent<NodeRedClient>();

        var wiringGuide = Object.FindAnyObjectByType<WiringGuideUI>(FindObjectsInactive.Include);
        if (wiringGuide == null) wiringGuide = host.AddComponent<WiringGuideUI>();
        wiringGuide.bench = bench;
        wiringGuide.screenSpaceOverlay = true;

        var panel = existingPanel;
        if (panel == null) panel = host.AddComponent<MRIntegrationPanel>();
        panel.bench = bench;
        panel.nodeRed = nodeRed;
        panel.wiringGuide = wiringGuide;

        if (forceBuild) panel.Build(true);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(host);
            EditorUtility.SetDirty(panel);
        }
#endif

        return panel;
    }

    private static void CleanupStrayOverlayHost(GameObject activeHost)
    {
        var stray = GameObject.Find(OverlayHostName);
        if (stray == null || stray == activeHost || stray.transform.childCount != 0) return;

        var components = stray.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && !(components[i] is Transform)) return;
        }

        if (Application.isPlaying) Object.Destroy(stray);
#if UNITY_EDITOR
        else Object.DestroyImmediate(stray);
#endif
    }
}
