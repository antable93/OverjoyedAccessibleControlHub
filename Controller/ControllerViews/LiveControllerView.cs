namespace OverjoyedVersion3;

/// <summary>
/// Routes pointer input to widgets for live play: press/release dispatch with implicit capture
/// (the widget whose OnPress returns true gets the matching release regardless of where the
/// pointer ends up), and broadcast hover (PointerMoved/PointerExited) for hover-based widgets.
/// </summary>
public class LiveControllerView : ControllerView
{
    private string? _pressedWidgetId;
    private readonly HashSet<string> _hoveredWidgetIds = new();

    public LiveControllerView(Controller controller) : base(controller)
    {
        BackgroundColor = Colors.Transparent;
        StartInteraction += OnStartInteraction;
        EndInteraction += OnEndInteraction;

        var pointer = new PointerGestureRecognizer();
        pointer.PointerMoved += OnPointerMoved;
        pointer.PointerExited += OnPointerExited;
        GestureRecognizers.Add(pointer);
    }

    public override void DisplayController(Controller controller)
    {
        DisplayedController = controller ?? throw new ArgumentNullException(nameof(controller));
        _pressedWidgetId = null;
        _hoveredWidgetIds.Clear();
        Invalidate();
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        var pt = e.Touches[0];

        foreach (var widget in WidgetsTopmostFirst())
        {
            var local = ToLocal(pt, widget);
            if (!InBounds(local, widget.GetSize())) continue;
            if (widget.OnPress(local))
            {
                _pressedWidgetId = widget.Descriptor.WidgetId;
                break;
            }
        }

        Invalidate();
    }
    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        if (_pressedWidgetId != null && DisplayedController.Widgets.TryGetValue(_pressedWidgetId, out var widget))
            widget.OnRelease();
        _pressedWidgetId = null;

        Invalidate();
    }
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(this);
        if (!pos.HasValue) return;
        var canvasPoint = pos.Value;

        var stillHovered = new HashSet<string>();
        foreach (var widget in WidgetsTopmostFirst())
        {
            var local = ToLocal(canvasPoint, widget);
            if (!InBounds(local, widget.GetSize())) continue;

            stillHovered.Add(widget.Descriptor.WidgetId);
            widget.OnPointerMoved(local);
        }

        foreach (var id in _hoveredWidgetIds)
        {
            if (stillHovered.Contains(id)) continue;
            if (DisplayedController.Widgets.TryGetValue(id, out var widget))
                widget.OnPointerExited();
        }

        _hoveredWidgetIds.Clear();
        foreach (var id in stillHovered) _hoveredWidgetIds.Add(id);

        Invalidate();
    }
    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        foreach (var id in _hoveredWidgetIds)
            if (DisplayedController.Widgets.TryGetValue(id, out var widget))
                widget.OnPointerExited();

        _hoveredWidgetIds.Clear();
        Invalidate();
    }
}
