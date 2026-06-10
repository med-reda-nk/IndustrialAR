using UnityEngine;
using UnityEngine.InputSystem;

public class EStopButton : BenchComponent
{
    public BenchSystem bench;
    public VFDController vfd;
    public SignalTower tower;
    public Transform buttonMesh;

    private bool isPressed = false;
    private Vector3 releasedPos;

    public bool IsPressed => isPressed;

    void Start()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        if (vfd == null) vfd = FindAnyObjectByType<VFDController>(FindObjectsInactive.Include);
        if (tower == null) tower = FindAnyObjectByType<SignalTower>(FindObjectsInactive.Include);
        
        if (buttonMesh != null)
            releasedPos = buttonMesh.localPosition;
    }

    public void Press()
    {
        if (isPressed) return;
        isPressed = true;
        if (buttonMesh != null)
            buttonMesh.localPosition = releasedPos - new Vector3(0, 0.01f, 0);
        
        if (vfd != null) vfd.PowerOff();
        if (tower != null) tower.SetState("estop");
        if (bench != null) bench.EStopPress();
    }

    public void Release()
    {
        isPressed = false;
        if (buttonMesh != null)
            buttonMesh.localPosition = releasedPos;
        
        if (tower != null) tower.SetState("idle");
        if (bench != null) bench.EStopRelease();
    }

    void Update()
    {
        // Sync visual state with bench system if available
        if (bench != null)
        {
            if (bench.eStopPressed && !isPressed)
            {
                // Visual only press
                isPressed = true;
                if (buttonMesh != null)
                    buttonMesh.localPosition = releasedPos - new Vector3(0, 0.01f, 0);
                
                // Sync external components
                if (vfd != null) vfd.PowerOff();
                if (tower != null) tower.SetState("estop");
            }
            else if (!bench.eStopPressed && isPressed)
            {
                // Visual only release
                isPressed = false;
                if (buttonMesh != null)
                    buttonMesh.localPosition = releasedPos;
                
                // Sync external components
                if (tower != null) tower.SetState("idle");
            }
        }

        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame) Press();
        if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame) Release();
    }
}
