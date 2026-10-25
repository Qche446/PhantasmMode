using FargowiltasSouls.Content.WorldGeneration;
using Monochrome.Core.Graphics;

namespace FargosPhantasmMode.Content.Bosses.Coffin;

/// <summary>
/// 棺材横向冲刺撞墙时沿竖直墙面爬行的冲击波。
/// <para>
/// <c>ai[0]</c> 是墙面方向（±1），<c>velocity.Y</c> 决定爬行方向与速度。
/// X 固定在墙面内侧随速度只走 Y：每帧检查这一行的墙面是否还连着，
/// 断开（通道口）时淡出，爬到场地上下边界时消失。
/// </para>
/// </summary>
public class CoffinWallShockwave : ModProjectile
{
    public override string Texture => "FargowiltasSouls/Content/Bosses/CursedCoffin/CoffinSlamShockwave";

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 3;
        ProjectileID.Sets.TrailCacheLength[Type] = 8;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = 40;
        Projectile.height = 34;
        Projectile.aiStyle = -1;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.light = 0.6f;
        Projectile.timeLeft = 60 * 4;
    }
    private ref float Side => ref Projectile.ai[0];

    public override void AI()
    {
        if (Side == 0f)
            Side = 1f;

        Projectile.velocity.X = 0f;
        Projectile.rotation = Projectile.velocity.Y >= 0f ? MathHelper.PiOver2 : -MathHelper.PiOver2;

        float wallX = CoffinArena.FightCenter.X + Side * (CoffinArena.VectorWidth / 2f - 20f);
        if (Math.Abs(Projectile.Center.X - wallX) > 2f)
            Projectile.position.X = wallX - Projectile.width / 2f;

        if (++Projectile.frameCounter >= 4)
        {
            Projectile.frameCounter = 0;
            Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
        }

        if (!WallIsThere(wallX))
        {
            Projectile.Opacity -= 0.08f;
            if (Projectile.Opacity <= 0f)
                Projectile.Kill();
        }
        else
            Projectile.Opacity = 1f;

        if (Math.Abs(Projectile.Center.Y - CoffinArena.FightCenter.Y) > CoffinArena.VectorHeight / 2f)
            Projectile.Kill();

        if (Main.rand.NextBool(2))
        {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Sand, 0f, Projectile.velocity.Y * 0.2f, 0, default, 1.3f);
            dust.noGravity = true;
        }
    }
    /// <summary>检查这一行的墙面还在不在：往墙里探 10px，取那一格瓦片。</summary>
    private bool WallIsThere(float wallX)
    {
        Point tile = new Vector2(wallX + Side * 10f, Projectile.Center.Y).ToTileCoordinates();
        if (!WorldGen.InWorld(tile.X, tile.Y, 2))
            return false;
        Tile t = Main.tile[tile.X, tile.Y];
        return t.HasUnactuatedTile && Main.tileSolid[t.TileType];
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
        int frameHeight = texture.Height / Main.projFrames[Type];
        Rectangle rectangle = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
        Vector2 origin = rectangle.Size() / 2f;
        Vector2 scale = new Vector2(2.2f, 0.9f) * Projectile.scale;
        SpriteEffects effects = Projectile.velocity.Y >= 0f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        Main.spriteBatch.UseBlendState(BlendState.Additive);
        for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
        {
            Color trailColor = Color.Lerp(Color.White, Color.Purple, 0.35f);
            trailColor *= (ProjectileID.Sets.TrailCacheLength[Type] - i) / (float)ProjectileID.Sets.TrailCacheLength[Type] * Projectile.Opacity * 0.5f;
            Vector2 oldPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Main.EntitySpriteDraw(texture, oldPos, rectangle, trailColor, Projectile.oldRot[i], origin, scale, effects, 0);
        }
        Main.spriteBatch.ResetToDefault();

        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), rectangle,
            Color.White * Projectile.Opacity, Projectile.rotation, origin, scale, effects, 0);
        return false;
    }
}
