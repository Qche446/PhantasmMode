using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Monochrome.Core.UI;

/// <summary>
/// 给 tModLoader 模组列表里属于本模组的那一行盖一层自己的皮肤。
/// <para>
/// 面板底色由 <c>UIPanel.DrawSelf</c> 画出来，那套贴图与配色是 tModLoader 内嵌的，既换不掉也没有扩展点。
/// 这里用 IL 钩子做两件事：注册过皮肤的行<b>跳过</b>原版底板，改由皮肤自己画整块；没注册的行照旧。
/// 随后把该行的名字文字接管过来自己画（见 <see cref="MonoModRowName"/>）。
/// </para>
/// <para>
/// 挂在两处：<c>UIModItem.DrawSelf</c> 负责跳过它自己那句底板调用；
/// <c>UIPanel.DrawSelf</c> 兜住另一条路径——别家模组（例如 Daybreak）会把列表行换成 <c>UIModItem</c> 的派生类，
/// 并在自己的 <c>DrawSelf</c> 里直接调用基类底板，那时 <c>UIModItem.DrawSelf</c> 根本不会执行。
/// 两条路径都只会画一次：跳过底板的行不会再走到 <c>UIPanel.DrawSelf</c>。
/// </para>
/// <para>
/// 行里的图标、按钮、启用状态都还在原来的绘制顺序上，不受影响。
/// 画什么由 <see cref="MonoRowSkins"/> 决定：调用方模组按自己的模组名注册，没注册的行什么都不画。
/// Monochrome 自己那一行也在同一张表里（见 <see cref="MonoModRowStyle"/>）。
/// </para>
/// </summary>
public sealed class MonoModRowSkinHook : ModSystem
{
    private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly ConditionalWeakTable<object, string> modNames = [];

    /// <summary>已经接管过名字文字的行，避免每帧重复挂子元素。</summary>
    private static readonly ConditionalWeakTable<object, object> nameOverlays = [];

    private static Type? rowType;
    private static FieldInfo? modField;
    private static PropertyInfo? modNameProperty;
    private static FieldInfo? nameField;

    /// <summary>本实例是否真的装上了钩子。装失败时保持 <see langword="false"/>，卸载时据此判断要不要清缓存。</summary>
    private bool installed;

    /// <inheritdoc/>
    public override void OnModLoad()
    {
        // 专用服务器没有界面，也就没有 Main.MenuUI 可画。
        if (Main.dedServ)
            return;

        installed = TryInstall();

        if (!installed)
            return;

        // 库自己也是消费者：先把 Monochrome 那一行的皮肤挂上去，API 有毛边当场就能发现。
        MonoRowSkins.Register(MonoModRowStyle.TargetModName, MonoModRowStyle.Draw, MonoModRowStyle.DrawName);
    }

    /// <inheritdoc/>
    public override void Unload()
    {
        // 注册表里存着调用方模组的委托，不在这里清掉会让 tML 报卸载残留。
        MonoRowSkins.Clear();

        // IL 钩子由 tModLoader 按程序集带走，这里只丢掉自己缓存的反射句柄与行对象。
        if (!installed)
            return;

        installed = false;
        rowType = null;
        modField = null;
        modNameProperty = null;
        nameField = null;
    }

    private static bool TryInstall()
    {
        rowType = Type.GetType("Terraria.ModLoader.UI.UIModItem, tModLoader");
        if (rowType is null)
        {
            MonoLog.Warn(MonoLogLevel.Warn, $"模组行皮肤：找不到 Terraria.ModLoader.UI.UIModItem，跳过；模组列表保持原样。");
            return false;
        }

        MethodInfo? drawSelf = rowType.GetMethod("DrawSelf", InstanceNonPublic);
        MethodInfo? panelDrawSelf = typeof(UIPanel).GetMethod("DrawSelf", InstanceNonPublic);
        MethodInfo? overlay = typeof(MonoModRowSkinHook).GetMethod(nameof(DrawOverlay), BindingFlags.Static | BindingFlags.NonPublic);

        if (drawSelf is null || overlay is null)
        {
            MonoLog.Warn(MonoLogLevel.Warn, $"模组行皮肤：UIModItem.DrawSelf 的结构和预期不同，跳过；模组列表保持原样。");
            return false;
        }

        // 名字文字是子元素，钩子里换不掉绘制；拿到它之后另挂一层自绘文字盖上去。
        nameField = rowType.GetField("_modName", InstanceNonPublic);

        MonoModHooks.Modify(drawSelf, il => PatchRow(il, overlay));

        if (panelDrawSelf is not null)
            MonoModHooks.Modify(panelDrawSelf, PatchPanel);

        if (nameField is null)
            MonoLog.Info(MonoLogLevel.Info, $"模组行皮肤：已挂到 UIModItem.DrawSelf 与 UIPanel.DrawSelf（没找到 _modName，名字文字保持原样）。");
        else
            MonoLog.Info(MonoLogLevel.Info, $"模组行皮肤：已挂到 UIModItem.DrawSelf 与 UIPanel.DrawSelf（底板与名字文字都可接管）。");
        return true;
    }

    private static void PatchRow(ILContext il, MethodInfo overlay)
    {
        // 第一步：在原版画完底板之后追加一次皮肤绘制。
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNext(MoveType.After, IsPanelDrawSelfCall))
        {
            MonoLog.Warn(MonoLogLevel.Warn, $"模组行皮肤：UIModItem.DrawSelf 里没有找到 UIPanel.DrawSelf 的调用，跳过；模组列表保持原样。");
            return;
        }

        // 这一刻栈上正好没有东西：再压回 this 与 spriteBatch，交给皮肤自己决定画不画。
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Call, il.Module.ImportReference(overlay));

        // 第二步：有皮肤的行跳过原版底板。
        // 必须插在方法入口：原版那句 call 之前栈上已经压着 this 与 spriteBatch，
        // 在那里插 brtrue 会让跳转落点（栈空）与分支点的栈深度对不上，CLR 直接判 InvalidProgram。
        MethodInfo? skipCheck = typeof(MonoModRowSkinHook).GetMethod(nameof(ShouldSkipPanel), BindingFlags.Static | BindingFlags.NonPublic);

        if (skipCheck is null)
            return;

        Instruction? overlayStart = FindOverlayStart(il);

        if (overlayStart is null)
            return;

        ILCursor skipCursor = new(il);

        skipCursor.Emit(OpCodes.Ldarg_0);
        skipCursor.Emit(OpCodes.Call, il.Module.ImportReference(skipCheck));
        skipCursor.Emit(OpCodes.Brtrue, overlayStart);
    }

    /// <summary>
    /// 兜底那条路径：列表行是别家的 <c>UIModItem</c> 派生类时，它会在自己的 <c>DrawSelf</c> 里直接调基类底板，
    /// 于是 <c>UIModItem.DrawSelf</c> 的钩子收不到。这里在 <c>UIPanel.DrawSelf</c> 入口先问一句：
    /// 这一行有皮肤就画完直接返回，没有就照常执行原版。
    /// </summary>
    private static void PatchPanel(ILContext il)
    {
        MethodInfo? drawSkin = typeof(MonoModRowSkinHook).GetMethod(nameof(DrawPanelOrSkin), BindingFlags.Static | BindingFlags.NonPublic);

        if (drawSkin is null || il.Instrs.Count == 0)
            return;

        Instruction original = il.Instrs[0];
        ILCursor cursor = new(il);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Call, il.Module.ImportReference(drawSkin));
        cursor.Emit(OpCodes.Brfalse, original);
        cursor.Emit(OpCodes.Ret);
    }

    /// <summary>原版 <c>UIPanel.DrawSelf</c> 调用之后紧跟的三条指令就是上面插进去的皮肤绘制。</summary>
    private static Instruction? FindOverlayStart(ILContext il)
    {
        for (int i = 0; i < il.Instrs.Count - 3; i++)
        {
            if (IsPanelDrawSelfCall(il.Instrs[i]))
                return il.Instrs[i + 1];
        }

        return null;
    }

    private static bool IsPanelDrawSelfCall(Instruction instruction)
    {
        return instruction.OpCode == OpCodes.Call
            && instruction.Operand is MethodReference method
            && method.Name == "DrawSelf"
            && method.DeclaringType?.FullName == typeof(UIPanel).FullName;
    }

    /// <summary>这一行有没有注册皮肤。注册过就不用再画原版底板了。</summary>
    private static bool ShouldSkipPanel(UIElement row)
    {
        return TryGetModName(row, out string modName) && MonoRowSkins.IsRegistered(modName);
    }

    /// <summary><c>UIPanel.DrawSelf</c> 的兜底入口：有皮肤就画完并跳过原版，返回 <see langword="true"/>。</summary>
    private static bool DrawPanelOrSkin(UIElement panel, SpriteBatch spriteBatch)
    {
        return TryDrawSkin(panel, spriteBatch);
    }

    /// <summary>原版画完底板后调用。没有皮肤的行直接走开，不产生任何绘制与分配。</summary>
    private static void DrawOverlay(UIElement row, SpriteBatch spriteBatch)
    {
        TryDrawSkin(row, spriteBatch);
    }

    private static bool TryDrawSkin(UIElement row, SpriteBatch spriteBatch)
    {
        if (!TryGetModName(row, out string modName) || !MonoRowSkins.TryGet(modName, out Action<MonoRowSkinContext>? draw) || draw is null)
            return false;

        EnsureNameOverlay(row, modName);

        MonoRowSkinContext context = new(spriteBatch, row, modName, row.GetDimensions(), row.GetInnerDimensions());

        try
        {
            draw(context);
        }
        catch (Exception exception)
        {
            MonoRowSkins.Disable(modName, exception);
        }

        return true;
    }

    /// <summary>
    /// 原版名字是 <c>UIText</c> 子元素，在 <c>DrawSelf</c> 之后才画，钩子里挤不到它下面；
    /// 所以把它的颜色清成全透明（保留布局与悬停判定），再挂一层自绘文字上去。
    /// </summary>
    private static void EnsureNameOverlay(UIElement row, string modName)
    {
        if (nameOverlays.TryGetValue(row, out _))
            return;

        if (nameField?.GetValue(row) is not UIText name)
            return;

        MonoModRowName overlay = new(name, modName)
        {
            IgnoresMouseInteraction = true,
        };

        row.Append(overlay);
        nameOverlays.Add(row, overlay);
    }

    private static bool TryGetModName(UIElement row, out string modName)
    {
        if (modNames.TryGetValue(row, out modName!))
            return true;

        modName = string.Empty;

        // 别家模组可能把行换成 UIModItem 的派生类，所以这里是类型兼容判断而不是等值判断。
        if (rowType is null || !rowType.IsInstanceOfType(row))
            return false;

        modField ??= rowType.GetField("_mod", InstanceNonPublic);
        object? localMod = modField?.GetValue(row);

        if (localMod is null)
            return false;

        modNameProperty ??= localMod.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
        if (modNameProperty?.GetValue(localMod) is not string name || name.Length == 0)
            return false;

        modNames.Add(row, name);
        modName = name;
        return true;
    }
}
