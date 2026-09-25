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
#if WINDOWS
        HandlerChanged += OnHandlerChanged;
#else
        StartInteraction += OnStartInteraction;
        EndInteraction += OnEndInteraction;
#endif

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

#if WINDOWS
    // GraphicsView's TouchEventArgs carries no mouse-button info, so the native pointer events
    // are used directly here to distinguish left/right/middle click.
    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement native) return;

        native.PointerPressed += (s, e) =>
        {
            var point = e.GetCurrentPoint(native);
            var button = point.Properties.IsRightButtonPressed ? MouseButtonKind.Right
                : point.Properties.IsMiddleButtonPressed ? MouseButtonKind.Middle
                : MouseButtonKind.Left;
            HandlePress(new Point(point.Position.X, point.Position.Y), button);
        };
        native.PointerReleased += (s, e) => HandleRelease();
        native.PointerCanceled += (s, e) => HandleRelease();
        native.PointerCaptureLost += (s, e) => HandleRelease();
    }
#else
    private void OnStartInteraction(object? sender, TouchEventArgs e) => HandlePress(e.Touches[0], MouseButtonKind.Left);
    private void OnEndInteraction(object? sender, TouchEventArgs e) => HandleRelease();
#endif

    private void HandlePress(Point pt, MouseButtonKind button)
    {
        foreach (var widget in WidgetsTopmostFirst())
        {
            var local = ToLocal(pt, widget);
            if (!InBounds(local, widget.GetSize())) continue;
            if (widget.OnPress(local, button))
            {
                _pressedWidgetId = widget.Descriptor.WidgetId;
                break;
            }
        }

        Invalidate();
    }
    private void HandleRelease()
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
