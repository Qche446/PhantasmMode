using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil.Mathematics.Geometry
{
    /// <summary>
    /// 二维有向线段。可变长（<see cref="Length"/> 非零），也可退化为一个点。
    /// </summary>
    public readonly struct MonoSegment : IEquatable<MonoSegment>
    {
        /// <summary>起点。</summary>
        public readonly Vector2 Start;

        /// <summary>终点。</summary>
        public readonly Vector2 End;

        /// <summary>以起点/终点构造。</summary>
        /// <param name="start">起点。</param>
        /// <param name="end">终点。</param>
        public MonoSegment(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }

        /// <summary>以起点与「起点→终点」的位移构造。</summary>
        /// <param name="origin">起点。</param>
        /// <param name="offset">位移。</param>
        public static MonoSegment FromOffset(Vector2 origin, Vector2 offset) => new(origin, origin + offset);

        /// <summary>终点减起点。</summary>
        public Vector2 Delta => End - Start;

        /// <summary>长度。</summary>
        public float Length => Delta.Length();

        /// <summary>长度的平方（省一次开方）。</summary>
        public float LengthSquared => Delta.LengthSquared();

        /// <summary>方向单位矢量；退化线段返回 <see cref="Vector2.UnitX"/>。</summary>
        public Vector2 Direction => MonoUtil.SafeNormalize(Delta, Vector2.UnitX);

        /// <summary>方向角度（弧度）。</summary>
        public float Angle => Delta.ToRotation();

        /// <summary>中点。</summary>
        public Vector2 Center => (Start + End) * 0.5f;

        /// <summary>左法线（逆时针 90°的单位矢量）。</summary>
        public Vector2 Normal => MonoUtil.Perpendicular(Direction);

        /// <summary>轴对齐包围盒。</summary>
        public Rectangle Bounds
        {
            get
            {
                float minX = Math.Min(Start.X, End.X);
                float minY = Math.Min(Start.Y, End.Y);
                float maxX = Math.Max(Start.X, End.X);
                float maxY = Math.Max(Start.Y, End.Y);
                return new Rectangle((int)MathF.Floor(minX), (int)MathF.Floor(minY), (int)MathF.Ceiling(maxX - minX), (int)MathF.Ceiling(maxY - minY));
            }
        }

        /// <summary>取线段上参数 <paramref name="t"/> 处的点（<c>t=0</c> 为起点，<c>t=1</c> 为终点，不截断）。</summary>
        /// <param name="t">参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 PointAt(float t) => Start + Delta * t;

        /// <summary>点在线段上的投影参数（未截断，可能落在 [0,1] 之外）。退化线段返回 0。</summary>
        /// <param name="point">被投影的点。</param>
        public float ProjectionParameter(Vector2 point)
        {
            float lengthSquared = LengthSquared;
            if (lengthSquared <= MonoUtil.EpsilonSqr)
                return 0f;
            return Vector2.Dot(point - Start, Delta) / lengthSquared;
        }

        /// <summary>线段上距离 <paramref name="point"/> 最近的点。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestPoint(Vector2 point) => PointAt(MathHelper.Clamp(ProjectionParameter(point), 0f, 1f));

        /// <summary>查询点到线段的最短距离。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point) => Vector2.Distance(point, ClosestPoint(point));

        /// <summary>查询点到线段最短距离的平方（省开方，用于比较）。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceSquaredTo(Vector2 point) => Vector2.DistanceSquared(point, ClosestPoint(point));

        /// <summary>点到线段的最近点与其参数（<c>t</c> 已截断到 [0,1]）。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="t">截断后的参数。</param>
        public Vector2 ClosestPoint(Vector2 point, out float t)
        {
            t = MathHelper.Clamp(ProjectionParameter(point), 0f, 1f);
            return PointAt(t);
        }

        /// <summary>点是否落在线段上（含端点），按容差判断。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">点到线段的容差。</param>
        public bool Contains(Vector2 point, float tolerance = MonoUtil.Epsilon) => DistanceSquaredTo(point) <= tolerance * tolerance;

        /// <summary>把线段沿其法线平移 <paramref name="distance"/>（正值为左法线方向）。</summary>
        /// <param name="distance">平移距离。</param>
        public MonoSegment Offset(float distance)
        {
            Vector2 offset = Normal * distance;
            return new MonoSegment(Start + offset, End + offset);
        }

        /// <summary>把线段延长（两端各延长 <paramref name="amount"/>）。</summary>
        /// <param name="amount">延长量，可为负表示缩短。</param>
        public MonoSegment ExtendedBy(float amount)
        {
            Vector2 direction = Direction;
            return new MonoSegment(Start - direction * amount, End + direction * amount);
        }

        /// <summary>翻转方向。</summary>
        public MonoSegment Reversed() => new(End, Start);

        /// <summary>获取在线段上均匀分布的 <paramref name="count"/> 个点。</summary>
        /// <param name="count">点数，小于 2 时返回空数组。</param>
        public Vector2[] Sample(int count)
        {
            if (count < 2)
                return Array.Empty<Vector2>();
            Vector2[] points = new Vector2[count];
            for (int i = 0; i < count; i++)
                points[i] = PointAt(i / (count - 1f));
            return points;
        }

        /// <summary>点积形式的「点到直线」有符号距离（以 <see cref="Start"/> 为原点）。</summary>
        /// <param name="point">查询点。</param>
        public float SignedDistanceTo(Vector2 point) => Vector2.Dot(Direction, point - Start);

        /// <summary>点位于线段的哪一侧：正为左法线侧，负为右法线侧。</summary>
        /// <param name="point">查询点。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Side(Vector2 point) => MonoUtil.Cross(Delta, point - Start);

        /// <inheritdoc/>
        public bool Equals(MonoSegment other) => Start == other.Start && End == other.End;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is MonoSegment other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Start, End);

        /// <summary>逐分量相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoSegment a, MonoSegment b) => a.Equals(b);

        /// <summary>逐分量不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoSegment a, MonoSegment b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"Segment[{Start} -> {End}]";
    }
}
