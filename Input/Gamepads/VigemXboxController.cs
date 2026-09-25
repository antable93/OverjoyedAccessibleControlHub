using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace OverjoyedVersion3;

public class VigemXboxController : VirtualInputDevice
{
    private readonly ViGEmClient _client;
    private readonly IXbox360Controller _controller;

    public VigemXboxController()
    {
        VirtualDeviceType = VirtualDeviceType.VigemXboxController; 
        _client = new ViGEmClient();
        _controller = _client.CreateXbox360Controller();
        _controller.Connect();
    }

    protected override Dictionary<string, ActionInput> CreateActionInputs()
    {
        return new List<ActionInput>
        {
            CreateButton("Xbox A",           Xbox360Button.A),
            CreateButton("Xbox B",           Xbox360Button.B),
            CreateButton("Xbox X",           Xbox360Button.X),
            CreateButton("Xbox Y",           Xbox360Button.Y),
            CreateButton("Xbox LB",          Xbox360Button.LeftShoulder),
            CreateButton("Xbox RB",          Xbox360Button.RightShoulder),
            CreateButton("Xbox LS Click",    Xbox360Button.LeftThumb),
            CreateButton("Xbox RS Click",    Xbox360Button.RightThumb),
            CreateStickButton("Xbox LS Up",    Xbox360Axis.LeftThumbY,  1f),
            CreateStickButton("Xbox LS Down",  Xbox360Axis.LeftThumbY,  -1f),
            CreateStickButton("Xbox LS Left",  Xbox360Axis.LeftThumbX,  -1f),
            CreateStickButton("Xbox LS Right", Xbox360Axis.LeftThumbX,  1f),
            CreateStickButton("Xbox RS Up",    Xbox360Axis.RightThumbY, 1f),
            CreateStickButton("Xbox RS Down",  Xbox360Axis.RightThumbY, -1f),
            CreateStickButton("Xbox RS Left",  Xbox360Axis.RightThumbX, -1f),
            CreateStickButton("Xbox RS Right", Xbox360Axis.RightThumbX, 1f),
            CreateButton("Xbox D-Pad Up",    Xbox360Button.Up),
            CreateButton("Xbox D-Pad Down",  Xbox360Button.Down),
            CreateButton("Xbox D-Pad Left",  Xbox360Button.Left),
            CreateButton("Xbox D-Pad Right", Xbox360Button.Right),
            CreateButton("Xbox Start",       Xbox360Button.Start),
            CreateButton("Xbox Back",        Xbox360Button.Back),
            CreateButton("Xbox Guide",       Xbox360Button.Guide),
            CreateTrigger("Xbox LT",         Xbox360Slider.LeftTrigger),
            CreateTrigger("Xbox RT",         Xbox360Slider.RightTrigger),
        }.ToDictionary(a => a.InputId);
    }
    protected override Dictionary<string, AxisInput> CreateAxisInputs()
    {
        return new List<AxisInput>
        {
            CreateAxis1D("Xbox Left Stick Left",   Xbox360Axis.LeftThumbX,  -1f),
            CreateAxis1D("Xbox Left Stick Right",  Xbox360Axis.LeftThumbX,  1f),
            CreateAxis1D("Xbox Left Stick Up",     Xbox360Axis.LeftThumbY,  1f),
            CreateAxis1D("Xbox Left Stick Down",   Xbox360Axis.LeftThumbY,  -1f),
            CreateAxis1D("Xbox Right Stick Left",  Xbox360Axis.RightThumbX, -1f),
            CreateAxis1D("Xbox Right Stick Right", Xbox360Axis.RightThumbX, 1f),
            CreateAxis1D("Xbox Right Stick Up",    Xbox360Axis.RightThumbY, 1f),
            CreateAxis1D("Xbox Right Stick Down",  Xbox360Axis.RightThumbY, -1f),
            CreateAxis2D("Xbox Left Stick",        Xbox360Axis.LeftThumbX,  Xbox360Axis.LeftThumbY),
            CreateAxis2D("Xbox Right Stick",       Xbox360Axis.RightThumbX, Xbox360Axis.RightThumbY),
        }.ToDictionary(a => a.InputId);
    }

    private ActionInput CreateButton(string inputId, Xbox360Button button)
    {
        return new ActionInput(
            inputId,
            down: () => {
                _controller.SetButtonState(button, true);
                _controller.SubmitReport();
            },
            up: () => {
                _controller.SetButtonState(button, false);
                _controller.SubmitReport();
            }
        );
    }
    private ActionInput CreateTrigger(string inputId, Xbox360Slider slider)
    {
        return new ActionInput(
            inputId,
            down: () => {
                _controller.SetSliderValue(slider, 255);
                _controller.SubmitReport();
            },
            up: () => {
                _controller.SetSliderValue(slider, 0);
                _controller.SubmitReport();
            }
        );
    }
    // Digital press of an analog stick direction, letting a click/hover binding drive a single axis like a button.
    private ActionInput CreateStickButton(string inputId, Xbox360Axis axis, float direction)
    {
        return new ActionInput(
            inputId,
            down: () => {
                _controller.SetAxisValue(axis, (short)(direction * short.MaxValue));
                _controller.SubmitReport();
            },
            up: () => {
                _controller.SetAxisValue(axis, 0);
                _controller.SubmitReport();
            }
        );
    }
    // Binds a normalized float value to a single axis, scaled by direction (+1 or -1).
    private AxisInput CreateAxis1D(string inputId, Xbox360Axis axis, float direction)
    {
        return new AxisInput(
            inputId,
            move1D: (value) => {
                _controller.SetAxisValue(axis, (short)(value * direction * short.MaxValue));
                _controller.SubmitReport();
            }
        );
    }
    // Binds a normalized (x, y) position to two axes simultaneously.
    private AxisInput CreateAxis2D(string inputId, Xbox360Axis axisX, Xbox360Axis axisY)
    {
        return new AxisInput(
            inputId,
            move2D: (x, y) =>{
                _controller.SetAxisValue(axisX, (short)(x * short.MaxValue));
                _controller.SetAxisValue(axisY, (short)(y * short.MaxValue));
                _controller.SubmitReport();
            }
        );
    }
}
