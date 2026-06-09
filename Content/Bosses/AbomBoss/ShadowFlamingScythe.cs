using System;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss;

public class ShadowFlamingScythe : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_329";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[base.Projectile.type] = 6;
        ProjectileID.Sets.TrailingMode[base.Projectile.type] = 2;
    }

    public override void SetDefaults()
    {
        base.Projectile.width = 80;
        base.Projectile.height = 80;
        base.Projectile.aiStyle = -1;
        base.Projectile.hostile = true;
        base.Projectile.timeLeft = 180;
        base.CooldownSlot = 1;
        base.Projectile.light = 0.25f;
        base.Projectile.tileCollide = false;
        base.Projectile.hide = true;
        base.Projectile.penetrate = -1;
    }

    public override void AI()
    {
        if (base.Projectile.localAI[0] == 0f)
        {
            base.Projectile.hide = false;
            base.Projectile.rotation = Main.rand.NextFloat(MathF.PI / 2f);
            base.Projectile.direction = (base.Projectile.spriteDirection = (Main.rand.NextBool() ? 1 : (-1)));
            SoundEngine.PlaySound(in SoundID.Item8, base.Projectile.Center);
        }

        if ((base.Projectile.localAI[0] += 1f) < 160f)
        {
            base.Projectile.velocity *= 1.025f;
        }

        if (base.Projectile.ai[0] == 0f)
        {
            if (base.Projectile.localAI[0] == 140f)
            {
                base.Projectile.Kill();
            }
        }
        /*
        else if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.championBoss, ModContent.NPCType<FragowiltasSouls.Content.Bosses.Champions.ShadowChampion>()) && Main.npc[EModeGlobalNPC.championBoss].HasValidTarget)
        {
            float curAngle = base.Projectile.velocity.ToRotation();
            float targetAngle = (Main.player[Main.npc[EModeGlobalNPC.championBoss].target].Center - base.Projectile.Center).ToRotation();
            base.Projectile.velocity = new Vector2(base.Projectile.velocity.Length(), 0f).RotatedBy(curAngle.AngleLerp(targetAngle, 0.035f));
        }
        */
        base.Projectile.rotation += base.Projectile.velocity.Length() * 0.015f * (float)Math.Sign(base.Projectile.velocity.X);
    }

    public override void OnKill(int timeLeft)
    {
        /*
        if (FargoSoulsUtil.HostCheck)
        {
            for (int i = -1; i <= 1; i++)
            {
                Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), base.Projectile.Center, base.Projectile.velocity.RotatedBy(MathHelper.ToRadians(45f) * (float)i), base.Projectile.type, (int)((double)base.Projectile.damage / 3.0 * 4.0), 0f, base.Projectile.owner, 1f);
            }
        }

        for (int j = 0; j < 36; j++)
        {
            Vector2 vector = (Vector2.UnitX * 10f).RotatedBy((float)(j - 17) * (MathF.PI * 2f) / 36f) + base.Projectile.Center;
            Vector2 vector2 = vector - base.Projectile.Center;
            int num = Dust.NewDust(vector + vector2, 0, 0, 6, 0f, 0f, 0, default(Color), 3f);
            Main.dust[num].noGravity = true;
            Main.dust[num].velocity = vector2;
        }
        */
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(22, 300);
        if (WorldSavingSystem.EternityMode)
        {
            target.AddBuff(ModContent.BuffType<ShadowflameBuff>(), 300);
            target.AddBuff(80, 300);
            target.AddBuff(24, 900);
            target.AddBuff(ModContent.BuffType<LivingWastelandBuff>(), 900);
        }
    }

    public override Color? GetAlpha(Color lightColor)
    {
        return Color.White;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
        int num = TextureAssets.Projectile[base.Projectile.type].Value.Height / Main.projFrames[base.Projectile.type];
        int y = num * base.Projectile.frame;
        Rectangle rectangle = new Rectangle(0, y, value.Width, num);
        Vector2 origin = rectangle.Size() / 2f;
        Color newColor = lightColor;
        newColor = base.Projectile.GetAlpha(newColor);
        SpriteEffects effects = ((base.Projectile.spriteDirection >= 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        if (base.Projectile.ai[0] != 0f)
        {
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[base.Projectile.type]; i++)
            {
                Color color = Color.White * base.Projectile.Opacity * 0.75f * 0.5f;
                color *= (float)(ProjectileID.Sets.TrailCacheLength[base.Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[base.Projectile.type];
                Vector2 vector = base.Projectile.oldPos[i];
                float rotation = base.Projectile.oldRot[i];
                Main.EntitySpriteDraw(value, vector + base.Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, color, rotation, origin, base.Projectile.scale, effects);
            }
        }

        Main.EntitySpriteDraw(value, base.Projectile.Center - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, base.Projectile.GetAlpha(lightColor), base.Projectile.rotation, origin, base.Projectile.scale, effects);
        return false;
    }
}