using UnityEngine;
using UnityEngine.UI;

public class CyberGridGraphic : MaskableGraphic
{
    private Color major = new Color32(0, 229, 255, 42);
    private Color minor = new Color32(255, 42, 170, 18);
    private const float MinorSpacing = 38f;
    private const int MajorEvery = 4;

    public void SetColors(Color majorColor, Color minorColor)
    {
        major = majorColor;
        minor = minorColor;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        int xCount = Mathf.CeilToInt(r.width / MinorSpacing);
        int yCount = Mathf.CeilToInt(r.height / MinorSpacing);

        for (int x = 0; x <= xCount; x++)
        {
            float px = r.xMin + x * MinorSpacing;
            Color c = x % MajorEvery == 0 ? major : minor;
            CyberDashboardMesh.AddSolidQuad(vh, new Rect(px, r.yMin, x % MajorEvery == 0 ? 2f : 1f, r.height), c);
        }

        for (int y = 0; y <= yCount; y++)
        {
            float py = r.yMin + y * MinorSpacing;
            Color c = y % MajorEvery == 0 ? major : minor;
            CyberDashboardMesh.AddSolidQuad(vh, new Rect(r.xMin, py, r.width, y % MajorEvery == 0 ? 2f : 1f), c);
        }
    }
}
