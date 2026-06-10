using UnityEditor;
using UnityEngine;
using Vuforia;

[InitializeOnLoad]
public static class VuforiaPlayModeReloadGuard
{
    static VuforiaPlayModeReloadGuard()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= StopPlayModeBeforeReload;
        AssemblyReloadEvents.beforeAssemblyReload += StopPlayModeBeforeReload;
    }

    private static void StopPlayModeBeforeReload()
    {
        if (!EditorApplication.isPlaying) return;
        if (UnityEngine.Object.FindAnyObjectByType<VuforiaBehaviour>(FindObjectsInactive.Include) == null) return;

        Debug.Log("[IndustrialAR Lab] Script reload requested while Vuforia is running. Stopping Play Mode first to avoid the Vuforia black-screen state. Press Play again after compilation finishes.");
        EditorApplication.isPlaying = false;
    }
}
