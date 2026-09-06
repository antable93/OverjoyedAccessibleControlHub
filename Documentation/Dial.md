# Dial

A widget that renders a circular dial divided into interactive pie-slice regions ("quadrants"). `Dial` is a plain `Widget` (see `ControllerEditor.md`) — it owns its own drawing and hit-testing, and is drawn by whichever `ControllerView` (live or editor) currently displays its controller. There is a single `Dial` instance per descriptor, shared by every canvas.

---

## Files

| File | Location |
|---|---|
| `Dial.cs` | `Controller/Widgets/Dial/Dial.cs` |
| `DialQuadrant.cs` | `Controller/Widgets/Dial/DialQuadrant.cs` |

---

## `Dial`

**State** (fields inherited from `Widget` unless noted)

| Field | Type | Description |
|---|---|---|
| `Controller` | `Controller` | Back-reference used for bindings and icon loading |
| `Descriptor` | `WidgetDescriptor` | Configuration source, accessed as `DialDescriptor` |
| `icons` | `Dictionary<string, Icon?>` | Rasterized icons keyed by slot ID |
| `selectedSlotId` | `string?` | The currently highlighted slot. Set by live hover/click handling, or by `ControllerView` when selected in the editor |
| `_quadrants` | `List<DialQuadrant>` | Pre-computed quadrant list built from `QuadrantCount` |
| `_clickHeldSlotId` | `string?` | The slot held down via a Click-mode press, live mode only |

**Configuration (via `DialWidgetDescriptor`)**

| Property | Type | Default | Description |
|---|---|---|---|
| `QuadrantCount` | `int` | `4` | Number of pie slice segments |
| `StrokeColor` | `string` (hex) | `"#FFFFFF"` | Color of structural outlines and resting divider lines |
| `HighlightColor` | `string` (hex) | `"#7DB8F0"` | Color applied to the highlighted slot and its bounding divider lines |
| `DeadZoneRadius` | `float` | `70` | Radius of the center deadzone ring in device-independent pixels. Touches within this radius do nothing |
| `DialRadius` | `float` | `200` | Outer radius of the dial in device-independent pixels |

**Drawing pipeline**

`Draw(canvas, dirtyRect)` draws in local space (the canvas has already translated to this widget's position). Each frame it reads `selectedSlotId` directly to determine the current highlight (no virtual indirection — `ControllerView` is responsible for setting/clearing it appropriately for editor selection; live hover/click set it themselves). Steps in order:

1. **Slot fill** — if `selectedSlotId` is set, fills the slot shape with `HighlightColor` at 25% opacity
2. **Radial divider lines** — draws one line per quadrant boundary; the two lines bounding the highlighted quadrant are drawn in `HighlightColor`
3. **Structural outlines** — center deadzone ring
4. **Icons and placeholders** — draws each slot's icon if loaded; draws a faint `"?"` for unbound slots

**Geometry**

| Method | Description |
|---|---|
| `CreateQuadrants(count)` | Returns a list of `DialQuadrant` objects with equal sweep angles starting at 0° |
| `HitTestSlot(pt)` | Pure geometry, no side effects. Returns the slot ID at the given local point, or `null` if outside the dial ring or inside the deadzone. Used by `ControllerView` for editor click/drag selection, and internally for live hover/click |
| `GetSlotIconSize(slotId)` | Returns the pixel size an icon should be rasterized at for the given slot |
| `GetSize()` | Returns the dial's bounding square (`DialRadius * 2 + 4` on each side) |

**Slot IDs**

| Slot ID | Region |
|---|---|
| `Quadrant_0` … `Quadrant_N` | Pie slice segments (count from `DialWidgetDescriptor.QuadrantCount`) |

---

### Live interaction

Interaction mode is a per-slot option stored in `Controller.SlotData` under the key `"InteractionMode"`.

| Mode | Behavior |
|---|---|
| `Click` (default) | `Down` fires on `OnPress` if the pressed slot is Click-mode (captures `_clickHeldSlotId`); `Up` fires on the matching `OnRelease` |
| `Hover` | `Down` fires when `OnPointerMoved` detects entry into a Hover-mode slot; `Up` fires on exit (another slot, no slot, or `OnPointerExited`) |

Only `LiveControllerView` calls these; `EditorControllerView` never does. Editor click/drag-to-reposition is handled entirely by `EditorControllerView` using `HitTestSlot` — `Dial` has no editor-specific code.

---

### `DialQuadrant`

Data container for a single pie slice segment.

| Member | Description |
|---|---|
| `StartAngle` | Angle in degrees where this segment begins (0° = right, clockwise) |
| `SweepAngle` | Angular width of this segment in degrees |

---

## Layout

```
┌─────────────────────────────┐
│        Quadrant slices      │
│   (transparent, divided by  │
│      StrokeColor lines)     │
│                             │
│         ┌────────┐          │
│         │Deadzone│          │
│         │  ring  │          │
│         └────────┘          │
│      (non-interactive)      │
└─────────────────────────────┘
```

Touches within the deadzone ring (`DeadZoneRadius`) produce no hit and trigger no binding.

---

## Controller Integration

`Dial` holds a back-reference to its `Controller` and reads from `Controller.SlotData` directly. Live interaction calls `Controller.TriggerBindingDown`/`TriggerBindingUp` when a slot activates/deactivates. Per-slot options (e.g. `InteractionMode`) are read from `Controller.SlotData[key].Options` at interaction time, so changes via `Controller.SetSlotOptions` are reflected immediately.

---

## Icon Loading

`Dial` owns its own `Dictionary<string, Icon?>` (inherited from `Widget`) keyed by slot ID. Because there's only one `Dial` instance per descriptor, there's a single icon cache shared by the live page and the editor — no separate copy to keep in sync.

Icons are loaded by iterating `Controller.SlotData`, filtering to entries whose key starts with `"{WidgetId}/"`, then rasterizing each bound action's SVG at the size `GetSlotIconSize` returns for that slot. All slots are loaded in a single `async` pass ending with `OwningControllerView?.Invalidate()` (the currently-hosting `ControllerView`).

Icons are loaded in two situations:

- **On construction** — the constructor fires `_ = LoadIconsAsync()` after all fields are set
- **On explicit refresh** — `LoadIconForSlotAsync(slotId)` reloads a single slot after a binding change in the settings UI

See `IconSystem.md` for full details on the rasterization pipeline.
