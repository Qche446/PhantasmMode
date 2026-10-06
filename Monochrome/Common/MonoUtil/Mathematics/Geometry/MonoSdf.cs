using Monochrome.Common.MonoUtil.Mathematics.Geometry;
using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil
{
    //SDF（有向距离场）：距离函数、布尔运算、平滑并集。
    //自定义碰撞、光照遮挡、以及「点到形状最近点」查询。距离约定为外部为正、内部为负，与渲染/寻路社区的习惯一致。
    public static partial class MonoUtil
    {

        /// <summary>
        /// 基础 SDF 图元。全部为纯函数：给定查询点返回 <see cref="MonoSdfSample"/>。
        /// </summary>
        public static partial class Sdf
        {
            /// <summary>圆。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="center">圆心。</param>
            /// <param name="radius">半径。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample Circle(Vector2 point, Vector2 center, float radius)
            {
                Vector2 offset = point - center;
                float length = offset.Length();
                if (length <= MonoUtil.Epsilon)
                    return new MonoSdfSample(-radius, Vector2.UnitY);

                // 用平方根而不是除法：单次求逆再乘，避免两次除法。
                float inverseLength = 1f / length;
                return new MonoSdfSample(length - radius, offset * inverseLength);
            }

            /// <summary>点。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="center">目标点。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample Point(Vector2 point, Vector2 center)
            {
                Vector2 offset = point - center;
                float length = offset.Length();
                return new MonoSdfSample(length, length > MonoUtil.Epsilon ? offset / length : Vector2.UnitY);
            }

            /// <summary>线段（零厚度）。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="a">起点。</param>
            /// <param name="b">终点。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample Segment(Vector2 point, Vector2 a, Vector2 b)
            {
                MonoSegment segment = new(a, b);
                Vector2 closest = segment.ClosestPoint(point);
                Vector2 offset = point - closest;
                float length = offset.Length();
                return new MonoSdfSample(length, length > MonoUtil.Epsilon ? offset / length : Vector2.UnitY);
            }

            /// <summary>胶囊（带厚度的线段）。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="a">中轴起点。</param>
            /// <param name="b">中轴终点。</param>
            /// <param name="radius">厚度半径。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample Capsule(Vector2 point, Vector2 a, Vector2 b, float radius)
            {
                MonoSegment segment = new(a, b);
                Vector2 closest = segment.ClosestPoint(point);
                Vector2 offset = point - closest;
                float length = offset.Length();
                return new MonoSdfSample(length - radius, length > MonoUtil.Epsilon ? offset / length : Vector2.UnitY);
            }

            /// <summary>轴对齐盒（以半尺寸表示，中心在原点）。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="halfExtents">半尺寸。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample Box(Vector2 point, Vector2 halfExtents)
                => RoundedBox(point, halfExtents, 0f);

            /// <summary>
            /// 圆角盒（中心在原点）。<paramref name="halfExtents"/> 是<b>外沿</b>半尺寸（含圆角），
            /// 与 <see cref="MonoRoundedRect"/> 的 <c>HalfExtents</c> 语义一致；芯部尺寸 = 外沿 - 圆角。
            /// </summary>
            /// <param name="point">查询点。</param>
            /// <param name="halfExtents">外沿半尺寸（含圆角）。</param>
            /// <param name="round">圆角半径。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample RoundedBox(Vector2 point, Vector2 halfExtents, float round)
            {
                float corner = MathF.Max(round, 0f);
                Vector2 core = Vector2.Max(halfExtents - new Vector2(corner), Vector2.Zero);
                Vector2 q = Abs(point) - core;
                Vector2 outside = Vector2.Max(q, Vector2.Zero);
                float length = outside.Length();
                float distance = Math.Min(Math.Max(q.X, q.Y), 0f) + length - corner;
                Vector2 gradient = length > MonoUtil.Epsilon
                    ? new Vector2(MathF.Sign(point.X) * outside.X, MathF.Sign(point.Y) * outside.Y) / length
                    : (q.X > q.Y ? new Vector2(MathF.Sign(point.X), 0f) : new Vector2(0f, MathF.Sign(point.Y)));
                return new MonoSdfSample(distance, gradient);
            }

            /// <summary>圆角盒（任意中心与旋转）。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="center">中心。</param>
            /// <param name="halfExtents">外沿半尺寸（含圆角）。</param>
            /// <param name="round">圆角半径。</param>
            /// <param name="rotation">旋转角。</param>
            public static MonoSdfSample RoundedBox(Vector2 point, Vector2 center, Vector2 halfExtents, float round, float rotation = 0f)
            {
                Vector2 local = (point - center).RotatedBy(-rotation);
                MonoSdfSample sample = RoundedBox(local, halfExtents, round);
                return new MonoSdfSample(sample.Distance, sample.Gradient.RotatedBy(rotation));
            }

            /// <summary>圆角矩形（以 <see cref="MonoRoundedRect"/> 描述）。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="rect">圆角矩形。</param>
            public static MonoSdfSample RoundedRect(Vector2 point, MonoRoundedRect rect)
            {
                Vector2 local = (point - rect.Center).RotatedBy(-rect.Rotation);
                Vector2 coreHalfExtents = Vector2.Max(rect.HalfExtents - new Vector2(rect.EffectiveRadius), Vector2.Zero);
                MonoSdfSample sample = RoundedBox(local, coreHalfExtents, rect.EffectiveRadius);
                return new MonoSdfSample(sample.Distance, sample.Gradient.RotatedBy(rect.Rotation));
            }

            /// <summary>圆环（annulus）。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="center">圆心。</param>
            /// <param name="innerRadius">内半径。</param>
            /// <param name="outerRadius">外半径。</param>
            public static MonoSdfSample Annulus(Vector2 point, Vector2 center, float innerRadius, float outerRadius)
            {
                Vector2 offset = point - center;
                float length = offset.Length();
                float distance = MathF.Max(length - outerRadius, innerRadius - length);
                float centerRadius = (innerRadius + outerRadius) / 2f;
                Vector2 gradient = length <= MonoUtil.Epsilon
                    ? Vector2.UnitY
                    : length > centerRadius ? offset / length : length < centerRadius ? -offset / length : Vector2.UnitY;
                return new MonoSdfSample(distance, gradient);
            }

            /// <summary>
            /// 正多边形 / 星形（居中，无旋转）。
            /// <para>
            /// <paramref name="vertexCount"/> 是<b>顶点总数</b>（不是角数）：顶点均布在整圆上，
            /// 相邻顶点的角度间隔为 <c>2π / vertexCount</c>，半径在
            /// <paramref name="circumradius"/> 与 <c>circumradius * innerRatio</c> 之间交替。
            /// </para>
            /// <list type="bullet">
            /// <item><description><paramref name="innerRatio"/> = 1：正 <c>vertexCount</c> 边形（外接圆半径 = circumradius，内切半径 = circumradius·cos(π/vertexCount)）。</description></item>
            /// <item><description><paramref name="innerRatio"/> &lt; 1：<c>vertexCount/2</c> 个角的星形。</description></item>
            /// </list>
            /// </summary>
            /// <param name="point">查询点。</param>
            /// <param name="center">中心。</param>
            /// <param name="circumradius">外接圆半径（顶点到中心的距离）。</param>
            /// <param name="vertexCount">顶点总数，至少 3。</param>
            /// <param name="innerRatio">内顶点半径与外接半径之比，范围 (0,1]。</param>
            /// <param name="rotation">旋转角。</param>
            public static MonoSdfSample Star(Vector2 point, Vector2 center, float circumradius, int vertexCount, float innerRatio = 1f, float rotation = 0f)
            {
                vertexCount = Math.Max(vertexCount, 3);
                innerRatio = MathHelper.Clamp(innerRatio, 0.001f, 1f);

                float step = TwoPi / vertexCount;
                Vector2 local = (point - center).RotatedBy(-rotation);

                // 逐边求「点到线段的距离」，并用射线法判定内外，凹的星形也能给出精确结果。
                float minimum = float.MaxValue;
                Vector2 gradient = Vector2.UnitY;
                bool inside = false;

                for (int i = 0; i < vertexCount; i++)
                {
                    Vector2 a = VertexAt(i);
                    Vector2 b = VertexAt(i + 1);

                    MonoSegment edge = new(a, b);
                    Vector2 closest = edge.ClosestPoint(local);
                    Vector2 offset = local - closest;
                    float distance = offset.Length();
                    if (distance < minimum)
                    {
                        minimum = distance;
                        gradient = distance > MonoUtil.Epsilon ? offset / distance : Vector2.UnitY;
                    }

                    if ((a.Y > local.Y) != (b.Y > local.Y)
                        && local.X < (b.X - a.X) * (local.Y - a.Y) / (b.Y - a.Y) + a.X)
                    {
                        inside = !inside;
                    }
                }
                Vector2 finalGradient = inside ? -gradient : gradient;
                return new MonoSdfSample(inside ? -minimum : minimum, finalGradient.RotatedBy(rotation));

                Vector2 VertexAt(int index)
                {
                    int wrapped = MonoUtil.Mod(index, vertexCount);
                    float radius = wrapped % 2 == 0 ? circumradius : circumradius * innerRatio;
                    return (step * wrapped).ToRotationVector2() * radius;
                }
            }

            /// <summary>椭圆（近似 SDF，参见 <see cref="MonoEllipse.SignedDistanceTo"/>）。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="ellipse">椭圆。</param>
            public static MonoSdfSample Ellipse(Vector2 point, MonoEllipse ellipse)
            {
                Vector2 closest = ellipse.ClosestPoint(point);
                Vector2 offset = point - closest;
                float length = offset.Length();
                float sign = ellipse.Contains(point) ? -1f : 1f;
                return new MonoSdfSample(sign * length, length > MonoUtil.Epsilon ? (sign * offset / length) : Vector2.UnitY);
            }

            /// <summary>凸/凹多边形（自动判定内外）。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="vertices">顶点。</param>
            public static MonoSdfSample Polygon(Vector2 point, ReadOnlySpan<Vector2> vertices)
            {
                if (vertices.Length == 0)
                    return new MonoSdfSample(float.PositiveInfinity, Vector2.UnitY);
                if (vertices.Length == 1)
                    return Point(point, vertices[0]);
                if (vertices.Length == 2)
                    return Segment(point, vertices[0], vertices[1]);

                float minimum = float.MaxValue;
                Vector2 gradient = Vector2.UnitY;
                bool inside = false;

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector2 a = vertices[i];
                    Vector2 b = vertices[(i + 1) % vertices.Length];

                    MonoSdfSample segment = Segment(point, a, b);
                    if (segment.Distance < minimum)
                    {
                        minimum = segment.Distance;
                        gradient = segment.Gradient;
                    }

                    // 射线法累积内外判定。
                    if ((a.Y > point.Y) != (b.Y > point.Y)
                        && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                    {
                        inside = !inside;
                    }
                }

                return new MonoSdfSample(inside ? -minimum : minimum, inside ? -gradient : gradient);
            }

            /// <summary>三角形。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="a">顶点 A。</param>
            /// <param name="b">顶点 B。</param>
            /// <param name="c">顶点 C。</param>
            public static MonoSdfSample Triangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
                => Polygon(point, [a, b, c]);

            /// <summary>圆弧的描边（有厚度的弧）。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="arc">圆弧。</param>
            /// <param name="thickness">描边半厚。</param>
            public static MonoSdfSample Arc(Vector2 point, MonoArc arc, float thickness)
            {
                Vector2 closest = arc.ClosestPoint(point);
                Vector2 offset = point - closest;
                float length = offset.Length();
                int sign = length < thickness ? -1 : 1;
                return new MonoSdfSample(length - thickness, length > MonoUtil.Epsilon ? (sign * offset / length) : Vector2.UnitY);
            }

            /// <summary>半平面 <c>dot(normal, point) - offset</c>。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="normal">平面法线（应为单位矢量）。</param>
            /// <param name="offset">平面偏移。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample HalfPlane(Vector2 point, Vector2 normal, float offset)
                => new(Vector2.Dot(normal, point) - offset, normal);

            /// <summary>用数值差分估计任意距离函数的梯度。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="distanceFunction">距离函数。</param>
            /// <param name="epsilon">差分步长。</param>
            public static Vector2 EstimateGradient(Vector2 point, Func<Vector2, float> distanceFunction, float epsilon = 0.5f)
            {
                if (distanceFunction is null)
                    return Vector2.UnitY;

                float dx = distanceFunction(point + new Vector2(epsilon, 0f)) - distanceFunction(point - new Vector2(epsilon, 0f));
                float dy = distanceFunction(point + new Vector2(0f, epsilon)) - distanceFunction(point - new Vector2(0f, epsilon));
                return MonoUtil.SafeNormalize(new Vector2(dx, dy), Vector2.UnitY);
            }

            /// <summary>
            /// SDF 的布尔运算与平滑并集（smooth-min）。Metaball 的粘连效果就来自
            /// <see cref="SmoothMin(MonoSdfSample, MonoSdfSample, float)"/>。
            /// </summary>
            /// <summary>并集：取更小的距离。</summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample Union(MonoSdfSample a, MonoSdfSample b)
                => a.Distance < b.Distance ? a : b;

            /// <summary>交集：取更大的距离。</summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample Intersect(MonoSdfSample a, MonoSdfSample b)
                => a.Distance > b.Distance ? a : b;

            /// <summary>差集：从 <paramref name="a"/> 中挖掉 <paramref name="b"/>。</summary>
            /// <param name="a">被挖的形状。</param>
            /// <param name="b">挖去的形状。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample Subtract(MonoSdfSample a, MonoSdfSample b)
                => a.Distance > -b.Distance ? a : new MonoSdfSample(-b.Distance, -b.Gradient);

            /// <summary>异或：两个形状的对称差。</summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            public static MonoSdfSample Xor(MonoSdfSample a, MonoSdfSample b)
            {
                /*
                float min1 = MathF.Min(a.Distance, -b.Distance);
                float min2 = MathF.Min(-a.Distance, b.Distance);

                if (min1 > min2)
                    return new MonoSdfSample(min1, a.Distance < -b.Distance ? a.Gradient : -b.Gradient);
                if (min2 > min1)
                    return new MonoSdfSample(min2, -a.Distance < b.Distance ? -a.Gradient : b.Gradient);
                return new MonoSdfSample(min1, a.Gradient + b.Gradient);
                */
                // 差集 A \ B
                float d1 = MathF.Max(a.Distance, -b.Distance);
                Vector2 g1 = a.Distance > -b.Distance ? a.Gradient : -b.Gradient;

                // 差集 B \ A
                float d2 = MathF.Max(-a.Distance, b.Distance);
                Vector2 g2 = -a.Distance > b.Distance ? -a.Gradient : b.Gradient;

                if (d1 < d2)
                    return new MonoSdfSample(d1, g1);
                if (d2 < d1)
                    return new MonoSdfSample(d2, g2);
                return new MonoSdfSample(d1, g1 + g2);
            }

            /// <summary>
            /// 指数型平滑并集。这是最常用的 smin：k 越大越「粘」，k → 0 退化为普通并集。
            /// </summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            /// <param name="k">平滑半径（距离单位）。</param>
            public static MonoSdfSample SmoothMin(MonoSdfSample a, MonoSdfSample b, float k)
            {
                if (k <= MonoUtil.Epsilon)
                    return Union(a, b);

                float ea = MathF.Pow(2f, -a.Distance / k);
                float eb = MathF.Pow(2f, -b.Distance / k);
                float sum = ea + eb;
                float weight = ea / sum;

                return new MonoSdfSample(
                    -k * MathF.Log2(sum),
                    Vector2.Lerp(b.Gradient, a.Gradient, weight));
            }

            /// <summary>指数型平滑并集（显式命名，与 <see cref="SmoothMin"/> 等价）。</summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            /// <param name="k">平滑半径。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MonoSdfSample ExponentialSmoothMin(MonoSdfSample a, MonoSdfSample b, float k) => SmoothMin(a, b, k);

            /// <summary>平方根型平滑并集：比指数型更「圆」，但只会削减不会增厚。</summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            /// <param name="k">平滑半径。</param>
            public static MonoSdfSample RootSmoothMin(MonoSdfSample a, MonoSdfSample b, float k)
            {
                if (k <= MonoUtil.Epsilon)
                    return Union(a, b);

                k *= 2f;
                float x = b.Distance - a.Distance;
                float s = MathF.Sqrt(x * x + k * k);
                float weight = 0.5f * (1f + x / s);
                return new MonoSdfSample(0.5f * (a.Distance + b.Distance - s), Vector2.Lerp(b.Gradient, a.Gradient, weight));
            }

            /// <summary>多项式型平滑并集（Inigo Quilez 的 <c>smin</c>）：最快，形状稍硬。</summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            /// <param name="k">平滑半径。</param>
            public static MonoSdfSample PolynomialSmoothMin(MonoSdfSample a, MonoSdfSample b, float k)
            {
                if (k <= MonoUtil.Epsilon)
                    return Union(a, b);

                float h = MathF.Max(k - MathF.Abs(a.Distance - b.Distance), 0f) / k;
                float m = h * h * k * 0.25f;
                float minimum = MathF.Min(a.Distance, b.Distance) - m;
                float t = a.Distance < b.Distance ? 0.5f * h : 1f - 0.5f * h;
                Vector2 gradient = Vector2.Lerp(a.Gradient, b.Gradient, t);
                return new MonoSdfSample(minimum, gradient);
            }

            /// <summary>三次多项式平滑并集。</summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            /// <param name="k">平滑半径。</param>
            public static MonoSdfSample CubicSmoothMin(MonoSdfSample a, MonoSdfSample b, float k)
            {
                if (k <= MonoUtil.Epsilon)
                    return Union(a, b);

                k *= 6f;
                float d = a.Distance - b.Distance;
                float h = MathF.Max(k - MathF.Abs(d), 0f) / k;
                float m = h * h * h * k * (1f / 6f);
                // 距离更小的一侧权重更高：d<0 表示 a 更近。
                //float weight = d < 0f ? 1f - 0.5f * h * h : 0.5f * h * h;
                float t = d < 0f ? 0.5f * h * h : 1f - 0.5f * h * h;
                return new MonoSdfSample(MathF.Min(a.Distance, b.Distance) - m, Vector2.Lerp(a.Gradient, b.Gradient, t));
            }

            /// <summary>圆形平滑并集：在连接处生成圆弧状的过渡。</summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            /// <param name="k">平滑半径。</param>
            public static MonoSdfSample CircularSmoothMin(MonoSdfSample a, MonoSdfSample b, float k)
            {
                if (k <= MonoUtil.Epsilon)
                    return Union(a, b);

                k *= 1f / (1f - MathF.Sqrt(0.5f));
                float d = a.Distance - b.Distance;
                float h = MathF.Max(k - MathF.Abs(d), 0f) / k;
                float s = MathF.Sqrt(MathF.Max(1f - h * (h - 2f), 0f));
                float m = k * 0.5f * (1f + h - s);
                //float weight = s <= MonoUtil.Epsilon ? 0f : h / s;
                float t = d < 0 ? 0.5f - (1f - h) / (2f * s) : 0.5f + (1f - h) / (2f * s);
                return new MonoSdfSample(MathF.Min(a.Distance, b.Distance) - m, Vector2.Lerp(a.Gradient, b.Gradient, t));
            }

            /// <summary>倒角平滑并集：连接处是直的斜角。</summary>
            /// <param name="a">第一个采样。</param>
            /// <param name="b">第二个采样。</param>
            /// <param name="k">倒角半径。</param>
            public static MonoSdfSample ChamferSmoothMin(MonoSdfSample a, MonoSdfSample b, float k)
            {
                if (k <= MonoUtil.Epsilon)
                    return Union(a, b);

                float d = a.Distance - b.Distance;
                float h = MathF.Max(k - MathF.Abs(d), 0f) / k;
                float m = h * k * 0.5f;
                float weight = d < 0f ? 0.5f * h : 1f - 0.5f * h;
                return new MonoSdfSample(MathF.Min(a.Distance, b.Distance) - m, Vector2.Lerp(a.Gradient, b.Gradient, weight));
            }

            /// <summary>所有形状的合并（取最小）。</summary>
            /// <param name="samples">采样数组。</param>
            public static MonoSdfSample UnionAll(ReadOnlySpan<MonoSdfSample> samples)
            {
                if (samples.Length == 0)
                    return new MonoSdfSample(float.PositiveInfinity, Vector2.UnitY);

                MonoSdfSample result = samples[0];
                for (int i = 1; i < samples.Length; i++)
                    result = Union(result, samples[i]);
                return result;
            }

            /// <summary>所有形状的平滑合并（逐个折叠，开销与形状数成正比）。</summary>
            /// <param name="samples">采样数组。</param>
            /// <param name="k">平滑半径。</param>
            public static MonoSdfSample SmoothUnionAll(ReadOnlySpan<MonoSdfSample> samples, float k)
            {
                if (samples.Length == 0)
                    return new MonoSdfSample(float.PositiveInfinity, Vector2.UnitY);

                MonoSdfSample result = samples[0];
                for (int i = 1; i < samples.Length; i++)
                    result = SmoothMin(result, samples[i], k);
                return result;
            }

            /// <summary>圆角化：把整个形状向内收缩 <paramref name="radius"/>，尖角变为圆角。</summary>
            /// <param name="sample">采样。</param>
            /// <param name="radius">圆角半径。</param>
            public static MonoSdfSample Round(MonoSdfSample sample, float radius) => sample.Offset(radius);

            /// <summary>挖空成壳（onion）：保留距离 |d| &lt; radius 的一层。</summary>
            /// <param name="sample">采样。</param>
            /// <param name="radius">壳的半厚。</param>
            public static MonoSdfSample Onion(MonoSdfSample sample, float radius)
                => new(MathF.Abs(sample.Distance) - radius, sample.Distance < 0f ? -sample.Gradient : sample.Gradient);

            /// <summary>把形状沿某个方向拉伸（返回的是距离函数结果，需在调用前平移查询点）。</summary>
            /// <param name="sample">原采样。</param>
            /// <param name="amount">外扩量。</param>
            public static MonoSdfSample Expand(MonoSdfSample sample, float amount) => sample.Offset(-amount);
        }
    }

    /// <summary>
    /// 一次 SDF 采样：有符号距离 + 梯度（单位方向）。
    /// </summary>
    public readonly struct MonoSdfSample : IEquatable<MonoSdfSample>
    {
        /// <summary>有符号距离（外部为正、内部为负）。</summary>
        public readonly float Distance;

        /// <summary>梯度（单位矢量，指向距离增大的方向；退化时为 <see cref="Vector2.UnitY"/>）。</summary>
        public readonly Vector2 Gradient;

        /// <summary>构造采样。</summary>
        /// <param name="distance">有符号距离。</param>
        /// <param name="gradient">梯度，会被安全归一化。</param>
        public MonoSdfSample(float distance, Vector2 gradient)
        {
            Distance = distance;
            Gradient = gradient.LengthSquared() > MonoUtil.EpsilonSqr ? Vector2.Normalize(gradient) : Vector2.UnitY;
        }

        /// <summary>是否在形状内部。</summary>
        public bool IsInside => Distance < 0f;

        /// <summary>是否在形状表面上（按容差）。</summary>
        /// <param name="tolerance">容差。</param>
        public bool IsOnSurface(float tolerance = MonoUtil.Epsilon) => Math.Abs(Distance) <= tolerance;

        /// <summary>把采样沿梯度外推 <paramref name="amount"/> 得到的近似表面点。</summary>
        /// <param name="point">采样位置。</param>
        /// <param name="amount">外推距离，默认按当前距离推到表面。</param>
        public Vector2 ApproximateSurfacePoint(Vector2 point, float? amount = null)
            => point - Gradient * (amount ?? Distance);

        /// <summary>把采样沿梯度外推 <paramref name="distance"/> 并返回新的采样（等价于对采样做平移）。</summary>
        /// <param name="distance">外推距离。</param>
        public MonoSdfSample Offset(float distance) => new(Distance + distance, Gradient);

        /// <inheritdoc/>
        public bool Equals(MonoSdfSample other) => Distance == other.Distance && Gradient == other.Gradient;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is MonoSdfSample other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Distance, Gradient);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoSdfSample a, MonoSdfSample b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoSdfSample a, MonoSdfSample b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"Sdf[d={Distance:0.###} grad={Gradient}]";
    }
}
