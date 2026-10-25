namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EaterOfWorlds;

/// <summary>
/// 原版 <c>AI_006_Worms</c> 里世吞那段的复刻：移动、钻地音效、钻地碎块各一个方法，
/// 共用的「埋着」判据单独公开。都只处理单段，头、身、尾各自调用即可，互相之间不共享状态。
/// </summary>
internal static class EowVanilla
{
    /// <summary>
    /// 给头部使用的原版移动。有前节的段只做位置约束跟随；没有前节的段（头）按「压着实心块/液体就游、露着就落」走。
    /// targetPos一般填写Player.Center，或者加上偏移
    /// 会改写 <c>velocity</c> 与 <c>rotation</c>。
    /// </summary>
    public static void Move(NPC npc, Vector2 targetPos)
    {
        if (npc.ai[1] > 0f && (int)npc.ai[1] < Main.maxNPCs)
        {
            EowChain.Follow(npc);
            return;
        }

        if (npc.target < 0 || npc.target == 255 || !Main.player[npc.target].active)
            return;

        bool buried = Buried(npc, out _);

        // 原版把目标点与自身中心各自对齐到 16px 网格再取差值，游动时的抖动就来自这一步
        float targetX = (int)(targetPos.X / 16f) * 16f;
        float targetY = (int)(targetPos.Y / 16f) * 16f;
        float centerX = (int)(npc.Center.X / 16f) * 16f;
        float centerY = (int)(npc.Center.Y / 16f) * 16f;
        float dx = targetX - centerX;
        float dy = targetY - centerY;

        float maxSpeed = 20f;
        float accel = 0.25f;
        if (!buried)
        {
            // 空中：只受重力，横向能微调
            npc.velocity.Y += 0.11f;
            if (npc.velocity.Y > maxSpeed)
                npc.velocity.Y = maxSpeed;

            if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < maxSpeed * 0.4f)
            {
                if (npc.velocity.X < 0f)
                    npc.velocity.X -= accel * 1.1f;
                else
                    npc.velocity.X += accel * 1.1f;
            }
            else if (npc.velocity.Y == maxSpeed)
            {
                if (npc.velocity.X < dx)
                    npc.velocity.X += accel;
                else if (npc.velocity.X > dx)
                    npc.velocity.X -= accel;
            }
            else if (npc.velocity.Y > 4f)
            {
                if (npc.velocity.X < 0f)
                    npc.velocity.X += accel * 0.9f;
                else
                    npc.velocity.X -= accel * 0.9f;
            }
        }
        else
        {
            // 埋着：直接朝玩家游，速度归一化到 maxSpeed
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance < 1f)
                distance = 1f; // 原版没防这个零，会对齐到同一格后除出 NaN

            float scale = maxSpeed / distance;
            dx *= scale;
            dy *= scale;

            bool closing = (npc.velocity.X > 0f && dx > 0f) || (npc.velocity.X < 0f && dx < 0f)
                || (npc.velocity.Y > 0f && dy > 0f) || (npc.velocity.Y < 0f && dy < 0f);

            if (closing)
            {
                if (npc.velocity.X < dx)
                    npc.velocity.X += accel;
                else if (npc.velocity.X > dx)
                    npc.velocity.X -= accel;

                if (npc.velocity.Y < dy)
                    npc.velocity.Y += accel;
                else if (npc.velocity.Y > dy)
                    npc.velocity.Y -= accel;

                // 横向已经过头时，用纵向把它拉开，避免在原地震荡
                if (Math.Abs(dy) < maxSpeed * 0.2f && ((npc.velocity.X > 0f && dx < 0f) || (npc.velocity.X < 0f && dx > 0f)))
                {
                    if (npc.velocity.Y > 0f)
                        npc.velocity.Y += accel * 2f;
                    else
                        npc.velocity.Y -= accel * 2f;
                }
                if (Math.Abs(dx) < maxSpeed * 0.2f && ((npc.velocity.Y > 0f && dy < 0f) || (npc.velocity.Y < 0f && dy > 0f)))
                {
                    if (npc.velocity.X > 0f)
                        npc.velocity.X += accel * 2f;
                    else
                        npc.velocity.X -= accel * 2f;
                }
            }
            else if (Math.Abs(dx) > Math.Abs(dy))
            {
                if (npc.velocity.X < dx)
                    npc.velocity.X += accel * 1.1f;
                else if (npc.velocity.X > dx)
                    npc.velocity.X -= accel * 1.1f;

                if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < maxSpeed * 0.5f)
                {
                    if (npc.velocity.Y > 0f)
                        npc.velocity.Y += accel;
                    else
                        npc.velocity.Y -= accel;
                }
            }
            else
            {
                if (npc.velocity.Y < dy)
                    npc.velocity.Y += accel * 1.1f;
                else if (npc.velocity.Y > dy)
                    npc.velocity.Y -= accel * 1.1f;

                if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < maxSpeed * 0.5f)
                {
                    if (npc.velocity.X > 0f)
                        npc.velocity.X += accel;
                    else
                        npc.velocity.X -= accel;
                }
            }
        }

        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
    }

    //音效，但是1.4.4无
    public static void DigSound(NPC npc)
    {
        if (npc.soundDelay > 0)
            return;
        if (npc.target < 0 || npc.target >= Main.maxPlayers)
            return;
        if (!Buried(npc, out _))
            return;

        float interval = npc.Distance(Main.player[npc.target].Center) / 40f;
        npc.soundDelay = (int)MathHelper.Clamp(interval, 10f, 20f);
        //SoundEngine.PlaySound(SoundID.WormDig, npc.Center);
    }

    //原版钻地粒子特效
    public static void DigDust(NPC npc)
    {
        Rectangle box = npc.Hitbox;
        Rectangle tiles = TileBox(npc);

        for (int x = tiles.Left; x < tiles.Right; x++)
        {
            for (int y = tiles.Top; y < tiles.Bottom; y++)
            {
                if (!WorldGen.InWorld(x, y, 1))
                    continue;

                Tile tile = Main.tile[x, y];
                if (!tile.HasTile || tile.IsActuated || !Main.tileSolid[tile.TileType])
                    continue;

                if (!box.Intersects(new Rectangle(x * 16, y * 16, 16, 16)))
                    continue;

                if (Main.rand.NextBool(100))
                    WorldGen.KillTile(x, y, fail: true, effectOnly: true);
            }
        }
    }

    /// <summary>
    /// 原版的「埋着」判据：压着实心块或深于 64 的液体，或者 1000px 内没有玩家。
    /// <paramref name="inSolid"/> 表示确实压着方块。
    /// </summary>
    public static bool Buried(NPC npc, out bool inSolid)
    {
        inSolid = false;

        Rectangle box = npc.Hitbox;
        Rectangle tiles = TileBox(npc);
        bool buried = false;

        for (int x = tiles.Left; x < tiles.Right; x++)
        {
            for (int y = tiles.Top; y < tiles.Bottom; y++)
            {
                if (!WorldGen.InWorld(x, y, 1))
                    continue;

                Tile tile = Main.tile[x, y];
                bool blocked = tile.HasTile && !tile.IsActuated
                    && (Main.tileSolid[tile.TileType] || (Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0));
                if (!blocked && tile.LiquidAmount <= 64)
                    continue;

                if (!box.Intersects(new Rectangle(x * 16, y * 16, 16, 16)))
                    continue;

                inSolid = true;
                buried = true;
            }
        }

        if (buried)
            return true;

        // 1000px 内一个玩家都没有时也算埋着，于是它会原地游而不是往下掉
        Rectangle nearby = new((int)npc.position.X - 600, (int)npc.position.Y - 600, 1200, 1200);
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (!player.active)
                continue;

            Rectangle range = new((int)player.position.X - 1000, (int)player.position.Y - 1000, 2000, 2000);
            if (range.Intersects(nearby))
                return false;
        }

        return true;
    }

    // 原版扫的格子范围：本体外扩一格，再留两格余量
    private static Rectangle TileBox(NPC npc)
    {
        int left = Math.Max((int)(npc.position.X / 16f) - 1, 0);
        int top = Math.Max((int)(npc.position.Y / 16f) - 1, 0);
        int right = Math.Min((int)((npc.position.X + npc.width) / 16f) + 2, Main.maxTilesX);
        int bottom = Math.Min((int)((npc.position.Y + npc.height) / 16f) + 2, Main.maxTilesY);
        return new Rectangle(left, top, right - left, bottom - top);
    }
}
