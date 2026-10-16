# Content/Damage —— 伤害与防御管线

原版的伤害是一串钩子改出来的：谁先跑由模组加载顺序决定，写代码的人推不出来。这个模块提供一张**规则表**——消费者把减伤、增伤、元素倍率、破甲写成规则挂上去，库按固定次序遍历它们，并把每条改了什么都记下来。

规则跑完之后，最终那个数仍然由原版算。库只把累加器映射回 `HitModifiers` / `HurtModifiers` 已有的字段，第三方模组直接改这些字段的效果照旧生效，两边是叠加关系。

纯逻辑半边在 `Common/MonoUtil/Damage/`，不依赖游戏运行时，有离线用例覆盖。这一层负责与游戏接线。

---

## 1. 上手

在 `Mod.Load` 里登记规则：

```csharp
MonoDamage.Register(new MonoConditionalReductionRule(0.4f, targetType: NPCID.KingSlime));
MonoDamage.Register(new MonoBurstGuardRule(450, targetType: NPCID.KingSlime));
MonoDamage.Register(new MonoAbsorbCapRule(0.9f, targetType: NPCID.KingSlime));
```

冻结时机两种选法：显式在 `PostSetupContent` 末尾调 `MonoDamage.Freeze()`；不调的话第一次命中时自动冻结。冻结之后登记的规则会被拒绝，次数记在 `MonoDamage.Chain.LateRegistrationRejected`。

给弹幕或物品挂元素标签：

```csharp
projectile.GetGlobalProjectile<MonoProjTags>().Add(MonoDamageTag.Fire);
item.GetGlobalItem<MonoItemTags>().Add(MonoDamageTag.Physical | MonoDamageTag.Fire);
```

自定义元素：加载期登记一次，位号由库分配，消费者不写。

```csharp
int bit = MonoDamageTag.Register("Void");
ulong voidMask = MonoDamageTag.MaskOf("Void");
```

写自己的规则：继承 `MonoDamageRule`，声明段号，改累加器。

```csharp
public sealed class MyPhaseDR : MonoDamageRule
{
    public override MonoDamageStage Stage => MonoDamageStage.Reduction;
    public override int TargetType => NPCID.MoonLordCore;

    public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values)
    {
        if (!MonoDamageSource.TryTargetNpc(info, out NPC npc) || npc.ai[0] <= 2f)
            return;

        values.ReductionAdd += 0.4f;   // 减伤段写加算池；按比例削减伤写 ReductionScale
    }
}
```

规则表按（段号，`Order`，登记顺序）定序。`Order` 只在同段内起作用，默认 0。

---

## 2. 段

段号是**分类标签**，决定遍历顺序与诊断里的分组，不改变数值结果：各段写的是 `HitModifiers` 上互不相交的字段，真正把它们合起来的是原版 `GetDamage`，而加法与乘法各自可交换、取最小与取最大都幂等。库唯一固定下来的是"夹取在收尾做"，那由 `MonoDamage.Run` 在规则跑完之后无条件调 `Clamp()`，与规则挂在哪个段没有关系。

| 段 | 中文名 | 这一段的规则通常写哪个字段 |
| --- | --- | --- |
| `Source` | 源伤缩放 | `SourceMul` |
| `Flat` | 固定加伤 | `FlatBonus` |
| `PercentAdd` | 百分比加伤 | `PercentAdd`（加法池） |
| `PercentMul` | 独立乘区 | `DamageMul` |
| `Crit` | 暴击 | 原版字段（`SetCrit` / `DisableCrit`） |
| `Defense` | 防御 | `DefenseFlat` / `DefenseMul` |
| `Reduction` | 减伤 | `ReductionAdd`（加算池），削减写 `ReductionScale` |
| `Penetration` | 穿透 | `ScalingArmorPenetration` / `ArmorPenetration` |
| `Immunity` | 免疫 | `Nullified` |
| `Clamp` | 上下限 | `MinRatio` / `MaxRatio` / `MaxDamage` |
| `Post` | 收尾 | 需要看到夹过之后乘区的规则 |

`DamageMul` 是库自己的累加器，`Clamp` 段夹的就是它。规则直接往原版 `FinalDamage` 写的乘区不在封顶计算里，库看不见它们。

---

## 3. 钩子映射

```
NPC 侧（玩家打怪）
  GetIncomingStrikeModifiers        原版铺底：takenDamageMultiplier、defense
    → CombinedHooks.ModifyPlayerHitNPCWithItem / ModifyHitNPCWithProj
        → GlobalNPC.ModifyHitByItem / ModifyHitByProjectile   ← 规则表在这里跑
    → 原版按类型的零散调整
    → ToHitInfo → GetDamage           原版算最终数，clamp 到 _damageLimit
    → GlobalNPC.OnHitByItem / OnHitByProjectile               ← 补记结算值

玩家侧（怪打玩家 / 环境）
  PlayerLoader.ModifyHurt
    → 来源是弹幕：ModPlayer.ModifyHitByProjectile   ┐
    → 来源是 NPC ：ModPlayer.ModifyHitByNPC         ├ 规则表在这里跑
    → 来源不明    ：跳过                            ┘
    → ModPlayer.ModifyHurt（无条件）                 ← 只有来源不明时才在这里跑
    → ToHurtInfo → GetDamage
    → ModPlayer.OnHurt                               ← 补记结算值
```

`ModifyIncomingHit` 没有挂。它在 `GetIncomingStrikeModifiers` 内部执行，早于调用方拿到物品或弹幕，因此给不出伤害标签与来源，而按元素或按来源判定的规则正需要这两样。需要覆盖那条路径的消费者可以自己挂它，并调用公开的 `MonoDamage.Run(in info, ref modifiers)`。

---

## 4. 内置规则

都只在消费者显式登记之后生效，参数与数值全由消费者给。

| 类 | 段 | 落点 | 主要参数 |
| --- | --- | --- | --- |
| `MonoFlatArmorRule` | Defense | `DefenseFlat` | 护甲增减（负数为破甲） |
| `MonoElementMultiplierRule` | PercentMul | `DamageMul` | 标签掩码、倍率（小于 1 抗性，大于 1 弱点） |
| `MonoConditionalReductionRule` | Reduction | `ReductionAdd` | 减伤比例、触发条件 |
| `MonoDynamicReductionRule` | Reduction | `ReductionAdd` | 理想时长、基准减伤、量级系数、上下限 |
| `MonoHpRatioReductionRule` | Reduction | `ReductionAdd` | 满血减伤上限、曲线指数 |
| `MonoReductionScaleRule` | Reduction | `ReductionScale` | 减伤保留系数（0.5 = 砍一半） |
| `MonoPenetrationRule` | Penetration | `ScalingArmorPenetration` / `ArmorPenetration` | 比例穿透、固定穿透 |
| `MonoBurstGuardRule` | Clamp | `MaxDamage` | 单次伤害上限（点） |
| `MonoAbsorbCapRule` | Clamp | `MinRatio` | 总减伤上限 |

除血线曲线外都接受一个可选的 `when` 条件委托，条件里要读实体就用 `info.TargetIndex` 自己索引。

### 减伤的两个通道

规则表里的"减伤"走两个字段，收尾时库按 `DamageMul *= 1 - ReductionAdd × ReductionScale` 合成：

| 字段 | 中性值 | 多个来源 | 用途 |
| --- | --- | --- | --- |
| `ReductionAdd`（dr1） | 0 | **相加** | 加减减伤：`+0.1` 是多一成，`-0.1` 是扣掉一成 |
| `ReductionScale`（dr2） | 1 | **相乘** | 调整减伤百分比：`0.5` 是把总减伤砍一半 |

"某个状态削弱了目标的伤害减免"就是一条 `MonoReductionScaleRule`：

```csharp
MonoDamage.Register(new MonoReductionScaleRule(0.5f, targetType: NPCID.MoonLordCore));
```

它不需要知道目标当前减伤是多少——合成在收尾做，所以对血线曲线与跟随战斗进度的减伤同样成立，消费者不必复刻那些公式。乘积先夹进 `[0, 1]`，减伤叠过头最多把伤害压到 0；最终乘区再受 `MonoAbsorbCapRule` 的下界约束。

元素相性（`MonoElementMultiplierRule`）走的是伤害乘区，不在这条通道上，不受削减影响。

### 增删规则

`Register` 之外还有三个撤销入口，给第三方附属用——比如想把 FPM 注册的某条规则拿掉：

```csharp
MonoDamage.Unregister(rule);                                          // 按实例
MonoDamage.UnregisterAll<PModeBuffDamageRule>();                      // 按类型（含子类）
MonoDamage.UnregisterAll(r => r.Stage == MonoDamageStage.Reduction);  // 按条件
```

移除会立即重新编译规则表，**冻结状态保持不变**——所以移除之后再 `Register` 仍会被拒，`LateRegistrationRejected` 的含义不受影响。放在自己模组的 `Mod.Load` 里调即可，那时别人的规则都已经登记完了。移除条数计在 `MonoDamage.Chain.Removed` 上，`/monodamage rules` 会显示它。

`MonoAbsorbCapRule` 同时承担蓝图里 D7（总减伤封顶）与 D8（伤害下限）两件事：两者说的都是 `DamageMul` 的下界，`0.9` 的封顶与"不低于原始的一成"是同一个数。绝对点数下限在原版字段上无处安放，见下一节。

### 动态减伤

`MonoDynamicReductionRule` 按战斗进度浮动：掉血快过时间推进就往上加减伤，进度达标或已经超时就回到基准。

```csharp
MonoDamage.Register(new MonoDynamicReductionRule(
    idealKillSeconds: 90f,     // 这场战斗的理想时长
    baseReduction: 0.3f,       // 正常节奏下的减伤
    extraScale: 2f,            // 落后时追加得有多狠
    maxReduction: 0.9f,
    targetType: NPCID.MoonLordCore,
    when: info => !BossRushEvent.BossRushActive));

MonoDamage.Register(new MonoBurstGuardRule(900, targetType: NPCID.MoonLordCore));
```

判定只用两个进度量：血量比例 `hpRatio = life / lifeMax`（1 掉到 0）与时间比例 `timeRatio = 已打时长 / idealKillSeconds`（0 涨到 1）。两者相加就是"进度"：

- 进度不小于 1（掉血速度不慢于理想节奏）→ 只有 `baseReduction`；
- 进度小于 1（掉血快于理想节奏）→ 追加减伤，追加量随差距从 0 涨到 `(1 - baseReduction) × extraScale / 2`；
- 时间比例已经到 1（超时）→ 只有 `baseReduction`。

公式在 `MonoProgressReduction.Compute`，是纯函数。`idealKillSeconds` 一般每个 boss 一条规则、各填各的；`extraScale` 默认 10，按 FPM 的难度建议从 2 附近试。

战斗计时就是 `MonoDamageNPC` 上的 `ElapsedTicks`：那个全局 NPC 每个实体一份实例，出生即为 0，`PreAI` 每 tick 加一，与灾厄把 `killTimeTimer` 放在 GlobalNPC 的 PreAI 里是同一个形态。计时跟着 `PreAI` 走，NPC 不在已加载区块里时会暂停；boss 战里目标基本都在屏幕上。

它和单次上限各管一头：`MonoBurstGuardRule` 拦某一下的数字，动态减伤拦整场打得太快。

---

## 5. 诊断

```csharp
MonoDamage.Trace.Enabled = true;   // 开启后每次命中重记一遍
MonoDamage.Describe();             // 规则表：按执行顺序列出每条规则
MonoDamage.Trace.Describe();       // 过程：每条规则改了哪个字段、原版最终算出多少
```

追踪默认关闭，关闭时每次命中只多一次布尔判断。开启后会为每条规则保存一份累加器快照，属于开发期开销。

---

## 6. 边界

1. **NPC 打 NPC 与原版旧路径不进规则表。** `StrikeOtherNPC` 与 `StrikeNPC` 不会经过 `ModifyHitByItem` / `ModifyHitByProjectile`，持续伤害的每 tick 结算走的正是后者。需要覆盖时由消费者自己挂 `ModifyIncomingHit` 并调 `MonoDamage.Run`。
2. **`MonoDamageInfo.Crit` 现阶段恒为 false。** 暴击是 `ToHitInfo` 的实参，钩子执行时这个值还没被决定。
3. **玩家侧没有防御字段可改。** 玩家的防御是 `Player.statDefense`，所以累加器里的 `DefenseFlat` / `DefenseMul` 在玩家侧重写回时被忽略。
4. **封顶只看库内的乘区。** 第三方直接写进 `FinalDamage` 的乘区不参与 `MonoAbsorbCapRule` 的计算。
5. **追踪给不出中间值。** 它记录每条规则的增量与原版结算结果，不记录 `GetDamage` 内部每一步的数值。
6. **固定值减伤做不到。** 想要"这一下少打 20 点"，在只写回原版字段的前提下没有落点；能表达的是比例与单次上限。
7. **动态减伤的战斗计时按 tick 走。** `MonoDamageNPC` 每个实体一份实例，`ElapsedTicks` 出生即为 0、`PreAI` 每 tick 加一，不记身份也不在死亡时清理。计时跟着 `PreAI` 走，NPC 不在已加载区块里时暂停。

---

## 7. 明确不做

护盾池、伤害延迟结算、无敌帧并集、浮动伤害数字与格挡提示、部位肉质（需要命中点几何与部位概念）、重写 `GetDamage`、属性聚合管线（蓝图 5.5）。这些要么等真实消费者，要么需要另一套数值模型。

---

## 8. 常见错误速查

| 现象 | 原因 |
| --- | --- |
| 规则根本不跑 | `TargetType` 与目标不符，或 `when` 条件里取实体失败。取实体用 `MonoDamageSource.TryTargetNpc` / `TrySourceProjectile` / `TrySourceNpc`，别直接索引 `Main.npc`（下标越界与"目标是玩家"两种情况它都判了） |
| 第一次命中之后登记的规则不生效 | 表已冻结，`MonoDamage.Chain.LateRegistrationRejected` 会大于 0。登记要放在 `Mod.Load` 或 `PostSetupContent` |
| 减伤写进去了，伤害没变 | 减伤段要写 `ReductionAdd` / `ReductionScale`。写 `DamageMul` 是另一条通道，两者最终都并进乘区，但只有减伤通道会被"减伤削减"影响 |
| 玩家侧改了 `DefenseFlat` 没反应 | 玩家侧没有防御字段，提交时被忽略，见 §6 |
| 元素倍率不生效 | 弹幕或物品上没挂标签，见 §1。注意 `MonoShot.Ring` / `Arc` / `Spread` 返回数量而不是下标，用它们生成的弹幕挂不上标签，要在弹幕自己的 `ModProjectile` 里挂 |
| `trace dump` 里看不到某条规则 | 它这次的 `AppliesTo` 没过；或者追踪是在这次命中之后才开的 |
| 一条规则只占一行，分不出几个状态各自的贡献 | 同类效果收在一条规则里的代价。要按状态分辨就拆成多个实例 |
| 规则"不见了" | `MonoDamage.Chain.Removed` 大于 0，有东西把它撤掉了，`/monodamage rules` 会显示条数 |
