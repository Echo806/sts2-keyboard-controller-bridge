using System.Text.Json;
using Godot;

namespace Sts2KeyboardControllerBridge;

public sealed record BridgeAction(string ActionName, bool ActivatesFocusMode = true);

public static class KeyboardActionMap
{
    private const string ConfigFileName = "keybinds.json";

    private static readonly Dictionary<string, BridgeAction> KnownActions = new()
    {
        ["ui_up"] = new BridgeAction("ui_up"),
        ["ui_down"] = new BridgeAction("ui_down"),
        ["ui_left"] = new BridgeAction("ui_left"),
        ["ui_right"] = new BridgeAction("ui_right"),
        ["ui_accept"] = new BridgeAction("ui_accept"),
        ["ui_cancel"] = new BridgeAction("ui_cancel"),
        ["ui_select"] = new BridgeAction("ui_select"),
        ["mega_top_panel"] = new BridgeAction("mega_top_panel"),
        ["mega_view_draw_pile"] = new BridgeAction("mega_view_draw_pile"),
        ["mega_view_discard_pile"] = new BridgeAction("mega_view_discard_pile"),
        ["mega_view_exhaust_pile_and_tab_right"] = new BridgeAction("mega_view_exhaust_pile_and_tab_right"),
        ["mega_view_deck_and_tab_left"] = new BridgeAction("mega_view_deck_and_tab_left"),
        ["mega_view_map"] = new BridgeAction("mega_view_map", ActivatesFocusMode: false),
        ["mega_pause_and_back"] = new BridgeAction("mega_pause_and_back", ActivatesFocusMode: false),
    };

    private static readonly Dictionary<string, string[]> DefaultBindings = new()
    {
        ["ui_up"] = ["W", "Up"],
        ["ui_down"] = ["S", "Down"],
        ["ui_left"] = ["A", "Left"],
        ["ui_right"] = ["D", "Right"],
        ["ui_accept"] = ["I"],
        ["ui_cancel"] = ["L"],
        ["ui_select"] = ["K"],
        ["mega_top_panel"] = ["J"],
        ["mega_view_draw_pile"] = ["F"],
        ["mega_view_discard_pile"] = ["G"],
        ["mega_view_exhaust_pile_and_tab_right"] = ["H"],
        ["mega_view_deck_and_tab_left"] = ["B"],
        ["mega_view_map"] = ["M"],
        ["mega_pause_and_back"] = ["P"],
    };

    private static readonly Dictionary<Key, BridgeAction> Bindings = LoadBindings();

    public static BridgeAction? FromKeyEvent(InputEventKey key)
    {
        if (key.CtrlPressed || key.AltPressed || key.MetaPressed)
            return null;

        return Bindings.TryGetValue(key.Keycode, out var action) ? action : null;
    }

    private static Dictionary<Key, BridgeAction> LoadBindings()
    {
        var configPath = ResolveConfigPath();
        if (!File.Exists(configPath))
        {
            Logger.Log($"[KeyboardActionMap] {ConfigFileName} not found at {configPath}; using built-in defaults.");
            return BuildBindings(DefaultBindings);
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var configured = JsonSerializer.Deserialize<Dictionary<string, string[]>>(json, new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (configured == null)
            {
                Logger.Log($"[KeyboardActionMap] {ConfigFileName} is empty; using built-in defaults.");
                return BuildBindings(DefaultBindings);
            }

            var merged = MergeWithDefaults(configured);
            var bindings = BuildBindings(merged);
            if (bindings.Count == 0)
            {
                Logger.Log($"[KeyboardActionMap] {ConfigFileName} produced no valid bindings; using built-in defaults.");
                return BuildBindings(DefaultBindings);
            }

            Logger.Log($"[KeyboardActionMap] loaded {bindings.Count} key bindings from {configPath}.");
            return bindings;
        }
        catch (Exception e)
        {
            Logger.Log($"[KeyboardActionMap] failed to load {configPath}; using built-in defaults. error={e}");
            return BuildBindings(DefaultBindings);
        }
    }

    private static string ResolveConfigPath()
    {
        var dllDirectoryPath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        if (File.Exists(dllDirectoryPath))
            return dllDirectoryPath;

        // STS2 may load mod DLLs into a shared runtime directory. In that case,
        // prefer a keybinds.json placed next to the mod_manifest.json folder.
        var manifestDirectory = FindDirectoryContaining("mod_manifest.json", System.Environment.CurrentDirectory);
        return manifestDirectory == null
            ? dllDirectoryPath
            : Path.Combine(manifestDirectory, ConfigFileName);
    }

    private static string? FindDirectoryContaining(string fileName, string startDirectory)
    {
        try
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, fileName);
                if (File.Exists(candidate))
                    return directory.FullName;

                directory = directory.Parent;
            }
        }
        catch (Exception e)
        {
            Logger.Log($"[KeyboardActionMap] failed while searching for {fileName}: {e}");
        }

        return null;
    }

    private static Dictionary<string, string[]> MergeWithDefaults(Dictionary<string, string[]> configured)
    {
        var merged = new Dictionary<string, string[]>(DefaultBindings);
        foreach (var (actionName, keyNames) in configured)
            merged[actionName] = keyNames;

        return merged;
    }

    private static Dictionary<Key, BridgeAction> BuildBindings(Dictionary<string, string[]> config)
    {
        var bindings = new Dictionary<Key, BridgeAction>();

        foreach (var (actionName, keyNames) in config)
        {
            if (!KnownActions.TryGetValue(actionName, out var action))
            {
                Logger.Log($"[KeyboardActionMap] ignoring unknown action '{actionName}'.");
                continue;
            }

            if (keyNames == null)
            {
                Logger.Log($"[KeyboardActionMap] action '{actionName}' has null key list; no keys bound for this action.");
                continue;
            }

            foreach (var keyName in keyNames)
            {
                if (!TryParseKey(keyName, out var parsedKey))
                {
                    Logger.Log($"[KeyboardActionMap] ignoring unknown key '{keyName}' for action '{actionName}'.");
                    continue;
                }

                if (bindings.TryGetValue(parsedKey, out var previous))
                    Logger.Log($"[KeyboardActionMap] key '{parsedKey}' was already bound to '{previous.ActionName}'; overriding with '{actionName}'.");

                bindings[parsedKey] = action;
            }
        }

        return bindings;
    }

    private static bool TryParseKey(string keyName, out Key key)
    {
        key = Key.None;

        if (string.IsNullOrWhiteSpace(keyName))
            return false;

        var normalized = keyName.Trim();
        if (normalized.StartsWith("Key.", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[4..];

        return Enum.TryParse<Key>(normalized, ignoreCase: true, out key) && key != Key.None;
    }
}
