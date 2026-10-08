namespace Monochrome.Common.MonoUtil.Physics;

/// <summary>
/// 一条等长链形式的质点绳索：<see cref="Solver"/> 加一串距离约束，其中一个质点钉在锚点上。
/// 旗帜的挂绳、鞭子、触手、飘带都是这个拓扑。
/// <para>
/// <b>锚点默认是编号 0，可以用 <c>anchorIndex</c> 挪到中间</b>：取总段数的 1/3 就是"挂在三分之一处、
/// 前三分之一翘在锚点上方、后三分之二垂下来"的形态。位置列始终按编号顺序暴露在
/// <see cref="Points"/> 上，物理不会为了绘制去重排它。
/// </para>
/// <para>
/// <b>调用形态</b>：每 tick 先 <c>SetAnchor</c> 把锚点摆到当前世界位置，再 <see cref="Step"/>。
/// 步进由 <c>MonoPhysicsSystem</c> 统一驱动，消费者不需要自己算什么时候该走。
/// </para>
/// </summary>
public sealed class MonoRope
{
    /// <summary>默认段长（像素）。约等于一个玩家身高的十几分之一，观感接近布带。</summary>
    public const float DefaultSegmentLength = 6f;

    /// <summary>默认重力加速度（像素 / tick²）。这是视觉量级，不是物理单位。</summary>
    public static readonly Vector2 DefaultGravity = new(0f, 0.35f);

    private readonly int segmentCount;
    private readonly MonoDistanceConstraint[] segments;

    /// <summary>建立一条绳索。<see cref="Reset(Vector2)"/> 会把链子沿重力方向铺开。</summary>
    /// <param name="segmentCount">段数，也是质点数量，至少 2。</param>
    /// <param name="segmentLength">每段长度（像素）。</param>
    /// <param name="anchor">锚点的初始世界位置。</param>
    /// <param name="stiffness">距离约束的刚度，取 0–1。越软越像皮筋。</param>
    /// <param name="bendStiffness">弯曲刚度，<c>0</c> 关掉。开启时给"隔一个"的质点对加一条距离约束，
    /// 用来抑制折叠成尖刺；旗帜与触手用得上，普通挂绳不需要。</param>
    /// <param name="anchorIndex">被钉住的质点编号，默认 <c>0</c>。取中间的编号即可做出两头都垂下来的形态。</param>
    public MonoRope(int segmentCount, float segmentLength, Vector2 anchor, float stiffness = 1f, float bendStiffness = 0f, int anchorIndex = 0)
    {
        if (segmentCount < 2)
            throw new ArgumentOutOfRangeException(nameof(segmentCount), segmentCount, "绳索至少要两个质点。");
        if (segmentLength <= 0f)
            throw new ArgumentOutOfRangeException(nameof(segmentLength), segmentLength, "段长必须为正。");
        if (anchorIndex < 0 || anchorIndex >= segmentCount)
            throw new ArgumentOutOfRangeException(nameof(anchorIndex), anchorIndex, "锚点编号必须落在 [0, 段数) 内。");

        this.segmentCount = segmentCount;
        SegmentLength = segmentLength;
        AnchorIndex = anchorIndex;
        Solver = new MonoSolver(segmentCount) { Acceleration = DefaultGravity };

        segments = new MonoDistanceConstraint[segmentCount - 1];
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = new MonoDistanceConstraint(i, i + 1, segmentLength, stiffness);
            Solver.AddConstraint(segments[i]);
        }

        if (bendStiffness > 0f)
        {
            for (int i = 0; i + 2 < segmentCount; i++)
                Solver.AddConstraint(new MonoDistanceConstraint(i, i + 2, segmentLength * 2f, bendStiffness));
        }

        Solver.Pin(AnchorIndex);
        Solver.Iterations = 36;

        Reset(anchor);
    }

    /// <summary>质点数量。</summary>
    public int SegmentCount => segmentCount;

    /// <summary>被钉住的质点编号。</summary>
    public int AnchorIndex { get; private set; }

    /// <summary>锚点在链子上的归一化位置：<c>0</c> 在一端，<c>1</c> 在另一端。</summary>
    public float AnchorRatio => segmentCount <= 1 ? 0f : AnchorIndex / (float)(segmentCount - 1);

    /// <summary>每段的期望长度（像素）。改它不会自动改已有约束的休息长度。</summary>
    public float SegmentLength { get; private set; }

    /// <summary>锚点的最近一次设定值。</summary>
    public Vector2 Anchor { get; private set; }

    /// <summary>承载积分的求解器。</summary>
    public MonoSolver Solver { get; }

    /// <summary>位置列，编号顺序即物理顺序。</summary>
    public Vector2[] Points => Solver.Positions;

    /// <summary>编号 0 那一端的当前位置。</summary>
    public Vector2 Head => Solver.Positions[0];

    /// <summary>编号最后那一端的当前位置。</summary>
    public Vector2 Tip => Solver.Positions[segmentCount - 1];

    /// <summary>重力/外力加速度（像素 / tick²）。</summary>
    public Vector2 Gravity
    {
        get => Solver.Acceleration;
        set => Solver.Acceleration = value;
    }

    /// <summary>速度保留系数，转发给求解器。</summary>
    public float Damping
    {
        get => Solver.Damping;
        set => Solver.Damping = value;
    }

    /// <summary>约束投影迭代次数，转发给求解器。</summary>
    public int Iterations
    {
        get => Solver.Iterations;
        set => Solver.Iterations = value;
    }

    /// <summary>把锚点设到指定位置。<b>每 tick 在 <see cref="Step"/> 之前调一次。</b>
    /// 位移超过 <see cref="TeleportThreshold"/> 时整条链跟着平移。</summary>
    /// <param name="position">锚点的世界位置。</param>
    public void SetAnchor(Vector2 position)
    {
        FollowDiscontinuity(position);
        Anchor = position;
        Solver.SetPosition(AnchorIndex, position);
    }

    /// <summary>换一个质点当锚点，并把整条链按新锚点重新铺开。</summary>
    /// <param name="index">新的锚点编号。</param>
    public void PinAt(int index)
    {
        if (index < 0 || index >= segmentCount)
            throw new ArgumentOutOfRangeException(nameof(index), index, "锚点编号必须落在 [0, 段数) 内。");
        if (index == AnchorIndex)
            return;

        Solver.Unpin(AnchorIndex);
        AnchorIndex = index;
        Solver.Pin(index);
        Reset(Anchor, Gravity);
    }

    /// <summary>把整条链重新铺开：锚点两侧各按段长向相反方向排直，速度归零，并记下静止参考位置。</summary>
    /// <param name="anchor">锚点的世界位置。</param>
    /// <param name="direction">链子伸出的方向（编号增大的一侧朝这里），内部会归一化；为零向量时按重力方向。</param>
    public void Reset(Vector2 anchor, Vector2 direction)
    {
        Vector2 unit = direction.LengthSquared() > MonoUtil.EpsilonSqr
            ? Vector2.Normalize(direction)
            : MonoUtil.SafeNormalize(Solver.Acceleration, Vector2.UnitY);

        for (int i = 0; i < segmentCount; i++)
            Solver.SetPosition(i, anchor + unit * (SegmentLength * (i - AnchorIndex)));

        Anchor = anchor;
        Solver.CaptureRest();
    }

    /// <summary>按重力方向把链子重新铺开。锚点还在原来的位置。</summary>
    public void Reset() => Reset(Anchor, Gravity);

    /// <summary>按重力方向把链子重新铺开到指定锚点。</summary>
    /// <param name="anchor">锚点的世界位置。</param>
    public void Reset(Vector2 anchor) => Reset(anchor, Gravity);

    /// <summary>推进一个 tick。数值自愈触发时整条链回到铺开状态，避免一次坏算污染成永久 NaN。</summary>
    public void Step()
    {
        Solver.Step();
        if (Solver.LastStepUnstable)
            Reset(Anchor, Gravity);
    }

    /// <summary>
    /// 锚点一个 tick 里挪动超过这个距离（像素）就走"整体平移"而不是让约束去追，<c>0</c> 关闭。
    /// <para>
    /// 它针对两类情形。一类是<b>不连续</b>：锚点带 <c>-10 × direction</c> 的偏移，玩家一转身就在一个 tick 里
    /// 横跳 20 像素；玩家传送同理。另一类是<b>锚点跑赢链子</b>：冲刺速度下约束投影收敛不完，
    /// 实测 24 像素/tick 能把 40 像素的链子拉到 3 倍长并折叠，整体平移则保持形状。
    /// </para>
    /// <para>
    /// 默认 16：正常跑步的头部位移在 8 像素/tick 以内，冲刺在 16 以上，取中间。
    /// 调小会让常态跑步也变成刚性跟随，调大则冲刺时仍会拉伸。
    /// </para>
    /// </summary>
    public float TeleportThreshold = 16f;

    /// <summary>累计触发过多少次"锚点瞬移、整链平移"。它涨得比预期快就说明阈值或锚点算错了。</summary>
    public int TeleportCount { get; private set; }

    /// <summary>锚点单 tick 位移过大时，把整条链整体平移过去。</summary>
    /// <param name="position">新的锚点位置。</param>
    /// <returns>确实判成瞬移时为 true。</returns>
    private bool FollowDiscontinuity(Vector2 position)
    {
        float threshold = TeleportThreshold;
        if (threshold <= 0f)
            return false;

        Vector2 delta = position - Anchor;
        if (delta.LengthSquared() <= threshold * threshold)
            return false;

        Solver.Translate(delta);
        TeleportCount++;
        return true;
    }
}
