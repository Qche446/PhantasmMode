using Monochrome.Common.MonoUtil.Mathematics.Geometry;
using System.Collections.Generic;
using static Monochrome.Common.MonoUtil.MonoUtil;

namespace Monochrome.Common.MonoUtil
{
    /// <summary>
    /// 九宫格锚点，用于旋转/缩放的支点。
    /// </summary>
    public enum MonoAnchor
    {
        /// <summary>左上角。</summary>
        TopLeft,
        /// <summary>上边中点。</summary>
        TopCenter,
        /// <summary>右上角。</summary>
        TopRight,
        /// <summary>左边中点。</summary>
        MiddleLeft,
        /// <summary>中心。</summary>
        MiddleCenter,
        /// <summary>右边中点。</summary>
        MiddleRight,
        /// <summary>左下角。</summary>
        BottomLeft,
        /// <summary>下边中点。</summary>
        BottomCenter,
        /// <summary>右下角。</summary>
        BottomRight,
    }

    /// <summary>
    /// 坐标变换：世界 / 屏幕 / UI / 瓦片，以及局部坐标系、矩阵栈、锚点、镜像。
    /// <para>
    /// tML 里有 4 套坐标系，混用是「特效画错位置」的最常见根因。所有转换集中在这里，
    /// 其它地方不要再手写 <c>- Main.screenPosition</c> 之类的散装运算。
    /// </para>
    /// </summary>
    public static partial class MonoUtil
    {
        #region 基础空间转换
        /// <summary>世界坐标 → 屏幕像素坐标（不含 UI 缩放）。</summary>
        /// <param name="world">世界坐标。</param>
        public static Vector2 WorldToScreen(Vector2 world)
        {
            Vector2 screen = world - Main.screenPosition;
            return new Vector2(MathF.Floor(screen.X), MathF.Floor(screen.Y));
        }

        /// <summary>屏幕像素坐标 → 世界坐标。</summary>
        /// <param name="screen">屏幕像素坐标。</param>
        public static Vector2 ScreenToWorld(Vector2 screen) => screen + Main.screenPosition;

        /// <summary>
        /// 世界坐标 → 界面坐标（受 <see cref="Main.UIScale"/> 影响）。
        /// </summary>
        /// <param name="world">世界坐标。</param>
        public static Vector2 WorldToUi(Vector2 world)
            => WorldToScreen(world) / Main.UIScale;

        /// <summary>界面坐标 → 世界坐标。</summary>
        /// <param name="ui">界面坐标。</param>
        public static Vector2 UiToWorld(Vector2 ui)
            => ScreenToWorld(ui * Main.UIScale);

        /// <summary>
        /// 屏幕像素坐标 → 界面坐标。
        /// </summary>
        /// <param name="screen">屏幕像素坐标。</param>
        /// <param name="useZoom">是否除上 UI 缩放（<c>false</c> 时原样返回）。</param>
        public static Vector2 ScreenToUi(Vector2 screen, bool useZoom = true)
            => useZoom ? screen / Main.UIScale : screen;

        /// <summary>界面坐标 → 屏幕像素坐标。</summary>
        /// <param name="ui">界面坐标。</param>
        /// <param name="useZoom">是否乘上 UI 缩放。</param>
        public static Vector2 UiToScreen(Vector2 ui, bool useZoom = true)
            => useZoom ? ui * Main.UIScale : ui;

        /// <summary>
        /// 屏幕像素坐标 → 界面坐标（使用 <see cref="Main.UIScaleMatrix"/> 的完整变换，含 UI 偏移）。
        /// 当界面被整体平移（例如 UI 拖动）时应优先用这个重载。
        /// </summary>
        /// <param name="screen">屏幕像素坐标。</param>
        public static Vector2 ScreenToUiMatrix(Vector2 screen) => Vector2.Transform(screen, Matrix.Invert(Main.UIScaleMatrix));

        /// <summary>界面坐标 → 屏幕像素坐标（使用 <see cref="Main.UIScaleMatrix"/>）。</summary>
        /// <param name="ui">界面坐标。</param>
        public static Vector2 UiToScreenMatrix(Vector2 ui) => Vector2.Transform(ui, Main.UIScaleMatrix);

        /// <summary>
        /// 世界坐标 → 瓦片坐标。
        /// </summary>
        /// <param name="world">世界坐标。</param>
        /// <param name="alignToTileCorner">true 时对齐到瓦片左上角（<c>Main.tile</c> 语义），false 时对齐到瓦片中心（实体语义）。</param>
        public static Point WorldToTile(Vector2 world, bool alignToTileCorner = false)
        {
            Vector2 adjusted = alignToTileCorner ? world : world + new Vector2(8f);
            return new Point((int)MathF.Floor(adjusted.X / 16f), (int)MathF.Floor(adjusted.Y / 16f));
        }

        /// <summary>瓦片坐标 → 世界坐标（瓦片左上角）。</summary>
        /// <param name="tile">瓦片坐标。</param>
        public static Vector2 TileToWorld(Point tile) => new(tile.X * 16f, tile.Y * 16f);

        /// <summary>瓦片坐标 → 世界坐标（瓦片中心）。</summary>
        /// <param name="tile">瓦片坐标。</param>
        public static Vector2 TileCenter(Point tile) => new(tile.X * 16f + 8f, tile.Y * 16f + 8f);

        /// <summary>世界坐标取所在瓦片；等价于 <c>world.ToTileCoordinates()</c>，但走同一处实现以免混用。</summary>
        /// <param name="world">世界坐标。</param>
        public static Point ToTile(Vector2 world) => new((int)MathF.Floor(world.X / 16f), (int)MathF.Floor(world.Y / 16f));

        /// <summary>世界坐标是否在屏幕内（含外扩边距）。用于剔除。</summary>
        /// <param name="world">世界坐标。</param>
        /// <param name="margin">额外边距（像素）。</param>
        public static bool IsOnScreen(Vector2 world, float margin = 64f)
        {
            Vector2 screen = WorldToScreen(world);
            return screen.X >= -margin
                && screen.Y >= -margin
                && screen.X <= Main.screenWidth + margin
                && screen.Y <= Main.screenHeight + margin;
        }

        /// <summary>当前相机的世界可见矩形（含外扩边距）。</summary>
        /// <param name="margin">额外边距（像素）。</param>
        public static MonoAABB VisibleWorldBounds(float margin = 0f)
        {
            Vector2 topLeft = ScreenToWorld(new Vector2(-margin));
            Vector2 bottomRight = ScreenToWorld(new Vector2(Main.screenWidth + margin, Main.screenHeight + margin));
            return new MonoAABB(topLeft, bottomRight);
        }
        #endregion

        #region 局部坐标系
        /// <summary>
        /// 世界坐标 → 局部坐标。
        /// </summary>
        /// <param name="world">世界坐标。</param>
        /// <param name="origin">局部原点（世界坐标）。</param>
        /// <param name="rotation">局部坐标系相对世界的旋转角。</param>
        /// <param name="scale">局部坐标系相对世界的缩放（零分量会被替换为 1）。</param>
        public static Vector2 WorldToLocal(Vector2 world, Vector2 origin, float rotation, Vector2 scale)
        {
            Vector2 offset = (world - origin).RotatedBy(-rotation);
            return new Vector2(
                Math.Abs(scale.X) <= MonoUtil.Epsilon ? offset.X : offset.X / scale.X,
                Math.Abs(scale.Y) <= MonoUtil.Epsilon ? offset.Y : offset.Y / scale.Y);
        }

        /// <summary>局部坐标 → 世界坐标。</summary>
        /// <param name="local">局部坐标。</param>
        /// <param name="origin">局部原点（世界坐标）。</param>
        /// <param name="rotation">旋转角。</param>
        /// <param name="scale">缩放。</param>
        public static Vector2 LocalToWorld(Vector2 local, Vector2 origin, float rotation, Vector2 scale)
            => origin + new Vector2(local.X * scale.X, local.Y * scale.Y).RotatedBy(rotation);

        /// <summary>
        /// 由「缩放 → 旋转 → 平移」组合出变换矩阵（与 <see cref="SpriteBatch"/> 的
        /// <c>transformMatrix</c> 约定一致）。
        /// </summary>
        /// <param name="origin">平移量。</param>
        /// <param name="rotation">旋转角。</param>
        /// <param name="scale">缩放。</param>
        public static Matrix Compose(Vector2 origin, float rotation = 0f, Vector2? scale = null)
        {
            Vector2 actualScale = scale ?? Vector2.One;
            return Matrix.CreateScale(actualScale.X, actualScale.Y, 1f)
                 * Matrix.CreateRotationZ(rotation)
                 * Matrix.CreateTranslation(origin.X, origin.Y, 0f);
        }

        /// <summary>由「缩放 → 旋转 → 平移」组合出变换矩阵（原点在世界坐标）。</summary>
        /// <param name="origin">世界坐标原点。</param>
        /// <param name="screenPosition">当前屏幕位置（通常传 <c>Main.screenPosition</c>）。</param>
        /// <param name="rotation">旋转角。</param>
        /// <param name="scale">缩放。</param>
        public static Matrix ComposeScreen(Vector2 origin, Vector2 screenPosition, float rotation = 0f, Vector2? scale = null)
            => Compose(origin - screenPosition, rotation, scale);

        /// <summary>把点从局部空间变换到世界空间（矩阵版，便于批量使用）。</summary>
        /// <param name="local">局部坐标。</param>
        /// <param name="matrix">变换矩阵。</param>
        public static Vector2 TransformPoint(Vector2 local, Matrix matrix) => Vector2.Transform(local, matrix);

        /// <summary>反向变换（矩阵求逆）。</summary>
        /// <param name="world">世界坐标。</param>
        /// <param name="matrix">变换矩阵。</param>
        public static Vector2 InverseTransformPoint(Vector2 world, Matrix matrix)
            => Vector2.Transform(world, Matrix.Invert(matrix));
        #endregion

        #region 锚点与枢轴
        /// <summary>
        /// 九宫格锚点对应的归一化原点（0~1）。
        /// </summary>
        /// <param name="anchor">锚点。</param>
        public static Vector2 AnchorFactors(MonoAnchor anchor) => anchor switch
        {
            MonoAnchor.TopLeft => new Vector2(0f, 0f),
            MonoAnchor.TopCenter => new Vector2(0.5f, 0f),
            MonoAnchor.TopRight => new Vector2(1f, 0f),
            MonoAnchor.MiddleLeft => new Vector2(0f, 0.5f),
            MonoAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
            MonoAnchor.MiddleRight => new Vector2(1f, 0.5f),
            MonoAnchor.BottomLeft => new Vector2(0f, 1f),
            MonoAnchor.BottomCenter => new Vector2(0.5f, 1f),
            _ => new Vector2(1f, 1f),
        };

        /// <summary>
        /// 计算 <c>SpriteBatch.Draw</c> 需要的 <c>origin</c>（即旋转支点在纹理内的位置）。
        /// </summary>
        /// <param name="anchor">锚点。</param>
        /// <param name="size">纹理（帧）尺寸。</param>
        public static Vector2 SpriteOrigin(MonoAnchor anchor, Vector2 size) => AnchorFactors(anchor) * size;

        /// <summary>用自定义归一化枢轴（0~1）计算 <c>origin</c>。</summary>
        /// <param name="pivot">归一化枢轴。</param>
        /// <param name="size">纹理尺寸。</param>
        public static Vector2 SpriteOrigin(Vector2 pivot, Vector2 size) => pivot * size;

        /// <summary>
        /// 把锚点归一化坐标换算为「以某点为锚点的绘制偏移」。
        /// 把它加到「要对齐到锚点的坐标」上即可得到纹理左上角。
        /// </summary>
        /// <param name="anchor">锚点。</param>
        /// <param name="size">纹理尺寸。</param>
        public static Vector2 AnchorOffset(MonoAnchor anchor, Vector2 size) => -AnchorFactors(anchor) * size;

        /// <summary>把纹理矩形按锚点对齐到给定位置（返回左上角）。</summary>
        /// <param name="position">锚点目标位置。</param>
        /// <param name="size">纹理尺寸。</param>
        /// <param name="anchor">锚点。</param>
        public static Vector2 AlignToAnchor(Vector2 position, Vector2 size, MonoAnchor anchor)
            => position + AnchorOffset(anchor, size);

        /// <summary>按锚点对齐并返回整数矩形（避免半像素导致的模糊）。</summary>
        /// <param name="position">锚点目标位置。</param>
        /// <param name="size">尺寸。</param>
        /// <param name="anchor">锚点。</param>
        public static Rectangle AlignedRectangle(Vector2 position, Vector2 size, MonoAnchor anchor)
        {
            Vector2 topLeft = AlignToAnchor(position, size, anchor);
            return new Rectangle((int)MathF.Round(topLeft.X), (int)MathF.Round(topLeft.Y), (int)MathF.Round(size.X), (int)MathF.Round(size.Y));
        }
        #endregion

        #region 镜像与翻转
        /// <summary>
        /// 把「-1/1 的方向」换算为 <see cref="SpriteEffects"/>。
        /// </summary>
        /// <param name="direction">方向，正数不翻转、负数为水平翻转。</param>
        public static SpriteEffects DirectionToSpriteEffects(int direction)
            => direction >= 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        /// <summary>把 <see cref="SpriteEffects"/> 换算回方向（-1 或 1）。</summary>
        /// <param name="effects">精灵效果。</param>
        public static int SpriteEffectsToDirection(SpriteEffects effects)
            => (effects & SpriteEffects.FlipHorizontally) != 0 ? -1 : 1;

        /// <summary>水平镜像叠加：保留已有翻转标志。</summary>
        /// <param name="effects">原始效果。</param>
        public static SpriteEffects MirrorHorizontally(SpriteEffects effects) => effects ^ SpriteEffects.FlipHorizontally;

        /// <summary>垂直镜像叠加。</summary>
        /// <param name="effects">原始效果。</param>
        public static SpriteEffects MirrorVertically(SpriteEffects effects) => effects ^ SpriteEffects.FlipVertically;

        /// <summary>
        /// 世界坐标的水平镜像：以 <paramref name="axisX"/> 为对称轴翻转。
        /// 与 <see cref="SpriteEffects"/> 配套使用才能让「贴图 + 判定」同时正确。
        /// </summary>
        /// <param name="world">世界坐标。</param>
        /// <param name="axisX">对称轴的 X 坐标。</param>
        public static Vector2 MirrorWorldHorizontally(Vector2 world, float axisX) => new(axisX * 2f - world.X, world.Y);

        /// <summary>世界坐标的垂直镜像。</summary>
        /// <param name="world">世界坐标。</param>
        /// <param name="axisY">对称轴的 Y 坐标。</param>
        public static Vector2 MirrorWorldVertically(Vector2 world, float axisY) => new(world.X, axisY * 2f - world.Y);
        #endregion

        #region 矩阵栈
        #endregion
    }

    /// <summary>
    /// 变换矩阵栈：保存/恢复变换，避免层层手写逆变换。
    /// 用于「在旋转的父节点里再画一个旋转的子节点」这类嵌套绘制。
    /// </summary>
    public sealed class MonoMatrixStack
    {
        private readonly List<Matrix> _stack = new(16);
        private Matrix _current = Matrix.Identity;

        /// <summary>当前组合后的矩阵。</summary>
        public Matrix Current => _current;

        /// <summary>栈深度。</summary>
        public int Depth => _stack.Count;

        /// <summary>清空并重置为单位矩阵。</summary>
        public void Clear()
        {
            _stack.Clear();
            _current = Matrix.Identity;
        }

        /// <summary>压入当前矩阵（保存现场）。</summary>
        public void Push() => _stack.Add(_current);

        /// <summary>把当前矩阵乘以一个变换（在已有变换之上叠加）。</summary>
        /// <param name="transform">要叠加的变换。</param>
        public void Multiply(Matrix transform) => _current = transform * _current;

        /// <summary>在已有变换之上叠加「缩放 → 旋转 → 平移」。</summary>
        /// <param name="translation">平移量。</param>
        /// <param name="rotation">旋转角。</param>
        /// <param name="scale">缩放。</param>
        public void Append(Vector2 translation, float rotation = 0f, Vector2? scale = null)
            => Multiply(Compose(translation, rotation, scale));

        /// <summary>弹出并恢复上一层矩阵。</summary>
        /// <returns>是否成功弹出。</returns>
        public bool Pop()
        {
            if (_stack.Count == 0)
                return false;
            _current = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            return true;
        }

        /// <summary>把点从局部空间变换到当前矩阵的外层空间。</summary>
        /// <param name="local">局部坐标。</param>
        public Vector2 Transform(Vector2 local) => Vector2.Transform(local, _current);

        /// <summary>把点从外层空间逆变换回局部空间。</summary>
        /// <param name="outer">外层坐标。</param>
        public Vector2 InverseTransform(Vector2 outer) => Vector2.Transform(outer, Matrix.Invert(_current));

        /// <summary>
        /// 以「保存 → 叠加 → 执行 → 恢复」的方式包住一段绘制逻辑，杜绝漏 Pop。
        /// </summary>
        /// <param name="translation">平移量。</param>
        /// <param name="rotation">旋转角。</param>
        /// <param name="scale">缩放。</param>
        /// <param name="action">要执行的绘制逻辑。</param>
        public void WithTransform(Vector2 translation, float rotation, Vector2? scale, Action action)
        {
            Push();
            Append(translation, rotation, scale);
            try
            {
                action?.Invoke();
            }
            finally
            {
                Pop();
            }
        }
    }
}
