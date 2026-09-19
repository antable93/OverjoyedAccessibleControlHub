namespace OverjoyedVersion3;

public enum DialInteractionMode { Click, Hover }

public class Dial : Widget
{
    private List<DialQuadrant> _quadrants;
    private string? _clickHeldSlotId;

    private DialWidgetDescriptor DialDescriptor => (DialWidgetDescriptor)Descriptor;

    public Dial(Controller controller, DialWidgetDescriptor descriptor) : base(controller, descriptor)
    {
        _quadrants = CreateQuadrants(descriptor.QuadrantCount);

        VirtualInputManager.Instance.AddVirtualInputDevice(VirtualDeviceType.InputSimulatorKeyboard);
        VirtualInputManager.Instance.AddVirtualInputDevice(VirtualDeviceType.VigemXboxController);

        _ = LoadIconsAsync();
    }

    public void SetQuadrantCount(int count)
    {
        DialDescriptor.QuadrantCount = Math.Max(1, count);
        _quadrants = CreateQuadrants(DialDescriptor.QuadrantCount);
        OwningControllerView?.Invalidate();
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float cx = dirtyRect.Width / 2f;
        float cy = dirtyRect.Height / 2f;
        float dialR = DialDescriptor.DialRadius;
        float deadZoneR = DialDescriptor.DeadZoneRadius;
        string? highlighted = SelectedSlotId;

        Color highlightColor = Color.FromArgb(DialDescriptor.HighlightColor);
        Color strokeColor = Color.FromArgb(DialDescriptor.StrokeColor);

        // 1. Slot fill
        if (highlighted != null)
        {
            canvas.FillColor = highlightColor.WithAlpha(0.25f);
            DrawSlotFill(canvas, cx, cy, dialR, deadZoneR, highlighted);
        }

        // 2. Radial divider lines
        canvas.StrokeSize = 2f;
        for (int i = 0; i < _quadrants.Count; i++)
        {
            bool isHighlighted = highlighted != null &&
                highlighted.StartsWith("Quadrant_") &&
                int.TryParse(highlighted["Quadrant_".Length..], out int hIdx) &&
                (i == hIdx || i == (hIdx + 1) % _quadrants.Count);

            canvas.StrokeColor = isHighlighted ? highlightColor : strokeColor;
            float rad = _quadrants[i].StartAngle * MathF.PI / 180f;
            float cosA = MathF.Cos(rad), sinA = MathF.Sin(rad);
            canvas.DrawLine(cx + deadZoneR * cosA, cy + deadZoneR * sinA,
                            cx + dialR * cosA, cy + dialR * sinA);
        }

        // 3. Structural outlines
        canvas.StrokeColor = strokeColor;
        canvas.StrokeSize = 2f;
        canvas.DrawCircle(cx, cy, deadZoneR);

        // 4. Icons and placeholders
        DrawSlotContents(canvas, cx, cy, dialR, deadZoneR);
    }

    public override async Task LoadIconsAsync()
    {
        icons.Clear();

        string prefix = DialDescriptor.WidgetId + "/";

        foreach (var (key, slotData) in Controller.SlotData)
        {
            if (!key.StartsWith(prefix)) continue;

            var slotId = key[prefix.Length..];
            int size = (int)GetSlotIconSize(slotId);
            if (size > 0)
            {
                var c = GetIndicatorColor(slotData);
                icons[slotId] = await Icon.GenerateAsync(IconManager.GetSvgPath(slotData.SvgSource), size, size,
                    (int)(c.Red * 255), (int)(c.Green * 255), (int)(c.Blue * 255));
            }
        }

        OwningControllerView?.Invalidate();
    }
    public override async Task LoadIconForSlotAsync(string slotId)
    {
        icons.Remove(slotId);
        string key = $"{DialDescriptor.WidgetId}/{slotId}";

        if (Controller.SlotData.TryGetValue(key, out var slotData))
        {
            int size = (int)GetSlotIconSize(slotId);
            if (size > 0)
            {
                var c = GetIndicatorColor(slotData);
                icons[slotId] = await Icon.GenerateAsync(IconManager.GetSvgPath(slotData.SvgSource), size, size,
                    (int)(c.Red * 255), (int)(c.Green * 255), (int)(c.Blue * 255));
            }
        }

        OwningControllerView?.Invalidate();
    }
    public override Size GetSize()
    {
        double w = DialDescriptor.DialRadius * 2.0 + 4.0;
        return new Size(w, w);
    }
    public override string? GetHitSlotID(PointF pt)
    {
        var size = GetSize();
        float cx = (float)size.Width / 2f, cy = (float)size.Height / 2f;
        float dialR = DialDescriptor.DialRadius;
        float deadZoneR = DialDescriptor.DeadZoneRadius;

        float dx = pt.X - cx, dy = pt.Y - cy;
        float distSq = dx * dx + dy * dy;
        if (distSq > dialR * dialR) return null;
        if (distSq <= deadZoneR * deadZoneR) return null;

        float angle = MathF.Atan2(dy, dx) * 180f / MathF.PI;
        if (angle < 0) angle += 360f;

        for (int i = 0; i < _quadrants.Count; i++)
        {
            if (angle >= _quadrants[i].StartAngle && angle < _quadrants[i].StartAngle + _quadrants[i].SweepAngle)
            {
                return $"Quadrant_{i}";
            }
        }

        return "Quadrant_0";
    }

    public override bool OnPress(PointF localPoint)
    {
        var slotId = GetHitSlotID(localPoint);
        if (slotId == null || !IsModeEnabled(slotId, DialInteractionMode.Click)) return false;

        _clickHeldSlotId = slotId;
        Controller.TriggerBindingDown(WidgetId, slotId, nameof(DialInteractionMode.Click));
        return true;
    }
    public override void OnRelease()
    {
        if (_clickHeldSlotId != null)
        {
            Controller.TriggerBindingUp(WidgetId, _clickHeldSlotId, nameof(DialInteractionMode.Click));
            _clickHeldSlotId = null;
        }
    }
    public override void OnPointerMoved(PointF localPoint)
    {
        var slotId = GetHitSlotID(localPoint);
        if (slotId == SelectedSlotId) return;

        if (SelectedSlotId != null && IsModeEnabled(SelectedSlotId, DialInteractionMode.Hover))
            Controller.TriggerBindingUp(WidgetId, SelectedSlotId, nameof(DialInteractionMode.Hover));

        SelectedSlotId = slotId;

        if (slotId != null && IsModeEnabled(slotId, DialInteractionMode.Hover))
            Controller.TriggerBindingDown(WidgetId, slotId, nameof(DialInteractionMode.Hover));
    }
    public override void OnPointerExited()
    {
        if (SelectedSlotId != null && IsModeEnabled(SelectedSlotId, DialInteractionMode.Hover))
            Controller.TriggerBindingUp(WidgetId, SelectedSlotId, nameof(DialInteractionMode.Hover));

        SelectedSlotId = null;
    }

    private bool IsModeEnabled(string slotId, DialInteractionMode mode)
    {
        var key = $"{WidgetId}/{slotId}";
        if (!Controller.SlotData.TryGetValue(key, out var slotData))
            return mode == DialInteractionMode.Click;

        var option = $"{mode}Enabled";
        if (slotData.Options.TryGetValue(option, out var enabled) && bool.TryParse(enabled, out var result))
            return result;

        return slotData.Options.GetValueOrDefault("InteractionMode", "Click") == mode.ToString();
    }
    private static List<DialQuadrant> CreateQuadrants(int count)
    {
        var list = new List<DialQuadrant>();
        float sweep = 360f / Math.Max(1, count);
        float rotationOffset = count == 8 ? 22.5f : 0f;

        for (int i = 0; i < count; i++)
        {
            list.Add(new DialQuadrant(rotationOffset + i * sweep, sweep));
        }

        return list;
    }
    private float GetSlotIconSize(string slotId)
    {
        float dialR = DialDescriptor.DialRadius;
        float deadZoneR = DialDescriptor.DeadZoneRadius;

        if (slotId.StartsWith("Quadrant_") &&
            int.TryParse(slotId["Quadrant_".Length..], out int idx) &&
            idx < _quadrants.Count)
        {
            float r = (deadZoneR + dialR) / 2f;
            float sweepRad = _quadrants[idx].SweepAngle * MathF.PI / 180f;
            return MathF.Min(dialR - deadZoneR, 2f * r * MathF.Sin(sweepRad / 2f)) * 0.55f;
        }

        return 0f;
    }
    private void DrawSlotFill(ICanvas canvas, float cx, float cy, float dialR, float ringR, string slotId)
    {
        if (slotId.StartsWith("Quadrant_") && int.TryParse(slotId["Quadrant_".Length..], out int idx) &&
            idx < _quadrants.Count)
        {
            canvas.FillPath(DrawFilledQuadrant(cx, cy, ringR, dialR, _quadrants[idx]));
        }
    }
    private void DrawSlotContents(ICanvas canvas, float cx, float cy, float dialR, float ringR)
    {
        for (int i = 0; i < _quadrants.Count; i++)
        {
            var slotId = $"Quadrant_{i}";
            PointF center = GetSlotIconCenter(i, cx, cy, dialR, ringR);
            float size = GetSlotIconSize(slotId);
            float fontSize = MathF.Max(8f, size * 0.45f);

            string bindingKey = $"{DialDescriptor.WidgetId}/{slotId}";
            SlotData? slotData = null;
            Controller.SlotData.TryGetValue(bindingKey, out slotData);

            if (slotData == null) { continue; }

            bool isUsingIcon = slotData?.Options.GetValueOrDefault("IsUsingIcon", "true") != "false";

            float offsetX = float.TryParse(slotData?.Options.GetValueOrDefault("IndicatorOffsetX"), out var ox) ? ox : 0f;
            float offsetY = float.TryParse(slotData?.Options.GetValueOrDefault("IndicatorOffsetY"), out var oy) ? oy : 0f;
            float rawIndicatorSize = float.TryParse(slotData?.Options.GetValueOrDefault("IndicatorSize"), out var sz) && sz > 0 ? sz : 0f;
            float indicatorSize = rawIndicatorSize > 0 ? rawIndicatorSize : (isUsingIcon ? size : fontSize);

            if (!isUsingIcon)
            {
                canvas.FontColor = GetIndicatorColor(slotData);
                canvas.FontSize = indicatorSize;
                canvas.DrawString(slotData?.Label ?? string.Empty,
                    center.X - size / 2f + offsetX, center.Y - size / 2f + offsetY,
                    size, size, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            else if (icons.TryGetValue(slotId, out var icon) && icon?.Visual is not null)
            {
                var v = icon.Visual;
                canvas.DrawImage(v, center.X - indicatorSize / 2f + offsetX, center.Y - indicatorSize / 2f + offsetY, indicatorSize, indicatorSize);
            }
            else
            {
                canvas.FontColor = SettingsManager.FontColor.WithAlpha(0.35f);
                canvas.FontSize = fontSize;
                canvas.DrawString("?", center.X - size / 2f, center.Y - size / 2f,
                    size, size, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
    }
    private static Color GetIndicatorColor(SlotData? slotData) =>
        Color.FromArgb(slotData?.Options.GetValueOrDefault("IndicatorColor", "#FFFFFF") ?? "#FFFFFF");
    private PointF GetSlotIconCenter(int quadrantIndex, float cx, float cy, float dialR, float ringR)
    {
        var q = _quadrants[quadrantIndex];
        float bisector = (q.StartAngle + q.SweepAngle / 2f) * MathF.PI / 180f;
        float r = (ringR + dialR) / 2f;
        return new PointF(cx + r * MathF.Cos(bisector), cy + r * MathF.Sin(bisector));
    }
    private static PathF DrawFilledQuadrant(float cx, float cy, float innerR, float outerR, DialQuadrant q)
    {
        var path = new PathF();
        const int steps = 20;
        float startRad = q.StartAngle * MathF.PI / 180f;
        float endRad = (q.StartAngle + q.SweepAngle) * MathF.PI / 180f;

        path.MoveTo(cx + outerR * MathF.Cos(startRad), cy + outerR * MathF.Sin(startRad));
        for (int s = 1; s <= steps; s++)
        {
            float a = startRad + (endRad - startRad) * s / steps;
            path.LineTo(cx + outerR * MathF.Cos(a), cy + outerR * MathF.Sin(a));
        }
        for (int s = 0; s <= steps; s++)
        {
            float a = endRad - (endRad - startRad) * s / steps;
            path.LineTo(cx + innerR * MathF.Cos(a), cy + innerR * MathF.Sin(a));
        }

        path.Close();
        return path;
    }
}
