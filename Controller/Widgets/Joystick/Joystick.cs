namespace OverjoyedVersion3;

public class Joystick : Widget
{
    private PointF _knobOffset = PointF.Zero;
    private bool _suppressHoverUntilRelease = false;

    private JoystickWidgetDescriptor JoystickDescriptor => (JoystickWidgetDescriptor)Descriptor;

    public Joystick(Controller controller, JoystickWidgetDescriptor descriptor) : base(controller, descriptor)
    {
        VirtualInputManager.Instance.AddVirtualInputDevice(VirtualDeviceType.InputSimulatorKeyboard);
        VirtualInputManager.Instance.AddVirtualInputDevice(VirtualDeviceType.VigemXboxController);

        _ = LoadIconsAsync();
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float cx = dirtyRect.Width / 2f;
        float cy = dirtyRect.Height / 2f;
        float ringR = JoystickDescriptor.RingRadius;
        float knobR = JoystickDescriptor.KnobRadius;
        float knobX = cx + _knobOffset.X;
        float knobY = cy + _knobOffset.Y;
        string? highlighted = SelectedSlotId;

        Color highlightColor = Color.FromArgb(JoystickDescriptor.HighlightColor);
        Color strokeColor = Color.FromArgb(JoystickDescriptor.StrokeColor);

        // Ring background tint when active
        if (highlighted != null)
        {
            canvas.FillColor = highlightColor.WithAlpha(0.25f);
            canvas.FillCircle(cx, cy, ringR);
        }

        // Ring outline
        canvas.StrokeColor = highlighted != null ? highlightColor : strokeColor;
        canvas.StrokeSize = 2f;
        canvas.DrawCircle(cx, cy, ringR);

        // Knob
        canvas.FillColor = strokeColor;
        canvas.FillCircle(knobX, knobY, knobR);

        // Icon centered on knob
        if (icons.TryGetValue("Axis", out var icon) && icon?.Visual is not null)
        {
            float iox = JoystickDescriptor.IndicatorOffsetX;
            float ioy = JoystickDescriptor.IndicatorOffsetY;
            canvas.DrawImage(icon.Visual, knobX - knobR + iox, knobY - knobR + ioy, knobR * 2, knobR * 2);
        }
    }

    public override async Task LoadIconsAsync()
    {
        icons.Clear();

        string key = $"{JoystickDescriptor.WidgetId}/Axis";

        if (Controller.SlotData.TryGetValue(key, out var slotData))
        {
            int size = GetKnobIconSize();
            if (size > 0)
            {
                var c = Color.FromArgb(JoystickDescriptor.IndicatorColor);
                icons["Axis"] = await Icon.GenerateAsync(IconManager.GetSvgPath(slotData.SvgSource), size, size,
                    (int)(c.Red * 255), (int)(c.Green * 255), (int)(c.Blue * 255));
            }
        }

        OwningControllerView?.Invalidate();
    }
    public override Task LoadIconForSlotAsync(string slotID) => LoadIconsAsync();
    public override Size GetSize()
    {
        double w = JoystickDescriptor.RingRadius * 2.0 + 4.0;
        return new Size(w, w);
    }
    public override string? GetHitSlotID(PointF localPoint)
    {
        var size = GetSize();
        if (localPoint.X < 0 || localPoint.Y < 0 || localPoint.X > size.Width || localPoint.Y > size.Height)
            return null;
        return "Axis";
    }

    public override bool OnPress(PointF localPoint, MouseButtonKind button = MouseButtonKind.Left)
    {
        var desc = JoystickDescriptor;

        if (desc.GrabMode == JoystickGrabMode.Hover)
        {
            if (SelectedSlotId == null) return false;
            Deactivate();
            _suppressHoverUntilRelease = true;
            return true;
        }

        // Click mode: first click grabs, second click releases.
        // Hold mode: press grabs, release deactivates (handled in OnRelease).
        if (SelectedSlotId == null)
        {
            if (!IsOverKnob(localPoint)) return false;
            SelectedSlotId = "Axis";
        }
        else if (desc.GrabMode == JoystickGrabMode.Click)
        {
            Deactivate();
            return false;
        }

        UpdateKnob(localPoint);
        return true;
    }
    public override void OnRelease()
    {
        _suppressHoverUntilRelease = false;
        if (JoystickDescriptor.GrabMode == JoystickGrabMode.Hold)
            Deactivate();
    }
    public override void OnPointerMoved(PointF localPoint)
    {
        var desc = JoystickDescriptor;

        if (SelectedSlotId == null && desc.GrabMode == JoystickGrabMode.Hover && !_suppressHoverUntilRelease)
        {
            if (!IsOverKnob(localPoint)) return;
            SelectedSlotId = "Axis";
        }

        if (SelectedSlotId != null)
            UpdateKnob(localPoint);
    }
    public override void OnPointerExited()
    {
        if (JoystickDescriptor.ReleaseOnLeave)
            Deactivate();
    }

    private bool IsOverKnob(PointF pos)
    {
        var size = GetSize();
        float cx = (float)size.Width / 2f + _knobOffset.X;
        float cy = (float)size.Height / 2f + _knobOffset.Y;
        float dx = pos.X - cx;
        float dy = pos.Y - cy;
        return dx * dx + dy * dy <= JoystickDescriptor.KnobRadius * JoystickDescriptor.KnobRadius;
    }
    private void Deactivate()
    {
        if (JoystickDescriptor.ReturnMode == JoystickReturnMode.Center)
            _knobOffset = PointF.Zero;

        if (JoystickDescriptor.TriggerAfterRelease != true)
            Controller.TriggerBindingMove2D(WidgetId, "Axis", 0f, 0f);

        SelectedSlotId = null;
    }
    private void UpdateKnob(PointF pt)
    {
        var size = GetSize();
        float cx = (float)size.Width / 2f;
        float cy = (float)size.Height / 2f;
        float ringR = JoystickDescriptor.RingRadius;
        float knobR = JoystickDescriptor.KnobRadius;
        float maxTravel = ringR - knobR;

        float dx = (pt.X - cx) * (JoystickDescriptor.SensitivityX / 100f);
        float dy = (pt.Y - cy) * (JoystickDescriptor.SensitivityY / 100f);
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist > maxTravel && dist > 0)
        {
            dx = dx / dist * maxTravel;
            dy = dy / dist * maxTravel;
        }

        _knobOffset = new PointF(dx, dy);

        float normX = maxTravel > 0 ? dx / maxTravel : 0f;
        float normY = maxTravel > 0 ? dy / maxTravel : 0f;

        Controller.TriggerBindingMove2D(WidgetId, "Axis", normX, normY);
    }
    private int GetKnobIconSize() => (int)(JoystickDescriptor.KnobRadius * 2f);
}
