using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Content.BossBars;
using FargowiltasSouls.Content.Bosses.AbomBoss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Summons;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using FargowiltasSouls.Content.Buffs.Boss;
using System.Collections.Generic;
using FargosPhantasmMode.Common;
using static FargosPhantasmMode.Common.IDelegateStateMachine;

namespace FargosPhantasmMode.Content.Bosses.Abom
{
    public class AbomBossOverride : PModeNPCBehaviour, IDelegateStateMachine
    {
        private string TownNPCName;
        private bool droppedSummon = false;

        public int ritualProj, ringProj, spriteProj, ritualProjMaso, ritualProjFTW;
        public int PhaseIndex = 0;
        public Vector2 targetPos;
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(ModContent.NPCType<AbomBoss>());
        public AIMethod AIState { get; set; }
        public List<List<AIMethod>> PhaseList => [[ThrowScythes], Phase1, Phase1, Phase3];
        private List<AIMethod> Phase1 => [
            ThrowScythes,
            FlamingScytheSpread,
            PhoenixDash,
            SpecialAttackJump1st,
            SpecialAttackJump2nd,
            ChooseStrongAttack,//会自动判断是否在P2
            ];
        private List<AIMethod> Phase3 => [
            Final_ThrowScythes,
            Final_LaevateinnSword,
            SaucerWindmill,
            Final_LaevateinnSword,
            Final_PreHorizontalLaevateinn,
            Final_HorizontalLaevateinn,
            Final_HorizontalLaevateinn,
            Final_TabooLaevateinn,
            ActuallyDead
            ];
        public List<AIMethod> RitualCanNotMove => [
            PreDeathRain1st, DeathraysDash1st, PreDeathRain2nd, DeathraysDash2nd, LaevateinnSword, LaevateinnDash, WaitScythesClear, PreVerticalDive, VerticalLaevateinn, WaitScythesClear2nd,
            Final_LaevateinnSword,Final_PreHorizontalLaevateinn, Final_HorizontalLaevateinn, Final_TabooLaevateinn
            ];
        public override void SetDefaults(NPC npc)
        {
            npc.damage = 150;
            npc.defense = 100;
            npc.lifeMax = 1296000; // 680000
            npc.netAlways = true;
            npc.BossBar = ModContent.GetInstance<AbominationnBossBar>();
            AIState = ThrowScythes;
            base.SetDefaults(npc);
        }
        public override bool CanHitPlayer(NPC NPC, Player target, ref int CooldownSlot)
        {
            CooldownSlot = 1;
            return NPC.Distance(FargoSoulsUtil.ClosestPointInHitbox(target, NPC.Center)) < Player.defaultHeight && AIState != ShadowScycle && AIState != Final_ThrowScythes && AIState != ActuallyDead;

        }
        public override void OnSpawn(NPC NPC, IEntitySource source)
        {
            int[] rituals = [ModContent.ProjectileType<AbomRitual>(), ModContent.ProjectileType<AbomRitualMaso>(), ModContent.ProjectileType<AbomRitualFTW>(), ModContent.ProjectileType<AbomRitual2>()];
            Main.dayTime = false;
            Main.time = 0;
            Main.bloodMoon = true;
            Main.eclipse = false;
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                if (Main.projectile[i] != null && Main.projectile[i].active && rituals.Contains(Main.projectile[i].type))
                {
                    Main.projectile[i].Kill();
                }
            }
            if (ModContent.TryFind("Fargowiltas", "Abominationn", out ModNPC modNPC))
            {
                int n = NPC.FindFirstNPC(modNPC.Type);
                if (n != -1 && n != Main.maxNPCs)
                {
                    NPC.Bottom = Main.npc[n].Bottom;
                    TownNPCName = Main.npc[n].GivenName;

                    Main.npc[n].life = 0;
                    Main.npc[n].active = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                }
            }
            NPC.localAI[0] = Main.rand.Next(0, 3);
            NPC.localAI[1] = Main.rand.Next(0, 2);
            NPC.netUpdate = true;
        }
        public override bool SafePreAI(NPC npc)
        {
            ExecutePreAI(npc);
            ExecuteAbomAI(npc);
            return false;
        }
        private void ExecutePreAI(NPC npc)
        {
            EModeGlobalNPC.abomBoss = npc.whoAmI;
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();
            Player player = Main.player[npc.target];
            if (npc.localAI[3] == 0)
            {
                if (npc.timeLeft < 30)
                    npc.timeLeft = 30;
                if (npc.Distance(Main.player[npc.target].Center) < 1500)
                {
                    NextPhase(npc);
                    ChooseAttack(npc);
                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                }
            }
            else if (npc.localAI[3] == 1)
            {
                EModeGlobalNPC.Aura(npc, 2000f, true, -1, default,
                    ModContent.BuffType<GodEaterBuff>());
            }
            if (FargoSoulsUtil.HostCheck)
            {
                if (npc.localAI[3] == 2 && npc.ai[1] >= 120 && FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) == null)
                    ritualProj = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitual>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 0f, npc.whoAmI);

                if (npc.localAI[3] > 0 && FargoSoulsUtil.ProjectileExists(ritualProjMaso, ModContent.ProjectileType<AbomRitualMaso>()) == null)
                    ritualProjMaso = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitualMaso>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 0f, npc.whoAmI);

                if (Main.getGoodWorld && npc.localAI[3] == 2 && npc.ai[1] >= 120 && FargoSoulsUtil.ProjectileExists(ritualProjFTW, ModContent.ProjectileType<AbomRitualFTW>()) == null)
                    ritualProjFTW = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitualFTW>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 0f, npc.whoAmI);

                if (FargoSoulsUtil.ProjectileExists(ringProj, ModContent.ProjectileType<AbomRitual2>()) == null)
                    ringProj = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitual2>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);

                if (FargoSoulsUtil.ProjectileExists(spriteProj, ModContent.ProjectileType<AbomBossProjectile>()) == null)
                {
                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        int number = 0;
                        for (int index = 999; index >= 0; --index)
                        {
                            if (!Main.projectile[index].active)
                            {
                                number = index;
                                break;
                            }
                        }
                        if (number >= 0)
                        {
                            if (Main.netMode == NetmodeID.SinglePlayer)
                            {
                                Projectile projectile = Main.projectile[number];
                                projectile.SetDefaults(ModContent.ProjectileType<AbomBossProjectile>());
                                projectile.Center = npc.Center;
                                projectile.owner = Main.myPlayer;
                                projectile.velocity.X = 0;
                                projectile.velocity.Y = 0;
                                projectile.damage = 0;
                                projectile.knockBack = 0f;
                                projectile.identity = number;
                                projectile.gfxOffY = 0f;
                                projectile.stepSpeed = 1f;
                                projectile.ai[1] = npc.whoAmI;

                                spriteProj = number;
                            }
                        }
                    }
                    else //server
                    {
                        spriteProj = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomBossProjectile>(), 0, 0f, Main.myPlayer, 0, npc.whoAmI);
                    }
                }
            }

            // 添加Buff
            if (Main.LocalPlayer.active && npc.Distance(Main.LocalPlayer.Center) < 3000f)
            {
                if (WorldSavingSystem.EternityMode)
                {
                    Main.LocalPlayer.AddBuff(
                        ModContent.BuffType<AbomPresenceBuff>(), 2);
                }

                if (npc.life == 1 && WorldSavingSystem.MasochistModeReal)
                {
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<TimeStopCDBuff>(), 2);
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<GoldenStasisCDBuff>(), 2);
                }
            }

            // P2阶段音乐和天空
            if (npc.localAI[3] == 2 && npc.ai[1] >= 120)
            {
                int Music = MusicID.OtherworldlyPlantera;
                bool foundMod = ModLoader.TryGetMod("FargowiltasMusic", out Mod musicMod);
                if (foundMod)
                {
                    if (FargoSoulsUtil.AprilFools && musicMod.Version >= Version.Parse("0.1.5.1"))
                        Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Gigachad");
                    else if (musicMod.Version >= Version.Parse("0.1.5"))
                        Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Laevateinn_P2");
                    else
                        Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Stigma");
                }
                if (npc.ModNPC is AbomBoss abomboss)
                {
                    abomboss.Music = Music;
                }

                if (Main.GameModeInfo.IsJourneyMode && CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().Enabled)
                    CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().SetPowerInfo(false);

                if (!SkyManager.Instance["FargosPhantasmMode:AbomSky"].IsActive())
                {
                    SkyManager.Instance.Activate("FargosPhantasmMode:AbomSky");
                }
            }
            if (npc.dontTakeDamage && npc.localAI[3] >= 3)
            {
                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemTopaz, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            HandleCommonLogic(npc, player);
        }
        // 主AI逻辑
        public void ExecuteAbomAI(NPC npc)
        {
            Player player = Main.player[npc.target];
            npc.direction = npc.spriteDirection = npc.Center.X < player.Center.X ? 1 : -1;
            if (!AliveCheck(npc, player))
                return;
            Phase2Check(npc);
            var theMethodShouldBeInvoke = AIState;
            theMethodShouldBeInvoke?.Invoke(npc, player);
            //Main.NewText(AIState.Method);
            //Main.NewText(npc.ai[1]);
            
            if (npc.HasBuff<FrozenBuff>())
            {
                int frozen = npc.FindBuffIndex(ModContent.BuffType<FrozenBuff>());
                npc.DelBuff(frozen);
            }
        }
        #region 分散的ai方法
        internal void ActuallyDead(NPC npc, Player player)
        {
            npc.velocity *= 0.9f;
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemTopaz, 0f, 0f, 0, default, 2.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 12f;
            }
            if (++npc.ai[1] > 180)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    int trollSpeedUp = WorldSavingSystem.MasochistModeReal ? 2 : 1;
                    int max = WorldSavingSystem.MasochistModeReal ? 120 : 30;
                    for (int i = 0; i < max; i++)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                            trollSpeedUp * Vector2.UnitX.RotatedBy(Main.rand.NextDouble() * Math.PI) * Main.rand.NextFloat(30f),
                            ModContent.ProjectileType<AbomDeathScythe>(),
                            FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 10),
                            0f, Main.myPlayer);
                    }

                    if (ModContent.TryFind("Fargowiltas", "Abominationn", out ModNPC modNPC) && !NPC.AnyNPCs(modNPC.Type))
                    {
                        int n = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, modNPC.Type);
                        if (n != Main.maxNPCs)
                        {
                            Main.npc[n].homeless = true;
                            if (TownNPCName != default)
                                Main.npc[n].GivenName = TownNPCName;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                        }
                    }

                    Main.eclipse = false;
                    NetMessage.SendData(MessageID.WorldData);
                }
                npc.life = 0;
                npc.dontTakeDamage = false;
                npc.ai[0] = -4;//符合原版憎恶的checkdead钩子
                npc.checkDead();
                //AIState -= ActuallyDead;
            }
        }
        internal void PhaseChange1st(NPC npc, Player player)
        {
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;
            if (npc.buffType[0] != 0)
                npc.DelBuff(0);

            if (++npc.ai[1] > 120)
            {
                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemTopaz, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 4f;
                }
                Main.bloodMoon = false;
                Main.dayTime = true;
                Main.time = 27000;
                Main.eclipse = true; 
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);

                npc.localAI[3] = 2; //进P2
                int heal = (int)(npc.lifeMax / 90 * Main.rand.NextFloat(1f, 1.5f));
                npc.life += heal;
                if (npc.life > npc.lifeMax)
                    npc.life = npc.lifeMax;
                CombatText.NewText(npc.Hitbox, CombatText.HealLife, heal);
                if (npc.ai[1] > 210)
                {
                    PhaseIndex = 0;
                    npc.ai[0] = 0;
                    ChooseAttack(npc);
                }
            }
            else if (npc.ai[1] == 120)
            {
                FargoSoulsUtil.ClearFriendlyProjectiles(1);
                if (FargoSoulsUtil.HostCheck && FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) == null)
                {
                    ritualProj = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitual>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 0f, npc.whoAmI);
                }
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
            }
        }
        internal void ThrowScythes(NPC npc, Player player)
        {
            npc.dontTakeDamage = false;
            if (npc.localAI[2] == 0) 
            {
                npc.localAI[2] = player.SafeDirectionTo(npc.Center).ToRotation()
                    + MathHelper.ToRadians(WorldSavingSystem.EternityMode ? 90 : 70) * Main.rand.NextFloat(-1, 1);
                npc.netUpdate = true;
            }
            Vector2 targetPos = player.Center + 500 * npc.localAI[2].ToRotationVector2();
            if (npc.Distance(targetPos) > 16)
            {
                npc.position += (player.position - player.oldPosition) / 3;
                float speedModifier = npc.localAI[3] > 0 ? 1f : 2f;
                float maxspeed = npc.localAI[3] == 0 ? 9999 : 24;
                Movement(npc, targetPos, speedModifier, true, maxspeed);
            }
            if (npc.localAI[3] > 0) //in range, fight has begun
            {
                npc.ai[1]++;
                if (npc.ai[3] == 0)
                {
                    npc.ai[3] = 1;
                    if (WorldSavingSystem.MasochistModeReal) //phase 2 saucers
                    {
                        int max = npc.localAI[3] > 1 ? 5 : Main.zenithWorld ? 3 : 2;
                        for (int i = 0; i < max; i++)
                        {
                            float ai2 = i * MathHelper.TwoPi / max; //rotation offset
                            FargoSoulsUtil.NewNPCEasy(npc.GetSource_FromAI(), npc.Center, ModContent.NPCType<AbomSaucer>(), 0, npc.whoAmI, 0, ai2);
                        }
                    }
                }
            }
            if (npc.ai[1] == 120 - AbomStyxGazer.TelegraphTime)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    //float rotation = MathHelper.Pi * 1f * (npc.Center.X < player.Center.X ? 1 : -1);
                    float rotation = MathHelper.Pi * 1f * AbomStyxGazer.Direction;
                    AbomStyxGazer.Direction *= -1;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.DirectionTo(player.Center).RotatedBy(rotation * 0.6f),
                        ModContent.ProjectileType<AbomStyxGazer>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, npc.whoAmI, rotation / 60 * 2);
                }
            }
            if (npc.ai[1] > 120)
            {
                npc.netUpdate = true;
                npc.ai[1] = 70;//这里调节扔镰刀的频率
                npc.localAI[2] = 0;
                if (++npc.ai[2] > 7)
                {
                    ChooseAttack(npc);
                    npc.velocity = npc.SafeDirectionTo(player.Center) * 2f;
                }
                else if (FargoSoulsUtil.HostCheck)
                {
                    float ai0 = npc.Distance(player.Center) / 30 * 2f;
                    float ai1 = npc.localAI[3] > 1 ? 1f : 0f;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.SafeDirectionTo(player.Center) * 30f, ModContent.ProjectileType<AbomScytheSplit>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, ai0, ai1);
                }
            }
        }
        internal void FlamingScytheSpread(NPC npc, Player player)
        {
            npc.velocity = npc.SafeDirectionTo(player.Center);
            npc.velocity *= npc.localAI[3] > 1 ? 2f : 6f;

            int max = npc.localAI[3] > 1 ? 9 : 8;
            if (--npc.ai[1] < 0)
            {
                if (++npc.ai[2] > 4)
                {
                    ChooseAttack(npc);
                }
                else
                {
                    if (npc.localAI[3] > 1) // P2阶段
                    {
                        npc.ai[1] = 60;

                        float baseDelay = 60;
                        float extendedDelay = 90;
                        float speed = 20;
                        float offset = npc.ai[2] % 2 == 0 ? 0 : 0.5f;

                        if (FargoSoulsUtil.HostCheck && npc.HasPlayerTarget)
                        {
                            for (int i = 0; i < max; i++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                    npc.SafeDirectionTo(player.Center).RotatedBy(MathHelper.TwoPi / max * (i + offset)) * speed,
                                    ModContent.ProjectileType<AbomScytheFlaming>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer,
                                    baseDelay, baseDelay + extendedDelay, ai2: npc.target);
                            }
                        }
                    }
                    else // P1阶段
                    {
                        //调整参数
                        npc.ai[1] = 40;
                        float baseDelay = 50f;
                        float extendedDelay = 30f;
                        float speed = 30f;
                        float offset = 0.5f;

                        if (FargoSoulsUtil.HostCheck && npc.HasPlayerTarget)
                        {
                            for (int i = 0; i < max; i++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.SafeDirectionTo(player.Center).RotatedBy(MathHelper.TwoPi / max * (i + offset)) * speed, ModContent.ProjectileType<AbomScytheFlaming>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, baseDelay, baseDelay + extendedDelay, ai2: npc.target);
                            }
                        }
                    }
                    SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                }
            }
        }
        internal void PhoenixDash(NPC npc, Player player)
        {
            npc.velocity *= 0.9f;
            if (WorldSavingSystem.MasochistModeReal && npc.localAI[3] <= 1)
                npc.velocity *= 0.8f;

            int windup = 30;
            if (npc.ai[2] == 0 && npc.localAI[3] <= 1) //first dash waits a bit for scythes to clear in p1
                windup = 60;
            if (WorldSavingSystem.MasochistModeReal && npc.localAI[3] <= 1)
                windup = npc.ai[2] == 0 ? 30 : 10;
            if (npc.ai[2] == 0 && npc.localAI[3] > 1 && WorldSavingSystem.EternityMode) //delay on first entry here
                windup = 240;

            if (npc.ai[2] == 0) //first dash only
            {
                if (npc.localAI[3] > 1) //emode modified tells
                {
                    if (npc.ai[1] == 30 && WorldSavingSystem.EternityMode)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowRingHollow>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 3, npc.whoAmI);
                }

                if (npc.ai[1] == windup - 25)
                {
                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.Souls.IronParry>(), 0, 0f, Main.myPlayer);
                    npc.netUpdate = true;
                }
            }

            if (npc.ai[1] == 5 && npc.ai[2] != 0) //dont do before actually starting dashes
            {
                SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot, npc.Center);

                if (FargoSoulsUtil.HostCheck)
                {
                    for (int i = 0; i < 44; i++)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Main.rand.NextFloat(10f, 30f) * Vector2.Normalize(npc.velocity).RotatedByRandom(MathHelper.ToRadians(40)),
                            ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, ai2: 1);
                    }
                    //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity).RotatedBy(-rotation / 2),
                    //ModContent.ProjectileType<AbomStyxGazerDash>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, rotation / timeleft * 2, timeleft);
                }
            }

            if (++npc.ai[1] > windup)
            {
                npc.netUpdate = true;
                AIState = PhoenixDashWait;
                npc.ai[1] = 0;
                npc.ai[3] = 0;

                if (++npc.ai[2] > 5)
                {
                    ChooseAttack(npc);
                    npc.ai[2] = 0;
                }
                else
                {
                    npc.velocity = npc.SafeDirectionTo(player.Center + player.velocity) * 30f;

                    if (FargoSoulsUtil.HostCheck)
                    {
                        float rotation = MathHelper.Pi * 1.5f;
                        const int timeleft = 40;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity).RotatedBy(-rotation / 2),
                            ModContent.ProjectileType<AbomStyxGazerDash>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, rotation / timeleft * 2, timeleft);
                    }

                    if (npc.localAI[3] > 1)
                    {
                        if (WorldSavingSystem.EternityMode)
                            npc.velocity *= 1.2f;

                        const int ring = 128;
                        for (int index1 = 0; index1 < ring; ++index1)
                        {
                            Vector2 vector2 = (-Vector2.UnitY.RotatedBy(index1 * 3.14159274101257 * 2 / ring) * new Vector2(8f, 16f)).RotatedBy(npc.velocity.ToRotation());
                            int index2 = Dust.NewDust(npc.Center, 0, 0, DustID.GemTopaz, 0.0f, 0.0f, 0, new Color(), 1f);
                            Main.dust[index2].scale = 3f;
                            Main.dust[index2].noGravity = true;
                            Main.dust[index2].position = npc.Center;
                            Main.dust[index2].velocity = Vector2.Zero;
                            //Main.dust[index2].velocity = 5f * Vector2.Normalize(npc.Center - npc.velocity * 3f - Main.dust[index2].position);
                            Main.dust[index2].velocity += vector2 * 1.5f + npc.velocity * 0.5f;
                        }
                    }
                }
            }
        }
        internal void PhoenixDashWait(NPC npc, Player player)
        {
            npc.direction = npc.spriteDirection = Math.Sign(npc.velocity.X);
            if (npc.localAI[3] > 1)
            {
                for (int i = 0; i < 2; i++)
                {
                    int d = Dust.NewDust(npc.Center - npc.velocity * Main.rand.NextFloat(), 0, 0, DustID.GemTopaz, 0f, 0f, 0, new Color());
                    Main.dust[d].scale = 1f + 4f * (1f - npc.ai[1] / 30f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.1f;
                }
            }
            if (++npc.ai[3] > 5)
            {
                npc.ai[3] = 0;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer);
                    if (npc.localAI[3] > 1)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity).RotatedBy(Math.PI / 2), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity).RotatedBy(-Math.PI / 2), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer);
                        if (Main.zenithWorld)
                        {
                            for (float i = -1.5f; i <= 1.5f; i += 3f)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, i * Vector2.Normalize(npc.velocity).RotatedBy(Math.PI / 2), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer);
                            }
                        }
                    }
                }
            }

            if (++npc.ai[1] > 30)
            {
                npc.netUpdate = true;
                AIState = PhoenixDash;
                npc.ai[1] = 0;
                npc.ai[3] = 0;
            }
        }
        internal void SpecialAttackJump1st(NPC npc, Player player)
        {
            SoundEngine.PlaySound(SoundID.Roar, npc.Center);
            //List<AIMethod> specialList = [SicklePhalanx, CirnoIcicle, SaucerRockets];
            //AIState = specialList[Convert.ToInt32(npc.localAI[0])];
            AIState = Convert.ToInt32(npc.localAI[0]) switch
            {
                0 => SicklePhalanx,
                1 => CirnoIcicle,
                _ => SaucerRockets,
            };
            npc.netUpdate = true;
        }
        internal void SpecialAttackJump2nd(NPC npc, Player player)
        {
            int phase = Convert.ToInt32(npc.localAI[0]);
            if (phase == 0)
                AIState = npc.localAI[3] <= 1 ? P1BloodNeedle : P2BloodNeedle;
            else if (phase == 1)
                AIState = ShadowScycle;
            else
                ChooseAttack(npc);
            if (++npc.localAI[0] >= 3)
                npc.localAI[0] = 0;
            npc.netUpdate = true;
        }
        internal void SicklePhalanx(NPC npc, Player player)
        {
            npc.velocity = npc.SafeDirectionTo(player.Center) * 2f;

            if (++npc.ai[1] > (npc.localAI[3] > 1 ? 60 : 90))
            {
                npc.ai[1] = 0;
                if (++npc.ai[2] > (npc.localAI[3] == 1 ? 3 : 6))
                {
                    ChooseAttack(npc);
                }
                else
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        float baseRot = npc.SafeDirectionTo(player.Center).ToRotation();
                        float baseSpeed = npc.Distance(player.Center);
                        if (npc.localAI[3] > 1)
                        {
                            baseRot = Main.rand.NextFloat(0, 360f);
                            baseSpeed = Main.rand.NextFloat(600f, 800f);
                        }

                        baseSpeed /= 90f;

                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 straightSpeed = new Vector2(baseSpeed, 0).RotatedBy(baseRot + Math.PI / 2 * i);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, straightSpeed, ModContent.ProjectileType<AbomSickleSplit1>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI);

                            Vector2 diagonalSpeed = new Vector2(baseSpeed, baseSpeed).RotatedBy(baseRot + Math.PI / 2 * i);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, diagonalSpeed, ModContent.ProjectileType<AbomSickleSplit1>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI);

                        }
                    }
                    SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                    npc.netUpdate = true;
                }
            }
        }
        internal void P1BloodNeedle(NPC npc, Player player)
        {
            int ShootNum = 6;
            int ShootIntervel = 30;
            if (npc.ai[1] < 50)
            {
                if (npc.ai[1] == 0)
                {
                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    npc.netUpdate = true;
                }
                npc.velocity *= 0.9f;
                if (Main.rand.NextBool(3))
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height,
                        DustID.Blood, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.5f;
                }
            }
            else // 阶段2：发射血刺弹幕
            {
                if ((npc.ai[1] - 50) % ShootIntervel == 0 && npc.ai[2] < ShootNum) 
                {
                    Vector2 directionToPlayer = player.Center - npc.Center;
                    float baseAngle = directionToPlayer.ToRotation();
                    if (FargoSoulsUtil.HostCheck)
                    {
                        float spreadAngle = MathHelper.ToRadians(20);
                        float angleStep = MathHelper.ToRadians(4);

                        for (float angleOffset = -spreadAngle; angleOffset <= spreadAngle; angleOffset += angleStep)
                        {
                            // 计算弹幕生成位置（距离abom 1100像素）
                            for (int i = 0; i < 4; i++)
                            {
                                float currentAngle = baseAngle + angleOffset + (float)(i * 1 * Math.PI / 2);
                                Vector2 spawnOffset = Vector2.UnitX.RotatedBy(currentAngle) * 1100f;
                                Vector2 spawnPos = npc.Center + spawnOffset;

                                Vector2 velocity = -spawnOffset.SafeNormalize(Vector2.UnitX) * (0.6f + angleOffset / 100);
                                Projectile.NewProjectile(
                                    npc.GetSource_FromThis(),
                                    spawnPos,
                                    velocity,
                                    ModContent.ProjectileType<Projectiles.Masomode.BloodThornMissile>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage),
                                    0f,
                                    Main.myPlayer
                                );
                            }
                        }
                        SoundEngine.PlaySound(SoundID.Item17, npc.Center);
                        for (int i = 0; i < 10; i++)
                        {
                            int d = Dust.NewDust(npc.position, npc.width, npc.height,
                                DustID.Blood, 0f, 0f, 0, default, 2f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * 3f;
                        }
                    }
                    npc.ai[2]++;
                    npc.netUpdate = true;
                }
                npc.velocity *= 0.95f;
            }
            npc.ai[1]++;
            if (npc.ai[2] >= ShootNum && npc.ai[1] > 50 + ShootIntervel * ShootNum)
            {
                ChooseAttack(npc);

                SoundEngine.PlaySound(SoundID.Item25, npc.Center);
                for (int i = 0; i < 20; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height,
                        DustID.Blood, 0f, 0f, 0, default, 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 3f;
                }
            }
        }
        internal void P2BloodNeedle(NPC npc, Player player)
        {
            int currentPhase = Convert.ToInt32(npc.ai[2]);
            switch (currentPhase)
            {
                case 0://开始
                    {
                        npc.velocity *= 0.9f;
                        if (npc.ai[1] == 0)
                        {
                            SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                            for (int i = 0; i < 20; i++)
                            {
                                int d = Dust.NewDust(npc.position, npc.width, npc.height,
                                    DustID.Blood, 0f, 0f, 0, default, 2f);
                                Main.dust[d].noGravity = true;
                                Main.dust[d].velocity *= 3f;
                            }
                            npc.ai[3] = 0;
                            npc.localAI[2] = 0;
                            // 使用 localAI[0] 临时存储角度c（不会影响攻击顺序）
                            npc.localAI[0] = 0;

                            npc.netUpdate = true;
                        }
                        if (++npc.ai[1] >= 60)
                        {
                            npc.ai[2] = 1; // 进入阶段1
                            npc.ai[1] = 0;

                            // 进入新阶段特效
                            SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                            npc.netUpdate = true;
                        }
                    }
                    break;
                case 1:
                    {
                        npc.velocity *= 0.95f;
                        npc.ai[1]++;
                        if (npc.ai[1] % 4 == 0)
                        {
                            float baseAngleA = npc.ai[3];
                            for (int i = 0; i < 3; i++)
                            {
                                float directionAngle = baseAngleA + i * 120;
                                GenerateBloodThorn(npc, directionAngle, npc.Center, 1100f);
                            }
                            npc.ai[3] += 4;
                            if (npc.ai[3] >= 360)
                                npc.ai[3] -= 360;
                            if (npc.ai[1] % 10 == 0)
                            {
                                SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.1f }, npc.Center);
                            }
                        }
                        if (npc.ai[1] % 40 == 0 && npc.ai[1] > 0)
                        {
                            npc.ai[3] += 50;
                            if (npc.ai[3] >= 360)
                                npc.ai[3] -= 360;
                            for (int i = 0; i < 10; i++)
                            {
                                int d = Dust.NewDust(npc.position, npc.width, npc.height,
                                    DustID.Blood, 0f, 0f, 0, default, 2f);
                                Main.dust[d].noGravity = true;
                                Main.dust[d].velocity *= 2f;
                            }

                            SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                            npc.netUpdate = true;
                        }
                        if (npc.ai[1] >= 180)
                        {
                            npc.ai[2] = 2;
                            npc.ai[1] = 0;
                            npc.netUpdate = true;
                        }
                    }
                    break;
                case 2:
                    {
                        npc.velocity *= 0.95f;
                        npc.ai[1]++;
                        if (npc.ai[1] >= 10)
                        {
                            npc.ai[2] = 3;
                            npc.ai[1] = 0;
                            npc.netUpdate = true;
                        }
                    }
                    break;
                case 3:
                    {
                        npc.velocity *= 0.95f;
                        npc.ai[1]++;
                        if (npc.ai[1] % 3 == 0)
                        {
                            float currentB = npc.localAI[2];
                            for (int i = 0; i < 6; i++)
                            {
                                float directionAngle = currentB + i * 60;
                                GenerateBloodThorn(npc, directionAngle, npc.Center, 1100f);
                            }

                            npc.localAI[2] += 3;
                            if (npc.localAI[2] >= 360)
                                npc.localAI[2] -= 360;

                            if (npc.ai[1] % 10 == 0)
                            {
                                SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.2f }, npc.Center);
                            }
                        }
                        if (npc.ai[1] >= 80)
                        {
                            npc.ai[2] = 4;
                            npc.ai[1] = 0;
                            npc.netUpdate = true;
                        }
                    }
                    break;
                case 4:
                    {
                        npc.velocity *= 0.95f;
                        npc.ai[1]++;
                        if (npc.ai[1] % 3 == 0)
                        {
                            float currentB = npc.localAI[2];

                            for (int i = 0; i < 6; i++)
                            {
                                float directionAngle = currentB + i * 60;
                                GenerateBloodThorn(npc, directionAngle, npc.Center, 1100f);
                            }
                            npc.localAI[2] -= 3;
                            if (npc.localAI[2] < 0)
                                npc.localAI[2] += 360;
                            if (npc.ai[1] % 10 == 0)
                            {
                                SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.2f }, npc.Center);
                            }
                        }
                        if (npc.ai[1] >= 80)
                        {
                            npc.ai[2] = 5;
                            npc.ai[1] = 0;
                            npc.netUpdate = true;
                        }
                    }
                    break;
                case 5:
                    {
                        npc.velocity *= 0.95f;
                        npc.ai[1]++;
                        if (npc.ai[1] % 10 == 0)
                        {
                            float currentC = npc.localAI[0];
                            for (int i = 0; i < 20; i++)
                            {
                                float directionAngle = currentC + i * 18;
                                GenerateTangentialBloodThorns(npc, directionAngle, npc.Center, 1100f, 900 - 0.0486111f * npc.ai[1] * npc.ai[1]);
                            }
                            if (npc.ai[1] % 15 == 0)
                            {
                                SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.3f }, npc.Center);
                            }
                        }
                        if (npc.ai[1] >= 120)
                        {
                            npc.localAI[0] = 1;

                            ChooseAttack(npc);
                            SoundEngine.PlaySound(SoundID.Item25, npc.Center);
                            for (int i = 0; i < 30; i++)
                            {
                                int d = Dust.NewDust(npc.position, npc.width, npc.height,
                                    DustID.Blood, 0f, 0f, 0, default, 2.5f);
                                Main.dust[d].noGravity = true;
                                Main.dust[d].velocity *= 3f;
                            }

                            npc.netUpdate = true;
                        }
                    }
                    break;

            }
        }
        internal void CirnoIcicle(NPC npc, Player player)
        {
            npc.velocity *= 0.9f;
            if (npc.ai[2] == 0)
            {
                npc.ai[2] = 1;
                if (FargoSoulsUtil.HostCheck)
                {
                    for (int i = -3; i <= 3; i++) //make flockos
                    {
                        if (i == 0) //dont shoot one straight up
                            continue;
                        Vector2 overheadSpeed = new(Main.rand.NextFloat(40f), Main.rand.NextFloat(-20f, 20f));
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, overheadSpeed, ModContent.ProjectileType<AbomFlocko>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, 360 / 3 * i);
                    }
                    float offset = 420;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Main.rand.NextVector2CircularEdge(20, 20), ModContent.ProjectileType<AbomFlocko3>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, offset);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Main.rand.NextVector2CircularEdge(20, 20), ModContent.ProjectileType<AbomFlocko3>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, -offset);
                    if (npc.localAI[3] <= 1)
                    {
                        Vector2 speed = new(Main.rand.NextFloat(40f), Main.rand.NextFloat(-20f, 20f));
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed, ModContent.ProjectileType<AbomFlocko2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.target, 0, npc.localAI[3]);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed, ModContent.ProjectileType<AbomFlocko2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.target, 180, npc.localAI[3]);
                    }
                    else
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomFlocko4>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, -140, 1);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomFlocko4>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, -110, -1);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomFlocko4>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, -70, 1);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AbomFlocko4>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, -40, -1);
                    }
                }

                SoundEngine.PlaySound(SoundID.Item27, npc.Center);
                for (int index1 = 0; index1 < 30; ++index1)
                {
                    int index2 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Snow, 0.0f, 0.0f, 0, new Color(), 1f);
                    Main.dust[index2].noGravity = true;
                    Main.dust[index2].noLight = true;
                    Main.dust[index2].velocity *= 5f;
                }
            }
            if (npc.localAI[3] == 2 && npc.ai[1] % (Main.zenithWorld ? 30 : 60) == 0 && npc.ai[1] >= 60)
            {
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        Vector2 desiredPosition = npc.Center + j * Vector2.UnitX * 1100;
                        Vector2 direction = Main.player[npc.target].Center - desiredPosition;
                        direction /= direction.Length();
                        Projectile.NewProjectile(npc.GetSource_FromThis(), desiredPosition, direction.RotatedBy(i * MathHelper.Pi / 10) * 8, ModContent.ProjectileType<AbomFrostWave>(), npc.damage / 4, 0, npc.target);
                    }

                }
            }
            if (++npc.ai[1] > 420)
            {
                ChooseAttack(npc);
            }
        }
        internal void ShadowScycle(NPC npc, Player player)
        {
            if (npc.ai[1] == 0)
            {
                npc.localAI[2] = Main.rand.NextFloat(MathHelper.TwoPi);
                npc.ai[3] = Main.rand.NextFloat(400f, 600f); 
                npc.ai[2] = 0; 
                npc.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
            }
            targetPos = player.Center + npc.localAI[2].ToRotationVector2() * npc.ai[3];

            Movement(npc, targetPos, 0.5f);

            if (++npc.ai[1] >= 270)
            {
                npc.localAI[2] = 0;
                ChooseAttack(npc);
            }
            else
            {
                if (npc.ai[1] % 45 == 0 && npc.ai[1] < 270)
                {
                    SoundEngine.PlaySound(SoundID.Item14, npc.Center);

                    if (FargoSoulsUtil.HostCheck)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            for (int i = 0; i < (npc.localAI[3] > 1 ? 20 : 13); i++) // P2更多火焰弹幕
                            {
                                Vector2 vel = npc.SafeDirectionTo(player.Center).RotatedBy(Math.PI / 6 * (Main.rand.NextDouble() - 0.5) + 2 * Math.PI / 3 * j);
                                float ai0 = Main.rand.NextFloat(1.06f, 1.08f);
                                float ai1 = Main.rand.NextFloat(0.05f);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel,
                                    ModContent.ProjectileType<AbomShadowFlameburst>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 3),
                                    0f, Main.myPlayer, ai0, ai1);
                            }
                        }
                    }
                    npc.ai[2] = npc.ai[1] + 22;
                    npc.netUpdate = true;
                }
                if (FargoSoulsUtil.HostCheck && Math.Abs(npc.ai[1] - npc.ai[2]) < 0.5f && npc.ai[2] > 0)
                {
                    SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                    int scytheCount = 6; 
                    if (npc.localAI[3] > 1) 
                        scytheCount += 2;

                    float scytheSpeed = 1f;

                    for (int i = 0; i < scytheCount; i++)
                    {
                        Vector2 scytheVel = Vector2.UnitX.RotatedBy(MathHelper.TwoPi / scytheCount * i) * scytheSpeed;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, scytheVel,
                            ModContent.ProjectileType<ShadowFlamingScythe>(),
                            FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    }
                    npc.ai[2] = 0;
                    npc.netUpdate = true;
                }
            }
            if (Main.rand.NextBool(5)) 
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Shadowflame, 0f, 0f, 0, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }
        }
        internal void SaucerRockets(NPC npc, Player player)
        {
            npc.velocity *= 0.9f;
            if (npc.ai[1] == 0)
            {
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, -4);
            }
            if (++npc.ai[1] > 420)
            {
                ChooseAttack(npc);
            }
            else if (npc.ai[1] > 60) //spam lasers, lerp aim
            {
                if (npc.localAI[3] > 1) //p2 use a different lerp instead
                {
                    npc.ai[3] = MathHelper.Lerp(npc.ai[3], 1f, 0.1f);
                }
                else //p1 lerps slowly at you
                {
                    float targetRot = npc.SafeDirectionTo(player.Center).ToRotation();
                    while (targetRot < -(float)Math.PI)
                        targetRot += 2f * (float)Math.PI;
                    while (targetRot > (float)Math.PI)
                        targetRot -= 2f * (float)Math.PI;
                    npc.ai[3] = npc.ai[3].AngleLerp(targetRot, 0.04f);
                }

                if (++npc.ai[2] > 1) //spam lasers
                {
                    npc.ai[2] = 0;
                    SoundEngine.PlaySound(SoundID.Item12, npc.Center);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (npc.localAI[3] > 1) //p2 shoots to either side of you
                        {
                            float angleOffset = MathHelper.Lerp(180, 20, npc.ai[3]);

                            for (int i = -3; i <= 3; i += 2)
                            {
                                Vector2 speed = 16f * npc.SafeDirectionTo(player.Center).RotatedBy((Main.rand.NextDouble() - 0.5) * 0.785398185253143 / 3.0);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed.RotatedBy(MathHelper.ToRadians(angleOffset * i)), ModContent.ProjectileType<AbomLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                            }
                        }
                        else //p1 shoots directly
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                Vector2 speed = 16f * npc.ai[3].ToRotationVector2().RotatedBy((Main.rand.NextDouble() - 0.5) * 0.785398185253143 / 2.0);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed, ModContent.ProjectileType<AbomLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed.RotatedBy(Math.PI), ModContent.ProjectileType<AbomLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                            }
                        }
                    }
                }
                if (npc.localAI[3] == 1)
                {
                    if (++npc.localAI[2] > 60)
                    {
                        npc.localAI[2] = 0;
                        for (int i = 0; i < 7; i++)
                        {

                            Vector2 vel = npc.SafeDirectionTo(player.Center);
                            vel *= 6f;
                            float ai2 = npc.localAI[3] > 1 ? 0 : 1;
                            if (FargoSoulsUtil.HostCheck)
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel.RotatedBy(i * 2 * Math.PI / 7), ModContent.ProjectileType<AbomRocket>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.target, 20, ai2);
                        }
                    }
                }
                else
                {
                    if (++npc.localAI[2] % 15 == 0)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 vel = npc.SafeDirectionTo(player.Center);
                            vel *= 7f;
                            float ai2 = npc.localAI[3] > 1 ? 0 : 1;
                            if (FargoSoulsUtil.HostCheck)
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel.RotatedBy(i * 2 * Math.PI / 4), ModContent.ProjectileType<AbomRocket>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.target, 20, ai2);
                        }
                        for (int i = -1; i < 2;)
                        {

                            Vector2 vel = npc.SafeDirectionTo(player.Center);
                            vel *= 50f;
                            float ai2 = npc.localAI[3] > 1 ? 0 : 1;
                            if (FargoSoulsUtil.HostCheck)
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel.RotatedBy(i * 2 * Math.PI / 3), ModContent.ProjectileType<AbomRocket2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.target, 10, ai2);
                            i += 2;
                        }
                        npc.localAI[2] = 0;
                    }
                }
            }
            else
            {
                if (npc.localAI[3] > 1)
                {
                    npc.ai[3] = 0;
                }
                else
                {
                    npc.ai[3] = npc.DirectionFrom(player.Center).ToRotation() - 0.001f;
                    while (npc.ai[3] < -(float)Math.PI)
                        npc.ai[3] += 2f * (float)Math.PI;
                    while (npc.ai[3] > (float)Math.PI)
                        npc.ai[3] -= 2f * (float)Math.PI;
                }
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemTopaz, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
        }
        internal void ChooseStrongAttack(NPC npc, Player player)
        {
            npc.velocity *= 0.9f;
            npc.localAI[2] = 0;
            if (++npc.ai[1] > 90)
            {
                if (npc.localAI[3] > 1) //if in maso p2, do super attacks
                {
                    Initialize(npc);
                    if (npc.localAI[1] == 0)
                    {
                        npc.localAI[1] = 1;
                        AIState = LaevateinnSword;
                    }
                    else
                    {
                        npc.localAI[1] = 0;
                        AIState = ManeuverScycle;
                    }
                }
                else 
                {
                    ChooseAttack(npc);
                }
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                npc.ai[1] = npc.ai[2] = npc.ai[3] = 0;
            }
        }
        //死光雨
        internal void ManeuverScycle(NPC npc, Player player)
        {
            npc.velocity = Vector2.Zero;
            npc.localAI[2] = 0;

            if (npc.ai[1] < 60)
                FancyFireballs(npc, (int)npc.ai[1]);

            if (++npc.ai[1] == 1)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                npc.ai[3] = npc.SafeDirectionTo(player.Center).ToRotation();
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.ai[3].ToRotationVector2(), ModContent.ProjectileType<AbomDeathraySmall>(), 0, 0f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, -npc.ai[3].ToRotationVector2(), ModContent.ProjectileType<AbomDeathraySmall>(), 0, 0f, Main.myPlayer);
                }
            }
            else if (npc.ai[1] == 61)
            {
                const int max = 8;
                const float gap = 1200 / max;
                for (int j = -1; j <= 1; j += 2)
                {
                    Vector2 dustVel = npc.ai[3].ToRotationVector2() * j * 3f;

                    for (int i = 0; i < 20; i++)
                    {
                        int dust = Dust.NewDust(npc.Center, 0, 0, DustID.Smoke, dustVel.X, dustVel.Y, 0, default, 3f);
                        Main.dust[dust].velocity *= 1.4f;
                    }

                    for (int i = 1; i <= max + 2; i++)
                    {
                        float speed = i * j * gap / 30;
                        float ai1 = i % 2 == 0 ? -1 : 1;

                        Vector2 vel = speed * npc.ai[3].ToRotationVector2();

                        for (int k = 0; k < 3; k++)
                        {
                            int d = Dust.NewDust(npc.Center, 0, 0, DustID.PurpleCrystalShard, vel.X, vel.Y, Scale: 3f);
                            Main.dust[d].velocity *= 1.5f;
                            Main.dust[d].noGravity = true;
                        }

                        if (FargoSoulsUtil.HostCheck)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<AbomScytheSpin>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, npc.whoAmI, ai1);


                    }
                }
            }
            if (npc.ai[1] % 20 == 0 && npc.ai[1] > 120 && npc.ai[1] < 450)
            {

                Vector2 direction = Main.player[0].Center - npc.Center;
                direction.Normalize();
                for (int i = 0; i < 3; i++)
                {
                    direction = direction.RotatedBy(Math.PI * 2 / 3);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, direction, ModContent.ProjectileType<AbomLightningTelegraph>(), npc.damage, 0f, Main.myPlayer);
                }

            }
            else if (npc.ai[1] > 61 + 60 + 360 + 30)
            {
                Initialize(npc);
                AIState = PreDeathRain1st;
                npc.localAI[2] = 0;
                npc.netUpdate = true;
            }
        }
        internal void PreDeathRain1st(NPC npc, Player player)
        {
            if (npc.ai[2] == 0 && npc.ai[3] == 0) //target one side of arena
            {
                npc.ai[2] = npc.Center.X + (player.Center.X < npc.Center.X ? -1400 : 1400);
            }

            if (npc.localAI[2] == 0) //direction to dash in next
            {
                npc.localAI[2] = npc.ai[2] > npc.Center.X ? -1 : 1;
            }

            if (npc.ai[1] > 90)
            {
                FancyFireballs(npc, (int)npc.ai[1] - 90);
            }
            else
            {
                npc.ai[3] = player.Center.Y - 300;
            }

            Vector2 targetPos = new Vector2(npc.ai[2], npc.ai[3]);
            Movement(npc, targetPos, 1.4f);

            if (++npc.ai[1] > 150)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                AIState = DeathraysDash1st;
                npc.ai[1] = 0;
                npc.ai[2] = npc.localAI[2];
                npc.ai[3] = 0;
                npc.localAI[2] = 0;
                npc.netUpdate = true;
            }
        }
        internal void DeathraysDash1st(NPC npc, Player player)
        {
            npc.velocity.X = npc.ai[2] * 18f;
            MovementY(npc, player.Center.Y - 250, Math.Abs(player.Center.Y - npc.Center.Y) < 200 ? 2f : 0.7f);
            npc.direction = npc.spriteDirection = Math.Sign(npc.velocity.X);
            if (++npc.ai[3] > 5)
            {
                npc.ai[3] = 0;

                SoundEngine.PlaySound(SoundID.Item12, npc.Center);

                float timeLeft = 2400 / Math.Abs(npc.velocity.X) * 2 - npc.ai[1] + 120;
                if (npc.ai[1] <= 15)
                {
                    timeLeft = 0;
                }
                else
                {
                    if (npc.localAI[2] != 0)
                        timeLeft = 0;
                    if (++npc.localAI[2] > (Main.zenithWorld ? 1 : 2))
                        npc.localAI[2] = 0;
                }

                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 vel1 = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                    Vector2 vel2 = -Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel1, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel2, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                }
            }
            if (++npc.ai[1] > 2400 / Math.Abs(npc.velocity.X))
            {
                npc.netUpdate = true;
                npc.velocity.X = npc.ai[2] * 18f;
                AIState = PreDeathRain2nd;
                npc.ai[1] = 0;
                npc.ai[3] = 0;
            }
        }
        internal void PreDeathRain2nd(NPC npc, Player player)
        {
            npc.velocity.Y = 0f;
            npc.velocity *= 0.947f;
            npc.ai[3] += npc.velocity.Length();

            if (npc.ai[1] > 150)
                FancyFireballs(npc, (int)npc.ai[1] - 150);

            if (++npc.ai[1] > 210)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                npc.netUpdate = true;
                AIState = DeathraysDash2nd;
                npc.ai[1] = 0;
                npc.ai[3] = 0;
            }
        }
        internal void DeathraysDash2nd(NPC npc, Player player)
        {
            npc.velocity.X = npc.ai[2] * -18f;
            MovementY(npc, player.Center.Y - 250, Math.Abs(player.Center.Y - npc.Center.Y) < 200 ? 2f : 0.7f);
            npc.direction = npc.spriteDirection = Math.Sign(npc.velocity.X);
            if (++npc.ai[3] > 5)
            {
                npc.ai[3] = 0;

                SoundEngine.PlaySound(SoundID.Item12, npc.Center);

                float timeLeft = 2400 / Math.Abs(npc.velocity.X) * 2 - npc.ai[1] + 120;
                if (npc.ai[1] <= 15)
                {
                    timeLeft = 0;
                }
                else
                {
                    if (npc.localAI[2] != 0)
                        timeLeft = 0;
                    if (++npc.localAI[2] > (Main.zenithWorld ? 1 : 2))
                        npc.localAI[2] = 0;
                }

                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 vel1 = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                    Vector2 vel2 = -Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel1, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel2, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                }
            }
            if (++npc.ai[1] > 2400 / Math.Abs(npc.velocity.X))
            {
                npc.velocity.X = npc.ai[2] * -18f;
                AIState = PauseToPre;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        internal void PauseToPre(NPC npc, Player player)
        {
            npc.velocity *= 0.9f;
            if (++npc.ai[1] > 60)
            {
                ChooseAttack(npc);
            }
        }
        //莱瓦汀
        internal void LaevateinnSword(NPC npc, Player player)
        {
            npc.velocity *= 0.9f;
            // 前60帧显示火焰特效（第一轮）或快速火焰特效（第二轮）
            if (npc.ai[1] < 60)
                FancyFireballs(npc, (int)npc.ai[1]);

            if (npc.ai[1] == 0 && npc.ai[2] != 2 && FargoSoulsUtil.HostCheck)
            {
                float ai1 = npc.ai[2] == 1 ? -1 : 1;
                if (npc.ai[2] == 0) // 第一轮循环预警
                {
                    ai1 *= MathHelper.ToRadians(270) / 120 * -1 * 60;
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                        ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage),
                        0f, Main.myPlayer, 3, ai1);
                    if (p != Main.maxProjectiles)
                    {
                        Main.projectile[p].localAI[1] = npc.whoAmI;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncProjectile, number: p);
                    }
                }
                else // 第二轮循环快速预警
                {
                    ai1 *= MathHelper.ToRadians(270) / 120 * 1 * 105;//由于角度超过180，所以弃之符号进行尝试
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                        ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage),
                        0f, Main.myPlayer, 3, ai1);
                    if (p != Main.maxProjectiles)
                    {
                        Main.projectile[p].localAI[1] = npc.whoAmI;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncProjectile, number: p);
                    }
                    // 生成红色预警环
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                        Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowRing>(),
                        0, 0f, Main.myPlayer, npc.whoAmI, -3);
                }
            }

            npc.ai[1]++;
            if (npc.ai[2] == 0) // 第一轮循环：生成AbomSword2
            {
                if (npc.ai[1] > 90)
                {
                    npc.netUpdate = true;
                    AIState = LaevateinnDash;
                    npc.ai[1] = 0;
                    npc.velocity = npc.SafeDirectionTo(player.Center) * 3f;
                }
                else if (npc.ai[1] == 60 && FargoSoulsUtil.HostCheck)
                {
                    npc.netUpdate = true;
                    npc.velocity = Vector2.Zero;

                    //SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    float ai0 = npc.ai[2] == 1 ? -1 : 1;

                    ai0 *= MathHelper.ToRadians(270) / 120;
                    Vector2 vel = npc.SafeDirectionTo(player.Center).RotatedBy(-ai0 * 60);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel,
                        ModContent.ProjectileType<AbomSword2>(),
                        FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8),
                        0f, Main.myPlayer, ai0, npc.whoAmI, ai2: 1);

                }
            }
            else // 第二轮循环：生成快速旋转的AbomSword3
            {
                float ai0 = npc.ai[2] == 1 ? -1 : 1;
                ai0 *= MathHelper.ToRadians(270) / 120; // 更快的旋转速度
                Vector2 vel = npc.SafeDirectionTo(player.Center).RotatedBy(-ai0 * 60);
                if (npc.ai[1] > 90)
                {
                    npc.netUpdate = true;
                    AIState = LaevateinnDash;
                    npc.ai[1] = 0;
                    npc.velocity = npc.SafeDirectionTo(player.Center) * 20f;
                }
                else if (npc.ai[1] == 60)
                {
                    ai0 = npc.ai[2] == 1 ? -1 : 1;

                    ai0 *= MathHelper.ToRadians(270) / 120; // 更快的旋转速度
                    vel = npc.SafeDirectionTo(player.Center).RotatedBy(-ai0 * 60);
                }
                else if (npc.ai[1] == 90 && FargoSoulsUtil.HostCheck)
                {
                    npc.netUpdate = true;
                    npc.velocity = Vector2.Zero;

                    //SoundEngine.PlaySound(SoundID.Roar, npc.Center);

                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel,
                        ModContent.ProjectileType<AbomSword3>(),
                        FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8),
                        0f, Main.myPlayer, 3 * ai0, npc.whoAmI); // 旋转速度×3

                    // 快速旋转的特殊音效和效果
                    SoundEngine.PlaySound(FargosSoundRegistry.StyxGazer with { Volume = 2.0f, Pitch = -0.3f }, npc.Center);
                    for (int i = 0; i < 20; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height,
                            DustID.GemTopaz, 0f, 0f, 0, default, 3f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 6f;
                    }
                }

            }
        }
        internal void LaevateinnDash(NPC npc, Player player)
        {
            int waittime = npc.ai[2] == 1 ? 30 : 120;
            npc.direction = npc.spriteDirection = Math.Sign(npc.velocity.X);
            if (++npc.ai[1] > waittime)
            {
                npc.netUpdate = true;
                AIState = WaitScythesClear;
                npc.ai[1] = 0;
            }
        }
        internal void WaitScythesClear(NPC npc, Player player)
        {
            Vector2 targetPos = player.Center + player.SafeDirectionTo(npc.Center) * 500;
            if (npc.Distance(targetPos) > 50)
                Movement(npc, targetPos, 0.7f);
            if (++npc.ai[1] > 60)
            {
                npc.netUpdate = true;
                if (++npc.ai[2] < 2)
                {
                    AIState = LaevateinnSword;
                }
                else
                {
                    AIState = PreVerticalDive;
                    npc.ai[2] = 0;
                }
                npc.ai[1] = 0;
            }
        }
        internal void PreVerticalDive(NPC npc, Player player)
        {
            if (npc.ai[2] == 0 && npc.ai[3] == 0) //target one side of arena
            {
                npc.netUpdate = true;
                npc.ai[2] = player.Center.X;
                npc.ai[3] = player.Center.Y;
                if (FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) != null)
                {
                    npc.ai[2] = Main.projectile[ritualProj].Center.X;
                    npc.ai[3] = Main.projectile[ritualProj].Center.Y;
                }

                Vector2 offset;
                offset.X = Math.Sign(player.Center.X - npc.ai[2]);
                offset.Y = Math.Sign(player.Center.Y - npc.ai[3]);
                npc.localAI[2] = offset.ToRotation();
            }

            Vector2 actualTargetPositionOffset = (float)Math.Sqrt(2 * 1200 * 1200) * npc.localAI[2].ToRotationVector2();
            actualTargetPositionOffset.Y -= 450 * Math.Sign(actualTargetPositionOffset.Y);

            Vector2 targetPos = new Vector2(npc.ai[2], npc.ai[3]) + actualTargetPositionOffset;
            Movement(npc, targetPos, 1f);

            if (npc.ai[1] == 0 && FargoSoulsUtil.HostCheck)
            {
                float horizontalModifier = Math.Sign(npc.ai[2] - targetPos.X);
                float verticalModifier = Math.Sign(npc.ai[3] - targetPos.Y);

                float startRotation = horizontalModifier > 0 ? MathHelper.ToRadians(0.1f) * -verticalModifier : MathHelper.Pi - MathHelper.ToRadians(0.1f) * -verticalModifier;
                float ai1 = horizontalModifier > 0 ? MathHelper.Pi : 0;
                int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, startRotation.ToRotationVector2(), ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 4, ai1);
                if (p != Main.maxProjectiles)
                {
                    Main.projectile[p].localAI[1] = npc.whoAmI;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncProjectile, number: p);
                }
            }

            if (npc.ai[1] > 90)
                FancyFireballs(npc, (int)npc.ai[1] - 90);

            if (++npc.ai[1] > 150)
            {
                npc.netUpdate = true;
                npc.velocity = Vector2.Zero;
                AIState = VerticalLaevateinn;
                npc.ai[1] = 0;
            }
        }
        internal void VerticalLaevateinn(NPC npc, Player player)
        {
            npc.direction = npc.spriteDirection = Math.Sign(npc.ai[2] - npc.Center.X);
            int SpinTime = 60;
            if (npc.ai[1] == 0)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    float horizontalModifier = Math.Sign(npc.ai[2] - npc.Center.X);
                    float verticalModifier = Math.Sign(npc.ai[3] - npc.Center.Y);

                    float ai0 = horizontalModifier * MathHelper.Pi / SpinTime * verticalModifier;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX * -horizontalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, npc.whoAmI);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, -Vector2.UnitX * -horizontalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, npc.whoAmI);
                }
            }
            if (npc.ai[1] == SpinTime)
            {
                npc.netUpdate = true;
                npc.velocity.X = 0f;
                npc.velocity.Y = 24 * Math.Sign(npc.ai[3] - npc.Center.Y);
            }
            if (npc.ai[1].IsInRange(SpinTime, SpinTime + 90))
            {
                npc.velocity.Y *= 0.97f;
                npc.position += npc.velocity;
                npc.direction = npc.spriteDirection = Math.Sign(npc.ai[2] - npc.Center.X);
            }
            if (++npc.ai[1] > SpinTime + 90)
            {
                npc.netUpdate = true;
                AIState = WaitScythesClear2nd;
                npc.ai[1] = 0;
            }
        }
        internal void WaitScythesClear2nd(NPC npc, Player player)
        {
            npc.localAI[2] = 0;
            targetPos = player.Center;
            targetPos.X += 500 * (npc.Center.X < targetPos.X ? -1 : 1);
            if (npc.Distance(targetPos) > 50)
                Movement(npc, targetPos, 0.7f);
            if (++npc.ai[1] > 60)
            {
                ChooseAttack(npc);
            }
        }
        //P3
        internal void PhaseChange2nd(NPC npc, Player player)
        {
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemTopaz, 0f, 0f, 0, default, 2.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 12f;
            }
            if (++npc.ai[1] > 180)
            {
                ChooseAttack(npc);
            }
        }
        internal void Final_ThrowScythes(NPC npc, Player player)
        {
            npc.dontTakeDamage = true;
            Vector2 TargetPos = player.Center - new Vector2(0, 550);
            if (npc.Distance(TargetPos) > 50)
                Movement(npc, TargetPos, 0.8f);
            else
                npc.velocity *= 0.95f; 
            npc.ai[1]++;

            int throwCount = Convert.ToInt32(npc.ai[2]);
            int gazeSpawnTime = Convert.ToInt32(npc.ai[3]);
            int interval = 20;
            if (npc.ai[1].IsInRange(240, 360))
                interval = 10;
            else if (npc.ai[1].IsInRange(360, 480))
                interval = 5;

            if (npc.ai[1] == 60)
            {
                FargoSoulsUtil.NewNPCEasy(npc.GetSource_FromThis(), npc.Center, ModContent.NPCType<AbomSaucer2>(), 0, npc.whoAmI, 0, 0);
            }
            if (npc.ai[1] >= 60 && npc.ai[1] <= 480)
            {
                if ((npc.ai[1] - 60) % interval == 0)
                {
                    float ai0 = npc.Distance(player.Center) / 30 * 2f;
                    float ai1 = 0f;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.SafeDirectionTo(player.Center) * 30f,
                            ModContent.ProjectileType<AbomScytheSplit>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, ai0, ai1);
                    }
                    SoundEngine.PlaySound(SoundID.Item71, npc.Center);
                    throwCount++;
                    npc.ai[2] = throwCount;
                    if (npc.ai[1] > gazeSpawnTime + interval)
                    {
                        float rotation = MathHelper.Pi * 1f * AbomStyxGazer.Direction;
                        AbomStyxGazer.Direction *= -1; // 切换方向
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.DirectionTo(player.Center).RotatedBy(rotation * 0.6f),
                                ModContent.ProjectileType<AbomStyxGazer>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, rotation / 60 * 2);
                        }
                        gazeSpawnTime = (int)npc.ai[1];
                        npc.ai[3] = gazeSpawnTime;
                    }
                }
            }
            if (npc.ai[1] > 480)
            {
                ChooseAttack(npc);
                npc.localAI[2] = 0;
            }
        }
        internal void Final_LaevateinnSword(NPC npc, Player player)
        {
            if (npc.ai[1] < 60)
                FancyFireballs(npc, (int)npc.ai[1]);

            if (npc.ai[1] == 0 && npc.localAI[2] != 2 && FargoSoulsUtil.HostCheck)
            {
                float ai1 = npc.localAI[2] == 1 ? -1 : 1;
                ai1 *= MathHelper.ToRadians(270) / 120 * -1 * 60;
                int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), 0, 0f, Main.myPlayer, 3, ai1);
                if (p != Main.maxProjectiles)
                {
                    Main.projectile[p].localAI[1] = npc.whoAmI;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncProjectile, number: p);
                }
            }
            if (npc.ai[1] == 90)
            {
                npc.netUpdate = true;
                npc.velocity = npc.SafeDirectionTo(player.Center) * 3f;
            }
            else if (npc.ai[1] == 60)
            {
                npc.netUpdate = true;
                npc.velocity = Vector2.Zero;
                float ai0 = npc.localAI[2] == 1 ? -1 : 1;
                ai0 *= MathHelper.ToRadians(300) / 120;//角速度
                Vector2 vel = npc.SafeDirectionTo(player.Center).RotatedBy(-2.35619449f * Math.Sign(ai0));//135°初始角度
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<AbomSword2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0 * 2 / 3, npc.whoAmI, ai2: 1);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, -vel, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, npc.whoAmI, ai2: 1);
                }
            }
            if (npc.ai[1] < 90)
                npc.velocity *= 0.9f;
            else if (npc.ai[1] < 90 + 120)
            {
                npc.direction = npc.spriteDirection = Math.Sign(npc.velocity.X);
            }
            else if (npc.ai[1] < 210 + 30)  
            {
                Vector2 targetPos = player.Center + player.SafeDirectionTo(npc.Center) * 300;
                if (npc.Distance(targetPos) > 50)
                    Movement(npc, targetPos, 0.7f);
                //npc.velocity.X *= 0.97f;
                //npc.position += npc.velocity;
            }
            if (++npc.ai[1] > 210 + 30)
            {
                ChooseAttack(npc);
                npc.localAI[2] = npc.localAI[2] == 0 ? 1 : 0;
            }
        }    
        internal void SaucerWindmill(NPC npc, Player player)
        {
            Vector2 targetPos = player.Center + player.SafeDirectionTo(npc.Center) * 300;
            if (npc.Distance(targetPos) > 50)
                Movement(npc, targetPos, 0.7f);
            if (npc.ai[1] == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    FargoSoulsUtil.NewNPCEasy(npc.GetSource_FromThis(), npc.Center, ModContent.NPCType<AbomSaucer2>(), 0, npc.whoAmI, 0, i * 2 * MathHelper.Pi / 2, 0);
                }
            }
            if (++npc.ai[1] > 360)
            {
                ChooseAttack(npc);
            }
        }
        internal void Final_PreHorizontalLaevateinn(NPC npc, Player player)
        {
            if (npc.ai[2] == 0 && npc.ai[3] == 0) //target one side of arena
            {
                npc.netUpdate = true;
                npc.ai[2] = player.Center.X;
                npc.ai[3] = player.Center.Y;
                if (FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) != null)
                {
                    npc.ai[2] = Main.projectile[ritualProj].Center.X;
                    npc.ai[3] = Main.projectile[ritualProj].Center.Y;
                }

                Vector2 offset;
                offset.X = Math.Sign(player.Center.X - npc.ai[2]);
                offset.Y = Math.Sign(player.Center.Y - npc.ai[3]);
                npc.localAI[2] = offset.ToRotation();
            }

            Vector2 actualTargetPositionOffset = (float)Math.Sqrt(2 * 1200 * 1200) * npc.localAI[2].ToRotationVector2();
            actualTargetPositionOffset.X -= 450 * Math.Sign(actualTargetPositionOffset.X);

            targetPos = new Vector2(npc.ai[2], npc.ai[3]) + actualTargetPositionOffset;
            Movement(npc, targetPos, 1f);

            if (npc.ai[1] == 0 && FargoSoulsUtil.HostCheck)
            {
                float horizontalModifier = Math.Sign(npc.ai[2] - targetPos.X);
                float verticalModifier = Math.Sign(npc.ai[3] - targetPos.Y);

                float startRotation = verticalModifier > 0 ? MathHelper.ToRadians(0.1f) * -verticalModifier : MathHelper.Pi - MathHelper.ToRadians(0.1f) * -verticalModifier;
                float ai1 = verticalModifier < 0 ? MathHelper.Pi / 2 : MathHelper.Pi * 3 / 2;
                int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, startRotation.ToRotationVector2(), ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 4, ai1);
                if (p != Main.maxProjectiles)
                {
                    Main.projectile[p].localAI[1] = npc.whoAmI;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncProjectile, number: p);
                }
            }

            if (npc.ai[1] > 90)
                FancyFireballs(npc, (int)npc.ai[1] - 90);

            if (++npc.ai[1] > 150)
            {
                npc.velocity = Vector2.Zero;
                ChooseAttack(npc, false);
                npc.ai[1] = 0;
            }
        }
        internal void Final_HorizontalLaevateinn(NPC npc, Player player)
        {
            int SpinTime = 60;
            if (npc.ai[1] == 0)
            {
                npc.velocity = Vector2.Zero;
                if (FargoSoulsUtil.HostCheck)
                {
                    float horizontalModifier = Math.Sign(npc.ai[2] - npc.Center.X);
                    float verticalModifier = Math.Sign(npc.ai[3] - npc.Center.Y);

                    float ai0 = -1f * horizontalModifier * MathHelper.Pi / SpinTime * verticalModifier;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitY * -verticalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, npc.whoAmI);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, -Vector2.UnitY * -verticalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, npc.whoAmI);
                }
            }
            npc.direction = npc.spriteDirection = Math.Sign(npc.ai[2] - npc.Center.X);
            if (npc.ai[1] < SpinTime + 90)
            {
                if (npc.ai[1] == SpinTime)
                {
                    npc.velocity.X = 24 * Math.Sign(npc.ai[2] - npc.Center.X);//向仪式圈中心;
                    npc.velocity.Y = 0f;
                }
                npc.velocity.X *= 0.97f;
                npc.position += npc.velocity;
            }
            if (++npc.ai[1] > SpinTime + 90)
            {
                ChooseAttack(npc, false);
                npc.ai[1] = 0;
                npc.velocity.X = 0;
            }
        }
        internal void Final_TabooLaevateinn(NPC npc, Player player)
        {
            npc.direction = npc.spriteDirection = Math.Sign(npc.ai[2] - npc.Center.X);
            int SpinTime = 150;
            if (npc.ai[1] == 0)
            {
                float horizontalModifier = Math.Sign(npc.ai[2] - npc.Center.X);
                float verticalModifier = Math.Sign(npc.ai[3] - npc.Center.Y);
                npc.velocity.X = horizontalModifier * 2f;
                npc.velocity.Y = verticalModifier * 2f;
                if (FargoSoulsUtil.HostCheck)
                {
                    float ai0 = -horizontalModifier * MathHelper.Pi / 50 * verticalModifier;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitY * -verticalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, npc.whoAmI);
                }
            }
            if (npc.ai[1] > SpinTime)
            {
                Vector2 targetPos = player.Center;
                targetPos.X += 500 * (npc.Center.X < targetPos.X ? -1 : 1);
                if (npc.Distance(targetPos) > 50)
                    Movement(npc, targetPos, 0.7f);
            }
            if (++npc.ai[1] > SpinTime + 60)
            {
                ChooseAttack(npc);
            }
        }
        #endregion

        #region 辅助方法
        private void ChooseAttack(NPC npc, bool initialize = true) => IDelegateStateMachine.ChooseAttack(npc, this, initialize);
        private void NextPhase(NPC npc) => IDelegateStateMachine.NextPhase(npc, this);
        private static void FancyFireballs(NPC npc, int repeats)
        {
            float modifier = 0;
            for (int i = 0; i < repeats; i++)
                modifier = MathHelper.Lerp(modifier, 1f, 0.08f);

            float distance = 1400 * (1f - modifier);
            float rotation = MathHelper.TwoPi * modifier;
            const int max = 4;
            for (int i = 0; i < max; i++)
            {
                int d = Dust.NewDust(npc.Center + distance * Vector2.UnitX.RotatedBy(rotation + MathHelper.TwoPi / max * i), 0, 0, DustID.PurpleCrystalShard, npc.velocity.X * 0.3f, npc.velocity.Y * 0.3f, newColor: Color.White);
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 6f - 4f * modifier;
            }
        }
        private static void GenerateBloodThorn(NPC NPC, float directionAngle, Vector2 Center, float radius)
        {
            // 将角度转换为弧度
            float angleRad = MathHelper.ToRadians(directionAngle);
            NPC abomBoss = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>())
                {
                    abomBoss = Main.npc[i];
                    break;
                }
            }
            Vector2 bossCenter = abomBoss.Center;

            // 计算生成位置（在半径为radius的圆上）
            Vector2 spawnPos = bossCenter + new Vector2((float)Math.Cos(angleRad), (float)Math.Sin(angleRad)) * radius;

            // 计算速度方向（指向boss）
            Vector2 velocity = Vector2.Normalize(Center - spawnPos) * 0.8f; // 初始速度0.4

            // 生成血弹
            if (FargoSoulsUtil.HostCheck)
            {
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<Projectiles.Masomode.BloodThornMissile>(),
                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                    0f,
                    Main.myPlayer
                );
            }
        }
        private static void GenerateTangentialBloodThorns(NPC NPC, float directionAngle, Vector2 bossCenter, float spawnRadius, float tangentRadius)
        {
            // 将角度转换为弧度
            float angleRad = MathHelper.ToRadians(directionAngle);

            // 计算生成位置（在半径为spawnRadius的圆上）
            Vector2 spawnPos = bossCenter + new Vector2((float)Math.Cos(angleRad), (float)Math.Sin(angleRad)) * spawnRadius;

            // 计算切线方向（两个相反方向）
            // 切线方向与半径垂直
            Vector2 radialDir = Vector2.Normalize(spawnPos - bossCenter);
            Vector2 tangent1 = new Vector2(-radialDir.Y, radialDir.X); // 顺时针切线
            Vector2 tangent2 = new Vector2(radialDir.Y, -radialDir.X); // 逆时针切线

            // 调整速度使弹幕最终指向boss
            // 切线方向需要稍微调整以指向boss
            Vector2 velocity1 = Vector2.Normalize(bossCenter - (spawnPos + tangent1 * tangentRadius)) * 0.4f;
            Vector2 velocity2 = Vector2.Normalize(bossCenter - (spawnPos + tangent2 * tangentRadius)) * 0.4f;

            // 生成两个血弹
            if (FargoSoulsUtil.HostCheck)
            {
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    spawnPos,
                    velocity1,
                    ModContent.ProjectileType<Projectiles.Masomode.BloodThornMissile>(),
                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                    0f,
                    Main.myPlayer
                );

                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    spawnPos,
                    velocity2,
                    ModContent.ProjectileType<Projectiles.Masomode.BloodThornMissile>(),
                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                    0f,
                    Main.myPlayer
                );
            }
        }

        private bool AliveCheck(NPC npc, Player player)
        {
            if ((!player.active || player.dead || Vector2.Distance(npc.Center, player.Center) > 5000f) && npc.localAI[3] > 0)
            {
                npc.TargetClosest();
                player = Main.player[npc.target];
                if (!player.active || player.dead || Vector2.Distance(npc.Center, player.Center) > 5000f)
                {
                    if (npc.timeLeft > 30)
                        npc.timeLeft = 30;
                    npc.velocity.Y -= 1f;
                    if (npc.timeLeft == 1)
                    {
                        if (npc.position.Y < 0)
                            npc.position.Y = 0;
                        if (FargoSoulsUtil.HostCheck)
                        {
                            FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);

                            // 恢复原版NPC
                            if (ModLoader.TryGetMod("Fargowiltas", out Mod fargoMod))
                            {
                                if (fargoMod.TryFind("Abominationn", out ModNPC abomNPC) && !NPC.AnyNPCs(abomNPC.Type))
                                {
                                    int n = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, abomNPC.Type);
                                    if (n != Main.maxNPCs)
                                    {
                                        Main.npc[n].homeless = true;
                                        if (TownNPCName != default)
                                            Main.npc[n].GivenName = TownNPCName;
                                        if (Main.netMode == NetmodeID.Server)
                                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                                    }
                                }
                            }

                            Main.eclipse = false;
                            NetMessage.SendData(MessageID.WorldData);
                        }
                    }
                    return false;
                }
            }

            if (npc.timeLeft < 600)
                npc.timeLeft = 600;
            /*
            if (player.Center.Y / 16f > Main.worldSurface)
            {
                npc.velocity.X *= 0.95f;
                npc.velocity.Y -= 1f;
                if (npc.velocity.Y < -32f)
                    npc.velocity.Y = -32f;
                return false;
            }
            */
            return true;
        }
        private bool Phase2Check(NPC npc)
        {
            if (npc.life < npc.lifeMax * 0.66f && npc.localAI[3] < 2)
            {
                AIState = PhaseChange1st;
                npc.localAI[3] = 2;
                Initialize(npc);
                npc.netUpdate = true;
                FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);
                return true;
            }
            return false;
        }
        private static void Movement(NPC npc, Vector2 targetPos, float speedModifier, bool fastX = true, float maxSpeed = 24)
        {
            if (Math.Abs(npc.Center.X - targetPos.X) > 5)
            {
                if (npc.Center.X < targetPos.X)
                {
                    npc.velocity.X += speedModifier;
                    if (npc.velocity.X < 0)
                        npc.velocity.X += speedModifier * (fastX ? 2 : 1);
                }
                else
                {
                    npc.velocity.X -= speedModifier;
                    if (npc.velocity.X > 0)
                        npc.velocity.X -= speedModifier * (fastX ? 2 : 1);
                }
            }
            if (npc.Center.Y < targetPos.Y)
            {
                npc.velocity.Y += speedModifier;
                if (npc.velocity.Y < 0)
                    npc.velocity.Y += speedModifier * 2;
            }
            else
            {
                npc.velocity.Y -= speedModifier;
                if (npc.velocity.Y > 0)
                    npc.velocity.Y -= speedModifier * 2;
            }
            if (Math.Abs(npc.velocity.X) > maxSpeed)
                npc.velocity.X = maxSpeed * Math.Sign(npc.velocity.X);
            if (Math.Abs(npc.velocity.Y) > maxSpeed)
                npc.velocity.Y = maxSpeed * Math.Sign(npc.velocity.Y);
        }
        private static void MovementY(NPC NPC, float targetY, float speedModifier)
        {
            if (NPC.Center.Y < targetY)
            {
                NPC.velocity.Y += speedModifier;
                if (NPC.velocity.Y < 0)
                    NPC.velocity.Y += speedModifier * 2;
            }
            else
            {
                NPC.velocity.Y -= speedModifier;
                if (NPC.velocity.Y > 0)
                    NPC.velocity.Y -= speedModifier * 2;
            }
            if (Math.Abs(NPC.velocity.Y) > 24)
                NPC.velocity.Y = 24 * Math.Sign(NPC.velocity.Y);
        }
        private void HandleCommonLogic(NPC npc, Player player)
        {
            if (npc.HasBuff<FrozenBuff>())
            {
                int frozen = npc.FindBuffIndex(ModContent.BuffType<FrozenBuff>());
                npc.DelBuff(frozen);
            }
            if ((player.immune || player.hurtCooldowns[0] != 0 || player.hurtCooldowns[1] != 0) && npc.ModNPC is AbomBoss abomboss)
                abomboss.playerInvulTriggered = true;
            if (WorldSavingSystem.EternityMode && NPC.downedMoonlord &&
                !WorldSavingSystem.DownedAbom && FargoSoulsUtil.HostCheck &&
                npc.HasPlayerTarget && !droppedSummon)
            {
                Item.NewItem(npc.GetSource_Loot(), player.Hitbox,
                    ModContent.ItemType<AbomsCurse>());
                droppedSummon = true;
            }
        }
        #endregion
        #region 重写方法
        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            for (int i = 0; i < 3; i++)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemTopaz, 0f, 0f, 0, default, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 3f;
            }
        }
        public override bool CheckDead(NPC npc)
        {
            if (AIState == ActuallyDead)
                return true;

            npc.life = 1;
            npc.active = true;
            if (npc.localAI[3] < 3)
            {
                npc.localAI[3] = 3;
                AIState = PhaseChange2nd;
                npc.ai[0] = 0;
                Initialize(npc);
                npc.localAI[2] = 0;
                npc.dontTakeDamage = true;
                npc.netUpdate = true;
                FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);
            }
            return false;
        }
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);
            binaryWriter.Write7BitEncodedInt(PhaseIndex);
            binaryWriter.WriteVector2(targetPos);
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);
            PhaseIndex = binaryReader.Read7BitEncodedInt();
            targetPos = binaryReader.ReadVector2();
        }
        #endregion
        #region 贺贺
        /*NPC NPC = npc;
        switch ((int)npc.ai[0])
        {
            case -4: //ACTUALLY dead
                npc.velocity *= 0.9f;
                npc.dontTakeDamage = true;
                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemTopaz, 0f, 0f, 0, default, 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 12f;
                }
                if (++npc.ai[1] > 180)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int trollSpeedUp = WorldSavingSystem.MasochistModeReal ? 2 : 1;
                        int max = WorldSavingSystem.MasochistModeReal ? 120 : 30;
                        for (int i = 0; i < max; i++)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                                trollSpeedUp * Vector2.UnitX.RotatedBy(Main.rand.NextDouble() * Math.PI) * Main.rand.NextFloat(30f),
                                ModContent.ProjectileType<AbomDeathScythe>(),
                                FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 10),
                                0f, Main.myPlayer);
                        }

                        if (ModContent.TryFind("Fargowiltas", "Abominationn", out ModNPC modNPC) && !NPC.AnyNPCs(modNPC.Type))
                        {
                            int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, modNPC.Type);
                            if (n != Main.maxNPCs)
                            {
                                Main.npc[n].homeless = true;
                                if (TownNPCName != default)
                                    Main.npc[n].GivenName = TownNPCName;
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                            }
                        }

                        Main.eclipse = false;
                        NetMessage.SendData(MessageID.WorldData);
                    }
                    NPC.life = 0;
                    NPC.dontTakeDamage = false;
                    NPC.checkDead();
                }
                break;

            case -3: //pause to let arena recenter, then proceed
                if (!AliveCheck(npc, player))
                    break;
                NPC.velocity *= 0.9f;
                NPC.dontTakeDamage = true;
                if (++NPC.ai[1] > 120)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] = 15;
                    NPC.ai[1] = 0;
                }
                break;

            case -2: //dead, begin last stand
                if (!AliveCheck(npc, player))
                    break;
                NPC.velocity *= 0.9f;
                NPC.dontTakeDamage = true;
                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 12f;
                }
                if (++NPC.ai[1] > 180)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] = 24;
                    NPC.ai[1] = 0;
                }
                break;

            case -1: //phase 2 transition
                NPC.velocity *= 0.9f;
                NPC.dontTakeDamage = true;
                if (NPC.buffType[0] != 0)
                    NPC.DelBuff(0);

                if (++NPC.ai[1] > 120)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 4f;
                    }
                    // 修改这里：进入P2时切换到正午和日食
                    Main.bloodMoon = false;
                    Main.dayTime = true;
                    Main.time = 27000; // 正午
                    Main.eclipse = true; // 启用日食
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.WorldData);

                    NPC.localAI[3] = 2; //this marks p2
                    if (WorldSavingSystem.MasochistModeReal)
                    {
                        int heal = (int)(NPC.lifeMax / 90 * Main.rand.NextFloat(1f, 1.5f));
                        NPC.life += heal;
                        if (NPC.life > NPC.lifeMax)
                            NPC.life = NPC.lifeMax;
                        CombatText.NewText(NPC.Hitbox, CombatText.HealLife, heal);
                    }
                    if (NPC.ai[1] > 210)
                    {
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.netUpdate = true;
                    }
                }
                else if (NPC.ai[1] == 120)
                {
                    FargoSoulsUtil.ClearFriendlyProjectiles(1);
                    if (FargoSoulsUtil.HostCheck && FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) == null)
                    {
                        ritualProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitual>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);
                    }
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                }
                break;

            case 0: //track player, throw scythes 
                if (!AliveCheck(npc, player) || Phase2Check(npc))
                    break;

                NPC.dontTakeDamage = false;

                if (NPC.localAI[2] == 0) //store rotation offset
                {
                    NPC.localAI[2] = player.SafeDirectionTo(NPC.Center).ToRotation()
                        + MathHelper.ToRadians(WorldSavingSystem.EternityMode ? 90 : 70) * Main.rand.NextFloat(-1, 1);
                    NPC.netUpdate = true;
                }

                targetPos = player.Center;
                targetPos += 500 * NPC.localAI[2].ToRotationVector2();
                if (NPC.Distance(targetPos) > 16)
                {
                    NPC.position += (player.position - player.oldPosition) / 3;

                    float speedModifier = NPC.localAI[3] > 0 ? 1f : 2f;
                    if (NPC.Center.X < targetPos.X)
                    {
                        NPC.velocity.X += speedModifier;
                        if (NPC.velocity.X < 0)
                            NPC.velocity.X += speedModifier * 2;
                    }
                    else
                    {
                        NPC.velocity.X -= speedModifier;
                        if (NPC.velocity.X > 0)
                            NPC.velocity.X -= speedModifier * 2;
                    }
                    if (NPC.Center.Y < targetPos.Y)
                    {
                        NPC.velocity.Y += speedModifier;
                        if (NPC.velocity.Y < 0)
                            NPC.velocity.Y += speedModifier * 2;
                    }
                    else
                    {
                        NPC.velocity.Y -= speedModifier;
                        if (NPC.velocity.Y > 0)
                            NPC.velocity.Y -= speedModifier * 2;
                    }
                    if (NPC.localAI[3] > 0)
                    {
                        if (Math.Abs(NPC.velocity.X) > 24)
                            NPC.velocity.X = 24 * Math.Sign(NPC.velocity.X);
                        if (Math.Abs(NPC.velocity.Y) > 24)
                            NPC.velocity.Y = 24 * Math.Sign(NPC.velocity.Y);
                    }
                }

                if (NPC.localAI[3] > 0) //in range, fight has begun
                {
                    NPC.ai[1]++;

                    if (NPC.ai[3] == 0)
                    {
                        NPC.ai[3] = 1;
                        if (WorldSavingSystem.MasochistModeReal) //phase 2 saucers
                        {
                            int max = NPC.localAI[3] > 1 ? 5 : Main.zenithWorld ? 3 : 2;
                            for (int i = 0; i < max; i++)
                            {
                                float ai2 = i * MathHelper.TwoPi / max; //rotation offset
                                FargoSoulsUtil.NewNPCEasy(NPC.GetSource_FromAI(), NPC.Center, ModContent.NPCType<AbomSaucer>(), 0, NPC.whoAmI, 0, ai2);
                            }
                        }
                    }
                }

                if (NPC.ai[1] == 120 - AbomStyxGazer.TelegraphTime)
                {
                    if (NPC.ai[2] < (WorldSavingSystem.MasochistModeReal ? 9 : 6) && FargoSoulsUtil.HostCheck)
                    {
                        //float rotation = MathHelper.Pi * 1f * (NPC.Center.X < player.Center.X ? 1 : -1);
                        float rotation = MathHelper.Pi * 1f * AbomStyxGazer.Direction;
                        AbomStyxGazer.Direction *= -1;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.DirectionTo(player.Center).RotatedBy(rotation * 0.6f),
                            ModContent.ProjectileType<AbomStyxGazer>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, rotation / 60 * 2);
                    }
                }

                if (NPC.ai[1] > 120)
                {
                    NPC.netUpdate = true;
                    //NPC.TargetClosest();
                    NPC.ai[1] = WorldSavingSystem.MasochistModeReal ? Main.zenithWorld ? 75 : 70 : 45;
                    NPC.localAI[2] = 0;
                    if (++NPC.ai[2] > (WorldSavingSystem.MasochistModeReal ? 7 : 5))
                    {
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.velocity = NPC.SafeDirectionTo(player.Center) * 2f;
                    }
                    else if (FargoSoulsUtil.HostCheck)
                    {
                        float ai0 = NPC.Distance(player.Center) / 30 * 2f;
                        float ai1 = NPC.localAI[3] > 1 ? 1f : 0f;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center) * 30f, ModContent.ProjectileType<AbomScytheSplit>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai1);
                    }
                }
                *//*else if (NPC.ai[1] == 90)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center + player.velocity * 30) * 30f, ModContent.ProjectileType<AbomScythe>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                }*//*
                break;

            case 1: //flaming scythe spread (shoots out further in p2)
                {
                    if (!AliveCheck(npc, player) || Phase2Check(npc))
                        break;
                    NPC.velocity = NPC.SafeDirectionTo(player.Center);
                    NPC.velocity *= NPC.localAI[3] > 1 && WorldSavingSystem.EternityMode ? 2f : 6f;

                    int max = NPC.localAI[3] > 1 ? Main.zenithWorld ? 9 : 8 : 7;
                    if (WorldSavingSystem.MasochistModeReal)
                        max++;

                    if (--NPC.ai[1] < 0)
                    {
                        if (++NPC.ai[2] > 4)
                        {
                            NPC.ai[0]++;
                            NPC.ai[1] = 0;
                            NPC.ai[2] = 0;
                        }
                        else
                        {
                            // 分别处理P1和P2的间隔时间和攻击方式
                            if (NPC.localAI[3] > 1) // P2阶段 - 保持原始P2逻辑
                            {
                                NPC.ai[1] = 60;

                                float baseDelay = WorldSavingSystem.MasochistModeReal ? 60 : 90;
                                float extendedDelay = 90;
                                float speed = 20;
                                float offset = NPC.ai[2] % 2 == 0 ? 0 : 0.5f;

                                if (FargoSoulsUtil.HostCheck && NPC.HasPlayerTarget)
                                {
                                    for (int i = 0; i < max; i++)
                                    {
                                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                            NPC.SafeDirectionTo(player.Center).RotatedBy(MathHelper.TwoPi / max * (i + offset)) * speed,
                                            ModContent.ProjectileType<AbomScytheFlaming>(),
                                            FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer,
                                            baseDelay, baseDelay + extendedDelay, ai2: NPC.target);
                                    }
                                }
                            }
                            else // P1阶段 - 使用改良版但保持稳定性
                            {
                                // 使用P2风格但调整参数
                                NPC.ai[1] = 40;
                                float baseDelay = 50f;
                                float extendedDelay = 30f;
                                float speed = 30f;
                                float offset = NPC.ai[2] % 2 == 0 ? 0.5f : 0.5f;

                                if (FargoSoulsUtil.HostCheck && NPC.HasPlayerTarget)
                                {
                                    for (int i = 0; i < max; i++)
                                    {
                                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center).RotatedBy(MathHelper.TwoPi / max * (i + offset)) * speed, ModContent.ProjectileType<AbomScytheFlaming>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, baseDelay, baseDelay + extendedDelay, ai2: NPC.target);
                                    }
                                }
                            }
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center);
                        }
                        NPC.netUpdate = true;
                        break;
                    }
                }
                break;

            case 2: //pause and then initiate dash
                if (!AliveCheck(npc, player) || Phase2Check(npc))
                    break;

                NPC.velocity *= 0.9f;
                if (WorldSavingSystem.MasochistModeReal && NPC.localAI[3] <= 1)
                    NPC.velocity *= 0.8f;

                int windup = 30;
                if (NPC.ai[2] == 0 && NPC.localAI[3] <= 1) //first dash waits a bit for scythes to clear in p1
                    windup = 60;
                if (WorldSavingSystem.MasochistModeReal && NPC.localAI[3] <= 1)
                    windup = NPC.ai[2] == 0 ? 30 : 10;
                if (NPC.ai[2] == 0 && NPC.localAI[3] > 1 && WorldSavingSystem.EternityMode) //delay on first entry here
                    windup = 240;

                if (NPC.ai[2] == 0) //first dash only
                {
                    if (NPC.localAI[3] > 1) //emode modified tells
                    {
                        if (NPC.ai[1] == 30 && WorldSavingSystem.EternityMode)
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowRingHollow>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 3, NPC.whoAmI);
                    }

                    if (NPC.ai[1] == windup - 25)
                    {
                        if (FargoSoulsUtil.HostCheck)
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.Souls.IronParry>(), 0, 0f, Main.myPlayer);
                        NPC.netUpdate = true;
                    }
                }

                if (NPC.ai[1] == 5 && NPC.ai[2] != 0) //dont do before actually starting dashes
                {
                    SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot, NPC.Center);

                    if (FargoSoulsUtil.HostCheck)
                    {
                        for (int i = 0; i < 44; i++)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextFloat(10f, 30f) * Vector2.Normalize(NPC.velocity).RotatedByRandom(MathHelper.ToRadians(40)),
                                ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai2: 1);
                        }
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(-rotation / 2),
                        //ModContent.ProjectileType<AbomStyxGazerDash>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, rotation / timeleft * 2, timeleft);
                    }
                }

                if (++NPC.ai[1] > windup)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                    NPC.ai[3] = 0;

                    if (++NPC.ai[2] > 5)
                    {
                        NPC.ai[0]++; //go to next attack after dashes
                        NPC.ai[2] = 0;
                    }
                    else
                    {
                        NPC.velocity = NPC.SafeDirectionTo(player.Center + player.velocity) * 30f;

                        if (FargoSoulsUtil.HostCheck)
                        {
                            float rotation = MathHelper.Pi * 1.5f;
                            const int timeleft = 40;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(-rotation / 2),
                                ModContent.ProjectileType<AbomStyxGazerDash>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, rotation / timeleft * 2, timeleft);
                        }

                        if (NPC.localAI[3] > 1)
                        {
                            if (WorldSavingSystem.EternityMode)
                                NPC.velocity *= 1.2f;

                            const int ring = 128;
                            for (int index1 = 0; index1 < ring; ++index1)
                            {
                                Vector2 vector2 = (-Vector2.UnitY.RotatedBy(index1 * 3.14159274101257 * 2 / ring) * new Vector2(8f, 16f)).RotatedBy(NPC.velocity.ToRotation());
                                int index2 = Dust.NewDust(NPC.Center, 0, 0, DustID.GemTopaz, 0.0f, 0.0f, 0, new Color(), 1f);
                                Main.dust[index2].scale = 3f;
                                Main.dust[index2].noGravity = true;
                                Main.dust[index2].position = NPC.Center;
                                Main.dust[index2].velocity = Vector2.Zero;
                                //Main.dust[index2].velocity = 5f * Vector2.Normalize(NPC.Center - NPC.velocity * 3f - Main.dust[index2].position);
                                Main.dust[index2].velocity += vector2 * 1.5f + NPC.velocity * 0.5f;
                            }
                        }
                    }
                }
                break;

            case 3: //while dashing (p2 makes side scythes)
                if (Phase2Check(npc))
                    break;

                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);

                ClearFrozen();

                if (NPC.localAI[3] > 1)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        int d = Dust.NewDust(NPC.Center - NPC.velocity * Main.rand.NextFloat(), 0, 0, DustID.GemTopaz, 0f, 0f, 0, new Color());
                        Main.dust[d].scale = 1f + 4f * (1f - NPC.ai[1] / 30f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 0.1f;
                    }
                }

                if (++NPC.ai[3] > 5)
                {
                    NPC.ai[3] = 0;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0, Main.myPlayer);
                        if (NPC.localAI[3] > 1)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(Math.PI / 2), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(-Math.PI / 2), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0, Main.myPlayer);
                            if (Main.zenithWorld)
                            {
                                for (float i = -1.5f; i <= 1.5f; i += 3f)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, i * Vector2.Normalize(NPC.velocity).RotatedBy(Math.PI / 2), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0, Main.myPlayer);
                                }
                            }
                        }
                    }
                }

                if (++NPC.ai[1] > 30)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]--;
                    NPC.ai[1] = 0;
                    NPC.ai[3] = 0;
                }
                break;

            case 4: //choose the next attack
                if (!AliveCheck(npc, player))
                    break;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                NPC.netUpdate = true;
                //NPC.TargetClosest();
                Player target = Main.player[NPC.target];
                NPC.ai[0] += ++NPC.localAI[0];
                if (NPC.localAI[0] >= 3) //reset p1 hard option counter
                    NPC.localAI[0] = 0;
                break;


            case 5: //改
                if (!AliveCheck(npc, player))
                    break;
                if (Phase2Check(npc))
                    break;
                NPC.velocity = NPC.SafeDirectionTo(player.Center) * 2f;

                if (++NPC.ai[1] > (NPC.localAI[3] > 1 ? 60 : 90))
                {
                    NPC.ai[1] = 0;
                    if (++NPC.ai[2] > (NPC.localAI[3] == 1 ? 3 : 6))
                    {
                        NPC.ai[0] = 22;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        //NPC.TargetClosest();
                    }
                    else
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            float baseRot = NPC.SafeDirectionTo(player.Center).ToRotation();
                            float baseSpeed = NPC.Distance(player.Center);
                            if (NPC.localAI[3] > 1)
                            {
                                baseRot = Main.rand.NextFloat(0, 360f);
                                baseSpeed = Main.rand.NextFloat(600f, 800f);
                            }

                            baseSpeed /= 90f;

                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 straightSpeed = new Vector2(baseSpeed, 0).RotatedBy(baseRot + Math.PI / 2 * i);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, straightSpeed, ModContent.ProjectileType<AbomSickleSplit1>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);

                                Vector2 diagonalSpeed = new Vector2(baseSpeed, baseSpeed).RotatedBy(baseRot + Math.PI / 2 * i);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, diagonalSpeed, ModContent.ProjectileType<AbomSickleSplit1>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);

                            }
                        }
                        SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center);
                    }
                    NPC.netUpdate = true;
                    break;
                }
                break;

            case 6: //cirno icicle fall flocko swarm (p2 shoots ice waves horizontally after)
                if (Phase2Check(npc))
                    break;
                NPC.velocity *= 0.9f;
                if (NPC.ai[2] == 0)
                {

                    NPC.ai[2] = 1;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        for (int i = -3; i <= 3; i++) //make flockos
                        {
                            if (i == 0) //dont shoot one straight up
                                continue;
                            Vector2 overheadSpeed = new(Main.rand.NextFloat(40f), Main.rand.NextFloat(-20f, 20f));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, overheadSpeed, ModContent.ProjectileType<AbomFlocko>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 360 / 3 * i);
                        }

                        //prepare ice waves

                        float offset = 420;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(20, 20), ModContent.ProjectileType<AbomFlocko3>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, offset);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(20, 20), ModContent.ProjectileType<AbomFlocko3>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, -offset);
                        if (NPC.localAI[3] <= 1)
                        {
                            Vector2 speed = new(Main.rand.NextFloat(40f), Main.rand.NextFloat(-20f, 20f));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed, ModContent.ProjectileType<AbomFlocko2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, 0, NPC.localAI[3]);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed, ModContent.ProjectileType<AbomFlocko2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, 180, NPC.localAI[3]);
                        }
                        else
                        {
                            Vector2 speed = new(Main.rand.NextFloat(40f), Main.rand.NextFloat(-20f, 20f));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomFlocko4>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, -140, 1);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomFlocko4>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, -110, -1);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomFlocko4>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, -70, 1);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomFlocko4>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, -40, -1);
                        }


                        if (!WorldSavingSystem.MasochistModeReal)
                        {
                            for (int i = -1; i <= 1; i += 2)
                            {
                                for (int j = -1; j <= 1; j += 2)
                                {
                                    int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + 3000 * i * Vector2.UnitX, Vector2.UnitY * j, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 5, 220 * i);
                                    if (p != Main.maxProjectiles)
                                    {
                                        Main.projectile[p].localAI[1] = NPC.whoAmI;
                                        if (Main.netMode == NetmodeID.Server)
                                            NetMessage.SendData(MessageID.SyncProjectile, number: p);
                                    }
                                }
                            }
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item27, NPC.Center);
                    for (int index1 = 0; index1 < 30; ++index1)
                    {
                        int index2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Snow, 0.0f, 0.0f, 0, new Color(), 1f);
                        Main.dust[index2].noGravity = true;
                        Main.dust[index2].noLight = true;
                        Main.dust[index2].velocity *= 5f;
                    }
                }
                *//*if (NPC.ai[1] > 150 && NPC.ai[1] % 4 == 0) //rain down along the exact borders
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 spawnPos = NPC.Center - Vector2.UnitY * 1100;
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Main.rand.NextFloat(300, 450) * Vector2.UnitX * i, Vector2.UnitY * 8f * Main.rand.NextFloat(1f, 4f),
                                ModContent.ProjectileType<AbomFrostShard>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                        }
                    }
                }*//*
                if (NPC.localAI[3] == 2 && NPC.ai[1] % (Main.zenithWorld ? 30 : 60) == 0 && NPC.ai[1] >= 60)
                {
                    for (int i = -2; i <= 2; i++)
                    {
                        for (int j = -1; j <= 1; j += 2)
                        {
                            Vector2 desiredPosition = NPC.Center + j * Vector2.UnitX * 1100;
                            Vector2 direction = Main.player[NPC.target].Center - desiredPosition;
                            direction /= direction.Length();
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), desiredPosition, direction.RotatedBy(i * MathHelper.Pi / 10) * 8, ModContent.ProjectileType<AbomFrostWave>(), NPC.damage / 4, 0, NPC.target);
                        }

                    }
                }
                if (++NPC.ai[1] > 420)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] = 23;
                    NPC.ai[1] = 0;
                }
                break;

            case 7: //saucer laser spam with rockets (p2 does two spams)
                if (Phase2Check(npc))
                    break;
                NPC.velocity *= 0.9f;
                if (NPC.ai[1] == 0)
                {
                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -4);
                }
                if (++NPC.ai[1] > 420)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] = 8;
                    NPC.ai[1] = 0;
                    NPC.ai[3] = 0;
                }
                else if (NPC.ai[1] > 60) //spam lasers, lerp aim
                {
                    if (NPC.localAI[3] > 1) //p2 use a different lerp instead
                    {
                        NPC.ai[3] = MathHelper.Lerp(NPC.ai[3], 1f, 0.1f);
                    }
                    else //p1 lerps slowly at you
                    {
                        float targetRot = NPC.SafeDirectionTo(player.Center).ToRotation();
                        while (targetRot < -(float)Math.PI)
                            targetRot += 2f * (float)Math.PI;
                        while (targetRot > (float)Math.PI)
                            targetRot -= 2f * (float)Math.PI;
                        NPC.ai[3] = NPC.ai[3].AngleLerp(targetRot, 0.04f);
                    }

                    if (++NPC.ai[2] > 1) //spam lasers
                    {
                        NPC.ai[2] = 0;
                        SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                        if (FargoSoulsUtil.HostCheck)
                        {
                            if (NPC.localAI[3] > 1) //p2 shoots to either side of you
                            {
                                float angleOffset = MathHelper.Lerp(180, 20, NPC.ai[3]);

                                for (int i = -3; i <= 3; i += 2)
                                {
                                    Vector2 speed = 16f * NPC.SafeDirectionTo(player.Center).RotatedBy((Main.rand.NextDouble() - 0.5) * 0.785398185253143 / 3.0);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed.RotatedBy(MathHelper.ToRadians(angleOffset * i)), ModContent.ProjectileType<AbomLaser>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                                }
                            }
                            else //p1 shoots directly
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    Vector2 speed = 16f * NPC.ai[3].ToRotationVector2().RotatedBy((Main.rand.NextDouble() - 0.5) * 0.785398185253143 / 2.0);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed, ModContent.ProjectileType<AbomLaser>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed.RotatedBy(Math.PI), ModContent.ProjectileType<AbomLaser>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                                }
                            }
                        }
                    }
                    if (NPC.localAI[3] == 1)
                    {
                        if (++NPC.localAI[2] > 60)
                        {
                            NPC.localAI[2] = 0;
                            for (int i = 0; i < 7; i++)
                            {

                                Vector2 vel = NPC.SafeDirectionTo(player.Center);
                                vel *= 6f;
                                float ai2 = NPC.localAI[3] > 1 ? 0 : 1;
                                if (FargoSoulsUtil.HostCheck)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel.RotatedBy(i * 2 * Math.PI / 7), ModContent.ProjectileType<AbomRocket>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, 20, ai2);
                            }
                        }
                    }
                    else
                    {
                        if (++NPC.localAI[2] % 15 == 0)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 vel = NPC.SafeDirectionTo(player.Center);
                                vel *= 7f;
                                float ai2 = NPC.localAI[3] > 1 ? 0 : 1;
                                if (FargoSoulsUtil.HostCheck)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel.RotatedBy(i * 2 * Math.PI / 4), ModContent.ProjectileType<AbomRocket>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, 20, ai2);
                            }
                            for (int i = -1; i < 2;)
                            {

                                Vector2 vel = NPC.SafeDirectionTo(player.Center);
                                vel *= 50f;
                                float ai2 = NPC.localAI[3] > 1 ? 0 : 1;
                                if (FargoSoulsUtil.HostCheck)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel.RotatedBy(i * 2 * Math.PI / 3), ModContent.ProjectileType<AbomRocket2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, 10, ai2);
                                i += 2;
                            }
                            NPC.localAI[2] = 0;
                        }

                    }


                }
                else
                {
                    if (NPC.localAI[3] > 1)
                    {
                        NPC.ai[3] = 0;
                    }
                    else
                    {
                        NPC.ai[3] = NPC.DirectionFrom(player.Center).ToRotation() - 0.001f;
                        while (NPC.ai[3] < -(float)Math.PI)
                            NPC.ai[3] += 2f * (float)Math.PI;
                        while (NPC.ai[3] > (float)Math.PI)
                            NPC.ai[3] -= 2f * (float)Math.PI;
                    }

                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                    //make warning dust
                    for (int i = 0; i < 5; i++)
                    {
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 4f;
                    }
                }
                break;


            case 8: //return to beginning in p1, proceed in p2
                if (!AliveCheck(npc, player) || Phase2Check(npc))
                    break;
                NPC.velocity *= 0.9f;
                NPC.localAI[2] = 0;
                if (++NPC.ai[1] > 90)
                {
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    NPC.netUpdate = true;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    //NPC.TargetClosest();
                    if (NPC.localAI[3] > 1 && WorldSavingSystem.EternityMode) //if in maso p2, do super attacks
                    {
                        if (NPC.localAI[1] == 0)
                        {
                            NPC.localAI[1] = 1;
                            NPC.ai[0] = 15;
                        }
                        else
                        {
                            NPC.localAI[1] = 0;
                            NPC.ai[0]++;
                        }
                    }
                    else //still in p1
                    {
                        NPC.ai[0] = 0;
                    }
                }
                break;

            case 9: //beginning of scythe rows and deathray rain
                if (NPC.ai[1] == 0 && !AliveCheck(npc, player))
                    break;

                NPC.velocity = Vector2.Zero;
                NPC.localAI[2] = 0;

                if (NPC.ai[1] < 60)
                    FancyFireballs((int)NPC.ai[1]);

                if (++NPC.ai[1] == 1)
                {
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    NPC.ai[3] = NPC.SafeDirectionTo(player.Center).ToRotation();
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.ai[3].ToRotationVector2(), ModContent.ProjectileType<AbomDeathraySmall>(), 0, 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -NPC.ai[3].ToRotationVector2(), ModContent.ProjectileType<AbomDeathraySmall>(), 0, 0f, Main.myPlayer);
                    }
                }
                else if (NPC.ai[1] == 61)
                {
                    const int max = 8;
                    const float gap = 1200 / max;
                    for (int j = -1; j <= 1; j += 2)
                    {
                        Vector2 dustVel = NPC.ai[3].ToRotationVector2() * j * 3f;

                        for (int i = 0; i < 20; i++)
                        {
                            int dust = Dust.NewDust(NPC.Center, 0, 0, DustID.Smoke, dustVel.X, dustVel.Y, 0, default, 3f);
                            Main.dust[dust].velocity *= 1.4f;
                        }

                        for (int i = 1; i <= max + 2; i++)
                        {
                            float speed = i * j * gap / 30;
                            float ai1 = i % 2 == 0 ? -1 : 1;

                            Vector2 vel = speed * NPC.ai[3].ToRotationVector2();

                            for (int k = 0; k < 3; k++)
                            {
                                int d = Dust.NewDust(NPC.Center, 0, 0, DustID.PurpleCrystalShard, vel.X, vel.Y, Scale: 3f);
                                Main.dust[d].velocity *= 1.5f;
                                Main.dust[d].noGravity = true;
                            }

                            if (FargoSoulsUtil.HostCheck)
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<AbomScytheSpin>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, NPC.whoAmI, ai1);


                        }
                    }
                }
                if (NPC.ai[1] % 20 == 0 && NPC.ai[1] > 120 && NPC.ai[1] < 450)
                {

                    Vector2 direction = Main.player[0].Center - NPC.Center;
                    direction.Normalize();
                    for (int i = 0; i < 3; i++)
                    {
                        direction = direction.RotatedBy(Math.PI * 2 / 3);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, direction, ModContent.ProjectileType<AbomLightningTelegraph>(), NPC.damage, 0f, Main.myPlayer);
                    }

                }
                else if (NPC.ai[1] > 61 + 60 + 360 + 30)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                    NPC.ai[3] = 0;
                }
                break;

            case 10: //prepare deathray rain
                if (NPC.ai[1] < 90 && !AliveCheck(npc, player))
                    break;

                ClearFrozen();

                *//*for (int i = 0; i < 5; i++) //make warning dust
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 87, 0f, 0f, 0, default(Color), 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 4f;
                }*//*

                if (NPC.ai[2] == 0 && NPC.ai[3] == 0) //target one side of arena
                {
                    NPC.ai[2] = NPC.Center.X + (player.Center.X < NPC.Center.X ? -1400 : 1400);
                }

                if (NPC.localAI[2] == 0) //direction to dash in next
                {
                    NPC.localAI[2] = NPC.ai[2] > NPC.Center.X ? -1 : 1;
                }

                if (NPC.ai[1] > 90)
                {
                    FancyFireballs((int)NPC.ai[1] - 90);
                }
                else
                {
                    NPC.ai[3] = player.Center.Y - 300;
                }

                targetPos = new Vector2(NPC.ai[2], NPC.ai[3]);
                Movement(npc, targetPos, 1.4f);

                if (++NPC.ai[1] > 150)
                {
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = NPC.localAI[2];
                    NPC.ai[3] = 0;
                    NPC.localAI[2] = 0;
                }
                break;

            case 11: //dash and make deathrays
                NPC.velocity.X = NPC.ai[2] * 18f;
                MovementY(npc, player.Center.Y - 250, Math.Abs(player.Center.Y - NPC.Center.Y) < 200 ? 2f : 0.7f);
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);

                ClearFrozen();

                if (++NPC.ai[3] > 5)
                {
                    NPC.ai[3] = 0;

                    SoundEngine.PlaySound(SoundID.Item12, NPC.Center);

                    float timeLeft = 2400 / Math.Abs(NPC.velocity.X) * 2 - NPC.ai[1] + 120;
                    if (NPC.ai[1] <= 15)
                    {
                        timeLeft = 0;
                    }
                    else
                    {
                        if (NPC.localAI[2] != 0)
                            timeLeft = 0;
                        if (++NPC.localAI[2] > (Main.zenithWorld ? 1 : 2))
                            NPC.localAI[2] = 0;
                    }

                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 vel1 = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                        Vector2 vel2 = -Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel1, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel2, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                    }
                }
                if (++NPC.ai[1] > 2400 / Math.Abs(NPC.velocity.X))
                {
                    NPC.netUpdate = true;
                    NPC.velocity.X = NPC.ai[2] * 18f;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                    //NPC.ai[2] = 0; //will be reused shortly
                    NPC.ai[3] = 0;
                }
                break;

            case 12: //prepare for next deathrain
                if (NPC.ai[1] < 150 && !AliveCheck(npc, player))
                    break;

                ClearFrozen();

                NPC.velocity.Y = 0f;

                *//*for (int i = 0; i < 5; i++) //make warning dust
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 87, 0f, 0f, 0, default(Color), 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 4f;
                }*//*

                NPC.velocity *= 0.947f;
                NPC.ai[3] += NPC.velocity.Length();

                if (NPC.ai[1] > 150)
                    FancyFireballs((int)NPC.ai[1] - 150);

                if (++NPC.ai[1] > 210)
                {
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                    NPC.ai[3] = 0;
                }
                break;

            case 13: //second deathray dash
                NPC.velocity.X = NPC.ai[2] * -18f;
                MovementY(npc, player.Center.Y - 250, Math.Abs(player.Center.Y - NPC.Center.Y) < 200 ? 2f : 0.7f);
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);

                ClearFrozen();

                if (++NPC.ai[3] > 5)
                {
                    NPC.ai[3] = 0;

                    SoundEngine.PlaySound(SoundID.Item12, NPC.Center);

                    float timeLeft = 2400 / Math.Abs(NPC.velocity.X) * 2 - NPC.ai[1] + 120;
                    if (NPC.ai[1] <= 15)
                    {
                        timeLeft = 0;
                    }
                    else
                    {
                        if (NPC.localAI[2] != 0)
                            timeLeft = 0;
                        if (++NPC.localAI[2] > (Main.zenithWorld ? 1 : 2))
                            NPC.localAI[2] = 0;
                    }

                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 vel1 = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                        Vector2 vel2 = -Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel1, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel2, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                    }
                }
                if (++NPC.ai[1] > 2400 / Math.Abs(NPC.velocity.X))
                {
                    NPC.netUpdate = true;
                    NPC.velocity.X = NPC.ai[2] * -18f;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                }
                break;

            case 14: //pause before looping back to first attack
                if (!AliveCheck(npc, player))
                    break;
                NPC.velocity *= 0.9f;
                if (++NPC.ai[1] > 60)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] = NPC.dontTakeDamage ? -3 : 0;
                    NPC.ai[1] = 0;
                }
                break;

            case 15: //beginning of laevateinn, pause and then sworddash
                NPC.velocity *= 0.9f;

                void FancyFireballs(int repeats)
                {
                    float modifier = 0;
                    for (int i = 0; i < repeats; i++)
                        modifier = MathHelper.Lerp(modifier, 1f, 0.08f);

                    float distance = 1400 * (1f - modifier);
                    float rotation = MathHelper.TwoPi * modifier;
                    const int max = 4;
                    for (int i = 0; i < max; i++)
                    {
                        int d = Dust.NewDust(NPC.Center + distance * Vector2.UnitX.RotatedBy(rotation + MathHelper.TwoPi / max * i), 0, 0, DustID.PurpleCrystalShard, NPC.velocity.X * 0.3f, NPC.velocity.Y * 0.3f, newColor: Color.White);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].scale = 6f - 4f * modifier;
                    }
                }

                ClearFrozen();

                // 前60帧显示火焰特效（第一轮）或快速火焰特效（第二轮）
                if (NPC.ai[1] < 60)
                    FancyFireballs((int)NPC.ai[1]);

                if (NPC.ai[1] == 0 && NPC.ai[2] != 2 && FargoSoulsUtil.HostCheck)
                {
                    float ai1 = NPC.ai[2] == 1 ? -1 : 1;
                    if (NPC.ai[2] == 0) // 第一轮循环预警
                    {
                        ai1 *= MathHelper.ToRadians(270) / 120 * -1 * 60;
                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                            0f, Main.myPlayer, 3, ai1);
                        if (p != Main.maxProjectiles)
                        {
                            Main.projectile[p].localAI[1] = NPC.whoAmI;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncProjectile, number: p);
                        }
                    }
                    else // 第二轮循环快速预警
                    {
                        ai1 *= MathHelper.ToRadians(270) / 120 * -1 * 105;
                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                            0f, Main.myPlayer, 3, ai1);
                        if (p != Main.maxProjectiles)
                        {
                            Main.projectile[p].localAI[1] = NPC.whoAmI;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncProjectile, number: p);
                        }
                        // 生成红色预警环
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                            Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowRing>(),
                            0, 0f, Main.myPlayer, NPC.whoAmI, -3);
                    }
                }

                NPC.ai[1]++;
                if (NPC.ai[2] == 0) // 第一轮循环：生成AbomSword2
                {
                    if (NPC.ai[1] > 90)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.velocity = NPC.SafeDirectionTo(player.Center) * 3f;
                    }
                    else if (NPC.ai[1] == 60 && FargoSoulsUtil.HostCheck)
                    {
                        NPC.netUpdate = true;
                        NPC.velocity = Vector2.Zero;

                        //SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        float ai0 = NPC.ai[2] == 1 ? -1 : 1;

                        ai0 *= MathHelper.ToRadians(270) / 120;
                        Vector2 vel = NPC.SafeDirectionTo(player.Center).RotatedBy(-ai0 * 60);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                            ModContent.ProjectileType<AbomSword2>(),
                            FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8),
                            0f, Main.myPlayer, ai0, NPC.whoAmI, ai2: 1);

                    }
                }
                else // 第二轮循环：生成快速旋转的AbomSword3
                {
                    float ai0 = NPC.ai[2] == 1 ? -1 : 1;
                    ai0 *= MathHelper.ToRadians(270) / 120; // 更快的旋转速度
                    Vector2 vel = NPC.SafeDirectionTo(player.Center).RotatedBy(-ai0 * 60);
                    if (NPC.ai[1] > 90)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.velocity = NPC.SafeDirectionTo(player.Center) * 20f;
                    }
                    else if (NPC.ai[1] == 60)
                    {
                        ai0 = NPC.ai[2] == 1 ? -1 : 1;

                        ai0 *= MathHelper.ToRadians(270) / 120; // 更快的旋转速度
                        vel = NPC.SafeDirectionTo(player.Center).RotatedBy(-ai0 * 60);
                    }
                    else if (NPC.ai[1] == 90 && FargoSoulsUtil.HostCheck)
                    {
                        NPC.netUpdate = true;
                        NPC.velocity = Vector2.Zero;

                        //SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                            ModContent.ProjectileType<AbomSword3>(),
                            FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8),
                            0f, Main.myPlayer, 3 * ai0, NPC.whoAmI); // 旋转速度×3

                        // 快速旋转的特殊音效和效果
                        SoundEngine.PlaySound(FargosSoundRegistry.StyxGazer with { Volume = 2.0f, Pitch = -0.3f }, NPC.Center);
                        for (int i = 0; i < 20; i++)
                        {
                            int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                DustID.GemTopaz, 0f, 0f, 0, default, 3f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 6f;
                        }
                    }

                }

                break;

            case 16: //while dashing
                if (NPC.ai[2] == 1)
                {
                    NPC.direction = NPC.spriteDirection = 16 * Math.Sign(NPC.velocity.X);
                    if (++NPC.ai[1] > 30)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                    }
                    break;
                }
                else
                {
                    NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
                    if (++NPC.ai[1] > 120)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                    }
                    break;
                }


            case 17: //wait for scythes to clear
                if (!AliveCheck(npc, player))
                    break;

                ClearFrozen();

                targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 500;
                if (NPC.Distance(targetPos) > 50)
                    Movement(npc, targetPos, 0.7f);
                if (++NPC.ai[1] > 60) // || (NPC.dontTakeDamage && NPC.ai[1] > 30))
                {
                    NPC.netUpdate = true;
                    if (++NPC.ai[2] < 2)
                    {
                        NPC.ai[0] -= 2;
                    }
                    else
                    {
                        NPC.ai[0]++;
                        NPC.ai[2] = 0;
                    }
                    NPC.ai[1] = 0;
                }
                break;

            case 18: //beginning of vertical dive
                {
                    if (NPC.ai[1] < 90 && !AliveCheck(npc, player))
                        break;

                    ClearFrozen();

                    *//*for (int i = 0; i < 5; i++) //make warning dust
                    {
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 87, 0f, 0f, 0, default(Color), 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 4f;
                    }*//*

                    if (NPC.ai[2] == 0 && NPC.ai[3] == 0) //target one side of arena
                    {
                        NPC.netUpdate = true;
                        NPC.ai[2] = player.Center.X;
                        NPC.ai[3] = player.Center.Y;
                        if (FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) != null)
                        {
                            NPC.ai[2] = Main.projectile[ritualProj].Center.X;
                            NPC.ai[3] = Main.projectile[ritualProj].Center.Y;
                        }

                        Vector2 offset;
                        offset.X = Math.Sign(player.Center.X - NPC.ai[2]);
                        offset.Y = Math.Sign(player.Center.Y - NPC.ai[3]);
                        NPC.localAI[2] = offset.ToRotation();
                    }

                    Vector2 actualTargetPositionOffset = (float)Math.Sqrt(2 * 1200 * 1200) * NPC.localAI[2].ToRotationVector2();
                    actualTargetPositionOffset.Y -= 450 * Math.Sign(actualTargetPositionOffset.Y);

                    targetPos = new Vector2(NPC.ai[2], NPC.ai[3]) + actualTargetPositionOffset;
                    Movement(npc, targetPos, 1f);

                    if (NPC.ai[1] == 0 && FargoSoulsUtil.HostCheck)
                    {
                        float horizontalModifier = Math.Sign(NPC.ai[2] - targetPos.X);
                        float verticalModifier = Math.Sign(NPC.ai[3] - targetPos.Y);

                        float startRotation = horizontalModifier > 0 ? MathHelper.ToRadians(0.1f) * -verticalModifier : MathHelper.Pi - MathHelper.ToRadians(0.1f) * -verticalModifier;
                        float ai1 = horizontalModifier > 0 ? MathHelper.Pi : 0;
                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, startRotation.ToRotationVector2(), ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 4, ai1);
                        if (p != Main.maxProjectiles)
                        {
                            Main.projectile[p].localAI[1] = NPC.whoAmI;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncProjectile, number: p);
                        }
                    }

                    if (NPC.ai[1] > 90)
                        FancyFireballs((int)NPC.ai[1] - 90);

                    if (++NPC.ai[1] > 150)
                    {
                        NPC.netUpdate = true;
                        NPC.velocity = Vector2.Zero;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                    }
                    *//*else if (NPC.ai[1] == 180 || (NPC.dontTakeDamage && NPC.ai[1] == 120))
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        if (FargoSoulsUtil.HostCheck)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.UnitX * NPC.localAI[2], ModContent.ProjectileType<AbomDeathraySmall2>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                    }*//*
                }
                break;

            case 19: //prepare to dash
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.ai[2] - NPC.Center.X);

                ClearFrozen();
                int SpinTime = 60;
                if (NPC.ai[1] == 0)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        float horizontalModifier = Math.Sign(NPC.ai[2] - NPC.Center.X);
                        float verticalModifier = Math.Sign(NPC.ai[3] - NPC.Center.Y);

                        float ai0 = horizontalModifier * MathHelper.Pi / SpinTime * verticalModifier;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX * -horizontalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI);
                        if (WorldSavingSystem.MasochistModeReal)
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.UnitX * -horizontalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI);
                    }
                }

                if (++NPC.ai[1] > SpinTime)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;

                    NPC.velocity.X = 0f;//(player.Center.X - NPC.Center.X) / 90 / 4;
                    NPC.velocity.Y = 24 * Math.Sign(NPC.ai[3] - NPC.Center.Y);
                }
                break;

            case 20: //while dashing down

                ClearFrozen();

                NPC.velocity.Y *= 0.97f;
                NPC.position += NPC.velocity;
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.ai[2] - NPC.Center.X);
                if (++NPC.ai[1] > 90)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                }
                break;

            case 21: //wait for scythes to clear
                if (!AliveCheck(npc, player))
                    break;
                NPC.localAI[2] = 0;
                targetPos = player.Center;
                targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
                if (NPC.Distance(targetPos) > 50)
                    Movement(npc, targetPos, 0.7f);
                if (++NPC.ai[1] > 60)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] = NPC.dontTakeDamage ? -4 : 0;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                }
                break;
            case 22: //扇形血刺弹幕风暴
                {
                    if (!AliveCheck(npc, player))
                        break;

                    if (NPC.localAI[3] <= 1) // P1阶段
                    {
                        if (Main.zenithWorld)
                        {
                            #region stonger bloodneedle
                            // 使用 ai[2] 作为阶段状态 (0-5)
                            int currentPhase = (int)NPC.ai[2];
                            if (currentPhase == 0)
                            {
                                // 减速至静止
                                NPC.velocity *= 0.9f;

                                // 警告音效（仅在第0帧播放）
                                if (NPC.ai[1] == 0)
                                {
                                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                                    // 警告特效
                                    for (int i = 0; i < 20; i++)
                                    {
                                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                            DustID.Blood, 0f, 0f, 0, default, 2f);
                                        Main.dust[d].noGravity = true;
                                        Main.dust[d].velocity *= 3f;
                                    }

                                    // 重置角度
                                    // 使用 ai[3] 存储角度a
                                    NPC.ai[3] = 0; // 角度a初始为0
                                    NPC.localAI[2] = 0; // 角度b初始为0
                                                        // 使用 localAI[0] 临时存储角度c（不会影响攻击顺序）
                                    NPC.localAI[0] = 0; // 角度c初始为0

                                    NPC.netUpdate = true;
                                }

                                NPC.ai[1]++;

                                // 30帧后进入下一阶段
                                if (NPC.ai[1] >= 60)
                                {
                                    NPC.ai[2] = 1; // 进入阶段1
                                    NPC.ai[1] = 0;

                                    // 进入新阶段特效
                                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段1: 6次定向发射 (160帧，每隔40帧发射一次)
                            else if (currentPhase == 1)
                            {
                                // 保持静止
                                NPC.velocity *= 0.95f;
                                // 总计时器
                                NPC.ai[1]++;
                                // 每2帧发射一次
                                if (NPC.ai[1] % 4 == 0)
                                {
                                    // 当前基础角度a（存储在 ai[3] 中）
                                    float baseAngleA = NPC.ai[3];
                                    // 在6个方向生成血弹
                                    for (int i = 0; i < 3; i++)
                                    {
                                        float directionAngle = baseAngleA + i * 120;
                                        GenerateBloodThorn(npc, directionAngle, NPC.Center, 1100f);
                                    }
                                    // 每2帧基础角度a自增2°
                                    NPC.ai[3] += 4;
                                    if (NPC.ai[3] >= 360)
                                        NPC.ai[3] -= 360;

                                    // 播放发射音效（每10帧一次）
                                    if (NPC.ai[1] % 10 == 0)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.1f }, NPC.Center);
                                    }
                                }

                                // 每40帧额外自增30°
                                if (NPC.ai[1] % 40 == 0 && NPC.ai[1] > 0)
                                {
                                    NPC.ai[3] += 50;
                                    if (NPC.ai[3] >= 360)
                                        NPC.ai[3] -= 360;
                                    // 额外特效
                                    for (int i = 0; i < 10; i++)
                                    {
                                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                            DustID.Blood, 0f, 0f, 0, default, 2f);
                                        Main.dust[d].noGravity = true;
                                        Main.dust[d].velocity *= 2f;
                                    }

                                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                                    NPC.netUpdate = true;
                                }
                                // 160帧结束后进入暂停阶段
                                if (NPC.ai[1] >= 180)
                                {
                                    NPC.ai[2] = 2; // 进入阶段2
                                    NPC.ai[1] = 0;
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段2: 暂停30帧
                            else if (currentPhase == 2)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.ai[1]++;
                                // 30帧后进入下一阶段
                                if (NPC.ai[1] >= 10)
                                {
                                    NPC.ai[2] = 3; // 进入阶段3
                                    NPC.ai[1] = 0;
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段3: 递增旋转发射 (60帧)
                            else if (currentPhase == 3)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.ai[1]++;
                                // 每3帧生成一次
                                if (NPC.ai[1] % 3 == 0)
                                {
                                    // 当前角度b（存储在 localAI[2] 中）
                                    float currentB = NPC.localAI[2];
                                    // 在6个方向生成血弹
                                    for (int i = 0; i < 6; i++)
                                    {
                                        float directionAngle = currentB + i * 60;
                                        GenerateBloodThorn(npc, directionAngle, NPC.Center, 1100f);
                                    }

                                    // b自增2°
                                    NPC.localAI[2] += 3;
                                    if (NPC.localAI[2] >= 360)
                                        NPC.localAI[2] -= 360;

                                    // 播放发射音效
                                    if (NPC.ai[1] % 10 == 0)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.2f }, NPC.Center);
                                    }
                                }

                                // 60帧后进入下一阶段
                                if (NPC.ai[1] >= 80)
                                {
                                    NPC.ai[2] = 4; // 进入阶段4
                                    NPC.ai[1] = 0;
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段4: 递减旋转发射 (60帧)
                            else if (currentPhase == 4)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.ai[1]++;
                                // 每3帧生成一次
                                if (NPC.ai[1] % 3 == 0)
                                {
                                    // 当前角度b
                                    float currentB = NPC.localAI[2];

                                    // 在6个方向生成血弹
                                    for (int i = 0; i < 6; i++)
                                    {
                                        float directionAngle = currentB + i * 60;
                                        GenerateBloodThorn(npc, directionAngle, NPC.Center, 1100f);
                                    }
                                    // b自减2°
                                    NPC.localAI[2] -= 3;
                                    if (NPC.localAI[2] < 0)
                                        NPC.localAI[2] += 360;
                                    // 播放发射音效
                                    if (NPC.ai[1] % 10 == 0)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.2f }, NPC.Center);
                                    }
                                }
                                // 60帧后进入下一阶段
                                if (NPC.ai[1] >= 80)
                                {
                                    NPC.ai[2] = 5; // 进入阶段5
                                    NPC.ai[1] = 0;
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段5: 四方向切线发射 (120帧)
                            else if (currentPhase == 5)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.ai[1]++;
                                // 每帧生成
                                if (NPC.ai[1] % 10 == 0)
                                {
                                    // 当前角度c（存储在 localAI[0] 中，这里临时使用不会影响攻击顺序）
                                    float currentC = NPC.localAI[0];

                                    // 在4个方向生成血弹
                                    for (int i = 0; i < 20; i++)
                                    {
                                        float directionAngle = currentC + i * 18;
                                        // 生成两个与半径为200的圆相切的血弹
                                        GenerateTangentialBloodThorns(npc, directionAngle, NPC.Center, 1100f, 900 - 0.0486111f * NPC.ai[1] * NPC.ai[1]);
                                    }


                                    // 播放发射音效
                                    if (NPC.ai[1] % 15 == 0)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.3f }, NPC.Center);
                                    }
                                }
                                // 120帧后结束攻击，返回休息状态
                                if (NPC.ai[1] >= 120)
                                {
                                    NPC.localAI[0] = 1;

                                    NPC.ai[0] = 8; // 返回休息状态
                                    NPC.ai[1] = 0;
                                    NPC.ai[2] = 0;
                                    NPC.ai[3] = 0;

                                    // 结束特效
                                    SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                                    for (int i = 0; i < 30; i++)
                                    {
                                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                            DustID.Blood, 0f, 0f, 0, default, 2.5f);
                                        Main.dust[d].noGravity = true;
                                        Main.dust[d].velocity *= 3f;
                                    }

                                    NPC.netUpdate = true;
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            // 阶段1：预警音效并减速至静止
                            if (NPC.ai[1] < 50)
                            {
                                // 预警音效（仅在第一帧播放）
                                if (NPC.ai[1] == 0)
                                {
                                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                                    NPC.netUpdate = true;
                                }

                                // 逐渐减速至静止
                                NPC.velocity *= 0.9f;

                                // 生成预警特效
                                if (Main.rand.NextBool(3))
                                {
                                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                        DustID.Blood, 0f, 0f, 0, default, 1.5f);
                                    Main.dust[d].noGravity = true;
                                    Main.dust[d].velocity *= 0.5f;
                                }
                            }
                            else // 阶段2：发射血刺弹幕
                            {
                                // 使用 ai[2] 记录发射次数
                                if ((NPC.ai[1] - 50) % 30 == 0 && NPC.ai[2] < 6)
                                {
                                    // 计算abom到玩家的方向角度（弧度）
                                    Vector2 directionToPlayer = player.Center - NPC.Center;
                                    float baseAngle = directionToPlayer.ToRotation();

                                    // 生成扇形血刺弹幕
                                    if (FargoSoulsUtil.HostCheck)
                                    {
                                        // 扇形角度范围：-20°到+20°
                                        float spreadAngle = MathHelper.ToRadians(20);
                                        // 每隔4°生成一个弹幕
                                        float angleStep = MathHelper.ToRadians(4);

                                        for (float angleOffset = -spreadAngle; angleOffset <= spreadAngle; angleOffset += angleStep)
                                        {
                                            // 计算弹幕生成位置（距离abom 1100像素）
                                            for (int i = 0; i < 4; i++)
                                            {
                                                float currentAngle = baseAngle + angleOffset + (float)(i * 1 * Math.PI / 2);
                                                Vector2 spawnOffset = Vector2.UnitX.RotatedBy(currentAngle) * 1100f;
                                                Vector2 spawnPos = NPC.Center + spawnOffset;

                                                // 计算速度方向（指向abom）
                                                Vector2 velocity = -spawnOffset.SafeNormalize(Vector2.UnitX) * (0.6f + angleOffset / 100);

                                                // 生成BloodThornMissile弹幕
                                                Projectile.NewProjectile(
                                                    NPC.GetSource_FromThis(),
                                                    spawnPos,
                                                    velocity,
                                                    ModContent.ProjectileType<Projectiles.Masomode.BloodThornMissile>(),
                                                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                                                    0f,
                                                    Main.myPlayer
                                                );
                                            }
                                        }

                                        // 播放发射音效
                                        SoundEngine.PlaySound(SoundID.Item17, NPC.Center);

                                        // 发射特效
                                        for (int i = 0; i < 10; i++)
                                        {
                                            int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                                DustID.Blood, 0f, 0f, 0, default, 2f);
                                            Main.dust[d].noGravity = true;
                                            Main.dust[d].velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * 3f;
                                        }
                                    }

                                    // 记录发射次数
                                    NPC.ai[2]++;
                                    NPC.netUpdate = true;
                                }

                                // 保持静止状态
                                NPC.velocity *= 0.95f;
                            }

                            NPC.ai[1]++;

                            // 检查是否结束（发射6次后）
                            if (NPC.ai[2] >= 6 && NPC.ai[1] > 50 + 30 * 6)
                            {
                                NPC.netUpdate = true;
                                NPC.ai[0] = 8; // 返回到休息状态
                                NPC.ai[1] = 0;
                                NPC.ai[2] = 0;
                                NPC.ai[3] = 0;

                                // 结束特效
                                SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                                for (int i = 0; i < 20; i++)
                                {
                                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                        DustID.Blood, 0f, 0f, 0, default, 2.5f);
                                    Main.dust[d].noGravity = true;
                                    Main.dust[d].velocity *= 3f;
                                }
                            }
                        }
                    }
                    else // P2阶段：新攻击模式 （摸摸）
                    {
                        if (Main.zenithWorld)//gfb的石
                        {
                            // 阶段1：预警音效并减速至静止
                            if (NPC.ai[1] <= 60)
                            {
                                // 预警音效（仅在第一帧播放）
                                if (NPC.ai[1] == 0)
                                {
                                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                                    NPC.netUpdate = true;
                                }
                                if (NPC.ai[1] <= 36 && NPC.ai[1] % 4 == 0)
                                {
                                    float angle = (210f + 10 * NPC.ai[1] / 3) * MathHelper.Pi / 180;
                                    NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AbomSaucer3>(), 0, NPC.whoAmI, 0, angle);
                                }
                                // 逐渐减速至静止
                                NPC.velocity *= 0.9f;

                                // 生成预警特效
                                if (Main.rand.NextBool(3))
                                {
                                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                        DustID.Blood, 0f, 0f, 0, default, 1.5f);
                                    Main.dust[d].noGravity = true;
                                    Main.dust[d].velocity *= 0.5f;
                                }
                            }
                            NPC.ai[1]++;


                            if (NPC.ai[1] > 480 + 20)
                            {
                                NPC.netUpdate = true;
                                NPC.ai[0] = 8; // 返回到休息状态
                                NPC.ai[1] = 0;
                                NPC.ai[2] = 0;
                                NPC.ai[3] = 0;

                                // 结束特效
                                SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                                for (int i = 0; i < 20; i++)
                                {
                                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                        DustID.Blood, 0f, 0f, 0, default, 2.5f);
                                    Main.dust[d].noGravity = true;
                                    Main.dust[d].velocity *= 3f;
                                }
                            }
                        }
                        else //非gfb（原版）
                        {
                            #region stonger bloodneedle
                            // 使用 ai[2] 作为阶段状态 (0-5)
                            int currentPhase = (int)NPC.ai[2];
                            if (currentPhase == 0)
                            {
                                // 减速至静止
                                NPC.velocity *= 0.9f;

                                // 警告音效（仅在第0帧播放）
                                if (NPC.ai[1] == 0)
                                {
                                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                                    // 警告特效
                                    for (int i = 0; i < 20; i++)
                                    {
                                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                            DustID.Blood, 0f, 0f, 0, default, 2f);
                                        Main.dust[d].noGravity = true;
                                        Main.dust[d].velocity *= 3f;
                                    }

                                    // 重置角度
                                    // 使用 ai[3] 存储角度a
                                    NPC.ai[3] = 0; // 角度a初始为0
                                    NPC.localAI[2] = 0; // 角度b初始为0
                                                        // 使用 localAI[0] 临时存储角度c（不会影响攻击顺序）
                                    NPC.localAI[0] = 0; // 角度c初始为0

                                    NPC.netUpdate = true;
                                }

                                NPC.ai[1]++;

                                // 30帧后进入下一阶段
                                if (NPC.ai[1] >= 60)
                                {
                                    NPC.ai[2] = 1; // 进入阶段1
                                    NPC.ai[1] = 0;

                                    // 进入新阶段特效
                                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段1: 6次定向发射 (160帧，每隔40帧发射一次)
                            else if (currentPhase == 1)
                            {
                                // 保持静止
                                NPC.velocity *= 0.95f;
                                // 总计时器
                                NPC.ai[1]++;
                                // 每2帧发射一次
                                if (NPC.ai[1] % 4 == 0)
                                {
                                    // 当前基础角度a（存储在 ai[3] 中）
                                    float baseAngleA = NPC.ai[3];
                                    // 在6个方向生成血弹
                                    for (int i = 0; i < 3; i++)
                                    {
                                        float directionAngle = baseAngleA + i * 120;
                                        GenerateBloodThorn(npc, directionAngle, NPC.Center, 1100f);
                                    }
                                    // 每2帧基础角度a自增2°
                                    NPC.ai[3] += 4;
                                    if (NPC.ai[3] >= 360)
                                        NPC.ai[3] -= 360;

                                    // 播放发射音效（每10帧一次）
                                    if (NPC.ai[1] % 10 == 0)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.1f }, NPC.Center);
                                    }
                                }

                                // 每40帧额外自增30°
                                if (NPC.ai[1] % 40 == 0 && NPC.ai[1] > 0)
                                {
                                    NPC.ai[3] += 50;
                                    if (NPC.ai[3] >= 360)
                                        NPC.ai[3] -= 360;
                                    // 额外特效
                                    for (int i = 0; i < 10; i++)
                                    {
                                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                            DustID.Blood, 0f, 0f, 0, default, 2f);
                                        Main.dust[d].noGravity = true;
                                        Main.dust[d].velocity *= 2f;
                                    }

                                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                                    NPC.netUpdate = true;
                                }
                                // 160帧结束后进入暂停阶段
                                if (NPC.ai[1] >= 180)
                                {
                                    NPC.ai[2] = 2; // 进入阶段2
                                    NPC.ai[1] = 0;
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段2: 暂停30帧
                            else if (currentPhase == 2)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.ai[1]++;
                                // 30帧后进入下一阶段
                                if (NPC.ai[1] >= 10)
                                {
                                    NPC.ai[2] = 3; // 进入阶段3
                                    NPC.ai[1] = 0;
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段3: 递增旋转发射 (60帧)
                            else if (currentPhase == 3)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.ai[1]++;
                                // 每3帧生成一次
                                if (NPC.ai[1] % 3 == 0)
                                {
                                    // 当前角度b（存储在 localAI[2] 中）
                                    float currentB = NPC.localAI[2];
                                    // 在6个方向生成血弹
                                    for (int i = 0; i < 6; i++)
                                    {
                                        float directionAngle = currentB + i * 60;
                                        GenerateBloodThorn(npc, directionAngle, NPC.Center, 1100f);
                                    }

                                    // b自增2°
                                    NPC.localAI[2] += 3;
                                    if (NPC.localAI[2] >= 360)
                                        NPC.localAI[2] -= 360;

                                    // 播放发射音效
                                    if (NPC.ai[1] % 10 == 0)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.2f }, NPC.Center);
                                    }
                                }

                                // 60帧后进入下一阶段
                                if (NPC.ai[1] >= 80)
                                {
                                    NPC.ai[2] = 4; // 进入阶段4
                                    NPC.ai[1] = 0;
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段4: 递减旋转发射 (60帧)
                            else if (currentPhase == 4)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.ai[1]++;
                                // 每3帧生成一次
                                if (NPC.ai[1] % 3 == 0)
                                {
                                    // 当前角度b
                                    float currentB = NPC.localAI[2];

                                    // 在6个方向生成血弹
                                    for (int i = 0; i < 6; i++)
                                    {
                                        float directionAngle = currentB + i * 60;
                                        GenerateBloodThorn(npc, directionAngle, NPC.Center, 1100f);
                                    }
                                    // b自减2°
                                    NPC.localAI[2] -= 3;
                                    if (NPC.localAI[2] < 0)
                                        NPC.localAI[2] += 360;
                                    // 播放发射音效
                                    if (NPC.ai[1] % 10 == 0)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.2f }, NPC.Center);
                                    }
                                }
                                // 60帧后进入下一阶段
                                if (NPC.ai[1] >= 80)
                                {
                                    NPC.ai[2] = 5; // 进入阶段5
                                    NPC.ai[1] = 0;
                                    NPC.netUpdate = true;
                                }
                            }
                            // 阶段5: 四方向切线发射 (120帧)
                            else if (currentPhase == 5)
                            {
                                NPC.velocity *= 0.95f;
                                NPC.ai[1]++;
                                // 每帧生成
                                if (NPC.ai[1] % 10 == 0)
                                {
                                    // 当前角度c（存储在 localAI[0] 中，这里临时使用不会影响攻击顺序）
                                    float currentC = NPC.localAI[0];

                                    // 在4个方向生成血弹
                                    for (int i = 0; i < 20; i++)
                                    {
                                        float directionAngle = currentC + i * 18;
                                        // 生成两个与半径为200的圆相切的血弹
                                        GenerateTangentialBloodThorns(npc, directionAngle, NPC.Center, 1100f, 900 - 0.0486111f * NPC.ai[1] * NPC.ai[1]);
                                    }


                                    // 播放发射音效
                                    if (NPC.ai[1] % 15 == 0)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.3f }, NPC.Center);
                                    }
                                }
                                // 120帧后结束攻击，返回休息状态
                                if (NPC.ai[1] >= 120)
                                {
                                    NPC.localAI[0] = 1;

                                    NPC.ai[0] = 8; // 返回休息状态
                                    NPC.ai[1] = 0;
                                    NPC.ai[2] = 0;
                                    NPC.ai[3] = 0;

                                    // 结束特效
                                    SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                                    for (int i = 0; i < 30; i++)
                                    {
                                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                            DustID.Blood, 0f, 0f, 0, default, 2.5f);
                                        Main.dust[d].noGravity = true;
                                        Main.dust[d].velocity *= 3f;
                                    }

                                    NPC.netUpdate = true;
                                }
                            }
                            #endregion
                        }
                    }
                    break;
                }
            case 23: // 曲线火焰爆发与暗影镰刀组合攻击
                {
                    if (!AliveCheck(npc, player))
                        break;

                    // 初始化攻击：重置所有相关变量
                    if (NPC.ai[1] == 0)
                    {
                        // 随机选择固定角度和距离
                        NPC.localAI[2] = Main.rand.NextFloat(MathHelper.TwoPi); // 随机角度
                        NPC.ai[3] = Main.rand.NextFloat(400f, 600f); // 随机距离 400-600像素

                        // 重置所有攻击状态变量
                        NPC.ai[2] = 0; // 重置镰刀发射标记

                        // 确保不影响攻击顺序

                        NPC.netUpdate = true;

                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                        // P2阶段特殊初始化
                        if (NPC.localAI[3] > 1)
                        {
                            // 如果是P2阶段，确保攻击参数正确
                            // 可以添加P2特有的初始化代码
                        }
                    }

                    // 计算目标位置：玩家位置 + 固定角度和距离
                    targetPos = player.Center + NPC.localAI[2].ToRotationVector2() * NPC.ai[3];

                    // 使用憎恶原有的移动逻辑
                    Movement(npc, targetPos, 0.5f);

                    // 攻击计时器 - 总共6轮攻击，每轮45帧 = 270帧
                    if (++NPC.ai[1] >= 270)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0] = 8; // 返回休息状态
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.localAI[2] = 0;



                    }
                    else
                    {
                        // 每45帧发射一次曲线火焰爆发
                        if (NPC.ai[1] % 45 == 0 && NPC.ai[1] < 270)
                        {
                            SoundEngine.PlaySound(SoundID.Item14, NPC.Center);

                            if (FargoSoulsUtil.HostCheck)
                            {
                                // 发射曲线火焰爆发
                                int flameCount = NPC.localAI[3] > 1 ? 60 : 40; // P2阶段更多弹幕

                                // 确保火焰爆发只在合适的时机发射
                                for (int j = 0; j < 3; j++)
                                {
                                    for (int i = 0; i < (NPC.localAI[3] > 1 ? 20 : 13); i++) // P2更多火焰弹幕
                                    {
                                        Vector2 vel = NPC.SafeDirectionTo(player.Center).RotatedBy(Math.PI / 6 * (Main.rand.NextDouble() - 0.5) + 2 * Math.PI / 3 * j);
                                        float ai0 = Main.rand.NextFloat(1.06f, 1.08f);
                                        float ai1 = Main.rand.NextFloat(0.05f);
                                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                            ModContent.ProjectileType<AbomShadowFlameburst>(),
                                            FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3),
                                            0f, Main.myPlayer, ai0, ai1);
                                    }
                                }
                            }

                            // 在火焰爆发发射后的22帧（中间点）发射暗影镰刀
                            if (FargoSoulsUtil.HostCheck)
                            {
                                NPC.ai[2] = NPC.ai[1] + 22; // 设置镰刀发射时间
                                NPC.netUpdate = true;
                            }
                        }

                        // 检查是否到了发射暗影镰刀的时间
                        if (FargoSoulsUtil.HostCheck && Math.Abs(NPC.ai[1] - NPC.ai[2]) < 0.5f && NPC.ai[2] > 0)
                        {
                            SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

                            // 发射均匀分布在圆上的暗影镰刀
                            int scytheCount = WorldSavingSystem.MasochistModeReal ? 6 : 4; // Maso模式更多镰刀
                            if (NPC.localAI[3] > 1) // P2阶段增加数量
                                scytheCount += 2;

                            float scytheSpeed = 1f; // 调整速度，避免太快

                            for (int i = 0; i < scytheCount; i++)
                            {
                                Vector2 scytheVel = Vector2.UnitX.RotatedBy(MathHelper.TwoPi / scytheCount * i) * scytheSpeed;
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, scytheVel,
                                    ModContent.ProjectileType<ShadowFlamingScythe>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                            }

                            // 重置标记
                            NPC.ai[2] = 0;
                            NPC.netUpdate = true;
                        }
                    }

                    // 暗影主题视觉效果
                    if (Main.rand.NextBool(5)) // 降低频率，避免过度特效
                    {
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 0f, 0, default, 1.5f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 0.5f;
                    }
                    break;
                }

            case 24: // 新攻击：高空压制射击
                {
                    // 检查玩家是否存活
                    if (!AliveCheck(npc, player))
                        break;

                    NPC.dontTakeDamage = true;

                    // 计算目标位置：玩家头顶600像素
                    Vector2 TargetPos = player.Center - new Vector2(0, 550);

                    // 使用Movement函数保持位置（中等移动速度）
                    if (NPC.Distance(TargetPos) > 50)
                        Movement(npc, TargetPos, 0.8f);
                    else
                        NPC.velocity *= 0.95f; // 接近目标时减速

                    // 计时器：从0到360帧（6秒）
                    NPC.ai[1]++;

                    // 存储发射次数（在ai[2]中）
                    int throwCount = (int)NPC.ai[2];

                    // 存储冥河冥视发射时间（在ai[3]中）
                    int gazeSpawnTime = (int)NPC.ai[3];

                    // 第一阶段：1-4秒（60-240帧），每20帧发射一次
                    if (NPC.ai[1] >= 60 && NPC.ai[1] <= 240)
                    {
                        if (NPC.ai[1] == 60)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AbomSaucer2>(), 0, NPC.whoAmI, 0, 0);
                        }

                        // 每20帧发射一次
                        if ((NPC.ai[1] - 60) % 20 == 0)
                        {
                            // 发射镰刀（模仿case0的发射逻辑）
                            if (FargoSoulsUtil.HostCheck)
                            {
                                float ai0 = NPC.Distance(player.Center) / 30 * 2f;
                                float ai1 = 0f;
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                    NPC.SafeDirectionTo(player.Center) * 30f,
                                    ModContent.ProjectileType<AbomScytheSplit>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                                    0f, Main.myPlayer, ai0, ai1);

                                // 播放发射音效
                                SoundEngine.PlaySound(SoundID.Item71, NPC.Center);

                                // 增加发射计数
                                throwCount++;
                                NPC.ai[2] = throwCount;
                                NPC.netUpdate = true;
                            }
                        }
                        if ((NPC.ai[1] - 60) % 20 == 0 && NPC.ai[1] > gazeSpawnTime + 20)
                        {
                            if (FargoSoulsUtil.HostCheck)
                            {
                                // 冥河冥视的发射逻辑（模仿case0）
                                float rotation = MathHelper.Pi * 1f * AbomStyxGazer.Direction;
                                AbomStyxGazer.Direction *= -1; // 切换方向

                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                    NPC.DirectionTo(player.Center).RotatedBy(rotation * 0.6f),
                                    ModContent.ProjectileType<AbomStyxGazer>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                                    0f, Main.myPlayer, NPC.whoAmI, rotation / 60 * 2);

                                // 更新发射时间
                                gazeSpawnTime = (int)NPC.ai[1];
                                NPC.ai[3] = gazeSpawnTime;
                                NPC.netUpdate = true;
                            }
                        }
                    }
                    // 第二阶段：4-6秒（240-360帧），每10帧发射一次，频率加倍
                    else if (NPC.ai[1] > 240 && NPC.ai[1] <= 360)
                    {
                        // 每10帧发射一次镰刀
                        if ((NPC.ai[1] - 240) % 10 == 0)
                        {
                            if (FargoSoulsUtil.HostCheck)
                            {
                                float ai0 = NPC.Distance(player.Center) / 30 * 2f;
                                float ai1 = 0f;
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                    NPC.SafeDirectionTo(player.Center) * 30f,
                                    ModContent.ProjectileType<AbomScytheSplit>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                                    0f, Main.myPlayer, ai0, ai1);

                                // 播放发射音效
                                SoundEngine.PlaySound(SoundID.Item71, NPC.Center);

                                // 增加发射计数
                                throwCount++;
                                NPC.ai[2] = throwCount;
                                NPC.netUpdate = true;
                            }

                            // 冥河冥视发射频率也增加（每10帧一次）
                            if ((NPC.ai[1] - 240) % 10 == 0 && NPC.ai[1] > gazeSpawnTime + 10)
                            {
                                if (FargoSoulsUtil.HostCheck)
                                {
                                    // 冥河冥视的发射逻辑（模仿case0）
                                    float rotation = MathHelper.Pi * 1f * AbomStyxGazer.Direction;
                                    AbomStyxGazer.Direction *= -1; // 切换方向

                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                        NPC.DirectionTo(player.Center).RotatedBy(rotation * 0.6f),
                                        ModContent.ProjectileType<AbomStyxGazer>(),
                                        FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                                        0f, Main.myPlayer, NPC.whoAmI, rotation / 60 * 2);

                                    // 更新发射时间
                                    gazeSpawnTime = (int)NPC.ai[1];
                                    NPC.ai[3] = gazeSpawnTime;
                                    NPC.netUpdate = true;
                                }
                            }
                        }
                    }
                    //第三阶段4-8s
                    else if (NPC.ai[1] > 360 && NPC.ai[1] <= 480)
                    {
                        // 每5帧发射一次镰刀
                        if ((NPC.ai[1] - 360) % 5 == 0)
                        {
                            if (FargoSoulsUtil.HostCheck)
                            {
                                float ai0 = NPC.Distance(player.Center) / 30 * 2f;
                                float ai1 = 0f;
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                    NPC.SafeDirectionTo(player.Center) * 30f,
                                    ModContent.ProjectileType<AbomScytheSplit>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                                    0f, Main.myPlayer, ai0, ai1);

                                // 播放发射音效
                                SoundEngine.PlaySound(SoundID.Item71, NPC.Center);

                                // 增加发射计数
                                throwCount++;
                                NPC.ai[2] = throwCount;
                                NPC.netUpdate = true;
                            }

                            // 冥河冥视发射频率也增加（每5帧一次）
                            if ((NPC.ai[1] - 360) % 5 == 0 && NPC.ai[1] > gazeSpawnTime + 5)
                            {
                                if (FargoSoulsUtil.HostCheck)
                                {
                                    // 冥河冥视的发射逻辑（模仿case0）
                                    float rotation = MathHelper.Pi * 1f * AbomStyxGazer.Direction;
                                    AbomStyxGazer.Direction *= -1; // 切换方向

                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                        NPC.DirectionTo(player.Center).RotatedBy(rotation * 0.6f),
                                        ModContent.ProjectileType<AbomStyxGazer>(),
                                        FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage),
                                        0f, Main.myPlayer, NPC.whoAmI, rotation / 60 * 2);

                                    // 更新发射时间
                                    gazeSpawnTime = (int)NPC.ai[1];
                                    NPC.ai[3] = gazeSpawnTime;
                                    NPC.netUpdate = true;
                                }
                            }
                        }
                    }
                    // 攻击结束时（8秒后）的过渡
                    if (NPC.ai[1] > 480)
                    {
                        NPC.netUpdate = true;

                        // 重置所有参数

                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.ai[0]++; // 返回到休息/选择状态

                        NPC.TargetClosest();


                    }
                }
                break;
            case 25: //尾杀双刃莱瓦汀12开始
                NPC.velocity *= 0.9f;

                ClearFrozen();

                if (NPC.ai[1] < 60)
                    FancyFireballs((int)NPC.ai[1]);

                if (NPC.ai[1] == 0 && NPC.ai[2] != 2 && FargoSoulsUtil.HostCheck)
                {
                    float ai1 = NPC.ai[2] == 1 ? -1 : 1;
                    ai1 *= MathHelper.ToRadians(270) / 120 * -1 * 60; //spawning offset of sword below
                    int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 3, ai1);
                    if (p != Main.maxProjectiles)
                    {
                        Main.projectile[p].localAI[1] = NPC.whoAmI;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncProjectile, number: p);
                    }
                }
                if (++NPC.ai[1] > 90)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                    NPC.velocity = NPC.SafeDirectionTo(player.Center) * 3f;
                }
                else if (NPC.ai[1] == 60 && FargoSoulsUtil.HostCheck)
                {
                    NPC.netUpdate = true;
                    NPC.velocity = Vector2.Zero;

                    //SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    float ai0 = NPC.ai[2] == 1 ? -1 : 1;
                    ai0 *= MathHelper.ToRadians(300) / 120;//角速度
                    Vector2 vel = NPC.SafeDirectionTo(player.Center).RotatedBy(-2.35619449f * Math.Sign(ai0));//135°初始角度
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<AbomSword2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0 * 2 / 3, NPC.whoAmI, ai2: 1);
                    if (WorldSavingSystem.MasochistModeReal)
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -vel, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI, ai2: 1);
                }
                break;
            case 26: //while dashing
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
                if (++NPC.ai[1] > 120)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                }
                break;
            case 27: //wait for scythes to clear
                if (!AliveCheck(npc, player))
                    break;

                ClearFrozen();

                targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 300;
                if (NPC.Distance(targetPos) > 50)
                    Movement(npc, targetPos, 0.7f);
                if (++NPC.ai[1] > 30) // || (NPC.dontTakeDamage && NPC.ai[1] > 30))
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] = 34;
                    NPC.ai[1] = 0;
                }
                break;
            case 28: //禁忌莱瓦汀复刻开始
                {
                    if (NPC.ai[1] < 90 && !AliveCheck(npc, player))
                        break;

                    ClearFrozen();

                    if (NPC.ai[2] == 0 && NPC.ai[3] == 0) //target one side of arena
                    {
                        NPC.netUpdate = true;
                        NPC.ai[2] = player.Center.X;
                        NPC.ai[3] = player.Center.Y;
                        if (FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) != null)
                        {
                            NPC.ai[2] = Main.projectile[ritualProj].Center.X;
                            NPC.ai[3] = Main.projectile[ritualProj].Center.Y;
                        }

                        Vector2 offset;
                        offset.X = Math.Sign(player.Center.X - NPC.ai[2]);
                        offset.Y = Math.Sign(player.Center.Y - NPC.ai[3]);
                        NPC.localAI[2] = offset.ToRotation();
                    }

                    Vector2 actualTargetPositionOffset = (float)Math.Sqrt(2 * 1200 * 1200) * NPC.localAI[2].ToRotationVector2();
                    actualTargetPositionOffset.X -= 450 * Math.Sign(actualTargetPositionOffset.X);

                    targetPos = new Vector2(NPC.ai[2], NPC.ai[3]) + actualTargetPositionOffset;
                    Movement(npc, targetPos, 1f);

                    if (NPC.ai[1] == 0 && FargoSoulsUtil.HostCheck)
                    {
                        float horizontalModifier = Math.Sign(NPC.ai[2] - targetPos.X);
                        float verticalModifier = Math.Sign(NPC.ai[3] - targetPos.Y);

                        float startRotation = verticalModifier > 0 ? MathHelper.ToRadians(0.1f) * -verticalModifier : MathHelper.Pi - MathHelper.ToRadians(0.1f) * -verticalModifier;
                        float ai1 = verticalModifier < 0 ? MathHelper.Pi / 2 : MathHelper.Pi * 3 / 2;
                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, startRotation.ToRotationVector2(), ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 4, ai1);
                        if (p != Main.maxProjectiles)
                        {
                            Main.projectile[p].localAI[1] = NPC.whoAmI;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncProjectile, number: p);
                        }
                    }

                    if (NPC.ai[1] > 90)
                        FancyFireballs((int)NPC.ai[1] - 90);

                    if (++NPC.ai[1] > 150)
                    {
                        NPC.netUpdate = true;
                        NPC.velocity = Vector2.Zero;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                    }
                    *//*else if (NPC.ai[1] == 180 || (NPC.dontTakeDamage && NPC.ai[1] == 120))
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        if (FargoSoulsUtil.HostCheck)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.UnitX * NPC.localAI[2], ModContent.ProjectileType<AbomDeathraySmall2>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                    }*//*
                }
                break;
            case 29: //prepare to dash
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.ai[2] - NPC.Center.X);

                ClearFrozen();
                SpinTime = 60;
                if (NPC.ai[1] == 0)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        float horizontalModifier = Math.Sign(NPC.ai[2] - NPC.Center.X);
                        float verticalModifier = Math.Sign(NPC.ai[3] - NPC.Center.Y);

                        float ai0 = -1f * horizontalModifier * MathHelper.Pi / SpinTime * verticalModifier;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -verticalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI);
                        if (WorldSavingSystem.MasochistModeReal)
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.UnitY * -verticalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI);
                    }
                }

                if (++NPC.ai[1] > SpinTime)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;

                    NPC.velocity.X = 24 * Math.Sign(NPC.ai[2] - NPC.Center.X);//向仪式圈中心;
                    NPC.velocity.Y = 0f;
                }
                break;

            case 30: //while dashing down

                ClearFrozen();

                NPC.velocity.X *= 0.97f;
                NPC.position += NPC.velocity;
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.ai[2] - NPC.Center.X);
                if (++NPC.ai[1] > 90)
                {
                    NPC.velocity.X = 0;
                    NPC.ai[0]++;
                    NPC.netUpdate = true;
                    NPC.ai[1] = 0;
                }
                break;
            case 31: //prepare to dash
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.ai[2] - NPC.Center.X);

                ClearFrozen();
                SpinTime = 60;
                if (NPC.ai[1] == 0)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        float horizontalModifier = Math.Sign(NPC.ai[2] - NPC.Center.X);
                        float verticalModifier = Math.Sign(NPC.ai[3] - NPC.Center.Y);

                        float ai0 = -horizontalModifier * MathHelper.Pi / SpinTime * verticalModifier;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -verticalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI);
                        if (WorldSavingSystem.MasochistModeReal)
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.UnitY * -verticalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI);
                    }
                }

                if (++NPC.ai[1] > SpinTime)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;

                    NPC.velocity.X = 24 * Math.Sign(NPC.ai[2] - NPC.Center.X);//向仪式圈中心;
                    NPC.velocity.Y = 0f;
                }
                break;
            case 32: //while dashing down第二轮结束,莱瓦汀开始；

                ClearFrozen();

                NPC.velocity.X *= 0.97f;
                NPC.position += NPC.velocity;
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.ai[2] - NPC.Center.X);
                if (++NPC.ai[1] > 90)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                }
                break;
            case 33: //莱瓦汀旋转
                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.ai[2] - NPC.Center.X);

                ClearFrozen();
                SpinTime = 150;
                if (NPC.ai[1] == 0)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        float horizontalModifier = Math.Sign(NPC.ai[2] - NPC.Center.X);
                        float verticalModifier = Math.Sign(NPC.ai[3] - NPC.Center.Y);
                        NPC.velocity.X = horizontalModifier * 2f;
                        NPC.velocity.Y = verticalModifier * 2f;
                        float ai0 = -horizontalModifier * MathHelper.Pi / 50 * verticalModifier;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -verticalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI);

                    }
                }

                if (++NPC.ai[1] > SpinTime)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] = 35;
                    NPC.ai[1] = 0;
                }
                break;
            case 34://尾杀间招
                if (!AliveCheck(npc, player))
                    break;
                if (NPC.ai[2] == 1)
                {
                    NPC.ai[0] = 28;
                    NPC.ai[2] = 0;
                    break;
                }
                ClearFrozen();

                targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 300;
                if (NPC.Distance(targetPos) > 50)
                    Movement(npc, targetPos, 0.7f);
                if (NPC.ai[1] == 0 && NPC.ai[2] == 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AbomSaucer2>(), 0, NPC.whoAmI, 0, i * 2 * MathHelper.Pi / 2, 0);
                    }
                }
                if (++NPC.ai[1] > 360)
                {
                    NPC.netUpdate = true;
                    NPC.ai[1] = 0;
                    if (++NPC.ai[2] < 2)
                    {
                        NPC.ai[0] = 25;
                    }
                }
                break;

            case 35:
                if (!AliveCheck(npc, player))
                    break;
                NPC.localAI[2] = 0;
                targetPos = player.Center;
                targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
                if (NPC.Distance(targetPos) > 50)
                    Movement(npc, targetPos, 0.7f);
                if (++NPC.ai[1] > 60)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] = -4;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                }
                break;
            default:
                Main.NewText("UH OH, STINKY");
                NPC.netUpdate = true;
                NPC.ai[0] = 0;
                goto case 0;
        }*/
        #endregion
    }
}