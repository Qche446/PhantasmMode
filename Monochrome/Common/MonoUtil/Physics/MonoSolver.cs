namespace Monochrome.Common.MonoUtil.Physics;

/// <summary>
/// Verlet 积分 + PBD 约束投影的统一质点求解器。绳索、布条、软体共用这一份实现，差别只在约束拓扑。
/// <para>
/// <b>时间步是 tick，不接受 dt。</b> tML 的世界更新本来就是固定 1/60 秒，每 tick 调一次
/// <see cref="Step"/>，"固定时间步"这条纪律由调用位置保证。蓝图 §10.1 里 <c>Step(float dt)</c> 的形态
/// 在这里行不通：接受一个可变 dt 等于把"掉帧就爆炸、切出窗口绳索散架"重新引进来。
/// </para>
/// <para>
/// <b>热路径零分配。</b> 位置、上一帧位置、逆质量三条数组在构造时一次分配；
/// <see cref="Step"/> 只读写这三条数组与约束表，约束表在装配期建好、运行期只读。
/// </para>
/// <para>
/// <b>坐标系无关。</b> 它只认识 <see cref="Vector2"/>，不读 <c>Main</c>、不碰瓦片。
/// 瓦片碰撞由调用方在 <see cref="Step"/> 之外注入（见后续的碰撞约束），所以这一层能进离线验收台。
/// </para>
/// </summary>
public sealed class MonoSolver
{
    private readonly Vector2[] positions;
    private readonly Vector2[] previous;
    private readonly float[] inverseMass;
    private readonly Vector2[] rest;
    private readonly List<MonoConstraint> constraints = [];

    /// <summary>建立一个 <paramref name="count"/> 个质点的求解器，初始质量与位置都为零。</summary>
    /// <param name="count">质点数量，至少 1。</param>
    public MonoSolver(int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "质点数量至少为 1。");

        positions = new Vector2[count];
        previous = new Vector2[count];
        inverseMass = new float[count];
        rest = new Vector2[count];
        Array.Fill(inverseMass, 1f);
    }

    /// <summary>质点数量。</summary>
    public int Count => positions.Length;

    /// <summary>当前位置数组。索引即质点编号，<see cref="Step"/> 直接读写它。</summary>
    public Vector2[] Positions => positions;

    /// <summary>上一帧位置数组。速度由它与 <see cref="Positions"/> 的差算出。</summary>
    public Vector2[] Previous => previous;

    /// <summary>逆质量数组。<c>0</c> 表示该质点被钉住，约束投影与积分都不会移动它。</summary>
    public float[] InverseMasses => inverseMass;

    /// <summary>每 tick 施加的加速度（像素 / tick²）。重力与外力都加在这里。</summary>
    public Vector2 Acceleration;

    /// <summary>速度保留系数，<c>1</c> 为不衰减。每 tick 与位移相乘，所以 <c>0.98</c> 大约两百 tick 衰减到两成。</summary>
    public float Damping = 1f;

    /// <summary>单 tick 位移上限（像素）。防止一帧的巨大位移把点甩穿瓦片。<c>0</c> 表示不限制。</summary>
    public float MaxStep = 16f;

    /// <summary>约束投影的迭代次数。次数越多越硬也越贵，绳索常用 4–12。</summary>
    public int Iterations = 8;

    /// <summary>本 tick 允许的约束投影次数上限，用于全局预算。默认不限制。</summary>
    public int ProjectionBudget = int.MaxValue;

    /// <summary>上一次 <see cref="Step"/> 实际执行的约束投影次数，供预算统计与诊断读取。</summary>
    public int LastProjections { get; private set; }

    /// <summary>上一次 <see cref="Step"/> 实际跑完的迭代轮数。预算提前叫停时会小于 <see cref="Iterations"/>。</summary>
    public int LastIterations { get; private set; }

    /// <summary>上一次 <see cref="Step"/> 是否触发过数值自愈。</summary>
    public bool LastStepUnstable { get; private set; }

    /// <summary>累计触发过多少次数值自愈。正常游玩路径上它应当恒为 0。</summary>
    public int InstabilityCount { get; private set; }

    /// <summary>约束数量。</summary>
    public int ConstraintCount => constraints.Count;

    /// <summary>只读的约束表，供诊断枚举。</summary>
    public IReadOnlyList<MonoConstraint> Constraints => constraints;

    /// <summary>追加一条约束。装配期调用，运行期只读。</summary>
    /// <param name="constraint">要追加的约束。</param>
    public void AddConstraint(MonoConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        constraints.Add(constraint);
    }

    /// <summary>清空约束表。</summary>
    public void ClearConstraints() => constraints.Clear();

    /// <summary>钉住一个质点：它的逆质量归零，积分与全部约束投影都推不动它。</summary>
    /// <param name="index">质点编号。</param>
    public void Pin(int index) => inverseMass[index] = 0f;

    /// <summary>解除钉住，恢复单位质量的逆质量。</summary>
    /// <param name="index">质点编号。</param>
    public void Unpin(int index) => inverseMass[index] = 1f;

    /// <summary>该质点是否被钉住。</summary>
    /// <param name="index">质点编号。</param>
    public bool IsPinned(int index) => inverseMass[index] <= 0f;

    /// <summary>把质点放到指定位置，同时把上一帧位置也设成它（速度归零，不会把速度带进去）。</summary>
    /// <param name="index">质点编号。</param>
    /// <param name="position">目标位置。</param>
    public void SetPosition(int index, Vector2 position)
    {
        positions[index] = position;
        previous[index] = position;
    }

    /// <summary>把质点放到指定位置并给它一个速度，用于"锚点带着绳子甩"这类场合。</summary>
    /// <param name="index">质点编号。</param>
    /// <param name="position">目标位置。</param>
    /// <param name="velocity">该质点的速度（上一帧位置 = 位置 − 速度）。</param>
    public void SetPosition(int index, Vector2 position, Vector2 velocity)
    {
        positions[index] = position;
        previous[index] = position - velocity;
    }

    /// <summary>读取一个质点的位置。</summary>
    /// <param name="index">质点编号。</param>
    public Vector2 Position(int index) => positions[index];

    /// <summary>读取一个质点的速度（由当前与上一帧位置的差算出，已含阻尼）。</summary>
    /// <param name="index">质点编号。</param>
    public Vector2 Velocity(int index) => (positions[index] - previous[index]) * Damping;

    /// <summary>把所有质点的速度归零（把上一帧位置对齐到当前位置）。</summary>
    public void StopMotion()
    {
        for (int i = 0; i < positions.Length; i++)
            previous[i] = positions[i];
    }

    /// <summary>
    /// 把全部质点整体平移一段位移，位置、上一帧位置、静止参考位置一起走。
    /// 三个数组一起动是关键：只挪位置的话，平移量会被下一帧的积分当成速度再走一遍。
    /// <para>
    /// 用途是"瞬移"——锚点在一个 tick 里跳了几十像素时，让链子整体跟过去，
    /// 而不是让距离约束把链子拽着转过去（那一段看起来就是瞬移）。
    /// </para>
    /// </summary>
    /// <param name="delta">平移量。</param>
    public void Translate(Vector2 delta)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] += delta;
            previous[i] += delta;
            rest[i] += delta;
        }
    }

    /// <summary>
    /// 把当前位置记成"静止参考位置"。数值自愈会回到它。
    /// 装配期或 <c>Reset</c> 之后调用一次。
    /// </summary>
    public void CaptureRest()
    {
        for (int i = 0; i < positions.Length; i++)
            rest[i] = positions[i];
    }

    /// <summary>把所有质点恢复到最近一次 <see cref="CaptureRest"/> 记下的位置。</summary>
    public void RestoreRest()
    {
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = rest[i];
            previous[i] = rest[i];
        }
    }

    /// <summary>
    /// 推进一个 tick：位置积分 → 约束投影迭代 → 数值自愈。
    /// <para>
    /// 自愈条件目前只有"位置或上一帧位置成了 NaN / 无穷"。速度异常（数值有限但离谱）的判定
    /// 留给调用方在 <see cref="Step"/> 之后读 <see cref="Velocity"/> 自己做——它需要知道
    /// "多快算离谱"，而那是拓扑参数。
    /// </para>
    /// </summary>
    public void Step()
    {
        Integrate();

        // 自愈要跑两趟：积分之后一趟拦住坏值，投影之后再一趟收拾被约束传染开的坏值。
        // 只跑一趟的话，一个 NaN 会在同一次投影里被距离约束写进邻居，然后再被写回来，
        // 最终一个坏点会把整条链拖成一片坏点。两趟各是 O(质点) 的扫描，代价可以忽略。
        bool unstable = Sanitize();
        ProjectConstraints();
        if (Sanitize())
            unstable = true;

        if (unstable)
            InstabilityCount++;

        LastStepUnstable = unstable;
    }

    /// <summary>位置积分：Verlet 位移 + 阻尼 + 加速度，位移上限由 <see cref="MaxStep"/> 封顶。</summary>
    private void Integrate()
    {
        Vector2 acceleration = Acceleration;
        float damping = Damping;
        float maxStep = MaxStep;
        float maxStepSqr = maxStep * maxStep;

        for (int i = 0; i < positions.Length; i++)
        {
            if (inverseMass[i] <= 0f)
                continue;

            Vector2 step = (positions[i] - previous[i]) * damping;
            if (maxStep > 0f && step.LengthSquared() > maxStepSqr)
                step = MonoUtil.SafeNormalize(step, Vector2.Zero) * maxStep;

            previous[i] = positions[i];
            positions[i] += step + acceleration;
        }
    }

    /// <summary>约束投影迭代。命中预算时提前收工，并把实际轮数与投影次数记下来。</summary>
    private void ProjectConstraints()
    {
        int constraintCount = constraints.Count;
        int budget = ProjectionBudget;
        LastProjections = 0;
        LastIterations = 0;

        for (int iteration = 0; iteration < Iterations; iteration++)
        {
            LastIterations++;

            for (int c = 0; c < constraintCount; c++)
            {
                if (LastProjections >= budget)
                    return;

                constraints[c].Project(positions, inverseMass);
                LastProjections++;
            }
        }
    }

    /// <summary>数值自愈：把非有限的位置拉回静止参考位置。返回本次是否修过东西，计数由 <see cref="Step"/> 负责。</summary>
    private bool Sanitize()
    {
        bool unstable = false;

        for (int i = 0; i < positions.Length; i++)
        {
            if (IsFinite(positions[i]) && IsFinite(previous[i]))
                continue;

            positions[i] = rest[i];
            previous[i] = rest[i];
            unstable = true;
        }

        return unstable;
    }

    /// <summary>两个分量都是有限值。NaN 与正负无穷都算非有限。</summary>
    /// <param name="value">要检查的矢量。</param>
    private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
