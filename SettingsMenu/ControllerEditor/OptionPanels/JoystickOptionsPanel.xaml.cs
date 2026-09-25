namespace OverjoyedVersion3;

public partial class JoystickOptionsPanel : ContentView
{
    private static readonly Color ActiveColor = Color.FromArgb("#3A4A6A");
    private static Color InactiveColor => SettingsManager.FieldColor;

    private static readonly string[] XboxIcons =
    [
        "xbox_stick_l",     "xbox_stick_r",
        "xbox_stick_top_l", "xbox_stick_top_r",
        "xbox_stick_l_up",  "xbox_stick_l_down",  "xbox_stick_l_left",  "xbox_stick_l_right",
        "xbox_stick_r_up",  "xbox_stick_r_down",  "xbox_stick_r_left",  "xbox_stick_r_right"
    ];
    private static readonly string[] KeyboardIcons = ["keyboard_wasd", "keyboard_arrows_all"];

    private readonly ControllerEditor _editor;
    private Joystick? _widget;
    private string _slotId = string.Empty;
    private bool _isUpdating;
    private readonly Dictionary<string, Button> _bindingButtons = new();
    private readonly Dictionary<string, Border> _iconListButtons = new();

    private Controller Controller => _widget!.Controller!;
    private JoystickWidgetDescriptor Descriptor => (JoystickWidgetDescriptor)_widget!.Descriptor!;

    public JoystickOptionsPanel(ControllerEditor editor)
    {
        _editor = editor;
        InitializeComponent();
        SettingsManager.SecondaryColorChanged += (_, _) => RefreshInactiveColors();
        SettingsManager.FontColorChanged += (_, _) => RefreshTextColors();
        IconManager.IconsRecolored += (_, _) => RefreshIconColors();
        _editor.InputDeviceTypeChanged += (_, _) =>
        {
            CreateBindingList();
            CreateIconList();
        };
        InitializePanel();

        XSlider.ValueChanged += (_, e) => XLabel.Text = $"X  {(int)e.NewValue}";
        YSlider.ValueChanged += (_, e) => YLabel.Text = $"Y  {(int)e.NewValue}";
        XSlider.DragCompleted += async (_, _) => {
            Descriptor.SensitivityX = (float)XSlider.Value;
            await ControllerManager.SaveControllerAsync(Controller.Name);
        };
        YSlider.DragCompleted += async (_, _) => {
            Descriptor.SensitivityY = (float)YSlider.Value;
            await ControllerManager.SaveControllerAsync(Controller.Name);
        };

        RingRadiusSlider.ValueChanged += (_, e) => RingRadiusLabel.Text = $"Ring Radius  {(int)e.NewValue}";
        KnobRadiusSlider.ValueChanged += (_, e) => KnobRadiusLabel.Text = $"Knob Radius  {(int)e.NewValue}";
        RingRadiusSlider.DragCompleted += async (_, _) => {
            if (_widget == null) return;
            Descriptor.RingRadius = (float)RingRadiusSlider.Value;
            _editor.InvalidateCanvas();
            await ControllerManager.SaveControllerAsync(Controller.Name);
        };
        KnobRadiusSlider.DragCompleted += async (_, _) => {
            if (_widget == null) return;
            Descriptor.KnobRadius = (float)KnobRadiusSlider.Value;
            _editor.InvalidateCanvas();
            await ControllerManager.SaveControllerAsync(Controller.Name);
        };

        IndicatorOffsetXSlider.ValueChanged += (_, e) => IndicatorOffsetXLabel.Text = $"Offset X  {(int)e.NewValue}";
        IndicatorOffsetYSlider.ValueChanged += (_, e) => IndicatorOffsetYLabel.Text = $"Offset Y  {(int)e.NewValue}";
        IndicatorOffsetXSlider.DragCompleted += async (_, _) => {
            if (_widget == null) return;
            Descriptor.IndicatorOffsetX = (float)IndicatorOffsetXSlider.Value;
            _editor.InvalidateCanvas();
            await ControllerManager.SaveControllerAsync(Controller.Name);
        };
        IndicatorOffsetYSlider.DragCompleted += async (_, _) => {
            if (_widget == null) return;
            Descriptor.IndicatorOffsetY = (float)IndicatorOffsetYSlider.Value;
            _editor.InvalidateCanvas();
            await ControllerManager.SaveControllerAsync(Controller.Name);
        };
    }
    private void InitializePanel()
    {
        GrabModePicker.Items.Add("Hover");
        GrabModePicker.Items.Add("Click");
        GrabModePicker.Items.Add("Hold");
        ReturnModePicker.Items.Add("Center");
        ReturnModePicker.Items.Add("Stay");

        CreateIconList();
    }

    public void Update(string slotId, Widget widget)
    {
        _isUpdating = true;
        _widget = (Joystick)widget;
        _slotId = slotId;

        XEntry.Text = Descriptor.X.ToString("0.##");
        YEntry.Text = Descriptor.Y.ToString("0.##");
        RotationLabel.Text = $"Rotation  {(int)Descriptor.Rotation}";
        RotationSlider.Value = Descriptor.Rotation;
        ScaleLabel.Text = $"Scale  {Descriptor.Scale:0.0}";
        ScaleSlider.Value = Descriptor.Scale;

        XLabel.Text = $"X  {(int)Descriptor.SensitivityX}";
        YLabel.Text = $"Y  {(int)Descriptor.SensitivityY}";
        XSlider.Value = Descriptor.SensitivityX;
        YSlider.Value = Descriptor.SensitivityY;

        HighlightColorEntry.Text = Descriptor.HighlightColor;
        StrokeColorEntry.Text = Descriptor.StrokeColor;
        IndicatorColorEntry.Text = Descriptor.IndicatorColor;

        RingRadiusLabel.Text = $"Ring Radius  {(int)Descriptor.RingRadius}";
        KnobRadiusLabel.Text = $"Knob Radius  {(int)Descriptor.KnobRadius}";
        RingRadiusSlider.Value = Descriptor.RingRadius;
        KnobRadiusSlider.Value = Descriptor.KnobRadius;

        IndicatorOffsetXLabel.Text = $"Offset X  {(int)Descriptor.IndicatorOffsetX}";
        IndicatorOffsetYLabel.Text = $"Offset Y  {(int)Descriptor.IndicatorOffsetY}";
        IndicatorOffsetXSlider.Value = Descriptor.IndicatorOffsetX;
        IndicatorOffsetYSlider.Value = Descriptor.IndicatorOffsetY;

        RefreshGrabMode(Descriptor.GrabMode);
        RefreshReturnMode(Descriptor.ReturnMode);
        ReleaseOnLeaveCheckBox.IsChecked = Descriptor.ReleaseOnLeave;
        TriggerAfterReleaseCheckBox.IsChecked = Descriptor.TriggerAfterRelease;

        BindingListLayout.Children.Clear();
        _bindingButtons.Clear();
        CreateBindingList();

        CreateIconList();
        RefreshIconListSelection();
        _isUpdating = false;
    }

    // Helpers
    private void CreateBindingList()
    {
        if (_widget == null) return;
        BindingListLayout.Children.Clear();
        _bindingButtons.Clear();
        var key = $"{Descriptor.WidgetId}/{_slotId}";
        Controller.SlotData.TryGetValue(key, out var slotDataForList);
        var selected = slotDataForList?.BoundInputIds.ToHashSet() ?? new HashSet<string>();


        var availableInputIds = VirtualInputManager.Instance.GetAxisInputLabels(_editor.InputDeviceType)
                   .Where(inputId => VirtualInputManager.Instance.FindAxisInput(inputId)?.Move2D != null);

        foreach (var inputId in availableInputIds)
        {
            var btn = CreateBindingButton(inputId);
            if (selected.Contains(inputId))
                btn.BackgroundColor = ActiveColor;
            btn.Clicked += async (_, _) => await BindAction(inputId);
            _bindingButtons[inputId] = btn;
            BindingListLayout.Children.Add(btn);
        }
    }
    private void CreateIconList()
    {
        IconListLayout.Children.Clear();
        _iconListButtons.Clear();

        var availableIcons = _editor.InputDeviceType == VirtualDeviceType.InputSimulatorKeyboard
            ? KeyboardIcons : XboxIcons;
        foreach (var name in availableIcons)
        {
            var image = new Image { Aspect = Aspect.AspectFit, Margin = new Thickness(6), Source = IconManager.GetIcon(name) };
            var border = new Border
            {
                WidthRequest = 44,
                HeightRequest = 44,
                BackgroundColor = InactiveColor,
                StrokeThickness = 0,
                Margin = new Thickness(0, 0, 4, 4),
                Content = image,
            };
            border.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(4) };
            var captured = name;
            border.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => _ = OnIconSelectedAsync(captured))
            });
            _iconListButtons[name] = border;
            IconListLayout.Children.Add(border);
        }
    }
    private static Button CreateBindingButton(string inputId) => new()
    {
        Text = inputId,
        FontSize = 14,
        HeightRequest = 32,
        Padding = new Thickness(10, 0),
        Margin = new Thickness(0, 0, 4, 4),
        BackgroundColor = InactiveColor,
        TextColor = SettingsManager.FontColor,
        CornerRadius = 4,
        HorizontalOptions = LayoutOptions.Start,
    };
    private void RefreshIconListSelection()
    {
        var key = $"{Descriptor.WidgetId}/{_slotId}";
        var currentIcon = Controller.SlotData.TryGetValue(key, out var slotData)
            ? slotData.SvgSource : "question";

        foreach (var (name, btn) in _iconListButtons)
            btn.BackgroundColor = name == currentIcon ? ActiveColor : InactiveColor;
    }
    private void RefreshGrabMode(JoystickGrabMode mode) => GrabModePicker.SelectedIndex = (int)mode;
    private void RefreshReturnMode(JoystickReturnMode mode) => ReturnModePicker.SelectedIndex = (int)mode;

    private void RefreshInactiveColors()
    {
        foreach (var btn in _bindingButtons.Values)
        {
            if (btn.BackgroundColor != ActiveColor) btn.BackgroundColor = InactiveColor;
        }
        foreach (var border in _iconListButtons.Values)
        {
            if (border.BackgroundColor != ActiveColor) border.BackgroundColor = InactiveColor;
        }
    }

    private void RefreshTextColors()
    {
        foreach (var btn in _bindingButtons.Values)
        {
            btn.TextColor = SettingsManager.FontColor;
        }
    }

    private void RefreshIconColors()
    {
        foreach (var (name, border) in _iconListButtons)
        {
            if (border.Content is Image image)
            {
                image.Source = IconManager.GetIcon(name);
            }
        }
    }

    private async void OnHighlightColorEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (_widget == null) return;
        if (TryParseHexColor(HighlightColorEntry.Text, out var hex))
        {
            Descriptor.HighlightColor = hex;
            _editor.InvalidateCanvas();
            await ControllerManager.SaveControllerAsync(Controller.Name);
        }
        else
        {
            HighlightColorEntry.Text = Descriptor.HighlightColor;
        }
    }
    private async void OnStrokeColorEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (_widget == null) return;
        if (TryParseHexColor(StrokeColorEntry.Text, out var hex))
        {
            Descriptor.StrokeColor = hex;
            _editor.InvalidateCanvas();
            await ControllerManager.SaveControllerAsync(Controller.Name);
        }
        else
        {
            StrokeColorEntry.Text = Descriptor.StrokeColor;
        }
    }
    private async void OnIndicatorColorEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (_widget == null) return;
        if (TryParseHexColor(IndicatorColorEntry.Text, out var hex))
        {
            Descriptor.IndicatorColor = hex;
            await _widget.LoadIconForSlotAsync(_slotId);
            await ControllerManager.SaveControllerAsync(Controller.Name);
        }
        else
        {
            IndicatorColorEntry.Text = Descriptor.IndicatorColor;
        }
    }
    private static bool TryParseHexColor(string? text, out string hex)
    {
        hex = string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var candidate = text.StartsWith('#') ? text : $"#{text}";
        if (!System.Text.RegularExpressions.Regex.IsMatch(candidate, "^#[0-9A-Fa-f]{6}$")) return false;

        hex = candidate.ToUpperInvariant();
        return true;
    }

    // Button and input handlers
    private async Task OnIconSelectedAsync(string iconName)
    {
        if (_widget == null) return;

        foreach (var btn in _iconListButtons.Values)
            btn.BackgroundColor = InactiveColor;
        if (_iconListButtons.TryGetValue(iconName, out var selected))
            selected.BackgroundColor = ActiveColor;

        Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId).SvgSource = iconName;

        await _widget.LoadIconForSlotAsync(_slotId);

        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private async void OnGrabModeChanged(object sender, EventArgs e)
    {
        if (_widget == null || _isUpdating) return;
        var mode = (JoystickGrabMode)GrabModePicker.SelectedIndex;
        Descriptor.GrabMode = mode;
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private async void OnReleaseOnLeaveChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_widget == null || _isUpdating) return;
        Descriptor.ReleaseOnLeave = e.Value;
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private async void OnTriggerAfterReleaseChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_widget == null || _isUpdating) return;
        Descriptor.TriggerAfterRelease = e.Value;
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private async void OnReturnModeChanged(object sender, EventArgs e)
    {
        if (_widget == null || _isUpdating) return;
        var mode = (JoystickReturnMode)ReturnModePicker.SelectedIndex;
        Descriptor.ReturnMode = mode;
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private async void OnXEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (_widget == null) return;
        if (double.TryParse(XEntry.Text, out var value))
        {
            Descriptor.X = value;
            _editor.InvalidateCanvas();
            await ControllerManager.SaveControllerAsync(Controller.Name);
        }
        else
        {
            XEntry.Text = Descriptor.X.ToString("0.##");
        }
    }
    private async void OnYEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (_widget == null) return;
        if (double.TryParse(YEntry.Text, out var value))
        {
            Descriptor.Y = value;
            _editor.InvalidateCanvas();
            await ControllerManager.SaveControllerAsync(Controller.Name);
        }
        else
        {
            YEntry.Text = Descriptor.Y.ToString("0.##");
        }
    }
    private void OnRotationSliderChanged(object sender, ValueChangedEventArgs e)
        => RotationLabel.Text = $"Rotation  {(int)e.NewValue}";
    private async void OnRotationSliderCompleted(object sender, EventArgs e)
    {
        if (_widget == null) return;
        Descriptor.Rotation = (float)RotationSlider.Value;
        _editor.InvalidateCanvas();
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private void OnScaleSliderChanged(object sender, ValueChangedEventArgs e)
        => ScaleLabel.Text = $"Scale  {e.NewValue:0.0}";
    private async void OnScaleSliderCompleted(object sender, EventArgs e)
    {
        if (_widget == null) return;
        Descriptor.Scale = (float)ScaleSlider.Value;
        _editor.InvalidateCanvas();
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_widget != null) _ = _editor.DeleteWidgetAsync(_widget.WidgetId);
    }
    private void OnTransformHeaderTapped(object? sender, EventArgs e) => ToggleSection(TransformContent, TransformChevron);
    private void OnAppearanceHeaderTapped(object? sender, EventArgs e) => ToggleSection(AppearanceContent, AppearanceChevron);
    private void OnIndicatorHeaderTapped(object? sender, EventArgs e) => ToggleSection(IndicatorContent, IndicatorChevron);
    private void OnBehaviorHeaderTapped(object? sender, EventArgs e) => ToggleSection(BehaviorContent, BehaviorChevron);
    private void OnDangerZoneHeaderTapped(object? sender, EventArgs e) => ToggleSection(DeleteWidgetButton, DangerZoneChevron);
    private static void ToggleSection(VisualElement content, Label chevron)
    {
        content.IsVisible = !content.IsVisible;
        chevron.Text = content.IsVisible ? "\u25BE" : "\u25B8";
    }

    private async Task BindAction(string inputId)
    {
        var slot = Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId);
        bool wasSelected = slot.BoundInputIds.Contains(inputId);

        slot.BoundInputIds.Clear();
        if (!wasSelected)
            slot.BoundInputIds.Add(inputId);

        await ControllerManager.SaveControllerAsync(Controller.Name);
        CreateBindingList();

        if (!wasSelected &&
            BindingIconMap.TryGetIcon(inputId, out var iconName) && _iconListButtons.ContainsKey(iconName))
            await OnIconSelectedAsync(iconName);
    }
}
