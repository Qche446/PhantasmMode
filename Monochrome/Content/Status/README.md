# Monochrome 状态 / Buff 系统

给「一条 buff 上还要带层数、时长、每实体数据，还要接一堆实体级钩子」用。库负责把钩子递到对的地方、把每实体数据装好；层数怎么算、叠不叠、怎么转换、属性怎么变，全写在消费者自己重写的方法里。

前四个文件不引用任何 Terraria 类型，能直接进离线验收台 `Monochrome.CoreTests`。

## 1. 它由哪几块组成

| 文件 | 管什么 | 依赖 tML | 能离线测 |
|---|---|---|---|
| `MonoBuffSet.cs` | 每实体槽表：把原版 `buffType` 投影成「谁活着、排第几」，给出进入 / 离开 / 断档 | 否 | 是 |
| `MonoBuffSlotData.cs` | 每 (实体, 状态) 的伴生数据：`Ai` 数组与可选 `Custom` | 否 | 是 |
| `MonoBuffTime.cs` | 时长算术：叠加后夹进 `[0, max]` | 否 | 是 |
| `MonoRoman.cs` | 罗马数字转换 | 否 | 是 |
| `MonoBuff.cs` | 抽象基类（`: ModBuff`）：槽位、`AiSize`、`CreateState`、生命周期与实体级钩子 | 是 | 否 |
| `MonoBuffSystem.cs` | 装配点：加载期按内容顺序分配槽位，提供投影用的类型解析 | 是 | 否 |
| `MonoBuffPlayer.cs` | 玩家宿主：持槽表，按槽位派发玩家侧钩子 | 是 | 否 |
| `MonoBuffNPC.cs` | NPC 宿主：同上，派发 NPC 侧钩子 | 是 | 否 |
| `MonoBuffAccess.cs` | 消费入口：`player.MonoBuffs()` / `npc.MonoBuffs()` 与泛型查询 | 是 | 否 |
| `MonoBuffControl.cs` | 原版 buff 数组的操控：读 / 设 / 叠加时长、施加、移除、转换 | 是 | 否 |
| `MonoBuffIcon.cs` | 图标角标：右下角锚点与画字方法 | 是 | 否 |
| `MonoBuffSlot.cs` | `MonoBuffSlot<T>.Index` 静态入口 | 是 | 否 |

## 2. 它在帧里的哪一格

**槽表在每个 tick 的 `ResetEffects` 里投影一次**（玩家宿主与 NPC 宿主都在这一格）。这一格早于原版对 buff 数组的更新：`Player.Update` 里 `ResetEffects`（22489）在前、`UpdateBuffs`（22553）在后；`NPC.UpdateNPC` 里 `NPCLoader.ResetEffects`（88391）在前、buff 更新（90046）在后。

由此定下三条节拍：

- **同一 tick 中途施加或移除的状态，要到下一 tick 才可见。** `OnApplied` 与 `OnRemoved` 也落在这个节拍上。
- **投影会断档。** 实体本身就会跳过这一格——多人客户端上不在已加载区块的 NPC 在 `UpdateNPC` 里提前 return，早于 `ResetEffects`。两次投影间隔超过一 tick 时按断档处理：旧数据作废、当前还在的当作刚施加、离开的不补发 `OnRemoved`。
- **「有没有这个状态」的真相源是原版 `buffType` 数组。** 右键取消、`ClearBuff`、死亡重置、多人同步都只改那一份；槽表只做它的投影。

钩子派发的对应关系：

| 宿主钩子 | 派发到 `MonoBuff` 的 | 时机 |
|---|---|---|
| `ModPlayer.ResetEffects` | `ResetEffects(Player)`，另加生命周期与 `Apply` / `Remove` | 帧初，属性重置之后 |
| `ModPlayer.PostUpdateEquips` | `PostUpdateEquips(Player)` | 装备结算之后 |
| `ModPlayer.UpdateBadLifeRegen` | `UpdateBadLifeRegen(Player)` | 生命再生结算 |
| `ModPlayer.OnHurt` | `OnHurt(Player, info)` | 玩家受击 |
| `ModPlayer.DrawEffects` | `DrawEffects(Player, ...)` | 玩家绘制 |
| `GlobalNPC.ResetEffects` | `ResetEffects(NPC)`，另加生命周期与 `Apply` / `Remove` | 帧初 |
| `GlobalNPC.AI` | `AI(NPC)` | 每 tick |
| `GlobalNPC.UpdateLifeRegen` | `UpdateLifeRegen(NPC, ref damage)` | 生命再生结算 |
| `GlobalNPC.ModifyIncomingHit` | `ModifyIncomingHit(NPC, ref modifiers)` | 受击结算 |
| `GlobalNPC.DrawEffects` | `DrawEffects(NPC, ref drawColor)` | 染色 |
| `GlobalNPC.PreDraw` / `PostDraw` | `PreDraw` / `PostDraw(NPC, ...)` | 本体绘制前后 |
| `GlobalNPC.OnKill` | `OnKill(NPC)` | 死亡 |

tML 自己在 `ModBuff` 上的钩子直接重写即可，库不中转：`Update(Player/NPC, ref buffIndex)`、`ReApply`、`ModifyBuffText`、图标 `PreDraw` / `PostDraw`、`RightClick`。

派发是每个钩子一趟槽表遍历，只在状态活着的时候调用；身上没有状态时，每个钩子的成本就是走一遍布尔数组。

## 3. 部件索引

| 想做的事 | 用哪个 |
|---|---|
| 写一个状态 | 继承 `MonoBuff`，有钩子就重写 |
| 问实体身上有没有这个状态 | `npc.MonoBuffs().Has<MyBuff>()` |
| 取这个实体上的伴生数据 | `npc.MonoBuffs().Data<MyBuff>()` |
| 建一份伴生数据 | `npc.MonoBuffs().GetOrCreateData<MyBuff>()` |
| 每实体参数数组 | `MonoBuffSlotData.Ai`，长度由 `AiSize` 定 |
| 每实体结构化数据 | 重写 `CreateState()` |
| 知道它在原版数组里排第几 | `MonoBuffSet.BuffIndex(slot)` |
| 读 / 设 / 叠加剩余时长 | `MonoBuffControl.TimeLeft` / `SetTime` / `AddTime` |
| 施加 / 移除 / 转换 | `MonoBuffControl.Give` / `Take` / `Replace` |
| 图标右下角画层数 | `MonoBuffIcon.DrawCount` / `DrawRomanCount` |
| 图标右下角画别的东西 | `MonoBuffIcon.BadgeAnchor` 配 `DrawBadge` |
| 罗马数字 | `MonoRoman.ToRoman` |
| 槽位 | `MonoBuffSlot<T>.Index` |

## 4. 一个状态的最短路径

```csharp
public class MyBuff : MonoBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    // 叠加：怎么叠自己写。返回 true 表示已处理完，原版不再动 buffTime。
    public override bool ReApply(Player player, int time, int buffIndex)
    {
        MonoBuffSlotData data = player.MonoBuffs().GetOrCreateData<MyBuff>();
        data.Ai[0] += 1f;                       // 层数放 Ai[0]

        int left = MonoBuffControl.TimeLeft(player, Type);
        MonoBuffControl.SetTime(player, Type, left > time ? left + time / 2 : left / 2 + time);
        return true;
    }

    // 原版钩子直接重写
    public override void Update(Player player, ref int buffIndex)
    {
        int level = (int)(player.MonoBuffs().Data<MyBuff>()?.Ai[0] ?? 1f);
        player.statDefense -= level;
    }

    // 实体级钩子：库按实体派发，只在状态活着时被叫到
    public override void UpdateBadLifeRegen(Player player) { /* 玩家 DoT 写这里 */ }
    public override void UpdateLifeRegen(NPC npc, ref int damage) { /* NPC DoT 写这里 */ }

    // 图标角标
    public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
    {
        int level = (int)(Main.LocalPlayer.MonoBuffs().Data<MyBuff>()?.Ai[0] ?? 1f);
        MonoBuffIcon.DrawRomanCount(spriteBatch, drawParams, level);
    }
}
```

施加、移除、转换：

```csharp
MonoBuffControl.Give(npc, ModContent.BuffType<MyBuff>(), 300);
MonoBuffControl.Take(npc, ModContent.BuffType<MyBuff>());
MonoBuffControl.Replace(npc, ModContent.BuffType<ABuff>(), ModContent.BuffType<BBuff>(), 300);
```

图标角标跟着剩余时间一起画在 buff 栏上，所以 `PostDraw` 里取的是 `Main.LocalPlayer` 的层数。

## 5. 参数指南

| 参数 | 位置 | 作用 |
|---|---|---|
| `AiSize` | `MonoBuff` | 伴生数据里 `Ai` 的长度，默认 4 |
| `CreateState()` | `MonoBuff` | 返回每实体的结构化数据，装进 `Custom`；默认 `null` |
| `Ai` | `MonoBuffSlotData` | 每实体参数数组，用法对标 `NPC.ai` |
| `Custom` | `MonoBuffSlotData` | 每实体自定义对象 |
| `slotCount` | `MonoBuffSet` | 槽位总数，由宿主按注册数传 |
| `tick` | `MonoBuffSet.Refresh` | 投影用的时基，宿主传 `Main.GameUpdateCount` |
| `max` | `MonoBuffControl.AddTime` | 时长上限 |
| `time` / `quiet` | `Give` / `Replace` | 施加时长与是否弹提示 |
| `scale` / `textColor` / `outlineColor` | `MonoBuffIcon` 的画字方法 | 角标字号与配色，默认 0.75 / 白 / 黑描边 |

## 6. 这一版做什么、不做什么

**做**：抽象基类与 tML 自动注册、玩家与 NPC 两套实体钩子派发、每实体伴生数据（`Ai` 与 `Custom`）、按 tick 的投影与进入 / 离开、断档作废、六个 buff 操控方法、图标角标与罗马数字。

**不做**：

- 声明式的状态定义——层数模式枚举、数值聚合管线、互斥组与优先级、驱散类别、免疫标签，全部由消费者在重写方法里自己写
- 状态实例的网络同步。现有消费者都走原版 `AddBuff`，原版自会同步 buff 本身；自定义层数不跨端
- 图标汇总、悬浮列表、超出槽位上限的处理
- 命令面与运行期诊断

## 7. 常见错误速查

| 症状 | 原因 |
|---|---|
| 重写了钩子却没被调用 | 基类写成了 `ModBuff`；这样是纯原版行为，库不会派发任何东西 |
| `Data<T>()` 一直是 `null` | 状态不在身上，而且没人调用过 `GetOrCreateData` |
| 层数在 buff 结束后没归零 | 同一 tick 内「过期又被加上」时投影看不到中间那段消失；或者实体停更过又回来 |
| 拿缓存的下标去改 `buffTime` 改错了条 | `DelBuff` 会把数组往下压实，旧下标失效；用 `MonoBuffControl` 或当场 `FindBuffIndex` |
| 状态在槽表里看起来还活着 | `Has` 读的是本 tick 的投影；同一 tick 内移除的，要到下一 tick 才反映出来 |
| 角标位置不对 | 锚点取的是 `Position` 加 `SourceRectangle` 的尺寸；改 `Position` 会把角标一起挪 |
| NPC 身上层数永远不涨 | 层数只在施加时由外部拷进去，见消费方的写法 |
