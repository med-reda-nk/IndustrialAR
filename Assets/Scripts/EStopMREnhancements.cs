using UnityEngine;
using TMPro;

public class EStopMREnhancements : MonoBehaviour
{
    [Header("References")]
    public BenchSystem bench;
    public Transform physicalButton;
    
    [Header("Ghost Button")]
    public GameObject ghostButtonPrefab;
    private GameObject ghostButtonInstance;
    
    [Header("Safety Perimeter")]
    public LineRenderer floorPerimeter;
    public GameObject safetyFence;
    public float perimeterRadius = 2.0f;
    public Color normalColor = new Color32(0, 229, 255, 255);
    public Color alertColor = Color.red;

    void Start()
    {
        if (bench == null) bench = FindAnyObjectByType<BenchSystem>(FindObjectsInactive.Include);
        
        if (floorPerimeter != null)
        {
            SetupPerimeter();
        }
        
        if (ghostButtonPrefab != null && physicalButton != null)
        {
            ghostButtonInstance = Instantiate(ghostButtonPrefab, physicalButton.position, physicalButton.rotation);
            // Ensure ghost button is visible through walls (requires specific shader, but we'll set up the object)
            ghostButtonInstance.name = "EStop_Ghost";
        }

        if (safetyFence != null) safetyFence.SetActive(false);
    }

    void SetupPerimeter()
    {
        floorPerimeter.positionCount = 50;
        floorPerimeter.loop = true;
        floorPerimeter.useWorldSpace = true;
        
        Vector3 center = physicalButton.position;
        center.y = 0; // Floor level

        for (int i = 0; i < 50; i++)
        {
            float angle = i * Mathf.PI * 2 / 50;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * perimeterRadius, 0, Mathf.Sin(angle) * perimeterRadius);
            floorPerimeter.SetPosition(i, center + pos);
        }
    }

    void Update()
    {
        if (bench == null) return;

        bool active = bench.eStopPressed;

        // Update Perimeter
        if (floorPerimeter != null)
        {
            floorPerimeter.startColor = floorPerimeter.endColor = active ? alertColor : normalColor;
            floorPerimeter.widthMultiplier = active ? 0.05f : 0.02f;
        }

        // Update Safety Fence
        if (safetyFence != null)
        {
            safetyFence.SetActive(active);
        }

        // Ghost Button pulse if active
        if (ghostButtonInstance != null && active)
        {
            float scale = 1.0f + Mathf.PingPong(Time.time * 2f, 0.2f);
            ghostButtonInstance.transform.localScale = Vector3.one * scale;
        }
    }
}
