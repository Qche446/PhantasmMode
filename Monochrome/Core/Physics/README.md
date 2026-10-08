# Monochrome 物理模块

质点 + 约束 + 迭代求解。绳索、旗帜、触手、飘带共用同一份求解器，差别只在约束拓扑。

## 1. 它由哪两块组成

| 位置 | 命名空间 | 依赖 tML | 能离线测 |
|---|---|---|---|
| `Common/MonoUtil/Physics/` | `Monochrome.Common.MonoUtil.Physics` | 否 | 是 |
| `Core/Physics/` | `Monochrome.Core.Physics` | 是 | 否 |

求解器那一半只认识 `Vector2`，不读 `Main`、不碰瓦片，所以 `Monochrome.CoreTests` 里能直接跑它（`dotnet run --project Monochrome.CoreTests -p:BuildMod=false`）。驱动与绘制那一半是 tML 面，必须在游戏里验。

## 2. 它在帧里的哪一格

`MonoPhysicsSystem` 挂在 `PostUpdateDusts`（`Main.cs:17584`）。这个时机有两个性质：

- 它位于 `ShouldUpdateEntities()` 闸门之内（`Main.cs:16938`），命中定格与世界时间缩放为 0 时物理跟着世界一起停。
- 它排在 NPC / 玩家 / 弹幕更新之后，所以锚点读到的都是这一 tick 的最终位置。

**只在客户端跑**：`Main.dedServ` 上系统直接返回，不做任何同步。用它驱动玩法（例如绳去拉一个 Boss）时要在权威端自己跑一份并同步"驱动状态"（锚点），不要同步每个质点。

## 3. 部件索引

| 想做的事 | 用哪个 |
|---|---|
| 建一条绳 | `new MonoRope(段数, 段长, 锚点, anchorIndex: 编号)` |
| 每 tick 把锚点摆到实体位置 | `rope.SetAnchor(worldPos)` |
| 交给系统自动推进 | `MonoPhysicsSystem.Register(rope)`（重复注册是空操作） |
| 不再需要它 | `MonoPhysicsSystem.Unregister(rope)` |
| 锚点一 tick 挪太远要整体跟过去 | `rope.TeleportThreshold`（默认 16） |
| 换锚点编号 | `rope.PinAt(index)` |
| 取某个质点的速度 | `rope.Solver.Velocity(i)` |
| 读整条位置列 | `rope.Points`（锚点在 `rope.AnchorIndex`，归一化位置 `rope.AnchorRatio`） |
| 画成一条条带 | `MonoRopeRenderer.Draw(rope, settings)` |
| 画进 RT 再当普通贴图贴回 | `MonoRopeRenderer.DrawToTarget(rope, rt, origin, settings)` |
| 自己写一条约束 | 继承 `MonoConstraint`，再 `solver.AddConstraint(...)` |

## 4. 一条绳的最短路径

```csharp
// 1) 建：16 段、每段 2.5 像素，锚点钉在总段数的三分之一处
MonoRope rope = new(16, 2.5f, anchorWorldPos, anchorIndex: 5);
rope.Damping = 0.94f;                 // 速度保留系数
rope.Gravity = new Vector2(0f, 0.3f); // 每 tick 的加速度

// 2) 交给系统每 tick 推进。步进时机由系统负责，消费者不要自己调 Step()。
MonoPhysicsSystem.Register(rope);

// 3) 每 tick 更新锚点位置（放在任意 Update* 里都行，系统在 PostUpdateDusts 才走）
rope.SetAnchor(player.Center + headOffset);

// 4) 世界相位画出来。t = 质点编号 /（段数 − 1），锚点就在 rope.AnchorRatio 处
Main.spriteBatch.PrepareForShaders();     // 或挂在 On_Main.DrawDust 且先调 orig
MonoRopeRenderer.Draw(rope, new MonoPrimitiveSettings(
    t => 6.5f - 3f * Math.Abs(t - rope.AnchorRatio) / (1f - rope.AnchorRatio),
    _ => Color.White));
Main.spriteBatch.ResetToDefault(end: false);

// 5) 不再需要时摘掉（世界卸载时系统会自己清空）
MonoPhysicsSystem.Unregister(rope);
```

**要画在实体身上（跟绘制层走、接原版染料着色器）时**，条带不能直接画：玩家绘制在 `Main.cs:60063` 就结束了，而原先以为可用的 `DrawDust` 在 60104，晚了一帧。做法是先在 `PostDrawTiles`（60052，批次已 End 且排在玩家绘制之前）把条带画进一张 RT，再在绘制层里用 `DrawData` 把 RT 当普通贴图画回去（飘带就是这么做的）。

## 5. 参数指南

| 参数 | 默认 | 作用 | 实测参考 |
|---|---|---|---|
| `Iterations` | 绳索 16（求解器 8） | 约束投影迭代次数 | 15 段 × 2.5 的链子在锚点 8 / 24 / 40 像素每 tick 下，8 次迭代拉伸 1.71× / 3.07× / 4.44×，16 次 1.35× / 2.09× / 2.57×，32 次 1.13× / 1.51× / 1.62× |
| `TeleportThreshold` | 16 | 锚点单 tick 位移超过它就整链平移 | 正常跑步头部位移 ≤8 像素/tick，冲刺 ≥16；转身时锚点横跳约 20 |
| `Damping` | 1 | 速度保留系数 | 调小更"拖"；跑步时的向后拖曳主要来自它 |
| `Gravity` | (0, 0.35) | 每 tick 的加速度 | 视觉量级，不是物理单位 |
| `Solver.MaxStep` | 16 | 单 tick 位移上限 | 防穿透用，`0` 关闭 |
| `Solver.ProjectionBudget` | 不限制 | 单次 `Step` 的投影次数上限 | 留给将来的全局预算，现在还没接 |

**关于"向后飘"**：不干预时链子的稳态本来就在锚点后面（实测速度 2 / 4 / 6 / 8 像素每 tick 时，尾端分别落后 8.95 / 15.53 / 19.68 / 22.26 像素）。曾经有一个 `AnchorDrag` 想让它"跟手"，做法是把锚点速度叠加到质点位移上，稳态下会把链子推到锚点**前面 47 像素**，已经删除。位置求解器的稳态由约束投影决定，速度层面的干预改不了它，所以这一层没有这类旋钮。

## 6. 这一版做什么、不做什么

**做**：等长链绳索（锚点可落在任意编号）、距离约束、整链平移的抗瞬移、数值自愈（非有限值回到静止参考位置、绳索整条重铺）、条带绘制与"画进 RT 再贴回"的绘制路径、能离线测的纯逻辑核心。

**不做**（都等真实消费者出现再谈）：
- 力场模块——风这类效果写在各自的运动逻辑里
- 布条 / 软体——第二个约束拓扑
- 瓦片碰撞——绳索贴地
- 自碰撞、断裂约束
- LOD（按距离降更新频率）
- 渲染插值——渲染锁在 60Hz，只有降频更新时才需要
- 光照——默认用 `Lighting.AddLight`，自定义累积光属于图形层
- 全局投影预算、距离裁剪、`/monocore physics` 诊断面

## 7. 常见错误速查

| 症状 | 原因 |
|---|---|
| 在 `PlayerDrawLayer` 里画条带，精灵批量错位 | 原始绘制必须落在 `End()` 与下一次 `Begin()` 之间，绘制层里批次开着 |
| 飘带比人物慢一拍 | RT 渲染挂在了 `DrawDust`；玩家在 60063 就画完了，RT 要在 `PostDrawTiles` 填 |
| 链子被拉成好几条带子 | 锚点跑赢链子，约束投影收敛不完；提高迭代次数或降低 `TeleportThreshold` |
| 转身时链子甩一下 | 锚点偏移带 `direction`，转身是约 20 像素的不连续；确认 `TeleportThreshold` 没有设得比它大 |
| 给钉住的质点设速度却没效果 | 逆质量为 0 的质点不参与积分，写它的 `previous` 是空操作 |
| 定格 / 暂停时链子还在动 | 物理挂错了时机，它必须在 `PostUpdateDusts`（闸门内） |
| 专用服务器上崩 | 物理是客户端表现，`Main.dedServ` 上不要建绳 |
