using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    public class DarkStar : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public override string Texture => "Terraria/Images/Projectile_12";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dark Star");

            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.aiStyle = -1;
            Projectile.alpha = 50;
            //Projectile.light = 1f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
            Projectile.hostile = true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;

            Rectangle trailHitbox = projHitbox;
            trailHitbox.X = (int)Projectile.oldPosition.X;
            trailHitbox.Y = (int)Projectile.oldPosition.Y;
            if (trailHitbox.Intersects(targetHitbox))
                return true;

            return false;
        }

        bool lastSecondAccel;

        public override void AI()
        {
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = 60 + Main.rand.Next(60);
                //SoundEngine.PlaySound(SoundID.Item9, Projectile.position);
            }

            if (Projectile.localAI[1] == 0)
            {
                Projectile.localAI[1] = 1f;
                SoundStyle style = MechElectricOrb.ShotSound with
                {
                    Volume = 0.3f,
                    MaxInstances = 4
                };
                SoundEngine.PlaySound(in style, base.Projectile.position);
                //doing it this way so projs that inherit from dark star dont inherit the accel
                lastSecondAccel = Projectile.type == ModContent.ProjectileType<DarkStar>();
            }

            if (Projectile.localAI[0] == 0)
                Projectile.localAI[0] = 1f;
            Projectile.alpha += (int)(25.0 * Projectile.localAI[0]);
            if (Projectile.alpha > 200)
            {
                Projectile.alpha = 200;
                Projectile.localAI[0] = -1f;
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
                Projectile.localAI[0] = 1f;
            }

            Projectile.rotation = Projectile.rotation + (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * Projectile.direction;

            if (Main.rand.NextBool(30))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Enchanted_Pink, (float)(Projectile.velocity.X * 0.5), (float)(Projectile.velocity.Y * 0.5), 150, default, 1.2f);
            if (Main.rand.NextBool(3))
            {
                Vector2 vector = Vector2.Normalize(-base.Projectile.velocity.RotatedByRandom(0.62831854820251465));
                float num = Math.Max(4f, base.Projectile.velocity.Length() / 2f);
                Particle particle = new ElectricSpark(base.Projectile.Center, vector * num, Color.OrangeRed * 0.7f, Main.rand.NextFloat(0.7f, 1f), 20);
                particle.Spawn();
            }
            /*
            for (int i = 0; i < 3; i++) 
            {
                Vector2 vector = Vector2.Normalize(base.Projectile.velocity.RotatedByRandom(0.62831854820251465));
                float num = Math.Max(4f, base.Projectile.velocity.Length() / 2f);
                CosmicFireMetaBall cosmicFireMetaBall = ModContent.GetInstance<CosmicFireMetaBall>();
                cosmicFireMetaBall.CreateParticle(Projectile.Center, vector, 80);
            }
            */
            Lighting.AddLight(Projectile.Center, 0.9f, 0.8f, 0.1f);

            if (lastSecondAccel && Projectile.ai[0] == -1 && --Projectile.ai[1] < 0)
                Projectile.velocity *= 1.03f;

            //cap proj velocity so to reduce the gap in its hitbox
            float ratio = Projectile.velocity.Length() / (Projectile.width * 3);
            if (ratio > 1)
                Projectile.velocity /= ratio;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.primeBoss, NPCID.SkeletronPrime))
                target.AddBuff(ModContent.BuffType<NanoInjectionBuff>(), 360);
            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.destroyBoss, NPCID.TheDestroyer))
                target.AddBuff(BuffID.Electrified, 60);
            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.retiBoss, NPCID.Retinazer))
                target.AddBuff(ModContent.BuffType<PhSublimationBuff>(), 180);
            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.spazBoss, NPCID.Spazmatism))
                target.AddBuff(ModContent.BuffType<PhSublimationBuff>(), 180);
            target.AddBuff(ModContent.BuffType<OiledBuff>(), 300);
        }

        public override void OnKill(int timeLeft)
        {
            //SoundEngine.PlaySound(SoundID.Item10, Projectile.position);

            //Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 58, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 150, new Color(), 1.2f);
            /*int Type = Main.rand.Next(16, 18);
            if (Projectile.type == 503)
                Type = 16;
            if (!Main.dedServ)
                Gore.NewGore(Projectile.position, new Vector2(Projectile.velocity.X * 0.05f, Projectile.velocity.Y * 0.05f), Type, 1f);*/

            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Enchanted_Gold, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 150, new Color(), 1.2f);
            if (!Main.dedServ)
                Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position, new Vector2(Projectile.velocity.X * 0.05f, Projectile.velocity.Y * 0.05f), Main.rand.Next(16, 18), 1f);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 100, 100, lightColor.A - Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glow = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Bosses/VanillaEternity/Twins/DarkStar_Glow", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            int rect1 = glow.Height / Main.projFrames[Projectile.type];
            int rect2 = rect1 * Projectile.frame;
            Rectangle glowrectangle = new(0, rect2, glow.Width, rect1);
            Vector2 gloworigin2 = glowrectangle.Size() / 2f;
            Color glowcolor = Color.Lerp(new Color(255, 100, 100, 150), Color.Transparent, 0.8f);
            Vector2 drawCenter = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 14;

            Main.EntitySpriteDraw(glow, drawCenter - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle),//create small, non transparent trail texture
                   Projectile.GetAlpha(lightColor), Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, Projectile.scale / 2, SpriteEffects.None, 0);

            for (int i = 0; i < 3; i++) //create multiple transparent trail textures ahead of the projectile
            {
                Vector2 drawCenter2 = drawCenter + (Projectile.velocity.SafeNormalize(Vector2.UnitX) * 12).RotatedBy(MathHelper.Pi / 5 - i * MathHelper.Pi / 5); //use a normalized version of the projectile's velocity to offset it at different angles
                drawCenter2 -= Projectile.velocity.SafeNormalize(Vector2.UnitX) * 12; //then move it backwards
                Main.EntitySpriteDraw(glow, drawCenter2 - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle),
                    glowcolor, Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, Projectile.scale, SpriteEffects.None, 0);
            }

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++) //reused betsy fireball scaling trail thing
            {

                Color color27 = glowcolor;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                float scale = Projectile.scale * (ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i] - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 14;
                Main.EntitySpriteDraw(glow, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle), color27,
                    Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, scale, SpriteEffects.None, 0);
            }

            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);

        }
        public float WidthFunction(float completionRatio)
        {
            float baseWidth = Projectile.scale * Projectile.width * 1.3f;
            return MathHelper.SmoothStep(baseWidth, 3.5f, completionRatio);
        }

        public Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(Color.OrangeRed, Color.Transparent, completionRatio) * 0.6f;
        }
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, _ => Projectile.Size * 0.5f, Pixelate: true, Shader: shader));
        }
    }
}
