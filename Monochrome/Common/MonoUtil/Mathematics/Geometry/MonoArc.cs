using System.Runtime.CompilerServices;

using static Monochrome.Common.MonoUtil.MonoUtil;

namespace Monochrome.Common.MonoUtil.Mathematics.Geometry
{
    /// <summary>
    /// 二维圆弧：以 <see cref="Center"/> 为圆心、<see cref="Radius"/> 为半径，
    /// 从 <see cref="StartAngle"/> 起逆时针扫过 <see cref="SweepAngle"/>。
    /// <para>
    /// <see cref="SweepAngle"/> 取负即为顺时针。|Sweep| ≥ 2π 时退化为整圆。
    /// </para>
    /// </summary>
    public readonly struct MonoArc : IEquatable<MonoArc>
    {
        /// <summary>圆心。</summary>
        public readonly Vector2 Center;

        /// <summary>半径。</summary>
        public readonly float Radius;

        /// <summary>起始角（弧度）。</summary>
        public readonly float StartAngle;

        /// <summary>张角（弧度，可正可负）。</summary>
        public readonly float SweepAngle;

        /// <summary>构造圆弧。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        /// <param name="startAngle">起始角。</param>
        /// <param name="sweepAngle">张角。</param>
        public MonoArc(Vector2 center, float radius, float startAngle, float sweepAngle)
        {
            Center = center;
            Radius = radius;
            StartAngle = startAngle;
            SweepAngle = sweepAngle;
        }

        /// <summary>以圆心、半径与两个边界角构造（按逆时针取 <c>start → end</c> 的弧）。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        /// <param name="startAngle">起始角。</param>
        /// <param name="endAngle">终止角。</param>
        public static MonoArc FromAngles(Vector2 center, float radius, float startAngle, float endAngle)
            => new(center, radius, startAngle, MonoUtil.WrapAngle(endAngle - startAngle));

        /// <summary>是否为整圆（|张角| ≥ 2π）。</summary>
        public bool IsFullCircle => Math.Abs(SweepAngle) >= TwoPi - MonoUtil.Epsilon;

        /// <summary>终止角。</summary>
        public float EndAngle => StartAngle + SweepAngle;

        /// <summary>起点。</summary>
        public Vector2 StartPoint => Center + StartAngle.ToRotationVector2() * Radius;

        /// <summary>终点。</summary>
        public Vector2 EndPoint => Center + EndAngle.ToRotationVector2() * Radius;

        /// <summary>中点（弧长意义上的中分点）。</summary>
        public Vector2 MidPoint => Center + (StartAngle + SweepAngle * 0.5f).ToRotationVector2() * Radius;

        /// <summary>弧长。</summary>
        public float ArcLength => Math.Abs(SweepAngle) * Radius;

        /// <summary>扇形弦长（起点到终点的直线距离）。</summary>
        public float ChordLength => Vector2.Distance(StartPoint, EndPoint);

        /// <summary>轴对齐包围盒（保守：以圆的包围盒为准，保证不漏）。</summary>
        public Rectangle Bounds
        {
            get
            {
                int size = (int)MathF.Ceiling(Radius * 2f);
                return new Rectangle((int)MathF.Floor(Center.X - Radius), (int)MathF.Floor(Center.Y - Radius), size, size);
            }
        }

        /// <summary>轴上指定参数 <paramref name="t"/>（0→起点，1→终点）处的点，沿角度线性插值。</summary>
        /// <param name="t">参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 PointAt(float t) => Center + (StartAngle + SweepAngle * t).ToRotationVector2() * Radius;

        /// <summary>指定角处的点。</summary>
        /// <param name="angle">角度（弧度）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 PointAtAngle(float angle) => Center + angle.ToRotationVector2() * Radius;

        /// <summary>该角度是否落在弧的扫掠范围内。</summary>
        /// <param name="angle">被检查的角度。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AngleInRange(float angle) => MonoUtil.AngleInSweep(angle, StartAngle, SweepAngle);

        /// <summary>点是否落在弧线上（含端点），按容差判断。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">容差。</param>
        public bool Contains(Vector2 point, float tolerance = MonoUtil.Epsilon)
        {
            if (Math.Abs(Vector2.Distance(point, Center) - Radius) > tolerance)
                return false;
            return AngleInRange((point - Center).ToRotation());
        }

        /// <summary>点是否落在扇形（圆心 + 弧围成的区域）内，含边界。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">容差。</param>
        public bool ContainsSector(Vector2 point, float tolerance = MonoUtil.Epsilon)
        {
            if (Vector2.Distance(point, Center) > Radius + tolerance)
                return false;
            if (Vector2.DistanceSquared(point, Center) <= tolerance * tolerance)
                return true;
            return AngleInRange((point - Center).ToRotation());
        }

        /// <summary>弧线上距离 <paramref name="point"/> 最近的点。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestPoint(Vector2 point)
        {
            Vector2 offset = point - Center;
            if (offset.LengthSquared() <= MonoUtil.EpsilonSqr)
                return StartPoint;

            float angle = offset.ToRotation();
            if (AngleInRange(angle))
                return Center + angle.ToRotationVector2() * Radius;

            Vector2 start = StartPoint;
            Vector2 end = EndPoint;
            return Vector2.DistanceSquared(point, start) <= Vector2.DistanceSquared(point, end) ? start : end;
        }

        /// <summary>点到弧线的最短距离。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point) => Vector2.Distance(point, ClosestPoint(point));

        /// <summary>把弧按最大角步长细分为折线点。</summary>
        /// <param name="segments">分段数，至少为 1。</param>
        public Vector2[] ToPolyline(int segments)
        {
            segments = Math.Max(segments, 1);
            Vector2[] points = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
                points[i] = PointAt(i / (float)segments);
            return points;
        }

        /// <summary>按目标弦高误差估算合适的分段数——绘制时避免弧看起来是折线。</summary>
        /// <param name="maxError">允许的最大弦高误差（像素），越小越圆滑。</param>
        public int SegmentCountForError(float maxError)
        {
            if (maxError <= MonoUtil.Epsilon || Radius <= MonoUtil.Epsilon)
                return 1;
            float ratio = MathHelper.Clamp(1f - maxError / Radius, -1f, 1f);
            float maxStep = 2f * MathF.Acos(ratio);
            if (maxStep <= MonoUtil.Epsilon)
                return 1;
            return Math.Max(1, (int)MathF.Ceiling(Math.Abs(SweepAngle) / maxStep));
        }

        /// <summary>转为 SDF 采样（外部为正、内部为负）。</summary>
        /// <param name="point">查询点。</param>
        public float SignedDistanceTo(Vector2 point) => Vector2.Distance(point, ClosestPoint(point));

        /// <inheritdoc/>
        public bool Equals(MonoArc other)
            => Center == other.Center && Radius == other.Radius && StartAngle == other.StartAngle && SweepAngle == other.SweepAngle;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is MonoArc other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Center, Radius, StartAngle, SweepAngle);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoArc a, MonoArc b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoArc a, MonoArc b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"Arc[{Center} r={Radius} start={StartAngle} sweep={SweepAngle}]";
    }
}
