using FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins;
using FargowiltasSouls;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.BossWeapons;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Destroyer
{
    public class PHDestroyer : FargowiltasSouls.Content.Bosses.VanillaEternity.Destroyer
    {
        public float lightning = 0;
        public bool Phase2 = false;
        int Timer = 0;
        int State = 0;
        int Flag = 0;
        public override bool SafePreAI(NPC npc)
        {
            
            if (lightning > 0)
            {
                lightning -= 0.025f;
            }
            
            if (Phase2 == false && npc.life < 0.9f * npc.lifeMax)
            {
                Phase2 = true;
                Timer = 0;
                State = -1;
            }//辅助P1进P2
            Player player = Main.player[npc.target];
            npc.localAI[2] = 0;
            switch (State)
            {
                case -1:
                    if (FargoSoulsUtil.HostCheck)
                    {
                        float maxSpeed = 24 * Math.Abs((float)Timer - 120f) / 120f;
                        float num15 = 0.2f;   // 转向加速度
                        float num16 = 0.3f;  // 直线加速度
                        MovementAI(npc, player.Center, num15, num16, maxSpeed);
                        if (++Timer >= 120)
                        {
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched, Main.player[npc.target].Center);
                            Timer = 0;
                            State = 1;
                            lightning = 1;
                            if (!Main.raining)
                            {
                                Main.raining = true;
                            }
                            Main.maxRaining = 0.5f;
                        }
                    }
                    break;//P1进P2
                case 0:
                    if (FargoSoulsUtil.HostCheck)
                    {
                        float maxSpeed = 24f;
                        float num15 = 0.2f;   // 转向加速度
                        float num16 = 0.3f;  // 直线加速度
                                              // 如果玩家速度比蠕虫最大速度大且蠕虫大致面对玩家，则提升最大速度以追上玩家
                        float comparisonSpeed = Main.player[npc.target].velocity.Length() * 2f;
                        float rotationDifference = MathHelper.WrapAngle(npc.velocity.ToRotation() - npc.SafeDirectionTo(Main.player[npc.target].Center).ToRotation());
                        bool inFrontOfMe = Math.Abs(rotationDifference) < MathHelper.ToRadians(90 / 2);
                        if (maxSpeed < comparisonSpeed && inFrontOfMe)
                        {
                            maxSpeed = comparisonSpeed;
                        }
                        float distance = npc.Distance(Main.player[npc.target].Center);
                        if (distance < 600)      // 靠近玩家时降低速度
                        {
                            maxSpeed *= 0.8f;
                            num15 *= 0.5f;
                            if (Timer % 30 == 0)
                            {
                                float randangle = Main.rand.NextFloat(0, MathHelper.PiOver4);
                                float randvel = Main.rand.NextFloat(0.6f, 1.2f);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, randvel * npc.velocity.RotatedBy(randangle), ModContent.ProjectileType<MechElectricOrbCharged>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer, 0, -0.05f, MechElectricOrb.Blue);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, randvel * npc.velocity.RotatedBy(-randangle), ModContent.ProjectileType<MechElectricOrbCharged>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer, 0, -0.05f, MechElectricOrb.Blue);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, 1.5f * randvel * npc.velocity, ModContent.ProjectileType<MechElectricOrbCharged>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer, 0, 2, MechElectricOrb.Blue);
                            }
                        }
                        else if (distance > 900)    // 远离玩家时加速追击
                        {
                            num15 *= 2f;
                            num16 *= 2f;
                        }
                        MovementAI(npc,player.Center, num15, num16, maxSpeed);
                        /*
                        if (Timer % 120 == 0)
                        {
                            int p = Projectile.NewProjectile(npc.GetSource_FromAI(), player.Center - 1800 * Vector2.UnitY, 3 * Vector2.UnitY, ModContent.ProjectileType<Lightning>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer,ai0:MathHelper.PiOver2,ai2:0);
                            SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0"), Main.projectile[p].Center);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), player.Center - 1800 * Vector2.UnitY + 450 * Vector2.UnitX, 3 * Vector2.UnitY, ModContent.ProjectileType<Lightning>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer, ai0: MathHelper.PiOver2, ai2: 0);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), player.Center - 1800 * Vector2.UnitY - 450 * Vector2.UnitX, 3 * Vector2.UnitY, ModContent.ProjectileType<Lightning>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer, ai0: MathHelper.PiOver2, ai2: 0);
                        }
                        *///放电
                        if (++Timer >= 480)
                        {
                            Timer = 0;
                            State = 0;
                            Flag = 0;
                        }
                    }//P1常态hp<0.9
                    break;//P1
                case 1:
                    if (FargoSoulsUtil.HostCheck)
                    {
                        float maxSpeed = 30f;
                        float num15 = 0.3f;   // 转向加速度
                        float num16 = 0.5f;  // 直线加速度
                        // 如果玩家速度比蠕虫最大速度大且蠕虫大致面对玩家，则提升最大速度以追上玩家
                        float comparisonSpeed = Main.player[npc.target].velocity.Length() * 2f;
                        float rotationDifference = MathHelper.WrapAngle(npc.velocity.ToRotation() - npc.SafeDirectionTo(Main.player[npc.target].Center).ToRotation());
                        bool inFrontOfMe = Math.Abs(rotationDifference) < MathHelper.ToRadians(90 / 2f);
                        if (maxSpeed < comparisonSpeed && inFrontOfMe)
                        {
                            maxSpeed = comparisonSpeed;
                        }
                        float distance = npc.Distance(Main.player[npc.target].Center);
                        if (distance < 600)      // 靠近玩家时降低速度
                        {
                            num15 *= 0.2f;
                        }
                        else if (distance > 1000)    // 远离玩家时加速追击
                        {
                            num15 *= 6f;
                            num16 *= 5f;
                        }
                        MovementAI(npc, player.Center, num15, num16, maxSpeed);
                        if (++Timer >= 360)
                        {
                            Timer = 0;
                            State = 2;
                            SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0"), player.Center);
                            ScreenShakeSystem.StartShake(8f);
                        }
                    }
                    break;//P2开始,快速连冲
                case 2:
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (Timer == 0)
                        {
                            SoundEngine.PlaySound(ScanSound with { Pitch = 0.5f, Volume = 2f }, Main.player[npc.target].Center);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 6, npc.whoAmI);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 6, npc.whoAmI);
                        }
                        int P = Timer < 50 ? 7 : Timer < 70 ? 3 : Timer < 90 ? 2 : 1;
                        if (Main.rand.NextBool(P))
                        {
                            Vector2 dustPos = player.Center + Main.rand.NextVector2Circular(10, 20);
                            Vector2 dustVel = player.velocity * 0.3f + Main.rand.NextVector2Circular(0.5f, 0.5f);
                            int dust = Dust.NewDust(dustPos, 0, 0, DustID.Electric, dustVel.X, dustVel.Y, 100, Color.LightBlue, 1f);
                            Main.dust[dust].noGravity = true;
                            Main.dust[dust].velocity *= 0.8f;
                            Main.dust[dust].fadeIn = 0.8f;
                        }
                        float maxSpeed = 4;
                        float num15 = 0.08f;   // 转向加速度
                        float num16 = 0.2f;  // 直线加速度

                        if (Timer % 40 == 0 && Timer >= 150 && Timer < 350)
                        {
                            for (int i = -2; i <= 2; i++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), player.Center - 2400 * Vector2.UnitY + i * 300 * Vector2.UnitX, 3 * Vector2.UnitY, ModContent.ProjectileType<Lightning>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer, ai0: MathHelper.PiOver2, ai2: 0);
                                ScreenShakeSystem.StartShake(4f);
                            }
                            lightning = 1;
                            SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0"), player.Center);
                        }
                        MovementAI(npc, player.Center, num15, num16, maxSpeed);
                        if (++Timer >= 360)
                        {
                            Timer = 0;
                            State = 3;
                        }

                    }
                    break;//减速,放雷
                case 3:
                    if (FargoSoulsUtil.HostCheck)
                    {
                        #region 杂项
                        float maxSpeed = 16f;
                        float num15 = 0.1f;   // 转向加速度
                        float num16 = 0.15f;  // 直线加速度
                        float distance = npc.Distance(Main.player[npc.target].Center);
                        if (Timer < 115)
                        {
                            if (distance < 600)         // 靠近玩家时降低速度
                                maxSpeed *= 0.25f;
                            else if (distance > 900)    // 远离玩家时加速追击
                            {
                                num15 *= 2f;
                                num16 *= 2f;
                            }
                            float comparisonSpeed = Main.player[npc.target].velocity.Length() * 1.5f;
                            float rotationDifference = MathHelper.WrapAngle(npc.velocity.ToRotation() - npc.SafeDirectionTo(Main.player[npc.target].Center).ToRotation());
                            bool inFrontOfMe = Math.Abs(rotationDifference) < MathHelper.ToRadians(90 / 2);
                            if (maxSpeed < comparisonSpeed && inFrontOfMe)
                            {
                                maxSpeed = comparisonSpeed;
                            }
                        }
                        
                        #endregion
                        if (Timer == 0)
                        {
                            SoundEngine.PlaySound(ScanSound with { Volume = 2f }, npc.Center);
                            float angle = MathHelper.Pi * 0.7f;
                            int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.velocity, ModContent.ProjectileType<DestroyerScanTelegraph>(), 0, 0f, Main.myPlayer, 0, angle, 4000);
                            if (p != Main.maxProjectiles)
                                Main.projectile[p].timeLeft = 120;
                        }
                        if (Timer < 120)
                        {
                            npc.velocity = Vector2.Lerp(npc.velocity, (Main.player[npc.target].Center - npc.Center) / 80, 0.05f);
                        }
                        if (Timer >= 120 && Timer < 540)
                        {
                            if (Timer == 120) // 第一阶段：高速冲向玩家
                            {
                                if (maxSpeed < 32)
                                    maxSpeed = 32;
                                maxSpeed *= 2f;
                                num15 *= 40;
                                num16 *= 40;

                                SecondaryAttackTimer = 1;
                                npc.velocity = 40f * npc.SafeDirectionTo(player.Center);
                                
                                npc.netUpdate = true;
                                NetSync(npc);
                            }
                            else // 第二阶段：转弯后发射线状激光
                            {
                                double angle = npc.SafeDirectionTo(player.Center).ToRotation() - npc.velocity.ToRotation();
                                while (angle > Math.PI) angle -= 2.0 * Math.PI;
                                while (angle < -Math.PI) angle += 2.0 * Math.PI;
                                int rotationTowardsPlayer = Math.Sign(angle);

                                bool playerIsInFront = Math.Abs(angle) < MathHelper.ToRadians(45);
                                if (!playerIsInFront)
                                {
                                    if (WorldSavingSystem.MasochistModeReal)
                                        maxSpeed /= 4;
                                    else if (maxSpeed > 2)
                                        maxSpeed = 2;

                                    if (npc.velocity.Length() > maxSpeed)
                                        npc.velocity *= 0.98f;

                                    float turnModifier = 15f;
                                    num15 /= turnModifier;
                                    num16 /= turnModifier;
                                }

                                // 非常轻微的转向
                                npc.velocity = npc.velocity.RotatedBy(MathHelper.ToRadians(0.1f) * rotationTowardsPlayer);

                                // 从身体段发射线状激光（GlowLine）
                                if (Timer <= 490 && (Timer - 120) % 90 == 20)
                                {
                                    LightshowSlowTimer = 120;
                                    bool flip = Main.rand.NextBool();
                                    bool spawn = true;
                                    foreach (NPC n in Main.npc.Where(n => n.active && n.realLife == npc.whoAmI))
                                    {
                                        spawn = !spawn;
                                        if (!spawn)
                                            continue;

                                        if (FargoSoulsUtil.HostCheck)
                                        {
                                            if (Main.rand.NextFloat() > npc.life / npc.lifeMax)
                                            {
                                                float range = MathHelper.ToRadians(10);
                                                float ai1 = n.rotation + (flip ? 0 : MathHelper.Pi) + Main.rand.NextFloat(-range, range);
                                                int p = Projectile.NewProjectile(npc.GetSource_FromThis(), n.Center, Vector2.Zero, ModContent.ProjectileType<GlowLine>(), ProjectileDamage(npc), 0f, Main.myPlayer, 11, n.whoAmI);
                                                if (p != Main.maxProjectiles)
                                                {
                                                    Main.projectile[p].localAI[1] = ai1;
                                                    if (Main.netMode == NetmodeID.Server)
                                                        NetMessage.SendData(MessageID.SyncProjectile, number: p);
                                                }
                                            }
                                        }
                                        flip = !flip;
                                        if (Main.rand.NextBool(5))
                                            flip = !flip;
                                    }
                                }
                            }
                        }
                        MovementAI(npc, player.Center, num15, num16, maxSpeed);
                        if (++Timer >= 600)
                        {
                            Timer = 0;
                            State = 1;
                            Flag = 0;
                        }
                    }
                    break;//灯光秀

            }
            EModeUtils.DropSummon(npc, "MechWorm", NPC.downedMechBoss1, ref DroppedSummon, Main.hardMode);
            return true;
        }
        private static new void MovementAI(NPC npc, Vector2 target, float num15, float num16, float maxSpeed)
        {
            float num17 = target.X;
            float num18 = target.Y;

            float num21 = num17 - npc.Center.X;
            float num22 = num18 - npc.Center.Y;
            float num23 = (float)Math.Sqrt((double)num21 * (double)num21 + (double)num22 * (double)num22);

            // 以下代码来自原版地面移动AI，但被用于飞行
            float num2 = (float)Math.Sqrt(num21 * num21 + num22 * num22);
            float num3 = Math.Abs(num21);
            float num4 = Math.Abs(num22);
            float num5 = maxSpeed / num2;
            float num6 = num21 * num5;
            float num7 = num22 * num5;

            // 加速/减速逻辑：使npc.velocity逐渐接近理想速度(num6, num7)
            if ((npc.velocity.X > 0f && num6 > 0f || npc.velocity.X < 0f && num6 < 0f) && (npc.velocity.Y > 0f && num7 > 0f || npc.velocity.Y < 0f && num7 < 0f))
            {
                if (npc.velocity.X < num6) npc.velocity.X += num16;
                else if (npc.velocity.X > num6) npc.velocity.X -= num16;
                if (npc.velocity.Y < num7) npc.velocity.Y += num16;
                else if (npc.velocity.Y > num7) npc.velocity.Y -= num16;
            }
            if (npc.velocity.X > 0f && num6 > 0f || npc.velocity.X < 0f && num6 < 0f || npc.velocity.Y > 0f && num7 > 0f || npc.velocity.Y < 0f && num7 < 0f)
            {
                if (npc.velocity.X < num6) npc.velocity.X += num15;
                else if (npc.velocity.X > num6) npc.velocity.X -= num15;
                if (npc.velocity.Y < num7) npc.velocity.Y += num15;
                else if (npc.velocity.Y > num7) npc.velocity.Y -= num15;

                if (Math.Abs(num7) < maxSpeed * 0.2f && (npc.velocity.X > 0f && num6 < 0f || npc.velocity.X < 0f && num6 > 0f))
                {
                    if (npc.velocity.Y > 0f) npc.velocity.Y += num15 * 2f;
                    else npc.velocity.Y -= num15 * 2f;
                }
                if (Math.Abs(num6) < maxSpeed * 0.2f && (npc.velocity.Y > 0f && num7 < 0f || npc.velocity.Y < 0f && num7 > 0f))
                {
                    if (npc.velocity.X > 0f) npc.velocity.X += num15 * 2f;
                    else npc.velocity.X -= num15 * 2f;
                }
            }
            else if (num3 > num4)
            {
                if (npc.velocity.X < num6) npc.velocity.X += num15 * 1.1f;
                else if (npc.velocity.X > num6) npc.velocity.X -= num15 * 1.1f;

                if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < maxSpeed * 0.5f)
                {
                    if (npc.velocity.Y > 0f) npc.velocity.Y += num15;
                    else npc.velocity.Y -= num15;
                }
            }
            else
            {
                if (npc.velocity.Y < num7) npc.velocity.Y += num15 * 1.1f;
                else if (npc.velocity.Y > num7) npc.velocity.Y -= num15 * 1.1f;

                if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < maxSpeed * 0.5f)
                {
                    if (npc.velocity.X > 0f) npc.velocity.X += num15;
                    else npc.velocity.X -= num15;
                }
            }

            // 根据速度设置旋转（朝向运动方向 + 90度）
            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
            npc.netUpdate = true;
            npc.localAI[0] = 1f;
        }
        public override void ModifyHitByAnything(NPC npc, Player player, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByAnything(npc, player, ref modifiers);

            if (IsCoiling)
            {
                if (npc.life < npc.lifeMax / 10)
                {
                    float modifier = Math.Min(1f, AttackModeTimer / 480f);
                    modifiers.FinalDamage /= modifier; // 随时间逐渐恢复正常伤害
                }
                else
                {
                    modifiers.FinalDamage /= 0.4f; // 普通盘旋时60%减伤
                }
            }
            else if (npc.life < npc.lifeMax / 10)
            {
                modifiers.FinalDamage /= 0.1f; // 绝望阶段（非盘旋）90%减伤
            }
            else if (PrepareToCoil || AttackModeTimer >= P2_COIL_BEGIN_TIME - 120)
            {
                modifiers.FinalDamage /= 0.4f; // 预备盘旋或即将预备时60%减伤
            }
        }//抵消减伤
    }
    public class PHDestroyerSegment : DestroyerSegment
    {
        public override bool SafePreAI(NPC npc)
        {
            NPC destroyer = FargoSoulsUtil.NPCExists(npc.realLife, NPCID.TheDestroyer);
            if (destroyer == null || npc.life <= 0 || !destroyer.active || destroyer.life <= 0)
            {
                // 头部死亡时立即杀死身体
                if (FargoSoulsUtil.HostCheck)
                {
                    npc.life = 0;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                    npc.active = false;
                }
                return true;
            }//紫砂判定
            PHDestroyer phdestroyer = destroyer.GetGlobalNPC<PHDestroyer>();
            npc.defense = npc.defDefense;
            npc.localAI[0] = 0f; // 禁用原版激光AI
            npc.ai[2] = 0;
            ProbeReleaseTimer = 0;
            npc.buffImmune[ModContent.BuffType<TimeFrozenBuff>()] = destroyer.buffImmune[ModContent.BuffType<TimeFrozenBuff>()];
            return true;
        }
        public override void ModifyHitByAnything(NPC npc, Player player, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByAnything(npc, player, ref modifiers);

        }
        public override void SafeModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            base.SafeModifyHitByProjectile(npc, projectile, ref modifiers);
            if (projectile.type == ModContent.ProjectileType<LightningExplosion>() || projectile.type == ModContent.ProjectileType<Lightning>())
                modifiers.FinalDamage /= 4;
            PierceResistance(projectile, ref modifiers);
        }
    }
    public class PHProbe : Probe
    {
        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);
            // 如果蠕虫活着，探测器血量增加50%
            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.destroyBoss, NPCID.TheDestroyer))
                npc.lifeMax = (int)(npc.lifeMax / 1.5f);
        }
        public override bool SafePreAI(NPC npc)
        {
            npc.TargetClosest();
            if (npc.localAI[0] > 30)
                npc.localAI[0] = -30;
            // 轨道方向切换
            if (++OrbitChangeTimer > 120)
            {
                OrbitChangeTimer = 0;
                OrbitDirection = Main.rand.NextBool() ? 1 : -1;
                npc.netUpdate = true;
                NetSync(npc);
            }

            if (ShootLaser)
            {
                // 开始射击时记录当前相对于玩家的角度（固定住轨道方向）
                if (AttackTimer == 0)
                {
                    TargetOrbitRotation = Main.player[npc.target].SafeDirectionTo(npc.Center).ToRotation();
                    npc.netUpdate = true;
                    NetSync(npc);
                }

                const int attackTime = 110;

                Vector2 towardsPlayer = 6f * npc.SafeDirectionTo(Main.player[npc.target].Center);
                int dustID = WorldSavingSystem.EternityMode && SoulConfig.Instance.BossRecolors ? DustID.GemSapphire : DustID.GemRuby;
                float dustScale = 0.5f + 2.5f * AttackTimer / attackTime;
                int d = Dust.NewDust(npc.position, npc.width, npc.height, dustID, 2f * towardsPlayer.X, 2f * towardsPlayer.Y, 0, default, dustScale);
                Main.dust[d].noGravity = true;

                if (++AttackTimer > attackTime)
                {
                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, towardsPlayer, ProjectileID.DeathLaser, (int)(1.1 * FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage)), 0f, Main.myPlayer);
                    AttackTimer = 0;
                    ShootLaser = false;
                    npc.netUpdate = true;
                    NetSync(npc);
                }
            }

            // 移动逻辑：绕玩家轨道运动
            float orbitDistance = ShootLaser ? 500 : 300;
            Vector2 vel = Main.player[npc.target].Center - npc.Center;
            vel += orbitDistance * (ShootLaser ? Vector2.UnitX.RotatedBy(TargetOrbitRotation) : Main.player[npc.target].SafeDirectionTo(npc.Center).RotatedBy(MathHelper.ToRadians(22) * OrbitDirection));

            // 跟随玩家移动（1/3的玩家速度补偿）
            npc.position += (Main.player[npc.target].position - Main.player[npc.target].oldPosition) / 2;

            if (npc.Distance(Main.player[npc.target].Center) < 200) // 太近时瞬间弹开
            {
                npc.velocity = vel / 20;
            }
            else // 正常追踪
            {
                vel.Normalize();
                vel *= 16f;
                float moveSpeed = 1.0f;
                if (ShootLaser)
                {
                    vel *= 1.5f;
                    moveSpeed *= 1.5f;
                }

                // 加速逻辑分段
                if (npc.velocity.X < vel.X)
                {
                    npc.velocity.X += moveSpeed;
                    if (npc.velocity.X < 0 && vel.X > 0)
                        npc.velocity.X += moveSpeed;
                }
                else if (npc.velocity.X > vel.X)
                {
                    npc.velocity.X -= moveSpeed;
                    if (npc.velocity.X > 0 && vel.X < 0)
                        npc.velocity.X -= moveSpeed;
                }
                if (npc.velocity.Y < vel.Y)
                {
                    npc.velocity.Y += moveSpeed;
                    if (npc.velocity.Y < 0 && vel.Y > 0)
                        npc.velocity.Y += moveSpeed;
                }
                else if (npc.velocity.Y > vel.Y)
                {
                    npc.velocity.Y -= moveSpeed;
                    if (npc.velocity.Y > 0 && vel.Y < 0)
                        npc.velocity.Y -= moveSpeed;
                }
            }

            return true;
        }
    }
    public class PHDestroyerModSystem : ModSystem
    {
        public override void Load()
        {
            MethodInfo targetMethod1 = typeof(FargowiltasSouls.Content.Bosses.VanillaEternity.Destroyer).GetMethod("SafePreAI", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(targetMethod1, ILDestroyerAI);
            MethodInfo targetMethod2 = typeof(DestroyerSegment).GetMethod("SafePreAI", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(targetMethod2, ILDestroyerAI);
            MethodInfo targetMethod3 = typeof(Probe).GetMethod("SafePreAI", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(targetMethod3, ILDestroyerAI);
            MethodInfo targetMethod4 = typeof(FargowiltasSouls.Content.Bosses.VanillaEternity.Destroyer).GetMethod("ModifyHitByAnything", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(targetMethod4, ILReducedamage);
            MethodInfo targetMethod5 = typeof(DestroyerSegment).GetMethod("ModifyHitByAnything", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(targetMethod5, ILReducedamage);
        }
        private void ILDestroyerAI(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.EmitDelegate<Func<bool>>(() =>
            {
                return true;
            });
            c.Emit(OpCodes.Ret);
        }
        private void ILReducedamage(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.Emit(OpCodes.Ret);
        }
    }
}
