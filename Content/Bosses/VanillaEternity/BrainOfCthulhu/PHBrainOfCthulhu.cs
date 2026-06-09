using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.ModLoader.IO;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using Microsoft.CodeAnalysis;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using static Terraria.Utilities.NPCUtils;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core;
using FargowiltasSouls;
using Luminance.Common.Utilities;
using ReLogic.Content;
using Terraria.Audio;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.BrainOfCthulhu
{
    public class PHBrainOfCthulhu : BrainofCthulhu
    {
        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            npc.lifeMax = (int)Math.Round(npc.lifeMax * 0.8f); 
            npc.scale -= 0.25f; 
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.life > 0)
                modifiers.FinalDamage /= Math.Max(0.18f, (float)Math.Sqrt((double)npc.life / npc.lifeMax)); // 最小减伤82%
            base.ModifyIncomingHit(npc, ref modifiers);
        }
        public override bool SafePreAI(NPC npc)
        {
            // 标记全局脑部Boss索引
            EModeGlobalNPC.brainBoss = npc.whoAmI;

            // 确保玩家debuff时间不会太短
            if (Main.LocalPlayer.active && Main.LocalPlayer.Eternity().ShorterDebuffsTimer < 2)
                Main.LocalPlayer.Eternity().ShorterDebuffsTimer = 2;

            // 玩家远离时强制消失逻辑
            if (!npc.HasValidTarget || npc.Distance(Main.player[npc.target].Center) > 3000)
            {
                if (++ForceDespawnTimer > 60) // 60帧后开始下降
                {
                    npc.velocity.Y += 0.75f; // 加速下降
                    if (npc.timeLeft > 60)
                        npc.timeLeft = 60; // 限制存在时间
                }
            }
            else
            {
                ForceDespawnTimer = 0; // 玩家在范围内重置计时器
            }

            // 隐身状态下保持安全距离（360像素）
            if (npc.alpha > 0 && (npc.ai[0] == 2 || npc.ai[0] == -3) && npc.HasValidTarget)
            {
                const float safeRange = 360;
                Vector2 stayAwayFromHere = Main.player[npc.target].Center;
                if (npc.Distance(stayAwayFromHere) < safeRange)
                    npc.Center = stayAwayFromHere + npc.DirectionFrom(stayAwayFromHere) * safeRange; // 瞬移到安全距离外
            }

            // 第二阶段特殊机制
            if (EnteredPhase2)
            {
                // 混乱攻击相关常量
                int confusionThreshold = 400;      // 混乱攻击总时长
                int confusionThreshold2 = confusionThreshold - 60; // 第二阶段开始时间

                // 分身冲刺参数
                float cloneTime = WorldSavingSystem.MasochistModeReal ? 50 : 50; // 受虐模式不影响
                int dashTime = 60; // 冲刺持续时间
                ref float teleportTimer = ref npc.localAI[1]; // 传送计时器引用

                // 状态判定
                bool confused = npc.HasPlayerTarget && Main.player[npc.target].HasBuff(BuffID.Confused);
                bool noFadeDash = ConfusionTimer.IsWithinBounds(confusionThreshold2 - 60, confusionThreshold2);

                // 分身淡出逻辑
                if (teleportTimer >= cloneTime - 25 && !noFadeDash)
                {
                    if (CloneFade == 0 && npc.HasPlayerTarget)
                    {
                        if (FargoSoulsUtil.HostCheck) // 主机检查
                        {
                            // 在玩家位置生成分身克隆
                            Player player = Main.player[npc.target];
                            if (WorldSavingSystem.MasochistModeReal)
                                FargoSoulsUtil.NewNPCEasy(npc.GetSource_FromAI(), player.Center + (player.Center - npc.Center),
                                    ModContent.NPCType<BrainClone>(), npc.whoAmI);
                        }
                    }
                    if (CloneFade < 1)
                        CloneFade += 0.05f; // 逐渐淡出
                }
                else
                {
                    CloneFade = 0; // 重置透明度
                }

                // 分身冲刺攻击逻辑
                if (teleportTimer >= cloneTime && teleportTimer <= 60 && !noFadeDash)
                {
                    if (!confused)
                    {
                        ConfusionTimer++; // 未混乱时增加计时器
                    }

                    // 执行冲刺
                    if (ClonefadeDashTimer < dashTime && npc.HasPlayerTarget)
                    {
                        Player player = Main.player[npc.target];
                        if (ClonefadeDashTimer == 0)
                        {
                            npc.netUpdate = true; // 首次冲刺时同步网络
                        }

                        KnockbackImmune = true; // 冲刺期间免疫击退
                        ClonefadeDashTimer++;
                        teleportTimer = cloneTime + 5; // 重置传送计时器
                        npc.velocity += npc.DirectionTo(player.Center) * 0.28f; // 向玩家加速
                    }
                    else
                    {
                        teleportTimer = 60; // 冲刺结束
                        npc.netUpdate = true; // 网络同步
                    }
                }

                // 冲刺前的安全距离保持
                if (teleportTimer < cloneTime)
                {
                    const float safeRange = 360;
                    Vector2 stayAwayFromHere = Main.player[npc.target].Center;
                    if (npc.Distance(stayAwayFromHere) < safeRange)
                        npc.Center = stayAwayFromHere + npc.DirectionFrom(stayAwayFromHere) * safeRange;

                    ClonefadeDashTimer = 0; // 重置冲刺计时器
                    KnockbackImmune = false; // 取消击退免疫
                }

                // 隐身时清除debuff
                if (npc.alpha > 0 && npc.buffType[0] != 0)
                {
                    npc.DelBuff(0);
                }

                // 混乱攻击提示环生成函数
                void TelegraphConfusion(Vector2 spawn)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawn, Vector2.Zero,
                        ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 8, 180);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawn, Vector2.Zero,
                        ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 8, 200);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawn, Vector2.Zero,
                        ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 8, 220);
                };

                // 激光扩散攻击函数
                void LaserSpread(Vector2 spawn)
                {
                    if (npc.life > npc.lifeMax / 2 && !WorldSavingSystem.MasochistModeReal)
                        return; // 血量高于50%且非受虐模式时不发射激光

                    if (npc.HasValidTarget && FargoSoulsUtil.HostCheck)
                    {
                        int max = WorldSavingSystem.MasochistModeReal ? 7 : 3; // 受虐模式更多激光
                        int degree = WorldSavingSystem.MasochistModeReal ? 2 : 3; // 角度间隔
                        int laserDamage = FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 3);

                        // 生成混乱效果弹幕
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawn, new Vector2(0, -4),
                            ModContent.ProjectileType<BrainofConfusion>(), 0, 0, Main.myPlayer);

                        // 生成激光束
                        for (int i = -max; i <= max; i++)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawn,
                                0.2f * Main.player[npc.target].DirectionFrom(spawn).RotatedBy(MathHelper.ToRadians(degree) * i),
                                ModContent.ProjectileType<DestroyerLaser>(), laserDamage, 0f, Main.myPlayer);
                    }
                };

                // 混乱攻击倒计时
                if (--ConfusionTimer < 0)
                {
                    ConfusionTimer = confusionThreshold; // 重置计时器

                    if (!Main.player[npc.target].HasBuff(BuffID.Confused))
                    {
                        SoundEngine.PlaySound(SoundID.Roar, npc.Center); // 播放咆哮音效

                        // 在玩家四个对角线方向生成提示环
                        Vector2 offset = npc.Center - Main.player[npc.target].Center;
                        Vector2 spawnPos = Main.player[npc.target].Center;

                        TelegraphConfusion(new Vector2(spawnPos.X + offset.X, spawnPos.Y + offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X + offset.X, spawnPos.Y - offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X - offset.X, spawnPos.Y + offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X - offset.X, spawnPos.Y - offset.Y));
                    }

                    npc.netUpdate = true; // 网络同步
                    NetSync(npc);
                }
                else if (ConfusionTimer > confusionThreshold2) // 混乱攻击第二阶段（提示阶段）
                {
                    KnockbackImmune = false; // 取消击退免疫
                    teleportTimer = 2; // 禁止传送

                    // 非受虐模式且血量高时不使用分身冲刺
                    if (!(npc.life > npc.lifeMax / 2 && !WorldSavingSystem.MasochistModeReal))
                    {
                        ClonefadeDashTimer = 0;
                        CloneFade = 0;
                    }

                    // 判断是否需要移动
                    bool isConfused = Main.player[npc.target].HasBuff(BuffID.Confused);
                    bool shouldMove = ConfusionTimer == confusionThreshold2 + 1 ? confused : !confused;

                    // 移动到预定位置
                    if (shouldMove)
                    {
                        if (npc.HasPlayerTarget)
                        {
                            Player player = Main.player[npc.target];
                            Vector2 desiredPos = player.Center;
                            Vector2 toNPC = npc.Center - desiredPos;
                            // 计算目标位置（玩家对角线方向300像素）
                            desiredPos += Vector2.UnitX * MathF.Sign(toNPC.X) * 300f +
                                          Vector2.UnitY * MathF.Sign(toNPC.Y) * 300f;
                            npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionTo(desiredPos) *
                                Math.Min(10, npc.Distance(desiredPos)), 0.2f); // 平滑移动
                            KnockbackImmune = true; // 移动时免疫击退
                        }
                    }

                    // 提示环生成函数
                    void TelegraphCircle()
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            // 根据计时器计算环的大小
                            float size = 20f + 180f * (ConfusionTimer - confusionThreshold2) /
                                (confusionThreshold - confusionThreshold2);
                            foreach (Player p in Main.player.Where(p => p.Alive()))
                                Projectile.NewProjectile(npc.GetSource_FromThis(), p.Center, Vector2.Zero,
                                    ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 15, size);
                        }
                    }

                    // 非受虐模式下播放提示音效
                    if (ConfusionTimer % 15 == 0 && !WorldSavingSystem.MasochistModeReal)
                        if (!Main.dedServ)
                        {
                            TelegraphCircle();
                            SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/ReticleBeep"),
                                Main.LocalPlayer.Center);
                        }

                    // 混乱攻击触发点
                    if (ConfusionTimer == confusionThreshold2 + 2)
                    {
                        if (!WorldSavingSystem.MasochistModeReal)
                        {
                            ConfusionIdleTimer = ConfusionIdleTime; // 设置空闲时间
                            npc.netUpdate = true;
                            if (!Main.dedServ)
                            {
                                TelegraphCircle();
                                SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/ReticleBeep")
                                    with
                                { Pitch = -0.5f }, Main.LocalPlayer.Center);
                            }
                        }

                        // 对玩家施加或清除混乱debuff
                        if (npc.Distance(Main.LocalPlayer.Center) < 3000)
                        {
                            if (!Main.LocalPlayer.HasBuff(BuffID.Confused))
                            {
                                int idle = WorldSavingSystem.MasochistModeReal ? 0 : ConfusionIdleTime;
                                FargoSoulsUtil.AddDebuffFixedDuration(Main.LocalPlayer, BuffID.Confused,
                                    confusionThreshold + 10 + idle, false);
                            }
                            else
                                Main.LocalPlayer.ClearBuff(BuffID.Confused);
                        }
                    }

                    // 空闲计时器处理
                    if (ConfusionTimer == confusionThreshold2 + 1)
                    {
                        if (ConfusionIdleTimer > 0)
                        {
                            ConfusionIdleTimer--;
                            ConfusionTimer++; // 延长时间
                        }
                    }
                }
                else if (ConfusionTimer == confusionThreshold2) // 混乱攻击执行点
                {
                    // 玩家未混乱时生成攻击幻影
                    if (!Main.player[npc.target].HasBuff(BuffID.Confused))
                    {
                        SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                        TelegraphConfusion(npc.Center);

                        IllusionTimer = 120 + 90; // 重置幻影计时器

                        if (FargoSoulsUtil.HostCheck)
                        {
                            int type = ModContent.ProjectileType<BrainIllusionProj>();
                            int alpha = (int)(255f * npc.life / npc.lifeMax);

                            // 幻影生成函数
                            void SpawnClone(Vector2 center)
                            {
                                int n = NPC.NewNPC(npc.GetSource_FromAI(), (int)center.X, (int)center.Y,
                                    ModContent.NPCType<BrainIllusionAttack>(), npc.whoAmI, npc.whoAmI, alpha);
                                if (n != Main.maxNPCs)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                            }

                            // 清理并转换现有幻影为攻击幻影
                            foreach (Projectile p in Main.projectile.Where(p => p.active && p.type == type &&
                                p.ai[0] == npc.whoAmI && p.ai[1] == 0f))
                            {
                                if (p.Distance(Main.player[npc.target].Center) < 1000)
                                {
                                    SpawnClone(p.Center);
                                }
                                p.Kill();
                            }

                            // 在四个对角线位置生成攻击幻影
                            Vector2 offset = npc.Center - Main.player[npc.target].Center;
                            Vector2 spawnPos = Main.player[npc.target].Center;

                            SpawnClone(new Vector2(spawnPos.X + offset.X, spawnPos.Y + offset.Y));
                            SpawnClone(new Vector2(spawnPos.X + offset.X, spawnPos.Y - offset.Y));
                            SpawnClone(new Vector2(spawnPos.X - offset.X, spawnPos.Y + offset.Y));
                            SpawnClone(new Vector2(spawnPos.X - offset.X, spawnPos.Y - offset.Y));
                        }
                    }
                    else // 玩家已混乱时发射激光
                    {
                        Vector2 offset = npc.Center - Main.player[npc.target].Center;
                        Vector2 spawnPos = Main.player[npc.target].Center;

                        LaserSpread(new Vector2(spawnPos.X + offset.X, spawnPos.Y + offset.Y));
                        LaserSpread(new Vector2(spawnPos.X + offset.X, spawnPos.Y - offset.Y));
                        LaserSpread(new Vector2(spawnPos.X - offset.X, spawnPos.Y + offset.Y));
                        LaserSpread(new Vector2(spawnPos.X - offset.X, spawnPos.Y - offset.Y));
                    }
                }

                // 幻影生成逻辑
                if (--IllusionTimer < 0)
                {
                    // 根据条件设置幻影生成间隔
                    IllusionTimer = Main.rand.Next(5, 11);
                    if (npc.life > npc.lifeMax / 2)
                        IllusionTimer += 5; // 血量高时生成更慢
                    if (npc.life < npc.lifeMax / 10)
                        IllusionTimer -= 2; // 血量低时生成更快
                    if (WorldSavingSystem.MasochistModeReal)
                        IllusionTimer -= 2; // 受虐模式生成更快
                    npc.netUpdate = true;

                    // 生成移动幻影弹幕
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 spawn = Main.player[npc.target].Center + Main.rand.NextVector2CircularEdge(1200f, 1200f);
                        Vector2 speed = Main.player[npc.target].Center + Main.player[npc.target].velocity * 45f +
                            Main.rand.NextVector2Circular(-600f, 600f) - spawn;
                        speed = Vector2.Normalize(speed) * Main.rand.NextFloat(12f, 48f);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawn, speed,
                            ModContent.ProjectileType<BrainIllusionProj>(),
                            FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 3), 0f, Main.myPlayer, npc.whoAmI);
                    }
                }

                // 幻影攻击期间的特殊AI控制
                if (IllusionTimer > 60)
                {
                    // 强制传送
                    if (npc.ai[0] == -1f && npc.localAI[1] < 80)
                    {
                        npc.localAI[1] = 80f;
                    }
                    // 保持隐身无敌状态
                    if (npc.ai[0] == -3f && npc.ai[3] > 200)
                    {
                        npc.dontTakeDamage = true;
                        npc.ai[0] = -3f;
                        npc.ai[3] = 255;
                        npc.alpha = 255;
                        return false;
                    }
                }
                // 幻影攻击结束
                if (IllusionTimer == 60)
                {
                    npc.localAI[1] = 120;
                    npc.ai[0] = -1;
                }
            }
            // 进入第二阶段的初始化
            else if (!npc.dontTakeDamage)
            {
                EnteredPhase2 = true; // 标记已进入第二阶段

                // 生成三个幻影分身
                if (FargoSoulsUtil.HostCheck)
                {
                    bool recolor = true && WorldSavingSystem.EternityMode;
                    int type = recolor ? ModContent.NPCType<BrainIllusion2>() : ModContent.NPCType<BrainIllusion>();

                    // 生成三个不同偏移的分身
                    FargoSoulsUtil.NewNPCEasy(npc.GetSource_FromAI(), npc.Center, type, npc.whoAmI, npc.whoAmI, -1, 1);
                    FargoSoulsUtil.NewNPCEasy(npc.GetSource_FromAI(), npc.Center, type, npc.whoAmI, npc.whoAmI, 1, -1);
                    FargoSoulsUtil.NewNPCEasy(npc.GetSource_FromAI(), npc.Center, type, npc.whoAmI, npc.whoAmI, 1, 1);

                    // 清理旧的金色脓液弹幕
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<GoldenShowerHoming>())
                            Main.projectile[i].Kill();
                    }
                }
            }

            // 掉落召唤物
            EModeUtils.DropSummon(npc, "GoreySpine", NPC.downedBoss2, ref DroppedSummon);

            // 强制防御为0
            npc.defense = 0;
            npc.defDefense = 0;

            return true;
        }

    }
    public class PHBrainOfCthulhuModSystem : ModSystem
    {
        public override void Load()
        {
            ApplyILEdits();
        }       
        private void ApplyILEdits()
        {
            // First, get the MethodInfo of the method you want to apply the IL patch to.
            MethodInfo targetMethod = typeof(BrainofCthulhu).GetMethod("SafePreAI", BindingFlags.Instance | BindingFlags.Public);

            // Call MonoModHooks.Modify using the target method and your patch method.
            MonoModHooks.Modify(targetMethod, ILBOCAI);
        }
        private void ILBOCAI(ILContext il)
        {
            var c = new ILCursor(il);
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
}