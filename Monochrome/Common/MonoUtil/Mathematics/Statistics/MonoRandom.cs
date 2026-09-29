using static Monochrome.Common.MonoUtil.MonoUtil;
using System.Collections.Generic;

namespace Monochrome.Common.MonoUtil.Mathematics.Statistics
{
    /// <summary>
    /// 确定性随机流（PCG32 变体）。
    /// <para>
    /// 与 <c>Terraria.Utilities.UnifiedRandom</c> 的区别：这是 <c>struct</c>、可自由复制、
    /// 状态只有 8 字节，适合放进 ECS 组件或做「多流随机」（渲染一个流、玩法一个流，
    /// 避免渲染消耗随机数导致多人不同步）。
    /// </para>
    /// </summary>
    public struct MonoRandom : IEquatable<MonoRandom>
    {
        private ulong _state;
        private ulong _increment;

        /// <summary>以种子构造。</summary>
        /// <param name="seed">种子。</param>
        public MonoRandom(int seed)
        {
            _state = 0UL;
            _increment = 0x9e3779b97f4a7c15UL;
            NextUInt();
            _state += (ulong)seed;
            NextUInt();
        }

        /// <summary>以完整状态构造（用于存档/回放确定性模拟）。</summary>
        /// <param name="state">内部状态。</param>
        /// <param name="sequence">序列选择值。</param>
        public MonoRandom(ulong state, ulong sequence)
        {
            _state = 0UL;
            _increment = (sequence << 1) | 1UL;
            NextUInt();
            _state += state;
            NextUInt();
        }

        /// <summary>当前内部状态。</summary>
        public ulong State => _state;

        /// <summary>当前序列选择值。</summary>
        public ulong Sequence => _increment;

        /// <summary>生成下一个 32 位随机数。</summary>
        public uint NextUInt()
        {
            ulong oldState = _state;
            _state = oldState * 6364136223846793005UL + _increment;
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rotation = (int)(oldState >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>生成 [0, bound) 的随机整数。</summary>
        /// <param name="bound">上界（不含）。</param>
        public int NextInt(int bound)
        {
            if (bound <= 0)
                return 0;
            return (int)(NextUInt() % (uint)bound);
        }

        /// <summary>生成 [min,max) 的随机整数。</summary>
        /// <param name="min">下界。</param>
        /// <param name="max">上界（不含）。</param>
        public int NextInt(int min, int max)
        {
            if (max <= min)
                return min;
            return min + NextInt(max - min);
        }

        /// <summary>生成 [0,1) 的随机浮点。</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>生成 [0, max) 的随机浮点。</summary>
        /// <param name="max">上界。</param>
        public float NextFloat(float max) => NextFloat() * max;

        /// <summary>生成 [min, max) 的随机浮点。</summary>
        /// <param name="min">下界。</param>
        /// <param name="max">上界。</param>
        public float NextFloat(float min, float max) => min + NextFloat() * (max - min);

        /// <summary>生成单位圆内的随机点。</summary>
        public Vector2 NextVector2InCircle(float radius = 1f)
        {
            // 用平方根校正面积均匀性，避免随机点向圆心聚集。
            float angle = NextFloat(TwoPi);
            float r = MathF.Sqrt(NextFloat()) * radius;
            return angle.ToRotationVector2() * r;
        }

        /// <summary>生成单位圆边界上的随机点。</summary>
        /// <param name="radius">半径。</param>
        public Vector2 NextVector2OnCircle(float radius = 1f) => NextFloat(TwoPi).ToRotationVector2() * radius;

        /// <summary>生成盒内的随机点。</summary>
        /// <param name="min">下界。</param>
        /// <param name="max">上界。</param>
        public Vector2 NextVector2(Vector2 min, Vector2 max) => new(NextFloat(min.X, max.X), NextFloat(min.Y, max.Y));

        /// <summary>以给定概率返回 true。</summary>
        /// <param name="chance">概率，0 恒 false、1 恒 true。</param>
        public bool NextBool(float chance = 0.5f) => NextFloat() < chance;

        /// <summary>从数组中随机取一个元素。</summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="items">候选数组。</param>
        public T NextItem<T>(IReadOnlyList<T> items)
        {
            if (items is null || items.Count == 0)
                return default;
            return items[NextInt(items.Count)];
        }

        /// <summary>Box-Muller 变换采样标准正态分布。</summary>
        /// <param name="standardDeviation">标准差。</param>
        /// <param name="mean">均值。</param>
        public float NextGaussian(float standardDeviation = 1f, float mean = 0f)
        {
            float angle = NextFloat(TwoPi);
            // 下界取 1e-6，避免 log(0) 得到 -Infinity。
            float interpolant = NextFloat(1e-6f, 1f);
            return MathF.Sqrt(MathF.Log(interpolant) * -2f) * MathF.Cos(angle) * standardDeviation + mean;
        }

        /// <summary>Fisher-Yates 洗牌（原地）。</summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="items">待洗牌的列表。</param>
        public void Shuffle<T>(IList<T> items)
        {
            if (items is null)
                return;
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        /// <inheritdoc/>
        public bool Equals(MonoRandom other) => _state == other._state && _increment == other._increment;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is MonoRandom other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(_state, _increment);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoRandom a, MonoRandom b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoRandom a, MonoRandom b) => !a.Equals(b);
    }
}
