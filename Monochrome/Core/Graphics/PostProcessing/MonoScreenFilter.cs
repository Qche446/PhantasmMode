using Monochrome.Core.Graphics.Screen;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 按名字取用、靠 <see cref="Activate"/> 逐帧续期的屏幕滤镜：不透明度自动淡入淡出，降到 0 之后自动停止申请屏幕捕获。
/// <para>
/// 与"直接往 <see cref="MonoPostFxPipeline"/> 里加 pass"的分工：那条路适合由消费者自己决定开关的特效；
/// 这条路适合"由某个实体的状态决定、每帧续期一次"的滤镜——消费者只写一句 <see cref="Activate"/>，
/// 出现与消失的过渡由这里统一处理。
/// </para>
/// <para>
/// 着色器契约与其它屏幕 pass 相同：输入贴图绑在采样槽 1，淡出走 <c>fxOpacity</c>
/// （模板见 <c>Assets/Effects/Screen/</c> 下的任一同胞）。
/// </para>
/// </summary>
public sealed class MonoScreenFilter : MonoShaderPass
{
    /// <summary>
    /// 每帧的不透明度步长。默认 <c>0.015</c>，约 67 帧（1.1 秒）走完一次淡入或淡出。
    /// </summary>
    public float FadeSpeed { get; set; } = 0.015f;

    internal MonoScreenFilter(string shaderName, MonoPostFxPipeline pipeline) : base(shaderName, order: 0)
    {
        // 出生时是不生效的：它只在第一次 Activate 之后淡入。
        Enabled = false;
        Opacity = 0f;
        pipeline.Add(this);
    }

    /// <summary>本帧是否被续期过。诊断用，<see cref="Tick"/> 会把它消费掉。</summary>
    public bool ActivatedThisFrame { get; private set; }

    /// <summary>
    /// 让它在这一帧生效。需要在它该出现的每一帧调用一次；只调一次的话，它会在一个淡出周期后自己消失。
    /// </summary>
    public void Activate() => ActivatedThisFrame = true;

    /// <summary>
    /// 具名滤镜不带自己的参数，效果强弱就是 <c>fxOpacity</c> 本身。
    /// 需要写参数的滤镜请直接派生 <see cref="MonoShaderPass"/>。
    /// </summary>
    protected override void ApplyParameters(MonoShader shader) { }

    /// <summary>
    /// 推进一次淡入淡出，并消费本帧的续期标记。由 <see cref="MonoScreenCaptureSystem"/> 在每帧绘制之前调用，
    /// 消费者不需要自己调。
    /// </summary>
    internal void Tick()
    {
        float target = ActivatedThisFrame ? 1f : 0f;
        float speed = Math.Max(0f, FadeSpeed);
        Opacity = target > Opacity
            ? Math.Min(target, Opacity + speed)
            : Math.Max(target, Opacity - speed);
        ActivatedThisFrame = false;

        // 不透明度到 0 就从链里退出去：屏幕捕获系统按这个判断要不要向原版申请捕获，
        // 所以闲置的滤镜一帧开销都不产生。
        Enabled = Opacity > 0f;
    }
}
