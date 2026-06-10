using UnityEngine;

public class VFDController : BenchComponent
{
    public float frequency = 0f;
    public float voltage = 0f;
    public float current = 0f;
    public float power = 0f;

    void Update()
    {
        if (NodeRedClient.Instance != null && !NodeRedClient.Instance.useSimulation) return;

        if (!isPowered) { frequency = 0; voltage = 0; current = 0; power = 0; return; }
        voltage = Mathf.Lerp(voltage, 400f * (frequency / 50f), Time.deltaTime * 2f);
        current = Mathf.Lerp(current, 3.2f * (frequency / 50f), Time.deltaTime * 2f);
        power = (voltage * current * 1.732f) / 1000f;
    }

    public void SetFrequency(float hz)
    {
        frequency = Mathf.Clamp(hz, 0f, 50f);
        isPowered = hz > 0;
    }

    public void FrequencyUp() { SetFrequency(frequency + 5f); }
    public void FrequencyDown() { SetFrequency(frequency - 5f); }

    public override void PowerOn() { base.PowerOn(); SetFrequency(50f); }
    public override void PowerOff() { base.PowerOff(); frequency = 0; }
}