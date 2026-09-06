# Controller Editor

## Layout

The editor is a three-column page defined in `ControllerEditor.xaml`.

| Column | Contents |
|--------|----------|
| Left | Controller list (scrollable) |
| Middle | Canvas viewport — scaled view of the active controller, widget drawer below |
| Right | Inspector panel — shows the OptionsPanel for the selected widget slot |

The inspector panel (`InspectorPanel`) is a `ContentView` that starts hidden. Its `Content` is swapped at runtime to whichever OptionsPanel is active.

---

## Widget Drawer

The widget drawer (`WidgetDrawer`) is a `FlexLayout` in `ControllerEditor.xaml`, populated entirely from code in `LoadWidgetDrawer()`. Two sets of cards are created at load time: the stationary cards added to `WidgetDrawer`, and a matching set stored in `_dragPreviews` for reuse as drag overlays.

### CreateWidgetCard

`CreateWidgetCard(WidgetType)` is the single factory for all widget card views. It returns a `View` (a `Border` containing a `VerticalStackLayout` with a `Label` and `Image`) and handles:

- Resolving the SVG path from the `WidgetType` and awaiting `Icon.GenerateAsync` to load the image.
- Wiring Windows-specific raw pointer events for drag initiation (see [Windows.md — Widget Card Dragging](Windows.md)).

Both the stationary drawer cards and the drag preview cards are produced by the same function. The previews are never added to any layout at creation time — they sit parentless until a drag begins.

### Drag Flow (creating a widget)

1. **Begin** — `WidgetCardBeginDrag(widgetType, pos)` retrieves the pre-created view from `_dragPreviews`, sets its opacity to 0.85, and adds it to `DragOverlay` (an `InputTransparent` `AbsoluteLayout` that spans all three columns).
2. **Move** — `WidgetCardMoveDrag(pos)` repositions the preview in `DragOverlay`, centered on the pointer.
3. **End** — `WidgetCardEndDrag(pos)` resets opacity to 1.0, removes the preview from `DragOverlay` (leaving it parentless and ready for the next drag), then checks whether the drop landed inside `_scalingWrapper`. If it did, it converts the page-space position to logical controller coordinates using `_scale` and `GetBoundsRelativeTo`, creates the appropriate `WidgetDescriptor`, calls `controller.AddWidget`, and invalidates the canvas (`_controllerView?.Invalidate()`) instead of rebuilding it.

This is unrelated to *repositioning an already-placed widget*, which happens entirely inside `ControllerView` — see below.

---

## Loading a Controller

`LoadController(Controller controller)` is called from `OnAppearing` (on the active controller) and from `OnControllerFileClicked` when the user picks a different controller from the list.

### Steps

1. **Reset the inspector.** `InspectorPanel` is hidden and its content cleared.

2. **Calculate scale.** The logical controller dimensions (`GetLiveControllerWidth/Height`) are scaled down to fit the editor canvas using the actual measured size of `ControllerCanvasScrollView` (`Width`/`Height`), so the controller visual always fills whatever space the canvas panel ends up with. A `SizeChanged` handler on that `ScrollView` re-triggers `LoadController` to rescale when the panel resizes.

3. **Create the scaling wrapper (first load only).** `_controllerVisual` (an `AbsoluteLayout` sized to the logical controller dimensions, with `Scale` applied) and `_scalingWrapper` (a fixed-size wrapper that accounts for the scale transform) are created once and added to `ControllerContainer`.

4. **Create the canvas once, then reuse it.** On the first call, an `EditorControllerView(controller)` is constructed, sized to fill `_controllerVisual`, and added as its single child. On every subsequent call (switching to a different controller), the same instance is kept and retargeted via `_controllerView.DisplayController(controller)` instead of being rebuilt — the canvas is long-lived for the page's entire lifetime, not per-controller.

5. **Wire selection.** `_controllerView.WidgetSelected` is subscribed once, at creation, to `ShowOptionsPanel(widget, slotId)` — a small type-switch (`Dial` → `_dialOptionsPanel`, `Joystick` → `_joystickOptionsPanel`) that updates and shows the right panel. After every load, `_controllerView.RestoreLastSelectedWidget()` fires `WidgetSelected` for whatever widget/slot was last selected, if it still exists on *that* controller (see `EditorControllerView` below), so the inspector reopens to the same slot when switching back to a previously-edited controller.

```
controller.Widgets[id]
    └─ Dial      → _dialOptionsPanel
    └─ Joystick  → _joystickOptionsPanel
```

The editor owns one instance of each OptionsPanel (`_dialOptionsPanel`, `_joystickOptionsPanel`) for its entire lifetime. Multiple widgets of the same type share a single panel; the panel is refreshed on each interaction.

---

## Widget Classes

### Widget

`Widget` is a plain abstract class (not a `View`) that provides the shared state and abstract interface for all widgets:

```csharp
public abstract class Widget : IDrawable
{
    protected Dictionary<string, Icon?> icons = new();
    protected string? selectedSlotId;

    public Controller Controller { get; }
    public WidgetDescriptor Descriptor { get; }
    public string WidgetId => Descriptor.WidgetId;
    public ControllerView? OwningControllerView;

    public abstract Task LoadIconsAsync();
    public abstract Task LoadIconForSlotAsync(string slotId);
    public abstract Size GetSize();
    public abstract void Draw(ICanvas canvas, RectF dirtyRect);
    public abstract string? HitTestSlot(PointF localPoint);

    public virtual bool OnPress(PointF localPoint) => false;
    public virtual void OnRelease() { }
    public virtual void OnPointerMoved(PointF localPoint) { }
    public virtual void OnPointerExited() { }
}
```

There is exactly one concrete class per widget type (`Dial`, `Joystick`) — no more separate "live" and "settings" subclasses. A single `Widget` instance is created per descriptor (`Controller.Widgets[id]`) and reused by every `ControllerView` that ever displays the controller, live or editor.

- **`Draw(canvas, dirtyRect)`** draws in local space — `(0, 0)` is the widget's own top-left corner, regardless of where `ControllerView` positions it. The canvas translates the drawing context before calling this.
- **`HitTestSlot(localPoint)`** is pure geometry with no side effects: given a point in local space, return the slot it belongs to, or `null`. This is shape-aware per type — `Dial` excludes the deadzone and outside-the-ring area; `Joystick` returns `"Axis"` for any point inside its bounding square. It's used by the editor (click/drag-select) and reused internally by `Dial`'s own hover/click handling.
- **`OwningControllerView`** is reassigned to whichever `ControllerView` last drew this widget (every `Draw` call). Widgets call `OwningControllerView?.Invalidate()` after async work (icon loading) completes, since they no longer have an `Invalidate()` of their own.
- **`OnPress`/`OnRelease`/`OnPointerMoved`/`OnPointerExited`** are interaction hooks called only by `LiveControllerView`. `Dial` and `Joystick` each implement the subset they need — see `ControllerView` below for exactly when each is called.

### Dial

Keeps the quadrant geometry, the four-step `Draw` pipeline (slot fill → divider lines → deadzone ring → icons/placeholders), and icon loading exactly as before. `HitTestSlot` is the old `HitTest` (shape-aware: null outside the ring or inside the deadzone). Live interaction (`OnPress`/`OnRelease`/`OnPointerMoved`/`OnPointerExited`) ports the old `LiveDial` click/hover state machine — `_clickHeldSlotId` for click-mode, `selectedSlotId` for hover-mode, both keyed off the per-slot `"InteractionMode"` option. There is no editor-specific code in `Dial` at all — `ControllerView` handles selection generically using `HitTestSlot`.

### Joystick

Keeps `Draw` (ring, knob, icon) and icon loading. `HitTestSlot` is new: it returns `"Axis"` for any point inside the bounding square, used only by the editor (any click anywhere on the joystick selects it). Live grab/drag (`OnPress`/`OnRelease`/`OnPointerMoved`/`OnPointerExited`) ports the old `LiveJoystick` state machine verbatim, including the private `IsOverKnob` knob-precision check (deliberately separate from `HitTestSlot`, since live grabbing needs knob precision while editor selection wants the whole widget clickable) and the `GrabMode`/`ReturnMode`/`ReleaseOnLeave`/`TriggerAfterRelease` behavior.

---

## ControllerView

`Controller/ControllerViews/ControllerView.cs` is an abstract `GraphicsView`/`IDrawable` base shared by `Controller/ControllerViews/LiveControllerView.cs` (used by `MainPage`) and `Controller/ControllerViews/EditorControllerView.cs` (used by `ControllerEditor`). It draws every widget in `Controller.Layout`; each subclass owns its own interaction behavior — `Dial`/`Joystick` know about neither.

Both pages construct their canvas once and keep it for the page's lifetime, retargeting it to a different `Controller` via the abstract `DisplayController(controller)` method instead of constructing a new instance per controller.

### Drawing

`ControllerView.Draw` iterates `Controller.Layout` in order (index 0 = bottom, later entries draw on top). For each descriptor it looks up `Controller.Widgets[id]`, then applies the widget's full transform before calling `widget.Draw(canvas, new RectF(PointF.Zero, widget.GetSize()))`: translate by `(descriptor.X, descriptor.Y)`, `Rotate(descriptor.Rotation, cx, cy)`, then scale uniformly by `(descriptor.Scale, descriptor.Scale)` around the same center (`cx,cy = GetSize()/2`). `Scale` is a multiplier (`1.0` = native size), not a pixel dimension — it's uniform (not independent X/Y) specifically so circular widgets like `Dial`/`Joystick` stay circular instead of stretching into an ellipse. `DrawWidgetOverlays(canvas, descriptor, size)` runs inside the same transform (so the editor's selection rectangle rotates/scales with the widget too) — it's a no-op on the base and `LiveControllerView`, and overridden by `EditorControllerView` to draw a white selection-border rectangle around the selected widget. `Draw` also reassigns `widget.OwningControllerView = this` on every pass, so async icon loads always redraw whichever canvas most recently displayed the widget.

### Hit-testing transform

`ToLocal(canvasPoint, widget)` converts a canvas-space point into the widget's local, untransformed geometry space by applying the exact inverse of the transform above — un-translate, subtract the center, inverse-rotate, divide by `Scale`, then add the center back. This means `HitTestSlot`/`IsOverKnob` keep running in the same space they always have regardless of `Rotation`/`Scale`, and a rotated/resized widget's clickable area matches what's drawn. `InBounds` is unchanged — it still runs on the already-detransformed point against the widget's logical `GetSize()`.

### Loading controllers

`DisplayController(controller)` is abstract on the base — each subclass overrides it directly to update `DisplayedController`, reset whatever in-progress interaction state it owns (e.g. `LiveControllerView` clears `_pressedWidgetId`/the hover set; `EditorControllerView` clears the in-progress press/drag state), and call `Invalidate()` itself. Per-controller UI state that should *survive* this — like the editor's remembered selection — is intentionally not touched; see `EditorControllerView` below.

### Input events

Each subclass wires the same two event sources individual widgets used to wire on their own `GraphicsView`:

- `StartInteraction`/`DragInteraction`/`EndInteraction` (`GraphicsView`'s built-in `TouchEventArgs`) — press, continuous drag, and release.
- A `PointerGestureRecognizer`'s `PointerMoved`/`PointerExited` — hover and hold-free continuous tracking (mouse-only, since hover isn't a touch concept).

### LiveControllerView dispatch

- **Press** (`StartInteraction`): widgets are walked topmost-first (`Controller.Layout` in reverse); for each whose bounds (`GetSize()`) contain the translated point, `OnPress(localPoint)` is called until one returns `true`. That widget becomes `_pressedWidgetId`.
- **Release** (`EndInteraction`): `OnRelease()` is called on `_pressedWidgetId` if set, regardless of where the pointer currently is — this mirrors implicit pointer capture across a held drag (e.g. Click-mode joystick: grab on press, keep dragging with the button up, release on the second click).
- **Move** (`PointerMoved`): broadcast, not exclusive — every widget whose bounds contain the translated point gets `OnPointerMoved(localPoint)` on every move (this is how `Dial` hover-detection and `Joystick` hover-grab-detection work: the widget itself decides via its own state/`HitTestSlot`/`IsOverKnob` whether anything changed). Widgets that were in-bounds on the previous move but aren't anymore get `OnPointerExited()` once.
- **Canvas exit** (`PointerExited`): every currently-hovered widget gets `OnPointerExited()`.

### EditorControllerView dispatch (select and optionally drag-to-reposition)

- **Press** (`StartInteraction`): widgets are walked topmost-first; the first whose `HitTestSlot(localPoint)` returns non-null is recorded (`_pressedWidgetDescriptor`, `_pressSlotId`, the press point, and the descriptor's starting `X`/`Y`). A `Dial`'s deadzone is not grabbable, by design.
- **Drag** (`DragInteraction`): once the pointer has moved more than a small threshold (4px) from the press point, the press is reclassified as a drag, and `descriptor.X`/`Y` are updated live from the accumulated delta — this is plain translation in canvas space, so it's unaffected by the widget's own `Rotation`/`Scale`.
- **Release** (`EndInteraction`): **any interaction always selects** — `_lastSelectedWidgetId`/`_lastSelectedSlotId` are updated and `WidgetSelected` fires regardless of whether the press turned into a drag. If it was a drag, the controller is additionally saved (to persist the new position). There's no separate "clean click vs. drag" distinction for selection purposes — only the threshold check that decides whether the widget actually moved.

### Last-selected widget memory

`EditorControllerView` keeps two plain fields, `_lastSelectedWidgetId`/`_lastSelectedSlotId` — the "what's selected" state that used to live on `Controller` itself. Keeping it on the canvas instead means `Controller` stays pure data:

- `RestoreLastSelectedWidget()` — called by `ControllerEditor.LoadController` right after `DisplayController`; fires `WidgetSelected` for the remembered widget/slot if it still exists on the current controller, so the inspector panel reopens to the same slot.
- `ClearLastSelectedWidget()` — called by `ControllerEditor.DeleteWidgetAsync` before `Controller.RemoveWidget`, so a stale selection pointing at a just-deleted widget isn't restored later.

This is the resolution to "hit detection differs by shape and by context": shape-aware hit-testing (`HitTestSlot`) lives once per widget type and is reused everywhere; *what a hit means* (trigger a binding vs. select/drag) is decided entirely by which `ControllerView` subclass is in use, never by the widget.

---

## OptionsPanels

`DialOptionsPanel` and `JoystickOptionsPanel` are XAML `ContentView`s in `ControllerEditor/OptionPanels/`. Each panel is stateless until `Update` is called.

### Widget References

Each panel holds a typed reference to its concrete widget class:

```csharp
// DialOptionsPanel
private Dial? _widget;
private Controller Controller => _widget!.Controller;
private DialWidgetDescriptor Descriptor => (DialWidgetDescriptor)_widget!.Descriptor;

// JoystickOptionsPanel
private Joystick? _widget;
private Controller Controller => _widget!.Controller;
private JoystickWidgetDescriptor Descriptor => (JoystickWidgetDescriptor)_widget!.Descriptor;
```

Since there's only one `Widget` instance per descriptor (shared by every canvas), there's no second "live" copy to reload or invalidate — every mutation just needs `await _widget.LoadIconForSlotAsync(slotId)` (or `LoadIconsAsync()`) and/or `_editor.InvalidateCanvas()` once.

### Update

```csharp
public void Update(string slotId, Widget widget)
```

Called by `ControllerEditor.ShowPanelFor` every time a slot is selected on the canvas (or restored on reload). The panel casts `widget` to the concrete type and stores it, then reads all data through `_widget.Controller` and `_widget.Descriptor`.
- Clears and rebuilds the binding list for the new slot.
- Refreshes slot-specific controls (interaction mode buttons for dial; sensitivity sliders for joystick).

### Binding

Each binding button in the list, when clicked:

1. Calls `controller.Bind(widgetId, slotId, inputId)` and saves via `ControllerManager.SaveControllerAsync`.
2. Calls `_widget.LoadIconForSlotAsync(slotId)` so the widget redraws the updated icon (this internally calls `OwningControllerView?.Invalidate()`, so no separate invalidate is needed).
3. Updates button highlight colors (active: `#3A4A6A`, inactive: `#2A2A2A`).

The available inputs differ by panel:
- **DialOptionsPanel** — lists all action inputs from `VirtualInputManager.GetAllActionInputLabels()`.
- **JoystickOptionsPanel** — lists axis inputs filtered to those with a `Move2D` component (`GetAllAxisInputLabels()` where `FindAxisInput(id)?.Move2D != null`).

### Slot-specific Options

- **DialOptionsPanel**
  - *Interaction Mode* toggle (Click / Hover) — stored in `slotData.Options["InteractionMode"]` via `controller.SetSlotOptions`.
  - *Display* toggle (Icon / Label) — stored in `slotData.Options["IsUsingIcon"]` as `"true"` or `"false"`. Selecting Icon reveals a `FlexLayout` of 44×44 icon buttons populated from the panel's static `AvailableIcons` list (all keyboard SVGs, Xbox button/trigger SVGs, and a question mark fallback). The selected icon path is written to `slotData.SvgSource` and immediately reloaded via `LoadIconForSlotAsync`. Selecting Label reveals a text `Entry` whose value is written to `slotData.Label`.

- **JoystickOptionsPanel**
  - *Appearance / Icon* — a `FlexLayout` of 44×44 icon buttons from the panel's static `AvailableIcons` list (Xbox stick SVGs). Selecting an icon writes to `slotData.SvgSource` and calls `LoadIconForSlotAsync`. The joystick always displays an icon; there is no label mode.
  - X/Y sensitivity sliders, ring/knob radius sliders, highlight/stroke color entries, grab mode picker, return mode picker, and release/trigger-after-release checkboxes. Numeric values are displayed in real time as labels and written to the descriptor on drag complete, then saved.
