using System.Runtime.CompilerServices;

namespace Monochrome.Common.MonoUtil.Mathematics.Statistics
{
    /// <summary>
    /// 噪声发生器。用静态方法直接调用即为「无状态、按坐标哈希」的版本；
    /// 也可以构造实例来持有一张置换表（略快，且易于按种子切换）。
    /// </summary>
    public sealed class MonoNoise
    {
        private const int PermutationSize = 256;
        private const int PermutationMask = 255;

        private readonly byte[] _permutation = new byte[PermutationSize * 3];

        /// <summary>用种子构造带置换表的噪声发生器。</summary>
        /// <param name="seed">种子。</param>
        public MonoNoise(int seed)
        {
            byte[] source = new byte[PermutationSize];
            for (int i = 0; i < PermutationSize; i++)
                source[i] = (byte)i;

            MonoRandom random = new((ulong)seed);
            for (int i = PermutationSize - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                (source[i], source[j]) = (source[j], source[i]);
            }

            // 表长取 3 倍：`Perm(Perm(x) + y)` 的下标最大可达 255 + 255，必须留足余量。
            for (int i = 0; i < PermutationSize * 3; i++)
                _permutation[i] = source[i & PermutationMask];
        }

        #region 基础工具
        /// <summary>整点哈希到 [0,1)。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Hash(int x, int y, int seed) => MonoUtil.FloatAt(x, y, seed);

        /// <summary>Perlin 的五次淡入淡出曲线。</summary>
        /// <param name="t">输入值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        /// <summary>线性插值。</summary>
        /// <param name="a">起点。</param>
        /// <param name="b">终点。</param>
        /// <param name="t">系数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>由哈希值选出 8 个均匀方向之一（Perlin 梯度）。</summary>
        private static Vector2 Gradient(int hash)
        {
            // 8 个方向：4 个轴向 + 4 个对角，已归一化以避免幅值不均。
            const float diagonal = 0.70710678f;
            switch (hash & 7)
            {
                case 0: return new Vector2(1f, 0f);
                case 1: return new Vector2(-1f, 0f);
                case 2: return new Vector2(0f, 1f);
                case 3: return new Vector2(0f, -1f);
                case 4: return new Vector2(diagonal, diagonal);
                case 5: return new Vector2(-diagonal, diagonal);
                case 6: return new Vector2(diagonal, -diagonal);
                default: return new Vector2(-diagonal, -diagonal);
            }
        }

        private int Perm(int index) => _permutation[index & PermutationMask];

        private int HashWithTable(int x, int y) => Perm(Perm(x) + y);
        #endregion

        #region Perlin
        /// <summary>二维 Perlin 噪声，输出约在 [-1,1]。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        public float Perlin(float x, float y)
        {
            int xi = (int)MathF.Floor(x);
            int yi = (int)MathF.Floor(y);
            float xf = x - xi;
            float yf = y - yi;

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = HashWithTable(xi, yi);
            int ab = HashWithTable(xi, yi + 1);
            int ba = HashWithTable(xi + 1, yi);
            int bb = HashWithTable(xi + 1, yi + 1);

            float x1 = Lerp(DotGradient(aa, xf, yf), DotGradient(ba, xf - 1f, yf), u);
            float x2 = Lerp(DotGradient(ab, xf, yf - 1f), DotGradient(bb, xf - 1f, yf - 1f), u);
            return Lerp(x1, x2, v);
        }

        private static float DotGradient(int hash, float x, float y)
        {
            Vector2 gradient = Gradient(hash);
            return gradient.X * x + gradient.Y * y;
        }

        /// <summary>二维 Perlin 噪声（无状态版本，直接用坐标与种子哈希）。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        public static float Perlin(float x, float y, int seed)
        {
            int xi = (int)MathF.Floor(x);
            int yi = (int)MathF.Floor(y);
            float xf = x - xi;
            float yf = y - yi;

            float u = Fade(xf);
            float v = Fade(yf);

            float x1 = Lerp(DotGradient(HashToByte(xi, yi, seed), xf, yf), DotGradient(HashToByte(xi + 1, yi, seed), xf - 1f, yf), u);
            float x2 = Lerp(DotGradient(HashToByte(xi, yi + 1, seed), xf, yf - 1f), DotGradient(HashToByte(xi + 1, yi + 1, seed), xf - 1f, yf - 1f), u);
            return Lerp(x1, x2, v);
        }

        private static int HashToByte(int x, int y, int seed) => (int)(MonoUtil.Hash(x, y, seed) & 0xff);
        #endregion

        #region Simplex
        private static readonly float SimplexF2 = 0.5f * (MathF.Sqrt(3f) - 1f);
        private static readonly float SimplexG2 = (3f - MathF.Sqrt(3f)) / 6f;

        private static readonly Vector2[] SimplexGradients =
        {
            new(1f, 1f), new(-1f, 1f), new(1f, -1f), new(-1f, -1f),
            new(1f, 0f), new(-1f, 0f), new(1f, 0f), new(-1f, 0f),
            new(0f, 1f), new(0f, -1f), new(0f, 1f), new(0f, -1f),
        };

        /// <summary>二维 Simplex 噪声，输出约在 [-1,1]。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        public float Simplex(float x, float y)
        {
            float skew = (x + y) * SimplexF2;
            int i = (int)MathF.Floor(x + skew);
            int j = (int)MathF.Floor(y + skew);

            float unskew = (i + j) * SimplexG2;
            float x0 = x - (i - unskew);
            float y0 = y - (j - unskew);

            int i1;
            int j1;
            if (x0 > y0)
            {
                i1 = 1;
                j1 = 0;
            }
            else
            {
                i1 = 0;
                j1 = 1;
            }

            float x1 = x0 - i1 + SimplexG2;
            float y1 = y0 - j1 + SimplexG2;
            float x2 = x0 - 1f + 2f * SimplexG2;
            float y2 = y0 - 1f + 2f * SimplexG2;

            int gi0 = Perm(i + Perm(j)) % 12;
            int gi1 = Perm(i + i1 + Perm(j + j1)) % 12;
            int gi2 = Perm(i + 1 + Perm(j + 1)) % 12;

            float n0 = Corner(x0, y0, gi0);
            float n1 = Corner(x1, y1, gi1);
            float n2 = Corner(x2, y2, gi2);

            return 70f * (n0 + n1 + n2);
        }

        private static float Corner(float x, float y, int gradientIndex)
        {
            float t = 0.5f - x * x - y * y;
            if (t < 0f)
                return 0f;
            t *= t;
            Vector2 gradient = SimplexGradients[gradientIndex];
            return t * t * (gradient.X * x + gradient.Y * y);
        }

        /// <summary>二维 Simplex 噪声（无状态版本）。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        public static float Simplex(float x, float y, int seed)
        {
            float skew = (x + y) * SimplexF2;
            int i = (int)MathF.Floor(x + skew);
            int j = (int)MathF.Floor(y + skew);

            float unskew = (i + j) * SimplexG2;
            float x0 = x - (i - unskew);
            float y0 = y - (j - unskew);

            int i1 = x0 > y0 ? 1 : 0;
            int j1 = x0 > y0 ? 0 : 1;

            float x1 = x0 - i1 + SimplexG2;
            float y1 = y0 - j1 + SimplexG2;
            float x2 = x0 - 1f + 2f * SimplexG2;
            float y2 = y0 - 1f + 2f * SimplexG2;

            int gi0 = (int)(MonoUtil.Hash(i, j, seed) % 12);
            int gi1 = (int)(MonoUtil.Hash(i + i1, j + j1, seed) % 12);
            int gi2 = (int)(MonoUtil.Hash(i + 1, j + 1, seed) % 12);

            return 70f * (Corner(x0, y0, gi0) + Corner(x1, y1, gi1) + Corner(x2, y2, gi2));
        }
        #endregion

        #region Worley / Cellular
        /// <summary>Worley 噪声，返回最近的特征点距离（0 为特征点中心）。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        public static float Worley(float x, float y, int seed) => WorleyInternal(x, y, seed, out _);

        /// <summary>Worley 噪声，并输出第二近的特征点距离（用于求「到边界的距离」）。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        /// <param name="secondNearest">第二近的特征点距离。</param>
        public static float Worley(float x, float y, int seed, out float secondNearest)
            => WorleyInternal(x, y, seed, out secondNearest);

        private static float WorleyInternal(float x, float y, int seed, out float secondNearest)
        {
            int cellX = (int)MathF.Floor(x);
            int cellY = (int)MathF.Floor(y);

            float nearest = float.MaxValue;
            secondNearest = float.MaxValue;

            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    int cx = cellX + offsetX;
                    int cy = cellY + offsetY;
                    Vector2 feature = new(
                        cx + MonoUtil.FloatAt(cx, cy, seed),
                        cy + MonoUtil.FloatAt(cx, cy, seed ^ 0x5bf03635));

                    float distance = Vector2.Distance(new Vector2(x, y), feature);
                    if (distance < nearest)
                    {
                        secondNearest = nearest;
                        nearest = distance;
                    }
                    else if (distance < secondNearest)
                    {
                        secondNearest = distance;
                    }
                }
            }

            return nearest;
        }

        /// <summary>Cellular 样式：返回 <c>第二近 - 最近</c>，值越小越接近细胞边界，适合做裂纹与泡沫。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        public static float Cellular(float x, float y, int seed)
        {
            float nearest = WorleyInternal(x, y, seed, out float second);
            return second - nearest;
        }

        /// <summary>Value 噪声：格点随机值 + 双线性平滑插值。比 Perlin 便宜，观感更「块状」。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        public static float Value(float x, float y, int seed)
        {
            int xi = (int)MathF.Floor(x);
            int yi = (int)MathF.Floor(y);
            float xf = x - xi;
            float yf = y - yi;

            float u = Fade(xf);
            float v = Fade(yf);

            float a = MonoUtil.FloatAt(xi, yi, seed);
            float b = MonoUtil.FloatAt(xi + 1, yi, seed);
            float c = MonoUtil.FloatAt(xi, yi + 1, seed);
            float d = MonoUtil.FloatAt(xi + 1, yi + 1, seed);

            return Lerp(Lerp(a, b, u), Lerp(c, d, u), v) * 2f - 1f;
        }
        #endregion

        #region 分形与派生物
        /// <summary>分形布朗运动（多倍频程叠加）。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="octaves">倍频程数。</param>
        /// <param name="seed">种子。</param>
        /// <param name="gain">每层的幅值衰减。</param>
        /// <param name="lacunarity">每层的频率倍增。</param>
        /// <param name="useSimplex">为 true 时用 Simplex 作为基函数，否则用 Perlin。</param>
        public static float Fbm(float x, float y, int octaves, int seed = 0, float gain = 0.5f, float lacunarity = 2f, bool useSimplex = false)
        {
            octaves = Math.Max(octaves, 1);
            float amplitude = 1f;
            float frequency = 1f;
            float sum = 0f;
            float normalization = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sample = useSimplex
                    ? Simplex(x * frequency, y * frequency, seed + i * 131)
                    : Perlin(x * frequency, y * frequency, seed + i * 131);

                sum += sample * amplitude;
                normalization += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return normalization <= MonoUtil.Epsilon ? 0f : sum / normalization;
        }

        /// <summary>脊状噪声：把 FBM 取绝对值再翻折，得到锐利的山脊线，适合地形与火焰。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="octaves">倍频程数。</param>
        /// <param name="seed">种子。</param>
        /// <param name="gain">每层的幅值衰减。</param>
        /// <param name="lacunarity">每层的频率倍增。</param>
        public static float Ridged(float x, float y, int octaves, int seed = 0, float gain = 0.5f, float lacunarity = 2f)
        {
            octaves = Math.Max(octaves, 1);
            float amplitude = 1f;
            float frequency = 1f;
            float sum = 0f;
            float normalization = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sample = Perlin(x * frequency, y * frequency, seed + i * 131);
                sample = 1f - Math.Abs(sample);
                sample *= sample;
                sum += sample * amplitude;
                normalization += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return normalization <= MonoUtil.Epsilon ? 0f : sum / normalization * 2f - 1f;
        }

        /// <summary>
        /// 湍流：累加绝对值噪声，产生「烟雾/云」的翻滚质感。
        /// </summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="octaves">倍频程数。</param>
        /// <param name="seed">种子。</param>
        /// <param name="gain">每层的幅值衰减。</param>
        public static float Turbulence(float x, float y, int octaves, int seed = 0, float gain = 0.5f)
        {
            octaves = Math.Max(octaves, 1);
            float amplitude = 1f;
            float frequency = 1f;
            float sum = 0f;
            float normalization = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += Math.Abs(Perlin(x * frequency, y * frequency, seed + i * 131)) * amplitude;
                normalization += amplitude;
                amplitude *= gain;
                frequency *= 2f;
            }

            return normalization <= MonoUtil.Epsilon ? 0f : sum / normalization;
        }

        /// <summary>
        /// 域扭曲：用一层噪声去扰动另一层噪声的采样坐标，得到「流体/大理石」般的纹理。
        /// </summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="strength">扭曲强度。</param>
        /// <param name="seed">种子。</param>
        /// <param name="octaves">FBM 倍频程数。</param>
        public static float DomainWarp(float x, float y, float strength = 1f, int seed = 0, int octaves = 3)
        {
            float warpX = Fbm(x, y, octaves, seed);
            float warpY = Fbm(x + 5.2f, y + 1.3f, octaves, seed + 1);
            return Fbm(x + warpX * strength, y + warpY * strength, octaves, seed + 2);
        }
        #endregion

        #region 实例接口
        /// <summary>用实例的置换表采样 Perlin。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="octaves">倍频程数，1 表示原始 Perlin。</param>
        /// <param name="gain">每层的幅值衰减。</param>
        /// <param name="lacunarity">每层的频率倍增。</param>
        public float SampleFbm(float x, float y, int octaves = 1, float gain = 0.5f, float lacunarity = 2f)
        {
            octaves = Math.Max(octaves, 1);
            float amplitude = 1f;
            float frequency = 1f;
            float sum = 0f;
            float normalization = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += Perlin(x * frequency, y * frequency) * amplitude;
                normalization += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return normalization <= MonoUtil.Epsilon ? 0f : sum / normalization;
        }
        #endregion

        /// <summary>无状态 Perlin（等价于 <see cref="Perlin(float, float, int)"/>），便于当函数指针用。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="seed">种子。</param>
        public static float SamplePerlin(float x, float y, int seed) => Perlin(x, y, seed);
    }
}
