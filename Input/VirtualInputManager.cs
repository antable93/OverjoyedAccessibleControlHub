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
    private VirtualDeviceType? _activeDeviceType;

    private VirtualInputManager()
    {
        AddVirtualInputDevice(VirtualDeviceType.InputSimulatorKeyboard);
        AddVirtualInputDevice(VirtualDeviceType.VigemXboxController);
    }

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

    public void SetActiveVirtualInputDevice(VirtualDeviceType type)
    {
        foreach (var device in _devices)
        {
            if (device.VirtualDeviceType == type)
                device.Activate();
            else
                device.Deactivate();
        }

        _activeDeviceType = type;
    }

    public void DeactivateAllVirtualInputDevices()
    {
        foreach (var device in _devices)
            device.Deactivate();
        _activeDeviceType = null;
    }

    public ActionInput? FindActionInput(string label)
    {
        return _devices.FirstOrDefault(device => device.VirtualDeviceType == _activeDeviceType)?
            .ActionInputs.GetValueOrDefault(label);
    }
    public AxisInput? FindAxisInput(string label)
    {
        return _devices.FirstOrDefault(device => device.VirtualDeviceType == _activeDeviceType)?
            .AxisInputs.GetValueOrDefault(label);
    }
    public IEnumerable<string> GetAllActionInputLabels() => _devices.SelectMany(d => d.ActionInputs.Keys);
    public IEnumerable<string> GetAllAxisInputLabels() => _devices.SelectMany(d => d.AxisInputs.Keys);
    public IEnumerable<string> GetAllInputLabels() => 
        _devices.SelectMany(d => d.ActionInputs.Keys).Concat(_devices.SelectMany(d => d.ActionInputs.Keys));

    public IEnumerable<string> GetActionInputLabels(VirtualDeviceType deviceType) =>
        _devices.Where(d => d.VirtualDeviceType == deviceType).SelectMany(d => d.ActionInputs.Keys);
    public IEnumerable<string> GetAxisInputLabels(VirtualDeviceType deviceType) =>
        _devices.Where(d => d.VirtualDeviceType == deviceType).SelectMany(d => d.AxisInputs.Keys);
    public IEnumerable<string> Get2DAxisInputLabels(VirtualDeviceType deviceType) =>
        _devices.Where(d => d.VirtualDeviceType == deviceType)
            .SelectMany(d => d.AxisInputs.Where(input => input.Value.Move2D != null).Select(input => input.Key));
}
