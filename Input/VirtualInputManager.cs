namespace OverjoyedVersion3;

public enum VirtualDeviceType
{
    VigemXboxController,
    InputSimulatorKeyboard
}

public class VirtualInputManager
{
    public static VirtualInputManager Instance { get; } = new();

    private readonly List<VirtualInputDevice> _devices = [];

    private VirtualInputManager() {}

    public void AddVirtualInputDevice(VirtualDeviceType type)
    {
        if (_devices.Any(d => d.VirtualDeviceType == type))
        {
            return;
        }
        
        VirtualInputDevice device = type switch
        {
            VirtualDeviceType.VigemXboxController => new VigemXboxController(),
            VirtualDeviceType.InputSimulatorKeyboard => new InputSimulatorKeyboard(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        _devices.Add(device);
    }

    public ActionInput? FindActionInput(string label)
    {
        foreach (var device in _devices)
        {
            if (device.ActionInputs.TryGetValue(label, out var action))
            {
                return action;
            }
        }
        return null;
    }
    public AxisInput? FindAxisInput(string label)
    {
        foreach (var device in _devices)
        {
            if (device.AxisInputs.TryGetValue(label, out var axis))
            {
                return axis;
            }
        }
        return null;
    }
    public IEnumerable<string> GetAllActionInputLabels() => _devices.SelectMany(d => d.ActionInputs.Keys);
    public IEnumerable<string> GetAllAxisInputLabels() => _devices.SelectMany(d => d.AxisInputs.Keys);
    public IEnumerable<string> GetAllInputLabels() => 
        _devices.SelectMany(d => d.ActionInputs.Keys).Concat(_devices.SelectMany(d => d.ActionInputs.Keys));
}
