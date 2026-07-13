using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantSphereRing : MutantSphereRing
    {/*
        float Angle = 0;
        Vector2 direct = Vector2.Zero;
        int flag = 1;
        int turntimer = 0;
        float speed = 14;
        float timer = 0;*/
        double flag = 0;
        public override void SetDefaults()
        {
            base.Projectile.width = 40;
            base.Projectile.height = 40;
            base.Projectile.hostile = true;
            base.Projectile.ignoreWater = true;
            base.Projectile.tileCollide = false;
            base.Projectile.timeLeft = 480;
            base.Projectile.alpha = 200;
            base.CooldownSlot = 1;
            if (base.Projectile.type == ModContent.ProjectileType<MutantSphereRing>() || base.Projectile.type == ModContent.ProjectileType<PHMutantSphereRing>())
            {
                DieOutsideArena = true;
                base.Projectile.FargoSouls().TimeFreezeImmune = WorldSavingSystem.MasochistModeReal && FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<MutantBoss>()) && Main.npc[EModeGlobalNPC.mutantBoss].ai[0] == -5f;
            }
        }
        public override void AI()
        {
            if (!spawned)
            {
                spawned = true;
                originalSpeed = Projectile.velocity.Length();
            }
            /*
            direct = Vector2.Normalize(Projectile.velocity);
            if (++timer >= 40 && turntimer < 6)
            {
                Projectile.velocity *= 0.96f;
            }
            if (timer % 40 == 0 && timer > 30)
            {
                float detalangle = Projectile.ai[1] * flag * (turntimer == 0 ? MathHelper.PiOver4 : MathHelper.PiOver2);
                flag = flag == -1 ? 1 : -1;
                Projectile.velocity += speed * direct.RotatedBy(detalangle) * (turntimer == 0 ? 1 : 2);
                turntimer++;
            }
            Angle = Projectile.velocity.ToRotation() + Projectile.ai[1] * flag * (turntimer == 0 ? MathHelper.PiOver4 : MathHelper.PiOver2);
            */
            Projectile.localAI[0] += 1f;
            flag += 1f;
            double num = flag;
            if (Projectile.localAI[0] % 60 < 20 && Projectile.localAI[0] > 40)
            {
                flag -= 2f;
                num *= -1;
            }
            Projectile.velocity = originalSpeed * Vector2.Normalize(Projectile.velocity).RotatedBy(Projectile.ai[1] / (Math.PI * 2.0 * Projectile.ai[0] * num));
            #region 其他
            if (base.Projectile.alpha > 0)
            {
                base.Projectile.alpha -= 20;
                if (base.Projectile.alpha < 0)
                {
                    base.Projectile.alpha = 0;
                }
            }

            base.Projectile.scale = 1f - (float)base.Projectile.alpha / 255f;
            if (++base.Projectile.frameCounter >= 6)
            {
                base.Projectile.frameCounter = 0;
                if (++base.Projectile.frame > 1)
                {
                    base.Projectile.frame = 0;
                }
            }

            if (DieOutsideArena)
            {
                if (ritualID == -1)
                {
                    ritualID = -2;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<PHMutantRitual>())
                        {
                            ritualID = i;
                            break;
                        }
                    }
                }

                Projectile projectile = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>());
                if (projectile != null && base.Projectile.Distance(projectile.Center) > 1200f)
                {
                    base.Projectile.timeLeft = 0;
                }
            }

            TryTimeStop();
            #endregion
        }
        public override bool PreDraw(ref Color lightColor)
        {
            /*
            #region 额外绘制
            Texture2D texture = TextureAssets.Projectile[ModContent.ProjectileType<MutantSphereRing>()].Value;
            int sizeY = texture.Height / Main.projFrames[ModContent.ProjectileType<MutantSphereRing>()];
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

                float modifier = (float)(ProjectileID.Sets.TrailCacheLength[ModContent.ProjectileType<MutantSphereRing>()] - i) /
                                 ProjectileID.Sets.TrailCacheLength[ModContent.ProjectileType<MutantSphereRing>()];
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

            Asset<Texture2D> line = TextureAssets.Extra[178];
            float opacity = 0.55f; // 预警线透明度

            Main.EntitySpriteDraw(
                line.Value, // 纹理：细长的直线
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                null,
                color * opacity,
            // 通过速度向量的角度确定旋转，使预警线指向弹幕飞行方向
                Angle,

                new Vector2(0, line.Height() * 0.5f),
                new Vector2(0.33f, Projectile.scale * 5),

                SpriteEffects.None
            );
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                rectangle, Color.White,
                Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            #endregion
            */
            #region 原始绘制
            Texture2D value = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/MutantBoss/MutantSphereGlow", AssetRequestMode.ImmediateLoad).Value;
            int height = value.Height;
            int y = 0;
            Rectangle rectangle = new Rectangle(0, y, value.Width, height);
            Vector2 origin = rectangle.Size() / 2f;
            Color color = Color.Lerp(FargoSoulsUtil.AprilFools ? Color.Red : new Color(196, 247, 255, 0), Color.Transparent, 0.9f);
            color *= base.Projectile.Opacity;
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[base.Projectile.type]; i++)
            {
                Color color2 = color;
                color2 *= (float)(ProjectileID.Sets.TrailCacheLength[base.Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[base.Projectile.type];
                float num = base.Projectile.scale * (float)(ProjectileID.Sets.TrailCacheLength[base.Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[base.Projectile.type];
                Vector2 vector = base.Projectile.oldPos[i] - Vector2.Normalize(base.Projectile.velocity) * i * 6f;
                Main.EntitySpriteDraw(value, vector + base.Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, color2, base.Projectile.velocity.ToRotation() + MathF.PI / 2f, origin, num * 1.5f, SpriteEffects.None);
            }

            color = Color.Lerp(new Color(255, 255, 255, 0), Color.Transparent, 0.85f);
            Main.EntitySpriteDraw(value, base.Projectile.position + base.Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, color, base.Projectile.velocity.ToRotation() + MathF.PI / 2f, origin, base.Projectile.scale * 1.5f, SpriteEffects.None);
            return false;
            #endregion
        }
    }
}
