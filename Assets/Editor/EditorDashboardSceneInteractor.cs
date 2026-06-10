#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class EditorDashboardSceneInteractor
{
    private static readonly List<GraphicRaycaster> Raycasters = new List<GraphicRaycaster>(32);
    private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>(32);
    private static EventSystem editorEventSystem;
    private static GameObject pressedTarget;

    static EditorDashboardSceneInteractor()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        AssemblyReloadEvents.beforeAssemblyReload -= Cleanup;
        AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        EditorApplication.quitting -= Cleanup;
        EditorApplication.quitting += Cleanup;
    }

    private static void Cleanup()
    {
        if (editorEventSystem != null)
        {
            Object.DestroyImmediate(editorEventSystem.gameObject);
            editorEventSystem = null;
        }
        pressedTarget = null;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!IsEditorInteractionEnabled()) return;

        Event evt = Event.current;
        if (evt == null) return;
        if (evt.alt) return;
        if (evt.button != 0) return;
        if (evt.type != EventType.MouseDown && evt.type != EventType.MouseUp) return;

        if (!TryRaycastUI(sceneView.camera, evt.mousePosition, out RaycastResult hit))
        {
            if (evt.type == EventType.MouseUp) pressedTarget = null;
            return;
        }

        GameObject target = FindClickTarget(hit.gameObject);
        if (target == null) return;

        if (evt.type == EventType.MouseDown)
        {
            pressedTarget = target;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            InvokeTriggers(target, EventTriggerType.PointerDown, hit);
            evt.Use();
        }
        else if (evt.type == EventType.MouseUp)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            InvokeTriggers(pressedTarget, EventTriggerType.PointerUp, hit);

            if (pressedTarget == target)
            {
                InvokeButtonClick(pressedTarget);
            }

            pressedTarget = null;
            evt.Use();
        }
    }

    private static bool TryRaycastUI(Camera camera, Vector2 guiPoint, out RaycastResult hit)
    {
        hit = new RaycastResult();
        if (camera == null) return false;

        EnsureEventSystem();
        Vector2 screenPoint = HandleUtility.GUIPointToScreenPixelCoordinate(guiPoint);
        var eventData = new PointerEventData(editorEventSystem)
        {
            position = screenPoint,
            button = PointerEventData.InputButton.Left
        };

        RaycastResults.Clear();
        Raycasters.Clear();

        var allRaycasters = Object.FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include);
        foreach (var raycaster in allRaycasters)
        {
            if (raycaster == null) continue;
            var canvas = raycaster.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace) continue;
            if (!canvas.gameObject.activeInHierarchy) continue;

            Camera previousWorldCamera = canvas.worldCamera;
            canvas.worldCamera = camera;
            raycaster.Raycast(eventData, RaycastResults);
            canvas.worldCamera = previousWorldCamera;
        }

        if (RaycastResults.Count == 0) return false;

        RaycastResults.Sort((a, b) =>
        {
            int order = b.sortingOrder.CompareTo(a.sortingOrder);
            if (order != 0) return order;
            int depth = b.depth.CompareTo(a.depth);
            if (depth != 0) return depth;
            return a.distance.CompareTo(b.distance);
        });

        hit = RaycastResults[0];
        return hit.gameObject != null;
    }

    private static GameObject FindClickTarget(GameObject hitObject)
    {
        if (hitObject == null) return null;
        var button = hitObject.GetComponentInParent<Button>();
        if (button != null) return button.gameObject;
        var trigger = hitObject.GetComponentInParent<EventTrigger>();
        if (trigger != null) return trigger.gameObject;
        return hitObject;
    }

    private static void InvokeTriggers(GameObject target, EventTriggerType type, RaycastResult hit)
    {
        if (target == null) return;

        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null || trigger.triggers == null) return;

        EnsureEventSystem();
        var eventData = new PointerEventData(editorEventSystem)
        {
            position = hit.screenPosition,
            button = PointerEventData.InputButton.Left
        };

        foreach (var entry in trigger.triggers)
        {
            if (entry.eventID != type) continue;
            RecordBenches("Dashboard Interaction");
            entry.callback.Invoke(eventData);
        }
    }

    private static void InvokeButtonClick(GameObject target)
    {
        if (target == null) return;
        var button = target.GetComponent<Button>();
        if (button == null || !button.interactable || !button.gameObject.activeInHierarchy) return;

        RecordBenches("Dashboard Click");
        button.onClick.Invoke();
        SceneView.RepaintAll();
    }

    private static void EnsureEventSystem()
    {
        if (editorEventSystem != null) return;
        var go = new GameObject("EditorEventSystem");
        go.hideFlags = HideFlags.HideAndDontSave;
        editorEventSystem = go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private static void RecordBenches(string label)
    {
        var benches = Object.FindObjectsByType<BenchSystem>(FindObjectsInactive.Include);
        foreach (var bench in benches)
        {
            if (bench == null) continue;
            Undo.RecordObject(bench, label);
        }
    }

    private static bool IsEditorInteractionEnabled()
    {
        var benches = Object.FindObjectsByType<BenchSystem>(FindObjectsInactive.Include);
        foreach (var bench in benches)
        {
            if (bench == null) continue;
            if (bench.previewInEditor && bench.enableEditorInteraction) return true;
        }
        return false;
    }
}
#endif
