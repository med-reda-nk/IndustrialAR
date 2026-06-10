using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public static class DashboardUIFactory
{
    public const float ReadableWorldScaleMultiplier = 2.1f;
    public const float TargetCoverageMultiplier = 1.15f;
    private const float MinReadableWorldDimension = 0.65f;

    private static Sprite circleSprite;
    private static Sprite roundedRectSprite;

    public enum CyberStyle
    {
        Generic,
        PLC,
        HMI,
        Motor,
        SignalTower,
        EStop,
        PowerMeter,
        PowerMeterGrey
    }

    private struct CyberPalette
    {
        public Color backgroundA;
        public Color backgroundB;
        public Color accentA;
        public Color accentB;
        public Color accentC;
        public Color text;
        public Color muted;
        public Color danger;
    }

    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = dist <= radius ? (byte)255 : (byte)0;
                tex.SetPixel(x, y, new Color32(255, 255, 255, alpha));
            }
        }
        tex.Apply();

        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

    public static Sprite GetRoundedRectSprite()
    {
        if (roundedRectSprite != null) return roundedRectSprite;

        const int size = 128;
        const int radius = 28;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, 0, x - (size - 1 - radius));
                float dy = Mathf.Max(radius - y, 0, y - (size - 1 - radius));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                byte alpha = dist <= radius ? (byte)255 : (byte)0;
                tex.SetPixel(x, y, new Color32(255, 255, 255, alpha));
            }
        }

        tex.Apply();
        roundedRectSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedRectSprite;
    }

    public static void ApplyRoundedCorners(Image image)
    {
        if (image == null || image.sprite != null) return;
        image.sprite = GetRoundedRectSprite();
        image.type = Image.Type.Sliced;
    }

    public static Canvas EnsureWorldCanvas(GameObject host, Vector2 size, float scale)
    {
        if (host == null) return null;

        var canvas = host.GetComponent<Canvas>();
        if (canvas == null) canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;

        var rect = host.GetComponent<RectTransform>();
        if (rect == null) rect = host.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.localScale = new Vector3(scale, scale, scale);

        var scaler = host.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 28f;

        var raycaster = host.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = host.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    public static CameraFacingDashboard EnsureCameraFacingDashboard(GameObject host)
    {
        if (host == null) return null;

        var facing = host.GetComponent<CameraFacingDashboard>();
        if (facing == null) facing = host.AddComponent<CameraFacingDashboard>();
        facing.matchCameraPlane = true;
        return facing;
    }

    public static float ComputeScaleForImageTarget(GameObject host, Vector2 canvasSize, float fallbackScale)
    {
        if (host == null) return fallbackScale;
        var target = host.GetComponentInParent<Vuforia.ImageTargetBehaviour>();
        if (target == null) return fallbackScale;

        var size = ResolveImageTargetVisualSize(host, target);
        if (size.x <= 0f || size.y <= 0f) return fallbackScale;

        float scaleX = size.x / Mathf.Max(1f, canvasSize.x);
        float scaleY = size.y / Mathf.Max(1f, canvasSize.y);
        float canvasMaxDimension = Mathf.Max(1f, Mathf.Max(canvasSize.x, canvasSize.y));
        float coverScale = Mathf.Max(scaleX, scaleY) * TargetCoverageMultiplier;
        float readableScale = fallbackScale * ReadableWorldScaleMultiplier;
        float minScale = MinReadableWorldDimension / canvasMaxDimension;

        return Mathf.Max(coverScale, readableScale, minScale);
    }

    private static Vector2 ResolveImageTargetVisualSize(GameObject host, Vuforia.ImageTargetBehaviour target)
    {
        var markerCanvas = target.transform.Find("Canvas");
        if (markerCanvas != null && markerCanvas.gameObject != host)
        {
            var markerRect = markerCanvas.GetComponent<RectTransform>();
            if (markerRect != null && markerRect.rect.width > 0f && markerRect.rect.height > 0f)
            {
                return new Vector2(
                    markerRect.rect.width * Mathf.Abs(markerCanvas.localScale.x),
                    markerRect.rect.height * Mathf.Abs(markerCanvas.localScale.y));
            }
        }

        return target.GetSize();
    }

    public static void AlignToImageTargetTop(Transform host, float extraOffset)
    {
        if (host == null) return;
        var target = host.GetComponentInParent<Vuforia.ImageTargetBehaviour>();
        if (target == null) return;

        var size = ResolveImageTargetVisualSize(host.gameObject, target);
        if (size.y <= 0f) return;

        float panelHalfHeight = 0f;
        var rect = host.GetComponent<RectTransform>();
        if (rect != null && rect.sizeDelta.y > 0f)
        {
            panelHalfHeight = rect.sizeDelta.y * Mathf.Abs(rect.localScale.y) * 0.5f;
        }

        host.localPosition = new Vector3(0f, (size.y * 0.5f) + panelHalfHeight + extraOffset, 0f);
    }

    public static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, size, anchoredPos, anchorMin, anchorMax, pivot);
        return rect;
    }

    public static void SetRect(RectTransform rect, Vector2 size, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        if (rect == null) return;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    public static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 anchoredPos, Color color, Sprite sprite = null)
    {
        var rect = CreateRect(name, parent, size, anchoredPos, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
        }
        else
        {
            ApplyRoundedCorners(image);
        }
        image.raycastTarget = false;
        return image;
    }

    public static Image CreateFilledImage(string name, Transform parent, Vector2 size, Vector2 anchoredPos, Color color, Image.FillMethod fillMethod)
    {
        var rect = CreateRect(name, parent, size, anchoredPos, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = GetCircleSprite();
        image.type = Image.Type.Filled;
        image.fillMethod = fillMethod;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = false;
        image.fillAmount = 0.6f;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, TMP_FontAsset font, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var rect = CreateRect(name, parent, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = fontSize;
        tmp.fontSizeMin = Mathf.Max(8f, fontSize * 0.72f);
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    public static Button CreateButton(string name, Transform parent, Vector2 size, Vector2 anchoredPos, Color color, string label, TMP_FontAsset font, float fontSize, Color labelColor)
    {
        var rect = CreateRect(name, parent, size, anchoredPos, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        ApplyRoundedCorners(image);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        AddMotion(rect, DashboardMotionAnimator.MotionType.Button);

        var text = CreateText("Label", rect, label, font, fontSize, TextAlignmentOptions.Center, labelColor);
        SetRect(text.rectTransform, size, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        return button;
    }

    public static Button CreateCircleButton(string name, Transform parent, float diameter, Vector2 anchoredPos, Color color, string label, TMP_FontAsset font, float fontSize, Color labelColor)
    {
        var rect = CreateRect(name, parent, new Vector2(diameter, diameter), anchoredPos, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = GetCircleSprite();
        image.type = Image.Type.Simple;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        AddMotion(rect, DashboardMotionAnimator.MotionType.Button);

        var text = CreateText("Label", rect, label, font, fontSize, TextAlignmentOptions.Center, labelColor);
        SetRect(text.rectTransform, new Vector2(diameter, diameter), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        return button;
    }

    public static void ApplyCyberFuturisticSkin(RectTransform root, CyberStyle style, bool includeBackground = true)
    {
        if (root == null) return;

        var palette = GetPalette(style);
        Vector2 visualSize = ResolveRectSize(root);
        if (root.GetComponent<RectMask2D>() == null) root.gameObject.AddComponent<RectMask2D>();
        AddMotion(root, DashboardMotionAnimator.MotionType.Panel);

        if (includeBackground)
        {
            var backdrop = CreateRect("CyberBackdrop", root, visualSize, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            backdrop.gameObject.AddComponent<CanvasRenderer>();
            var backdropGraphic = backdrop.gameObject.AddComponent<global::CyberGradientGraphic>();
            backdropGraphic.SetColors(palette.backgroundA, palette.backgroundB, new Color(palette.accentB.r, palette.accentB.g, palette.accentB.b, 0.18f));
            backdropGraphic.raycastTarget = false;
            backdrop.SetAsFirstSibling();

            var grid = CreateRect("CyberGrid", root, visualSize, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            grid.gameObject.AddComponent<CanvasRenderer>();
            var gridGraphic = grid.gameObject.AddComponent<global::CyberGridGraphic>();
            gridGraphic.SetColors(new Color(palette.accentA.r, palette.accentA.g, palette.accentA.b, 0.16f), new Color(palette.accentB.r, palette.accentB.g, palette.accentB.b, 0.08f));
            gridGraphic.raycastTarget = false;
            grid.SetSiblingIndex(1);
        }

        StyleImages(root, palette);
        StyleText(root, palette);
        StyleButtons(root, palette);

        AddCyberFrame(root, palette, visualSize);
        AddDataRails(root, palette, visualSize);

        if (includeBackground)
        {
            var scanlines = CreateRect("CyberScanlines", root, visualSize, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            scanlines.gameObject.AddComponent<CanvasRenderer>();
            var scanGraphic = scanlines.gameObject.AddComponent<global::CyberScanlineGraphic>();
            scanGraphic.SetColor(new Color(palette.accentA.r, palette.accentA.g, palette.accentA.b, 0.055f));
            scanGraphic.raycastTarget = false;
            scanlines.SetAsLastSibling();
        }
    }

    public static void ApplyCyberOverlay(RectTransform root, CyberStyle style, bool includeBackground = false, bool includeLinework = true)
    {
        if (root == null) return;

        var palette = GetPalette(style);
        Vector2 visualSize = ResolveRectSize(root);
        if (root.GetComponent<RectMask2D>() == null) root.gameObject.AddComponent<RectMask2D>();
        AddMotion(root, DashboardMotionAnimator.MotionType.Panel);

        if (includeBackground)
        {
            var backdrop = CreateRect("CyberBackdrop", root, visualSize, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            backdrop.gameObject.AddComponent<CanvasRenderer>();
            var backdropGraphic = backdrop.gameObject.AddComponent<global::CyberGradientGraphic>();
            backdropGraphic.SetColors(palette.backgroundA, palette.backgroundB, new Color(palette.accentB.r, palette.accentB.g, palette.accentB.b, 0.12f));
            backdropGraphic.raycastTarget = false;
            AddCyberAnimation(backdrop, CyberLayerAnimator.AnimationMode.Pulse, 0.28f, 0.72f, 1f, 0f, Vector2.zero);
            backdrop.SetAsFirstSibling();

            if (includeLinework)
            {
                var grid = CreateRect("CyberGrid", root, visualSize, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                grid.gameObject.AddComponent<CanvasRenderer>();
                var gridGraphic = grid.gameObject.AddComponent<global::CyberGridGraphic>();
                gridGraphic.SetColors(new Color(palette.accentA.r, palette.accentA.g, palette.accentA.b, 0.12f), new Color(palette.accentB.r, palette.accentB.g, palette.accentB.b, 0.06f));
                gridGraphic.raycastTarget = false;
                AddCyberAnimation(grid, CyberLayerAnimator.AnimationMode.DriftPulse, 0.08f, 0.45f, 0.85f, 3f, new Vector2(1f, -0.35f));
                grid.SetSiblingIndex(1);
            }
        }

        if (!includeLinework) return;

        AddCyberFrame(root, palette, visualSize);
        AddDataRails(root, palette, visualSize);

        var scanlines = CreateRect("CyberScanlines", root, visualSize, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        scanlines.gameObject.AddComponent<CanvasRenderer>();
        var scanGraphic = scanlines.gameObject.AddComponent<global::CyberScanlineGraphic>();
        scanGraphic.SetColor(new Color(palette.accentA.r, palette.accentA.g, palette.accentA.b, includeBackground ? 0.04f : 0.025f));
        scanGraphic.raycastTarget = false;
        AddCyberAnimation(scanlines, CyberLayerAnimator.AnimationMode.Scanlines, 1.05f, 0.55f, 1f, 0f, Vector2.zero);
        scanlines.SetAsLastSibling();
    }

    private static Vector2 ResolveRectSize(RectTransform rect)
    {
        Vector2 size = rect.sizeDelta;
        if (size.x <= 1f || size.y <= 1f) size = rect.rect.size;
        if (size.x <= 1f) size.x = 480f;
        if (size.y <= 1f) size.y = 480f;
        return size;
    }

    private static CyberPalette GetPalette(CyberStyle style)
    {
        var palette = new CyberPalette
        {
            backgroundA = new Color32(24, 24, 24, 242),
            backgroundB = new Color32(58, 58, 58, 238),
            accentA = new Color32(198, 198, 198, 255),
            accentB = new Color32(118, 148, 152, 255),
            accentC = new Color32(226, 232, 232, 255),
            text = new Color32(238, 242, 242, 255),
            muted = new Color32(166, 176, 176, 255),
            danger = new Color32(255, 47, 88, 255)
        };

        switch (style)
        {
            case CyberStyle.PLC:
                palette.accentA = new Color32(196, 206, 206, 255);
                palette.accentB = new Color32(94, 128, 132, 255);
                palette.accentC = new Color32(214, 224, 224, 255);
                break;
            case CyberStyle.HMI:
                palette.accentA = new Color32(210, 218, 218, 255);
                palette.accentB = new Color32(86, 116, 120, 255);
                palette.accentC = new Color32(234, 238, 238, 255);
                break;
            case CyberStyle.Motor:
                palette.accentA = new Color32(196, 206, 206, 255);
                palette.accentB = new Color32(96, 126, 132, 255);
                palette.accentC = new Color32(226, 232, 232, 255);
                break;
            case CyberStyle.SignalTower:
                palette.accentA = new Color32(198, 198, 198, 255);
                palette.accentB = new Color32(110, 138, 142, 255);
                palette.accentC = new Color32(226, 232, 232, 255);
                break;
            case CyberStyle.EStop:
                palette.backgroundA = new Color32(24, 24, 24, 240);
                palette.backgroundB = new Color32(58, 58, 58, 238);
                palette.accentA = new Color32(190, 190, 190, 255);
                palette.accentB = new Color32(112, 112, 112, 255);
                palette.accentC = new Color32(228, 228, 228, 255);
                break;
            case CyberStyle.PowerMeter:
                palette.accentA = new Color32(198, 198, 198, 255);
                palette.accentB = new Color32(120, 120, 120, 255);
                palette.accentC = new Color32(228, 228, 228, 255);
                break;
            case CyberStyle.PowerMeterGrey:
                palette.backgroundA = new Color32(28, 28, 28, 224);
                palette.backgroundB = new Color32(78, 78, 78, 210);
                palette.accentA = new Color32(198, 198, 198, 255);
                palette.accentB = new Color32(120, 120, 120, 255);
                palette.accentC = new Color32(228, 228, 228, 255);
                palette.text = new Color32(238, 238, 238, 255);
                palette.muted = new Color32(176, 176, 176, 255);
                break;
        }

        return palette;
    }

    private static void StyleImages(RectTransform root, CyberPalette palette)
    {
        var images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            var image = images[i];
            if (image == null) continue;
            string lowerName = image.name.ToLowerInvariant();
            if (lowerName.Contains("glow") || lowerName.Contains("led") || lowerName.Contains("module") || lowerName.Contains("mushroom"))
            {
                AddGlow(image.gameObject, image.color, 2f);
                var motion = AddMotion(image.rectTransform, DashboardMotionAnimator.MotionType.Indicator);
                if (motion != null)
                {
                    motion.pulse = lowerName.Contains("glow") || lowerName.Contains("led");
                    motion.pulseAmount = 0.012f;
                    motion.pulseSpeed = 0.8f;
                }
                continue;
            }

            var rect = image.rectTransform;
            float area = Mathf.Abs(rect.rect.width * rect.rect.height);
            if (area > 900f)
            {
                Color source = image.color;
                float alpha = Mathf.Clamp(source.a, 0.38f, 0.92f);
                Color glass = new Color(palette.backgroundA.r, palette.backgroundA.g, palette.backgroundA.b, alpha);
                image.color = Color.Lerp(glass, source, 0.18f);
                AddGlow(image.gameObject, palette.accentA, area > 25000f ? 3f : 1.5f);
            }
            else if (area > 120f)
            {
                image.color = Color.Lerp(image.color, palette.accentA, 0.12f);
            }
        }
    }

    private static void StyleText(RectTransform root, CyberPalette palette)
    {
        var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            var text = texts[i];
            if (text == null) continue;

            string upper = text.text != null ? text.text.ToUpperInvariant() : string.Empty;
            bool warning = upper.Contains("FAULT") || upper.Contains("STOP") || upper.Contains("HALT") || upper.Contains("ALARM");
            bool live = upper.Contains("RUN") || upper.Contains("GOOD") || upper.Contains("READY");

            if (warning) text.color = palette.danger;
            else if (live) text.color = palette.accentC;
            else text.color = Color.Lerp(text.color, palette.text, 0.74f);

            text.fontStyle |= FontStyles.UpperCase;
            text.characterSpacing = Mathf.Max(text.characterSpacing, 1.4f);
            text.outlineColor = new Color(palette.accentA.r, palette.accentA.g, palette.accentA.b, 0.48f);
            text.outlineWidth = Mathf.Max(text.outlineWidth, 0.055f);
            text.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    private static void StyleButtons(RectTransform root, CyberPalette palette)
    {
        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button == null) continue;

            var image = button.targetGraphic as Image;
            if (image != null)
            {
                string lower = button.name.ToLowerInvariant();
                Color accent = lower.Contains("stop") || lower.Contains("reset") || lower.Contains("ack") ? palette.danger : palette.accentA;
                image.color = new Color(accent.r, accent.g, accent.b, 0.18f);
                AddGlow(button.gameObject, accent, 2f);
            }

            AddMotion(button.GetComponent<RectTransform>(), DashboardMotionAnimator.MotionType.Button);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(palette.accentA.r, palette.accentA.g, palette.accentA.b, 0.9f);
            colors.pressedColor = new Color(palette.accentB.r, palette.accentB.g, palette.accentB.b, 0.95f);
            colors.selectedColor = new Color(palette.accentC.r, palette.accentC.g, palette.accentC.b, 0.8f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.25f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
        }
    }

    private static DashboardMotionAnimator AddMotion(RectTransform rect, DashboardMotionAnimator.MotionType type)
    {
        if (rect == null) return null;
        var motion = rect.GetComponent<DashboardMotionAnimator>();
        if (motion == null) motion = rect.gameObject.AddComponent<DashboardMotionAnimator>();
        motion.motionType = type;
        if (type == DashboardMotionAnimator.MotionType.Button)
        {
            motion.hoverScale = 1.045f;
            motion.pressScale = 0.955f;
            motion.entranceDuration = 0.16f;
        }
        else if (type == DashboardMotionAnimator.MotionType.Panel)
        {
            motion.entranceDuration = 0.36f;
        }
        return motion;
    }

    private static void AddGlow(GameObject target, Color color, float distance)
    {
        if (target == null) return;

        var outline = target.GetComponent<Outline>();
        if (outline == null) outline = target.AddComponent<Outline>();
        outline.effectColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a) * 0.55f);
        outline.effectDistance = new Vector2(distance, -distance);

        var shadow = target.GetComponent<Shadow>();
        if (shadow == null) shadow = target.AddComponent<Shadow>();
        shadow.effectColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a) * 0.18f);
        shadow.effectDistance = new Vector2(distance * 1.8f, -distance * 1.8f);
    }

    private static void AddCyberFrame(RectTransform root, CyberPalette palette, Vector2 visualSize)
    {
        var frame = CreateRect("CyberFrame", root, visualSize, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        frame.gameObject.AddComponent<CanvasRenderer>();
        var frameGraphic = frame.gameObject.AddComponent<global::CyberCornerFrameGraphic>();
        frameGraphic.SetColors(palette.accentA, palette.accentB, palette.accentC);
        frameGraphic.raycastTarget = false;
        AddCyberAnimation(frame, CyberLayerAnimator.AnimationMode.Pulse, 0.7f, 0.42f, 1f, 0f, Vector2.zero);
        frame.SetAsLastSibling();
    }

    private static void AddDataRails(RectTransform root, CyberPalette palette, Vector2 visualSize)
    {
        float width = Mathf.Max(visualSize.x, 100f);
        float height = Mathf.Max(visualSize.y, 100f);
        float topY = height * 0.5f - 26f;
        float bottomY = -height * 0.5f + 26f;

        var topRail = CreateImage("CyberTopRail", root, new Vector2(width * 0.92f, 3f), new Vector2(0f, topY), new Color(palette.accentA.r, palette.accentA.g, palette.accentA.b, 0.72f));
        topRail.raycastTarget = false;
        AddCyberAnimation(topRail.rectTransform, CyberLayerAnimator.AnimationMode.DriftPulse, 0.42f, 0.55f, 1f, 8f, Vector2.right);
        var bottomRail = CreateImage("CyberBottomRail", root, new Vector2(width * 0.92f, 3f), new Vector2(0f, bottomY), new Color(palette.accentB.r, palette.accentB.g, palette.accentB.b, 0.58f));
        bottomRail.raycastTarget = false;
        AddCyberAnimation(bottomRail.rectTransform, CyberLayerAnimator.AnimationMode.DriftPulse, 0.36f, 0.5f, 0.95f, 7f, Vector2.left);

        for (int i = 0; i < 5; i++)
        {
            float x = -width * 0.37f + i * width * 0.185f;
            var tick = CreateImage("CyberRailTick" + i, root, new Vector2(42f, 8f), new Vector2(x, topY - 10f), new Color(palette.accentC.r, palette.accentC.g, palette.accentC.b, 0.55f));
            tick.raycastTarget = false;
            AddCyberAnimation(tick.rectTransform, CyberLayerAnimator.AnimationMode.Pulse, 0.55f + i * 0.08f, 0.28f, 0.9f, 0f, Vector2.zero);
        }
    }

    private static CyberLayerAnimator AddCyberAnimation(RectTransform rect, CyberLayerAnimator.AnimationMode mode, float speed, float pulseMin, float pulseMax, float driftAmount, Vector2 driftAxis)
    {
        if (rect == null) return null;
        var animator = rect.GetComponent<CyberLayerAnimator>();
        if (animator == null) animator = rect.gameObject.AddComponent<CyberLayerAnimator>();
        animator.mode = mode;
        animator.speed = speed;
        animator.pulseMin = pulseMin;
        animator.pulseMax = pulseMax;
        animator.driftAmount = driftAmount;
        animator.driftAxis = driftAxis;
        return animator;
    }

    public static EventTrigger AddPointerEvent(GameObject target, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        if (target == null) return null;

        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
        return trigger;
    }

    public static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            RepairCanvasRenderers(parent.GetChild(i));
            if (Application.isPlaying) Object.Destroy(parent.GetChild(i).gameObject);
            else Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void RepairCanvasRenderers(Transform child)
    {
        if (child == null) return;
        var graphics = child.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            var graphic = graphics[i];
            if (graphic != null && graphic.GetComponent<CanvasRenderer>() == null)
            {
                graphic.gameObject.AddComponent<CanvasRenderer>();
            }
        }
    }
}
