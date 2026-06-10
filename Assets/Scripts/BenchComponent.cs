using UnityEngine;

public class BenchComponent : MonoBehaviour
{
    public bool isPowered = false;

    public virtual void PowerOn() { isPowered = true; }
    public virtual void PowerOff() { isPowered = false; }
}