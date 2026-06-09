using FargowiltasSouls;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.NPCs;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using MonoMod.Cil;
using Luminance.Common.Utilities;
using System.Reflection;
using Mono.Cecil.Cil;

namespace FargosPhantasmMode.Content.Bosses.EyeOfCthulhu
{
    internal class PHKingSlime : KingSlime
    {
        const int SpecialJumpTime = 60 * 15; // 特殊跳跃所需的计时器值（15秒）
        const int SummonWaves = 6; // 召唤小史莱姆的波次数


        public override bool SafePreAI(NPC npc)
        {
            PHKingSlimeAI(npc);
            return true;
        }
        public void PHKingSlimeAI(NPC npc)
        {
            // 攻击冷却时间递减
            if (CertainAttackCooldown > 0)
                CertainAttackCooldown--;
            npc.TargetClosest();
            Player player = Main.player[npc.target];
            ref float teleportTimer = ref npc.ai[2];
            // 当传送计时器在145到150之间时，暂停传送执行特殊跳跃
            if (teleportTimer >= 145 && teleportTimer < 150)
            {
                if (JumpTimer < SpecialJumpTime)
                    JumpTimer = SpecialJumpTime; // 强制设置跳跃计时器
                teleportTimer = 145; // 暂停传送计时器
            }

            // 血量达到阈值时召唤小史莱姆
            if (npc.GetLifePercent() < SummonCounter / SummonWaves && (CertainAttackCooldown <= 0 || WorldSavingSystem.MasochistModeReal))
            {
                const int Slimes = 6; // 每次召唤6只小史莱姆
                CertainAttackCooldown = 180; // 设置3秒冷却时间

                if (FargoSoulsUtil.HostCheck) // 仅在主机端生成
                {
                    /*//生成小史莱姆
                    for (int i = 0; i < Slimes; i++)
                    {
                        // 随机生成位置
                        int x = (int)(npc.position.X + Main.rand.NextFloat(npc.width - 32));
                        int y = (int)(npc.position.Y + Main.rand.NextFloat(npc.height - 32));
                        int type = ModContent.NPCType<SlimeSwarm>(); // 获取小史莱姆类型

                        int slime = NPC.NewNPC(npc.GetSource_FromThis(), x, y, type);
                        if (slime.IsWithinBounds(Main.maxNPCs))
                        {
                            Main.npc[slime].SetDefaults(type);
                            // 设置随机速度
                            Main.npc[slime].velocity.X = Main.rand.NextFloat(-15, 16) * 0.1f;
                            Main.npc[slime].velocity.Y = Main.rand.NextFloat(-30, -15) * 0.3f;

                            // 如果有有效目标，向玩家方向跳跃
                            if (npc.HasValidTarget)
                            {
                                Main.npc[slime].ai[0] = Math.Sign(player.Center.X - npc.Center.X);
                                Main.npc[slime].velocity.X = Main.rand.NextFloat(10, 16) * 0.4f * -npc.HorizontalDirectionTo(player.Center);
                            }

                            // 网络同步
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, slime);
                            }
                        }
                    }
                    */
                }

                SoundEngine.PlaySound(SoundID.Item167, npc.Center); // 播放召唤音效
                SummonCounter--; // 减少召唤计数器
            }

            // 在受虐模式下，给国王史莱姆添加额外的水平移动（恢复）
            
            if (WorldSavingSystem.MasochistModeReal)
                npc.position.X -= npc.velocity.X * 0.2f;
            
            // 着陆攻击逻辑
            if (LandingAttackReady)
            {
                if (npc.velocity.Y == 0f) // 当史莱姆着陆时
                {
                    LandingAttackReady = false; // 重置着陆攻击标志

                    // 执行特殊跳跃后的着陆攻击
                    if (JumpTimer >= SpecialJumpTime && !SpecialJumping && (CertainAttackCooldown <= 0 || WorldSavingSystem.MasochistModeReal))
                    {
                        /*
                        SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/VanillaEternity/KingSlime/KSCharge"), npc.Center);
                        // 生成粒子效果
                        Particle p = new ExpandingBloomParticle(npc.Center, Vector2.Zero, Color.Blue, Vector2.One, Vector2.One * 60, 40, true, Color.Transparent);
                        */
                        SpecialJumping = true; // 标记为特殊跳跃
                        CertainAttackCooldown = 240; // 4秒冷却
                        SpecialJumpWindupTimer = 60; // 1秒准备时间
                        //p.Spawn();
                    }
                    else
                    {
                        if (SpecialJumping) // 特殊跳跃结束
                        {
                            JumpTimer = 0; // 重置跳跃计时器
                            SpecialJumping = false; // 结束特殊跳跃
                            teleportTimer = 150; // 继续传送计时器
                        }
                        else // 普通着陆攻击
                        {
                            if (FargoSoulsUtil.HostCheck)
                            {
                                
                                // 受虐模式下的尖刺喷射
                                if (WorldSavingSystem.MasochistModeReal)
                                {
                                    for (int i = 0; i < 30; i++)
                                    {
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.Center.X + Main.rand.Next(-5, 5), npc.Center.Y - 15),
                                            new Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-8, -5)),
                                            ModContent.ProjectileType<RainbowSlimeSpike>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                    }
                                }
                                

                                // 受虐模式下的粘液球攻击
                                /*
                                if (WorldSavingSystem.MasochistModeReal && npc.HasValidTarget)
                                {
                                    SoundEngine.PlaySound(SoundID.Item21, player.Center);
                                    if (FargoSoulsUtil.HostCheck)
                                    {
                                        for (int i = 0; i < 6; i++) // 生成6个粘液球
                                        {
                                            Vector2 spawn = player.Center;
                                            spawn.X += Main.rand.Next(-150, 151); // 水平随机偏移
                                            spawn.Y -= Main.rand.Next(600, 901); // 从玩家上方生成
                                            Vector2 speed = player.Center - spawn;
                                            speed.Normalize();
                                            speed *= IsBerserk ? 10f : 5f; // 狂暴模式下速度更快
                                            speed = speed.RotatedByRandom(MathHelper.ToRadians(4)); // 添加随机旋转
                                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawn, speed, ModContent.ProjectileType<SlimeBallHostile>(),
                                                FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0f, Main.myPlayer);
                                        }
                                    }
                                }
                                */
                            }
                        }
                    }
                }
            }
            else if (npc.velocity.Y > 0) // 如果史莱姆正在下落
            {
                // 标记下一次着陆时将执行着陆攻击
                LandingAttackReady = true;
            }

            // 跳跃上升阶段的逻辑
            if (npc.velocity.Y < 0) // 跳跃上升中
            {
                if (!CurrentlyJumping) // 每跳只执行一次
                {
                    CurrentlyJumping = true; // 标记为正在跳跃

                    // 特殊跳跃逻辑
                    if (SpecialJumping)
                    {
                        //npc.velocity.Y = -18; // 设置垂直速度
                        int direction = Math.Sign(player.Center.X - npc.Center.X); // 玩家方向
                        int pastPlayer = 1000; // 跳过玩家的距离
                        Vector2 desiredDestination = player.Center + (Vector2.UnitX * pastPlayer * direction); // 计算目标位置

                        // 物理计算：计算跳跃时间
                        float jumpTime = Math.Abs(2 * npc.velocity.Y / npc.gravity);
                        //npc.velocity.X = (desiredDestination.X - npc.Center.X) / jumpTime; // 计算水平速度
                        //SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/VanillaEternity/KingSlime/KSJump"), npc.Center);
                    }
                    else // 普通跳跃
                    {
                        bool shootSpikes = false; // 是否发射尖刺

                        if (WorldSavingSystem.MasochistModeReal)
                            shootSpikes = true; // 受虐模式下总是发射尖刺

                        if (npc.HasValidTarget)
                        {
                            // 如果玩家在史莱姆上方，跳得更高
                            
                            if (player.Center.Y < npc.position.Y + npc.height - 240)
                            {
                                //npc.velocity.Y *= 1.5f;
                                shootSpikes = true; // 原版逻辑（已注释）
                            }
                            
                            // 根据与玩家的水平距离调整跳跃
                            const int XThreshold = 0;
                            float xDif = Math.Abs(player.Center.X - npc.Center.X);
                            if (xDif > XThreshold)
                            {
                                float modifier = xDif - XThreshold;
                                modifier /= 700f;
                                modifier *= modifier; // 平方增加效果
                                modifier += 1;
                                modifier = MathHelper.Clamp(modifier, 1, 3); // 限制在1-3倍之间
                                //npc.velocity.X *= modifier; // 调整水平速度
                                //npc.velocity.Y *= Math.Min((float)Math.Cbrt(modifier), 1.5f); // 调整垂直速度

                                // 额外增加水平速度
                                //npc.velocity.X += Math.Sign(npc.velocity.X) * 2.25f;
                            }
                        }

                        if (npc.ai[1] == 0) // 如果是大跳，不发射尖刺
                            shootSpikes = false;

                        // 发射尖刺弹幕
                        if (shootSpikes && FargoSoulsUtil.HostCheck)
                        {
                            const float gravity = 0.15f; // 重力值
                            float time = 90f; // 飞行时间
                            // 计算预测玩家位置的弹道
                            Vector2 distance = player.Center - npc.Center + player.velocity * 30f;
                            distance.X /= time;
                            distance.Y = distance.Y / time - 0.5f * gravity * time;
                            /*
                            for (int i = 0; i < 15; i++) // 发射15个尖刺
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, distance + Main.rand.NextVector2Square(-1f, 1f),
                                    ModContent.ProjectileType<SlimeSpike>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                            }
                            */
                        }
                    }
                }
            }
            else // 不在跳跃上升阶段
            {
                CurrentlyJumping = false; // 重置跳跃标志
            }

            // 在地面上的逻辑

            if (npc.velocity.Y == 0) // 在地面上
            {
                if (SpecialJumpWindupTimer > 0) // 特殊跳跃准备阶段
                {
                    npc.ai[0] = -999; // 阻止原版跳跃
                    SpecialJumpWindupTimer--;
                    if (SpecialJumpWindupTimer == 0)
                        npc.ai[0] = -1; // 允许跳跃
                }
            }
            else // 在空中
            {
                if (SpecialJumping) // 特殊跳跃中的逻辑
                {
                    JumpTimer++; // 增加跳跃计时器

                    const int ProjTime = 5; // 弹幕生成间隔

                    // 如果方向错误且距离玩家太远，取消特殊跳跃
                    if (Math.Sign(npc.velocity.X) != Math.Sign(npc.DirectionTo(player.Center).X) &&
                        Math.Abs(npc.Center.X - player.Center.X) > 250 && npc.velocity.Y > 0)
                    {
                        npc.velocity.X *= 2f; // 减速
                        SpecialJumping = false;
                        JumpTimer = 0;
                        teleportTimer = 150; // 继续传送
                    }
                    // 周期性生成尖刺弹幕
                    /*
                    else if (JumpTimer % ProjTime < 1 && (JumpTimer % (ProjTime * 3) > 1 || WorldSavingSystem.MasochistModeReal))
                    {
                        SoundEngine.PlaySound(SoundID.Item17, npc.Center);
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Vector2 spawnPos = npc.Bottom; // 从底部生成
                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, -4 * Vector2.UnitY,
                                ModContent.ProjectileType<SlimeSpike2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0f, Main.myPlayer);
                            
                        }
                    }
                    */
                }
            }

            // 狂暴状态或血量低于66%时，执行尖刺雨攻击
            if ((IsBerserk || npc.life < npc.lifeMax * .66f) && npc.HasValidTarget && !SpecialJumping)
            {
                if (--SpikeRainCounter < -120) // 尖刺雨计数器
                {
                    SpikeRainCounter = 120; // 4秒冷却

                    if (FargoSoulsUtil.HostCheck)
                    {
                        const int Gap = 300; // 尖刺间隔
                        Vector2 spawnPos = player.Center + (Vector2.UnitX * Main.rand.Next(-Gap / 2, Gap / 2));
                        for (int i = -12; i <= 12; i++) // 生成25个尖刺
                        {
                            Vector2 spikePos = spawnPos;
                            spikePos.X += Gap * i; // 水平分布
                            spikePos.Y -= 500; // 在玩家上方500像素生成
                            for (int j = -1; j <= 1; j += 2)
                            {
                                int p = Projectile.NewProjectile(npc.GetSource_FromThis(), spikePos, j * 2f * Vector2.UnitX - 2 * Vector2.UnitY,
                                ModContent.ProjectileType<SlimeSpike2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0f, Main.myPlayer);
                                Main.projectile[p].velocity.Y -= 0.15f;
                                Main.projectile[p].aiStyle = -1;
                            }
                            if (Main.zenithWorld || Main.getGoodWorld)
                            {
                                for (int j = -1; j <= 1; j += 2)
                                {
                                    int q = Projectile.NewProjectile(npc.GetSource_FromThis(), 0.4f * Gap * Vector2.UnitX + spikePos - 125 * Vector2.UnitY, j * 2f * Vector2.UnitX - 2 * Vector2.UnitY,
                                    ModContent.ProjectileType<SlimeSpike2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0f, Main.myPlayer);
                                    Main.projectile[q].velocity.Y -= 0.15f;
                                    Main.projectile[q].aiStyle = -1;
                                }  
                            }
                            if (Main.zenithWorld)
                            {
                                for (int j = -1; j <= 1; j += 2)
                                {
                                    int q = Projectile.NewProjectile(npc.GetSource_FromThis(), 0.8f * Gap * Vector2.UnitX + spikePos - 250 * Vector2.UnitY, j * 2f * Vector2.UnitX - 2.2f * Vector2.UnitY,
                                    ModContent.ProjectileType<SlimeSpike2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0f, Main.myPlayer);
                                    Main.projectile[q].velocity.Y -= 0.15f;
                                    Main.projectile[q].aiStyle = -1;
                                }

                            }
                        }
                    }
                }
            }
            // 传送时的视觉效果
            if (npc.ai[1] == 5) // 原版传送阶段
            {
                // 更新传送目标Y坐标
                if (npc.HasPlayerTarget && npc.ai[0] == 1)
                    npc.localAI[2] = player.Center.Y;

                // 计算传送位置
                Vector2 tpPos = new(npc.localAI[1], npc.localAI[2]);
                tpPos.X -= npc.width / 2;

                // 生成传送粒子效果
                for (int i = 0; i < 10; i++)
                {
                    int d = Dust.NewDust(tpPos, npc.width, npc.height / 2, DustID.t_Slime, 0, 0, 75, new Color(78, 136, 255, 80), 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity.Y -= 3f;
                    Main.dust[d].velocity *= 3f;
                }
            }

            // 掉落召唤物
            EModeUtils.DropSummon(npc, "SlimyCrown", NPC.downedSlimeKing, ref DroppedSummon);
        }



        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            // 先调用基类方法
            base.OnHitPlayer(npc, target, hurtInfo);
        }
    }
    public class PHKingSlimeModSystem : ModSystem
    {
        public override void Load()
        {
            MethodInfo targetMethod = typeof(KingSlime).GetMethod("SafePreAI", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(targetMethod, ILKSAI);
        }
        private void ILKSAI(ILContext il)
        {
            ILCursor c = new(il);
            ILCursor d = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(110)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 160);
            /*
            if (!d.TryGotoNext(MoveType.After, i => i.MatchLdcI4(55)))
                throw new Exception("IL edit failed!");
            d.Emit(OpCodes.Pop);
            d.Emit(OpCodes.Ldc_I4, 100);
            */
        }
    }
}
