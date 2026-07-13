using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantMark1 : MutantMark1
    {
        //会发射激光的幻影球
        public override void AI()
        {
            //Projectile.hostile = false;
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center + Projectile.velocity * Projectile.timeLeft, Vector2.Normalize(Projectile.velocity).RotatedBy(Projectile.ai[0]), ModContent.ProjectileType<PHMutantDeathraySmall>(), Projectile.damage, 0f, Projectile.owner);
                    Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center + Projectile.velocity * Projectile.timeLeft, -Vector2.Normalize(Projectile.velocity).RotatedBy(Projectile.ai[0]), ModContent.ProjectileType<PHMutantDeathraySmall>(), Projectile.damage, 0f, Projectile.owner);
                }     
            }
            //Projectile.velocity *= 0.96f;
            if (++Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame > 1)
                    Projectile.frame = 0;
            }
        }
        public override void OnKill(int timeleft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath6, Projectile.Center);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 208;
            Projectile.Center = Projectile.position;

            if (FargoSoulsUtil.HostCheck)
            {
                Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, Vector2.Normalize(Projectile.velocity).RotatedBy(Projectile.ai[0]), ModContent.ProjectileType<MutantDeathray1>(), Projectile.damage, 0f, Projectile.owner);
                Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, -Vector2.Normalize(Projectile.velocity).RotatedBy(Projectile.ai[0]), ModContent.ProjectileType<MutantDeathray1>(), Projectile.damage, 0f, Projectile.owner);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glow = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/MutantBoss/MutantSphereGlow", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            int rect1 = glow.Height;
            int rect2 = 0;
            Rectangle glowrectangle = new(0, rect2, glow.Width, rect1);
            Vector2 gloworigin2 = glowrectangle.Size() / 2f;
            Color glowcolor = Color.Lerp(FargoSoulsUtil.AprilFools ? Color.Red : new Color(255, 255, 255, 0), Color.Transparent, 0.85f);

            if (WorldSavingSystem.MasochistModeReal)
            {
                Asset<Texture2D> line = TextureAssets.Extra[178];
                float opacity = 1f;
                Main.EntitySpriteDraw(line.Value, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), null, Color.Cyan * opacity, Projectile.velocity.ToRotation() + Projectile.ai[0], new Vector2(0, line.Height() * 0.5f),
                    new Vector2(0.3f, Projectile.scale * 7), SpriteEffects.None);
                Main.EntitySpriteDraw(line.Value, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), null, Color.Cyan * opacity, Projectile.velocity.ToRotation() + Projectile.ai[0] + MathHelper.Pi, new Vector2(0, line.Height() * 0.5f),
                    new Vector2(0.3f, Projectile.scale * 7), SpriteEffects.None);
            }

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++) //reused betsy fireball scaling trail thing
            {

                Color color27 = glowcolor;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                float scale = Projectile.scale * (ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                Main.EntitySpriteDraw(glow, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle), color27,
                    Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, scale * 1.5f, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(glow, Projectile.position + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle), new Color(255, 255, 255, 200),
                    Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, Projectile.scale * 1.5f, SpriteEffects.None, 0);

            return false;
        }
    }
}
