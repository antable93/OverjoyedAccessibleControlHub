namespace OverjoyedVersion3;

/// <summary>
/// Shared GraphicsView base for drawing every widget in a Controller's Layout. LiveControllerView
/// and EditorControllerView each own their own interaction handling; this base only knows how to
/// draw widgets and how to swap which Controller is currently displayed.
/// </summary>
public abstract class ControllerView : GraphicsView, IDrawable
{
    public Controller DisplayedController { get; protected set; }

    protected ControllerView(Controller controller)
    {
        DisplayedController = controller ?? throw new ArgumentNullException(nameof(controller));
        Drawable = this;

        SettingsManager.FontColorChanged += (_, _) => Invalidate();
        SettingsManager.SecondaryColorChanged += (_, _) => Invalidate();
    }

    /// <summary>
    /// Swaps which controller this view displays, resetting any in-progress interaction state.
    /// Implementations must update DisplayedController and call Invalidate themselves.
    /// </summary>
    public abstract void DisplayController(Controller controller);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        foreach (var descriptor in DisplayedController.Layout)
        {
            if (!DisplayedController.Widgets.TryGetValue(descriptor.WidgetId, out var widget))
            {
                continue;
            }
            
            widget.OwningControllerView = this;

            var size = widget.GetSize();
            float cx = (float)size.Width / 2f;
            float cy = (float)size.Height / 2f;

            canvas.SaveState();
            canvas.Translate((float)descriptor.X, (float)descriptor.Y);
            canvas.Rotate(descriptor.Rotation, cx, cy);
            canvas.Translate(cx, cy);
            canvas.Scale(descriptor.Scale, descriptor.Scale);
            canvas.Translate(-cx, -cy);
            widget.Draw(canvas, new RectF(PointF.Zero, size));
            DrawWidgetOverlays(canvas, descriptor, size);
            canvas.RestoreState();
        }
    }
    /// <summary>
    /// Hook for each type of ControllerView to add custom drawing behavior. 
    /// Example: Selection rectangle around widgets in the EditorControllerView. 
    /// Does nothing by default.
    /// </summary>
    protected virtual void DrawWidgetOverlays(ICanvas canvas, WidgetDescriptor descriptor, Size size) { }

    protected IEnumerable<Widget> WidgetsTopmostFirst()
    {
        for (int i = DisplayedController.Layout.Count - 1; i >= 0; i--)
        {
            var descriptor = DisplayedController.Layout[i];
            if (DisplayedController.Widgets.TryGetValue(descriptor.WidgetId, out var widget))
                yield return widget;
        }
    }
    /// <summary>
    /// Converts a canvas-space point into the widget's local, untransformed geometry space by
    /// applying the inverse of the transform Draw applies (translate, then rotate+scale around the
    /// widget's center) — so HitTestSlot/IsOverKnob keep working in the same space they always have,
    /// regardless of the widget's Rotation/Scale.
    /// </summary>
    protected static PointF ToLocal(Point canvasPoint, Widget widget)
    {
        var descriptor = widget.Descriptor;
        var size = widget.GetSize();
        float cx = (float)size.Width / 2f;
        float cy = (float)size.Height / 2f;

        var q = new PointF((float)(canvasPoint.X - descriptor.X), (float)(canvasPoint.Y - descriptor.Y));
        q = new PointF(q.X - cx, q.Y - cy);
        q = RotatePoint(q, -descriptor.Rotation);
        q = new PointF(q.X / descriptor.Scale, q.Y / descriptor.Scale);
        return new PointF(q.X + cx, q.Y + cy);
    }
    private static PointF RotatePoint(PointF point, float degrees)
    {
        float radians = degrees * MathF.PI / 180f;
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        return new PointF(point.X * cos - point.Y * sin, point.X * sin + point.Y * cos);
    }
    protected static bool InBounds(PointF local, Size size) =>
        local.X >= 0 && local.Y >= 0 && local.X <= size.Width && local.Y <= size.Height;
}
