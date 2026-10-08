namespace Monochrome.Common.MonoUtil;

/// <summary>
/// 尘埃生成的收口层：把"生成一颗尘埃，再补几行属性"压成一次调用。
/// <para>
/// 尘埃是纯表现，不参与网络同步，也不需要在权威端之外单独开门。
/// </para>
/// </summary>
public static class MonoDust
{
    /// <summary>
    /// 在矩形内随机取一点生成一颗尘埃。前四个参数与 <c>Dust.NewDust</c> 对齐，
    /// 后面多出 <paramref name="velocityScale"/>、<paramref name="noGravity"/>、<paramref name="noLight"/> 三个后置开关。
    /// </summary>
    /// <param name="position">矩形的左上角，世界坐标。</param>
    /// <param name="width">矩形宽。为 0 时取点不随机。</param>
    /// <param name="height">矩形高。为 0 时取点不随机。</param>
    /// <param name="type">尘埃类型。</param>
    /// <param name="velocity">初速。<c>NewDust</c> 会在它基础上再叠一点随机。</param>
    /// <param name="alpha">透明度。</param>
    /// <param name="color">着色。</param>
    /// <param name="scale">缩放。</param>
    /// <param name="velocityScale">生成后把尘埃自身的速度乘上这个系数，用来放大 <c>NewDust</c> 给的随机速度。为 1 时不动。</param>
    /// <param name="noGravity">是否关掉重力。为 <c>false</c> 时不写这一项，避免覆盖尘埃类型自己的默认值。</param>
    /// <param name="noLight">是否关掉光照影响。为 <c>false</c> 时同上。</param>
    /// <returns>尘埃在 <c>Main.dust</c> 里的下标；没有真的生成时返回 -1。</returns>
    public static int Spawn(Vector2 position, int width, int height, int type,
        Vector2 velocity = default, int alpha = 0, Color color = default, float scale = 1f,
        float velocityScale = 1f, bool noGravity = false, bool noLight = false)
    {
        int index = Dust.NewDust(position, width, height, type, velocity.X, velocity.Y, alpha, color, scale);

        // NewDust 失败时会返回 Main.maxDust 当哨兵（服务端、主菜单、暂停、屏幕外都走这条路）。
        // Main.dust 比它长一格，直接索引会写到一个永不绘制的位置，所以这里先挡掉。
        if (index >= Main.maxDust)
            return -1;

        ref Dust dust = ref Main.dust[index];
        if (noGravity)
            dust.noGravity = true;
        if (noLight)
            dust.noLight = true;
        if (velocityScale != 1f)
            dust.velocity *= velocityScale;

        return index;
    }

    /// <summary>
    /// 在一个点上生成一颗尘埃。矩形收成一点，取点不再随机。
    /// </summary>
    /// <param name="center">生成点，世界坐标。</param>
    /// <param name="type">尘埃类型。</param>
    /// <param name="velocity">初速。</param>
    /// <param name="alpha">透明度。</param>
    /// <param name="color">着色。</param>
    /// <param name="scale">缩放。</param>
    /// <param name="velocityScale">生成后把尘埃自身的速度乘上这个系数。</param>
    /// <param name="noGravity">是否关掉重力。</param>
    /// <param name="noLight">是否关掉光照影响。</param>
    /// <returns>尘埃在 <c>Main.dust</c> 里的下标；没有真的生成时返回 -1。</returns>
    public static int At(Vector2 center, int type,
        Vector2 velocity = default, int alpha = 0, Color color = default, float scale = 1f,
        float velocityScale = 1f, bool noGravity = false, bool noLight = false)
        => Spawn(center, 0, 0, type, velocity, alpha, color, scale, velocityScale, noGravity, noLight);
}
