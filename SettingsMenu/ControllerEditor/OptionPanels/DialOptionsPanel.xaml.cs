namespace OverjoyedVersion3;

public partial class DialOptionsPanel : ContentView
{
    private static readonly Color ActiveColor = Color.FromArgb("#3A4A6A");
    private static Color InactiveColor => SettingsManager.FieldColor;
    private static readonly Color DisabledColor = Color.FromArgb("#9B2C2C");

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
        "xbox_stick_l_up",      "xbox_stick_l_down",   "xbox_stick_l_left",    "xbox_stick_l_right",
        "xbox_stick_r_up",      "xbox_stick_r_down",   "xbox_stick_r_left",    "xbox_stick_r_right",
    ];

    private readonly ControllerEditor _editor;
    private Dial? _widget;
    private string _slotId = string.Empty;
    private bool _isUpdating;
    private readonly Dictionary<string, Button> _bindingButtons = new();
    private readonly Dictionary<string, Border> _iconListButtons = new();
    private static readonly int[] QuadrantCounts = Enumerable.Range(1, 16).ToArray();
    private static readonly Dictionary<string, string> BindingModeLabels = new()
    {
        ["LeftClick"] = "Left Click",
        ["RightClick"] = "Right Click",
        ["MiddleClick"] = "Middle Click",
        ["Hover"] = "Hover",
    };
    private static readonly Dictionary<string, string> OppositeDirectionalBindings = new()
    {
        ["Xbox LS Up"] = "Xbox LS Down",
        ["Xbox LS Down"] = "Xbox LS Up",
        ["Xbox LS Left"] = "Xbox LS Right",
        ["Xbox LS Right"] = "Xbox LS Left",
        ["Xbox RS Up"] = "Xbox RS Down",
        ["Xbox RS Down"] = "Xbox RS Up",
        ["Xbox RS Left"] = "Xbox RS Right",
        ["Xbox RS Right"] = "Xbox RS Left",
    };
    private string _bindingMode = "Hover";

    private Controller Controller => _widget!.Controller!;
    private DialWidgetDescriptor Descriptor => (DialWidgetDescriptor)_widget!.Descriptor!;

    public DialOptionsPanel(ControllerEditor editor)
    {
        _editor = editor;
        InitializeComponent();
        SettingsManager.SecondaryColorChanged += (_, _) => RefreshInactiveColors();
        SettingsManager.FontColorChanged += (_, _) => RefreshTextColors();
        IconManager.IconsRecolored += (_, _) => RefreshIconColors();
        _editor.InputDeviceTypeChanged += (_, _) =>
        {
            RebuildBindingList();
            CreateIconList();
        };
        CreateIconList();
        QuadrantCountPicker.ItemsSource = QuadrantCounts;
        BindingModePicker.ItemsSource = BindingModeLabels.Values.ToList();
        BindingModePicker.SelectedItem = BindingModeLabels[_bindingMode];
    }

    private void RefreshInactiveColors()
    {
        foreach (var btn in _bindingButtons.Values)
        {
            if (!btn.IsEnabled)
            {
                btn.BackgroundColor = DisabledColor;
                continue;
            }
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

    public void Update(string slotId, Widget widget)
    {
        _isUpdating = true;
        _widget = (Dial)widget;
        _slotId = string.IsNullOrEmpty(slotId) ? "Quadrant_0" : slotId;

        if (_widget.SelectedSlotId != _slotId)
        {
            _widget.SelectedSlotId = _slotId;
            _editor.InvalidateCanvas();
        }

        var key = $"{Descriptor.WidgetId}/{_slotId}";
        Controller.SlotData.TryGetValue(key, out var slotData);

        BindingModePicker.SelectedItem = BindingModeLabels[_bindingMode];
        RebuildBindingList();

        float indicatorSize = float.TryParse(slotData?.Options.GetValueOrDefault("IndicatorSize"), out var sz) && sz >= 4 ? sz : 16f;
        IndicatorSizeSlider.Value = indicatorSize;

        IndicatorColorEntry.Text = slotData?.Options.GetValueOrDefault("IndicatorColor", "#000000") ?? "#000000";

        RefreshDisplayMode(slotData?.Options.GetValueOrDefault("IsUsingIcon", "true") != "false");
        CreateIconList();
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
        QuadrantCountPicker.SelectedItem = Descriptor.QuadrantCount;
        RefreshQuadrantCycleLabel();
        RotationLabel.Text = $"Rotation  {(int)Descriptor.Rotation}";
        RotationSlider.Value = Descriptor.Rotation;
        ScaleLabel.Text = $"Scale  {Descriptor.Scale:0.0}";
        ScaleSlider.Value = Descriptor.Scale;

        HighlightColorEntry.Text = Descriptor.HighlightColor;
        StrokeColorEntry.Text = Descriptor.StrokeColor;

        _isUpdating = false;
    }

    private void CreateIconList()
    {
        IconListLayout.Children.Clear();
        _iconListButtons.Clear();

        bool keyboardMode = _editor.InputDeviceType == VirtualDeviceType.InputSimulatorKeyboard;
        foreach (var name in AvailableIcons.Where(name => name.StartsWith("xbox_") != keyboardMode))
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
    private static List<string> GetBindings(SlotData? slotData, string mode)
    {
        if (slotData == null) return [];
        var bindings = mode switch
        {
            "Hover" => slotData.HoverBoundInputIds,
            "RightClick" => slotData.RightClickBoundInputIds,
            "MiddleClick" => slotData.MiddleClickBoundInputIds,
            _ => slotData.ClickBoundInputIds,
        };
        return bindings.Count > 0 || mode != "LeftClick" ? bindings : slotData.BoundInputIds;
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
    private async void OnQuadrantCountChanged(object sender, EventArgs e)
    {
        if (_widget == null || _isUpdating || QuadrantCountPicker.SelectedItem is not int count) return;

        _widget.SetQuadrantCount(count);
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
                ? slotData.Options.GetValueOrDefault("IndicatorColor", "#000000") : "#000000";
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
    private async void OnBindingModeChanged(object sender, EventArgs e)
    {
        if (_widget == null || _isUpdating || BindingModePicker.SelectedIndex < 0) return;

        var label = BindingModeLabels.Values.ElementAt(BindingModePicker.SelectedIndex);
        var previousMode = _bindingMode;
        _bindingMode = BindingModeLabels.First(kv => kv.Value == label).Key;

        if (previousMode != _bindingMode)
        {
            var key = $"{Descriptor.WidgetId}/{_slotId}";
            if (Controller.SlotData.TryGetValue(key, out var slot))
            {
                ClearBindingsForMode(slot, previousMode);
                await OnIconSelectedAsync("question");
                await ControllerManager.SaveControllerAsync(Controller.Name);
            }
        }

        RebuildBindingList();
    }

    private static void ClearBindingsForMode(SlotData slot, string mode)
    {
        switch (mode)
        {
            case "Hover":
                slot.HoverBoundInputIds.Clear();
                break;
            case "RightClick":
                slot.RightClickBoundInputIds.Clear();
                break;
            case "MiddleClick":
                slot.MiddleClickBoundInputIds.Clear();
                break;
            default:
                slot.ClickBoundInputIds.Clear();
                slot.BoundInputIds.Clear();
                break;
        }
    }

    private void RebuildBindingList()
    {
        if (_widget == null) return;
        BindingListLayout.Children.Clear();
        _bindingButtons.Clear();
        var key = $"{Descriptor.WidgetId}/{_slotId}";
        Controller.SlotData.TryGetValue(key, out var slotDataForList);
        var selected = GetBindings(slotDataForList, _bindingMode).ToHashSet();

        foreach (var label in VirtualInputManager.Instance.GetActionInputLabels(_editor.InputDeviceType))
        {
            var btn = CreateBindingButton(label);
            if (selected.Contains(label))
                btn.BackgroundColor = ActiveColor;
            btn.Clicked += async (_, _) => await BindAction(label);
            _bindingButtons[label] = btn;
            BindingListLayout.Children.Add(btn);
        }

        RefreshDirectionalBindingButtons(selected);
    }
    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_widget != null) _ = _editor.DeleteWidgetAsync(_widget.WidgetId);
    }
    private void OnTransformHeaderTapped(object? sender, EventArgs e) => ToggleSection(TransformContent, TransformChevron);
    private void OnQuadrantsHeaderTapped(object? sender, EventArgs e) => ToggleSection(QuadrantCountPicker, QuadrantsChevron);
    private void OnAppearanceHeaderTapped(object? sender, EventArgs e) => ToggleSection(AppearanceContent, AppearanceChevron);
    private void OnIndicatorHeaderTapped(object? sender, EventArgs e) => ToggleSection(IndicatorContent, IndicatorChevron);
    private void OnDangerZoneHeaderTapped(object? sender, EventArgs e) => ToggleSection(DeleteWidgetButton, DangerZoneChevron);
    private static void ToggleSection(VisualElement content, Label chevron)
    {
        content.IsVisible = !content.IsVisible;
        chevron.Text = content.IsVisible ? "\u25BE" : "\u25B8";
    }

    private void OnPreviousQuadrantClicked(object? sender, EventArgs e) => CycleQuadrant(-1);
    private void OnNextQuadrantClicked(object? sender, EventArgs e) => CycleQuadrant(1);
    private void CycleQuadrant(int delta)
    {
        if (_widget == null || Descriptor.QuadrantCount <= 0) return;

        int count = Descriptor.QuadrantCount;
        int current = _slotId.StartsWith("Quadrant_") && int.TryParse(_slotId["Quadrant_".Length..], out int idx) ? idx : 0;
        int next = ((current + delta) % count + count) % count;
        _editor.SelectSlot(Descriptor.WidgetId, $"Quadrant_{next}");
    }
    private void RefreshQuadrantCycleLabel()
    {
        int count = Descriptor.QuadrantCount;
        int current = _slotId.StartsWith("Quadrant_") && int.TryParse(_slotId["Quadrant_".Length..], out int idx) ? idx : 0;
        QuadrantCycleLabel.Text = $"Quadrant {current + 1} / {count}";
    }

    private async Task BindAction(string label)
    {
        var key = $"{Descriptor.WidgetId}/{_slotId}";
        bool wasSelected = Controller.SlotData.TryGetValue(key, out var slot) &&
            GetBindings(slot, _bindingMode).Contains(label);

        await Controller.Bind(Descriptor.WidgetId, _slotId, label, _bindingMode);

        if (_bindingButtons.TryGetValue(label, out var btn))
            btn.BackgroundColor = wasSelected ? InactiveColor : ActiveColor;

        slot = Controller.GetOrCreateSlotData(Descriptor.WidgetId, _slotId);
        var selected = GetBindings(slot, _bindingMode).ToHashSet();
        RefreshDirectionalBindingButtons(selected);

        if (!wasSelected && BindingIconMap.TryGetIcon(label, out var iconName) && _iconListButtons.ContainsKey(iconName))
            await OnIconSelectedAsync(iconName);
        else if (wasSelected && selected.Count == 0)
            await OnIconSelectedAsync("question");
    }
    private void RefreshDirectionalBindingButtons(IReadOnlySet<string> selected)
    {
        foreach (var (label, btn) in _bindingButtons)
        {
            bool isEnabled = !OppositeDirectionalBindings.TryGetValue(label, out var opposite) ||
                !selected.Contains(opposite);
            btn.IsEnabled = isEnabled;
            if (!isEnabled)
                btn.BackgroundColor = DisabledColor;
            else if (!selected.Contains(label))
                btn.BackgroundColor = InactiveColor;
        }
    }
}
