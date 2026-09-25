using System.Text.Json.Serialization;

namespace OverjoyedVersion3;

public class SlotData
{
    public string SvgSource { get; set; } = "question";
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, string> Options { get; set; } = new();
    public List<string> BoundInputIds { get; set; } = new();
    public List<string> ClickBoundInputIds { get; set; } = new();
    public List<string> RightClickBoundInputIds { get; set; } = new();
    public List<string> MiddleClickBoundInputIds { get; set; } = new();
    public List<string> HoverBoundInputIds { get; set; } = new();
}

/// <summary>
/// Contains the layout and bindings for a custom digital controller.
/// </summary>
public class Controller
{
    public string Name { get; set; } = "Default";
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public List<WidgetDescriptor> Layout { get; private set; } = new();
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, SlotData> SlotData { get; private set; } = new();

    [JsonIgnore]
    public Dictionary<string, Widget> Widgets { get; private set; } = new();

    public Controller() { }

    public void Initialize()
    {
        foreach (var widgetDescriptor in Layout)
            CreateWidget(widgetDescriptor);
    }
    public void AddWidget(WidgetDescriptor descriptor)
    {
        Layout.Add(descriptor);
        CreateWidget(descriptor);
    }
    public bool RemoveWidget(string widgetId)
    {
        var descriptor = Layout.FirstOrDefault(d => d.WidgetId == widgetId);
        if (descriptor == null) return false;

        Layout.Remove(descriptor);
        Widgets.Remove(widgetId);

        foreach (var key in SlotData.Keys.Where(k => k.StartsWith(widgetId + "/")).ToList())
            SlotData.Remove(key);

        return true;
    }

    public void TriggerBindingDown(string widgetId, string slotId, string bindingMode = "LeftClick")
    {
        if (SlotData.TryGetValue($"{widgetId}/{slotId}", out var slot))
            foreach (var id in GetBoundInputIds(slot, bindingMode))
                VirtualInputManager.Instance.FindActionInput(id)?.Down?.Invoke();
    }
    public void TriggerBindingUp(string widgetId, string slotId, string bindingMode = "LeftClick")
    {
        if (SlotData.TryGetValue($"{widgetId}/{slotId}", out var slot))
            foreach (var id in GetBoundInputIds(slot, bindingMode))
                VirtualInputManager.Instance.FindActionInput(id)?.Up?.Invoke();
    }
    public void TriggerBindingMove1D(string widgetId, string slotId, float axisValue)
    {
        if (SlotData.TryGetValue($"{widgetId}/{slotId}", out var slot))
            foreach (var id in slot.BoundInputIds)
                VirtualInputManager.Instance.FindAxisInput(id)?.Move1D?.Invoke(axisValue);
    }
    public void TriggerBindingMove2D(string widgetId, string slotId, float xAxisValue, float yAxisValue)
    {
        if (SlotData.TryGetValue($"{widgetId}/{slotId}", out var slot))
            foreach (var id in slot.BoundInputIds)
                VirtualInputManager.Instance.FindAxisInput(id)?.Move2D?.Invoke(xAxisValue, yAxisValue);
    }

    /// <summary>
    /// Toggles an input ID on the specified slot: adds if absent, removes if present.
    /// </summary>
    public async Task Bind(string widgetId, string slotId, string inputId, string bindingMode = "LeftClick")
    {
        var slot = GetOrCreateSlotData(widgetId, slotId);
        var bindings = GetModeBindings(slot, bindingMode);
        if (bindingMode == "LeftClick" && bindings.Count == 0 && slot.BoundInputIds.Count > 0)
        {
            bindings.AddRange(slot.BoundInputIds);
            slot.BoundInputIds.Clear();
        }
        if (bindings.Contains(inputId))
            bindings.Remove(inputId);
        else
            bindings.Add(inputId);
        await ControllerManager.SaveControllerAsync(Name);
    }
    public void Unbind(string widgetId, string slotId) =>
        SlotData.Remove(CreateSlotKey(widgetId, slotId));
    public void SetSlotOptions(string widgetId, string slotId, Dictionary<string, string> options) =>
        GetOrCreateSlotData(widgetId, slotId).Options = options;
    public SlotData GetOrCreateSlotData(string widgetId, string slotId)
    {
        var key = CreateSlotKey(widgetId, slotId);
        if (!SlotData.TryGetValue(key, out var data))
        {
            SlotData[key] = data = new SlotData();
        }
        return data;
    }

    private static List<string> GetModeBindings(SlotData slot, string bindingMode) => bindingMode switch
    {
        "Hover" => slot.HoverBoundInputIds,
        "RightClick" => slot.RightClickBoundInputIds,
        "MiddleClick" => slot.MiddleClickBoundInputIds,
        _ => slot.ClickBoundInputIds,
    };

    private static List<string> GetBoundInputIds(SlotData slot, string bindingMode)
    {
        var bindings = GetModeBindings(slot, bindingMode);
        return bindings.Count > 0 || bindingMode != "LeftClick" ? bindings : slot.BoundInputIds;
    }

    public static double GetLiveControllerWidth()
    {
        var info = DeviceDisplay.Current.MainDisplayInfo;
        return info.Width / info.Density;
    }
    public static double GetLiveControllerHeight()
    {
        var info = DeviceDisplay.Current.MainDisplayInfo;
        double screenH = info.Height / info.Density;
#if WINDOWS
        screenH -= 48.0;
#endif
        return screenH * (10.0 / 11.0);
    }
    private void CreateWidget(WidgetDescriptor descriptor)
    {
        Widgets[descriptor.WidgetId] = descriptor switch
        {
            DialWidgetDescriptor d => new Dial(this, d),
            JoystickWidgetDescriptor j => new Joystick(this, j),
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor))
        };
    }
    private static string CreateSlotKey(string widgetId, string slotId) => $"{widgetId}/{slotId}";
}
