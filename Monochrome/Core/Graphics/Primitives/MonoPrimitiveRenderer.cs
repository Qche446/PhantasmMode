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
    private static Texture2D? fallbackTexture;

    private static int vertexCount;
    private static int indexCount;

    /// <summary>重采样、描边、轮廓共用的暂存点列，避免每次绘制都分配数组。</summary>
    private static readonly Vector2[] scratch = new Vector2[MaxPoints];

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
        fallbackTexture = null;
        vertices = null;
        indices = null;
        vertexCount = 0;
        indexCount = 0;
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
    }

    private static bool HasRoomFor(int vertexNeed, int indexNeed)
        => vertexCount + vertexNeed <= MaxVertices && indexCount + indexNeed <= MaxIndices;

    /// <summary>写入一个顶点，返回它的索引。</summary>
    /// <param name="position">屏幕坐标。</param>
    /// <param name="color">顶点颜色。</param>
    /// <param name="length">沿形状的长度/角度参数，写入附加数据的 X。</param>
    /// <param name="section">截面坐标 <c>[0,1]</c>，写入附加数据的 Y。</param>
    /// <param name="pixelSpan">截面 0→1 跨越的像素距离，写入附加数据的 Z。</param>
    private static int PushVertex(Vector2 position, Color color, float length, float section, float pixelSpan)
    {
        vertices![vertexCount] = new MonoPrimitiveVertex(position, color, new Vector3(length, section, pixelSpan));
        return vertexCount++;
    }

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

        for (int i = 0; i < count; i++)
        {
            Vector2 previous = points[closed ? (i - 1 + count) % count : Math.Max(i - 1, 0)];
            Vector2 next = points[closed ? (i + 1) % count : Math.Min(i + 1, count - 1)];
            Vector2 forward = SafeNormalize(next - previous, Vector2.UnitX);
            Vector2 normal = new(-forward.Y, forward.X);
            float length = i / (float)Math.Max(count - 1, 1);
            Color color = colorAt(length);
            PushVertex(points[i] - normal * half, color, length, 0f, thickness);
            PushVertex(points[i] + normal * half, color, length, 1f, thickness);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int a = i * 2;
            int b = i * 2 + 1;
            int c = ((i + 1) % count) * 2 + 1;
            int d = ((i + 1) % count) * 2;
            PushQuad(a, b, c, d);
        }
    }

    /// <summary>以 <paramref name="centerScreen"/> 为扇心写一个实心三角形扇（圆、退化胶囊共用）。</summary>
    private static void BuildCircleFan(Vector2 centerScreen, float radius, Color color, int segments)
    {
        segments = Math.Clamp(segments, 3, MaxPoints);
        if (!HasRoomFor(segments * 3, segments * 3))
            return;

        int origin = PushVertex(centerScreen, color, 0f, 0f, radius);
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            Vector2 point = centerScreen + (MathHelper.TwoPi * t).ToRotationVector2() * radius;
            PushVertex(point, color, t, 1f, radius);
        }

        for (int i = 0; i < segments; i++)
            PushTriangle(origin, origin + 1 + i, origin + 1 + (i + 1) % segments);
    }

    #endregion

    #region 提交

    /// <summary>把当前网格一次性上传并绘制，随后恢复设备状态。</summary>
    /// <param name="shader">可选的自定义着色器；为 null 时走顶点色 <c>BasicEffect</c> 回退。</param>
    /// <param name="blendState">混合状态；为 null 时用 <see cref="BlendState.AlphaBlend"/>。</param>
    /// <param name="space">坐标空间，决定投影矩阵。</param>
    /// <param name="projectionWidth">投影宽度，null 表示 <c>Main.screenWidth</c>。</param>
    /// <param name="projectionHeight">投影高度，null 表示 <c>Main.screenHeight</c>。</param>
    private static void Flush(MonoShader? shader, BlendState? blendState, MonoGraphicsSpace space, int? projectionWidth, int? projectionHeight)
    {
        if (vertexCount <= 0 || indexCount <= 0 || !Ready())
            return;

        // 没传 shader 就用默认图元着色器；连它也没加载到才回退到 BasicEffect。
        shader ??= ResolveDefaultShader();

        GraphicsDevice device = Main.instance.GraphicsDevice;
        VertexBufferBinding[] oldVertexBuffers = device.GetVertexBuffers();
        IndexBuffer? oldIndexBuffer = device.Indices;
        RasterizerState oldRasterizer = device.RasterizerState;
        BlendState oldBlend = device.BlendState;
        try
        {
            Matrix matrix = CalculatePrimitiveMatrix(projectionWidth ?? Main.screenWidth, projectionHeight ?? Main.screenHeight, space);
            vertexBuffer!.SetData(vertices!, 0, vertexCount, SetDataOptions.Discard);
            indexBuffer!.SetData(indices!, 0, indexCount, SetDataOptions.Discard);
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;
            device.RasterizerState = RasterizerState.CullNone;
            device.BlendState = blendState ?? BlendState.AlphaBlend;

            if (shader is not null)
            {
                // "要不要乘 s1 上的贴图"由 fallbackTexture 决定：只有 RenderQuad 会设它。
                // useTexture **必须每次都显式写**——MonoShader 的参数带缓存，不写会沿用上一次的值，
                // 于是"先用 RenderQuad 画过贴图、再 DrawLine"就会让线段去采样一张没绑定的贴图。
                if (fallbackTexture is not null)
                    shader.SetTexture(fallbackTexture, 1, SamplerState.LinearClamp);
                shader.SetParameter("uWorldViewProjection", matrix)
                      .SetCommonParameters()
                      .SetParameter("useTexture", fallbackTexture is not null ? 1f : 0f)
                      .Apply();
            }
            else
            {
                fallbackEffect!.World = Matrix.Identity;
                fallbackEffect.View = Matrix.Identity;
                fallbackEffect.Projection = matrix;
                fallbackEffect.TextureEnabled = fallbackTexture is not null;
                fallbackEffect.Texture = fallbackTexture;
                fallbackEffect.CurrentTechnique.Passes[0].Apply();
            }

            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexCount, 0, indexCount / 3);
        }
        finally
        {
            device.SetVertexBuffers(oldVertexBuffers);
            device.Indices = oldIndexBuffer;
            device.RasterizerState = oldRasterizer;
            device.BlendState = oldBlend;
        }
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

    /// <summary>把 <see cref="MonoPrimitiveSettings.UseUnscaledMatrix"/> 换算成实际使用的坐标空间。</summary>
    private static MonoGraphicsSpace EffectiveSpace(MonoPrimitiveSettings settings)
        => settings.UseUnscaledMatrix ? MonoGraphicsSpace.Screen : settings.Space;

    #endregion

    #region 拖尾

    /// <summary>把点列渲染成一条一次提交的索引三角形带。</summary>
    /// <param name="positions">路径点，按 <paramref name="settings"/> 的坐标空间给出。</param>
    /// <param name="settings">绘制配置。</param>
    /// <param name="pointsToCreate">
    /// 重采样后的点数；越多越细腻也越贵，默认取 <paramref name="positions"/> 的点数。上限 <c>4096</c>。
    /// </param>
    public static void RenderTrail(IReadOnlyList<Vector2> positions, MonoPrimitiveSettings settings, int? pointsToCreate = null)
    {
        if (Main.dedServ || settings is null || positions is null || positions.Count < 2 || !Ready())
            return;

        int requested = Math.Clamp(pointsToCreate ?? positions.Count, 2, MaxPoints);
        if (!TryBuildPositions(positions, settings, requested, out int actualCount))
            return;

        ResetMesh();
        for (int i = 0; i < actualCount; i++)
        {
            float t = i / (float)(actualCount - 1);
            Vector2 current = scratch[i];
            Vector2 tangent = i == actualCount - 1 ? current - scratch[i - 1] : scratch[i + 1] - current;
            tangent = SafeNormalize(tangent, Vector2.UnitX);
            Vector2 normal = new(-tangent.Y, tangent.X);
            float width = Math.Max(0f, settings.WidthFunction(t));
            Color color = settings.ColorFunction(t);

            Vector2 left = current - normal * width * 0.5f;
            Vector2 right = current + normal * width * 0.5f;
            if (i == 0 && settings.InitialVertexPositionsOverride is { } overrides && overrides.Left != Vector2.Zero && overrides.Right != Vector2.Zero)
            {
                left = ToScreen(overrides.Left, settings.Space);
                right = ToScreen(overrides.Right, settings.Space);
            }

            // 截面：左缘 0 → 右缘 1，中心线 0.5；Z 为该处条带全宽。
            PushVertex(left, color, t, 0f, width);
            PushVertex(right, color, t, 1f, width);
        }

        for (int i = 0; i < actualCount - 1; i++)
            PushQuad(i * 2, i * 2 + 1, i * 2 + 3, i * 2 + 2);

        Flush(settings.Shader, settings.BlendState, EffectiveSpace(settings), settings.ProjectionWidth, settings.ProjectionHeight);
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

    /// <summary>把点列重采样成 <paramref name="requested"/> 个屏幕坐标点，写入 <c>scratch</c>。</summary>
    /// <param name="source">原始路径点。</param>
    /// <param name="settings">绘制配置（决定是否样条平滑与偏移函数）。</param>
    /// <param name="requested">期望点数。</param>
    /// <param name="count">实际写出的点数。</param>
    /// <returns>点数足够构成拖尾时为 true。</returns>
    private static bool TryBuildPositions(IReadOnlyList<Vector2> source, MonoPrimitiveSettings settings, int requested, out int count)
    {
        count = 0;
        if (!settings.Smoothen)
        {
            for (int i = 0; i < requested && count < MaxPoints; i++)
            {
                float t = i / (float)(requested - 1);
                int index = Math.Min(source.Count - 1, (int)(t * (source.Count - 1)));
                Vector2 point = source[index];
                if (point == Vector2.Zero)
                    continue;
                scratch[count++] = ToScreen(point + (settings.OffsetFunction?.Invoke(t) ?? Vector2.Zero), settings.Space);
            }
            return count >= 2;
        }

        for (int i = 0; i < requested && count < MaxPoints; i++)
        {
            float t = i / (float)(requested - 1);
            float scaled = t * (source.Count - 1);
            int index = Math.Min(source.Count - 2, (int)scaled);
            float local = scaled - index;
            Vector2 a = index > 0 ? source[index - 1] : source[index] * 2f - source[index + 1];
            Vector2 b = source[index];
            Vector2 c = source[index + 1];
            Vector2 d = index + 2 < source.Count ? source[index + 2] : source[index + 1] * 2f - source[index];
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

        sideCount = Math.Clamp(sideCount, 3, MaxPoints);
        if (!HasRoomFor(sideCount * 3, sideCount * 3))
            return;

        ResetMesh();
        Vector2 centerScreen = ToScreen(center, settings.Space);
        for (int i = 0; i < sideCount; i++)
        {
            float t = i / (float)sideCount;
            float nextT = (i + 1f) / sideCount;
            float radius = Math.Max(0f, settings.RadiusFunction(t));
            float nextRadius = Math.Max(0f, settings.RadiusFunction(nextT));
            Vector2 edge = centerScreen + (MathHelper.TwoPi * t).ToRotationVector2() * radius;
            Vector2 nextEdge = centerScreen + (MathHelper.TwoPi * nextT).ToRotationVector2() * nextRadius;

            // 截面：圆心 0 → 边缘 1；Z 为该切片的半径。
            int v0 = PushVertex(centerScreen, settings.ColorFunction(t), t, 0f, radius);
            int v1 = PushVertex(edge, settings.ColorFunction(t), t, 1f, radius);
            int v2 = PushVertex(nextEdge, settings.ColorFunction(nextT), nextT, 1f, nextRadius);
            PushTriangle(v0, v1, v2);
        }

        Flush(settings.Shader, settings.BlendState, settings.UseUnscaledMatrix ? MonoGraphicsSpace.Screen : settings.Space, settings.ProjectionWidth, settings.ProjectionHeight);
    }

    /// <summary>渲染一圈圆形描边（圆环带），环厚由内径向外径单侧展开。</summary>
    /// <param name="center">圆心，按 <paramref name="settings"/> 的坐标空间给出。</param>
    /// <param name="settings">内径、环厚与颜色配置。</param>
    /// <param name="pointCount">环上的分段数；越大越圆滑也越贵。上限 <c>4095</c>。</param>
    public static void RenderCircleEdge(Vector2 center, MonoCircleEdgeSettings settings, int pointCount = 128)
    {
        if (Main.dedServ || settings is null || !Ready())
            return;

        pointCount = Math.Clamp(pointCount, 3, MaxPoints - 1);
        if (!HasRoomFor((pointCount + 1) * 2, pointCount * 6))
            return;

        ResetMesh();
        Vector2 centerScreen = ToScreen(center, settings.Space);
        for (int i = 0; i <= pointCount; i++)
        {
            float t = i / (float)pointCount;
            Vector2 radial = (MathHelper.TwoPi * t).ToRotationVector2();
            float radius = Math.Max(0f, settings.RadiusFunction(t));
            float edgeWidth = settings.EdgeWidthFunction(t);
            Color color = settings.ColorFunction(t);

            // 截面：内径 0 → 外径 1；Z 为环厚。
            PushVertex(centerScreen + radial * radius, color, t, 0f, edgeWidth);
            PushVertex(centerScreen + radial * (radius + edgeWidth), color, t, 1f, edgeWidth);
        }

        for (int i = 0; i < pointCount; i++)
            PushQuad(i * 2, i * 2 + 1, i * 2 + 3, i * 2 + 2);

        Flush(settings.Shader, settings.BlendState, settings.UseUnscaledMatrix ? MonoGraphicsSpace.Screen : settings.Space, settings.ProjectionWidth, settings.ProjectionHeight);
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
    {
        if (Main.dedServ || texture is null || !Ready())
            return;

        Vector2 half = texture.Size() * scale * 0.5f;
        quadCorners[0] = new Vector2(-half.X, -half.Y);
        quadCorners[1] = new Vector2(half.X, -half.Y);
        quadCorners[2] = new Vector2(half.X, half.Y);
        quadCorners[3] = new Vector2(-half.X, half.Y);

        Vector2 centerScreen = ToScreen(center, space);
        // 截面：纵向 0 → 1 跨越的像素数就是纹理高度乘以缩放。
        float span = texture.Height * Math.Abs(scale.Y);

        ResetMesh();
        for (int i = 0; i < 4; i++)
        {
            float u = i is 1 or 2 ? 1f : 0f;
            float v = i >= 2 ? 1f : 0f;
            PushVertex(centerScreen + quadCorners[i].RotatedBy(rotation), color, u, v, span);
        }
        PushQuad(0, 1, 2, 3);

        fallbackTexture = texture;
        try
        {
            // 贴图与 useTexture 由 Flush 统一处理（见那里的注释），这里只负责把 fallbackTexture 架好。
            Flush(shader, blendState, space, projectionWidth, projectionHeight);
        }
        finally
        {
            fallbackTexture = null;
        }
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
        ResetMesh();
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

        ResetMesh();
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
            ResetMesh();
            BuildCircleFan(a, radius, color, arcPoints * 2);
            Flush(null, null, space, null, null);
            return;
        }

        int outlineCount = arcPoints * 2 + 2;
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

        ResetMesh();
        // 轮廓是凸的，用中点为扇心填充即可；Z 按"圆心 → 轮廓"的截面跨度取半径。
        int origin = PushVertex((a + b) * 0.5f, color, 0f, 0f, radius);
        for (int i = 0; i < outlineCount; i++)
            PushVertex(scratch[i], color, i / (float)outlineCount, 1f, radius);
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

        ResetMesh();
        for (float cursor = 0f; cursor < length; cursor += dash + gap)
        {
            if (!HasRoomFor(4, 6))
                break;
            float dashEnd = Math.Min(length, cursor + dash);
            Vector2 head = a + direction * cursor;
            Vector2 tail = a + direction * dashEnd;
            int v0 = PushVertex(head - normal * half, color, cursor / length, 0f, thickness);
            int v1 = PushVertex(head + normal * half, color, cursor / length, 1f, thickness);
            int v2 = PushVertex(tail + normal * half, color, dashEnd / length, 1f, thickness);
            int v3 = PushVertex(tail - normal * half, color, dashEnd / length, 0f, thickness);
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

        ResetMesh();
        if (IsConvex(polygon) && HasRoomFor(count + 1, count * 3))
        {
            // 截面：重心 0 → 轮廓 1，按到重心的距离归一化；Z 为最大重心里程。
            int origin = PushVertex(centroid, color, 0f, 0f, maxRadius);
            for (int i = 0; i < count; i++)
                PushVertex(polygon[i], color, i / (float)count, Vector2.Distance(centroid, polygon[i]) / maxRadius, maxRadius);
            for (int i = 0; i < count; i++)
                PushTriangle(origin, origin + 1 + i, origin + 1 + (i + 1) % count);
        }
        else
        {
            int[] triangles = Polygon.Triangulate(polygon);
            if (triangles.Length == 0 || !HasRoomFor(count, triangles.Length))
                return;
            for (int i = 0; i < count; i++)
                PushVertex(polygon[i], color, i / (float)count, Vector2.Distance(centroid, polygon[i]) / maxRadius, maxRadius);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
                PushTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
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

        ResetMesh();
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

        ResetMesh();
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

        ResetMesh();
        // 截面：纵向 0 → 1 跨越的像素数即矩形高度。
        float span = size.Y;
        int v0 = PushVertex(scratch[0], topLeft, 0f, 0f, span);
        int v1 = PushVertex(scratch[1], topRight, 0f, 0f, span);
        int v2 = PushVertex(scratch[2], bottomRight, 0f, 1f, span);
        int v3 = PushVertex(scratch[3], bottomLeft, 0f, 1f, span);
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

/// <summary>把图元渲染器挂进 tModLoader 生命周期。</summary>
public sealed class MonoPrimitiveSystem : ModSystem
{
    /// <inheritdoc/>
    public override void OnModLoad() => MonoPrimitiveRenderer.Initialize();

    /// <inheritdoc/>
    public override void Unload() => MonoPrimitiveRenderer.Unload();
}
