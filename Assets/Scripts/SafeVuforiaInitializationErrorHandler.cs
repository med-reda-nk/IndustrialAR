using UnityEngine;
using Vuforia;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-10000)]
public class SafeVuforiaInitializationErrorHandler : MonoBehaviour
{
    private bool subscribed;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorInstall()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying) Install();
        };
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeInstall()
    {
        Install();
    }

    public static void Install()
    {
        DisablePackagedHandlers();
        if (!Application.isPlaying) return;

        var behaviour = VuforiaBehaviour.Instance != null
            ? VuforiaBehaviour.Instance
            : FindAnyObjectByType<VuforiaBehaviour>(FindObjectsInactive.Include);

        var host = behaviour != null ? behaviour.gameObject : GameObject.FindWithTag("MainCamera");
        if (host == null) return;

        if (host.GetComponent<SafeVuforiaInitializationErrorHandler>() == null)
        {
            host.AddComponent<SafeVuforiaInitializationErrorHandler>();
        }
    }

    private void OnEnable()
    {
        DisablePackagedHandlers();
        if (Application.isPlaying) Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        var app = VuforiaApplication.Instance;
        if (app == null) return;

        app.OnVuforiaInitialized -= OnVuforiaInitialized;
        app.OnVuforiaInitialized += OnVuforiaInitialized;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (!Application.isPlaying)
        {
            subscribed = false;
            return;
        }

        var app = VuforiaApplication.Instance;
        if (app != null) app.OnVuforiaInitialized -= OnVuforiaInitialized;
        subscribed = false;
    }

    private void OnVuforiaInitialized(VuforiaInitError initError)
    {
        if (initError == VuforiaInitError.NONE) return;

        Debug.LogError(
            "Vuforia initialization failed: " + initError +
            ". Check camera permission/device availability, the Vuforia license key, and Editor play mode camera settings.");
    }

    private static void DisablePackagedHandlers()
    {
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        foreach (var behaviour in behaviours)
        {
            if (behaviour == null) continue;
            var type = behaviour.GetType();
            if (type == null || type.Name != "DefaultInitializationErrorHandler") continue;

            if (Application.isPlaying)
            {
                Destroy(behaviour);
            }
            else
            {
                DestroyImmediate(behaviour);
            }
        }
    }
}
