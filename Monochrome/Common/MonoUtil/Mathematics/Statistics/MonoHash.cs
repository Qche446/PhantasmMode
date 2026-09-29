using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Monochrome.Common.MonoUtil.MonoUtil;

namespace Monochrome.Common.MonoUtil
{
    /// <summary>
    /// 确定性哈希与伪随机流。同一输入永远得到同一输出，无需存储。
    /// </summary>
    public static partial class MonoUtil
    {
        /// <summary>
        /// 整数/坐标哈希：把坐标与种子映射为一个确定性的 32 位随机数。
        /// 典型用途是「同一块瓦片永远得到同一扰动值」。
        /// </summary>
        /// <summary>32 位整数混淆（MurmurHash3 的 finalizer）。</summary>
        /// <param name="value">输入值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hash(uint value)
        {
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return value;
        }

        /// <summary>二维坐标 + 种子的哈希。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hash(int x, int y, int seed)
            => Hash((uint)x * 0x45d9f3b7u ^ (uint)y * 0x27d4eb2fu ^ (uint)seed * 0x165667b1u);

        /// <summary>三维坐标 + 种子的哈希（用于体素或分帧抖动）。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="z">Z 坐标。</param>
        /// <param name="seed">种子。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hash(int x, int y, int z, int seed)
            => Hash(Hash((uint)x ^ (uint)y * 0x9e3779b9u) ^ (uint)z * 0x85ebca6bu ^ (uint)seed);

        /// <summary>把哈希结果映射到 [0,1)。</summary>
        /// <param name="value">哈希值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(uint value) => (value & 0xffffffu) * (1f / 16777216f);

        /// <summary>二维坐标哈希并映射到 [0,1)。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FloatAt(int x, int y, int seed) => ToFloat(Hash(x, y, seed));

        /// <summary>二维坐标哈希并映射到 [min,max)。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        /// <param name="min">下界。</param>
        /// <param name="max">上界。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RangeAt(int x, int y, int seed, float min, float max)
            => min + FloatAt(x, y, seed) * (max - min);

        /// <summary>由任意值派生一个方向确定的单位矢量（例如按格子生成朝向）。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 DirectionAt(int x, int y, int seed) => (FloatAt(x, y, seed) * TwoPi).ToRotationVector2();

    }

    
}
