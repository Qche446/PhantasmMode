namespace Monochrome.Core.Graphics.Particles;

/// <summary>
/// 粒子的生命周期与批量绘制。更新挂在 <c>PostUpdateDusts</c>；绘制按
/// <see cref="MonoGraphicsSpace"/> 走两个相位：<c>World</c> 走世界相位（<c>On_Main.DrawDust</c>，
/// 落在 <c>spriteBatch.End()</c> 与下一次 <c>Begin()</c> 之间），<c>Screen</c> / <c>UI</c> 走界面相位
/// （<see cref="PostDrawInterface"/>，画进游戏已经开好的 UI 批次，所以那里的混合状态由游戏决定）。
/// </summary>
public sealed class MonoParticleManager : ModSystem
{
    /// <summary>绘制分组键：混合状态 + 坐标空间。两者任一不同就不能合批。</summary>
    private readonly record struct DrawGroup(BlendState Blend, MonoGraphicsSpace Space);

    private static readonly List<MonoParticle> active = [];
    private static readonly Dictionary<DrawGroup, List<MonoParticle>> drawLists = [];
    private static readonly List<MonoEmitter> emitters = [];

    /// <summary>当前活动的全部粒子。</summary>
    public static IReadOnlyList<MonoParticle> Active => active;

    /// <summary>当前活动的全部发射器。</summary>
    public static IReadOnlyList<MonoEmitter> Emitters => emitters;

    /// <summary>把一个粒子纳入活动集合。已在活动集合中的实例会被忽略。</summary>
    /// <param name="particle">要激活的粒子。</param>
    internal static void Spawn(MonoParticle particle)
    {
        if (particle.Active)
            return;
        particle.Active = true;
        particle.Time = 0;
        active.Add(particle);
        GetList(particle).Add(particle);
    }

    /// <summary>把一个发射器纳入活动集合，并把它自己那点计数归零。已在集合里的实例会被忽略。</summary>
    /// <param name="emitter">要激活的发射器。</param>
    internal static void Attach(MonoEmitter emitter)
    {
        if (emitter.Active)
            return;

        emitter.Reset();
        emitter.Active = true;
        emitters.Add(emitter);
    }

    /// <summary>把一个发射器移出活动集合。它已经发出去的粒子不受影响。</summary>
    /// <param name="emitter">要停掉的发射器。</param>
    internal static void Detach(MonoEmitter emitter)
    {
        if (!emitter.Active)
            return;

        emitter.Active = false;
        emitters.Remove(emitter);
    }

    /// <summary>推进所有粒子的行为、位置、旋转与寿命，并回收到寿的粒子。</summary>
    public override void PostUpdateDusts()
    {
        if (Main.dedServ)
            return;

        // 倒着走：发射器在 Update 里可能因为 Duration 到点而把自己停掉（从表里移除）。
        for (int i = emitters.Count - 1; i >= 0; i--)
            emitters[i].Update();

        for (int i = active.Count - 1; i >= 0; i--)
        {
            MonoParticle particle = active[i];
            particle.Update();
            particle.Position += particle.Velocity;
            particle.Rotation += particle.RotationSpeed;
            particle.Time++;
            if (particle.Time < particle.Lifetime)
                continue;
            RemoveAt(i, particle);
        }
    }

    /// <summary>
    /// 界面相位：把 <c>Screen</c> / <c>UI</c> 的粒子画进<b>游戏已经开好的 UI 批次</b>。
    /// <para>
    /// <b>刻意不 Begin/End</b>：这个钩子是从界面的绘制序列里调的，批次已经开着，再 Begin 会抛异常。
    /// 代价是这些粒子的 <see cref="MonoParticle.BlendState"/> 覆盖不生效。
    /// </para>
    /// </summary>
    /// <param name="spriteBatch">游戏当前已经 <c>Begin</c> 的界面批次。</param>
    public override void PostDrawInterface(SpriteBatch spriteBatch)
    {
        if (Main.dedServ)
            return;
        foreach ((DrawGroup group, List<MonoParticle> particles) in drawLists)
        {
            if (group.Space == MonoGraphicsSpace.World || particles.Count == 0)
                continue;
            foreach (MonoParticle particle in particles)
                particle.Draw(spriteBatch);
        }
    }

    /// <summary>挂上世界相位的绘制钩子（客户端）。界面相位用 <see cref="PostDrawInterface"/>，不需要挂钩子。</summary>
    public override void Load()
    {
        // 热重载时静态表可能还留着上一版建的对象，而它们的行为方法与委托指向的已经不是这一版代码了——
        // 留着它们会在下一次 Update/Draw 里报"尝试调用已生成的 lambda 或本地函数"。先在装载时清一次。
        Clear();
        MonoParticleTextures.Dispose();

        if (!Main.dedServ)
            On_Main.DrawDust += DrawParticles;
    }

    /// <summary>摘掉绘制钩子、清空、丢掉生成的贴图。</summary>
    public override void Unload()
    {
        if (!Main.dedServ)
            On_Main.DrawDust -= DrawParticles;
        Clear();
        MonoParticleTextures.Dispose();
    }

    /// <summary>世界卸载时清空所有粒子与发射器。</summary>
    public override void OnWorldUnload() => Clear();

    /// <summary>模组卸载时再清一次。tML 的两个卸载钩子都会调，谁先谁后没有保证。</summary>
    public override void OnModUnload() => Clear();

    /// <summary>清空活动集合、全部绘制分组与全部发射器。对象本身不被释放，只是变为非活动。</summary>
    public static void Clear()
    {
        active.Clear();
        foreach (List<MonoParticle> list in drawLists.Values)
            list.Clear();
        drawLists.Clear();

        foreach (MonoEmitter emitter in emitters)
            emitter.Active = false;
        emitters.Clear();
    }

    private static void RemoveAt(int index, MonoParticle particle)
    {
        active.RemoveAt(index);
        if (drawLists.TryGetValue(GroupOf(particle), out List<MonoParticle>? list))
            list.Remove(particle);
        particle.Active = false;
    }

    private static DrawGroup GroupOf(MonoParticle particle) => new(particle.BlendState, particle.Space);

    private static List<MonoParticle> GetList(MonoParticle particle)
    {
        DrawGroup group = GroupOf(particle);
        if (!drawLists.TryGetValue(group, out List<MonoParticle>? list))
            drawLists[group] = list = [];
        return list;
    }

    /// <summary>私有钩子：先调 <c>orig</c>，再按混合状态逐组画世界相位的粒子。</summary>
    private static void DrawParticles(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);
        if (Main.dedServ)
            return;
        foreach ((DrawGroup group, List<MonoParticle> particles) in drawLists)
        {
            if (group.Space != MonoGraphicsSpace.World || particles.Count == 0)
                continue;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, group.Blend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (MonoParticle particle in particles)
                particle.Draw(Main.spriteBatch);
            Main.spriteBatch.End();
        }
    }
}
