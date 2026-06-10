using System.Collections.Generic;
using UnityEngine;
using Vuforia;

[ExecuteAlways]
public class ImageTargetLayout : MonoBehaviour
{
    public enum LayoutAxis
    {
        X,
        Z
    }

    public LayoutAxis axis = LayoutAxis.X;
    public float spacing = 0.12f;
    public float dashboardPadding = 0.08f;
    public bool centerLayout = true;
    public bool lockY = true;
    public bool lockZ = true;
    public float baseY = 0f;
    public float baseZ = 0f;
    public bool includeInactive = true;

    private void OnEnable()
    {
        Layout();
    }

    private void OnValidate()
    {
        Layout();
    }

    public void Layout()
    {
        var targets = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (!includeInactive && !child.gameObject.activeInHierarchy) continue;
            if (child.GetComponent<ImageTargetBehaviour>() != null)
                targets.Add(child);
        }

        targets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        if (targets.Count == 0) return;

        var widths = new List<float>(targets.Count);
        float total = 0f;
        for (int i = 0; i < targets.Count; i++)
        {
            float width = GetTargetSlotWidth(targets[i]);
            widths.Add(width);
            total += width;
        }
        total += spacing * (targets.Count - 1);

        float cursor = centerLayout ? -total * 0.5f : 0f;
        for (int i = 0; i < targets.Count; i++)
        {
            float width = widths[i];
            float offset = cursor + width * 0.5f;
            var local = targets[i].localPosition;
            float y = lockY ? baseY : local.y;
            float z = lockZ ? baseZ : local.z;

            if (axis == LayoutAxis.X)
                targets[i].localPosition = new Vector3(offset, y, z);
            else
                targets[i].localPosition = new Vector3(local.x, y, offset);

            cursor += width + spacing;
        }
    }

    /// <summary>
    /// Returns the slot width for a target, accounting for both the image
    /// and any dashboard canvas that might extend beyond it.
    /// </summary>
    private float GetTargetSlotWidth(Transform target)
    {
        float imageWidth = GetRawTargetWidth(target);

        // Check if there is a child canvas (dashboard) that might be wider
        float dashboardWorldWidth = 0f;
        var canvases = target.GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in canvases)
        {
            if (canvas.transform == target.transform) continue;
            var rect = canvas.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Compute approximate world-space width: rect width * lossyScale
                float w = rect.rect.width * canvas.transform.lossyScale.x;
                if (w > dashboardWorldWidth) dashboardWorldWidth = w;
            }
        }

        float slotWidth = Mathf.Max(imageWidth, dashboardWorldWidth) + dashboardPadding;
        return Mathf.Max(0.08f, slotWidth);
    }

    private float GetRawTargetWidth(Transform target)
    {
        var imageTarget = target.GetComponent<ImageTargetBehaviour>();
        if (imageTarget != null)
        {
            var size = imageTarget.GetSize();
            float width = size.x;
            // No division by 1000f here. We use the raw size from Vuforia.
            return Mathf.Max(0.08f, width);
        }
        return 0.1f;
    }
}
