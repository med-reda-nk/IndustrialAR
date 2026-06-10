using UnityEngine;
using UnityEngine.UI;

public enum MetricIconType
{
    Voltage,
    Current,
    Power,
    Frequency,
    PowerFactor,
    Energy
}

public class MetricIconGraphic : MaskableGraphic
{
    public MetricIconType iconType;
    public float strokeWidth = 2.6f;
    public Color secondaryColor = new Color(0.45f, 0.48f, 0.50f, 0.75f);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        Color primary = color;
        Color secondary = secondaryColor;

        switch (iconType)
        {
            case MetricIconType.Voltage:
                AddPolyline(vh, new[]
                {
                    Point(r, 0.56f, 0.96f),
                    Point(r, 0.30f, 0.50f),
                    Point(r, 0.52f, 0.50f),
                    Point(r, 0.42f, 0.04f),
                    Point(r, 0.74f, 0.58f),
                    Point(r, 0.52f, 0.58f)
                }, strokeWidth, primary);
                break;
            case MetricIconType.Current:
                AddWave(vh, r, 0.18f, 0.82f, 0.54f, 0.22f, primary);
                AddLine(vh, Point(r, 0.08f, 0.50f), Point(r, 0.92f, 0.50f), 1.2f, secondary);
                break;
            case MetricIconType.Power:
                AddBar(vh, r, 0.18f, 0.18f, 0.18f, 0.48f, secondary);
                AddBar(vh, r, 0.42f, 0.18f, 0.18f, 0.68f, primary);
                AddBar(vh, r, 0.66f, 0.18f, 0.18f, 0.34f, secondary);
                break;
            case MetricIconType.Frequency:
                AddWave(vh, r, 0.10f, 0.90f, 0.50f, 0.28f, primary);
                AddLine(vh, Point(r, 0.10f, 0.18f), Point(r, 0.90f, 0.18f), 1.5f, secondary);
                AddLine(vh, Point(r, 0.12f, 0.18f), Point(r, 0.12f, 0.82f), 1.5f, secondary);
                break;
            case MetricIconType.PowerFactor:
                AddArc(vh, r, 0.50f, 0.48f, 0.36f, 205f, -25f, strokeWidth, secondary);
                AddArc(vh, r, 0.50f, 0.48f, 0.36f, 205f, 84f, strokeWidth + 0.4f, primary);
                AddLine(vh, Point(r, 0.50f, 0.48f), Point(r, 0.69f, 0.64f), strokeWidth, primary);
                break;
            case MetricIconType.Energy:
                AddBattery(vh, r, primary, secondary);
                break;
        }
    }

    private static Vector2 Point(Rect r, float x, float y)
    {
        return new Vector2(Mathf.Lerp(r.xMin, r.xMax, x), Mathf.Lerp(r.yMin, r.yMax, y));
    }

    private static void AddBar(VertexHelper vh, Rect r, float x, float y, float w, float h, Color c)
    {
        CyberDashboardMesh.AddSolidQuad(vh, new Rect(Point(r, x, y), new Vector2(r.width * w, r.height * h)), c);
    }

    private static void AddWave(VertexHelper vh, Rect r, float startX, float endX, float centerY, float amp, Color c)
    {
        const int steps = 22;
        Vector2 prev = Point(r, startX, centerY);
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float x = Mathf.Lerp(startX, endX, t);
            float y = centerY + Mathf.Sin(t * Mathf.PI * 2f) * amp;
            Vector2 next = Point(r, x, y);
            AddLine(vh, prev, next, 2.4f, c);
            prev = next;
        }
    }

    private static void AddArc(VertexHelper vh, Rect r, float centerX, float centerY, float radius, float start, float end, float thick, Color c)
    {
        int steps = 20;
        Vector2 center = Point(r, centerX, centerY);
        float radiusPx = Mathf.Min(r.width, r.height) * radius;
        Vector2 prev = center + AngleToVector(start) * radiusPx;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float angle = Mathf.Lerp(start, end, t);
            Vector2 next = center + AngleToVector(angle) * radiusPx;
            AddLine(vh, prev, next, thick, c);
            prev = next;
        }
    }

    private static Vector2 AngleToVector(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private static void AddBattery(VertexHelper vh, Rect r, Color primary, Color secondary)
    {
        AddLine(vh, Point(r, 0.12f, 0.30f), Point(r, 0.78f, 0.30f), 2f, secondary);
        AddLine(vh, Point(r, 0.12f, 0.70f), Point(r, 0.78f, 0.70f), 2f, secondary);
        AddLine(vh, Point(r, 0.12f, 0.30f), Point(r, 0.12f, 0.70f), 2f, secondary);
        AddLine(vh, Point(r, 0.78f, 0.30f), Point(r, 0.78f, 0.70f), 2f, secondary);
        CyberDashboardMesh.AddSolidQuad(vh, new Rect(Point(r, 0.82f, 0.42f), new Vector2(r.width * 0.06f, r.height * 0.16f)), secondary);
        CyberDashboardMesh.AddSolidQuad(vh, new Rect(Point(r, 0.20f, 0.40f), new Vector2(r.width * 0.14f, r.height * 0.20f)), primary);
        CyberDashboardMesh.AddSolidQuad(vh, new Rect(Point(r, 0.39f, 0.40f), new Vector2(r.width * 0.14f, r.height * 0.20f)), primary);
        CyberDashboardMesh.AddSolidQuad(vh, new Rect(Point(r, 0.58f, 0.40f), new Vector2(r.width * 0.10f, r.height * 0.20f)), primary);
    }

    private static void AddPolyline(VertexHelper vh, Vector2[] points, float thick, Color c)
    {
        for (int i = 1; i < points.Length; i++) AddLine(vh, points[i - 1], points[i], thick, c);
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
