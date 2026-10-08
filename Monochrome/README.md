# Monochrome

Monochrome 是面向 tModLoader 内容模组的图形与特效基础库，主要服务于 Fargo's Souls Phantasm Mode。

## 从哪读起

| 想了解 | 读 |
|---|---|
| 怎么把一个特效写出来（最短路径） | 本文件 |
| 图形系统每个部件的作用、原理、调用顺序、性能红线、已知缺口 | [`Core/Graphics/README.md`](Core/Graphics/README.md) |
| 核心层：子系统生命周期、事件总线、对象池、确定性随机、时钟与调度、日志 | [`Core/README.md`](Core/README.md) |
| 物理模块：绳索求解器、参数指南、这一版做什么不做什么 | [`Core/Physics/README.md`](Core/Physics/README.md) |
| 蠕虫段链：布局描述、走链导航、体节跟随与头部移动 | [`Content/Worms/README.md`](Content/Worms/README.md) |
| 状态 / Buff 系统：层数、每实体数据、实体级钩子、图标角标 | [`Content/Status/README.md`](Content/Status/README.md) |
| 整个库的设计蓝图：分层、模块清单、优先级、工程规范、反模式 | [`docs/构建蓝图.md`](docs/构建蓝图.md) |
| 数学与几何工具 | [`Common/MonoUtil/`](Common/MonoUtil/)（每类都有 XML 注释，IDE 里直接看） |

游戏内的诊断命令都挂在 `/mono` 下，只读的那些随时可用，会改状态的（重编着色器、染色自检）只在开发期生效，见本文末尾。
核心层另外有 `/mono core`（子系统、事件总线、时钟、调度器、日志，以及用 `alloc` 实测每帧分配），见 [`Core/README.md`](Core/README.md)。

## 图形入口

> **图形系统的完整说明在 [`Core/Graphics/README.md`](Core/Graphics/README.md)**（每个部件的作用与原理、每个方法做什么、一帧里的调用顺序、性能红线与已知缺口）。本节只给最短的上手路径。

图形 API 位于 `Monochrome.Core.Graphics`：

```csharp
using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Metaballs;
using Monochrome.Core.Graphics.Particles;
using Monochrome.Core.Graphics.PostProcessing;
using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.RenderTargets;
using Monochrome.Core.Graphics.Screen;
using Monochrome.Core.Graphics.Shaders;
```

### 坐标空间：每个绘制调用都要显式声明

`MonoGraphicsSpace` 同时决定两件事：**是否减去 `Main.screenPosition`**，以及**用哪个投影矩阵**。每个绘制调用都把它显式传进去。

| 空间 | 是否减 `screenPosition` | 投影矩阵 |
|---|---|---|
| `World`（默认） | 是 | 含 `Main.GameViewMatrix.Zoom` 与反转重力翻转 |
| `Screen` | 否 | 不含缩放的正交投影 |
| `UI` | 否 | 同上，再先乘 `Main.UIScaleMatrix` |

```csharp
// 图元绘制是绕过 SpriteBatch 的原始 GPU 绘制，必须落在 End() 与下一次 Begin() 之间。
// 需要自己管理批次状态时用这些助手，不要手拼 Begin 的一堆参数。
Main.spriteBatch.PrepareForShaders();      // 或 ResetToDefault() / ResetToDefaultUI()
MonoPrim.DrawCircle(npc.Center, 32f, Color.Cyan, 2f);                       // World
MonoPrim.DrawDashedLine(a, b, 12f, 6f, Color.Red, 2f, MonoGraphicsSpace.Screen);
MonoPrim.DrawBezier(controlPoints, Color.White, 1.5f, 32);
MonoPrim.DrawCapsule(a, b, 8f, Color.Orange);                               // 实心
MonoPrim.DrawGradientRect(pos, size, tl, tr, bl, br);
MonoPrim.FillPolygon(points, Color.Cyan * 0.4f);                            // 凹凸均可
Main.spriteBatch.ResetToDefault(end: false);
```

### 顶点契约（写图元着色器时看这里）

`MonoPrimitiveVertex` 的第三分量是**附加数据位**（不采样贴图，三分量全部留给着色器）：

- **X** —— 沿形状的长度/角度参数 `t ∈ [0,1]`
- **Y** —— **截面坐标 `[0,1]`，`0.5` 永远是截面正中**。拖尾：左缘 0 → 右缘 1；圆/圆环：圆心或内径 0 → 边缘或外径 1
- **Z** —— **Y 从 0 走到 1 跨越的真实像素距离**（条带全宽 / 环厚 / 半径）。于是 `Y * Z` 就是"距截面 0 侧边缘的像素数"

归一化坐标只知道"在截面的百分之几"，不知道实际多宽；想让柔边恒定 3 像素、虚线在像素上等长，就必须用 Z。

```csharp
// 拖尾：整条轨迹一次组装、一次缓冲上传、一次绘制
MonoPrim.RenderTrail(oldPositions, new MonoPrimitiveSettings(
    WidthFunction: t => MathHelper.Lerp(12f, 1f, t),
    ColorFunction: t => Color.Cyan * (1f - t),
    Smoothen: true));
```

### RenderTarget：池化 vs 受管

```csharp
// 帧内短租：Dispose 即归还。注意专用服务器上返回 null，必须判空。
using MonoRenderTargetPool.Lease? lease = MonoRt.Rent(Main.screenWidth / 2, Main.screenHeight / 2);
if (lease is not null)
{
    RenderTarget2D rt = lease.Target;
    // 在这里画到 rt。MonoPostFxPipeline 负责 ping-pong 与状态恢复；
    // MonoScreenCapture.Apply(pipeline, rt) 则负责"捕获 → 过链 → 回贴"（原地处理，安全）。
}

// 跨帧长期持有 + 跟随分辨率：首次访问时创建，分辨率变化时自动重建，长期未用自动释放。
var screenTarget = MonoRt.ScreenSized();
MonoRenderTargetManager.RenderTargetUpdateLoop += () =>
{
    screenTarget.SwapToRenderTarget(Color.Transparent);
};
```

### 着色器

```csharp
MonoShader glow = MonoShaderManager.GetShader("Monochrome.Glow");
glow.SetParameter("uIntensity", 1.5f)
    .SetCommonParameters()
    .Apply();          // pass 会被缓存；名字查不到时退回第 0 个 pass
```

着色器源码放 `Assets/Effects/**/*.fx`，构建时由 `Tools/CompileMonochromeShaders.ps1` 用**随包携带**的 `fxc`（`Assets/AutoloadedEffects/Compiler/`）编译成同名 `.fxc`。运行时加载 `.fxc`；`.fx` **也进包**，因为运行时热重载要靠它重编（代价约 8 KB）。Monochrome 在 `PostSetupContent` 自动注册所有 `.fxc`；缺失或编译失败只记警告，不阻止模组加载。可用 `MonochromeShaderCompiler` 覆盖构建期编译器路径，`MonochromeShaderCompile=false` 关闭这一步。

改完 `.fx` **不用重启游戏也不用手动编译**：

```
/mono shader status                                  # 列出注册名、装入时间、资产路径
/mono shader reload                                  # 全部重载
/mono shader reload Monochrome.StandardPrimitive     # 重载单个
/mono shader reload all                              # 强制全部重编（忽略 mtime 预筛）
/mono capture status                                 # 屏幕后处理链的注册情况与捕获申请状态
/mono capture test on                                # 自检：把整个世界染成暖色（界面不受影响）
/mono capture fx                                     # 列出 6 个内置屏幕特效与各自的着色器状态
/mono capture fx vignette on                         # 单独开关一条（用"一眼能看出来"的演示参数）
```

它编到临时文件 → 构造成功**才**替换正在用的 `Effect` → 最后原子写回源目录的 `.fxc`。所以**编译失败只会回一行错误，旧特效原样保留**，不会让画面上的东西消失。

第一个可用资产是**图元默认着色器模板**：`MonoShaderManager.GetShader("Monochrome.StandardPrimitive")`，pass 名 `AutoloadPass`，贴图绑在**采样槽 1**（槽 0 留给 SpriteBatch）。它的 `debugMode ≥ 0.5` 会输出顶点契约可视化（R = 截面、G = 像素跨度 / 64、B = 长度参数），可用来肉眼验证契约；`useTexture` 默认 0，即纯顶点色。

> **只改 `.fx` 而不重新编译 `.fxc`，等于什么都没改。** 产物必须与源一起提交。两条路都能重编：构建（自动重编比 `.fxc` 新的 `.fx`，`-Force` 强制全编）或游戏里 `/mono shader reload`（会顺带把 `.fxc` 写回源目录）。

### 其它

```csharp
// Metaball：提交即可。累积（OnPreDraw）与合成（On_Main.DrawDust）都是自动的，不需要手动画。
// 一次性数据：层在每帧 PreUpdateWorld 会被清空，所以要在世界更新相位提交。
MonoMetaballManager.Add("boss", new MonoMetaball(npc.Center, 48f, Color.White));

// 有行为的球：派生 MonoMetaballInstance，覆写 Update / ShouldKill / DrawField，
// 用 LayerName 声明归属后 Spawn() 不必传参——之后每帧由管理器推进、回收、合成。
new EmberBlob(npc.Center).Spawn();         // 两种提交可以混在同一层里一起融合

// 可见半径恒等于你给的 Radius；阈值只决定"多早开始搭桥"。
MonoMetaballManager.Settings.Threshold = 0.5f;   // 默认：两球中心距 <= 3R 就连起来
MonoMetaballManager.Settings.EdgeColor = Color.White * 0.8f;

MonoScreenEffectsSystem.StartShake(0.6f);
MonoScreenEffectsSystem.StartHitstop(3);            // 世界真的停 3 帧；输入 / 绘制 / 界面照常
MonoScreenEffectsSystem.WorldTimeScale = 0.35f;     // 慢动作：按比例跳过世界更新帧（0 = 完全停）

// 粒子：默认 World 空间，按 (混合状态, 空间) 自动合批，绘制挂在 On_Main.DrawDust。
// 想要界面侧粒子，就在派生类里覆写 `Space => MonoGraphicsSpace.UI`，
// 管理器会自动把它改到界面相位（PostDrawInterface）画。
new FlameParticle().Spawn();

// 后处理：链本身只负责 ping-pong（两块中间目标，pass 按 Order 升序）；
// "捕获 → 过链 → 回贴"由 MonoScreenCapture 一步做完。
// 想让链自动作用到整个世界画面上，注册一次即可——之后看链里有没有启用的 pass 自动开关。
MonoPostFxPipeline pipeline = MonoPostFxSystem.GetOrCreate("Monochrome.Main");
MonoScreenFx.AddTo(pipeline, "colorgrade", order: 20);   // 内置：blur / colorgrade / chromatic / scanline / grain / vignette
pipeline.Add(new MonoVignettePass { Strength = 0.6f });  // 或者直接手搓一条
MonoScreenCaptureSystem.Register(pipeline);
// 自检：/mono capture test on（整幅染色，验证通路）；/mono capture fx <名字> on（逐条验证特效）
```

**屏幕后处理的作用范围是「世界 + 世界内界面」，不含 HUD / 聊天 / 战斗文字 / 光标**——因为原版在 `FilterManager.EndCapture`（`Main.cs:60145`）之后才开始画界面，而那正是唯一的合法插入点。想让特效连 UI 一起处理需要另一条完全不同的路由，本库不做。

### 相机跟随（默认关闭）

```csharp
// 平滑跟随 + 死区 + 前瞻。只替换原版的"跟随基准"，平移 / 原版相机修饰器 / 别的模组的贡献全部保留。
MonoCameraFollow.DeadZone = new Vector2(80f, 56f);   // 目标在这个矩形内时相机完全不动
MonoCameraFollow.Smoothing = 0.12f;                  // 越小越黏
MonoCameraFollow.LookAhead = 0.12f;                  // 按速度提前把相机推出去（秒）
MonoCameraFollow.FollowPlayer();                     // 或 Follow(某个世界坐标点)，Stop() 停止
```

边界钳制与整像素取整由原版在钩子之后完成，不需要自己做。**"速度自适应缩放"不在其中**：`PlayerInput.SetZoom_World()` 在相机钩子之后才跑，在那里写 zoom 会被同一帧覆盖（见图形文档 §5.10）。

## 诊断命令与开发期开关

```
/mono shader status                                  # 列出注册名、装入时间、资产路径
/mono capture status                                 # 屏幕后处理链的注册情况与捕获申请状态
/mono capture fx                                     # 列出内置屏幕特效与各自的着色器状态
```

上面三条**只读**，任何情况下都能用。下面这些会改动状态——热重载会把编译产物写回磁盘，自检会把整个画面染色——所以**只在开发期生效**：

```
/mono shader reload [名字|all|*]
/mono capture test [on|off]
/mono capture fx <名字|all> on|off
```

"开发期"指**有调试器附加**（从 IDE 启动游戏，或给进程附加调试器）。之所以不用编译配置当判据，是因为 tML 构建模组时默认就是 Debug 配置，那样等于永远放行。

没有调试器又需要用到它们时，设环境变量 `MONOCHROME_DEBUG=1` 再启动游戏即可（`MonoDebug.OverrideVariable`）。被拒绝时命令会直接把这段说明回给你。

