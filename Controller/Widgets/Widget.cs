namespace OverjoyedVersion3;

public abstract class Widget : IDrawable
{
    public Controller Controller { get; }
    public WidgetDescriptor Descriptor { get; }
    public string WidgetId => Descriptor.WidgetId;
    public ControllerView? OwningControllerView;
    
    protected Dictionary<string, Icon?> icons = new();

    /// <summary>
    /// The slot currently shown with the highlight-color fill — set by live hover/click handling,
    /// or by EditorControllerView when a slot is selected in the editor.
    /// </summary>
    public string? SelectedSlotId;

    protected Widget(Controller controller, WidgetDescriptor descriptor)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public abstract Task LoadIconsAsync();
    public abstract Task LoadIconForSlotAsync(string slotId);
    public abstract Size GetSize();
    public abstract void Draw(ICanvas canvas, RectF dirtyRect);

    /// <summary>
    /// Pure geometry, no side effects. Returns the slot at the given point in this widget's local
    /// space, or null if the point doesn't belong to any slot. Used by the editor for click/drag
    /// selection, and reused internally by live dial hover/click handling.
    /// </summary>
    public abstract string? GetHitSlotID(PointF localPoint);

    /// <summary>
    /// Called for every widget whose bounds (GetSize()) contain the press point, topmost
    /// first, until one returns true. Returning true means "route the matching release to me,
    /// regardless of where the pointer is by then" (mirrors implicit pointer capture on press).
    /// </summary>
    public virtual bool OnPress(PointF localPoint) => false;
    /// <summary>
    /// Called on whichever widget's OnPress returned true for the matching press.
    /// </summary>
    public virtual void OnRelease() { }
    /// <summary>
    /// Called for every widget whose bounds (GetSize()) contain the pointer, on every move.
    /// </summary>
    public virtual void OnPointerMoved(PointF localPoint) { }
    /// <summary>
    /// Called once when the pointer leaves this widget's bounds (or leaves the canvas).
    /// </summary>
    public virtual void OnPointerExited() { }
}
