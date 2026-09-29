using Monochrome.Common.MonoUtil.Mathematics.Geometry;

namespace Monochrome.Common.MonoUtil
{
    /// <summary>
    /// 曲线：二次/三次贝塞尔、Catmull-Rom、Hermite、B 样条，全部带弧长参数化。
    /// </summary>
    public static partial class MonoUtil
    {
        /// <summary>
        /// 曲线工厂与通用工具。
        /// </summary>
        /// <summary>三次 Hermite 基函数求值（Berry 形式）。</summary>
        /// <param name="p0">起点。</param>
        /// <param name="m0">起点切线。</param>
        /// <param name="p1">终点。</param>
        /// <param name="m1">终点切线。</param>
        /// <param name="t">局部参数。</param>
        public static Vector2 CubicHermite(Vector2 p0, Vector2 m0, Vector2 p1, Vector2 m1, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return p0 * (2f * t3 - 3f * t2 + 1f)
                 + m0 * (t3 - 2f * t2 + t)
                 + p1 * (-2f * t3 + 3f * t2)
                 + m1 * (t3 - t2);
        }

        /// <summary>把一条折线（路点序列）拟合成 Catmull-Rom 样条。</summary>
        /// <param name="polyline">路点。</param>
        public static MonoCurve ThroughPoints(Vector2[] polyline) => new MonoCatmullRom(polyline, false);

        /// <summary>用「起点、终点、弯曲强度」快速构造一条自然弧线。</summary>
        /// <param name="start">起点。</param>
        /// <param name="end">终点。</param>
        /// <param name="bulge">弯曲强度，正负决定弯曲方向，绝对值约为曲线中点相对弦的最大偏离距离。</param>
        public static MonoCurve ArcBetween(Vector2 start, Vector2 end, float bulge)
        {
            float length = Vector2.Distance(start, end);
            Vector2 direction = MonoUtil.SafeNormalize(end - start, Vector2.UnitX);
            Vector2 normal = MonoUtil.Perpendicular(direction);
            Vector2 mid = (start + end) * 0.5f + normal * bulge;

            // 二次贝塞尔在 t=0.5 处经过 (p0 + 2*p1 + p2)/4，反解出控制点。
            Vector2 control = mid * 2f - (start + end) * 0.5f;

            // 弯曲量为 0 时退化为直线，直接返回由两点定义的样条以免出现数值抖动。
            if (Math.Abs(bulge) <= MonoUtil.Epsilon || length <= MonoUtil.Epsilon)
                return new MonoCatmullRom(new[] { start, end }, false);

            return new MonoQuadraticBezier(start, control, end);
        }
    }

    /// <summary>
    /// 参数曲线基类。
    /// <para>
    /// <b>弧长参数化是必须的</b>：直接对 <c>t</c> 均匀取值会让沿曲线运动的物体忽快忽慢
    /// （贝塞尔曲线在两端的「速度」远小于中段）。
    /// <see cref="PointAtDistance"/> / <see cref="EvenSamples"/> 内部使用一张按弧长建立的查找表，
    /// 在构造时一次性烘焙，之后查询为 O(1)（表内二分）。
    /// </para>
    /// </summary>
    public abstract class MonoCurve
    {
        private float[] _arcLengthTable;
        private int _arcLengthSteps;
        private float _totalLength;

        /// <summary>控制点数量。</summary>
        public abstract int ControlPointCount { get; }

        /// <summary>取第 <paramref name="index"/> 个控制点。</summary>
        /// <param name="index">控制点索引。</param>
        public abstract Vector2 this[int index] { get; }

        /// <summary>弧长查找表的默认精度（分段数）。越大越准，内存为 <c>(steps+1)</c> 个 float。</summary>
        public const int DefaultArcLengthSteps = 64;

        /// <summary>默认用于求切线/二分搜索的微小参数增量。</summary>
        protected const float ParamDelta = 1e-3f;

        /// <summary>求曲线上归一化参数 <paramref name="t"/> 处的点（<c>t</c> 会被截断到 [0,1]）。</summary>
        /// <param name="t">归一化参数。</param>
        public Vector2 Evaluate(float t) => PointAt(MathHelper.Clamp(t, 0f, 1f));

        /// <summary>求曲线上归一化参数 <paramref name="t"/> 处的点（不截断，用于外插研究）。</summary>
        /// <param name="t">归一化参数。</param>
        public abstract Vector2 PointAt(float t);

        /// <summary>求曲线上归一化参数 <paramref name="t"/> 处的单位切矢量。</summary>
        /// <param name="t">归一化参数。</param>
        public virtual Vector2 TangentAt(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            float delta = Math.Min(ParamDelta, 0.5f);
            float a = Math.Max(t - delta, 0f);
            float b = Math.Min(t + delta, 1f);
            if (b - a <= MonoUtil.Epsilon)
                return Vector2.UnitX;
            return MonoUtil.SafeNormalize(PointAt(b) - PointAt(a), Vector2.UnitX);
        }

        /// <summary>求曲线上归一化参数 <paramref name="t"/> 处的单位法线（切矢量逆时针 90°）。</summary>
        /// <param name="t">归一化参数。</param>
        public Vector2 NormalAt(float t) => MonoUtil.Perpendicular(TangentAt(t));

        /// <summary>求曲线上归一化参数 <paramref name="t"/> 处的曲率（1/半径），直线段为 0。</summary>
        /// <param name="t">归一化参数。</param>
        public float CurvatureAt(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            float delta = 0.01f;
            float a = Math.Max(t - delta, 0f);
            float b = Math.Min(t + delta, 1f);
            if (b - a <= MonoUtil.Epsilon)
                return 0f;

            Vector2 pa = PointAt(a);
            Vector2 pb = PointAt(b);
            Vector2 pt = PointAt(t);
            Vector2 first = (pb - pa) / (b - a);
            float h1 = t - a;
            float h2 = b - t;
            Vector2 second = 2f * ((pb - pt) / h2 - (pt - pa) / h1) / (h1 + h2);
            float speedSquared = first.LengthSquared();
            if (speedSquared <= MonoUtil.EpsilonSqr)
                return 0f;
            return MonoUtil.Cross(first, second) / MathF.Pow(speedSquared, 1.5f);
        }

        /// <summary>曲线总弧长（近似，精度由 <see cref="EnsureArcLengthTable"/> 的步数决定）。</summary>
        public float TotalLength
        {
            get
            {
                EnsureArcLengthTable(DefaultArcLengthSteps);
                return _totalLength;
            }
        }

        /// <summary>确保弧长查找表已按给定步数烘焙。步数变化时会重建。</summary>
        /// <param name="steps">分段数。</param>
        public void EnsureArcLengthTable(int steps = DefaultArcLengthSteps)
        {
            steps = Math.Max(steps, 2);
            if (_arcLengthTable is not null && _arcLengthSteps == steps)
                return;

            _arcLengthTable = new float[steps + 1];
            Vector2 previous = PointAt(0f);
            _arcLengthTable[0] = 0f;
            float accumulated = 0f;
            for (int i = 1; i <= steps; i++)
            {
                Vector2 current = PointAt(i / (float)steps);
                accumulated += Vector2.Distance(previous, current);
                _arcLengthTable[i] = accumulated;
                previous = current;
            }

            _totalLength = accumulated;
            _arcLengthSteps = steps;
        }

        /// <summary>把归一化参数 <paramref name="t"/> 换算为已走过的弧长。</summary>
        /// <param name="t">归一化参数。</param>
        public float ArcLengthAt(float t)
        {
            EnsureArcLengthTable();
            t = MathHelper.Clamp(t, 0f, 1f);
            if (_totalLength <= MonoUtil.Epsilon)
                return 0f;

            float position = t * _arcLengthSteps;
            int index = (int)position;
            if (index >= _arcLengthSteps)
                return _totalLength;

            float fraction = position - index;
            return MathHelper.Lerp(_arcLengthTable[index], _arcLengthTable[index + 1], fraction);
        }

        /// <summary>把已走过的弧长换算回归一化参数（O(log n) 二分 + 线性内插）。</summary>
        /// <param name="distance">弧长。</param>
        public float ParameterAtDistance(float distance)
        {
            EnsureArcLengthTable();
            if (_totalLength <= MonoUtil.Epsilon)
                return 0f;

            distance = MathHelper.Clamp(distance, 0f, _totalLength);

            int low = 0;
            int high = _arcLengthSteps;
            while (low < high)
            {
                int mid = (low + high) >> 1;
                if (_arcLengthTable[mid] < distance)
                    low = mid + 1;
                else
                    high = mid;
            }

            if (low == 0)
                return 0f;

            float segmentStart = _arcLengthTable[low - 1];
            float segmentLength = _arcLengthTable[low] - segmentStart;
            float fraction = segmentLength <= MonoUtil.Epsilon ? 0f : (distance - segmentStart) / segmentLength;
            return (low - 1 + fraction) / _arcLengthSteps;
        }

        /// <summary>按弧长取点：<paramref name="distance"/> 为从起点沿曲线走过的距离。</summary>
        /// <param name="distance">弧长。</param>
        public Vector2 PointAtDistance(float distance) => PointAt(ParameterAtDistance(distance));

        /// <summary>按弧长取切矢量。</summary>
        /// <param name="distance">弧长。</param>
        public Vector2 TangentAtDistance(float distance) => TangentAt(ParameterAtDistance(distance));

        /// <summary>
        /// 以匀速生成 <paramref name="count"/> 个采样点（沿弧长均匀）。
        /// 这是「让弹幕沿曲线匀速飞行」的正确姿势。
        /// </summary>
        /// <param name="count">采样点数，至少 2。</param>
        public Vector2[] EvenSamples(int count)
        {
            if (count < 2)
                return [];
            EnsureArcLengthTable();

            Vector2[] points = new Vector2[count];
            for (int i = 0; i < count; i++)
                points[i] = PointAtDistance(_totalLength * i / (count - 1));
            return points;
        }

        /// <summary>把曲线按归一化参数细分为折线点。</summary>
        /// <param name="segments">分段数，至少 1。</param>
        public Vector2[] ToPolyline(int segments)
        {
            segments = Math.Max(segments, 1);
            Vector2[] points = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
                points[i] = PointAt(i / (float)segments);
            return points;
        }

        /// <summary>
        /// 按最大弦高误差估算合适的折线分段数，用于绘制时既圆滑又不浪费顶点。
        /// </summary>
        /// <param name="maxError">允许的最大弦高误差（像素）。</param>
        /// <param name="maxSegments">分段数上限，避免极端情况下爆炸。</param>
        public int SegmentCountForError(float maxError, int maxSegments = 128)
        {
            if (maxError <= MonoUtil.Epsilon)
                return 1;

            EnsureArcLengthTable();
            if (_totalLength <= MonoUtil.Epsilon)
                return 1;

            // 用离散曲率估计：半径 ≈ 1/|曲率|，沿弧长需要的角度步长由弦高误差决定。
            const int probeCount = 16;
            float worstCurvature = 0f;
            for (int i = 0; i <= probeCount; i++)
                worstCurvature = Math.Max(worstCurvature, Math.Abs(CurvatureAt(i / (float)probeCount)));

            if (worstCurvature <= MonoUtil.Epsilon)
                return Math.Max(1, Math.Min(maxSegments, (int)MathF.Ceiling(_totalLength / Math.Max(maxError * 8f, 1f))));

            float radius = 1f / worstCurvature;
            float ratio = MathHelper.Clamp(1f - maxError / radius, -1f, 1f);
            float maxAngleStep = 2f * MathF.Acos(ratio);
            if (maxAngleStep <= MonoUtil.Epsilon)
                return maxSegments;

            float totalAngle = 0f;
            for (int i = 1; i <= probeCount; i++)
                totalAngle += Math.Abs(MonoUtil.AngleDifference(TangentAt((i - 1) / (float)probeCount).ToRotation(), TangentAt(i / (float)probeCount).ToRotation()));

            int count = (int)MathF.Ceiling(totalAngle / maxAngleStep);
            return Math.Clamp(count, 1, maxSegments);
        }

        /// <summary>
        /// 在曲线上按弧长等距放置标签/挂点时的间隔计算：返回 <paramref name="spacing"/> 对应的段数。
        /// </summary>
        /// <param name="spacing">期望间距（像素）。</param>
        public int SegmentCountForSpacing(float spacing)
        {
            if (spacing <= MonoUtil.Epsilon)
                return 1;
            EnsureArcLengthTable();
            return Math.Max(1, (int)MathF.Ceiling(_totalLength / spacing));
        }

        /// <summary>把曲线按弧长拆成若干等长段，返回每段的端点。</summary>
        /// <param name="segmentCount">段数。</param>
        public MonoSegment[] ToSegments(int segmentCount)
        {
            if (segmentCount < 1)
                return [];

            EnsureArcLengthTable();
            MonoSegment[] segments = new MonoSegment[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                float from = _totalLength * i / segmentCount;
                float to = _totalLength * (i + 1) / segmentCount;
                segments[i] = new MonoSegment(PointAtDistance(from), PointAtDistance(to));
            }
            return segments;
        }

        /// <summary>
        /// 求距离 <paramref name="point"/> 最近的曲线点，返回该点与其归一化参数。
        /// 采用「粗采样 + 局部二分细化」，采样数越大越不易漏掉多个局部极小。
        /// </summary>
        /// <param name="point">查询点。</param>
        /// <param name="samples">粗采样数，至少 4。</param>
        /// <param name="t">最近点对应的归一化参数。</param>
        public Vector2 ClosestPoint(Vector2 point, out float t, int samples = 32)
        {
            samples = Math.Max(samples, 4);
            float bestT = 0f;
            float bestDistance = float.MaxValue;

            Vector2 previous = PointAt(0f);
            for (int i = 1; i <= samples; i++)
            {
                float current = i / (float)samples;
                Vector2 currentPoint = PointAt(current);
                MonoSegment probe = new(previous, currentPoint);
                float candidate = probe.ProjectionParameter(point);
                float candidateT = (i - 1 + MathHelper.Clamp(candidate, 0f, 1f)) / samples;
                float distance = Vector2.DistanceSquared(point, PointAt(candidateT));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestT = candidateT;
                }
                previous = currentPoint;
            }

            // 局部细化的二分搜索。
            float span = 1f / samples;
            float low = Math.Max(bestT - span, 0f);
            float high = Math.Min(bestT + span, 1f);
            for (int iteration = 0; iteration < 24; iteration++)
            {
                float midLow = low + (high - low) / 3f;
                float midHigh = high - (high - low) / 3f;
                if (Vector2.DistanceSquared(point, PointAt(midLow)) <= Vector2.DistanceSquared(point, PointAt(midHigh)))
                    high = midHigh;
                else
                    low = midLow;
            }

            t = (low + high) * 0.5f;
            return PointAt(t);
        }

        /// <summary>点到曲线的最短距离。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="samples">粗采样数。</param>
        public float DistanceTo(Vector2 point, int samples = 32) => Vector2.Distance(point, ClosestPoint(point, out _, samples));

        /// <summary>曲线的轴对齐包围盒（按采样点求，覆盖控制点）。</summary>
        public MonoAABB FloatBounds
        {
            get
            {
                Vector2 min = this[0];
                Vector2 max = this[0];
                for (int i = 1; i < ControlPointCount; i++)
                {
                    min = Vector2.Min(min, this[i]);
                    max = Vector2.Max(max, this[i]);
                }

                EnsureArcLengthTable();
                for (int i = 0; i <= _arcLengthSteps; i++)
                {
                    Vector2 sample = PointAt(i / (float)_arcLengthSteps);
                    min = Vector2.Min(min, sample);
                    max = Vector2.Max(max, sample);
                }

                return new MonoAABB(min, max);
            }
        }

        /// <summary>整数包围盒。</summary>
        public Rectangle Bounds => FloatBounds.ToRectangle();

        /// <summary>曲线的多边形近似。</summary>
        /// <param name="segments">分段数。</param>
        public MonoPolygon ToPolygon(int segments) => new(ToPolyline(segments), false);

        /// <summary>控制点副本。</summary>
        protected abstract Vector2[] CopyControlPoints();

        /// <summary>把曲线烘焙成折线并交给回调（零 GC 的遍历接口）。</summary>
        /// <param name="segments">分段数。</param>
        /// <param name="action">接收每一小段线段。</param>
        public void ForEachSegment(int segments, Action<MonoSegment> action)
        {
            if (action is null)
                return;

            segments = Math.Max(segments, 1);
            Vector2 previous = PointAt(0f);
            for (int i = 1; i <= segments; i++)
            {
                Vector2 current = PointAt(i / (float)segments);
                action(new MonoSegment(previous, current));
                previous = current;
            }
        }

        /// <summary>把曲线按弧长匀速细分为折线（与 <see cref="ToPolyline"/> 不同，此方法保证每段等长）。</summary>
        /// <param name="count">点数，至少 2。</param>
        public MonoPolygon ToEvenPolygon(int count) => new(EvenSamples(count), false);
    }

    /// <summary>
    /// 二次贝塞尔曲线：3 个控制点。
    /// </summary>
    public sealed class MonoQuadraticBezier : MonoCurve
    {
        private readonly Vector2 _p0;
        private readonly Vector2 _p1;
        private readonly Vector2 _p2;

        /// <summary>构造二次贝塞尔曲线。</summary>
        /// <param name="p0">起点。</param>
        /// <param name="p1">控制点。</param>
        /// <param name="p2">终点。</param>
        public MonoQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            _p0 = p0;
            _p1 = p1;
            _p2 = p2;
        }

        /// <inheritdoc/>
        public override int ControlPointCount => 3;

        /// <inheritdoc/>
        public override Vector2 this[int index] => MonoUtil.Mod(index, 3) switch
        {
            0 => _p0,
            1 => _p1,
            _ => _p2,
        };

        /// <inheritdoc/>
        public override Vector2 PointAt(float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * _p0 + 2f * inverse * t * _p1 + t * t * _p2;
        }

        /// <inheritdoc/>
        public override Vector2 TangentAt(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            Vector2 derivative = 2f * (1f - t) * (_p1 - _p0) + 2f * t * (_p2 - _p1);
            return MonoUtil.SafeNormalize(derivative, MonoUtil.SafeNormalize(_p2 - _p0, Vector2.UnitX));
        }

        /// <inheritdoc/>
        protected override Vector2[] CopyControlPoints() => new[] { _p0, _p1, _p2 };
    }

    /// <summary>
    /// 三次贝塞尔曲线：4 个控制点。最常用的曲线类型。
    /// </summary>
    public sealed class MonoCubicBezier : MonoCurve
    {
        private readonly Vector2 _p0;
        private readonly Vector2 _p1;
        private readonly Vector2 _p2;
        private readonly Vector2 _p3;

        /// <summary>构造三次贝塞尔曲线。</summary>
        /// <param name="p0">起点。</param>
        /// <param name="p1">控制点 1。</param>
        /// <param name="p2">控制点 2。</param>
        /// <param name="p3">终点。</param>
        public MonoCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            _p0 = p0;
            _p1 = p1;
            _p2 = p2;
            _p3 = p3;
        }

        /// <summary>以「起点 + 两个手柄 + 终点」构造。</summary>
        /// <param name="start">起点。</param>
        /// <param name="handle1">起点侧手柄（绝对坐标）。</param>
        /// <param name="handle2">终点侧手柄（绝对坐标）。</param>
        /// <param name="end">终点。</param>
        public static MonoCubicBezier FromHandles(Vector2 start, Vector2 handle1, Vector2 handle2, Vector2 end)
            => new(start, handle1, handle2, end);

        /// <summary>以「起点 + 起点方向/长度 + 终点方向/长度 + 终点」构造，专为弹幕路径设计。</summary>
        /// <param name="start">起点。</param>
        /// <param name="startDirection">起点出发方向。</param>
        /// <param name="startHandleLength">起点手柄长度。</param>
        /// <param name="end">终点。</param>
        /// <param name="endDirection">进入终点时的方向。</param>
        /// <param name="endHandleLength">终点手柄长度。</param>
        public static MonoCubicBezier FromDirections(Vector2 start, Vector2 startDirection, float startHandleLength, Vector2 end, Vector2 endDirection, float endHandleLength)
            => new(
                start,
                start + MonoUtil.SafeNormalize(startDirection, Vector2.UnitX) * startHandleLength,
                end - MonoUtil.SafeNormalize(endDirection, Vector2.UnitX) * endHandleLength,
                end);

        /// <inheritdoc/>
        public override int ControlPointCount => 4;

        /// <inheritdoc/>
        public override Vector2 this[int index] => MonoUtil.Mod(index, 4) switch
        {
            0 => _p0,
            1 => _p1,
            2 => _p2,
            _ => _p3,
        };

        /// <inheritdoc/>
        public override Vector2 PointAt(float t)
        {
            float inverse = 1f - t;
            float inverse2 = inverse * inverse;
            float t2 = t * t;
            return inverse2 * inverse * _p0
                 + 3f * inverse2 * t * _p1
                 + 3f * inverse * t2 * _p2
                 + t2 * t * _p3;
        }

        /// <inheritdoc/>
        public override Vector2 TangentAt(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            float inverse = 1f - t;
            Vector2 derivative = 3f * inverse * inverse * (_p1 - _p0)
                               + 6f * inverse * t * (_p2 - _p1)
                               + 3f * t * t * (_p3 - _p2);
            if (derivative.LengthSquared() <= MonoUtil.EpsilonSqr)
                return MonoUtil.SafeNormalize(_p3 - _p0, Vector2.UnitX);
            return MonoUtil.SafeNormalize(derivative, Vector2.UnitX);
        }

        /// <summary>拆成两段三次贝塞尔（de Casteljau 在 <paramref name="t"/> 处切分）。常用于「曲线命中后分裂」。</summary>
        /// <param name="t">切分参数。</param>
        /// <returns>切分后的两段曲线。</returns>
        public (MonoCubicBezier Left, MonoCubicBezier Right) Split(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);

            Vector2 a = Vector2.Lerp(_p0, _p1, t);
            Vector2 b = Vector2.Lerp(_p1, _p2, t);
            Vector2 c = Vector2.Lerp(_p2, _p3, t);
            Vector2 d = Vector2.Lerp(a, b, t);
            Vector2 e = Vector2.Lerp(b, c, t);
            Vector2 f = Vector2.Lerp(d, e, t);

            return (new MonoCubicBezier(_p0, a, d, f), new MonoCubicBezier(f, e, c, _p3));
        }

        /// <summary>求曲线在 <paramref name="t"/> 处的曲率半径（曲率为 0 时返回 <see cref="float.PositiveInfinity"/>）。</summary>
        /// <param name="t">归一化参数。</param>
        public float RadiusOfCurvatureAt(float t)
        {
            float curvature = Math.Abs(CurvatureAt(t));
            return curvature <= MonoUtil.Epsilon ? MonoUtil.Infinity : 1f / curvature;
        }

        /// <inheritdoc/>
        protected override Vector2[] CopyControlPoints() => new[] { _p0, _p1, _p2, _p3 };
    }

    /// <summary>
    /// Catmull-Rom 样条：穿过所有控制点，是「给定一串路点，生成平滑路径」的首选。
    /// </summary>
    public sealed class MonoCatmullRom : MonoCurve
    {
        private readonly Vector2[] _points;

        /// <summary>构造 Catmull-Rom 样条。</summary>
        /// <param name="points">路点，至少 2 个；少于 2 个时按单点退化。</param>
        /// <param name="copy">是否复制输入数组。</param>
        public MonoCatmullRom(Vector2[] points, bool copy = true)
        {
            if (points is null || points.Length == 0)
            {
                _points = [Vector2.Zero];
                return;
            }

            _points = copy ? (Vector2[])points.Clone() : points;
        }

        /// <inheritdoc/>
        public override int ControlPointCount => _points.Length;

        /// <inheritdoc/>
        public override Vector2 this[int index] => _points[MonoUtil.Mod(index, _points.Length)];

        /// <summary>路点数量。</summary>
        public int PointCount => _points.Length;

        /// <inheritdoc/>
        public override Vector2 PointAt(float t)
        {
            if (_points.Length == 1)
                return _points[0];
            if (_points.Length == 2)
                return Vector2.Lerp(_points[0], _points[1], t);

            float scaled = t * (_points.Length - 1);
            int index = Math.Clamp((int)MathF.Floor(scaled), 0, _points.Length - 2);
            float local = MathHelper.Clamp(scaled - index, 0f, 1f);

            Vector2 p0 = _points[Math.Max(index - 1, 0)];
            Vector2 p1 = _points[index];
            Vector2 p2 = _points[index + 1];
            Vector2 p3 = _points[Math.Min(index + 2, _points.Length - 1)];

            return MonoUtil.CubicHermite(p1, (p2 - p0) * 0.5f, p2, (p3 - p1) * 0.5f, local);
        }

        /// <inheritdoc/>
        protected override Vector2[] CopyControlPoints() => (Vector2[])_points.Clone();
    }

    /// <summary>
    /// 三次 Hermite 样条：由「点 + 切线」序列定义。
    /// </summary>
    public sealed class MonoHermiteSpline : MonoCurve
    {
        private readonly Vector2[] _points;
        private readonly Vector2[] _tangents;

        /// <summary>构造 Hermite 样条。</summary>
        /// <param name="points">路点，至少 2 个。</param>
        /// <param name="tangents">与路点一一对应的切线（未归一化即可，长度影响插值形状）。</param>
        /// <param name="copy">是否复制输入数组。</param>
        public MonoHermiteSpline(Vector2[] points, Vector2[] tangents, bool copy = true)
        {
            if (points is null || points.Length == 0)
            {
                _points = [Vector2.Zero];
                _tangents = [Vector2.Zero];
                return;
            }

            _points = copy ? (Vector2[])points.Clone() : points;
            if (tangents is null || tangents.Length != _points.Length)
            {
                // 切线缺失时退化为 Catmull-Rom 的有限差分切线。
                _tangents = new Vector2[_points.Length];
                for (int i = 0; i < _points.Length; i++)
                {
                    Vector2 previous = _points[Math.Max(i - 1, 0)];
                    Vector2 next = _points[Math.Min(i + 1, _points.Length - 1)];
                    _tangents[i] = (next - previous) * 0.5f;
                }
            }
            else
            {
                _tangents = copy ? (Vector2[])tangents.Clone() : tangents;
            }
        }

        /// <inheritdoc/>
        public override int ControlPointCount => _points.Length;

        /// <inheritdoc/>
        public override Vector2 this[int index] => _points[MonoUtil.Mod(index, _points.Length)];

        /// <summary>取第 <paramref name="index"/> 个切线。</summary>
        /// <param name="index">索引（会取模）。</param>
        public Vector2 Tangent(int index) => _tangents[MonoUtil.Mod(index, _tangents.Length)];

        /// <summary>按弦长的自然参数化（切线自动按段缩放），只需提供路点。</summary>
        /// <param name="points">路点。</param>
        /// <param name="copy">是否复制。</param>
        public static MonoHermiteSpline FromPoints(Vector2[] points, bool copy = true)
            => new(points, null, copy);

        /// <inheritdoc/>
        public override Vector2 PointAt(float t)
        {
            if (_points.Length == 1)
                return _points[0];
            if (_points.Length == 2)
                return Vector2.Lerp(_points[0], _points[1], t);

            float scaled = t * (_points.Length - 1);
            int index = Math.Clamp((int)MathF.Floor(scaled), 0, _points.Length - 2);
            float local = MathHelper.Clamp(scaled - index, 0f, 1f);
            return MonoUtil.CubicHermite(_points[index], _tangents[index], _points[index + 1], _tangents[index + 1], local);
        }

        /// <inheritdoc/>
        protected override Vector2[] CopyControlPoints() => (Vector2[])_points.Clone();
    }

    /// <summary>
    /// 均匀三次 B 样条（clamped）：曲线被控制点包围但一般不过点，端点因重复节点而穿过首尾。
    /// 适合「用控制点围出一个平滑形状」而不是「经过路点」。
    /// </summary>
    public sealed class MonoBSpline : MonoCurve
    {
        private readonly Vector2[] _points;

        /// <summary>构造 clamped 均匀三次 B 样条。</summary>
        /// <param name="points">控制点，至少 2 个。</param>
        /// <param name="copy">是否复制输入数组。</param>
        public MonoBSpline(Vector2[] points, bool copy = true)
        {
            if (points is null || points.Length == 0)
            {
                _points = [Vector2.Zero];
                return;
            }

            _points = copy ? (Vector2[])points.Clone() : points;
        }

        /// <inheritdoc/>
        public override int ControlPointCount => _points.Length;

        /// <inheritdoc/>
        public override Vector2 this[int index] => _points[MonoUtil.Mod(index, _points.Length)];

        /// <summary>
        /// 求曲线上归一化参数 <paramref name="t"/> 处的点。
        /// <para>
        /// 使用 clamped 均匀三次节点向量：前 4 个节点为 0，末 4 个节点为 <c>count-2</c>，
        /// 中间 <c>count-3</c> 个节点依次递增。节点数组长度为 <c>count+5</c>
        /// （比控制点数多 4，且因端点重复而再多 1），这一点是本实现唯一容易写崩的地方。
        /// </para>
        /// </summary>
        /// <param name="t">归一化参数。</param>
        public override Vector2 PointAt(float t)
        {
            int count = _points.Length;
            if (count == 1)
                return _points[0];
            if (count == 2)
                return Vector2.Lerp(_points[0], _points[1], MathHelper.Clamp(t, 0f, 1f));

            t = MathHelper.Clamp(t, 0f, 1f);

            // 控制点少于 4 个时，clamped 三次 B 样条退化为一条低次贝塞尔曲线。
            if (count == 3)
            {
                float inverse = 1f - t;
                return inverse * inverse * _points[0] + 2f * inverse * t * _points[1] + t * t * _points[2];
            }

            // clamped 均匀三次节点向量：节点总数为「控制点数 + 次数 + 1」= count + 4。
            // 前 4 个与末 4 个节点重复，中间 count-4 个内部节点均匀分布在 (0, maxKnot) 上。
            int internalKnots = count - 4;
            float lastKnot = internalKnots + 1f;
            float[] knots = new float[count + 4];

            for (int i = 0; i < 4; i++)
            {
                knots[i] = 0f;
                knots[internalKnots + 4 + i] = lastKnot;
            }
            for (int i = 0; i < internalKnots; i++)
                knots[4 + i] = i + 1f;

            float u = t * lastKnot;
            if (t >= 1f)
                u = lastKnot;

            // 有效的节点区间是 [3, count-1]（三次样条 span ∈ [p, n-1]）。
            // span = count-1 时 knot 下标最高到 count+2，正好等于 knots.Length-1。
            int lastSpan = count - 1;
            int span = 3;
            for (int i = 3; i < lastSpan; i++)
            {
                if (u >= knots[i] && u < knots[i + 1])
                {
                    span = i;
                    break;
                }
            }

            if (u >= knots[lastSpan])
                span = lastSpan;
            else if (u <= knots[3])
                span = 3;

            span = Math.Clamp(span, 3, lastSpan);

            // de Boor 递推（三次，4 个局部控制点）。
            Vector2 d0 = _points[span - 3];
            Vector2 d1 = _points[span - 2];
            Vector2 d2 = _points[span - 1];
            Vector2 d3 = _points[span];

            float a1 = SafeRatio(u - knots[span - 2], knots[span + 1] - knots[span - 2]);
            float a2 = SafeRatio(u - knots[span - 1], knots[span + 2] - knots[span - 1]);
            float a3 = SafeRatio(u - knots[span], knots[span + 3] - knots[span]);

            Vector2 e0 = Vector2.Lerp(d0, d1, a1);
            Vector2 e1 = Vector2.Lerp(d1, d2, a2);
            Vector2 e2 = Vector2.Lerp(d2, d3, a3);

            float b1 = SafeRatio(u - knots[span - 1], knots[span + 1] - knots[span - 1]);
            float b2 = SafeRatio(u - knots[span], knots[span + 2] - knots[span]);

            Vector2 f0 = Vector2.Lerp(e0, e1, b1);
            Vector2 f1 = Vector2.Lerp(e1, e2, b2);

            return Vector2.Lerp(f0, f1, SafeRatio(u - knots[span], knots[span + 1] - knots[span]));
        }

        private static float SafeRatio(float numerator, float denominator)
            => Math.Abs(denominator) <= MonoUtil.Epsilon ? 0f : numerator / denominator;

        /// <inheritdoc/>
        protected override Vector2[] CopyControlPoints() => (Vector2[])_points.Clone();
    }
}
