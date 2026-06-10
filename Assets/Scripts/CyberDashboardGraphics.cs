using UnityEngine;
using UnityEngine.UI;

public static class CyberDashboardMesh
{
    public static void AddSolidQuad(VertexHelper vh, Rect rect, Color color)
    {
        Vector2 min = new Vector2(Mathf.Min(rect.xMin, rect.xMax), Mathf.Min(rect.yMin, rect.yMax));
        Vector2 max = new Vector2(Mathf.Max(rect.xMin, rect.xMax), Mathf.Max(rect.yMin, rect.yMax));
        AddQuad(vh, min, max, color, color, color, color);
    }

    public static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color bottomLeft, Color topLeft, Color topRight, Color bottomRight)
    {
        int index = vh.currentVertCount;
        UIVertex vert = UIVertex.simpleVert;
        vert.position = new Vector3(min.x, min.y);
        vert.color = bottomLeft;
        vh.AddVert(vert);
        vert.position = new Vector3(min.x, max.y);
        vert.color = topLeft;
        vh.AddVert(vert);
        vert.position = new Vector3(max.x, max.y);
        vert.color = topRight;
        vh.AddVert(vert);
        vert.position = new Vector3(max.x, min.y);
        vert.color = bottomRight;
        vh.AddVert(vert);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }
}
