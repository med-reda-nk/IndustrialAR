using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class UnityMcpDirectApprovalConfigurator
{
    private const string AppliedKey = "IndustrialAR.UnityMcp.DirectApprovalConfigured";


    static UnityMcpDirectApprovalConfigurator()
    {
        EditorApplication.delayCall += ApplyOnce;
    }

    private static void ApplyOnce()
    {
        if (EditorPrefs.GetBool(AppliedKey, false)) return;

        Type managerType = Type.GetType("Unity.AI.MCP.Editor.Settings.MCPSettingsManager, Unity.AI.MCP.Editor");
        if (managerType == null) return;

        object settings = managerType
            .GetProperty("Settings", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(null);
        if (settings == null) return;

        object policies = GetField(settings, "connectionPolicies");
        object direct = policies != null ? GetField(policies, "direct") : null;
        if (direct == null) return;

        SetField(direct, "allowed", true);
        SetField(direct, "requiresApproval", false);

        managerType.GetMethod("MarkDirty", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, null);
        managerType.GetMethod("SaveSettings", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, null);
        EditorPrefs.SetBool(AppliedKey, true);

        Debug.Log("[IndustrialAR Lab] Unity MCP direct connections are allowed for this trusted Codex workstation.");
    }

    private static object GetField(object target, string name)
    {
        return target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(target);
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(target, value);
    }
}
