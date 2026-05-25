using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Sts2KeyboardControllerBridge;

public sealed partial class KeyboardBridgeProbe : Node
{
    private NControllerManager? _controllerManager;
    private bool _activated;

    public void ActivateAfterManualAdd()
    {
        if (_activated)
            return;

        _activated = true;
        ProcessMode = ProcessModeEnum.Always;
        SetProcessInput(true);
        SetProcessUnhandledInput(true);
        SetProcessUnhandledKeyInput(true);
        GetWindow().WindowInput += OnWindowInput;
        Logger.Log($"[KeyboardBridgeProbe] ActivateAfterManualAdd; insideTree={IsInsideTree()} path={GetPath()} WindowInput/_Input/_UnhandledInput enabled. F10/F11/F12 remain diagnostics.");
        ModEntry.ProbeEnteredTree();
    }

    public override void _Ready()
    {
        Logger.Log($"[KeyboardBridgeProbe] _Ready callback observed; insideTree={IsInsideTree()} path={GetPath()}");
    }

    public override void _ExitTree()
    {
        try
        {
            GetWindow().WindowInput -= OnWindowInput;
        }
        catch (Exception e)
        {
            Logger.Log($"[KeyboardBridgeProbe] _ExitTree WindowInput detach failed: {e}");
        }
    }

    public override void _EnterTree()
    {
        Logger.Log($"[KeyboardBridgeProbe] _EnterTree; parent={GetParent()?.Name}");
        ModEntry.ProbeEnteredTree();
    }

    public override void _Input(InputEvent inputEvent)
    {
        HandleInputEvent(inputEvent, "_Input");
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        HandleInputEvent(inputEvent, "_UnhandledInput");
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        HandleInputEvent(inputEvent, "_UnhandledKeyInput");
    }

    private void OnWindowInput(InputEvent inputEvent)
    {
        HandleInputEvent(inputEvent, "WindowInput");
    }

    private void HandleInputEvent(InputEvent inputEvent, string source)
    {
        if (inputEvent is not InputEventKey key || key.Echo)
            return;

        if (key.Pressed && HandleDiagnosticKey(key, source))
            return;

        var bridgeAction = KeyboardActionMap.FromKeyEvent(key);
        if (bridgeAction == null)
            return;

        if (!CanInjectBridgeAction(bridgeAction, key.Pressed, out var reason))
        {
            Logger.Log($"[KeyboardBridgeProbe] {source} suppressed {key.Keycode} physical={key.PhysicalKeycode} label={key.KeyLabel} -> {bridgeAction.ActionName}; reason={reason}");
            GetViewport()?.SetInputAsHandled();
            return;
        }

        _controllerManager ??= FindControllerManager(GetTree().Root);
        if (bridgeAction.ActivatesFocusMode && key.Pressed && _controllerManager != null)
            ControllerFocusActivator.TryActivate(_controllerManager);

        InjectGameAction(bridgeAction.ActionName, key.Pressed);
        Logger.Log($"[KeyboardBridgeProbe] {source} {(key.Pressed ? "pressed" : "released")} {key.Keycode} physical={key.PhysicalKeycode} label={key.KeyLabel} -> {bridgeAction.ActionName}");
        GetViewport()?.SetInputAsHandled();
    }

    private bool HandleDiagnosticKey(InputEventKey key, string source)
    {
        switch (key.Keycode)
        {
            case Key.F10:
                Logger.Log($"[KeyboardBridgeProbe] {source} F10 pressed: mod input node is alive. physical={key.PhysicalKeycode} label={key.KeyLabel}");
                GetViewport()?.SetInputAsHandled();
                return true;

            case Key.F11:
                Logger.Log($"[KeyboardBridgeProbe] {source} F11 pressed: injecting ui_cancel diagnostic. physical={key.PhysicalKeycode} label={key.KeyLabel}");
                InjectGameAction("ui_cancel", pressed: true);
                InjectGameAction("ui_cancel", pressed: false);
                GetViewport()?.SetInputAsHandled();
                return true;

            case Key.F12:
                _controllerManager ??= FindControllerManager(GetTree().Root);
                Logger.Log($"[KeyboardBridgeProbe] {source} F12 pressed: controller manager found = {_controllerManager != null}. physical={key.PhysicalKeycode} label={key.KeyLabel}");
                if (_controllerManager != null)
                {
                    ControllerFocusActivator.TryActivate(_controllerManager);
                    InjectGameAction("ui_down", pressed: true);
                    InjectGameAction("ui_down", pressed: false);
                    Logger.Log("[KeyboardBridgeProbe] F12 injected ui_down after focus activation.");
                }
                GetViewport()?.SetInputAsHandled();
                return true;

            default:
                return false;
        }
    }

    private bool CanInjectBridgeAction(BridgeAction bridgeAction, bool pressed, out string reason)
    {
        reason = string.Empty;

        if (bridgeAction.ActionName != "mega_view_exhaust_pile_and_tab_right")
            return true;

        if (!pressed)
            return true;

        var exhaustPileButton = FindNode<NExhaustPileButton>(GetTree().Root);
        if (exhaustPileButton == null)
            return true;

        if (!exhaustPileButton.Visible || !exhaustPileButton.IsEnabled)
        {
            reason = $"exhaust pile unavailable visible={exhaustPileButton.Visible} enabled={exhaustPileButton.IsEnabled}";
            return false;
        }

        return true;
    }

    private static T? FindNode<T>(Node node) where T : Node
    {
        if (node is T typed)
            return typed;

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                var found = FindNode<T>(childNode);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private static NControllerManager? FindControllerManager(Node node)
    {
        if (node is NControllerManager manager)
            return manager;

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                var found = FindControllerManager(childNode);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private static void InjectGameAction(string actionName, bool pressed)
    {
        Godot.Input.ParseInputEvent(new InputEventAction
        {
            Action = actionName,
            Pressed = pressed
        });
    }
}
