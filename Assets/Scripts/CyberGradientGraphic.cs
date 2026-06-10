using UnityEngine;
using UnityEngine.UI;

public class CyberGradientGraphic : MaskableGraphic
{
    private Color topLeft = new Color32(3, 6, 14, 242);
    private Color bottomRight = new Color32(13, 18, 32, 238);
    private Color diagonal = new Color32(255, 42, 170, 40);

    public void SetColors(Color a, Color b, Color c)
    {
        topLeft = a;
        bottomRight = b;
        diagonal = c;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        CyberDashboardMesh.AddQuad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMax), bottomRight, topLeft, topLeft, bottomRight);

        float band = Mathf.Min(r.width, r.height) * 0.36f;
        CyberDashboardMesh.AddQuad(vh,
            new Vector2(r.xMin, r.yMax - band),
            new Vector2(r.xMax, r.yMax),
            new Color(diagonal.r, diagonal.g, diagonal.b, 0f),
            diagonal,
            new Color(diagonal.r, diagonal.g, diagonal.b, 0.22f),
            new Color(diagonal.r, diagonal.g, diagonal.b, 0f));
    }
}
