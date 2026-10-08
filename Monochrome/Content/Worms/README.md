# Monochrome 蠕虫段链

给"一串 NPC 连成的虫"用的导航与走法工具。它回答"这条链现在长什么样"，不改任何字段；位置与速度写不写回 NPC 由调用方决定。

## 1. 它由哪几块组成

| 文件 | 管什么 | 依赖 tML | 能离线测 |
|---|---|---|---|
| `MonoWormLayout.cs` | 布局描述：链写在哪些 ai 槽、头尾怎么判 | 否 | 是 |
| `MonoWormFollow.cs` | 体节跟随：定距、蛇形 | 否 | 是 |
| `MonoWormMove.cs` | 头部移动：追击、带转向率的追踪、环绕、驻留、朝向 | 否 | 是 |
| `MonoWormChain.cs` | 走链纯逻辑：找头、取相邻段、段序、段数、连接校验 | 否 | 是 |
| `MonoWormNav.cs` | 上面那层的 NPC 适配，消费者直接用这一层 | 是 | 否 |

前四个只认识 `Vector2` 与 `int`，离线验收台里能直接跑（`dotnet run --project Monochrome.CoreTests -p:BuildMod=false`）。`MonoWormNav` 读 `Main.npc`，只能在游戏里验。

## 2. 它在帧里的哪一格

没有节拍，也没有登记步骤。在体节自己的 `PreAI` 里按需调用——那是天然的逐实体时机。

查询不要挂到 `MonoEventBus` 上：总线是系统级的，每个帧阶段只通知一次，它不知道你说的是哪只 NPC。

## 3. 部件索引

| 想做的事 | 用哪个 |
|---|---|
| 从任意一段找到头 | `MonoWormNav.TryFindHead(npc, layout, out head)` |
| 一次拿全头 / 段序 / 段数 / 前后段 | `MonoWormNav.Describe(npc, layout, out info)` |
| 我是第几段 | `info.Index`，或 `MonoWormNav.IndexFromHead(npc, layout)` |
| 整条虫有几段 | `info.Count`，或 `MonoWormNav.Count(npc, layout)` |
| 收整条虫的 NPC 列表 | `MonoWormNav.Walk(npc, layout, list)` |
| 前一段 / 后一段 | `MonoWormNav.TryGetAhead` / `TryGetBehind` |
| 连接还成不成立 | `MonoWormNav.ValidateAhead` / `ValidateBehind`（`out MonoWormLinkError`） |
| 这一段是不是头 / 尾 | `MonoWormNav.IsHead` / `IsTail` |
| 读一段的字段快照 | `MonoWormNav.Snapshot(npc)` |
| 体节摆到前一段后面 | `MonoWormFollow.FixedSpacing` / `Snake` |
| 头往哪飞 | `MonoWormMove.Chase` / `Steer` / `Orbit` / `Hover` |
| 头的旋转怎么写 | `MonoWormMove.Facing(npc.velocity)` |

## 4. 最短路径

体节 AI 里的完整片段：

```csharp
// 只读：什么都不改，原版跟随物理照常跑
if (MonoWormNav.Describe(npc, MonoWormLayout.Vanilla, out MonoWormChainInfo info))
{
    NPC head = Main.npc[info.HeadIndex];
    int myIndex = info.Index;      // 0 就是头
    int total = info.Count;
}

// 自己接管位置
if (!MonoWormNav.ValidateAhead(npc, MonoWormLayout.Vanilla, out _)
    || !MonoWormNav.TryGetAhead(npc, MonoWormLayout.Vanilla, out NPC? ahead) || ahead is null)
    return true;                    // 链有问题，交回原版

int index = MonoWormNav.IndexFromHead(npc, MonoWormLayout.Vanilla);
if (index == 1)
{
    MonoWormFollow.FixedSpacing(ahead.Center, ahead.rotation, 40f, ahead.velocity,
        out Vector2 center, out float rotation);
    npc.Center = center;
    npc.rotation = rotation;
}
else if (MonoWormNav.TryGetAhead(ahead, MonoWormLayout.Vanilla, out NPC? ahead2) && ahead2 is not null)
{
    MonoWormFollow.Snake(ahead.Center, ahead2.Center, npc.Center, 40f, 1f, npc.localAI[0],
        out Vector2 center, out float rotation);
    npc.Center = center;
    npc.rotation = rotation;
}

return false;                        // 拦掉原版 AI，否则它下一句就把位置盖回去
```

头部 AI 里：

```csharp
// 常规追击
MonoWormMove.Chase(npc.Center, npc.velocity, target.Center, 16f, 0.15f, out Vector2 velocity);
npc.velocity = velocity;

// 或者环绕
MonoWormMove.Orbit(npc.velocity, 20f, 600f, spin, out velocity);
npc.velocity = velocity;

npc.rotation = MonoWormMove.Facing(npc.velocity);
```

断链处置库里只给判据，怎么处理由消费者定。原版世界吞噬怪的做法是头部发现自己身后那一段不成立了就收场：

```csharp
if (MonoWormNav.IsHead(npc, MonoWormLayout.Vanilla)
    && !MonoWormNav.ValidateBehind(npc, MonoWormLayout.Vanilla, out _))
{
    npc.life = 0;
    npc.checkDead();
}
```

## 5. 布局

`MonoWormLayout` 说清"链写在哪些 ai 槽里"。原版那一套就是 `MonoWormLayout.Vanilla`：

| 字段 | 原版预设 | 含义 |
|---|---|---|
| `AheadSlot` | 1 | `ai[1]` 存前一段下标 |
| `BehindSlot` | 0 | `ai[0]` 存后一段下标 |
| `HeadIsAheadless` | true | `ai[1] <= 0` 的那一段就是头 |
| `RequiresSameAiStyle` | true | `aiStyle` 相同才算同一条虫 |

**`AheadSlot` 与 `BehindSlot` 必须都填**，否则 `IsComplete` 为假、所有走链直接失败。`IndexSlot`（段序）与 `TailFlagSlot`（尾标记）可选，填了能省一次走链。

自己的虫用别的槽位时这样写：

```csharp
private static readonly MonoWormLayout MyLayout = new()
{
    AheadSlot = 2,        // ai[2] = 前一段下标
    BehindSlot = 0,       // ai[0] = 后一段下标
    IndexSlot = 3,        // ai[3] = 段序
    TailFlagSlot = 1,     // ai[1] == 1 表示尾
    TailFlag = 1f,
};
```

**头的 NPC 槽位不能是 0 号**：原版把 `ai[1] <= 0` 判成"没有前一段"，0 号槽会被体节误读成头。

## 6. 参数指南

| 参数 | 位置 | 作用 |
|---|---|---|
| `spacing` | `MonoWormFollow` | 两段之间的距离 |
| `strength` | `MonoWormFollow.Snake` | 弯曲强度，越大越硬，通常由头部按当前动作给 |
| `springOffset` | `MonoWormFollow.Snake` | 横向弹簧偏移，由调用方自己推进 |
| `SnakeBearingScale` | `MonoWormFollow` | 蛇形偏移的缩放系数，默认 0.055 |
| `maxSpeed` / `accel` | `MonoWormMove.Chase` | 追速上限与每 tick 加速度 |
| `turnRate` / `speedLerp` | `MonoWormMove.Steer` | 每 tick 允许的最大转向弧度与速率插值比例 |
| `radius` / `direction` | `MonoWormMove.Orbit` | 环绕半径与旋转方向，正数逆时针 |
| `lerp` | `MonoWormMove.Hover` | 驻留时的靠拢速度 |
| `ReverseBoost` | `MonoWormMove` | `Chase` 反向时的加速度倍率，默认 2 |
| `stepCap` | `MonoWormChain` | 走链步数上限，游戏侧固定传 `Main.maxNPCs` |

## 7. 这一版做什么、不做什么

**做**：布局描述、双向走链、连接校验（下标范围 / 目标活着 / `aiStyle` 一致 / 反向指针指回我）、一次走链拿全概况、体节跟随两种、头部移动五种、退化输入的定点返回。

**不做**：
- 重写跟随物理——原版 `AI_006_Worms` 照常跑，库只在你明确接管时把算好的位置给你
- 全局注册表与自动采纳——原版虫由谁登记、什么时候登记，是消费者的事
- 周期巡检与自愈策略——`ValidateAhead` / `ValidateBehind` 只给判据
- 断层 span 模型、Ghost span、无限长虫
- 命令面、渲染、网络包
- 只存"前一段"的布局预设（布局要求两向齐全）

## 8. 常见错误速查

| 症状 | 原因 |
|---|---|
| `TryFindHead` 一直失败 | 布局缺 `AheadSlot`；或链里有段不在了 / `aiStyle` 不一致 / 下标越界 |
| 校验报 `NoBackReference` | 前一段的反向指针没指回我，链其实是断的 |
| `Count` 返回 0 | 布局缺 `BehindSlot`，`IsComplete` 为假，走链直接失败 |
| 写了 `npc.Center` 又被拽回去 | 没拦掉原版 AI，`PreAI` 要返回 `false` |
| 头不动 | 速度算完没写回 `npc.velocity` |
| 头的下标落在 0 号槽 | 原版判据把 `ai[1] <= 0` 当头，0 号槽不能当头的下标 |
| 在 `MonoEventBus` 的回调里拿不到"当前 NPC" | 总线是系统级的，逐实体逻辑要放在体节自己的 `PreAI` 里 |
