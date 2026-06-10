using System.Text;
using UnityEngine;
using Vuforia;

[DefaultExecutionOrder(-8500)]
public class VuforiaDetectionDiagnostics : MonoBehaviour
{
    public float logInterval = 5f;
    public bool logWhenNoTargetTracked = false;
    public bool logOnlyWhenStatusChanges = true;
    public bool warnWhenOnlyImageTargets = true;

    private float timer;
    private string lastStatusSignature;
    private bool detectionModeWarningLogged;

    private void Update()
    {
        LogDetectionModeWarningOnce();

        timer += Time.deltaTime;
        if (timer < logInterval) return;
        timer = 0f;

        var observers = FindObjectsByType<ImageTargetBehaviour>(FindObjectsInactive.Include);
        int tracked = 0;
        var builder = new StringBuilder("Vuforia target status:");

        foreach (var observer in observers)
        {
            if (observer == null) continue;
            var status = observer.TargetStatus;
            bool visible = status.Status == Status.TRACKED;
            if (visible) tracked++;
            builder.Append(" [")
                .Append(observer.gameObject.name)
                .Append(": ")
                .Append(status.Status)
                .Append("/")
                .Append(status.StatusInfo)
                .Append("]");
        }

        string statusSignature = builder.Append(" tracked=").Append(tracked).Append("/").Append(observers.Length).ToString();
        if (logOnlyWhenStatusChanges && statusSignature == lastStatusSignature) return;
        lastStatusSignature = statusSignature;

        if (tracked > 0 || logWhenNoTargetTracked)
        {
            Debug.Log(statusSignature);
        }
    }

    private void LogDetectionModeWarningOnce()
    {
        if (!warnWhenOnlyImageTargets || detectionModeWarningLogged) return;
        detectionModeWarningLogged = true;

        var imageTargets = FindObjectsByType<ImageTargetBehaviour>(FindObjectsInactive.Include);
        var modelTargets = FindObjectsByType<ModelTargetBehaviour>(FindObjectsInactive.Include);
        var areaTargets = FindObjectsByType<AreaTargetBehaviour>(FindObjectsInactive.Include);

        if (imageTargets.Length == 0 || modelTargets.Length > 0 || areaTargets.Length > 0) return;

        Debug.LogWarning(
            "IndustrialAR detection mode is IMAGE TARGET only. " +
            "This detects printed markers or component images, including a phone displaying the marker. " +
            "It will not reliably detect unmarked real 3D components. " +
            "For lab testing, print Docs/Markers/IndustrialAR_Physical_Marker_Sheet.pdf and attach the markers to the real components. " +
            "For markerless real component recognition, import a Vuforia Model Target database generated from CAD/3D scans.");
    }
}
