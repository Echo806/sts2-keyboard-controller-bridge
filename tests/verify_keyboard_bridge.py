#!/usr/bin/env python3
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
map_src = (root / "src" / "KeyboardActionMap.cs").read_text()
probe_src = (root / "src" / "KeyboardBridgeProbe.cs").read_text()
entry_src = (root / "src" / "ModEntry.cs").read_text()
patch_src = (root / "src" / "PersistentKeyboardMappingPatch.cs").read_text()

expected_default_bindings = {
    'ui_up': ['Key.W', 'Key.Up'],
    'ui_down': ['Key.S', 'Key.Down'],
    'ui_left': ['Key.A', 'Key.Left'],
    'ui_right': ['Key.D', 'Key.Right'],
    'ui_accept': ['Key.I'],
    'ui_cancel': ['Key.L'],
    'ui_select': ['Key.K'],
    'mega_top_panel': ['Key.J'],
    'mega_view_draw_pile': ['Key.F'],
    'mega_view_discard_pile': ['Key.G'],
    'mega_view_exhaust_pile_and_tab_right': ['Key.H'],
    'mega_view_deck_and_tab_left': ['Key.B'],
    'mega_view_map': ['Key.M'],
    'mega_pause_and_back': ['Key.P'],
}

errors = []
if 'System.Text.Json' not in map_src:
    errors.append('KeyboardActionMap must use System.Text.Json to load user keybinds.json')
if 'keybinds.json' not in map_src:
    errors.append('KeyboardActionMap must look for keybinds.json next to the mod DLL')
if 'AppContext.BaseDirectory' not in map_src:
    errors.append('KeyboardActionMap must resolve keybinds.json from AppContext.BaseDirectory')
if 'DefaultBindings' not in map_src:
    errors.append('KeyboardActionMap must keep built-in default bindings when config is missing or invalid')
if 'Logger.Log' not in map_src:
    errors.append('KeyboardActionMap must log config load/fallback problems')
if 'Enum.TryParse<Key>' not in map_src:
    errors.append('KeyboardActionMap must parse Godot Key names from JSON strings')
if 'ActivatesFocusMode: false' not in map_src:
    errors.append('KeyboardActionMap must preserve no-focus behavior for map and pause actions')

sample_path = root / 'keybinds.example.json'
if not sample_path.exists():
    errors.append('repository must include keybinds.example.json for users to copy/edit')
else:
    sample_text = sample_path.read_text()
    for action, keys in expected_default_bindings.items():
        if f'"{action}"' not in sample_text:
            errors.append(f'keybinds.example.json missing action {action}')
        for key in keys:
            user_key = key.removeprefix('Key.')
            if f'"{user_key}"' not in sample_text:
                errors.append(f'keybinds.example.json missing default key {user_key} for {action}')

for action, keys in expected_default_bindings.items():
    if f'"{action}"' not in map_src:
        errors.append(f'missing default action {action}')
    for key in keys:
        user_key = key.removeprefix('Key.')
        if f'"{user_key}"' not in map_src:
            errors.append(f'missing default key {user_key} for {action}')

if 'var bridgeAction = KeyboardActionMap.FromKeyEvent(key);' not in probe_src:
    errors.append('KeyboardBridgeProbe must route normal keys through KeyboardActionMap, not only F10/F11/F12 diagnostics')
if 'bridgeAction.ActivatesFocusMode && key.Pressed' not in probe_src:
    errors.append('KeyboardBridgeProbe must activate controller/focus mode before mapped key presses')
if 'InjectGameAction(bridgeAction.ActionName, key.Pressed)' not in probe_src:
    errors.append('KeyboardBridgeProbe must inject mapped action press/release events')
if 'GetWindow().WindowInput += OnWindowInput' not in probe_src:
    errors.append('KeyboardBridgeProbe must also subscribe to WindowInput so keys are seen before Controls consume _Input')
if 'SetProcess(true)' in probe_src or 'NativeKeyboardShortcutSuppressor' in probe_src:
    errors.append('KeyboardBridgeProbe must not contain the old runtime native shortcut suppressor; persistent settings patch lives separately')
if 'CanInjectBridgeAction' not in probe_src or 'NExhaustPileButton' not in probe_src or 'mega_view_exhaust_pile_and_tab_right' not in probe_src:
    errors.append('KeyboardBridgeProbe must guard H/exhaust-pile injection when the exhaust pile button is unavailable')
if 'exhaustPileButton == null)\n            return true;' not in probe_src:
    errors.append('KeyboardBridgeProbe must allow H/RB outside combat screens where NExhaustPileButton does not exist')
if 'exhaust pile unavailable' not in probe_src:
    errors.append('KeyboardBridgeProbe must log when H/exhaust-pile injection is suppressed')
if 'override void _EnterTree()' not in probe_src or 'ProbeEnteredTree()' not in probe_src:
    errors.append('KeyboardBridgeProbe must log _EnterTree and notify ModEntry after deferred insertion')

if 'KeyboardBridgeHooks.Initialize(harmony)' in entry_src:
    errors.append('ModEntry should not require Harmony hooks for the main keyboard bridge path')
if 'PendingProbe' not in entry_src:
    errors.append('ModEntry must keep a strong reference while deferred root insertion is pending')
if 'ProcessFrame += ProcessFrameHandler' not in entry_src:
    errors.append('ModEntry must schedule probe installation on SceneTree.ProcessFrame, after STS2 root initialization')
if 'Root.AddChild(PendingProbe)' not in entry_src:
    errors.append('ModEntry must AddChild the pending probe from the ProcessFrame callback')
if 'ActivateAfterManualAdd()' not in entry_src:
    errors.append('ModEntry must explicitly activate the probe after AddChild because Godot virtual callbacks did not fire in this STS2 mod context')
if 'install scheduled on next ProcessFrame' not in entry_src or 'AddChild on ProcessFrame complete' not in entry_src:
    errors.append('ModEntry must log ProcessFrame scheduling and AddChild completion evidence')
if 'root.AddChild(probe)' in entry_src or 'GetPath()' in entry_src or 'CallDeferred(Node.MethodName.AddChild, PendingProbe)' in entry_src:
    errors.append('ModEntry must not direct-add during initialization or rely on CallDeferred; STS2 did not run the deferred add here')
if 'PersistentKeyboardMappingPatch.Start(Tree)' not in entry_src:
    errors.append('ModEntry must start the persistent keyboard mapping patch on load')

if 'SaveKeyboardInputMapping' not in patch_src or 'BindingFlags.Instance | BindingFlags.NonPublic' not in patch_src:
    errors.append('PersistentKeyboardMappingPatch must call NInputManager private SaveKeyboardInputMapping via reflection')
if 'Key.None' not in patch_src:
    errors.append('PersistentKeyboardMappingPatch must persistently clear keyboard shortcuts to Key.None')
if 'PreservedCardSelect' in patch_src or 'Key.Key1' in patch_src or 'Key.Key0' in patch_src:
    errors.append('PersistentKeyboardMappingPatch must no longer preserve number-card selection; all native keyboard shortcuts should be None for now')

if errors:
    print('FAIL')
    for e in errors:
        print('-', e)
    raise SystemExit(1)

print('PASS keyboard bridge mapping/static checks')
