using Monochrome.Core.Graphics.Shaders;
using static Monochrome.Common.MonoUtil.MonoUtil;

namespace Monochrome.Core.Graphics.Primitives;

/// <summary>
/// 图元网格渲染器：把拖尾、圆、圆弧、胶囊、多边形、贝塞尔曲线等组装成索引三角形网格，用动态顶点/索引
/// 缓冲一次性提交。顶点携带的截面坐标与像素宽度见 <see cref="MonoPrimitiveVertex"/>。
/// <para>
/// 坐标空间决定要不要在写入顶点前减 <c>Main.screenPosition</c>，投影矩阵同样按空间选择；
/// 这些是绕过 <see cref="SpriteBatch"/> 的原始绘制，必须落在 <c>spriteBatch.End()</c> 与下一次
/// <c>Begin()</c> 之间，批量状态用 <see cref="MonoSpriteBatchExtensions"/> 的助手处理。
/// </para>
/// </summary>
public static class MonoPrimitiveRenderer
{
    #region 容量

    /// <summary>单个网格允许的最大"路径点"数。拖尾 2 顶点/点、圆环 2 顶点/点、圆 3 顶点/段。</summary>
    private const int MaxPoints = 4096;

    /// <summary>顶点缓冲容量。</summary>
    private const int MaxVertices = MaxPoints * 4;

    /// <summary>索引缓冲容量。</summary>
    private const int MaxIndices = MaxPoints * 6;

    #endregion

    #region GPU 资源

    private static MonoPrimitiveVertex[]? vertices;
    private static short[]? indices;
    private static DynamicVertexBuffer? vertexBuffer;
    private static DynamicIndexBuffer? indexBuffer;
    private static BasicEffect? fallbackEffect;

    private static int vertexCount;
    private static int indexCount;

    /// <summary>一条延迟绘制的命令：几何落在缓冲的哪一段，以及它自己的绘制状态。</summary>
    private struct DrawCommand
    {
        public int StartIndex;
        public int IndexCount;
        public MonoShader? Shader;
        public MonoEffectBinding? EffectBinding;
        public BlendState? BlendState;
        public MonoGraphicsSpace Space;
        public int? ProjectionWidth;
        public int? ProjectionHeight;
        public Texture2D? Texture;
        public SamplerState? Sampler;
        public float UseTexture;

        /// <summary>这条命令画完时，着色器写入日志的长度。回放用它切出"这个实体写过什么"。</summary>
        public int WriteEnd;

        /// <summary>录制这条命令时设备采样槽 1 上绑着的贴图。有些消费者直接写设备槽（如 <c>SetTexture1()</c>），靠它把状态带回每条命令。</summary>
        public Texture2D? SnapshotTexture;

        /// <summary>与 <see cref="SnapshotTexture"/> 配套的采样状态。</summary>
        public SamplerState? SnapshotSampler;
    }

    /// <summary>一个块最多攒多少条命令的初始容量。块真正的上限是顶点容量，这里只是预分配。</summary>
    private const int MaxCommands = 512;

    private static readonly List<DrawCommand> commands = new(MaxCommands);
    /// <summary>当前图元从索引缓冲的哪个下标开始，提交时用它切出这条命令的范围。</summary>
    private static int commandIndexStart;
    private static readonly List<MonoShader.Write> pendingShaderWrites = new(16);

    /// <summary>是否处在 <see cref="MonoPrimitiveBatch"/> 的作用域里。</summary>
    private static bool batching;

    private static Matrix cachedMatrix;
    private static int cachedWidth;
    private static int cachedHeight;
    private static MonoGraphicsSpace cachedSpace;
    private static bool hasCachedMatrix;

    private static uint statisticsFrame;
    private static int submissionsThisFrame;
    private static int verticesThisFrame;

    /// <summary>上一 tick 提交的批数。诊断用。</summary>
    public static int LastFrameSubmissions { get; private set; }

    /// <summary>上一 tick 送进 GPU 的顶点数。诊断用。</summary>
    public static int LastFrameVertices { get; private set; }

    /// <summary>当前是否处在批次作用域里。诊断用。</summary>
    public static bool IsBatching => batching;

    /// <summary>重采样、描边、轮廓共用的暂存点列，避免每次绘制都分配数组。</summary>
    private static readonly Vector2[] scratch = new Vector2[MaxPoints];

    /// <summary>滤掉零值槽之后的点列，供重采样的两条分支共用。</summary>
    private static readonly Vector2[] cleanPositions = new Vector2[MaxPoints];

    /// <summary>贴图四边形的四个角，避免每次绘制都分配数组。</summary>
    private static readonly Vector2[] quadCorners = new Vector2[4];

    /// <summary><see cref="RenderTrail(IEnumerable{Vector2}, MonoPrimitiveSettings, int?)"/> 的暂存列表。</summary>
    private static readonly List<Vector2> scratchList = [with(MaxPoints)];

    private static bool initialized;
    private static bool schedulePending;
    private static bool warnedUnavailable;

    /// <summary>生命周期代号：让"卸载时的延迟释放"在重新加载后自动作废，避免释放掉新资源。</summary>
    private static int generation;

    /// <summary>GPU 资源是否已创建。未就绪时所有绘制调用都会安静跳过。</summary>
    public static bool IsReady => initialized;

    #endregion

    #region 生命周期

    /// <summary>
    /// 默认图元着色器的注册名。资产在 <c>Assets/Effects/Primitives/StandardPrimitive.fx</c>，
    /// 与 <c>MonoShader.DefaultPassName</c>（<c>AutoloadPass</c>）配套。
    /// </summary>
    public const string DefaultShaderName = "Monochrome.StandardPrimitive";

    /// <summary>缓存下来的默认着色器包装。它身上只有 <c>Effect</c> 会被热重载替换，所以缓存包装是安全的。</summary>
    private static MonoShader? defaultShader;

    /// <summary>
    /// 取默认图元着色器：不传 <c>shader</c> 的图元会用它。<b>取到就缓存</b>；取不到返回 null，
    /// 调用方回退到 <c>BasicEffect</c>——于是"着色器资产缺失"只让效果退化，不会让图元画不出来。
    /// <para>
    /// <b>失败不缓存</b>：<c>MonoShaderManager</c> 可能在这之后才注册（模组/着色器热重载），
    /// 而字典查询的成本可以忽略，所以每次都重新查一次。
    /// </para>
    /// </summary>
    private static MonoShader? ResolveDefaultShader()
    {
        if (defaultShader is not null && !defaultShader.IsDisposed)
            return defaultShader;
        defaultShader = MonoShaderManager.TryGet(DefaultShaderName, out MonoShader? found) ? found : null;
        return defaultShader;
    }

    /// <summary>排队创建 GPU 资源。设备尚未就绪时会保留"未初始化"状态，由后续绘制调用重试。</summary>
    internal static void Initialize()
    {
        if (Main.dedServ || initialized || schedulePending)
            return;
        schedulePending = true;
        Main.QueueMainThreadAction(CreateDeviceResources);
    }

    /// <summary>排队释放 GPU 资源。会立刻把状态置为未就绪，之后重新加载可再次初始化。</summary>
    internal static void Unload()
    {
        initialized = false;
        schedulePending = false;
        int expected = unchecked(++generation);
        Main.QueueMainThreadAction(() =>
        {
            // 卸载排队期间发生了重新初始化（模组热重载），这次释放已作废。
            if (generation != expected)
                return;
            ReleaseDeviceResources();
            warnedUnavailable = false;
        });
    }

    private static void CreateDeviceResources()
    {
        schedulePending = false;
        if (initialized || Main.dedServ)
            return;

        GraphicsDevice? device = Main.instance?.GraphicsDevice;
        if (device is null)
        {
            // 刻意**不**置位 initialized：Ready() 会在下一帧再排一次，届时设备通常已就绪。
            WarnOnce("图元渲染器：GraphicsDevice 尚未就绪，本次跳过创建，将在后续帧自动重试。");
            return;
        }

        try
        {
            ReleaseDeviceResources();
            vertices = new MonoPrimitiveVertex[MaxVertices];
            indices = new short[MaxIndices];
            vertexBuffer = new DynamicVertexBuffer(device, MonoPrimitiveVertex.Declaration, MaxVertices, BufferUsage.WriteOnly);
            indexBuffer = new DynamicIndexBuffer(device, IndexElementSize.SixteenBits, MaxIndices, BufferUsage.WriteOnly);
            fallbackEffect = new BasicEffect(device) { VertexColorEnabled = true, TextureEnabled = false, LightingEnabled = false };
            initialized = true;
            warnedUnavailable = false;
        }
        catch (Exception ex)
        {
            ReleaseDeviceResources();
            WarnOnce($"图元渲染器：GPU 资源创建失败，图元绘制将不可用（{ex.Message}）。");
        }
    }

    private static void ReleaseDeviceResources()
    {
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        fallbackEffect?.Dispose();
        vertexBuffer = null;
        indexBuffer = null;
        fallbackEffect = null;
        vertices = null;
        indices = null;
        vertexCount = 0;
        indexCount = 0;
        commands.Clear();
        MonoShader.WriteLog = null;
        batching = false;
        hasCachedMatrix = false;
    }

    private static void WarnOnce(string message)
    {
        if (warnedUnavailable)
            return;
        warnedUnavailable = true;
        Monochrome.Instance?.Logger.Warn(message);
    }

    /// <summary>
    /// 是否可以进行绘制。未就绪时**重新排一次资源创建**（<c>schedulePending</c> 保证不堆积），
    /// 因此初始化失败不再是"一次失败就永久失效"——下一帧的绘制调用会自愈。
    /// </summary>
    private static bool Ready()
    {
        if (initialized && vertices is not null && indices is not null && vertexBuffer is not null && indexBuffer is not null && fallbackEffect is not null)
            return true;
        Initialize();
        return false;
    }

    #endregion

    #region 网格组装

    private static void ResetMesh()
    {
        vertexCount = 0;
        indexCount = 0;
        commandIndexStart = 0;
    }

    private static bool HasRoomFor(int vertexNeed, int indexNeed)
        => vertexCount + vertexNeed <= MaxVertices && indexCount + indexNeed <= MaxIndices;

    /// <summary>
    /// 开始一个图元：批次外清空网格，批次里记下它在缓冲中的起点，装不下就先提交当前块。
    /// <para>
    /// <paramref name="vertexNeed"/> / <paramref name="indexNeed"/> 是这个图元要用的量，
    /// 让分块落在图元之间而不是把它劈成两半。
    /// </para>
    /// </summary>
    private static void ReservePrimitive(int vertexNeed, int indexNeed)
    {
        if (!batching)
        {
            ResetMesh();
            return;
        }

        if (vertexCount > 0 && (vertexCount + vertexNeed > MaxVertices || indexCount + indexNeed > MaxIndices))
        {
            // Current primitive's setters ran before ReservePrimitive. Preserve writes that
            // don't belong to the previous command block being submitted now.
            List<MonoShader.Write>? log = MonoShader.WriteLog;
            int consumed = commands.Count > 0 ? commands[^1].WriteEnd : 0;
            pendingShaderWrites.Clear();
            if (log is not null)
                for (int i = consumed; i < log.Count; i++)
                    pendingShaderWrites.Add(log[i]);
            try
            {
                SubmitCommands();
                if (log is not null)
                    log.AddRange(pendingShaderWrites);
            }
            finally { pendingShaderWrites.Clear(); }
        }

        commandIndexStart = indexCount;
    }

    /// <summary>写入一个顶点，返回它的索引。</summary>
    /// <param name="position">屏幕坐标。</param>
    /// <param name="color">顶点颜色。</param>
    /// <param name="length">沿形状的长度/角度参数，写入附加数据的 X。</param>
    /// <param name="coordinate">附加数据的 Y/Z，由 <see cref="MonoPrimitiveVertex"/> 的两个契约函数生成。</param>
    private static int PushVertex(Vector2 position, Color color, float length, Vector2 coordinate)
    {
        vertices![vertexCount] = new MonoPrimitiveVertex(position, color, new Vector3(length, coordinate.X, coordinate.Y));
        return vertexCount++;
    }

    /// <summary>写入一个条带顶点：截面由真实像素宽度描述（拖尾、描边、虚线）。</summary>
    /// <param name="position">屏幕坐标。</param>
    /// <param name="color">顶点颜色。</param>
    /// <param name="length">沿形状的长度参数。</param>
    /// <param name="section"><c>0</c> / <c>1</c> 为条带两侧，<c>0.5</c> 为中线。</param>
    /// <param name="pixelSpan">条带全宽（像素）。</param>
    private static int PushSegmentVertex(Vector2 position, Color color, float length, float section, float pixelSpan)
        => PushVertex(position, color, length, MonoPrimitiveVertex.SegmentCoordinate(section, pixelSpan));

    /// <summary>写入一个归一化顶点：截面本身就是 0→1（圆、圆环、多边形填充、贴图四边形）。</summary>
    /// <param name="position">屏幕坐标。</param>
    /// <param name="color">顶点颜色。</param>
    /// <param name="length">沿形状的长度参数。</param>
    /// <param name="section">截面坐标。</param>
    private static int PushNormalizedVertex(Vector2 position, Color color, float length, float section)
        => PushVertex(position, color, length, MonoPrimitiveVertex.NormalizedCoordinate(section));

    private static void PushTriangle(int a, int b, int c)
    {
        indices![indexCount++] = (short)a;
        indices![indexCount++] = (short)b;
        indices![indexCount++] = (short)c;
    }

    /// <summary>按 <c>a→b→c→d</c> 的绕行写入一个四边形（两个三角形）。</summary>
    private static void PushQuad(int a, int b, int c, int d)
    {
        PushTriangle(a, b, c);
        PushTriangle(a, c, d);
    }

    /// <summary>把世界坐标按空间换算到屏幕坐标。只有 <see cref="MonoGraphicsSpace.World"/> 需要偏移。</summary>
    private static Vector2 ToScreen(Vector2 position, MonoGraphicsSpace space)
        => space == MonoGraphicsSpace.World ? position - Main.screenPosition : position;

    /// <summary>
    /// 沿一串屏幕坐标点构建描边条带（每点两侧各一个顶点，相邻两侧连成四边形）。
    /// <para>
    /// 法线取相邻两段方向的平均（廉价斜接），因此尖角内侧会略微收缩，但**不会产生尖刺**。
    /// 所有位置都必须已经是屏幕坐标。
    /// </para>
    /// </summary>
    /// <param name="points">屏幕坐标点列。</param>
    /// <param name="closed">true 表示首尾相连（多边形描边），false 表示开放折线。</param>
    /// <param name="thickness">条带全宽（像素）。</param>
    /// <param name="colorAt">按 <c>[0,1]</c> 参数取颜色的函数。</param>
    private static void BuildStroke(ReadOnlySpan<Vector2> points, bool closed, float thickness, Func<float, Color> colorAt)
    {
        int count = points.Length;
        if (count < 2)
            return;

        float half = Math.Max(thickness, 0.01f) * 0.5f;
        int segmentCount = closed ? count : count - 1;
        if (!HasRoomFor(count * 2, segmentCount * 6))
            return;

        int baseVertex = vertexCount;
        for (int i = 0; i < count; i++)
        {
            Vector2 previous = points[closed ? (i - 1 + count) % count : Math.Max(i - 1, 0)];
            Vector2 next = points[closed ? (i + 1) % count : Math.Min(i + 1, count - 1)];
            Vector2 forward = SafeNormalize(next - previous, Vector2.UnitX);
            Vector2 normal = new(-forward.Y, forward.X);
            float length = i / (float)Math.Max(count - 1, 1);
            Color color = colorAt(length);
            PushSegmentVertex(points[i] - normal * half, color, length, 0f, thickness);
            PushSegmentVertex(points[i] + normal * half, color, length, 1f, thickness);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int a = baseVertex + i * 2;
            int b = baseVertex + i * 2 + 1;
            int c = baseVertex + ((i + 1) % count) * 2 + 1;
            int d = baseVertex + ((i + 1) % count) * 2;
            PushQuad(a, b, c, d);
        }
    }

    /// <summary>以 <paramref name="centerScreen"/> 为扇心写一个实心三角形扇（圆、退化胶囊共用）。</summary>
    private static void BuildCircleFan(Vector2 centerScreen, float radius, Color color, int segments)
    {
        segments = Math.Clamp(segments, 3, MaxPoints);
        if (!HasRoomFor(segments * 3, segments * 3))
            return;

        int origin = PushNormalizedVertex(centerScreen, color, 0f, 0f);
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            Vector2 point = centerScreen + (MathHelper.TwoPi * t).ToRotationVector2() * radius;
            PushNormalizedVertex(point, color, t, 1f);
        }

        for (int i = 0; i < segments; i++)
            PushTriangle(origin, origin + 1 + i, origin + 1 + (i + 1) % segments);
    }

    #endregion

    #region 提交

    /// <summary>
    /// 收下一个图元：批次里只记一条命令，批次外立刻提交。
    /// </summary>
    /// <param name="shader">可选的自定义着色器；为 null 时走顶点色 <c>BasicEffect</c> 回退。</param>
    /// <param name="blendState">混合状态；为 null 时用 <see cref="BlendState.AlphaBlend"/>。</param>
    /// <param name="space">坐标空间，决定投影矩阵。</param>
    /// <param name="projectionWidth">投影宽度，null 表示 <c>Main.screenWidth</c>。</param>
    /// <param name="projectionHeight">投影高度，null 表示 <c>Main.screenHeight</c>。</param>
    /// <param name="texture">要绑到采样槽 1 的贴图；为 null 时不动槽 1。</param>
    /// <param name="sampler">上面那张贴图的采样状态；为 null 时用 <see cref="SamplerState.LinearClamp"/>。</param>
    /// <param name="useTexture">是否让着色器把槽 1 的贴图乘进颜色。只有 <see cref="RenderQuad"/> 会开。</param>
    /// <param name="effectBinding">可选的 Asset/Effect 绘制绑定，优先于 shader。</param>
    private static void Flush(MonoShader? shader, BlendState? blendState, MonoGraphicsSpace space, int? projectionWidth, int? projectionHeight, Texture2D? texture = null, SamplerState? sampler = null, bool useTexture = false, MonoEffectBinding? effectBinding = null)
    {
        if (vertexCount <= 0 || indexCount <= 0 || !Ready())
        {
            if (!batching)
                ResetMesh();

            return;
        }

        Texture2D? snapshotTexture = null;
        SamplerState? snapshotSampler = null;
        if (batching)
        {
            // 采样槽 1 是"被绘制的那张贴图"的约定位置。录下来，提交时按命令还原，
            // 这样直接写设备槽的消费者（FargoSoulsUtil.SetTexture1 之类）在批次里也不会被后一条覆盖。
            GraphicsDevice device = Main.instance.GraphicsDevice;
            snapshotTexture = device.Textures[1] as Texture2D;
            snapshotSampler = device.SamplerStates[1];
        }

        commands.Add(new DrawCommand
        {
            StartIndex = commandIndexStart,
            IndexCount = indexCount - commandIndexStart,
            Shader = shader,
            EffectBinding = effectBinding,
            BlendState = blendState,
            Space = space,
            ProjectionWidth = projectionWidth,
            ProjectionHeight = projectionHeight,
            Texture = texture,
            Sampler = sampler,
            UseTexture = useTexture ? 1f : 0f,
            WriteEnd = MonoShader.WriteLog?.Count ?? 0,
            SnapshotTexture = snapshotTexture,
            SnapshotSampler = snapshotSampler,
        });

        if (!batching)
            SubmitCommands();
    }

    /// <summary>
    /// 提交攒下的命令：整块只上传一次顶点/索引缓冲，然后按命令顺序各画一次。
    /// <para>
    /// 顺序不变，所以叠色结果与逐个绘制一致；省掉的是每个图元一次的缓冲上传与状态切换。
    /// </para>
    /// </summary>
    private static void SubmitCommands()
    {
        if (commands.Count == 0)
        {
            ResetMesh();
            return;
        }

        int submittedVertices = vertexCount;
        int submittedIndices = indexCount;
        if (submittedVertices <= 0 || submittedIndices <= 0 || !Ready())
        {
            commands.Clear();
            ResetMesh();
            return;
        }

        GraphicsDevice device = Main.instance.GraphicsDevice;
        VertexBufferBinding[] oldVertexBuffers = device.GetVertexBuffers();
        IndexBuffer? oldIndexBuffer = device.Indices;
        RasterizerState oldRasterizer = device.RasterizerState;
        BlendState oldBlend = device.BlendState;
        hasCachedMatrix = false;

        // 提交期间写进去的参数属于框架自己的那几项，不能再记一遍日志。
        bool previousSuppress = MonoShader.SuppressRecording;
        MonoShader.SuppressRecording = true;
        int writeStart = 0;
        try
        {
            vertexBuffer!.SetData(vertices!, 0, submittedVertices, SetDataOptions.Discard);
            indexBuffer!.SetData(indices!, 0, submittedIndices, SetDataOptions.Discard);
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;
            device.RasterizerState = RasterizerState.CullNone;

            for (int i = 0; i < commands.Count; i++)
            {
                DrawCommand command = commands[i];
                if (command.IndexCount <= 0)
                    continue;

                // 没传 shader 就用默认图元着色器；连它也没加载到才回退到 BasicEffect。
                MonoShader? shader = command.EffectBinding is null ? command.Shader ?? ResolveDefaultShader() : null;
                device.BlendState = command.BlendState ?? BlendState.AlphaBlend;
                Matrix matrix = ResolveMatrix(command);

                if (command.EffectBinding is not null)
                {
                    // 混合批次仍消费 MonoShader 日志，避免后一条命令丢失它之前的状态写入。
                    ReplayShaderWrites(writeStart, command.WriteEnd);
                    writeStart = command.WriteEnd;
                    if (command.SnapshotTexture is not null)
                    {
                        device.Textures[1] = command.SnapshotTexture;
                        if (command.SnapshotSampler is not null)
                            device.SamplerStates[1] = command.SnapshotSampler;
                    }
                    if (command.Texture is not null)
                    {
                        device.Textures[1] = command.Texture;
                        device.SamplerStates[1] = command.Sampler ?? SamplerState.LinearClamp;
                    }

                    // 回调设置强类型/原生参数，框架随后设置投影并应用 pass。
                    if (!command.EffectBinding.Apply(matrix, command.Texture is not null && command.UseTexture > 0f))
                        continue;
                }
                else if (shader is not null)
                {
                    // 先还原录制时设备槽 1 的状态（覆盖直接写设备槽的消费者），再按命令绑 settings.Texture。
                    if (command.SnapshotTexture is not null)
                    {
                        device.Textures[1] = command.SnapshotTexture;
                        if (command.SnapshotSampler is not null)
                            device.SamplerStates[1] = command.SnapshotSampler;
                    }

                    // 贴图按命令各自绑：同一块缓冲里可能混着用不同条纹贴图的拖尾。
                    if (command.Texture is not null)
                        shader.SetTexture(command.Texture, 1, command.Sampler ?? SamplerState.LinearClamp);

                    // 回放这个实体在录制期间写过的着色器状态。批次把绘制推迟了，
                    // 而消费者是"边攒边设参数"的：不回放的话，整批会用最后一个实体的参数去画。
                    ReplayShaderWrites(writeStart, command.WriteEnd);
                    writeStart = command.WriteEnd;

                    // useTexture **必须每条都显式写**——MonoShader 的参数带缓存，不写会沿用上一条的值。
                    if (!shader.SetParameter("uWorldViewProjection", matrix)
                          .SetCommonParameters()
                          .SetParameter("useTexture", command.Texture is null ? 0f : command.UseTexture)
                          .Apply())
                        continue;
                }
                else
                {
                    writeStart = command.WriteEnd;

                    fallbackEffect!.World = Matrix.Identity;
                    fallbackEffect.View = Matrix.Identity;
                    fallbackEffect.Projection = matrix;
                    fallbackEffect.TextureEnabled = command.Texture is not null && command.UseTexture > 0f;
                    fallbackEffect.Texture = command.Texture;
                    fallbackEffect.CurrentTechnique.Passes[0].Apply();
                }

                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, submittedVertices, command.StartIndex, command.IndexCount / 3);
            }
        }
        finally
        {
            MonoShader.SuppressRecording = previousSuppress;
            device.SetVertexBuffers(oldVertexBuffers);
            device.Indices = oldIndexBuffer;
            device.RasterizerState = oldRasterizer;
            device.BlendState = oldBlend;
            // 用户参数回调抛异常时也不能遗留命令和参数引用到下一批。
            commands.Clear();
            MonoShader.WriteLog?.Clear();
            ResetMesh();
        }

        CountSubmission(submittedVertices);
    }

    /// <summary>
    /// 回放日志里 <paramref name="from"/>..<paramref name="to"/> 这一段着色器写入。
    /// <para>
    /// 消费者是"边攒边设参数"的（<c>shader.SetTexture</c> / <c>TrySetParameter</c>），批次把绘制推迟之后，
    /// 不回放就会让整批都用最后一个实体写下的状态去画。按命令切段回放，各实体才还是各写各的。
    /// </para>
    /// </summary>
    private static void ReplayShaderWrites(int from, int to)
    {
        List<MonoShader.Write>? log = MonoShader.WriteLog;
        if (log is null)
            return;

        int end = Math.Min(to, log.Count);
        for (int i = Math.Max(0, from); i < end; i++)
        {
            MonoShader.Write write = log[i];
            if (write.IsTexture)
            {
                if (write.Texture is not null)
                    write.Shader.ReplayTexture(write.Slot, write.Texture, write.Sampler);
            }
            else if (write.Name is not null)
            {
                write.Shader.ReplayParameter(write.Name, write.Value);
            }
        }
    }

    /// <summary>同一次提交里投影矩阵通常不变，算一次就复用。</summary>
    private static Matrix ResolveMatrix(in DrawCommand command)
    {
        int width = command.ProjectionWidth ?? Main.screenWidth;
        int height = command.ProjectionHeight ?? Main.screenHeight;
        if (hasCachedMatrix && width == cachedWidth && height == cachedHeight && command.Space == cachedSpace)
            return cachedMatrix;

        cachedMatrix = CalculatePrimitiveMatrix(width, height, command.Space);
        cachedWidth = width;
        cachedHeight = height;
        cachedSpace = command.Space;
        hasCachedMatrix = true;
        return cachedMatrix;
    }

    /// <summary>按 tick 统计提交批数与顶点数，供诊断读取。</summary>
    private static void CountSubmission(int submittedVertices)
    {
        if (statisticsFrame != Main.GameUpdateCount)
        {
            statisticsFrame = Main.GameUpdateCount;
            LastFrameSubmissions = submissionsThisFrame;
            LastFrameVertices = verticesThisFrame;
            submissionsThisFrame = 0;
            verticesThisFrame = 0;
        }

        submissionsThisFrame++;
        verticesThisFrame += submittedVertices;
    }

    /// <summary>
    /// 按坐标空间计算顶点着色器用的世界×视图×投影矩阵。
    /// <para>
    /// <b><see cref="MonoGraphicsSpace.World"/> 这一段与 Luminance 的
    /// <c>CalculatePrimitiveMatrices</c> 逐行一致</b>（俯视 Z 轴 → 平移高度 → 绕 Z 转 180° →
    /// 反转重力翻转 → 乘缩放 → 正交投影），没有理由改动：它已在整个 Calamity/Daybreak 生态里跑过。
    /// 屏幕与 UI 空间复用的是同一套"不含缩放的正交投影"，UI 额外先乘
    /// <c>Main.UIScaleMatrix</c>。
    /// </para>
    /// </summary>
    private static Matrix CalculatePrimitiveMatrix(int width, int height, MonoGraphicsSpace space)
    {
        if (space != MonoGraphicsSpace.World)
        {
            Matrix orthographic = Matrix.CreateOrthographicOffCenter(0f, width, height, 0f, -1f, 1f);
            // 行向量约定：p * (UIScale * Ortho) 即"先换算到屏幕像素，再进裁剪空间"。
            return space == MonoGraphicsSpace.UI ? Main.UIScaleMatrix * orthographic : orthographic;
        }

        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix view = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);
        view *= Matrix.CreateTranslation(0f, -height, 0f);
        view *= Matrix.CreateRotationZ(MathHelper.Pi);
        if (Main.LocalPlayer.gravDir == -1f)
            view *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);
        Matrix zoomScale = Matrix.CreateScale(zoom.X, zoom.Y, 1f);
        view *= zoomScale;
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, width * zoom.X, 0f, height * zoom.Y, 0f, 1f) * zoomScale;
        return view * projection;
    }

    /// <summary>
    /// 把 <see cref="MonoPrimitiveSettings.UseUnscaledMatrix"/> 换算成实际使用的坐标空间。
    /// 像素化目标里的坐标已经减过 <c>Main.screenPosition</c>，必须配不含缩放的正交投影：
    /// 消费者漏写 <c>UseUnscaledMatrix</c> 时在这里兜住，免得整层画到屏幕外。
    /// </summary>
    private static MonoGraphicsSpace EffectiveSpace(MonoPrimitiveSettings settings)
        => EffectiveSpace(settings.UseUnscaledMatrix, settings.Space);

    /// <summary><see cref="EffectiveSpace(MonoPrimitiveSettings)"/> 的组件重载，圆与圆环走这里。</summary>
    private static MonoGraphicsSpace EffectiveSpace(bool useUnscaledMatrix, MonoGraphicsSpace space)
        => useUnscaledMatrix || MonoPixelationSystem.CurrentlyRendering ? MonoGraphicsSpace.Screen : space;

    /// <summary>像素化目标里如果配了缩放矩阵，图元会画到屏幕外；提示一次，绘制仍按无缩放兜住。</summary>
    private static void WarnIfUnscaledMissing(bool useUnscaledMatrix, MonoGraphicsSpace space)
    {
        if (MonoPixelationSystem.CurrentlyRendering && !useUnscaledMatrix && space == MonoGraphicsSpace.World)
            WarnOnce("像素化图元：在 RenderPixelatedPrimitives 里绘制时给图元设置 UseUnscaledMatrix = true；本次已按无缩放矩阵绘制。");
    }

    #endregion

    #region 批次

    /// <summary>
    /// 开一个批次：作用域内的 <c>MonoPrim.*</c> 不再各自提交，而是一起攒到结束时提交。
    /// <para>
    /// 提交时每条命令仍按调用顺序各画一次，叠色结果与逐个绘制一致；省下的是每个图元一次的
    /// 缓冲上传与状态切换。一块装不下（顶点超过 <c>MaxVertices</c>）会先提交当前块再继续。
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">已经处在一个批次里（批次不能嵌套）。</exception>
    public static MonoPrimitiveBatch Batch()
    {
        if (batching)
            throw new InvalidOperationException("MonoPrimitiveBatch 不能嵌套：外面那个批次还没结束。");

        batching = true;
        commands.Clear();
        MonoShader.WriteLog = [with(128)];
        ResetMesh();
        return new MonoPrimitiveBatch();
    }

    /// <summary>提交批次里已经攒下的内容，批次本身仍然有效。</summary>
    internal static void SubmitBatch() => SubmitCommands();

    /// <summary>提交并结束当前批次。重复调用安全。</summary>
    internal static void EndBatch()
    {
        if (!batching)
            return;

        try
        {
            SubmitCommands();
        }
        finally
        {
            // 提交抛异常时也要退出批次状态，否则之后的绘制会一直卡在"批次里"，谁开新批次都会撞异常。
            batching = false;
            MonoShader.WriteLog = null;
        }
    }

    #endregion

    #region 拖尾

    /// <summary>把点列渲染成一条一次提交的索引三角形带。</summary>
    /// <param name="positions">路径点，按 <paramref name="settings"/> 的坐标空间给出。零值点视为"未填充"，重采样前会被滤掉。</param>
    /// <param name="settings">绘制配置。</param>
    /// <param name="pointsToCreate">
    /// 重采样后的点数；越多越细腻也越贵，默认取 <paramref name="positions"/> 的点数。上限 <c>4096</c>。
    /// </param>
    public static void RenderTrail(IReadOnlyList<Vector2> positions, MonoPrimitiveSettings settings, int? pointsToCreate = null)
    {
        if (Main.dedServ || settings is null || positions is null || positions.Count < 2 || !Ready())
            return;

        WarnIfUnscaledMissing(settings.UseUnscaledMatrix, settings.Space);

        int requested = Math.Clamp(pointsToCreate ?? positions.Count, 2, MaxPoints);
        if (!TryBuildPositions(positions, settings, requested, out int actualCount))
            return;

        ReservePrimitive(actualCount * 2, (actualCount - 1) * 6);
        // 索引一律用缓冲里的绝对下标：同一批次里前面可能已经躺了别的图元，从 0 数会画成别人的形状。
        int baseVertex = vertexCount;
        for (int i = 0; i < actualCount; i++)
        {
            float t = i / (float)(actualCount - 1);
            Vector2 current = scratch[i];
            Vector2 tangent = i == actualCount - 1 ? current - scratch[i - 1] : scratch[i + 1] - current;
            tangent = SafeNormalize(tangent, Vector2.UnitX);
            Vector2 normal = new(-tangent.Y, tangent.X);
            // 宽度函数给的是半宽（与 Luminance 的 WidthFunction 同义），条带向两侧各展开这么多。
            float halfWidth = Math.Max(0f, settings.WidthFunction(t));
            float span = halfWidth * 2f;
            Color color = settings.ColorFunction(t);

            Vector2 left = current - normal * halfWidth;
            Vector2 right = current + normal * halfWidth;
            if (i == 0 && settings.InitialVertexPositionsOverride is { } overrides && overrides.Left != Vector2.Zero && overrides.Right != Vector2.Zero)
            {
                left = ToScreen(overrides.Left, settings.Space);
                right = ToScreen(overrides.Right, settings.Space);
            }

            // 截面：左缘 0 → 右缘 1，中心线 0.5；pixelSpan 是该处条带全宽。
            PushSegmentVertex(left, color, t, 0f, span);
            PushSegmentVertex(right, color, t, 1f, span);
        }

        for (int i = 0; i < actualCount - 1; i++)
            PushQuad(baseVertex + i * 2, baseVertex + i * 2 + 1, baseVertex + i * 2 + 3, baseVertex + i * 2 + 2);

        // 拖尾常配"条纹贴图 + 自定义着色器"：贴图由 Flush 记进命令、提交时再绑，
        // 否则同一个批次里的多条拖尾会互相覆盖槽 1。useTexture 保持关——条纹由着色器自己采样。
        Flush(
            settings.Shader,
            settings.BlendState,
            EffectiveSpace(settings),
            settings.ProjectionWidth,
            settings.ProjectionHeight,
            settings.Shader is null && settings.EffectBinding is null ? null : settings.Texture,
            settings.Sampler ?? SamplerState.LinearWrap,
            effectBinding: settings.EffectBinding);
    }

    /// <summary>
    /// <see cref="RenderTrail(IReadOnlyList{Vector2}, MonoPrimitiveSettings, int?)"/> 的惰性序列重载。
    /// 非列表输入会先拷进复用的暂存列表，避免 <c>ToArray()</c> 那样的每次调用分配。
    /// </summary>
    /// <param name="positions">路径点。</param>
    /// <param name="settings">绘制配置。</param>
    /// <param name="pointsToCreate">重采样后的点数。</param>
    public static void RenderTrail(IEnumerable<Vector2> positions, MonoPrimitiveSettings settings, int? pointsToCreate = null)
    {
        if (Main.dedServ || positions is null || settings is null)
            return;
        if (positions is IReadOnlyList<Vector2> list)
        {
            RenderTrail(list, settings, pointsToCreate);
            return;
        }

        scratchList.Clear();
        foreach (Vector2 position in positions)
            scratchList.Add(position);
        RenderTrail(scratchList, settings, pointsToCreate);
    }

    /// <summary>
    /// 把点列重采样成 <paramref name="requested"/> 个屏幕坐标点，写入 <c>scratch</c>。
    /// <para>
    /// 零值槽先滤掉再重采样。调用方常用 <c>Projectile.oldPos</c> 当点列，而它还没被写过的槽就是零值，
    /// 平滑会把这些槽当成真实坐标，把条带一路拉到世界原点（屏幕左上角）去。
    /// </para>
    /// </summary>
    /// <param name="source">原始路径点。</param>
    /// <param name="settings">绘制配置（决定是否样条平滑与偏移函数）。</param>
    /// <param name="requested">期望点数。</param>
    /// <param name="count">实际写出的点数。</param>
    /// <returns>点数足够构成拖尾时为 true。</returns>
    private static bool TryBuildPositions(IReadOnlyList<Vector2> source, MonoPrimitiveSettings settings, int requested, out int count)
    {
        count = 0;

        int cleanCount = 0;
        for (int i = 0; i < source.Count && cleanCount < MaxPoints; i++)
        {
            Vector2 point = source[i];
            if (point != Vector2.Zero)
                cleanPositions[cleanCount++] = point;
        }

        if (cleanCount < 2)
            return false;

        ReadOnlySpan<Vector2> path = cleanPositions.AsSpan(0, cleanCount);

        if (!settings.Smoothen)
        {
            for (int i = 0; i < requested && count < MaxPoints; i++)
            {
                float t = i / (float)(requested - 1);
                int index = Math.Min(cleanCount - 1, (int)(t * (cleanCount - 1)));
                scratch[count++] = ToScreen(path[index] + (settings.OffsetFunction?.Invoke(t) ?? Vector2.Zero), settings.Space);
            }
            return count >= 2;
        }

        for (int i = 0; i < requested && count < MaxPoints; i++)
        {
            float t = i / (float)(requested - 1);
            float scaled = t * (cleanCount - 1);
            int index = Math.Min(cleanCount - 2, (int)scaled);
            float local = scaled - index;
            Vector2 a = index > 0 ? path[index - 1] : path[index] * 2f - path[index + 1];
            Vector2 b = path[index];
            Vector2 c = path[index + 1];
            Vector2 d = index + 2 < cleanCount ? path[index + 2] : path[index + 1] * 2f - path[index];
            scratch[count++] = ToScreen(Vector2.CatmullRom(a, b, c, d, local) + (settings.OffsetFunction?.Invoke(t) ?? Vector2.Zero), settings.Space);
        }
        return count >= 2;
    }

    #endregion

    #region 圆与圆环

    /// <summary>渲染一个实心圆（三角形扇）。</summary>
    /// <param name="center">圆心，按 <paramref name="settings"/> 的坐标空间给出。</param>
    /// <param name="settings">半径与颜色配置。</param>
    /// <param name="sideCount">扇形的切片数；越大边缘越圆滑也越贵。上限 <c>4096</c>。</param>
    public static void RenderCircle(Vector2 center, MonoCircleSettings settings, int sideCount = 128)
    {
        if (Main.dedServ || settings is null || !Ready())
            return;

        WarnIfUnscaledMissing(settings.UseUnscaledMatrix, settings.Space);

        sideCount = Math.Clamp(sideCount, 3, MaxPoints);
        ReservePrimitive(sideCount * 3, sideCount * 3);
        if (!HasRoomFor(sideCount * 3, sideCount * 3))
            return;

        Vector2 centerScreen = ToScreen(center, settings.Space);
        for (int i = 0; i < sideCount; i++)
        {
            float t = i / (float)sideCount;
            float nextT = (i + 1f) / sideCount;
            float radius = Math.Max(0f, settings.RadiusFunction(t));
            float nextRadius = Math.Max(0f, settings.RadiusFunction(nextT));
            Vector2 edge = centerScreen + (MathHelper.TwoPi * t).ToRotationVector2() * radius;
            Vector2 nextEdge = centerScreen + (MathHelper.TwoPi * nextT).ToRotationVector2() * nextRadius;

            // 截面：圆心 0 → 边缘 1；扇形内部靠线性插值得到 d / r，着色器不需要知道半径。
            int v0 = PushNormalizedVertex(centerScreen, settings.ColorFunction(t), t, 0f);
            int v1 = PushNormalizedVertex(edge, settings.ColorFunction(t), t, 1f);
            int v2 = PushNormalizedVertex(nextEdge, settings.ColorFunction(nextT), nextT, 1f);
            PushTriangle(v0, v1, v2);
        }

        Flush(settings.Shader, settings.BlendState, EffectiveSpace(settings.UseUnscaledMatrix, settings.Space), settings.ProjectionWidth, settings.ProjectionHeight, effectBinding: settings.EffectBinding);
    }

    /// <summary>渲染一圈圆形描边（圆环带），环厚由内径向外径单侧展开。</summary>
    /// <param name="center">圆心，按 <paramref name="settings"/> 的坐标空间给出。</param>
    /// <param name="settings">内径、环厚与颜色配置。</param>
    /// <param name="pointCount">环上的分段数；越大越圆滑也越贵。上限 <c>4095</c>。</param>
    public static void RenderCircleEdge(Vector2 center, MonoCircleEdgeSettings settings, int pointCount = 128)
    {
        if (Main.dedServ || settings is null || !Ready())
            return;

        WarnIfUnscaledMissing(settings.UseUnscaledMatrix, settings.Space);

        pointCount = Math.Clamp(pointCount, 3, MaxPoints - 1);
        ReservePrimitive((pointCount + 1) * 2, pointCount * 6);
        if (!HasRoomFor((pointCount + 1) * 2, pointCount * 6))
            return;

        int baseVertex = vertexCount;
        Vector2 centerScreen = ToScreen(center, settings.Space);
        for (int i = 0; i <= pointCount; i++)
        {
            float t = i / (float)pointCount;
            Vector2 radial = (MathHelper.TwoPi * t).ToRotationVector2();
            float radius = Math.Max(0f, settings.RadiusFunction(t));
            float edgeWidth = settings.EdgeWidthFunction(t);
            Color color = settings.ColorFunction(t);

            // 截面：内径 0 → 外径 1。
            PushNormalizedVertex(centerScreen + radial * radius, color, t, 0f);
            PushNormalizedVertex(centerScreen + radial * (radius + edgeWidth), color, t, 1f);
        }

        for (int i = 0; i < pointCount; i++)
            PushQuad(baseVertex + i * 2, baseVertex + i * 2 + 1, baseVertex + i * 2 + 3, baseVertex + i * 2 + 2);

        Flush(settings.Shader, settings.BlendState, EffectiveSpace(settings.UseUnscaledMatrix, settings.Space), settings.ProjectionWidth, settings.ProjectionHeight, effectBinding: settings.EffectBinding);
    }

    #endregion

    #region 贴图四边形

    /// <summary>
    /// 用动态顶点路径渲染一个居中的贴图四边形（带旋转）。适合需要旋转矩阵或自定义着色器的场合；
    /// 普通贴图请直接用 <see cref="SpriteBatch"/>。
    /// </summary>
    /// <param name="texture">贴图。</param>
    /// <param name="center">四边形中心，按 <paramref name="space"/> 给出。</param>
    /// <param name="scale">缩放；四边形尺寸为 <c>贴图尺寸 × scale</c>。</param>
    /// <param name="rotation">绕中心的旋转弧度。</param>
    /// <param name="color">顶点色（会与贴图相乘）。</param>
    /// <param name="shader">自定义着色器；为 null 时走采样贴图的 <c>BasicEffect</c> 回退。</param>
    /// <param name="blendState">混合状态；为 null 时用 <see cref="BlendState.AlphaBlend"/>。</param>
    /// <param name="space">坐标空间。</param>
    /// <param name="projectionWidth">
    /// 投影区域宽度；为 null 时取 <c>Main.screenWidth</c>。把四边形画进<b>非全屏</b>渲染目标时必须给，
    /// 否则投影按屏幕尺寸算，四边形只会盖住目标的一部分（屏幕后处理的分辨率无关性就靠它）。
    /// </param>
    /// <param name="projectionHeight">投影区域高度；为 null 时取 <c>Main.screenHeight</c>。</param>
    public static void RenderQuad(Texture2D texture, Vector2 center, Vector2 scale, float rotation, Color color, MonoShader? shader = null, BlendState? blendState = null, MonoGraphicsSpace space = MonoGraphicsSpace.World, int? projectionWidth = null, int? projectionHeight = null)
        => RenderQuadCore(texture, center, scale, rotation, color, shader, blendState, space, projectionWidth, projectionHeight, null);

    /// <summary>用 Asset/Effect 绑定绘制贴图四边形；坐标、投影与 RenderQuad 相同。独立命名以兼容原来的 null shader 调用。</summary>
    public static void RenderQuadWithEffect(Texture2D texture, Vector2 center, Vector2 scale, float rotation, Color color, MonoEffectBinding effectBinding, BlendState? blendState = null, MonoGraphicsSpace space = MonoGraphicsSpace.World, int? projectionWidth = null, int? projectionHeight = null)
    {
        ArgumentNullException.ThrowIfNull(effectBinding);
        RenderQuadCore(texture, center, scale, rotation, color, null, blendState, space, projectionWidth, projectionHeight, effectBinding);
    }

    private static void RenderQuadCore(Texture2D texture, Vector2 center, Vector2 scale, float rotation, Color color, MonoShader? shader, BlendState? blendState, MonoGraphicsSpace space, int? projectionWidth, int? projectionHeight, MonoEffectBinding? effectBinding)
    {
        if (Main.dedServ || texture is null || !Ready())
            return;

        Vector2 half = texture.Size() * scale * 0.5f;
        quadCorners[0] = new Vector2(-half.X, -half.Y);
        quadCorners[1] = new Vector2(half.X, -half.Y);
        quadCorners[2] = new Vector2(half.X, half.Y);
        quadCorners[3] = new Vector2(-half.X, half.Y);

        Vector2 centerScreen = ToScreen(center, space);

        ReservePrimitive(4, 6);
        int baseVertex = vertexCount;
        for (int i = 0; i < 4; i++)
        {
            // 四边形的 Y 直接就是贴图 v 坐标；Z 写 1，着色器那条还原公式对它不改变。
            float u = i is 1 or 2 ? 1f : 0f;
            float v = i >= 2 ? 1f : 0f;
            PushNormalizedVertex(centerScreen + quadCorners[i].RotatedBy(rotation), color, u, v);
        }
        PushQuad(baseVertex, baseVertex + 1, baseVertex + 2, baseVertex + 3);

        // 贴图与 useTexture 由 Flush 记进命令，提交时再绑。
        Flush(shader, blendState, space, projectionWidth, projectionHeight, texture, SamplerState.LinearClamp, useTexture: true, effectBinding: effectBinding);
    }

    #endregion

    #region 即时图元（蓝图 §11.8）

    /// <summary>画一条线段。</summary>
    /// <param name="start">起点。</param>
    /// <param name="end">终点。</param>
    /// <param name="color">颜色。</param>
    /// <param name="width">线宽（像素）。</param>
    /// <param name="space">坐标空间。</param>
    public static void DrawLine(Vector2 start, Vector2 end, Color color, float width = 1f, MonoGraphicsSpace space = MonoGraphicsSpace.World)
    {
        if (Main.dedServ || !Ready())
            return;
        scratch[0] = ToScreen(start, space);
        scratch[1] = ToScreen(end, space);
        ReservePrimitive(4, 6);
        BuildStroke(scratch.AsSpan(0, 2), closed: false, width, _ => color);
        Flush(null, null, space, null, null);
    }

    /// <summary>画一个空心圆（圆环描边），线条由半径向外展开 <paramref name="width"/> 像素。</summary>
    /// <param name="center">圆心。</param>
    /// <param name="radius">内径。</param>
    /// <param name="color">颜色。</param>
    /// <param name="width">线宽（像素）。</param>
    /// <param name="segments">分段数。</param>
    /// <param name="space">坐标空间。</param>
    public static void DrawCircle(Vector2 center, float radius, Color color, float width = 1f, int segments = 32, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => RenderCircleEdge(center, new MonoCircleEdgeSettings(_ => width, _ => radius, _ => color, Space: space), segments);

    /// <summary>画一段圆弧。</summary>
    /// <param name="center">圆心。</param>
    /// <param name="radius">半径。</param>
    /// <param name="startAngle">起始角（弧度）。</param>
    /// <param name="sweepAngle">张角（弧度，可正可负）。</param>
    /// <param name="color">颜色。</param>
    /// <param name="width">线宽（像素）。</param>
    /// <param name="segments">分段数。</param>
    /// <param name="space">坐标空间。</param>
    public static void DrawArc(Vector2 center, float radius, float startAngle, float sweepAngle, Color color, float width = 1f, int segments = 24, MonoGraphicsSpace space = MonoGraphicsSpace.World)
    {
        if (Main.dedServ || !Ready())
            return;

        segments = Math.Clamp(segments, 1, (MaxPoints - 2) / 2);
        Vector2 centerScreen = ToScreen(center, space);
        for (int i = 0; i <= segments; i++)
            scratch[i] = centerScreen + (startAngle + sweepAngle * i / segments).ToRotationVector2() * radius;

        ReservePrimitive((segments + 1) * 2, segments * 6);
        BuildStroke(scratch.AsSpan(0, segments + 1), closed: false, width, _ => color);
        Flush(null, null, space, null, null);
    }

    /// <summary>
    /// 画一个<b>实心胶囊</b>：线段两端各接一个半径 <paramref name="radius"/> 的半圆。
    /// 整颗胶囊一次提交。<paramref name="start"/> 与 <paramref name="end"/> 重合时退化为实心圆。
    /// </summary>
    /// <param name="start">线段起点（胶囊一端的圆心）。</param>
    /// <param name="end">线段终点（另一端的圆心）。</param>
    /// <param name="radius">端部半径，也是胶囊半宽。</param>
    /// <param name="color">颜色。</param>
    /// <param name="capSegments">每个半圆的分段数，范围 2..64。</param>
    /// <param name="space">坐标空间。</param>
    public static void DrawCapsule(Vector2 start, Vector2 end, float radius, Color color, int capSegments = 16, MonoGraphicsSpace space = MonoGraphicsSpace.World)
    {
        if (Main.dedServ || radius <= 0f || !Ready())
            return;

        Vector2 a = ToScreen(start, space);
        Vector2 b = ToScreen(end, space);
        int arcPoints = Math.Clamp(capSegments, 2, 64);

        Vector2 axis = b - a;
        float axial = axis.Length();
        if (axial <= 0.001f)
        {
            // 两端重合：直接当实心圆画，别把同一个圆周描两遍。
            int fanSegments = arcPoints * 2;
            ReservePrimitive(fanSegments * 3, fanSegments * 3);
            BuildCircleFan(a, radius, color, fanSegments);
            Flush(null, null, space, null, null);
            return;
        }

        int outlineCount = arcPoints * 2 + 2;
        ReservePrimitive(outlineCount + 1, outlineCount * 3);
        if (!HasRoomFor(outlineCount + 1, outlineCount * 3))
            return;

        Vector2 direction = axis / axial;
        Vector2 normal = new(-direction.Y, direction.X);

        int slot = 0;
        for (int i = 0; i <= arcPoints; i++)
        {
            float phi = MathHelper.Pi * i / arcPoints;
            scratch[slot++] = a + (normal * MathF.Cos(phi) - direction * MathF.Sin(phi)) * radius;
        }
        for (int i = 0; i <= arcPoints; i++)
        {
            float phi = MathHelper.Pi + MathHelper.Pi * i / arcPoints;
            scratch[slot++] = b + (normal * MathF.Cos(phi) - direction * MathF.Sin(phi)) * radius;
        }

        // 轮廓是凸的，用中点为扇心填充即可；截面从扇心 0 到轮廓 1。
        int origin = PushNormalizedVertex((a + b) * 0.5f, color, 0f, 0f);
        for (int i = 0; i < outlineCount; i++)
            PushNormalizedVertex(scratch[i], color, i / (float)outlineCount, 1f);
        for (int i = 0; i < outlineCount; i++)
            PushTriangle(origin, origin + 1 + i, origin + 1 + (i + 1) % outlineCount);

        Flush(null, null, space, null, null);
    }

    /// <summary>画一条虚线。<b>整条虚线一次提交</b>，不是每段一次绘制。</summary>
    /// <param name="start">起点。</param>
    /// <param name="end">终点。</param>
    /// <param name="dashLength">每段的长度（像素），最小按 0.01 处理。</param>
    /// <param name="gapLength">段间空隙（像素），最小按 0.01 处理。</param>
    /// <param name="color">颜色。</param>
    /// <param name="thickness">线宽（像素）。</param>
    /// <param name="space">坐标空间。</param>
    public static void DrawDashedLine(Vector2 start, Vector2 end, float dashLength, float gapLength, Color color, float thickness = 1f, MonoGraphicsSpace space = MonoGraphicsSpace.World)
    {
        if (Main.dedServ || !Ready())
            return;

        Vector2 a = ToScreen(start, space);
        Vector2 b = ToScreen(end, space);
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length <= 0.001f)
            return;

        Vector2 direction = delta / length;
        Vector2 normal = new(-direction.Y, direction.X);
        float dash = Math.Max(dashLength, 0.01f);
        float gap = Math.Max(gapLength, 0.01f);
        float half = Math.Max(thickness, 0.01f) * 0.5f;

        int dashCount = (int)(length / (dash + gap)) + 1;
        ReservePrimitive(dashCount * 4, dashCount * 6);
        for (float cursor = 0f; cursor < length; cursor += dash + gap)
        {
            if (!HasRoomFor(4, 6))
                break;
            float dashEnd = Math.Min(length, cursor + dash);
            Vector2 head = a + direction * cursor;
            Vector2 tail = a + direction * dashEnd;
            int v0 = PushSegmentVertex(head - normal * half, color, cursor / length, 0f, thickness);
            int v1 = PushSegmentVertex(head + normal * half, color, cursor / length, 1f, thickness);
            int v2 = PushSegmentVertex(tail + normal * half, color, dashEnd / length, 1f, thickness);
            int v3 = PushSegmentVertex(tail - normal * half, color, dashEnd / length, 0f, thickness);
            PushQuad(v0, v1, v2, v3);
        }

        Flush(null, null, space, null, null);
    }

    /// <summary>
    /// 填充一个<b>简单多边形</b>（凹凸均可，不允许自交）。
    /// <para>
    /// 逆时针且凸时走零分配的三角形扇；其余情况交给
    /// <c>MonoUtil.Polygon.Triangulate</c> 的耳切三角化（会有一次索引数组分配）。
    /// 顶点不会自动去重，重复点可能让耳切退化——请先清理输入。
    /// </para>
    /// </summary>
    /// <param name="points">多边形顶点，按 <paramref name="space"/> 给出。</param>
    /// <param name="color">填充色。</param>
    /// <param name="space">坐标空间。</param>
    public static void FillPolygon(IReadOnlyList<Vector2> points, Color color, MonoGraphicsSpace space = MonoGraphicsSpace.World)
    {
        if (Main.dedServ || points is null || !Ready())
            return;

        int count = Math.Min(points.Count, MaxPoints - 1);
        if (count < 3)
            return;

        for (int i = 0; i < count; i++)
            scratch[i] = ToScreen(points[i], space);

        Span<Vector2> polygonBuffer = scratch.AsSpan(0, count);
        // 统一成逆时针：这样 Triangulate 不会在内部反转顶点，它返回的索引才真正指向我们的数组。
        if (!Polygon.IsCounterClockwise(polygonBuffer))
            Reverse(polygonBuffer);
        ReadOnlySpan<Vector2> polygon = polygonBuffer;

        Vector2 centroid = Polygon.AverageVertex(polygon);
        float maxRadius = 0f;
        for (int i = 0; i < count; i++)
            maxRadius = MathF.Max(maxRadius, Vector2.Distance(centroid, polygon[i]));
        if (maxRadius <= 0.001f)
            return;

        ReservePrimitive(count + 1, count * 3);
        if (IsConvex(polygon) && HasRoomFor(count + 1, count * 3))
        {
            // 截面：重心 0 → 轮廓 1，按到重心的距离归一化。
            int origin = PushNormalizedVertex(centroid, color, 0f, 0f);
            for (int i = 0; i < count; i++)
                PushNormalizedVertex(polygon[i], color, i / (float)count, Vector2.Distance(centroid, polygon[i]) / maxRadius);
            for (int i = 0; i < count; i++)
                PushTriangle(origin, origin + 1 + i, origin + 1 + (i + 1) % count);
        }
        else
        {
            int[] triangles = Polygon.Triangulate(polygon);
            if (triangles.Length == 0 || !HasRoomFor(count, triangles.Length))
                return;
            int baseVertex = vertexCount;
            for (int i = 0; i < count; i++)
                PushNormalizedVertex(polygon[i], color, i / (float)count, Vector2.Distance(centroid, polygon[i]) / maxRadius);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
                PushTriangle(baseVertex + triangles[i], baseVertex + triangles[i + 1], baseVertex + triangles[i + 2]);
        }

        Flush(null, null, space, null, null);
    }

    /// <summary>画一个多边形的描边。</summary>
    /// <param name="points">多边形顶点，按 <paramref name="space"/> 给出。</param>
    /// <param name="color">颜色。</param>
    /// <param name="thickness">线宽（像素）。</param>
    /// <param name="closed">true 表示首尾相连（多边形），false 表示开放折线。</param>
    /// <param name="space">坐标空间。</param>
    public static void StrokePolygon(IReadOnlyList<Vector2> points, Color color, float thickness = 1f, bool closed = true, MonoGraphicsSpace space = MonoGraphicsSpace.World)
    {
        if (Main.dedServ || points is null || !Ready())
            return;

        int count = Math.Min(points.Count, (MaxPoints - 1) / 2);
        if (count < 2)
            return;

        for (int i = 0; i < count; i++)
            scratch[i] = ToScreen(points[i], space);

        ReservePrimitive(count * 2, (closed ? count : count - 1) * 6);
        BuildStroke(scratch.AsSpan(0, count), closed, thickness, _ => color);
        Flush(null, null, space, null, null);
    }

    /// <summary>
    /// 画一条任意次贝塞尔曲线（控制点数量不限，用 Bernstein 显式形式求值，不需要额外缓冲）。
    /// 曲线被采样成折线后按普通描边处理。
    /// </summary>
    /// <param name="controlPoints">控制点，至少 2 个，按 <paramref name="space"/> 给出。</param>
    /// <param name="color">颜色。</param>
    /// <param name="thickness">线宽（像素）。</param>
    /// <param name="segments">采样段数，范围 1..4094。</param>
    /// <param name="space">坐标空间。</param>
    public static void DrawBezier(IReadOnlyList<Vector2> controlPoints, Color color, float thickness = 1f, int segments = 24, MonoGraphicsSpace space = MonoGraphicsSpace.World)
    {
        if (Main.dedServ || controlPoints is null || controlPoints.Count < 2 || !Ready())
            return;

        segments = Math.Clamp(segments, 1, (MaxPoints - 2) / 2);
        for (int i = 0; i <= segments; i++)
            scratch[i] = ToScreen(EvaluateBezier(controlPoints, i / (float)segments), space);

        ReservePrimitive((segments + 1) * 2, segments * 6);
        BuildStroke(scratch.AsSpan(0, segments + 1), closed: false, thickness, _ => color);
        Flush(null, null, space, null, null);
    }

    /// <summary>
    /// 画一个四角异色的矩形渐变（顶点色插值，不需要贴图与着色器）。
    /// </summary>
    /// <param name="position">左上角，按 <paramref name="space"/> 给出。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="topLeft">左上角颜色。</param>
    /// <param name="topRight">右上角颜色。</param>
    /// <param name="bottomLeft">左下角颜色。</param>
    /// <param name="bottomRight">右下角颜色。</param>
    /// <param name="space">坐标空间。</param>
    public static void DrawGradientRect(Vector2 position, Vector2 size, Color topLeft, Color topRight, Color bottomLeft, Color bottomRight, MonoGraphicsSpace space = MonoGraphicsSpace.World)
    {
        if (Main.dedServ || !Ready())
            return;

        scratch[0] = ToScreen(position, space);
        scratch[1] = ToScreen(position + new Vector2(size.X, 0f), space);
        scratch[2] = ToScreen(position + size, space);
        scratch[3] = ToScreen(position + new Vector2(0f, size.Y), space);

        ReservePrimitive(4, 6);
        // 渐变完全由顶点色插值得到：上边 Y=0、下边 Y=1。
        int v0 = PushNormalizedVertex(scratch[0], topLeft, 0f, 0f);
        int v1 = PushNormalizedVertex(scratch[1], topRight, 0f, 0f);
        int v2 = PushNormalizedVertex(scratch[2], bottomRight, 0f, 1f);
        int v3 = PushNormalizedVertex(scratch[3], bottomLeft, 0f, 1f);
        PushQuad(v0, v1, v2, v3);

        Flush(null, null, space, null, null);
    }

    /// <summary>整数矩形重载，内部换算成左上角 + 尺寸。</summary>
    /// <param name="rectangle">矩形，按 <paramref name="space"/> 给出。</param>
    /// <param name="topLeft">左上角颜色。</param>
    /// <param name="topRight">右上角颜色。</param>
    /// <param name="bottomLeft">左下角颜色。</param>
    /// <param name="bottomRight">右下角颜色。</param>
    /// <param name="space">坐标空间。</param>
    public static void DrawGradientRect(Rectangle rectangle, Color topLeft, Color topRight, Color bottomLeft, Color bottomRight, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => DrawGradientRect(new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.Width, rectangle.Height), topLeft, topRight, bottomLeft, bottomRight, space);

    #endregion

    #region 几何辅助

    /// <summary>就地反转一段顶点的顺序（用于把多边形统一成逆时针）。</summary>
    private static void Reverse(Span<Vector2> points)
    {
        for (int i = 0, j = points.Length - 1; i < j; i++, j--)
            (points[i], points[j]) = (points[j], points[i]);
    }

    /// <summary>逆时针顶点序列是否处处左转（凸）。小于容差的共线点不算凹。</summary>
    private static bool IsConvex(ReadOnlySpan<Vector2> points)
    {
        int count = points.Length;
        for (int i = 0; i < count; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % count];
            Vector2 c = points[(i + 2) % count];
            if (Cross(a, b, c) < -Epsilon)
                return false;
        }
        return true;
    }

    /// <summary>Bernstein 显式形式求贝塞尔曲线上一点，控制点数量不限且不需要暂存数组。</summary>
    /// <param name="controlPoints">控制点。</param>
    /// <param name="t">归一化参数 <c>[0,1]</c>。</param>
    private static Vector2 EvaluateBezier(IReadOnlyList<Vector2> controlPoints, float t)
    {
        int degree = controlPoints.Count - 1;
        double oneMinusT = 1.0 - t;
        Vector2 result = Vector2.Zero;
        for (int i = 0; i <= degree; i++)
        {
            double weight = Binomial(degree, i) * Math.Pow(t, i) * Math.Pow(oneMinusT, degree - i);
            result += controlPoints[i] * (float)weight;
        }
        return result;
    }

    /// <summary>二项式系数 <c>C(n,k)</c>，递推计算避免阶乘溢出。</summary>
    private static double Binomial(int n, int k)
    {
        if (k < 0 || k > n)
            return 0.0;
        if (k > n - k)
            k = n - k;
        double value = 1.0;
        for (int i = 1; i <= k; i++)
            value = value * (n - k + i) / i;
        return value;
    }

    #endregion
}

/// <summary>
/// 一个图元批次。用 <c>using</c> 包住一段绘制，期间调用的 <c>MonoPrim.*</c> 会攒到一起提交。
/// <code>
/// using MonoPrimitiveBatch batch = MonoPrim.Batch();
/// foreach (MonoTrail trail in trails)
///     trail.Draw();
/// </code>
/// <para>
/// 作用域里不要再做别的原始绘制：命令是延迟执行的，自己直接调 <c>DrawIndexedPrimitives</c>
/// 或开 <see cref="SpriteBatch"/> 会与提交时机错开。批次不能嵌套。
/// </para>
/// </summary>
public sealed class MonoPrimitiveBatch : IDisposable
{
    internal MonoPrimitiveBatch()
    {
    }

    /// <summary>把已经攒下的内容立刻提交，批次仍然有效，可以继续攒。</summary>
    public void Flush() => MonoPrimitiveRenderer.SubmitBatch();

    /// <summary>提交并结束批次。重复调用安全。</summary>
    public void Dispose() => MonoPrimitiveRenderer.EndBatch();
}

/// <summary>把图元渲染器挂进 tModLoader 生命周期。</summary>
public sealed class MonoPrimitiveSystem : ModSystem
{
    /// <inheritdoc/>
    public override void OnModLoad() => MonoPrimitiveRenderer.Initialize();

    /// <inheritdoc/>
    public override void Unload() => MonoPrimitiveRenderer.Unload();
}
