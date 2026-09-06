namespace OverjoyedVersion3;

/// <summary>
/// Provider-agnostic abstraction for virtual input devices.
/// </summary>
public abstract class VirtualInputDevice
{
    public VirtualDeviceType VirtualDeviceType { get; protected set; }
    public IReadOnlyDictionary<string, ActionInput> ActionInputs { get; }
    public IReadOnlyDictionary<string, AxisInput> AxisInputs { get; }

    protected VirtualInputDevice()
    {
        ActionInputs = CreateActionInputs();
        AxisInputs = CreateAxisInputs();
    }

    protected virtual Dictionary<string, ActionInput> CreateActionInputs() => new();
    protected virtual Dictionary<string, AxisInput> CreateAxisInputs() => new();
}
