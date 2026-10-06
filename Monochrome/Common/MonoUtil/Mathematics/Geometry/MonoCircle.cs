using System.Runtime.CompilerServices;

using static Monochrome.Common.MonoUtil.MonoUtil;

namespace Monochrome.Common.MonoUtil.Mathematics.Geometry
{
    /// <summary>
    /// 二维圆。用作判定圈、SDF 图元、以及各类「范围」查询。
    /// </summary>
    public readonly struct MonoCircle : IEquatable<MonoCircle>
    {
        /// <summary>圆心。</summary>
        public readonly Vector2 Center;

        /// <summary>半径。</summary>
        public readonly float Radius;

        /// <summary>以圆心与半径构造。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        public MonoCircle(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        /// <summary>以直径两端点构造（中点即圆心）。</summary>
        /// <param name="a">直径端点 A。</param>
        /// <param name="b">直径端点 B。</param>
        public static MonoCircle FromDiameter(Vector2 a, Vector2 b) => new((a + b) * 0.5f, Vector2.Distance(a, b) * 0.5f);

        /// <summary>直径。</summary>
        public float Diameter => Radius * 2f;

        /// <summary>面积。</summary>
        public float Area => Pi * Radius * Radius;

        /// <summary>周长。</summary>
        public float Circumference => TwoPi * Radius;

        /// <summary>轴对齐包围盒（向外取整）。</summary>
        public Rectangle Bounds
        {
            get
            {
                int size = (int)MathF.Ceiling(Radius * 2f);
                return new Rectangle((int)MathF.Floor(Center.X - Radius), (int)MathF.Floor(Center.Y - Radius), size, size);
            }
        }

        /// <summary>最小外接正方形（浮点，不取整）。</summary>
        public MonoAABB FloatBounds => new(Center - new Vector2(Radius), Center + new Vector2(Radius));

        /// <summary>点是否在圆内（含边界）。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">容差。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vector2 point, float tolerance = 0f)
            => Vector2.DistanceSquared(point, Center) <= (Radius + tolerance) * (Radius + tolerance);

        /// <summary>查询点到圆周最近的点。点位于圆心时沿 +X 方向退避。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestPoint(Vector2 point)
            => Center + MonoUtil.SafeNormalize(point - Center, Vector2.UnitX) * Radius;

        /// <summary>查询点到圆周的无符号距离。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point) => Math.Abs(Vector2.Distance(point, Center) - Radius);

        /// <summary>有符号距离：圆内为负，圆外为正（SDF 语义）。</summary>
        /// <param name="point">查询点。</param>
        public float SignedDistanceTo(Vector2 point) => Vector2.Distance(point, Center) - Radius;

        /// <summary>圆周上指定角度处的点。</summary>
        /// <param name="angle">角度（弧度）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 PointAtAngle(float angle) => Center + angle.ToRotationVector2() * Radius;

        /// <summary>圆上均匀分布的 <paramref name="count"/> 个点。</summary>
        /// <param name="count">点数。</param>
        /// <param name="angleOffset">起始角偏移。</param>
        public Vector2[] Sample(int count, float angleOffset = 0f)
        {
            if (count <= 0)
                return Array.Empty<Vector2>();
            Vector2[] points = new Vector2[count];
            for (int i = 0; i < count; i++)
                points[i] = PointAtAngle(angleOffset + TwoPi * i / count);
            return points;
        }

        /// <summary>该圆是否包含另一个圆（边界接触也算）。</summary>
        /// <param name="other">另一个圆。</param>
        /// <param name="tolerance">容差。</param>
        public bool Contains(MonoCircle other, float tolerance = 0f)
            => Vector2.Distance(Center, other.Center) + other.Radius <= Radius + tolerance;

        /// <summary>两圆是否相交或相切（边界接触也算）。</summary>
        /// <param name="other">另一个圆。</param>
        /// <param name="tolerance">容差。</param>
        public bool Intersect(MonoCircle other, float tolerance = 0f) 
            => Vector2.Distance(Center, other.Center) <= other.Radius + Radius + tolerance;

        /// <summary>缩放半径与圆心（相对原点）。</summary>
        /// <param name="scale">缩放系数。</param>
        public MonoCircle Scaled(float scale) => new(Center * scale, Radius * scale);

        /// <summary>平移。</summary>
        /// <param name="offset">位移。</param>
        public MonoCircle Moved(Vector2 offset) => new(Center + offset, Radius);

        /// <summary>半径放大 <paramref name="amount"/>（可为负）。</summary>
        /// <param name="amount">变化量。</param>
        public MonoCircle Inflated(float amount) => new(Center, Radius + amount);

        /// <inheritdoc/>
        public bool Equals(MonoCircle other) => Center == other.Center && Radius == other.Radius;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is MonoCircle other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Center, Radius);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoCircle a, MonoCircle b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoCircle a, MonoCircle b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"Circle[{Center} r={Radius}]";
    }
}
