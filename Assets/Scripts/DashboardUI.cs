using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashboardUI : MonoBehaviour
{
    [Header("References")]
    public VFDController vfd;
    public BenchSystem bench;

    [Header("Status")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public Image statusIndicator;

    [Header("Live Values")]
    public TextMeshProUGUI textVoltage;
    public TextMeshProUGUI textCurrent;
    public TextMeshProUGUI textPower;
    public TextMeshProUGUI textEnergy;
    public TextMeshProUGUI textFrequency;
    public TextMeshProUGUI textFrequencySecondary;

    [Header("Progress Bars")]
    public Image barVoltage;
    public Image barCurrent;
    public Image barPower;

    private float energy = 161f;
    private Color colorRunning = new Color(0f, 0.85f, 0.4f);
    private Color colorIdle = new Color(0.5f, 0.5f, 0.5f);
    private Color colorFault = new Color(0.9f, 0.2f, 0.2f);

    void Update()
    {
        if (bench == null && vfd == null) return;

        float voltage = bench != null ? bench.voltage : vfd.voltage;
        float current = bench != null ? bench.current : vfd.current;
        float power = bench != null ? bench.power : vfd.power;
        float frequency = bench != null ? bench.frequency : vfd.frequency;
        bool powered = bench != null ? bench.vfdPowered : vfd.isPowered;

        energy += power * Time.deltaTime / 3.6f;

        textVoltage.text = voltage.ToString("F2") + " V";
        textCurrent.text = current.ToString("F4") + " A";
        textPower.text = power.ToString("F3") + " kW";
        textEnergy.text = energy.ToString("F1") + " Wh";
        textFrequency.text = frequency.ToString("F1") + " Hz";
        if (textFrequencySecondary != null)
            textFrequencySecondary.text = frequency.ToString("F1") + " Hz";

        if (barVoltage != null) barVoltage.fillAmount = voltage / 400f;
        if (barCurrent != null) barCurrent.fillAmount = current / 3.2f;
        if (barPower != null) barPower.fillAmount = Mathf.Clamp01(power / 2f);

        if (powered && frequency > 1f)
        {
            statusText.text = "RUNNING";
            statusText.color = colorRunning;
            if (statusIndicator != null) statusIndicator.color = colorRunning;
        }
        else
        {
            statusText.text = "IDLE";
            statusText.color = colorIdle;
            if (statusIndicator != null) statusIndicator.color = colorIdle;
        }
    }
}
