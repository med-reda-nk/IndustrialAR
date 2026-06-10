using UnityEngine;
using UnityEngine.Events;
using Vuforia;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-9000)]
public class SafeVuforiaObserverEventHandler : MonoBehaviour
{
    public enum TrackingStatusFilter
    {
        Tracked,
        Tracked_ExtendedTracked,
        Tracked_ExtendedTracked_Limited
    }

    public TrackingStatusFilter statusFilter = TrackingStatusFilter.Tracked;
    public bool logTrackingChanges = true;
    public UnityEvent onTargetFound = new UnityEvent();
    public UnityEvent onTargetLost = new UnityEvent();

    private ObserverBehaviour observer;
    private TargetStatus previousStatus = TargetStatus.NotObserved;
    private bool callbackReceivedOnce;
    private bool currentVisible;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorInstall()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying) InstallAll();
        };
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeInstall()
    {
        InstallAll();
    }

    public static void InstallAll()
    {
        DisablePackagedHandlers();
        var observers = FindObjectsByType<ObserverBehaviour>(FindObjectsInactive.Include);
        foreach (var item in observers)
        {
            if (item == null) continue;
            if (item.GetComponent<SafeVuforiaObserverEventHandler>() == null)
            {
                item.gameObject.AddComponent<SafeVuforiaObserverEventHandler>();
            }
        }
    }

    private void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer == null) return;

        observer.OnTargetStatusChanged -= OnObserverStatusChanged;
        observer.OnTargetStatusChanged += OnObserverStatusChanged;
        observer.OnBehaviourDestroyed -= OnObserverDestroyed;
        observer.OnBehaviourDestroyed += OnObserverDestroyed;
        OnObserverStatusChanged(observer, observer.TargetStatus);
    }

    private void Update()
    {
        if (observer == null) observer = GetComponent<ObserverBehaviour>();
        if (observer == null) return;

        bool shouldBeVisible = ShouldBeRendered(observer.TargetStatus.Status);
        if (shouldBeVisible != currentVisible)
        {
            SetTrackingVisible(shouldBeVisible, true);
        }
        else if (shouldBeVisible)
        {
            SetTrackingVisible(true, false);
        }
    }

    private void OnDestroy()
    {
        if (observer == null) return;
        observer.OnTargetStatusChanged -= OnObserverStatusChanged;
        observer.OnBehaviourDestroyed -= OnObserverDestroyed;
        observer = null;
    }

    private void OnObserverDestroyed(ObserverBehaviour destroyedObserver)
    {
        if (destroyedObserver != null)
        {
            destroyedObserver.OnTargetStatusChanged -= OnObserverStatusChanged;
            destroyedObserver.OnBehaviourDestroyed -= OnObserverDestroyed;
        }
        observer = null;
    }

    private void OnObserverStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        if (behaviour == null) return;

        bool renderedBefore = ShouldBeRendered(previousStatus.Status);
        bool renderedNow = ShouldBeRendered(targetStatus.Status);

        if (renderedBefore != renderedNow)
        {
            SetTrackingVisible(renderedNow, true);
        }
        else if (!callbackReceivedOnce && !renderedNow)
        {
            SetTrackingVisible(false, false);
        }

        previousStatus = targetStatus;
        callbackReceivedOnce = true;
    }

    private bool ShouldBeRendered(Status status)
    {
        if (status == Status.TRACKED) return true;
        if (statusFilter == TrackingStatusFilter.Tracked_ExtendedTracked && status == Status.EXTENDED_TRACKED) return true;
        return statusFilter == TrackingStatusFilter.Tracked_ExtendedTracked_Limited
            && (status == Status.EXTENDED_TRACKED || status == Status.LIMITED);
    }

    private void SetTrackingVisible(bool visible, bool invokeEvents)
    {
        currentVisible = visible;
        SetChildHierarchyActive(visible);

        if (!invokeEvents) return;
        if (logTrackingChanges)
        {
            Debug.Log((visible ? "Target found: " : "Target lost: ") + gameObject.name + " overlayVisible=" + visible);
        }

        if (visible) onTargetFound.Invoke();
        else onTargetLost.Invoke();
    }

    private void SetChildHierarchyActive(bool visible)
    {
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            if (IsLegacyDashboard(child))
            {
                child.gameObject.SetActive(false);
                continue;
            }

            if (IsManagedOverlayRoot(child))
            {
                child.gameObject.SetActive(visible);
            }
        }
    }

    private static bool IsManagedOverlayRoot(Transform child)
    {
        if (child == null) return false;

        string name = child.name.ToLowerInvariant();
        if (name.Contains("dashboard")) return true;
        if (name.Contains("frontpanel")) return true;
        if (name.Contains("front_panel")) return true;

        if (child.GetComponent<Canvas>() != null) return true;
        if (child.GetComponent<CameraFacingDashboard>() != null) return true;
        if (child.GetComponent<ComponentDashboardUI>() != null) return true;
        if (child.GetComponent<PM2200Dashboard>() != null) return true;
        if (child.GetComponent<PLCDashboard>() != null) return true;
        if (child.GetComponent<HMIDashboard>() != null) return true;
        if (child.GetComponent<MotorDashboard>() != null) return true;
        if (child.GetComponent<SignalTowerDashboard>() != null) return true;
        if (child.GetComponent<EStopDashboard>() != null) return true;
        if (child.GetComponent<PM2200FrontPanelBuilder>() != null) return true;
        if (child.GetComponent<PLCFrontPanelBuilder>() != null) return true;
        if (child.GetComponent<HMIFrontPanelBuilder>() != null) return true;
        if (child.GetComponent<MotorFrontPanelBuilder>() != null) return true;
        if (child.GetComponent<SignalTowerFrontPanelBuilder>() != null) return true;
        if (child.GetComponent<EStopFrontPanelBuilder>() != null) return true;

        return false;
    }

    private static bool IsLegacyDashboard(Transform child)
    {
        if (child == null) return false;

        string name = child.name.ToLowerInvariant();
        if (name.Contains("pm2200_dashboard")) return true;
        if (child.GetComponent<ComponentDashboardUI>() != null) return true;
        if (child.GetComponent<PM2200DashboardBuilder>() != null) return true;
        return false;
    }

    private static void DisablePackagedHandlers()
    {
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        foreach (var behaviour in behaviours)
        {
            if (behaviour == null || behaviour.GetType().Name != "DefaultObserverEventHandler") continue;
            behaviour.enabled = false;
        }
    }
}
