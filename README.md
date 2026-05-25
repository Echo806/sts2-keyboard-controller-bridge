# STS2 Keyboard Controller Bridge

A Slay the Spire 2 mod that maps keyboard keys to controller-style actions, enabling keyboard-only navigation through the game's existing controller focus system.

This project is an early, practical accessibility/QoL bridge. It avoids Harmony runtime patching and instead uses a Godot `Node` input listener that injects STS2/Godot action events.

## Features

- Keyboard-to-controller-style navigation.
- WASD and arrow keys for focus movement.
- IJKL as controller face-button style controls.
- F/G/H/B for pile and tab controls.
- M for map.
- P for pause/settings/back.
- Persistent clearing of STS2's native keyboard shortcuts to avoid conflicts.
- Guard for the H/exhaust-pile action so pressing H in combat with no exhaust pile does not crash the game.
- Diagnostic keys and log output for troubleshooting.

## Current keymap

| Keyboard | Controller semantic | STS2/Godot action | Notes |
|---|---|---|---|
| W / Up | D-pad / stick up | `ui_up` | Move focus up |
| S / Down | D-pad / stick down | `ui_down` | Move focus down |
| A / Left | D-pad / stick left | `ui_left` | Move focus left |
| D / Right | D-pad / stick right | `ui_right` | Move focus right |
| I | Xbox Y / Confirm | `ui_accept` | Proceed / confirm / end turn |
| L | Xbox B / Cancel | `ui_cancel` | Cancel / back |
| K | Xbox A / Select | `ui_select` | Select focused card/button/enemy |
| J | Xbox X / Top panel | `mega_top_panel` | Top panel |
| F | LT | `mega_view_draw_pile` | View draw pile |
| G | RT | `mega_view_discard_pile` | View discard pile |
| H | RB | `mega_view_exhaust_pile_and_tab_right` | View exhaust pile / tab right |
| B | LB | `mega_view_deck_and_tab_left` | View deck / tab left |
| M | View/Back | `mega_view_map` | Map |
| P | Menu/Start | `mega_pause_and_back` | Pause / settings / back |

Diagnostic keys:

| Key | Behavior |
|---|---|
| F10 | Log-only; proves the mod input node receives keyboard input |
| F11 | Injects `ui_cancel` |
| F12 | Activates controller focus mode and injects `ui_down` |

## Important behavior: native keyboard shortcuts are cleared

This mod persistently clears STS2's built-in keyboard shortcuts by setting the game's native keyboard mapping to `Key.None` through STS2's own `SaveKeyboardInputMapping()` path.

Why:

- STS2's native keyboard shortcuts can conflict with the bridge keys.
- The in-game UI does not provide a convenient way to leave bindings empty.
- Placeholder keys are awkward because bindings cannot cleanly share one unused key.

Effect:

- On mod startup, all entries in STS2's native keyboard shortcut map are saved as `None`.
- This affects the shared STS2 `settings.save`, not just a single run save.
- If you later uninstall the mod and want the original native keyboard shortcuts back, reset controls in-game or regenerate your `settings.save`.

Linux settings path is usually:

```text
~/.local/share/SlayTheSpire2/steam/<steam_id>/settings.save
```

## Installation

1. Download the release zip, for example:

```text
Sts2KeyboardControllerBridge-v0.2.0.zip
```

2. Extract it.

3. Copy the extracted `Sts2KeyboardControllerBridge` folder into the game's `mods` directory.

Expected layout:

```text
Slay the Spire 2/
└── mods/
    └── Sts2KeyboardControllerBridge/
        ├── mod_manifest.json
        └── Sts2KeyboardControllerBridge.dll
```

Linux Steam path is often:

```text
~/.steam/steam/steamapps/common/Slay the Spire 2/mods/Sts2KeyboardControllerBridge/
```

Windows Steam path is often:

```text
C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\Sts2KeyboardControllerBridge\
```

4. Launch Slay the Spire 2.

5. Enable `STS2 Keyboard Controller Bridge` in the game's mod UI if needed.

## Troubleshooting

The mod writes a log file on Linux here:

```text
~/.config/SlayTheSpire2/logs/mod_log.txt
```

Useful expected lines:

```text
[STS2 Keyboard Controller Bridge] loaded; KeyboardBridgeProbe install scheduled on next ProcessFrame...
[KeyboardBridgeProbe] ActivateAfterManualAdd; insideTree=True ... WindowInput/_Input/_UnhandledInput enabled...
[PersistentKeyboardMappingPatch] applied and saved. ... disabled=25 total=25
```

If the mod seems loaded but keys do not work:

1. Press F10 in-game.
2. Check `mod_log.txt`.
3. If F10 logs, the input node is receiving keys; the issue is likely action/focus behavior.
4. If F10 does not log, verify the mod folder layout and whether the game loaded the DLL.

Game discovery/loading evidence is usually in:

```text
~/.local/share/SlayTheSpire2/logs/godot.log
```

Look for lines like:

```text
Found mod manifest file .../mods/Sts2KeyboardControllerBridge/mod_manifest.json
Loading assembly DLL .../Sts2KeyboardControllerBridge.dll
Calling initializer method ... ModEntry
Finished mod initialization ...
```

## Building from source

Prerequisites:

- .NET SDK 10 or compatible SDK for the target game build.
- Python 3 for the static verification script.
- Local Slay the Spire 2 assemblies copied into `references/`:
  - `sts2.dll`
  - `GodotSharp.dll`

Do not redistribute the game assemblies. See `references/README.md`.

Build:

```sh
python3 tests/verify_keyboard_bridge.py
dotnet build src/Sts2KeyboardControllerBridge.csproj -c Release
```

With Nix:

```sh
nix shell nixpkgs#python3 --command python3 tests/verify_keyboard_bridge.py
nix shell nixpkgs#dotnet-sdk_10 --command dotnet build src/Sts2KeyboardControllerBridge.csproj -c Release --no-restore
```

The built DLL will be at:

```text
src/bin/Release/Sts2KeyboardControllerBridge.dll
```

## Packaging a release zip

From the repository root:

```sh
rm -rf dist
mkdir -p dist/Sts2KeyboardControllerBridge

cp src/bin/Release/Sts2KeyboardControllerBridge.dll \
   dist/Sts2KeyboardControllerBridge/Sts2KeyboardControllerBridge.dll

cp src/Sts2KeyboardControllerBridge.json \
   dist/Sts2KeyboardControllerBridge/mod_manifest.json

cd dist
zip -r Sts2KeyboardControllerBridge-v0.2.0.zip Sts2KeyboardControllerBridge
```

The zip should contain this top-level folder:

```text
Sts2KeyboardControllerBridge/
├── mod_manifest.json
└── Sts2KeyboardControllerBridge.dll
```

## Implementation overview

Main files:

| File | Purpose |
|---|---|
| `src/ModEntry.cs` | STS2 mod entry point. Schedules the bridge node on `SceneTree.ProcessFrame` and starts the persistent keyboard settings patch. Also contains the logger. |
| `src/KeyboardActionMap.cs` | Maps keyboard keys to STS2/Godot action names. |
| `src/KeyboardBridgeProbe.cs` | Godot `Node` that subscribes to `WindowInput`, captures key press/release events, activates controller focus mode, and injects `InputEventAction`s. |
| `src/ControllerFocusActivator.cs` | Activates STS2 controller/focus mode before navigation/select actions. |
| `src/PersistentKeyboardMappingPatch.cs` | Reflects into `NInputManager._keyboardInputMap`, sets all native keyboard shortcuts to `Key.None`, then invokes STS2's private `SaveKeyboardInputMapping()` method. |
| `src/Sts2KeyboardControllerBridge.json` | Mod manifest source file; packaged as `mod_manifest.json`. |
| `tests/verify_keyboard_bridge.py` | Static regression checks for mappings and important implementation invariants. |

Why no Harmony:

- Early experiments showed Harmony/MonoMod native detour failures on this Linux/NixOS runtime.
- The current mod uses a no-Harmony Godot input-node approach for the main path.

## Current limitations

- This is an early usable build, not a complete polished accessibility mod.
- It relies on STS2 internal action names and private fields, so game updates may break it.
- It intentionally clears native keyboard shortcuts globally.
- Some screens may still have controller-focus quirks inherited from STS2 itself.

## License

Choose a license before publishing. MIT is a common choice for small mods.
