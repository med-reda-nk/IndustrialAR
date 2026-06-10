using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

[System.Serializable]
public class TelemetryData
{
    public float voltage;
    public float vab;
    public float vbc;
    public float vca;
    public float current;
    public float power;
    public float frequency;
    public float powerFactor;
    public float pf;
    public bool isPowered;
    public float targetFrequency;
    public bool estop_active;
    public bool vfdPowered;
    public float motorRPM;
    public float torque;
    public float motorTemp;
    public float vibration;
    public float efficiency;
    public bool plcRunning;
    public string cpuMode;
    public float scanCycleMs;
    public int faultCode;
    public string faultDescription;
    public bool plcConnected;
    public string activeScreen;
    public int alarmCount;
    public int warningCount;
    public bool lightGreen;
    public bool lightOrange;
    public bool lightRed;
    public bool lightBlinking;
    public string towerState;
    public string timestamp;
}

[System.Serializable]
public class Pm2230LatestResponse
{
    public string device;
    public string timestamp;
    public Pm2230Measurements data;
}

[System.Serializable]
public class Pm2230Measurements
{
    public float va;
    public float vb;
    public float vc;
    public float v_ln_avg;
    public float vab;
    public float vbc;
    public float vca;
    public float v_ll_avg;
    public float ia;
    public float ib;
    public float ic;
    public float i_n;
    public float i_avg;
    public float p_total;
    public float q_total;
    public float s_total;
    public float pf;
    public float pa;
    public float pb;
    public float pc;
    public float qa;
    public float qb;
    public float qc;
    public float freq;
    public float energy_kwh;
    public float thd_va;
    public float thd_vb;
    public float thd_vc;
    public float thd_ia;
    public float thd_ib;
    public float thd_ic;
    public float unb_v;
    public float unb_i;
}

[System.Serializable]
public class TwinDataResponse
{
    public string timestamp;
    public TwinVfdData vfd;
    public TwinMotorData motor;
    public TwinElectricalData electrical;
}

[System.Serializable]
public class TwinVfdData
{
    public string state;
    public float output_frequency_Hz;
    public float output_current_A;
    public float thermal_state_pct;
    public float health_score;
}

[System.Serializable]
public class TwinMotorData
{
    public string state;
    public float rotor_speed_rpm;
    public float synchronous_speed_rpm;
    public float load_pct;
    public float torque_Nm;
    public float mechanical_power_kW;
    public float estimated_temperature_C;
    public float efficiency_pct;
    public float vibration_mm_s;
    public float health_score;
    public bool anomaly_detected;
}

[System.Serializable]
public class TwinElectricalData
{
    public float voltage_LL_V;
    public float current_avg_A;
    public float power_active_kW;
    public float power_factor;
    public float frequency_Hz;
}

[System.Serializable]
public class ScadaSnapshotData
{
    public string timestamp;
    public float Vab;
    public float Vbc;
    public float Vca;
    public float Vavg;
    public float Vavg_LL;
    public float Van;
    public float Vbn;
    public float Vcn;
    public float VavgLN;
    public float Vavg_LN;
    public float Ia;
    public float Ib;
    public float Ic;
    public float Iavg;
    public float P;
    public float Q;
    public float S;
    public float PF;
    public float PFtot;
    public float E;
    public float Hz;
}

[System.Serializable]
public class NodeRedCommandPayload
{
    public string source = "IndustrialAR";
    public string device;
    public string command;
    public string value;
    public float numericValue;
    public string timestamp;
}

[System.Serializable]
public class NodeRedStatePayload
{
    public string source = "IndustrialAR";
    public string timestamp;
    public float voltage;
    public float current;
    public float power;
    public float frequency;
    public float targetFrequency;
    public bool vfdPowered;
    public float motorRPM;
    public float torque;
    public float motorTemp;
    public float vibration;
    public float efficiency;
    public bool plcRunning;
    public string cpuMode;
    public int faultCode;
    public string faultDescription;
    public bool plcConnected;
    public string activeScreen;
    public int alarmCount;
    public int warningCount;
    public bool lightGreen;
    public bool lightOrange;
    public bool lightRed;
    public bool lightBlinking;
    public string towerState;
    public bool eStopPressed;
    public bool safetyCircuitOK;
    public string lastOperatorAction;
}

public class NodeRedClient : MonoBehaviour
{
    public static NodeRedClient Instance;

    [Header("Node-RED Configuration")]
    public string nodeRedBaseUrl = "http://200.200.200.177:1880";
    public string telemetryPath = "/twin-data";
    public string legacyTelemetryPath = "/telemetry";
    public string twinDataPath = "/twin-data";
    public bool autoFallbackTelemetryPath = true;
    public bool autoDisableOutputsOnReadOnlyApi = true;
    public string nodeRedUrl = "http://200.200.200.177:1880/twin-data";
    public string commandUrl = "http://200.200.200.177:1880/commands";
    public string stateUrl = "http://200.200.200.177:1880/state";
    public string dashboardUrl = "http://200.200.200.177:1880/ui";
    public string nodeRedPassword = "";
    public string passwordHeaderName = "X-Node-RED-Password";
    public bool sendBearerToken = true;
    public bool readDashboardSocketValues = true;
    public string dashboardSocketPath = "/ui/socket.io/";
    public float scadaAuthoritySeconds = 5f;
    public float pollInterval = 1f;
    public float dashboardSocketPollInterval = 1f;
    public float publishStateInterval = 1f;
    public int requestTimeoutSeconds = 4;
    public int maxPayloadBytes = 1024 * 1024;
    
    [Header("Mode")]
    public bool useSimulation = true;
    public bool sendCommandsWhenLive = true;
    public bool publishStateWhenLive = true;

    [Header("Connection Resilience")]
    public bool preflightLiveConnection = true;
    public float preflightTimeoutSeconds = 1.25f;
    public bool pauseLiveAfterRepeatedFailures = false;
    public int maxConsecutiveTelemetryFailures = 3;

    [Header("Status")]
    public string lastSuccessfulPollTime;
    public string lastSuccessfulCommandTime;
    public string lastStatePublishTime;
    public string lastError;
    public int maxConnectionLogLines = 10;
    [TextArea(4, 10)] public string connectionLog = "Node-RED bridge idle.";

    private VFDController vfd;
    private BenchSystem bench;
    private Coroutine pollRoutine;
    private Coroutine publishRoutine;
    private Coroutine dashboardSocketRoutine;
    private int consecutiveTelemetryFailures;
    private string lastTelemetryFailureSignature;
    private string dashboardSocketSessionId;
    private float lastScadaSocketTelemetryTime = -999f;
    private string lastScadaSocketLogSecond;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ResolveReferences();
        ConfigureFromBaseUrl(nodeRedBaseUrl);
        pollRoutine = StartCoroutine(PollData());
        publishRoutine = StartCoroutine(PublishStateLoop());
        dashboardSocketRoutine = StartCoroutine(PollDashboardSocketLoop());
    }

    public void ToggleSimulation()
    {
        useSimulation = !useSimulation;
        if (!useSimulation) ResetTelemetryFailureState();
    }

    public void UseLiveData()
    {
        useSimulation = false;
        pauseLiveAfterRepeatedFailures = false;
        ResetTelemetryFailureState();
        ConfigureFromBaseUrl(nodeRedBaseUrl);
    }

    public void UseSimulation()
    {
        useSimulation = true;
    }

    public void ConfigureFromBaseUrl(string baseUrl)
    {
        nodeRedBaseUrl = NormalizeBaseUrl(baseUrl);
        nodeRedUrl = BuildEndpointUrl(telemetryPath);
        ResetTelemetryFailureState();
        ConfigureOutputEndpointsForTelemetryPath(telemetryPath);
        dashboardUrl = nodeRedBaseUrl + "/ui";
        AppendConnectionLog("Configured " + nodeRedBaseUrl);
    }

    public void SendCommand(string device, string command)
    {
        SendCommand(device, command, string.Empty, 0f);
    }

    public void SendCommand(string device, string command, string value)
    {
        SendCommand(device, command, value, 0f);
    }

    public void SendCommand(string device, string command, float numericValue)
    {
        SendCommand(device, command, numericValue.ToString("0.###"), numericValue);
    }

    public void SendCommand(string device, string command, string value, float numericValue)
    {
        if (!sendCommandsWhenLive || useSimulation || !isActiveAndEnabled || string.IsNullOrEmpty(commandUrl)) return;

        var payload = new NodeRedCommandPayload
        {
            device = device,
            command = command,
            value = value,
            numericValue = numericValue,
            timestamp = System.DateTime.UtcNow.ToString("o")
        };

        StartCoroutine(PostJson(commandUrl, JsonUtility.ToJson(payload), "command"));
    }

    IEnumerator PollData()
    {
        while (true)
        {
            if (!useSimulation)
            {
                ResolveReferences();

                if (preflightLiveConnection)
                {
                    bool endpointReachable = false;
                    string preflightError = string.Empty;
                    yield return CheckEndpointReachable(nodeRedUrl, (reachable, error) =>
                    {
                        endpointReachable = reachable;
                        preflightError = error;
                    });

                    if (!endpointReachable)
                    {
                        RegisterTelemetryFailure(preflightError);
                        yield return new WaitForSeconds(pollInterval);
                        continue;
                    }
                }

                using (UnityWebRequest webRequest = UnityWebRequest.Get(nodeRedUrl))
                {
                    ApplyAuthentication(webRequest);
                    webRequest.timeout = requestTimeoutSeconds;

                    UnityWebRequestAsyncOperation operation = null;
                    bool requestBlocked = false;
                    try
                    {
                        operation = webRequest.SendWebRequest();
                    }
                    catch (System.Exception e)
                    {
                        RegisterTelemetryFailure("Telemetry request blocked: " + e.Message);
                        requestBlocked = true;
                    }

                    if (requestBlocked)
                    {
                        yield return new WaitForSeconds(pollInterval);
                        continue;
                    }

                    yield return operation;

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        string json = webRequest.downloadHandler.text;
                        lastError = string.Empty;
                        ResetTelemetryFailureState();
                        
                        // Safety Check: Avoid processing massive strings that cause OOM
                        if (json.Length > maxPayloadBytes)
                        {
                            lastError = "Payload too large: " + json.Length + " bytes";
                            Debug.LogError("Node-RED payload too large: " + json.Length + " bytes. Ignoring to prevent OOM.");
                            yield return new WaitForSeconds(pollInterval);
                            continue;
                        }

                        try
                        {
                            string telemetrySource = ApplyTelemetryPayload(json);
                            lastSuccessfulPollTime = ExtractTimestamp(json);
                            AppendConnectionLog("Telemetry OK " + lastSuccessfulPollTime);
                            if (!string.IsNullOrEmpty(telemetrySource)) AppendConnectionLog("Source " + telemetrySource);
                        }
                        catch (System.Exception e)
                        {
                            lastError = e.Message;
                            AppendConnectionLog("Telemetry parse error: " + e.Message);
                            Debug.LogWarning("Failed to parse Node-RED data: " + e.Message);
                        }
                    }
                    else
                    {
                        string responseBody = webRequest.downloadHandler != null ? webRequest.downloadHandler.text : string.Empty;
                        string failure = BuildHttpFailure("Telemetry", webRequest.responseCode, webRequest.error, responseBody);
                        if (!TryFallbackTelemetryEndpoint(webRequest.responseCode, responseBody, webRequest.error))
                        {
                            RegisterTelemetryFailure(failure);
                        }
                    }
                }
            }
            yield return new WaitForSeconds(pollInterval);
        }
    }

    private IEnumerator PublishStateLoop()
    {
        while (true)
        {
            if (!useSimulation && publishStateWhenLive)
            {
                ResolveReferences();
                if (bench != null)
                {
                    var payload = CreateStatePayload();
                    yield return PostJson(stateUrl, JsonUtility.ToJson(payload), "state");
                }
            }

            yield return new WaitForSeconds(publishStateInterval);
        }
    }

    private IEnumerator PollDashboardSocketLoop()
    {
        while (true)
        {
            if (!useSimulation && readDashboardSocketValues)
            {
                if (string.IsNullOrEmpty(dashboardSocketSessionId))
                {
                    yield return OpenDashboardSocket();
                }

                if (!string.IsNullOrEmpty(dashboardSocketSessionId))
                {
                    yield return PollDashboardSocket();
                }
            }
            else
            {
                dashboardSocketSessionId = string.Empty;
            }

            yield return new WaitForSeconds(dashboardSocketPollInterval);
        }
    }

    private IEnumerator OpenDashboardSocket()
    {
        string url = BuildDashboardSocketUrl(string.Empty);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = requestTimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                dashboardSocketSessionId = string.Empty;
                yield break;
            }

            dashboardSocketSessionId = ExtractStringField(request.downloadHandler.text, "sid");
        }

        if (string.IsNullOrEmpty(dashboardSocketSessionId)) yield break;

        byte[] body = Encoding.UTF8.GetBytes("40");
        using (var request = new UnityWebRequest(BuildDashboardSocketUrl(dashboardSocketSessionId), UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "text/plain;charset=UTF-8");
            request.timeout = requestTimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                dashboardSocketSessionId = string.Empty;
            }
        }
    }

    private IEnumerator PollDashboardSocket()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(BuildDashboardSocketUrl(dashboardSocketSessionId)))
        {
            request.timeout = requestTimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                dashboardSocketSessionId = string.Empty;
                yield break;
            }

            string body = request.downloadHandler.text;
            if (ContainsScadaSnapshotFields(body))
            {
                ApplyScadaSnapshotTelemetry(null, body);
                lastScadaSocketTelemetryTime = Time.time;
                lastError = string.Empty;
                string second = System.DateTime.Now.ToString("HH:mm:ss");
                if (second != lastScadaSocketLogSecond)
                {
                    lastScadaSocketLogSecond = second;
                    AppendConnectionLog("Dashboard SCADA socket OK " + second);
                }
            }
        }
    }

    private IEnumerator PostJson(string url, string json, string label)
    {
        if (string.IsNullOrWhiteSpace(url)) yield break;

        if (preflightLiveConnection)
        {
            bool endpointReachable = false;
            string preflightError = string.Empty;
            yield return CheckEndpointReachable(url, (reachable, error) =>
            {
                endpointReachable = reachable;
                preflightError = error;
            });

            if (!endpointReachable)
            {
                lastError = label + " endpoint unavailable: " + preflightError;
                AppendConnectionLog(lastError);
                yield break;
            }
        }

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            ApplyAuthentication(request);
            request.timeout = requestTimeoutSeconds;

            UnityWebRequestAsyncOperation operation = null;
            bool requestBlocked = false;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (System.Exception e)
            {
                lastError = label + " request blocked: " + e.Message;
                AppendConnectionLog(lastError);
                requestBlocked = true;
            }

            if (requestBlocked)
            {
                yield break;
            }

            yield return operation;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string stamp = System.DateTime.Now.ToString("HH:mm:ss");
                if (label == "command") lastSuccessfulCommandTime = stamp;
                if (label == "state") lastStatePublishTime = stamp;
                lastError = string.Empty;
                AppendConnectionLog(label + " POST OK " + stamp);
            }
            else
            {
                lastError = BuildHttpFailure(label + " output", request.responseCode, request.error, request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                AppendConnectionLog(lastError);
                Debug.LogWarning("Node-RED " + lastError);
            }
        }
    }

    private void ApplyTelemetry(TelemetryData data, string json)
    {
        ResolveReferences();
        float voltageAB = PickFirstNonZero(
            ExtractNumberField(json, "Vab"),
            ExtractNumberField(json, "vab"),
            data.vab);
        float voltageBC = PickFirstNonZero(
            ExtractNumberField(json, "Vbc"),
            ExtractNumberField(json, "vbc"),
            data.vbc);
        float voltageCA = PickFirstNonZero(
            ExtractNumberField(json, "Vca"),
            ExtractNumberField(json, "vca"),
            data.vca);
        float averageVoltage = PickFirstNonZero(
            data.voltage,
            AverageNonZero(voltageAB, voltageBC, voltageCA));
        float frequencyValue = PickFirstNonZero(
            data.frequency,
            ExtractNumberField(json, "freq"),
            ExtractNumberField(json, "frequency_Hz"),
            ExtractNumberField(json, "output_frequency_Hz"));
        float powerFactorValue = PickFirstNonZero(
            data.powerFactor,
            data.pf,
            ExtractNumberField(json, "facteur puissance"),
            ExtractNumberField(json, "facteur_puissance"),
            ExtractNumberField(json, "power_factor"),
            ExtractNumberField(json, "power factor"));

        if (vfd != null)
        {
            if (HasField(json, "voltage") || voltageAB > 0f || voltageBC > 0f || voltageCA > 0f) vfd.voltage = averageVoltage;
            if (HasField(json, "current")) vfd.current = data.current;
            if (HasField(json, "power")) vfd.power = data.power;
            if (HasField(json, "frequency") || HasField(json, "freq") || HasField(json, "output_frequency_Hz")) vfd.frequency = frequencyValue;
            if (HasField(json, "isPowered")) vfd.isPowered = data.isPowered;
        }

        if (bench == null) return;

        if (HasField(json, "voltage") || voltageAB > 0f || voltageBC > 0f || voltageCA > 0f) bench.voltage = averageVoltage;
        if (voltageAB > 0f) bench.voltageAB = voltageAB;
        if (voltageBC > 0f) bench.voltageBC = voltageBC;
        if (voltageCA > 0f) bench.voltageCA = voltageCA;
        if (HasField(json, "current")) bench.current = data.current;
        if (HasField(json, "power")) bench.power = data.power;
        if (HasField(json, "frequency") || HasField(json, "freq") || HasField(json, "output_frequency_Hz")) bench.frequency = frequencyValue;
        if (powerFactorValue > 0f) bench.powerFactor = powerFactorValue;
        if (HasField(json, "targetFrequency")) bench.targetFrequency = data.targetFrequency;
        if (HasField(json, "vfdPowered")) bench.vfdPowered = data.vfdPowered;
        else if (HasField(json, "isPowered")) bench.vfdPowered = data.isPowered;

        if (HasField(json, "motorRPM")) bench.motorRPM = data.motorRPM;
        if (HasField(json, "torque")) bench.torque = data.torque;
        if (HasField(json, "motorTemp")) bench.motorTemp = data.motorTemp;
        if (HasField(json, "vibration")) bench.vibration = data.vibration;
        if (HasField(json, "efficiency")) bench.efficiency = data.efficiency;

        if (HasField(json, "plcRunning")) bench.plcRunning = data.plcRunning;
        if (HasField(json, "cpuMode")) bench.cpuMode = data.cpuMode;
        if (HasField(json, "scanCycleMs")) bench.scanCycleMs = data.scanCycleMs;
        if (HasField(json, "faultCode")) bench.faultCode = data.faultCode;
        if (HasField(json, "faultDescription")) bench.faultDescription = data.faultDescription;

        if (HasField(json, "plcConnected")) bench.plcConnected = data.plcConnected;
        if (HasField(json, "activeScreen")) bench.activeScreen = data.activeScreen;
        if (HasField(json, "alarmCount")) bench.alarmCount = data.alarmCount;
        if (HasField(json, "warningCount")) bench.warningCount = data.warningCount;

        if (HasField(json, "lightGreen")) bench.lightGreen = data.lightGreen;
        if (HasField(json, "lightOrange")) bench.lightOrange = data.lightOrange;
        if (HasField(json, "lightRed")) bench.lightRed = data.lightRed;
        if (HasField(json, "lightBlinking")) bench.lightBlinking = data.lightBlinking;
        if (HasField(json, "towerState")) bench.towerState = data.towerState;

        if (HasField(json, "estop_active") && data.estop_active != bench.eStopPressed)
        {
            bench.suppressNodeRedCommandOutput = true;
            if (data.estop_active) bench.EStopPress();
            else bench.EStopRelease();
            bench.suppressNodeRedCommandOutput = false;
        }
    }

    private string ApplyTelemetryPayload(string json)
    {
        if (IsScadaSnapshotPayload(json))
        {
            var response = JsonUtility.FromJson<ScadaSnapshotData>(json);
            ApplyScadaSnapshotTelemetry(response, json);
            return "SCADA dashboard snapshot";
        }

        if (IsPm2230LatestPayload(json))
        {
            var response = JsonUtility.FromJson<Pm2230LatestResponse>(json);
            ApplyPm2230Telemetry(response, json);
            return string.IsNullOrEmpty(response.device) ? "PM2230 API" : response.device + " API";
        }

        if (IsTwinDataPayload(json))
        {
            var response = JsonUtility.FromJson<TwinDataResponse>(json);
            ApplyTwinTelemetry(response, json);
            return "Digital twin";
        }

        var data = JsonUtility.FromJson<TelemetryData>(json);
        ApplyTelemetry(data, json);
        return "IndustrialAR bridge";
    }

    private void ApplyPm2230Telemetry(Pm2230LatestResponse response, string json)
    {
        ResolveReferences();
        var data = response != null ? response.data : null;
        if (data == null) return;

        float voltageValue = PickFirstNonZero(data.v_ll_avg, AverageNonZero(data.vab, data.vbc, data.vca), data.vab, data.vbc, data.vca);
        float currentValue = PickFirstNonZero(data.i_avg, AverageNonZero(data.ia, data.ib, data.ic), data.ia, data.ib, data.ic);
        float powerValue = PickFirstNonZero(data.p_total, data.pa + data.pb + data.pc);
        float frequencyValue = data.freq;
        float powerFactorValue = PickFirstNonZero(
            data.pf,
            ExtractNumberField(json, "facteur puissance"),
            ExtractNumberField(json, "facteur_puissance"),
            ExtractNumberField(json, "power_factor"),
            ExtractNumberField(json, "power factor"));

        if (vfd != null)
        {
            vfd.voltage = voltageValue;
            vfd.current = currentValue;
            vfd.power = powerValue;
            vfd.frequency = frequencyValue;
            vfd.isPowered = frequencyValue > 1f || currentValue > 0.05f || powerValue > 0.01f;
        }

        if (bench == null) return;

        bench.voltage = voltageValue;
        bench.voltageAB = data.vab;
        bench.voltageBC = data.vbc;
        bench.voltageCA = data.vca;
        bench.current = currentValue;
        bench.power = powerValue;
        bench.frequency = frequencyValue;
        bench.powerFactor = powerFactorValue;
        bench.energy = data.energy_kwh * 1000f;
        bench.harmonicDistortion = AverageNonZero(data.thd_ia, data.thd_ib, data.thd_ic, data.thd_va, data.thd_vb, data.thd_vc);
        bench.harmonicDistortionPM = bench.harmonicDistortion;
        bench.voltageUnbalance = data.unb_v;
        bench.currentUnbalance = data.unb_i;
        bench.vfdPowered = frequencyValue > 1f || currentValue > 0.05f || powerValue > 0.01f;
        bench.plcConnected = true;
        bench.pm2200DataUpdating = true;
        bench.lastDataUpdateTime = Time.time;
        bench.activeScreen = string.IsNullOrEmpty(response.device) ? "PM2230" : response.device;
        bench.warningCount = data.unb_v > 5f || data.unb_i > 15f || bench.harmonicDistortion > 8f ? 1 : 0;
        bench.alarmCount = voltageValue <= 0f && currentValue <= 0f ? 1 : 0;
        bench.cpuMode = bench.vfdPowered ? "RUN" : "STOP";
    }

    private void ApplyScadaSnapshotTelemetry(ScadaSnapshotData data, string json)
    {
        ResolveReferences();

        float voltageAB = PickFirstNonZero(
            data != null ? data.Vab : 0f,
            ExtractNumberField(json, "Vab"),
            ExtractNumberField(json, "Voltage_AB"));
        float voltageBC = PickFirstNonZero(
            data != null ? data.Vbc : 0f,
            ExtractNumberField(json, "Vbc"),
            ExtractNumberField(json, "Voltage_BC"));
        float voltageCA = PickFirstNonZero(
            data != null ? data.Vca : 0f,
            ExtractNumberField(json, "Vca"),
            ExtractNumberField(json, "Voltage_CA"),
            ExtractNumberField(json, "Voltage_AC"));
        float voltageAverage = PickFirstNonZero(
            data != null ? data.Vavg : 0f,
            data != null ? data.Vavg_LL : 0f,
            ExtractNumberField(json, "Vavg"),
            ExtractNumberField(json, "Vavg_LL"),
            AverageNonZero(voltageAB, voltageBC, voltageCA));
        float currentAverage = PickFirstNonZero(
            data != null ? data.Iavg : 0f,
            ExtractNumberField(json, "Iavg"),
            AverageNonZero(data != null ? data.Ia : 0f, data != null ? data.Ib : 0f, data != null ? data.Ic : 0f));
        float powerActive = PickFirstNonZero(
            data != null ? data.P : 0f,
            ExtractNumberField(json, "P"),
            ExtractNumberField(json, "Ptot"),
            ExtractNumberField(json, "P_total"));
        float frequencyValue = PickFirstNonZero(
            data != null ? data.Hz : 0f,
            ExtractNumberField(json, "Hz"),
            ExtractNumberField(json, "frequency_Hz"),
            ExtractNumberField(json, "output_frequency_Hz"));
        float powerFactorValue = PickFirstNonZero(
            data != null ? data.PF : 0f,
            data != null ? data.PFtot : 0f,
            ExtractNumberField(json, "PF"),
            ExtractNumberField(json, "PFtot"),
            ExtractNumberField(json, "PF_total"),
            ExtractNumberField(json, "facteur puissance"),
            ExtractNumberField(json, "facteur_puissance"),
            ExtractNumberField(json, "Power_Factor"));

        bool running = frequencyValue > 1f || currentAverage > 0.05f || powerActive > 0.01f;

        if (vfd != null)
        {
            if (voltageAverage > 0f) vfd.voltage = voltageAverage;
            if (currentAverage > 0f) vfd.current = currentAverage;
            if (powerActive > 0f) vfd.power = powerActive;
            if (frequencyValue > 0f) vfd.frequency = frequencyValue;
            vfd.isPowered = running;
        }

        if (bench == null) return;

        if (voltageAverage > 0f) bench.voltage = voltageAverage;
        if (voltageAB > 0f) bench.voltageAB = voltageAB;
        if (voltageBC > 0f) bench.voltageBC = voltageBC;
        if (voltageCA > 0f) bench.voltageCA = voltageCA;
        if (currentAverage > 0f) bench.current = currentAverage;
        if (powerActive > 0f) bench.power = powerActive;
        if (frequencyValue > 0f) bench.frequency = frequencyValue;
        if (powerFactorValue > 0f) bench.powerFactor = powerFactorValue;
        float energyKWh = PickFirstNonZero(data != null ? data.E : 0f, ExtractNumberField(json, "E"), ExtractNumberField(json, "Energy_kWh"));
        if (energyKWh > 0f) bench.energy = energyKWh * 1000f;
        bench.vfdPowered = running;
        bench.plcConnected = true;
        bench.pm2200DataUpdating = true;
        bench.lastDataUpdateTime = Time.time;
        bench.activeScreen = "SCADA";
        bench.cpuMode = running ? "RUN" : "STOP";
    }

    private void ApplyTwinTelemetry(TwinDataResponse response, string json)
    {
        ResolveReferences();
        if (response == null) return;

        float voltageAB = PickFirstNonZero(
            ExtractNumberField(json, "Vab"),
            ExtractNumberField(json, "Voltage_AB"));
        float voltageBC = PickFirstNonZero(
            ExtractNumberField(json, "Vbc"),
            ExtractNumberField(json, "Voltage_BC"));
        float voltageCA = PickFirstNonZero(
            ExtractNumberField(json, "Vca"),
            ExtractNumberField(json, "Voltage_CA"),
            ExtractNumberField(json, "Voltage_AC"));
        float voltageValue = PickFirstNonZero(
            ExtractNumberField(json, "Vavg"),
            ExtractNumberField(json, "Vavg_LL"),
            AverageNonZero(voltageAB, voltageBC, voltageCA),
            response.electrical != null ? response.electrical.voltage_LL_V : 0f);
        float currentValue = response.electrical != null ? response.electrical.current_avg_A : 0f;
        float powerValue = response.electrical != null ? response.electrical.power_active_kW : 0f;
        float frequencyValue = PickFirstNonZero(
            response.vfd != null ? response.vfd.output_frequency_Hz : 0f,
            response.electrical != null ? response.electrical.frequency_Hz : 0f);
        float powerFactorValue = PickFirstNonZero(
            ExtractNumberField(json, "PF"),
            ExtractNumberField(json, "PFtot"),
            ExtractNumberField(json, "PF_total"),
            ExtractNumberField(json, "facteur puissance"),
            ExtractNumberField(json, "facteur_puissance"),
            response.electrical != null ? response.electrical.power_factor : 0f);
        bool running = (response.motor != null && response.motor.state == "RUNNING") || (response.vfd != null && response.vfd.state == "RUNNING");
        bool scadaOwnsElectrical = HasFreshScadaSocketTelemetry();

        if (vfd != null)
        {
            if (!scadaOwnsElectrical)
            {
                vfd.voltage = voltageValue;
                vfd.current = currentValue;
                vfd.power = powerValue;
            }
            vfd.frequency = frequencyValue;
            vfd.isPowered = running;
        }

        if (bench == null) return;

        if (!scadaOwnsElectrical)
        {
            bench.voltage = voltageValue;
            bench.voltageAB = voltageAB > 0f ? voltageAB : voltageValue;
            bench.voltageBC = voltageBC > 0f ? voltageBC : voltageValue;
            bench.voltageCA = voltageCA > 0f ? voltageCA : voltageValue;
            bench.current = currentValue;
            bench.power = powerValue;
            bench.powerFactor = powerFactorValue > 0f ? powerFactorValue : bench.powerFactor;
        }
        bench.frequency = frequencyValue;
        bench.vfdPowered = running;
        bench.motorRPM = response.motor != null ? response.motor.rotor_speed_rpm : bench.motorRPM;
        bench.targetRPM = response.motor != null ? response.motor.synchronous_speed_rpm : bench.targetRPM;
        bench.torque = response.motor != null ? response.motor.torque_Nm : bench.torque;
        bench.motorTemp = response.motor != null ? response.motor.estimated_temperature_C : bench.motorTemp;
        bench.temperature = bench.motorTemp;
        bench.vibration = response.motor != null ? response.motor.vibration_mm_s : bench.vibration;
        bench.efficiency = response.motor != null ? response.motor.efficiency_pct : bench.efficiency;
        bench.motorFault = response.motor != null && response.motor.anomaly_detected;
        bench.cpuMode = running ? "RUN" : "STOP";
        bench.plcConnected = true;
        bench.activeScreen = "TWIN";
        bench.warningCount = bench.motorFault ? 1 : 0;
        bench.alarmCount = 0;
    }

    private static bool HasField(string json, string fieldName)
    {
        return !string.IsNullOrEmpty(json)
            && json.IndexOf("\"" + fieldName + "\"", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsPm2230LatestPayload(string json)
    {
        return HasField(json, "data")
            && (HasField(json, "freq") || HasField(json, "v_ll_avg") || HasField(json, "i_avg") || HasField(json, "p_total"));
    }

    private static bool IsScadaSnapshotPayload(string json)
    {
        return !HasField(json, "electrical")
            && (HasField(json, "Vab") || HasField(json, "Vbc") || HasField(json, "Vca") || HasField(json, "Vavg") || HasField(json, "Vavg_LL"))
            && (HasField(json, "PF") || HasField(json, "PFtot") || HasField(json, "PF_total") || HasField(json, "facteur puissance"));
    }

    private bool HasFreshScadaSocketTelemetry()
    {
        return readDashboardSocketValues
            && !string.IsNullOrEmpty(dashboardSocketSessionId)
            && Time.time - lastScadaSocketTelemetryTime <= Mathf.Max(0.5f, scadaAuthoritySeconds);
    }

    private static bool IsTwinDataPayload(string json)
    {
        return HasField(json, "electrical")
            && HasField(json, "motor")
            && HasField(json, "vfd");
    }

    private static float AverageNonZero(params float[] values)
    {
        float sum = 0f;
        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (Mathf.Abs(values[i]) <= 0.0001f) continue;
            sum += values[i];
            count++;
        }

        return count > 0 ? sum / count : 0f;
    }

    private static float PickFirstNonZero(params float[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (Mathf.Abs(values[i]) > 0.0001f) return values[i];
        }

        return 0f;
    }

    private static float ExtractNumberField(string json, string fieldName)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(fieldName)) return 0f;

        int keyIndex = json.IndexOf("\"" + fieldName + "\"", StringComparison.OrdinalIgnoreCase);
        if (keyIndex < 0) return 0f;

        int colonIndex = json.IndexOf(':', keyIndex);
        if (colonIndex < 0) return 0f;

        int valueStart = colonIndex + 1;
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart])) valueStart++;

        if (valueStart < json.Length && json[valueStart] == '"') valueStart++;

        int valueEnd = valueStart;
        while (valueEnd < json.Length)
        {
            char c = json[valueEnd];
            bool valid = char.IsDigit(c) || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E';
            if (!valid) break;
            valueEnd++;
        }

        if (valueEnd <= valueStart) return 0f;

        string raw = json.Substring(valueStart, valueEnd - valueStart);
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
    }

    private static string BuildHttpFailure(string label, long responseCode, string error, string responseBody)
    {
        var builder = new StringBuilder();
        builder.Append(label);
        builder.Append(" failed: ");
        builder.Append(responseCode);

        if (responseCode == 503) builder.Append(" Service Unavailable");
        else if (responseCode == 401) builder.Append(" Unauthorized");
        else if (responseCode == 404) builder.Append(" Not Found");

        if (!string.IsNullOrWhiteSpace(error) && builder.ToString().IndexOf(error, System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            builder.Append(" ");
            builder.Append(error.Trim());
        }

        string body = CompactLogText(responseBody, 170);
        if (!string.IsNullOrEmpty(body))
        {
            builder.Append(" | ");
            builder.Append(body);
        }

        return builder.ToString();
    }

    private static string CompactLogText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);
        bool previousSpace = false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool isSpace = char.IsWhiteSpace(c);
            if (isSpace)
            {
                if (previousSpace) continue;
                builder.Append(' ');
                previousSpace = true;
                continue;
            }

            builder.Append(c);
            previousSpace = false;
        }

        string compact = builder.ToString().Trim();
        if (compact.Length <= maxLength) return compact;
        return compact.Substring(0, maxLength - 3) + "...";
    }

    private string ExtractTimestamp(string json)
    {
        if (IsPm2230LatestPayload(json))
        {
            var response = JsonUtility.FromJson<Pm2230LatestResponse>(json);
            return !string.IsNullOrEmpty(response.timestamp) ? response.timestamp : System.DateTime.Now.ToString("HH:mm:ss");
        }

        if (IsTwinDataPayload(json))
        {
            var response = JsonUtility.FromJson<TwinDataResponse>(json);
            return !string.IsNullOrEmpty(response.timestamp) ? response.timestamp : System.DateTime.Now.ToString("HH:mm:ss");
        }

        var data = JsonUtility.FromJson<TelemetryData>(json);
        return !string.IsNullOrEmpty(data.timestamp) ? data.timestamp : System.DateTime.Now.ToString("HH:mm:ss");
    }

    private void ResolveReferences()
    {
        if (vfd == null) vfd = FindAnyObjectByType<VFDController>(FindObjectsInactive.Include);
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
    }

    private NodeRedStatePayload CreateStatePayload()
    {
        return new NodeRedStatePayload
        {
            timestamp = System.DateTime.UtcNow.ToString("o"),
            voltage = bench.voltage,
            current = bench.current,
            power = bench.power,
            frequency = bench.frequency,
            targetFrequency = bench.targetFrequency,
            vfdPowered = bench.vfdPowered,
            motorRPM = bench.motorRPM,
            torque = bench.torque,
            motorTemp = bench.motorTemp,
            vibration = bench.vibration,
            efficiency = bench.efficiency,
            plcRunning = bench.plcRunning,
            cpuMode = bench.cpuMode,
            faultCode = bench.faultCode,
            faultDescription = bench.faultDescription,
            plcConnected = bench.plcConnected,
            activeScreen = bench.activeScreen,
            alarmCount = bench.alarmCount,
            warningCount = bench.warningCount,
            lightGreen = bench.lightGreen,
            lightOrange = bench.lightOrange,
            lightRed = bench.lightRed,
            lightBlinking = bench.lightBlinking,
            towerState = bench.towerState,
            eStopPressed = bench.eStopPressed,
            safetyCircuitOK = bench.safetyCircuitOK,
            lastOperatorAction = bench.lastOperatorAction
        };
    }

    private void ApplyAuthentication(UnityWebRequest request)
    {
        if (request == null || string.IsNullOrEmpty(nodeRedPassword)) return;

        if (!string.IsNullOrEmpty(passwordHeaderName))
        {
            request.SetRequestHeader(passwordHeaderName, nodeRedPassword);
        }

        if (sendBearerToken)
        {
            request.SetRequestHeader("Authorization", "Bearer " + nodeRedPassword);
        }
    }

    private string BuildEndpointUrl(string path)
    {
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? legacyTelemetryPath : path.Trim();
        if (!normalizedPath.StartsWith("/")) normalizedPath = "/" + normalizedPath;
        return nodeRedBaseUrl + normalizedPath;
    }

    private string BuildDashboardSocketUrl(string sid)
    {
        string path = string.IsNullOrWhiteSpace(dashboardSocketPath) ? "/ui/socket.io/" : dashboardSocketPath.Trim();
        if (!path.StartsWith("/")) path = "/" + path;
        if (!path.EndsWith("/")) path += "/";
        string url = nodeRedBaseUrl + path + "?EIO=4&transport=polling";
        if (!string.IsNullOrEmpty(sid)) url += "&sid=" + sid;
        return url;
    }

    private static bool ContainsScadaSnapshotFields(string json)
    {
        return !string.IsNullOrEmpty(json)
            && (HasField(json, "Vab") || HasField(json, "Vbc") || HasField(json, "Vca") || HasField(json, "Vavg"))
            && (HasField(json, "PF") || HasField(json, "PFtot") || HasField(json, "PF_total"));
    }

    private static string ExtractStringField(string json, string fieldName)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(fieldName)) return string.Empty;

        int keyIndex = json.IndexOf("\"" + fieldName + "\"", StringComparison.OrdinalIgnoreCase);
        if (keyIndex < 0) return string.Empty;
        int colonIndex = json.IndexOf(':', keyIndex);
        if (colonIndex < 0) return string.Empty;
        int quoteStart = json.IndexOf('"', colonIndex + 1);
        if (quoteStart < 0) return string.Empty;
        int quoteEnd = json.IndexOf('"', quoteStart + 1);
        if (quoteEnd <= quoteStart) return string.Empty;
        return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
    }

    private bool TryFallbackTelemetryEndpoint(long responseCode, string responseBody, string requestError)
    {
        if (!autoFallbackTelemetryPath) return false;

        bool requestTimedOut = responseCode == 0
            || (!string.IsNullOrEmpty(requestError)
                && requestError.IndexOf("timed", System.StringComparison.OrdinalIgnoreCase) >= 0);
        bool serverApiNotConfigured = !string.IsNullOrEmpty(responseBody)
            && (responseBody.IndexOf("NOT_CONFIGURED", System.StringComparison.OrdinalIgnoreCase) >= 0
                || responseBody.IndexOf("API_TOKEN", System.StringComparison.OrdinalIgnoreCase) >= 0);

        if (requestTimedOut)
        {
            return SwitchToNextTelemetryEndpoint("Telemetry timeout");
        }

        if (responseCode == 404)
        {
            return SwitchToNextTelemetryEndpoint("Telemetry path missing");
        }

        if (responseCode == 503 || responseCode == 401 || responseCode == 403 || serverApiNotConfigured)
        {
            return SwitchToNextTelemetryEndpoint("Telemetry endpoint unavailable");
        }

        return false;
    }

    private bool SwitchToNextTelemetryEndpoint(string reason)
    {
        string[] paths = new[]
        {
            telemetryPath,
            twinDataPath,
            legacyTelemetryPath,
            "/api/pm2230/latest",
            "/api/latest",
            "/pm2230/latest"
        };

        int currentIndex = -1;
        for (int i = 0; i < paths.Length; i++)
        {
            if (BuildEndpointUrl(paths[i]) == nodeRedUrl)
            {
                currentIndex = i;
                break;
            }
        }

        int start = currentIndex >= 0 ? currentIndex : -1;
        for (int step = 1; step <= paths.Length; step++)
        {
            string candidate = paths[(start + step + paths.Length) % paths.Length];
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            if (BuildEndpointUrl(candidate) == nodeRedUrl) continue;
            return SwitchTelemetryEndpoint(candidate, reason);
        }

        return false;
    }

    private bool SwitchTelemetryEndpoint(string path, string reason)
    {
        if (BuildEndpointUrl(path) == nodeRedUrl) return false;
        nodeRedUrl = BuildEndpointUrl(path);
        ConfigureOutputEndpointsForTelemetryPath(path);
        ResetTelemetryFailureState();
        lastError = string.Empty;
        AppendConnectionLog(reason + " - using " + path);
        return true;
    }

    private IEnumerator CheckEndpointReachable(string url, Action<bool, string> result)
    {
        if (result == null) yield break;

        if (!TryGetEndpointHostPort(url, out string host, out int port, out string parseError))
        {
            result(false, parseError);
            yield break;
        }

        using (var client = new TcpClient())
        {
            var connectTask = client.ConnectAsync(host, port);
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, preflightTimeoutSeconds);

            while (!connectTask.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (!connectTask.IsCompleted)
            {
                client.Close();
                result(false, host + ":" + port + " did not answer within " + preflightTimeoutSeconds.ToString("0.##") + "s");
                yield break;
            }

            if (connectTask.IsFaulted || connectTask.IsCanceled)
            {
                string error = connectTask.Exception != null && connectTask.Exception.GetBaseException() != null
                    ? connectTask.Exception.GetBaseException().Message
                    : "connection refused";
                result(false, host + ":" + port + " unreachable - " + error);
                yield break;
            }
        }

        result(true, string.Empty);
    }

    private bool TryGetEndpointHostPort(string url, out string host, out int port, out string error)
    {
        host = string.Empty;
        port = 0;
        error = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
        {
            error = "Invalid Node-RED URL: " + url;
            return false;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            error = "Unsupported Node-RED URL scheme: " + uri.Scheme;
            return false;
        }

        host = uri.Host;
        port = uri.IsDefaultPort ? (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80) : uri.Port;
        return true;
    }

    private void RegisterTelemetryFailure(string message)
    {
        lastError = message;
        consecutiveTelemetryFailures++;

        if (message != lastTelemetryFailureSignature || consecutiveTelemetryFailures >= Mathf.Max(1, maxConsecutiveTelemetryFailures))
        {
            AppendConnectionLog(message);
            lastTelemetryFailureSignature = message;
        }

        if (!pauseLiveAfterRepeatedFailures) return;
        if (consecutiveTelemetryFailures < Mathf.Max(1, maxConsecutiveTelemetryFailures)) return;

        useSimulation = true;
        string pauseMessage = "Live Node-RED paused after " + consecutiveTelemetryFailures
            + " failed polls. Check IP/port/firewall/Node-RED, then press CONNECT LIVE.";
        lastError = pauseMessage;
        AppendConnectionLog(pauseMessage);
        ResetTelemetryFailureState();
    }

    private void ResetTelemetryFailureState()
    {
        consecutiveTelemetryFailures = 0;
        lastTelemetryFailureSignature = string.Empty;
    }

    private void ConfigureOutputEndpointsForTelemetryPath(string path)
    {
        bool readOnlyApi = autoDisableOutputsOnReadOnlyApi
            && !string.IsNullOrEmpty(path)
            && (path.StartsWith("/api/", System.StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("api/", System.StringComparison.OrdinalIgnoreCase)
                || path.Equals(twinDataPath, System.StringComparison.OrdinalIgnoreCase));

        if (readOnlyApi)
        {
            commandUrl = string.Empty;
            stateUrl = string.Empty;
            sendCommandsWhenLive = false;
            publishStateWhenLive = false;
            AppendConnectionLog("Read-only Node-RED API mode");
            return;
        }

        commandUrl = nodeRedBaseUrl + "/commands";
        stateUrl = nodeRedBaseUrl + "/state";
        sendCommandsWhenLive = true;
        publishStateWhenLive = true;
    }

    private static string NormalizeBaseUrl(string value)
    {
        string trimmed = string.IsNullOrWhiteSpace(value) ? "200.200.200.177:1880" : value.Trim();
        trimmed = trimmed.TrimEnd('/');

        if (!trimmed.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "http://" + trimmed;
        }

        int hostStart = trimmed.IndexOf("://", System.StringComparison.Ordinal) + 3;
        int pathStart = trimmed.IndexOf('/', hostStart);
        if (pathStart >= 0) trimmed = trimmed.Substring(0, pathStart);

        string host = trimmed.Substring(hostStart);
        if (!host.Contains(":")) trimmed += ":1880";
        return trimmed;
    }

    private void AppendConnectionLog(string line)
    {
        string stamp = System.DateTime.Now.ToString("HH:mm:ss");
        string entry = "[" + stamp + "] " + line;
        if (string.IsNullOrEmpty(connectionLog) || connectionLog == "Node-RED bridge idle.")
        {
            connectionLog = entry;
            return;
        }

        connectionLog = entry + "\n" + connectionLog;
        string[] lines = connectionLog.Split('\n');
        int lineLimit = Mathf.Clamp(maxConnectionLogLines, 4, 20);
        if (lines.Length <= lineLimit) return;

        var builder = new StringBuilder();
        for (int i = 0; i < lineLimit; i++)
        {
            if (i > 0) builder.Append('\n');
            builder.Append(lines[i]);
        }
        connectionLog = builder.ToString();
    }
}
