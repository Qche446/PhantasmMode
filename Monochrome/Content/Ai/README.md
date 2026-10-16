# Monochrome 状态机

给 Boss / 复杂 NPC 的 AI 用的扁平状态机。机器只记住"当前在哪个状态"，行为与转换由消费者注册进来；状态的计时器与其它局部数据留在消费者自己的字段里。

没有栈，所以没有"被打断的状态等在下面、稍后接着跑"这回事。全仓目前也没有消费者需要它。

## 1. 它由哪几块组成

| 文件 | 管什么 | 依赖 tML | 能离线测 |
|---|---|---|---|
| `MonoFsm.cs` | 状态机本体：行为派发、转换检查、劫持、切换事件 | 否 | 是 |
| `MonoBehaviorAttribute.cs` | `[MonoBehavior&lt;TId&gt;(...)]`，把公开方法标成某状态的行为 | 否 | 是 |

两个文件都只认识枚举、委托和反射，离线验收台里能直接跑。

## 2. 它在帧里的哪一格

在 NPC 自己的 `AI()` 里每帧调一次 `Update()`。注册（行为、转换、劫持）只在初始化做一次，和 `LoadStateMachine` 一样。

机器实例挂在实体上、随实体重建。不要把它或它的委托放进静态字段、`ModSystem`、`MonoService`——模组重载后旧程序集里的委托还留着的话，调用时会报"找不到方法/本地函数"。

## 3. 最短路径

```csharp
public enum MyAI { Idle, Charge, Spin }

public partial class MyBoss : ModNPC
{
    private MonoFsm<MyAI> fsm;
    private int timer;

    private MonoFsm<MyAI> Fsm
    {
        get
        {
            if (fsm is null)
            {
                fsm = new MonoFsm<MyAI>(MyAI.Idle);
                fsm.AutoBind(this);                                  // 收集 [MonoBehavior] 方法
                fsm.To(MyAI.Idle, MyAI.Charge, () => timer >= 120);
                fsm.To(MyAI.Charge, () => PickNext(), () => timer >= 90);
                fsm.Changed += (oldState, newState) => timer = 0;    // 计时归零就是"刚进入"的信号
            }
            return fsm;
        }
    }

    private MyAI PickNext() => Main.rand.NextBool() ? MyAI.Idle : MyAI.Spin;

    public override void AI()
    {
        Fsm.Update();
        timer++;
    }

    [MonoBehavior<MyAI>(MyAI.Idle)]
    public void Idle() { /* timer == 0 表示刚进来 */ }

    [MonoBehavior<MyAI>(MyAI.Charge)]
    public void Charge() { }

    [MonoBehavior<MyAI>(MyAI.Spin)]
    public void Spin() { }
}
```

`timer++` 写在 `Update()` 之后，于是新状态的第一帧看到 `timer == 0`，与原来的 `EntityAIState.Time` 一致。构造与 `Reset()` 不会触发 `Changed`，初始状态的计时由字段默认值负责。

## 4. 转换的三种写法

| 写法 | 什么时候用 |
|---|---|
| `To(from, to, when, then)` | 条件成立切到固定目标 |
| `To(from, () => ... , when, then)` | 目标临场决定：随机招式、按权重挑、洗牌出招 |
| `ToAny(to, when, then, except...)` | 从每个状态各注册一条：任意状态下都要强插的切入、任何攻击结束都回待机 |

`when` 每帧问一次，命中就切换。同一状态注册了多条时按注册顺序取**第一条**满足的。

条件成立的当帧，目标委托求值一次；`Then` 与 `Changed` 随后触发，顺序是**劫持回调 → `Changed` → 转换自己的 `Then`**。新状态的行为要等下一帧的 `Update()`。

撤销口子：`ClearBehavior(id)`、`ClearTransitions(from)`、`ClearHijacks()`。

## 5. 劫持

`Hijack(select, then)` 只注册一条，在**任何**转换发生时都能改写它的目标。返回原值表示不接手，后面的劫持继续问；返回别的值表示改写，回调随即触发，再往后的劫持不再问。

适合"血量掉到某个比例就无视当前转换、直接切演出"这类判据。它不是每帧检查的：只有确实有一条转换被触发时才轮到它。

## 6. 一帧内的链式切换

切换完成后会立刻检查新状态的转换，条件仍成立就继续切。于是：

- 中间状态的行为不会在本帧跑，`Changed` 会为每一次切换触发一次；
- 离开条件要能随状态变化失效。两个状态互相指回来、且条件恒为真时，`Update()` 不会返回。库不做深度防护，这类循环要自己避免。

## 7. 这一版做什么、不做什么

**做**：按状态派发行为、`[MonoBehavior]` 反射绑定、固定/委托两种目标、`ToAny` 批量注册、劫持改写、切换事件、撤销口子、`Dump()` 一行诊断。

**不做**：

- 下推与可恢复中断——被打断的状态不会等在栈里，需要"暂停再原样恢复"的自己存状态与计时
- 状态局部数据——`Time`、`float[4]` 这类由消费者保管，库不建状态实例
- 加权随机转换——`To` 的委托目标已经能做，权重写在消费者的委托里
- 网络同步——状态就是一个枚举值，同步走消费者自己的 `SendExtraAI` / `MonoNet.Fields`
- 链式切换的深度上限
- Luminance 适配层

## 8. 常见错误速查

| 症状 | 原因 |
|---|---|
| 某个状态卡住不动、`BehaviorCount` 比预期少 | 行为方法是 `private`、静态、带参或非 `void`——`AutoBind` 静默跳过 |
| 游戏卡死一帧不返回 | 两个状态的离开条件同时恒为真，链式切换转不出去 |
| 模组重载后报找不到方法 / 本地函数 | 机器或它的委托被放进了静态字段、`ModSystem` 或长寿命服务 |
| 新状态第一帧的判断错位 | 忘记在 `Changed` 里把计时归零，或计时器在 `Update()` 之前自增 |
| 阶段切换偶尔晚几帧 | 劫持只在有转换被触发时检查；需要每帧检查的判据写成 `ToAny` |
