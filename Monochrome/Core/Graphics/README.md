# Monochrome 图形系统

Monochrome 的图形与特效层：图元网格、渲染目标、着色器与热重载、粒子、融合球、屏幕后处理与相机跟随。
代码位于 `Monochrome/Core/Graphics/`，命名空间 `Monochrome.Core.Graphics`。

| 想看什么 | 去哪 |
|---|---|
| 最短的上手路径：怎么把一个特效写出来 | [模组级 README](../../README.md) |
| 整个库的分层、模块清单、工程规范与反模式 | [构建蓝图](../../docs/构建蓝图.md) |
| 每个部件什么时候被谁调用、有哪些坑 | 本文件 |
| 数学与几何工具 | `Common/MonoUtil/`（每类都有 XML 注释） |

## 目录

- [1. 三条不变量](#1-三条不变量)
- [2. 部件地图](#2-部件地图)
- [3. 一帧的时间线](#3-一帧的时间线)
- [4. 部件索引](#4-部件索引)
- [5. 部件参考](#5-部件参考)
  - [5.1 MonoGraphicsSpace —— 坐标空间](#51-monographicspace--坐标空间)
  - [5.2 Primitives —— 图元层](#52-primitives--图元层)
  - [5.3 MonoPrim / MonoRt —— 门面](#53-monoprim--monort--门面)
  - [5.4 MonoSpriteBatchExtensions —— 批次状态](#54-monospritebatchextensions--批次状态)
  - [5.5 Shaders —— 着色器](#55-shaders--着色器)
  - [5.6 RenderTargets —— 渲染目标](#56-rendertargets--渲染目标)
  - [5.7 PostProcessing —— 后处理链](#57-postprocessing--后处理链)
  - [5.8 Particles —— 粒子](#58-particles--粒子)
  - [5.9 Metaballs —— 融合球](#59-metaballs--融合球)
  - [5.10 Screen —— 屏幕特效与相机](#510-screen--屏幕特效与相机)
  - [5.11 MonoGraphicsCommand —— 诊断命令](#511-monographicscommand--诊断命令)
  - [5.12 MonoScreenCapture —— 把管线接到画面上](#512-monoscreencapture--把管线接到画面上)
  - [5.13 MonoShaderPass —— 内置屏幕特效](#513-monoshaderpass--内置屏幕特效)
- [6. 图元层深入：从点列到三角形](#6-图元层深入从点列到三角形)
- [7. 容量、索引与降级](#7-容量索引与降级)
- [8. 性能红线](#8-性能红线)
- [9. 已知缺口与历史](#9-已知缺口与历史)
- [10. 常见错误速查](#10-常见错误速查)
- [附录：Z（像素跨度）验证配方](#附录z像素跨度验证配方)

---

## 1. 三条不变量

整套系统只有三条硬约束。违反任意一条都会产生"看着像玄学"的 bug，所以把它们放在最前面：

| # | 不变量 | 违反后的症状 |
|---|---|---|
| **I** | **坐标空间是单一事实来源**：每个绘制调用显式声明 `MonoGraphicsSpace`，由它唯一决定"是否减 `Main.screenPosition`"与"用哪个投影矩阵" | 分辨率一变、缩放一改，特效就飘；UI 元素画在世界里 |
| **II** | **绕过 `SpriteBatch` 的原始绘制必须落在 `spriteBatch.End()` 与下一次 `Begin()` 之间** | 批处理被打断、精灵错位、画面撕裂 |
| **III** | **GPU 资源必须有明确的所有者与生命周期**（缓冲跟随模组，渲染目标走池或受管） | 显存泄漏；分辨率变化后画面定格；热重载后资源被释放两次 |

#### I. 坐标空间

`MonoGraphicsSpace` 的三个取值同时决定两件事，没有第三处约定：

| 空间 | 是否减 `Main.screenPosition` | 投影矩阵 |
|---|---|---|
| `World`（默认） | **是** | 含 `Main.GameViewMatrix.Zoom` 与反转重力翻转的世界矩阵 |
| `Screen` | 否 | `CreateOrthographicOffCenter(0, w, h, 0, -1, 1)` |
| `UI` | 否 | 同上，再**先**乘 `Main.UIScaleMatrix` |

实现只有两个落点：`MonoPrimitiveRenderer.ToScreen`（唯一做坐标换算的地方，只有 `World` 会减）与
`CalculatePrimitiveMatrix`（唯一选矩阵的地方）。空间由三个绘制设置 record 的 `Space` 成员承载；
`MonoPrimitiveSettings.UseUnscaledMatrix` 只覆盖矩阵、不改换算（供像素化图元用）。

世界矩阵那一段与 Luminance 的 `CalculatePrimitiveMatrices` 逐行一致，已在 Calamity/Daybreak 生态里
跑过，没有理由改动。`Main.UIScaleMatrix` 由绘制循环写入，所以 `UI` 必须在 UI 绘制阶段调用，
否则会用到上一帧的值。

#### II. 批次状态

GPU 图元不经过 `SpriteBatch` 的顶点队列，是直接 `DrawIndexedPrimitives`；而 `Main.spriteBatch`
在整个场景绘制期间几乎一直是 begun 状态。

```csharp
// 错误：在已 begun 的批次里做原始绘制
MonoPrim.DrawCircle(center, radius, color);

// 正确：先结束批次，画完再恢复
Main.spriteBatch.End();                 // 或用 PrepareForShaders() 之类的助手
MonoPrim.DrawCircle(center, radius, color);
Main.spriteBatch.ResetToDefault(end: false);
```

`MonoPrimitiveRenderer.Flush` 会在绘制前后自己保存并恢复顶点缓冲、索引缓冲、光栅化状态与混合状态，
所以它不会污染 `SpriteBatch`；但**批次的 Begin/End 归属必须由调用方处理**——这就是
`MonoSpriteBatchExtensions` 存在的全部理由。

#### III. GPU 资源生命周期

| 模型 | 何时用 | 谁负责释放 |
|---|---|---|
| **池**（`MonoRenderTargetPool`） | 帧内短租，用完立刻还 | `Lease.Dispose()` 推回池；池在 `Unload` 全部释放 |
| **受管**（`MonoManagedRenderTarget`） | 跨帧长期持有、跟随分辨率、闲置回收 | `MonoRenderTargetManager`（分辨率变化重建、闲置到期释放），`Unload` 兜底 |
| **图元缓冲**（`MonoPrimitiveRenderer`） | 单例，跟随模组 | 自己的 `Initialize` / `Unload`，带可重试与生命周期代号 |

不要用池里的租约去长期持有（忘了 `Dispose` 就永远不还），也不要用受管目标去做帧内 ping-pong
（每次访问都会刷新闲置计时器，分辨率变化时还会被重建）。

#### 三个设计取舍

1. **图元层不复用 `SpriteBatch`**：`SpriteBatch` 的顶点格式里没有"沿轨迹参数 / 截面坐标 / 像素宽度"
   这三个量，而自定义特效需要它们。代价是自己管缓冲、绕行与状态恢复，这些被收敛进
   `MonoPrimitiveRenderer` 一个类。
2. **坐标空间做成显式枚举而不是重载**：`DrawCircleWorld` / `DrawCircleScreen` 这类重载会让"忘了选"
   变成"默认选了一个可能是错的"，而显式枚举让"忘了想"变成编译期可见的选择。
3. **渲染目标两种所有权模型**：帧内短租与跨帧持有的生命周期完全不同，合成一套 API 必然在其中一边写错。

---

## 2. 部件地图

```
Monochrome/Core/Graphics/
├─ MonoGraphicsSpace.cs              枚举：World / Screen / UI          ← 不变量 I 的定义处
├─ MonoGraphicsFacades.cs            MonoPrim（图元门面）、MonoRt（RT 门面）
├─ MonoSpriteBatchExtensions.cs      批次状态助手                       ← 不变量 II 的工具
├─ MonoGraphicsCommand.cs            /monoshader（着色器与屏幕后处理诊断）
├─ Primitives/                       ★ 图元层：几何 → 三角形网格 → GPU
│  ├─ MonoPrimitiveVertex.cs         顶点布局 + 附加数据位契约
│  ├─ MonoPrimitiveSettings.cs       三个绘制设置 record
│  └─ MonoPrimitiveRenderer.cs       网格组装 + 提交 + GPU 资源生命周期 ← 不变量 III
├─ RenderTargets/                    渲染目标
│  ├─ MonoRenderTargetPool.cs        帧内短租（池 + Lease）
│  ├─ MonoManagedRenderTarget.cs     跨帧长期持有 + 跟随分辨率
│  ├─ MonoRenderTarget.cs            受管目标的兼容包装
│  └─ MonoRenderTargetManager.cs     统一维护全部受管目标
├─ Shaders/                          着色器
│  ├─ MonoShader.cs                  参数缓存 / 纹理绑定 / pass 选择 / 热替换
│  ├─ MonoShaderManager.cs           注册表、自动加载、管辖范围判定
│  └─ MonoShaderReloader.cs          运行时编译与写回
├─ Particles/                        对象式粒子（带独立行为与绘制）
├─ Metaballs/                        融合球数据与 GPU 等值面
├─ PostProcessing/                   ping-pong 后处理链 + 内置屏幕特效
│  ├─ IMonoPostFxPass.cs             一个 pass 的契约
│  ├─ MonoPostFxPipeline.cs          链：pass 排序 + ping-pong
│  ├─ MonoShaderPass.cs              用着色器的 pass 的基类
│  ├─ MonoScreenFx.cs                内置特效的名字表与工厂
│  ├─ MonoVignettePass.cs 等 6 个    暗角 / 色差 / 色阶 / 扫描线 / 噪点 / 模糊
│  └─ MonoTintPass.cs                无着色器降级/测试 pass
└─ Screen/                           把效果接到"画面上"的那些东西
   ├─ MonoScreenEffectsSystem.cs     震动 / 命中定格 / 世界时间缩放
   ├─ MonoScreenCapture.cs           一步到位：捕获 → 过链 → 回贴
   ├─ MonoScreenCaptureSystem.cs     钩世界捕获点，按需申请捕获并应用注册的链
   └─ MonoCameraFollow.cs            相机跟随（平滑 + 死区 + 前瞻），默认关闭
```

依赖方向是单向的：

```
                 Monochrome（Mod 主类，只做装配）
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
   MonoPrim / MonoRt     MonoShaderManager     各 ModSystem
        │                     │                     │
        ▼                     ▼                     ▼
  MonoPrimitiveRenderer  MonoShader        RenderTargets / Particles
        │                     │                     │
        └─────────────────────┴─────────────────────┘
                              ▼
              共享底层：MonoGraphicsSpace、MonoRenderTargetPool、
                        MonoManagedRenderTarget、MonoGraphicsCommand
```

---

## 3. 一帧的时间线

**哪些代码在什么时候被 tML 调用**，所有时机都对着反编译源码核对过。

| 阶段 | 谁被调用 | 做什么 |
|---|---|---|
| **加载期（一次）** | `MonoPrimitiveSystem.OnModLoad` | 排队创建图元 GPU 资源（`MonoPrimitiveRenderer.Initialize`） |
| | `MonoShaderManager.OnModLoad` | 建空注册表 |
| | `MonoRenderTargetManager.OnModLoad` | 挂 `Main.OnPreDraw` 与 `On_Main.SetDisplayMode`（专用服务器跳过） |
| | `MonoParticleManager.Load` | 挂 `On_Main.DrawDust` |
| | `MonoScreenCaptureSystem.Load` | 挂 `On_FilterManager.EndCapture` |
| | `Monochrome.PostSetupContent` | 扫每个模组的着色器资产（`LoadForMod`），再排空 `PostShaderLoadActions` |
| **每帧更新** | `MonoMetaballManager.PreUpdateWorld` | 清空所有层，于是 `Add` 等于"每帧重新提交" |
| | `MonoParticleManager.PostUpdateDusts` | 推进粒子的行为、位置、旋转、寿命，回收到寿的 |
| | `MonoScreenEffectsSystem.PreUpdateEntities` | 算"这一帧世界要不要推进"（世界更新闸门 hook 读它）、递减定格计数 |
| | `MonoScreenCaptureSystem.PreUpdateEntities` | 按"有没有启用的 pass"同步是否向原版申请屏幕捕获 |
| | `MonoShaderManager.PostUpdateEverything` | 每秒一次：压住 Luminance 的文件监视器 |
| **每帧绘制前** | `MonoRenderTargetManager.HandleTargetUpdateLoop`（`Main.OnPreDraw`，`Main.cs:59544`） | 保存/恢复渲染目标绑定 → 触发 `RenderTargetUpdateLoop` → 递增闲置计数、到期回收 |
| **相机定位** | `MonoScreenEffectsSystem.ModifyScreenPosition`（`Main.cs:60765`） | 把震动位移加进 `Main.screenPosition`，推进 trauma 衰减 |
| | `MonoCameraFollow.ModifyScreenPosition` | 只在开启跟随时生效：把跟随基准换成自己的焦点（§5.10） |
| **绘制循环内** | `MonoParticleManager.DrawParticles`（`On_Main.DrawDust`） | 按混合状态分批绘制粒子 |
| | `MonoMetaballManager`（`Main.OnPreDraw` / `DrawDust`） | 累积融合场 → 合成等值面 |
| | 消费者代码 | 调 `MonoPrim.*`；调用点要落在 `End()` 与 `Begin()` 之间 |
| **世界画完、界面之前** | `MonoScreenCaptureSystem.EndCaptureDetour`（`On_FilterManager.EndCapture`，`Main.cs:60145`） | 把注册的屏幕后处理链原地作用到世界捕获目标上（§5.12） |
| **分辨率变化** | `MonoRenderTargetManager.ResizeScreenSizedTargets`（`On_Main.SetDisplayMode`） | 给每个允许重建的受管目标排队 `Recreate` |
| **卸载期** | 各 `Unload` / `OnModUnload`（在线程池线程上） | 释放缓冲与效果、摘掉钩子。受管目标的显存释放会经 `Main.QueueMainThreadAction` 推回主线程 |

**两个时机的实测依据**（"原版会先做 X"这类断言必须落到源码）：

- 原版在 `Main.cs` 里的顺序是 `60103 spriteBatch.End()` → `60104 DrawDust()` → `60105 spriteBatch.Begin(...)`，
  所以 `On_Main.DrawDust` 是唯一天然落在 End/Begin 之间的现成钩子。
- `On_FilterManager.EndCapture`（`60145`）是唯一"世界已经画完、界面还没开始"的现成钩子。名字更像
  "绘制之后"的 `Main.OnPostDraw`（`Draw_Inner`，`59465-59480`）在 `DoDraw` 整体结束后才触发，
  那时世界早已被合成到后备缓冲上，拿不回来了。
- `ModifyScreenPosition` 之后紧接着是 `60766-60767 screenPosition.X/Y = (int)screenPosition.X/Y`，
  **因此震动与相机位置都会被截断成整像素**。这是原版行为，不是本系统的 bug（见 §9）。

---

## 4. 部件索引

| 需求 | 用哪个 |
|---|---|
| 画线 / 圆 / 弧 / 胶囊 / 虚线 / 贝塞尔 / 多边形 / 渐变矩形 | `MonoPrim.Draw*`、`MonoPrim.FillPolygon`（§5.2、§5.3） |
| 逐点变宽的拖尾 | `MonoPrim.RenderTrail` + `MonoPrimitiveSettings.WidthFunction`（§5.2） |
| 画一块带旋转或自定义着色器的贴图 | `MonoPrim.RenderQuad`（§5.2） |
| 帧内借用渲染目标 | `MonoRt.Rent` / `MonoRenderTargetPool.Rent`（§5.6） |
| 一块跟随屏幕分辨率的渲染目标 | `MonoRt.ScreenSized`（§5.6） |
| 写/用自定义着色器 | `MonoShaderManager` + `MonoShader`（§5.5） |
| 马上看到 `.fx` 改动 | `/monoshader reload <名字>`（§5.5） |
| 成百上千个带行为的粒子 | 派生 `MonoParticle`（§5.8） |
| 会互相粘连的形体 | `MonoMetaballManager`（§5.9） |
| 屏幕震动 / 定格 / 慢动作 | `MonoScreenEffectsSystem`（§5.10） |
| 世界画面的后处理（暗角、色差、模糊……） | `MonoScreenFx` + `MonoScreenCaptureSystem`（§5.12、§5.13） |
| 相机跟随 | `MonoCameraFollow`（§5.10） |

---

## 5. 部件参考

每个部件都按同一套问题写：**它是什么 → 和谁协作 → 怎么用 → 边界与坑**。

### 5.1 MonoGraphicsSpace —— 坐标空间

三个取值，语义见 §1 的表格。

**怎么选**：

- 位置来自世界坐标（`NPC.Center`、`Projectile.oldPos`、图格坐标）→ `World`。
- 位置是你自己按屏幕算出来的（固定 HUD 位置、全屏四边形）→ `Screen`。
- 要在 UI 绘制阶段画（跟着 `Main.UIScaleMatrix` 缩放）→ `UI`。

**边界**：`UseUnscaledMatrix`（只有 `MonoPrimitiveSettings` 有）把空间折算成 `Screen`，
用于像素化图元——它只改投影矩阵，不改坐标换算。`UI` 必须在 UI 绘制阶段调用。

---

### 5.2 Primitives —— 图元层

把几何组装成索引三角形网格，用动态顶点/索引缓冲一次提交。GPU 不画圆也不画线，只画三角形，
所以每个形状在这里都被拆成顶点与索引。

#### 5.2.1 MonoPrimitiveVertex —— 顶点契约

布局：`Vector2` 位置（偏移 0）+ `Color`（8）+ `Vector3` 附加数据位（12），共 24 字节。
三个附加分量是留给图元着色器的自由数据，没有贴图被采样：

| 分量 | 含义 |
|---|---|
| **X** | 沿形状的长度/角度参数 `t ∈ [0,1]`。拖尾沿路径、圆与圆弧按角度、贴图四边形为真实纹理横坐标 |
| **Y** | 截面坐标 `[0,1]`。`0` / `1` 是截面两侧，`0.5` 恒为正中。拖尾是左缘→右缘，圆与圆环是圆心/内径→边缘/外径 |
| **Z** | `Y` 从 0 走到 1 跨越的**真实像素距离**。拖尾是条带全宽、圆环是环厚、圆是半径 |

于是 `Y * Z` 就是"距截面 0 侧边缘的像素数"，这就是柔边能恒定 3 像素、虚线能在像素上等长的原因。
布局与 Luminance 的 `VertexPosition2DColorTexture` 逐字段一致，所以顶点声明不需要试错。

> **Z 已经实测验证过**，配方见文末附录。等宽拖尾证明不了它——`Solid(32f)` 的 Z 处处都是 32，
> 无论它是"像素宽度""半宽"还是"常数"，画面都长得一样。

#### 5.2.2 绘制设置

| 类型 | 用途 | 关键成员 |
|---|---|---|
| `MonoPrimitiveSettings` | 拖尾 | `WidthFunction` / `ColorFunction` / `OffsetFunction` / `Smoothen` / `InitialVertexPositionsOverride`，以及共有的 `Shader` / `BlendState` / `Space` / `UseUnscaledMatrix` / `ProjectionWidth·Height` |
| `MonoCircleSettings` | 实心圆 | `RadiusFunction`（沿角度的半径，可做心形/星形） |
| `MonoCircleEdgeSettings` | 圆环带 | `RadiusFunction`（内径）+ `EdgeWidthFunction`（环厚，向外展开） |

等宽单色拖尾用 `MonoPrimitiveSettings.Solid(width, color)`。三者都是 record，用命名参数构造。

```csharp
// 沿轨迹收细、渐隐的拖尾
MonoPrim.RenderTrail(projectile.oldPos, new MonoPrimitiveSettings(
    WidthFunction: t => MathHelper.Lerp(12f, 1f, t),
    ColorFunction: t => Color.Cyan * (1f - t)));
```

#### 5.2.3 MonoPrimitiveRenderer —— 内部结构

| 方面 | 做法 |
|---|---|
| 缓冲 | **动态**顶点/索引缓冲，容量 `MaxPoints = 4096` 点 → 16384 顶点 / 24576 索引；每次提交 `SetDataOptions.Discard` 覆盖写 |
| 索引类型 | `short`。最大顶点索引 16383 < `short` 上限，所以安全 |
| 一次提交 | 一个形状一次 `DrawIndexedPrimitives`。这是图元层存在的全部意义 |
| 状态 | `Flush` 前后自己保存/恢复顶点缓冲绑定、索引缓冲、光栅化（`CullNone`）与混合状态，**不污染 `SpriteBatch`** |
| 零分配 | 复用静态 `scratch[]` / `quadCorners[]` / `scratchList`；弧、贝塞尔、虚线都不 `new[]` |
| 着色器 | 没传 `shader` 就用默认图元着色器；连它也没加载到才回退 `BasicEffect`（只用顶点色）。成功取到会缓存，失败则下次重试 |
| 降级 | 所有入口过 `HasRoomFor`，超限**安静截断**而不是越界或抛异常 |
| 就绪 | 设备资源排队创建；`Ready()` 在失败后会重排一次，所以"一次失败"不会变成永久失效 |

`Flush` 每次都会显式写 `useTexture`：参数缓存是持久的，不写就会沿用上一次的值，于是
"先用 `RenderQuad` 画过贴图、再 `DrawLine`"会让线段去采样一张没绑定的贴图。

#### 5.2.4 MonoPrimitiveSystem

`ModSystem`，只负责图元缓冲的生命周期：`OnModLoad` 排队创建、`Unload` 排队释放。
释放带一个**生命周期代号**，让"卸载期间发生重新初始化"（模组热重载）时那次释放自动作废，
不会释放掉新资源。

---

### 5.3 MonoPrim / MonoRt —— 门面

`MonoPrim` 是 `MonoPrimitiveRenderer` 的短别名（逐个转发），`MonoRt` 是渲染目标的短别名。
它们存在的理由是让调用点短、并且把"该用哪种渲染目标"这件事收在一处。

```csharp
MonoPrim.DrawCircle(npc.Center, 32f, Color.Cyan, 2f);              // World
MonoPrim.FillPolygon(points, Color.Cyan * 0.4f);                   // 凹凸均可
using MonoRenderTargetPool.Lease? lease = MonoRt.Rent(w, h);       // 服务器上返回 null
var screenTarget = MonoRt.ScreenSized();
```

**边界**：门面不带任何额外行为，参数含义与 `MonoPrimitiveRenderer` 上的同名方法完全一致。

---

### 5.4 MonoSpriteBatchExtensions —— 批次状态

不变量 II 的工具。提供"结束当前批次 → 交给原始绘制 → 恢复默认 Begin"的助手，
省掉手拼 `Begin` 的九个参数（其中任一个写错都会静默产生错的混合或错位）。

**边界**：它只是助手，不会隐式切换批次——什么时候切仍然由调用方决定。

---

### 5.5 Shaders —— 着色器

#### 运行时只认 `.fxc` / `.xnb`

- tML 的 `AdditionalFiles` 收集 `**/*.fx`（`tMLMod.targets:41`），但只喂 IDE 与分析器，不产生可加载资源。
- 资产读取器注册表只有 `.png` / `.xnb` / `.rawimg` / **`.fxc`** / `.wav` / `.mp3` / `.ogg`
  （`Terraria.Initializers\AssetInitializer.cs:27-33`），没有 `.fx`。
- `FxcReader` 的实现就是 `new Effect(graphicsDevice, bytes)`（`FxcReader.cs:27`）；FNA 不含 HLSL 编译器。
- 因此 `.fxc` 必须是**完整效果**（`fx_2_0`）。实测产物头：`/T fx_2_0` → `01 09 FF FE`；
  裸 `/T ps_3_0` → `00 03 FF FF`，`new Effect` 解析不了。

所以 `.fx` 是源码、`.fxc` 是产物，**两个都要进仓库也都要进 `.tmod`**——`.fx` 进包是因为运行时重载要靠它。

| 环节 | 位置 |
|---|---|
| 源 | `Assets/Effects/**/*.fx` |
| 产物 | 同目录同名 `.fxc`，与 `.fx` 一起提交 |
| 编译器 | `Assets/AutoloadedEffects/Compiler/{fxc.exe, d3dcompiler_47.dll}`（随模组打包，所以运行时重载可用） |
| 脚本 | `Tools/CompileMonochromeShaders.ps1`，只重编比 `.fxc` 新的 `.fx`，`-Force` 强制全编 |
| MSBuild | `Monochrome.csproj` 的 `CompileMonochromeShaders` target，`BeforeTargets="CoreCompile"` |

#### 构建链与 PowerShell 的编码要求是矛盾的

`fxc` **拒绝 BOM**（带 BOM 的 `.fx` 在 `(1,1)` 报 `X3000: Illegal character in shader file`，
报错位置在最开头，很容易被误读成语法错），而 Windows PowerShell **5.1 要求**含中文的 `.ps1`
必须带 UTF-8 BOM，否则把中文当乱码、报一堆 `MissingEndCurlyBrace`。

因此不要用"统一编码"的思路解决，而是消除对编码的依赖：`Monochrome.csproj` 优先调用
PowerShell 7（默认按 UTF-8 读脚本，不需要 BOM），找不到才退回 5.1（那时那份 BOM 仍然必要）。
构建脚本与运行时重载器都会在编译前再规范化一次源文件（去 BOM / UTF-16 转 UTF-8），
但仓库里的 `.fx` 本身也应存成无 BOM UTF-8，否则别的工具（如 ModdersToolkitFXBuilder）依旧编不过。

#### `/monoshader reload [名字|all|*]`

改完 `.fx` 不用重启游戏。它编到临时文件 → 构造新 `Effect` → **构造成功才替换** → 原子写回源目录的
`.fxc`，所以编译失败只回一行错误，旧特效原样保留。

| 形式 | 行为 |
|---|---|
| 不带名字 | 全部重载，但做 mtime 预筛，被跳过的项会明确写出来 |
| `<名字>` | 点名一律重编，不做预筛 |
| `all` / `*` | 强制全部重编 |

`MonoShaderReloader` 的源文件优先取所属模组的 `SourceFolder`；没有源目录时退回从 `.tmod` 里取出
`.fx` 写到临时目录，所以 `.fx` 必须留在包里。它每次只重编**在管辖范围内**的模组（见下）。

#### MonoShaderManager

按与 Luminance 相同的约定自动加载：文件名以 `.fxc` 或 `.xnb` 结尾，且路径含
`Assets/AutoloadedEffects/Shaders` 或 `Assets/Effects`。`Monochrome.PostSetupContent` 对每个已加载模组
各调一次，所以声明了管辖的模组所带着色器也会进这张表。

**管辖范围**由模组主类上的 `[MonoShaderScope]` 显式声明（Monochrome 自己总是算在内）。这条限制是必须的：
热重载会把编好的 `.fxc` 写回该模组的源目录，那是会动别人磁盘的操作，该由消费者自己表态。
判据不用"程序集引用了 Monochrome"，因为那只说明消费者用到了某个类型——只用到会被内联的常量时，
编译器不会写出那条引用，判定会静默失效。有资产却没声明的模组会收到一条点名日志。

同名 `.fxc` 与 `.xnb` 并存时以 `.fxc` 为准（分两趟扫，先 `.fxc` 后 `.xnb`）。这个顺序是必需的：
`Mod.GetFileNames()` 的枚举顺序没有任何保证，单趟扫描等于把"用哪一份"交给运气。热重载写回 `.fxc`
时会顺手删掉同名 `.xnb`，加载期发现重复也会记一条日志。

#### MonoShader

参数缓存、纹理绑定、pass 选择与热替换。参数缓存的意义是跳过重复的 `SetValue`（那要穿过 FNA 的指针层）。

| 成员 | 说明 |
|---|---|
| `TrySetParameter` / `SetParameter` | 名字不存在或值未变化时返回 false，不视为错误 |
| `SetTexture(texture, index)` | 绑到采样槽并写 `textureSize{index}`；自定义图元的贴图从槽 **1** 开始（槽 0 归 `SpriteBatch`） |
| `Apply(pass)` | pass 名查不到时退回第 0 个 pass（FNA 的字符串索引器返回 null 而不抛） |
| `ResetParameterCache` | 同一个 `Effect` 被别处绕过本类直接写参数时必须调用 |
| `Replace` / `Dispose` | **都不释放底层 `Effect`**——同一个资产名在 `AssetRepository` 缓存里只有一个实例，Luminance 的 `ManagedShader` 也持有它 |

#### 第一个资产：`Monochrome.StandardPrimitive`

图元的默认着色器模板，pass 名 `AutoloadPass`，贴图绑在采样槽 1。`debugMode ≥ 0.5` 输出顶点契约可视化
（R = 截面、G = 像素跨度 / 64、B = 长度参数），可用来肉眼验证契约；`useTexture` 默认 0，即纯顶点色。

---

### 5.6 RenderTargets —— 渲染目标

三种东西，**不要混用**（不变量 III）：

| 类型 | 生命周期 | 用法 |
|---|---|---|
| `MonoRenderTargetPool` + `Lease` | 帧内短租，`Dispose` 即归还 | `using MonoRenderTargetPool.Lease? lease = MonoRt.Rent(w, h);` 然后判空 |
| `MonoManagedRenderTarget` | 跨帧持有、跟随分辨率、闲置可回收 | `MonoRt.ScreenSized()` 或 `MonoRt.Create(factory)` |
| `MonoRenderTarget` | 受管目标的兼容包装 | 为了对接需要 `RenderTarget2D` 的旧 API |

#### MonoRenderTargetPool（池）

按 (宽, 高, 表面格式, 深度格式) 分组的渲染目标池。每次 `new RenderTarget2D` 都会真的分配显存并重建
后备缓冲，在后处理链这种"每帧都要两块中间目标"的场景里是纯粹的浪费。

**边界**：专用服务器上返回 `null`——`using` 对 null 安全，但 `lease.Target` 不是，调用方必须判空。

#### MonoManagedRenderTarget（受管）

首次访问时才创建，分辨率变化时自动重建，长期未使用时允许自动释放显存。构造时把自己登记到
`MonoRenderTargetManager`，因为"分辨率变了"这个事件只有一处能拿到。

两个容易踩的点：

- **`Target` 的 getter 有副作用**：它把闲置计时器归零（`Width` / `Height` 也会）。别把它放进每帧
  轮询的循环里，否则空闲回收永远不会触发。
- **`Dispose` 不是终态**：它只丢掉显存里那块纹理，之后访问 `Target` 会按需重建。这是为了让 wrapper
  比底层目标活得久——它是唯一持有重建委托的东西。要永久注销请用 `Retire()`，那才会释放显存**并**
  把自己从登记表摘掉，此后访问会抛 `ObjectDisposedException`。

#### MonoRenderTargetManager

统一维护全部受管目标：挂 `Main.OnPreDraw` 做每帧的"触发重建 + 递增闲置计数 + 到期回收"，
挂 `On_Main.SetDisplayMode` 在分辨率变化时排队 `Recreate`。专用服务器上两个钩子都不挂。

**边界**：它回收的是**不属于自己**的对象，所以受管目标的 `Dispose` 才必须保持"可重建"语义。

---

### 5.7 PostProcessing —— 后处理链

链只负责"把给定的一块内容过一遍 pass，结果放到另一块"。捕获与回贴由 §5.12 负责，
真正看得见的效果由 §5.13 负责。

#### IMonoPostFxPass

```csharp
public interface IMonoPostFxPass
{
    int Order { get; }      // 越小越先执行
    bool Enabled { get; }   // false 时被跳过，且不消耗中间目标
    void Apply(RenderTarget2D source, RenderTarget2D destination, SpriteBatch spriteBatch);
}
```

实现方约定：自己 `SetRenderTarget` 到 `destination`，画完恢复调用前的绑定；批次自己 `Begin`/`End`。

#### MonoPostFxPipeline

| 成员 | 作用 |
|---|---|
| `Passes` / `Add` / `Remove` | 加入时按 `Order` 排序；重复加入同一实例被忽略 |
| `IsActive` | 至少有一个 pass 处于启用状态。屏幕捕获用它决定"要不要为这一帧申请捕获" |
| `Apply(source, destination, batch)` | 先挡 `Main.dedServ` 并静默返回；`source` 与 `destination` 是同一块时记一次日志并跳过（FNA 不允许一块纹理同时当渲染目标和采样源）；没有启用的 pass 就退化成一次整幅拷贝；否则租两块中间目标做 ping-pong |
| `CopyTo(src, dst, sb)` | `internal`，`MonoScreenCapture` 也用它，所以两条路径的拷贝语义是同一份实现 |

ping-pong 的意义是两个 pass 之间不需要新的渲染目标，把每帧的 RT 分配降到零。

#### MonoPostFxSystem

按名字保存后处理链的注册表（`GetOrCreate(name)`，名字不区分大小写）。用名字而不是全局单例，
是为了让多个模组各自拥有独立的链而互不干扰。`Unload` 清空。

#### MonoTintPass

无着色器的整幅染色 pass，定位是**测试与降级**：验证中间目标、ping-pong 顺序与状态恢复是否正确，
以及着色器缺失时提供一个可用的效果。正式特效请写 `.fx` + `MonoShaderPass`。

---

### 5.8 Particles —— 粒子

**定位**：需要"独立行为 + 自定义绘制"的粒子（拖尾、软粒子、光照），数量级以**数百**为限。
几千个火星碎屑请走原版 `Dust` 或结构体池。

#### MonoParticle（基类）

| 成员 | 说明 |
|---|---|
| `Position` / `Velocity` / `Scale` / `Color` / `Rotation` / `RotationSpeed` / `Opacity` | 数据。`Velocity` 每帧**直接**加到 `Position` 上，不乘时间步长 |
| `Time` / `Lifetime` / `LifetimeRatio` | 寿命。`LifetimeRatio` 是 `[0,1]` 的进度；`Lifetime` 非正时恒为 1 |
| `Active` | 是否处于活动集合内，由管理器写 |
| `Space` | 虚属性，默认 `World`。它同时决定坐标系**与绘制相位** |
| `Update()` / `Draw(...)` / `ShouldKill()` | 虚方法：行为、绘制、消亡条件 |
| `Spawn()` / `Kill()` | 交给自己所属的管理器 |

`Draw` 的默认实现是 `Space == World ? Position - Main.screenPosition : Position`。

> **`TextureAssets.MagicPixel` 是 1×1000，不是 1×1**（实测）。默认 `Draw` 的 `Scale` 是**贴图倍数**，
> 拿它当 1 像素贴图用会画出一条横跨全屏的竖线（`1000 × Scale.Y`）。要固定像素大小就覆写 `Draw`
> 走 `new Rectangle(x, y, w, h)` 重载。

#### MonoParticleManager

更新挂在 `PostUpdateDusts`；绘制按 `Space` 分两个相位：

| `Space` | 绘制相位 | 说明 |
|---|---|---|
| `World` | `On_Main.DrawDust` | 天然落在 `End()` 与下一次 `Begin()` 之间；**按混合状态分组**合并成少量批次 |
| `Screen` / `UI` | `PostDrawInterface` | 画进游戏已经开好的界面批次 |

界面相位的两条限制：`Screen` 与 `UI` 对粒子而言是同一相位、同一变换；它们的 `BlendState` 覆盖
**不生效**（那时批次已经开着，自己 `Begin` 会抛异常）。需要界面侧自定义混合或着色器时，
得自己在 `End()` / `Begin()` 之间画。

---

### 5.9 Metaballs —— 融合球

多个球的影响力场相加后取等值面，所以靠近时会自然粘连——这正是它相对"半透明圆盘"的全部价值。

#### MonoMetaballSettings

| 设置 | 默认 | 说明 |
|---|---|---|
| `Threshold` | 0.5 | **融合积极性**：两球多早开始搭桥。融合距离 `0.5 → 3R`、`0.25 → 2.33R`、`0.1 → 2.11R`、`0.02 → 2.04R` |
| `SupportRadius(visible)` | — | `visible / max(0.1, 1 - clamp(Threshold, 0, 0.9))`。提交时支撑半径按它放大，等值面因此恰好落在你给的 `Radius` 上 |
| `Softness` / `EdgeWidth` / `EdgeColor` | — | 合成时的阈值过渡宽度与边缘带 |
| `InfluencePower` | — | 影响力曲线的幂次，决定融合手感 |
| `Segments` / `ResolutionDivisor` / `Opacity` | 24 / 2 / — | 每颗球的分段数、半分辨率累积的除数、整体不透明度 |

**阈值不改变球的大小**：可见半径恒等于你给的 `Radius`。

#### 两种球

| 类型 | 特点 | 提交 |
|---|---|---|
| `MonoMetaball` | 只有数据，没有行为 | `MonoMetaballManager.Add(layer, ball)`，层在每帧 `PreUpdateWorld` 被清空，等于每帧重新提交 |
| `MonoMetaballInstance` | 有自己的行为与生命周期，形状与 `MonoParticle` 对齐 | `new MyBlob(pos).Spawn()`；层名由 `LayerName` 提供。管理器在 `PostUpdateWorld` 推进：`Update` → 位移 → 计龄 → 回收 |

两者可以混在同一层里一起融合。

#### MonoMetaballBrush

往融合场里写球的低层入口：`Begin(shader, blendState, segments)` 与 `Ball(center, visibleRadius, color, strength, influencePower)`。
它内部会把可见半径换算成支撑半径，所以调用方不必关心阈值。

#### MonoMetaballManager

按"层"收集，同层融合、跨层互不影响。GPU 路线分两段：

1. **累积**：订阅 `MonoRenderTargetManager.RenderTargetUpdateLoop`（时机是 `Main.OnPreDraw`），
   把每层的球用加法混合画进该层的**半分辨率**目标（着色器 `Monochrome.MetaballField`，
   走"每颗球一个扇形"的图元路径）。
2. **合成**：用 `smoothstep` 阈值把场切成等值面，再加一圈边缘带
   （`Monochrome.MetaballComposite`，整屏四边形 + 采样槽 1 的贴图）。

`AutoDraw` 默认为 true，所以调用方只要提交，不必在绘制相位手动调 `Draw`。`Sample(point)` 给出
GPU 场的 CPU 版本，用它可以对照"GPU 画得对不对"。`DrawFallback` 只是"半透明圆盘 + 描边"，
不会让两个球粘连，是降级路径。

累积用的混合状态是自己持有的 `(One, One)`（预乘加法），不碰 FNA 的共享状态。

---

### 5.10 Screen —— 屏幕特效与相机

#### MonoScreenEffectsSystem

| 成员 | 作用 |
|---|---|
| `IsHitstopActive` / `IsWorldUpdateSkipped` | 诊断 |
| `WorldTimeScale` | 世界时间倍率，真正生效：`1` 正常、`0.5` 半速、`0` 停住。实现是"按比例跳过世界更新帧"，所以非整数倍率会有轻微顿挫（0.7 = 每 10 帧跳 3 帧） |
| `StartShake(strength, decay, center, radius)` | 追加震动源。实际振幅是 trauma 的**平方**乘 18 像素，多个源可叠加而不互相压制；`center` + `radius` 做距离衰减 |
| `StartHitstop(frames)` | 请求定格。多次请求取较大者，不累加 |
| `PreUpdateEntities()` | 算"这一帧世界要不要推进"并递减定格计数 |
| `ModifyScreenPosition()` | 逐源算 trauma → 加到 `Main.screenPosition` → 衰减 → 归零的移除 |

**机制与两条硬约束**见代码注释；要点是时间缩放走原版现成的世界更新闸门（`Main.cs:16938`），
所以世界停住而输入、绘制、界面、网络包处理照常。

#### MonoCameraFollow

平滑跟随 + 死区 + 前瞻，**默认关闭**。

```csharp
MonoCameraFollow.DeadZone = new Vector2(80f, 56f);   // 目标在这个矩形内时相机完全不动
MonoCameraFollow.Smoothing = 0.12f;                  // 每帧追上"超出死区部分"的比例，越小越黏
MonoCameraFollow.LookAhead = 0.12f;                  // 前瞻秒数
MonoCameraFollow.FollowPlayer();                     // 或 Follow(世界坐标点)，Stop() 停止
```

它**只替换原版的跟随基准**，其余全部保留（见代码注释里的步骤表与 `extras` 推导）：
平移、原版相机修饰器、其它模组与震动的贡献都不会被吃掉，也不会累加漂移。边界钳制与整像素取整由
原版在钩子之后完成，不必自己做。

**速度自适应缩放没有实现**：`PlayerInput.SetZoom_World()` 在相机钩子之后才跑
（`Main.cs:60230` vs `59593`），会把 `SpriteViewMatrix.Zoom` 重新推一遍，在那里写 zoom 等于没写。

---

### 5.11 MonoGraphicsCommand —— 诊断命令

**触发词是 `monoshader`**（一个词）。tML 用命令触发文本的**第一段**做键，
所以 `"mono shader"` 的键是 `"mono"`——与核心命令的 `"mono"` 撞在一起，先注册的吃下全部 `/mono ...`，
另一个**永远接不到且不报错**。两个独立命令的触发词必须是两个不同的第一段，现在是
`monocore` 与 `monoshader`。完整说明见 `Core/README.md` §7.4。

子命令与命令的第一段同名时省略：`/monoshader reload`（不是 `shader reload`）。
旧写法 `/monoshader shader status` 也认，等价于 `/monoshader status`。

| 命令 | 何时可用 | 作用 |
|---|---|---|
| `/monoshader help` | 总是 | 子命令清单（打错时用它查；打错本身只回一行提示） |
| `/monoshader status` | 总是 | 着色器注册表 + 屏幕捕获概要 |
| `/monoshader reload [名字\|all\|*]` | 仅开发期 | 编译 `.fx` 并替换正在用的 `Effect` |
| `/monoshader capture status` | 总是 | 已注册的屏幕后处理链 + 本帧是否已申请捕获 |
| `/monoshader capture fxs` | 总是 | 列出全部内置屏幕特效与各自的着色器状态 |
| `/monoshader capture test [on\|off]` | 仅开发期 | 挂一条整幅染色链，验证屏幕捕获通路 |
| `/monoshader capture fx <名字\|all> <on\|off>` | 仅开发期 | 单独开关一条内置特效（用"一眼能看出来"的演示参数） |

**"开发期"＝有调试器附加**（从 IDE 启动，或给进程附加调试器）。不用编译配置当判据，因为 tML
构建模组时默认就是 Debug，那样等于永远放行。没有调试器时设环境变量 `MONOCHROME_DEBUG=1` 也能开。

**不做 `FileSystemWatcher`**：热重载是主动动作，代价是每次会在主线程阻塞一次 `fxc`（约 0.1–0.3 秒）。

---

### 5.12 MonoScreenCapture —— 把管线接到画面上

§5.7 的链只管 ping-pong，"整幅画面从哪来、处理完放回哪去"由这里补上。

#### 插入点：世界画完、界面还没开始的那一瞬间

| 步 | 位置 | 发生什么 |
|---|---|---|
| 1 | `Main.DoDraw` 算出 `flag2`（`Main.cs:59861`） | 为真时 `Filters.Scene.BeginCapture(...)`（`59864`）把渲染目标切到离屏目标，随后整个世界画进去 |
| 2 | `Filters.Scene.EndCapture(null, screenTarget, ...)`（`60145`） | 把 `screenTarget` 过一遍原版滤镜，再合成到帧末目标（第一个参数是 `null`，即后备缓冲） |
| 3 | `Main.cs:60148` 起 | 之后才开始画界面：聊天、战斗文字、物品栏、光标 |

`MonoScreenCaptureSystem` 钩 `On_FilterManager.EndCapture`，**在 `orig` 之前**把 `screenTarget1`
原地改掉。**回贴不用我们做**——第 2 步本来就要把 `screenTarget1` 合成到屏幕上。

作用范围是世界 + 世界内界面；HUD、聊天、战斗文字、光标在 `EndCapture` 之后才画，所以不覆盖。
想连 UI 一起处理是另一件完全不同的事（要在帧末抢后备缓冲），本系统不做。

#### 怎么让原版愿意捕获

`flag2` 里的 `Filters.Scene.CanCapture()` 是 `HasActiveFilter() || OnPostDraw != null`
（`FilterManager.cs:219-226`），也就是原版只给了两个开关：挂一个原版滤镜，或者订阅
`FilterManager.OnPostDraw`。本类只在至少一条链真正启用了 pass 时才订阅，空闲时立刻退订——
订阅本身会让整局游戏每帧多付一次离屏切换与一次全屏复制。

同步点选 `PreUpdateEntities`：它在 `DoDraw` 之前，且在世界更新闸门之外（`PostUpdateEverything`
在闸门内，命中定格时不执行）。原版什么时候**不**捕获：`flag2` 还要求 `Lighting.NotRetro`，
且不在 `drawToScreen`、不是全屏地图、不是 `netMode == 2`。这些情况下效果自然缺席。

#### MonoScreenCapture

| 成员 | 作用 |
|---|---|
| `Apply(pipeline, scene, batch?)` | 一步到位：捕获 → 过链 → 回贴，结果覆盖 `scene`。链里没有启用的 pass 时直接返回，连拷贝都不做 |
| `ApplyToBoundTarget(pipeline, batch?)` | 捕获当前绑定的渲染目标并原地过链 |
| `DrawFullscreen(source, destination, shader, blend?)` | 屏幕 pass 的出口，见图元路径说明 |
| `BoundTarget` | 当前绑定的渲染目标；绑定的是后备缓冲时返回 `null` |

**原地处理为什么安全**：GPU 不允许一块纹理同时当渲染目标和采样源。这里先把内容拷进池里租来的
`ping`，pass 在 ping/pong 之间倒，最后把结果写回原目标——每一步的读写目标都不同，不会形成反馈回路。

**`BoundTarget` 在后备缓冲上返回 `null`**：D3D11 不允许把后备缓冲当纹理采样，所以"随时把当前屏幕
抓下来"这条路由根本不存在，必须让原版先把世界画进一块真正的渲染目标。

---

### 5.13 MonoShaderPass —— 内置屏幕特效

#### 着色器契约（`Assets/Effects/Screen/*.fx`）

六个内置特效的 `.fx` 都按同一套约定写，看懂这四条就能自己加一条：

| # | 约定 | 为什么 |
|---|---|---|
| 1 | 输入贴图绑在**采样槽 1**（`sampler uSource : register(s1);`） | 槽 0 留给 `SpriteBatch` |
| 2 | UV 取 `TextureCoordinates.xy` | 复用图元顶点契约，不需要第二套位置语义 |
| 3 | 顶点着色器逐字沿用 `StandardPrimitive.fx` | 提交路径完全相同，`uWorldViewProjection` 由渲染器写 |
| 4 | 结果写成 `lerp(原始, 处理结果, saturate(fxOpacity))` | 每条特效天然支持淡入淡出 |

第 4 条的名字必须是 `fxOpacity`：图元渲染路径每次提交都调用 `SetCommonParameters()`，
而它每帧把 `opacity` 写成 1，叫 `opacity` 会被覆盖、淡出直接失效。

#### MonoShaderPass

子类只实现 `ApplyParameters(MonoShader)`。父类包办：解析着色器（缺资产就安静跳过，只记一次警告）、
绑定目标、`finally` 恢复绑定、铺满全屏、写 `fxOpacity`。

| 成员 | 说明 |
|---|---|
| `ShaderName` / `Shader` / `IsAvailable` | 着色器注册名与解析结果 |
| `Order` | **改完要重新 `Add` 一次**，链只在加入时排序 |
| `Enabled` / `Opacity` | 关掉后不参与；`Opacity` 是 `[0,1]` 淡出，要淡入淡出就动它而不是改强度参数 |
| `Apply` | 绑定目标 → 写参数 → 铺满 → 恢复绑定。**不清 `Clear`**：全屏四边形会写满每个像素（含 alpha），清屏是纯浪费的填充率 |

#### 六个内置特效

```csharp
MonoPostFxPipeline pipeline = MonoPostFxSystem.GetOrCreate("Monochrome.Main");
MonoScreenFx.AddTo(pipeline, "vignette", order: 60);     // 或直接 new MonoVignettePass { Strength = 0.5f }
MonoScreenCaptureSystem.Register(pipeline);
```

| 效果名 | 类 | 默认 `Order` | 关键参数（默认值） |
|---|---|---|---|
| `blur` | `MonoBlurPass` ×2 | 10 / 11 | `Radius = 3` px。必须成对（横 + 纵）且相邻 |
| `colorgrade` | `MonoColorGradePass` | 20 | `Tint = 白`、`Saturation = 1`、`Contrast = 1`、`Brightness = 0`，即**默认恒等** |
| `chromatic` | `MonoChromaticAberrationPass` | 30 | `Strength = 0.004`（UV 量纲）、`Falloff = 1`（只有边角） |
| `scanline` | `MonoScanlinePass` | 40 | `Strength = 0.25`、`Count = 540`、`Speed = 0` |
| `grain` | `MonoGrainPass` | 50 | `Strength = 0.08`、`Scale = 600`、`Monochrome = true` |
| `vignette` | `MonoVignettePass` | 60 | `Strength = 0.65`、`Softness = 0.55`、`Color = 黑` |

默认 Order 就是推荐叠法：**模糊 → 定调 → 光学瑕疵 → 扫描线 → 噪点 → 暗角**。模糊要在最前
（否则会把别的效果抹掉），色差在模糊之后（否则彩边被糊掉），噪点与暗角在最后。

`colorgrade` 的默认是完全恒等的，这样它可以在任何链里存在而不改变画面；想调风格再改参数。

#### 自检

```
/monoshader capture fxs                # 列出全部内置特效 + 各自的着色器有没有注册
/monoshader capture fx vignette on     # 开一条（用"一眼能看出来"的演示参数，不是默认参数）
/monoshader capture fx all off         # 全关
```

自检用的参数与正式默认值不同：默认值保守（甚至恒等），自检要的是"看不出变化就等于没生效"。

#### 加一条新特效要做什么

1. 在 `Assets/Effects/Screen/` 放一个 `.fx`，照抄任一同胞的骨架，改 `PixelShaderFunction`；
2. 在 `Core/Graphics/PostProcessing/` 放一个 `MonoShaderPass` 子类，把参数写进 `ApplyParameters`；
3. 想在 `/monoshader capture fx` 里也能开关它，就在 `MonoScreenFx.Names` 与 `Create` 各加一行；
4. 新 `.fx` 会被构建自动编成 `.fxc`（脚本递归扫 `Assets/Effects/**/*.fx`）。

#### 还缺什么

Bloom（亮部提取 + 降采样 + 加性合成）与 Distortion（需要第二张采样槽）都还没有；`RenderQuad` 已经
能画进任意尺寸的目标，所以做 Bloom 的路是通的。Pixelate 不做——Luminance 已经有了。

---

## 6. 图元层深入：从点列到三角形

以一条拖尾为例，完整数据流：

```
① 输入：IReadOnlyList<Vector2> positions（世界坐标，常见来源是 Projectile.oldPos）
        │  RenderTrail() 先判断 dedServ / null / Count < 2 / Ready()
        ▼
② 重采样：写进静态 scratch[]，共 actualCount 个"屏幕坐标"点
        · Smoothen ? Catmull-Rom 样条（首尾端点精确落在输入点上）
        · 否则最近索引取样，并丢掉 Vector2.Zero（未初始化的 oldPos 槽位）
        · 每个点加 OffsetFunction(t)，再按空间减 Main.screenPosition（仅 World）
        ▼
③ 组装顶点：每个点推 2 个顶点（法线左右各一个）
        · 法线 = 切线的 90° 旋转；切线 = 当前点指向下一个点（末点用前一段）
        · width = WidthFunction(t)，左右各展开 width/2
        · 附加数据位：左 (t, 0, width)、右 (t, 1, width)
        ▼
④ 组装索引：每相邻两点一个四边形（6 个索引）→ 共 2N 顶点、6(N−1) 索引
        ▼
⑤ Flush()：选矩阵 → SetData(Discard) 上传 → 绑缓冲 → 设状态
           → DrawIndexedPrimitives 一次画完 → finally 恢复状态
```

**每点两个顶点 + 相邻两点连四边形**，是为了让宽度可以逐点不同；`SpriteBatch` 一次绘制只有一个矩形，
做不到这件事。**用 Catmull-Rom 而不是线性插值**，是因为点列通常来自 `oldPos`，间距不均且可能有跳变，
线性插值会让条带出现折角。端点用镜像外插（`2P₀ − P₁`）保证曲线精确经过首尾点。

---

## 7. 容量、索引与降级

统一预算：**`MaxPoints = 4096`**，顶点缓冲 16384，索引缓冲 24576。

| 形状 | 顶点 | 索引 | 上限时的实际用量 |
|---|---|---|---|
| 拖尾（N 点） | `2N` | `6(N−1)` | N=4096 → 8192 / 24570 |
| 圆环（P 段） | `2(P+1)` | `6P` | P=4095 → 8192 / 24570 |
| 实心圆（S 切片） | `3S` | `3S` | S=4096 → 12288 / 12288 |
| 多边形填充（凸，n 顶点） | `n+1` | `3n` | n=4095 → 4096 / 12285 |
| 多边形填充（凹，耳切） | `n` | `3(n−2)` | n=4095 → 4095 / 12279 |
| 多边形描边 / 开放折线 | `2n` | `6n` 或 `6(n−1)` | n≤2047 → 4094 / 12282 |
| 胶囊 | `2·capSegments+3` | `3(2·capSegments+2)` | capSegments=64 → 131 / 390 |
| 虚线 | `4 × 段数` | `6 × 段数` | 受 `HasRoomFor` 逐段检查 |

最大顶点索引 16383 小于 `short` 上限 32767，所以索引用 `short` 是安全的。

**降级策略一律是"安静截断"**：所有入口都过 `HasRoomFor`，超限就少画一点；虚线在画不下时提前退出。

---

## 8. 性能红线

来自蓝图 §11.10，结合本系统的落点：

| 项目 | 红线 | 本系统的做法 |
|---|---|---|
| `SpriteBatch` Begin/End 切换 | 每帧 < 10 次 | 提供助手但不隐式切换；粒子按混合状态分组，避免每组一次以上 |
| 渲染目标切换 | 每帧 < 20 次 | 池化 + ping-pong 两块 |
| 纹理切换 | 尽量 0 | 图集（尚未实现） |
| 每帧纹理上传 | 0 | 没有 `SetData` 到纹理的路径 |
| 全屏 RT 数量 | ≤ 2 张全分辨率 | ping-pong 恰好两块 |
| 后处理 Pass 数 | ≤ 5 | 由调用方控制 |
| 着色器变体 | 单文件 ≤ 4 个 technique | 由资产方控制 |
| 每次形状的 GPU 提交 | — | **一次**，这是图元层存在的全部意义 |
| 绘制路径上的托管分配 | 0 | 静态 `scratch` / `quadCorners` / `scratchList`；虚线整条一个网格；弧与贝塞尔不 `new[]` |

---

## 9. 已知缺口与历史

下面每一条都是"代码里就是这样"，不是"应该这样"。

### 未实现

| 项 | 现状 | 缺口 |
|---|---|---|
| 着色器热重载 | 构建期链路与运行时 `/monoshader reload` 都在，顺序是"编到临时文件 → 构造成功才替换 → 原子写回" | **运行期那条通路还没在游戏里逐步确认过**。日志里"已停用 Luminance 的 3 个文件监视器"来自每秒一次的独立钩子，不能当作"重载成功过"的证据 |
| Metaball 的 CPU 轮廓提取 | GPU 路线已实现，`DrawFallback` 降为降级路径 | 蓝图中"可选的 Marching Squares"仍未做，只有需要"球体参与物理碰撞"时才值得 |
| 界面空间粒子的两条限制 | 见 §5.8 | 需要界面侧自定义混合或着色器时得自己在 `End()` / `Begin()` 之间画 |
| 相机跟随 | 平滑跟随 + 死区 + 前瞻 + 漂移守卫已实现，默认关闭 | 速度自适应缩放没做，理由是硬性的（见 §5.10） |
| 纹理工具 | 无 | 图集 / 描边生成 / 调色板替换 / 9-slice / 预渲染动画贴图全部未做 |
| 着色器资产 | 9 个：图元默认、融合球两个、屏幕特效六个 | 缺 Bloom 与 Distortion；**且 6 个屏幕特效只经过编译与参数核对，没有逐条目视确认过**——`/monoshader capture fx <名字> on` 就是为这件事准备的 |
| 融合球的形状 | `MonoMetaballBrush` 只有 `Ball` | 一个实例可以用 `DrawField` 摆出复杂形状，但**单颗非圆**不行 |
| 屏幕裁剪 | `Flush` 没有设 `ScissorRectangle` | 屏幕外的图元照样进光栅化。要做就得用自己持有的 `RasterizerState` 实例，不能改 FNA 的共享静态对象 |
| 屏幕特效的质量档位 | 只有代码侧的 `Enabled` 静态总开关 | 蓝图要求 ModConfig 里的低/中/高与关闭项，**没有** |
| 测试 | 没有留在仓库里的自动化测试 | `Triangulate` 的修复用执行证据收尾过（脚本切片真实源码 + 10 组用例），但那份脚本没有留下 |

### 已知缺陷（未修）

| 项 | 说明 |
|---|---|
| 震动与相机位置被截断成整像素 | 原版在 `ModifyScreenPosition` 之后立刻取整，所以亚像素的震动与跟随做不到 |
| 屏幕后处理只覆盖世界，不覆盖 HUD | 设计取舍，但确实是"屏幕后处理"这个名字下最容易误解的一条 |

### 已修复缺陷（历史记录）

留这张表是因为每一行都对应一条可复用的教训。完整叙述见蓝图 §18.3。

| 缺陷 | 症状 | 修法 |
|---|---|---|
| 着色器链 **0% 可用** | 编不过又不报错：`.fx` 从没被编过，注册的资产路径还都带尾点 | csproj 的条件改成肯定式判断、脚本改用 `/T fx_2_0`、去扩展名改用 `path[..^Path.GetExtension(path).Length]` |
| `MonoPostFxPipeline.Apply` 条件写反 | 有启用的 pass 时反而直接返回，所有 pass 永不执行。**编译零警告** | 改成 `== 0`，并补 `Main.dedServ` 早退 |
| `LoadForMod` 抛 NRE | `Mod.GetFileNames()` 对没有 `.tmod` 的模组返回 **null** 而不是空表 | 先判空再遍历 |
| `MonoUtil.Polygon.Triangulate` 顺时针输入索引错位 | 耳切在副本的索引空间里算，输出却当成输入编号；顺时针 L 形的三角形面积合计 9 而多边形只有 5 | 加一层"副本索引 → 输入索引"的映射；修复后 10/10 用例通过，输出绕行恒为逆时针 |
| `MonoPostFxPipeline.Apply` 每帧分配数组 | `passes.Where(...).ToArray()` | 换成复用的 `List` 字段 |
| `.ps1` 的 BOM 被编辑工具静默丢掉 | 构建报 `MSB3073`，报错完全不提编码 | 加回 BOM；根治办法是构建优先调用 PowerShell 7 |
| `fxc` 报 `X3000 (1,1)` | 源文件带 BOM | 重载器与构建脚本都先规范化，仓库里的 `.fx` 也存成无 BOM |
| 同名 `.fxc` 与 `.xnb` 并存 | 枚举顺序没有保证，旧 `.xnb` 可能盖掉新 `.fxc` | 分两趟扫（`.fxc` 优先），并打一条聚合日志说明忽略了几个 `.xnb` |
| Luminance 的文件监视器停不掉 | 日志报"`ShaderWatchers` 字段找不到"，实际它**一直是属性** | 属性优先、字段兜底，并缓存解析结果 |
| 粒子画成一条横跨全屏的竖线 | `TextureAssets.MagicPixel` 是 1×1000 而不是 1×1 | 记住尺寸；要固定像素大小就用矩形重载 |
| `.fx`-only 重载后 Luminance 握着已释放的 `Effect` | 同一个 `Effect` 被两边注册表共享，而 `Dispose`/`Replace` 会释放它 | `MonoShader` 明确不拥有 `Effect`，并在重载后同步 Luminance 注册表 |
| `RenderQuad` 的投影写死成屏幕尺寸 | 把四边形画进非全屏目标时只盖住一部分，角落留着上次的内容 | 补 `projectionWidth` / `projectionHeight` 两个可选参数 |

### 不一致

| 项 | 说明 |
|---|---|
| `ModSystem` 钩子混用 `Load/Unload` 与 `OnModLoad/OnModUnload` | 两者都会被调用，目前没有实际差别。但 `OnModLoad` 保证在所有内容 autoload 完成之后，将来哪个系统需要查内容表时必须换成它 |
| `RenderTargets` 目录名与蓝图目标布局的 `Targets/` 不一致 | 整个图形模块都在 `Core/Graphics/`，迁移单独一轮做（见蓝图里的"已知偏差"） |

---

## 10. 常见错误速查

| 症状 | 最可能的原因 | 怎么查 |
|---|---|---|
| **什么都不显示** | ① `MonoPrimitiveRenderer` 未就绪 ② 坐标空间搞错，形状被画到视口外 ③ 顶点数或索引数为 0 | 看日志里有没有"图元渲染器：…"的警告；先试 `Screen` 空间 + 屏幕中心画一个圆 |
| **分辨率一变/缩放一改就飘** | 坐标空间与实际坐标不符 | 明确传 `space:`：世界坐标 → `World`，`Main.screenPosition` 相对坐标 → `Screen` |
| **画在 UI 里但位置完全不对** | 用了 `Screen` 而不是 `UI`（少了 `Main.UIScaleMatrix`），或不在 UI 绘制阶段 | 换 `UI`；确认调用点 |
| **画面撕裂 / 精灵错位** | 在已 begun 的批次里做了原始绘制 | 用 `MonoSpriteBatchExtensions` 把绘制夹在 `End`/`Begin` 之间 |
| **改了 `.fx` 但效果没变** | 没有重新编译：运行时只加载 `.fxc` | 重跑构建，或游戏里 `/monoshader reload <名字>` |
| **改了参数但着色器没反应** | 参数缓存在起作用，或热重载后 `EffectParameter` 还是旧的 | 调 `ResetParameterCache()`；确认走了 `Replace()` |
| **`Rent` 返回的东西一用就崩** | 专用服务器上 `Rent` 返回 `null` | 判空 |
| **服务器上访问受管目标就崩** | 专用服务器没有 `GraphicsDevice` | 用 `MonoManagedRenderTarget.IsAvailable` 提前分支 |
| **受管目标注销了却还能用** | 只调了 `Dispose()`——它只丢纹理，不注销 | 要永久注销请调 `Retire()` |
| **加载时 `LoadForMod` 抛 NRE** | `Mod.GetFileNames()` 可能返回 `null` | 已修；你在别处用 `GetFileNames()` 时同样要判空 |
| **粒子画成一条横跨全屏的竖线** | 用了 `TextureAssets.MagicPixel` 当贴图 | 见 §5.8 |
| **`/monoshader reload` 报 `X3000`，位置是 `(1,1)`** | `.fx` 带 BOM | 重载器会自动规范化，但源文件本身也应存成无 BOM UTF-8 |
| **重载成功但下次启动还是旧效果** | 同名 `.xnb` 与 `.fxc` 并存 | 已修：`.fxc` 优先，且写回时删掉同名 `.xnb` |
| **后处理链一点效果都没有** | ① 在专用服务器上 ② 所有 pass 都 `Enabled = false`（此时它故意退化成整幅拷贝）③ pass 自己没把结果画进 `destination` | 先用 `Tint = Color.Red` 的 `MonoTintPass` 替换整条链，证明"中间目标 + ping-pong + 状态恢复"是通的 |
| **屏幕后处理没反应（世界颜色没变）** | ① 忘了 `Register` ② "复古光照"开着 ③ 全屏地图 / 截图相机模式 / `netMode == 2` ④ 没有启用的 pass | `/monoshader capture test on` 先验证通路；再看 `/monoshader capture status` 里"本帧是否已申请捕获" |
| **某一条内置特效看不出效果** | ① `colorgrade` 的默认参数完全恒等 ② 着色器没注册 | `/monoshader capture fx` 看状态；再用 `/monoshader capture fx <名字> on` |
| **淡出不起作用** | 着色器里把淡出参数写成了 `opacity` | 用 `fxOpacity`（§5.13） |
| **自定义 pass 只画了一部分 / 角落有残留** | 自己拼四边形时投影还是按屏幕尺寸算的 | 走 `MonoScreenCapture.DrawFullscreen`，或显式给 `projectionWidth` / `projectionHeight` |
| **开启相机跟随后画面乱飞** | 另一个模组也在**绝对**设置 `screenPosition`，谁后跑谁赢 | 看日志里有没有漂移守卫那条警告；冲突就让 `Following = false` |
| **显存缓慢上涨** | 用池却忘了 `Dispose` 租约；或某处直接 `new RenderTarget2D` | 租约一律 `using`；受管目标交给管理器 |
| **`/monocore` 跑到了图形命令里（或反之）** | 两个 `ModCommand` 的触发词**第一段**撞了同一个键。tML 按第一段做键，后注册的永远接不到且不报错 | 已修：触发词改成两个不同的词 `monocore` / `monoshader`，见 `Core/README.md` §7.4 |
| **受管目标一直不回收** | 有代码每帧访问 `.Target`/`.Width`/`.Height`，把闲置计时器刷回 0 | 别在轮询里碰 `Target` |
| **粒子画了两遍** | `DrawDust` 在开了 `CaptureEntities` 的绘制设置下会被调用两次 | 与 Luminance 一致的既有特性；需要严格不重影时改用单一钩子 |

---

## 附录：Z（像素跨度）验证配方

**结论：已验证通过。** 实测是一条从尾巴到头由细变粗的楔形，颜色由暗红渐变到亮青，与预测逐项吻合：

| 观察 | 预测 | 实测 |
|---|---|---|
| 形状 | 宽度 4 px → 64 px 的楔形 | 阶梯状楔形（阶梯来自 61 个采样点） |
| 左端颜色 | G≈16/255、B≈0 → 暗红 | 符合 |
| 右端颜色 | G≈255、B≈255 → 亮青 | 符合 |
| 沿厚度方向 | R 从 0 到 255 | 符合 |

也就是说 `TextureCoordinates.z` 确实等于"截面 0→1 跨越的像素距离"，量纲与数值都对。
以后动过顶点写入或 `StandardPrimitive.fx`，重跑一次即可。

**为什么必须用变宽拖尾**：`Solid(32f)` 的 Z 处处都是 32，无论它是"像素宽度""半宽"还是"常数"，
画面都长得一样——等宽拖尾证明不了这件事。

### 配方

放在一个合法的绘制点上（必须是 `spriteBatch.End()` 与下一次 `Begin()` 之间；`On_Main.DrawDust`
之后是现成的位置，`RenderTrail` 内部会立刻 `Flush`，它不排队）。坐标是屏幕坐标，
`MonoGraphicsSpace.Screen` 的投影不含缩放，所以画出来的厚度就是设备像素数：

```csharp
const int N = 61;                                    // 等距密集点 → t 同时等于索引比与归一化弧长
Vector2[] axis = new Vector2[N];
for (int i = 0; i < N; i++)
    axis[i] = new Vector2(100f + 1000f * i / (N - 1f), 300f);   // 横平竖直：厚度 = 宽度函数

MonoShader prim = MonoShaderManager.GetShader(MonoPrimitiveRenderer.DefaultShaderName);
prim.SetParameter("debugMode", 1f);
MonoPrim.RenderTrail(axis, new MonoPrimitiveSettings(
    WidthFunction: t => 4f + 60f * t,
    ColorFunction: _ => Color.White,
    Smoothen: false,
    Shader: prim,
    Space: MonoGraphicsSpace.Screen));
prim.SetParameter("debugMode", 0f);                  // 参数带缓存，必须显式还原
```

`debugMode` 下每个像素是 `float4(coord.y, coord.z / 64, coord.x, 1)`，所以应该看到：**R 在同一个 x 上
沿厚度从 0 到 255**（证明 Y 是左缘 0 / 右缘 1）、**G 在同一个 x 上恒定，只沿长度从 ≈16 爬到 ≈255**
（这就是 Z）、**B 沿长度从 0 到 255**。

| 位置 | 全宽 | 预期 G |
|---|---|---|
| 尾巴 `t = 0` | 4 px | 16/255 |
| 中间 `t = 0.5` | 34 px | 135/255 |
| 头 `t = 1` | 64 px | 255/255 |

### 交叉验证：用尺子，不信着色器

把 `debugMode` 关回 0、颜色改白，截图，直接量条带在 `x = 100` 与 `x = 1100` 处的竖直像素厚度。
预测：尾巴 4 px、头 64 px，中间线性。

这一步的价值在于两个数字来源完全不相干：厚度来自 CPU 组出来的三角形条带与光栅化，
G 来自顶点 Z 属性经着色器换算。两边对上，契约才成立。

| 假设的缺陷 | G 的表现 | 与厚度是否对得上 |
|---|---|---|
| Z 恒为 0 | 全黑 | 对不上 |
| Z 是常数（1 或 32） | 一条水平纯色（≈4 或 128） | 对不上 |
| Z 是半宽而不是全宽 | 8 → 128 | 对不上（厚度量出来仍是 4→64） |
| Z 被 zoom 乘过 | 跟着缩放变 | 只有 zoom = 1 时才对得上 |
| Z 正确 | 16 → 255 | 对得上 |
