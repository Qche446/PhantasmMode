using Monochrome.Common.MonoUtil.Mathematics.Geometry;
using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil
{
    /// <summary>
    /// 求交：线段×线段 / 线段×圆 / 圆×圆 / 射线×AABB / 射线×多边形。
    /// </summary>
    public static partial class MonoUtil
    {

        /// <summary>
        /// 求交算法集合。所有方法都返回 <see cref="MonoHit"/>（<c>bool</c> + 参数 + 命中点），
        /// 需要「只要布尔」时直接用隐式转换。
        /// </summary>
        public static partial class Intersect
        {
            /// <summary>
            /// 线段 × 线段。两端点各自按 [0,1] 截断。
            /// </summary>
            /// <param name="a">第一条线段。</param>
            /// <param name="b">第二条线段。</param>
            public static MonoHit SegmentSegment(MonoSegment a, MonoSegment b)
            {
                Vector2 r = a.Delta;
                Vector2 s = b.Delta;
                float denominator = MonoUtil.Cross(r, s);
                Vector2 offset = b.Start - a.Start;

                if (Math.Abs(denominator) <= MonoUtil.Epsilon)
                {
                    // 平行（或共线）：仅当共线且投影区间重叠时才认为重合，返回重叠段的中点。
                    if (Math.Abs(MonoUtil.Cross(offset, r)) > MonoUtil.Epsilon)
                        return MonoHit.None;

                    float rr = Vector2.Dot(r, r);
                    if (rr <= MonoUtil.EpsilonSqr)
                        return MonoHit.None;

                    float t0 = Vector2.Dot(b.Start - a.Start, r) / rr;
                    float t1 = Vector2.Dot(b.End - a.Start, r) / rr;
                    float low = Math.Min(t0, t1);
                    float high = Math.Max(t0, t1);
                    float start = Math.Max(low, 0f);
                    float end = Math.Min(high, 1f);
                    if (start > end + MonoUtil.Epsilon)
                        return MonoHit.None;

                    float mid = (start + end) * 0.5f;
                    return new MonoHit(true, a.PointAt(mid), mid, b.ProjectionParameter(a.PointAt(mid)));
                }

                float t = MonoUtil.Cross(offset, s) / denominator;
                float u = MonoUtil.Cross(offset, r) / denominator;

                if (t < -MonoUtil.Epsilon || t > 1f + MonoUtil.Epsilon
                 || u < -MonoUtil.Epsilon || u > 1f + MonoUtil.Epsilon)
                {
                    return MonoHit.None;
                }

                float clampedT = MathHelper.Clamp(t, 0f, 1f);
                return new MonoHit(true, a.PointAt(clampedT), clampedT, MathHelper.Clamp(u, 0f, 1f));
            }

            /// <summary>
            /// 线段 × 圆（第一个交点，沿线段方向由起点向终点）。
            /// </summary>
            /// <param name="segment">线段。</param>
            /// <param name="circle">圆。</param>
            public static MonoHit SegmentCircle(MonoSegment segment, MonoCircle circle)
            {
                Vector2 delta = segment.Delta;
                Vector2 toStart = segment.Start - circle.Center;

                float aa = Vector2.Dot(delta, delta);
                if (aa <= MonoUtil.EpsilonSqr)
                {
                    return circle.Contains(segment.Start, MonoUtil.Epsilon)
                        ? new MonoHit(true, segment.Start, 0f, 0f)
                        : MonoHit.None;
                }

                float bb = 2f * Vector2.Dot(toStart, delta);
                float cc = Vector2.Dot(toStart, toStart) - circle.Radius * circle.Radius;

                float discriminant = bb * bb - 4f * aa * cc;
                if (discriminant < 0f)
                    return MonoHit.None;

                float root = MathF.Sqrt(discriminant);
                float t1 = (-bb - root) / (2f * aa);
                float t2 = (-bb + root) / (2f * aa);

                float chosen = t1;
                if (chosen < -MonoUtil.Epsilon || chosen > 1f + MonoUtil.Epsilon)
                    chosen = t2;
                if (chosen < -MonoUtil.Epsilon || chosen > 1f + MonoUtil.Epsilon)
                {
                    // 线段整体落在圆内：无边界交点。
                    return MonoHit.None;
                }

                chosen = MathHelper.Clamp(chosen, 0f, 1f);
                return new MonoHit(true, segment.PointAt(chosen), chosen, 0f);
            }

            /// <summary>线段 × 圆，返回全部（最多两个）交点。</summary>
            /// <param name="segment">线段。</param>
            /// <param name="circle">圆。</param>
            /// <param name="results">写入结果的定长数组，长度至少 2。</param>
            /// <returns>实际交点数量。</returns>
            public static int SegmentCircleAll(MonoSegment segment, MonoCircle circle, MonoHit[] results)
            {
                if (results is null || results.Length < 2)
                    return 0;

                Vector2 delta = segment.Delta;
                Vector2 toStart = segment.Start - circle.Center;

                float aa = Vector2.Dot(delta, delta);
                if (aa <= MonoUtil.EpsilonSqr)
                {
                    if (!circle.Contains(segment.Start, MonoUtil.Epsilon))
                        return 0;
                    results[0] = new MonoHit(true, segment.Start, 0f, 0f);
                    return 1;
                }

                float bb = 2f * Vector2.Dot(toStart, delta);
                float cc = Vector2.Dot(toStart, toStart) - circle.Radius * circle.Radius;
                float discriminant = bb * bb - 4f * aa * cc;
                if (discriminant < 0f)
                    return 0;

                float root = MathF.Sqrt(discriminant);
                int count = 0;
                float[] candidates = { (-bb - root) / (2f * aa), (-bb + root) / (2f * aa) };
                for (int i = 0; i < 2; i++)
                {
                    float t = candidates[i];
                    if (t < -MonoUtil.Epsilon || t > 1f + MonoUtil.Epsilon)
                        continue;
                    t = MathHelper.Clamp(t, 0f, 1f);
                    results[count++] = new MonoHit(true, segment.PointAt(t), t, 0f);
                }
                return count;
            }

            /// <summary>圆 × 圆。</summary>
            /// <param name="a">第一个圆。</param>
            /// <param name="b">第二个圆。</param>
            public static MonoHit CircleCircle(MonoCircle a, MonoCircle b)
            {
                float distance = Vector2.Distance(a.Center, b.Center);
                float radiusSum = a.Radius + b.Radius;
                if (distance > radiusSum + MonoUtil.Epsilon || distance < MonoUtil.Epsilon)
                    return MonoHit.None;

                // 含容差的接触点：取两圆心连线上的半径分割点。
                float t = a.Radius / radiusSum;
                Vector2 point = Vector2.Lerp(a.Center, b.Center, t);
                return new MonoHit(true, point, t, 0f);
            }

            /// <summary>圆是否与线段相交（含被包含）。</summary>
            /// <param name="circle">圆。</param>
            /// <param name="segment">线段。</param>
            public static bool CircleSegment(MonoCircle circle, MonoSegment segment)
                => segment.DistanceSquaredTo(circle.Center) <= circle.Radius * circle.Radius;

            /// <summary>射线 × 轴对齐盒（slab 法）。返回沿射线的距离与命中点。</summary>
            /// <param name="ray">射线（无限或有界）。</param>
            /// <param name="box">轴对齐盒（浮点）。</param>
            /// <param name="distance">沿射线的命中距离。</param>
            /// <param name="point">命中点。</param>
            public static bool RayAABB(MonoRay ray, MonoAABB box, out float distance, out Vector2 point)
            {
                distance = 0f;
                point = Vector2.Zero;

                Vector2 direction = ray.Direction;
                Vector2 origin = ray.Origin;

                float tMin = 0f;
                float tMax = ray.IsInfinite ? float.PositiveInfinity : ray.Length;

                for (int axis = 0; axis < 2; axis++)
                {
                    float directionComponent = axis == 0 ? direction.X : direction.Y;
                    float originComponent = axis == 0 ? origin.X : origin.Y;
                    float minComponent = axis == 0 ? box.Min.X : box.Min.Y;
                    float maxComponent = axis == 0 ? box.Max.X : box.Max.Y;

                    if (Math.Abs(directionComponent) <= MonoUtil.Epsilon)
                    {
                        if (originComponent < minComponent || originComponent > maxComponent)
                            return false;
                        continue;
                    }

                    float inverse = 1f / directionComponent;
                    float t1 = (minComponent - originComponent) * inverse;
                    float t2 = (maxComponent - originComponent) * inverse;
                    if (t1 > t2)
                        (t1, t2) = (t2, t1);

                    tMin = Math.Max(tMin, t1);
                    tMax = Math.Min(tMax, t2);
                    if (tMin > tMax + MonoUtil.Epsilon)
                        return false;
                }

                distance = tMin;
                point = ray.PointAtDistance(tMin);
                return true;
            }

            /// <summary>
            /// 射线 × 线段。
            /// <para>
            /// 不能用「把无限射线转成线段再求交」的写法：<see cref="MonoRay.End"/> 对无限射线是无穷大，
            /// 会让线段求交退化并产生 NaN。这里直接解 2×2 线性方程组，有限/无限射线共用同一套逻辑。
            /// </para>
            /// </summary>
            /// <param name="ray">射线。</param>
            /// <param name="segment">线段。</param>
            public static MonoHit RaySegment(MonoRay ray, MonoSegment segment)
            {
                Vector2 rayDirection = ray.Direction;
                Vector2 segmentDelta = segment.Delta;
                Vector2 offset = segment.Start - ray.Origin;

                float denominator = MonoUtil.Cross(rayDirection, segmentDelta);
                if (Math.Abs(denominator) <= MonoUtil.Epsilon)
                {
                    // 平行：只有共线时才可能命中，此时返回线段上与射线起点最近的点。
                    if (Math.Abs(MonoUtil.Cross(offset, rayDirection)) > MonoUtil.Epsilon)
                        return MonoHit.None;

                    Vector2 candidate = segment.ClosestPoint(ray.Origin);
                    float distance = Vector2.Distance(ray.Origin, candidate);
                    if (!ray.IsInfinite && distance > ray.Length + MonoUtil.Epsilon)
                        return MonoHit.None;

                    // 还需确认该点确实在射线的正向一侧。
                    if (Vector2.Dot(candidate - ray.Origin, rayDirection) < -MonoUtil.Epsilon)
                        return MonoHit.None;

                    return new MonoHit(true, candidate, distance, segment.ProjectionParameter(candidate));
                }

                float rayParameter = MonoUtil.Cross(offset, segmentDelta) / denominator;
                float segmentParameter = MonoUtil.Cross(offset, rayDirection) / denominator;

                if (rayParameter < -MonoUtil.Epsilon || segmentParameter < -MonoUtil.Epsilon || segmentParameter > 1f + MonoUtil.Epsilon)
                    return MonoHit.None;
                if (!ray.IsInfinite && rayParameter > ray.Length + MonoUtil.Epsilon)
                    return MonoHit.None;

                float distanceAlong = Math.Max(rayParameter, 0f);
                return new MonoHit(true, ray.PointAtDistance(distanceAlong), distanceAlong, MathHelper.Clamp(segmentParameter, 0f, 1f));
            }

            /// <summary>射线 × 圆，返回最近一次命中。</summary>
            /// <param name="ray">射线。</param>
            /// <param name="circle">圆。</param>
            public static MonoHit RayCircle(MonoRay ray, MonoCircle circle)
            {
                Vector2 toOrigin = ray.Origin - circle.Center;

                float aa = Vector2.Dot(ray.Direction, ray.Direction);
                float bb = 2f * Vector2.Dot(toOrigin, ray.Direction);
                float cc = Vector2.Dot(toOrigin, toOrigin) - circle.Radius * circle.Radius;

                float discriminant = bb * bb - 4f * aa * cc;
                if (discriminant < 0f || aa <= MonoUtil.EpsilonSqr)
                    return MonoHit.None;

                float root = MathF.Sqrt(discriminant);
                float t1 = (-bb - root) / (2f * aa);
                float t2 = (-bb + root) / (2f * aa);

                float chosen = t1 >= -MonoUtil.Epsilon ? t1 : t2;
                if (chosen < -MonoUtil.Epsilon)
                    return MonoHit.None;
                if (!ray.IsInfinite && chosen > ray.Length + MonoUtil.Epsilon)
                    return MonoHit.None;

                chosen = Math.Max(chosen, 0f);
                return new MonoHit(true, ray.PointAtDistance(chosen), chosen, 0f);
            }

            /// <summary>
            /// 射线 × 凸多边形。返回最近一次的命中（沿射线方向）。
            /// 使用逐边求交，因此对凹多边形会返回「最近的边界命中」，不做内外判定。
            /// <para>
            /// 无限射线不能直接转成线段（端点会是无穷大，导致退化线段的投影参数变成 NaN），
            /// 因此这里对无限射线用「射线 × 线段」求解，对有限射线才降级为线段求交。
            /// </para>
            /// </summary>
            /// <param name="ray">射线。</param>
            /// <param name="polygon">多边形。</param>
            public static MonoHit RayPolygon(MonoRay ray, MonoPolygon polygon)
            {
                if (polygon is null || polygon.Count < 3)
                    return MonoHit.None;

                bool infinite = ray.IsInfinite;
                MonoSegment raySegment = infinite ? default : new MonoSegment(ray.Origin, ray.End);

                float bestDistance = float.MaxValue;
                MonoHit best = MonoHit.None;

                for (int i = 0; i < polygon.Count; i++)
                {
                    MonoHit hit = infinite ? RaySegment(ray, polygon.Edge(i)) : SegmentSegment(raySegment, polygon.Edge(i));
                    if (!hit.Hit)
                        continue;

                    float distance = Vector2.Distance(ray.Origin, hit.Point);
                    if (!infinite && distance > ray.Length + MonoUtil.Epsilon)
                        continue;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = new MonoHit(true, hit.Point, distance, hit.T2);
                    }
                }

                return best;
            }

            /// <summary>射线 × 有向盒。</summary>
            /// <param name="ray">射线。</param>
            /// <param name="box">有向盒。</param>
            public static MonoHit RayOBB(MonoRay ray, MonoOBB box)
            {
                MonoRay local = new(ray.Origin - box.Center, ray.Direction.RotatedBy(-box.Rotation), ray.Length);
                MonoAABB localBox = new(-box.HalfExtents, box.HalfExtents);
                if (!RayAABB(local, localBox, out float distance, out Vector2 localPoint))
                    return MonoHit.None;

                Vector2 world = box.Center + localPoint.RotatedBy(box.Rotation);
                return new MonoHit(true, world, distance, 0f);
            }

            /// <summary>线段是否与轴对齐盒相交。</summary>
            /// <param name="segment">线段。</param>
            /// <param name="box">轴对齐盒。</param>
            public static bool SegmentAABB(MonoSegment segment, MonoAABB box)
            {
                if (box.Contains(segment.Start) || box.Contains(segment.End))
                    return true;

                MonoRay ray = MonoRay.FromSegment(segment);
                return RayAABB(ray, box, out _, out _);
            }

            /// <summary>线段是否与有向盒相交。</summary>
            /// <param name="segment">线段。</param>
            /// <param name="box">有向盒。</param>
            public static bool SegmentOBB(MonoSegment segment, MonoOBB box)
            {
                MonoSegment local = new((segment.Start - box.Center).RotatedBy(-box.Rotation), (segment.End - box.Center).RotatedBy(-box.Rotation));
                MonoAABB localBox = new(-box.HalfExtents, box.HalfExtents);
                return SegmentAABB(local, localBox);
            }

            /// <summary>两轴对齐盒的相交测试（等价于 <see cref="MonoAABB.Intersects"/>）。</summary>
            /// <param name="a">第一个盒。</param>
            /// <param name="b">第二个盒。</param>
            public static bool AABBAABB(MonoAABB a, MonoAABB b) => a.Intersects(b);

            /// <summary>取两个轴对齐盒的重叠矩形；不重叠时返回空。</summary>
            /// <param name="a">第一个盒。</param>
            /// <param name="b">第二个盒。</param>
            /// <param name="overlap">重叠区域。</param>
            public static bool AABBOverlap(MonoAABB a, MonoAABB b, out MonoAABB overlap)
            {
                overlap = a.Intersection(b);
                if (overlap.Size.X <= 0f || overlap.Size.Y <= 0f)
                    return false;
                return true;
            }

            /// <summary>圆 × 轴对齐盒。</summary>
            /// <param name="circle">圆。</param>
            /// <param name="box">轴对齐盒。</param>
            public static bool CircleAABB(MonoCircle circle, MonoAABB box) => box.IntersectsCircle(circle.Center, circle.Radius);

            /// <summary>圆 × 有向盒。</summary>
            /// <param name="circle">圆。</param>
            /// <param name="box">有向盒。</param>
            public static bool CircleOBB(MonoCircle circle, MonoOBB box) => box.DistanceTo(circle.Center) <= circle.Radius;

            /// <summary>圆 × 圆（含容差）。</summary>
            /// <param name="a">第一个圆。</param>
            /// <param name="b">第二个圆。</param>
            /// <param name="tolerance">容差。</param>
            public static bool CircleCircle(MonoCircle a, MonoCircle b, float tolerance)
                => Vector2.Distance(a.Center, b.Center) <= a.Radius + b.Radius + tolerance;

            /// <summary>圆 × 多边形。</summary>
            /// <param name="circle">圆。</param>
            /// <param name="polygon">多边形。</param>
            public static bool CirclePolygon(MonoCircle circle, MonoPolygon polygon)
                => polygon is not null && polygon.IntersectsCircle(circle.Center, circle.Radius);

            /// <summary>胶囊 × 圆。</summary>
            /// <param name="capsule">胶囊。</param>
            /// <param name="circle">圆。</param>
            public static bool CapsuleCircle(MonoCapsule capsule, MonoCircle circle)
                => capsule.Axis.DistanceSquaredTo(circle.Center) <= (capsule.Radius + circle.Radius) * (capsule.Radius + circle.Radius);

            /// <summary>胶囊 × 线段。</summary>
            /// <param name="capsule">胶囊。</param>
            /// <param name="segment">线段。</param>
            public static bool CapsuleSegment(MonoCapsule capsule, MonoSegment segment)
            {
                // 中线到中线的最短距离小于半径和即相交。
                float best = float.MaxValue;
                best = Math.Min(best, capsule.Axis.DistanceSquaredTo(segment.Start));
                best = Math.Min(best, capsule.Axis.DistanceSquaredTo(segment.End));
                best = Math.Min(best, segment.DistanceSquaredTo(capsule.Start));
                best = Math.Min(best, segment.DistanceSquaredTo(capsule.End));

                if (best <= capsule.Radius * capsule.Radius)
                    return true;

                MonoHit hit = SegmentSegment(capsule.Axis, segment);
                if (hit.Hit)
                    return true;

                // 两条线段不相交时需要真正求「线段到线段」的距离。
                return SegmentSegmentDistanceSquared(capsule.Axis, segment) <= capsule.Radius * capsule.Radius;
            }

            /// <summary>两条线段之间的最短距离的平方。</summary>
            /// <param name="a">第一条线段。</param>
            /// <param name="b">第二条线段。</param>
            public static float SegmentSegmentDistanceSquared(MonoSegment a, MonoSegment b)
            {
                if (SegmentSegment(a, b).Hit)
                    return 0f;

                float best = a.DistanceSquaredTo(b.Start);
                best = Math.Min(best, a.DistanceSquaredTo(b.End));
                best = Math.Min(best, b.DistanceSquaredTo(a.Start));
                best = Math.Min(best, b.DistanceSquaredTo(a.End));
                return best;
            }

            /// <summary>两条线段之间的最短距离。</summary>
            /// <param name="a">第一条线段。</param>
            /// <param name="b">第二条线段。</param>
            public static float SegmentSegmentDistance(MonoSegment a, MonoSegment b) => MathF.Sqrt(SegmentSegmentDistanceSquared(a, b));

            /// <summary>
            /// 扫掠圆检测（连续碰撞 / CCD）：一个半径 <paramref name="radius"/> 的圆从
            /// <paramref name="previousCenter"/> 移动到 <paramref name="currentCenter"/> 是否碰到 <paramref name="target"/>。
            /// 高速弹幕用这个方法可以避免「穿墙」。
            /// </summary>
            /// <param name="previousCenter">上一帧圆心。</param>
            /// <param name="currentCenter">当前帧圆心。</param>
            /// <param name="radius">半径。</param>
            /// <param name="target">目标盒。</param>
            public static bool SweptCircleAABB(Vector2 previousCenter, Vector2 currentCenter, float radius, MonoAABB target)
            {
                MonoAABB expanded = target.Inflated(radius);
                if (expanded.Contains(previousCenter) || expanded.Contains(currentCenter))
                    return true;

                MonoSegment path = new(previousCenter, currentCenter);
                return SegmentAABB(path, expanded);
            }

            /// <summary>扫掠圆 × 圆。</summary>
            /// <param name="previousCenter">上一帧圆心。</param>
            /// <param name="currentCenter">当前帧圆心。</param>
            /// <param name="radius">移动圆半径。</param>
            /// <param name="target">目标圆。</param>
            public static bool SweptCircleCircle(Vector2 previousCenter, Vector2 currentCenter, float radius, MonoCircle target)
                => new MonoSegment(previousCenter, currentCenter).DistanceSquaredTo(target.Center) <= (radius + target.Radius) * (radius + target.Radius);
        }
    }

    /// <summary>
    /// 一次求交的结果。约定：<c>t1</c> 为第一个图元的归一化参数，<c>t2</c> 为第二个；
    /// 点图元的参数无意义（恒为 0）。
    /// </summary>
    public readonly struct MonoHit
    {
        /// <summary>是否命中。</summary>
        public readonly bool Hit;

        /// <summary>命中点。</summary>
        public readonly Vector2 Point;

        /// <summary>第一个图元的归一化参数。</summary>
        public readonly float T1;

        /// <summary>第二个图元的归一化参数。</summary>
        public readonly float T2;

        /// <summary>构造求交结果。</summary>
        /// <param name="hit">是否命中。</param>
        /// <param name="point">命中点。</param>
        /// <param name="t1">第一个图元的参数。</param>
        /// <param name="t2">第二个图元的参数。</param>
        public MonoHit(bool hit, Vector2 point, float t1, float t2)
        {
            Hit = hit;
            Point = point;
            T1 = t1;
            T2 = t2;
        }

        /// <summary>未命中的结果。</summary>
        public static MonoHit None => new(false, Vector2.Zero, 0f, 0f);

        /// <inheritdoc/>
        public override string ToString() => Hit ? $"Hit[{Point} t1={T1:0.###} t2={T2:0.###}]" : "Miss";

        /// <summary>隐式转换为 bool，便于 <c>if (hit)</c>。</summary>
        /// <param name="hit">求交结果。</param>
        public static implicit operator bool(MonoHit hit) => hit.Hit;
    }
}
