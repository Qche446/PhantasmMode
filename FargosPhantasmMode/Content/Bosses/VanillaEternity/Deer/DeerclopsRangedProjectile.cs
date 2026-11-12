using FargowiltasSouls.Content.Buffs.Masomode;
using Terraria.GameContent;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Deer;

public class DeerclopsRangedProjectile : ModProjectile
{
    //ai[0]存活帧数,满了5帧才开始下落; ai[1]造型(0~11); ai[2]生成后等待多少帧开始蓄力,0表示不蓄力
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeerclopsRangedProjectile;

    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 32;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 220;
        Projectile.coldDamage = true;
        Projectile.extraUpdates = 1;
        Projectile.light = 1f;
    }

    public override void AI()
    {
        Projectile.frame = (int)Projectile.ai[1];
        if (Projectile.localAI[0] == 0f)
        {
            Projectile.localAI[0] = 1f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.ai[2] > 0f)
                Projectile.timeLeft += (int)Projectile.ai[2];
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.Cloud, Projectile.velocity * MathHelper.Lerp(0.2f, 0.7f, Main.rand.NextFloat()));
                dust.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                dust.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
            }
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.Cloud, Main.rand.NextVector2Circular(2f, 2f) + Projectile.velocity * MathHelper.Lerp(0.2f, 0.5f, Main.rand.NextFloat()));
                dust.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                dust.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
                dust.fadeIn = 1f;
            }
        }
        if (Projectile.ai[2] <= 0f)
        {
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] >= 5f)
                Projectile.velocity.Y += 0.15f;
            return;
        }

        //一次更新等于几帧,extraUpdates 会让 AI 一帧跑多次
        float step = 1f / (Projectile.extraUpdates + 1f);
        Projectile.localAI[1] += step;
        float wait = Projectile.ai[2];

        if (Projectile.localAI[1] <= wait)
        {
            Projectile.velocity.Y += 0.15f * step;
        }
        else if (Projectile.localAI[1] <= wait + 20f)
        {
            //20帧内减速到悬停
            Projectile.velocity *= 1f - 0.15f * step;

            Vector2 offset = Main.rand.NextVector2CircularEdge(80f, 80f);
            Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Cloud, offset * -0.06f);
            dust.noGravity = true;
            dust.scale = 1.2f;
        }
        else if (Projectile.localAI[2] == 0f && Projectile.localAI[1] >= wait + 40f)
        {
            Projectile.localAI[2] = 1f;
            if (Main.netMode != NetmodeID.MultiplayerClient)
                SpawnSpikes();
            Projectile.Kill();
        }
    }

    private void SpawnSpikes()
    {
        Player target = null;
        foreach (Player player in Main.ActivePlayers)
        {
            if (player.dead)
                continue;
            if (target == null || player.Distance(Projectile.Center) < target.Distance(Projectile.Center))
                target = player;
        }
        if (target == null)
            return;

        Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(-Vector2.UnitY);
        float ai1 = MathHelper.Clamp(Vector2.Distance(Projectile.Center, target.Center) / 200f, 1f, 2f);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, direction, PProjID.DeerclopsIceSpike, Projectile.damage, 0f, Main.myPlayer, -40f, ai1, 0f);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, -direction, PProjID.DeerclopsIceSpike, Projectile.damage, 0f, Main.myPlayer, -40f, ai1, 0f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
        Rectangle frame = texture.Frame(3, 4, Projectile.frame % 3, Projectile.frame / 3);
        Vector2 origin = frame.Size() / 2f;
        Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);

        float fade = Utils.Remap(Projectile.ai[0], 0f, 30f, 1f, 0f);
        Color glow = Projectile.GetAlpha(Color.White) * fade * fade * 0.3f;
        glow.A = 0;
        for (int i = 0; i < 4; i++)
            Main.EntitySpriteDraw(texture, drawPos + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2 * i) * 2f * Projectile.scale, frame, glow, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);

        Main.EntitySpriteDraw(texture, drawPos, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 20; i++)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 16f) * Projectile.scale, DustID.Cloud, Main.rand.NextVector2Circular(2f, 2f));
            dust.velocity += Projectile.velocity * 0.1f;
            dust.scale = 1f;
            dust.alpha = 100;
        }
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(BuffID.Frostburn, 90);
        target.AddBuff(ModContent.BuffType<MarkedforDeathBuff>(), 900);
        target.AddBuff(ModContent.BuffType<HypothermiaBuff>(), 1200);
    }
}
