using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

[ExecuteAlways]
public class BenchSystem : MonoBehaviour
{
    [Header("VFD / POWER SECTION")]
    public float frequency;
    public float targetFrequency = 50f;
    public float voltage;
    public float voltageAB;
    public float voltageBC;
    public float voltageCA;
    public float current;
    public float power;
    public float powerFactor = 0.85f;
    public float energy = 161f; // Wh
    public float demandkW;
    public float peakDemandkW;
    public float harmonicDistortion; // THD %
    public float energyCost;
    public float co2Emissions;
    public bool vfdPowered;
    public float rampUpTime = 3f;
    public float rampDownTime = 2f;

    [Header("MOTOR SECTION")]
    public float motorRPM;
    public float targetRPM;
    public float torque;
    public float motorTemp = 20f;
    public float temperature; // Compatibility
    public float vibration = 0.5f;
    public float efficiency; // %
    public float runningTimeHours;
    public string motorDirection = "FWD";
    public string direction; // Compatibility
    public bool motorOverheat;
    public bool motorFault;

    [Header("PLC SECTION")]
    public bool plcRunning;
    public string cpuMode = "STOP";
    public float scanCycleMs;
    public int faultCode;
    public string faultDescription = "No fault";
    public bool[] digitalInputs = new bool[8];
    public bool[] digitalOutputs = new bool[6];
    public int programCycleCount;
    public float cpuLoadPercent;
    public float memoryUsedKB = 48.2f;
    public bool plcMaintenanceRequired;

    [Header("HMI SECTION")]
    public bool plcConnected = true;
    public string activeScreen = "MAIN";
    public int alarmCount;
    public int warningCount;
    public string lastCommand; // Compatibility
    public string lastOperatorAction = "NONE";
    public float hmiSessionTime;

    [Header("Editor Preview")]
    public bool previewInEditor = true;
    public bool simulateInEditMode = true;
    public bool pauseInEditMode;
    public bool enableEditorInteraction = true;
    public float simTime;
    public float simDeltaTime;

    [Header("POWER METER (PM2200)")]
    public int pm2200ScreenIndex;
    public string[] pm2200Screens = { "Récapitulatif", "U-U", "I", "POS" };
    public float pm2200ResetHoldTimer;
    public int pm2200Register3204; // DI1 = bit 0
    public bool pm2200DI1;
    public bool pm2200DI2;
    public float voltageUnbalance;
    public float currentUnbalance;
    public float harmonicDistortionPM;
    public float harmonic3rd = 2.4f;
    public float harmonic5th = 1.1f;
    public float harmonic7th = 0.5f;
    public List<string> pmEventLog = new List<string>();
    public bool pm2200Relay1;
    public bool pm2200Relay2;
    public int modbusAddress = 1;
    public string gatewayIP = "192.168.1.150";
    public float lastDataUpdateTime;
    public bool pm2200DataUpdating;

    [Header("SIGNAL TOWER")]
    public bool lightGreen;
    public bool lightOrange;
    public bool lightRed;
    public bool lightBlinking;
    public string towerState = "IDLE";

    [Header("ESTOP")]
    public bool eStopPressed;
    public bool isLockedOut;
    public int pressCount; // Compatibility
    public int eStopPressCount;
    public float haltDuration;
    public string lastPressedTime; // Compatibility
    public string lastEStopTime = "NEVER";
    public bool safetyCircuitOK = true;

    [Header("Demand Tracking")]
    public float demandPowerSum;
    public int demandSampleCount;
    public float demandTimer;
    public float startupTimer;

#if UNITY_EDITOR
    private double lastEditorTime;
#endif

    private float lastVoltage;
    private float lastCurrent;
    private float lastPower;
    private float lastEnergy;
    [NonSerialized] public bool suppressNodeRedCommandOutput;

    public void OnEnable()
    {
#if UNITY_EDITOR
        lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
#endif
        simTime = Application.isPlaying ? Time.time : 0f;
        simDeltaTime = 0f;
    }

    public void Start()
    {
        lastVoltage = voltage;
        lastCurrent = current;
        lastPower = power;
        lastEnergy = energy;
        if (pm2200Screens == null || pm2200Screens.Length == 0)
        {
            pm2200Screens = new[] { "Récapitulatif", "U-U", "I", "POS" };
        }
        if (pmEventLog == null) pmEventLog = new List<string>();
    }

    public void Update()
    {
        UpdateSimTime();
        if (IsNodeRedLive())
        {
            SyncCompatibilityFields();
            if (Application.isPlaying) HandleKeyboard();
            return;
        }
        if (!Application.isPlaying && (!simulateInEditMode || pauseInEditMode)) return;
        SimulatePowerAndMotor();
        SimulatePLC();
        SimulateIOToggle();
        UpdateSignalTower();
        UpdateHaltTimer();
        UpdatePM2200();
        UpdateHMI();
        if (Application.isPlaying) HandleKeyboard();
    }

    private bool IsNodeRedLive()
    {
        return NodeRedClient.Instance != null && !NodeRedClient.Instance.useSimulation;
    }

    private void SyncCompatibilityFields()
    {
        temperature = motorTemp;
        direction = motorDirection;
        pressCount = eStopPressCount;
        lastPressedTime = lastEStopTime;
        lastCommand = lastOperatorAction;
    }

    public void UpdateSimTime()
    {
        if (Application.isPlaying)
        {
            simTime = Time.time;
            simDeltaTime = Time.deltaTime;
            return;
        }

#if UNITY_EDITOR
        double now = UnityEditor.EditorApplication.timeSinceStartup;
        simDeltaTime = (float)(now - lastEditorTime);
        if (simDeltaTime < 0f || simDeltaTime > 0.1f) simDeltaTime = 0f;
        simTime = (float)now;
        lastEditorTime = now;
#else
        simTime = 0f;
        simDeltaTime = 0f;
#endif
    }

    // Simulate VFD, power meter, and motor physics in one place for consistency.
    public void SimulatePowerAndMotor()
    {
        float deltaTime = simDeltaTime;

        // Sync compatibility fields
        temperature = motorTemp;
        direction = motorDirection;
        pressCount = eStopPressCount;
        lastPressedTime = lastEStopTime;
        lastCommand = lastOperatorAction;

        // VFD ramping
        float rampRate = (targetFrequency > frequency) ? (50f / Mathf.Max(0.1f, rampUpTime)) : (50f / Mathf.Max(0.1f, rampDownTime));
        if (vfdPowered && !eStopPressed && !motorFault)
        {
            frequency = Mathf.MoveTowards(frequency, targetFrequency, rampRate * deltaTime);
        }
        else
        {
            frequency = Mathf.MoveTowards(frequency, 0f, (50f / Mathf.Max(0.1f, rampDownTime)) * deltaTime);
        }

        // 400 V L-L nominal at 50 Hz
        float targetVoltage = (frequency / 50f) * 400f;
        voltage = Mathf.Lerp(voltage, targetVoltage, deltaTime * 5f);
        voltageAB = voltage * 1.01f;
        voltageBC = voltage * 0.99f;
        voltageCA = voltage;

        float load = Mathf.Clamp01(frequency / 50f);
        float targetCurrent = (vfdPowered && frequency > 0.1f) ? Mathf.Lerp(0.35f, 1.25f, load) : 0f;
        current = Mathf.Lerp(current, targetCurrent, deltaTime * 2f);

        powerFactor = (vfdPowered && frequency > 0.1f) ? Mathf.Lerp(0.72f, 0.90f, load) : 0.85f;
        power = (voltage * current * 1.732f * powerFactor) / 1000f;

        // THD simulation increases with drive load
        harmonicDistortion = vfdPowered ? 1.5f + (load * 4.0f) + UnityEngine.Random.Range(-0.2f, 0.2f) : 0.2f;
        harmonicDistortionPM = harmonicDistortion;

        // Energy accumulation (Wh)
        if (power > 0.001f)
        {
            float deltaWh = (power * 1000f) * (deltaTime / 3600f);
            energy += deltaWh;
            energyCost += deltaWh * 0.00015f; // $0.15 / kWh
            co2Emissions += deltaWh * 0.00045f; // 0.45 kg CO2 / kWh
        }

        // Motor speed (4-pole, 50 Hz -> ~1500 rpm, slip ~4%)
        float syncRpm = 1500f * (frequency / 50f);
        targetRPM = syncRpm * 0.96f;
        motorRPM = Mathf.Lerp(motorRPM, targetRPM, deltaTime * 1.5f);

        float ratedTorque = 2.4f; // ~0.37 kW at 1450 rpm
        float targetTorque = (vfdPowered && frequency > 0.1f) ? Mathf.Lerp(0.6f, ratedTorque * 1.2f, load) : 0f;
        torque = Mathf.Lerp(torque, targetTorque, deltaTime * 2f);

        float loadFactor = Mathf.Clamp01(motorRPM / 1450f);
        efficiency = (vfdPowered && motorRPM > 100f) ? (0.88f - Mathf.Pow(loadFactor - 0.8f, 2f)) * 100f : 0f;

        if (motorRPM > 100f)
        {
            motorTemp = Mathf.MoveTowards(motorTemp, 85f, (2f / 60f) * deltaTime);
        }
        else
        {
            motorTemp = Mathf.MoveTowards(motorTemp, 20f, (0.5f / 60f) * deltaTime);
        }
        motorOverheat = motorTemp > 80f;

        float baseVib = motorRPM > 10f ? Mathf.Lerp(0.6f, 2.4f, load) : 0f;
        vibration = vfdPowered && motorRPM > 10f ? baseVib + UnityEngine.Random.Range(-0.1f, 0.1f) : 0f;

        if (motorRPM > 100f) runningTimeHours += deltaTime / 3600f;

        // Fault checks
        if (motorOverheat) SetFault(101, "Motor overtemperature");
        if (current > 1.6f) SetFault(102, "Overcurrent");
        if (vibration > 3.5f) SetFault(103, "Vibration fault");

        // Rolling demand (smoothed)
        demandTimer += deltaTime;
        if (demandTimer >= 1f)
        {
            demandTimer = 0f;
            demandkW = Mathf.Lerp(demandkW, power, 0.2f);
            if (demandkW > peakDemandkW)
            {
                peakDemandkW = demandkW;
                LogPMEvent("NEW PEAK DEMAND: " + peakDemandkW.ToString("F3") + " kW");
            }
        }
    }

    // PLC simulation for scan cycle, CPU load, and maintenance state.
    public void SimulatePLC()
    {
        if (cpuMode == "STARTUP")
        {
            startupTimer += simDeltaTime;
            if (startupTimer >= 2f)
            {
                cpuMode = "RUN";
                plcRunning = true;
            }
        }

        if (plcRunning && !eStopPressed && faultCode == 0)
        {
            scanCycleMs = UnityEngine.Random.Range(1.0f, 2.5f);
            cpuLoadPercent = 18f + (frequency / 50f) * 35f;
            programCycleCount++;
        }
        else
        {
            scanCycleMs = 0f;
            cpuLoadPercent = 5f;
        }

        plcMaintenanceRequired = cpuLoadPercent > 85f || runningTimeHours > 200f;
        warningCount = plcMaintenanceRequired ? 1 : 0;
    }

    // Update digital I/O to feed PLC LED indicators.
    public void SimulateIOToggle()
    {
        if (!plcRunning || digitalInputs == null || digitalOutputs == null) return;

        if (digitalInputs.Length > 0) digitalInputs[0] = eStopPressed;
        if (digitalInputs.Length > 1) digitalInputs[1] = motorRPM > 100f;
        if (digitalInputs.Length > 2) digitalInputs[2] = motorFault;

        for (int i = 3; i < digitalInputs.Length; i++)
        {
            if (UnityEngine.Random.value > 0.99f) digitalInputs[i] = !digitalInputs[i];
        }

        if (digitalOutputs.Length > 0) digitalOutputs[0] = motorRPM > 100f; // Q0.0 motor contactor
        if (digitalOutputs.Length > 1) digitalOutputs[1] = lightGreen;      // Q0.1
        if (digitalOutputs.Length > 2) digitalOutputs[2] = lightOrange;     // Q0.2
        if (digitalOutputs.Length > 3) digitalOutputs[3] = lightRed;        // Q0.3

        for (int i = 4; i < digitalOutputs.Length; i++)
        {
            if (UnityEngine.Random.value > 0.995f) digitalOutputs[i] = !digitalOutputs[i];
        }
    }

    // Signal tower logic driven by system state.
    public void UpdateSignalTower()
    {
        bool hasFault = faultCode > 0 || motorFault;
        bool isTransition = vfdPowered && !eStopPressed && !hasFault && motorRPM > 10f && motorRPM < 100f;

        if (eStopPressed) towerState = "ESTOP";
        else if (hasFault) towerState = "FAULT";
        else if (motorOverheat || vibration > 2.8f) towerState = "WARNING";
        else if (isTransition) towerState = "TRANSITION";
        else if (motorRPM > 100f) towerState = "RUNNING";
        else towerState = "IDLE";

        lightGreen = towerState == "RUNNING";
        lightOrange = towerState == "WARNING" || towerState == "TRANSITION";
        lightRed = towerState == "FAULT" || towerState == "ESTOP";
        lightBlinking = towerState == "TRANSITION";
    }

    // Track halt and session timers for HMI overlays.
    public void UpdateHaltTimer()
    {
        if (eStopPressed) haltDuration += simDeltaTime;
        else haltDuration = 0f;
        hmiSessionTime += simDeltaTime;
        safetyCircuitOK = !eStopPressed;
    }

    // PM2200-specific calculations and update flags.
    public void UpdatePM2200()
    {
        float t = simTime;
        pm2200DI1 = (pm2200Register3204 & 1) == 1;

        voltageUnbalance = 0.5f + (Mathf.PerlinNoise(t * 0.1f, 500f) * 1.5f);
        currentUnbalance = 1.2f + (Mathf.PerlinNoise(t * 0.2f, 600f) * 4.0f);
        harmonic3rd = 2.0f + Mathf.Sin(t * 0.5f) * 0.5f;
        harmonic5th = 1.0f + Mathf.Sin(t * 0.35f) * 0.3f;
        harmonic7th = 0.5f + Mathf.Sin(t * 0.2f) * 0.2f;

        bool changed = Mathf.Abs(voltage - lastVoltage) > 0.05f
                       || Mathf.Abs(current - lastCurrent) > 0.01f
                       || Mathf.Abs(power - lastPower) > 0.01f
                       || Mathf.Abs(energy - lastEnergy) > 0.05f;
        if (changed) lastDataUpdateTime = t;
        pm2200DataUpdating = (t - lastDataUpdateTime) < 0.25f;

        lastVoltage = voltage;
        lastCurrent = current;
        lastPower = power;
        lastEnergy = energy;
    }

    // HMI alarm and connectivity status.
    public void UpdateHMI()
    {
        alarmCount = faultCode > 0 ? 1 : 0;
        if (plcConnected && UnityEngine.Random.value > 0.995f)
        {
            plcConnected = false;
        }
        else if (!plcConnected && UnityEngine.Random.value > 0.98f)
        {
            plcConnected = true;
        }
    }

    public void VFDPowerOn() { vfdPowered = true; if (targetFrequency == 0f) targetFrequency = 50f; lastOperatorAction = "VFD START"; SendNodeRedCommand("vfd", "start", "true", 1f); }
    public void VFDPowerOff() { vfdPowered = false; lastOperatorAction = "VFD STOP"; SendNodeRedCommand("vfd", "stop", "false", 0f); }
    public void VFDFrequencyUp() { targetFrequency = Mathf.Min(60f, targetFrequency + 5f); lastOperatorAction = "FREQ UP"; SendNodeRedCommand("vfd", "set_frequency", targetFrequency.ToString("0.###"), targetFrequency); }
    public void VFDFrequencyDown() { targetFrequency = Mathf.Max(0f, targetFrequency - 5f); lastOperatorAction = "FREQ DOWN"; SendNodeRedCommand("vfd", "set_frequency", targetFrequency.ToString("0.###"), targetFrequency); }
    public void MotorSetFWD() { motorDirection = "FWD"; lastOperatorAction = "SET FWD"; SendNodeRedCommand("motor", "set_direction", "FWD", 1f); }
    public void MotorSetREV() { motorDirection = "REV"; lastOperatorAction = "SET REV"; SendNodeRedCommand("motor", "set_direction", "REV", -1f); }
    public void ResetFaults() { faultCode = 0; faultDescription = "No fault"; motorFault = false; lastOperatorAction = "RESET FAULTS"; SendNodeRedCommand("system", "reset_faults", "true", 1f); }
    public void TogglePLC() { plcRunning = !plcRunning; cpuMode = plcRunning ? "RUN" : "STOP"; lastOperatorAction = "TOGGLE PLC"; SendNodeRedCommand("plc", plcRunning ? "run" : "stop", cpuMode, plcRunning ? 1f : 0f); }
    public void PLCSetRun() { plcRunning = true; cpuMode = "RUN"; lastOperatorAction = "PLC RUN"; SendNodeRedCommand("plc", "run", "RUN", 1f); }
    public void PLCSetStop() { plcRunning = false; cpuMode = "STOP"; lastOperatorAction = "PLC STOP"; SendNodeRedCommand("plc", "stop", "STOP", 0f); }

    public void PM2200ToggleRelay1()
    {
        pm2200Relay1 = !pm2200Relay1;
        string state = pm2200Relay1 ? "ON" : "OFF";
        lastOperatorAction = "PM2200 R1 " + state;
        LogPMEvent("RELAY 1 (K1) " + state);
        SendNodeRedCommand("pm2200", "relay_1", state, pm2200Relay1 ? 1f : 0f);
    }

    public void PM2200ToggleRelay2()
    {
        pm2200Relay2 = !pm2200Relay2;
        string state = pm2200Relay2 ? "ON" : "OFF";
        lastOperatorAction = "PM2200 R2 " + state;
        LogPMEvent("RELAY 2 (K2) " + state);
        SendNodeRedCommand("pm2200", "relay_2", state, pm2200Relay2 ? 1f : 0f);
    }

    public void PM2200NextScreen()
    {
        if (pm2200Screens == null || pm2200Screens.Length == 0) return;
        pm2200ScreenIndex = (pm2200ScreenIndex + 1) % pm2200Screens.Length;
        lastOperatorAction = "PM2200 NAV NEXT";
        SendNodeRedCommand("pm2200", "screen_next", pm2200ScreenIndex.ToString(), pm2200ScreenIndex);
    }

    public void PM2200PrevScreen()
    {
        if (pm2200Screens == null || pm2200Screens.Length == 0) return;
        pm2200ScreenIndex = (pm2200ScreenIndex - 1 + pm2200Screens.Length) % pm2200Screens.Length;
        lastOperatorAction = "PM2200 NAV PREV";
        SendNodeRedCommand("pm2200", "screen_prev", pm2200ScreenIndex.ToString(), pm2200ScreenIndex);
    }

    public void PM2200Select() { lastOperatorAction = "PM2200 SELECT"; SendNodeRedCommand("pm2200", "select", pm2200ScreenIndex.ToString(), pm2200ScreenIndex); }

    public void ResetEnergy() { energy = 0f; energyCost = 0f; co2Emissions = 0f; lastOperatorAction = "RESET ENERGY"; LogPMEvent("ENERGY COUNTER RESET"); SendNodeRedCommand("pm2200", "reset_energy", "true", 1f); }
    public void ResetDemand() { demandPowerSum = 0f; demandSampleCount = 0; peakDemandkW = 0f; lastOperatorAction = "RESET DEMAND"; LogPMEvent("DEMAND REGISTERS CLEAR"); SendNodeRedCommand("pm2200", "reset_demand", "true", 1f); }
    public void AcknowledgeAlarm() { alarmCount = 0; warningCount = 0; lastOperatorAction = "ACK ALARMS"; SendNodeRedCommand("hmi", "ack_alarms", "true", 1f); }

    public void ToggleEStop()
    {
        if (eStopPressed) EStopRelease();
        else EStopPress();
    }

    public void EStopPress()
    {
        eStopPressed = true;
        isLockedOut = true;
        eStopPressCount++;
        lastEStopTime = DateTime.Now.ToString("HH:mm:ss");
        lastOperatorAction = "ESTOP PRESS";
        pm2200Register3204 |= 1;
        SendNodeRedCommand("estop", "press", "true", 1f);
    }

    public void EStopRelease()
    {
        eStopPressed = false;
        isLockedOut = false;
        lastOperatorAction = "ESTOP RELEASE";
        pm2200Register3204 &= ~1;
        SendNodeRedCommand("estop", "release", "false", 0f);
    }

    public void SetFault(int code, string desc)
    {
        faultCode = code;
        faultDescription = desc;
        motorFault = true;
        lastOperatorAction = "FAULT TRIGGERED";
        SendNodeRedCommand("system", "fault", desc, code);
    }

    public float GetAverageDemand()
    {
        return demandSampleCount > 0 ? demandPowerSum / demandSampleCount : demandkW;
    }

    // Simple keyboard shortcuts for simulation testing.
    public void HandleKeyboard()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame) ToggleEStop();
        if (Keyboard.current.rKey.wasPressedThisFrame) ResetFaults();
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            if (vfdPowered) VFDPowerOff();
            else VFDPowerOn();
        }
    }

    public void LogPMEvent(string msg)
    {
        if (pmEventLog == null) pmEventLog = new List<string>();
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        pmEventLog.Insert(0, "[" + timestamp + "] " + msg);
        if (pmEventLog.Count > 5) pmEventLog.RemoveAt(5);
    }

    private void SendNodeRedCommand(string device, string command, string value, float numericValue)
    {
        if (suppressNodeRedCommandOutput) return;
        if (NodeRedClient.Instance == null) return;
        NodeRedClient.Instance.SendCommand(device, command, value, numericValue);
    }
}
