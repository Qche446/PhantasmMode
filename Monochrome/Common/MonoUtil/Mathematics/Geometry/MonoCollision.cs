using Monochrome.Common.MonoUtil.Mathematics.Geometry;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil
{
    /// <summary>
    /// 碰撞检测：SAT（分离轴）与最小平移向量、扫掠（CCD）、宽相加速。
    /// <para>
    /// 这一层<b>不替代</b> tML 的 <c>Terraria.Collision</c>。瓦片碰撞继续用原版 API
    /// （<c>Collision.CheckAABBvLineCollision</c> 等）；这里补的是原版缺失的场景：
    /// 自定义判定区域（Boss 的扇形/多边形攻击范围）、视觉物理、编辑器工具。
    /// </para>
    /// </summary>
    public static partial class MonoUtil
    {

        /// <summary>
        /// 碰撞检测算法集合（全部为无状态纯函数，热路径零分配）。
        /// </summary>
        /// <summary>
        /// 分离轴定理：两个凸多边形是否相交（含刚好接触）,若存在某个轴能让这两个凸多边形顶点在轴上的投影区域不相交。
        /// 顶点顺序以边的连线为准，保证闭合
        /// </summary>
        /// <param name="a">第一个凸多边形顶点。</param>
        /// <param name="b">第二个凸多边形顶点。</param>
        public static bool SatOverlap(ReadOnlySpan<Vector2> a, ReadOnlySpan<Vector2> b)
        {
            if (a.Length < 3 || b.Length < 3)
                return false;

            return !(HasSeparatingAxis(a, b) || HasSeparatingAxis(b, a));
        }

        /// <summary>
        /// 对凸多边形 <paramref name="reference"/> 的所有边法线检查是否存在分离轴。
        /// 返回 true 表示找到分离轴（即不相交）。
        /// </summary>
        private static bool HasSeparatingAxis(ReadOnlySpan<Vector2> reference, ReadOnlySpan<Vector2> other)
        {
            for (int i = 0; i < reference.Length; i++)
            {
                Vector2 edge = reference[(i + 1) % reference.Length] - reference[i];
                Vector2 axis = MonoUtil.Perpendicular(edge);
                if (axis.LengthSquared() <= MonoUtil.EpsilonSqr)
                    continue;

                axis = MonoUtil.SafeNormalize(axis, Vector2.UnitX);

                Project(reference, axis, out float minA, out float maxA);
                Project(other, axis, out float minB, out float maxB);

                if (minA > maxB + MonoUtil.Epsilon || minB > maxA + MonoUtil.Epsilon)
                    return true;
            }
            return false;
        }

        /// <summary>把顶点集投影到轴上，求最小/最大投影值。</summary>
        /// <param name="points">顶点集。</param>
        /// <param name="axis">投影轴（应为单位矢量）。</param>
        /// <param name="min">最小投影值。</param>
        /// <param name="max">最大投影值。</param>
        public static void Project(ReadOnlySpan<Vector2> points, Vector2 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                float value = Vector2.Dot(points[i], axis);
                if (value < min)
                    min = value;
                if (value > max)
                    max = value;
            }
        }

        /// <summary>
        /// 分离轴 + 最小平移向量（MTV）。用于「把重叠的两个判定体推开」。
        /// </summary>
        /// <param name="a">第一个凸多边形顶点。</param>
        /// <param name="b">第二个凸多边形顶点。</param>
        public static MonoCollisionInfo Sat(ReadOnlySpan<Vector2> a, ReadOnlySpan<Vector2> b)
        {
            if (a.Length < 3 || b.Length < 3)
                return MonoCollisionInfo.None;

            float smallestOverlap = float.MaxValue;
            Vector2 smallestAxis = Vector2.Zero;

            if (!AccumulateAxis(a, b, ref smallestOverlap, ref smallestAxis, out MonoCollisionInfo early))
                return early;

            if (!AccumulateAxis(b, a, ref smallestOverlap, ref smallestAxis, out early))
                return early;

            // 统一法线方向：让 normal 由 b 指向 a，从而「a + MTV」即为脱离接触后的位置。
            Vector2 direction = MonoUtil.SafeDirectionTo(MonoUtil.AverageVertex(b), MonoUtil.AverageVertex(a), Vector2.UnitX);
            if (Vector2.Dot(smallestAxis, direction) < 0f)
                smallestAxis = -smallestAxis;

            Vector2 mtv = smallestAxis * smallestOverlap;
            Vector2 contact = FindContactPoint(a, b, smallestAxis);
            return new MonoCollisionInfo(true, mtv, smallestAxis, contact);
        }

        /// <summary>逐轴累计最小重叠量；发现分离轴时提前返回 false。</summary>
        private static bool AccumulateAxis(
            ReadOnlySpan<Vector2> reference,
            ReadOnlySpan<Vector2> other,
            ref float smallestOverlap,
            ref Vector2 smallestAxis,
            out MonoCollisionInfo earlyResult)
        {
            earlyResult = MonoCollisionInfo.None;

            for (int i = 0; i < reference.Length; i++)
            {
                Vector2 edge = reference[(i + 1) % reference.Length] - reference[i];
                if (edge.LengthSquared() <= MonoUtil.EpsilonSqr)
                    continue;

                Vector2 axis = MonoUtil.SafeNormalize(MonoUtil.Perpendicular(edge), Vector2.UnitX);
                Project(reference, axis, out float minA, out float maxA);
                Project(other, axis, out float minB, out float maxB);

                float overlap = Math.Min(maxA, maxB) - Math.Max(minA, minB);
                if (overlap < -MonoUtil.Epsilon)
                    return false;

                if (overlap < smallestOverlap)
                {
                    smallestOverlap = overlap;
                    smallestAxis = axis;
                }
            }

            return true;
        }

        /// <summary>
        /// 轴对齐盒 × 轴对齐盒。比 SAT 更快，直接用重叠量求 MTV。
        /// </summary>
        /// <param name="a">第一个盒。</param>
        /// <param name="b">第二个盒。</param>
        public static MonoCollisionInfo CollideAABB(MonoAABB a, MonoAABB b)
        {
            float overlapX = Math.Min(a.Max.X, b.Max.X) - Math.Max(a.Min.X, b.Min.X);
            float overlapY = Math.Min(a.Max.Y, b.Max.Y) - Math.Max(a.Min.Y, b.Min.Y);

            if (overlapX < -MonoUtil.Epsilon || overlapY < -MonoUtil.Epsilon)
                return MonoCollisionInfo.None;

            Vector2 normal;
            float penetration;
            if (overlapX <= overlapY)
            {
                normal = a.Center.X <= b.Center.X ? new Vector2(-1f, 0f) : new Vector2(1f, 0f);
                penetration = overlapX;
            }
            else
            {
                normal = a.Center.Y <= b.Center.Y ? new Vector2(0f, -1f) : new Vector2(0f, 1f);
                penetration = overlapY;
            }

            Vector2 mtv = normal * penetration;
            Vector2 contact = new(
                Math.Max(a.Min.X, b.Min.X) + overlapX * 0.5f,
                Math.Max(a.Min.Y, b.Min.Y) + overlapY * 0.5f);
            return new MonoCollisionInfo(true, mtv, normal, contact);
        }

        /// <summary>有向盒 × 有向盒（转成 4 顶点后走 SAT）。</summary>
        /// <param name="a">第一个有向盒。</param>
        /// <param name="b">第二个有向盒。</param>
        public static MonoCollisionInfo CollideOBB(MonoOBB a, MonoOBB b)
            => Sat(a.Corners, b.Corners);

        /// <summary>轴对齐盒 × 有向盒。</summary>
        /// <param name="a">轴对齐盒。</param>
        /// <param name="b">有向盒。</param>
        public static MonoCollisionInfo CollideAABBOBB(MonoAABB a, MonoOBB b)
            => Sat(a.Corners, b.Corners);

        /// <summary>凸多边形 × 轴对齐盒。</summary>
        /// <param name="polygon">凸多边形。</param>
        /// <param name="box">轴对齐盒。</param>
        public static MonoCollisionInfo CollidePolygonAABB(MonoPolygon polygon, MonoAABB box)
            => polygon is null ? MonoCollisionInfo.None : Sat(polygon.Vertices, box.Corners);

        /// <summary>凸多边形 × 凸多边形。</summary>
        /// <param name="a">第一个凸多边形。</param>
        /// <param name="b">第二个凸多边形。</param>
        public static MonoCollisionInfo CollidePolygonPolygon(MonoPolygon a, MonoPolygon b)
            => a is null || b is null ? MonoCollisionInfo.None : Sat(a.Vertices, b.Vertices);

        /// <summary>胶囊 × 圆：返回把圆推离胶囊所需的最小向量（方向由胶囊指向圆）。</summary>
        /// <param name="capsule">胶囊。</param>
        /// <param name="circle">圆。</param>
        public static MonoCollisionInfo CollideCapsuleCircle(MonoCapsule capsule, MonoCircle circle)
        {
            Vector2 closest = capsule.Axis.ClosestPoint(circle.Center);
            Vector2 offset = circle.Center - closest;
            float distance = offset.Length();
            float radiusSum = capsule.Radius + circle.Radius;

            if (distance > radiusSum + MonoUtil.Epsilon)
                return MonoCollisionInfo.None;

            // 约定：Normal 由第二个形状（圆）指向第一个形状（胶囊），a + MTV 即脱离接触。
            // 这里的 offset 是「胶囊轴 → 圆心」，方向与约定相反，取负后 capsule + MTV 才能分离。
            Vector2 normal = MonoUtil.SafeNormalize(-offset, -Vector2.UnitY);
            float penetration = radiusSum - distance;
            return new MonoCollisionInfo(true, normal * penetration, normal, closest);
        }

        /// <summary>圆 × 轴对齐盒：返回把圆推离盒所需的最小向量（方向由盒指向圆）。</summary>
        /// <param name="circle">圆。</param>
        /// <param name="box">轴对齐盒。</param>
        public static MonoCollisionInfo CollideCircleAABB(MonoCircle circle, MonoAABB box)
        {
            Vector2 center = circle.Center;

            if (!box.Contains(center))
            {
                Vector2 closest = box.ClosestPoint(center);
                Vector2 offset = center - closest;
                float distance = offset.Length();
                if (distance > circle.Radius + MonoUtil.Epsilon)
                    return MonoCollisionInfo.None;

                Vector2 normal = MonoUtil.SafeNormalize(offset, Vector2.UnitY);
                return new MonoCollisionInfo(true, normal * (circle.Radius - distance), normal, closest);
            }

            // 圆心在盒内：推向最近的一条边。
            float left = center.X - box.Min.X;
            float right = box.Max.X - center.X;
            float top = center.Y - box.Min.Y;
            float bottom = box.Max.Y - center.Y;

            float best = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
            Vector2 insideNormal;
            Vector2 contact;
            if (best == left)
            {
                insideNormal = new Vector2(-1f, 0f);
                contact = new Vector2(box.Min.X, center.Y);
            }
            else if (best == right)
            {
                insideNormal = new Vector2(1f, 0f);
                contact = new Vector2(box.Max.X, center.Y);
            }
            else if (best == top)
            {
                insideNormal = new Vector2(0f, -1f);
                contact = new Vector2(center.X, box.Min.Y);
            }
            else
            {
                insideNormal = new Vector2(0f, 1f);
                contact = new Vector2(center.X, box.Max.Y);
            }

            // 圆在盒内时，圆柱是否还能伸出去决定是否真的相交。
            float penetration = best + circle.Radius;
            return new MonoCollisionInfo(true, insideNormal * penetration, insideNormal, contact);
        }

        /// <summary>
        /// 沿 <paramref name="direction"/> 移动 <paramref name="moving"/> 时，首次与 <paramref name="target"/> 接触的进度。
        /// 采用「按半径膨胀 + 射线扫描」的保守方式，适合弹幕与高速实体。
        /// </summary>
        /// <param name="moving">移动盒。</param>
        /// <param name="target">目标盒。</param>
        /// <param name="direction">位移向量。</param>
        /// <param name="hitTime">命中进度（0~1），未命中时为 1。</param>
        /// <param name="point">接触点。</param>
        public static bool SweepAABBAABB(MonoAABB moving, MonoAABB target, Vector2 direction, out float hitTime, out Vector2 point)
        {
            hitTime = 1f;
            point = Vector2.Zero;

            if (moving.Intersects(target))
            {
                hitTime = 0f;
                point = moving.Center;
                return true;
            }

            // 把移动盒视为点，目标盒按移动盒半尺寸膨胀，问题化归为射线 vs AABB。
            MonoAABB expanded = target.Inflated(moving.HalfExtents);
            Vector2 origin = moving.Center;

            if (direction.LengthSquared() <= MonoUtil.EpsilonSqr)
                return false;

            MonoRay ray = new(origin, direction, direction.Length());
            if (!MonoUtil.RayAABB(ray, expanded, out float distance, out point))
                return false;

            hitTime = distance / direction.Length();
            return true;
        }


        /// <summary>由两条凸多边形的接触寻找一个近似接触点（取互相最近的顶点对的中点）。</summary>
        private static Vector2 FindContactPoint(ReadOnlySpan<Vector2> a, ReadOnlySpan<Vector2> b, Vector2 axis)
        {
            Vector2 bestA = a[0];
            Vector2 bestB = b[0];
            float bestDistance = float.MaxValue;

            for (int i = 0; i < a.Length; i++)
            {
                for (int j = 0; j < b.Length; j++)
                {
                    float distance = Vector2.DistanceSquared(a[i], b[j]);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestA = a[i];
                        bestB = b[j];
                    }
                }
            }

            return (bestA + bestB) * 0.5f;
        }
    }

    /// <summary>
    /// 一次凸体碰撞的结果：布尔 + 最小平移向量（MTV）+ 接触点。
    /// </summary>
    public readonly struct MonoCollisionInfo
    {
        /// <summary>是否发生碰撞（含刚好接触）。</summary>
        public readonly bool Colliding;

        /// <summary>
        /// 最小平移向量：把第一个形状沿该向量移动即可脱离接触。
        /// 未碰撞时为 <see cref="Vector2.Zero"/>。
        /// </summary>
        public readonly Vector2 MinimumTranslationVector;

        /// <summary>穿透深度（最小平移向量的长度）。</summary>
        public readonly float Penetration;

        /// <summary>分离法线（单位矢量，由第二个形状指向第一个形状，与 MTV 同向）。</summary>
        public readonly Vector2 Normal;

        /// <summary>接触点。</summary>
        public readonly Vector2 ContactPoint;

        /// <summary>构造碰撞结果。</summary>
        /// <param name="colliding">是否碰撞。</param>
        /// <param name="minimumTranslationVector">最小平移向量。</param>
        /// <param name="normal">分离法线。</param>
        /// <param name="contactPoint">接触点。</param>
        public MonoCollisionInfo(bool colliding, Vector2 minimumTranslationVector, Vector2 normal, Vector2 contactPoint)
        {
            Colliding = colliding;
            MinimumTranslationVector = minimumTranslationVector;
            Penetration = minimumTranslationVector.Length();
            Normal = normal;
            ContactPoint = contactPoint;
        }

        /// <summary>未碰撞的结果。</summary>
        public static MonoCollisionInfo None => new(false, Vector2.Zero, Vector2.Zero, Vector2.Zero);

        /// <inheritdoc/>
        public override string ToString() => Colliding ? $"Collision[pen={Penetration:0.##} mtv={MinimumTranslationVector}]" : "NoCollision";

        /// <summary>隐式转换为 bool，便于 <c>if (info)</c>。</summary>
        /// <param name="info">碰撞结果。</param>
        public static implicit operator bool(MonoCollisionInfo info) => info.Colliding;
    }

    /// <summary>
    /// 均匀网格宽相。用于「大量实体两两检测」的场景：先按包围盒分桶，再只对同桶对做精测。
    /// 这是本层唯一带状态的类型，需在实体集合变化后调用 <see cref="Rebuild"/>。
    /// </summary>
    public sealed class MonoUniformGrid
    {
        private readonly float _cellSize;
        private MonoAABB[] _bounds = Array.Empty<MonoAABB>();
        private readonly Dictionary<long, List<int>> _cells = new();
        private readonly HashSet<long> _visitedPairs = new();
        private int _count;

        /// <summary>构造网格。</summary>
        /// <param name="cellSize">格子边长（像素），建议接近典型实体尺寸的 2 倍。</param>
        public MonoUniformGrid(float cellSize = 64f)
        {
            _cellSize = Math.Max(cellSize, 1f);
        }

        /// <summary>当前登记的实体数量。</summary>
        public int Count => _count;

        /// <summary>格子边长。</summary>
        public float CellSize => _cellSize;

        /// <summary>清空并重新登记一组包围盒。</summary>
        /// <param name="bounds">实体包围盒数组（索引即实体 id）。</param>
        public void Rebuild(MonoAABB[] bounds)
        {
            _bounds = bounds ?? Array.Empty<MonoAABB>();
            _count = _bounds.Length;
            _cells.Clear();

            for (int i = 0; i < _bounds.Length; i++)
            {
                GetCellRange(_bounds[i], out int minX, out int minY, out int maxX, out int maxY);
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        long key = PackKey(x, y);
                        if (!_cells.TryGetValue(key, out List<int> bucket))
                        {
                            bucket = new List<int>(4);
                            _cells[key] = bucket;
                        }
                        bucket.Add(i);
                    }
                }
            }
        }

        /// <summary>
        /// 查询可能与 <paramref name="query"/> 重叠的实体索引。
        /// 结果写入 <paramref name="results"/>，避免每次分配。
        /// </summary>
        /// <param name="query">查询包围盒。</param>
        /// <param name="results">写入结果的列表，调用方负责 Clear。</param>
        /// <returns>写入的候选数量。</returns>
        public int Query(MonoAABB query, List<int> results)
        {
            if (results is null)
                return 0;

            GetCellRange(query, out int minX, out int minY, out int maxX, out int maxY);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (!_cells.TryGetValue(PackKey(x, y), out List<int> bucket))
                        continue;

                    for (int i = 0; i < bucket.Count; i++)
                        results.Add(bucket[i]);
                }
            }

            return results.Count;
        }

        /// <summary>
        /// 对所有可能重叠的实体对执行回调。宽相去重后仍会由回调自行做精测。
        /// 回调返回 false 可提前终止。
        /// </summary>
        /// <param name="action">接收两个实体索引的回调。</param>
        public void ForEachPair(Func<int, int, bool> action)
        {
            if (action is null)
                return;

            _visitedPairs.Clear();
            foreach (KeyValuePair<long, List<int>> entry in _cells)
            {
                List<int> bucket = entry.Value;
                for (int i = 0; i < bucket.Count; i++)
                {
                    for (int j = i + 1; j < bucket.Count; j++)
                    {
                        int a = Math.Min(bucket[i], bucket[j]);
                        int b = Math.Max(bucket[i], bucket[j]);
                        if (a == b)
                            continue;
                        if (!_visitedPairs.Add(((long)a << 32) ^ (uint)b))
                            continue;

                        // 两个实体必须真的包围盒相交才值得回调。
                        if (!_bounds[a].Intersects(_bounds[b]))
                            continue;

                        if (!action(a, b))
                            return;
                    }
                }
            }
        }

        /// <summary>清空网格。</summary>
        public void Clear()
        {
            _cells.Clear();
            _bounds = Array.Empty<MonoAABB>();
            _count = 0;
        }

        private void GetCellRange(MonoAABB bounds, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = (int)MathF.Floor(bounds.Min.X / _cellSize);
            minY = (int)MathF.Floor(bounds.Min.Y / _cellSize);
            maxX = (int)MathF.Floor(bounds.Max.X / _cellSize);
            maxY = (int)MathF.Floor(bounds.Max.Y / _cellSize);
        }

        private static long PackKey(int x, int y) => ((long)x << 32) ^ (uint)y;
    }
}
