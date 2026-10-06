using static Monochrome.Common.MonoUtil.MonoUtil;
using System.Collections.Generic;

namespace Monochrome.Common.MonoUtil.Mathematics.Statistics
{
    /// <summary>
    /// 确定性随机流（PCG32：64 位 LCG 状态 + XSH-RR 输出变换，O'Neill 2014）。
    /// <para>
    /// <b>它是全库唯一的随机数生成器。</b>L1 核心层与这里的数学层曾经各有一份实现
    /// （<c>Core.MonoRngStream</c> 与 <c>MonoRandom</c>）——那不只是重复，而是<b>两套要找各自参考值证明的序列</b>，
    /// 且"随机数和预期不一样"这种故障极难归因。现在合成了一个类型：它同时具备
    /// </para>
    /// <list type="bullet">
    /// <item>派生式构造（<see cref="MonoRng.Derive(ulong, ulong, ulong, ulong)"/> 纯函数算种子），</item>
    /// <item>显式状态（<see cref="State"/> + <see cref="Sequence"/>）——存档与回放靠它精确重建，</item>
    /// <item>溯源（<see cref="Seed"/> + <see cref="Calls"/>）——定位"是不是多取了一次"，</item>
    /// <item>分支（<see cref="Fork(ulong)"/>）——同一次事件里的子事件各走一条流，</item>
    /// <item>无偏取整（<see cref="NextInt(int)"/> 用拒绝阈值 + 重抽，不是 <c>% bound</c>）。</item>
    /// </list>
    /// <para>
    /// <b>它是 <c>struct</c></b>：状态只有 24 字节、可自由复制、不占堆，所以能放进 ECS 组件、数组或别的结构体里。
    /// 代价是<b>推进只发生在你手上这一份</b>——把一个流按值传进方法，方法推进的是副本，
    /// 调用方那一份没动。要改原实例就传 <c>ref</c>（<c>MonoSampling</c> 的各个方法都收 <c>ref MonoRandom</c> 就是这个原因）。
    /// </para>
    /// <para>
    /// 三条硬约束，全部来自多人同步的真实故障：
    /// 不用 <c>Main.rand</c>（全局、与世界状态耦合，两端序列不同）；
    /// 同一件事永远得到同一个数（种子由纯函数派生，不依赖调用顺序）；
    /// 不同用途走不同流（视觉多生成一个粒子不该让掉落物变样）。
    /// </para>
    /// </summary>
    public struct MonoRandom : IEquatable<MonoRandom>
    {
        /// <summary>PCG32 的输出乘数（PCG 论文里的默认常数）。</summary>
        private const ulong Multiplier = 6364136223846793005UL;

        /// <summary>PCG32 的默认步进常数。必须是奇数。</summary>
        private const ulong DefaultIncrement = 1442695040888963407UL;

        private ulong _state;
        private ulong _increment;
        private ulong _seed;
        private ulong _calls;

        /// <summary>
        /// 用一个 64 位种子建一条流：<b>同一个种子永远给出同一条序列</b>。
        /// <para>
        /// 种子建议用 <see cref="MonoRng.Derive(ulong, ulong, ulong, ulong)"/> 算出来，而不是随手写一个数字——
        /// 手写常量意味着"这件事的随机"与世界、实体、时刻都无关，也就无法按帧复现。
        /// </para>
        /// </summary>
        /// <param name="seed">种子。</param>
        public MonoRandom(ulong seed)
        {
            _state = 0UL;
            _seed = seed;
            _calls = 0UL;

            // PCG 的标准播种：先置 0、走一步、加种子、再走一步。
            // 少走一步的后果是"种子只影响高位"——相邻种子的头几个输出会非常接近。
            // 注意：这里用【默认步进常数】走播种那两步，与序列选择无关；真正的 _increment 最后才赋值。
            _increment = DefaultIncrement;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        /// <summary>
        /// 用<b>完整状态</b>建一条流（存档 / 回放确定性模拟用）：能精确重建，而不只是"从同一个种子再来一遍"。
        /// </summary>
        /// <param name="state">内部状态。</param>
        /// <param name="sequence">序列选择值；<b>只有最低位以外有意义的那些位会生效</b>（步进常数必须是奇数）。</param>
        public MonoRandom(ulong state, ulong sequence)
        {
            _state = 0UL;
            _seed = 0UL;
            _calls = 0UL;

            _increment = (sequence << 1) | 1UL;
            NextUInt();
            _state += state;
            NextUInt();
        }

        /// <summary>当前内部状态。</summary>
        public readonly ulong State => _state;

        /// <summary>
        /// 当前步进常数（序列选择值）。
        /// <para>
        /// 它是 <c>(sequence &lt;&lt; 1) | 1</c>：<b>和构造时传进去的 <c>sequence</c> 不是同一个数</b>，
        /// 直接把它当 <c>sequence</c> 存回存档再重建会得到另一条流。存档请连 <see cref="State"/> 一起存 <b>原样的</b> <c>sequence</c>。
        /// </para>
        /// </summary>
        public readonly ulong Sequence => _increment;

        /// <summary>建这条流时用的种子。序列化/回放时用它重建，而不是存内部状态。</summary>
        public readonly ulong Seed => _seed;

        /// <summary>
        /// 取过多少次随机数（播种时的那两次不算）。
        /// <para>
        /// 它的用途很具体：<b>"这一帧的随机数是不是被谁多取了一次"</b>。
        /// 多人不同步最常见的成因就是两条本该一致的流被喂了不同次数的调用。
        /// </para>
        /// </summary>
        public readonly ulong Calls => _calls;

        /// <summary>
        /// 建一条独立的分支流（"同一次事件里还有子事件"，例如一次爆炸里的每个碎片）。
        /// <para>
        /// 它从<b>种子</b>与<b>已经取过的次数</b>派生，<b>不</b>从当前状态派生——所以父流取了多少次数、
        /// 在什么时候建分支，都不影响分支本身。反过来说：同一个父流在同一个 <see cref="Calls"/> 上取两次分支，
        /// 拿到的是同一个分支。
        /// </para>
        /// </summary>
        /// <param name="salt">分支标签。</param>
        public readonly MonoRandom Fork(ulong salt) => new(MonoRng.Derive(_seed, salt, _calls, 0));

        /// <summary>取下一个 32 位无符号随机数（PCG32 + XSH-RR 输出变换）。</summary>
        public uint NextUInt()
        {
            ulong oldState = _state;
            _state = (oldState * Multiplier) + _increment;
            _calls++;

            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rotation = (int)(oldState >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>
        /// 取一个 <c>[0, bound)</c> 的 32 位无符号数，<b>没有模偏差</b>。
        /// <para>
        /// 用的是 PCG 论文里的"拒绝阈值 + 重抽"，而不是 <c>NextUInt() % bound</c>——后者会让前
        /// <c>2^32 mod bound</c> 个值出现得略多。在掉了率这种"必须精确"的地方它是实打实的错误，不是"概率上差不多"。
        /// </para>
        /// </summary>
        /// <param name="bound">上界（不含）。必须 &gt; 0。</param>
        public uint NextUInt(uint bound)
        {
            if (bound == 0)
                throw new ArgumentOutOfRangeException(nameof(bound), bound, "上界必须 > 0。");

            // 阈值 = (2^32 - bound) % bound：低于它的样本落在"被多算的那一段"里，丢弃重抽。
            uint threshold = (uint)((0x1_0000_0000UL - bound) % bound);
            while (true)
            {
                uint value = NextUInt();
                if (value >= threshold)
                    return value % bound;
            }
        }

        /// <summary>生成 <c>[0, bound)</c> 的随机整数。无偏，见 <see cref="NextUInt(uint)"/>。</summary>
        /// <param name="bound">上界（不含）。≤ 0 时返回 0。</param>
        public int NextInt(int bound) => bound <= 0 ? 0 : (int)NextUInt((uint)bound);

        /// <summary>生成 <c>[min, max)</c> 的随机整数。<c>max &lt;= min</c> 时直接返回 <paramref name="min"/>。</summary>
        /// <param name="min">下界（含）。</param>
        /// <param name="max">上界（不含）。</param>
        public int NextInt(int min, int max) => max <= min ? min : min + (int)NextUInt((uint)(max - min));

        /// <summary>生成 <c>[0, 1)</c> 的单精度随机数。<b>只用高 24 位</b>，因为 float 的有效位数就是 24。</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>生成 <c>[0, max)</c> 的单精度随机数。</summary>
        /// <param name="max">上界（不含）。</param>
        public float NextFloat(float max) => NextFloat() * max;

        /// <summary>生成 <c>[min, max)</c> 的单精度随机数。</summary>
        /// <param name="min">下界（含）。</param>
        /// <param name="max">上界（不含）。</param>
        public float NextFloat(float min, float max) => min + (NextFloat() * (max - min));

        /// <summary>生成 <c>[0, 1)</c> 的双精度随机数。</summary>
        public double NextDouble() => NextUInt() * (1.0 / 4294967296.0);

        /// <summary>以给定概率返回 true。<c>chance &lt;= 0</c> 恒 false、<c>&gt;= 1</c> 恒 true。</summary>
        /// <param name="chance">概率，0–1。</param>
        public bool NextBool(float chance = 0.5f)
        {
            if (chance <= 0f)
                return false;
            if (chance >= 1f)
                return true;
            return NextFloat() < chance;
        }

        /// <summary>取一个 <c>-1 或 1</c>。用它代替 <c>NextInt(2) == 0 ? -1 : 1</c>，省得每处都写一遍。</summary>
        public int NextSign() => (NextUInt() & 1) == 0 ? -1 : 1;

        /// <summary>生成单位圆内的随机点（面积均匀，所以半径要开方）。</summary>
        /// <param name="radius">圆半径。</param>
        public Vector2 NextVector2InCircle(float radius = 1f)
        {
            // 开方是必需的：不取平方根的话点会向圆心聚集（面积的均匀分布对应半径的平方分布）。
            float angle = NextFloat(TwoPi);
            float r = MathF.Sqrt(NextFloat()) * radius;
            return new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);
        }

        /// <summary>生成单位圆边界上的随机点。</summary>
        /// <param name="radius">圆半径。</param>
        public Vector2 NextVector2OnCircle(float radius = 1f)
        {
            float angle = NextFloat(TwoPi);
            return new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
        }

        /// <summary>生成单位圆上的随机<b>方向</b>（长度为 1）。</summary>
        public Vector2 NextUnitVector()
        {
            float angle = NextFloat(TwoPi);
            return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }

        /// <summary>生成盒内的随机点。</summary>
        /// <param name="min">下界。</param>
        /// <param name="max">上界。</param>
        public Vector2 NextVector2(Vector2 min, Vector2 max) => new(NextFloat(min.X, max.X), NextFloat(min.Y, max.Y));

        /// <summary>从数组中随机取一个元素。</summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="items">候选数组。</param>
        /// <returns>随机选中的元素；<paramref name="items"/> 为 null 或空时返回 <see langword="default"/>。</returns>
        public T? NextItem<T>(IReadOnlyList<T> items)
        {
            if (items is null || items.Count == 0)
                return default;
            return items[NextInt(items.Count)];
        }

        /// <summary>Box-Muller 变换采样正态分布。</summary>
        /// <param name="standardDeviation">标准差。</param>
        /// <param name="mean">均值。</param>
        public float NextGaussian(float standardDeviation = 1f, float mean = 0f)
        {
            float angle = NextFloat(TwoPi);
            // 下界取 1e-6，避免 log(0) 得到 -Infinity。
            float interpolant = NextFloat(1e-6f, 1f);
            return (MathF.Sqrt(MathF.Log(interpolant) * -2f) * MathF.Cos(angle) * standardDeviation) + mean;
        }

        /// <summary>
        /// 原地洗牌（Fisher–Yates）。
        /// <para>
        /// 遍历方向必须是<b>从后往前</b>、交换对象是 <c>[0, i]</c>。写成"从前往后、和 <c>[i+1, n)</c> 里随便一个换"
        /// 同样能得到"看起来随机"的结果，但<b>每个排列的概率不相等</b>——这是洗牌最常见的错法。
        /// </para>
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="items">要就地打乱的跨度。</param>
        public void Shuffle<T>(Span<T> items)
        {
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        /// <summary>原地洗牌（列表版）。语义与 <see cref="Shuffle{T}(Span{T})"/> 完全一致。</summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="items">要就地打乱的列表；<c>null</c> 是安全空操作。</param>
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

        /// <summary>
        /// 按权重取一个下标（线性扫描）。
        /// <para>
        /// 权重全为 0 或全为负时<b>返回 -1</b>，而不是"随便返回 0"——喂了非法权重却拿到一个看似合理的结果，
        /// 会让调用方以为配置生效了。蓝图 §7.1 记的"权重和 ≤ 0 导致栈空死锁"就是这一类。
        /// </para>
        /// </summary>
        /// <param name="weights">权重表。</param>
        /// <returns>选中的下标；权重非法时是 -1。</returns>
        public int NextWeighted(ReadOnlySpan<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0f)
                    total += weights[i];
            }

            if (total <= 0f)
                return -1;

            float roll = NextFloat() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f)
                    continue;
                roll -= weights[i];
                if (roll < 0f)
                    return i;
            }

            // 浮点累减的尾差：兜底返回最后一个正权重项，而不是 -1。
            for (int i = weights.Length - 1; i >= 0; i--)
            {
                if (weights[i] > 0f)
                    return i;
            }
            return -1;
        }

        /// <inheritdoc/>
        public readonly bool Equals(MonoRandom other)
            => _state == other._state && _increment == other._increment;

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj) => obj is MonoRandom other && Equals(other);

        /// <inheritdoc/>
        public readonly override int GetHashCode() => HashCode.Combine(_state, _increment);

        /// <summary>相等比较（比的是"当前状态与序列"，不是种子）。</summary>
        public static bool operator ==(MonoRandom a, MonoRandom b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        public static bool operator !=(MonoRandom a, MonoRandom b) => !a.Equals(b);

        /// <inheritdoc/>
        public readonly override string ToString() => $"MonoRandom(seed=0x{_seed:X16}, calls={_calls}, state=0x{_state:X16})";
    }

    /// <summary>
    /// 确定性随机的<b>种子派生</b>与<b>用途标签</b>。
    /// <para>
    /// 它只做一件事：把"世界种子 + 用途 + 实体 + 时刻"揉成一个 64 位种子。<b>纯函数</b>——
    /// 同样的四个输入永远得到同样的输出，不依赖调用顺序、不依赖"这是我第几次问它"。
    /// 于是"三号怪在第 120 tick 的掉落"在任何一端都能各自算出同一个值，不需要网络同步。
    /// </para>
    /// <para>
    /// 多人同步的随机必须由服务端产出并通过 <c>ModPacket</c> 广播，或保证同种子同序列；
    /// 这里只提供后者。
    /// </para>
    /// </summary>
    public static class MonoRng
    {
        /// <summary>
        /// 由"世界种子 + 用途 + 实体 + 时刻"派生一个 64 位随机种子。
        /// <para>
        /// 四个输入分别覆盖四种常见需求：<paramref name="worldSeed"/> 让不同存档不一样、
        /// <paramref name="purpose"/> 让不同系统互不干扰、<paramref name="entityId"/> 让不同实体不一样、
        /// <paramref name="tick"/> 让同一个实体在不同时刻不一样。用不到的传 0 即可。
        /// </para>
        /// </summary>
        /// <param name="worldSeed">世界种子（<c>Main.worldSeed</c>）。</param>
        /// <param name="purpose">用途标签，见 <see cref="MonoRngPurpose"/>。</param>
        /// <param name="entityId">实体标识（<c>whoAmI</c>、弹幕序号等）。</param>
        /// <param name="tick">时刻（游戏 tick 数）。</param>
        public static ulong Derive(ulong worldSeed, ulong purpose, ulong entityId, ulong tick)
        {
            // 用乘法与移位把四个域分开，再各自跑一遍 splitmix64 的收尾混合。顺序敏感的混入是关键：
            // 只把它们异或到一起的话，"实体 0 的第 5 tick"与"实体 5 的第 0 tick"会撞车。
            ulong state = worldSeed;
            state = Mix(state ^ (purpose * 0x9E3779B97F4A7C15UL));
            state = Mix(state + (entityId * 0xBF58476D1CE4E5B9UL));
            state = Mix(state ^ (tick * 0x94D049BB133111EBUL));
            return Mix(state);
        }

        /// <summary>派生一个种子并立刻建一条流。等价于 <c>new MonoRandom(Derive(...))</c>。</summary>
        /// <param name="worldSeed">世界种子。</param>
        /// <param name="purpose">用途标签。</param>
        /// <param name="entityId">实体标识。</param>
        /// <param name="tick">时刻。</param>
        public static MonoRandom Stream(ulong worldSeed, ulong purpose, ulong entityId = 0, ulong tick = 0)
            => new(Derive(worldSeed, purpose, entityId, tick));

        /// <summary>splitmix64 的收尾混合函数（Steele 等，2014）。它的作用是让相邻种子产生互不相关的输出。</summary>
        /// <param name="z">输入。</param>
        private static ulong Mix(ulong z)
        {
            z += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    /// <summary>
    /// 用途标签：分流域的键。
    /// <para>
    /// 它只是一个 <c>ulong</c> 常量。之所以做成具名常量而不是随手写数字，是因为<b>写错一个数字的后果是
    /// "某个系统偶尔和另一个系统用了同一个流"</b>——那表现为难以复现的行为漂移，而不是崩溃。
    /// </para>
    /// <para>新增系统时在这里加一行，不要复用已有的值。</para>
    /// </summary>
    public static class MonoRngPurpose
    {
        /// <summary>视觉表现：粒子初速度、抖动、闪光。改它永远不会影响玩法。</summary>
        public const ulong Visual = 0x01;

        /// <summary>纯装饰的细节：背景元素、环境变化。</summary>
        public const ulong Cosmetic = 0x02;

        /// <summary>玩法判定：掉落、暴击、伤害浮动、状态触发概率。这一条必须由服务端结算。</summary>
        public const ulong Gameplay = 0x03;

        /// <summary>程序生成：地形修补、结构摆放。</summary>
        public const ulong Generation = 0x04;

        /// <summary>AI 决策中的"随机权重"（例如加权转换的抽样）。</summary>
        public const ulong Ai = 0x05;

        /// <summary>战斗判定中的随机（弹幕散布、多段命中的分布）。</summary>
        public const ulong Combat = 0x06;

        /// <summary>网络与同步层的抖动、重传延迟。绝不参与玩法。</summary>
        public const ulong Network = 0x07;

        /// <summary>调试与自检：测试用的流，永远不该出现在正式路径里。</summary>
        public const ulong Debug = 0xFF;
    }
}
