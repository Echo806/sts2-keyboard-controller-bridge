using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace Sts2KeyboardControllerBridge;

public static class ControllerFocusActivator
{
    private static readonly PropertyInfo? IsUsingControllerProp =
        typeof(NControllerManager).GetProperty("IsUsingController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private static readonly FieldInfo? LastMouseField =
        typeof(NControllerManager).GetField("_lastMousePosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    public static void TryActivate(NControllerManager controller)
    {
        try
        {
            if (IsUsingControllerProp != null)
            {
                var current = IsUsingControllerProp.GetValue(controller);
                if (current is bool currentBool && currentBool)
                    return;

                IsUsingControllerProp.SetValue(controller, true);
            }

            var viewport = controller.GetViewport();
            if (viewport != null)
            {
                var mousePos = DisplayServer.MouseGetPosition();
                var windowPos = DisplayServer.WindowGetPosition();
                var localMouse = new Vector2(mousePos.X - windowPos.X, mousePos.Y - windowPos.Y);
                LastMouseField?.SetValue(controller, localMouse);
                viewport.WarpMouse(Vector2.One * -1000f);
            }

            ActiveScreenContext.Instance.FocusOnDefaultControl();
            controller.EmitSignal("ControllerDetected");
            Logger.Log("[ControllerFocusActivator] controller/focus mode activated.");
        }
        catch (Exception e)
        {
            Logger.Log($"[ControllerFocusActivator] activation failed: {e}");
        }
    }
}
