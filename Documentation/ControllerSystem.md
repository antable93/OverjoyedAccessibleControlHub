# Controller System

The controller system connects **inputs** to **widgets** (Dial, Joystick, etc.) at runtime. Bindings are stored in `Controller` objects that can be loaded from and saved to JSON files on disk — `Controller` is pure data, with no UI/session state of its own. Input triggering is delegated to `VirtualInputManager` — see `InputSystem.md`. Widgets are drawn and hit-tested by a long-lived `LiveControllerView`/`EditorControllerView` — see `ControllerEditor.md` for the widget/canvas architecture.

---

## Files

| File | Location |
|---|---|
| `Controller.cs` | `Controller/Controller.cs` |
| `ControllerManager.cs` | `Controller/ControllerManager.cs` |
| `ControllerView.cs` | `Controller/ControllerViews/ControllerView.cs` |
| `LiveControllerView.cs` | `Controller/ControllerViews/LiveControllerView.cs` |
| `EditorControllerView.cs` | `Controller/ControllerViews/EditorControllerView.cs` |
| `Widget.cs` | `Controller/Widgets/Widget.cs` |
| `WidgetDescriptor.cs` | `Controller/Widgets/WidgetDescriptor.cs` |

---

## Classes

### `WidgetDescriptor`
Abstract base for serializable widget configuration. Used in `Controller.Layout` to describe which widgets should be created and how.

Uses `[JsonPolymorphic]` with a `"WidgetType"` discriminator so the correct subclass is deserialized.

All descriptors carry `WidgetId`, `X`, `Y`, `Scale`, `Rotation`, `HighlightColor`, and
`StrokeColor` from the base class. `Scale` is a uniform multiplier (`1.0` = native size), not an
absolute pixel dimension, and `Rotation` is in degrees (clockwise-positive, matching
`ICanvas.Rotate`) — both applied by `ControllerView.Draw` around the widget's local center, on top
of whatever size `GetSize()` already computes. See `ControllerEditor.md` for how this transform
(and its inverse, for hit-testing) works.

| Subclass | Discriminator | Extra properties |
|---|---|---|
| `DialWidgetDescriptor` | `"Dial"` | `QuadrantCount`, `DeadZoneRadius`, `DialRadius` |
| `JoystickWidgetDescriptor` | `"Joystick"` | `RingRadius`, `KnobRadius`, `SensitivityX`, `SensitivityY`, `GrabMode`, `ReturnMode`, `ReleaseOnLeave`, `TriggerAfterRelease` |

---

### `SlotData`
Pairs bound input IDs with display data and a dictionary of per-slot options. Stored as the value in `Controller.SlotData`.

| Member | Description |
|---|---|
| `BoundInputIds` | The IDs of the bound `ActionInput`s or `AxisInput`s (e.g. `"W"`, `"LeftStick"`) |
| `SvgSource` | App-package path to the SVG icon for this binding (default: `"Icons/Misc/question.svg"`) |
| `Label` | Text label shown instead of the icon when display mode is set to Label (not all widget types support labels) |
| `Options` | Arbitrary string key/value pairs interpreted by the widget (e.g. `"InteractionMode": "Hover"`) |

#### Per-slot options

Slot-specific display and behavior flags are stored in `slotData.Options` as string key/value pairs. The widget reads these at draw/interaction time and the OptionsPanel writes them when the user makes a change.

For widgets that have **multiple slots** (e.g. Dial), every slot has its own `SlotData` and can independently carry options like `"IsUsingIcon"` or `"InteractionMode"`. Not all widget types expose the same options — for example, only the Dial supports label mode (`"IsUsingIcon": "false"`), since rendering a text label on the joystick knob is not meaningful.

---

### `Controller`
A serializable controller. Contains the widget layout and all slot bindings. `Layout` and `SlotData` use `[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]` so they deserialize correctly despite having `private set`.

Trigger methods resolve the binding for a slot and delegate to `VirtualInputManager.Instance` to fire the input.

| Member | Description |
|---|---|
| `Name` | Display name for this controller (default: `"Default"`) |
| `Layout` | List of `WidgetDescriptor` — defines which widgets to create and their configuration, and their draw order (later entries draw on top) |
| `SlotData` | `Dictionary<string, SlotData>` keyed by `"WidgetId/SlotId"` — the single source of truth for all bindings |
| `Widgets` | `Dictionary<string, Widget>` — one widget instance per descriptor, shared by every canvas that displays this controller, `[JsonIgnore]` |
| `Initialize()` | Creates one `Widget` for each descriptor in `Layout` via `CreateWidget`. Must be called after deserialization |
| `AddWidget(descriptor)` / `RemoveWidget(widgetId)` | Add/remove a descriptor, its `Widget`, and its `SlotData` entries |
| `TriggerBindingDown(widgetId, slotId)` | Fires `ActionInput.Down` for the slot's binding |
| `TriggerBindingUp(widgetId, slotId)` | Fires `ActionInput.Up` for the slot's binding |
| `TriggerBindingMove1D(widgetId, slotId, float)` | Fires `AxisInput.Move1D` for the slot's binding |
| `TriggerBindingMove2D(widgetId, slotId, float, float)` | Fires `AxisInput.Move2D` for the slot's binding |
| `Bind(widgetId, slotId, inputId)` | Toggles an input ID on a slot's `BoundInputIds` |
| `Unbind(widgetId, slotId)` | Removes a slot's `SlotData` entirely |
| `SetSlotOptions(widgetId, slotId, options)` | Replaces a slot's `Options` dictionary |
| `GetLiveControllerWidth()` | Returns the screen width to use for the live controller canvas |
| `GetLiveControllerHeight()` | Returns the screen height fraction to use for the live controller canvas (accounts for Windows taskbar) |

**Example controller JSON:**
```json
{
  "Name": "TestBindings",
  "Layout": [
    {
      "WidgetType": "Dial",
      "WidgetId": "Dial_Main",
      "QuadrantCount": 4,
      "DeadZoneRadius": 70,
      "DialRadius": 200,
      "HighlightColor": "#7DB8F0",
      "StrokeColor": "#FFFFFF"
    }
  ],
  "SlotData": {
    "Dial_Main/Quadrant_0": { "BoundInputIds": ["W"], "SvgSource": "Icons/Keyboard/keyboard_w.svg", "Label": "", "Options": { "InteractionMode": "Hover", "IsUsingIcon": "true" } },
    "Dial_Main/Quadrant_1": { "BoundInputIds": ["A"], "SvgSource": "Icons/Keyboard/keyboard_a.svg", "Label": "", "Options": { "InteractionMode": "Hover", "IsUsingIcon": "true" } },
    "Dial_Main/Quadrant_2": { "BoundInputIds": ["S"], "SvgSource": "Icons/Misc/question.svg",       "Label": "Down", "Options": { "InteractionMode": "Click", "IsUsingIcon": "false" } },
    "Dial_Main/Quadrant_3": { "BoundInputIds": ["D"], "SvgSource": "Icons/Keyboard/keyboard_d.svg", "Label": "", "Options": { "InteractionMode": "Click", "IsUsingIcon": "true" } }
  }
}
```

---

### `ControllerManager`
Singleton that owns all `Controller` objects and controls which one is active.

| Member | Description |
|---|---|
| `ControllerDirectory` | `FileSystem.AppDataDirectory/Controllers` — where controller JSON files are read from and written to |
| `Create()` | Initializes the singleton: deserializes all controllers from `ControllerDirectory`, calls `Initialize()` on each, and sets the first as active |
| `ActiveController` | The currently active controller |
| `Controllers` | Dictionary of all loaded controllers keyed by name |
| `AddController(controller)` | Registers a controller in memory |
| `RemoveController(name)` | Removes a controller from memory |
| `SetControllerAsActive(controller)` | Updates the active controller pointer. The caller is responsible for swapping the UI |
| `SaveControllerAsync(controllerName)` | Serializes the named controller to `{controllerName}.json` in `ControllerDirectory` |
| `SaveControllerAsync(controller, fileName)` | Serializes a controller to a specific file name |

**Initializing:**
```csharp
await ControllerManager.Create();
```

**Switching controllers (e.g. on the main page):**
```csharp
ControllerManager.SetControllerAsActive(controller);
_controllerView.DisplayController(controller); // reuses the existing long-lived LiveControllerView
```

> **Note:** `ControllerManager` only reads files already present in `ControllerDirectory`. There is no API to enumerate app-bundle contents, so bundled controllers (`Resources/Raw`) must be copied to `ControllerDirectory` by name before `Create()` is called, or listed in a manifest.

---

## How It Works

### Startup sequence
1. Register input devices via `VirtualInputManager.Instance.AddInputDevice(...)` before `ControllerManager.Create()`
2. `ControllerManager.Create()` deserializes all `*.json` files in `ControllerDirectory` into `Controller` objects
3. `Initialize()` is called on each controller, which creates one `Widget` per descriptor via `CreateWidget`, storing it in `Widgets`
4. The first controller is set as active
5. Each page that displays a controller owns one long-lived canvas — `MainPage` a `LiveControllerView`, `ControllerEditor` an `EditorControllerView` — created once and reused for every controller it ever displays; widgets read bindings directly from `Controller.SlotData` at interaction time and call `TriggerBinding*` to fire inputs

### Switching controllers
`SetControllerAsActive` only updates the pointer. The UI swap is the caller's responsibility: call `view.DisplayController(controller)` on the page's existing canvas rather than constructing a new one.

### Binding at runtime
```csharp
var controller = ControllerManager.GetActiveController();
controller.Bind("Dial_Main", "Quadrant_0", "W");
```

### Saving
```csharp
await ControllerManager.SaveControllerAsync("TestBindings");
```

---

## Adding a New Widget Type

1. Create a `WidgetDescriptor` subclass with the widget's configuration properties
2. Add a `[JsonDerivedType(typeof(YourDescriptor), "YourType")]` attribute to `WidgetDescriptor`
3. Add an arm to `Controller.CreateWidget`'s pattern match that constructs your `Widget` subclass from the descriptor
4. Implement `Widget`: `GetSize()`, `Draw()`, `HitTestSlot()`, icon loading, and (if it needs live-mode interaction) `OnPress`/`OnRelease`/`OnPointerMoved`/`OnPointerExited`. There is no separate "settings" variant to write — `EditorControllerView` handles editor selection/dragging generically for every widget type. See `ControllerEditor.md` for the full `Widget`/canvas contract.

---

## Initial Creation Flow Diagram

```
ControllerManager          Controller                  Dial / Joystick
        │                       │                            │
LoadAllControllers()            │                            │
 (deserialize JSON)             │                            │
        │                       |                            |
        │── Initialize() ──────►│                            │
        │                       │── new Dial(this, d) ──────►│
        │                       │   Widgets[id] = widget     │
        │                       │                            │
        │── _activeController = first                        │
        │                       │                            │
```

A `LiveControllerView`/`EditorControllerView` is then created (once, long-lived) by whichever page displays the controller; it draws every widget in `controller.Layout` and routes pointer input to them — see `ControllerEditor.md`.

---

## Notes

- `Controller` is the single source of truth for all bindings, and holds no UI/session state of its own — things like which widget is selected in the editor live on `EditorControllerView` instead, keyed by `Controller`, so multiple controllers can each remember their own selection across a single canvas's lifetime
- Widgets hold a back-reference to their `Controller` and look up `Controller.SlotData` directly — there is no secondary copy of bindings inside widgets
- Each descriptor has exactly **one** `Widget` instance in `Controller.Widgets`, shared by every canvas that displays the controller (live or editor) — there is no separate live/settings copy to keep in sync
- `Controller` stores input IDs (strings) and string option maps — controllers are fully portable across device implementations
- `Layout` and `SlotData` use `[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]` because they have `private set`; the deserializer populates the existing collection instances in-place
- Input devices must be registered with `VirtualInputManager` before `TriggerBinding*` is called — if an input ID in a saved controller has no matching input in any registered device, that binding is silently skipped
