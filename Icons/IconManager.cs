namespace OverjoyedVersion3;

/// <summary>
/// Pre-generates and caches every icon used by menus and the controller editor, tinted with the
/// current FontColor. Widgets render their own (user-selected) icons ad hoc instead of going
/// through this cache, since those are arbitrary SVGs sized to fit the widget's own geometry.
/// </summary>
public static class IconManager
{
    private const int DefaultSize = 64;

    /// <summary>Icons that render larger than the default 64px (currently just the widget-type cards).</summary>
    private static readonly Dictionary<string, int> SizeOverrides = new()
    {
        ["dial"] = 128,
        ["joystick"] = 128,
    };

    private static readonly Dictionary<string, string> Manifest = new()
    {
        ["gear"] = "Icons/Misc/gear.svg",
        ["gear-filled"] = "Icons/Misc/gear-filled.svg",
        ["gamepad"] = "Icons/Misc/gamepad.svg",
        ["paint-roller"] = "Icons/Misc/paint-roller.svg",
        ["options"] = "Icons/Misc/options.svg",
        ["mouse"] = "Icons/Misc/mouse.svg",
        ["trash"] = "Icons/Misc/trash.svg",
        ["question"] = "Icons/Misc/question.svg",
        ["dial"] = "Icons/Widgets/dial.svg",
        ["joystick"] = "Icons/Widgets/joystick.svg",

        ["keyboard_a"] = "Icons/Keyboard/keyboard_a.svg",
        ["keyboard_b"] = "Icons/Keyboard/keyboard_b.svg",
        ["keyboard_c"] = "Icons/Keyboard/keyboard_c.svg",
        ["keyboard_d"] = "Icons/Keyboard/keyboard_d.svg",
        ["keyboard_e"] = "Icons/Keyboard/keyboard_e.svg",
        ["keyboard_f"] = "Icons/Keyboard/keyboard_f.svg",
        ["keyboard_g"] = "Icons/Keyboard/keyboard_g.svg",
        ["keyboard_h"] = "Icons/Keyboard/keyboard_h.svg",
        ["keyboard_i"] = "Icons/Keyboard/keyboard_i.svg",
        ["keyboard_j"] = "Icons/Keyboard/keyboard_j.svg",
        ["keyboard_k"] = "Icons/Keyboard/keyboard_k.svg",
        ["keyboard_l"] = "Icons/Keyboard/keyboard_l.svg",
        ["keyboard_m"] = "Icons/Keyboard/keyboard_m.svg",
        ["keyboard_n"] = "Icons/Keyboard/keyboard_n.svg",
        ["keyboard_o"] = "Icons/Keyboard/keyboard_o.svg",
        ["keyboard_p"] = "Icons/Keyboard/keyboard_p.svg",
        ["keyboard_q"] = "Icons/Keyboard/keyboard_q.svg",
        ["keyboard_r"] = "Icons/Keyboard/keyboard_r.svg",
        ["keyboard_s"] = "Icons/Keyboard/keyboard_s.svg",
        ["keyboard_t"] = "Icons/Keyboard/keyboard_t.svg",
        ["keyboard_u"] = "Icons/Keyboard/keyboard_u.svg",
        ["keyboard_v"] = "Icons/Keyboard/keyboard_v.svg",
        ["keyboard_w"] = "Icons/Keyboard/keyboard_w.svg",
        ["keyboard_x"] = "Icons/Keyboard/keyboard_x.svg",
        ["keyboard_y"] = "Icons/Keyboard/keyboard_y.svg",
        ["keyboard_z"] = "Icons/Keyboard/keyboard_z.svg",
        ["keyboard_shift"] = "Icons/Keyboard/keyboard_shift.svg",
        ["keyboard_ctrl"] = "Icons/Keyboard/keyboard_ctrl.svg",
        ["keyboard_alt"] = "Icons/Keyboard/keyboard_alt.svg",
        ["keyboard_escape"] = "Icons/Keyboard/keyboard_escape.svg",
        ["keyboard_backspace"] = "Icons/Keyboard/keyboard_backspace.svg",
        ["keyboard_capslock"] = "Icons/Keyboard/keyboard_capslock.svg",
        ["keyboard_arrow_up"] = "Icons/Keyboard/keyboard_arrow_up.svg",
        ["keyboard_arrow_down"] = "Icons/Keyboard/keyboard_arrow_down.svg",
        ["keyboard_arrow_left"] = "Icons/Keyboard/keyboard_arrow_left.svg",
        ["keyboard_arrow_right"] = "Icons/Keyboard/keyboard_arrow_right.svg",
        ["keyboard_enter"] = "Icons/Keyboard/keyboard_enter.svg",
        ["keyboard_tab"] = "Icons/Keyboard/keyboard_tab.svg",
        ["keyboard_space"] = "Icons/Keyboard/keyboard_space.svg",
        ["keyboard_delete"] = "Icons/Keyboard/keyboard_delete.svg",
        ["keyboard_insert"] = "Icons/Keyboard/keyboard_insert.svg",
        ["keyboard_home"] = "Icons/Keyboard/keyboard_home.svg",
        ["keyboard_end"] = "Icons/Keyboard/keyboard_end.svg",
        ["keyboard_page_up"] = "Icons/Keyboard/keyboard_page_up.svg",
        ["keyboard_page_down"] = "Icons/Keyboard/keyboard_page_down.svg",
        ["keyboard_numlock"] = "Icons/Keyboard/keyboard_numlock.svg",
        ["keyboard_printscreen"] = "Icons/Keyboard/keyboard_printscreen.svg",
        ["keyboard_f1"] = "Icons/Keyboard/keyboard_f1.svg",
        ["keyboard_f2"] = "Icons/Keyboard/keyboard_f2.svg",
        ["keyboard_f3"] = "Icons/Keyboard/keyboard_f3.svg",
        ["keyboard_f4"] = "Icons/Keyboard/keyboard_f4.svg",
        ["keyboard_f5"] = "Icons/Keyboard/keyboard_f5.svg",
        ["keyboard_f6"] = "Icons/Keyboard/keyboard_f6.svg",
        ["keyboard_f7"] = "Icons/Keyboard/keyboard_f7.svg",
        ["keyboard_f8"] = "Icons/Keyboard/keyboard_f8.svg",
        ["keyboard_f9"] = "Icons/Keyboard/keyboard_f9.svg",
        ["keyboard_f10"] = "Icons/Keyboard/keyboard_f10.svg",
        ["keyboard_f11"] = "Icons/Keyboard/keyboard_f11.svg",
        ["keyboard_f12"] = "Icons/Keyboard/keyboard_f12.svg",

        ["xbox_button_a"] = "Icons/XboxController/xbox_button_a.svg",
        ["xbox_button_b"] = "Icons/XboxController/xbox_button_b.svg",
        ["xbox_button_x"] = "Icons/XboxController/xbox_button_x.svg",
        ["xbox_button_y"] = "Icons/XboxController/xbox_button_y.svg",
        ["xbox_lb"] = "Icons/XboxController/xbox_lb.svg",
        ["xbox_rb"] = "Icons/XboxController/xbox_rb.svg",
        ["xbox_ls"] = "Icons/XboxController/xbox_ls.svg",
        ["xbox_rs"] = "Icons/XboxController/xbox_rs.svg",
        ["xbox_dpad_up"] = "Icons/XboxController/xbox_dpad_up.svg",
        ["xbox_dpad_down"] = "Icons/XboxController/xbox_dpad_down.svg",
        ["xbox_dpad_left"] = "Icons/XboxController/xbox_dpad_left.svg",
        ["xbox_dpad_right"] = "Icons/XboxController/xbox_dpad_right.svg",
        ["xbox_button_start"] = "Icons/XboxController/xbox_button_start.svg",
        ["xbox_button_back"] = "Icons/XboxController/xbox_button_back.svg",
        ["xbox_guide"] = "Icons/XboxController/xbox_guide.svg",
        ["xbox_lt"] = "Icons/XboxController/xbox_lt.svg",
        ["xbox_rt"] = "Icons/XboxController/xbox_rt.svg",
        ["xbox_stick_l"] = "Icons/XboxController/xbox_stick_l.svg",
        ["xbox_stick_r"] = "Icons/XboxController/xbox_stick_r.svg",
        ["xbox_stick_top_l"] = "Icons/XboxController/xbox_stick_top_l.svg",
        ["xbox_stick_top_r"] = "Icons/XboxController/xbox_stick_top_r.svg",
    };

    public static event EventHandler? IconsRecolored;

    private static readonly Dictionary<string, Icon> _cache = new();
    private static bool _initialized;

    public static async Task Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        foreach (var name in Manifest.Keys)
            _cache[name] = await GenerateAsync(name);

        SettingsManager.FontColorChanged += (_, _) => _ = RecolorAllAsync();
    }

    /// <summary>
    /// Resolves an icon name to its app-package SVG path. Falls back to the question-mark icon
    /// for unknown names, since this is also used to resolve widget-bound icon names that may
    /// reference an icon that no longer exists.
    /// </summary>
    public static string GetSvgPath(string name) =>
        Manifest.TryGetValue(name, out var path) ? path : Manifest["question"];

    public static Icon? GetIcon(string name) => _cache.GetValueOrDefault(name);

    public static async Task RecolorAllAsync()
    {
        foreach (var name in _cache.Keys.ToList())
            _cache[name] = await GenerateAsync(name);

        IconsRecolored?.Invoke(null, EventArgs.Empty);
    }

    private static Task<Icon> GenerateAsync(string name)
    {
        int size = SizeOverrides.GetValueOrDefault(name, DefaultSize);
        var c = SettingsManager.FontColor;
        return Icon.GenerateAsync(Manifest[name], size, size, (int)(c.Red * 255), (int)(c.Green * 255), (int)(c.Blue * 255));
    }
}
