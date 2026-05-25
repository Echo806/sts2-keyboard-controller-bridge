using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2KeyboardControllerBridge;

[ModInitializer("Initialize")]
public static class ModEntry
{
    public const string ModId = "Sts2KeyboardControllerBridge";
    public const string ModName = "STS2 Keyboard Controller Bridge";

    // Keep strong references for C# objects used by deferred Godot calls/signals.
    private static KeyboardBridgeProbe? PendingProbe;
    private static SceneTree? Tree;
    private static Node? Root;
    private static Action? ProcessFrameHandler;

    public static void Initialize()
    {
        try
        {
            Tree = (SceneTree)Godot.Engine.GetMainLoop();
            Root = Tree.Root;
            PendingProbe = new KeyboardBridgeProbe();
            ProcessFrameHandler = InstallPendingProbe;
            Tree.ProcessFrame += ProcessFrameHandler;
            PersistentKeyboardMappingPatch.Start(Tree);
            Logger.Log($"[{ModName}] loaded; KeyboardBridgeProbe install scheduled on next ProcessFrame. rootChildCount={Root.GetChildCount()}");
        }
        catch (Exception e)
        {
            Logger.Log($"[{ModName}] initialization failed: {e}");
        }
    }

    private static void InstallPendingProbe()
    {
        try
        {
            if (Tree != null && ProcessFrameHandler != null)
                Tree.ProcessFrame -= ProcessFrameHandler;
            ProcessFrameHandler = null;

            if (Root == null || PendingProbe == null)
            {
                Logger.Log($"[{ModName}] ProcessFrame install skipped; root={Root != null} pendingProbe={PendingProbe != null}");
                return;
            }

            Root.AddChild(PendingProbe);
            Logger.Log($"[{ModName}] KeyboardBridgeProbe AddChild on ProcessFrame complete. rootChildCount={Root.GetChildCount()}");
            PendingProbe.ActivateAfterManualAdd();
        }
        catch (Exception e)
        {
            Logger.Log($"[{ModName}] ProcessFrame AddChild failed: {e}");
        }
    }

    public static void ProbeEnteredTree()
    {
        PendingProbe = null;
        Logger.Log($"[{ModName}] KeyboardBridgeProbe entered tree; released pending reference.");
    }
}

public static class Logger
{
    private static readonly string LogPath;

    static Logger()
    {
        LogPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "SlayTheSpire2", "logs", "mod_log.txt"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
    }

    public static void Log(string message)
    {
        File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
    }
}
