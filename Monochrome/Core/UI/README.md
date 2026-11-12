# Core/UI —— 界面层的公共入口

这里放的是**别的模组会直接调用**的东西。与 `Core` 其余部分不同，它对接口的稳定性要求更高：
改动会同时影响下游模组的编译。

目前是两件：**模组列表行皮肤**，以及画圆角面板要用的**九宫格底板**。


## 九宫格底板

行皮肤跳过了原版底板，想保留"四角圆弧"就得自己画。`MonoPanel` 就是原版 `UIPanel.DrawPanel` 的那套切分，
贴图仍然用原版的 `Images/UI/PanelBackground`（28×28，12px 圆角 + 4px 边条）：

```csharp
MonoPanel.Draw(context.Batch, bounds, Color.White);
```

四角按原尺寸画、四边沿一个轴拉伸、中心两个轴一起拉伸，所以任意宽高都不会把圆角压扁。
要换成自己画的同布局贴图（同样是 28×28）就传第三个参数：`MonoPanel.Draw(batch, bounds, texture, color)`。
批次与坐标空间由调用方负责，它只按给定矩形落笔、不改绘制状态。

自己的 `.fx`（放在 `Assets/AutoloadedEffects/Shaders/` 下）需要换 effect 时，
配合 `MonoBatchScope` 与 `ResetToDefaultUI` 用，写法见下文「要用着色器或图元」。

## 行皮肤

### 它解决什么

tModLoader 的模组列表里，每一行是内部的 `UIModItem`，底板贴图与边框颜色都写死在里面，
既没有扩展点，类型也不公开（`internal`，直接引用会 `CS0122`）。想换掉自己那一行的底板，
只能靠 IL 钩子。

`MonoModRowSkinHook` 挂了两处，都按**模组名**查注册表：

- `UIModItem.DrawSelf`：注册过的行**跳过**原版那次 `UIPanel.DrawSelf`（不再画 tML 的蓝底板），交给皮肤自己铺满整行；没注册的行原样。
- `UIPanel.DrawSelf`：兜住另一条路径。有的模组（例如 Daybreak）会把列表行换成 `UIModItem` 的派生类，并在自己的 `DrawSelf` 里直接调基类底板，那时 `UIModItem.DrawSelf` 根本不会执行。这里再问一次：这一行有皮肤就画完返回，没有就照常画原版。两条路径不会重复绘制——跳过底板的行不会再走到这里。
- 名字文字也走这张表：传了 `drawName` 就把原版 `UIText` 设成全透明（保留布局，右侧信息图标位置不受影响）再由回调画；
  不传则原版文字照常显示。Monochrome 自己那行的描边与紫青渐变就是注册进去的 `MonoModRowStyle.DrawName`。

### 怎么用

在自己模组的 `PostSetupContent`（或之后任何时刻）注册一次：

```csharp
using Monochrome.Core.UI;

public override void PostSetupContent()
{
    MonoRowSkins.Register("MyMod", static context =>
    {
        Rectangle bounds = new(
            (int)context.Outer.X,
            (int)context.Outer.Y,
            (int)context.Outer.Width,
            (int)context.Outer.Height);

        context.Batch.Draw(TextureAssets.MagicPixel.Value, bounds, Color.Black);
    });
}
```

要注销就 `MonoRowSkins.Unregister("MyMod")`。模组卸载时 Monochrome 会 `Clear()` 整张表。

### 自定义名字

名字文字默认用 Monochrome 那套描边加强调色。想自己画，给 `Register` 传第三个参数：

```csharp
MonoRowSkins.Register("MyMod", DrawBackground, static name =>
{
    // 位置、字号缩放、字体都已算好，直接画即可。
    ChatManager.DrawColorCodedStringWithShadow(
        name.Batch,
        name.Font,
        name.Text,
        name.Position,
        name.Hovered ? Color.Gold : Color.White,
        0f,
        Vector2.Zero,
        new Vector2(name.Scale));
});

private static void DrawBackground(MonoRowSkinContext context) { /* ... */ }
```

`MonoRowNameContext` 给的东西：

| 成员 | 含义 |
|---|---|
| `Batch` | 原版批次，已 `Begin` |
| `Source` | 原版 `UIText`，用来读 `TextOriginX/Y`、`IsMouseHovering` 之类的只读信息 |
| `ModName` / `Text` | 模组名、要画的文字（可能带富文本标记） |
| `Font` | `FontAssets.MouseText` |
| `Position` | 文字左上角的 UI 坐标，已含原版的排版偏移（含 `TextOrigin` 与 `-2f` 那一下） |
| `Scale` | 原版算出的缩放，名字太长时小于 1 |
| `Inner` | 名字所在的矩形 |
| `Hovered` | 鼠标是否停在这一行上 |

不传 `drawName` 时本库不接管名字，原版文字照常显示。Monochrome 自己那一行的名字样式也没有特殊路径：
它就是注册在表里的 `MonoModRowStyle.DrawName`（描边 + 紫青渐变），与消费者能写的东西是同一套。
名字回调抛异常与底板回调同样处理：整条皮肤当场停用，这一行连同名字一起回到原版。
`Unregister` 之后，原版文字的颜色会被还原，不会留下半透明的残影。

### 回调契约

| 项 | 约定 |
|---|---|
| 批次 | **已经 Begin 过**（原版的 `Deferred + AlphaBlend + Main.UIScaleMatrix`）。回调里不要再 `Begin`/`End`，需要改状态就 `End` 之后用 `MonoBatchScope` 重开、画完用 `ResetToDefaultUI` 还原 |
| 坐标 | UI 单位，原点已在 UI 空间。`Outer` 是整行，`Inner` 是去掉内边距的内部区域 |
| 必须铺满整行 | 原版底板**已经跳过**，皮肤是这一行唯一的底色。留空或半透明会直接露出后面的界面 |
| 圆角与悬停 | 原版底板带 12px 圆角、会随悬停改边框色；跳过之后这两件事都归皮肤自己 |
| 绘制顺序 | 皮肤 → 分隔线、右侧信息图标 → 子元素（图标、名字、按钮、启用状态） |
| 名字文字 | 底板回调不负责名字；不传 `drawName` 就保留原版，两处都不要与对方重叠 |
| 调用次数 | 每行每帧一次，只在模组列表显示时 |
| 线程 | UI 线程，不需要加锁 |

回调里 `Append` 自己的 `UIElement` 是允许的，但行对象每次进界面都会重建，
所以不要在回调里反复 `Append`（按帧号或自己记一个标记去重）。

### 要用着色器或图元

回调里的批次是开着的，而 Monochrome 的着色器是给图元层的原始绘制用的（约定 `uWorldViewProjection`、
`useTexture`、`AutoloadPass`），不能直接当 `SpriteBatch.Begin(effect:)` 的自定义效果用。
需要在皮肤里用它们时，把当前批次收掉、画完再按 UI 参数重开：

```csharp
public override void PostSetupContent()
{
    MonoRowSkins.Register("MyMod", static context =>
    {
        context.Batch.End();

        Rectangle bounds = new(
            (int)context.Outer.X,
            (int)context.Outer.Y,
            (int)context.Outer.Width,
            (int)context.Outer.Height);

        // 图元层自己应用效果、自己提交绘制；UI 空间会乘 Main.UIScaleMatrix，坐标就是 context.Outer 那一套。
        // scale 乘以贴图尺寸得到整块大小，所以 1×1 的 MagicPixel 直接传 bounds 的宽高。
        MonoPrim.RenderQuad(
            texture: TextureAssets.MagicPixel.Value,
            center: bounds.Center.ToVector2(),
            scale: new Vector2(bounds.Width, bounds.Height),
            rotation: 0f,
            color: Color.White,
            shader: shader,
            blendState: BlendState.AlphaBlend,
            space: MonoGraphicsSpace.UI);

        context.Batch.ResetToDefaultUI(end: false);
    });
}
```

代价是每帧每行一次 `End` / `Begin`（蓝图 §11.10 的预算是 10 次以内/帧），只在真需要着色器时这么做；
纯渐变、描边、发光仍然用现成的 `SpriteBatch.Draw` 就够，`MonoModRowStyle` 就是这么写的。

如果结果要当贴图复用（多次绘制、做模糊、跨帧缓存），先把皮肤渲染进渲染目标再 `SpriteBatch.Draw` 贴回来。
注意 Deferred 批次里 `Draw` 只是记录纹理、到 `End` 才真正采样，所以那块目标**不能在回调里归还**：
要么画完先 `End` 再以 `Immediate` 贴回（贴完就能归还），要么换成跨帧持有的受管渲染目标。

### 边界

- **只认模组名**，不认行对象、不认下标——行每次进界面都会重建。
- 回调抛异常时该皮肤**当场停用**，记一条 `MonoLog` Error，这一行之后按原版画。继续每帧调用只会让日志与开销一起放大。
- 专用服务器上钩子根本不装（没有 `Main.MenuUI`）。
- 只管"底板"这一层，外加名字文字。图标、按钮、启用状态、分隔线都还是原版。
- 名字只能换"怎么画"，位置与字号仍按原版排版；要挪位置得等以后再加接口，不要在回调里子类化 `UIText`。

### 已知风险

钩子依赖 tModLoader 的几处内部细节：`UIModItem` 的类型名与 `_mod` / `_modName` 字段、
`UIModItem.DrawSelf` 里第一句 `base.DrawSelf` 的形态、以及 `UIPanel.DrawSelf` 的签名。
tML 更新后如果对不上，`UIModItem` 那半会**安静地不装**并在 `MonoLog` 里留一条 Warn
（游戏内 `/monocore log 40` 能看到），`UIPanel` 兜底那半仍会独立尝试；两边都失效时模组列表保持原样，不会崩。

### 文件

| 文件 | 内容 |
|---|---|
| `MonoRowSkins.cs` | 注册表：`Register` / `Unregister` / `IsRegistered` / `Clear` / `Count` |
| `MonoRowSkinContext.cs` | 底板回调的上下文：`Batch` / `Row` / `ModName` / `Outer` / `Inner` / `Hovered` |
| `MonoRowNameContext.cs` | 名字回调的上下文：`Batch` / `Source` / `ModName` / `Text` / `Font` / `Position` / `Scale` / `Inner` / `Hovered` |
| `MonoModRowSkinHook.cs` | IL 钩子与模组名解析；Monochrome 自己的皮肤也在这里注册 |
| `MonoModRowStyle.cs` | Monochrome 那一行的皮肤（底板 `Draw` + 名字 `DrawName`），同时是注册回调的参考实现 |
| `MonoPanel.cs` | 原版九宫格底板：`Draw` / `Background` / `CornerSize` / `BarSize` |
| `MonoModRowName.cs` | 接管名字文字的自绘 `UIText`：从原文字实时读文本与偏移，按注册表分发；没回调就把原文字还原 |
