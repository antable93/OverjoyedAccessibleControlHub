namespace OverjoyedVersion3;

public partial class DialOptionsPanel : ContentView
{
    private static readonly Color ActiveColor = Color.FromArgb("#3A4A6A");
    private static Color InactiveColor => SettingsManager.FieldColor;

    private static readonly string[] AvailableIcons =
    [
        "keyboard_a",           "keyboard_b",          "keyboard_c",           "keyboard_d", 
        "keyboard_e",           "keyboard_f",          "keyboard_g",           "keyboard_h",
        "keyboard_i",           "keyboard_j",          "keyboard_k",           "keyboard_l",
        "keyboard_m",           "keyboard_n",          "keyboard_o",           "keyboard_p",
        "keyboard_q",           "keyboard_r",          "keyboard_s",           "keyboard_t",
        "keyboard_u",           "keyboard_v",          "keyboard_w",           "keyboard_x",
        "keyboard_y",           "keyboard_z",          "keyboard_shift",       "keyboard_ctrl",
        "keyboard_alt",         "keyboard_escape",     "keyboard_backspace",   "keyboard_capslock",
        "keyboard_arrow_up",    "keyboard_arrow_down", "keyboard_arrow_left",  "keyboard_arrow_right",
        "keyboard_enter",       "keyboard_tab",        "keyboard_space",       "keyboard_delete",
        "keyboard_insert",      "keyboard_home",       "keyboard_end",         "keyboard_page_up",
        "keyboard_page_down",   "keyboard_numlock",    "keyboard_printscreen", "keyboard_f1",  
        "keyboard_f2",          "keyboard_f3",         "keyboard_f4",          "keyboard_f5",  
        "keyboard_f6",          "keyboard_f7",         "keyboard_f8",          "keyboard_f9",  
        "keyboard_f10",         "keyboard_f11",        "keyboard_f12",         "xbox_button_a",     
        "xbox_button_b",        "xbox_button_x",       "xbox_button_y",        "xbox_lb",           
        "xbox_rb",              "xbox_ls",             "xbox_rs",              "xbox_dpad_up",      
        "xbox_dpad_down",       "xbox_dpad_left",      "xbox_dpad_right",      "xbox_button_start", 
        "xbox_button_back",     "xbox_guide",          "xbox_lt",              "xbox_rt",
    ];

    private readonly ControllerEditor _editor;
    private Dial? _widget;
    private string _slotId = string.Empty;
    private bool _isUpdating;
    private readonly Dictionary<string, Button> _bindingButtons = new();
    private readonly Dictionary<string, Border> _iconListButtons = new();

    private Controller Controller => _widget!.Controller!;
    private DialWidgetDescriptor Descriptor => (DialWidgetDescriptor)_widget!.Descriptor!;

    public DialOptionsPanel(ControllerEditor editor)
    {
        _editor = editor;
        InitializeComponent();
        SettingsManager.SecondaryColorChanged += (_, _) => RefreshInactiveColors();
        SettingsManager.FontColorChanged += (_, _) => RefreshTextColors();
        IconManager.IconsRecolored += (_, _) => RefreshIconColors();
        CreateIconList();
    }

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
        if (ClickButton.BackgroundColor != ActiveColor) ClickButton.BackgroundColor = InactiveColor;
        if (HoverButton.BackgroundColor != ActiveColor) HoverButton.BackgroundColor = InactiveColor;
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

    public void Update(string slotId, Widget widget)
    {
        _isUpdating = true;
        _widget = (Dial)widget;
        _slotId = slotId;

        BindingListLayout.Children.Clear();
        _bindingButtons.Clear();
        CreateBindingList();

        var key = $"{Descriptor.WidgetId}/{slotId}";
        Controller.SlotData.TryGetValue(key, out var slotData);

        var interactionMode = slotData?.Options.GetValueOrDefault("InteractionMode", "Click") ?? "Click";
        RefreshInteractionMode(interactionMode);

        float indicatorSize = float.TryParse(slotData?.Options.GetValueOrDefault("IndicatorSize"), out var sz) && sz >= 4 ? sz : 16f;
        IndicatorSizeSlider.Value = indicatorSize;

        IndicatorColorEntry.Text = slotData?.Options.GetValueOrDefault("IndicatorColor", "#FFFFFF") ?? "#FFFFFF";

        RefreshDisplayMode(slotData?.Options.GetValueOrDefault("IsUsingIcon", "true") != "false");
        RefreshIconListSelection();
        LabelEntry.Text = slotData?.Label ?? string.Empty;
        LabelEntry.WidthRequest = Math.Max(150, (slotData?.Label?.Length ?? 0) * 9.0 + 16);
        float ox = float.TryParse(slotData?.Options.GetValueOrDefault("IndicatorOffsetX"), out var oxv) ? oxv : 0f;
        float oy = float.TryParse(slotData?.Options.GetValueOrDefault("IndicatorOffsetY"), out var oyv) ? oyv : 0f;
        IndicatorOffsetXLabel.Text = $"Offset X  {(int)ox}";
        IndicatorOffsetYLabel.Text = $"Offset Y  {(int)oy}";
        IndicatorOffsetXSlider.Value = ox;
        IndicatorOffsetYSlider.Value = oy;

        XEntry.Text = Descriptor.X.ToString("0.##");
        YEntry.Text = Descriptor.Y.ToString("0.##");
        RotationLabel.Text = $"Rotation  {(int)Descriptor.Rotation}";
        RotationSlider.Value = Descriptor.Rotation;
        ScaleLabel.Text = $"Scale  {Descriptor.Scale:0.0}";
        ScaleSlider.Value = Descriptor.Scale;

        HighlightColorEntry.Text = Descriptor.HighlightColor;
        StrokeColorEntry.Text = Descriptor.StrokeColor;

        _isUpdating = false;
    }

    private void CreateBindingList()
    {
        var key = $"{Descriptor.WidgetId}/{_slotId}";
        Controller.SlotData.TryGetValue(key, out var slotDataForList);
        var selected = slotDataForList?.BoundInputIds.ToHashSet() ?? new HashSet<string>();

        foreach (var label in VirtualInputManager.Instance.GetAllActionInputLabels())
        {
            var btn = CreateBindingButton(label);
            if (selected.Contains(label))
                btn.BackgroundColor = ActiveColor;
            btn.Clicked += async (_, _) => await BindAction(label);
            _bindingButtons[label] = btn;
            BindingListLayout.Children.Add(btn);
        }
    }
    private void CreateIconList()
    {
        foreach (var name in AvailableIcons)
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
    private static Button CreateBindingButton(string text) => new()
    {
        Text = text,
        FontSize = 14,
        HeightRequest = 32,
        Padding = new Thickness(10, 0),
        Margin = new Thickness(0, 0, 4, 4),
        BackgroundColor = InactiveColor,
        TextColor = SettingsManager.FontColor,
        CornerRadius = 4,
        HorizontalOptions = LayoutOptions.Start,
    };
    private void RefreshDisplayMode(bool isUsingIcon)
    {
        IsUsingIconCheckBox.IsChecked = isUsingIcon;
        IconListScrollView.IsVisible = isUsingIcon;
        LabelTextLayout.IsVisible = !isUsingIcon;
        LabelSizeLayout.IsVisible = true;
        IndicatorSizeLabel.Text = $"{(isUsingIcon ? "Icon Size" : "Font Size")}  {(int)IndicatorSizeSlider.Value}";
    }
    private void RefreshIconListSelection()
    {
        var key = $"{Descriptor.WidgetId}/{_slotId}";
        var currentIcon = Controller.SlotData.TryGetValue(key, out var slotData)
            ? slotData.SvgSource : "question";

        foreach (var (name, btn) in _iconListButtons)
            btn.BackgroundColor = name == currentIcon ? ActiveColor : InactiveColor;
    }
    private void RefreshInteractionMode(string active)
    {
        ClickButton.BackgroundColor = active == "Click" ? ActiveColor : InactiveColor;
        HoverButton.BackgroundColor = active == "Hover" ? ActiveColor : InactiveColor;
    }

    // Button and input handlers
    private async void OnIsUsingIconChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_widget == null || _isUpdating) return;
        Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId).Options["IsUsingIcon"] = e.Value ? "true" : "false";
        RefreshDisplayMode(e.Value);
        _editor.InvalidateCanvas();
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private void OnLabelEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_widget == null) return;
        Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId).Label = e.NewTextValue ?? string.Empty;
        LabelEntry.WidthRequest = Math.Max(150, (e.NewTextValue?.Length ?? 0) * 9.0 + 16);
    }
    private async void OnLabelEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (_widget == null) return;
        Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId).Label = LabelEntry.Text ?? string.Empty;
        _editor.InvalidateCanvas();
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private void OnIndicatorSizeSliderChanged(object sender, ValueChangedEventArgs e)
        => IndicatorSizeLabel.Text = $"{(IsUsingIconCheckBox.IsChecked ? "Icon Size" : "Font Size")}  {(int)e.NewValue}";
    private async void OnIndicatorSizeSliderCompleted(object sender, EventArgs e)
    {
        if (_widget == null) return;
        Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId).Options["IndicatorSize"] = ((float)IndicatorSizeSlider.Value).ToString();
        _editor.InvalidateCanvas();
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private void OnIndicatorOffsetXSliderChanged(object sender, ValueChangedEventArgs e)
        => IndicatorOffsetXLabel.Text = $"Offset X  {(int)e.NewValue}";
    private async void OnIndicatorOffsetXSliderCompleted(object sender, EventArgs e)
    {
        if (_widget == null) return;
        Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId).Options["IndicatorOffsetX"] = ((float)IndicatorOffsetXSlider.Value).ToString();
        _editor.InvalidateCanvas();
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
    private void OnIndicatorOffsetYSliderChanged(object sender, ValueChangedEventArgs e)
        => IndicatorOffsetYLabel.Text = $"Offset Y  {(int)e.NewValue}";
    private async void OnIndicatorOffsetYSliderCompleted(object sender, EventArgs e)
    {
        if (_widget == null) return;
        Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId).Options["IndicatorOffsetY"] = ((float)IndicatorOffsetYSlider.Value).ToString();
        _editor.InvalidateCanvas();
        await ControllerManager.SaveControllerAsync(Controller.Name);
    }
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
    private async void OnIndicatorColorEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (_widget == null) return;
        if (TryParseHexColor(IndicatorColorEntry.Text, out var hex))
        {
            Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId).Options["IndicatorColor"] = hex;
            await _widget.LoadIconForSlotAsync(_slotId);
            await ControllerManager.SaveControllerAsync(Controller.Name);
        }
        else
        {
            IndicatorColorEntry.Text = Controller.SlotData.TryGetValue($"{Descriptor.WidgetId}/{_slotId}", out var slotData)
                ? slotData.Options.GetValueOrDefault("IndicatorColor", "#FFFFFF") : "#FFFFFF";
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
    private static bool TryParseHexColor(string? text, out string hex)
    {
        hex = string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var candidate = text.StartsWith('#') ? text : $"#{text}";
        if (!System.Text.RegularExpressions.Regex.IsMatch(candidate, "^#[0-9A-Fa-f]{6}$")) return false;

        hex = candidate.ToUpperInvariant();
        return true;
    }
    private async void OnClickButtonClicked(object sender, EventArgs e) => await SetMode("Click");
    private async void OnHoverButtonClicked(object sender, EventArgs e) => await SetMode("Hover");
    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_widget != null) _ = _editor.DeleteWidgetAsync(_widget.WidgetId);
    }

    private async Task BindAction(string label)
    {
        var key = $"{Descriptor.WidgetId}/{_slotId}";
        bool wasSelected = Controller.SlotData.TryGetValue(key, out var slot) && slot.BoundInputIds.Contains(label);

        await Controller.Bind(Descriptor.WidgetId, _slotId, label);

        if (_bindingButtons.TryGetValue(label, out var btn))
            btn.BackgroundColor = wasSelected ? InactiveColor : ActiveColor;
    }
    private async Task SetMode(string mode)
    {
        Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId).Options["InteractionMode"] = mode;
        await ControllerManager.SaveControllerAsync(Controller.Name);
        RefreshInteractionMode(mode);
    }
}
