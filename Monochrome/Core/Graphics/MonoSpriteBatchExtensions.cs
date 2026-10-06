namespace Monochrome.Core.Graphics;

/// <summary>
/// 主 <see cref="SpriteBatch"/> 的状态助手。
/// <para>
/// <b>为什么必须有它：</b>Monochrome 的图元绘制（<see cref="MonoPrim"/>）是绕过 SpriteBatch 的原始 GPU 绘制，
/// 而 Terraria 的 <c>Main.spriteBatch</c> 几乎整个绘制循环都处于 <c>Begin</c> / <c>End</c> 之间。
/// 因此原始绘制只能插在 <c>End()</c> 之后、下一次 <c>Begin()</c> 之前——否则批处理会被打断。
/// 这些助手就是把"结束上一批、用正确的状态重新开始"这件事收敛到一处，
/// 消费者不必自己拼 <c>Begin</c> 的一堆参数。
/// </para>
/// <para>
/// <b>代价：</b>每次 <c>End</c>/<c>Begin</c> 都有固定开销，一帧调用次数请控制在 10 次以内（蓝图 §11.10）。
/// </para>
/// </summary>
public static class MonoSpriteBatchExtensions
{
    /// <summary>
    /// 用指定混合状态重新开始批次（<see cref="SpriteSortMode.Deferred"/>、世界变换矩阵）。
    /// <para>只改变混合状态、不需要即时模式的场合用它。</para>
    /// </summary>
    /// <param name="spriteBatch">目标批次。</param>
    /// <param name="blendState">新的混合状态。</param>
    public static void UseBlendState(this SpriteBatch spriteBatch, BlendState blendState)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, blendState, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
    }

    /// <summary>
    /// 用 <see cref="SpriteSortMode.Immediate"/> 重新开始批次，供自定义着色器使用。
    /// <para>Immediate 模式下每个 <c>Draw</c> 都会立即提交，因此不能与批处理合并——只在真的需要着色器时用。</para>
    /// </summary>
    /// <param name="spriteBatch">目标批次。</param>
    /// <param name="blendState">混合状态；为 null 时用 <see cref="BlendState.AlphaBlend"/>。</param>
    /// <param name="ui">true 表示 UI 绘制（用 <c>Main.UIScaleMatrix</c>），false 用世界变换矩阵。</param>
    public static void PrepareForShaders(this SpriteBatch spriteBatch, BlendState? blendState = null, bool ui = false)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, blendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, ui ? Main.UIScaleMatrix : Main.GameViewMatrix.TransformationMatrix);
    }

    /// <summary>
    /// 把批次恢复到世界绘制的默认状态（Deferred + <see cref="BlendState.AlphaBlend"/> + 世界变换矩阵）。
    /// </summary>
    /// <param name="spriteBatch">目标批次。</param>
    /// <param name="end">是否先 <c>End</c> 掉当前批次。若调用方刚刚自己 <c>End</c> 过，传 false。</param>
    public static void ResetToDefault(this SpriteBatch spriteBatch, bool end = true)
    {
        if (end)
            spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
    }

    /// <summary>把批次恢复到 UI 绘制的默认状态（<c>Main.UIScaleMatrix</c>）。</summary>
    /// <param name="spriteBatch">目标批次。</param>
    /// <param name="end">是否先 <c>End</c> 掉当前批次。</param>
    public static void ResetToDefaultUI(this SpriteBatch spriteBatch, bool end = true)
    {
        if (end)
            spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
    }
}
