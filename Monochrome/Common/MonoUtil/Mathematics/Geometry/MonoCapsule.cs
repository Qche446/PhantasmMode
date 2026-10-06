using static Monochrome.Common.MonoUtil.MonoUtil;

namespace Monochrome.Common.MonoUtil.Mathematics.Geometry
{
    /// <summary>
    /// 二维胶囊体：一条线段向两侧外扩固定半径得到的形状。
    /// 等价于线段与圆的 SDF 的闵可夫斯基和，是「带厚度的线段」的最自然表达。
    /// </summary>
    public readonly struct MonoCapsule : IEquatable<MonoCapsule>
    {
        /// <summary>中轴线起点。</summary>
        public readonly Vector2 Start;

        /// <summary>中轴线终点。</summary>
        public readonly Vector2 End;

        /// <summary>厚度半径。</summary>
        public readonly float Radius;

        /// <summary>以两端点与半径构造。</summary>
        /// <param name="start">中轴线起点。</param>
        /// <param name="end">中轴线终点。</param>
        /// <param name="radius">厚度半径。</param>
        public MonoCapsule(Vector2 start, Vector2 end, float radius)
        {
            Start = start;
            End = end;
            Radius = radius;
        }

        /// <summary>以线段与半径构造。</summary>
        /// <param name="segment">中轴线。</param>
        /// <param name="radius">厚度半径。</param>
        public MonoCapsule(MonoSegment segment, float radius)
            : this(segment.Start, segment.End, radius)
        {
        }

        /// <summary>中轴线。</summary>
        public MonoSegment Axis => new(Start, End);

        /// <summary>中轴线长度。</summary>
        public float AxisLength => Vector2.Distance(Start, End);

        /// <summary>总面积。</summary>
        public float Area => AxisLength * Radius * 2f + Pi * Radius * Radius;

        /// <summary>中心（中轴线中点）。</summary>
        public Vector2 Center => (Start + End) * 0.5f;

        /// <summary>两个端帽圆心。</summary>
        public (Vector2 Start, Vector2 End) CapCenters => (Start, End);

        /// <summary>点是否在胶囊体内（含边界）。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">容差。</param>
        public bool Contains(Vector2 point, float tolerance = 0f)
            => Axis.DistanceSquaredTo(point) <= (Radius + tolerance) * (Radius + tolerance);

        /// <summary>SDF 语义有符号距离：内部为负。</summary>
        /// <param name="point">查询点。</param>
        public float SignedDistanceTo(Vector2 point) => Axis.DistanceTo(point) - Radius;

        /// <summary>形状表面或中轴线上距离 <paramref name="point"/> 最近的点。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestPoint(Vector2 point) => Axis.ClosestPoint(point);

        /// <summary>形状边界上距离 <paramref name="point"/> 最近的点。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestBoundaryPoint(Vector2 point)
        {
            Vector2 axisPoint = Axis.ClosestPoint(point);
            return axisPoint + MonoUtil.SafeNormalize(point - axisPoint, Vector2.UnitX) * Radius;
        }

        /// <summary>点到形状边界的距离；点在内部时为 0。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point) => Math.Max(SignedDistanceTo(point), 0f);

        /// <summary>轴对齐包围盒（浮点）。</summary>
        public MonoAABB FloatBounds
            => new(Vector2.Min(Start, End) - new Vector2(Radius), Vector2.Max(Start, End) + new Vector2(Radius));

        /// <summary>整数精度的轴对齐包围盒。</summary>
        public Rectangle Bounds => FloatBounds.ToRectangle();

        /// <summary>两个端帽圆。</summary>
        public (MonoCircle Start, MonoCircle End) Caps => (new MonoCircle(Start, Radius), new MonoCircle(End, Radius));

        /// <summary>转为凸多边形近似（用于 SAT 求交与图元绘制）。</summary>
        /// <param name="segments">每侧端帽的分段数，至少 2。</param>
        public MonoPolygon ToPolygon(int segments = 8)
        {
            segments = Math.Max(segments, 2);
            Vector2 direction = MonoUtil.SafeNormalize(End - Start, Vector2.UnitX);
            float baseAngle = direction.ToRotation();
            Vector2[] points = new Vector2[segments * 2 + 2];

            int index = 0;
            for (int i = 0; i <= segments; i++)
                points[index++] = Start + (baseAngle + PiOver2 + Pi * i / segments).ToRotationVector2() * Radius;
            for (int i = 0; i <= segments; i++)
                points[index++] = End + (baseAngle - PiOver2 + Pi * i / segments).ToRotationVector2() * Radius;

            return new MonoPolygon(points, false);
        }

        /// <summary>以起点与终点、厚度构造（等价于扩展线段）。</summary>
        /// <param name="distance">两端各延长量。</param>
        public MonoCapsule ExtendedBy(float distance)
        {
            MonoSegment extended = Axis.ExtendedBy(distance);
            return new MonoCapsule(extended.Start, extended.End, Radius);
        }

        /// <summary>沿法线平移。</summary>
        /// <param name="offset">位移。</param>
        public MonoCapsule Moved(Vector2 offset) => new(Start + offset, End + offset, Radius);

        /// <inheritdoc/>
        public bool Equals(MonoCapsule other) => Start == other.Start && End == other.End && Radius == other.Radius;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is MonoCapsule other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Start, End, Radius);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoCapsule a, MonoCapsule b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoCapsule a, MonoCapsule b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"Capsule[{Start} -> {End} r={Radius}]";
    }
}
