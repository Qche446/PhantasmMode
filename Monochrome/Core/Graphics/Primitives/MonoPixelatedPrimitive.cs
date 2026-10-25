namespace Monochrome.Core.Graphics.Primitives;

/// <summary>
/// 像素化图元画到哪一层。<see cref="MonoPixelationSystem"/> 会在这些时机把对应的离屏目标贴出来。
/// </summary>
public enum MonoPixelationLayer
{
    /// <summary>NPC 之前。</summary>
    BeforeNPCs,

    /// <summary>NPC 之后、弹幕之前。</summary>
    AfterNPCs,

    /// <summary>弹幕之前。</summary>
    BeforeProjectiles,

    /// <summary>弹幕之后。</summary>
    AfterProjectiles,
}

/// <summary>
/// 让 NPC 或弹幕把"要像素化的图元"交给 <see cref="MonoPixelationSystem"/> 统一绘制。
/// <para>
/// 像素化靠"半分辨率离屏目标 + 2× 点采样"实现，所以图元必须先画进那张目标、再在正确的世界绘制时机贴出来。
/// 实现本接口之后，消费者只要在 <see cref="RenderPixelatedPrimitives"/> 里照常提交图元——把
/// <c>MonoPrimitiveSettings.UseUnscaledMatrix</c> 置 <see langword="true"/>（这就是"像素化"那一步，
/// 它让图元走不含缩放的正交投影，与半分辨率目标的缩放配合出像素块）——目标、时机与放大都由系统负责。
/// </para>
/// <para>
/// 漏写 <c>UseUnscaledMatrix</c> 时系统会兜住并按无缩放投影绘制，但仍然请照写：读代码的人要靠它知道
/// 这段图元落在一张半分辨率目标里。
/// </para>
/// <para>
/// <b>不要在实现里开关 <see cref="SpriteBatch"/></b>：调用点已经在原始绘制路径上（与 <c>MonoPrim.*</c> 同一约定），
/// 开着的批次会被随后的原始绘制打乱。系统会用 <see cref="MonoPrimitiveBatch"/> 包住整层的绘制，
/// 在里面另做原始绘制同样会与提交时机错开。
/// </para>
/// <para>
/// 有不由 NPC / 弹幕提交的手工绘制时，用 <see cref="MonoPixelationSystem.EnqueueRender"/> 排进同一层，
/// 它会排在该层消费者之后、同一个批次里提交。单个消费者抛异常只会损失它自己那部分，系统按类型告警一次。
/// </para>
/// </summary>
public interface IMonoPixelatedPrimitive
{
    /// <summary>画到哪一层。默认 <see cref="MonoPixelationLayer.BeforeProjectiles"/>。</summary>
    MonoPixelationLayer LayerToRenderTo => MonoPixelationLayer.BeforeProjectiles;

    /// <summary>在这里提交要像素化的图元。</summary>
    void RenderPixelatedPrimitives();
}
