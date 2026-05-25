#!/usr/bin/env python3
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
map_src = (root / "src" / "KeyboardActionMap.cs").read_text()
probe_src = (root / "src" / "KeyboardBridgeProbe.cs").read_text()
entry_src = (root / "src" / "ModEntry.cs").read_text()
patch_src = (root / "src" / "PersistentKeyboardMappingPatch.cs").read_text()

expected_cases = {
    'Key.W': 'ui_up',
    'Key.S': 'ui_down',
    'Key.A': 'ui_left',
    'Key.D': 'ui_right',
    'Key.I': 'ui_accept',
    'Key.L': 'ui_cancel',
    'Key.K': 'ui_select',
    'Key.J': 'mega_top_panel',
    'Key.F': 'mega_view_draw_pile',
    'Key.G': 'mega_view_discard_pile',
    'Key.H': 'mega_view_exhaust_pile_and_tab_right',
    'Key.B': 'mega_view_deck_and_tab_left',
    'Key.M': 'mega_view_map',
    'Key.P': 'mega_pause_and_back',
}

errors = []
for key, action in expected_cases.items():
    pattern = re.compile(rf"\b{re.escape(key)}\b[^=\n]*=>\s*new BridgeAction\(\"{re.escape(action)}\"")
    if not pattern.search(map_src):
        errors.append(f"missing mapping {key} -> {action}")

for legacy_key in ['Key.Enter', 'Key.KpEnter', 'Key.Space', 'Key.Escape', 'Key.Backspace', 'Key.Tab']:
    if legacy_key in map_src:
        errors.append(f"legacy MVP key should not be primary controller mapping anymore: {legacy_key}")

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
