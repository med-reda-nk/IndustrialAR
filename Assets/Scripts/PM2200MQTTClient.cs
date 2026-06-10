using UnityEngine;
using TMPro;
using System.Text;
using Newtonsoft.Json;
using UnityEngine.UI;
// NOTE: This script requires an MQTT library like M2Mqtt.
// You can add it to your project via a .unitypackage or DLL.
// using uPLibrary.Networking.M2Mqtt;
// using uPLibrary.Networking.M2Mqtt.Messages;

[System.Serializable]
public class PM2200Data
{
    public float voltage;
    public float current;
    public float power;
    public bool estop_status;
}

public class PM2200MQTTClient : MonoBehaviour
{
    [Header("MQTT Broker Configuration")]
    public string brokerAddress = "localhost";
    public int brokerPort = 1883;
    public string topic = "industrial/pm2200/data";
    public string commandTopic = "industrial/pm2200/control";

    [Header("3D Visual References")]
    public TextMeshPro powerLabel3D;
    public MeshRenderer estopMeshRenderer;
    public Material estopSafeMaterial;
    public Material estopTrippedMaterial;

    [Header("UI Control References")]
    public Button relayTripButton;

    [Header("Mode")]
    public bool useSimulation = true;
    public float simulationUpdateRate = 1.0f;

    [Header("Logic References")]
    public BenchSystem bench;

    // Conceptual MQTT Client - Replace with actual M2Mqtt client if available
    // private MqttClient client;

    private PM2200Data latestData;
    private bool isConnected = false;

    void Start()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        
        if (useSimulation)
        {
            InvokeRepeating(nameof(SimulateMQTTData), 0.5f, simulationUpdateRate);
        }
        else
        {
            ConnectToBroker();
        }
    }

    void SimulateMQTTData()
    {
        if (!useSimulation) return;

        string simJson = $"{{\"voltage\": {230f + Random.Range(-5f, 5f)}, \"current\": {Random.Range(1f, 5f)}, \"power\": {Random.Range(0.5f, 2.5f)}, \"estop_status\": {(Random.value > 0.95f ? "true" : "false")}}}" ;
        ProcessMessage(simJson);
    }

    void ConnectToBroker()
    {
        Debug.Log($"Connecting to MQTT Broker at {brokerAddress}:{brokerPort}...");
        // Placeholder for MQTT connection logic:
        /*
        client = new MqttClient(brokerAddress, brokerPort, false, null, null, MqttSslProtocols.None);
        client.MqttMsgPublishReceived += OnMessageReceived;
        string clientId = System.Guid.NewGuid().ToString();
        client.Connect(clientId);
        client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        isConnected = true;
        */
        
        // Simulation for now since library is not installed
        isConnected = true; 
    }

    // This would be triggered by the MQTT library on message arrival
    public void ProcessMessage(string jsonPayload)
    {
        try
        {
            latestData = JsonConvert.DeserializeObject<PM2200Data>(jsonPayload);
            UpdateVisuals();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing PM2200 MQTT JSON: " + e.Message);
        }
    }

    void UpdateVisuals()
    {
        if (latestData == null) return;

        // Sync with BenchSystem
        if (bench != null)
        {
            bench.voltage = latestData.voltage;
            bench.current = latestData.current;
            bench.power = latestData.power;
            bench.eStopPressed = latestData.estop_status;
            // Map to PM2200 Digital Input Register 3204 (Industrial Standard)
            bench.pm2200DI1 = latestData.estop_status;
            bench.pm2200Register3204 = latestData.estop_status ? 1 : 0;
        }

        // 1. Update 3D TextMeshPro for Power
        if (powerLabel3D != null)
        {
            powerLabel3D.text = $"POWER: {latestData.power:F3} kW";
        }

        // 2. Update 3D Material color for E-Stop
        if (estopMeshRenderer != null)
        {
            estopMeshRenderer.material = latestData.estop_status ? estopTrippedMaterial : estopSafeMaterial;
        }

        // 3. Safety: Disable 'relay_trip' button if estop_status is true
        if (relayTripButton != null)
        {
            relayTripButton.interactable = !latestData.estop_status;
        }
    }

    // Function to be called by VR/AR virtual button
    public void OnRelayTripButtonPressed()
    {
        if (latestData != null && latestData.estop_status)
        {
            Debug.LogWarning("Cannot publish relay_trip: E-Stop is active!");
            return;
        }

        PublishCommand("relay_trip");
    }

    void PublishCommand(string command)
    {
        if (!isConnected) return;

        string payload = $"{{\"command\": \"{command}\"}}";
        Debug.Log($"Publishing MQTT command to {commandTopic}: {payload}");
        
        // Placeholder for MQTT publish logic:
        /*
        client.Publish(commandTopic, Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
        */
    }

    private void OnDestroy()
    {
        // Placeholder for MQTT disconnect logic:
        /*
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
        }
        */
    }
}
