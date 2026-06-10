using UnityEngine;
using UnityEngine.UI;

public class CyberScanlineGraphic : MaskableGraphic
{
    private Color scanline = new Color32(0, 229, 255, 16);
    private float offset;
    private const float Spacing = 12f;

    public void SetColor(Color c)
    {
        scanline = c;
        SetVerticesDirty();
    }

    public void SetOffset(float value)
    {
        offset = Mathf.Repeat(value, Spacing);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        for (float y = r.yMin - Spacing + offset; y <= r.yMax; y += Spacing)
        {
            CyberDashboardMesh.AddSolidQuad(vh, new Rect(r.xMin, y, r.width, 2f), scanline);
        }
    }
}
