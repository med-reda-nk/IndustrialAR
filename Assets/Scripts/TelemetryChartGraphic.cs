using UnityEngine;
using UnityEngine.UI;

public class TelemetryChartGraphic : MaskableGraphic
{
    private readonly float[] values = new float[128];
    private int count;
    private float minValue;
    private float maxValue = 1f;
    private Color lineColor = Color.white;
    private Color gridColor = new Color(1f, 1f, 1f, 0.10f);

    public void SetData(float[] source, int sourceCount, float min, float max, Color line, Color grid)
    {
        count = Mathf.Clamp(sourceCount, 0, Mathf.Min(values.Length, source != null ? source.Length : 0));
        for (int i = 0; i < count; i++) values[i] = source[i];
        minValue = min;
        maxValue = Mathf.Approximately(max, min) ? min + 1f : max;
        lineColor = line;
        gridColor = grid;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        DrawGrid(vh, r);
        if (count < 2) return;

        Vector2 previous = ChartPoint(r, 0, count);
        for (int i = 1; i < count; i++)
        {
            Vector2 next = ChartPoint(r, i, count);
            AddLine(vh, previous, next, 2.4f, lineColor);
            previous = next;
        }
    }

    private void DrawGrid(VertexHelper vh, Rect r)
    {
        for (int i = 1; i < 4; i++)
        {
            float x = Mathf.Lerp(r.xMin, r.xMax, i / 4f);
            float y = Mathf.Lerp(r.yMin, r.yMax, i / 4f);
            CyberDashboardMesh.AddSolidQuad(vh, new Rect(x, r.yMin, 1f, r.height), gridColor);
            CyberDashboardMesh.AddSolidQuad(vh, new Rect(r.xMin, y, r.width, 1f), gridColor);
        }
    }

    private Vector2 ChartPoint(Rect r, int index, int total)
    {
        float x = total <= 1 ? r.xMin : Mathf.Lerp(r.xMin, r.xMax, index / (float)(total - 1));
        float normalized = Mathf.InverseLerp(minValue, maxValue, values[index]);
        float y = Mathf.Lerp(r.yMin + 6f, r.yMax - 6f, Mathf.Clamp01(normalized));
        return new Vector2(x, y);
    }

    private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float thick, Color c)
    {
        Vector2 direction = b - a;
        if (direction.sqrMagnitude <= 0.001f) return;
        Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (thick * 0.5f);
        int index = vh.currentVertCount;
        UIVertex vert = UIVertex.simpleVert;
        vert.color = c;
        vert.position = a - normal;
        vh.AddVert(vert);
        vert.position = a + normal;
        vh.AddVert(vert);
        vert.position = b + normal;
        vh.AddVert(vert);
        vert.position = b - normal;
        vh.AddVert(vert);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }
}
