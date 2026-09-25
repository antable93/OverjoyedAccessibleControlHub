using GregsStack.InputSimulatorStandard;
using GregsStack.InputSimulatorStandard.Native;

namespace OverjoyedVersion3;

public class InputSimulatorKeyboard : VirtualInputDevice
{
    private readonly IKeyboardSimulator _kb;

    public InputSimulatorKeyboard()
    {
        VirtualDeviceType = VirtualDeviceType.InputSimulatorKeyboard;
        _kb = new InputSimulator().Keyboard;
    }

    protected override Dictionary<string, ActionInput> CreateActionInputs()
    {
        return new List<ActionInput>
        {
            CreateKey("A", VirtualKeyCode.VK_A),
            CreateKey("B", VirtualKeyCode.VK_B),
            CreateKey("C", VirtualKeyCode.VK_C),
            CreateKey("D", VirtualKeyCode.VK_D),
            CreateKey("E", VirtualKeyCode.VK_E),
            CreateKey("F", VirtualKeyCode.VK_F),
            CreateKey("G", VirtualKeyCode.VK_G),
            CreateKey("H", VirtualKeyCode.VK_H),
            CreateKey("I", VirtualKeyCode.VK_I),
            CreateKey("J", VirtualKeyCode.VK_J),
            CreateKey("K", VirtualKeyCode.VK_K),
            CreateKey("L", VirtualKeyCode.VK_L),
            CreateKey("M", VirtualKeyCode.VK_M),
            CreateKey("N", VirtualKeyCode.VK_N),
            CreateKey("O", VirtualKeyCode.VK_O),
            CreateKey("P", VirtualKeyCode.VK_P),
            CreateKey("Q", VirtualKeyCode.VK_Q),
            CreateKey("R", VirtualKeyCode.VK_R),
            CreateKey("S", VirtualKeyCode.VK_S),
            CreateKey("T", VirtualKeyCode.VK_T),
            CreateKey("U", VirtualKeyCode.VK_U),
            CreateKey("V", VirtualKeyCode.VK_V),
            CreateKey("W", VirtualKeyCode.VK_W),
            CreateKey("X", VirtualKeyCode.VK_X),
            CreateKey("Y", VirtualKeyCode.VK_Y),
            CreateKey("Z", VirtualKeyCode.VK_Z),

            CreateKey("Shift",     VirtualKeyCode.SHIFT),
            CreateKey("Ctrl",      VirtualKeyCode.CONTROL),
            CreateKey("Alt",       VirtualKeyCode.MENU),
            CreateKey("Escape",    VirtualKeyCode.ESCAPE),
            CreateKey("Backspace", VirtualKeyCode.BACK),
            CreateKey("CapsLock",  VirtualKeyCode.CAPITAL),

            CreateKey("Up",    VirtualKeyCode.UP),
            CreateKey("Down",  VirtualKeyCode.DOWN),
            CreateKey("Left",  VirtualKeyCode.LEFT),
            CreateKey("Right", VirtualKeyCode.RIGHT),

            CreateKey("Enter",        VirtualKeyCode.RETURN),
            CreateKey("Tab",          VirtualKeyCode.TAB),
            CreateKey("Space",        VirtualKeyCode.SPACE),
            CreateKey("Delete",       VirtualKeyCode.DELETE),
            CreateKey("Insert",       VirtualKeyCode.INSERT),
            CreateKey("Home",         VirtualKeyCode.HOME),
            CreateKey("End",          VirtualKeyCode.END),
            CreateKey("Page Up",      VirtualKeyCode.PRIOR),
            CreateKey("Page Down",    VirtualKeyCode.NEXT),
            CreateKey("Num Lock",     VirtualKeyCode.NUMLOCK),
            CreateKey("Scroll Lock",  VirtualKeyCode.SCROLL),
            CreateKey("Print Screen", VirtualKeyCode.SNAPSHOT),
            CreateKey("Windows",      VirtualKeyCode.LWIN),

            CreateKey("F1",  VirtualKeyCode.F1),
            CreateKey("F2",  VirtualKeyCode.F2),
            CreateKey("F3",  VirtualKeyCode.F3),
            CreateKey("F4",  VirtualKeyCode.F4),
            CreateKey("F5",  VirtualKeyCode.F5),
            CreateKey("F6",  VirtualKeyCode.F6),
            CreateKey("F7",  VirtualKeyCode.F7),
            CreateKey("F8",  VirtualKeyCode.F8),
            CreateKey("F9",  VirtualKeyCode.F9),
            CreateKey("F10", VirtualKeyCode.F10),
            CreateKey("F11", VirtualKeyCode.F11),
            CreateKey("F12", VirtualKeyCode.F12),
        }.ToDictionary(a => a.InputId);
    }

    private ActionInput CreateKey(string inputId, VirtualKeyCode code)
    {
        return new ActionInput(
            inputId,
            down: () => _kb.KeyDown(code),
            up: () => _kb.KeyUp(code)
        );
    }

    protected override Dictionary<string, AxisInput> CreateAxisInputs()
    {
        return new List<AxisInput>
        {
            CreateDirectionalAxis2D("WASD", VirtualKeyCode.VK_A, VirtualKeyCode.VK_D, VirtualKeyCode.VK_W, VirtualKeyCode.VK_S),
            CreateDirectionalAxis2D("Arrow Keys", VirtualKeyCode.LEFT, VirtualKeyCode.RIGHT, VirtualKeyCode.UP, VirtualKeyCode.DOWN),
        }.ToDictionary(a => a.InputId);
    }

    // Keyboards can't report analog position, so joystick movement is translated into
    // discrete key presses/releases once the stick crosses a deadzone threshold per direction.
    private AxisInput CreateDirectionalAxis2D(string inputId, VirtualKeyCode left, VirtualKeyCode right, VirtualKeyCode up, VirtualKeyCode down)
    {
        bool leftDown = false, rightDown = false, upDown = false, downDown = false;
        const float threshold = 0.5f;

        return new AxisInput(
            inputId,
            move2D: (x, y) =>
            {
                bool wantLeft = x < -threshold;
                bool wantRight = x > threshold;
                bool wantUp = y < -threshold;
                bool wantDown = y > threshold;

                if (wantLeft != leftDown) { if (wantLeft) _kb.KeyDown(left); else _kb.KeyUp(left); leftDown = wantLeft; }
                if (wantRight != rightDown) { if (wantRight) _kb.KeyDown(right); else _kb.KeyUp(right); rightDown = wantRight; }
                if (wantUp != upDown) { if (wantUp) _kb.KeyDown(up); else _kb.KeyUp(up); upDown = wantUp; }
                if (wantDown != downDown) { if (wantDown) _kb.KeyDown(down); else _kb.KeyUp(down); downDown = wantDown; }
            }
        );
    }
}
