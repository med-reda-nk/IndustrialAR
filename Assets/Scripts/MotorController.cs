using UnityEngine;

public class MotorController : BenchComponent
{
    public VFDController vfd;
    public Transform shaft;
    private float currentRPM = 0f;

    void Update()
    {
        float targetRPM = (60f * vfd.frequency / 2f) * 0.96f;
        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * 2f);
        if (shaft != null)
            shaft.Rotate(0f, currentRPM * 6f * Time.deltaTime, 0f);
    }
}