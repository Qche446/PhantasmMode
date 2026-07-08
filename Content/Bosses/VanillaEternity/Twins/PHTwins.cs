using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using MonoMod.Logs;
using FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /*
    public class PHRetinazer : Retinazer
    {
        public int P2Timer = 0;
        public int P2State = 0;
        public float P2flag = 0;
        int waittime = 180;
        public bool down20 = false;
        bool P3 = false;
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (!Phase2 && npc.ai[1] == 0 || P2State == 0 )
            {
                return false;
            }
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }
        // 设置默认属性：增加20%血量
        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);
            npc.lifeMax = (int)(npc.lifeMax * 5 / 6);//恢复
            npc.damage = (int)(0.8f * npc.damage);
        }
        public override bool SafePreAI(NPC npc)
        {
            // 标记全局Retinazer Boss索引
            EModeGlobalNPC.retiBoss = npc.whoAmI;
            Resist = false;
            if (!npc.HasValidTarget || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest();
                Player p = Main.player[npc.target];
                if (!npc.HasValidTarget || !p.active || p.dead)
                {
                    npc.noTileCollide = true; // 取消碰撞
                    if (npc.timeLeft > 30)
                        npc.timeLeft = 30; // 快速消失

                    if (npc.velocity.Y > 0)
                        npc.velocity.Y = 0;
                    npc.velocity.Y -= 0.5f; // 向上飞出屏幕
                    return false;
                }
            }
            if (npc.ai[0] == 1 || npc.ai[0] == 2)
                Resist = true;
            NPC spazmatism = FargoSoulsUtil.NPCExists(EModeGlobalNPC.spazBoss, NPCID.Spazmatism);
            // 进入第二阶段检查：转阶段动画
            if (!Phase2)
            {
                if (npc.GetLifePercent() < 0.66f || (spazmatism != null && spazmatism.GetLifePercent() < 0.66f))
                {
                    Phase2 = true;
                    npc.ai[0] = 1f; // 触发相位转换动画
                    npc.ai[1] = 0.0f;
                    npc.ai[2] = 0.0f;
                    npc.ai[3] = 0.0f;
                    npc.netUpdate = true;
                }
            }
            if (npc.life <= npc.lifeMax / 2 || npc.dontTakeDamage)
            {
                npc.dontTakeDamage = npc.life == 1 || !npc.HasValidTarget;
                if (npc.life != 1 && npc.HasValidTarget)
                    npc.dontTakeDamage = false;
                // 当另一个双子也低血量时取消无敌
                if (npc.dontTakeDamage && npc.HasValidTarget && (spazmatism == null || spazmatism.life == 1))
                    npc.dontTakeDamage = false;
            }
            if (npc.life <= 0.2f * npc.lifeMax)
            {
                down20 = true;
            }//小于20%state0狂暴
            // 白天逃脱逻辑
            if (Main.dayTime && !Main.remixWorld)
            {
                if (npc.velocity.Y > 0)
                    npc.velocity.Y = 0;
                npc.velocity.Y -= 0.5f; // 向上飞
                npc.dontTakeDamage = true; // 白天无敌

                if (spazmatism != null)
                {
                    if (npc.timeLeft < 60)
                        npc.timeLeft = 60;
                    if (spazmatism.timeLeft < 60)
                        spazmatism.timeLeft = 60;

                    npc.TargetClosest(false);
                    spazmatism.TargetClosest(false);
                    // 玩家远离时双双消失
                    if (npc.Distance(Main.player[npc.target].Center) > 2000 &&
                        spazmatism.Distance(Main.player[spazmatism.target].Center) > 2000)
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            npc.active = false;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                            spazmatism.active = false;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, EModeGlobalNPC.spazBoss);
                        }
                    }
                }
                return true;
            }
            if (Phase2 && spazmatism == null && !P3)//进尾杀
            {
                P2State = -1;
                P2Timer = 0;
                P2flag = 0;
                P3 = true;
                npc.dontTakeDamage = true;
                FargoSoulsUtil.ClearHostileProjectiles(1, npc.whoAmI);
            }
            if (!Phase2)
            {
                npc.damage = npc.defDamage;
                ref float ai_State = ref npc.ai[1];      // AI状态：0-激光，1-冲刺准备，2-冲刺中
                ref float ai_StateTimer = ref npc.ai[2]; // 状态计时器
                ref float ai_ShotTimer = ref npc.ai[3];  // 射击计时器

                if (!npc.HasPlayerTarget || Main.IsItDay())
                    return true;

                Player player = Main.player[npc.target];

                switch (ai_State)
                {
                    case 0: // 正常激光状态（持续600帧）
                        {
                            int stateTime = 600;
                            if (ai_StateTimer >= stateTime - 1) // 状态结束时切换到冲刺准备
                            {
                                ai_State = 1f;
                                ai_StateTimer = 0f;
                                ai_ShotTimer = 0f;
                                npc.netUpdate = true;
                                npc.TargetClosest();
                                goto case 1; // 直接执行case1逻辑
                            }

                            // 移动：保持在玩家上方450像素
                            float accel = 1f;
                            Vector2 desired = player.Center - npc.Center -450 * Vector2.UnitY;
                            TwinDefaultMovement(npc, desired.X, desired.Y, accel, 3);

                            // 改进的P1激光：交替射击模式
                            float delay = 40f;
                            if (ai_ShotTimer >= delay && ai_StateTimer > 20)
                            {
                                LaserSide++; // 切换射击侧
                                if (LaserSide > 1)
                                    LaserSide = 0;
                                ai_ShotTimer = 0f;

                                Vector2 shootPos = new Vector2(npc.position.X + npc.width * 0.5f,
                                    npc.position.Y + npc.height * 0.5f);
                                float targetX = player.Center.X - shootPos.X;
                                float targetY = player.Center.Y - shootPos.Y;
                                npc.netUpdate = true;

                                if (FargoSoulsUtil.HostCheck) // 主机生成弹幕
                                {
                                    float num429 = 10.5f; // 弹幕速度
                                    int projDamage = npc.GetAttackDamage_ForProjectiles(20f, 19f);
                                    float angle = (float)Math.Sqrt(targetX * targetX + targetY * targetY);
                                    angle = num429 / angle; // 标准化
                                    targetX *= angle;
                                    targetY *= angle;

                                    targetX *= 0.6f; // 降低速度
                                    targetY *= 0.6f;
                                    Vector2 vel = new(targetX, targetY);
                                    shootPos.X += targetX * 10f;
                                    shootPos.Y += targetY * 10f;

                                    int spread = 1; // 弹幕扩散数
                                    if (LaserSide == 1) // 交替射击：一发单射，一发三发
                                        spread = 0;

                                    for (int i = -spread - 1; i <= spread + 1; i++)
                                    {
                                        if (spread == 1 && i == 0) // 跳过中间弹幕（如果扩散数>0）
                                            continue;
                                        float offset = i;
                                        offset *= (int)npc.HorizontalDirectionTo(player.Center);
                                        Vector2 vel2 = vel.RotatedBy(MathHelper.PiOver2 * 0.4f * offset);
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                            3 * vel2, ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                                        for (int j = -3; j <= 3; j++)
                                        {
                                            Vector2 particleVel = (vel2 * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                                .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                            Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel - vel2,
                                                particleVel, Color.Yellow, Main.rand.NextFloat(0.7f, 1f), 20);
                                            p.Spawn();
                                        }
                                    }
                                    npc.velocity -= vel; // 后坐力
                                }
                            }
                        }
                        break;

                    case 1: // 冲刺准备帧
                        {
                            float prepTime = WorldSavingSystem.MasochistModeReal ? 60 : 75; // 受虐模式准备时间更短

                            if (ai_StateTimer == 0) // 初始化锁定角度
                            {
                                LockedRotation = player.DirectionTo(npc.Center).ToRotation();
                                int dir = Main.rand.NextBool() ? 1 : -1; // 随机旋转方向

                                // 避免与Spazmatism重叠
                                if (spazmatism != null && spazmatism.TypeAlive(NPCID.Spazmatism))
                                {
                                    float spazmatism_Angle = player.DirectionTo(spazmatism.Center).ToRotation();
                                    dir = MathF.Sign(LockedRotation - spazmatism_Angle);
                                }
                                LockedRotation += dir * MathHelper.PiOver2 * (0.25f + Main.rand.NextFloat(0.2f));
                            }

                            // 锁定到玩家侧方位置
                            float distance = WorldSavingSystem.MasochistModeReal ? 480f : 600f;
                            Vector2 desiredPos = player.Center + LockedRotation.ToRotationVector2() * distance;
                            float desiredX = desiredPos.X - npc.Center.X;
                            float desiredY = desiredPos.Y - npc.Center.Y;

                            float accel = 0.6f;
                            TwinDefaultMovement(npc, desiredX, desiredY, accel, 4);
                            TwinManageRotation(npc); // 面向玩家

                            // 等待Spazmatism就位（协同攻击）
                            bool waitForSpaz = spazmatism.ai[1] == npc.ai[1];
                            Vector2 spazDesiredPos = player.Center +
                                spazmatism.GetGlobalNPC<PHSpazmatism>().LockedRotation.ToRotationVector2() * distance;
                            if (spazmatism.Distance(spazDesiredPos) <= 300)
                                waitForSpaz = false;

                            // 如果未到达位置或等待Spazmatism，保持准备状态
                            if (ai_StateTimer < prepTime - 30 && (npc.Distance(desiredPos) > 300 || waitForSpaz))
                                ai_StateTimer--;

                            int flashDelay = WorldSavingSystem.MasochistModeReal ? 25 : 35;
                            if (ai_StateTimer < prepTime - flashDelay && spazmatism.ai[2] < npc.ai[2])
                                npc.ai[2] = spazmatism.ai[2]; // 与Spazmatism同步计时器

                            // 冲刺前闪光提示
                            if (ai_StateTimer == prepTime - flashDelay)
                            {
                                if (FargoSoulsUtil.HostCheck)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(),
                                        npc.Center + TwinsEyeFlash.Offset(npc), Vector2.Zero,
                                        ModContent.ProjectileType<TwinsEyeFlash>(), 0, 0, Main.myPlayer, npc.whoAmI);
                                }
                            }

                            // 准备时间结束，开始冲刺
                            if (++ai_StateTimer >= prepTime)
                            {
                                npc.velocity = npc.DirectionTo(player.Center) * 40; // 高速冲刺
                                ai_StateTimer = 0;
                                LockedRotation = 0;
                                ai_State = 2; // 切换到冲刺中状态
                            }
                            return false; // 阻止原版AI
                        }

                    case 2: // 冲刺中状态
                        {
                            npc.damage = (int)(npc.defDamage * 1.5f); // 冲刺时伤害增加50%
                            ai_StateTimer += 1f;

                            if (ai_StateTimer >= 25f) // 冲刺25帧后结束
                            {
                                ai_ShotTimer += 1f;
                                ai_StateTimer = 0f;
                                npc.TargetClosest();

                                if (ai_ShotTimer >= 4f) // 完成4次冲刺后返回激光状态
                                {
                                    ai_State = 0f;
                                    ai_ShotTimer = 0f;
                                }
                                else // 否则继续准备冲刺
                                {
                                    ai_State = 1f;
                                }
                            }
                            if (ai_StateTimer % 2 == 0)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                            3f * Vector2.Normalize(npc.velocity), ModContent.ProjectileType<MechElectricOrbSpaz>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                            }
                            else
                            {
                                // 冲刺时的旋转（面向移动方向）
                                npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - 1.57f;
                                if (ai_StateTimer >= 20) // 冲刺最后5帧减速
                                    npc.velocity *= 0.95f;
                            }
                            return false;
                        }
                }
            }
            else // 第二阶段
            {
                ShouldDrawAura = true; // 开始绘制光环
                if (P3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Ichor, 0f, 0f, 0, default, 1.8f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 5f;
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, 0f, 0f, 0, default, 1.8f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 6f;
                    }
                }//粒子效果
                Player player = Main.player[npc.target];
                npc.AddBuff(BuffID.CursedInferno, 9999999);
                npc.AddBuff(BuffID.Ichor, 9999999);
                // 劫持原版激光代码发射机械电球
                if (npc.ai[1] != 0)
                    npc.localAI[1] -= 1f;
                npc.localAI[1] = 0;
                #region 仪式圈和死亡射线相关
                // 光环逻辑：死亡射线期间收缩，非射线期间扩张
                if (DeathrayState == 0 || DeathrayState == 3) // 非死亡射线状态
                {
                    AuraRadiusCounter--; // 光环扩大
                    if (AuraRadiusCounter < 0)
                        AuraRadiusCounter = 0;
                }
                else // 死亡射线状态
                {
                    AuraRadiusCounter++;// 光环收缩
                    if (AuraRadiusCounter > 180)
                        AuraRadiusCounter = 180;
                }

                // 光环效果：施加油debuff并将玩家拉入竞技场
                float auraDistance = AuraRadius();
                if (auraDistance < 2000 - 1)
                {
                    EModeGlobalNPC.Aura(npc, auraDistance, true, -1, default,
                        ModContent.BuffType<OiledBuff>());
                    float threshold = auraDistance;

                    Player localPlayer = Main.LocalPlayer;
                    float distance = localPlayer.Distance(npc.Center);

                    // 将玩家拉入光环范围
                    if (localPlayer.active && !localPlayer.dead && !localPlayer.ghost)
                    {
                        if (distance > threshold && distance < threshold * 4f)
                        {
                            if (distance > threshold * 2f) // 距离过远时定身
                            {
                                localPlayer.Incapacitate();
                                localPlayer.velocity.X = 0f;
                                localPlayer.velocity.Y = -0.4f;
                            }

                            Vector2 movement = npc.Center - localPlayer.Center;
                            float difference = movement.Length() - threshold;
                            movement.Normalize();
                            movement *= Math.Min(difference, 30f); // 每次最多拉近30像素
                            localPlayer.position += movement;
                        }
                    }
                }
                #endregion
                switch (P2State)
                {
                    case -1:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            npc.dontTakeDamage = true ;
                            npc.velocity *= 0.95f;
                            if (P2Timer == 30)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsFireBackground>(), 0, 0, Main.myPlayer, ai0: npc.whoAmI, 0);
                            }
                            if (P2Timer > 30)
                            {
                                int heal = (int)(npc.lifeMax / 90 * Main.rand.NextFloat(1f, 1.5f));
                                npc.life += heal;
                                if (npc.life > 0.45f * npc.lifeMax)
                                    npc.life = (int)(0.35f * npc.lifeMax);
                                CombatText.NewText(npc.Hitbox, CombatText.HealLife, heal);
                            }
                            if (++P2Timer > 90)
                            {
                                npc.dontTakeDamage = false;
                                P2Timer = 0;
                                P2State = 0;
                                P2flag = 0;
                                DeathrayState = 0;
                                SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                                ScreenShakeSystem.StartShake(20f);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CalcloneWave>(), 0, 0, Main.myPlayer, ai1: 0, ai2: 20);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CalcloneWave>(), 0, 0, Main.myPlayer, ai1: 0, ai2: 16);
                            }
                        }
                        break;
                    case 0:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            if (P2Timer >= waittime)
                            {
                                Vector2 desired = player.Center - npc.Center - 500 * Vector2.UnitY.RotatedBy(P2flag * MathHelper.Pi / 6);
                                TwinDefaultMovement(npc, desired.X, desired.Y, 3f, 4);
                                TwinManageRotation(npc);
                                int inter = P3 ? 15 : (down20 ? 20 : 30);
                                if (P2Timer % inter == 0 && P2Timer > 60+ waittime)
                                {
                                    Vector2 vel = npc.SafeDirectionTo(player.Center);
                                    for (int i = -2; i <= 2; i++)
                                    {
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                            20 * vel.RotatedBy(i * MathHelper.Pi / 6), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                            30 * vel.RotatedBy(i * MathHelper.Pi / 6), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                                        for (int j = -3; j <= 3; j++)
                                        {
                                            Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                                .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                            Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                                particleVel, Color.Yellow, Main.rand.NextFloat(0.7f, 1f), 20);
                                            p.Spawn();
                                        }
                                    }
                                    if (P3)
                                    {
                                        for (float i = 0.7f; i <= 1.6f; i += 0.2f)
                                        {
                                            for (int j = -1; j <= 1; j++)
                                            {
                                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                                i * vel.RotatedBy(j * MathHelper.Pi / 3), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                                Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                                            }
                                            for (int j = -3; j <= 3; j++)
                                            {
                                                Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                                    .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                                Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                                    particleVel, Color.Green, Main.rand.NextFloat(0.7f, 1f), 20);
                                                p.Spawn();
                                            }
                                        }
                                    }
                                    npc.velocity -= 2 * vel;
                                    P2flag += 1f;
                                }
                                waittime = 0;
                            }
                            if (++P2Timer >= 360 + waittime)
                            {
                                P2Timer = 0;
                                P2State = 1;
                                P2flag = 0;
                            }
                        }
                        break;//常态360帧
                    case 1:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            npc.velocity *= 0.80f;
                            if (P2Timer % 10 == 0 && P2Timer > 30 && P2Timer < 330)
                            {
                                int max = Main.getGoodWorld ? 12 : 8 ;
                                for (int i = 0; i < max; i++)
                                {
                                    Vector2 vel = Vector2.UnitX.RotatedBy((i + (float)P2flag / 15) * MathHelper.TwoPi / max);
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                        6 * vel, ModContent.ProjectileType<MechElectricOrbPolyline>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                        Main.myPlayer, npc.target,1, ai2: MechElectricOrb.Yellow);
                                    if (Main.getGoodWorld)
                                    {
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                        6 * vel, ModContent.ProjectileType<MechElectricOrbPolyline>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                        Main.myPlayer, npc.target, -1, ai2: MechElectricOrb.Yellow);
                                    }
                                    if (P3)
                                    {
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                        16 * Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / max +  3 * P2flag), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                        Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                                    }
                                    for (int j = -3; j <= 3; j++)
                                    {
                                        Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                            .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                        Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                            particleVel, Color.Yellow, Main.rand.NextFloat(0.7f, 1f), 20);
                                        p.Spawn();
                                    }
                                }
                                P2flag++;
                            }
                            if (++P2Timer > 360)
                            {
                                P2Timer = 0;
                                P2State = 2;
                                P2flag = 0;
                            }
                        }
                        break;//360
                    case 2:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            int flagY = Math.Sign(npc.Center.Y - player.Center.Y);
                            Vector2 desired = player.Center - npc.Center + flagY * 500 * Vector2.UnitY;
                            if (P3)
                            {
                                npc.velocity *= 0.1f;
                            }
                            else
                            {
                                TwinDefaultMovement(npc, desired.X, desired.Y, 0.3f, 2);
                            }
                            TwinManageRotation(npc);
                            Vector2 vel = npc.SafeDirectionTo(player.Center);
                            if (P2Timer % 5 == 0 && P2Timer > 20)
                            {
                                if (!P3)
                                {
                                    for (int i = -2; i <= 2; i++)
                                    {
                                        if (i == 0)
                                            i++;
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                            3 * vel.RotatedBy(i * MathHelper.Pi / 5), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);

                                    }
                                }
                                else
                                {
                                    for (int i = -1; i <= 1; i+=2)
                                    {
                                        double interangle = i * ( 1 + Math.Sin(1.5f * P2Timer * MathHelper.Pi / 90)) * MathHelper.Pi / 3;
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                            16 * vel.RotatedBy(interangle), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                                        double interangle2 = i * (1 - Math.Sin(1.5f * P2Timer * MathHelper.Pi / 90)) * MathHelper.Pi / 3;
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                            16 * vel.RotatedBy(interangle2), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                                    } 
                                }
                                for (int j = -3; j <= 3; j++)
                                {
                                    Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                        .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                    Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                        particleVel, Color.Yellow, Main.rand.NextFloat(0.7f, 1f), 20);
                                    p.Spawn();
                                }
                                npc.velocity -= 0.5f * vel;
                            }
                            if (++P2Timer > (P3 ? 360 : 180))
                            {
                                P2Timer = 0;
                                P2State = 3;
                                P2flag = 0;
                                npc.ai[0] = 608;
                            }
                        }
                        
                        break;//P3 ? 360 : 180
                    case 3:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            npc.ai[0]++;
                            P2Timer++;
                            float rotationInterval = 2f * (float)Math.PI * 1.2f / 4f / 60f;
                            if (WorldSavingSystem.MasochistModeReal)
                                rotationInterval *= 1.05f; // 受虐模式旋转更快
                            if (P3 && P2Timer % 10 == 0 && P2Timer <= 420 && P2Timer > 30)
                            {
                                if (FargoSoulsUtil.HostCheck)
                                {
                                    // 逐渐增加速度（30-150帧内从0到12）
                                    float speed = 12f * Math.Min((P2Timer - 30) / 120f, 1f);
                                    int timeLeft = (int)(speed / 12f * 90f); // 根据速度调整存在时间
                                    float baseRotation = (StoredDirectionToPlayer ? 1f : -1f) * 2f * (float)Math.PI * 1.2f / 4f / 60f;
                                    if (timeLeft > 5) // 速度足够时才发射
                                    {
                                        for (int i = 0; i < 6; i++)
                                        {
                                            int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                                (baseRotation * (P2Timer / 1.4f) + MathHelper.TwoPi / 6 * i).ToRotationVector2() * speed / 8f,
                                                ModContent.ProjectileType<MechElectricOrbAcc>(),
                                                FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage),
                                                0f, Main.myPlayer, ai2: MechElectricOrb.Green);
                                            if (p != Main.maxProjectiles)
                                            {
                                                Main.projectile[p].timeLeft = 2 * timeLeft;
                                            }
                                        }
                                    }
                                }
                            }
                            // 死亡射线状态机
                            switch (DeathrayState)
                            {
                                case 0: // 空闲状态
                                    if (!npc.HasValidTarget) // 玩家死亡时停止计数
                                    {
                                        npc.ai[0]--;
                                        if (spazmatism == null) // Spazmatism也死亡时快速消失
                                            npc.velocity.Y -= 0.5f;
                                    }

                                    if (npc.ai[0] > 604f) // 计时器达到604帧后开始死亡射线
                                    {
                                        npc.ai[0] = 4f;
                                        if (npc.HasPlayerTarget)
                                        {
                                            // 初始化旋转方向
                                            npc.rotation = npc.Center.X < Main.player[npc.target].Center.X ? 0 : (float)Math.PI;
                                            npc.rotation -= MathHelper.PiOver2;

                                            DeathrayState++; // 进入状态1
                                            npc.ai[3] = -npc.rotation; // 存储旋转
                                            if (--npc.ai[2] > 295f)
                                                npc.ai[2] = 295f;
                                            StoredDirectionToPlayer = Main.player[npc.target].Center.X - npc.Center.X < 0;

                                            // 生成警告环
                                            if (FargoSoulsUtil.HostCheck)
                                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                                    Vector2.Zero, ModContent.ProjectileType<GlowRing>(),
                                                    0, 0f, Main.myPlayer, npc.whoAmI, npc.type);

                                            SoundEngine.PlaySound(FargosSoundRegistry.TwinsWarning with { Volume = 4f }, npc.Center);
                                        }

                                        // 网络同步
                                        if (Main.netMode == NetmodeID.Server)
                                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                                        NetSync(npc);
                                    }
                                    break;

                                case 1: // 减速并开始旋转
                                    if (npc.HasPlayerTarget)
                                    {
                                        Vector2 pos = player.Center + player.DirectionTo(npc.Center) * 250;
                                        npc.velocity = FargoSoulsUtil.SmartAccel(npc.Center, pos, npc.velocity, 0.9f, 0.9f);
                                    }

                                    // 逐渐减速
                                    npc.velocity *= 1f - (npc.ai[0] - 4f) / 120f;
                                    npc.localAI[1] = 0f;

                                    // 开始旋转
                                    npc.ai[3] -= (npc.ai[0] - 4f) / 120f * rotationInterval * (StoredDirectionToPlayer ? 1f : -1f);
                                    npc.rotation = -npc.ai[3];

                                    // 35帧时生成引导线
                                    if (npc.ai[0] == 35f)
                                    {
                                        if (FargoSoulsUtil.HostCheck)
                                        {
                                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                                Vector2.Zero, ModContent.ProjectileType<GlowLine>(),
                                                0, 0f, Main.myPlayer, 9f, npc.whoAmI);
                                        }
                                    }

                                    // 155帧时发射死亡射线
                                    if (npc.ai[0] >= 155f)
                                    {
                                        if (!Main.dedServ)
                                            SoundEngine.PlaySound(FargosSoundRegistry.TwinsDeathray with { Volume = 2f }, npc.Center);
                                        if (FargoSoulsUtil.HostCheck)
                                        {
                                            Vector2 speed = Vector2.UnitX.RotatedBy(npc.rotation);
                                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                                speed, ModContent.ProjectileType<RetinazerDeathray>(),
                                                FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 3),
                                                0f, Main.myPlayer, 0f, npc.whoAmI);
                                        }
                                        DeathrayState++; // 进入状态2
                                        npc.ai[0] = 4f;

                                        // 网络同步
                                        if (Main.netMode == NetmodeID.Server)
                                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                                        NetSync(npc);
                                    }
                                    return false; // 阻止原版AI

                                case 2: // 全速旋转
                                    npc.velocity = Vector2.Zero; // 停止移动
                                    npc.localAI[1] = 0f;

                                    // 持续旋转
                                    npc.ai[3] -= rotationInterval * (StoredDirectionToPlayer ? 1f : -1f);
                                    npc.rotation = -npc.ai[3];

                                    // 旋转244帧后结束
                                    if (npc.ai[0] >= 244f)
                                    {
                                        DeathrayState++; // 进入状态3
                                        npc.ai[0] = 4f;

                                        // 网络同步
                                        if (Main.netMode == NetmodeID.Server)
                                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                                        NetSync(npc);
                                    }
                                    else if (!npc.HasValidTarget) // 玩家死亡时立即结束
                                    {
                                        npc.TargetClosest(false);
                                        if (!npc.HasValidTarget)
                                            npc.ai[0] = 244f;
                                    }
                                    return false;

                                case 3: // 减速旋转并恢复移动
                                    npc.velocity *= (npc.ai[0] - 4f) / 60f; // 逐渐恢复速度
                                    npc.localAI[1] = 0f;

                                    // 减速旋转
                                    npc.ai[3] -= (1f - (npc.ai[0] - 4f) / 60f) * rotationInterval * (StoredDirectionToPlayer ? 1f : -1f);
                                    npc.rotation = -npc.ai[3];

                                    // 64帧后返回空闲状态
                                    if (npc.ai[0] >= 64f)
                                    {
                                        DeathrayState = 0;
                                        npc.ai[0] = 4f;
                                        P2State = 0;
                                        P2Timer = 0;
                                        // 网络同步
                                        if (Main.netMode == NetmodeID.Server)
                                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                                        NetSync(npc);
                                    }
                                    return false;

                                default: // 异常状态重置
                                    DeathrayState = 0;
                                    npc.ai[0] = 4f;
                                    P2State = 0;
                                    npc.netUpdate = true;
                                    NetSync(npc);
                                    break;
                            }
                        }
                        break;//450
                }
                if (waittime == 0)
                {
                    return false;
                }

            }
            // 死亡射线期间有50%减伤
            if (DeathrayState > 0)
                Resist = true;
            // 掉落召唤物
            EModeUtils.DropSummon(npc, "MechEye", NPC.downedMechBoss2, ref DroppedSummon, Main.hardMode);
            return true;
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (Resist)
                modifiers.FinalDamage *= 2;
            base.ModifyIncomingHit(npc, ref modifiers);
        }
    }

    public class PHSpazmatism : Spazmatism
    {
        /*
        // 状态变量定义
        public int ProjectileTimer;          // 弹幕计时器（用于诅咒火焰喷射）
        public float LockedRotation;         // 锁定旋转角度（用于冲刺准备）
        public int FlameWheelSpreadTimer;    // 火焰轮扩散计时器
        public int FlameWheelCount;          // 火焰轮数量
        public int MechElectricOrbTimer;     // 机械电球攻击计时器
        public int P3DashPhaseDelay;         // P3冲刺阶段延迟

        public bool Phase2;                  // 是否进入第二阶段
        public bool HasSaidEndure;           // 是否已发送"坚毅"消息
        public bool Resist;                  // 是否处于伤害减免状态
        public float RealRotation;           // 实际旋转角度（用于火焰轮）
        public int RespawnTimer;             // 复活计时器（受虐模式下）
        */
    /*
        int P2Timer = 0;
        int P2State = 0;
        float P2flag = 0;
        int DeathrayState = 0;
        int state = 10;
        int waittime = 180;
        bool down20 = false;
        bool P3 = false;
        Vector2 P3target = Vector2.Zero;
        Vector2 oriVel = Vector2.Zero;
        public static readonly SoundStyle DeathrayFire = new SoundStyle("FargosPhantasmMode/Assets/Sounds/DeathrayFire")
        {
            Volume = 2f,          // 音量 (0.0f 到 1.0f)
            PitchVariance = 0.3f,   // 音高随机变化范围，增加声音自然度
            MaxInstances = 1,       // 最多同时存在的实例数，防止声音叠加
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew 
        };
        
        public static readonly SoundStyle LensEject = new SoundStyle("FargosPhantasmMode/Assets/Sounds/LensEject");

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if ((!Phase2 && npc.ai[1] == 0) || state == 0 || (P2State == 8 && P2Timer < 60) || P2State == 5 || P2State == 7 || P2State == 6)
            {
                return false;
            }
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }
        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);
            npc.lifeMax = (int)(npc.lifeMax * 5 / 6);//恢复
            npc.damage = (int)(0.8f * npc.damage); 
        }
        public override bool SafePreAI(NPC npc)
        {
            #region 杂项
            EModeGlobalNPC.spazBoss = npc.whoAmI;
            Resist = false;
            if (!npc.HasValidTarget || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest();
                Player p = Main.player[npc.target];
                if (!npc.HasValidTarget || !p.active || p.dead)
                {
                    npc.noTileCollide = true;
                    if (npc.timeLeft > 30)
                        npc.timeLeft = 30;

                    if (npc.velocity.Y > 0)
                        npc.velocity.Y = 0;
                    npc.velocity.Y -= 0.5f;
                    return false;
                }
            }
            if (npc.ai[0] == 1 || npc.ai[0] == 2)
                Resist = true;
            NPC retinazer = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            float modifier = (float)npc.life / npc.lifeMax;
            if (WorldSavingSystem.MasochistModeReal)
                modifier *= modifier;
            if (Main.getGoodWorld || Main.zenithWorld)
                modifier *= modifier;
            if (!Phase2)
            {
                if (npc.GetLifePercent() < 0.66f || (retinazer != null && retinazer.GetLifePercent() < 0.66f))
                {
                    Phase2 = true;
                    npc.ai[0] = 1f; // 触发相位转换动画
                    npc.ai[1] = 0.0f;
                    npc.ai[2] = 0.0f;
                    npc.ai[3] = 0.0f;
                    npc.netUpdate = true;
                }
            }
            if (npc.life <= npc.lifeMax / 2 || npc.dontTakeDamage)
            {
                npc.dontTakeDamage = npc.life == 1 || !npc.HasValidTarget;
                if (npc.life != 1 && npc.HasValidTarget)
                    npc.dontTakeDamage = false;
                if (npc.dontTakeDamage && npc.HasValidTarget && (retinazer == null || retinazer.life == 1))
                    npc.dontTakeDamage = false;
            }
            if (npc.life <= 0.2f * npc.lifeMax)
            {
                down20 = true;
            }
            if (Main.dayTime && !Main.remixWorld)
            {
                if (npc.velocity.Y > 0)
                    npc.velocity.Y = 0;
                npc.velocity.Y -= 0.5f;
                npc.dontTakeDamage = true;

                if (retinazer != null)
                {
                    if (npc.timeLeft < 60)
                        npc.timeLeft = 60;
                    if (retinazer.timeLeft < 60)
                        retinazer.timeLeft = 60;

                    npc.TargetClosest(false);
                    retinazer.TargetClosest(false);
                    if (npc.Distance(Main.player[npc.target].Center) > 2000 &&
                        retinazer.Distance(Main.player[retinazer.target].Center) > 2000)
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            npc.active = false;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                            retinazer.active = false;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, EModeGlobalNPC.retiBoss);
                        }
                    }
                }
                return true;
            }
            #endregion
            if (Phase2 && P2State <= 3 && retinazer== null && !P3)//进尾杀
            {
                P2State = 4;
                P2Timer = 0;
                P2flag = 0;
                P3 = true;
                npc.dontTakeDamage = true;
                FargoSoulsUtil.ClearHostileProjectiles(1, npc.whoAmI);
            }
            if (!Phase2)
            {
                npc.damage = npc.defDamage;
                ref float ai_State = ref npc.ai[1];      // AI状态：0-火球，1-冲刺准备，2-冲刺中
                ref float ai_StateTimer = ref npc.ai[2]; // 状态计时器
                ref float ai_ShotTimer = ref npc.ai[3];  // 射击计时器

                if (!npc.HasPlayerTarget || Main.IsItDay())
                    return true;

                Player player = Main.player[npc.target];

                switch (ai_State)
                {
                    case 0: // 正常火球状态（持续600帧）
                        {
                            int num470 = 600;
                            if (ai_StateTimer >= num470 - 1) // 状态结束时切换到冲刺准备
                            {
                                ai_State = 1f;
                                ai_StateTimer = 0f;
                                ai_ShotTimer = 0f;
                                npc.TargetClosest();
                                npc.netUpdate = true;
                                goto case 1;
                            }

                            // 移动：保持在玩家侧方400像素
                            float accel = 2f;
                            Vector2 desired = player.Center - npc.Center + 450 * Vector2.UnitX.RotatedBy(MathHelper.Pi * ai_StateTimer / 120);
                            TwinDefaultMovement(npc, desired.X, desired.Y, accel, 3);
                            // 改进的P1火球：发射前减速
                            float delay = 43f;
                            if (WorldSavingSystem.MasochistModeReal) // 受虐模式射速更快
                                ai_ShotTimer += 0.25f;
                            if (ai_ShotTimer >= delay && ai_StateTimer > 20) // 发射火球
                            {
                                ai_ShotTimer = 0f;
                                Vector2 shootPos = new Vector2(npc.position.X + npc.width * 0.5f,
                                    npc.position.Y + npc.height * 0.5f);
                                float targetX = player.Center.X - shootPos.X;
                                float targetY = player.Center.Y - shootPos.Y;

                                if (FargoSoulsUtil.HostCheck)
                                {
                                    float vel = 14f;
                                    int projDamage = npc.GetAttackDamage_ForProjectiles(25f, 22f);
                                    float angle = (float)Math.Sqrt(targetX * targetX + targetY * targetY);
                                    angle = vel / angle; // 标准化
                                    targetX *= angle;
                                    targetY *= angle;

                                    // 添加随机偏移
                                    targetX += Main.rand.Next(-40, 41) * 0.05f;
                                    targetY += Main.rand.Next(-40, 41) * 0.05f;

                                    shootPos.X += targetX * 4f;
                                    shootPos.Y += targetY * 4f;

                                    // 发射绿色电球（特殊版本）
                                    for (float i = 0.7f ; i<=1.3f ; i += 0.3f)
                                    {
                                        int num473 = Projectile.NewProjectile(npc.GetSource_FromThis(),
                                        shootPos.X, shootPos.Y, i * targetX / 10f,i * targetY / 10f,
                                        ModContent.ProjectileType<MechElectricOrbSpaz>(),
                                        projDamage, 0f, Main.myPlayer,
                                        ai0: npc.target, ai2: MechElectricOrb.Green);
                                    }
                                    Vector2 shotVel = new(targetX, targetY);
                                    npc.velocity -= shotVel / 2f; // 后坐力

                                    // 生成电火花粒子
                                    for (int i = -3; i <= 3; i++)
                                    {
                                        Vector2 particleVel = (shotVel * 1.2f).RotatedBy(MathHelper.PiOver2 * 0.1f * i)
                                            .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                        Particle p = new ElectricSpark(shootPos - shotVel,
                                            particleVel, Color.Green, Main.rand.NextFloat(0.7f, 1f), 20);
                                        p.Spawn();
                                    }
                                }
                            }
                        }
                        break;

                    case 1: // 冲刺准备帧（与Retinazer类似）
                        {
                            float prepTime = WorldSavingSystem.MasochistModeReal ? 60 : 75;

                            if (ai_StateTimer == 0)
                            {
                                LockedRotation = player.DirectionTo(npc.Center).ToRotation();
                                int dir = Main.rand.NextBool() ? 1 : -1;

                                // 避免与Retinazer重叠
                                if (retinazer != null && retinazer.TypeAlive(NPCID.Spazmatism))
                                {
                                    float spazmatism_Angle = player.DirectionTo(retinazer.Center).ToRotation();
                                    dir = MathF.Sign(LockedRotation - spazmatism_Angle);
                                }
                                LockedRotation += dir * MathHelper.PiOver2 * (0.25f + Main.rand.NextFloat(0.2f));
                                npc.netUpdate = true;
                            }

                            float distance = WorldSavingSystem.MasochistModeReal ? 480f : 600f;
                            Vector2 desiredPos = player.Center + LockedRotation.ToRotationVector2() * distance;
                            float desiredX = desiredPos.X - npc.Center.X;
                            float desiredY = desiredPos.Y - npc.Center.Y;

                            float accel = 0.6f;
                            Retinazer.TwinDefaultMovement(npc, desiredX, desiredY, accel, 4);
                            Retinazer.TwinManageRotation(npc);

                            // 等待Retinazer就位
                            bool waitForReti = retinazer.ai[1] == npc.ai[1];
                            Vector2 spazDesiredPos = player.Center +
                                retinazer.GetGlobalNPC<PHRetinazer>().LockedRotation.ToRotationVector2() * distance;
                            if (retinazer.Distance(spazDesiredPos) <= 300)
                                waitForReti = false;

                            int flashDelay = WorldSavingSystem.MasochistModeReal ? 25 : 35;

                            // 同步调整
                            if (ai_StateTimer < prepTime - flashDelay && (npc.Distance(desiredPos) > 300 || waitForReti))
                            {
                                ai_StateTimer--;
                                npc.netUpdate = true;
                            }

                            if (ai_StateTimer < prepTime - flashDelay && retinazer.ai[2] < npc.ai[2])
                            {
                                npc.ai[2] = retinazer.ai[2];
                                npc.netUpdate = true;
                            }

                            // 冲刺前闪光提示
                            if (ai_StateTimer == prepTime - 25)
                            {
                                if (FargoSoulsUtil.HostCheck)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(),
                                        npc.Center + TwinsEyeFlash.Offset(npc), Vector2.Zero,
                                        ModContent.ProjectileType<TwinsEyeFlash>(), 0, 0, Main.myPlayer, npc.whoAmI);
                                }
                            }

                            // 准备时间结束，开始冲刺
                            if (++ai_StateTimer >= prepTime)
                            {
                                npc.velocity = npc.DirectionTo(player.Center) * 40;
                                ai_StateTimer = 0;
                                LockedRotation = 0;
                                ai_State = 2;
                                npc.netUpdate = true;
                            }
                            return false;
                        }

                    case 2: // 冲刺中状态
                        {
                            npc.damage = (int)(npc.defDamage * 1.5f); // 冲刺时伤害增加50%
                            ai_StateTimer += 1f;

                            if (ai_StateTimer >= 25f) // 冲刺25帧后结束
                            {
                                ai_ShotTimer += 1f;
                                ai_StateTimer = 0f;
                                npc.TargetClosest();

                                if (ai_ShotTimer >= 7f) // 完成7次冲刺后返回火球状态
                                {
                                    ai_State = 0f;
                                    ai_ShotTimer = 0f;
                                }
                                else // 否则继续准备冲刺
                                {
                                    ai_State = 1f;
                                }
                            }
                            if (ai_StateTimer % 2 == 0)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                            3f * Vector2.Normalize(npc.velocity), ModContent.ProjectileType<MechElectricOrbSpaz>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                            }
                            else
                            {
                                npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) - 1.57f;
                                if (ai_StateTimer >= 20)
                                    npc.velocity *= 0.95f;
                            }
                            return false;
                        }
                }
            }
            else // 第二阶段及以上
            {
                if (P3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Ichor, 0f, 0f, 0, default, 1.8f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 5f;
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, 0f, 0f, 0, default, 1.8f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 6f;
                    }
                    /*
                    if (P2Timer % 4 == 0)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 target = npc.Center + 1100 * Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / 16 + (float)P2Timer / 90);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), target,
                                        10 * Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / 16 + P2Timer / 90 + MathHelper.PiOver2 + MathHelper.ToRadians(Main.rand.NextFloat(-0.1f, 0.1f))),
                                        ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                        }
                    }
                    */
                }//粒子效果和领域展开
/*
                Player player = Main.player[npc.target];
                npc.AddBuff(BuffID.CursedInferno,9999999);
                npc.AddBuff(BuffID.Ichor, 9999999);
                if (retinazer != null)
                {
                    P2Timer = retinazer.GetGlobalNPC<PHRetinazer>().P2Timer;
                    P2State = retinazer.GetGlobalNPC<PHRetinazer>().P2State;
                    P2flag = retinazer.GetGlobalNPC<PHRetinazer>().P2flag;
                    DeathrayState = retinazer.GetGlobalNPC<PHRetinazer>().DeathrayState;
                }
                state = P2State;
                switch (P2State)
                {
                    case 0:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            if (P2Timer >= waittime)
                            {
                                Vector2 desired = player.Center - npc.Center + 500 * Vector2.UnitY.RotatedBy(P2flag * MathHelper.Pi / 6);
                                TwinDefaultMovement(npc, desired.X, desired.Y, 3f, 4);
                                TwinManageRotation(npc);
                                if (P2Timer % (down20 ? 20 : 30) == 0 && P2Timer > 60 + waittime)
                                {
                                    Vector2 vel = npc.SafeDirectionTo(player.Center);
                                    for (float i = 0.7f; i <= 1.3f; i+= 0.2f)
                                    {
                                        for (int j = -1; j <= 1; j++)
                                        {
                                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                            i * vel.RotatedBy(j * MathHelper.Pi / 3), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                                        }
                                        for (int j = -3; j <= 3; j++)
                                        {
                                            Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                                .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                            Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                                particleVel, Color.Green, Main.rand.NextFloat(0.7f, 1f), 20);
                                            p.Spawn();
                                        }
                                    }
                                    npc.velocity -= 2 * vel;
                                }
                                waittime = 0;
                            }
                        }
                        break;
                    case 1:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            if (P2Timer == 0) oriVel = -npc.SafeDirectionTo(player.Center);
                            Vector2 detalvec = oriVel.RotatedBy(P2Timer * MathHelper.Pi / 60);
                            Vector2 desired = player.Center - npc.Center + 500 * detalvec;
                            TwinDefaultMovement(npc, desired.X, desired.Y, 3f, 4);
                            npc.rotation = npc.SafeDirectionTo(player.Center).ToRotation() - MathHelper.Pi;
                            if (P2Timer % 5 == 0 && P2Timer > 30 && P2Timer < 330)
                            {
                                for (int i = 1; i <= 3; i++)
                                {
                                    Vector2 vel = detalvec.RotatedBy(MathHelper.PiOver2);
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                        i * vel, ModContent.ProjectileType<MechElectricOrbSpaz>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                        Main.myPlayer, npc.target, 1, ai2: MechElectricOrb.Green);
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                        10 * vel.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-0.1f, 0.1f))), 
                                        ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage),0f, Main.myPlayer);
                                    for (int j = -3; j <= 3; j++)
                                    {
                                        Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                            .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                        Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                            particleVel, Color.Green, Main.rand.NextFloat(0.7f, 1f), 20);
                                        p.Spawn();
                                    }
                                }
                            }
                        }
                        break;
                    case 2:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            int flagY = Math.Sign(npc.Center.Y - player.Center.Y);
                            Vector2 desired = player.Center - npc.Center + 500 * flagY * Vector2.UnitY;
                            TwinDefaultMovement(npc, desired.X, desired.Y, 0.2f, 3);
                            TwinManageRotation(npc);
                            Vector2 vel = npc.SafeDirectionTo(player.Center);
                            if (P2Timer % 5 == 0 && P2Timer > 20 && P2Timer <= 150)
                            {
                                for (int i = -2; i <= 2; i++)
                                {

                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                        3 * vel.RotatedBy(i * MathHelper.Pi / 5), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                        Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                                    for (int j = -3; j <= 3; j++)
                                    {
                                        Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                            .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                        Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                            particleVel, Color.Green, Main.rand.NextFloat(0.7f, 1f), 20);
                                        p.Spawn();
                                    }
                                }
                                npc.velocity -= 0.5f * vel;
                            }
                        }
                        break;
                    case 3:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Resist = false; // 取消减伤
                            if (retinazer != null)
                            {
                                Vector2 target = retinazer.Center + retinazer.SafeDirectionTo(npc.Center) * 100;
                                npc.velocity = (target - npc.Center) / 60;

                                FlameWheelCount = 1; // 基础2个
                                if (modifier < 0.4f) // 血量低于40%时3个
                                    FlameWheelCount = 2;
                                if (WorldSavingSystem.MasochistModeReal) // 受虐模式+1
                                    FlameWheelCount++;
                                if (Main.getGoodWorld || Main.zenithWorld)
                                    FlameWheelCount++;
                                if (P2Timer > 30 && P2Timer < 420)
                                {
                                    npc.rotation += 0.6f;
                                }
                                if (P2Timer < 30)
                                {
                                    npc.rotation = npc.SafeDirectionTo(retinazer.Center).ToRotation() - MathHelper.PiOver2;
                                    RealRotation = npc.rotation;
                                }
                                // 之后每15帧发射一次火焰轮
                                else if (P2Timer % (Main.getGoodWorld ? 12 : 16) == 0 && P2Timer <= 420)
                                {
                                    if (FargoSoulsUtil.HostCheck)
                                    {
                                        // 逐渐增加速度（30-150帧内从0到12）
                                        float speed = 12f * Math.Min((P2Timer - 30) / 120f, 1f);
                                        int timeLeft = (int)(speed / 12f * 90f); // 根据速度调整存在时间
                                        float baseRotation = -(retinazer.GetGlobalNPC<PHRetinazer>().StoredDirectionToPlayer ? 1f : -1f) * (float)P2Timer / 60f;

                                        if (timeLeft > 5) // 速度足够时才发射
                                        {
                                            for (int i = 0; i < FlameWheelCount; i++)
                                            {
                                                int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                                    (baseRotation + MathHelper.TwoPi / FlameWheelCount * i).ToRotationVector2() * speed,
                                                    ModContent.ProjectileType<MechElectricOrb>(),
                                                    FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage),
                                                    0f, Main.myPlayer, ai2: MechElectricOrb.Green);
                                                int q = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                                    (-baseRotation + MathHelper.TwoPi / FlameWheelCount * i).ToRotationVector2() * speed,
                                                    ModContent.ProjectileType<MechElectricOrb>(),
                                                    FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage),
                                                    0f, Main.myPlayer, ai2: MechElectricOrb.Green);
                                                if (p != Main.maxProjectiles)
                                                {
                                                    Main.projectile[p].timeLeft = timeLeft;
                                                    Main.projectile[q].timeLeft = timeLeft;
                                                }   
                                            }
                                        }
                                    }
                                }
                                return false;
                            }
                        }
                        break;
                    case 4://进入P3
                        if (FargoSoulsUtil.HostCheck)
                        {
                            npc.velocity *= 0.95f;
                            npc.dontTakeDamage = true; 
                            if (P2Timer == 30)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsFireBackground>(), 0, 0, Main.myPlayer, ai0: npc.whoAmI,1);
                            }
                            if (P2Timer > 30)
                            {
                                int heal = (int)(npc.lifeMax / 90 * Main.rand.NextFloat(1f, 1.5f));
                                npc.life += heal;
                                if (npc.life > 0.5f * npc.lifeMax)
                                    npc.life = (int)(0.5f * npc.lifeMax);
                                CombatText.NewText(npc.Hitbox, CombatText.HealLife, heal);
                            }
                            if (++P2Timer > 90)
                            {
                                P2Timer = 0;
                                P2State = 5;
                                P2flag = 0;
                                npc.dontTakeDamage = false;
                                SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                                ScreenShakeSystem.StartShake(20f);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CalcloneWave>(), 0, 0, Main.myPlayer, ai1: 1, ai2: 20);
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CalcloneWave>(), 0, 0, Main.myPlayer, ai1: 1, ai2: 16);
                            }
                        }
                        break;
                    case 5:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            if (P2Timer >= 0)
                            {
                                Vector2 desired = player.Center - npc.Center - 500 * Vector2.UnitY.RotatedBy(P2flag * MathHelper.Pi / 6);
                                TwinDefaultMovement(npc, desired.X, desired.Y, 3f, 4);
                                TwinManageRotation(npc);
                                int inter = 15;
                                if (P2Timer % inter == 0 && P2Timer >= 60)
                                {
                                    Vector2 vel = npc.SafeDirectionTo(player.Center);
                                    for (int i = -2; i <= 2; i++)
                                    {
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                            20 * vel.RotatedBy(i * MathHelper.Pi / 6), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                            30 * vel.RotatedBy(i * MathHelper.Pi / 6), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                                        for (int j = -3; j <= 3; j++)
                                        {
                                            Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                                .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                            Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                                particleVel, Color.Yellow, Main.rand.NextFloat(0.7f, 1f), 20);
                                            p.Spawn();
                                        }
                                        for (float k = 0.7f; k <= 1.3f; k += 0.2f)
                                        {
                                            for (int j = -1; j <= 1; j++)
                                            {
                                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel,
                                                k * vel.RotatedBy(j * MathHelper.Pi / 3), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                                Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                                            }
                                            for (int j = -3; j <= 3; j++)
                                            {
                                                Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                                    .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                                Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                                    particleVel, Color.Green, Main.rand.NextFloat(0.7f, 1f), 20);
                                                p.Spawn();
                                            }
                                        }
                                    }
                                    
                                    npc.velocity -= 1 * vel;
                                    P2flag += 1f;
                                }
                            }
                            if (++P2Timer >= 360)
                            {
                                P2Timer = 0;
                                P2State++;
                                P2flag = 0;
                            }
                        }
                        break;//p3尾杀开始
                    case 6:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            if (P2Timer == 0) oriVel = -npc.SafeDirectionTo(player.Center);
                            Vector2 detalvec = oriVel.RotatedBy(P2Timer * MathHelper.Pi / 60);
                            Vector2 desired = player.Center - npc.Center + 500 * detalvec;
                            TwinDefaultMovement(npc, desired.X, desired.Y, 3f, 4);
                            npc.rotation = npc.SafeDirectionTo(player.Center).ToRotation() - MathHelper.Pi;
                            if (P2Timer % 5 == 0 && P2Timer > 30 && P2Timer < 330)
                            {
                                for (int i = 1; i <= 3; i++)
                                {
                                    Vector2 vel = detalvec.RotatedBy(MathHelper.PiOver2);
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                        i * vel, ModContent.ProjectileType<MechElectricOrbSpaz>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                        Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                        10 * vel.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-0.1f, 0.1f))),
                                        ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                    for (int j = -3; j <= 3; j++)
                                    {
                                        Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                                            .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                                        Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel,
                                            particleVel, Color.Green, Main.rand.NextFloat(0.7f, 1f), 20);
                                        p.Spawn();
                                    }
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                        npc.SafeDirectionTo(player.Center).RotatedBy(MathHelper.PiOver4 * (i-1)), ModContent.ProjectileType<MechElectricOrbSpaz>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                        Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                                }
                            }
                            if (++P2Timer > 360)
                            {
                                P2Timer = 0;
                                P2State = 7;
                                P2flag = 0;
                            }
                        }
                        break;
                    case 7:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Resist = true;
                            if (P2Timer == 0)
                                P3target = 400 * Vector2.UnitX.RotatedBy(MathHelper.TwoPi * Main.rand.NextFloat(0,1));
                            Vector2 desired = player.Center - npc.Center + P3target;
                            if (P2Timer < 30)
                            {
                                TwinDefaultMovement(npc, desired.X, desired.Y, 3f, 4f);
                                TwinManageRotation(npc);
                            }
                            else
                            {
                                npc.velocity *= 0.9f;
                            }
                            if (P2Timer == 60)
                            {
                                float offect = 2 * MathHelper.Pi / 3;
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + offect),
                                    ModContent.ProjectileType<SpazmatismGlowLine>(), 0, 0f, Main.myPlayer, 0, npc.whoAmI, offect);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 - offect),
                                    ModContent.ProjectileType<SpazmatismGlowLine>(), 0, 0f, Main.myPlayer, 0, npc.whoAmI, -offect);
                                int p = Projectile.NewProjectile(npc.GetSource_FromThis(),
                                        npc.Center + TwinsEyeFlash.Offset(npc), Vector2.Zero,
                                        ModContent.ProjectileType<TwinsEyeFlash>(), 0, 0, Main.myPlayer, npc.whoAmI);
                                Main.projectile[p].scale = 1.6f;
                            }
                            if (P2Timer >= 120 && P2Timer < 150)
                            {
                                float rangeangle = (float)Math.Sqrt(P2Timer - 120) * (P2Timer - 120) * 0.99f;
                                for (int i = 0; i < 12; i++)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center - 1 * (npc.width - 24) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2),
                                        30 * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + MathHelper.ToRadians(Main.rand.NextFloat(-1, 1) + rangeangle)),
                                        ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center - 1 * (npc.width - 24) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2),
                                        30 * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + MathHelper.ToRadians(Main.rand.NextFloat(-1, 1) - rangeangle)),
                                        ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                }
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + 1 * (npc.width - 24) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2),
                                    30*Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + MathHelper.ToRadians(rangeangle)), ModContent.ProjectileType<MechElectricOrbPolyline>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                    Main.myPlayer, npc.target,1, ai2: MechElectricOrb.Green);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + 1 * (npc.width - 24) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2),
                                    30*Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + MathHelper.ToRadians(-rangeangle)), ModContent.ProjectileType<MechElectricOrbPolyline>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                    Main.myPlayer, npc.target,-1, ai2: MechElectricOrb.Green);
                                npc.velocity -= 0.9f * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
                                ScreenShakeSystem.StartShake(3f);
                            }
                            if (++P2Timer >= 180)
                            {
                                P2flag++;
                                P2Timer = 0;
                            }
                            if (P2flag >= 3)
                            {
                                P2State = 8;
                                P2Timer = 0;
                                P2flag = 0;
                            } 
                        }
                        break;
                    case 8:
                        if (FargoSoulsUtil.HostCheck)
                        {
                            if (P2Timer == 0)
                            {
                                SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                            }
                            if (P2Timer < 60)
                            {
                                if (P2Timer == 0) oriVel = -npc.SafeDirectionTo(player.Center);
                                Vector2 detalvec = oriVel.RotatedBy(MathHelper.TwoPi * Main.rand.NextFloat(0,1));
                                Vector2 desired = player.Center - npc.Center + 360 * detalvec;
                                TwinDefaultMovement(npc, desired.X, desired.Y, 0.6f, 4);
                                TwinManageRotation(npc);
                                float offect = MathHelper.Pi / 3;
                                if (P2Timer % 2 == 0)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + offect),
                                        ModContent.ProjectileType<SpazmatismGlowLine>(), 0, 0f, Main.myPlayer, 0, npc.whoAmI, offect);
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 - offect),
                                        ModContent.ProjectileType<SpazmatismGlowLine>(), 0, 0f, Main.myPlayer, 0, npc.whoAmI, -offect);
                                }
                            }

                            else
                            {
                                npc.velocity *= 0.9f;
                            }
                            if (P2Timer == 90)
                            {
                                SoundEngine.PlaySound(DeathrayFire);
                            }
                            if (P2Timer >= 120)
                            {
                                int scycletime = Main.getGoodWorld ? 80 : 120;
                                float omiga = MathHelper.Pi * (P2Timer - 120) / (60 * scycletime);
                                omiga = omiga >= MathHelper.Pi / scycletime ? MathHelper.Pi / scycletime : omiga;
                                npc.rotation += omiga;
                                for (int i = 0; i< 15; i++)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center - (npc.width - 24) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2),
                                        30 * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + MathHelper.ToRadians(Main.rand.NextFloat(-(P2Timer-120)/24f, (P2Timer - 120) / 24f))),
                                        ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                }
                                if (P2Timer >= 120 & P2Timer % 10 == 0)
                                {
                                    for (int i = 2;i <= 11; i++)
                                    {
                                        Vector2 target = 100 *  i * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2) + npc.Center;
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), target,
                                        Vector2.Normalize(player.Center - target), ModContent.ProjectileType<MechElectricOrbSpaz>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                        Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                                    }
                                }
                                ScreenShakeSystem.StartShake(3f);
                                npc.velocity -= (Main.zenithWorld ? 0.6f : 0.4f) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
                            }
                            if (++P2Timer > 120 + 360)
                            {
                                P2State = 5;
                                P2Timer = 0;
                                P2flag = 0;
                            }
                        }
                        break;
                }
                if (waittime == 0)
                {
                    return false;
                }
            }
            return true;
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (Resist)
                modifiers.FinalDamage *= 2;//修正
            base.ModifyIncomingHit(npc, ref modifiers);
        }
        public static void TwinDefaultMovement(NPC npc, float desiredX, float desiredY, float accel, float decelMult)
        {
            if (npc.velocity.X < desiredX)
            {
                npc.velocity.X += accel;
                if (npc.velocity.X < 0f && desiredX > 0f)
                {
                    npc.velocity.X += accel * decelMult;
                }
            }
            else if (npc.velocity.X > desiredX)
            {
                npc.velocity.X -= accel;
                if (npc.velocity.X > 0f && desiredX < 0f)
                {
                    npc.velocity.X -= accel * decelMult;
                }
            }
            if (npc.velocity.Y < desiredY)
            {
                npc.velocity.Y += accel;
                if (npc.velocity.Y < 0f && desiredY > 0f)
                {
                    npc.velocity.Y += accel * decelMult;
                }
            }
            else if (npc.velocity.Y > desiredY)
            {
                npc.velocity.Y -= accel;
                if (npc.velocity.Y > 0f && desiredY < 0f)
                {
                    npc.velocity.Y -= accel * decelMult;
                }
            }
        }
        public static void TwinManageRotation(NPC npc)
        {
            if (!npc.HasPlayerTarget)
                return;

            // 计算目标角度
            float num412 = npc.Center.X - Main.player[npc.target].Center.X;
            float num413 = npc.Center.Y - Main.player[npc.target].Center.Y;
            float num414 = (float)Math.Atan2(num413, num412) + 1.57f; // +90度（面朝下方）

            // 角度规范化（0-2π）
            if (num414 < 0f)
                num414 += 6.283f;
            else if (num414 > 6.283)
                num414 -= 6.283f;

            // 平滑旋转
            float num415 = 0.1f;
            if (npc.rotation < num414)
            {
                if ((num414 - npc.rotation) > 3.1415) // 如果逆时针旋转更快
                    npc.rotation -= num415;
                else
                    npc.rotation += num415;
            }
            else if (npc.rotation > num414)
            {
                if ((npc.rotation - num414) > 3.1415)
                    npc.rotation += num415;
                else
                    npc.rotation -= num415;
            }

            // 角度微调
            if (npc.rotation > num414 - num415 && npc.rotation < num414 + num415)
                npc.rotation = num414;

            // 再次规范化
            if (npc.rotation < 0f)
                npc.rotation += 6.283f;
            else if (npc.rotation > 6.283)
                npc.rotation -= 6.283f;
        }
    }
    public class PHTwinsModSystem : ModSystem
    {
        public override void Load()
        {
            MethodInfo targetMethod1 = typeof(Retinazer).GetMethod("SafePreAI", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(targetMethod1, ILTwinsAI);
            MethodInfo targetMethod2 = typeof(Spazmatism).GetMethod("SafePreAI", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(targetMethod2, ILTwinsAI);
        }
        private void ILTwinsAI(ILContext il)
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
    }
    */
//}
