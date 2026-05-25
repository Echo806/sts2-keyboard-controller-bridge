using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Sts2KeyboardControllerBridge;

public static class PersistentKeyboardMappingPatch
{
    private static SceneTree? _tree;
    private static Action? _processFrameHandler;
    private static int _attempts;
    private static bool _applied;

    public static void Start(SceneTree tree)
    {
        _tree = tree;
        _attempts = 0;
        _applied = false;
        _processFrameHandler = TryApplyOnFrame;
        _tree.ProcessFrame += _processFrameHandler;
        Logger.Log("[PersistentKeyboardMappingPatch] scheduled; will persistently clear all native keyboard shortcuts to Key.None.");
    }

    private static void TryApplyOnFrame()
    {
        if (_applied)
            return;

        _attempts++;
        try
        {
            var inputManager = NInputManager.Instance;
            if (inputManager == null)
            {
                RetryOrGiveUp("NInputManager.Instance is null");
                return;
            }

            var field = typeof(NInputManager).GetField("_keyboardInputMap", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Stop("_keyboardInputMap field not found");
                return;
            }

            if (field.GetValue(inputManager) is not Dictionary<StringName, Key> map || map.Count == 0)
            {
                RetryOrGiveUp("_keyboardInputMap not initialized yet");
                return;
            }

            var changed = 0;
            foreach (var action in map.Keys.ToList())
            {
                if (map[action] != Key.None)
                {
                    map[action] = Key.None;
                    changed++;
                }
            }

            var saveMethod = typeof(NInputManager).GetMethod("SaveKeyboardInputMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            if (saveMethod == null)
            {
                Stop("SaveKeyboardInputMapping method not found");
                return;
            }

            saveMethod.Invoke(inputManager, null);
            var disabled = map.Count(kvp => kvp.Value == Key.None);
            Logger.Log($"[PersistentKeyboardMappingPatch] applied and saved. attempts={_attempts} changed={changed} disabled={disabled} total={map.Count}");
            Stop(null);
        }
        catch (Exception e)
        {
            RetryOrGiveUp(e.ToString());
        }
    }

    private static void RetryOrGiveUp(string reason)
    {
        if (_attempts == 1 || _attempts == 30 || _attempts == 120)
            Logger.Log($"[PersistentKeyboardMappingPatch] waiting; attempts={_attempts}; reason={reason}");

        if (_attempts >= 600)
            Stop($"gave up after {_attempts} frames; last reason={reason}");
    }

    private static void Stop(string? reason)
    {
        _applied = reason == null;
        if (_tree != null && _processFrameHandler != null)
            _tree.ProcessFrame -= _processFrameHandler;
        _processFrameHandler = null;
        if (reason != null)
            Logger.Log($"[PersistentKeyboardMappingPatch] stopped without applying: {reason}");
    }
}
