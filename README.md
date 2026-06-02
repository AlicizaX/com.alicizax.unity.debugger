# Aliciza X Debugger

`com.alicizax.unity.debugger` 提供两套运行时调试能力：

- `AlicizaX.Debugger.DebuggerComponent`：基于 UI Toolkit 的运行时调试面板，用于查看日志、系统信息、场景信息、Profiler、运行时内存、对象池、引用池、Audio、Timer 等信息，也支持注册自定义调试窗口。
- `AlicizaX.Console.AlicizaXConsoleUITK`：基于 UI Toolkit 的命令控制台，用于输入、补全、执行 `[Command]` 命令，支持宏、命令历史、异步命令、外部脚本和 Unity 日志拦截。

包内 `Debugger.prefab` 已同时挂载 `UIDocument`、`AlicizaXConsoleUITK` 和 `DebuggerComponent`，推荐直接使用该 prefab 接入。

## 快速接入

1. 将 `Packages/com.alicizax.unity.debugger/Debugger.prefab` 放入启动场景。
2. 确认场景中存在框架根节点，并且 `DebuggerComponent` 能注册到 `AppServices.App`。
3. 运行后：
   - `DebuggerComponent` 默认显示浮动 FPS 按钮，双击按钮打开完整调试面板，拖动按钮可移动位置。
   - `AlicizaXConsoleUITK` 默认不会启动时打开，按 prefab 上配置的快捷键 `Alt + Tab` 可切换命令控制台。

也可以手动创建 GameObject 并添加：

- `UIDocument`
- `AlicizaX.Console.AlicizaXConsoleUITK`
- `AlicizaX.Debugger.DebuggerComponent`

`DebuggerComponent` 会在运行时创建或复用 `EventSystem`，并创建自己的 `PanelSettings` 实例。若未指定 `PanelSettings`，会尝试从 `Resources/DebuggerPanelSettings` 读取，读取失败则创建默认配置。

## Debugger 面板

`DebuggerComponent` 命名空间为 `AlicizaX.Debugger`，核心服务接口为 `IDebuggerService`。

```csharp
using AlicizaX;
using AlicizaX.Debugger;

IDebuggerService debugger = AppServices.Require<IDebuggerService>();
debugger.ActiveWindow = true;

DebuggerComponent.Instance.ShowFullWindow = true;
DebuggerComponent.Instance.WindowOpacity = 0.9f;
DebuggerComponent.Instance.WindowScale = 1.2f;
```

### 激活模式

`DebuggerComponent` Inspector 中的 `Active Window` 对应：

```csharp
public enum DebuggerActiveWindowType : byte
{
    AlwaysOpen = 0,
    OnlyOpenWhenDevelopment,
    OnlyOpenInEditor,
    AlwaysClose,
}
```

含义：

| 值 | 行为 |
| --- | --- |
| `AlwaysOpen` | 始终启用调试器 |
| `OnlyOpenWhenDevelopment` | 仅 `Debug.isDebugBuild` 为 `true` 时启用 |
| `OnlyOpenInEditor` | 仅 Unity Editor 中启用 |
| `AlwaysClose` | 始终关闭 |

### 面板操作

- 双击浮动 FPS 按钮：打开完整窗口。
- 拖动浮动 FPS 按钮：移动按钮位置。
- 开启 `EnableFloatingToggleSnap`：按钮会吸附到屏幕边缘。
- 拖动窗口标题栏：移动完整窗口。
- 拖动窗口右下角：调整完整窗口大小。
- 点击窗口 `Reset`：恢复默认布局。
- 点击窗口 `Close`：关闭完整窗口，保留浮动按钮。

布局会写入 `PlayerPrefs`，字段包括浮动按钮位置、窗口位置、窗口大小、窗口缩放和吸附开关。

### 内置窗口

`DebuggerComponent.Start()` 会注册以下窗口路径：

- `Console`
- `Information/System`
- `Information/Environment`
- `Information/Screen`
- `Information/Graphics`
- `Information/Input`
- `Information/Other/Scene`
- `Information/Other/Time`
- `Information/Other/Quality`
- `Profiler/Summary`
- `Profiler/Memory/Summary`
- `Profiler/Memory/All`
- `Profiler/Memory/Texture`
- `Profiler/Memory/Mesh`
- `Profiler/Memory/Material`
- `Profiler/Memory/Shader`
- `Profiler/Memory/AnimationClip`
- `Profiler/Memory/AudioClip`
- `Profiler/Memory/Font`
- `Profiler/Memory/TextAsset`
- `Profiler/Memory/ScriptableObject`
- `Profiler/Object Pool`
- `Profiler/Reference Pool`
- `Profiler/Audio`
- `Profiler/Timer`
- `Other/Settings`

可通过路径直接选中窗口：

```csharp
using AlicizaX.Debugger;

DebuggerComponent.Instance.SelectDebuggerWindow("Profiler/Timer");
```

### 日志面板

`DebuggerComponent` 的内置 `Console` 窗口监听 `Application.logMessageReceived`，缓存最近日志并显示：

- Info / Warning / Error / Fatal 计数
- 类型过滤开关
- 锁定滚动
- 选中日志的堆栈信息
- 复制选中日志
- 清空日志

读取最近日志：

```csharp
using System.Collections.Generic;
using AlicizaX.Debugger;
using UnityEngine;

public sealed class DebugLogExportExample
{
    private readonly List<DebuggerComponent.LogNode> _logs = new();

    public void DumpRecentLogs()
    {
        DebuggerComponent debugger = DebuggerComponent.Instance;
        if (debugger == null)
        {
            return;
        }

        _logs.Clear();
        debugger.GetRecentLogs(_logs, 50);

        foreach (DebuggerComponent.LogNode log in _logs)
        {
            Debug.Log($"{log.LogType}: {log.LogMessage}");
        }
    }
}
```

`LogNode` 来自框架 `MemoryPool`。通过 `GetRecentLogs` 获取到的是只读引用，调用方不要手动释放。

### 注册自定义窗口

自定义窗口实现 `IDebuggerWindow`，并返回一个 UI Toolkit `VisualElement`。

```csharp
using AlicizaX.Debugger;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PlayerDebugWindow : IDebuggerWindow
{
    private Label _label;

    public void Initialize(params object[] args)
    {
    }

    public void Shutdown()
    {
    }

    public void OnEnter()
    {
        Refresh();
    }

    public void OnLeave()
    {
    }

    public void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        Refresh();
    }

    public VisualElement CreateView()
    {
        VisualElement root = new VisualElement();
        _label = new Label();
        root.Add(_label);
        Refresh();
        return root;
    }

    private void Refresh()
    {
        if (_label != null)
        {
            _label.text = $"Frame: {Time.frameCount}";
        }
    }
}
```

注册和注销：

```csharp
using AlicizaX.Debugger;
using UnityEngine;

public sealed class RegisterPlayerDebugWindow : MonoBehaviour
{
    private void Start()
    {
        DebuggerComponent.Instance.RegisterDebuggerWindow(
            "Gameplay/Player",
            new PlayerDebugWindow());
    }

    private void OnDestroy()
    {
        if (DebuggerComponent.Instance != null)
        {
            DebuggerComponent.Instance.UnregisterDebuggerWindow("Gameplay/Player");
        }
    }
}
```

路径使用 `/` 分组，会显示为侧边栏树形菜单。不要和内置路径重复，否则注册时会抛出异常。

### 布局控制

```csharp
using AlicizaX.Debugger;
using UnityEngine;

public sealed class DebuggerLayoutExample : MonoBehaviour
{
    private void Start()
    {
        DebuggerComponent debugger = DebuggerComponent.Instance;
        if (debugger == null)
        {
            return;
        }

        debugger.IconRect = new Rect(24f, 24f, 180f, 56f);
        debugger.WindowRect = new Rect(24f, 96f, 1320f, 760f);
        debugger.EnableFloatingToggleSnap = true;
        debugger.WindowScale = 1.2f;
        debugger.ResetLayout();
    }
}
```

## AlicizaX Console 命令控制台

`AlicizaXConsoleUITK` 命名空间为 `AlicizaX.Console`，用于执行反射扫描到的命令。它与 `DebuggerComponent` 的日志 `Console` 窗口不是同一个功能：前者可输入命令，后者只展示 Unity 日志。

### 控制台快捷键

prefab 默认配置：

| 操作 | 默认值 |
| --- | --- |
| 打开 / 关闭控制台 | `Alt + Tab` |
| 提交命令 | `Return` |
| 选择下一条建议 | `Tab` |
| 选择上一条建议 | `Shift + Tab` |
| 上一条历史命令 | `UpArrow` |
| 下一条历史命令 | `DownArrow` |
| 放大 | `Ctrl + =` |
| 缩小 | `Ctrl + -` |
| 拖动控制台 | `Shift + Mouse0` |
| 取消正在执行的 action | `Ctrl + C` |

这些快捷键都可以在 `AlicizaXConsoleUITK` Inspector 中修改。

### 程序调用

通过单例或路由器调用：

```csharp
using AlicizaX.Console;

AlicizaXConsoleUITK console = AlicizaXConsoleUITK.Instance;
console.Activate();
console.InvokeCommand("help");
console.LogToConsole("custom message");
console.ClearConsole();

AlicizaXConsoleRouter.ActiveConsole?.LogToConsole("route message");
```

不依赖 UI 时，可以直接调用命令处理器：

```csharp
using AlicizaX.Console;

object result = AlicizaXConsoleProcessor.InvokeCommand("command-count");
```

### 命令扫描程序集

`AlicizaXConsoleProcessor` 默认只扫描程序集 `AlicizaX.Debugger`。

如果自定义命令写在其他 asmdef 中，需要在 `AlicizaXConsoleUITK` 的 `Command Assembly Names` 中加入你的程序集名，例如：

```text
AlicizaX.Debugger
Game.Runtime
Game.DebugCommands
```

也可以在代码中手动生成命令表：

```csharp
using AlicizaX.Console;

AlicizaXConsoleProcessor.GenerateCommandTableFromAssemblyNames(
    new[] { "AlicizaX.Debugger", "Game.Runtime" },
    deployThread: true,
    forceReload: true);
```

### 添加命令

静态方法命令：

```csharp
using AlicizaX.Console;
using UnityEngine;

public static class GameDebugCommands
{
    [Command("give-gold", "增加金币")]
    public static string GiveGold(int amount)
    {
        // PlayerModel.Gold += amount;
        return $"Give gold: {amount}";
    }

    [Command("teleport-player", "传送玩家")]
    public static void TeleportPlayer(Vector3 position)
    {
        Debug.Log($"Teleport to {position}");
    }
}
```

控制台输入：

```text
give-gold 100
teleport-player 1,2,3
```

带参数说明：

```csharp
using AlicizaX.Console;

public static class MatchCommands
{
    [Command("set-round")]
    [CommandDescription("设置当前回合数")]
    public static void SetRound(
        [CommandParameterDescription("目标回合，从 1 开始")] int round)
    {
        // ...
    }
}
```

字段和属性也可以加 `[Command]`，字段会自动生成读写命令，只读字段只生成读取命令：

```csharp
using AlicizaX.Console;

public static class DebugSettings
{
    [Command("god-mode", "读取或设置无敌模式")]
    public static bool GodMode;

    [Command("spawn-rate", "读取或设置刷怪倍率")]
    public static float SpawnRate { get; set; } = 1f;
}
```

控制台输入：

```text
god-mode
god-mode true
spawn-rate
spawn-rate 2.5
```

### MonoBehaviour 命令

非静态 `MonoBehaviour` 命令会按 `MonoTargetType` 查找目标实例。

```csharp
using AlicizaX.Console;
using UnityEngine;

public sealed class PlayerController : MonoBehaviour
{
    [Command("damage-player", "扣除玩家生命", MonoTargetType.Single)]
    public void Damage(int value)
    {
        // Hp -= value;
    }

    [Command("heal-all-players", "治疗所有玩家", MonoTargetType.All)]
    public void Heal(int value)
    {
        // Hp += value;
    }
}
```

常用 `MonoTargetType`：

| 类型 | 行为 |
| --- | --- |
| `Single` | 查找场景中第一个激活的目标组件 |
| `All` | 查找场景中所有激活的目标组件 |
| `Registry` | 使用 `AlicizaXConsoleRegistry` 注册的实例，非 `MonoBehaviour` 实例只支持这种方式 |
| `Singleton` | 自动创建并复用单例实例 |
| `SingleInactive` | 查找第一个目标组件，包含未激活对象 |
| `AllInactive` | 查找所有目标组件，包含未激活对象 |
| `Argument` | 第一个命令参数指定目标实例 |
| `ArgumentMulti` | 第一个命令参数指定目标实例数组 |

注册普通对象到 Registry：

```csharp
using AlicizaX.Console;

public sealed class DebugCommandOwner
{
    [Command("reload-config", MonoTargetType.Registry)]
    public void ReloadConfig()
    {
        // ...
    }
}

public static class DebugCommandBootstrap
{
    private static readonly DebugCommandOwner Owner = new();

    public static void Register()
    {
        AlicizaXConsoleRegistry.RegisterObject(Owner);
    }

    public static void Deregister()
    {
        AlicizaXConsoleRegistry.DeregisterObject(Owner);
    }
}
```

### 参数格式

命令按空格拆分参数，但会保留 `""`、`()`, `[]`, `{}` 和 `<>` 中的空格与分隔符。

常用输入格式：

| 类型 | 示例 |
| --- | --- |
| `string` | `"hello world"` |
| `int` / `float` / `bool` | `10`、`1.5`、`true` |
| `Vector2` / `Vector3` / `Vector4` | `1,2`、`1,2,3`、`1,2,3,4` |
| `Vector2Int` / `Vector3Int` | `1,2`、`1,2,3` |
| `Quaternion` | `0,0,0,1` |
| `Color` | `red` 或颜色解析器支持的颜色值 |
| 数组 | `[1,2,3]` |
| `GameObject` | 对象名 |
| `Component` | 组件所在对象或组件解析器支持的输入 |
| `Type` | `int`、`Vector3`、`List<int>`、完整类型名 |

泛型命令使用 `<T>`：

```text
some-generic-command<int> 1
some-generic-command<UnityEngine.Vector3> 1,2,3
```

类型解析默认命名空间包含：

- `System`
- `System.Collections`
- `System.Collections.Generic`
- `UnityEngine`
- `UnityEngine.UI`
- `AlicizaX.Debugger`
- `AlicizaX.Console`

可在控制台中管理命名空间：

```text
all-namespaces
use-namespace Game.Runtime
remove-namespace Game.Runtime
reset-namespaces
```

### 宏

宏以 `#` 引用。除 `#define` 本身外，输入命令会先经过宏展开。

```text
#define p Player
all-macros
remove-macro p
clear-macros
dump-macros "C:/Temp/console-macros.txt"
load-macros "C:/Temp/console-macros.txt"
```

定义后，`#p` 会替换为宏内容。宏名不能包含空白、换行和 `#`。

### 外部命令脚本

每行一条命令：

```text
help
commands
max-fps 60
time-scale 1
```

运行：

```text
qc-script-extern "C:/Temp/debug-commands.txt"
```

WebGL 平台不支持外部文件命令。

### 内置命令

常用命令：

| 命令 | 说明 |
| --- | --- |
| `help` | 显示基础帮助 |
| `man <command>` / `manual <command>` | 查看指定命令手册 |
| `commands` / `all-commands` | 列出所有命令 |
| `user-commands` | 列出用户命令 |
| `command-count` | 查看已加载命令数量 |
| `clear` | 清空命令控制台输出 |
| `verbose-errors` | 读取或设置详细错误输出 |
| `verbose-logging` | 读取或设置详细日志阈值 |
| `logging-level` | 读取或设置 Unity 日志拦截阈值 |
| `max-logs` | 读取或设置最大存储日志数 |
| `register-object<T>` | 注册对象到 Registry |
| `deregister-object<T>` | 从 Registry 移除对象 |
| `display-registry<T>` | 显示 Registry 内容 |
| `clear-registry<T>` | 清空 Registry |
| `#define` | 定义宏 |
| `all-macros` | 列出宏 |
| `remove-macro` | 移除宏 |
| `clear-macros` | 清空宏 |
| `dump-macros` / `load-macros` | 导出 / 导入宏 |
| `all-namespaces` | 查看类型解析命名空间 |
| `use-namespace` | 添加类型解析命名空间 |
| `remove-namespace` | 移除类型解析命名空间 |
| `reset-namespaces` | 重置类型解析命名空间 |

运行时工具命令：

| 命令 | 说明 |
| --- | --- |
| `quit` | 退出应用 |
| `max-fps` | 设置 `Application.targetFrameRate` |
| `vsync` | 开关垂直同步 |
| `msaa` | 设置 MSAA |
| `time-scale` | 设置 `Time.timeScale` |
| `active-scene` | 查看当前活动场景 |
| `all-scenes` | 列出构建中的场景 |
| `loaded-scenes` | 列出已加载场景 |
| `load-scene` / `load-scene-index` | 加载场景 |
| `unload-scene` / `unload-scene-index` | 卸载场景 |
| `set-active-scene` | 设置活动场景 |
| `current-resolution` | 查看当前分辨率 |
| `set-resolution` | 设置分辨率 |
| `supported-resolutions` | 列出支持的分辨率 |
| `fullscreen` | 开关全屏 |
| `screen-dpi` | 查看屏幕 DPI |
| `screen-orientation` | 设置屏幕方向 |
| `capture-screenshot` | 截屏 |
| `get-scene-hierarchy` | 查看场景层级 |
| `get-object-info` | 查看对象信息 |
| `set-active` | 设置对象启用状态 |
| `teleport` / `teleport-relative` | 移动对象 |
| `rotate` | 旋转对象 |
| `set-parent` | 设置父节点 |
| `add-component` / `destroy-component` | 添加 / 销毁组件 |
| `instantiate` | 实例化对象 |
| `destroy` | 销毁对象 |
| `send-message` | 向对象发送消息 |
| `call-static` / `call-instance` | 通过反射调用方法 |

## API 速查

### Debugger

| API | 说明 |
| --- | --- |
| `DebuggerComponent.Instance` | 当前调试器组件 |
| `DebuggerComponent.ActiveWindow` | 是否启用调试器 |
| `DebuggerComponent.ShowFullWindow` | 是否显示完整窗口 |
| `DebuggerComponent.WindowOpacity` | 窗口透明度，范围 `0.2` 到 `1` |
| `DebuggerComponent.WindowScale` | 窗口缩放，最小 `0.5` |
| `DebuggerComponent.CustomFont` | 自定义字体 |
| `DebuggerComponent.EnableFloatingToggleSnap` | 浮动按钮是否吸附边缘 |
| `DebuggerComponent.IconRect` | 浮动按钮位置和尺寸，尺寸固定为默认值 |
| `DebuggerComponent.WindowRect` | 完整窗口位置和尺寸 |
| `DebuggerComponent.ResetLayout()` | 重置布局 |
| `DebuggerComponent.GetRecentLogs(...)` | 获取日志窗口缓存 |
| `DebuggerComponent.RegisterDebuggerWindow(...)` | 注册调试窗口 |
| `DebuggerComponent.UnregisterDebuggerWindow(path)` | 注销调试窗口 |
| `DebuggerComponent.GetDebuggerWindow(path)` | 获取调试窗口 |
| `DebuggerComponent.SelectDebuggerWindow(path)` | 选中调试窗口 |
| `IDebuggerService.ActiveWindow` | 服务层启用状态 |
| `IDebuggerService.DebuggerWindowRoot` | 调试窗口根节点 |

### Console

| API | 说明 |
| --- | --- |
| `AlicizaXConsoleUITK.Instance` | 当前命令控制台实例 |
| `AlicizaXConsoleUITK.Activate()` | 打开控制台 |
| `AlicizaXConsoleUITK.Deactivate()` | 关闭控制台 |
| `AlicizaXConsoleUITK.Toggle()` | 切换控制台 |
| `AlicizaXConsoleUITK.InvokeCommand(command)` | 执行命令并输出结果 |
| `AlicizaXConsoleUITK.LogToConsole(text)` | 写入控制台输出 |
| `AlicizaXConsoleUITK.ClearConsole()` | 清空控制台 |
| `AlicizaXConsoleUITK.InvokeExternalCommandsAsync(path)` | 执行外部命令文件 |
| `AlicizaXConsoleRouter.ActiveConsole` | 当前活动控制台 |
| `AlicizaXConsoleProcessor.GenerateCommandTable(...)` | 生成命令表 |
| `AlicizaXConsoleProcessor.InvokeCommand(command)` | 不经过 UI 执行命令 |
| `AlicizaXConsoleProcessor.GetAllCommands()` | 获取所有命令 |
| `AlicizaXConsoleProcessor.GetUniqueCommands()` | 获取去重后的命令 |
| `AlicizaXConsoleRegistry.RegisterObject(obj)` | 注册 Registry 命令目标 |
| `AlicizaXConsoleRegistry.DeregisterObject(obj)` | 注销 Registry 命令目标 |

## 注意事项

1. `DebuggerComponent` 和 `AlicizaXConsoleUITK` 都使用 UI Toolkit。若挂在同一个 GameObject 上，`DebuggerComponent` 会重建 `UIDocument.rootVisualElement`，可能覆盖同一 `UIDocument` 上已有 UXML。包内 prefab 保持这种组合时，`AlicizaXConsoleUITK` 会在启用时先构建自身 UI，`DebuggerComponent` 随后构建调试面板；如需完全独立显示两套 UI，建议将两者拆到不同 GameObject 和不同 `UIDocument`。
2. 生产包建议将 `DebuggerComponent.Active Window` 设置为 `OnlyOpenWhenDevelopment`、`OnlyOpenInEditor` 或 `AlwaysClose`，并将 `AlicizaXConsoleUITK.Supported State` 设置为 `Development`、`Editor` 或 `Never`。
3. 自定义命令所在程序集必须被命令表扫描。最常见做法是在 `AlicizaXConsoleUITK.Command Assembly Names` 中加入你的 asmdef 名称。
4. `[Command]` 别名和 `[CommandPrefix]` 不能包含空格、括号、中括号、大括号和尖括号。
5. 命令重载按“命令名 + 参数数量”区分；同名同参数数量只会注册一个。
6. 默认参数会生成多个可调用重载。例如 `Foo(int a, int b = 1)` 会生成 `foo a` 和 `foo a b` 两种签名。
7. WebGL 不支持后台线程生成命令表，也不支持外部文件命令和宏文件导入导出。
8. `AlicizaXConsoleUITK` 默认会拦截 `Application.logMessageReceivedThreaded`；如不需要，可在 Inspector 关闭 `Intercept Debug Logger`。
