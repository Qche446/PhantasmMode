using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil.Mathematics.Geometry
{
    /// <summary>
    /// 二维射线：从 <see cref="Origin"/> 沿 <see cref="Direction"/> 出发。
    /// <see cref="Length"/> 为 <see cref="MonoUtil.Infinity"/> 时是真正的无限射线，
    /// 否则等价于一条有向线段——这样「线段」与「射线」的求交、最近点逻辑只需写一份。
    /// </summary>
    public readonly struct MonoRay : IEquatable<MonoRay>
    {
        /// <summary>起点。</summary>
        public readonly Vector2 Origin;

        /// <summary>方向（应为单位矢量；构造时不会自动归一化，请用工厂方法）。</summary>
        public readonly Vector2 Direction;

        /// <summary>长度，默认为无限。</summary>
        public readonly float Length;

        /// <summary>以起点与方向构造无限射线。</summary>
        /// <param name="origin">起点。</param>
        /// <param name="direction">方向，会被安全归一化。</param>
        public MonoRay(Vector2 origin, Vector2 direction)
            : this(origin, direction, MonoUtil.Infinity)
        {
        }

        /// <summary>以起点、方向与有限长度构造。</summary>
        /// <param name="origin">起点。</param>
        /// <param name="direction">方向，会被安全归一化。</param>
        /// <param name="length">长度，传 <see cref="MonoUtil.Infinity"/> 表示无限。</param>
        public MonoRay(Vector2 origin, Vector2 direction, float length)
        {
            Origin = origin;
            Direction = MonoUtil.SafeNormalize(direction, Vector2.UnitX);
            Length = length < 0f ? 0f : length;
        }

        /// <summary>是否为无限射线。</summary>
        public bool IsInfinite => float.IsInfinity(Length);

        /// <summary>从线段构造。</summary>
        /// <param name="segment">源线段。</param>
        public static MonoRay FromSegment(MonoSegment segment) => new(segment.Start, segment.Direction, segment.Length);

        /// <summary>从两点的连线构造有限射线。</summary>
        /// <param name="start">起点。</param>
        /// <param name="end">终点。</param>
        public static MonoRay FromTwoPoints(Vector2 start, Vector2 end) => new(start, end - start, Vector2.Distance(start, end));

        /// <summary>取射线上参数 <paramref name="distanceAlongRay"/>（沿方向的**距离**）处的点。</summary>
        /// <param name="distanceAlongRay">沿方向的距离，可为负（反向延长）。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 PointAtDistance(float distanceAlongRay) => Origin + Direction * distanceAlongRay;

        /// <summary>取归一化参数 <paramref name="t"/>（0 为起点，1 为终点）处的点。无限射线的 1 无意义，会返回 <see cref="float.PositiveInfinity"/> 结果。</summary>
        /// <param name="t">归一化参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 PointAt(float t) => Origin + Direction * (Length * t);

        /// <summary>有限射线的终点；无限射线返回 <see cref="float.PositiveInfinity"/> 方向的点。</summary>
        public Vector2 End => PointAtDistance(Length);

        /// <summary>把点向方向投影得到的距离（未按长度截断，可能为负）。</summary>
        /// <param name="point">查询点。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ProjectionDistance(Vector2 point) => Vector2.Dot(point - Origin, Direction);

        /// <summary>射线上距离 <paramref name="point"/> 最近的点。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestPoint(Vector2 point)
            => PointAtDistance(MathHelper.Clamp(ProjectionDistance(point), 0f, Length));

        /// <summary>查询点到射线的最短距离。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point) => Vector2.Distance(point, ClosestPoint(point));

        /// <summary>点到射线的最近点及其沿方向的距离。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="distanceAlongRay">截断后的沿方向距离。</param>
        public Vector2 ClosestPoint(Vector2 point, out float distanceAlongRay)
        {
            distanceAlongRay = MathHelper.Clamp(ProjectionDistance(point), 0f, Length);
            return PointAtDistance(distanceAlongRay);
        }

        /// <summary>点是否落在射线上（含起点与终点），按容差判断。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">容差。</param>
        public bool Contains(Vector2 point, float tolerance = MonoUtil.Epsilon) => DistanceTo(point) <= tolerance;

        /// <summary>点到射线所在**直线**的有符号距离（负为方向右侧）。</summary>
        /// <param name="point">查询点。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Side(Vector2 point) => MonoUtil.Cross(Direction, point - Origin);

        /// <summary>转换为等效线段；无限射线会退化为 <c>0~Length</c> 的极大线段。</summary>
        public MonoSegment ToSegment() => new(Origin, End);

        /// <inheritdoc/>
        public bool Equals(MonoRay other) => Origin == other.Origin && Direction == other.Direction && Length == other.Length;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is MonoRay other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Origin, Direction, Length);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoRay a, MonoRay b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoRay a, MonoRay b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"Ray[{Origin} dir {Direction} len {(IsInfinite ? "inf" : Length.ToString())}]";
    }
}
