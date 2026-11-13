using FargowiltasSouls.Assets.ExtraTextures;
using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.Shaders;
using Monochrome.Core.Graphics.Trails;

namespace FargosPhantasmMode.Content.Bosses.Coffin;
/// <summary>
/// ai0减速时间,ai1减速后的速度，ai2计时器
/// </summary>
public class P_CoffinWaveShot : ModProjectile, IMonoPixelatedPrimitive
{
    public override string Texture => "FargowiltasSouls/Content/Bosses/CursedCoffin/CoffinWaveShot";
    ref float RelaxTime => ref Projectile.ai[0];
    ref float Speed => ref Projectile.ai[1];
    ref float Timer => ref Projectile.ai[2];
    Vector2 vecdir;
    MonoTrail trail;
    public override void SetStaticDefaults()
    {
        // DisplayName.SetDefault("Banished Baron Scrap");
        ProjectileID.Sets.TrailCacheLength[Type] = 8;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.width = 18;
        Projectile.height = 18;
        Projectile.aiStyle = -1;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.scale = 1f;
        Projectile.light = 1;
        Projectile.timeLeft = 60 * 3;
    }
    public override void AI()
    {
        if (Projectile.localAI[0] < 12)
        {
            vecdir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.localAI[0]++;
            Projectile.scale = MathHelper.Lerp(0, 1, Projectile.localAI[0] / 12);
        }
        if (Timer < RelaxTime)
        {
            Projectile.velocity *= 0.95f;
        }
        else if (Timer == RelaxTime)
            Projectile.velocity = vecdir * Speed;
        else
        {
            float rotStr = Main.getGoodWorld ? 0.075f : 0.06f;
            float rot = MathHelper.PiOver2 * rotStr * MathF.Sin(MathF.Tau * ((Timer - RelaxTime) / 50f));
            Projectile.velocity = Projectile.velocity.RotatedBy(rot);

            float accel = 1.012f;
            if (Projectile.velocity.Length() < 11f && false)
                Projectile.velocity *= accel;
        }
        Timer++;
        EnsureTrail();
    }
    protected void EnsureTrail()
    {
        if (Main.dedServ)
            return;
        trail ??= new()
        {
            Texture = FargosTextureRegistry.FadedStreak.Value,
            EffectBinding = new Monochrome.Core.Graphics.Primitives.MonoEffectBinding(
                    PModeShaders.Library.GetShader("FargowiltasSouls.BlobTrail").Resource),
            WidthFunction = WidthFunction,
            ColorFunction = ColorFunction,
            Offset = Projectile.Size * 0.5f,
            PointsToCreate = 16
        };
    }
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(FBuffID.ShadowflameBuff, 60 * 4);
    }
    private static readonly Color GlowColor = new(224, 196, 252, 0);
    public override bool PreDraw(ref Color lightColor)
    {
        //draw projectile
        Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
        int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
        int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
        Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
        Vector2 origin2 = rectangle.Size() / 2f;
        Vector2 drawOffset = Projectile.rotation.ToRotationVector2() * (texture2D13.Width - Projectile.width) / 2;

        SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Main.EntitySpriteDraw(texture2D13, Projectile.Center + drawOffset - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, effects, 0);

        return false;
    }
    public override void OnKill(int timeLeft)
    {
        for (int index1 = 0; index1 < 40; ++index1)
        {
            int index2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3));
            Main.dust[index2].noGravity = true;
            Dust dust1 = Main.dust[index2];
            dust1.velocity *= 3f;
        }
    }
    public float WidthFunction(float t) => MathHelper.SmoothStep(0, Projectile.scale * Projectile.width * 0.625f, 1 - t);
    public Color ColorFunction(float t) => Color.Lerp(Color.Lerp(Color.Lerp(Color.MediumPurple, Color.DeepPink, 0.5f), GlowColor, 0.5f), GlowColor with { A = 100 } * 0.8f, t);
    public void RenderPixelatedPrimitives()
    {
        trail?.DrawFrom(Projectile.oldPos);
    }
}
