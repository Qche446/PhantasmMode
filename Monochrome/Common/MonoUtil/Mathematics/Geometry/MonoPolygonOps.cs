using Monochrome.Common.MonoUtil.Mathematics.Geometry;
using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil
{
    /// <summary>
    /// 多边形算法：凸包、耳切三角化、内缩外扩、重采样、圆滑。
    /// </summary>
    public static partial class MonoUtil
    {
        /// <summary>多边形算法集合（全部为无状态纯函数）。</summary>
        public static partial class Polygon
        {
            /// <summary>
            /// Andrew 单调链凸包。返回的多边形顶点按数学逆时针排列，且首尾不重复。
            /// 去重后不足 3 点时按退化情况返回。
            /// </summary>
            /// <param name="points">输入点集，不会被修改。</param>
            public static MonoPolygon ConvexHull(ReadOnlySpan<Vector2> points)
            {
                if (points.Length == 0)
                    return new MonoPolygon(Array.Empty<Vector2>(), false);
                if (points.Length <= 2)
                    return new MonoPolygon(points.ToArray(), false);

                Vector2[] sorted = points.ToArray();
                Array.Sort(sorted, static (a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));

                // 按容差去重。
                int uniqueCount = 1;
                for (int i = 1; i < sorted.Length; i++)
                {
                    if (!MonoUtil.Approx(sorted[i], sorted[uniqueCount - 1]))
                        sorted[uniqueCount++] = sorted[i];
                }

                if (uniqueCount <= 2)
                {
                    Vector2[] degenerate = new Vector2[uniqueCount];
                    Array.Copy(sorted, degenerate, uniqueCount);
                    return new MonoPolygon(degenerate, false);
                }

                Vector2[] hull = new Vector2[uniqueCount * 2];
                int k = 0;

                for (int i = 0; i < uniqueCount; i++)
                {
                    while (k >= 2 && MonoUtil.Cross(hull[k - 2], hull[k - 1], sorted[i]) <= 0f)
                        k--;
                    hull[k++] = sorted[i];
                }

                int lower = k + 1;
                for (int i = uniqueCount - 2; i >= 0; i--)
                {
                    while (k >= lower && MonoUtil.Cross(hull[k - 2], hull[k - 1], sorted[i]) <= 0f)
                        k--;
                    hull[k++] = sorted[i];
                }

                // 末尾会回到起点，去掉重复的最后一个点。
                int finalCount = Math.Max(k - 1, 0);
                if (finalCount >= 3)
                {
                    Vector2[] result = new Vector2[finalCount];
                    Array.Copy(hull, result, finalCount);
                    return new MonoPolygon(result, false);
                }

                Vector2[] fallback = new Vector2[uniqueCount];
                Array.Copy(sorted, fallback, uniqueCount);
                return new MonoPolygon(fallback, false);
            }

            /// <summary>
            /// 耳切（Ear Clipping）三角化。要求多边形是**简单多边形**（不自交），凹凸均可。
            /// 返回的数组每三个索引构成一个三角形，索引指向<b>输入顶点</b>（输入是顺时针还是逆时针都一样）。
            /// </summary>
            /// <param name="vertices">多边形顶点。</param>
            public static int[] Triangulate(ReadOnlySpan<Vector2> vertices)
            {
                int count = vertices.Length;
                if (count < 3)
                    return Array.Empty<int>();

                Vector2[] points = vertices.ToArray();
                bool reversed = !IsCounterClockwise(points);
                if (reversed)
                    Array.Reverse(points);

                // 下面整段耳切都在 points 空间里算：points 空间的第 j 个点，其实是输入的第
                // (count-1-j) 个点（一旦上面翻转过）。所以**输出索引必须换回输入空间**。
                // 早先这里直接把 points 空间的编号当成了输入编号，于是顺时针输入时吐出的三角形
                // 根本不铺满原多边形（实测：顺时针 L 形，三角形面积合计 9，多边形只有 5）。
                int ToInput(int pointIndex) => reversed ? count - 1 - pointIndex : pointIndex;

                int[] indices = new int[count];
                for (int i = 0; i < count; i++)
                    indices[i] = i;

                int[] triangles = new int[(count - 2) * 3];
                int triangleIndex = 0;
                int remaining = count;
                int guard = count * count + 16;

                while (remaining > 3 && guard-- > 0)
                {
                    bool earFound = false;
                    for (int i = 0; i < remaining; i++)
                    {
                        int previous = indices[MonoUtil.Mod(i - 1, remaining)];
                        int current = indices[i];
                        int next = indices[(i + 1) % remaining];

                        Vector2 a = points[previous];
                        Vector2 b = points[current];
                        Vector2 c = points[next];

                        // 凹点不能作为耳尖（按逆时针绕行，凸点叉积为正）。
                        if (MonoUtil.Cross(a, b, c) <= MonoUtil.Epsilon)
                            continue;

                        bool containsOther = false;
                        for (int j = 0; j < remaining; j++)
                        {
                            int test = indices[j];
                            if (test == previous || test == current || test == next)
                                continue;
                            if (PointInTriangle(points[test], a, b, c))
                            {
                                containsOther = true;
                                break;
                            }
                        }

                        if (containsOther)
                            continue;

                        triangles[triangleIndex++] = ToInput(previous);
                        triangles[triangleIndex++] = ToInput(current);
                        triangles[triangleIndex++] = ToInput(next);

                        for (int j = i; j < remaining - 1; j++)
                            indices[j] = indices[j + 1];
                        remaining--;
                        earFound = true;
                        break;
                    }

                    // 找不到耳（自交或数值病态），直接收尾，避免死循环。
                    if (!earFound)
                        break;
                }

                if (remaining >= 3)
                {
                    // 把剩余环摊平成绕行一致的三角形。
                    for (int i = 1; i < remaining - 1; i++)
                    {
                        Vector2 a = points[indices[0]];
                        Vector2 b = points[indices[i]];
                        Vector2 c = points[indices[i + 1]];
                        if (MonoUtil.Cross(a, b, c) >= 0f)
                        {
                            triangles[triangleIndex++] = ToInput(indices[0]);
                            triangles[triangleIndex++] = ToInput(indices[i]);
                            triangles[triangleIndex++] = ToInput(indices[i + 1]);
                        }
                        else
                        {
                            triangles[triangleIndex++] = ToInput(indices[0]);
                            triangles[triangleIndex++] = ToInput(indices[i + 1]);
                            triangles[triangleIndex++] = ToInput(indices[i]);
                        }
                    }
                }

                if (triangleIndex == triangles.Length)
                    return triangles;

                int[] trimmed = new int[triangleIndex];
                Array.Copy(triangles, trimmed, triangleIndex);
                return trimmed;
            }

            /// <summary>点是否在三角形内（含边界），按容差判断。</summary>
            /// <param name="point">查询点。</param>
            /// <param name="a">三角形顶点 A。</param>
            /// <param name="b">三角形顶点 B。</param>
            /// <param name="c">三角形顶点 C。</param>
            public static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
            {
                float d1 = MonoUtil.Cross(a, b, point);
                float d2 = MonoUtil.Cross(b, c, point);
                float d3 = MonoUtil.Cross(c, a, point);

                bool hasNegative = d1 < -MonoUtil.Epsilon || d2 < -MonoUtil.Epsilon || d3 < -MonoUtil.Epsilon;
                bool hasPositive = d1 > MonoUtil.Epsilon || d2 > MonoUtil.Epsilon || d3 > MonoUtil.Epsilon;
                return !(hasNegative && hasPositive);
            }

            /// <summary>顶点是否按数学逆时针绕行。</summary>
            /// <param name="vertices">多边形顶点。</param>
            public static bool IsCounterClockwise(ReadOnlySpan<Vector2> vertices)
            {
                float sum = 0f;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector2 a = vertices[i];
                    Vector2 b = vertices[(i + 1) % vertices.Length];
                    sum += a.X * b.Y - b.X * a.Y;
                }
                return sum > 0f;
            }

            /// <summary>
            /// 多边形的内缩（正）或外扩（负）。
            /// 采用逐顶点斜接（miter）法，斜接长度超过
            /// <paramref name="miterLimit"/> 倍偏移量时限制其长度，避免尖角爆炸。
            /// 非凸多边形大幅外扩可能自交，本方法不做自交修复。
            /// </summary>
            /// <param name="vertices">多边形顶点。</param>
            /// <param name="distance">偏移距离，正为内缩。</param>
            /// <param name="miterLimit">斜接比例上限，默认 2。</param>
            public static MonoPolygon OffsetPolygon(ReadOnlySpan<Vector2> vertices, float distance, float miterLimit = 2f)
            {
                int count = vertices.Length;
                if (count < 3 || Math.Abs(distance) <= MonoUtil.Epsilon)
                    return new MonoPolygon(vertices.ToArray(), false);

                float sign = IsCounterClockwise(vertices) ? 1f : -1f;
                Vector2[] result = new Vector2[count];

                for (int i = 0; i < count; i++)
                {
                    Vector2 previous = vertices[MonoUtil.Mod(i - 1, count)];
                    Vector2 current = vertices[i];
                    Vector2 next = vertices[(i + 1) % count];

                    Vector2 normalIn = MonoUtil.Perpendicular(MonoUtil.SafeNormalize(current - previous, Vector2.UnitX)) * sign;
                    Vector2 normalOut = MonoUtil.Perpendicular(MonoUtil.SafeNormalize(next - current, Vector2.UnitX)) * sign;

                    Vector2 bisector = normalIn + normalOut;
                    float bisectorLength = bisector.Length();
                    if (bisectorLength <= MonoUtil.Epsilon)
                    {
                        // 180° 折返，无法求角平分线：沿单侧法线平移。
                        result[i] = current + normalOut * distance;
                        continue;
                    }

                    bisector /= bisectorLength;
                    float cosHalf = Vector2.Dot(bisector, normalOut);
                    result[i] = Math.Abs(cosHalf) <= 1f / miterLimit
                        ? current + bisector * (distance * miterLimit)
                        : current + bisector * (distance / Math.Max(Math.Abs(cosHalf), MonoUtil.Epsilon));
                }

                return new MonoPolygon(result, false);
            }

            /// <summary>
            /// 按弧长把多边形边界重采样为 <paramref name="count"/> 个点。
            /// 用于把「疏密不均的碰撞点」变成均匀的绘制/物理采样点。
            /// </summary>
            /// <param name="vertices">多边形顶点。</param>
            /// <param name="count">目标点数，至少 3。</param>
            public static MonoPolygon ResamplePolygon(ReadOnlySpan<Vector2> vertices, int count)
            {
                int requested = Math.Max(count, 3);
                int sourceCount = vertices.Length;
                if (sourceCount < 2)
                    return new MonoPolygon(vertices.ToArray(), false);

                float perimeter = 0f;
                for (int i = 0; i < sourceCount; i++)
                    perimeter += Vector2.Distance(vertices[i], vertices[(i + 1) % sourceCount]);

                if (perimeter <= MonoUtil.Epsilon)
                    return new MonoPolygon(vertices.ToArray(), false);

                Vector2[] result = new Vector2[requested];
                float step = perimeter / requested;

                int edgeIndex = 0;
                float walked = 0f;
                MonoSegment edge = new(vertices[0], vertices[1 % sourceCount]);
                float edgeLength = edge.Length;
                float edgeStart = 0f;

                for (int i = 0; i < requested; i++)
                {
                    float target = i * step;
                    while (target - edgeStart > edgeLength && edgeIndex < sourceCount - 1)
                    {
                        walked += edgeLength;
                        edgeIndex++;
                        edge = new MonoSegment(vertices[edgeIndex], vertices[(edgeIndex + 1) % sourceCount]);
                        edgeLength = edge.Length;
                        edgeStart = walked;
                    }

                    float localT = edgeLength <= MonoUtil.Epsilon ? 0f : (target - edgeStart) / edgeLength;
                    result[i] = edge.PointAt(MathHelper.Clamp(localT, 0f, 1f));
                }

                return new MonoPolygon(result, false);
            }

            /// <summary>
            /// Chaikin 角切细分：每轮把每条边替换为靠近端点的两个内插点，收敛到一条二次 B 样条。
            /// 适合把粗糙的碰撞多边形变成圆滑的绘制轮廓。
            /// </summary>
            /// <param name="vertices">多边形顶点。</param>
            /// <param name="iterations">迭代轮数，0 表示原样返回。</param>
            public static MonoPolygon ChaikinSmooth(ReadOnlySpan<Vector2> vertices, int iterations = 1)
            {
                if (vertices.Length < 3 || iterations <= 0)
                    return new MonoPolygon(vertices.ToArray(), false);

                Vector2[] current = vertices.ToArray();
                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    int count = current.Length;
                    Vector2[] next = new Vector2[count * 2];
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 a = current[i];
                        Vector2 b = current[(i + 1) % count];
                        next[i * 2] = a * 0.75f + b * 0.25f;
                        next[i * 2 + 1] = a * 0.25f + b * 0.75f;
                    }
                    current = next;
                }

                return new MonoPolygon(current, false);
            }

            /// <summary>用鞋带公式求一组顶点的有符号面积（逆时针为正）。</summary>
            /// <param name="vertices">多边形顶点。</param>
            public static float SignedArea(ReadOnlySpan<Vector2> vertices)
            {
                float sum = 0f;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector2 a = vertices[i];
                    Vector2 b = vertices[(i + 1) % vertices.Length];
                    sum += a.X * b.Y - b.X * a.Y;
                }
                return sum * 0.5f;
            }

            /// <summary>顶点平均值。</summary>
            /// <param name="vertices">多边形顶点。</param>
            public static Vector2 AverageVertex(ReadOnlySpan<Vector2> vertices)
            {
                if (vertices.Length == 0)
                    return Vector2.Zero;
                Vector2 sum = Vector2.Zero;
                for (int i = 0; i < vertices.Length; i++)
                    sum += vertices[i];
                return sum / vertices.Length;
            }
        }
    }
}

