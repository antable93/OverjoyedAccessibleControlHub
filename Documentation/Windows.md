# Windows Platform

This document covers Windows-specific code in the project.

---

## OnWindowCreated (`MauiProgram.cs`)

Located in the `#if WINDOWS` block inside `MauiProgram.CreateMauiApp()`, the `OnWindowCreated` lifecycle hook runs immediately after the native Windows window is created.

**What it does:**

When a window is created, it retrieves the native window handle (`HWND`) via `WinRT.Interop.WindowNative.GetWindowHandle`, then reads the window's extended style flags using `NativeMethods.GetWindowLongPtr`. It OR-in the `WS_EX_NOACTIVATE` flag and writes it back with `NativeMethods.SetWindowLongPtr`.

**Why `WS_EX_NOACTIVATE`:**

This extended window style prevents the window from stealing input focus when it is shown or clicked. This is important for overlay-style UI (e.g., on-screen controls or HUD windows) that should remain visible without interrupting the user's active application.

```csharp
windows.OnWindowCreated(window =>
{
    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
    var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
    NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
        (IntPtr)(exStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE));
});
```

---

## Widget Card Dragging (`ControllerEditor/ControllerEditor.xaml.cs`)

Widget cards in the controller editor use raw WinUI pointer events rather than MAUI's built-in `DragGestureRecognizer`. This section explains what the code does and why the platform-specific path is necessary.

### Why not DragGestureRecognizer

MAUI's `DragGestureRecognizer` is designed for inter-app drag-and-drop with a data payload. It does not support custom in-app drag UI such as a floating preview view, and it does not expose pointer capture — meaning the drag can silently drop if the pointer leaves the source element. Raw platform events are needed for both.

### How it works

The pointer event setup runs inside `card.HandlerChanged`, which fires once the MAUI element has a live native platform view. From there, four events are registered on the native `Microsoft.UI.Xaml.UIElement`:

| Event | Role |
|---|---|
| `PointerPressed` | Captures the pointer to the card, resolves `nativeRoot`, and calls `WidgetCardBeginDrag`. |
| `PointerMoved` | Calls `WidgetCardMoveDrag` with the current position while dragging. |
| `PointerReleased` | Releases pointer capture and calls `WidgetCardEndDrag`. |
| `PointerCaptureLost` | Same as `PointerReleased` — handles OS-level cancellation (e.g., Alt+Tab, window losing focus). |

**Pointer capture** (`CapturePointer` / `ReleasePointerCapture`) is what ensures `PointerMoved` and `PointerReleased` keep firing even after the pointer leaves the card's bounds. Without it, moving the pointer quickly off the card would silently end the drag.

**Coordinate conversion** — all positions are reported via `e.GetCurrentPoint(nativeRoot).Position`, where `nativeRoot` is the platform view of the MAUI page root (`_dragRoot`). This converts from the card's local coordinate space into page space, which is the same space used by `DragOverlay` and `_scalingWrapper` for hit-testing.

`nativeRoot` is resolved lazily on first press (via `??=`) rather than in `HandlerChanged`, because `_dragRoot` may not have a platform view attached yet at the time the card's handler is set.

### Mac equivalent

On Mac Catalyst the equivalent is a `UIPanGestureRecognizer` added to the card's `UIView`. On native macOS it is an `NSPanGestureRecognizer`. Neither requires explicit pointer capture — the gesture recognizer handles that automatically. See [ControllerEditor.md — Widget Drawer](ControllerEditor.md) for context on how the drag flow is structured.

---

## NativeMethods (`Platforms/Windows/NativeMethods.cs`)

A thin P/Invoke wrapper around the Win32 `user32.dll` functions needed to read and write window style flags.

| Member | Kind | Description |
|---|---|---|
| `GWL_EXSTYLE` | `const int = -20` | Index for `GetWindowLongPtr`/`SetWindowLongPtr` that targets the extended window style flags. |
| `WS_EX_NOACTIVATE` | `const int = 0x08000000` | Extended style flag that prevents the window from being activated (gaining focus) on show or click. |
| `GetWindowLongPtr(hWnd, nIndex)` | P/Invoke | Returns the current value of the specified window attribute. Used here to read `GWL_EXSTYLE`. |
| `SetWindowLongPtr(hWnd, nIndex, dwNewLong)` | P/Invoke | Writes a new value for the specified window attribute. Used here to apply the modified `GWL_EXSTYLE`. |

Both P/Invoke signatures are declared `internal` and `static`, and the class is `internal static` — they are not exposed outside the `OverjoyedVersion3.WinUI` namespace.
