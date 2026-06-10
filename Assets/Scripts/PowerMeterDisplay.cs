using UnityEngine;
using TMPro;

public class PowerMeterDisplay : MonoBehaviour
{
    public VFDController vfd;
    public TextMeshProUGUI textVoltage;
    public TextMeshProUGUI textCurrent;
    public TextMeshProUGUI textPower;
    public TextMeshProUGUI textEnergy;

    private float energy = 161f;

    void Update()
    {
        if (vfd == null) return;
        energy += vfd.power * Time.deltaTime / 3.6f;
        textVoltage.text = "Vmoy: " + vfd.voltage.ToString("F4") + " V";
        textCurrent.text = "Imoy: " + vfd.current.ToString("F4") + " mA";
        textPower.text = "Ptot: " + vfd.power.ToString("F5") + " kW";
        textEnergy.text = "E.Fni: " + energy.ToString("F1") + " Wh";
    }
}