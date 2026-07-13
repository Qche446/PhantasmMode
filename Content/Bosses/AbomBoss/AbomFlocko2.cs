using FargowiltasSouls;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss
{
    public class AbomFlocko2 : AbomFlocko
    {
        public override string Texture => "Terraria/Images/NPC_352";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.projFrames[Projectile.type] = Main.npcFrameCount[NPCID.Flocko];
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            if (Projectile.ai[0] < 0 || Projectile.ai[0] >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player player = Main.player[(int)Projectile.ai[0]];

            
            // 寻找AbomBoss
            NPC abomBoss = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>())
                {
                    abomBoss = Main.npc[i];
                    break;
                }
            }
            if (abomBoss == null)
            {
                Projectile.Kill();
                return;
            }

            // 如果找不到Boss，使用玩家位置作为备用目标
            Vector2 bossCenter = abomBoss.Center;
            // 绕玩家旋转的逻辑
            float rotationRadius = 1100f;
            float rotationSpeed = 0.02f; // 角速度

            // 使用localAI[2]存储旋转角度
            float currentAngle = Projectile.localAI[2] + rotationSpeed;
            Projectile.localAI[2] = currentAngle;

            // 计算目标位置（绕玩家旋转）
            Vector2 targetPosition = bossCenter + new Vector2(
                (float)Math.Cos(currentAngle + MathHelper.ToRadians(Projectile.ai[1])) * rotationRadius,
                (float)Math.Sin(currentAngle + MathHelper.ToRadians(Projectile.ai[1])) * rotationRadius
            );

            // 平滑移动到目标位置
            Vector2 direction = targetPosition - Projectile.Center;
            float distance = direction.Length();

            if (distance > 10f)
            {
                direction.Normalize();
                float moveSpeed = MathHelper.Clamp(distance * 0.1f, 5f, 15f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * moveSpeed, 0.1f);
            }
            else
            {
                Projectile.velocity *= 0.95f;
            }

            // 限制最大速度
            if (Projectile.velocity.Length() > 15f)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 15f;

            // 修改后的弹幕发射逻辑
            if (++Projectile.localAI[0] > 60)
            {
                float waveSpeed = WorldSavingSystem.MasochistModeReal ? 7f : 5f;

                // 根据阶段调整发射间隔和角度
                int fireInterval;
                float angleSpread;

                // P2阶段（Projectile.ai[2] > 1）使用更短的间隔和更小的角度差
                if (Projectile.ai[2] > 1)
                {
                    fireInterval = 5; // P2阶段每5帧发射一次
                    angleSpread = 40f; // 角度差从55度减少到40度
                }
                else
                {
                    fireInterval = Main.zenithWorld ? 15 : 30 ; // 非P2阶段保持30帧间隔
                    angleSpread = 55f; // 非P2阶段保持55度角度差
                }

                // 发射逻辑
                if (++Projectile.localAI[1] > fireInterval)
                {
                    Projectile.localAI[1] = 0f;
                    Projectile.frameCounter = 15;
                    SoundEngine.PlaySound(SoundID.Item120, Projectile.position);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 vel = Projectile.SafeDirectionTo(bossCenter) * waveSpeed;
                        float iter = 1;
                        if (WorldSavingSystem.MasochistModeReal)
                            iter = 0.5f;
                        for (float i = -1; i <= 1; i += iter)
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel.RotatedBy(MathHelper.ToRadians(angleSpread) * i), ModContent.ProjectileType<AbomFrostWave>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    }
                }
            }

            // 视觉效果
            Projectile.rotation = System.Math.Min(MathHelper.PiOver2, Projectile.velocity.X / 16f);
            Projectile.frame = 0;
            if (--Projectile.frameCounter > 0)
                Projectile.frame = Projectile.velocity.X > 0 ? 1 : 2; // 根据移动方向显示不同帧
        }
    }
}