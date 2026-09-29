using Monochrome.Common.MonoUtil.Mathematics.Geometry;
using Monochrome.Common.MonoUtil.Mathematics.Statistics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil
{
    /// <summary>
    /// 采样分布：加权随机、洗牌、蓄水池抽样、泊松盘、蓝噪声、Halton 序列。
    /// </summary>
    public static partial class MonoUtil
    {
        #region 选择
        /// <summary>
        /// 加权随机选择。权重之和不必为 1；全部非正时返回 -1。
        /// </summary>
        /// <param name="weights">权重数组。</param>
        /// <param name="random">随机流。</param>
        /// <returns>被选中的索引，未选中时为 -1。</returns>
        public static int WeightedPick(ReadOnlySpan<float> weights, ref MonoRandom random)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
                total += MathF.Max(weights[i], 0f);

            if (total <= MonoUtil.Epsilon)
                return -1;

            float target = random.NextFloat() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                target -= MathF.Max(weights[i], 0f);
                if (target <= 0f)
                    return i;
            }
            return weights.Length - 1;
        }

        /// <summary>加权随机选择（按概率返回对应元素）。</summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="items">候选元素。</param>
        /// <param name="weights">与元素一一对应的权重。</param>
        /// <param name="random">随机流。</param>
        public static T WeightedPick<T>(IReadOnlyList<T> items, ReadOnlySpan<float> weights, ref MonoRandom random)
        {
            if (items is null || items.Count == 0)
                return default;

            int index = WeightedPick(weights, ref random);
            return index < 0 ? default : items[index];
        }

        /// <summary>按概率分布累计构建别名表（O(1) 抽样），适合大量重复抽样。</summary>
        /// <param name="weights">权重数组。</param>
        public static MonoAliasTable BuildAliasTable(ReadOnlySpan<float> weights)
        {
            int count = weights.Length;
            float[] probability = new float[count];
            int[] alias = new int[count];
            if (count == 0)
                return new MonoAliasTable(probability, alias);

            float total = 0f;
            for (int i = 0; i < count; i++)
                total += MathF.Max(weights[i], 0f);

            if (total <= MonoUtil.Epsilon)
            {
                for (int i = 0; i < count; i++)
                    probability[i] = 1f / count;
                return new MonoAliasTable(probability, alias);
            }

            float[] scaled = new float[count];
            for (int i = 0; i < count; i++)
                scaled[i] = MathF.Max(weights[i], 0f) * count / total;

            Stack<int> small = new();
            Stack<int> large = new();
            for (int i = 0; i < count; i++)
            {
                if (scaled[i] < 1f)
                    small.Push(i);
                else
                    large.Push(i);
            }

            while (small.Count > 0 && large.Count > 0)
            {
                int low = small.Pop();
                int high = large.Pop();
                probability[low] = scaled[low];
                alias[low] = high;
                scaled[high] = scaled[high] + scaled[low] - 1f;
                if (scaled[high] < 1f)
                    small.Push(high);
                else
                    large.Push(high);
            }

            while (large.Count > 0)
            {
                int index = large.Pop();
                probability[index] = 1f;
            }

            while (small.Count > 0)
            {
                int index = small.Pop();
                probability[index] = 1f;
            }

            return new MonoAliasTable(probability, alias);
        }

        /// <summary>Fisher-Yates 原地洗牌。</summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="items">待洗牌列表。</param>
        /// <param name="random">随机流。</param>
        public static void Shuffle<T>(IList<T> items, ref MonoRandom random)
        {
            if (items is null)
                return;
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        /// <summary>Fisher-Yates 洗牌（数组版）。</summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="items">待洗牌数组。</param>
        /// <param name="random">随机流。</param>
        public static void Shuffle<T>(T[] items, ref MonoRandom random)
        {
            if (items is null)
                return;
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        /// <summary>
        /// 蓄水池抽样：在只遍历一次、且不知道总量的情况下等概率取出 <paramref name="sampleCount"/> 个元素。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="sampleCount">抽样数量。</param>
        /// <param name="random">随机流。</param>
        public static List<T> ReservoirSample<T>(IEnumerable<T> source, int sampleCount, ref MonoRandom random)
        {
            List<T> reservoir = new(Math.Max(sampleCount, 0));
            if (source is null || sampleCount <= 0)
                return reservoir;

            int seen = 0;
            foreach (T item in source)
            {
                seen++;
                if (reservoir.Count < sampleCount)
                {
                    reservoir.Add(item);
                    continue;
                }

                int index = random.NextInt(seen);
                if (index < sampleCount)
                    reservoir[index] = item;
            }

            return reservoir;
        }
        #endregion

        #region 低差异序列
        /// <summary>Halton 序列的第 <paramref name="index"/> 项（指定进制）。</summary>
        /// <param name="index">序号，从 0 或 1 开始均可（0 返回 0）。</param>
        /// <param name="baseValue">进制，通常取质数（2、3、5、7…）。</param>
        public static float Halton(int index, int baseValue)
        {
            if (index <= 0 || baseValue < 2)
                return 0f;

            float result = 0f;
            float fraction = 1f / baseValue;
            int i = index;
            while (i > 0)
            {
                result += fraction * (i % baseValue);
                i /= baseValue;
                fraction /= baseValue;
            }
            return result;
        }

        /// <summary>二维 Halton 点（进制 2 与 3），用于把 N 个粒子撒得比纯随机更均匀。</summary>
        /// <param name="index">序号。</param>
        public static Vector2 Halton2D(int index) => new(Halton(index, 2), Halton(index, 3));

        /// <summary>生成 <paramref name="count"/> 个二维 Halton 点，并映射到给定矩形内。</summary>
        /// <param name="count">点数。</param>
        /// <param name="bounds">目标矩形。</param>
        /// <param name="offset">序号偏移，用于把连续调用拼成一条长序列而不重复。</param>
        public static Vector2[] HaltonSet(int count, MonoAABB bounds, int offset = 0)
        {
            count = Math.Max(count, 0);
            Vector2[] points = new Vector2[count];
            Vector2 size = bounds.Size;
            for (int i = 0; i < count; i++)
            {
                Vector2 unit = Halton2D(offset + i + 1);
                points[i] = bounds.Min + new Vector2(unit.X * size.X, unit.Y * size.Y);
            }
            return points;
        }

        /// <summary>生成黄金比低差异序列（R2 序列），比 Halton 更快且分布同样均匀。</summary>
        /// <param name="index">序号。</param>
        public static Vector2 R2Sequence(int index)
        {
            const float alphaX = 0.7548776662466927f;
            const float alphaY = 0.5698402909980532f;
            return new Vector2(
                MonoUtil.Mod(index * alphaX, 1f),
                MonoUtil.Mod(index * alphaY, 1f));
        }
        #endregion

        #region 泊松盘
        /// <summary>
        /// 泊松盘采样（Bridson 算法）。比纯随机视觉上均匀得多，适合粒子初始分布与掉落点布置。
        /// </summary>
        /// <param name="bounds">采样区域。</param>
        /// <param name="minimumDistance">任意两点的最小间距。</param>
        /// <param name="attempts">每个活动点的尝试次数，建议 20~30。</param>
        /// <param name="seed">种子。</param>
        /// <param name="maxPoints">点数上限，避免病态参数下无限循环。</param>
        public static List<Vector2> PoissonDisk(MonoAABB bounds, float minimumDistance, int attempts = 24, int seed = 0, int maxPoints = 4096)
        {
            List<Vector2> points = new();
            if (minimumDistance <= MonoUtil.Epsilon || bounds.Size.X <= 0f || bounds.Size.Y <= 0f)
                return points;

            MonoRandom random = new(seed);
            float cellSize = minimumDistance / MathF.Sqrt(2f);
            int gridWidth = Math.Max(1, (int)MathF.Ceiling(bounds.Size.X / cellSize));
            int gridHeight = Math.Max(1, (int)MathF.Ceiling(bounds.Size.Y / cellSize));
            int[] grid = new int[gridWidth * gridHeight];
            Array.Fill(grid, -1);

            List<int> active = new();

            Vector2 first = new(
                random.NextFloat(bounds.Min.X, bounds.Max.X),
                random.NextFloat(bounds.Min.Y, bounds.Max.Y));
            points.Add(first);
            active.Add(0);
            grid[GridIndex(first, bounds, cellSize, gridWidth, gridHeight)] = 0;

            float minimumSquared = minimumDistance * minimumDistance;

            while (active.Count > 0)
            {
                int activeIndex = random.NextInt(active.Count);
                int pointIndex = active[activeIndex];
                Vector2 origin = points[pointIndex];
                bool accepted = false;

                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    float angle = random.NextFloat(TwoPi);
                    float radius = minimumDistance * MathF.Sqrt(random.NextFloat(1f, 4f));
                    Vector2 candidate = origin + angle.ToRotationVector2() * radius;

                    if (!bounds.Contains(candidate))
                        continue;

                    if (IsFarEnough(candidate, points, grid, bounds, cellSize, gridWidth, gridHeight, minimumSquared))
                    {
                        points.Add(candidate);
                        active.Add(points.Count - 1);
                        grid[GridIndex(candidate, bounds, cellSize, gridWidth, gridHeight)] = points.Count - 1;
                        accepted = true;
                        break;
                    }
                }

                if (!accepted)
                {
                    active[activeIndex] = active[^1];
                    active.RemoveAt(active.Count - 1);
                }

                if (points.Count >= maxPoints)
                    break;
            }

            return points;
        }

        private static bool IsFarEnough(
            Vector2 candidate,
            List<Vector2> points,
            int[] grid,
            MonoAABB bounds,
            float cellSize,
            int gridWidth,
            int gridHeight,
            float minimumSquared)
        {
            int cellX = (int)((candidate.X - bounds.Min.X) / cellSize);
            int cellY = (int)((candidate.Y - bounds.Min.Y) / cellSize);

            int minX = Math.Max(cellX - 2, 0);
            int maxX = Math.Min(cellX + 2, gridWidth - 1);
            int minY = Math.Max(cellY - 2, 0);
            int maxY = Math.Min(cellY + 2, gridHeight - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int index = grid[y * gridWidth + x];
                    if (index < 0)
                        continue;
                    if (Vector2.DistanceSquared(candidate, points[index]) < minimumSquared)
                        return false;
                }
            }

            return true;
        }

        private static int GridIndex(Vector2 point, MonoAABB bounds, float cellSize, int gridWidth, int gridHeight)
        {
            int x = Math.Clamp((int)((point.X - bounds.Min.X) / cellSize), 0, gridWidth - 1);
            int y = Math.Clamp((int)((point.Y - bounds.Min.Y) / cellSize), 0, gridHeight - 1);
            return y * gridWidth + x;
        }
        #endregion

        #region 蓝噪声
        /// <summary>
        /// 蓝噪声抖动阈值：0..N 的整数序号抖动，用于「同一帧的 N 个粒子错开更新/绘制」。
        /// 返回 <paramref name="index"/> 对应的错位值。
        /// </summary>
        /// <param name="index">粒子序号。</param>
        /// <param name="period">错位周期。</param>
        /// <param name="seed">种子。</param>
        public static int BlueNoiseOffset(int index, int period, int seed = 0)
        {
            if (period <= 1)
                return 0;
            return (int)(MonoUtil.Hash((uint)index * 0x9e3779b9u ^ (uint)seed) % (uint)period);
        }

        /// <summary>在给定矩形内撒 <paramref name="count"/> 个「近似蓝噪声」点（R2 序列 + 抖动）。</summary>
        /// <param name="count">点数。</param>
        /// <param name="bounds">目标矩形。</param>
        /// <param name="jitter">抖动强度（相对格子尺寸），0 得到规则的 R2 序列。</param>
        /// <param name="seed">种子。</param>
        public static Vector2[] BlueNoiseSet(int count, MonoAABB bounds, float jitter = 0.35f, int seed = 0)
        {
            count = Math.Max(count, 0);
            Vector2[] points = new Vector2[count];
            Vector2 size = bounds.Size;
            MonoRandom random = new(seed);

            for (int i = 0; i < count; i++)
            {
                Vector2 unit = R2Sequence(i + 1);
                if (jitter > 0f)
                {
                    unit.X += random.NextFloat(-jitter, jitter) / MathF.Max(count, 1f);
                    unit.Y += random.NextFloat(-jitter, jitter) / MathF.Max(count, 1f);
                    unit = MonoUtil.Saturate(unit);
                }
                points[i] = bounds.Min + new Vector2(unit.X * size.X, unit.Y * size.Y);
            }

            return points;
        }
        #endregion

        /// <summary>在圆周上均匀分布 <paramref name="count"/> 个点。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        /// <param name="count">点数。</param>
        /// <param name="rotation">起始角偏移。</param>
        public static Vector2[] CirclePoints(Vector2 center, float radius, int count, float rotation = 0f)
        {
            count = Math.Max(count, 0);
            Vector2[] points = new Vector2[count];
            for (int i = 0; i < count; i++)
                points[i] = center + (rotation + TwoPi * i / count).ToRotationVector2() * radius;
            return points;
        }

        /// <summary>在圆内随机分布 <paramref name="count"/> 个点（面积均匀）。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        /// <param name="count">点数。</param>
        /// <param name="seed">种子。</param>
        public static Vector2[] CircleScatter(Vector2 center, float radius, int count, int seed = 0)
        {
            count = Math.Max(count, 0);
            Vector2[] points = new Vector2[count];
            MonoRandom random = new(seed);
            for (int i = 0; i < count; i++)
                points[i] = center + random.NextVector2InCircle(radius);
            return points;
        }

        /// <summary>在环带内随机分布 <paramref name="count"/> 个点（面积均匀）。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="innerRadius">内半径。</param>
        /// <param name="outerRadius">外半径。</param>
        /// <param name="count">点数。</param>
        /// <param name="seed">种子。</param>
        public static Vector2[] RingScatter(Vector2 center, float innerRadius, float outerRadius, int count, int seed = 0)
        {
            count = Math.Max(count, 0);
            Vector2[] points = new Vector2[count];
            MonoRandom random = new(seed);
            float innerSquared = innerRadius * innerRadius;
            float outerSquared = outerRadius * outerRadius;

            for (int i = 0; i < count; i++)
            {
                float radius = MathF.Sqrt(random.NextFloat(innerSquared, outerSquared));
                points[i] = center + random.NextFloat(TwoPi).ToRotationVector2() * radius;
            }
            return points;
        }

    }

    /// <summary>
    /// 别名表（Walker/Vose alias method）。构建 O(n)，抽样 O(1)，适合「每帧大量抽样」的掉落表。
    /// </summary>
    public sealed class MonoAliasTable
    {
        private readonly float[] _probability;
        private readonly int[] _alias;

        /// <summary>构造别名表。</summary>
        /// <param name="probability">每个槽位的概率（0~1）。</param>
        /// <param name="alias">每个槽位的别名索引。</param>
        public MonoAliasTable(float[] probability, int[] alias)
        {
            _probability = probability ?? Array.Empty<float>();
            _alias = alias ?? Array.Empty<int>();
        }

        /// <summary>表长。</summary>
        public int Count => _probability.Length;

        /// <summary>O(1) 抽样。</summary>
        /// <param name="random">随机流。</param>
        public int Sample(ref MonoRandom random)
        {
            if (_probability.Length == 0)
                return -1;

            int column = random.NextInt(_probability.Length);
            return random.NextFloat() < _probability[column] ? column : _alias[column];
        }
    }
}
