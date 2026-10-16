# FPM 对 Luminance 的依赖扫描

扫描时间：2026-10-13。扫描对象：`FargosPhantasmMode/` 全部 337 个 `.cs`。

## 口径

用 Roslyn 载入 FPM 的完整编译单元（235 个引用程序集，与 `dotnet build` 同源），对每个语法节点做语义解析，筛出「符号归属程序集 = Luminance」的引用。编译诊断 0 错误、0 个文件解析失败，所以下面这份清单是完整的，不存在靠正则匹配漏掉或错认的项。

- 命中符号：**120 个**（已剔除 `using` 带出的命名空间符号，以及具名实参解析出的参数符号）
- 引用点：**871 处**
- 涉及文件：**98 个**

`using` 分布：`Luminance` 153 处、`Luminance.Common` 98、`Luminance.Common.Utilities` 62、`Luminance.Core` 53、`Luminance.Core.Graphics` 52、`Luminance.Common.DataStructures` 31、`Luminance.Common.StateMachines` 5、`Luminance.Assets` 2、`Luminance.Core.Sounds` 1。

## 分组总览

| 组 | 含义 | 符号数 | 引用点 | 文件 |
|---|---|---|---|---|
| A | Monochrome 已有同形对应，换名即可 | 17 | 369 | 49 |
| B | Monochrome 有对应能力，但签名 / 单位 / 形态不同，需改写 | 24 | 240 | 51 |
| C | Monochrome 没有，需要新增 | 42 | 124 | 59 |
| D | 本轮不做（棺材 boss 状态机） | 36 | 109 | 6 |

另有 `Utilities` 类型本身被显式写出 29 处（形如 `Utilities.SafeDirectionTo(...)`），它只是限定名，实际归属按被调用的成员算进 A/B/C。

---

## A 组：换名即可

命名空间同时要换：`Luminance.Core.Graphics` → `Monochrome.Core.Graphics` / `Monochrome.Core.Graphics.Shaders` / `Monochrome.Core.Graphics.Screen`，`Luminance.Common.Utilities` → `Monochrome.Common.MonoUtil`。

| Luminance | Monochrome | 引用点 |
|---|---|---|
| `ManagedShader.TrySetParameter` | `MonoShader.TrySetParameter` | 164 |
| `ShaderManager`（类型） | `MonoShaderManager` | 40 |
| `ShaderManager.GetShader` | `MonoShaderManager.GetShader` | 39 |
| `ManagedShader`（类型） | `MonoShader` | 39 |
| `ScreenShakeSystem.StartShake` | `MonoScreenEffectsSystem.StartShake` | 22 |
| `ScreenShakeSystem`（类型） | `MonoScreenEffectsSystem` | 22 |
| `ManagedShader.WrappedEffect` | `MonoShader.WrappedEffect` | 12 |
| `SpriteBatch.ResetToDefault` | `MonoSpriteBatchExtensions.ResetToDefault` | 10 |
| `SpriteBatch.UseBlendState` | `MonoSpriteBatchExtensions.UseBlendState` | 6 |
| `ManagedShader.Apply` | `MonoShader.Apply` | 5 |
| `Utilities.NonZeroSign` | `MonoUtil.NonZeroSign` | 2 |
| `Utilities.RotateTowards` | `MonoUtil.RotateTowards` | 2 |
| `Utilities.SecondsToFrames` | `MonoUtil.SecondsToFrames` | 2 |
| `Utilities.ClampLength` | `MonoUtil.ClampLength` | 1 |
| `ManagedShader.SetTexture` | `MonoShader.SetTexture` | 1 |
| `SpriteBatch.PrepareForShaders` | `MonoSpriteBatchExtensions.PrepareForShaders` | 1 |
| `SpriteBatch.ResetToDefaultUI` | `MonoSpriteBatchExtensions.ResetToDefaultUI` | 1 |

着色器命名约定两边一致（`模组名.文件名`），FPM 的 `Mod` 类已经打了 `[MonoShaderScope]`，所以 `ShaderManager.GetShader("FargosPhantasmMode.BigTentacle")` 这类调用换成 `MonoShaderManager` 后名字不用动。

## B 组：有对应，需改写

| Luminance | Monochrome | 差异 | 引用点 |
|---|---|---|---|
| `PrimitiveRenderer` | `MonoPrim` | 类型改名 | 22 |
| `PrimitiveRenderer.RenderTrail` | `MonoPrim.RenderTrail` | 同名同形 | 22 |
| `PrimitiveSettings` | `MonoPrimitiveSettings` | 构造参数表更长（多 `BlendState`、`Space`、`Texture`、`Sampler`） | 12 |
| `PrimitiveSettings` 构造调用 | `MonoPrimitiveSettings` 构造调用 | `WidthFunction` 在 Luminance 是半宽、在 Monochrome 是全宽，同一个 lambda 搬过来要乘 2 | 5 |
| `PrimitiveSettings.VertexWidthFunction` | `System.Func<float,float>` | 委托类型改名 | 5 |
| `Particle` | `MonoParticle` | 类型改名 | 16 |
| `Particle.Spawn` | `MonoParticle.Spawn` | 同名 | 19 |
| `Particle.DrawColor` | `MonoParticle.Color` | 字段改名 | 2 |
| `Particle.Texture` | `MonoParticle.Texture` | 类型从 `AtlasTexture` 变 `Texture2D` | 3 |
| `Particle.Scale / Velocity / Position / Rotation / Lifetime / LifetimeRatio / Opacity` | 同名 | 无 | 15 |
| `Utilities.SafeDirectionTo`（Entity 扩展，100 处 / 22 个文件） | `MonoUtil.SafeDirectionTo(from, to, fallback)` | Monochrome 那个是 `internal` 且签名不同，需要公开并补 `Entity` 扩展 | 107 |
| `Utilities.CircularHitboxCollision` | `MonoUtil.Intersect.CircleAABB(MonoCircle, MonoAABB)` | 需补 `Rectangle` 便捷重载；Luminance 那份取四角最近距离判定，Monochrome 是标准圆-盒判定 | 8 |
| `ManagedRenderTarget` + 构造 | `MonoManagedRenderTarget` / `MonoRt.Create` / `MonoRt.ScreenSized` | 构造参数顺序不同（Luminance 首参是 `shouldResetUponScreenResize`） | 3 |
| `Utilities.InverseLerpBump(a, b, c, d, x)` | `MonoUtil.Plateau(x, a, b, c, d)` | 参数顺序不同，语义一致 | 1 |

## C 组：Monochrome 没有，需要新增

### C1 像素化图元层（19 处 / 13 个文件）

- `IPixelatedPrimitiveRenderer`（13）：`Content/Bosses/Coffin/DarkOnyx.cs`、`Mutant/MutantCoffinWaveShot.cs`、`Mutant/MutantTrueEyeLSR.cs`、`Mutant/PhantasmalDeathrayWOFS.cs`、`EyeOfCthulhu/MoonBolt.cs`、`MoonlightTrail.cs`、`MoonScythe.cs`、`MoonScythe2.cs`、`Kingslime/KingSlimeSword.cs`、`Twins/DarkStar.cs`、`Twins/TwinCurvedLaser.cs`、`Items/Global/Accessories/Enchantments/Nature/Nature.cs`、`Nature/Snow.cs`
- `PixelationPrimitiveLayer` + `BeforeNPCs` / `AfterProjectiles`（6）
- `PrimitiveSettings.Pixelate` 参数（14 处具名实参；它们解析成参数符号，不在上面 124 的统计里）
- `PrimitivePixelationSystem` 未直接用到，但同属这套

Monochrome 的 `Core/Graphics/README.md`「还缺什么」一节写着「Pixelate 不做——Luminance 已经有了」，现在这条前提消失，需要重新决策。

### C2 Luminance 的 Metaball 体系（20 处 / 6 个文件）

- `MetaballType`（5）、`MetaballType.CreateParticle`（4）、`ActiveParticleCount`（2）、`LayerTargets`（2）、`ShouldRender`（1）、`PrepareShaderForTarget`（1）
- `MetaballInstance`（2）、`MetaballInstance.Size`（2）、`MetaballInstance.Velocity`（1）
- 文件：`Common/MetaBalls/FixedMetaball.cs`、`LuMetaBallDrawfixed.cs`、`CosmicFireMetaBall.cs`、`Content/Bosses/Coffin/DarkOnyx.cs`、`Content/Bosses/VanillaEternity/Twins/ShadowFlame.cs`、`Content/Items/Weapons/Monochrome/HunterMark_Black.cs`

Monochrome 的 metaball 是另一套模型（`MonoMetaball` 值类型 + `MonoMetaballInstance`/`MonoMetaballBrush`/`MonoMetaballManager`/`MonoMetaballSettings`），没有「注册一个 MetaballType 子类、由管理器批量出粒子」这层。**已按路线一落地**：FPM 这边改成 `MonoMetaballInstance` 子类 `CosmicFireBlob` + `CosmicFireMetaballSystem`，Monochrome 侧给 `MonoMetaballSettings` 加了 `CompositeShader` / `ConfigureComposite` 两个口子，用来接 `FargosPhantasmMode.BigTentacle`。

### C3 弹幕归属结构（47 处 / 30 个文件）

- `Luminance.Common.DataStructures.IProjOwnedByBoss<T>`（46）、`KillAll()`（1）
- 全部集中在 `Content/Bosses/Mutant/`，实现者是 Mutant 的弹幕类，宿主泛型参数是 FSS 的 `MutantBoss`

Monochrome 没有对应设施。这个接口的语义是「我把这颗弹幕挂在 boss 名下」，便于 boss 死亡时统一清场。

### C4 Utilities 里缺的工具（16 处）

| 符号 | 引用点 | 文件 |
|---|---|---|
| `Utilities.As<TNPC>()` / `As<TProjectile>()` | 4 + 2 | `Content/Bosses/Coffin/CursedCoffinAI.cs`、`P_CursedSpirit.cs`、`Abom/AbomDeathrayMark.cs`、`Items/Weapons/Monochrome/HunterMark_Black.cs` |
| `Utilities.HorizontalDirectionTo` | 3 | `CursedCoffinAI.cs`、`CursedCoffinStateManagement.cs`、`Kingslime/KingSlime.cs` |
| `Utilities.OnRightSideOf` | 2 | `Twins/Twins.cs` |
| `Utilities.AnyBosses()` | 2 | `Content/InfoDisplays/InfoModPlayer.cs`、`Core/Systems/WorldUpdateSystem.cs` |
| `Utilities.HeldMouseItem()` | 1 | `Content/Items/Global/PModeGlobalItem.cs` |
| `Utilities.DrawBloomLine()` | 1 | `Content/Render/ParticleRender.cs`（依赖 Bloom，Monochrome 的 Bloom 也在暂缓清单里） |
| `Utilities.UniversalBindingFlags` | 1 | `Core/Systems/PModeNet.cs` |

### C5 图集与资产注册（10 处 / 3 个文件）

- `Atlas`、`AtlasTexture`（`Frame` / `Atlas` / `Texture`）：`Common/Particles/FlameParticle.cs`
- `LazyAsset<Texture2D>.Value`、`MiscTexturesRegistry.InvisiblePixel` / `BloomFlare`：`Content/Bossbar/BossBarRegistry.cs`、`Bosses/VanillaEternity/Twins/Twins.cs`

Monochrome 只有 `MonoParticleTextures` / `MonoTrailTextures` 两个内置纹理集合，没有通用的图集与 `LazyAsset` 注册表。**已定：图集不进库**。`FlameParticle.cs` 与空文件 `MoonFire.cs` 已核实 0 引用并连同 `Common/Particles/` 目录删除；剩下两处贴图已换成本地来源——`InvisiblePixel` → `Terraria/Images/MagicPixel`，`BloomFlare` → FSS 的 `FargosTextureRegistry.BloomFlareTexture`（FPM 本来就依赖 FSS）。

### C6 循环音效（9 处 / 1 个文件）

- `LoopedSoundInstance`（`HasLoopSoundBeenStarted` / `HasBeenStopped` / `Stop` / `Restart` / `Update`）、`LoopedSoundManager.CreateNew`
- 只有 `Content/Bosses/Trojan/TrojanSquirrelArms.cs`

Monochrome 没有音频模块。

### C7 屏幕滤镜取用形态（3 处 / 1 个文件）

- `ShaderManager.GetFilter("...")` → `ManagedScreenFilter.Activate()`，在 `Content/Bosses/Mutant/MutantBoss.cs:3709`

Monochrome 的对应物是 `MonoPostFxSystem.GetOrCreate` + `MonoShaderPass` / `MonoScreenFx`，是「往管线里加 pass」的模型，没有「取一条滤镜然后 Activate」的形态。

## D 组：本轮不做

棺材 boss 的状态机，共 6 个文件：`Content/Bosses/Coffin/CursedCoffinAI.cs`、`CursedCoffinStateManagement.cs`、`P_CursedCoffin.cs`、`P_CursedSpirit.cs`，以及 FPM 自己的包装 `Common/PhanUtil.cs`、`Common/Pstate.cs`。

用到的 Luminance 符号是 `PushdownAutomata<TState, E>`、`EntityAIState<TId>`、`IState<TId>`、`AutoloadAsBehavior` 及它们的 `StateStack` / `StateRegistry` / `CurrentState` / `Identifier` / `Time` / `RegisterTransition` / `ApplyToAllStatesExcept` / `OnStateTransition` / `OnStackEmpty` 等，109 处。Monochrome 侧已落库的 `MonoFsm<TId>` 是扁平状态机，与下推自动机不同形；`PhanUtil` 的 5 个扩展与 `Pstate` 本来就在既定的 P4 清死代码清单里。

## 决策结果（2026-10-13）

| 项 | 决定 | 状态 |
|---|---|---|
| C1 像素化图元层 | 补进 Monochrome | **已落地**（`IMonoPixelatedPrimitive` + `MonoPixelationLayer` + `MonoPixelationSystem`，FPM 侧 13 个文件已迁移） |
| C2 Metaball | 路线一：FPM 改写到 `MonoMetaballInstance`，Monochrome 补 `CompositeShader` / `ConfigureComposite` | **已落地** |
| C3 `IProjOwnedByBoss<T>` | 在 Monochrome 做通用版，再替换 FPM 的 30 个文件 | **已落地**（`IMonoOwnedProj<TOwner>` + `MonoProj.KillOwned<TOwner>`） |
| C4 Utilities 七个工具 | 随 A/B 批次按需补进 `MonoUtil` | 待做 |
| C5 图集 | 不进库；无引用的 `FlameParticle` / `MoonFire` 删除，两处贴图换本地来源 | **已落地** |
| C6 循环音效 | 进 Monochrome | **已落地**（`MonoLoopedSound` + `MonoLoopedSoundManager`） |
| C7 屏幕滤镜 | Monochrome 补 `GetFilter` / `Activate` 形态 | **已落地** |

`AutoDraw` 保持全局开关、不做逐层版本；需要自己挑合成时机的层由消费者关掉它再手动 `Draw(layer)`（幽火即在 `On_Main.DrawProjectiles` 里这么做）。

C 组至此清零。A/B 两组按批替换，进度：

| 批次 | 内容 | 状态 |
|---|---|---|
| ⑤ 着色器 | `ManagedShader`/`ShaderManager` → `MonoShader`/`MonoShaderManager`，24 个文件 | 已落地 |
| ⑥ 图元 | 余下 9 处 `PrimitiveRenderer`/`PrimitiveSettings` → `MonoPrim`/`MonoPrimitiveSettings`（宽度在调用点 ×2），`VertexWidthFunction` → `Func<float,float>` | 已落地 |
| ⑦ 屏幕震动 | `ScreenShakeSystem` → `MonoScreenEffectsSystem`；因两边参数语义不同，Monochrome 补了像素单位的 `StartShakeByAmplitude`，28 处调用照搬旧数字 | 已落地 |
| ⑧ 渲染目标 | `ManagedRenderTarget` → `MonoRt.ScreenSized()` | 已落地 |
| ⑨ 粒子 | `Particle` → `MonoParticle` | **已落地**（FPM 自建 6 个 `MonoParticle` 子类，贴图取自 FSS 的粒子图集） |
| ⑩ Utilities | `SafeDirectionTo`(107) / `CircularHitboxCollision`(8) / `As<T>`(6) 等工具补进 `MonoUtil` 后替换 | 已落地（56 个文件） |
| ⑪ 收尾 | 删净 `using Luminance.*`（已删 47 条），清 `MonoShaderReloader` 的 Luminance 编译期引用 | **已落地** |

**收尾状态（2026-10-13）**：FPM 内非状态机的 Luminance 引用为 **0**；`MonoShaderReloader` 的 5 处编译期引用已改为反射，`Monochrome.csproj` 里的 Luminance 引用随之删除（运行期关系仍由 `build.txt` 的 `weakReferences` / `sortAfter` 描述）。

**FPM 的 `FargosPhantasmMode.csproj` 里的 `<Reference Include="Luminance">` 暂时还不能删**：D 组那 6 个文件仍在用 `Luminance.Common.StateMachines`（109 处，用户明确「暂时不管棺材 boss 的状态机」）。这是最后一段路——D 组处理完，FPM 就能彻底摘掉这个引用。

重扫后的剩余量：**36 符号 / 109 处 / 6 文件——全部是 D 组（棺材 boss 状态机）**，非状态机引用为 **0**。
`MonoShaderReloader.cs` 里还剩 5 处 Luminance 的编译期引用（4 处 `ShaderManager` 调用 + 1 处 `typeof(ShaderRecompilationMonitor)`），改成反射即可让 Monochrome 也摘掉 `lib/Luminance.dll`。

**FPM 的 `FargosPhantasmMode.csproj` 里的 `<Reference Include="Luminance">` 暂时还不能删**：D 组那 6 个文件仍在用 `Luminance.Common.StateMachines`（用户明确「暂时不管棺材 boss 的状态机」）。

## 顺带发现的

`Monochrome/Core/Graphics/Shaders/MonoShaderReloader.cs` 目前有对 Luminance 的编译期引用（`SyncLuminanceRegistry` 里调 `Luminance.Core.Graphics.ShaderManager.TryGetShader` / `SetShader` / `TryGetFilter`，以及反射停用它的文件监视器）。FPM 摘掉 Luminance 之后这段要跟着处理，否则 Monochrome 自己会拖住 Luminance。

---

原始数据（逐符号 + 逐行位置）与扫描器都是本机临时产物，留在 `.tmp/lumiscan/`，未纳入版本控制；需要重扫时照该目录的 `Program.cs` 用 `dotnet msbuild -getItem:` 导出的引用表跑一遍即可。
