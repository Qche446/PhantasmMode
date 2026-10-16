namespace Monochrome.Core.Graphics.Metaballs;

/// <summary>
/// 一颗融合球粒子。<b>它只有数据，行为全在拥有它的 <see cref="MonoMetaballType"/> 里</b>——
/// 运动写 <c>UpdateParticle</c>，消亡写 <c>ShouldKillParticle</c>，所以同一个类型下的粒子行为一致。
/// <para>
/// <see cref="Size"/> 是这颗球写进场里时软圆贴图的<b>绘制边长（像素）</b>：它同时是"球看起来多大"，
/// 不是半径，也不参与任何阈值换算——融合的胖瘦完全由贴图自己的 alpha 梯度决定。
/// </para>
/// <para>
/// <see cref="ExtraInfo"/> 是留给类型自己的四个浮点数（旋转、亮度、相位、透明度……），
/// 含义由拥有它的类型定义，基类不读不写。
/// </para>
/// </summary>
public sealed class MonoMetaballInstance
{
    /// <summary>球心。世界层给世界坐标，界面层（<see cref="MonoMetaballType.Space"/> 为 <see cref="MonoGraphicsSpace.UI"/>）给界面坐标。</summary>
    public Vector2 Center;

    /// <summary>每帧位移：由类型在 <c>UpdateParticle</c> 里改，管理器直接加上去，不乘时间步长。</summary>
    public Vector2 Velocity;

    /// <summary>写场用的绘制边长（像素），见类型说明。</summary>
    public float Size;

    /// <summary>类型自定义的四个浮点数，含义由类型约定。</summary>
    public float[] ExtraInfo;

    /// <param name="center">球心。</param>
    /// <param name="velocity">每帧位移。</param>
    /// <param name="size">绘制边长（像素）。</param>
    /// <param name="extraInfo0">类型自定义数据 0。</param>
    /// <param name="extraInfo1">类型自定义数据 1。</param>
    /// <param name="extraInfo2">类型自定义数据 2。</param>
    /// <param name="extraInfo3">类型自定义数据 3。</param>
    public MonoMetaballInstance(Vector2 center, Vector2 velocity, float size, float extraInfo0 = 0f, float extraInfo1 = 0f, float extraInfo2 = 0f, float extraInfo3 = 0f)
    {
        Center = center;
        Velocity = velocity;
        Size = size;
        ExtraInfo = [extraInfo0, extraInfo1, extraInfo2, extraInfo3];
    }
}
