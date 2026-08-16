using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using Luminance.Common.DataStructures;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    //PH投矛发射的会追踪的幻影球
    public class PHMutantSphereSmall : MutantSphereSmall, IProjOwnedByBoss<MutantBoss>
    {
        float Angle = 0;
        public int waittime = 85;
        bool SpecialPhase = false;
        int SpecialTimer = 0;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(SpecialPhase);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            SpecialPhase = reader.ReadBoolean();
        }
        public override void AI()
        {
            if (Projectile.ai[0] > -1 && Projectile.ai[0] < Main.maxPlayers)
            {
                const int homingDelay = 20;
                const float desiredFlySpeedInPixelsPerFrame = 5;
                const float amountOfFramesToLerpBy = 20; // minimum of 1, please keep in full numbers even though it's a float!
                int foundTarget = (int)Projectile.ai[0];
                Player p = Main.player[foundTarget];
                if (!SpecialPhase)
                {
                    if (Projectile.ai[1] > homingDelay && Projectile.ai[1] < waittime)
                    {
                        Vector2 desiredVelocity = Projectile.SafeDirectionTo(p.Center) * desiredFlySpeedInPixelsPerFrame;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 1f / amountOfFramesToLerpBy);
                    }
                    if (Projectile.ai[1] == 15 && FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MutantBombSmall>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    }
                    if (Projectile.ai[1] < waittime)
                    {
                        Angle = Projectile.SafeDirectionTo(p.Center + p.velocity * 30f).ToRotation();
                    }
                }
                else
                {
                    SpecialTimer++;
                    if (SpecialTimer > 5)
                    {
                        if (Main.getGoodWorld)
                        {
                            float vel = 80 * (float)Math.Pow(0.96f, SpecialTimer % 40);
                            Projectile.velocity = vel * Vector2.UnitX.RotatedBy(Angle);
                        }
                        else
                        {
                            float vel = 3 * MathHelper.Clamp(SpecialTimer, 0, 50);
                            Projectile.velocity = vel * Vector2.UnitX.RotatedBy(Angle);
                        }
                    }
                }
                Projectile.ai[1]++;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == ModContent.NPCType<MutantBoss>() && Main.npc[i].active)
                {
                    float attack = Main.npc[i].ai[0];
                    if (attack < 10 || attack == 25)
                    {
                        waittime = 85;
                    }
                    else
                    {
                        waittime = 55;
                    }
                }
            }

            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 20;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;
            }
            Projectile.scale = (1f - Projectile.alpha / 255f) * .75f;

            if (++Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame > 1)
                    Projectile.frame = 0;
            }
        }
        public static void EnterSpecialPhase()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].ModProjectile is PHMutantSphereSmall ball)
                {
                    ball.SpecialPhase = true;
                    if (Main.projectile[i].timeLeft < 80)
                    Main.projectile[i].timeLeft = 80;
                    Main.projectile[i].netUpdate = true;
                }
            }
        }
        public override void OnKill(int timeleft)
        {
            //SoundEngine.PlaySound(SoundID.NPCDeath6, Projectile.Center);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 208;
            Projectile.Center = Projectile.position;
            for (int index1 = 0; index1 < 3; ++index1)
            {
                int index2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0.0f, 0.0f, 100, new Color(), 1.5f);
                Main.dust[index2].position = new Vector2(Projectile.width / 2, 0.0f).RotatedBy(6.28318548202515 * Main.rand.NextDouble(), new Vector2()) * (float)Main.rand.NextDouble() + Projectile.Center;
            }
            for (int index1 = 0; index1 < 10; ++index1)
            {
                int index2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, 0.0f, 0.0f, 0, new Color(), 2.5f);
                Main.dust[index2].position = new Vector2(Projectile.width / 2, 0.0f).RotatedBy(6.28318548202515 * Main.rand.NextDouble(), new Vector2()) * (float)Main.rand.NextDouble() + Projectile.Center;
                Main.dust[index2].noGravity = true;
                Dust dust1 = Main.dust[index2];
                dust1.velocity *= 1f;
                int index3 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, 0.0f, 0.0f, 100, new Color(), 1.5f);
                Main.dust[index3].position = new Vector2(Projectile.width / 2, 0.0f).RotatedBy(6.28318548202515 * Main.rand.NextDouble(), new Vector2()) * (float)Main.rand.NextDouble() + Projectile.Center;
                Dust dust2 = Main.dust[index3];
                dust2.velocity *= 1f;
                Main.dust[index3].noGravity = true;
            }
            /*
            if (FargoSoulsUtil.HostCheck) //explosion
                Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MutantBombSmall>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            */
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ModContent.ProjectileType<MutantSphereSmall>()].Value;
            int sizeY = texture.Height / Main.projFrames[ModContent.ProjectileType<MutantSphereSmall>()];
            int sizeX = texture.Width;

            int frameY = Projectile.frame * sizeY;
            int frameX = sizeX;

            Rectangle rectangle = new(frameX, frameY, sizeX, sizeY);
            Vector2 origin = rectangle.Size() / 2f; 

            SpriteEffects spriteEffects = Projectile.spriteDirection > 0 ?
                SpriteEffects.None : SpriteEffects.FlipHorizontally;


            Color color = Color.Aqua;
            // 4. 绘制轨迹拖尾效果
            for (float i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i += 0.33f)
            {
                Color oldColor = color;
                oldColor.A = 50;

                float modifier = (float)(ProjectileID.Sets.TrailCacheLength[ModContent.ProjectileType<MutantSphereSmall>()] - i) /
                                 ProjectileID.Sets.TrailCacheLength[ModContent.ProjectileType<MutantSphereSmall>()];
                oldColor *= modifier;

                float scale = (Projectile.scale / 1) + (Projectile.scale * modifier / 2);

                int max0 = (int)i - 1;
                if (max0 < 0) 
                    continue;

                Vector2 oldPos = Vector2.Lerp(Projectile.oldPos[(int)i],
                    Projectile.oldPos[max0], 1 - i % 1) + (origin / 2);

                // 使用前一个点的旋转角度
                float oldRot = Projectile.oldRot[max0];
                Main.EntitySpriteDraw(texture, oldPos - Main.screenPosition +
                    new Vector2(0f, Projectile.gfxOffY), rectangle, oldColor,
                    oldRot, origin, scale, spriteEffects, 0);
            }

            Asset<Texture2D> line = TextureAssets.Extra[ExtrasID.FairyQueenLance];
            float opacity = 0.55f; // 预警线透明度

            Main.EntitySpriteDraw(
                line.Value, // 纹理：细长的直线
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                null,
                color * opacity * (1 - MathHelper.Clamp(SpecialTimer / 15f, 0, 1)),
                // 通过速度向量的角度确定旋转，使预警线指向弹幕飞行方向
                Angle,

                new Vector2(0, line.Height() * 0.5f),
                new Vector2(0.33f, Projectile.scale * 5),

                SpriteEffects.None
            );
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                rectangle, Color.White * (1 - MathHelper.Clamp(SpecialTimer / 15f, 0, 1)),
                Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            //原始绘制
            Texture2D glow = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/MutantBoss/MutantSphereGlow", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            int rect1 = glow.Height;
            int rect2 = 0;
            Rectangle glowrectangle = new(0, rect2, glow.Width, rect1);
            Vector2 gloworigin2 = glowrectangle.Size() / 2f;
            Color glowcolor = Color.Lerp(FargoSoulsUtil.AprilFools ? Color.Red : new Color(196, 247, 255, 0), Color.Transparent, 0.85f);
            if (SpecialPhase)
            {
                Color acolor = FargoSoulsUtil.AprilFools ? Color.Purple : new Color(138, 177, 255, 0);
                glowcolor = Color.Lerp(glowcolor, acolor, MathHelper.Clamp(SpecialTimer / 15f, 0, 1));
            }
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++) //reused betsy fireball scaling trail thing
            {
                Color color27 = glowcolor;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                float scale = Projectile.scale * (ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i] - Vector2.Normalize(Projectile.velocity) * i * 2;
                Main.EntitySpriteDraw(glow, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle), color27,
                    Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, scale * 1.5f, SpriteEffects.None, 0);
            }
            glowcolor = Color.Lerp(new Color(255, 255, 255, 0), Color.Transparent, 0.8f);
            if (SpecialPhase)
            {
                Color acolor = FargoSoulsUtil.AprilFools ? Color.Purple : new Color(138, 177, 255, 0);
                glowcolor = Color.Lerp(glowcolor, acolor, MathHelper.Clamp(SpecialTimer / 15f, 0, 1));
            }
            Main.EntitySpriteDraw(glow, Projectile.position + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle), glowcolor,
                    Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, Projectile.scale * 1.5f, SpriteEffects.None, 0);

            return false;
        }
    }
}
