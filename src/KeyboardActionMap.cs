using Godot;

namespace Sts2KeyboardControllerBridge;

public sealed record BridgeAction(string ActionName, bool ActivatesFocusMode = true);

public static class KeyboardActionMap
{
    public static BridgeAction? FromKeyEvent(InputEventKey key)
    {
        if (key.CtrlPressed || key.AltPressed || key.MetaPressed)
            return null;

        return key.Keycode switch
        {
            // Xbox D-pad / left stick navigation
            Key.W or Key.Up => new BridgeAction("ui_up"),
            Key.S or Key.Down => new BridgeAction("ui_down"),
            Key.A or Key.Left => new BridgeAction("ui_left"),
            Key.D or Key.Right => new BridgeAction("ui_right"),

            // Xbox face buttons: Y/B/A/X
            Key.I => new BridgeAction("ui_accept"),
            Key.L => new BridgeAction("ui_cancel"),
            Key.K => new BridgeAction("ui_select"),
            Key.J => new BridgeAction("mega_top_panel"),

            // Xbox shoulders/triggers
            Key.F => new BridgeAction("mega_view_draw_pile"),
            Key.G => new BridgeAction("mega_view_discard_pile"),
            Key.H => new BridgeAction("mega_view_exhaust_pile_and_tab_right"),
            Key.B => new BridgeAction("mega_view_deck_and_tab_left"),

            // Xbox View/Back and Menu/Start
            Key.M => new BridgeAction("mega_view_map", ActivatesFocusMode: false),
            Key.P => new BridgeAction("mega_pause_and_back", ActivatesFocusMode: false),

            _ => null,
        };
    }
}
