namespace OverjoyedVersion3;

/// <summary>
/// Routes pointer input to widgets for editing: click-or-drag selects a widget/slot, dragging
/// also repositions it. Remembers the last-selected widget/slot (rather than that state living on
/// Controller itself, which is meant to be pure data) so switching back to the same controller
/// restores the inspector panel.
/// </summary>
public class EditorControllerView : ControllerView
{
    private const float SelectionPadding = 6f;
    private const double DragThreshold = 4.0;

    // Fired whenever a widget is selected, by direct interaction or by RestoreLastSelectedWidget.
    public event Action<Widget, string?>? WidgetSelected;

    private string? _lastSelectedWidgetId;
    private string? _lastSelectedSlotId;

    private WidgetDescriptor? _pressedWidgetDescriptor;
    private string? _pressSlotId;
    private Point _pressCanvasPoint;
    private double _pressStartX;
    private double _pressStartY;
    private bool _dragMoved;

    public EditorControllerView(Controller controller) : base(controller)
    {
        StartInteraction += OnStartInteraction;
        DragInteraction += OnDragInteraction;
        EndInteraction += OnEndInteraction;
    }

    public override void DisplayController(Controller controller)
    {
        DisplayedController = controller ?? throw new ArgumentNullException(nameof(controller));
        _pressedWidgetDescriptor = null;
        _pressSlotId = null;
        _dragMoved = false;
        Invalidate();
    }

    protected override void DrawWidgetOverlays(ICanvas canvas, WidgetDescriptor descriptor, Size size)
    {
        if (descriptor.WidgetId != _lastSelectedWidgetId) return;

        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2f;
        canvas.StrokeDashPattern = [6f, 4f];
        canvas.DrawRoundedRectangle(-SelectionPadding, -SelectionPadding,
            (float)size.Width + SelectionPadding * 2, (float)size.Height + SelectionPadding * 2, 5f);
        canvas.StrokeDashPattern = null;
    }

    /// <summary>
    /// Fires WidgetSelected for whichever widget was last selected, if it still exists on the
    /// current controller.
    /// Called after DisplayController so the inspector panel can reopen to the same slot.
    /// </summary>
    public void RestoreLastSelectedWidget()
    {
        if (_lastSelectedWidgetId != null &&
            DisplayedController.Widgets.TryGetValue(_lastSelectedWidgetId, out var widget))
        {
            widget.SelectedSlotId = _lastSelectedSlotId;
            WidgetSelected?.Invoke(widget, _lastSelectedSlotId);
        }
    }
    /// <summary>
    /// Selects the given widget/slot programmatically (e.g. right after a drag-drop creates a new
    /// widget), mirroring what a direct click would do: updates the selection outline, remembers it
    /// for RestoreLastSelectedWidget, and fires WidgetSelected so the inspector opens.
    /// </summary>
    public void SelectWidget(string widgetId, string? slotId)
    {
        if (!DisplayedController.Widgets.TryGetValue(widgetId, out var widget)) return;

        if (_lastSelectedWidgetId != null && _lastSelectedWidgetId != widgetId &&
            DisplayedController.Widgets.TryGetValue(_lastSelectedWidgetId, out var previousWidget))
        {
            previousWidget.SelectedSlotId = null;
        }

        widget.SelectedSlotId = slotId;
        _lastSelectedWidgetId = widgetId;
        _lastSelectedSlotId = slotId;
        WidgetSelected?.Invoke(widget, slotId);
        Invalidate();
    }
    /// <summary>
    /// Clears the last widget selection. 
    /// Called before removing a widget from the controller.
    /// </summary>
    public void ClearLastSelectedWidget()
    {
        _lastSelectedWidgetId = null;
        _lastSelectedSlotId = null;
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        var pt = e.Touches[0];
        _pressedWidgetDescriptor = null;
        _dragMoved = false;

        foreach (var widget in WidgetsTopmostFirst())
        {
            var local = ToLocal(pt, widget);
            var slot = widget.GetHitSlotID(local);
            if (slot == null) continue;

            _pressedWidgetDescriptor = widget.Descriptor;
            _pressSlotId = slot;
            _pressCanvasPoint = pt;
            _pressStartX = widget.Descriptor.X;
            _pressStartY = widget.Descriptor.Y;
            break;
        }

        Invalidate();
    }
    private void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (_pressedWidgetDescriptor == null) return;

        var pt = e.Touches[0];
        double dx = pt.X - _pressCanvasPoint.X;
        double dy = pt.Y - _pressCanvasPoint.Y;

        if (!_dragMoved && (Math.Abs(dx) > DragThreshold || Math.Abs(dy) > DragThreshold))
            _dragMoved = true;

        if (_dragMoved)
        {
            _pressedWidgetDescriptor.X = _pressStartX + dx;
            _pressedWidgetDescriptor.Y = _pressStartY + dy;
            Invalidate();
        }
    }
    private async void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        if (_pressedWidgetDescriptor != null)
        {
            // Any interaction — click or drag — selects the widget and updates the inspector.
            if (DisplayedController.Widgets.TryGetValue(_pressedWidgetDescriptor.WidgetId, out var widget))
            {
                if (_lastSelectedWidgetId != null && _lastSelectedWidgetId != widget.WidgetId &&
                    DisplayedController.Widgets.TryGetValue(_lastSelectedWidgetId, out var previousWidget))
                {
                    previousWidget.SelectedSlotId = null;
                }

                widget.SelectedSlotId = _pressSlotId;
                _lastSelectedWidgetId = _pressedWidgetDescriptor.WidgetId;
                _lastSelectedSlotId = _pressSlotId;
                WidgetSelected?.Invoke(widget, _pressSlotId);
            }

            if (_dragMoved)
            {
                await ControllerManager.SaveControllerAsync(DisplayedController.Name);
            }

            _pressedWidgetDescriptor = null;
            _pressSlotId = null;
            _dragMoved = false;
        }

        Invalidate();
    }
}
