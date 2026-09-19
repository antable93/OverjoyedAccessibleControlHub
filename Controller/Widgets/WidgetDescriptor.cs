using System.Text.Json.Serialization;

namespace OverjoyedVersion3;

public enum WidgetType { Dial, Joystick }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "WidgetType")]
[JsonDerivedType(typeof(DialWidgetDescriptor), nameof(WidgetType.Dial))]
[JsonDerivedType(typeof(JoystickWidgetDescriptor), nameof(WidgetType.Joystick))]
public abstract class WidgetDescriptor
{
    [JsonIgnore] 
    public WidgetType WidgetType { get; protected set; }
    public string WidgetId { get; set; } = string.Empty;
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public float Scale { get; set; } = 1f;
    public float Rotation { get; set; } = 0f;
    public string HighlightColor { get; set; } = "#87CEFA";
    public string StrokeColor { get; set; } = "#FFFFFF";
}

public class DialWidgetDescriptor : WidgetDescriptor
{
    public DialWidgetDescriptor() { WidgetType = WidgetType.Dial; }
    public int QuadrantCount { get; set; } = 8;
    public float DeadZoneRadius { get; set; } = 70f;
    public float DialRadius { get; set; } = 200f;
}

public enum JoystickGrabMode { Hover, Click, Hold }
public enum JoystickReturnMode { Center, Stay }

public class JoystickWidgetDescriptor : WidgetDescriptor
{
    public JoystickWidgetDescriptor() { WidgetType = WidgetType.Joystick; }
    public float IndicatorOffsetX { get; set; } = 0f;
    public float IndicatorOffsetY { get; set; } = 0f;
    public string IndicatorColor { get; set; } = "#FFFFFF";
    public float RingRadius { get; set; } = 150f;
    public float KnobRadius { get; set; } = 50f;
    public float SensitivityX { get; set; } = 100.0f;
    public float SensitivityY { get; set; } = 100.0f;
    public JoystickGrabMode GrabMode { get; set; } = JoystickGrabMode.Hover;
    public JoystickReturnMode ReturnMode { get; set; } = JoystickReturnMode.Center;
    public bool ReleaseOnLeave { get; set; } = true;
    public bool TriggerAfterRelease { get; set; } = false;
}
