using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil
{
    /// <summary>
    /// 缓动类型。
    /// </summary>
    public enum MonoEaseKind
    {
        /// <summary>线性。</summary>
        Linear,

        /// <summary>二次。</summary>
        Quad,

        /// <summary>三次。</summary>
        Cubic,

        /// <summary>四次。</summary>
        Quart,

        /// <summary>五次。</summary>
        Quint,

        /// <summary>正弦。</summary>
        Sine,

        /// <summary>指数。</summary>
        Expo,

        /// <summary>圆形。</summary>
        Circ,

        /// <summary>回弹（会越过目标再回来）。</summary>
        Back,

        /// <summary>弹性（阻尼振荡）。</summary>
        Elastic,

        /// <summary>弹跳（触底反弹）。</summary>
        Bounce,
    }

    /// <summary>
    /// 缓动方向。
    /// </summary>
    public enum MonoEaseMode
    {
        /// <summary>渐入（慢起）。</summary>
        In,

        /// <summary>渐出（慢停）。</summary>
        Out,

        /// <summary>两端都缓。</summary>
        InOut,

        /// <summary>两端缓但斜率更平（更「软」）。</summary>
        InOutSoft,
    }

    /// <summary>
    /// 缓动与数值工具：全表缓动、弹簧阻尼、以及 Remap / MoveTowards / Wobble 等常用工具。
    /// </summary>
    public static partial class MonoUtil
    {
        #region 基础表
        /// <summary>线性。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Linear(float t) => Saturate(t);

        /// <summary>二次渐入。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InQuad(float t) => (t = Saturate(t)) * t;

        /// <summary>二次渐出。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutQuad(float t) => 1f - (1f - Saturate(t)) * (1f - Saturate(t));

        /// <summary>二次两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutQuad(float t)
        {
            t = Saturate(t);
            return t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
        }

        /// <summary>三次渐入。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InCubic(float t) => (t = Saturate(t)) * t * t;

        /// <summary>三次渐出。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutCubic(float t) => 1f - MathF.Pow(1f - Saturate(t), 3f);

        /// <summary>三次两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutCubic(float t)
        {
            t = Saturate(t);
            return t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        /// <summary>四次渐入。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InQuart(float t) => MathF.Pow(Saturate(t), 4f);

        /// <summary>四次渐出。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutQuart(float t) => 1f - MathF.Pow(1f - Saturate(t), 4f);

        /// <summary>四次两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutQuart(float t)
        {
            t = Saturate(t);
            return t < 0.5f ? 8f * t * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 4f) * 0.5f;
        }

        /// <summary>五次渐入。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InQuint(float t) => MathF.Pow(Saturate(t), 5f);

        /// <summary>五次渐出。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutQuint(float t) => 1f - MathF.Pow(1f - Saturate(t), 5f);

        /// <summary>五次两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutQuint(float t)
        {
            t = Saturate(t);
            return t < 0.5f ? 16f * t * t * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 5f) * 0.5f;
        }

        /// <summary>正弦渐入。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InSine(float t) => 1f - MathF.Cos(Saturate(t) * PiOver2);

        /// <summary>正弦渐出。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutSine(float t) => MathF.Sin(Saturate(t) * PiOver2);

        /// <summary>正弦两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutSine(float t) => -(MathF.Cos(Pi * Saturate(t)) - 1f) * 0.5f;

        /// <summary>指数渐入。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InExpo(float t)
        {
            t = Saturate(t);
            return t <= 0f ? 0f : MathF.Pow(2f, 10f * t - 10f);
        }

        /// <summary>指数渐出。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutExpo(float t)
        {
            t = Saturate(t);
            return t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);
        }

        /// <summary>指数两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutExpo(float t)
        {
            t = Saturate(t);
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;
            return t < 0.5f
                ? MathF.Pow(2f, 20f * t - 10f) * 0.5f
                : (2f - MathF.Pow(2f, -20f * t + 10f)) * 0.5f;
        }

        /// <summary>圆形渐入。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InCirc(float t) => 1f - MathF.Sqrt(MathF.Max(1f - Saturate(t) * Saturate(t), 0f));

        /// <summary>圆形渐出。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutCirc(float t)
        {
            float u = 1f - Saturate(t);
            return MathF.Sqrt(MathF.Max(1f - u * u, 0f));
        }

        /// <summary>圆形两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutCirc(float t)
        {
            t = Saturate(t);
            if (t < 0.5f)
            {
                float u = 2f * t;
                return (1f - MathF.Sqrt(MathF.Max(1f - u * u, 0f))) * 0.5f;
            }
            {
                float u = -2f * t + 2f;
                return (MathF.Sqrt(MathF.Max(1f - u * u, 0f)) + 1f) * 0.5f;
            }
        }

        /// <summary>回弹渐入（先向后退一点再冲出去）。</summary>
        /// <param name="t">归一化时间。</param>
        /// <param name="overshoot">回弹强度。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InBack(float t, float overshoot = 1.70158f)
        {
            t = Saturate(t);
            return (overshoot + 1f) * t * t * t - overshoot * t * t;
        }

        /// <summary>回弹渐出（冲过目标再回来）。</summary>
        /// <param name="t">归一化时间。</param>
        /// <param name="overshoot">回弹强度。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            float u = Saturate(t) - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }

        /// <summary>回弹两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        /// <param name="overshoot">回弹强度。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutBack(float t, float overshoot = 1.70158f)
        {
            t = Saturate(t);
            float c2 = overshoot * 1.525f;
            if (t < 0.5f)
            {
                float u = 2f * t;
                return u * u * ((c2 + 1f) * u - c2) * 0.5f;
            }
            {
                float u = 2f * t - 2f;
                return (u * u * ((c2 + 1f) * u + c2) + 2f) * 0.5f;
            }
        }

        /// <summary>弹性渐入。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InElastic(float t)
        {
            t = Saturate(t);
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;
            const float c4 = TwoPi / 3f;
            return -MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((t * 10f - 10.75f) * c4);
        }

        /// <summary>弹性渐出。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutElastic(float t)
        {
            t = Saturate(t);
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;
            const float c4 = TwoPi / 3f;
            return MathF.Pow(2f, -10f * t) * MathF.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>弹性两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutElastic(float t)
        {
            t = Saturate(t);
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;

            const float c5 = TwoPi / 4.5f;
            return t < 0.5f
                ? -(MathF.Pow(2f, 20f * t - 10f) * MathF.Sin((20f * t - 11.125f) * c5)) * 0.5f
                : MathF.Pow(2f, -20f * t + 10f) * MathF.Sin((20f * t - 11.125f) * c5) * 0.5f + 1f;
        }

        /// <summary>弹跳渐入。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InBounce(float t) => 1f - OutBounce(1f - Saturate(t));

        /// <summary>弹跳渐出。</summary>
        /// <param name="t">归一化时间。</param>
        public static float OutBounce(float t)
        {
            t = Saturate(t);
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
                return n1 * t * t;
            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        /// <summary>弹跳两端缓。</summary>
        /// <param name="t">归一化时间。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutBounce(float t)
        {
            t = Saturate(t);
            return t < 0.5f
                ? (1f - OutBounce(1f - 2f * t)) * 0.5f
                : (1f + OutBounce(2f * t - 1f)) * 0.5f;
        }

        /// <summary>阶跃：小于阈值返回 0，否则返回 1。</summary>
        /// <param name="t">归一化时间。</param>
        /// <param name="threshold">阈值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Step(float t, float threshold = 0.5f) => t < threshold ? 0f : 1f;

        #endregion

        #region 常驻委托表
        /// <summary>
        /// 「类型 + 方向」→ 已缓存的委托。<b>它的唯一目的是让缓动可以当 <c>Func&lt;float,float&gt;</c> 传出去却不分配。</b>
        /// <para>
        /// 补间、时间轴这类消费者的签名是 <c>Func&lt;float,float&gt;</c>，而<see cref="MonoEaseKind"/> 与
        /// <see cref="MonoEaseMode"/> 是两个 4 字节的枚举。每次补间都写
        /// <c>ease: t =&gt; Evaluate(kind, mode, t)</c> 会包一个闭包——那是每帧每补间一次分配，
        /// 正好踩在本库的零分配纪律上。
        /// </para>
        /// <para>
        /// 表在静态构造里一次性建好，之后只读；<b>它不含 <c>overshoot</c></b>，
        /// 用的是 <see cref="InBack(float,float)"/> 的默认回弹强度。要自定义回弹强度就用
        /// <c>Apply(...)</c> 或自己写 lambda。
        /// </para>
        /// </summary>
        /// <param name="kind">缓动类型。</param>
        /// <param name="mode">缓动方向。<c>InOutSoft</c> 按 <c>InOut</c> 处理（本表没有"更软的 InOut"这个变体）。</param>
        /// <returns>可以直接传进补间/时间轴的委托。恒不为 null。</returns>
        public static Func<float, float> Ease(MonoEaseKind kind, MonoEaseMode mode) => EaseTable[(int)kind, (int)mode];

        /// <summary>
        /// 默认缓动（<c>Linear</c>）：<b>补间不传缓动时用它，而时间轴通道的 <c>null</c> 也是同一个意思</b>
        /// ——全库的"不做缓动"只有一个语义。
        /// <para>
        /// 它<b>就是表里 <c>(Linear, In)</c> 那一格本身</b>，不是一个另外写出来的 lambda——
        /// 写 <c>ease: t =&gt; t</c> 会每次新建一个闭包对象，而这里是一个静态字段，传多少次都是同一个引用。
        /// 字段在<b>静态构造函数</b>里赋值（不能在这里写初始化式：那会形成静态初始化循环）。
        /// </para>
        /// </summary>
        public static readonly Func<float, float> LinearEase;

        /// <summary>已缓存的缓动委托。索引是 <c>[类型, 方向]</c>，两维都按枚举值排。</summary>
        private static readonly Func<float, float>[,] EaseTable;

        /// <summary>
        /// 建表并挑出默认缓动。
        /// <para>
        /// 它<b>必须是静态构造函数</b>：<see cref="LinearEase"/> 是 <c>static readonly</c>，只能在静态构造函数或字段
        /// 初始化式里赋值，普通的静态方法（哪怕只被调用一次）都不行（CS0198）。
        /// </para>
        /// </summary>
        static MonoUtil()
        {
            int kinds = Enum.GetValues<MonoEaseKind>().Length;
            int modes = Enum.GetValues<MonoEaseMode>().Length;
            Func<float, float>[,] table = new Func<float, float>[kinds, modes];

            for (int k = 0; k < kinds; k++)
            {
                for (int m = 0; m < modes; m++)
                {
                    MonoEaseKind kind = (MonoEaseKind)k;
                    MonoEaseMode mode = (MonoEaseMode)m;
                    table[k, m] = t => Evaluate(kind, mode, t);
                }
            }

            EaseTable = table;

            // 默认缓动复用表里那一格，而不是再包一个等价但不相同的委托。
            LinearEase = table[(int)MonoEaseKind.Linear, (int)MonoEaseMode.In];
        }

        #endregion

        #region 分派
        /// <summary>按类型与方向分派到具体缓动函数。</summary>
        /// <param name="kind">缓动类型。</param>
        /// <param name="mode">缓动方向。</param>
        /// <param name="t">归一化时间。</param>
        /// <param name="overshoot">回弹强度（仅 <see cref="MonoEaseKind.Back"/> 使用）。</param>
        public static float Evaluate(MonoEaseKind kind, MonoEaseMode mode, float t, float overshoot = 1.70158f)
        {
            switch (kind)
            {
                case MonoEaseKind.Linear:
                    return Linear(t);
                case MonoEaseKind.Quad:
                    return mode switch
                    {
                        MonoEaseMode.In => InQuad(t),
                        MonoEaseMode.Out => OutQuad(t),
                        _ => InOutQuad(t),
                    };
                case MonoEaseKind.Cubic:
                    return mode switch
                    {
                        MonoEaseMode.In => InCubic(t),
                        MonoEaseMode.Out => OutCubic(t),
                        _ => InOutCubic(t),
                    };
                case MonoEaseKind.Quart:
                    return mode switch
                    {
                        MonoEaseMode.In => InQuart(t),
                        MonoEaseMode.Out => OutQuart(t),
                        _ => InOutQuart(t),
                    };
                case MonoEaseKind.Quint:
                    return mode switch
                    {
                        MonoEaseMode.In => InQuint(t),
                        MonoEaseMode.Out => OutQuint(t),
                        _ => InOutQuint(t),
                    };
                case MonoEaseKind.Sine:
                    return mode switch
                    {
                        MonoEaseMode.In => InSine(t),
                        MonoEaseMode.Out => OutSine(t),
                        _ => InOutSine(t),
                    };
                case MonoEaseKind.Expo:
                    return mode switch
                    {
                        MonoEaseMode.In => InExpo(t),
                        MonoEaseMode.Out => OutExpo(t),
                        _ => InOutExpo(t),
                    };
                case MonoEaseKind.Circ:
                    return mode switch
                    {
                        MonoEaseMode.In => InCirc(t),
                        MonoEaseMode.Out => OutCirc(t),
                        _ => InOutCirc(t),
                    };
                case MonoEaseKind.Back:
                    return mode switch
                    {
                        MonoEaseMode.In => InBack(t, overshoot),
                        MonoEaseMode.Out => OutBack(t, overshoot),
                        _ => InOutBack(t, overshoot),
                    };
                case MonoEaseKind.Elastic:
                    return mode switch
                    {
                        MonoEaseMode.In => InElastic(t),
                        MonoEaseMode.Out => OutElastic(t),
                        _ => InOutElastic(t),
                    };
                case MonoEaseKind.Bounce:
                    return mode switch
                    {
                        MonoEaseMode.In => InBounce(t),
                        MonoEaseMode.Out => OutBounce(t),
                        _ => InOutBounce(t),
                    };
                default:
                    return Linear(t);
            }
        }

        /// <summary>
        /// 把缓动直接作用到 [start,end] 区间上：<c>Apply(0f, 100f, MonoEaseKind.OutCubic,...)</c>。
        /// </summary>
        /// <param name="start">起始值。</param>
        /// <param name="end">结束值。</param>
        /// <param name="kind">缓动类型。</param>
        /// <param name="mode">缓动方向。</param>
        /// <param name="t">归一化时间。</param>
        /// <param name="overshoot">回弹强度。</param>
        public static float Apply(float start, float end, MonoEaseKind kind, MonoEaseMode mode, float t, float overshoot = 1.70158f)
            => MathHelper.Lerp(start, end, Evaluate(kind, mode, t, overshoot));

        /// <summary>把缓动作用到矢量区间上。</summary>
        /// <param name="start">起点。</param>
        /// <param name="end">终点。</param>
        /// <param name="kind">缓动类型。</param>
        /// <param name="mode">缓动方向。</param>
        /// <param name="t">归一化时间。</param>
        public static Vector2 Apply(Vector2 start, Vector2 end, MonoEaseKind kind, MonoEaseMode mode, float t)
            => MonoUtil.Lerp(start, end, Evaluate(kind, mode, t));

        /// <summary>把缓动作用到颜色上（用于渐隐/闪烁）。</summary>
        /// <param name="start">起始颜色。</param>
        /// <param name="end">结束颜色。</param>
        /// <param name="kind">缓动类型。</param>
        /// <param name="mode">缓动方向。</param>
        /// <param name="t">归一化时间。</param>
        public static Color Apply(Color start, Color end, MonoEaseKind kind, MonoEaseMode mode, float t)
            => Color.Lerp(start, end, Evaluate(kind, mode, t));
        #endregion

        /// <summary>
        /// 数值与弹簧工具。弹簧阻尼特别适合「目标会动」的跟随（相机、UI 指示器），
        /// 缓动函数则适合「目标固定」的一次性过渡。
        /// </summary>
        /// <summary>
        /// 临界阻尼弹簧：把 <paramref name="current"/> 拉向 <paramref name="target"/>，同时维护速度。
        /// 使用半隐式欧拉 + 精确的临界阻尼解，与帧率无关（<paramref name="deltaTime"/> 变化时行为一致）。
        /// </summary>
        /// <param name="current">当前值。</param>
        /// <param name="target">目标值。</param>
        /// <param name="velocity">速度（会被就地更新）。</param>
        /// <param name="smoothTime">到达目标所需的近似时间，越小越快。必须为正。</param>
        /// <param name="deltaTime">帧间隔秒数。</param>
        /// <param name="maxSpeed">最大速度，&lt;= 0 表示不限。</param>
        public static float SpringTo(float current, float target, ref float velocity, float smoothTime, float deltaTime, float maxSpeed = 0f)
        {
            smoothTime = MathF.Max(smoothTime, 1e-4f);

            // 精确的临界阻尼解，避免大 dt 时发散。
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exponential = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

            float change = current - target;
            float originalTarget = target;

            if (maxSpeed > 0f)
            {
                float maxChange = maxSpeed * smoothTime;
                change = Math.Clamp(change, -maxChange, maxChange);
            }

            target = current - change;

            float temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exponential;
            float result = target + (change + temp) * exponential;

            // 防止过冲：如果结果已经越过目标且方向与初始位移相反，则直接吸附。
            if (originalTarget - current > 0f == result > originalTarget)
            {
                result = originalTarget;
                velocity = (result - originalTarget) / MathF.Max(deltaTime, 1e-5f);
            }

            return result;
        }

        /// <summary>矢量版临界阻尼弹簧。</summary>
        /// <param name="current">当前矢量。</param>
        /// <param name="target">目标矢量。</param>
        /// <param name="velocity">速度（会被就地更新）。</param>
        /// <param name="smoothTime">平滑时间。</param>
        /// <param name="deltaTime">帧间隔秒数。</param>
        /// <param name="maxSpeed">最大速度，&lt;= 0 表示不限。</param>
        public static Vector2 SpringTo(Vector2 current, Vector2 target, ref Vector2 velocity, float smoothTime, float deltaTime, float maxSpeed = 0f)
        {
            float vx = velocity.X;
            float vy = velocity.Y;
            float x = SpringTo(current.X, target.X, ref vx, smoothTime, deltaTime, maxSpeed);
            float y = SpringTo(current.Y, target.Y, ref vy, smoothTime, deltaTime, maxSpeed);
            velocity = new Vector2(vx, vy);
            return new Vector2(x, y);
        }

        /// <summary>角度弹簧：沿最短路径转动。</summary>
        /// <param name="current">当前角。</param>
        /// <param name="target">目标角。</param>
        /// <param name="velocity">角速度（会被就地更新）。</param>
        /// <param name="smoothTime">平滑时间。</param>
        /// <param name="deltaTime">帧间隔秒数。</param>
        public static float SpringAngleTo(float current, float target, ref float velocity, float smoothTime, float deltaTime)
        {
            // 把目标折算到当前角附近，避免跨 ±π 时弹簧「绕远路」。
            float wrappedTarget = current + MonoUtil.AngleDifference(current, target);
            return SpringTo(current, wrappedTarget, ref velocity, smoothTime, deltaTime);
        }

        /// <summary>按最大步长把值推向目标（线性，不做缓动）。</summary>
        /// <param name="current">当前值。</param>
        /// <param name="target">目标值。</param>
        /// <param name="maxDelta">单次最大步长。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MoveTowards(float current, float target, float maxDelta) => MonoUtil.Approach(current, target, maxDelta);

        /// <summary>按最大步长把矢量推向目标。</summary>
        /// <param name="current">当前矢量。</param>
        /// <param name="target">目标矢量。</param>
        /// <param name="maxDelta">单次最大位移。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta) => MonoUtil.Approach(current, target, maxDelta);

        /// <summary>
        /// 双端渐入渐出：在 [fadeInStart, fadeOutEnd] 上形成 0→1→0 的平台。
        /// 常用于「距离衰减」与「相机聚焦强度」。
        /// </summary>
        /// <param name="value">输入值。</param>
        /// <param name="riseStart">开始上升处。</param>
        /// <param name="plateauStart">升到 1 处。</param>
        /// <param name="plateauEnd">开始下降处。</param>
        /// <param name="fallEnd">降到 0 处。</param>
        public static float Plateau(float value, float riseStart, float plateauStart, float plateauEnd, float fallEnd)
            => MonoUtil.InverseLerp(riseStart, plateauStart, value) * MonoUtil.InverseLerp(fallEnd, plateauEnd, value);

        /// <summary>摆动：以 <paramref name="amplitude"/> 为幅值、<paramref name="frequency"/> 为频率的正弦抖动。</summary>
        /// <param name="time">时间（秒）。</param>
        /// <param name="amplitude">幅值。</param>
        /// <param name="frequency">频率（Hz）。</param>
        /// <param name="phase">相位偏移。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Wobble(float time, float amplitude = 1f, float frequency = 1f, float phase = 0f)
            => MathF.Sin((time * frequency + phase) * TwoPi) * amplitude;

        /// <summary>
        /// 非周期正弦：叠加两个无理数频率的正弦，得到永不重复的「随机波动」，
        /// 比 <c>rand()</c> 每帧重取更平滑，也不会占随机流。
        /// </summary>
        /// <param name="time">时间。</param>
        /// <param name="offset">相位偏移。</param>
        /// <param name="a">第一频率系数（建议无理数）。</param>
        /// <param name="b">第二频率系数（建议无理数）。</param>
        public static float AperiodicSin(float time, float offset = 0f, float a = Pi, float b = MathHelper.E)
            => (MathF.Sin(time * a + offset) + MathF.Sin(time * b + offset)) * 0.5f;

        /// <summary>帧数 → 秒数（假设 60 FPS）。</summary>
        /// <param name="frames">帧数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FramesToSeconds(float frames) => frames / 60f;

        /// <summary>秒数 → 帧数（假设 60 FPS）。</summary>
        /// <param name="seconds">秒数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SecondsToFrames(float seconds) => (int)MathF.Round(seconds * 60f);

        /// <summary>把一个值按帧率无关的比例插值：<c>1 - exp(-rate * dt)</c>。</summary>
        /// <param name="rate">速率（每秒衰减系数）。</param>
        /// <param name="deltaTime">帧间隔秒数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FrameRateIndependentLerp(float rate, float deltaTime) => 1f - MathF.Exp(-rate * deltaTime);

    }
}

