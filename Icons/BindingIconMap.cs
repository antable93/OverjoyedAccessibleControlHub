namespace OverjoyedVersion3;

/// <summary>
/// Maps ActionInput labels (as produced by VirtualInputDevice implementations) to the matching
/// icon name in IconManager, so the inspector can auto-select a fitting icon when a binding is chosen.
/// </summary>
public static class BindingIconMap
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["Xbox A"] = "xbox_button_a",
        ["Xbox B"] = "xbox_button_b",
        ["Xbox X"] = "xbox_button_x",
        ["Xbox Y"] = "xbox_button_y",
        ["Xbox LB"] = "xbox_lb",
        ["Xbox RB"] = "xbox_rb",
        ["Xbox LS Click"] = "xbox_ls",
        ["Xbox RS Click"] = "xbox_rs",
        ["Xbox LS Up"] = "xbox_stick_l_up",
        ["Xbox LS Down"] = "xbox_stick_l_down",
        ["Xbox LS Left"] = "xbox_stick_l_left",
        ["Xbox LS Right"] = "xbox_stick_l_right",
        ["Xbox RS Up"] = "xbox_stick_r_up",
        ["Xbox RS Down"] = "xbox_stick_r_down",
        ["Xbox RS Left"] = "xbox_stick_r_left",
        ["Xbox RS Right"] = "xbox_stick_r_right",
        ["Xbox D-Pad Up"] = "xbox_dpad_up",
        ["Xbox D-Pad Down"] = "xbox_dpad_down",
        ["Xbox D-Pad Left"] = "xbox_dpad_left",
        ["Xbox D-Pad Right"] = "xbox_dpad_right",
        ["Xbox Start"] = "xbox_button_start",
        ["Xbox Back"] = "xbox_button_back",
        ["Xbox Guide"] = "xbox_guide",
        ["Xbox LT"] = "xbox_lt",
        ["Xbox RT"] = "xbox_rt",

        ["Shift"] = "keyboard_shift",
        ["Ctrl"] = "keyboard_ctrl",
        ["Alt"] = "keyboard_alt",
        ["Escape"] = "keyboard_escape",
        ["Backspace"] = "keyboard_backspace",
        ["CapsLock"] = "keyboard_capslock",
        ["Up"] = "keyboard_arrow_up",
        ["Down"] = "keyboard_arrow_down",
        ["Left"] = "keyboard_arrow_left",
        ["Right"] = "keyboard_arrow_right",
        ["Enter"] = "keyboard_enter",
        ["Tab"] = "keyboard_tab",
        ["Space"] = "keyboard_space",
        ["Delete"] = "keyboard_delete",
        ["Insert"] = "keyboard_insert",
        ["Home"] = "keyboard_home",
        ["End"] = "keyboard_end",
        ["Page Up"] = "keyboard_page_up",
        ["Page Down"] = "keyboard_page_down",
        ["Num Lock"] = "keyboard_numlock",
        ["Print Screen"] = "keyboard_printscreen",
        ["F1"] = "keyboard_f1", ["F2"] = "keyboard_f2", ["F3"] = "keyboard_f3", ["F4"] = "keyboard_f4",
        ["F5"] = "keyboard_f5", ["F6"] = "keyboard_f6", ["F7"] = "keyboard_f7", ["F8"] = "keyboard_f8",
        ["F9"] = "keyboard_f9", ["F10"] = "keyboard_f10", ["F11"] = "keyboard_f11", ["F12"] = "keyboard_f12",
    };

    static BindingIconMap()
    {
        foreach (var c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            Map[c.ToString()] = $"keyboard_{char.ToLowerInvariant(c)}";

        Map["WASD"] = "keyboard_wasd";
        Map["Arrow Keys"] = "keyboard_arrows_all";
    }

    public static bool TryGetIcon(string label, out string iconName) => Map.TryGetValue(label, out iconName!);
}
