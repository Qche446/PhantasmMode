# Monochrome 核心层

Monochrome 的 L1 核心：子系统注册表、事件总线、对象池、确定性随机、时钟与调度、日志。
代码位于 `Monochrome/Core/`（除 `Core/Graphics/` 之外），命名空间 `Monochrome.Core`。

| 想看什么 | 去哪 |
|---|---|
| 最短的上手路径：怎么把一个特效写出来 | [模组级 README](../README.md) |
| 图形系统的部件细节 | [`Core/Graphics/README.md`](Graphics/README.md) |
| 整个库的分层、模块清单、工程规范与反模式 | [`docs/构建蓝图.md`](../docs/构建蓝图.md)（本层对应第 4 章） |
| 数学与几何工具 | `Common/MonoUtil/`（每类都有 XML 注释） |

> **本次交付的验证状态（读之前先知道）**
>
> - **编译已验证。** `dotnet build Monochrome/Monochrome.csproj` **0 错误 0 警告**；
>   `Monochrome.CoreTests` **80/80 通过**（`dotnet run --project Monochrome.CoreTests`）。
>   > 构建时要加 `-p:BuildMod=false`：tML 的打包步骤要写 `Mods/*.tmod`，那个目录在当前文件沙箱之外。
>   > 这是**打包**（把 .dll 封成 .tmod）被挡住，不是编译失败——看 `bin/Debug/net8.0/Monochrome.dll` 有没有被刷新即可。
> - **重复实现已合并。** 本层原有一份 13 条曲线的缓动副表与一份独立的 PCG32 生成器，与 `Common/MonoUtil` 重复。
>   现在**曲线只有 `MonoUtil` 一份、随机生成器只有 `MonoRandom` 一份**，本层只保留"约定"部分
>   （`MonoRng.Derive` / `MonoRngPurpose`）。取舍理由见 §2 与 §5.4 / §5.8。
> - **tML 钩子名已逐个核对过**（对着本机 tML 1.4.4.9 的 `Terraria.ModLoader.ModSystem` 元数据）。
>   核对推翻了原设计里三处按命名习惯推出来的假设，它们各自都编不过：
>   `PreDrawTiles` **不存在**（`ModSystem` 上只有 `PostDrawTiles`）；
>   `PostUpdateEntities` **不存在**（实体后的钩子是 `PostUpdateNPCs` / `PostUpdatePlayers` / `PostUpdateProjectiles`）；
>   这些钩子在 tML 里是**可重写方法而不是 event**，所以没有 `+=`/`-=`，也就不存在"忘了摘钩子"。
> - **另一条硬事实：`ModSystem` 没有任何"绘制开始之前"的钩子。** 绘制相位只有 `PostDrawTiles`（世界内容画完之后）
>   与 `PostDrawInterface`（界面画完之后）。因此 `PreDraw` 与 `PostUpdate` **共用** `PostUpdateDusts` 那个时刻——
>   它是 `ModSystem` 能提供的最后一个"更新已完、绘制未始"的挂点。真正的"抢在第一批绘制之前"在图元层。
> - **`Monochrome.CoreTests` 是兄弟目录工程**（不是 `Tools/` 下的脚本）：SDK 风格工程的默认 Compile glob 是
>   `**/*.cs`，把另一个工程放进 `Monochrome/` 里会被编进 `Monochrome.dll` 造成类型重复。
>   它需要**显式引用 FNA**（`lib/FNA.dll`）：`Monochrome.csproj` 的 FNA 来自 `tMLMod.targets` 里的私有引用，
>   不会流到兄弟工程，缺了它 `using Microsoft.Xna.Framework;` 直接 CS0234。
> - **驱动入口是 `public` 而不是 `internal`**（`MonoScheduler.Pump` / `MonoTween.Update` / `MonoTween.PumpTicks` /
>   `MonoTimeline.Update(double)` / `MonoTime` 的 setter / `MonoAllocProbe.Tick`）：验收台是兄弟程序集，
>   `internal` 会让它整个编不过，而 `InternalsVisibleTo` 会把整个库的内部面对它敞开——为测试开一次后门不划算。
> - **每帧 0 分配**那一项仍然只能在游戏里量（它要帧钩子）。`/monocore alloc` 会让 `MonoFrameSystem`
>   连续采样 N 帧的 `GC.GetAllocatedBytesForCurrentThread()` 差值并报告，**非 0 时它会先扣掉日志与调度器
>   自己那条路径**，把它们分别报出来，否则"我自己在测的时候记了一条日志"会被误读成"核心层分配"。

## 目录

- [1. 三条不变量](#1-三条不变量)
- [2. 部件地图](#2-部件地图)
- [3. 一帧的时间线](#3-一帧的时间线)
- [4. 部件索引](#4-部件索引)
- [5. 部件参考](#5-部件参考)
  - [5.1 MonoServiceHost —— 子系统注册表](#51-monoservicehost--子系统注册表)
  - [5.2 MonoEventBus —— 事件总线](#52-monoeventbus--事件总线)
  - [5.3 MonoPool / MonoHandle / MonoRingBuffer —— 池与句柄](#53-monopool--monohandle--monoringbuffer--池与句柄)
  - [5.4 MonoRng —— 确定性随机](#54-monorg--确定性随机)
  - [5.5 MonoTime / MonoFrame —— 时钟与帧末动作](#55-monotime--monoframe--时钟与帧末动作)
  - [5.6 MonoScheduler —— 延迟 / 冷却 / 协程](#56-monoscheduler--延迟--冷却--协程)
  - [5.7 MonoTimeline —— 时间轴](#57-monotimeline--时间轴)
  - [5.8 MonoTween / MonoEaseKind —— 补间与缓动](#58-monotween--monoeasekind--补间与缓动)
  - [5.9 MonoLog —— 日志](#59-monolog--日志)
  - [5.10 一个完整的接线（可参照案例）](#510-一个完整的接线可参照案例)
- [6. 零分配是怎么做到的](#6-零分配是怎么做到的)
- [7. 验收](#7-验收)
  - [7.1 离线验收台](#71-离线验收台monochromecoretests)
  - [7.2 游戏内自检](#72-游戏内自检mono-core-selftest)
  - [7.3 只能人工制造条件的两件事](#73-仍然只能人工制造条件的两件事)
  - [7.4 命令面：只允许一个 ModCommand](#74-命令面全模组只允许一个-modcommand)
- [8. 已知缺口](#8-已知缺口)
- [9. 常见错误速查](#9-常见错误速查)

---

## 1. 三条不变量

| # | 不变量 | 违反后的症状 |
|---|---|---|
| **I** | **身份 = 下标 + 代号**。跨帧持有的引用一律用 `MonoHandle`（或蠕虫系统里的 `MonoWormId`），**绝不用裸下标** | 旧句柄静默指向一个毫不相干的对象；症状离原因很远，极难归因 |
| **II** | **玩法逻辑用 tick，纯视觉用真实时间**。两个时钟都有名字（`MonoTime.Tick` / `MonoTime.RealTime`），每个模块必须显式声明用哪个 | 暂停时东西还在动；切出窗口回来物理爆炸；多人两端不同步 |
| **III** | **热路径零分配**。每帧跑的东西不 `new`、不 LINQ、不做字典查找、不拼字符串 | 弹幕海下卡顿；GC 尖峰与帧数无关地随机出现 |

#### I. 身份 ≠ 下标

tML 的槽位会被复用：同一个 `whoAmI` 先是一条虫的头，死后可能变成一只史莱姆。只存下标的句柄在下一帧
就指向了一个完全无关的实体，而且**不会报错**。

```csharp
MonoHandle handle = pool.Rent(out Particle p);
p.Position = ...;                    // 用法：借出来的 ref 直接写
pool.TryGet(handle, out Particle q); // 校验代号，过期句柄返回 false 而不是给你别人的槽位
pool.Return(handle);                 // 归还后旧句柄立刻失效
```

`MonoPool<T>` 的每次 `Rent` 都会让代号 +1，`Return` 时再 +1，所以**过期句柄永远匹配不上**。
双重归还是安全空操作，不会把同一个槽位塞进空闲链表两次。

#### II. 两个时钟

| 时钟 | 谁用 | 性质 |
|---|---|---|
| `MonoTime.Tick` | 玩法逻辑、状态机、计时机、伤害结算 | 世界加载时归零；**命中定格 / 时间缩放 0 时不前进**；多人两端一致 |
| `MonoTime.RealTime` / `RealDelta` | 呼吸、闪烁、噪声滚动、UI 动画 | 不随世界暂停；`RealDelta` 已 clamp 到 `1/30` 秒 |

`MonoScheduler` 与 `MonoTween` 走 tick，`MonoTimeline` 走真实时间——这不是随意的：**"30 tick 后开火"
必须能跟着世界停住，而"暗角在 0.4 秒内淡出"不该因为一次命中定格就被拉长。**

#### III. 零分配

热路径上的三件事都有零分配的写法，用错写法不会有任何提示：

```csharp
// 日志：级别不够时不拼串；级别够时只分配最终那一个 string
MonoLog.Debug($"怪物 {npc.whoAmI} 掉了 {n} 个");

// 延迟调用：Action<object?> + 状态参数，lambda 只要不捕获局部变量就是静态委托
MonoScheduler.Delay(30, static s => ((Foo)s!).Do(), this);   // 零分配
MonoScheduler.Delay(30, () => foo.Do());                     // 每次分配一个闭包

// 事件：派发只遍历 List<Entry>，不装箱、不拍快照、不用 LINQ
```

---

## 2. 部件地图

```
Monochrome/Core/
├─ Services/
│  ├─ MonoService.cs            IMonoService（五阶段生命周期）+ MonoServiceHost（注册表 + 驱动器）
│  └─ MonoLifecycleProbe.cs     只做记录的子系统：让五阶段在正式加载路径上真的跑起来
├─ MonoEventBus.cs              按帧阶段分派的弱订阅总线 + MonoEventBusSystem
├─ MonoPool.cs                  MonoHandle / MonoPool<T> / MonoRingBuffer<T> / MonoFrameQueue<T>
├─ MonoFrame.cs                 MonoTime（统一时钟）/ MonoFrame.Defer / MonoFrameSystem
├─ MonoScheduler.cs             延迟调用 / 冷却表 / 协程入口 + MonoSchedulerSystem
├─ MonoCoroutine.cs             MonoWait / MonoCoroutine / MonoChannel<T> / MonoTimeline
├─ MonoEase.cs                  MonoTween（池化补间；缓动曲线本身在 MonoUtil，本文件不再自带一份）
├─ MonoLog.cs                   分级日志 + 环形缓冲 + MonoLogInterpolatedStringHandler
├─ MonoCoreCommand.cs           /monocore 诊断 + 自检入口（处理器，不是 ModCommand）
├─ MonoCoreCommand.cs            /monocore（核心层诊断 + 自检）
├─ MonoCoreCommand.cs           /monocore（核心层诊断 + 自检）
└─ Graphics/                    L4 表现层（命名空间 Monochrome.Core.Graphics.*，见它自己的 README）
```

**本层只有三个 `ModSystem`**：`MonoFrameSystem`、`MonoEventBusSystem`、`MonoSchedulerSystem`。
它们各自负责一件"只有帧钩子拿得到"的事（时钟推进 / 事件派发 / 低频清理），除此之外什么都没有。
`MonoServiceHost` 是第四个，它不碰帧钩子，只驱动 `IMonoService` 的生命周期。

**tML 按类型的 `FullName` 给 `ModSystem` 排序，没有顺序 API**，所以"谁先跑"不能靠设计意图去保证。
帧起点上的几件事彼此有依赖，于是它们全部写在 `MonoFrameSystem.PreUpdateEntities` 一个方法里，顺序由
代码写死；其余钩子各自独立，先后无所谓。动那几行的顺序之前先看它的方法体注释。

**目前唯一注册的服务是 `MonoLifecycleProbe`**，它不做任何事、只记录自己走到了哪一阶段。
它存在的理由是：加载路径上需要一个真实的消费者，否则五个阶段只有"机制写好了"，没有任何证据说明它被驱动过。
它走的是显式注册（`Monochrome.Load` 里那一行），自动注册扫描会发现它已经注册过而跳过。见 §7.2。

**依赖方向：`Core` 不引用 `Core.Graphics`、不引用 `Gameplay`；但它引用 `Common/MonoUtil`** ——
而且只依赖其中**纯函数**的那两样：缓动曲线与确定性随机生成器。

这一条与最初的设计相反（原设计要求核心层零依赖），是本轮刻意改的，理由是**两处重复的代价高于一条 using**：

- 缓动曲线在两处各有一份 → 同一个名字可能给出两个差几个百分点的数，而这是最难一眼看出的偏差；
- PCG32 在两处各有一份 → **两套要各自证明的随机序列**，而"随机数和预期不一样"几乎无法归因。

合并后数学层持有**实现**，核心层持有**约定**：缓动只保留"类型 + 方向 → 常驻委托"这个消费入口，
随机只保留 `MonoRng.Derive(worldSeed, purpose, entityId, tick)` 这个纯函数派生与 `MonoRngPurpose` 分流域。
"多人同步"这件事真正的知识在**那四个输入**里，而不在 LCG 的常数里——所以约定留在 L1 是对的。

反过来仍不成立：`Common/MonoUtil` 与 `Graphics` 都不引用 `Core`。

---

## 3. 一帧的时间线

> **钩子是 tML 的可重写方法，不是 event**，所以下表里没有一处 `+=`/`-=`。
> 下面每个钩子名都对着 tML 1.4.4.9 的 `ModSystem` 核对过（见文首"验证状态"）。

| 时机（tML） | 谁被调用 | 做什么 |
|---|---|---|
| **模组加载（一次）** | `MonoFrameSystem` / `MonoEventBusSystem` | 什么都不用挂：钩子是虚方法，tML 自己分发 |
| | `MonoServiceHost.Load` | 打开注册窗口（只允许此后到 `PostSetupContent` 之间注册服务） |
| **所有内容注册完** | `MonoServiceHost.PostSetupContent` | 按 Order 依次跑 `Load → RegisterDefs → PostSetup → InstallHooks → Ready` |
| **`PreUpdateEntities`** | `MonoFrameSystem` | `Frame++`、更新 `RealDelta`/`RealTime`、**泵 `MonoScheduler`**（含上一帧排的帧末动作）、派发 `MonoFramePhase.PreUpdate`、驱动 `/monocore selftest`。**四件事的顺序写在他的方法体里**，拆到别的类就只剩类名排序决定先后 |
| **`PostUpdateDusts`** | `MonoEventBusSystem` | 依次派发 `WorldUpdate` → `PostUpdate` → `PreDraw`（三者共用一个时机，见下） |
| **`PostUpdateNPCs`** | `MonoEventBusSystem` | 派发 `MonoFramePhase.PostUpdateNPCs` |
| **`PostUpdatePlayers`** | `MonoEventBusSystem` | 派发 `MonoFramePhase.PostUpdatePlayers` |
| **`PostUpdateProjectiles`** | `MonoEventBusSystem` | 派发 `MonoFramePhase.PostUpdateProjectiles` |
| **`PostUpdateEverything`** | `MonoFrameSystem` | `Tick++`、推进全部 `MonoTween` |
| | `MonoSchedulerSystem` | 每 300 tick 一次：清过期冷却记录、压缩已取消的排程项 |
| **`PostDrawTiles`** | `MonoEventBusSystem` | 派发 `MonoFramePhase.PostDraw`（专用服务器不触发） |
| **`PostDrawInterface`** | `MonoEventBusSystem` | 派发 `MonoFramePhase.PostDrawInterface`，带游戏已 `Begin` 的界面批次 |
| **`OnWorldLoad`** | `MonoFrameSystem` | 世界时钟归零 |
| **`OnWorldUnload`** | `MonoFrameSystem` | 清掉全部补间 |
| | `MonoEventBusSystem` | 派发一次 `MonoFramePhase.WorldUnload` |
| | `MonoSchedulerSystem` | 清空排程表**与冷却表**（冷却按 tick 计，跨世界没有意义；**不清就会变成"刚进世界头几帧音效不响"的假冷却**） |
| **模组卸载** | `MonoEventBusSystem.OnModUnload` | 清空全部订阅（静态订阅是"永活"条目，只有这里能清掉） |
| | `MonoSchedulerSystem.OnModUnload` | 复位调度器 |
| | `MonoFrameSystem.OnModUnload` | 清补间、复位调度器 |
| | `MonoServiceHost.OnModUnload` | 逆序 `Unload` → `DisposeBuffers` → **断言注册表为空** |

**三个时机的实测依据：**

- `PreUpdateEntities` 在**世界更新闸门之外**（`MonoScreenEffectsSystem` 的定格是靠闸门实现的），
  所以它是"即使世界停住也要跑的东西"的唯一落点——时钟推进、帧末动作、`PreUpdate` 派发都在这里。
- `PostUpdateEverything` 在**闸门之内**，所以 `Tick` 只在世界真的推进时前进。
  这条性质是"玩法逻辑跟着世界暂停"的全部实现。
- **`WorldUpdate` / `PostUpdate` / `PreDraw` 三个阶段挤在 `PostUpdateDusts` 上。** 这不是偷懒：
  `ModSystem` 既没有比它更晚的"更新后"钩子，也**没有任何"绘制前"钩子**（绘制相位只有 `PostDrawTiles`
  与 `PostDrawInterface`）。三者语义不同但时基相同，所以按枚举顺序一次派发完，订阅者只管选语义。

**排程时基（容易算错，写在这里）：**

| 你写的 | 实际在第几 tick 执行 |
|---|---|
| 第 `t` tick 排 `Delay(n)` | `t + n + 2` |
| 第 `t` tick 排 `Delay(0)`（按 1 处理） | `t + 3` |
| 第 `t` tick 排 `At(x)` | `max(x, t + 2)` |
| `MonoFrame.Defer(...)` | 下一个帧起点（`Delay(1)`） |

两个 `+1` 都有出处：一个是帧起点泵的时候 `Tick` 还是上一帧的值（所以泵的截止线是 `tick + 1`），
一个来自"延迟 n tick 就不早于 n tick"这条保证。混合使用 `Pump(t)` 与 `Delay` 时先回来看这张表。

---

## 4. 部件索引

| 需求 | 用哪个 |
|---|---|
| 写一个有自己的生命周期的子系统 | `IMonoService` + `MonoServiceHost.Register<T>()`（§5.1） |
| 让别人的代码在某个帧阶段被通知 | `MonoEventBus.Subscribe<T>(phase, cb)`（§5.2） |
| 借一个槽位当临时对象，用完归还 | `MonoPool<T>` + `MonoHandle`（§5.3） |
| 记住最近 N 条遥测 / 事件 | `MonoRingBuffer<T>`（§5.3） |
| 只在渲染相位收集、更新相位处理 | `MonoFrameQueue<T>`（§5.3） |
| 让掉落 / 地形 / 粒子各自确定、互不干扰 | `MonoRng.Derive(...)` + `MonoRandom`（§5.4） |
| "30 tick 后做一件事" | `MonoScheduler.Delay(30, ...)`（§5.6） |
| "同一个音效 6 tick 内只响一次" | `MonoScheduler.Cooldown("key", 6)`（§5.6） |
| 多阶段攻击 / 分步演出 | `MonoScheduler.Start(MyRoutine())` + `IEnumerator<MonoWait>`（§5.6） |
| Boss 阶段编排、关键帧动画 | `MonoTimeline` + `MonoChannel<T>`（§5.7） |
| 把一个数值平滑推到目标 | `MonoTween.Spawn(...)`（§5.8） |
| 挑一条缓动曲线 | `MonoEaseKind` + `MonoEaseMode`（曲线本体在 `MonoUtil`，见 §5.8） |
| 分级日志 / 诊断导出 | `MonoLog`（§5.9） |

---

## 5. 部件参考

### 5.1 MonoServiceHost —— 子系统注册表

**它是什么**：`Core` 层里唯一驱动 `IMonoService` 的东西。生命周期是**显式五阶段**，顺序固定，不可自定义：

```
Load → RegisterDefs → PostSetup → InstallHooks → Ready      （退场：Unload → DisposeBuffers，逆序）
```

**为什么要它**：tML 的 `ModSystem` 只给"一个 `Load` 和一堆帧钩子"，而真实系统需要"先建表、再注册数据、
再装配、最后挂钩子"这四个明确不同的时刻。把它们塞进一个 `Load` 里，结果是初始化顺序变成隐式的、
由类的声明顺序决定——出问题时无法推理。

```csharp
public sealed class MySystem : IMonoService
{
    public int Order => 300;                      // 必填：越小越先初始化
    public string Name => nameof(MySystem);       // 必填：诊断里显示的名字
    public void InstallHooks() { }                // 只挂自己的钩子；其余五个阶段方法都有默认空实现
}

// 注册那一行通常不用写：PostSetupContent 会把它自动扫进来。
// 想自己控制时机，就在某个 Mod.Load 里 MonoServiceHost.Register<MySystem>();
```

**注册有两条路**：

- **自动注册**：`PostSetupContent` 开头扫一遍所有已加载模组的程序集，把实现了 `IMonoService`、类型可实例化
  （非抽象、非开放泛型、有公开无参构造）、且 `AutoRegister` 为真的类型注册进来。注册表里已有同类型实例的会跳过，
  所以显式注册过的类型不会被重复构造。
- **显式注册**：`MonoServiceHost.Register<T>()` / `RegisterInstance()`，只允许在 `Mod.Load` 到
  `PostSetupContent` 开头这段时间里调。晚注册会被拒绝并记日志——它会错过 `PostSetup` 与 `InstallHooks`，
  而那类 bug 表现为"功能随机失效"。

**边界与坑**：

- **不想被自动注册**就写 `static bool AutoRegister => false;`。共享用的中间基类请标 `abstract`，
  否则它自己也会被当成一个服务构造出来。
- `AutoRegister` 是**静态**成员，判断发生在实例存在之前；这也意味着读它的时候构造函数还没跑过。
- `Unload` 必须与 `Load` 对称。`OnModUnload` 结束时会**断言注册表为空**并记一条警告——
  这是提前抓住热重载泄漏的那道断言。
- **库不代消费者挂钩子**。`InstallHooks` 只服务自己；消费者挂钩子用 `MonoUtil.AddHooks`，宿主是消费者自己的程序集。
- **默认实现只能从接口那一侧调用**（`((IMonoService)this).Unload()`）。生命周期本来就归 `MonoServiceHost` 驱动，
  自己调它的场合很少。

---

### 5.2 MonoEventBus —— 事件总线

**它是什么**：按帧阶段分派的**弱订阅**总线。十一个阶段，每个对应一个确切的 tML 时机（见 §3 的表）。

```csharp
// 实例回调：无参方法组。总线对它只持弱引用，目标被回收后订阅自动失效。
using MonoEventSubscription sub = MonoEventBus.Subscribe<MySystem>(MonoFramePhase.PreUpdate, OnPreUpdate);
// 或者：MonoEventSubscription sub = ...; MonoEventBus.Unsubscribe(sub);

private void OnPreUpdate() { /* 状态从 this 上取 */ }
```

**为什么不用普通 `event`**：事件由发布者强引用订阅者。模组卸载后回调仍被持有，下一次触发就会碰到
已卸载的类型 → `TypeLoadException`。这里用 `WeakReference<T>` 持有目标，**目标被回收后订阅自动失效**。

**这条弱引用是真的，靠的是"不存绑定到目标的委托"**：条目里放的是订阅时建好的**开放委托**
（实例当第一个参数传进去），它只持有 `MethodInfo`。存 closed delegate 或捕获它的闭包同样能派发，
但那会把订阅者强引用住——`WeakReference` 就永远不失效，整个特性变成摆设。验收台用一条
`GC.Collect()` 之后的断言钉住这件事。

**派发为什么是零分配的**：

- 按阶段分槽（数组索引），不遍历全部阶段；
- 反向遍历 `List<Entry>`，按**每条自己的 `Alive`** 判断"这次派发途中它是不是被退订了"；
- 退订**不在遍历中移除**条目（只置 `Alive = false`），改在派发结束后一次性压缩——移除会让下标语义变复杂。

```csharp
// 实例回调：无参方法组，走弱引用
MonoEventBus.Subscribe<MySystem>(MonoFramePhase.PreUpdate, OnPreUpdate);   // private void OnPreUpdate()
// 静态回调：用 SubscribeStatic（名字不同，不是重载——理由见下）
MonoEventBus.SubscribeStatic(MonoFramePhase.PostDraw, static () => Count++);
```

**边界与坑**（这一节的几条都是实测踩出来的，别靠"常识"改）：

- **静态回调必须用 `SubscribeStatic`，实例回调必须用 `Subscribe<T>`——这两个名字不同是刻意的，不能做成重载。**
  做成两个都叫 `Subscribe` 的重载时会踩一个极隐蔽的坑：**任何捕获了实例的 lambda**
  （`Subscribe(phase, _ => OnTick())`）都会**优先选中 `Action<object>` 那个**，
  于是被当成"传了实例方法"而**拒绝注册**。症状是"订阅了但回调一次都不跑"，看起来完全正常。
- **实例回调要传无参方法组，别传 lambda。** 前者的委托目标是订阅者自己，总线能对它建弱引用；
  后者的目标是编译器生成的闭包，会被当成"永活"条目——那个实例再也没有被回收的一天。
  这种"绑定目标不是 `T`"的委托现在会记一条警告（`/monocore log` 能看到），不再只是静默降级。
- **`SubscribeStatic` 不检查 `Target`。** 曾经它用"`callback.Target is null` 才算静态"来拒绝实例方法——
  那**根本站不住**：现代 C# 把静态 lambda 编译成一个**单例显示类的实例方法**，所以静态 lambda 的
  `Target` **不是 null**（实测是 `P+<>c`）。那个检查于是把**所有静态 lambda 都拒了**。
  现在它无条件注册，靠"不同的方法名"来区分意图，而不是靠运行时猜测。
- **有效性是"每条"的，不是"每个阶段"的。** 曾经每条存下当时的 `versions[phase]`、派发时与该阶段当前版本比——
  于是**每次新订阅都会让同一阶段上先前的条目立刻失效**，一个阶段只有最后一个订阅者能收到回调。
  现在每条有自己的 `Alive` + 全库唯一的 `Token`。
- **退订绝不 `RemoveAt`。** 派发是按下标反向遍历，`RemoveAt(i)` 会把后面的元素前移，
  于是**紧挨着被删条目的那个订阅者会被跳过**。所以退订只置 `Alive = false`，删除留给收尾的 `Compact`。
- **订阅者抛出的异常会向外传播**。这是刻意的：静默吞掉异常等于把 bug 藏起来。
- 挂在**世界更新闸门内**的阶段（`WorldUpdate` / `PostUpdate` / `PreDraw` / 四个 `PostUpdate*`）在命中定格时整段不执行。
  需要"无论世界停不停都要跑"的逻辑请挂 `PreUpdate`。
- **`PreDraw` 不是"绘制之前"。** tML 的 `ModSystem` 没有这种东西，它和 `PostUpdate` 是同一个时刻
  （`PostUpdateDusts`）——见 §3。要做"抢在绘制之前"的事，去图元层。
- `MonoFramePhase.Custom` **库不自动派发**。它是留给调用方自己泵的节拍，调 `MonoEventBus.Dispatch(MonoFramePhase.Custom)`。
- `Describe()` 会分配，只在命令里调。它**列出全部 11 个阶段（含订阅为 0 的）并带派发次数**——
  "接线有没有在跑"的唯一证据就是那个计数在涨。

---

### 5.3 MonoPool / MonoHandle / MonoRingBuffer —— 池与句柄

| 类型 | 用途 |
|---|---|
| `MonoHandle` | 句柄：`Index` + `Version`。`Version == 0` 是空句柄 |
| `MonoPool<T>` | 定长槽位池，空闲链表用 `int[]`，绝不扩容 |
| `MonoRingBuffer<T>` | 定长环形缓冲，满了覆盖最旧的一条 |
| `MonoFrameQueue<T>` | 帧内入队、帧末处理，满了安静丢弃并计数 |

```csharp
var pool = new MonoPool<Particle>(1024, "粒子");
MonoHandle h = pool.Rent(out Particle p);
pool.GetRef(h).Position += velocity;      // 热路径：已验证活着时直接拿引用写
if (pool.TryGet(h, out Particle copy)) { } // 冷路径：带校验
pool.Return(h);
```

**边界与坑**：

- **`Rent` 返回 `ref`**，所以「我改的是副本」这种误会不会发生。但**别把 `ref T` 存起来跨帧用**——
  归还之后那一格会被复用。
- 池满时 `Rent` 返回 `MonoHandle.None`（`IsValid == false`）并累加 `MissCount`。
  **`MissCount` 非 0 就说明容量该调大，或者有东西忘了归还。**
- `TryGet` 会校验代号，所以**过期句柄返回 false，而不是给你一个别人的槽位**。
- **不要用 `MonoPool` 存 `Vector2` 这类"本来就该复制"的小值类型。** 池的价值在"构造代价高、生命周期交错"的对象上。
- `MonoRingBuffer` **不擦除底层数组**（`Clear` 只挪指针）：它常存值类型快照，逐格清零是纯浪费。

---

### 5.4 MonoRng —— 确定性随机

**三条硬约束**（全部来自多人同步的真实故障）：

1. **不用 `Main.rand`**——全局、与世界状态耦合，客户端与服务端序列不同。
2. **同一件事永远得到同一个数**——种子由纯函数派生。
3. **不同用途走不同流**——视觉、细节、玩法各一条，否则"多生成一个粒子"会让掉落物变样。

```csharp
// 纯函数派生：同样的四个输入永远得到同样的种子
ulong seed = MonoRng.Derive(Main.worldSeed, MonoRngPurpose.Gameplay, (ulong)npc.whoAmI, (ulong)MonoTime.Tick);
MonoRandom rng = new(seed);

if (rng.NextBool(0.25f)) Drop();                      // 25%
int index = rng.NextWeighted(weights);               // 权重表；非法权重返回 -1
Vector2 dir = rng.NextUnitVector();                  // 单位圆上的方向
rng.Shuffle(span);                                   // Fisher–Yates 就地洗牌
```

**算法**：PCG32（O'Neill, 2014）——64 位 LCG 状态 + 32 位输出变换（XSH-RR）。
选它的理由不是"最快"，而是它便宜、可复现，**且低位可用**（裸 LCG 的低位周期极短，拿去做 `% n` 会看出规律）。

**生成器是 `MonoRandom`，它住在数学层（`Common/MonoUtil/Mathematics/Statistics/MonoRandom.cs`）。**
本层原来自己有一份 `MonoRngStream`，与它算法相同——那不只是重复，而是**两套要各自找参考值证明的序列**。
合并时保留了数学层的类型名（因为 `MonoNoise` / `MonoSampling` 依赖它），把它补齐成两份能力的**并集**：

| 能力 | 谁原来有 | 现在 |
|---|---|---|
| `Derive` 纯函数派生 / `MonoRngPurpose` 分流域 | 核心层 | 仍在 `MonoRng`（本层保留的"约定"部分） |
| `Fork(salt)` 分支流 | 核心层 | `MonoRandom.Fork` |
| `Seed` / `Calls` 溯源 | 核心层 | `MonoRandom.Seed` / `.Calls` |
| `State` / `Sequence` 显式状态（存档、回放） | 数学层 | `MonoRandom.State` / `.Sequence` |
| 无偏取整 | 核心层 | `MonoRandom.NextInt`（数学层的取模版已删） |

**边界与坑**：

- `NextUInt(max)` / `NextInt(max)` **没有模偏差**（用拒绝阈值 + 重抽，不是 `% max`）。在掉落表这种"必须精确"的地方，
  取模的偏差是实打实的错误，不是"概率上差不多"。合并前数学层那版就是 `% bound`，现在全库只有一条路径。
- **不要把这个结构体推进状态之后期待原实例被改**：值类型会复制。放进数组/字段里再调 `Next*` 才是正确用法；
  要推进调用方那一份就传 `ref`（`MonoSampling` 的方法都收 `ref MonoRandom`）。
- **`MonoRandom(ulong state, ulong sequence)` 的播种用的是默认步进常数**，`sequence` 只在播种完之后才生效。
  这条很容易写反（先在播种那两步用 `(sequence << 1) | 1`），而两种写法的差异**只在"同一种子换序列"时才看得出来**，
  所以验收台专门有一条用例钉住它。
- `Fork(salt)` 给"同一次事件里的子事件"用（例如一次爆炸里的每个碎片），它从 `seed` 与**已调用次数**派生，
  所以父流取了多少次数都不影响子流。
- 多人同步的随机**必须由服务端产出**并通过 `ModPacket` 广播，或保证同种子同序列；本类只提供后者。
- `NextWeighted` 在权重全为 0 / 全为负时**返回 -1**，不"随便给一个下标"——喂了非法权重却拿到看似合理的结果，
  会让调用方以为配置生效了。

---

### 5.5 MonoTime / MonoFrame —— 时钟与帧末动作

见 §1 的「两个时钟」表与 §3 的时间线。补充两条：

**`MonoFrame.Defer(action)`**：把动作推迟到**下一帧的帧起点**（那里所有派发都已结束）。
典型用途是"在 `OnKill` 里让另一个 NPC 消失"和"在派发过程中注销自己"——直接做会踩空集合。

**`MonoFrameSystem`** 是唯一推进 `MonoTime` 的地方。**不要自己去写 `Main.GameUpdateCount` 来计时**：
它是全局的、跨世界累计的，"第 300 tick" 在两次游戏里含义不同。

---

### 5.6 MonoScheduler —— 延迟 / 冷却 / 协程

```csharp
MonoScheduler.Delay(30, () => Explode(), "爆炸");                 // 30 tick 后
long id = MonoScheduler.Delay(60, static s => ((Foo)s!).Done(), this);
MonoScheduler.Cancel(id);                                         // 撤销

// 目标可能中途消失的场合，把存活判定一起交出去：判定为假就安静作废。
MonoScheduler.Delay(30, static s => ((NPC)s!).DoThing(), npc, "boss.thing", MonoAlive.Npc);

if (MonoScheduler.Cooldown("hit-sound", 6)) PlaySound();          // 同种音效 6 tick 内只响一次

MonoCoroutine routine = MonoScheduler.Start(MultiStageAttack());  // 注意括号
routine.Stop(() => ResetState());                                 // 中断 + 收尾
```

**时间基准是 `MonoTime.Tick`，不是帧数也不是真实时间。** 后果：世界暂停（命中定格、时间缩放 0）时，
"30 tick 后"不会提前到期，因为 tick 本身没走。

**协程**：`IEnumerator<MonoWait>`，用 `yield return MonoWait.For(30)` / `.Seconds(0.25)` / `.NextTick` / `.Forever`。
思路来自 Monocle 的 `Coroutine`——用迭代器把"多阶段流程"写成顺序代码，而不是一坨 `switch (state)`。

**边界与坑**：

- 容量固定 512（`DefaultCapacity`），**满了拒绝并计数**，不扩容。`Rejected` 非 0 通常意味着有东西在**每帧重排**
  而不是排一次。
- 取消要拿**序号**。别用 lambda 当键——每次写的 lambda 都是新实例，永远匹配不上。
- **目标会中途消失的排程，请把存活判定一起传进去**（`Delay` / `At` 带 `state` 的重载最后一个参数）。
  判定为假时这一项会被安静丢弃：不执行、不记日志，只计入 `Skipped`。`MonoAlive.Npc` / `.Projectile` /
  `.Player` 是随包的三个常用判定。少了它，"NPC 已经死了"这件正常事会先抛一次异常，再被记成一条错误日志；
  更麻烦的是实体槽位会被复用，那次调用可能作用在一个完全不相干的实体上，而且不会抛异常。
  判定函数自己抛异常仍然计入 `Failed`——那是判定写错了，属于真 bug。
- `Cancel` 只把项标记为已取消（**不移动堆数组**），真正的清理由 `MonoSchedulerSystem` 每 300 tick 做一次。
- 冷却表的键必须**稳定**（`"player-hit-sound"` 而不是玩家名字）。它由 `MonoSchedulerSystem` 定期清理，
  **不清就是一条缓慢的内存泄漏**。
- 派发中排入的项**不会在同一轮里执行**：`Pump` 每取一个就重新读堆顶，只有到期日 ≤ 当前 tick + 1 的才跑。
  所以"Delay(0)"是下一帧。

---

### 5.7 MonoTimeline —— 时间轴

```csharp
var timeline = new MonoTimeline { Loop = true };
timeline.AddFloat("intensity", [0.0, 0.4, 1.2], [0f, 1f, 0.2f], MonoUtil.InOutSine);
timeline.AddVector("offset", [0.0, 0.4], [Vector2.Zero, new Vector2(0, -12)], MonoUtil.OutBack);
timeline.Play();

// 每帧（通常挂 PreUpdate 订阅）：
timeline.Update();
float shake = timeline.Get<float>("intensity");
```

> 通道的 `ease` 参数收的就是 `Func<float, float>`，所以直接传 `MonoUtil` 上的曲线函数即可（`null` = 线性）。
> 想用"类型 + 方向"那套，用 `MonoUtil.Ease(kind, mode)` 取常驻委托，别现写 lambda。

**时间基准是真实秒数**（`MonoTime.RealDelta`，已 clamp），因为演出属于表现层。
需要跟随世界暂停的编排请用 `MonoScheduler` 的 tick 排程。

**边界与坑**：

- **倒放是状态而不是负速度**（`Reversed`）：两者在到达端点时的行为不同，用速度符号表达会让它们纠缠不清。
- 时间超出两端时**钳制到端点，不外插**——外插会把数值推到设计范围之外（`OutBack` 的通道尤其危险）。
- 通道的关键帧数组**会被复制并排序**，所以调用方之后改自己的数组不会影响它；同一条通道可以被多条时间轴共用。
- `Get<T>` 取不到通道会**抛异常**（通道名拼错必须立刻可见），不静默返回默认值。

---

### 5.8 MonoTween / MonoEaseKind —— 补间与缓动

```csharp
// 用唯一的缓动表挑一条（推荐：内部取的是常驻委托，不分配）
MonoTween.Spawn(from: 0f, to: 100f, durationTicks: 30, setter: v => _barWidth = v,
                kind: MonoEaseKind.Expo, mode: MonoEaseMode.Out);

// 或者直接传一个函数（曲线本体也在 MonoUtil）
MonoTween.Spawn(0f, 1f, 12, setter, MonoUtil.OutBack);

// 目标会动的跟随场景：
tween.Retarget(newTarget);       // 从当前值继续，不跳
```

**为什么要有它**：手写 `x += (target - x) * 0.1f` 有三个已知毛病——与帧率耦合、永远到不了目标、改目标时会跳。

**缓动曲线不由本层定义。** 完整表（11 类 × In/Out/InOut = 31 条，外加弹簧阻尼、`Remap`、`Wobble`）
在 `Common/MonoUtil/Mathematics/Easings/MonoEasing.cs`。本层**原有另一份 13 条的副表**，已整体删除：
两份定义只要有一处写法不同，同一个名字就会给出两个差几个百分点的数，而那是最难一眼看出的偏差。
现在本层只保留两个消费入口：`MonoEaseKind` + `MonoEaseMode`（走常驻委托表）与任意 `Func<float, float>`。

**边界与坑**：

- 补间是**池化**的（`Spawn` 借、`Stop` 还），所以"每个 UI 元素一个补间"不会变成每秒几百次分配。
  容量在类型初始化时一次性造满 512 个实例，**永不扩容**——会扩容的池恰好在最需要它的时候制造一次 GC。
- 池满时 `Spawn` 会**直接跳到终值**并返回 null。降级方向是刻意的：宁可没有动画，也不要停在旧值上。
- `setter` **每 tick 调一次**，别在里面做重活。
- 补间走 **tick**，所以世界停住时补间也停。要"定格时仍然动"的纯视觉缓动，自己用 `MonoTime.RealDelta` 插值。
- **时长单位是 tick，推进单位是秒**，中间靠一个累计器换算（这正是它避开 `MathF.Round(1/60) = 0`
  那个陷阱的手法）。两者不可混为一谈：`Spawn(..., 10, ...)` 是 **10 tick = 1/6 秒**，不是 10 帧。
- **想要可复现的步进就用 `MonoTween.PumpTicks(n)`**（按 tick 直接推进）。`Update(dt)` 走的是**有状态**的换算器，
  在没有真实帧钟的地方调它，"推进 5 tick"要么一步不走、要么受上次遗留的余量影响而多走几步——两种都看起来像"补间坏了"。
- **默认缓动是线性**（`MonoUtil.LinearEase`），与 `MonoChannel` 的 `null` 语义一致。
  它是一个**静态字段**而不是现写的 lambda：`ease: t => t` 每次都会新建一个闭包，而这个字段传多少次都是同一个引用。
  验收台有一条用例专门断言"两次 `Spawn` 拿到的默认缓动是同一个实例"，以及"`(类型,方向)` 重载复用表里那一格"。
- **`MonoEaseKind` / `MonoEaseMode` 住在数学层**，本层通过根命名空间直接用（见 §2 的依赖方向说明）。

---

### 5.9 MonoLog —— 日志

```csharp
MonoLog.Info(MonoLogLevel.Info, $"载入完成，共 {count} 项");
MonoLog.Warn(MonoLogLevel.Warn, $"...");          // 注意：字面量也要写成 $"..." 才会走惰性路径
MonoLog.Write(MonoLogLevel.Info, someString);     // 已经是 string 时走这个（不惰性）
```

- **分级**：`Trace / Debug / Info / Warn / Error`。默认 `Minimum` 在 Debug 构建是 `Debug`、Release 是 `Info`。
- **环形缓冲 512 条**，预分配、绝不增长（诊断本身不能改变被观测对象的性能特征）。
- **级别不够时连字符串都不拼**：方法接 `MonoLogInterpolatedStringHandler`，`Append*` 全是空操作。
  代价是**真正记下去的那一条会分配一个 string**——这是"要留下历史"的固有价格，躲不掉。
- `MonoLog.Dump()` 按时间顺序导出历史，`Describe()` 给一行统计。

**边界与坑**（这一段是实测踩出来的，别照着"常识"改）：

- **没有 `Info(string)` 这种重载，这是刻意的。** 插值串同时能转成 `string` 与处理器类型，而重载决议在
  两者都可用时**总是选 `string`**——于是 `MonoLog.Info($"...")` 会先无条件把插值求值成字符串，
  处理器变成永远走不到的死代码，"关闭的级别连字符串都不拼"当场失效。要记一条已经拼好的字符串，
  用 `MonoLog.Write(level, message)`：**名字必须不同**，否则等于把惰性路径废掉。
- **插值里的表达式一定会被求值，这不归处理器管。** 表达式的求值由编译器生成在**调用点**，处理器是
  之后才拿到值的。想让"关掉时连表达式都不算"，只能调用方自己守卫：
  `if (MonoLog.IsEnabled(MonoLogLevel.Info)) MonoLog.Info(MonoLogLevel.Info, $"昂贵 {Compute()}");`
- **处理器参数不能带 `ref`，级别参数必须在它前面。** 两条都是硬约束，违反了编译器会放弃处理器转换，
  转而把插值串当普通参数塞进来，报的是 CS7036「缺少 formattedCount/level」——
  一个和真实原因看起来毫无关系的错误。
- **别在每帧路径上无条件记日志。** 即使插值被跳过，那次分支也还是成本；
  更要紧的是"每帧一条日志"会让环形缓冲只剩 8.5 秒的历史，那正好把有用的上下文冲掉。

---

### 5.10 一个完整的接线（可参照案例）

下面这段不引入新 API，只是把前面几节最常用的几样接成一个真实形状：一个"演出导演"子系统。

```csharp
using Monochrome.Common.MonoUtil;   // MonoEaseKind / MonoEaseMode 与曲线本体都在数学层
using Monochrome.Core;

public sealed class ShowDirector : IMonoService
{
    public int Order => 300;                  // 300–599 = 玩法系统（号段见 §5.1）
    public string Name => nameof(ShowDirector);

    private readonly MonoRingBuffer<string> trace = new(16);   // 诊断留痕：定长，永不增长
    private MonoTween? fade;                                   // 跨帧持有的补间
    private float pulse;

    public void Load()                                // 建运行时状态；这里不读世界
    {
        trace.Add("Load");
    }

    // RegisterDefs / PostSetup / Ready 没用到，直接不写——接口给了默认空实现。
    public void Ready() { }

    public void InstallHooks()                        // 只挂自己的钩子
    {
        MonoEventBus.Subscribe<ShowDirector>(MonoFramePhase.PreUpdate, OnPreUpdate);
        MonoEventBus.Subscribe<ShowDirector>(MonoFramePhase.PostUpdate, OnPostUpdate);
    }

    public void Unload()                              // 必须与 Load 对称
    {
        fade?.Stop();
        fade = null;
    }

    /// <summary>一次演出：40 tick 淡出，120 tick 后炸一次。</summary>
    public void Play()
    {
        fade = MonoTween.Spawn(0f, 1f, 40, ApplyFade, MonoEaseKind.Cubic, MonoEaseMode.Out);
        MonoScheduler.Delay(120, static state => ((ShowDirector)state!).Burst(), this, "show.burst");
    }

    private void ApplyFade(float value)
    {
        // 写进你自己要驱动的那份状态
    }

    private void OnPreUpdate()                        // 无参实例方法，状态从 this 上取
    {
        if (MonoScheduler.Cooldown("show-hit-sound", 6))   // 冷却表的键要稳定
        {
            // 播你的音效
        }
    }

    private void OnPostUpdate(ShowDirector self)
    {
        // 呼吸灯属于纯视觉：它跟真实时间走，世界定格时照样在动。
        pulse = 0.5f + 0.5f * MathF.Sin((float)MonoTime.RealTime * 3f);
    }

    private void Burst()
    {
        trace.Add("Burst");

        // 不能在遍历集合的过程中改集合：把它推迟到下一帧的帧起点。
        MonoFrame.Defer(() => trace.Add("Deferred"));
    }
}

// 注册只允许在 Mod.Load 阶段：
// MonoServiceHost.Register<ShowDirector>();
```

| 写法 | 为什么 | 出处 |
|---|---|---|
| 状态放实例字段，不塞静态 | `IMonoService` 的五阶段对应"建表 → 注册 → 装配 → 挂钩 → 就绪"这五个不同时刻 | §5.1 |
| 不写注册那一行 | `PostSetupContent` 的自动注册扫描会接住它；显式注册留给"要控制时机"的场合 | §5.1 |
| 实例回调传无参方法组 | 总线对它只持弱引用，目标被回收后订阅自动失效；lambda 会退化成永活条目 | §5.2 |
| `Unload` 与 `Load` 对称 | 退场时断言注册表为空，漏一条就是热重载泄漏 | §5.1 |
| 补间存字段、`Unload` 里 `Stop` | 补间是池化对象，`Stop` 才是归还 | §5.8 |
| 玩法计时走 `MonoTime.Tick`，视觉走 `MonoTime.RealTime` | 世界暂停时前者停、后者不停 | §1、§5.5 |
| `MonoFrame.Defer` 而不是当场改集合 | 推迟到下一帧帧起点，那里不在任何派发里 | §5.5 |
| `Delay` 用带 `state` 的静态 lambda | 这是零分配那条路，捕获局部变量会每个排程分配一个闭包 | §5.6 |

---

## 6. 零分配是怎么做到的

| 位置 | 手法 |
|---|---|
| 事件派发 | 按阶段分槽 + `List<Entry>` 反向遍历 + 版本号；死条目延后压缩；订阅时反射构造一次 `Action<object>` |
| 日志 | `[InterpolatedStringHandler]`：级别不够时所有 `Append*` 都是空操作，级别够时只分配最终那一个 string |
| 调度 | 固定容量最小堆，`(Action<object?>, state)` 而不是闭包；`Pending` 是结构体数组 |
| 池 | `T[]` + `int[]` 空闲链表，无装箱、无 `Queue<int>` |
| 随机 | 结构体流，状态就地推进；`NextWeighted` / `Shuffle` 走 `Span<T>` |
| 冷却 | 只在冷路径用 `Dictionary<string,long>`，且定期清理 |
| 补间 | 池化 + `Action<float>` setter（`Spawn` 时给一次，不每帧构造） |
| 事件订阅 | 只在**订阅时**分配（一个 `Action<object>` 包装 + 一个 `WeakReference<T>`），不在派发时 |

**能证明它的是执行，不是推理。** 见 §7。

---

## 7. 验收

蓝图 §17.2 对阶段 1（核心基础层）的验收标准是：**每帧 0 分配，用 `GC.GetAllocatedBytesForCurrentThread()` 验证。**

验收分成两半，**两边都不能省**：离线验收台证明"逻辑与时间基准"，游戏内自检证明"钩子真的挂上了"。

```powershell
# ① 不启动游戏：纯逻辑与时间基准（不需要图形设备）
dotnet run --project Monochrome.CoreTests

# ② 进游戏之后（客户端，必须在一个世界里）
/monocore selftest        # 采样 16 帧，回执摘要 + 落盘完整报告
/monocore where           # 报告文件路径
/monocore last            # 重放上一次报告
```

> `selftest` 与 `alloc` 会起采样、往 `Mods` 目录写报告文件，所以**只在开发期可用**（判定见 `MonoDebug`）：
> 从 IDE 附加调试器启动游戏，或者设环境变量 `MONOCHROME_DEBUG=1` 后重启。只读的子命令任何时候都能用，
> 被拒绝时回执里会写清楚原因与开启方法。

### 7.1 离线验收台（`Monochrome.CoreTests`）

> `Monochrome.CoreTests` 是**兄弟目录工程**（不是 `Tools/` 下的脚本）——理由与"分析器必须放兄弟目录"一样：
> SDK 风格工程的默认 Compile glob 是 `**/*.cs`，放进 `Monochrome/` 里会被编进 `Monochrome.dll`（类型重复）。
> 它需要**显式引用 FNA**（`lib/FNA.dll`）：`Monochrome.csproj` 的 FNA 来自 `tMLMod.targets` 里的私有引用，
> 不会流到兄弟工程。当前状态是 **80/80 通过**。

它覆盖五类**能在没有游戏的情况下证明**的性质：

1. **确定性**：同种子同序列；`Derive` 在 4096 组输入上互不碰撞；PCG32 的输出序列与参考值逐位一致；
   `NextInt` 与"拒绝+取模"的参考实现逐位相同（无偏）。
2. **句柄语义**：过期句柄 `TryGet` 返回 false；双重归还不会污染空闲链表；池满时 `Rent` 返回 `None`。
3. **结构不变量**：最小堆的到期顺序（含"派发中排入的项不在同一轮跑"这条快照语义）、环形缓冲覆盖顺序、
   RingBuffer/CopyTo 的边界、协程状态机的两个终态（`Done` 与 `Stopped` 都不能算"活着"）。
4. **时基**：`Delay(n)` 的到期时刻、`Cooldown` 的到期判定、时间轴的端点钳制与倒放、补间按 tick 走完并精确到终点。
5. **不重复（本轮的取舍本身）**：反射扫一遍 `Monochrome.dll`，断言 14 个曲线名各只有**一个** `float → float`
   定义、`Monochrome.Core.MonoEase` 类型不存在；断言补间/时间轴的默认缓动与 `(类型,方向)` 重载复用的是
   **同一个委托实例**（写成每帧新建的 lambda 也能跑对，但那正是分配来源）。

> 第 5 条有一个只在验收台成立的坑，值得单独记：**反射本身在离线环境里会抛**。
> `Monochrome.dll` 里大量类型的签名引用 `Terraria`，而验收台没有 tML 运行时，
> 于是 `Assembly.GetTypes()` 抛 `ReflectionTypeLoadException`、`type.Namespace` 抛 `FileNotFoundException`、
> **`method.ReturnType` 也会抛**（签名是延迟解析的）。
> 所以那条用例的每一步都自带 try/catch，并且**另外断言"实际扫到了 ≥ 5 个类型"**——
> 否则它会退化成一条永远绿的摆设。

### 7.2 游戏内自检（`/monocore selftest`）

**它证明的是离线验收台原理上证明不了的那一类事实，而这一类的失败模式全是静默的：**

| 要证的事 | 静默失败长什么样 |
|---|---|
| `ModSystem` 的 9 个钩子真的被触发了 | 钩子名写错一个字母不报错，只是永不触发 → "订阅了但回调从没跑过" |
| `WorldUpdate` / `PostUpdate` / `PreDraw` 的**帧内顺序** | 顺序错了只表现为"位置差一帧" |
| `Delay(n)` 的时基是**游戏 tick** 而不是帧 | 定格时该停的没停 |
| 每帧分配 | `/monocore alloc` 的量级 |
| `IMonoService` 五阶段真的被驱动过 | **注册表恒为 0 也会"看起来正常"** |

**推进点不是订阅，而是直接写在 `MonoFrameSystem.PreUpdateEntities` 里**——那是游戏每帧本来就会走的路。
如果自检靠订阅总线来推进自己，就会变成"用被测对象测试被测对象"：总线一旦全哑，自检只会卡死，
而不是报出"总线全哑"。现在的写法让"某个阶段一次都没派发"变成一条明确的红。

**判定标准**：`/monocore selftest` 的聊天回执里 **`通过 N / N` 且没有 `×` 行**。
任何一条红都直接指出是哪个阶段、哪个钩子没动。

**报告落盘**：`<tML mod 目录>/Monochrome-CoreSelfTest.txt`（`/monocore where` 给出绝对路径）。
掉出聊天记录的详细证据都在里面——尤其是"各阶段观测到的派发次数"那张表。

**两项自检只测量、不判定**（要在游戏里人为制造条件，见 §7.3）：

- **弱引用订阅目标被 GC 回收后条目失效**：回收时机不可控。
- **每帧分配必须为 0**：采样窗口里同进程还有别的模组在跑，所以那个数是**基线**而不是阈值。

**副作用（务必知道）**：自检在收尾时会真的调一次 `MonoServiceHost.Register<T>()` 来验证"晚注册被拦住"，
这会往 `MonoServiceHost.LateRegistrationRejected` 上加 1，并在日志里留一条 Error。
那是**预期行为**，不是故障；`LateRegistrationRejected` 在正常游玩路径上恒为 0。

### 7.3 仍然只能人工制造条件的两件事

| 要证的事 | 怎么制造条件 | 观察什么 |
|---|---|---|
| 命中定格时 `Tick` 停住而 `Frame` 继续涨 | 打一个带 hitstop 的 Boss 招式，或调用 `MonoScreenEffectsSystem` 的定格接口 | 定格期间 `/monocore` 里 tick 不动、帧号在涨 |
| 弱引用订阅目标被回收 | 让一个订阅目标在订阅后失去所有强引用，触发一次 `GC.Collect()` | `/monocore bus` 里该条目变成 `×`，下一次派发后消失 |
| 热重载不泄漏 | 游戏内重载模组（`/reload`） | `/monocore services` 注册表为空；日志里没有"MonoServiceHost 退场时注册表里还有 N 个服务条目" |

**每帧分配**那一项的常驻入口是 `/monocore alloc [帧数]`：`MonoFrameSystem` 连续采样 N 帧的
`GC.GetAllocatedBytesForCurrentThread()` 差值并报告——**非 0 时它会先扣掉日志与调度器自己那条路径**，
把它们分别报出来，否则"我自己在测的时候记了一条日志"会被误读成"核心层分配"。

### 7.4 命令面：触发词的第一段不能撞

**这不是风格问题，是 tML 的键规则逼出来的。**

`ModCommand.Command` 是"触发这段文本"，而 tML 用它的**第一段**做键：

```
Command => "mono"        →  键 "mono"
Command => "mono core"   →  键 "mono"        ← 同一个键
```

两个命令争同一个键时，先注册的那个吃下**全部** `/mono ...`，另一个永远接不到，而且**没有任何报错**。
此前的实际症状就是：`/monocore` 被图形命令接走，核心层诊断与自检**完全没法用**。

修法是**把触发词改成两个不同的词**，两个命令各自独立、各自有 `help`：

| 命令 | 类 | 触发词 |
|---|---|---|
| `/monocore` | `MonoCoreCommand` | `monocore` |
| `/monoshader` | `MonoGraphicsCommand` | `monoshader` |

**子命令打错时不再倒出全部清单**——那可能十几行，而"打错了"是最常见的情况。取而代之：

```
/monocore            →  等同于 /monocore help
/monocore help       →  子命令清单（十几行，只有你明确要才给）
/monocore xyz        →  「不存在子命令「xyz」。可用 /monocore help 查询全部子命令。」
```

`/monoshader` 同形：`help` / `status` / `reload` / `capture`，打错也只是一行提示。
`/monocore status` 看概要（以前"裸命令"给概要，现在裸命令给 help——那更符合"我不知道有什么"的处境）。

#### 传给 `Action` 的 `args` 到底是什么形状（**实测，别按源码想当然**）

tML 的 `CommandLoader.HandleCommand` 源码里写的是：

```csharp
var args = input.TrimEnd().Split(' ');
args = args.Skip(1).ToArray();
```

照着读会得出"`input` 含命令名，所以 `args[0]` 是命令名、子命令在 `args[1]`"。**这个推断是错的**，
而且错得很隐蔽：它只在"裸命令"这一种情况下露馅。

实测（用下面这张表反推出来的，来源是一次真实报障：`/monocore status` 打不开、
`/monocore monocore status` 却能打开）：

| 输入 | tML 实际传的 `args` | 子命令在哪 |
|---|---|---|
| `/monocore` | `[]`（**长度 0**，不是 1） | 没有 |
| `/monocore status` | `["status"]` | `args[0]` |
| `/monocore monocore status` | `["monocore", "status"]` | `args[1]` |
| `/monocore  status`（两个空格） | `["", "status"]` | `args[1]`，且 `args[0]` 是空串 |

**规律：能用 ⇔ 子命令落在 `args[1]`。** 也就是说 `args[0]` 是"命令名那一格"，
但它**本身被 `Skip` 掉了**——所以正常写法（`/monocore status`）里子命令在 `args[0]`，
而裸命令的 `args` 长度是 **0**。

**现在的做法是不依赖这个形状**：先把 `args` 里的空串去掉，再看 `args[0]` 是不是命令触发词——
是就剥掉，于是后续一律按 `[子命令, 参数…]` 读。四种形状都能正确落地。

> 那个"能用/不能用"的反差就是最好的判据。当时它把排查方向从"tML 的兜底掩盖了异常"
> 拉回到"参数形状与我以为的不一样"——**源码与实测不一致时，以实测为准**。

> 顺带一个坑：tML 的兜底是 `catch (Exception) { Reply("Usage: " + mc.Usage, Red); }`。
> 也就是**任何**内部异常都会被伪装成"你打错子命令了"，看起来完全不像崩溃。
> 所以两个命令的 `Action` 现在自己 `catch` 并记日志、回一句红色错误——不然真故障永远看不见。

**怎么防止这个 bug 复发**：验收台里三条用例直接读 `Monochrome.dll` 的**元数据**（不是反射）：

| 用例 | 守什么 |
|---|---|
| 两个命令的触发词第一段互不相同 | 谁把某个触发词改回 `"mono"` / `"mono core"`，立刻红 |
| 冲突检查的扫描面非空 | 防止它变成"什么都没读到所以永远绿"的摆设 |
| 检测器不是瞎的 | 逐个核对预期触发词（`monocore` / `monoshader`），证明检测器真在读那个位置 |

> **为什么用元数据而不是反射**：两个命令都继承 `ModCommand`，而 `ModCommand` 在验收台没有引用的
> `tModLoader.dll` 里。运行时解析不了基类，反射**连这些类型都拿不到**（`GetTypes()` 抛
> `ReflectionTypeLoadException` 后它们不在 `Types` 数组里）——于是"数一数有几个命令"会得到 **0**，
> 一条本该报警的用例反而变成永远绿。这件事是实测踩出来的：先用反射写，得到的就是 0；元数据能看到 2。

---

## 8. 已知缺口

| 项 | 现状 | 缺口 |
|---|---|---|
| `MonoRandom` 的 xoshiro 变体 | 只有 PCG32 | 蓝图 §4.4 提到 "PCG32 / xoshiro128**"。**只实现了一条**，理由是没有第二个使用场景；要加时它是同一个接口的一个分支。**注意**：换算法等于换序列，验收台里那条参考向量会当场变红——那正是它的用途 |
| 事件总线的订阅上限 | 无上限，按需增长 | 蓝图要求"零热路径分配"，而 `List.Add` 在订阅期会扩容。**订阅是加载期行为，所以这不是热路径问题**——但如果将来有人每帧订阅，它会变成问题 |
| `MonoPool` 的容量调整 | 构造后固定 | 没有 `Resize`。刻意的（§1 不变量 III），但"压力下该调到多大"只能靠 `MissCount` 事后发现 |
| 调度器的 `Cancel` 复杂度 | O(n) 线性扫描 | 512 项上限下无所谓。要 O(1) 需要反向索引，收益与复杂度不成比例 |
| `Delay` 的时基 | 到期 = 排程 tick + n + 2 | 那两个 `+1` 各有出处（见 §3 的表），但"`Delay(30)` 在第 131 tick 跑"确实不是第一眼能猜到的数。**已知代价**：想彻底消掉它就得把 `Pump` 改成严格语义，代价是每一处"下一帧"都要自己算 `+2`——两边都不干净，选了把约定集中写在 `Pump` 一处 |
| `MonoTimeline` 的事件关键帧 | 没有 | 蓝图 §4.5 提到"关键帧 + 插值通道"；**事件帧**（"第 N 帧触发回调"）没做，`MonoScheduler.At` 可以替代，但接口不统一 |
| 时间轴的曲线类型 | float / Vector2 / Color | 缺任意结构体的重载（可以自己 `new MonoChannel<T>`，但没有便捷重载） |
| `MonoLog` 的表达式求值 | 只保证"不拼串" | 插值里的表达式仍会被求值（编译器语义，见 §5.9）。要"不算"只能调用方自己 `IsEnabled` 守卫 |
| `MonoLog` 的输出目标 | 只有内存环形缓冲 | 没有写到 `tModLoader-Logs` 的文件通道。诊断导出靠 `Dump()` 返回字符串，由命令回显 |
| 缓动曲线的可选参数 | `OutBack(t, overshoot)` / `Step(t, threshold)` 有默认值，不能用方法组直接转成 `Func<float,float>` | `MonoUtil.Ease(kind, mode)` 走的是默认值那一格。要自定义强度得自己写 lambda 或调 `Apply(...)`——目前没有"带强度的常驻委托"这一格 |
| 核心层对数学层的依赖 | `Core` 依赖 `Common/MonoUtil` 的缓动与随机 | 这是刻意的（§2），但它是**双向耦合的起点风险**：往后 `MonoUtil` 里任何一次改动都可能波及 L1。守住它的办法是"只依赖纯函数、不依赖任何带状态的类型" |
| 单元测试 | `Monochrome.CoreTests` 独立控制台 runner（71 用例）+ 游戏内自检 `/monocore selftest` | 没有 xunit 工程（与 16.7.5 记录的分析器测试是同一个原因：离线）。**仍然只能人工制造条件的**：命中定格时 tick 停住、弱引用被 GC 回收、热重载不泄漏——见 §7.3 |
| 五阶段的**退场**路径 | 世界里服务处于已装配态（自检断言了这一点） | `Unload` / `DisposeBuffers` 只在热重载或退出游戏时跑，自检跑不到。**要验它就得真的 `/reload` 一次**，见 §7.3 |
| 游戏内自检的副作用 | 收尾会故意触发一次"晚注册被拒绝"，留下一条 Error 日志并把 `LateRegistrationRejected` +1 | 这是刻意的（否则那条拒绝路径没有任何证据）。**正常游玩路径上该计数恒为 0**；若自检之外它涨了，说明有东西在加载期之外注册服务 |

---

## 9. 常见错误速查

| 症状 | 原因 |
|---|---|
| 句柄指向了一个毫不相干的对象 | 用了裸下标当身份（不变量 I） |
| 暂停时东西还在动 | 玩法逻辑用了 `RealTime` 而不是 `Tick` |
| 切出窗口回来绳索炸开 | 积分器用了未 clamp 的 dt；`RealDelta` 已经 clamp 到 1/30 秒 |
| 命中定格期间事件没触发 | 订阅挂在 `WorldUpdate` / `PostUpdate` / `PreDraw` 上（三个都在闸门内） |
| 模组卸载后崩在 `TypeLoadException` | 用了普通 `event` 而不是 `MonoEventBus`；或者把**实例方法**订阅进了 `Action<object>` 那个重载 |
| 同一个回调被触发两次 | 同一个静态方法订阅了两次。用 `SubscribeStatic`（它按委托身份查重）而不是泛型重载 |
| 掉落物因为多了一个粒子而变样 | 视觉与玩法共用了同一条随机流（`MonoRngPurpose` 分流域） |
| `Rejected` / `MissCount` 一直在涨 | 每帧重排而不是排一次；或者有东西忘了归还 |
| "延迟 30 tick"晚了一两 tick | 见 §3 的时基表：`Delay(n)` 在第 `t+n+2` tick 跑。这是约定，不是 bug |
| 刚进世界头几帧音效不响 | `MonoScheduler.Reset()` 没清冷却表，读到了上一个世界留下的假冷却 |
| 某一个动作在自己执行时又跑了 | `Pump` 必须按"泵开始时的快照"处理，不能让本轮新排入的项在同一个 while 里被取走 |
| 补间永远到不了目标 | 手写了 `x += (t - x) * 0.1f`。用 `MonoTween`（它按 tick 走完，终点精确） |
| 补间调了 `Update` 却一步不走 | 它走的是**有状态**的"秒换 tick"换算器：`Update(1/60)` 要 60 次才够一秒。测试/工具里要可复现步进请用 `MonoTween.PumpTicks(n)` |
| 补间在掉帧/切出窗口后卡住 | 换算器把上限夹在了扣减之后，被减成负数。顺序必须先夹上限再扣（见 `MonoTween.Update`） |
| 时间轴"播放头没动" | 用了 `Update()`，它读 `MonoTime.RealDelta`——那个只在游戏里被 `MonoFrameSystem` 推进。手动步进请用 `Update(double)` |
| 非循环时间轴停在末尾却还 `IsPlaying` | 端点判断写成了严格不等号。`Position` 正好在 `Duration` 上时也该钳制并停住 |
| `Stop()` 之后协程还是"活着" | 状态机只用数值比较判终态，而 `Stopped` 的数值比 `Done` 大。两个终态都要列出来（见 `MonoCoroutine.IsAlive`） |
| 缓动在 180° 掉头处得到 NaN 旋转 | `Smoothed` 切线在 `dir_prev + dir_next ≈ 0` 时归一化（见蓝图 §7.6.4 第 3 条） |
| 日志里只剩 8 秒历史 | 每帧在记日志。环形缓冲 512 条，每秒 60 条就是 8.5 秒 |
| `MonoLog.Info($"...")` 像是不走处理器 | 不要给 `Info` 加 `string` 重载：插值串会优先选它，惰性路径当场变成死代码。要记拼好的字符串用 `MonoLog.Write(level, s)` |
| 给日志方法加处理器参数后编译器抱怨"缺少 formattedCount" | 处理器参数**不能带 `ref`**，而且被 `[InterpolatedStringHandlerArgument("level")]` 引用的级别参数必须排在它**前面** |
| "我明明只改了一处缓动，另一个特效也跟着变了"（或反过来：两处都叫 `OutQuad` 却不一致） | 库里有第二份缓动定义。曲线只允许一份，验收台那条反射用例就是守它的 |
| 同一个种子换一条随机构造方式后序列对不上 | `MonoRandom(state, sequence)` 的 `sequence` 只在播种**之后**生效；把 `(sequence << 1) \| 1` 提前到播种那两步会得到另一条流。验收台 `SeedingIgnoresSequence` 钉的是正确的那一种 |
| 掉落表的概率总是比配置偏一点点 | 取整用了 `% bound`。`bound` 不是 2 的幂时它会让前 `2^32 mod bound` 个值偏多——用 `MonoRandom.NextInt`（拒绝阈值 + 重抽） |
| 反射检查"永远绿"、实际什么都没查 | 离线环境里 `GetTypes()` / `Namespace` / `ReturnType` 都会抛（签名里引用了没装 tML 的程序集）。反射扫描必须逐步 try/catch，并且**额外断言扫描面非空** |
| "订阅了但回调从没跑过" | 钩子名写错（`ModSystem` 的重写方法写错不报错，只是永不触发）。跑 `/monocore selftest`：它会把 11 个阶段各自的派发次数列出来，哪个是 0 就是哪个钩子没挂上 |
| `/monocore bus` 里所有阶段都是"订阅 0" | 这是**正常的**：库自己不是消费者。关键是看"派发 N"那一列——它在涨就说明接线是活的 |
| 事件订阅为什么监听器数量是 0 却还有回调在跑 | `MonoLifecycleProbe` / 自检那类**静态**或已回收目标的条目。`Describe()` 里 `·` 是活的、`×` 是已死待压缩 |
| **"订阅了但回调一次都不跑"** | 先确认它订阅的阶段在不在世界更新闸门内（定格时整段不执行）；再确认实例回调传的是方法组。实例写 `Subscribe<T>`、静态写 `SubscribeStatic` —— 见 §5.2 |
| **一个阶段只有最后一个订阅者能收到回调** | 有效性曾经是"每个阶段一个版本号"，每次新订阅都会让先前的条目失效。现在每条自己的 `Alive` —— 见 §5.2 |
| 某个订阅者"偶尔不执行" | 派发途中有人退订时用了 `RemoveAt`，把相邻条目的下标挪走导致被跳过。退订只置 `Alive = false` —— 见 §5.2 |
| `/monocore` 的命令没反应（或跑到了图形命令里） | **两个 `ModCommand` 的触发词第一段撞了**。tML 按第一段做键，后注册的那个永远接不到且不报错。两个独立命令的触发词必须是两个不同的词：`monocore` / `monoshader` —— 见 §7.4 |
| 新加的 `/mono xxx` "没反应" | 同上：`"mono xxx"` 的键就是 `"mono"`，会与别的 `mono...` 命令撞。给新命令一个**独立的第一个词** |
| 子命令打错了却刷屏十几行 | 打错的默认分支应只回一行"可用 /xxx help 查询"，清单留给显式的 `help` 子命令 |
