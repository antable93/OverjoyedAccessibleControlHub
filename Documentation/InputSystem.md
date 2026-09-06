# Input System

The input system provides a provider-agnostic abstraction for simulating any kind of input device — keyboard, mouse, gamepad, or otherwise. Each device is a `VirtualInputDevice` subclass that exposes dictionaries of `ActionInput` and `AxisInput` objects keyed by label. Active devices are registered through `VirtualInputManager`, the singleton that owns and queries all devices.

---

## Files

| File | Location |
|---|---|
| `VirtualInputManager.cs` | `Input/VirtualInputManager.cs` |
| `VirtualInputDevice.cs` | `Input/VirtualInputDevice.cs` |
| `ActionInput.cs` | `Input/ActionInput.cs` |
| `AxisInput.cs` | `Input/AxisInput.cs` |
| `InputSimulatorKeyboard.cs` | `Input/Keyboards/InputSimulatorKeyboard.cs` |
| `VigemXboxController.cs` | `Input/Gamepads/VigemXboxController.cs` |

---

## Classes

### `ActionInput`

Represents a single bindable discrete input — a keyboard key, mouse button, gamepad button, or anything else. Holds an identifier and two delegate slots for the input's lifecycle.

| Member | Description |
|---|---|
| `InputId` | Unique identifier used as the binding key (e.g. `"A"`, `"Shift"`, `"F5"`) |
| `Down` | Holds the input down without releasing |
| `Up` | Releases a previously held input |

Either delegate can be null. A device that has no meaningful concept of hold (e.g. a one-shot event) can provide only one.

---

### `AxisInput`

Represents a single bindable continuous input — a joystick axis, trigger, scroll wheel, or anything else. Holds an identifier and delegate slots for 1D and 2D movement.

| Member | Description |
|---|---|
| `InputId` | Unique identifier used as the binding key (e.g. `"LeftStick"`, `"RightTrigger"`) |
| `Move1D` | `Action<float>` — fires with a single-axis value |
| `Move2D` | `Action<float, float>` — fires with an x and y value |

Either delegate can be null. A joystick can provide both; a trigger only provides `Move1D`.

---

### `VirtualInputDevice`

Abstract base class for all virtual input devices.

| Member | Description |
|---|---|
| `VirtualDeviceType` | The `VirtualDeviceType` enum value identifying this device's type. Set by the subclass constructor |
| `ActionInputs` | `IReadOnlyDictionary<string, ActionInput>` — all discrete inputs this device provides, keyed by `Label` |
| `AxisInputs` | `IReadOnlyDictionary<string, AxisInput>` — all continuous inputs this device provides, keyed by `Label` |
| `CreateActionInputs()` | Overridden by subclasses — returns the action input dictionary wired to the underlying provider |
| `CreateAxisInputs()` | Overridden by subclasses — returns the axis input dictionary wired to the underlying provider |

Both dictionaries are populated in the base constructor by calling the respective `Create*` methods.

---

### `VirtualDeviceType`

Enum identifying the available device implementations. Used by `VirtualInputManager.AddVirtualInputDevice` and stored on each `VirtualInputDevice` instance.

| Value | Device |
|---|---|
| `VigemXboxController` | Virtual Xbox 360 controller via ViGEm |
| `InputSimulatorKeyboard` | Keyboard via InputSimulator |

---

### `VirtualInputManager`

Singleton that owns all active `VirtualInputDevice` instances. Provides device registration, input lookup, and input triggering. The device list starts empty — call `AddVirtualInputDevice` to register devices at the appropriate point in startup.

```csharp
VirtualInputManager.Instance.AddVirtualInputDevice(VirtualDeviceType.InputSimulatorKeyboard);
VirtualInputManager.Instance.AddVirtualInputDevice(VirtualDeviceType.VigemXboxController);
```

| Member | Description |
|---|---|
| `Instance` | The singleton instance |
| `Devices` | `IReadOnlyList<VirtualInputDevice>` — all registered devices |
| `AddVirtualInputDevice(type)` | Instantiates and registers the device for the given type. No-op if a device of that type is already registered |
| `FindActionInput(label)` | Returns the first `ActionInput` with the given label across all devices, or `null` |
| `FindAxisInput(label)` | Returns the first `AxisInput` with the given label across all devices, or `null` |
| `GetAllActionInputLabels()` | Returns all action input labels across all devices |
| `GetAllAxisInputLabels()` | Returns all axis input labels across all devices |
| `GetAllInputLabels()` | Returns all action and axis input labels combined |

---

## Implementations

### `InputSimulatorKeyboard`

A keyboard implementation backed by `GregsStack.InputSimulatorStandard`. Windows only.

Sets `VirtualDeviceType = VirtualDeviceType.InputSimulatorKeyboard`.

Provides `ActionInputs` for:

| Category | Keys |
|---|---|
| Letters | A – Z |
| Modifiers | Shift, Ctrl, Alt, Escape, Backspace, CapsLock |
| Arrows | Up, Down, Left, Right |
| System | Enter, Tab, Space, Delete, Insert, Home, End, Page Up, Page Down, Num Lock, Scroll Lock, Print Screen, Windows |
| Function | F1 – F12 |

Each action is built with a `CreateKey` helper that wires `Down` and `Up` to the library's key codes:

```csharp
CreateKey("A", VirtualKeyCode.VK_A)
// Down → _kb.KeyDown(VirtualKeyCode.VK_A)
// Up   → _kb.KeyUp(VirtualKeyCode.VK_A)
```

Key combinations are composed by holding modifiers across separate calls:

```csharp
shiftAction.Down();
aAction.Down();
aAction.Up();
shiftAction.Up();
// Result: types "A" (uppercase)
```

---

### `VigemXboxController`

A virtual Xbox 360 gamepad backed by `Nefarius.ViGEm.Client`. Windows only.

Sets `VirtualDeviceType = VirtualDeviceType.VigemXboxController`.

**ActionInputs** — buttons and triggers treated as discrete inputs:

| Category | Labels |
|---|---|
| Face | A, B, X, Y |
| Shoulder | LB, RB |
| Sticks (click) | LS, RS |
| D-Pad | Up, Down, Left, Right |
| System | Start, Back, Guide |
| Triggers | LT, RT |

**AxisInputs** — analog axes:

| Category | Labels |
|---|---|
| Left Stick | LeftStick Left, LeftStick Right, LeftStick Up, LeftStick Down, LeftStick |
| Right Stick | RightStick Left, RightStick Right, RightStick Up, RightStick Down, RightStick |

The combined `LeftStick` and `RightStick` labels provide `Move2D`. The directional variants provide `Move1D`.

---

## Adding a New Device Type

1. Create a class that extends `VirtualInputDevice`
2. Add a new value to the `VirtualDeviceType` enum
3. Set `VirtualDeviceType` in the subclass constructor
4. Override `CreateActionInputs()` and/or `CreateAxisInputs()` and return dictionaries wired to your input library
5. Add a `switch` arm for the new type in `VirtualInputManager.AddVirtualInputDevice`

**Example skeleton for a virtual mouse:**

```csharp
public class InputSimulatorMouse : VirtualInputDevice
{
    public InputSimulatorMouse()
    {
        VirtualDeviceType = VirtualDeviceType.InputSimulatorMouse; // new enum value
    }

    protected override Dictionary<string, ActionInput> CreateActionInputs() =>
        new List<ActionInput>
        {
            new("LeftClick",  down: ..., up: ...),
            new("RightClick", down: ..., up: ...),
            new("ScrollUp",   down: ..., up: null),
            new("ScrollDown", down: ..., up: null),
        }.ToDictionary(a => a.InputId);
}
```

---

## Notes

- `InputId` must be unique within a device instance — it is the key `Controller.Bindings` uses to persist and resolve bindings
- Swapping to a different device implementation requires no changes to bindings or views — input IDs are the only contract
- `ActionInput` and `AxisInput` do not hold icons or SVG paths — icon source paths live in `Binding.SvgSource` and are loaded by views at their own display size (see `IconSystem.md`)
- `AddVirtualInputDevice` is a no-op if the device type is already registered — each type can only appear once
