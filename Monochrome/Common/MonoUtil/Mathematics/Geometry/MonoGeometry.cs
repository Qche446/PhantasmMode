using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil
{
    /// <summary>
    /// 数学与几何工具库的公共常量与数值工具。
    /// <para>
    /// 本文件是整个 Geometry 层的「纪律来源」：所有几何类型统一使用
    /// <see cref="MonoUtil.Epsilon"/> 做浮点比较、统一用 <see cref="MonoUtil.WrapAngle"/> 处理角度差、
    /// 统一用 <see cref="MonoUtil.SafeNormalize(Vector2, Vector2)"/> 处理零向量。
    /// </para>
    /// <para>
    /// 放在 <c>MonoUtil</c> 命名空间下而非 <c>...Geometry</c>，是为了让几何类型
    /// 与已有的 <c>MonoUtil</c> 共用一条 <c>using Monochrome.Common.MonoUtil;</c>，
    /// 避免每处消费都额外引一个命名空间。
    /// </para>
    /// </summary>
    public static partial class MonoUtil
    {

        /// <summary>逐分量取绝对值。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 Abs(Vector2 value) => new(Math.Abs(value.X), Math.Abs(value.Y));

        /// <summary>线性插值，供几何类型内部使用。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Lerp(float a, float b, float t) => a + (b - a) * t;

        #region 常量
        /// <summary>
        /// 浮点比较容差。不要再用 <c>==</c> 比较浮点，一律用 <see cref="Approx(float, float, float)"/> 系列。
        /// </summary>
        public const float Epsilon = 1e-5f;

        /// <summary>
        /// 平方比较用的容差（<see cref="Epsilon"/> 的平方），用于省去开方的距离比较。
        /// </summary>
        public const float EpsilonSqr = Epsilon * Epsilon;

        /// <summary>π。</summary>
        public const float Pi = MathHelper.Pi;
        /// <summary>2π。 </summary>
        public const float TwoPi = MathHelper.TwoPi;
        /// <summary>π/2。</summary>
        public const float PiOver2 = MathHelper.PiOver2;
        /// <summary>π/4。</summary>
        public const float PiOver4 = MathHelper.PiOver4;
        /// <summary>π/3</summary>
        public const float PiOver3 = MathHelper.Pi / 3f;
        /// <summary>π/5</summary>
        public const float PiOver5 = MathHelper.Pi / 5f;
        /// <summary>π/6</summary>
        public const float PiOver6 = MathHelper.Pi / 6f;

        /// <summary>
        /// 正无穷，用于「射线」这类无界图元的长度。
        /// </summary>
        public const float Infinity = float.PositiveInfinity;
        #endregion

        #region 标量
        /// <summary>
        /// 判断两浮点是否近似相等。
        /// </summary>
        /// <param name="a">第一个值。</param>
        /// <param name="b">第二个值。</param>
        /// <param name="tolerance">容差，默认 <see cref="Epsilon"/>。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approx(float a, float b, float tolerance = Epsilon) => Math.Abs(a - b) <= tolerance;

        /// <summary>
        /// 判断矢量两分量是否均近似相等。
        /// </summary>
        /// <param name="a">第一个矢量。</param>
        /// <param name="b">第二个矢量。</param>
        /// <param name="tolerance">容差，默认 <see cref="Epsilon"/>。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approx(Vector2 a, Vector2 b, float tolerance = Epsilon)
            => Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance;

        /// <summary>
        /// 判断一浮点是否近似为 0。
        /// </summary>
        /// <param name="value">被检查的值。</param>
        /// <param name="tolerance">容差，默认 <see cref="Epsilon"/>。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(float value, float tolerance = Epsilon) => Math.Abs(value) <= tolerance;

        /// <summary>
        /// 判断矢量是否近似为零矢量。
        /// </summary>
        /// <param name="value">被检查的矢量。</param>
        /// <param name="tolerance">容差，默认 <see cref="Epsilon"/>。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(Vector2 value, float tolerance = Epsilon) => value.LengthSquared() <= tolerance * tolerance;

        /// <summary>
        /// 截断到 [0,1]。
        /// </summary>
        /// <param name="value">输入值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Saturate(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

        /// <summary>
        /// 截断到 [0,1]，逐分量。
        /// </summary>
        /// <param name="value">输入矢量。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Saturate(Vector2 value) => Vector2.Clamp(value, Vector2.Zero, Vector2.One);

        /// <summary>
        /// 真正的取模。C# 的 <c>%</c> 会保留被除数的符号，做角度/循环索引时是错的。
        /// </summary>
        /// <param name="value">被除数。</param>
        /// <param name="modulus">模数，必须为正。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Mod(float value, float modulus) => value - MathF.Floor(value / modulus) * modulus;

        /// <summary>
        /// 真正的取模（整数版），结果恒为非负。
        /// </summary>
        /// <param name="value">被除数。</param>
        /// <param name="modulus">模数，必须为正。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Mod(int value, int modulus)
        {
            int r = value % modulus;
            return r < 0 ? r + modulus : r;
        }

        /// <summary>
        /// 把 <paramref name="value"/> 从 [0,<paramref name="period"/>] 折叠回 [0,period/2] 的三角波。
        /// 常用于「来回摆动」的运动。
        /// </summary>
        /// <param name="value">输入相位。</param>
        /// <param name="period">周期。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float PingPong(float value, float period)
        {
            float t = Mod(value, period);
            return t <= period * 0.5f ? t : period - t;
        }

        /// <summary>
        /// 把 0~1 的输入映射为 0→1→0 的「正弦鼓包」。
        /// </summary>
        /// <param name="x">输入值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Bump(float x) => MathF.Sin(Pi * Saturate(x));

        /// <summary>
        /// <c>(sin(x) + 1) / 2</c>，把正弦压回 0~1。
        /// </summary>
        /// <param name="x">输入值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin01(float x) => MathF.Sin(x) * 0.5f + 0.5f;

        /// <summary>
        /// <c>(cos(x) + 1) / 2</c>，把余弦压回 0~1。
        /// </summary>
        /// <param name="x">输入值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cos01(float x) => MathF.Cos(x) * 0.5f + 0.5f;

        /// <summary>
        /// 非零符号：返回 1 或 -1，输入 0 时返回 1（避免破坏方向）。
        /// </summary>
        /// <param name="x">输入值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NonZeroSign(float x) => x >= 0f ? 1 : -1;

        /// <summary>
        /// 三次平滑（<c>3x² - 2x³</c>），C1 连续。
        /// </summary>
        /// <param name="x">输入值，会被截断到 [0,1]。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothStep(float x)
        {
            x = Saturate(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>
        /// 五次平滑（Perlin 的 fade 曲线），C2 连续。
        /// </summary>
        /// <param name="x">输入值，会被截断到 [0,1]。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmootherStep(float x)
        {
            x = Saturate(x);
            return x * x * x * (x * (x * 6f - 15f) + 10f);
        }

        /// <summary>
        /// 反插值（带截断）。<c>a == b</c> 时返回 0，不产生除零。
        /// </summary>
        /// <param name="a">区间起点。</param>
        /// <param name="b">区间终点。</param>
        /// <param name="value">被求值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InverseLerp(float a, float b, float value)
        {
            float d = b - a;
            return Math.Abs(d) <= Epsilon ? 0f : Saturate((value - a) / d);
        }

        /// <summary>
        /// 线性映射：把 <paramref name="value"/> 从 [inMin,inMax] 重映射到 [outMin,outMax]（带截断）。
        /// </summary>
        /// <param name="value">被映射的值。</param>
        /// <param name="inMin">输入下界。</param>
        /// <param name="inMax">输入上界。</param>
        /// <param name="outMin">输出下界。</param>
        /// <param name="outMax">输出上界。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
            => MathHelper.Lerp(outMin, outMax, InverseLerp(inMin, inMax, value));

        /// <summary>
        /// 按 <paramref name="maxDelta"/> 的步长把 <paramref name="current"/> 推向 <paramref name="target"/>，不会越过。
        /// </summary>
        /// <param name="current">当前值。</param>
        /// <param name="target">目标值。</param>
        /// <param name="maxDelta">单次最大步长（取绝对值）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Approach(float current, float target, float maxDelta)
        {
            maxDelta = Math.Abs(maxDelta);
            float diff = target - current;
            if (Math.Abs(diff) <= maxDelta)
                return target;
            return current + MathF.Sign(diff) * maxDelta;
        }

        /// <summary>
        /// 把矢量 <paramref name="current"/> 按最大步长推向 <paramref name="target"/>。
        /// </summary>
        /// <param name="current">当前矢量。</param>
        /// <param name="target">目标矢量。</param>
        /// <param name="maxDelta">单次最大位移（像素）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Approach(Vector2 current, Vector2 target, float maxDelta)
        {
            Vector2 diff = target - current;
            float length = diff.Length();
            if (length <= maxDelta || length <= Epsilon)
                return target;
            return current + diff / length * maxDelta;
        }

        /// <summary>
        /// 带衰减的指数趋近。与帧率无关的正确写法是传入 <c>1 - exp(-rate * dt)</c> 形式的系数；
        /// 此处 <paramref name="amount"/> 为单帧比例，调用方自行换算。
        /// </summary>
        /// <param name="current">当前值。</param>
        /// <param name="target">目标值。</param>
        /// <param name="amount">单帧比例，会被截断到 [0,1]。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothDampLerp(float current, float target, float amount)
            => current + (target - current) * Saturate(amount);

        /// <summary>
        /// 逐分量线性插值。
        /// </summary>
        /// <param name="a">起点。</param>
        /// <param name="b">终点。</param>
        /// <param name="t">插值系数，不截断。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;
        #endregion

        #region 角度
        /// <summary>
        /// 把角度规范到 (-π, π]。几何里做角度差一律走这里，直接相减在跨 ±π 时会产生 2π 的误差
        /// （表现为敌人瞬间转身、旋转特效反向）。
        /// </summary>
        /// <param name="angle">输入角度（弧度）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float WrapAngle(float angle) => MathHelper.WrapAngle(angle);

        /// <summary>
        /// 把角度规范到 [0, 2π)。
        /// </summary>
        /// <param name="angle">输入角度（弧度）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float WrapAngle360(float angle) => Mod(angle, TwoPi);

        /// <summary>
        /// 两角之间的最短有符号差，结果落在 (-π, π]。
        /// </summary>
        /// <param name="from">起始角。</param>
        /// <param name="to">目标角。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleDifference(float from, float to) => WrapAngle(to - from);

        /// <summary>
        /// 两角之间的最短无符号距离，结果落在 [0, π]。
        /// </summary>
        /// <param name="a">第一个角。</param>
        /// <param name="b">第二个角。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleDistance(float a, float b) => Math.Abs(AngleDifference(a, b));

        /// <summary>
        /// 沿最短路径把 <paramref name="current"/> 角按最大步长转向 <paramref name="target"/> 角。
        /// </summary>
        /// <param name="current">当前角。</param>
        /// <param name="target">目标角。</param>
        /// <param name="maxDelta">单次最大角步长。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleTowards(float current, float target, float maxDelta)
            => current + Math.Clamp(AngleDifference(current, target), -Math.Abs(maxDelta), Math.Abs(maxDelta));

        /// <summary>
        /// 沿最短路径对角插值。
        /// </summary>
        /// <param name="a">起始角。</param>
        /// <param name="b">目标角。</param>
        /// <param name="t">插值系数，不截断。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpAngle(float a, float b, float t) => a + AngleDifference(a, b) * t;

        /// <summary>
        /// 判断角度是否落在从 <paramref name="start"/> 逆时针扫过 <paramref name="sweep"/> 的扇形内。
        /// </summary>
        /// <param name="angle">被检查的角度。</param>
        /// <param name="start">扇形起始角。</param>
        /// <param name="sweep">扇形张角（弧度，可为负表示顺时针）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AngleInSweep(float angle, float start, float sweep)
        {
            float delta = WrapAngle(angle - start);
            if (sweep >= 0f)
                return delta >= -Epsilon && delta <= sweep + Epsilon;
            return delta <= Epsilon && delta >= sweep - Epsilon;
        }

        /// <summary>
        /// 把角度吸附到最近的 <paramref name="increment"/> 整数倍。
        /// </summary>
        /// <param name="angle">输入角度。</param>
        /// <param name="increment">吸附步长。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SnapAngle(float angle, float increment)
            => MathF.Round(angle / increment) * increment;
        #endregion

        #region 矢量
        /// <summary>
        /// 安全归一化：零矢量（或长度小于容差）时返回 <paramref name="fallback"/>。
        /// 直接用 <see cref="Vector2.Normalize(Vector2)"/> 对零矢量会得到 NaN，随后污染整条计算链。
        /// </summary>
        /// <param name="value">待归一化的矢量。</param>
        /// <param name="fallback">退化时返回的替代矢量，建议传单位矢量。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 SafeNormalize(Vector2 value, Vector2 fallback = default)
        {
            float lengthSquared = value.LengthSquared();
            if (lengthSquared <= EpsilonSqr)
                return fallback;
            return value / MathF.Sqrt(lengthSquared);
        }

        /// <summary>
        /// 从 <paramref name="from"/> 指向 <paramref name="to"/> 的单位矢量；两点重合时返回 <paramref name="fallback"/>。
        /// </summary>
        /// <param name="from">起点。</param>
        /// <param name="to">终点。</param>
        /// <param name="fallback">退化时返回的替代矢量。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 SafeDirectionTo(Vector2 from, Vector2 to, Vector2 fallback = default)
            => SafeNormalize(to - from, fallback);

        /// <summary>
        /// 垂直矢量（逆时针 90°），即 <c>new Vector2(-v.Y, v.X)</c>。
        /// </summary>
        /// <param name="value">输入矢量。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Perpendicular(Vector2 value) => new(-value.Y, value.X);

        /// <summary>
        /// 二维叉积（标量），用于判断左右侧与求交。
        /// </summary>
        /// <param name="a">第一个矢量。</param>
        /// <param name="b">第二个矢量。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

        /// <summary>
        /// 计算 <c>a</c>、<c>b</c>、<c>c</c> 的有符号面积的两倍。
        /// 正为逆时针，负为顺时针，绝对值等于三角形面积的两倍。
        /// </summary>
        /// <param name="a">点 A。</param>
        /// <param name="b">点 B。</param>
        /// <param name="c">点 C。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross(Vector2 a, Vector2 b, Vector2 c) => Cross(b - a, c - a);

        /// <summary>
        /// 把长度约束到 [min,max]；零矢量按 <paramref name="fallback"/> 方向处理。
        /// </summary>
        /// <param name="value">输入矢量。</param>
        /// <param name="min">最小长度。</param>
        /// <param name="max">最大长度。</param>
        /// <param name="fallback">零矢量时的方向。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ClampLength(Vector2 value, float min, float max, Vector2 fallback = default)
        {
            float length = value.Length();
            if (length <= Epsilon)
                return SafeNormalize(value, fallback) * min;
            return value / length * Math.Clamp(length, min, max);
        }

        /// <summary>
        /// 把矢量按最大转角旋转向目标方向，保持长度不变。
        /// </summary>
        /// <param name="value">输入矢量。</param>
        /// <param name="idealAngle">目标角度。</param>
        /// <param name="angleIncrement">单次最大转角。</param>
        public static Vector2 RotateTowards(Vector2 value, float idealAngle, float angleIncrement)
        {
            float length = value.Length();
            if (length <= Epsilon)
                return idealAngle.ToRotationVector2() * length;
            float newAngle = AngleTowards(value.ToRotation(), idealAngle, angleIncrement);
            return newAngle.ToRotationVector2() * length;
        }

        /// <summary>
        /// 以 <paramref name="axis"/> 为对称轴翻转矢量方向（只翻角度，不改长度）。
        /// </summary>
        /// <param name="value">输入矢量。</param>
        /// <param name="axis">对称轴方向矢量。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mirror(Vector2 value, Vector2 axis) => value.RotatedBy(2f * AngleDifference(value.ToRotation(), axis.ToRotation()));

        /// <summary>
        /// 绕 <paramref name="pivot"/> 旋转。
        /// </summary>
        /// <param name="value">输入点。</param>
        /// <param name="pivot">旋转中心。</param>
        /// <param name="angle">旋转角（弧度）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RotateAround(Vector2 value, Vector2 pivot, float angle)
            => pivot + (value - pivot).RotatedBy(angle);

        /// <summary>
        /// 取整到 <paramref name="gridSize"/> 的网格交点，用于「对齐到像素」防止纹理渗色。
        /// </summary>
        /// <param name="value">输入点。</param>
        /// <param name="gridSize">网格尺寸，默认 1 像素。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 SnapToGrid(Vector2 value, float gridSize = 1f)
            => new(MathF.Round(value.X / gridSize) * gridSize, MathF.Round(value.Y / gridSize) * gridSize);
        #endregion
    }
}

