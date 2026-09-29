using System.Runtime.CompilerServices;

using static Monochrome.Common.MonoUtil.MonoUtil;

namespace Monochrome.Common.MonoUtil.Mathematics.Geometry
{
    /// <summary>
    /// 二维多边形（顶点环，不重复首尾点）。
    /// <para>
    /// 顶点顺序约定：在 Y 轴向下的屏幕坐标系里，<b>逆时针</b>绕行为正方向
    /// （表现为顺时针）。<see cref="IsClockwise"/> 判断的是数学意义上的有符号面积。
    /// </para>
    /// <para>
    /// 这是一个类而非结构体：顶点数可变、且常被缓存复用。若只想做一次性计算，
    /// 请用不接受 <see cref="MonoPolygon"/> 的 <c>MonoUtil</c> 静态方法（接收 <see cref="ReadOnlySpan{T}"/>），避免构造对象的分配。
    /// </para>
    /// </summary>
    public sealed class MonoPolygon : IEquatable<MonoPolygon>
    {
        private readonly Vector2[] _vertices;

        /// <summary>以顶点数组构造。传入的数组会被直接持有，不会被复制。</summary>
        /// <param name="vertices">顶点数组（不重复首尾点）。</param>
        public MonoPolygon(Vector2[] vertices)
        {
            _vertices = vertices ?? [];
        }

        /// <summary>以顶点数组构造，可选择是否复制。</summary>
        /// <param name="vertices">顶点数组。</param>
        /// <param name="copy">为 true 时复制一份，避免外部修改影响本对象。</param>
        public MonoPolygon(Vector2[] vertices, bool copy)
        {
            _vertices = vertices is null ? [] : copy ? (Vector2[])vertices.Clone() : vertices;
        }

        /// <summary>以点集构造（可选是否复制）。</summary>
        /// <param name="vertices">点集。</param>
        /// <param name="copy">是否复制。</param>
        public MonoPolygon(ReadOnlySpan<Vector2> vertices, bool copy = true)
        {
            _vertices = copy ? vertices.ToArray() : Array.Empty<Vector2>();
        }

        /// <summary>顶点视图（只读）。</summary>
        public ReadOnlySpan<Vector2> Vertices => _vertices;

        /// <summary>顶点数。</summary>
        public int Count => _vertices.Length;

        /// <summary>是否为退化多边形（顶点数少于 3）。</summary>
        public bool IsDegenerate => _vertices.Length < 3;

        /// <summary>取第 <paramref name="index"/> 个顶点（索引会按顶点数取模，支持环形访问）。</summary>
        /// <param name="index">顶点索引。</param>
        public Vector2 this[int index] => _vertices[MonoUtil.Mod(index, Math.Max(_vertices.Length, 1))];

        /// <summary>取第 <paramref name="index"/> 条边（从顶点 i 到顶点 i+1）。</summary>
        /// <param name="index">边索引。</param>
        public MonoSegment Edge(int index)
        {
            if (_vertices.Length < 2)
                return new MonoSegment(Vector2.Zero, Vector2.Zero);
            int i = MonoUtil.Mod(index, _vertices.Length);
            return new MonoSegment(_vertices[i], _vertices[(i + 1) % _vertices.Length]);
        }

        /// <summary>有符号面积的两倍（鞋带公式）。逆时针为正。</summary>
        public float SignedAreaDouble
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < _vertices.Length; i++)
                {
                    Vector2 a = _vertices[i];
                    Vector2 b = _vertices[(i + 1) % _vertices.Length];
                    sum += a.X * b.Y - b.X * a.Y;
                }
                return sum;
            }
        }

        /// <summary>面积（恒非负）。</summary>
        public float Area => Math.Abs(SignedAreaDouble) * 0.5f;

        /// <summary>是否为数学意义上的顺时针绕行。</summary>
        public bool IsClockwise => SignedAreaDouble < 0f;

        /// <summary>是否为数学意义上的逆时针绕行。</summary>
        public bool IsCounterClockwise => SignedAreaDouble > 0f;

        /// <summary>质心（面心）。退化时返回顶点平均值。</summary>
        public Vector2 Centroid
        {
            get
            {
                float areaDouble = SignedAreaDouble;
                if (Math.Abs(areaDouble) <= MonoUtil.Epsilon)
                    return AverageVertex();

                float cx = 0f;
                float cy = 0f;
                for (int i = 0; i < _vertices.Length; i++)
                {
                    Vector2 a = _vertices[i];
                    Vector2 b = _vertices[(i + 1) % _vertices.Length];
                    float cross = a.X * b.Y - b.X * a.Y;
                    cx += (a.X + b.X) * cross;
                    cy += (a.Y + b.Y) * cross;
                }

                float factor = 1f / (3f * areaDouble);
                return new Vector2(cx * factor, cy * factor);
            }
        }

        /// <summary>周长。</summary>
        public float Perimeter
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < _vertices.Length; i++)
                    sum += Vector2.Distance(_vertices[i], _vertices[(i + 1) % _vertices.Length]);
                return sum;
            }
        }

        /// <summary>顶点的平均值（与面积无关，总在形状近旁）。</summary>
        public Vector2 AverageVertex()
        {
            if (_vertices.Length == 0)
                return Vector2.Zero;
            Vector2 sum = Vector2.Zero;
            for (int i = 0; i < _vertices.Length; i++)
                sum += _vertices[i];
            return sum / _vertices.Length;
        }

        /// <summary>顶点的轴对齐包围盒（浮点）。</summary>
        public MonoAABB FloatBounds => MonoAABB.FromPoints(_vertices);

        /// <summary>顶点的整数包围盒（向外取整）。</summary>
        public Rectangle Bounds => FloatBounds.ToRectangle();

        /// <summary>最长的边。</summary>
        public MonoSegment LongestEdge()
        {
            MonoSegment longest = Edge(0);
            float best = longest.LengthSquared;
            for (int i = 1; i < _vertices.Length; i++)
            {
                MonoSegment edge = Edge(i);
                if (edge.LengthSquared > best)
                {
                    best = edge.LengthSquared;
                    longest = edge;
                }
            }
            return longest;
        }

        /// <summary>点是否在多边形内（射线法 + 缠绕数，可用于凹多边形）。</summary>
        /// <param name="point">查询点。</param>
        public bool Contains(Vector2 point)
        {
            if (_vertices.Length < 3)
                return false;

            bool inside = false;
            for (int i = 0, j = _vertices.Length - 1; i < _vertices.Length; j = i++)
            {
                Vector2 vi = _vertices[i];
                Vector2 vj = _vertices[j];

                // 先判「点在边上」，避免边界上的点因数值抖动被误判为外部。
                if (new MonoSegment(vj, vi).Contains(point))
                    return true;

                if ((vi.Y > point.Y) != (vj.Y > point.Y)
                    && point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>点到多边形边界（或内部）的最近点：内部点返回其自身，外部点返回边界最近点。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestPoint(Vector2 point)
        {
            if (Contains(point))
                return point;

            Vector2 closest = point;
            float best = float.MaxValue;
            for (int i = 0; i < _vertices.Length; i++)
            {
                MonoSegment edge = Edge(i);
                Vector2 candidate = edge.ClosestPoint(point);
                float distance = Vector2.DistanceSquared(point, candidate);
                if (distance < best)
                {
                    best = distance;
                    closest = candidate;
                }
            }
            return closest;
        }

        /// <summary>点到多边形边界的最近点（内部点也会被推到边界上）。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestBoundaryPoint(Vector2 point)
        {
            Vector2 closest = point;
            float best = float.MaxValue;
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector2 candidate = Edge(i).ClosestPoint(point);
                float distance = Vector2.DistanceSquared(point, candidate);
                if (distance < best)
                {
                    best = distance;
                    closest = candidate;
                }
            }
            return closest;
        }

        /// <summary>点到多边形的最短距离；内部点为 0。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point)
            => Contains(point) ? 0f : Vector2.Distance(point, ClosestBoundaryPoint(point));

        /// <summary>
        /// 多边形的 SDF 语义有符号距离：外部为正，内部为负。
        /// </summary>
        /// <param name="point">查询点。</param>
        public float SignedDistanceTo(Vector2 point)
        {
            float distance = Vector2.Distance(point, ClosestBoundaryPoint(point));
            return Contains(point) ? -distance : distance;
        }

        /// <summary>点是否在圆与多边形的重叠区域内。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        public bool IntersectsCircle(Vector2 center, float radius)
        {
            if (Contains(center))
                return true;
            for (int i = 0; i < _vertices.Length; i++)
            {
                if (Edge(i).DistanceSquaredTo(center) <= radius * radius)
                    return true;
            }
            return false;
        }

        /// <summary>是否所有顶点都是凸的（要求顶点绕行一致）。</summary>
        public bool IsConvex()
        {
            if (_vertices.Length < 3)
                return false;

            int sign = 0;
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector2 a = _vertices[i];
                Vector2 b = _vertices[(i + 1) % _vertices.Length];
                Vector2 c = _vertices[(i + 2) % _vertices.Length];
                float cross = MonoUtil.Cross(a, b, c);
                if (Math.Abs(cross) <= MonoUtil.Epsilon)
                    continue;

                int currentSign = cross > 0f ? 1 : -1;
                if (sign == 0)
                    sign = currentSign;
                else if (sign != currentSign)
                    return false;
            }
            return true;
        }

        /// <summary>返回顶点顺序被反转的新多边形（用于统一绕行方向）。</summary>
        public MonoPolygon Reversed()
        {
            Vector2[] reversed = new Vector2[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
                reversed[i] = _vertices[_vertices.Length - 1 - i];
            return new MonoPolygon(reversed, false);
        }

        /// <summary>返回顶点顺序为逆时针的新多边形（必要时反转）。</summary>
        public MonoPolygon ToCounterClockwise() => IsClockwise ? Reversed() : this;

        /// <summary>返回顶点顺序为顺时针的新多边形（必要时反转）。</summary>
        public MonoPolygon ToClockwise() => IsCounterClockwise ? Reversed() : this;

        /// <summary>按给定变换逐顶点映射，返回新多边形。</summary>
        /// <param name="transform">顶点变换函数。</param>
        public MonoPolygon Transformed(Func<Vector2, Vector2> transform)
        {
            Vector2[] result = new Vector2[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
                result[i] = transform(_vertices[i]);
            return new MonoPolygon(result, false);
        }

        /// <summary>平移。</summary>
        /// <param name="offset">位移。</param>
        public MonoPolygon Moved(Vector2 offset)
        {
            Vector2[] result = new Vector2[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
                result[i] = _vertices[i] + offset;
            return new MonoPolygon(result, false);
        }

        /// <summary>绕 <paramref name="pivot"/> 旋转。</summary>
        /// <param name="angle">旋转角。</param>
        /// <param name="pivot">旋转中心。</param>
        public MonoPolygon Rotated(float angle, Vector2 pivot)
            => Transformed(v => MonoUtil.RotateAround(v, pivot, angle));

        /// <summary>以 <paramref name="pivot"/> 为中心缩放。</summary>
        /// <param name="scale">缩放系数。</param>
        /// <param name="pivot">缩放中心。</param>
        public MonoPolygon Scaled(Vector2 scale, Vector2 pivot)
            => Transformed(v => pivot + (v - pivot) * scale);

        /// <summary>凸包（Andrew 单调链）。返回的多边形为逆时针（数学意义）。</summary>
        public MonoPolygon ConvexHull() => MonoUtil.ConvexHull(_vertices);

        /// <summary>耳切三角化。返回的数组每三个索引构成一个三角形（索引指向本多边形的顶点）。</summary>
        public int[] Triangulate() => MonoUtil.Triangulate(_vertices);

        /// <summary>内缩（<paramref name="distance"/> 为正）或外扩（为负）。</summary>
        /// <param name="distance">偏移距离。</param>
        public MonoPolygon Offset(float distance) => MonoUtil.OffsetPolygon(_vertices, distance);

        /// <summary>重采样：在周长上均匀取 <paramref name="count"/> 个点，适合做平滑与绘制。</summary>
        /// <param name="count">目标点数。</param>
        public MonoPolygon Resample(int count) => MonoUtil.ResamplePolygon(_vertices, count);

        /// <summary>用 Chaikin 细分做一次圆滑处理。</summary>
        public MonoPolygon Smoothed() => MonoUtil.ChaikinSmooth(_vertices, 1);

        /// <summary>把多边形拆成边段数组（便于绘制与逐边检测）。</summary>
        public MonoSegment[] ToEdges()
        {
            int count = Math.Max(_vertices.Length, 0);
            MonoSegment[] edges = new MonoSegment[count];
            for (int i = 0; i < count; i++)
                edges[i] = Edge(i);
            return edges;
        }

        /// <summary>创建正多边形。</summary>
        /// <param name="center">中心。</param>
        /// <param name="radius">外接圆半径。</param>
        /// <param name="sides">边数，至少 3。</param>
        /// <param name="rotation">起始角偏移。</param>
        public static MonoPolygon Regular(Vector2 center, float radius, int sides, float rotation = 0f)
        {
            sides = Math.Max(sides, 3);
            Vector2[] vertices = new Vector2[sides];
            for (int i = 0; i < sides; i++)
                vertices[i] = center + (rotation + TwoPi * i / sides).ToRotationVector2() * radius;
            return new MonoPolygon(vertices, false);
        }

        /// <summary>创建矩形多边形（顶点为左上、右上、右下、左下）。</summary>
        /// <param name="center">中心。</param>
        /// <param name="halfExtents">半尺寸。</param>
        /// <param name="rotation">旋转角。</param>
        public static MonoPolygon Rectangle(Vector2 center, Vector2 halfExtents, float rotation = 0f)
        {
            Vector2 axisX = rotation.ToRotationVector2();
            Vector2 axisY = MonoUtil.Perpendicular(axisX);
            Vector2 ex = axisX * halfExtents.X;
            Vector2 ey = axisY * halfExtents.Y;
            return new MonoPolygon(new[] { center - ex - ey, center + ex - ey, center + ex + ey, center - ex + ey }, false);
        }

        /// <summary>创建星形多边形（顶点在长短半径间交替）。</summary>
        /// <param name="center">中心。</param>
        /// <param name="points">角数，至少 3。</param>
        /// <param name="outerRadius">外半径。</param>
        /// <param name="innerRatio">内半径相对外半径的比例，范围 (0,1)。</param>
        /// <param name="rotation">起始角偏移。</param>
        public static MonoPolygon Star(Vector2 center, int points, float outerRadius, float innerRatio = 0.5f, float rotation = 0f)
        {
            points = Math.Max(points, 3);
            innerRatio = MathHelper.Clamp(innerRatio, 0.001f, 0.999f);
            Vector2[] vertices = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float radius = i % 2 == 0 ? outerRadius : outerRadius * innerRatio;
                vertices[i] = center + (rotation + Pi * i / points).ToRotationVector2() * radius;
            }
            return new MonoPolygon(vertices, false);
        }

        /// <summary>创建圆的多边形近似。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        /// <param name="segments">分段数。</param>
        public static MonoPolygon Circle(Vector2 center, float radius, int segments = 24)
            => Regular(center, radius, segments);

        /// <summary>创建椭圆的多边形近似。</summary>
        /// <param name="ellipse">源椭圆。</param>
        /// <param name="segments">分段数。</param>
        public static MonoPolygon FromEllipse(MonoEllipse ellipse, int segments = 24)
        {
            segments = Math.Max(segments, 3);
            Vector2[] vertices = new Vector2[segments];
            for (int i = 0; i < segments; i++)
                vertices[i] = ellipse.PointAtAngle(TwoPi * i / segments);
            return new MonoPolygon(vertices, false);
        }

        /// <summary>创建圆弧的折线近似。</summary>
        /// <param name="arc">源圆弧。</param>
        /// <param name="segments">分段数。</param>
        public static MonoPolygon FromArc(MonoArc arc, int segments = 12)
            => new(arc.ToPolyline(segments), false);

        /// <inheritdoc/>
        public bool Equals(MonoPolygon other)
        {
            if (other is null || other._vertices.Length != _vertices.Length)
                return false;
            for (int i = 0; i < _vertices.Length; i++)
            {
                if (_vertices[i] != other._vertices[i])
                    return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is MonoPolygon other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            foreach (Vector2 vertex in _vertices)
                hash.Add(vertex);
            return hash.ToHashCode();
        }

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoPolygon a, MonoPolygon b) => ReferenceEquals(a, b) || (a is not null && a.Equals(b));

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoPolygon a, MonoPolygon b) => !(a == b);

        /// <inheritdoc/>
        public override string ToString() => $"Polygon[{_vertices.Length} vertices, area={Area:0.##}]";
    }
}
