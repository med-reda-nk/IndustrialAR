using UnityEngine;

public class SignalTower : BenchComponent
{
    public Renderer lightGreen;
    public Renderer lightOrange;
    public Renderer lightRed;

    private Material matGreen, matOrange, matRed;
    public string CurrentState { get; private set; } = "idle";
    private static readonly Color RunningLightColor = new Color32(0, 229, 255, 255);

    void Start()
    {
        if (lightGreen == null || lightOrange == null || lightRed == null) return;
        matGreen = lightGreen.material;
        matOrange = lightOrange.material;
        matRed = lightRed.material;
        matGreen.color = RunningLightColor;
        SetState("idle");
    }

    public void SetState(string state)
    {
        if (lightGreen == null || lightOrange == null || lightRed == null) return;
        CurrentState = state;
        SetEmission(matGreen, state == "running");
        SetEmission(matOrange, state == "warning");
        SetEmission(matRed, state == "fault" || state == "estop");
    }

    void SetEmission(Material mat, bool on)
    {
        if (on) mat.EnableKeyword("_EMISSION");
        else mat.DisableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", on ? mat.color * 3f : Color.black);
    }
}
