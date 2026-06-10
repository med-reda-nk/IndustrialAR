using UnityEngine;
using UnityEngine.UI;

public class CyberCornerFrameGraphic : MaskableGraphic
{
    private Color accentA = Color.cyan;
    private Color accentB = Color.magenta;
    private Color accentC = new Color32(255, 218, 77, 255);

    public void SetColors(Color a, Color b, Color c)
    {
        accentA = a;
        accentB = b;
        accentC = c;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float arm = Mathf.Min(r.width, r.height) * 0.13f;
        float thick = Mathf.Max(5f, Mathf.Min(r.width, r.height) * 0.008f);
        Color dimA = new Color(accentA.r, accentA.g, accentA.b, 0.86f);
        Color dimB = new Color(accentB.r, accentB.g, accentB.b, 0.72f);
        Color dimC = new Color(accentC.r, accentC.g, accentC.b, 0.58f);

        AddCorner(vh, r.xMin, r.yMax, arm, thick, 1f, -1f, dimA);
        AddCorner(vh, r.xMax, r.yMax, arm, thick, -1f, -1f, dimB);
        AddCorner(vh, r.xMin, r.yMin, arm, thick, 1f, 1f, dimB);
        AddCorner(vh, r.xMax, r.yMin, arm, thick, -1f, 1f, dimA);

        CyberDashboardMesh.AddSolidQuad(vh, new Rect(r.xMin + arm * 0.35f, r.yMax - thick * 1.5f, arm * 0.75f, thick), dimC);
        CyberDashboardMesh.AddSolidQuad(vh, new Rect(r.xMax - arm * 1.1f, r.yMin + thick * 0.5f, arm * 0.75f, thick), dimC);
    }

    private static void AddCorner(VertexHelper vh, float x, float y, float arm, float thick, float xDir, float yDir, Color c)
    {
        CyberDashboardMesh.AddSolidQuad(vh, new Rect(x, y - thick, arm * xDir, thick), c);
        CyberDashboardMesh.AddSolidQuad(vh, new Rect(x, y, thick * xDir, arm * yDir), c);
    }
}
