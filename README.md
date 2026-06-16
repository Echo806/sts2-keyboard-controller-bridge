# STS2 Keyboard Controller Bridge

Language: [English](#english) | [中文](#中文)

---

## English

A Slay the Spire 2 mod that maps keyboard keys to controller-style actions, enabling keyboard-only navigation through the game's existing controller focus system.



### Features

- Keyboard-to-controller-style navigation.
- WASD and arrow keys for focus movement.
- IJKL as controller face-button style controls.
- F/G/H/B for pile and tab controls.
- M for map.
- P for pause/settings/back.
- User-editable `keybinds.json` next to the mod DLL; no rebuild required for key changes.
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

### Custom keybinds

The mod looks for an optional `keybinds.json` file next to `Sts2KeyboardControllerBridge.dll` / `mod_manifest.json`.

For a normal install, place it here:

```text
Slay the Spire 2/
└── mods/
    └── Sts2KeyboardControllerBridge/
        ├── mod_manifest.json
        ├── Sts2KeyboardControllerBridge.dll
        └── keybinds.json
```

Edit the key names in keybinds.json. Example:

```json
{
  "ui_up": ["W", "Up"],
  "ui_down": ["S", "Down"],
  "ui_left": ["A", "Left"],
  "ui_right": ["D", "Right"],

  "ui_accept": ["Enter", "I"],
  "ui_cancel": ["Escape", "L"],
  "ui_select": ["Space", "K"],
  "mega_top_panel": ["J"],

  "mega_view_draw_pile": ["F"],
  "mega_view_discard_pile": ["G"],
  "mega_view_exhaust_pile_and_tab_right": ["H"],
  "mega_view_deck_and_tab_left": ["B"],

  "mega_view_map": ["M"],
  "mega_pause_and_back": ["P"]
}
```

Notes:

- Restart the game after editing `keybinds.json`.
- Missing actions keep their built-in default keys.
- To unbind an action, set it to an empty list, for example: `"mega_top_panel": []`.
- Key names are Godot `Key` enum names without the `Key.` prefix. Examples: `Enter`, `KpEnter`, `Space`, `Escape`, `Tab`, `Up`, `Down`, `Left`, `Right`, `Key1`, `F1`.
- Invalid action names or invalid key names are ignored and written to the mod log.

### Attention: native keyboard shortcuts are cleared

This mod persistently clears STS2's built-in keyboard shortcuts by setting the game's native keyboard mapping to `Key.None` through STS2's own `SaveKeyboardInputMapping()` path.

Why:

- STS2's native keyboard shortcuts can conflict with the bridge keys.
- The in-game UI does not provide a convenient way to leave bindings empty.
- Placeholder keys are awkward because bindings cannot cleanly share one unused key.

Effect:

- On mod startup, all entries in STS2's native keyboard shortcut map are saved as `None`.
- This affects the shared STS2 `settings.save`, not just a single run save.
- If you later uninstall the mod and want the original native keyboard shortcuts back, reset controls in-game or regenerate your `settings.save`.





### Installation

1. Download the release zip, for example:

```text
Sts2KeyboardControllerBridge-v0.3.0.zip
```

2. Extract it.

3. Copy the extracted `Sts2KeyboardControllerBridge` folder into the game's `mods` directory.

Expected layout:

```text
Slay the Spire 2/
└── mods/
    └── Sts2KeyboardControllerBridge/
        ├── mod_manifest.json
        ├── Sts2KeyboardControllerBridge.dll
        └── keybinds.json  
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

### License

MIT. See `LICENSE`.

---

## 中文

这是一个《杀戮尖塔 2》（Slay the Spire 2）模组，用来把键盘按键映射成游戏现有的手柄操作，让玩家可以用“类似 Xbox 手柄”的方式进行全键盘导航。



### 功能

- 用键盘模拟手柄式焦点导航。
- WASD 和方向键移动 UI 焦点。
- IJKL 对应手柄面键式操作。
- F/G/H/B 对应牌堆查看和左右切页。
- M 打开地图。
- P 打开暂停 / 设置 / 返回。
- 支持用户编辑 DLL 旁边的 `keybinds.json` 自定义键位，不需要重新编译。
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

：

### 自定义键位

模组会读取一个可选的 `keybinds.json` 文件。正常安装时，把它放在 `Sts2KeyboardControllerBridge.dll` / `mod_manifest.json` 旁边：

```text
Slay the Spire 2/
└── mods/
    └── Sts2KeyboardControllerBridge/
        ├── mod_manifest.json
        ├── Sts2KeyboardControllerBridge.dll
        └── keybinds.json
```

然后在`keybinds.json`编辑按键名。例如：

```json
{
  "ui_up": ["W", "Up"],
  "ui_down": ["S", "Down"],
  "ui_left": ["A", "Left"],
  "ui_right": ["D", "Right"],

  "ui_accept": ["Enter", "I"],
  "ui_cancel": ["Escape", "L"],
  "ui_select": ["Space", "K"],
  "mega_top_panel": ["J"],

  "mega_view_draw_pile": ["F"],
  "mega_view_discard_pile": ["G"],
  "mega_view_exhaust_pile_and_tab_right": ["H"],
  "mega_view_deck_and_tab_left": ["B"],

  "mega_view_map": ["M"],
  "mega_pause_and_back": ["P"]
}
```

说明：

- 修改 `keybinds.json` 后需要重启游戏。
- 没写到 JSON 里的 action 会继续使用内置默认键位。
- 如果想取消某个 action 的绑定，可以设为空数组，例如：`"mega_top_panel": []`。
- 按键名使用 Godot `Key` 枚举名，但不要写 `Key.` 前缀。常见例子：`Enter`, `KpEnter`, `Space`, `Escape`, `Tab`, `Up`, `Down`, `Left`, `Right`, `Key1`, `F1`。
- 无效 action 名或无效按键名会被忽略，并写入模组日志。

### 注意：会清空游戏原生键盘快捷键

本模组会通过 STS2 自己的 `SaveKeyboardInputMapping()` 保存路径，把游戏内置的键盘快捷键持久化设置为 `Key.None`。

原因：

- STS2 原生键盘快捷键会和本模组的桥接键位冲突。
- 游戏设置界面不方便把某个绑定留空。
- 使用“占位键”并不优雅，因为多个绑定不能很好地共用同一个无用键。

效果：

- 每次模组启动后，都会把 STS2 原生键盘快捷键表里的所有项目保存为 `None`。
- 这会影响共享的 STS2 `settings.save`，不只是某一个 run 存档。
- 如果之后卸载模组并想恢复原生快捷键，需要在游戏内重置键位，或者删除 / 重新生成 `settings.save`。



### 安装

1. 从 Releases 下载发布包，例如：

```text
Sts2KeyboardControllerBridge-v0.3.0.zip
```

2. 解压。

3. 把解压出来的 `Sts2KeyboardControllerBridge` 文件夹复制到游戏的 `mods` 目录。

最终结构应该是：

```text
Slay the Spire 2/
└── mods/
    └── Sts2KeyboardControllerBridge/
        ├── mod_manifest.json
        ├── Sts2KeyboardControllerBridge.dll
        └── keybinds.json  
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



### 许可证

MIT。见 `LICENSE`。
