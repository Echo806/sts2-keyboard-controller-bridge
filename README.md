# STS2 Keyboard Controller Bridge

Language: [English](#english) | [中文](#中文)

---

## English

A Slay the Spire 2 mod that maps keyboard keys to controller-style actions, enabling keyboard-only navigation through the game's existing controller focus system.

This project is an early, practical accessibility/QoL bridge. It avoids Harmony runtime patching and instead uses a Godot `Node` input listener that injects STS2/Godot action events.

### Features

- Keyboard-to-controller-style navigation.
- WASD and arrow keys for focus movement.
- IJKL as controller face-button style controls.
- F/G/H/B for pile and tab controls.
- M for map.
- P for pause/settings/back.
- Persistent clearing of STS2's native keyboard shortcuts to avoid conflicts.
- Guard for the H/exhaust-pile action so pressing H in combat with no exhaust pile does not crash the game.
- Diagnostic keys and log output for troubleshooting.

### Current keymap

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

### Important behavior: native keyboard shortcuts are cleared

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

### Installation

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

### Troubleshooting

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

### Building from source

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

### Packaging a release zip

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

### Implementation overview

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

### Current limitations

- This is an early usable build, not a complete polished accessibility mod.
- It relies on STS2 internal action names and private fields, so game updates may break it.
- It intentionally clears native keyboard shortcuts globally.
- Some screens may still have controller-focus quirks inherited from STS2 itself.

### License

MIT. See `LICENSE`.

---

## 中文

这是一个《杀戮尖塔 2》（Slay the Spire 2）模组，用来把键盘按键映射成游戏现有的手柄操作，让玩家可以用“类似 Xbox 手柄”的方式进行全键盘导航。

这是一个早期但已经可以初步使用的无障碍 / QoL 桥接模组。它不依赖 Harmony 运行时补丁，而是通过 Godot `Node` 监听键盘输入，并注入 STS2/Godot 的 action 事件。

### 功能

- 用键盘模拟手柄式焦点导航。
- WASD 和方向键移动 UI 焦点。
- IJKL 对应手柄面键式操作。
- F/G/H/B 对应牌堆查看和左右切页。
- M 打开地图。
- P 打开暂停 / 设置 / 返回。
- 启动时持久化清空 STS2 原生键盘快捷键，避免与模组键位冲突。
- 对 H / 消耗堆 action 加保护：战斗中没有消耗牌时按 H 不会导致游戏崩溃。
- 保留诊断按键和日志，方便排查问题。

### 当前键位

| 键盘 | 手柄语义 | STS2/Godot action | 说明 |
|---|---|---|---|
| W / ↑ | 十字键 / 摇杆上 | `ui_up` | 焦点上移 |
| S / ↓ | 十字键 / 摇杆下 | `ui_down` | 焦点下移 |
| A / ← | 十字键 / 摇杆左 | `ui_left` | 焦点左移 |
| D / → | 十字键 / 摇杆右 | `ui_right` | 焦点右移 |
| I | Xbox Y / Confirm | `ui_accept` | 确认 / 继续 / 结束回合 |
| L | Xbox B / Cancel | `ui_cancel` | 取消 / 返回 |
| K | Xbox A / Select | `ui_select` | 选择当前焦点卡牌 / 按钮 / 敌人 |
| J | Xbox X / Top panel | `mega_top_panel` | 顶部面板 |
| F | LT | `mega_view_draw_pile` | 查看抽牌堆 |
| G | RT | `mega_view_discard_pile` | 查看弃牌堆 |
| H | RB | `mega_view_exhaust_pile_and_tab_right` | 查看消耗堆 / 向右切页 |
| B | LB | `mega_view_deck_and_tab_left` | 查看牌组 / 向左切页 |
| M | View/Back | `mega_view_map` | 地图 |
| P | Menu/Start | `mega_pause_and_back` | 暂停 / 设置 / 返回 |

诊断按键：

| 按键 | 行为 |
|---|---|
| F10 | 只写日志，用来确认模组输入节点能收到键盘输入 |
| F11 | 注入 `ui_cancel` |
| F12 | 激活手柄焦点模式并注入 `ui_down` |

### 重要行为：会清空游戏原生键盘快捷键

本模组会通过 STS2 自己的 `SaveKeyboardInputMapping()` 保存路径，把游戏内置的键盘快捷键持久化设置为 `Key.None`。

原因：

- STS2 原生键盘快捷键会和本模组的桥接键位冲突。
- 游戏设置界面不方便把某个绑定留空。
- 使用“占位键”并不优雅，因为多个绑定不能很好地共用同一个无用键。

效果：

- 每次模组启动后，都会把 STS2 原生键盘快捷键表里的所有项目保存为 `None`。
- 这会影响共享的 STS2 `settings.save`，不只是某一个 run 存档。
- 如果之后卸载模组并想恢复原生快捷键，需要在游戏内重置键位，或者删除 / 重新生成 `settings.save`。

Linux 下设置文件通常在：

```text
~/.local/share/SlayTheSpire2/steam/<steam_id>/settings.save
```

### 安装

1. 从 Releases 下载发布包，例如：

```text
Sts2KeyboardControllerBridge-v0.2.0.zip
```

2. 解压。

3. 把解压出来的 `Sts2KeyboardControllerBridge` 文件夹复制到游戏的 `mods` 目录。

最终结构应该是：

```text
Slay the Spire 2/
└── mods/
    └── Sts2KeyboardControllerBridge/
        ├── mod_manifest.json
        └── Sts2KeyboardControllerBridge.dll
```

Linux Steam 常见路径：

```text
~/.steam/steam/steamapps/common/Slay the Spire 2/mods/Sts2KeyboardControllerBridge/
```

Windows Steam 常见路径：

```text
C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\Sts2KeyboardControllerBridge\
```

4. 启动《杀戮尖塔 2》。

5. 如果游戏有模组启用界面，请启用 `STS2 Keyboard Controller Bridge`。

### 故障排查

Linux 下模组日志路径：

```text
~/.config/SlayTheSpire2/logs/mod_log.txt
```

正常情况下可以看到类似日志：

```text
[STS2 Keyboard Controller Bridge] loaded; KeyboardBridgeProbe install scheduled on next ProcessFrame...
[KeyboardBridgeProbe] ActivateAfterManualAdd; insideTree=True ... WindowInput/_Input/_UnhandledInput enabled...
[PersistentKeyboardMappingPatch] applied and saved. ... disabled=25 total=25
```

如果模组看起来已经加载，但按键无效：

1. 在游戏里按 F10。
2. 查看 `mod_log.txt`。
3. 如果 F10 有日志，说明输入节点能收到键盘输入，问题更可能在 action 注入或焦点模式。
4. 如果 F10 没有日志，检查模组目录结构，以及游戏是否加载了 DLL。

游戏发现 / 加载模组的证据通常在：

```text
~/.local/share/SlayTheSpire2/logs/godot.log
```

可以搜索类似内容：

```text
Found mod manifest file .../mods/Sts2KeyboardControllerBridge/mod_manifest.json
Loading assembly DLL .../Sts2KeyboardControllerBridge.dll
Calling initializer method ... ModEntry
Finished mod initialization ...
```

### 从源码构建

前置要求：

- .NET SDK 10，或与当前游戏版本兼容的 SDK。
- Python 3，用于运行静态验证脚本。
- 把本地《杀戮尖塔 2》的程序集复制到 `references/`：
  - `sts2.dll`
  - `GodotSharp.dll`

不要分发游戏自带程序集。详见 `references/README.md`。

构建：

```sh
python3 tests/verify_keyboard_bridge.py
dotnet build src/Sts2KeyboardControllerBridge.csproj -c Release
```

使用 Nix：

```sh
nix shell nixpkgs#python3 --command python3 tests/verify_keyboard_bridge.py
nix shell nixpkgs#dotnet-sdk_10 --command dotnet build src/Sts2KeyboardControllerBridge.csproj -c Release --no-restore
```

构建产物位置：

```text
src/bin/Release/Sts2KeyboardControllerBridge.dll
```

### 打包发布 zip

在仓库根目录运行：

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

zip 内部应该包含这个顶层文件夹：

```text
Sts2KeyboardControllerBridge/
├── mod_manifest.json
└── Sts2KeyboardControllerBridge.dll
```

### 实现概览

主要文件：

| 文件 | 作用 |
|---|---|
| `src/ModEntry.cs` | STS2 模组入口。通过 `SceneTree.ProcessFrame` 安装桥接节点，并启动持久化键位设置补丁。也包含日志工具。 |
| `src/KeyboardActionMap.cs` | 定义键盘按键到 STS2/Godot action 名称的映射。 |
| `src/KeyboardBridgeProbe.cs` | Godot `Node`，订阅 `WindowInput`，捕获按键按下/释放，激活手柄焦点模式，并注入 `InputEventAction`。 |
| `src/ControllerFocusActivator.cs` | 在导航 / 选择类 action 注入前激活 STS2 的 controller/focus 模式。 |
| `src/PersistentKeyboardMappingPatch.cs` | 反射访问 `NInputManager._keyboardInputMap`，把所有原生键盘快捷键设为 `Key.None`，再调用 STS2 私有方法 `SaveKeyboardInputMapping()` 保存。 |
| `src/Sts2KeyboardControllerBridge.json` | 模组 manifest 源文件，打包时复制为 `mod_manifest.json`。 |
| `tests/verify_keyboard_bridge.py` | 静态回归检查脚本，用来验证映射和关键实现约束。 |

为什么不用 Harmony：

- 早期实验发现 Harmony/MonoMod native detour 在当前 Linux/NixOS 运行环境里会失败。
- 当前主路径使用不依赖 Harmony 的 Godot 输入节点方案。

### 当前限制

- 这是早期可用版本，不是完整打磨过的无障碍模组。
- 它依赖 STS2 内部 action 名称和私有字段，游戏更新后可能失效。
- 它会有意清空游戏原生键盘快捷键，而且是全局设置。
- 部分界面可能仍然存在 STS2 自身的 controller focus 行为限制。

### 许可证

MIT。见 `LICENSE`。
