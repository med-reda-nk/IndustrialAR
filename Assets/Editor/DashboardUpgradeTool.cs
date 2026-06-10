using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DashboardUpgradeTool : EditorWindow
{
    [MenuItem("Tools/Industrial AR/Upgrade Dashboards to New Architecture")]
    public static void ShowWindow()
    {
        GetWindow<DashboardUpgradeTool>("Dashboard Upgrader");
    }

    private void OnGUI()
    {
        GUILayout.Label("Upgrade Existing Dashboards", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Find and Upgrade All Dashboards"))
        {
            UpgradeAll();
        }
    }

    private static void UpgradeAll()
    {
        var bench = FindAnyObjectByType<BenchSystem>();
        if (bench == null)
        {
            Debug.LogError("BenchSystem not found in scene! Please add it first.");
            return;
        }

        var oldDashboards = FindObjectsByType<ComponentDashboardUI>(FindObjectsInactive.Include);
        int upgradedCount = 0;

        foreach (var oldUI in oldDashboards)
        {
            GameObject go = oldUI.gameObject;
            
            // Determine type based on the old UI's logic
            string parentName = go.transform.parent != null ? go.transform.parent.name : "";
            
            if (parentName.Contains("PLC"))
            {
                var newUI = GetOrAddComponent<PLCDashboard>(go);
                newUI.bench = bench;
                newUI.textPill = oldUI.pillText;
                newUI.textTitle = oldUI.titleText;
                if (newUI.textPill != null) newUI.textPill.text = "LOGIC CTRL";
                if (newUI.textTitle != null) 
                {
                    string cleanName = string.IsNullOrEmpty(parentName) ? "SIEMENS SIMATIC S7-1200" : parentName.ToUpper().Replace("IMAGETARGET", "").Trim('_').Trim();
                    newUI.textTitle.text = cleanName;
                }
                
                oldUI.dashboardType = ComponentDashboardUI.DashboardType.PLC;
                newUI.textCPUMode = oldUI.statusText;
                newUI.textScanCycle = oldUI.metricValues.Length > 0 ? oldUI.metricValues[0] : null;
                newUI.textCycleJitter = oldUI.card1Value;
                upgradedCount++;
            }
            else if (parentName.Contains("HMI"))
            {
                var newUI = GetOrAddComponent<HMIDashboard>(go);
                newUI.bench = bench;
                newUI.textPill = oldUI.pillText;
                newUI.textTitle = oldUI.titleText;
                if (newUI.textPill != null) newUI.textPill.text = "HMI PANEL";
                if (newUI.textTitle != null) 
                {
                    string cleanName = string.IsNullOrEmpty(parentName) ? "SIEMENS SIMATIC KTP700" : parentName.ToUpper().Replace("IMAGETARGET", "").Trim('_').Trim();
                    newUI.textTitle.text = cleanName;
                }
                
                oldUI.dashboardType = ComponentDashboardUI.DashboardType.HMI;
                upgradedCount++;
            }
            else if (parentName.Contains("Motor"))
            {
                var newUI = GetOrAddComponent<MotorDashboard>(go);
                newUI.bench = bench;
                newUI.textPill = oldUI.pillText;
                newUI.textTitle = oldUI.titleText;
                if (newUI.textPill != null) newUI.textPill.text = "DRIVE TRAIN";
                if (newUI.textTitle != null) 
                {
                    string cleanName = string.IsNullOrEmpty(parentName) ? "ABB 3~ ASYNC MOTOR" : parentName.ToUpper().Replace("IMAGETARGET", "").Trim('_').Trim();
                    newUI.textTitle.text = cleanName;
                }
                
                oldUI.dashboardType = ComponentDashboardUI.DashboardType.Motor;
                if (oldUI.metricValues.Length > 0) newUI.textRPM = oldUI.metricValues[0];
                if (oldUI.metricBars.Length > 0) newUI.barRPM = oldUI.metricBars[0];
                if (oldUI.metricValues.Length > 1) newUI.textTorque = oldUI.metricValues[1];
                if (oldUI.metricValues.Length > 2) newUI.textEfficiency = oldUI.metricValues[2];
                newUI.textPowerFactor = oldUI.card2Value;
                upgradedCount++;
            }
            else if (parentName.Contains("Signal"))
            {
                var newUI = GetOrAddComponent<SignalTowerDashboard>(go);
                newUI.bench = bench;
                newUI.textPill = oldUI.pillText;
                newUI.textTitle = oldUI.titleText;
                if (newUI.textPill != null) newUI.textPill.text = "VISUAL FB";
                if (newUI.textTitle != null) 
                {
                    string cleanName = string.IsNullOrEmpty(parentName) ? "WERMA KOMPAKT 37" : parentName.ToUpper().Replace("IMAGETARGET", "").Trim('_').Trim();
                    newUI.textTitle.text = cleanName;
                }
                
                oldUI.dashboardType = ComponentDashboardUI.DashboardType.SignalTower;
                upgradedCount++;
            }
            else if (parentName.Contains("Estop") || parentName.Contains("EStop"))
            {
                var newUI = GetOrAddComponent<EStopDashboard>(go);
                newUI.bench = bench;
                newUI.textPill = oldUI.pillText;
                newUI.textTitle = oldUI.titleText;
                if (newUI.textPill != null) newUI.textPill.text = "SAFETY RELAY";
                if (newUI.textTitle != null) 
                {
                    string cleanName = string.IsNullOrEmpty(parentName) ? "PILZ PNOZ s4" : parentName.ToUpper().Replace("IMAGETARGET", "").Trim('_').Trim();
                    newUI.textTitle.text = cleanName;
                }
                
                oldUI.dashboardType = ComponentDashboardUI.DashboardType.EStop;
                upgradedCount++;
            }
            // Default to PM2200
            else
            {
                var newUI = GetOrAddComponent<PM2200Dashboard>(go);
                newUI.bench = bench;
                newUI.textPill = oldUI.pillText;
                newUI.textTitle = oldUI.titleText;
                if (newUI.textPill != null) newUI.textPill.text = "PWR ANALYZER";
                if (newUI.textTitle != null) 
                {
                    string cleanName = string.IsNullOrEmpty(parentName) ? "SCHNEIDER PM2200" : parentName.ToUpper().Replace("IMAGETARGET", "").Trim('_').Trim();
                    newUI.textTitle.text = cleanName;
                }
                
                oldUI.dashboardType = ComponentDashboardUI.DashboardType.PM2200;
                if (oldUI.metricValues.Length > 0) newUI.textVoltageL1L2 = oldUI.metricValues[0];
                if (oldUI.metricValues.Length > 1) newUI.textCurrentL1 = oldUI.metricValues[1];
                if (oldUI.metricValues.Length > 2) newUI.textActivePower = oldUI.metricValues[2];
                // Map card values to advanced metrics if they exist
                newUI.textEnergyCost = oldUI.card1Value; 
                newUI.textCarbonFootprint = oldUI.card2Value;
                upgradedCount++;
            }

            // Disable old builders and UI to avoid conflicts
            oldUI.enabled = false;
            var builder = go.GetComponent("PM2200DashboardBuilder") as MonoBehaviour;
            if (builder != null) builder.enabled = false;

            // Force serialization and scene update
            EditorUtility.SetDirty(go);
            if (oldUI.pillText != null) EditorUtility.SetDirty(oldUI.pillText);
            if (oldUI.titleText != null) EditorUtility.SetDirty(oldUI.titleText);
        }

        Debug.Log($"Successfully upgraded {upgradedCount} dashboards. Titles forced to new branding.");
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }
}
