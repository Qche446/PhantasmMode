using FargosPhantasmMode.Content.Bossbar;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Content.Bosses.Champions.Will;
using FargowiltasSouls.Content.Bosses.Lifelight;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Summons;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class MutantBossOverride : PModeNPCBehaviour
    {
        public override bool InstancePerEntity => true;

        public bool playerInvulTriggered;
        public SlotId? TelegraphSound = null;
        public int ritualProj, spriteProj, ringProj;
        private bool droppedSummon = false;

        public Queue<float> attackHistory = new();
        public int attackCount;

        public int hyper;

        public float endTimeVariance;

        public bool ShouldDrawAura;
        public float AuraScale = 1f;

        public Vector2 AuraCenter = Vector2.Zero;

        private string TownNPCName;

        public const int HyperMax = 5;
        //后来用的
        public bool FirstSword = true;
        public Vector2 SansOldPos = Vector2.Zero;
        public Vector2 LieFlightPos = Vector2.Zero;//唐飞炸弹

        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(ModContent.NPCType<MutantBoss>());
        public override void SetDefaults(NPC npc)
        {
            npc.lifeMax = 12000000;
            npc.damage = 444 + 44;
            npc.defense = 255;
            npc.BossBar = ModContent.GetInstance<PhantasmBossBar>();
        }
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (ModContent.TryFind("Fargowiltas", "Mutant", out ModNPC modNPC))
            {
                int n = NPC.FindFirstNPC(modNPC.Type);
                if (n != -1 && n != Main.maxNPCs)
                {
                    npc.Bottom = Main.npc[n].Bottom;
                    TownNPCName = Main.npc[n].GivenName;

                    Main.npc[n].life = 0;
                    Main.npc[n].active = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                }
            }
            AuraCenter = npc.Center;
            if (npc.ModNPC is MutantBoss mutant)
            {
                //mutant.Music = MusicLoader.GetMusicSlot("FargosPhantasmMode/Assets/Music/HeartofGuardian");
            }
        }

        public override bool SafePreAI(NPC npc)
        {
            if (WorldSavingSystem.MasochistModeReal && !Main.dedServ)
            {
                if (!Main.LocalPlayer.ItemTimeIsZero && (Main.LocalPlayer.HeldItem.type == ItemID.RodofDiscord || Main.LocalPlayer.HeldItem.type == ItemID.RodOfHarmony))
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<TimeFrozenBuff>(), 600);
            }
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();
            Player player = Main.player[npc.target];
            MutantAI(npc, player);
            return false;
        }

        public void MutantAI(NPC npc, Player player)
        {
            EModeGlobalNPC.mutantBoss = npc.whoAmI;
            npc.dontTakeDamage = npc.ai[0] < 0;
            // Set this to false by default.
            ShouldDrawAura = false;

            ManageAurasAndPreSpawn(npc, player);
            ManageNeededProjectiles(npc);

            npc.direction = npc.spriteDirection = npc.Center.X < player.Center.X ? 1 : -1;

            bool drainLifeInP3 = true;

            if (TelegraphSound != null)
            {
                if (SoundEngine.TryGetActiveSound(TelegraphSound.Value, out ActiveSound s))
                {
                    s.Position = npc.Center;
                }
            }
            switch ((int)npc.ai[0])
            {
                #region phase 1

                case 0: SpearTossDirectP1AndChecks(npc, player); break;

                case 1: OkuuSpheresP1(npc); break;

                case 2: PrepareTrueEyeDiveP1(npc, player); break;
                case 3: TrueEyeDive(npc, player); break;

                case 4: PrepareSpearDashDirectP1(npc, player); break;
                case 5: SpearDashDirectP1(npc, player); break;
                case 6: WhileDashingP1(npc, player); break;

                case 7: ApproachForNextAttackP1(npc, player); break;
                case 8: VoidRaysP1(npc, player); break;

                case 9: BoundaryBulletHellAndSwordP1(npc, player); break;

                #endregion phase 1

                #region phase 2

                case 10: Phase2Transition(npc, player); break;

                case 11: ApproachForNextAttackP2(npc, player); break;
                case 12: LieFlightBomb(npc, player); break;

                case 13: PrepareSpearDashPredictiveP2(npc, player); break;
                case 14: SpearDashPredictiveP2(npc, player); break;
                case 15: WhileDashingP2(npc, player); break;

                case 16: goto case 11; //approach for bullet hell
                case 17: BoundaryBulletHellP2(npc, player); break;

                case 18: VoidRaysP2(npc); break; //虚无射线//概率由唐飞炸弹置换

                case 19: PillarDunk(npc, player); break;

                case 20: EOCStarSickles(npc, player); break;

                case 21: PrepareSpearDashDirectP2(npc, player); break;
                case 22: SpearDashDirectP2(npc, player); break;
                case 23: //while dashing
                    if (npc.ai[1] % 3 == 0)
                        npc.ai[1]++;
                    goto case 15;

                case 24: SpawnDestroyersForPredictiveThrow(npc, player); break;
                case 25: SpearTossPredictiveP2(npc, player); break;

                case 26: PrepareMechRayFan(npc, player); break;
                case 27: MechRayFan(npc, player); break;

                case 28: CoffinWave(npc, player); break; //free slot for new attack//意志攻击

                case 29: PrepareFishron1(npc, player); break;
                case 30: SpawnFishrons(npc); break;

                case 31: PrepareTrueEyeDiveP2(npc, player); break;
                case 32: goto case 3; //spawn eyes

                case 33: PrepareNuke(npc, player); break;
                case 34: Nuke(npc, player); break;

                case 35: PrepareSlimeRain(npc, player); break;
                case 36: SlimeRain(npc, player); break;

                case 37: PrepareFishron2(npc, player); break;
                case 38: goto case 30; //spawn fishrons

                case 39: PrepareOkuuSpheresP2(npc, player); break;
                case 40: OkuuSpheresP2(npc); break;

                case 41: SpearTossDirectP2(npc, player); break;

                case 42: PrepareTwinRangsAndCrystals(npc, player); break;
                case 43: TwinRangsAndCrystals(npc, player); break;

                case 44: EmpressSwordWave(npc, player); break;

                case 45: PrepareMutantSword(npc, player); break;
                case 46: MutantSword(npc, player); break;

                case 47: goto case 35;
                case 48: QueenSlimeRain(npc, player); break;

                case 49: SANSGOLEM(npc, player); break;

                case 50: IronVirgin(npc, player); break;

                //gap in the numbers here so the ai loops right
                //when adding a new attack, remember to make ChooseNextAttack() point to the right case!

                case 52: P2NextAttackPause(npc, player); break;

                #endregion phase 2

                #region phase 3

                case -1: drainLifeInP3 = Phase3Transition(npc, player); break;

                case -2: VoidRaysP3(npc); break;

                case -3: OkuuSpheresP3(npc, player); break;

                case -4: BoundaryBulletHellP3(npc, player); break;

                case -5: FinalSpark(npc, player); break;

                case -6: DyingDramaticPause(npc, player); break;
                case -7: DyingAnimationAndHandling(npc); break;

                #endregion phase 3

                default: npc.ai[0] = 11; goto case 11; //return to first phase 2 attack
            }

            #region 杂项

            //manage aura scale
            if (npc.ai[0] == 1) //ooku spheres p1
            {
                AuraScale = MathHelper.Lerp(AuraScale, 0.7f, 0.02f);
            }
            else if (npc.ai[0] == 5 || npc.ai[0] == 6)
            {
                AuraScale = MathHelper.Lerp(AuraScale, 1.25f, 0.1f);
            }
            else
            {
                AuraScale = MathHelper.Lerp(AuraScale, 1f, 0.1f);
            }
            //manage arena position
            if (!WorldSavingSystem.MasochistModeReal || npc.ai[0] != 5 && npc.ai[0] != 6) //spear dash direct p1
            {
                AuraCenter = Vector2.Lerp(AuraCenter, npc.Center, 0.3f);
            }
            //in emode p2
            if (WorldSavingSystem.EternityMode && (npc.ai[0] < 0 || npc.ai[0] > 10 || npc.ai[0] == 10 && npc.ai[1] > 150))
            {
                Main.dayTime = false;
                Main.time = 16200; //midnight, for empress visuals

                Main.raining = false; //disable rain
                Main.rainTime = 0;
                Main.maxRaining = 0;

                Main.bloodMoon = false; //disable blood moon
            }

            if (npc.ai[0] < 0 && npc.life > 1 && drainLifeInP3) //in desperation
            {
                int time = 480 + 240 + 420 + 480 + 1020 - 60;
                if (WorldSavingSystem.MasochistModeReal)
                    time = Main.getGoodWorld ? 5000 : 4350;
                int drain = npc.lifeMax / time;
                npc.life -= drain;
                if (npc.life < 1)
                    npc.life = 1;
            }

            if (player.immune || player.hurtCooldowns[0] != 0 || player.hurtCooldowns[1] != 0)
                playerInvulTriggered = true;
            //drop summon
            if (WorldSavingSystem.EternityMode && WorldSavingSystem.DownedAbom && !WorldSavingSystem.DownedMutant && FargoSoulsUtil.HostCheck && npc.HasPlayerTarget && !droppedSummon)
            {
                Item.NewItem(npc.GetSource_Loot(), player.Hitbox, ModContent.ItemType<MutantsCurse>());
                droppedSummon = true;
            }

            if (WorldSavingSystem.MasochistModeReal && Main.getGoodWorld && ++hyper > MutantBoss.HyperMax + 1)
            {
                hyper = 0;
                MutantAI(npc, player);
            }

            #endregion 杂项
        }

        #region P1

        private void SpearTossDirectP1AndChecks(NPC npc, Player player)//0蠕虫预判投矛
        {
            if (!AliveCheck(npc, player))
                return;
            if (Phase2Check(npc))
                return;
            npc.localAI[2] = 0;
            Vector2 targetPos = player.Center;
            targetPos.X += 500 * (npc.Center.X < targetPos.X ? -1 : 1);
            if (npc.Distance(targetPos) > 50)
            {
                Movement(npc, targetPos, npc.localAI[3] > 0 ? 0.5f : 2f, true, npc.localAI[3] > 0);
            }

            if (npc.ai[3] == 0)
            {
                npc.ai[3] = WorldSavingSystem.MasochistModeReal ? Main.rand.Next(2, 8) : 5;
                npc.netUpdate = true;
            }

            if (npc.localAI[3] > 0) //dont begin proper ai timer until in range to begin fight
                npc.ai[1]++;

            if (npc.ai[1] < 145) //track player up until just before attack
            {
                npc.localAI[0] = npc.SafeDirectionTo(player.Center + player.velocity * 30f).ToRotation();
            }

            if (npc.ai[1] > 150) //120)
            {
                npc.netUpdate = true;
                //NPC.TargetClosest();
                npc.ai[1] = 60;
                if (++npc.ai[2] > npc.ai[3])
                {
                    P1NextAttackOrMasoOptions(npc, npc.ai[0]);
                    npc.velocity = npc.SafeDirectionTo(player.Center) * 2f;
                }
                else if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 vel = npc.localAI[0].ToRotationVector2() * 25f;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<PHMutantSpearThrown>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.target);
                    if (WorldSavingSystem.MasochistModeReal)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, -Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    }
                }
                //控制ph幻影球
                for (int j = 0; j <= Main.maxProjectiles; j++)
                {
                    if (Main.projectile[j].type == ModContent.ProjectileType<PHMutantSphereSmall>() && Main.projectile[j].active && Main.projectile[j].ai[1] >= 30)
                    {
                        Main.projectile[j].ai[1] = 180;
                    }
                }
                npc.localAI[0] = 0;
            }
            else if (npc.ai[1] == 61 && npc.ai[2] < npc.ai[3] && FargoSoulsUtil.HostCheck)
            {
                if (WorldSavingSystem.EternityMode && WorldSavingSystem.SkipMutantP1 >= 10 && !WorldSavingSystem.MasochistModeReal)
                {
                    npc.ai[0] = 10; //skip to phase 2
                    npc.ai[1] = 0;
                    npc.ai[2] = 0;
                    npc.ai[3] = 0;
                    npc.localAI[0] = 0;
                    npc.netUpdate = true;

                    if (WorldSavingSystem.SkipMutantP1 == 10)
                        FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.NPCs.MutantBoss.SkipP1", Color.LimeGreen);

                    if (WorldSavingSystem.SkipMutantP1 >= 10)
                        npc.ai[2] = 1; //flag for different p2 transition animation

                    return;
                }

                if (WorldSavingSystem.MasochistModeReal && npc.ai[2] == 0) //first time only
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath13, npc.Center);
                    if (FargoSoulsUtil.HostCheck) //spawn worm
                    {
                        int appearance = Main.rand.Next(2);
                        if (FargoSoulsUtil.AprilFools)
                            appearance = 0;
                        for (int j = 0; j < 8; j++)
                        {
                            Vector2 vel = npc.DirectionFrom(player.Center).RotatedByRandom(MathHelper.ToRadians(120)) * 10f;
                            float ai1 = 0.8f + 0.4f * j / 5f;
                            int current = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MutantDestroyerHead>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.target, ai1, appearance);
                            //timeleft: remaining duration of this case + extra delay after + successive death
                            Main.projectile[current].timeLeft = 90 * ((int)npc.ai[3] + 1) + 30 + j * 6;
                            int max = Main.rand.Next(8, 19);
                            for (int i = 0; i < max; i++)
                                current = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MutantDestroyerBody>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, Main.projectile[current].identity, 0f, appearance);
                            int previous = current;
                            current = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MutantDestroyerTail>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, Main.projectile[current].identity, 0f, appearance);
                            Main.projectile[previous].localAI[1] = Main.projectile[current].identity;
                            Main.projectile[previous].netUpdate = true;
                        }
                    }
                }

                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.SafeDirectionTo(player.Center + player.velocity * 30f), ModContent.ProjectileType<MutantDeathrayAim>(), 0, 0f, Main.myPlayer, 85f, npc.whoAmI);
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, npc.whoAmI, 3);

                //Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);
            }
        }

        private void OkuuSpheresP1(NPC NPC)//1P1阿空圆环
        {
            if (Phase2Check(NPC))
                return;

            if (WorldSavingSystem.MasochistModeReal)
                NPC.velocity = Vector2.Zero;
            if (--NPC.ai[1] < 0)
            {
                NPC.netUpdate = true;
                float modifier = WorldSavingSystem.MasochistModeReal ? 6 : 2;
                NPC.ai[1] = 90 / modifier;
                if (++NPC.ai[2] > 4 * modifier)
                {
                    if (!WorldSavingSystem.MasochistModeReal || NPC.ai[2] > 6 * modifier) //extra endtime in maso
                    {
                        P1NextAttackOrMasoOptions(NPC, NPC.ai[0]);
                    }
                }
                else
                {
                    EdgyBossText(NPC, RandomObnoxiousQuote());

                    int max = WorldSavingSystem.MasochistModeReal ? 9 : 6;
                    float speed = WorldSavingSystem.MasochistModeReal ? 10 : 9;
                    int sign = WorldSavingSystem.MasochistModeReal ? NPC.ai[2] % 2 == 0 ? 1 : -1 : 1;
                    SpawnSphereRing(NPC, max, speed, (int)(0.8 * FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage)), 1f * sign);
                    SpawnSphereRing(NPC, max, speed, (int)(0.8 * FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage)), -0.5f * sign);
                }
            }
        }

        private void PrepareTrueEyeDiveP1(NPC NPC, Player player)//2准备真眼俯冲
        {
            if (!AliveCheck(NPC, player))
                return;
            if (Phase2Check(NPC))
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 700 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y -= 400;
            Movement(NPC, targetPos, 0.6f);
            if (NPC.Distance(targetPos) < 50 || ++NPC.ai[1] > 180) //dive here
            {
                NPC.velocity.X = 35f * (NPC.position.X < player.position.X ? 1 : -1);
                if (NPC.velocity.Y < 0)
                    NPC.velocity.Y *= -1;
                NPC.velocity.Y *= 0.3f;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                EdgyBossText(NPC, RandomObnoxiousQuote());
            }
        }

        private void TrueEyeDive(NPC NPC, Player player)//3真眼俯冲
        {
            if (NPC.ai[3] == 0)
                NPC.ai[3] = Math.Sign(NPC.Center.X - player.Center.X);

            if (NPC.ai[2] > 3)
            {
                Vector2 targetPos = player.Center;
                targetPos.X += NPC.Center.X < player.Center.X ? -500 : 500;
                if (NPC.Distance(targetPos) > 50)
                    Movement(NPC, targetPos, 0.3f);
            }
            else
            {
                NPC.velocity *= 0.99f;
            }

            if (--NPC.ai[1] < 0)
            {
                NPC.ai[1] = 15;
                int maxEyeThreshold = WorldSavingSystem.MasochistModeReal ? 6 : 3;
                int endlag = WorldSavingSystem.MasochistModeReal ? 3 : 5;
                if (++NPC.ai[2] > maxEyeThreshold + endlag)
                {
                    if (NPC.ai[0] == 3)
                        P1NextAttackOrMasoOptions(NPC, 2);
                    else
                        ChooseNextAttack(NPC, 13, 19, 21, 24, 33, 33, 33, 39, 41, 44, 50);
                }
                else if (NPC.ai[2] <= maxEyeThreshold)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int type;
                        float ratio = NPC.ai[2] / maxEyeThreshold * 3;
                        if (ratio <= 1f)
                            type = ModContent.ProjectileType<PHMutantTrueEyeL>();
                        else if (ratio <= 2f)
                            type = ModContent.ProjectileType<MutantTrueEyeS>();
                        else
                            type = ModContent.ProjectileType<MutantTrueEyeR>();

                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, type, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer, NPC.target);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -6 * Vector2.UnitY, ModContent.ProjectileType<PHMutantEyeHoming>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer, NPC.target, 150 + Main.rand.Next(75));
                        if (p != Main.maxProjectiles) //inform them which side attack began on
                        {
                            Main.projectile[p].localAI[1] = NPC.ai[3]; //this is ok, they sync this
                            Main.projectile[p].netUpdate = true;
                        }
                    }
                    SoundEngine.PlaySound(SoundID.Item92, NPC.Center);
                    for (int i = 0; i < 30; i++)
                    {
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceTorch, 0f, 0f, 0, default, 3f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 12f;
                    }
                }
            }
        }

        private void PrepareSpearDashDirectP1(NPC NPC, Player player)//4青冲预备
        {
            if (Phase2Check(NPC))
                return;
            if (NPC.ai[3] == 0)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.ai[3] = 1;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearSpin>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 240); // 250);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearSpin>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 240); // 250);
                    TelegraphSound = SoundEngine.PlaySound(FargosSoundRegistry.MutantUnpredictive with { Volume = 2f }, NPC.Center);
                }

                EdgyBossText(NPC, GFBQuote(4));
            }

            if (++NPC.ai[1] > 240)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.ai[0]++;
                NPC.ai[3] = 0;
                NPC.netUpdate = true;
            }

            Vector2 targetPos = player.Center;
            if (NPC.Top.Y < player.Bottom.Y)
                targetPos.X += 600f * Math.Sign(NPC.Center.X - player.Center.X);
            targetPos.Y += 400f;
            Movement(NPC, targetPos, 0.7f, false);
        }

        private void SpearDashDirectP1(NPC NPC, Player player)//5P1青冲
        {
            if (Phase2Check(NPC))
                return;
            NPC.velocity *= 0.9f;

            if (NPC.ai[3] == 0)
                NPC.ai[3] = WorldSavingSystem.MasochistModeReal ? Main.rand.Next(3, 15) : 10;

            if (++NPC.ai[1] > NPC.ai[3])
            {
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                if (++NPC.ai[2] > 5)
                {
                    P1NextAttackOrMasoOptions(NPC, 4); //go to next attack after dashes
                }
                else
                {
                    float speed = WorldSavingSystem.MasochistModeReal ? 45f : 30f;
                    NPC.velocity = speed * NPC.SafeDirectionTo(player.Center + player.velocity);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearDash>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);

                        if (WorldSavingSystem.MasochistModeReal)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                        }
                    }

                    EdgyBossText(NPC, GFBQuote(5));
                }
            }
        }

        private void WhileDashingP1(NPC NPC, Player player)
        {
            NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
            if (++NPC.ai[1] > 30)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.netUpdate = true;
                NPC.ai[0]--;
                NPC.ai[1] = 0;
            }
        }//6冲刺中

        private void ApproachForNextAttackP1(NPC NPC, Player player)//7靠近
        {
            if (!AliveCheck(NPC, player))
                return;
            if (Phase2Check(NPC))
                return;
            Vector2 targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 250;
            if (NPC.Distance(targetPos) > 50 && ++NPC.ai[2] < 180)
            {
                Movement(NPC, targetPos, 0.5f);
            }
            else
            {
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.ai[2] = player.SafeDirectionTo(NPC.Center).ToRotation();
                NPC.ai[3] = (float)Math.PI / 10f;
                if (player.Center.X < NPC.Center.X)
                    NPC.ai[3] *= -1;
            }
        }//

        private void VoidRaysP1(NPC NPC, Player player)//8虚无射线P1
        {
            if (Phase2Check(NPC))
                return;
            NPC.velocity = Vector2.Zero;
            if (--NPC.ai[1] < 0)
            {
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(9 - 0 * Math.Abs(NPC.ai[2]) / MathHelper.PiOver2, 0).RotatedBy(NPC.ai[2]), ModContent.ProjectileType<PHMutantMark1>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, MathHelper.Pi / 3);
                NPC.ai[1] = WorldSavingSystem.MasochistModeReal ? 3 : 5; //delay between projs
                NPC.ai[2] += NPC.ai[3];
                if (NPC.localAI[0]++ == 20 || NPC.localAI[0] == 40)
                {
                    NPC.netUpdate = true;
                    NPC.ai[2] -= NPC.ai[3] / (WorldSavingSystem.MasochistModeReal ? 3 : 2);

                    EdgyBossText(NPC, GFBQuote(6));
                }
                else if (NPC.localAI[0] >= (WorldSavingSystem.MasochistModeReal ? 60 : 40))
                {
                    P1NextAttackOrMasoOptions(NPC, 7);
                }
            }
        }

        private const int MUTANT_SWORD_SPACING = 100;
        private const int MUTANT_SWORD_MAX = 16;

        private void BoundaryBulletHellAndSwordP1(NPC NPC, Player player)
        {
            switch ((int)NPC.localAI[2])
            {
                case 0: //boundary lite
                    if (NPC.ai[3] == 0)
                    {
                        if (AliveCheck(NPC, player))
                        {
                            NPC.ai[3] = 1;
                            NPC.localAI[0] = Math.Sign(NPC.Center.X - player.Center.X);
                        }
                        else
                        {
                            break;
                        }

                        EdgyBossText(NPC, GFBQuote(7));
                    }

                    if (Phase2Check(NPC))
                        return;

                    NPC.velocity = Vector2.Zero;

                    //if (WorldSavingSystem.MasochistModeReal && NPC.ai[3] >= 300) //spear barrage
                    //{
                    //    if (NPC.ai[3] == 0)
                    //    {
                    //        NPC.ai[1] = 0;
                    //        NPC.ai[2] = Main.rand.NextFloat(MathHelper.TwoPi);
                    //        NPC.localAI[0] = player.Center.X;
                    //        NPC.localAI[1] = player.Center.Y;
                    //    }

                    //    const int spearsToMake = 18;
                    //    const int timeToFullSpears = 180;
                    //    const int timeGap = timeToFullSpears / spearsToMake;
                    //    if (NPC.ai[3] % timeGap == 0 && NPC.ai[3] < 300 + timeToFullSpears)
                    //    {
                    //        NPC.ai[2] += MathHelper.TwoPi / spearsToMake * ++NPC.ai[1];

                    //        Vector2 target = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                    //        Vector2 spawnpos = target + 600 * NPC.ai[2].ToRotationVector2();

                    //        if (FargoSoulsUtil.HostCheck)
                    //        {
                    //        }
                    //    }
                    //}
                    //else
                    if (++NPC.ai[1] > 2) //boundary
                    {
                        SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                        NPC.ai[1] = 0;
                        //ai3 - 300 so that when attack ends, the projs will behave like at start of attack normally (straight streams)
                        NPC.ai[2] += WorldSavingSystem.MasochistModeReal //maso uses true boundary
                                ? (float)Math.PI / 8 / 480 * (NPC.ai[3] - 300) * NPC.localAI[0]
                                : MathHelper.Pi / 77f;

                        if (FargoSoulsUtil.HostCheck)
                        {
                            int max = WorldSavingSystem.MasochistModeReal ? 7 : 6;
                            for (int i = 0; i < max; i++)
                            {
                                float vel = 6.5f;
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0f, -vel).RotatedBy(NPC.ai[2] + MathHelper.TwoPi / max * i),
                                    ModContent.ProjectileType<PHMutantEye>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                            }
                        }
                    }

                    if (++NPC.ai[3] > (WorldSavingSystem.MasochistModeReal ? 360 : 240))
                    {
                        P1NextAttackOrMasoOptions(NPC, NPC.ai[0]);
                    }
                    break;

                case 1:
                    PrepareMutantSword(NPC, player);
                    break;

                case 2:
                    MutantSword(NPC, player);
                    break;

                default:
                    break;
            }
        }//9P1波粒+突变剑

        private void PrepareMutantSword(NPC NPC, Player player)//准备突变剑
        {
            if (NPC.ai[0] == 9 && Main.LocalPlayer.active && NPC.Distance(Main.LocalPlayer.Center) < 3000f && Main.expertMode)
                Main.LocalPlayer.AddBuff(ModContent.BuffType<PurgedBuff>(), 2);

            //can alternate directions
            int sign = NPC.ai[0] != 9 && NPC.localAI[2] % 2 == 1 ? -1 : 1;

            if (NPC.ai[2] == 0) //move onscreen so player can see
            {
                if (!AliveCheck(NPC, player))
                    return;

                Vector2 targetPos = player.Center;
                float shouldX = 420 * Math.Sign(NPC.Center.X - player.Center.X);
                targetPos.X += (NPC.ai[0] == 9 || FirstSword) ? shouldX : -shouldX;//反向
                targetPos.Y -= 210 * sign;
                if (NPC.ai[0] == 9 || FirstSword)
                {
                    Movement(NPC, targetPos, 1.2f);
                }
                else
                {
                    NPC.Center = targetPos;
                }
                if ((++NPC.localAI[0] > 30 || WorldSavingSystem.MasochistModeReal) && NPC.Distance(targetPos) < 64)
                {
                    NPC.velocity = Vector2.Zero;
                    NPC.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                    NPC.localAI[1] = Math.Sign(player.Center.X - NPC.Center.X);
                    float startAngle = MathHelper.PiOver4 * -NPC.localAI[1];
                    NPC.ai[2] = startAngle * -4f / 20 * sign; //travel the full arc over number of ticks
                    if (sign < 0)
                        startAngle += MathHelper.PiOver2 * -NPC.localAI[1];

                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 offset = Vector2.UnitY.RotatedBy(startAngle) * -MUTANT_SWORD_SPACING;

                        void MakeSword(Vector2 pos, float spacing, float rotation = 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + pos, Vector2.Zero, ModContent.ProjectileType<MutantSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer, NPC.whoAmI, spacing);
                        }

                        for (int i = 0; i < MUTANT_SWORD_MAX; i++)
                        {
                            MakeSword(offset * i, MUTANT_SWORD_SPACING * i);
                        }

                        for (int i = -1; i <= 1; i += 2)
                        {
                            MakeSword(offset.RotatedBy(MathHelper.ToRadians(26.5f * i)), 60 * 3);
                            MakeSword(offset.RotatedBy(MathHelper.ToRadians(40 * i)), 60 * 4f);
                        }
                    }

                    EdgyBossText(NPC, GFBQuote(8));
                }
            }
            else
            {
                NPC.velocity = Vector2.Zero;

                int endtime = 90;
                if (NPC.ai[0] != 9 && !FirstSword)
                {
                    endtime = 15;
                }
                FancyFireballs(NPC, (int)(NPC.ai[1] / endtime * 60f));

                if (++NPC.ai[1] > endtime)
                {
                    if (NPC.ai[0] != 9)
                        NPC.ai[0]++;

                    NPC.localAI[2]++; //progresses state in p1, counts swings in p2
                    Vector2 targetPos = player.Center;
                    targetPos.X -= 300 * NPC.ai[2];
                    NPC.velocity = (targetPos - NPC.Center) / 20;
                    NPC.ai[1] = 0;
                    NPC.netUpdate = true;
                }

                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.localAI[1]);
            }
        }

        private void MutantSword(NPC NPC, Player player)//突变剑
        {
            if (NPC.ai[0] == 9 && Main.LocalPlayer.active && NPC.Distance(Main.LocalPlayer.Center) < 3000f && Main.expertMode)
                Main.LocalPlayer.AddBuff(ModContent.BuffType<PurgedBuff>(), 2);

            NPC.ai[3] += NPC.ai[2];
            NPC.direction = NPC.spriteDirection = Math.Sign(NPC.localAI[1]);

            if (NPC.ai[1] == 20)
            {
                if (!Main.dedServ && Main.LocalPlayer.active)
                    ScreenShakeSystem.StartShake(10, shakeStrengthDissipationIncrement: 10f / 30);

                //moon chain explosions
                int explosions = 0;
                if (WorldSavingSystem.EternityMode && NPC.ai[0] != 9 || WorldSavingSystem.MasochistModeReal)
                    explosions = 8;
                else if (WorldSavingSystem.EternityMode)
                    explosions = 5;
                if (explosions > 0)
                {
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(SoundID.Thunder with { Pitch = -0.5f }, NPC.Center);

                    float lookSign = Math.Sign(NPC.localAI[1]);
                    float arcSign = Math.Sign(NPC.ai[2]);
                    Vector2 offset = lookSign * Vector2.UnitX.RotatedBy(MathHelper.PiOver4 * arcSign);

                    const float length = MUTANT_SWORD_SPACING * MUTANT_SWORD_MAX / 2f;
                    Vector2 spawnPos = NPC.Center + length * offset;
                    Vector2 baseDirection = player.DirectionFrom(spawnPos);

                    int max = explosions; //spread
                    for (int i = 0; i < max; i++)
                    {
                        Vector2 angle = baseDirection.RotatedBy(MathHelper.TwoPi / max * i);
                        float ai1 = i <= 2 || i == max - 2 ? 48 : 24;
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos + Main.rand.NextVector2Circular(NPC.width / 2, NPC.height / 2), Vector2.Zero, FargoSoulsUtil.AprilFools ? ModContent.ProjectileType<MoonLordSunBlast>() : ModContent.ProjectileType<MoonLordMoonBlast>(),
                                FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer, MathHelper.WrapAngle(angle.ToRotation()), ai1);
                        }
                    }
                }
            }

            if (++NPC.ai[1] > 25)
            {
                if (NPC.ai[0] == 9)
                {
                    P1NextAttackOrMasoOptions(NPC, NPC.ai[0]);
                }
                else if (WorldSavingSystem.MasochistModeReal && NPC.localAI[2] < 4 * 3 * (endTimeVariance + 0.5))//乘4为补偿间隔缩短
                {
                    NPC.ai[0]--;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    NPC.localAI[1] = 0;
                    NPC.netUpdate = true;
                    FirstSword = false;
                }
                else
                {
                    FirstSword = true;
                    ChooseNextAttack(NPC, 13, 21, 24, 29, 31, 33, 37, 41, 42, 44, 47, 49, 50);
                }
            }
        }

        #endregion P1

        #region P2

        private void Phase2Transition(NPC npc, Player player)//10P2转换
        {
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;

            if (npc.buffType[0] != 0)
                npc.DelBuff(0);

            EModeSpecialEffects(npc);

            if (npc.ai[2] == 0)
            {
                if (npc.ai[1] < 60 && !Main.dedServ && Main.LocalPlayer.active)
                    FargoSoulsUtil.ScreenshakeRumble(6);
            }
            else
            {
                npc.velocity = Vector2.Zero;
            }

            if (npc.ai[1] < 240)
            {
                //make you stop attacking
                if (Main.LocalPlayer.active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost && npc.Distance(Main.LocalPlayer.Center) < 3000)
                {
                    Main.LocalPlayer.controlUseItem = false;
                    Main.LocalPlayer.controlUseTile = false;
                    Main.LocalPlayer.FargoSouls().NoUsingItems = 2;
                }
            }

            if (npc.ai[1] == 0)
            {
                FargoSoulsUtil.ClearAllProjectiles(2, npc.whoAmI);

                if (WorldSavingSystem.EternityMode)
                {
                    DramaticTransition(npc, false, npc.ai[2] == 0);

                    if (FargoSoulsUtil.HostCheck)
                    {
                        ritualProj = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<PHMutantRitual>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 0f, npc.whoAmI);

                        if (WorldSavingSystem.MasochistModeReal)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual2>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual3>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual4>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);
                        }
                    }
                }
            }
            else if (npc.ai[1] == 150)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                npc.netUpdate = true; 
                if (FargoSoulsUtil.HostCheck)
                {
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.GlowRingHollow>(), 0, 0f, Main.myPlayer, 5);
                    //Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.GlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -22);
                }

                if (WorldSavingSystem.EternityMode && WorldSavingSystem.SkipMutantP1 <= 10)
                {
                    WorldSavingSystem.SkipMutantP1++;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.WorldData);
                }

                for (int i = 0; i < 50; i++)
                {
                    int d = Dust.NewDust(Main.LocalPlayer.position, Main.LocalPlayer.width, Main.LocalPlayer.height, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, 0f, 0f, 0, default, 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 9f;
                }
                if (player.FargoSouls().TerrariaSoul)
                    EdgyBossText(npc, GFBQuote(1));
            }
            else if (npc.ai[1] > 150)
            {
                npc.localAI[3] = 3;
            }

            if (++npc.ai[1] > 270)
            {
                if (WorldSavingSystem.EternityMode)
                {
                    npc.life = npc.lifeMax;
                    npc.ai[0] = Main.rand.Next(new int[] { 11, 13, 16, 19, 20, 21, 24, 26, 29, 35, 37, 39, 42, 47, 49 }); //force a random choice
                }
                else
                {
                    npc.ai[0]++;
                }
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                //NPC.TargetClosest();
                npc.netUpdate = true;

                attackHistory.Enqueue(npc.ai[0]);
            }
        }

        private void ApproachForNextAttackP2(NPC NPC, Player player)//11挂机
        {
            if (!AliveCheck(NPC, player))
                return;
            Vector2 targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 300;
            if (NPC.Distance(targetPos) > 50 && ++NPC.ai[2] < 180)
            {
                Movement(NPC, targetPos, 0.8f);
            }
            else
            {
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.ai[2] = player.SafeDirectionTo(NPC.Center).ToRotation();
                NPC.ai[3] = (float)Math.PI / 10f;
                NPC.localAI[0] = 0;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (player.Center.X < NPC.Center.X)
                    NPC.ai[3] *= -1;
            }
        }

        private void VoidRaysP2(NPC npc)//12P2虚无射线
        {
            npc.velocity = Vector2.Zero;
            if (--npc.ai[1] < 0)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, new Vector2(8, 0).RotatedBy(npc.ai[2]), ModContent.ProjectileType<PHMutantMark1>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, MathHelper.Pi / 3);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, new Vector2(8, 0).RotatedBy(npc.ai[2] + MathHelper.Pi), ModContent.ProjectileType<PHMutantMark1>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, MathHelper.Pi / 3);
                }
                npc.ai[1] = 4;
                npc.ai[2] += npc.ai[3];

                if (npc.localAI[0]++ == 20 || npc.localAI[0] == 40)
                {
                    npc.netUpdate = true;
                    npc.ai[2] -= npc.ai[3] / (WorldSavingSystem.MasochistModeReal ? 3 : 2);

                    if (npc.localAI[0] == 21 && endTimeVariance > 0.33f //sometimes skip to end
                    || npc.localAI[0] == 41 && endTimeVariance < -0.33f)
                        npc.localAI[0] = 60;

                    EdgyBossText(npc, GFBQuote(6));
                }
                else if (npc.localAI[0] >= 60)
                {
                    ChooseNextAttack(npc, 13, 19, 21, 24, 31, 39, 41, 42, 49, 50);
                }
            }
            int num = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type == ModContent.ProjectileType<PHMutantMark1>() && Main.projectile[i].active)
                {
                    num++;
                }
            }
            if (num <= 6)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type == ModContent.ProjectileType<PHMutantMark1>() && Main.projectile[i].active)
                    {
                        Main.projectile[i].hostile = false;
                    }
                }
            }
        }

        private void PrepareSpearDashPredictiveP2(NPC NPC, Player player)//13蓝冲准备
        {
            if (NPC.ai[3] == 0)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.ai[3] = 1;
                //NPC.velocity = NPC.DirectionFrom(player.Center) * NPC.velocity.Length();
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearSpin>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 180); // + 60);
                    TelegraphSound = SoundEngine.PlaySound(FargosSoundRegistry.MutantPredictive with { Volume = 8f }, NPC.Center);
                }

                EdgyBossText(NPC, GFBQuote(9));
            }

            if (++NPC.ai[1] > 180)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.ai[3] = 0;
                //NPC.TargetClosest();
            }

            Vector2 targetPos = player.Center;
            targetPos.Y += 400f * Math.Sign(NPC.Center.Y - player.Center.Y); //can be above or below
            Movement(NPC, targetPos, 0.7f, false);
            if (NPC.Distance(player.Center) < 200)
                Movement(NPC, NPC.Center + NPC.DirectionFrom(player.Center), 1.4f);
        }

        private void SpearDashPredictiveP2(NPC NPC, Player player)//14蓝冲
        {
            if (NPC.localAI[1] == 0) //max number of attacks
            {
                if (WorldSavingSystem.EternityMode)
                    NPC.localAI[1] = Main.rand.Next(WorldSavingSystem.MasochistModeReal ? 3 : 5, 9);
                else
                    NPC.localAI[1] = 5;
            }

            if (NPC.ai[1] == 0) //telegraph
            {
                if (!AliveCheck(NPC, player))
                    return;

                if (NPC.ai[2] == NPC.localAI[1] - 1)
                {
                    if (NPC.Distance(player.Center) > 450) //get closer for last dash
                    {
                        Movement(NPC, player.Center, 0.6f);
                        return;
                    }

                    NPC.velocity *= 0.75f; //try not to bump into player
                }

                if (NPC.ai[2] < NPC.localAI[1])
                {
                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center + player.velocity * 30f), ModContent.ProjectileType<MutantDeathrayAim>(), 0, 0f, Main.myPlayer, 55, NPC.whoAmI);
                    if (NPC.ai[2] == NPC.localAI[1] - 1)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                        if (FargoSoulsUtil.HostCheck)
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 4);
                    }
                }
            }

            NPC.velocity *= 0.9f;

            if (NPC.ai[1] < 55) //track player up until just before dash
            {
                NPC.localAI[0] = NPC.SafeDirectionTo(player.Center + player.velocity * 30f).ToRotation();
            }

            int endTime = 60;
            if (NPC.ai[2] == NPC.localAI[1] - 1)
                endTime = 80;
            if (WorldSavingSystem.MasochistModeReal && (NPC.ai[2] == 0 || NPC.ai[2] >= NPC.localAI[1]))
                endTime = 0;
            if (++NPC.ai[1] > endTime)
            {
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.ai[3] = 0;
                if (++NPC.ai[2] > NPC.localAI[1])
                {
                    ChooseNextAttack(NPC, 16, 19, 20, 26, 29, 31, 33, 39, 42, 44, 45, 50);
                }
                else
                {
                    NPC.velocity = NPC.localAI[0].ToRotationVector2() * 45f;
                    float spearAi = 0f;
                    if (NPC.ai[2] == NPC.localAI[1])
                        spearAi = -2f;

                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<PHMutantSpearDash>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, spearAi);
                    }

                    EdgyBossText(NPC, GFBQuote(10));
                }
                for (int j = 0; j <= Main.maxProjectiles; j++)
                {
                    if (Main.projectile[j].type == ModContent.ProjectileType<PHMutantSphereSmall>() && Main.projectile[j].active && Main.projectile[j].ai[1] >= 30)
                    {
                        Main.projectile[j].ai[1] = 180;
                    }
                }
                NPC.localAI[0] = 0;
            }
        }

        private void WhileDashingP2(NPC NPC, Player player)
        {
            NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
            if (++NPC.ai[1] > 30)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.netUpdate = true;
                NPC.ai[0]--;
                NPC.ai[1] = 0;

                //quickly bounce back towards player
                if (NPC.ai[0] == 14 && NPC.ai[2] == NPC.localAI[1] - 1 && NPC.Distance(player.Center) > 450)
                    NPC.velocity = NPC.SafeDirectionTo(player.Center) * 16f;
            }
        }//冲刺中

        private void BoundaryBulletHellP2(NPC NPC, Player player)
        {
            NPC.velocity = Vector2.Zero;
            if (NPC.localAI[0] == 0)
            {
                NPC.localAI[0] = Math.Sign(NPC.Center.X - player.Center.X);
                //if (WorldSavingSystem.MasochistMode) NPC.ai[2] = NPC.SafeDirectionTo(player.Center).ToRotation(); //starting rotation offset to avoid hitting at close range
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.GlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -2);

                EdgyBossText(NPC, GFBQuote(11));

                if (WorldSavingSystem.MasochistModeReal)
                    NPC.ai[2] = Main.rand.NextFloat(MathHelper.Pi);
            }
            if (NPC.ai[3] > 60 && ++NPC.ai[1] > 2)
            {
                SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                NPC.ai[1] = 0;
                NPC.ai[2] += (float)Math.PI / 8 / 480 * NPC.ai[3] * NPC.localAI[0];
                if (NPC.ai[2] > (float)Math.PI)
                    NPC.ai[2] -= (float)Math.PI * 2;
                if (FargoSoulsUtil.HostCheck)
                {
                    int max = 5;
                    if (WorldSavingSystem.EternityMode)
                        max += 1;
                    if (WorldSavingSystem.MasochistModeReal)
                        max += 1;
                    if (Main.getGoodWorld)
                        max += 1;
                    for (int i = 0; i < max; i++)
                    {
                        float vel = 4.5f + (NPC.ai[3] - 60f) / 55f;
                        if (vel > 11)
                        {
                            vel = 11f;
                        }
                        vel *= Main.getGoodWorld ? 1.17f : 1f;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0f, -vel).RotatedBy(NPC.ai[2] + Math.PI * 2 / max * i),
                            ModContent.ProjectileType<PHMutantEye>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                    }
                }
            }

            int endTime = 360 + 60 + (int)(300 * endTimeVariance);
            if (++NPC.ai[3] > endTime)
            {
                ChooseNextAttack(NPC, 11, 13, 19, 20, 21, 24, WorldSavingSystem.MasochistModeReal ? 31 : 26, 33, 41, 44, 50);
            }
        }//17波粒

        private void PillarDunk(NPC NPC, Player player)//19天界柱投掷
        {
            if (!AliveCheck(NPC, player))
                return;

            int pillarAttackDelay = Main.getGoodWorld ? 40 : 60;

            if (Main.zenithWorld && NPC.ai[1] > 180)
                player.confused = true;

            if (NPC.ai[2] == 0 && NPC.ai[3] == 0) //target one corner of arena
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (FargoSoulsUtil.HostCheck) //spawn cultists
                {
                    void Clone(float ai1, float ai2, float ai3) => FargoSoulsUtil.NewNPCEasy(NPC.GetSource_FromAI(), NPC.Center, ModContent.NPCType<PHMutantIllusion>(), NPC.whoAmI, NPC.whoAmI, ai1, ai2, ai3);
                    Clone(-1, 1, pillarAttackDelay * 4);
                    Clone(1, -1, pillarAttackDelay * 2);
                    Clone(1, 1, pillarAttackDelay * 3);
                    if (WorldSavingSystem.MasochistModeReal)
                    {
                        Clone(1, -1, pillarAttackDelay * 6);

                        if (Main.getGoodWorld)
                        {
                            Clone(1, 1, pillarAttackDelay * 7);
                            Clone(-1, 1, pillarAttackDelay * 8);
                            Clone(1, -1, pillarAttackDelay * 10);
                            Clone(1, 1, pillarAttackDelay * 11);
                            Clone(-1, 1, pillarAttackDelay * 12);
                        }
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center, new Vector2(0, -4), ModContent.ProjectileType<BrainofConfusion>(), 0, 0, Main.myPlayer);
                }

                EdgyBossText(NPC, GFBQuote(12));

                NPC.netUpdate = true;
                NPC.ai[2] = NPC.Center.X;
                NPC.ai[3] = NPC.Center.Y;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<PHMutantRitual>() && Main.projectile[i].ai[1] == NPC.whoAmI)
                    {
                        NPC.ai[2] = Main.projectile[i].Center.X;
                        NPC.ai[3] = Main.projectile[i].Center.Y;
                        break;
                    }
                }

                Vector2 offset = 1000f * Vector2.UnitX.RotatedBy(MathHelper.ToRadians(45));
                if (Main.rand.NextBool()) //always go to a side player isn't in but pick a way to do it randomly
                {
                    if (player.Center.X > NPC.ai[2])
                        offset.X *= -1;
                    if (Main.rand.NextBool())
                        offset.Y *= -1;
                }
                else
                {
                    if (Main.rand.NextBool())
                        offset.X *= -1;
                    if (player.Center.Y > NPC.ai[3])
                        offset.Y *= -1;
                }

                NPC.localAI[1] = NPC.ai[2]; //for illusions
                NPC.localAI[2] = NPC.ai[3];

                NPC.ai[2] = offset.Length();
                NPC.ai[3] = offset.ToRotation();
            }

            Vector2 targetPos = player.Center;
            targetPos.X += NPC.Center.X < player.Center.X ? -700 : 700;
            targetPos.Y += NPC.ai[1] < 240 ? 400 : 150;
            if (NPC.Distance(targetPos) > 50)
                Movement(NPC, targetPos, 1f);

            int endTime = 240 + pillarAttackDelay * 4 + 60;
            if (WorldSavingSystem.MasochistModeReal)
            {
                endTime += pillarAttackDelay * 2;
                if (Main.getGoodWorld)
                    endTime += 210 + 60;//改了
            }

            NPC.localAI[0] = endTime - NPC.ai[1]; //for pillars to know remaining duration
            NPC.localAI[0] += 60f + 60f * (1f - NPC.ai[1] / endTime); //staggered despawn

            if (++NPC.ai[1] > endTime)
            {
                ChooseNextAttack(NPC, 11, 13, 20, 21, 26, 33, 41, 44, 49, 50);
            }
            else if (NPC.ai[1] == pillarAttackDelay)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -5,
                        ModContent.ProjectileType<PHMutantPillar>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0, Main.myPlayer, 3, NPC.whoAmI);
                }
            }
            else if (WorldSavingSystem.MasochistModeReal && NPC.ai[1] == pillarAttackDelay * 5)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -5,
                        ModContent.ProjectileType<PHMutantPillar>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0, Main.myPlayer, 1, NPC.whoAmI);
                }
            }
            else if (WorldSavingSystem.MasochistModeReal && NPC.ai[1] == pillarAttackDelay * 9 && Main.getGoodWorld)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -5,
                        ModContent.ProjectileType<PHMutantPillar>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0, Main.myPlayer, 1, NPC.whoAmI);
                }
            }
        }

        private void EOCStarSickles(NPC NPC, Player player)//20克苏鲁星镰
        {
            if (!AliveCheck(NPC, player))
                return;

            if (NPC.ai[1] == 0)
            {
                float ai1 = 0;

                if (WorldSavingSystem.MasochistModeReal) //begin attack much faster
                {
                    ai1 = 30;
                    NPC.ai[1] = 30;
                }

                if (FargoSoulsUtil.HostCheck)
                {
                    int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.UnitY, ModContent.ProjectileType<MutantEyeOfCthulhu>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, ai1);
                    if (WorldSavingSystem.MasochistModeReal && p != Main.maxProjectiles)
                        Main.projectile[p].timeLeft -= 30;
                }
            }

            if (NPC.ai[1] < 120) //stop tracking when eoc begins attacking, this locks arena in place
            {
                NPC.ai[2] = player.Center.X;
                NPC.ai[3] = player.Center.Y;
            }

            if (NPC.ai[1] == 120)
            {
                EdgyBossText(NPC, GFBQuote(13));
            }

            /*if (NPC.Distance(player.Center) < 200)
            {
                Movement(NPC.Center + 200 * NPC.DirectionFrom(player.Center), 0.9f);
            }
            else
            {*/
            Vector2 targetPos = new(NPC.ai[2], NPC.ai[3]);
            targetPos += NPC.DirectionFrom(targetPos).RotatedBy(MathHelper.ToRadians(-5)) * 450f;
            if (NPC.Distance(targetPos) > 50)
                Movement(NPC, targetPos, 0.25f);
            //}

            if (++NPC.ai[1] > 450)
            {
                ChooseNextAttack(NPC, 11, 13, 16, 21, 26, 29, 31, 33, 35, 37, 41, 44, 45, 47, 49, 50);
            }

            /*if (Math.Abs(targetPos.X - player.Center.X) < 150) //avoid crossing up player
            {
                targetPos.X = player.Center.X + 150 * Math.Sign(targetPos.X - player.Center.X);
                Movement(targetPos, 0.3f);
            }
            if (NPC.Distance(targetPos) > 50)
            {
                Movement(targetPos, 0.5f);
            }

            if (--NPC.ai[1] < 0)
            {
                NPC.ai[1] = 60;
                if (++NPC.ai[2] > (WorldSavingSystem.MasochistMode ? 3 : 1))
                {
                    //float[] options = { 13, 19, 21, 24, 26, 31, 33, 40 }; AttackChoice = options[Main.rand.Next(options.Length)];
                    AttackChoice++;
                    NPC.ai[2] = 0;
                    NPC.TargetClosest();
                }
                else
                {
                    if (FargoSoulsUtil.HostCheck)
                        for (int i = 0; i < 8; i++)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(Math.PI / 4 * i) * 10f, ModContent.ProjectileType<MutantScythe1>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer, NPC.whoAmI);
                    SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center);
                }
                NPC.netUpdate = true;
                break;
            }*/
        }

        private void PrepareSpearDashDirectP2(NPC NPC, Player player)//21青冲预备
        {
            if (NPC.ai[3] == 0)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.ai[3] = 1;
                //NPC.velocity = NPC.DirectionFrom(player.Center) * NPC.velocity.Length();
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearSpin>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 180);// + (WorldSavingSystem.MasochistMode ? 10 : 20));
                    TelegraphSound = SoundEngine.PlaySound(FargosSoundRegistry.MutantUnpredictive with { Volume = 2f }, NPC.Center);
                }

                EdgyBossText(NPC, GFBQuote(14));
            }

            if (++NPC.ai[1] > 180)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.ai[3] = 0;
                //NPC.TargetClosest();
            }

            Vector2 targetPos = player.Center;
            targetPos.Y += 450f * Math.Sign(NPC.Center.Y - player.Center.Y); //can be above or below
            Movement(NPC, targetPos, 0.7f, false);
            if (NPC.Distance(player.Center) < 200)
                Movement(NPC, NPC.Center + NPC.DirectionFrom(player.Center), 1.4f);
        }

        private void SpearDashDirectP2(NPC NPC, Player player)//22青冲
        {
            NPC.velocity *= 0.9f;

            if (NPC.localAI[1] == 0) //max number of attacks
            {
                if (WorldSavingSystem.EternityMode)
                    NPC.localAI[1] = Main.rand.Next(WorldSavingSystem.MasochistModeReal ? 3 : 5, 9);
                else
                    NPC.localAI[1] = 5;
            }

            if (++NPC.ai[1] > (WorldSavingSystem.EternityMode ? 5 : 20))
            {
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                if (++NPC.ai[2] > NPC.localAI[1])
                {
                    if (WorldSavingSystem.MasochistModeReal)
                        ChooseNextAttack(NPC, 11, 13, 16, 19, 20, 31, 33, 35, 39, 42, 44, 47, 50);
                    else
                        ChooseNextAttack(NPC, 11, 16, 26, 29, 31, 35, 37, 39, 42, 44, 47);
                }
                else
                {
                    NPC.velocity = NPC.SafeDirectionTo(player.Center) * (WorldSavingSystem.MasochistModeReal ? 60f : 45f);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearDash>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);
                    }
                }

                EdgyBossText(NPC, GFBQuote(15));
            }
        }

        private void SpawnDestroyersForPredictiveThrow(NPC NPC, Player player)//24蠕虫预判投矛准备
        {
            if (!AliveCheck(NPC, player))
                return;

            if (WorldSavingSystem.EternityMode)
            {
                Vector2 targetPos = player.Center + NPC.DirectionFrom(player.Center) * 500;
                if (Math.Abs(targetPos.X - player.Center.X) < 150) //avoid crossing up player
                {
                    targetPos.X = player.Center.X + 150 * Math.Sign(targetPos.X - player.Center.X);
                    Movement(NPC, targetPos, 0.3f);
                }
                if (NPC.Distance(targetPos) > 50)
                {
                    Movement(NPC, targetPos, 0.9f);
                }
            }
            else
            {
                Vector2 targetPos = player.Center;
                targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
                if (NPC.Distance(targetPos) > 50)
                {
                    Movement(NPC, targetPos, 0.4f);
                }
            }

            if (NPC.localAI[1] == 0) //max number of attacks
            {
                if (WorldSavingSystem.EternityMode)
                    NPC.localAI[1] = Main.rand.Next(WorldSavingSystem.MasochistModeReal ? 3 : 5, 9);
                else
                    NPC.localAI[1] = 5;

                NPC.localAI[2] = Main.rand.Next(2);

                EdgyBossText(NPC, GFBQuote(16));
            }

            if (++NPC.ai[1] > 60)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 30;
                int cap = 3;
                if (WorldSavingSystem.EternityMode)
                {
                    cap += 2;
                }
                if (WorldSavingSystem.MasochistModeReal)
                {
                    cap += 2;
                    NPC.ai[1] += 15; //faster
                }

                if (++NPC.ai[2] > cap)
                {
                    //NPC.TargetClosest();
                    NPC.ai[0]++;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath13, NPC.Center);
                    if (FargoSoulsUtil.HostCheck) //spawn worm
                    {
                        Vector2 vel = NPC.DirectionFrom(player.Center).RotatedByRandom(MathHelper.ToRadians(120)) * 10f;
                        float ai1 = 0.8f + 0.4f * NPC.ai[2] / 5f;
                        if (WorldSavingSystem.MasochistModeReal)
                            ai1 += 0.4f;
                        float appearance = NPC.localAI[2];
                        if (FargoSoulsUtil.AprilFools)
                            appearance = 0;
                        int current = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<MutantDestroyerHead>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, ai1, appearance);
                        //timeleft: remaining duration of this case + duration of next case + extra delay after + successive death
                        Main.projectile[current].timeLeft = 30 * (cap - (int)NPC.ai[2]) + 60 * (int)NPC.localAI[1] + 30 + (int)NPC.ai[2] * 6;
                        int max = Main.rand.Next(8, 19);
                        for (int i = 0; i < max; i++)
                            current = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<MutantDestroyerBody>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, Main.projectile[current].identity, 0f, appearance);
                        int previous = current;
                        current = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<MutantDestroyerTail>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, Main.projectile[current].identity, 0f, appearance);
                        Main.projectile[previous].localAI[1] = Main.projectile[current].identity;
                        Main.projectile[previous].netUpdate = true;
                    }
                }
            }
        }

        private void SpearTossPredictiveP2(NPC NPC, Player player)//25蠕虫预判投矛
        {
            if (!AliveCheck(NPC, player))
                return;

            Vector2 targetPos = player.Center;
            targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
            if (NPC.Distance(targetPos) > 25)
                Movement(NPC, targetPos, 0.8f);

            if (++NPC.ai[1] > 60)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 0;
                bool shouldAttack = true;
                if (++NPC.ai[2] > NPC.localAI[1])
                {
                    shouldAttack = false;
                    if (WorldSavingSystem.MasochistModeReal)
                        ChooseNextAttack(NPC, 11, 19, 20, 29, 31, 33, 35, 37, 39, 42, 44, 45, 47, 50);
                    else
                        ChooseNextAttack(NPC, 11, 19, 20, 26, 26, 26, 29, 31, 33, 35, 37, 39, 42, 44, 47);
                }

                if ((shouldAttack || WorldSavingSystem.MasochistModeReal) && FargoSoulsUtil.HostCheck)
                {
                    Vector2 vel = NPC.SafeDirectionTo(player.Center + player.velocity * 30f) * 30f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<PHMutantSpearThrown>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, 1f);
                }
                for (int j = 0; j <= Main.maxProjectiles; j++)
                {
                    if (Main.projectile[j].type == ModContent.ProjectileType<PHMutantSphereSmall>() && Main.projectile[j].active && Main.projectile[j].ai[1] >= 30)
                    {
                        Main.projectile[j].ai[1] = 180;
                    }
                }
            }
            else if (NPC.ai[1] == 1 && (NPC.ai[2] < NPC.localAI[1] || WorldSavingSystem.MasochistModeReal) && FargoSoulsUtil.HostCheck)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center + player.velocity * 30f), ModContent.ProjectileType<MutantDeathrayAim>(), 0, 0f, Main.myPlayer, 60f, NPC.whoAmI);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 2);
            }
        }

        private void PrepareMechRayFan(NPC NPC, Player player)//26准备机械光扇
        {
            if (NPC.ai[1] == 0)
            {
                if (!AliveCheck(NPC, player))
                    return;

                if (WorldSavingSystem.MasochistModeReal)
                    NPC.ai[1] = 31; //skip the pause, skip the telegraph
            }

            if (NPC.ai[1] == 30)
            {
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center); //eoc roar
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, NPCID.Retinazer);

                EdgyBossText(NPC, GFBQuote(17));
            }

            Vector2 targetPos;
            if (NPC.ai[1] < 30)
            {
                targetPos = player.Center + NPC.DirectionFrom(player.Center).RotatedBy(MathHelper.ToRadians(15)) * 500f;
                if (NPC.Distance(targetPos) > 50)
                    Movement(NPC, targetPos, 0.3f);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(NPC.Center, 0, 0, DustID.Torch, 0f, 0f, 0, default, 3f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 12f;
                }

                targetPos = player.Center;
                targetPos.X += 600 * (NPC.Center.X < targetPos.X ? -1 : 1);
                Movement(NPC, targetPos, 1.2f, false);
            }

            if (++NPC.ai[1] > 150 || WorldSavingSystem.MasochistModeReal && NPC.Distance(targetPos) < 64)
            {
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                //NPC.TargetClosest();
            }
        }

        private void MechRayFan(NPC NPC, Player player)//27机械光扇
        {
            NPC.velocity = Vector2.Zero;

            if (NPC.ai[2] == 0)
            {
                NPC.ai[2] = Main.rand.NextBool() ? -1 : 1; //randomly aim either up or down
            }

            if (NPC.ai[3] == 0 && FargoSoulsUtil.HostCheck)
            {
                int max = 7;
                for (int i = 0; i <= max; i++)
                {
                    Vector2 dir = Vector2.UnitX.RotatedBy(NPC.ai[2] * i * MathHelper.Pi / max) * 6; //rotate initial velocity of telegraphs by 180 degrees depending on velocity of lasers
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + dir, Vector2.Zero, ModContent.ProjectileType<MutantGlowything>(), 0, 0f, Main.myPlayer, dir.ToRotation(), NPC.whoAmI, 0f);
                }
            }

            int endTime = 60 + 180 + 150;

            if (NPC.ai[3] > (WorldSavingSystem.MasochistModeReal ? 45 : 60) && NPC.ai[3] < 60 + 180 && ++NPC.ai[1] > 10)
            {
                NPC.ai[1] = 0;
                if (FargoSoulsUtil.HostCheck)
                {
                    float rotation = MathHelper.ToRadians(245) * NPC.ai[2] / 80f;
                    int timeBeforeAttackEnds = endTime - (int)NPC.ai[3];

                    void SpawnRay(Vector2 pos, float angleInDegrees, float turnRotation)
                    {
                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, MathHelper.ToRadians(angleInDegrees).ToRotationVector2(),
                            ModContent.ProjectileType<MutantDeathray3>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0, Main.myPlayer, turnRotation, NPC.whoAmI);
                        if (p != Main.maxProjectiles && Main.projectile[p].timeLeft > timeBeforeAttackEnds)
                            Main.projectile[p].timeLeft = timeBeforeAttackEnds;
                    }
                    ;

                    SpawnRay(NPC.Center, 8 * NPC.ai[2], rotation);
                    SpawnRay(NPC.Center, -8 * NPC.ai[2] + 180, -rotation);

                    if (WorldSavingSystem.MasochistModeReal)
                    {
                        Vector2 spawnPos = NPC.Center + NPC.ai[2] * -1200 * Vector2.UnitY;
                        SpawnRay(spawnPos, 8 * NPC.ai[2] + 180, rotation);
                        SpawnRay(spawnPos, -8 * NPC.ai[2], -rotation);
                    }
                }
            }

            void SpawnPrime(float varianceInDegrees, float rotationInDegrees)
            {
                SoundEngine.PlaySound(SoundID.Item21, NPC.Center);

                if (FargoSoulsUtil.HostCheck)
                {
                    float spawnOffset = (Main.rand.NextBool() ? -1 : 1) * Main.rand.NextFloat(1400, 1800);
                    float maxVariance = MathHelper.ToRadians(varianceInDegrees);
                    Vector2 aimPoint = NPC.Center - Vector2.UnitY * NPC.ai[2] * 600;
                    Vector2 spawnPos = aimPoint + spawnOffset * Vector2.UnitY.RotatedByRandom(maxVariance).RotatedBy(MathHelper.ToRadians(rotationInDegrees));
                    Vector2 vel = 32f * Vector2.Normalize(aimPoint - spawnPos);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, vel, ModContent.ProjectileType<MutantGuardian>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer);
                }
            }

            if (NPC.ai[3] < 180 + 60 && ++NPC.localAI[0] > 1)
            {
                NPC.localAI[0] = 0;
                if (NPC.ai[3] < 60)
                {
                    SpawnPrime(15, 0);
                }
                else
                {
                    SpawnPrime(15, NPC.ai[3] - 60);
                }
            }

            if (WorldSavingSystem.MasochistModeReal && NPC.ai[3] == endTime - 70)
            {
                Vector2 aimPoint = NPC.Center - Vector2.UnitY * NPC.ai[2] * 600;
                for (int i = -3; i <= 3; i++)
                {
                    Vector2 spawnPos = aimPoint + 200 * i * Vector2.UnitX;
                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<MutantReticle2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                }
            }

            if (++NPC.ai[3] > endTime - 30)
            {
                if (WorldSavingSystem.MasochistModeReal) //maso prime jumpscare after rays
                {
                    for (int i = 0; i < 60; i++)
                        SpawnPrime(45, 90);
                }

                if (WorldSavingSystem.EternityMode) //use full moveset
                {
                    ChooseNextAttack(NPC, 11, 13, 16, 19, 21, 24, 29, 31, 33, 35, 37, 39, 41, 42, 45, 47, 49, 50);
                }
                else
                {
                    NPC.ai[0] = 11;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                }
                NPC.netUpdate = true;
            }
        }

        private void PrepareFishron1(NPC NPC, Player player)//29准备猪鲨夹击
        {
            if (!AliveCheck(NPC, player))
                return;
            Vector2 targetPos = new(player.Center.X, player.Center.Y + 600 * Math.Sign(NPC.Center.Y - player.Center.Y));
            Movement(NPC, targetPos, 1.4f, false);
            if (NPC.ai[1] == 0) //always dash towards same side i started on
                NPC.ai[2] = Math.Sign(NPC.Center.X - player.Center.X);

            if (++NPC.ai[1] > 60 || NPC.Distance(targetPos) < 64) //dive here
            {
                NPC.velocity.X = 30f * NPC.ai[2];
                NPC.velocity.Y = 0f;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;

                EdgyBossText(NPC, GFBQuote(18));
            }
        }

        private void SpawnFishrons(NPC NPC)//30生成猪鲨夹击
        {
            NPC.velocity *= 0.97f;
            if (NPC.ai[1] == 0)
            {
                NPC.ai[2] = MathHelper.PiOver2 + Main.LocalPlayer.SafeDirectionTo(NPC.Center).ToRotation() + Main.rand.NextFloat(MathHelper.Pi / 8, MathHelper.Pi / 3); ;
            }
            /*
            const int fishronDelay = 3;
            int maxFishronSets = WorldSavingSystem.MasochistModeReal ? 3 : 2;
            
            if (NPC.ai[1] % fishronDelay == 0 && NPC.ai[1] <= fishronDelay * maxFishronSets)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    int projType = NPC.ai[0] == 30 ? ModContent.ProjectileType<MutantFishron>() : ModContent.ProjectileType<MutantShadowHand>();
                    for (int j = -1; j <= 1; j += 2) //to both sides of player
                    {
                        int max = (int)NPC.ai[1] / fishronDelay;
                        for (int i = -max; i <= max; i++) //fan of fishron
                        {
                            if (Math.Abs(i) != max) //only spawn the outmost ones
                                continue;
                            float spread = MathHelper.Pi / 3 / (maxFishronSets + 1);
                            Vector2 offset = NPC.ai[2] == 0 ? Vector2.UnitY.RotatedBy(spread * i) * -450f * j : Vector2.UnitX.RotatedBy(spread * i) * 475f * j;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, projType, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, offset.X, offset.Y);
                        }
                    }
                }
                for (int i = 0; i < 30; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceTorch, 0f, 0f, 0, default, 3f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 12f;
                }
            }
            */
            if (FargoSoulsUtil.HostCheck)
            {
                int projType = NPC.ai[0] == 30 ? ModContent.ProjectileType<MutantFishron>() : ModContent.ProjectileType<MutantShadowHand>();
                if (projType == ModContent.ProjectileType<MutantFishron>())
                {
                    int fishronDelay = 40;
                    int maxtime = WorldSavingSystem.MasochistModeReal ? 5 : 3;
                    if (NPC.ai[1] % fishronDelay == 0 && NPC.ai[1] < fishronDelay * maxtime)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 targetpos = 425 * Vector2.UnitX.RotatedBy((i + 0.3f * NPC.ai[1] / (float)fishronDelay) * MathHelper.TwoPi / 2 + NPC.ai[2]);
                            Vector2 targetpos2 = 425 * Vector2.UnitX.RotatedBy((0.1f + i + 0.3f * NPC.ai[1] / (float)fishronDelay) * MathHelper.TwoPi / 2 + NPC.ai[2]);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, projType, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, targetpos.X, targetpos.Y);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, projType, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, targetpos2.X, targetpos2.Y);
                        }
                        for (int i = 0; i < 30; i++)
                        {
                            int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceTorch, 0f, 0f, 0, default, 3f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].noLight = true;
                            Main.dust[d].velocity *= 12f;
                        }
                    }
                }
                else
                {
                    int shadowdelay = 10;
                    int maxtime = WorldSavingSystem.MasochistModeReal ? 20 : 12;
                    if (NPC.ai[1] % shadowdelay == 0 && NPC.ai[1] < shadowdelay * maxtime)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 targetpos = i * 450 * Vector2.UnitX.RotatedBy(NPC.ai[2]);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, projType, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, targetpos.X, targetpos.Y);
                        }
                        for (int i = 0; i < 30; i++)
                        {
                            int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceTorch, 0f, 0f, 0, default, 3f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].noLight = true;
                            Main.dust[d].velocity *= 12f;
                        }
                    }
                }
            }
            if (++NPC.ai[1] > (WorldSavingSystem.MasochistModeReal ? 200 : 120))
            {
                ChooseNextAttack(NPC, 13, 19, 20, 21, WorldSavingSystem.MasochistModeReal ? 44 : 26, 28, 31, 31, 31, 33, 35, 39, 41, 42, 44, 47, 49, 50);
            }
        }

        private void PrepareTrueEyeDiveP2(NPC NPC, Player player)//31准备真眼俯冲
        {
            if (!AliveCheck(NPC, player))
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 400 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y += 400;
            Movement(NPC, targetPos, 1.2f);

            //dive here
            if (++NPC.ai[1] > 60)
            {
                NPC.velocity.X = 30f * (NPC.position.X < player.position.X ? 1 : -1);
                if (NPC.velocity.Y > 0)
                    NPC.velocity.Y *= -1;
                NPC.velocity.Y *= 0.3f;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;
            }
        }

        private void PrepareNuke(NPC NPC, Player player)//33准备核弹
        {
            if (!AliveCheck(NPC, player))
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 400 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y -= 400;
            Movement(NPC, targetPos, 1.2f, false);
            if (++NPC.ai[1] > 60)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (FargoSoulsUtil.HostCheck)
                {
                    float gravity = 0.2f;
                    float time = WorldSavingSystem.MasochistModeReal ? 120f : 180f;
                    Vector2 distance = player.Center - NPC.Center;
                    distance.X /= time;
                    distance.Y = distance.Y / time - 0.5f * gravity * time;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, distance, ModContent.ProjectileType<MutantNuke>(), WorldSavingSystem.MasochistModeReal ? FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3f) : 0, 0f, Main.myPlayer, gravity);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantFishronRitual>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer, NPC.whoAmI);
                }
                NPC.ai[0]++;
                NPC.ai[1] = 0;

                if (Math.Sign(player.Center.X - NPC.Center.X) == Math.Sign(NPC.velocity.X))
                    NPC.velocity.X *= -1f;
                if (NPC.velocity.Y < 0)
                    NPC.velocity.Y *= -1f;
                NPC.velocity.Normalize();
                NPC.velocity *= 3f;

                NPC.netUpdate = true;

                EdgyBossText(NPC, GFBQuote(19));
                //NPC.TargetClosest();
            }
        }

        private void Nuke(NPC NPC, Player player)//34核弹
        {
            if (!AliveCheck(NPC, player))
                return;

            Vector2 target = NPC.Bottom.Y < player.Top.Y
            ? player.Center + 300f * Vector2.UnitX * Math.Sign(NPC.Center.X - player.Center.X)
                : NPC.Center + 30 * NPC.DirectionFrom(player.Center).RotatedBy(MathHelper.ToRadians(60) * Math.Sign(player.Center.X - NPC.Center.X));
            Movement(NPC, target, 0.1f);
            int maxSpeed = WorldSavingSystem.MasochistModeReal ? 3 : 2;
            if (NPC.velocity.Length() > maxSpeed)
                NPC.velocity = Vector2.Normalize(NPC.velocity) * maxSpeed;

            if (NPC.ai[1] > (WorldSavingSystem.MasochistModeReal ? 120 : 180))
            {
                if (!Main.dedServ && Main.LocalPlayer.active)
                    FargoSoulsUtil.ScreenshakeRumble(6);

                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 safeZone = NPC.Center;
                    safeZone.Y -= 100;
                    const float safeRange = 150 + 200;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spawnPos = NPC.Center + Main.rand.NextVector2Circular(1200, 1200);
                        if (Vector2.Distance(safeZone, spawnPos) < safeRange)
                        {
                            Vector2 directionOut = spawnPos - safeZone;
                            directionOut.Normalize();
                            spawnPos = safeZone + directionOut * Main.rand.NextFloat(safeRange, 1200);
                        }
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<MutantBomb>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer);
                    }
                }
            }

            if (++NPC.ai[1] > 360 + 210 * endTimeVariance)
            {
                ChooseNextAttack(NPC, 11, 13, 16, 19, 24, 28, WorldSavingSystem.MasochistModeReal ? 26 : 29, 31, 35, 37, 39, 41, 42, 47, 49);
            }

            if (NPC.ai[1] > 45)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 offset = new();
                    offset.Y -= 100;
                    double angle = Main.rand.NextDouble() * 2d * Math.PI;
                    offset.X += (float)(Math.Sin(angle) * 150);
                    offset.Y += (float)(Math.Cos(angle) * 150);
                    Dust dust = Main.dust[Dust.NewDust(NPC.Center + offset - new Vector2(4, 4), 0, 0, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, 0, 0, 100, Color.White, 1.5f)];
                    dust.velocity = NPC.velocity;
                    if (Main.rand.NextBool(3))
                        dust.velocity += Vector2.Normalize(offset) * 5f;
                    dust.noGravity = true;
                }
                /*
                if (NPC.ai[1] % 60 == 0 && NPC.ai[1] > 120)
                {
                    int max = WorldSavingSystem.masochistModeReal ? Main.getGoodWorld ? 4 : 3 : 2;
                    float omiga = 0f;
                    int delay = NPC.ai[1] == 180 ? 30 : 0;
                    if (WorldSavingSystem.MasochistModeReal) // evil spin
                    {
                        omiga = Main.rand.NextBool() ? 1 : -1;
                        omiga *= MathF.Tau / 360;
                    }
                    SpawnWillJavelin(NPC, player.Center, max, Main.rand.NextFloat(MathF.Tau), omiga, delay);
                }
                */
            }
        }

        private void PrepareSlimeRain(NPC NPC, Player player)//35准备史莱姆雨
        {
            if (!AliveCheck(NPC, player))
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 700 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y += 200;
            Movement(NPC, targetPos, 2f);

            if (++NPC.ai[2] > 30 || WorldSavingSystem.MasochistModeReal && NPC.Distance(targetPos) < 64)
            {
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                NPC.netUpdate = true;
                //NPC.TargetClosest();

                EdgyBossText(NPC, GFBQuote(20));
            }
        }

        private void SlimeRain(NPC NPC, Player player)//36史莱姆雨
        {
            if (NPC.ai[3] == 0)
            {
                NPC.ai[3] = 1;
                //Main.NewText(NPC.position.Y);
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSlimeRain>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);
            }

            if (NPC.ai[1] == 0) //telegraphs for where slime will fall
            {
                bool first = NPC.localAI[0] == 0;
                NPC.localAI[0] = Main.rand.Next(5, 8) * 120;
                if (first) //always start on the same side as the player
                {
                    if (player.Center.X < NPC.Center.X && NPC.localAI[0] > 1200)
                        NPC.localAI[0] -= 1200;
                    else if (player.Center.X > NPC.Center.X && NPC.localAI[0] < 1200)
                        NPC.localAI[0] += 1200;
                }
                else //after that, always be on opposite side from player
                {
                    if (player.Center.X < NPC.Center.X && NPC.localAI[0] < 1200)
                        NPC.localAI[0] += 1200;
                    else if (player.Center.X > NPC.Center.X && NPC.localAI[0] > 1200)
                        NPC.localAI[0] -= 1200;
                }
                NPC.localAI[0] += 60;

                Vector2 basePos = NPC.Center;
                basePos.X -= 1200;
                for (int i = -360; i <= 2760; i += 120) //spawn telegraphs
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (i + 60 == (int)NPC.localAI[0])
                            continue;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), basePos.X + i + 60, basePos.Y, 0f, 0f, ModContent.ProjectileType<MutantReticle>(), 0, 0f, Main.myPlayer);
                    }
                }

                if (WorldSavingSystem.MasochistModeReal)
                {
                    NPC.ai[1] += 20; //less startup
                    NPC.ai[2] += 20; //stay synced
                }
            }

            if (NPC.ai[1] > 120 && NPC.ai[1] % 5 == 0) //rain down slime balls
            {
                SoundEngine.PlaySound(SoundID.Item34, player.Center);
                if (FargoSoulsUtil.HostCheck)
                {
                    void Slime(Vector2 pos, float off, Vector2 vel)
                    {
                        //dont flip in maso wave 3
                        int flip = WorldSavingSystem.MasochistModeReal && NPC.ai[2] < 180 * 2 && Main.rand.NextBool() ? -1 : 1;
                        Vector2 spawnPos = pos + off * Vector2.UnitY * flip;
                        float ai0 = FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<PHMutantRitual>()) == null ? 0f : NPC.Distance(Main.projectile[ritualProj].Center);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, vel * flip * 2 /* x2 to compensate for removed extraUpdates */, ModContent.ProjectileType<MutantSlimeBall>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0);
                        //int frame = Main.rand.Next(3);
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, vel * flip * 2, ModContent.ProjectileType<MutantSlimeSpike>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai2: frame);
                    }

                    Vector2 basePos = NPC.Center;
                    basePos.X -= 1200;
                    float yOffset = -1300;

                    const float safeRange = 110;
                    for (int i = -360; i <= 2760; i += 75)
                    {
                        float xOffset = i + Main.rand.Next(75);
                        if (Math.Abs(xOffset - NPC.localAI[0]) < safeRange) //dont fall over safespot
                            continue;

                        Vector2 spawnPos = basePos;
                        spawnPos.X += xOffset;
                        Vector2 velocity = Vector2.UnitY * Main.rand.NextFloat(15f, 20f);

                        Slime(spawnPos, yOffset, velocity);
                    }

                    //spawn right on safespot borders
                    Slime(basePos + Vector2.UnitX * (NPC.localAI[0] + safeRange), yOffset, Vector2.UnitY * 20f);
                    Slime(basePos + Vector2.UnitX * (NPC.localAI[0] - safeRange), yOffset, Vector2.UnitY * 20f);
                }
            }
            if (++NPC.ai[1] > 180)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.ai[1] = 0;
            }

            const int masoMovingRainAttackTime = 180 * 3 - 60;
            if (WorldSavingSystem.MasochistModeReal && NPC.ai[1] == 120 && NPC.ai[2] < masoMovingRainAttackTime && Main.rand.NextBool(3))
                NPC.ai[2] = masoMovingRainAttackTime;

            NPC.velocity = Vector2.Zero;

            const int timeToMove = 240;
            if (WorldSavingSystem.MasochistModeReal)
            {
                if (NPC.ai[2] == masoMovingRainAttackTime)
                {
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                    EdgyBossText(NPC, GFBQuote(21));
                }

                if (NPC.ai[2] > masoMovingRainAttackTime + 30)
                {
                    if (NPC.ai[1] > 170) //let the balls keep falling
                        NPC.ai[1] -= 30;

                    if (NPC.localAI[1] == 0) //direction to move safespot towards
                    {
                        float safespotX = NPC.Center.X - 1200f + NPC.localAI[0];
                        NPC.localAI[1] = Math.Sign(NPC.Center.X - safespotX);
                    }

                    //move the safespot
                    //NPC.localAI[0] += 1000f / timeToMove * NPC.localAI[1];

                    NPC.Center += Vector2.UnitX * 1200f / timeToMove * NPC.localAI[1]; //move along with the movement已修改1000>1200

                }
                if (NPC.ai[2] > masoMovingRainAttackTime + 30 + 40 && NPC.ai[2] % 40 == 0)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center) * 20f, ModContent.ProjectileType<PHMutantSlimeSpearThrown>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                }
            }

            int endTime = 180 * 3;
            if (WorldSavingSystem.MasochistModeReal)
                endTime += timeToMove + (int)(300 * endTimeVariance) - 30;
            if (++NPC.ai[2] > endTime)
            {
                ChooseNextAttack(NPC, 11, 16, 19, 20, 28, WorldSavingSystem.MasochistModeReal ? 26 : 29, 31, 33, 37, 39, 41, 42, 45);
            }
        }

        private void QueenSlimeRain(NPC NPC, Player player)//48皇后史莱姆雨
        {
            if (NPC.ai[3] == 0)
            {
                NPC.ai[3] = 1;
                //Main.NewText(NPC.position.Y);
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSlimeRain>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);
            }

            if (NPC.ai[1] == 0) //telegraphs for where slime will fall
            {
                NPC.localAI[0] = Main.rand.Next(6, 9) * 120;
                //always start on the same side as the player
                if (player.Center.X > NPC.Center.X)
                    NPC.localAI[0] += 600;
                NPC.localAI[0] += 60;

                Vector2 basePos = NPC.Center;
                basePos.X -= 1200;
                for (int i = -360; i <= 2760; i += 120) //spawn telegraphs
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (i + 60 == (int)NPC.localAI[0])
                            continue;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), basePos.X + i + 60, basePos.Y, 0f, 0f, ModContent.ProjectileType<MutantReticle>(), 0, 0f, Main.myPlayer, ai2: 1);
                    }
                }
            }

            const int masoMovingRainAttackTime = 80;//

            if (NPC.ai[1] > masoMovingRainAttackTime && NPC.ai[1] % 3 == 0) //rain down slime balls
            {
                SoundEngine.PlaySound(SoundID.Item34, player.Center);
                if (FargoSoulsUtil.HostCheck)
                {
                    int frame = Main.rand.Next(3);

                    void Slime(Vector2 pos, float off, Vector2 vel)
                    {
                        const int flip = 1;
                        Vector2 spawnPos = pos + off * Vector2.UnitY * flip;
                        float ai0 = FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<PHMutantRitual>()) == null ? 0f : NPC.Distance(Main.projectile[ritualProj].Center);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, vel * flip, ModContent.ProjectileType<MutantSlimeSpike>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai2: frame);
                    }

                    Vector2 basePos = NPC.Center;
                    basePos.X -= 1200;
                    float yOffset = -1300;

                    const int safeRange = 110;
                    const int spacing = safeRange;
                    for (int i = 0; i < 2400; i += spacing)
                    {
                        float rightOffset = NPC.localAI[0] + safeRange + i;
                        if (basePos.X + rightOffset < NPC.Center.X + 1200)
                            Slime(basePos + Vector2.UnitX * rightOffset, yOffset, Vector2.UnitY * 20f);
                        float leftOffset = NPC.localAI[0] - safeRange - i;
                        if (basePos.X + leftOffset > NPC.Center.X - 1200)
                            Slime(basePos + Vector2.UnitX * leftOffset, yOffset, Vector2.UnitY * 20f);
                    }/*
                    if (NPC.ai[1] > masoMovingRainAttackTime + 60 && NPC.ai[1] % 60 == 0)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center) * 20f, ModContent.ProjectileType<PHMutantSlimeSpearThrown>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                    }
                    /*
                    for (int i = 0; i < 1; i++)
                    {
                        float ai0 = FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<PHMutantRitual>()) == null ? 0f : NPC.Distance(Main.projectile[ritualProj].Center);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, 4 * Vector2.UnitX.RotatedBy(MathHelper.PiOver4 * Main.rand.NextFloat(-1, 1)), ModContent.ProjectileType<PHMutantSlimeSpike>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai1: 90, ai2: frame);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, 4 * Vector2.UnitX.RotatedBy(MathHelper.PiOver4 * Main.rand.NextFloat(3, 5)), ModContent.ProjectileType<PHMutantSlimeSpike>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai1: 270, ai2: frame);
                    }*/
                }
            }

            NPC.velocity = Vector2.Zero;

            const int timeToMove = 360;
            if (NPC.ai[1] == masoMovingRainAttackTime)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                EdgyBossText(NPC, GFBQuote(21));
            }

            if (NPC.ai[1] > masoMovingRainAttackTime && --NPC.ai[2] < 0)
            {
                float safespotMoveSpeed = WorldSavingSystem.MasochistModeReal ? 7f : 6f;

                if (--NPC.localAI[2] < 0) //reset and recalibrate for the other direction
                {
                    float safespotX = NPC.Center.X - 1200f + NPC.localAI[0];
                    NPC.localAI[1] = Math.Sign(NPC.Center.X - safespotX); //direction to move safespot towards

                    float farSideArenaBorder = NPC.Center.X + 1200f * NPC.localAI[1];
                    float distanceToBorder = Math.Abs(farSideArenaBorder - safespotX);
                    float minRequiredDistance = Math.Abs(NPC.Center.X - safespotX) + 100;

                    float distanceToTravel = MathHelper.Lerp(minRequiredDistance, distanceToBorder, Main.rand.NextFloat(0.6f));

                    NPC.localAI[2] = distanceToTravel / safespotMoveSpeed;
                    NPC.ai[2] = WorldSavingSystem.MasochistModeReal ? 15 : 30; //adds a pause when turning around
                }

                //move the safespot
                NPC.localAI[0] += safespotMoveSpeed * NPC.localAI[1];
            }

            int endTime = masoMovingRainAttackTime + timeToMove + (int)(300 * endTimeVariance);
            if (++NPC.ai[1] > endTime)
            {
                ChooseNextAttack(NPC, 11, 16, 19, 20, WorldSavingSystem.MasochistModeReal ? 26 : 29, 31, 33, 37, 39, 41, 42, 45);
            }
        }

        private void PrepareFishron2(NPC NPC, Player player)//37准备猪鲨2
        {
            if (!AliveCheck(NPC, player))
                return;

            Vector2 targetPos = player.Center;
            targetPos.X += 400 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y -= 400;
            Movement(NPC, targetPos, 0.9f);

            if (++NPC.ai[1] > 60 || WorldSavingSystem.MasochistModeReal && NPC.Distance(targetPos) < 32) //dive here
            {
                NPC.velocity.X = 35f * (NPC.position.X < player.position.X ? 1 : -1);
                NPC.velocity.Y = 10f;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;
                //NPC.TargetClosest();

                EdgyBossText(NPC, GFBQuote(18));
            }
        }

        private void PrepareOkuuSpheresP2(NPC NPC, Player player)//39阿空圆环准备
        {
            if (!AliveCheck(NPC, player))
                return;
            Vector2 targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 450;
            if (++NPC.ai[1] < 180 && NPC.Distance(targetPos) > 50)
            {
                Movement(NPC, targetPos, 0.8f);
            }
            else
            {
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
            }
        }

        private void OkuuSpheresP2(NPC NPC)//30阿空圆环
        {
            NPC.velocity = Vector2.Zero;

            int endTime = 420 + (int)(300 * endTimeVariance);

            if (++NPC.ai[1] > 10 && NPC.ai[3] > 60 && NPC.ai[3] < endTime - 60)
            {
                NPC.ai[1] = 0;
                float rotation = MathHelper.ToRadians(80) * (NPC.ai[3] - 45) / 240 * NPC.ai[2];
                int max = WorldSavingSystem.MasochistModeReal ? 16 : 12;
                float speed = WorldSavingSystem.MasochistModeReal ? 9f : 8f;
                //SpawnSphereRing(NPC, max, speed, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), -1f, rotation);
                //SpawnSphereRing(NPC, max, speed, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 1f, rotation);
                SpawnPHSphereRing(NPC, max, speed, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0.9f, rotation);
                SpawnPHSphereRing(NPC, max, speed, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), -0.9f, rotation);

            }

            if (NPC.ai[2] == 0)
            {
                NPC.ai[2] = Main.rand.NextBool() ? -1 : 1;
                NPC.ai[3] = Main.rand.NextFloat((float)Math.PI * 2);
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -2);

                EdgyBossText(NPC, GFBQuote(22));
            }

            if (++NPC.ai[3] > endTime)
            {
                ChooseNextAttack(NPC, 13, 19, 20, WorldSavingSystem.MasochistModeReal ? 13 : 26, 28, WorldSavingSystem.MasochistModeReal ? 44 : 33, 41, 44, 49);
            }

            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }
        }

        private void SpawnSpearTossDirectP2Attack(NPC NPC, Player player)//生成投矛辅助方法
        {
            if (FargoSoulsUtil.HostCheck)
            {
                Vector2 vel = NPC.SafeDirectionTo(player.Center) * 30f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<MutantSpearThrown>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel.RotatedBy(MathHelper.TwoPi / 4), ModContent.ProjectileType<MutantSpearThrown>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel.RotatedBy(-MathHelper.TwoPi / 4), ModContent.ProjectileType<MutantSpearThrown>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target);
            }

            EdgyBossText(NPC, RandomObnoxiousQuote());
        }

        private void SpearTossDirectP2(NPC NPC, Player player)//41环绕投矛
        {
            if (!AliveCheck(NPC, player))
                return;

            if (NPC.ai[1] == 0)
            {
                NPC.localAI[0] = MathHelper.WrapAngle((NPC.Center - player.Center).ToRotation()); //remember initial angle offset

                //random max number of attacks
                if (WorldSavingSystem.EternityMode)
                    NPC.localAI[1] = Main.rand.Next(WorldSavingSystem.MasochistModeReal ? 3 : 5, 9);
                else
                    NPC.localAI[1] = 5;

                if (WorldSavingSystem.MasochistModeReal)
                {
                    NPC.localAI[1] += Main.rand.Next(6);
                    if (Main.getGoodWorld)
                        NPC.localAI[1] += 5;
                }
                NPC.localAI[2] = Main.rand.NextBool() ? -1 : 1; //pick a random rotation direction
                NPC.netUpdate = true;
            }

            //slowly rotate in full circle around player
            Vector2 targetPos = player.Center + 500f * Vector2.UnitX.RotatedBy(MathHelper.TwoPi / 300 * NPC.ai[3] * NPC.localAI[2] + NPC.localAI[0]);
            if (NPC.Distance(targetPos) > 25)
                Movement(NPC, targetPos, 0.6f);

            ++NPC.ai[3]; //for keeping track of how much time has actually passed (ai1 jumps around)

            if (++NPC.ai[1] > 180)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 150;

                bool shouldAttack = true;
                if (++NPC.ai[2] > NPC.localAI[1])
                {
                    if (Main.getGoodWorld) // Can't combo into slime rain in ftw
                        ChooseNextAttack(NPC, 11, 16, 19, 20, WorldSavingSystem.MasochistModeReal ? 44 : 26, 28, 31, 33, /*35,*/ 42, 44, 45, 47, 50);
                    else
                        ChooseNextAttack(NPC, 11, 16, 19, 20, WorldSavingSystem.MasochistModeReal ? 44 : 26, 28, 31, 33, 35, 42, 44, 45, 47);
                    shouldAttack = false;
                }

                if (shouldAttack || WorldSavingSystem.MasochistModeReal)
                {
                    SpawnSpearTossDirectP2Attack(NPC, player);
                }
            }
            else if (WorldSavingSystem.MasochistModeReal && NPC.ai[1] == 165)
            {
                SpawnSpearTossDirectP2Attack(NPC, player);
            }
            else if (NPC.ai[1] == 151)
            {
                if (NPC.ai[2] > 0 && (NPC.ai[2] < NPC.localAI[1] || WorldSavingSystem.MasochistModeReal) && FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 1);
            }
            else if (NPC.ai[1] == 1)
            {
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, -1);
            }
        }

        private void PrepareTwinRangsAndCrystals(NPC NPC, Player player)//42准备双子水晶
        {
            if (!AliveCheck(NPC, player))
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
            if (NPC.Distance(targetPos) > 50)
                Movement(NPC, targetPos, 0.8f);
            if (++NPC.ai[1] > 45)
            {
                NPC.netUpdate = true;
                NPC.ai[0]++;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                //NPC.TargetClosest();

                EdgyBossText(NPC, GFBQuote(23));
            }
        }

        private void TwinRangsAndCrystals(NPC NPC, Player player)//43双子水晶
        {
            NPC.velocity = Vector2.Zero;

            if (NPC.ai[3] == 0)
            {
                NPC.localAI[0] = NPC.DirectionFrom(player.Center).ToRotation();

                if (!WorldSavingSystem.MasochistModeReal && FargoSoulsUtil.HostCheck)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + Vector2.UnitX.RotatedBy(Math.PI / 2 * i) * 525, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + Vector2.UnitX.RotatedBy(Math.PI / 2 * i + Math.PI / 4) * 350, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 2f);
                    }
                }
            }

            int ringDelay = WorldSavingSystem.MasochistModeReal ? 12 : 15;
            int ringMax = WorldSavingSystem.MasochistModeReal ? 5 : 4;
            if (NPC.ai[3] % ringDelay == 0 && NPC.ai[3] < ringDelay * ringMax)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    float rotationOffset = MathHelper.TwoPi / ringMax * NPC.ai[3] / ringDelay + NPC.localAI[0];
                    int baseDelay = 60;
                    float flyDelay = 120 + NPC.ai[3] / ringDelay * (WorldSavingSystem.MasochistModeReal ? 40 : 50);
                    int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, 300f / baseDelay * Vector2.UnitX.RotatedBy(rotationOffset), ModContent.ProjectileType<MutantMark2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, baseDelay, baseDelay + flyDelay);
                    if (p != Main.maxProjectiles)
                    {
                        const int max = 5;
                        const float distance = 125f;
                        float rotation = MathHelper.TwoPi / max;
                        for (int i = 0; i < max; i++)
                        {
                            float myRot = rotation * i + rotationOffset;
                            Vector2 spawnPos = NPC.Center + new Vector2(distance, 0f).RotatedBy(myRot);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<MutantCrystalLeaf>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, Main.projectile[p].identity, myRot);
                        }
                    }
                }
            }

            if (NPC.ai[3] > 45 && --NPC.ai[1] < 0)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 20;
                NPC.ai[2] = NPC.ai[2] > 0 ? -1 : 1;

                SoundEngine.PlaySound(SoundID.Item92, NPC.Center);

                if (FargoSoulsUtil.HostCheck && NPC.ai[3] < 330)
                {
                    const float retiRad = 525;
                    const float spazRad = 350;
                    float retiSpeed = 2 * (float)Math.PI * retiRad / 300;
                    float spazSpeed = 2 * (float)Math.PI * spazRad / 180;
                    float retiAcc = retiSpeed * retiSpeed / retiRad * NPC.ai[2];
                    float spazAcc = spazSpeed * spazSpeed / spazRad * -NPC.ai[2];
                    float rotationOffset = WorldSavingSystem.MasochistModeReal ? MathHelper.PiOver4 : 0;
                    for (int i = 0; i < 4; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(Math.PI / 2 * i + rotationOffset) * retiSpeed, ModContent.ProjectileType<MutantRetirang>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, retiAcc, 300);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(Math.PI / 2 * i + Math.PI / 4 + rotationOffset) * spazSpeed, ModContent.ProjectileType<MutantSpazmarang>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, spazAcc, 180);
                    }
                }
            }
            if (++NPC.ai[3] > 450)
            {
                ChooseNextAttack(NPC, 11, 13, 16, 21, 24, 26, 29, 31, 33, 35, 39, 41, 44, 45, 47, 49, 50);
            }
        }

        private void EmpressSwordWave(NPC NPC, Player player)//44女皇剑阵
        {
            if (!AliveCheck(NPC, player))
                return;

            if (!WorldSavingSystem.EternityMode)
            {
                NPC.ai[0]++; //dont do this attack in expert
                return;
            }

            //Vector2 targetPos = player.Center + 360 * NPC.DirectionFrom(player.Center).RotatedBy(MathHelper.ToRadians(10)); Movement(targetPos, 0.25f);
            NPC.velocity = Vector2.Zero;
            int startup = 90;
            float multiple = 0.5f;
            int attackThreshold = (int)((WorldSavingSystem.MasochistModeReal ? 48 : 60) * multiple);
            int timesToAttack = (4 + (int)Math.Round(3 * endTimeVariance)) * 2;
            
            if (NPC.ai[1] == 0)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                NPC.ai[3] = Main.rand.NextFloat(MathHelper.TwoPi);

                EdgyBossText(NPC, GFBQuote(24));
            }

            void Sword(Vector2 pos, float ai0, float ai1, Vector2 vel)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), pos - vel * 60f, vel,
                        ProjectileID.FairyQueenLance, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai1);
                }
            }

            if (NPC.ai[1] >= startup && NPC.ai[1] < startup + attackThreshold * timesToAttack && --NPC.ai[2] < 0) //walls of swords
            {
                NPC.ai[2] = attackThreshold;

                SoundEngine.PlaySound(SoundID.Item163, player.Center);

                if (Math.Abs(MathHelper.WrapAngle(NPC.DirectionFrom(player.Center).ToRotation() - NPC.ai[3])) > MathHelper.PiOver2)
                    NPC.ai[3] += MathHelper.Pi; //swords always spawn closer to player

                int maxHorizSpread = (int)(1600 * 2);
                const int arenaRadius = 1200;
                int max = WorldSavingSystem.MasochistModeReal ? (Main.getGoodWorld ? 14 : 10) : 6;
                float gap = maxHorizSpread / max;

                float attackAngle = NPC.ai[3];// + Main.rand.NextFloat(MathHelper.ToDegrees(10)) * (Main.rand.NextBool() ? -1 : 1);
                Vector2 spawnOffset = -attackAngle.ToRotationVector2();

                //start by focusing on player
                Vector2 focusPoint = player.Center;

                //move focus point along grid closer so attack stays centered
                //Vector2 home = NPC.Center;// FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<PHMutantRitual>()) == null ? NPC.Center : Main.projectile[ritualProj].Center;
                /*
                for (float i = 0; i < arenaRadius; i += gap)
                {
                    Vector2 newFocusPoint = focusPoint + gap * attackAngle.ToRotationVector2();
                    if ((home - newFocusPoint).Length() > (home - focusPoint).Length())
                        break;
                    focusPoint = newFocusPoint;
                }
                */
                //doing it this way to guarantee it always remains aligned to grid
                float spawnDistance = 0;
                while (spawnDistance < arenaRadius)
                    spawnDistance += gap;

                float mirrorLength = 2f * (float)Math.Sqrt(2f * spawnDistance * spawnDistance);
                int swordCounter = 0;
                for (int i = -max; i <= max; i++)
                {
                    Vector2 spawnPos = focusPoint + spawnOffset * spawnDistance + spawnOffset.RotatedBy(MathHelper.PiOver2) * gap * i;
                    float Ai1 = swordCounter++ / (max * 2f + 1);

                    Vector2 randomOffset = Main.rand.NextVector2Unit();
                    if (WorldSavingSystem.MasochistModeReal)
                    {
                        if (randomOffset.Length() < 0.5f)
                            randomOffset = 0.5f * randomOffset.SafeNormalize(Vector2.UnitX);
                        randomOffset *= 2f;
                        randomOffset *= Main.getGoodWorld ? 1.2f : 1;//ftw改动
                    }
                    float intervel = MathHelper.Pi / 4;
                    Sword(spawnPos, attackAngle + intervel, Ai1, randomOffset);
                    Sword(spawnPos, attackAngle - intervel, Ai1, randomOffset);

                    if (WorldSavingSystem.MasochistModeReal)
                    {
                        Sword(spawnPos + mirrorLength * (attackAngle + intervel).ToRotationVector2(), attackAngle + intervel + MathHelper.Pi, Ai1, randomOffset);
                        Sword(spawnPos + mirrorLength * (attackAngle - intervel).ToRotationVector2(), attackAngle - intervel + MathHelper.Pi, Ai1, randomOffset);
                    }
                }

                NPC.ai[3] += MathHelper.PiOver4 / 6 + MathHelper.Pi / 2; //rotate 90 degrees
                                                                         //+ Main.rand.NextFloat(MathHelper.PiOver4 / 2) * (Main.rand.NextBool() ? -1 : 1); //variation

                NPC.netUpdate = true;
            }

            void MegaSwordSwarm(Vector2 target, bool turn = false)
            {
                SoundEngine.PlaySound(SoundID.Item164, player.Center);

                float safeAngle = NPC.ai[3];
                safeAngle += turn ? MathHelper.PiOver2 : 0;
                float safeRange = MathHelper.ToRadians(10);
                int max = 60;
                for (int i = 0; i < max; i++)
                {
                    float rotationOffset = Main.rand.NextFloat(safeRange, MathHelper.Pi - safeRange);
                    Vector2 offset = Main.rand.NextFloat(600f, 2400f) * (safeAngle + rotationOffset).ToRotationVector2();
                    if (Main.rand.NextBool())
                        offset *= -1;

                    //if (WorldSavingSystem.MasochistModeReal) //block one side so only one real exit exists
                    //    target += Main.rand.NextFloat(600) * safeAngle.ToRotationVector2();

                    Vector2 spawnPos = target + offset;
                    Vector2 vel = (target - spawnPos) / 60f;
                    Sword(spawnPos, vel.ToRotation(), (float)i / max, -0.75f * vel);
                }
                EdgyBossText(NPC, GFBQuote(25)); //you really didn't
            }



            //massive sword barrage
            int swordSwarmTime = startup + attackThreshold * timesToAttack + 40 + 20;//20为改动
            if (NPC.ai[1] == swordSwarmTime)
            {
                MegaSwordSwarm(player.Center);
                NPC.localAI[0] = player.Center.X;
                NPC.localAI[1] = player.Center.Y;
            }

            if (WorldSavingSystem.MasochistModeReal && NPC.ai[1] == swordSwarmTime + 30)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    MegaSwordSwarm(new Vector2(NPC.localAI[0], NPC.localAI[1]) + 600 * i * NPC.ai[3].ToRotationVector2());
                }
            }
            if (++NPC.ai[1] > swordSwarmTime + (WorldSavingSystem.MasochistModeReal ? Main.getGoodWorld ? 60 : 60 : 30))
            {
                ChooseNextAttack(NPC, 11, 13, 16, 21, WorldSavingSystem.MasochistModeReal ? 26 : 24, 28, 29, 31, 35, 37, 39, 41, 45, 47, 49, 50);
            }
        }

        private void SANSGOLEM(NPC NPC, Player player)//49鳝丝石巨人
        {
            Vector2 targetPos = player.Center + NPC.DirectionFrom(player.Center) * 300;
            Movement(NPC, targetPos, 0.3f);

            int attackDelay = WorldSavingSystem.MasochistModeReal ? Main.getGoodWorld ? 55 : 50 : 70;//减少ftw加速影响

            if (NPC.ai[1] > 0 && NPC.ai[1] % attackDelay == 0)
            {
                EdgyBossText(NPC, GFBQuote(35));
                float angle = Main.getGoodWorld ? Main.rand.Next(0, 3) * 30 : 0;
                Vector2 unitX = Vector2.UnitX.RotatedBy(MathHelper.ToRadians(angle));
                Vector2 unitY = unitX.RotatedBy(MathHelper.PiOver2);
                Vector2 centerPoint = FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<PHMutantRitual>()) == null ? player.Center : Main.projectile[ritualProj].Center;
                Vector2 goalPos = Main.rand.Next(-500, 501) * unitX + Main.rand.Next(-500, 501) * unitY;
                while ((goalPos - SansOldPos).Length() > 250)
                    goalPos = Main.rand.Next(-500, 501) * unitX + Main.rand.Next(-500, 501) * unitY;
                if (NPC.localAI[0] == 0)
                {
                    NPC.localAI[0] = 1;
                    goalPos = player.Center - centerPoint;
                    if (goalPos.Length() > 600)
                    {
                        goalPos = 600 * goalPos.SafeNormalize(Vector2.Zero);
                    }
                }
                centerPoint += goalPos;
                SansOldPos = goalPos;
                int travelTime = 50;
                const int timeToReachMiddle = 60;
                for (int i = -1; i <= 1; i += 2)//水平
                {
                    float SpeedWhenAttacking = Main.rand.NextFloat(10f, 20f);
                    int safegas = WorldSavingSystem.masochistModeReal ? 120 : 150;
                    for (int j = -1; j <= 1; j += 2)
                    {
                        Vector2 sansTargetPos = centerPoint;
                        sansTargetPos += SpeedWhenAttacking * timeToReachMiddle * i * unitX + safegas * j * unitY;
                        Vector2 vel = (sansTargetPos - NPC.Center) / travelTime;
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                ModContent.ProjectileType<PHMutantSansHead>(),
                                FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer,
                                travelTime, SpeedWhenAttacking * i * j, (j + 1) * 90 + angle);
                        }
                    }
                }
                for (int i = -1; i <= 1; i += 2)//垂直
                {
                    float SpeedWhenAttacking = Main.rand.NextFloat(10f, 20f);
                    int safegas = WorldSavingSystem.masochistModeReal ? 120 : 150;
                    for (int j = -1; j <= 1; j += 2)
                    {
                        Vector2 sansTargetPos = centerPoint;
                        sansTargetPos += 1.8f * safegas * i * unitX + SpeedWhenAttacking * timeToReachMiddle * j * unitY;
                        Vector2 vel = (sansTargetPos - NPC.Center) / travelTime;
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                ModContent.ProjectileType<PHMutantSansHead>(),
                                FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer,
                                travelTime, -SpeedWhenAttacking * i * j, (j + 1) * 90 + angle - i * j * 90);
                        }
                    }
                }
                NPC.netUpdate = true;
            }

            //(attacks + 1) - 5 to stop mutant from doing the last one, give a gap to next attack
            //doing the math round to make the endtimes discrete
            const int attacksToDo = 6;
            int endTime = attackDelay * (attacksToDo + 1) - 5 + attackDelay * (int)Math.Round(4 * endTimeVariance);
            if (++NPC.ai[1] > endTime)
            {
                SansOldPos = Vector2.Zero;
                ChooseNextAttack(NPC, 13, 19, 20, 21, 24, 31, 33,/* 35,*/ 41, 44, 50);
            }
        }
        /*
        private void WoFUpAndDown(NPC npc, Player player)//50血肉墙
        {
            Vector2 centerPoint = FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<PHMutantRitual>()) == null ? player.Center : Main.projectile[ritualProj].Center;
            Vector2 targetPos = centerPoint - 1200 * Vector2.UnitY;
            int starttime = WorldSavingSystem.MasochistModeReal ? 30 : 50;
            Movement(npc, targetPos, 0.3f);

            int attackDelay = 2 * 180;
            int timer = (int)npc.ai[1] - starttime;
            
            if (npc.ai[1] == starttime / 2)
            {
                for (int i = 500; i <= 1100; i += 190)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), centerPoint + i * Vector2.UnitX, Vector2.Zero, ModContent.ProjectileType<MutantWOFReticle>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0, Main.myPlayer, ai1 : 0);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), centerPoint - i * Vector2.UnitX, Vector2.Zero, ModContent.ProjectileType<MutantWOFReticle>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0, Main.myPlayer, ai1 : 0);
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<MutantPungentAuraProj>(), 0, 0, Main.myPlayer);
            }
            if (timer <= 3 * attackDelay && timer >= 0)
            {
                if (timer % attackDelay < attackDelay / 2)//诅咒
                {
                    if (timer % attackDelay == 0)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX, ModContent.ProjectileType<MutantCursedDeathray>(), 0, 0, Main.myPlayer, npc.whoAmI);
                }//诅咒火
                if (timer % attackDelay >= attackDelay / 2)//灵液
                {
                    if (timer % 8 == 0)
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            int max = WorldSavingSystem.MasochistModeReal ? 16 : 12;
                            int flip = 1;
                            for (int i = 0; i < max; i++)
                            {
                                flip *= -1;
                                Vector2 target = npc.Center;
                                target.Y += 1200f * (timer % (attackDelay / 2)) / 180f;
                                target.Y += Main.rand.NextFloat(-100, 100);
                                target.X += Main.rand.NextFloat(-450, 450);

                                const float gravity = 0.5f;
                                float time = 60f;
                                Vector2 distance = target - npc.Center;
                                distance.Y /= time;
                                distance.X = distance.X / time - 0.5f * gravity * time;
                                distance.X *= flip;

                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + Vector2.UnitY * 8f, distance,
                                    ModContent.ProjectileType<MutantGoldenShower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.75f), 0f, Main.myPlayer, time, 2f, flip);
                            }
                        }
                    }
                }//灵液
                /*
                if (timer % 60 == 0 && timer > 0)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        float distance = 800;
                        Vector2 vel = i * distance * Vector2.UnitX / 30;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MutantCrystalBomb>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    }
                }
                */
        /*
                if (timer > 30)
                {
                    if (timer % 3 == 0)
                    {
                        foreach (Player n in Main.player.Where(n => n.active))
                        {
                            PungentGazeBuffPlayer pgp = player.GetModPlayer<PungentGazeBuffPlayer>();
                            if (pgp.aimedCD >= 90)
                            {
                                pgp.aimedCD = 45;
                                Vector2 vel = npc.SafeDirectionTo(player.Center);
                                if (!pgp.Gazed)
                                {
                                    vel = vel.RotatedBy(Main.rand.NextFloat(MathHelper.Pi / 3, MathHelper.PiOver2) * (Main.rand.NextBool() ? 1 : -1));
                                    pgp.aimedCD = 0;
                                }
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MutantPhantasmalDeathrayWOFS>(),
                                    0, 0f, Main.myPlayer, ai1: npc.whoAmI); 
                            }
                        }
                    }
                }//检测剑客蛛丝
                if (timer % attackDelay == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        //Projectile.NewProjectile(npc.GetSource_FromThis(), player.Center + 50 * Vector2.UnitX.RotatedBy(Main.rand.NextFloat(0,MathHelper.TwoPi)), Vector2.Zero, ModContent.ProjectileType<MutantWOFReticle>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0, Main.myPlayer, ai1: 90);
                    }
                    
                }
            }
            
            if (timer > 3 * attackDelay + 40)
            {
                npc.ai[1] = 0;
                ChooseNextAttack(npc, 13, 19, 20, 21, 24, 28, 31, 33, 35, 41, 44);
            } 
            npc.ai[1]++;
        }
        */
        private void IronVirgin(NPC npc, Player player)//50肉山+世纪花
        {
            Vector2 centerPoint = FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<PHMutantRitual>()) == null ? player.Center : Main.projectile[ritualProj].Center;
            if (npc.localAI[0] == 0)
            {
                npc.localAI[0] = Main.rand.Next(500, 701);
                npc.localAI[1] = player.Center.Y - centerPoint.Y > 0 ? 1 : -1;
                npc.netUpdate = true;
            }
            Vector2 targetPos = centerPoint - npc.localAI[1] * npc.localAI[0] * Vector2.UnitY;
            int starttime = WorldSavingSystem.MasochistModeReal ? 60 : 90;
            Movement(npc, targetPos, 0.3f);

            //starttime后120帧内用诅咒火墙限制x位置，用灵液雨下压，之后向下两个垂直轨道扔铁处女叶绿水晶弹幕
            //诅咒焰+灵液下压
            if (npc.ai[1] >= starttime)
            {
                float offsetX = 1800 - 1200 * (npc.ai[1] - starttime) / 120f;
                if (offsetX < 400)
                {
                    offsetX = 400 - 5 * (npc.ai[1] - starttime - 140);
                }
                offsetX = offsetX < 80 ? 80 : offsetX;
                if (npc.ai[1] % 4 == 0 && npc.ai[1] < starttime + 140 + 64)
                {
                    SpawnCursedFlamesWall(npc, centerPoint + offsetX * Vector2.UnitX);
                    SpawnCursedFlamesWall(npc, centerPoint - offsetX * Vector2.UnitX);
                }
                if (npc.ai[1] < starttime + 180)
                {
                    float timer = npc.ai[1] - starttime;
                    if (timer % 8 == 0)
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            int max = WorldSavingSystem.MasochistModeReal ? 16 : 12;
                            int flip = 1;
                            for (int i = 0; i < max; i++)
                            {
                                flip *= -1;
                                Vector2 target = npc.Center;
                                target.Y += 800f * (timer % 180) / 180f * npc.localAI[1];
                                target.Y += Main.rand.NextFloat(-100, 100);
                                target.X += Main.rand.NextFloat(-450, 450);

                                const float gravity = 0.5f;
                                float time = 60f;
                                Vector2 distance = target - npc.Center;
                                distance.Y /= time;
                                distance.X = distance.X / time - 0.5f * gravity * time;
                                distance.X *= flip;

                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + Vector2.UnitY * 8f, distance,
                                    ModContent.ProjectileType<MutantGoldenShower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.75f), 0f, Main.myPlayer, time, 2f, flip);
                            }
                        }
                    }
                }
            }
            //准备铁处女
            if (npc.ai[1] >= starttime && npc.ai[1] <= starttime + 360)
            {
                int timer = (int)npc.ai[1] - starttime;
                int delay = 120;
                int offset = WorldSavingSystem.masochistModeReal ? Main.getGoodWorld ? 250 : 350 : 450;//maybe = 200
                int maxX = WorldSavingSystem.masochistModeReal && Main.getGoodWorld ? 2 : 1;
                
                if (timer % delay == 0)
                {
                    for (int i = -maxX; i <= maxX; i++)
                    {
                        Vector2 target = centerPoint + (2 * i - 0.5f) * offset * Vector2.UnitX - 1000 * Vector2.UnitY * npc.localAI[1];
                        Vector2 distance = target - npc.Center;
                        distance /= 60f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, distance,
                            ModContent.ProjectileType<IronVirgin>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.75f), 0f, Main.myPlayer, npc.localAI[1], -1, 0);
                    }
                    npc.netUpdate = true;
                }
                if (timer % delay == delay / 2)
                {
                    for (int i = -maxX; i <= maxX; i++)
                    {
                        Vector2 target = centerPoint + (2 * i + 0.5f) * offset * Vector2.UnitX - 1000 * Vector2.UnitY * npc.localAI[1];
                        Vector2 distance = target - npc.Center;
                        distance /= 60f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, distance,
                            ModContent.ProjectileType<IronVirgin>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.75f), 0f, Main.myPlayer, npc.localAI[1], 1, MathHelper.Pi / 4);
                    }
                    npc.netUpdate = true;
                }
                /*弃用的差分
                if (timer % (delay / 2) == 0)
                {
                    if (Main.rand.NextBool())
                    {
                        for (int i = -maxX; i <= maxX; i++)
                        {
                            Vector2 target = centerPoint + (2 * i - 0.5f) * offset * Vector2.UnitX - 1000 * Vector2.UnitY;
                            Vector2 distance = target - npc.Center;
                            distance /= 60f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, distance,
                                ModContent.ProjectileType<IronVirgin>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.75f), 0f, Main.myPlayer, 60, -1, 0);
                        }
                    }
                    else
                    {
                        for (int i = -maxX; i <= maxX; i++)
                        {
                            Vector2 target = centerPoint + (2 * i + 0.5f) * offset * Vector2.UnitX - 1000 * Vector2.UnitY;
                            Vector2 distance = target - npc.Center;
                            distance /= 60f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, distance,
                                ModContent.ProjectileType<IronVirgin>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.75f), 0f, Main.myPlayer, 60, 1, MathHelper.Pi / 4);
                        }
                    }
                }
                */
            }
            if (++npc.ai[1] > starttime + 360 + 120)
            {
                ChooseNextAttack(npc, 11, 13, 16, 21, 24, 26, 29, 31, 33, 35, 39, 41, 44, 45, 47, 49, 50);
                npc.netUpdate = true;
            }


        } 
        private void LieFlightBomb(NPC npc, Player player)//或与12.唐飞炸弹（hyw
        {
            npc.velocity = Vector2.Zero;
            int StartTime = WorldSavingSystem.MasochistModeReal ? 60 : WorldSavingSystem.EternityMode ? 75 : 85;
            if (npc.ai[1] == 0)
            {
                LieFlightPos = player.Center + 300 * Main.rand.NextVector2Unit();
                npc.velocity = Vector2.Zero;
                if (Main.rand.NextBool())
                {
                    npc.ai[0] = 18;
                    LieFlightPos = Vector2.Zero;
                }
                npc.netUpdate = true;
            }
            if (npc.ai[1] == 1)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), LieFlightPos, Vector2.Zero, ModContent.ProjectileType<LifeTpTelegraph>(), 0, 0f, Main.myPlayer, -60, npc.whoAmI);
                const int max = 16;
                for (int i = 0; i < max; i++)
                {
                    float angle = i * MathHelper.TwoPi / max;

                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), LieFlightPos, Vector2.Zero, ModContent.ProjectileType<BloomLine>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, 1, angle);
                    }
                }
                npc.netUpdate = true;
            }
            if (npc.ai[1] == StartTime)
            {
                npc.Center = LieFlightPos;
                npc.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                for (int i = 0; i < 16; i++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, new Vector2(24f, 0f).RotatedBy(MathHelper.Pi / 8 * i), ModContent.ProjectileType<LifeProjLarge>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 3f, Main.myPlayer);
                }
                FargoSoulsUtil.ScreenshakeRumble(30);

                //telegraph nukes
                for (int i = 0; i < 16; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), LieFlightPos, Vector2.Zero, ModContent.ProjectileType<BloomLine>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, 1, i * MathHelper.Pi / 4f);
                }
            }
            if (npc.ai[1] >= StartTime + 60 && (npc.ai[1] - (StartTime + 60)) % 3 == 0 && npc.ai[1] < StartTime + 60 + 47) //nukes
            {
                SoundEngine.PlaySound(SoundID.Item91, npc.Center);
                float ShotCount = (npc.ai[1] - (StartTime + 60)) / 3;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float ai0 = WorldSavingSystem.MasochistModeReal ? 32 : 24;
                    float ai1 = 0;
                    float speed = 20;
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, new Vector2(speed, 0f).RotatedBy(MathHelper.Pi / 4 * ShotCount), ModContent.ProjectileType<MutantLifeNuke>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 3f, Main.myPlayer, ai0, ai1);
                    //if (p != Main.maxProjectiles)
                    //Main.projectile[p].timeLeft = 60;
                }
            }
            if (++npc.ai[1] >= StartTime + 360 - 60)
            {
                npc.ai[1] = 0;
                LieFlightPos = Vector2.Zero;
                ChooseNextAttack(npc, 13, 19, 21, 24, 28, 31, 39, 41, 42, 49, 50);
                npc.netUpdate = true;
            }
        }
        /*
        private void WillAttack(NPC npc, Player player)
        {
            Vector2 targetPos = player.Center + npc.DirectionFrom(player.Center) * 300;
            Movement(npc, targetPos, 0.3f);
            if (++npc.ai[1] == 60) //spawn bomb 60帧后爆炸
            {
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);

                if (FargoSoulsUtil.HostCheck)
                {
                    float omigaflag = Main.rand.NextBool() ? -1 : 1;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 vel = Main.rand.NextFloat(0.6f, 1.5f) * (player.Center - npc.Center).RotatedByRandom(MathF.PI / 15) / 30f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MutantWillBomb>(), npc.defDamage / 4, 0f, Main.myPlayer, 0f, npc.whoAmI, omigaflag);
                    }
                }
            }
            else if (npc.ai[1] > 150)
            {
                if (npc.ai[1] % 60 == 0)//意志投矛
                {
                    int max = WorldSavingSystem.masochistModeReal ? Main.getGoodWorld ? 4 : 3 : 2;
                    float omiga = 0f;
                    int delay = npc.ai[1] == 180 ? 30 : 0;
                    if (WorldSavingSystem.MasochistModeReal) // evil spin
                    {
                        omiga = Main.rand.NextBool() ? 1 : -1;
                        omiga *= MathF.Tau / 360;
                    }
                    SpawnWillJavelin(npc, player.Center, max, Main.rand.NextFloat(MathF.Tau), omiga, delay);
                }
            }
            if (npc.ai[1] > 120 + 300)
            {
                npc.ai[1] = 0;
                ChooseNextAttack(npc, 13, 19, 21, 24, 31, 39, 41, 42, 49, 50);
            }
        }
        */
        private void CoffinWave(NPC npc, Player player)//28棺材波动 + 意志金雷
        {
            // NPC保持和玩家距离300
            Vector2 targetPos = player.Center + npc.DirectionFrom(player.Center) * 300;
            Movement(npc, targetPos, 0.3f);
            Vector2 centerPoint = FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<PHMutantRitual>()) == null
                ? player.Center : Main.projectile[ritualProj].Center;
            float speed = 18f;
            float displacementAmplitude = 2 * speed * (MathHelper.PiOver2 * 0.7f) * 50f / MathHelper.TwoPi;

            float difficultyMultiplier = WorldSavingSystem.masochistModeReal ? Main.getGoodWorld ? Main.zenithWorld ? 1f : 1f : 2f : 2f;

            float spacing = difficultyMultiplier * displacementAmplitude; // 间隔 = 位移振幅 × 2
            void SpawnCoffinWave(Vector2 spawnCenter, int flag, float Minister)
            {
                if (FargoSoulsUtil.HostCheck)
                {

                    Vector2 velocity = flag * new Vector2(speed, 0f);
                    float verticalRange = 1200f; // ±600 垂直覆盖范围
                    int countPerSide = (int)(verticalRange / spacing);
                    for (int i = -countPerSide; i <= countPerSide; i++)
                    {
                        Vector2 spawnPos = spawnCenter + i * spacing * Vector2.UnitY;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, velocity,
                            ModContent.ProjectileType<MutantCoffinWaveShot>(),
                            FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, Minister, ai2 : npc.whoAmI);
                    }
                }
            }//生成弹幕辅助方法
            if (npc.localAI[0] == 0)
            {
                SoundEngine.PlaySound(SoundID.Item163, player.Center);
                npc.localAI[0] = 1;
                npc.localAI[1] = Main.rand.Next(-100,101);
                npc.localAI[2] = Main.rand.Next(-50, 51);
                EdgyBossText(npc, GFBQuote(11));
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, -2);
                npc.netUpdate = true;
            }
            if (/*npc.ai[1] >= 60 && */npc.ai[1] % 10 == 0 && npc.ai[1] <= 420)
            {   
                float minister = -2f * npc.ai[1] * MathHelper.Pi / 180f;
                Vector2 otherpos = new (npc.localAI[1], npc.localAI[2]);
                SpawnCoffinWave(centerPoint + otherpos + 1800 * Vector2.UnitX - 0.5f * displacementAmplitude * (1f - MathF.Cos(minister)) * Vector2.UnitY, -1, minister);
                SpawnCoffinWave(centerPoint + otherpos - 1800 * Vector2.UnitX + 0.5f * displacementAmplitude * (1f - MathF.Cos(minister)) * Vector2.UnitY, 1, minister);

            }
            if (npc.localAI[0] == 1 && npc.ai[1] > 4 * 60 - 30)
            {
                npc.localAI[0] = 2;//用于给诅咒棺材弹幕传参决定是否显现
                npc.netUpdate = true;
            }

            // 360帧后结束发射，等待60帧后选择下一招
            if (npc.ai[1] == 420 + 120)
            {
                for (int i = 0; i <= 6; i++)
                {
                    if (i == 3) i++;
                    float angle = (20 * i + 30) * MathHelper.Pi / 180f;
                    Vector2 Pos = new Vector2(600, 0).RotatedBy(angle + MathF.PI) + player.Center;
                    Vector2 vel = (Pos - npc.Center) / 30f;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MutantWillBomb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 
                        MathHelper.PiOver4, npc.whoAmI, i > 3 ? 1 : -1);
                }
                npc.netUpdate = true;
            }

            if (++npc.ai[1] > 420 + 120 + 120)
            {
                ChooseNextAttack(npc, 13, 19, 20, 21, 24, 31, 33, /*35,*/ 41, 44, 50);
                npc.netUpdate = true;
            }
            
        }
        private void P2NextAttackPause(NPC NPC, Player player) //choose next attack but actually, this also gives breathing space for mp to sync up
        {
            if (!AliveCheck(NPC, player))
                return;

            EModeSpecialEffects(NPC); //manage these here, for case where players log out/rejoin in mp

            Vector2 targetPos = player.Center + NPC.DirectionFrom(player.Center) * 400;
            Movement(NPC, targetPos, 0.3f);
            if (NPC.Distance(targetPos) > 200) //faster if offscreen
                Movement(NPC, targetPos, 0.3f);

            if (++NPC.ai[1] > 60 || NPC.Distance(targetPos) < 200 && NPC.ai[1] > (NPC.localAI[3] >= 3 ? 15 : 30))
            {
                /*EModeGlobalNPC.PrintAI(npc);
                string output = "";
                foreach (float attack in attackHistory)
                    output += attack.ToString() + " ";
                Main.NewText(output);*/

                NPC.velocity *= WorldSavingSystem.MasochistModeReal ? 0.25f : 0.75f;

                //NPC.TargetClosest();
                NPC.ai[0] = NPC.ai[2];
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.netUpdate = true;

                EdgyBossText(NPC, RandomObnoxiousQuote());
            }
        }

        #endregion P2

        #region P3

        private bool Phase3Transition(NPC NPC, Player player)//P3转阶段
        {
            bool retval = true;

            NPC.localAI[3] = 3;

            EModeSpecialEffects(NPC);

            //NPC.damage = 0;
            if (NPC.buffType[0] != 0)
                NPC.DelBuff(0);

            if (NPC.ai[1] == 0) //entering final phase, give healing
            {
                NPC.life = NPC.lifeMax;

                DramaticTransition(NPC, true);
            }

            if (NPC.ai[1] < 60 && !Main.dedServ && Main.LocalPlayer.active)
                FargoSoulsUtil.ScreenshakeRumble(6);

            if (NPC.ai[1] == 360)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            }

            if (++NPC.ai[1] > 480)
            {
                retval = false; //dont drain life during this time, ensure it stays synced

                if (!AliveCheck(NPC, player))
                    return retval;
                Vector2 targetPos = player.Center;
                targetPos.Y -= 300;
                Movement(NPC, targetPos, 1f, true, false);
                if (NPC.Distance(targetPos) < 50 || NPC.ai[1] > 720)
                {
                    NPC.netUpdate = true;
                    NPC.velocity = Vector2.Zero;
                    NPC.localAI[0] = 0;
                    NPC.ai[0]--;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = NPC.DirectionFrom(player.Center).ToRotation();
                    NPC.ai[3] = (float)Math.PI / 20f;
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    if (player.Center.X < NPC.Center.X)
                        NPC.ai[3] *= -1;
                    EdgyBossText(NPC, GFBQuote(26));
                }
            }
            else
            {
                NPC.velocity *= 0.9f;

                //make you stop attacking
                if (Main.LocalPlayer.active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost && NPC.Distance(Main.LocalPlayer.Center) < 3000)
                {
                    Main.LocalPlayer.controlUseItem = false;
                    Main.LocalPlayer.controlUseTile = false;
                    Main.LocalPlayer.FargoSouls().NoUsingItems = 2;
                }

                if (--NPC.localAI[0] < 0)
                {
                    NPC.localAI[0] = Main.rand.Next(15);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 spawnPos = NPC.position + new Vector2(Main.rand.Next(NPC.width), Main.rand.Next(NPC.height));
                        int type = ModContent.ProjectileType<MutantBombSmall>();
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer);
                    }
                }
            }

            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }

            return retval;
        }

        private void VoidRaysP3(NPC NPC)//P3虚无射线
        {
            if (--NPC.ai[1] < 0)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    float speed = WorldSavingSystem.MasochistModeReal && NPC.localAI[0] <= 40 ? 4f : 2f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed * Vector2.UnitX.RotatedBy(NPC.ai[2]), ModContent.ProjectileType<MutantMark1>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                }
                NPC.ai[1] = 1;
                NPC.ai[2] += NPC.ai[3];

                if (NPC.localAI[0] < 30)
                {
                    EModeSpecialEffects(NPC);
                    TryMasoP3Theme(NPC);
                }

                if (NPC.localAI[0]++ == 40 || NPC.localAI[0] == 80 || NPC.localAI[0] == 120)
                {
                    NPC.netUpdate = true;
                    NPC.ai[2] -= NPC.ai[3] / (WorldSavingSystem.MasochistModeReal ? 3 : 2);
                }
                else if (NPC.localAI[0] >= (WorldSavingSystem.MasochistModeReal ? 160 : 120))
                {
                    NPC.netUpdate = true;
                    NPC.ai[0]--;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    NPC.localAI[0] = 0;
                    EdgyBossText(NPC, GFBQuote(27));
                }
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }

            NPC.velocity = Vector2.Zero;
        }

        private void OkuuSpheresP3(NPC NPC, Player player)//阿空圆环
        {
            if (NPC.ai[2] == 0)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.ai[2] = Main.rand.NextBool() ? -1 : 1;
                NPC.ai[3] = Main.rand.NextFloat((float)Math.PI * 2);
            }

            int endTime = 360 + 120;
            if (WorldSavingSystem.MasochistModeReal)
                endTime += 360;

            if (++NPC.ai[1] > 10 && NPC.ai[3] > 60 && NPC.ai[3] < endTime - 120)
            {
                NPC.ai[1] = 0;
                float rotation = MathHelper.ToRadians(45) * (NPC.ai[3] - 60) / 240 * NPC.ai[2];
                int max = WorldSavingSystem.MasochistModeReal ? 11 : 10;
                float speed = WorldSavingSystem.MasochistModeReal ? 11f : 10f;
                SpawnSphereRing(NPC, max, speed, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), -0.75f, rotation);
                SpawnSphereRing(NPC, max, speed, FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0.75f, rotation);
            }

            if (NPC.ai[3] < 30)
            {
                EModeSpecialEffects(NPC);
                TryMasoP3Theme(NPC);
            }
            if (NPC.ai[3] == (int)(endTime / 2))
            {
                EdgyBossText(NPC, GFBQuote(28));
            }
            if (++NPC.ai[3] > endTime)
            {
                NPC.netUpdate = true;
                NPC.ai[0]--;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                EdgyBossText(NPC, GFBQuote(29));
                //NPC.TargetClosest();
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }

            NPC.velocity = Vector2.Zero;
        }

        private void BoundaryBulletHellP3(NPC NPC, Player player)//波粒
        {
            if (NPC.localAI[0] == 0)
            {
                if (!AliveCheck(NPC, player))
                    return;
                NPC.localAI[0] = Math.Sign(NPC.Center.X - player.Center.X);
            }

            if (++NPC.ai[1] > 3)
            {
                SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                NPC.ai[1] = 0;
                NPC.ai[2] += (float)Math.PI / 5 / 420 * NPC.ai[3] * NPC.localAI[0] * (WorldSavingSystem.MasochistModeReal ? 2f : 1);
                if (NPC.ai[2] > (float)Math.PI)
                    NPC.ai[2] -= (float)Math.PI * 2;
                if (FargoSoulsUtil.HostCheck)
                {
                    int max = WorldSavingSystem.MasochistModeReal ? 10 : 8;
                    for (int i = 0; i < max; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0f, -6f).RotatedBy(NPC.ai[2] + MathHelper.TwoPi / max * i),
                            ModContent.ProjectileType<PHMutantEye>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                    }
                }
            }

            if (NPC.ai[3] < 30)
            {
                EModeSpecialEffects(NPC);
                TryMasoP3Theme(NPC);
            }

            int endTime = 360;
            if (WorldSavingSystem.MasochistModeReal)
                endTime += 360;
            if (NPC.ai[3] == (int)endTime / 2)
            {
                EdgyBossText(NPC, GFBQuote(30));
            }
            if (++NPC.ai[3] > endTime)
            {
                //NPC.TargetClosest();
                NPC.ai[0]--;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                NPC.localAI[0] = 0;
                NPC.netUpdate = true;
            }

            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }

            NPC.velocity = Vector2.Zero;
        }

        private void FinalSpark(NPC NPC, Player player)//最终火花
        {
            void SpinLaser(bool useMasoSpeed)
            {
                float newRotation = NPC.SafeDirectionTo(Main.player[NPC.target].Center).ToRotation();
                float difference = MathHelper.WrapAngle(newRotation - NPC.ai[3]);
                float rotationDirection = 2f * (float)Math.PI * 1f / 6f / 60f;
                rotationDirection *= useMasoSpeed ? 1.1f : 1f;
                float change = Math.Min(rotationDirection, Math.Abs(difference)) * Math.Sign(difference);
                if (useMasoSpeed)
                {
                    change *= 1.1f;
                    float angleLerp = NPC.ai[3].AngleLerp(newRotation, 0.015f) - NPC.ai[3];
                    if (Math.Abs(MathHelper.WrapAngle(angleLerp)) > Math.Abs(MathHelper.WrapAngle(change)))
                        change = angleLerp;
                }
                NPC.ai[3] += change;

                EdgyBossText(NPC, GFBQuote(31));
            }

            /*
            //if targets are all dead, will despawn much more aggressively to reduce respawn cheese
            if (NPC.localAI[2] > 30)
            {
                NPC.localAI[2] += 1; //after 30 ticks of no target, despawn can't be stopped
                if (NPC.localAI[2] > 120)
                    AliveCheck(player, true);
                return;
            }
            */
            if (!AliveCheck(NPC, player))
                return;

            if (--NPC.localAI[0] < 0) //just visual explosions
            {
                NPC.localAI[0] = Main.rand.Next(30);
                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 spawnPos = NPC.position + new Vector2(Main.rand.Next(NPC.width), Main.rand.Next(NPC.height));
                    int type = ModContent.ProjectileType<MutantBombSmall>();
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer);
                }
            }

            bool harderRings = WorldSavingSystem.MasochistModeReal && NPC.ai[2] >= 420 - 90;
            int ringTime = harderRings ? 100 : 120;
            if (++NPC.ai[1] > ringTime)
            {
                NPC.ai[1] = 0;

                EModeSpecialEffects(NPC);
                TryMasoP3Theme(NPC);

                if (FargoSoulsUtil.HostCheck)
                {
                    int max = /*harderRings ? 11 :*/ 10;
                    int damage = FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage);
                    SpawnSphereRing(NPC, max, 6f, damage, 0.5f);
                    SpawnSphereRing(NPC, max, 6f, damage, -.5f);
                }
            }

            if (NPC.ai[2] == 0)
            {
                if (!WorldSavingSystem.MasochistModeReal)
                    NPC.localAI[1] = 1;
            }
            else if (NPC.ai[2] == 420 - 90) //dramatic telegraph
            {
                if (NPC.localAI[1] == 0) //maso do ordinary spark
                {
                    NPC.localAI[1] = 1;
                    NPC.ai[2] -= 600 + 180;

                    //bias in one direction
                    NPC.ai[3] -= MathHelper.ToRadians(20);

                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(NPC.ai[3]),
                            ModContent.ProjectileType<MutantGiantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.5f), 0f, Main.myPlayer, 0, NPC.whoAmI);
                    }

                    NPC.netUpdate = true;
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                    if (FargoSoulsUtil.HostCheck)
                    {
                        const int max = 8;
                        for (int i = 0; i < max; i++)
                        {
                            float offset = i - 0.5f;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.ai[3] + MathHelper.TwoPi / max * offset).ToRotationVector2(), ModContent.ProjectileType<GlowLine>(), 0, 0f, Main.myPlayer, 13f, NPC.whoAmI);
                        }
                    }
                }
            }

            if (NPC.ai[2] < 420)
            {
                //disable it while doing maso's first ray
                if (NPC.localAI[1] == 0 || NPC.ai[2] > 420 - 90)
                    NPC.ai[3] = NPC.DirectionFrom(player.Center).ToRotation(); //hold it here for glow line effect
            }
            else
            {
                if (!Main.dedServ)
                {
                    ManagedScreenFilter filter = ShaderManager.GetFilter("FargowiltasSouls.FinalSpark");
                    filter.Activate();
                    if (SoulConfig.Instance.ForcedFilters && Main.WaveQuality == 0)
                        Main.WaveQuality = 1;
                }

                if (NPC.ai[1] % 3 == 0 && FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, 24f * Vector2.UnitX.RotatedBy(NPC.ai[3]), ModContent.ProjectileType<PHMutantEyeWavy>(), 0, 0f, Main.myPlayer,
                      Main.rand.NextFloat(0.5f, 1.25f) * (Main.rand.NextBool() ? -1 : 1), Main.rand.Next(10, 60));
                }
            }

            int endTime = 1020;
            if (WorldSavingSystem.MasochistModeReal)
                endTime += 180;
            if (++NPC.ai[2] > endTime && NPC.life <= 1)
            {
                NPC.netUpdate = true;
                NPC.ai[0]--;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                FargoSoulsUtil.ClearAllProjectiles(2, NPC.whoAmI);
            }
            else if (NPC.ai[2] == 420)
            {
                NPC.netUpdate = true;

                //bias it in one direction
                NPC.ai[3] += MathHelper.ToRadians(20) * (WorldSavingSystem.MasochistModeReal ? 1 : -1);

                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(NPC.ai[3]),
                        ModContent.ProjectileType<MutantGiantDeathray2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.5f), 0f, Main.myPlayer, 0, NPC.whoAmI);
                }
            }
            else if (NPC.ai[2] < 300 && NPC.localAI[1] != 0) //charging up dust
            {
                float num1 = 0.99f;
                if (NPC.ai[2] >= 60)
                    num1 = 0.79f;
                if (NPC.ai[2] >= 120)
                    num1 = 0.58f;
                if (NPC.ai[2] >= 180)
                    num1 = 0.43f;
                if (NPC.ai[2] >= 240)
                    num1 = 0.33f;
                for (int i = 0; i < 9; ++i)
                {
                    if (Main.rand.NextFloat() >= num1)
                    {
                        float f = Main.rand.NextFloat() * 6.283185f;
                        float num2 = Main.rand.NextFloat();
                        Dust dust = Dust.NewDustPerfect(NPC.Center + f.ToRotationVector2() * (110 + 600 * num2), DustID.Vortex, (f - 3.141593f).ToRotationVector2() * (14 + 8 * num2), 0, default, 1f);
                        dust.scale = 0.9f;
                        dust.fadeIn = 1.15f + num2 * 0.3f;
                        //dust.color = new Color(1f, 1f, 1f, num1) * (1f - num1);
                        dust.noGravity = true;
                        //dust.noLight = true;
                    }
                }
            }

            SpinLaser(WorldSavingSystem.MasochistModeReal && NPC.ai[2] >= 420);

            if (AliveCheck(NPC, player))
                NPC.localAI[2] = 0;
            else
                NPC.localAI[2]++;

            NPC.velocity = Vector2.Zero; //prevents mutant from moving despite calling AliveCheck()
        }

        private void DyingDramaticPause(NPC NPC, Player player)
        {
            if (!AliveCheck(NPC, player))
                return;
            NPC.ai[3] -= (float)Math.PI / 6f / 60f;
            NPC.velocity = Vector2.Zero;
            //in maso, if player got timestopped at very end of final spark, fucking kill them
            bool killPlayer = WorldSavingSystem.MasochistModeReal && Main.player[NPC.target].HasBuff(ModContent.BuffType<TimeFrozenBuff>());
            if (killPlayer)
            {
                if (++NPC.ai[2] > 15)
                {
                    NPC.ai[2] -= 15;
                    int realDefDamage = NPC.defDamage;
                    NPC.defDamage *= 10;
                    SpawnSpearTossDirectP2Attack(NPC, player);
                    NPC.defDamage = realDefDamage;
                }
            }
            else if (++NPC.ai[1] > 120)
            {
                NPC.netUpdate = true;
                NPC.ai[0]--;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = (float)-Math.PI / 2;
                NPC.netUpdate = true;
                if (FargoSoulsUtil.HostCheck) //shoot death anim mega ray
                {
                    int damage = WorldSavingSystem.MasochistModeReal ? FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 0.5f) : 0;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -1,
                        ModContent.ProjectileType<MutantGiantDeathray2>(),
                        damage, 0f, Main.myPlayer, 1, NPC.whoAmI);
                }
                EdgyBossText(NPC, GFBQuote(32));
            }
            if (--NPC.localAI[0] < 0)
            {
                NPC.localAI[0] = Main.rand.Next(15);
                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 spawnPos = NPC.position + new Vector2(Main.rand.Next(NPC.width), Main.rand.Next(NPC.height));
                    int type = ModContent.ProjectileType<MutantBomb>();
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer);
                }
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }
        }

        private void DyingAnimationAndHandling(NPC NPC)//死亡动画
        {
            /*if (WorldSavingSystem.MasochistModeReal)
            {
                if (!AliveCheck(player))
                    return;
                i'm not THAT fucked up
            }*/
            NPC.velocity = Vector2.Zero;
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, 0f, 0f, 0, default, 2.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 12f;
            }
            if (--NPC.localAI[0] < 0)
            {
                NPC.localAI[0] = Main.rand.Next(5);
                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 spawnPos = NPC.Center + Main.rand.NextVector2Circular(240, 240);
                    int type = ModContent.ProjectileType<MutantBomb>();
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer);
                }
            }
            if (++NPC.ai[1] % 3 == 0 && FargoSoulsUtil.HostCheck)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, 24f * Vector2.UnitX.RotatedBy(NPC.ai[3]), ModContent.ProjectileType<PHMutantEyeWavy>(), 0, 0f, Main.myPlayer,
                    Main.rand.NextFloat(0.75f, 1.5f) * (Main.rand.NextBool() ? -1 : 1), Main.rand.Next(10, 90));
            }
            if (++NPC.alpha > 255)
            {
                NPC.alpha = 255;
                NPC.life = 0;
                NPC.dontTakeDamage = false;
                NPC.checkDead();
                // 显式设置 ai[0] 为 -7 以确保 CheckDead 能正确判断
                NPC.ai[0] = -7;

                NPC.checkDead();

                // checkDead 后，NPC 应该已经死亡，后面的代码不会再影响
                // 但还是加一个判断以防万一
                if (!NPC.active)
                    return;
                if (FargoSoulsUtil.HostCheck && ModContent.TryFind("Fargowiltas", "Mutant", out ModNPC modNPC) && !NPC.AnyNPCs(modNPC.Type))
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
                EdgyBossText(NPC, GFBQuote(33));
            }
        }

        #endregion P3

        #region 方法移植

        private bool spawned;

        private void ManageAurasAndPreSpawn(NPC npc, Player player)
        {
            if (!spawned)
            {
                spawned = true;

                int prevLifeMax = npc.lifeMax;
                if (WorldSavingSystem.AngryMutant) //doing it here to avoid overflow i think
                {
                    npc.lifeMax *= 100;
                    if (npc.lifeMax < prevLifeMax)
                        npc.lifeMax = int.MaxValue;
                }
                npc.life = npc.lifeMax;

                if (player.FargoSouls().TerrariaSoul && WorldSavingSystem.MasochistModeReal)
                    EdgyBossText(npc, GFBQuote(1));
            }

            if (WorldSavingSystem.MasochistModeReal && Main.LocalPlayer.active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost)
                Main.LocalPlayer.AddBuff(ModContent.BuffType<MutantPresenceBuff>(), 2);

            if (npc.localAI[3] == 0)
            {
                npc.TargetClosest();
                if (npc.timeLeft < 30)
                    npc.timeLeft = 30;
                if (npc.Distance(Main.player[npc.target].Center) < 1500)
                {
                    npc.localAI[3] = 1;
                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    EdgyBossText(npc, GFBQuote(2));
                    if (FargoSoulsUtil.HostCheck)
                    {
                        //if (FargowiltasSouls.Instance.MasomodeEXLoaded) Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModLoader.GetMod("MasomodeEX").ProjectileType("MutantText"), 0, 0f, Main.myPlayer, NPC.whoAmI);

                        if (WorldSavingSystem.AngryMutant && WorldSavingSystem.MasochistModeReal)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BossRush>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                    }
                }
            }
            else if (npc.localAI[3] == 1)
            {
                ShouldDrawAura = true;
                // -1 means no dust is drawn, as it looks ugly.
                ArenaAura(AuraCenter, 2000f * AuraScale, true, -1, default, ModContent.BuffType<GodEaterBuff>(), ModContent.BuffType<MutantFangBuff>());
            }
            else
            {
                if (Main.LocalPlayer.active && npc.Distance(Main.LocalPlayer.Center) < 3000f)
                {
                    if (Main.expertMode)
                    {
                        Main.LocalPlayer.AddBuff(ModContent.BuffType<MutantPresenceBuff>(), 2);
                        if (Main.getGoodWorld)
                            Main.LocalPlayer.AddBuff(ModContent.BuffType<GoldenStasisCDBuff>(), 2);
                    }

                    if (WorldSavingSystem.EternityMode && npc.ai[0] < 0 && npc.ai[0] > -6)
                    {
                        Main.LocalPlayer.AddBuff(ModContent.BuffType<GoldenStasisCDBuff>(), 2);
                        if (WorldSavingSystem.MasochistModeReal)
                        {
                            Main.LocalPlayer.AddBuff(ModContent.BuffType<TimeStopCDBuff>(), 2);
                            Main.LocalPlayer.AddBuff(ModContent.BuffType<MutantDesperationBuff>(), 2);
                        }
                    }
                    //if (FargowiltasSouls.Instance.CalamityLoaded)
                    //{
                    //    Main.LocalPlayer.buffImmune[ModLoader.GetMod("CalamityMod").BuffType("RageMode")] = true;
                    //    Main.LocalPlayer.buffImmune[ModLoader.GetMod("CalamityMod").BuffType("AdrenalineMode")] = true;
                    //}
                }
            }
        }

        private void ManageNeededProjectiles(NPC NPC)
        {
            if (FargoSoulsUtil.HostCheck) //checks for needed projs
            {
                if (WorldSavingSystem.EternityMode && NPC.ai[0] != -7 && (NPC.ai[0] < 0 || NPC.ai[0] > 10) && FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<PHMutantRitual>()) == null)
                    ritualProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<PHMutantRitual>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);

                if (FargoSoulsUtil.ProjectileExists(ringProj, ModContent.ProjectileType<MutantRitual5>()) == null)
                    ringProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual5>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);

                if (FargoSoulsUtil.ProjectileExists(spriteProj, ModContent.ProjectileType<MutantBossProjectile>()) == null)
                {
                    /*if (Main.netMode == NetmodeID.Server)
                        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("wheres my sprite"), Color.LimeGreen);
                    else
                        Main.NewText("wheres my sprite");*/
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
                            Projectile projectile = Main.projectile[number];
                            projectile.SetDefaults(ModContent.ProjectileType<MutantBossProjectile>());
                            projectile.Center = NPC.Center;
                            projectile.owner = Main.myPlayer;
                            projectile.velocity.X = 0;
                            projectile.velocity.Y = 0;
                            projectile.damage = 0;
                            projectile.knockBack = 0f;
                            projectile.identity = number;
                            projectile.gfxOffY = 0f;
                            projectile.stepSpeed = 1f;
                            projectile.ai[1] = NPC.whoAmI;

                            spriteProj = number;
                        }
                    }
                    else //server
                    {
                        spriteProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantBossProjectile>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                        /*if (Main.netMode == NetmodeID.Server)
                            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"got sprite {spriteProj}"), Color.LimeGreen);
                        else
                            Main.NewText($"got sprite {spriteProj}");*/
                    }
                }
            }
        }

        private bool AliveCheck(NPC npc, Player p, bool forceDespawn = false)
        {
            if (WorldSavingSystem.SwarmActive || forceDespawn || (!p.active || p.dead || Vector2.Distance(npc.Center, p.Center) > 3000f) && npc.localAI[3] > 0)
            {
                npc.TargetClosest();
                p = Main.player[npc.target];
                if (WorldSavingSystem.SwarmActive || forceDespawn || !p.active || p.dead || Vector2.Distance(npc.Center, p.Center) > 3000f)
                {
                    if (npc.timeLeft > 30)
                        npc.timeLeft = 30;
                    npc.velocity.Y -= 1f;
                    if (npc.timeLeft == 1)
                    {
                        EdgyBossText(npc, GFBQuote(36));
                        if (npc.position.Y < 0)
                            npc.position.Y = 0;
                        if (FargoSoulsUtil.HostCheck && ModContent.TryFind("Fargowiltas", "Mutant", out ModNPC modNPC) && !NPC.AnyNPCs(modNPC.Type))
                        {
                            FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);
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
                    }
                    return false;
                }
            }

            if (npc.timeLeft < 3600)
                npc.timeLeft = 3600;
            /*
            if (p.Center.Y / 16f > Main.worldSurface)//存在改动
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

        private void ChooseNextAttack(NPC NPC, params int[] args)
        {
            float buffer = NPC.ai[0] + 1;
            NPC.ai[0] = 52;
            NPC.ai[1] = 0;
            NPC.ai[2] = buffer;
            NPC.ai[3] = 0;
            NPC.localAI[0] = 0;
            NPC.localAI[1] = 0;
            NPC.localAI[2] = 0;
            //NPC.TargetClosest();
            NPC.netUpdate = true;

            EdgyBossText(NPC, RandomObnoxiousQuote());

            if (WorldSavingSystem.EternityMode)
            {
                //become more likely to use randoms as life decreases
                bool useRandomizer = NPC.localAI[3] >= 3 && (WorldSavingSystem.MasochistModeReal || Main.rand.NextFloat(0.8f) + 0.2f > (float)Math.Pow((float)NPC.life / NPC.lifeMax, 2));

                if (FargoSoulsUtil.HostCheck)
                {
                    Queue<float> recentAttacks = new(attackHistory); //copy of attack history that i can remove elements from freely

                    //if randomizer, start with a random attack, else use the previous state + 1 as starting attempt BUT DO SOMETHING ELSE IF IT'S ALREADY USED
                    if (useRandomizer)
                        NPC.ai[2] = Main.rand.Next(args);

                    //Main.NewText(useRandomizer ? "(Starting with random)" : "(Starting with regular next attack)");

                    while (recentAttacks.Count > 0)
                    {
                        bool foundAttackToUse = false;

                        for (int i = 0; i < 5; i++) //try to get next attack that isnt in this queue
                        {
                            if (!recentAttacks.Contains(NPC.ai[2]))
                            {
                                foundAttackToUse = true;
                                break;
                            }
                            NPC.ai[2] = Main.rand.Next(args);
                        }

                        if (foundAttackToUse)
                            break;

                        //couldn't find an attack to use after those attempts, forget 1 attack and repeat
                        recentAttacks.Dequeue();

                        //Main.NewText("REDUCE");
                    }

                    /*text = "";
                    foreach (float f in recentAttacks)
                        text += f.ToString() + " ";
                    Main.NewText($"recent: {text}");*/
                }
            }

            if (FargoSoulsUtil.HostCheck)
            {
                int maxMemory = WorldSavingSystem.MasochistModeReal ? 12 : 18;

                if (attackCount++ > maxMemory * 1.25) //after doing this many attacks, shorten queue so i can be more random again
                {
                    attackCount = 0;
                    maxMemory /= 4;
                }

                attackHistory.Enqueue(NPC.ai[2]);
                while (attackHistory.Count > maxMemory)
                    attackHistory.Dequeue();
            }

            endTimeVariance = WorldSavingSystem.MasochistModeReal ? Main.rand.NextFloat(-0.5f, 1f) : 0;

            /*text = "";
            foreach (float f in attackHistory)
                text += f.ToString() + " ";
            Main.NewText($"after: {text}");*/
        }//选择攻击

        private void P1NextAttackOrMasoOptions(NPC NPC, float sourceAI)
        {
            if (WorldSavingSystem.MasochistModeReal && Main.rand.NextBool(3))
            {
                int[] options = [0, 1, 2, 4, 7, 9, 9];
                NPC.ai[0] = Main.rand.Next(options);
                if (NPC.ai[0] == sourceAI) //dont repeat attacks consecutively
                    NPC.ai[0] = sourceAI == 9 ? 0 : 9;

                bool badCombo = false;
                //dont go into boundary/sword from spheres, true eye dive, void rays
                if (NPC.ai[0] == 9 && (sourceAI == 1 || sourceAI == 2 || sourceAI == 7))
                    badCombo = true;
                //dont go into destroyer-toss or void rays from true eye dive
                if ((NPC.ai[0] == 0 || NPC.ai[0] == 7) && sourceAI == 2)
                    badCombo = true;

                if (badCombo)
                    NPC.ai[0] = 4; //default to dashes
                else if (NPC.ai[0] == 9 && Main.rand.NextBool())
                    NPC.localAI[2] = 1f; //force sword attack instead of boundary
                else
                    NPC.localAI[2] = 0f;
            }
            else
            {
                if (NPC.ai[0] == 9 && NPC.localAI[2] == 0)
                {
                    NPC.localAI[2] = 1;
                }
                else
                {
                    NPC.ai[0]++;
                    NPC.localAI[2] = 0f;
                }
            }

            if (NPC.ai[0] >= 10) //dont accidentally go into p2
                NPC.ai[0] = 0;

            EdgyBossText(NPC, RandomObnoxiousQuote());

            NPC.ai[1] = 0;
            NPC.ai[2] = 0;
            NPC.ai[3] = 0;
            NPC.localAI[0] = 0;
            NPC.localAI[1] = 0;
            //NPC.localAI[2] = 0; //excluded because boundary-sword logic
            NPC.netUpdate = true;
        }

        private void SpawnSphereRing(NPC NPC, int max, float speed, int damage, float rotationModifier, float offset = 0)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float rotation = 2f * (float)Math.PI / max;
            int type = ModContent.ProjectileType<MutantSphereRing>();
            for (int i = 0; i < max; i++)
            {
                Vector2 vel = speed * Vector2.UnitY.RotatedBy(rotation * i + offset);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, type, damage, 0f, Main.myPlayer, rotationModifier * NPC.spriteDirection, speed);
            }
            SoundEngine.PlaySound(SoundID.Item84, NPC.Center);
        }
        private void SpawnPHSphereRing(NPC NPC, int max, float speed, int damage, float velmodifier, float offset = 0)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float rotation = 2f * (float)Math.PI / max;
            int type = ModContent.ProjectileType<PHMutantSphereRing>();
            for (int i = 0; i < max; i++)
            {
                Vector2 vel = speed * Vector2.UnitY.RotatedBy(rotation * i + offset);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, type, damage, 0f, Main.myPlayer, velmodifier * NPC.spriteDirection, speed);
            }
            SoundEngine.PlaySound(SoundID.Item84, NPC.Center);
        }

        private bool Phase2Check(NPC NPC)
        {
            if (Main.expertMode && NPC.life < NPC.lifeMax * (2f / 3))
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    NPC.ai[0] = 10;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    NPC.netUpdate = true;
                    FargoSoulsUtil.ClearHostileProjectiles(1, NPC.whoAmI);
                    EdgyBossText(NPC, GFBQuote(3));
                }
                return true;
            }
            return false;
        }

        private void Movement(NPC NPC, Vector2 target, float speed, bool fastX = true, bool obeySpeedCap = true)
        {
            float turnaroundModifier = 1f;
            float maxSpeed = 24;

            if (WorldSavingSystem.MasochistModeReal)
            {
                speed *= 2;
                turnaroundModifier *= 2f;
                maxSpeed *= 1.5f;
            }

            if (Math.Abs(NPC.Center.X - target.X) > 10)
            {
                if (NPC.Center.X < target.X)
                {
                    NPC.velocity.X += speed;
                    if (NPC.velocity.X < 0)
                        NPC.velocity.X += speed * (fastX ? 2 : 1) * turnaroundModifier;
                }
                else
                {
                    NPC.velocity.X -= speed;
                    if (NPC.velocity.X > 0)
                        NPC.velocity.X -= speed * (fastX ? 2 : 1) * turnaroundModifier;
                }
            }
            if (NPC.Center.Y < target.Y)
            {
                NPC.velocity.Y += speed;
                if (NPC.velocity.Y < 0)
                    NPC.velocity.Y += speed * 2 * turnaroundModifier;
            }
            else
            {
                NPC.velocity.Y -= speed;
                if (NPC.velocity.Y > 0)
                    NPC.velocity.Y -= speed * 2 * turnaroundModifier;
            }

            if (obeySpeedCap)
            {
                if (Math.Abs(NPC.velocity.X) > maxSpeed)
                    NPC.velocity.X = maxSpeed * Math.Sign(NPC.velocity.X);
                if (Math.Abs(NPC.velocity.Y) > maxSpeed)
                    NPC.velocity.Y = maxSpeed * Math.Sign(NPC.velocity.Y);
            }
        }

        private void DramaticTransition(NPC NPC, bool fightIsOver, bool normalAnimation = true)
        {
            NPC.velocity = Vector2.Zero;

            if (fightIsOver)
            {
                Main.player[NPC.target].ClearBuff(ModContent.BuffType<MutantFangBuff>());
                Main.player[NPC.target].ClearBuff(ModContent.BuffType<AbomRebirthBuff>());
            }

            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 1.5f }, NPC.Center);

            if (normalAnimation)
            {
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MutantBomb>(), 0, 0f, Main.myPlayer);
            }

            const int max = 40;
            float totalAmountToHeal = fightIsOver
                ? Main.player[NPC.target].statLifeMax2 / 4f
                : NPC.lifeMax - NPC.life + NPC.lifeMax * 0.1f;
            for (int i = 0; i < max; i++)
            {
                int heal = (int)(Main.rand.NextFloat(0.9f, 1.1f) * totalAmountToHeal / max);
                Vector2 vel = normalAnimation
                    ? Main.rand.NextFloat(2f, 18f) * -Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) //looks messier normally
                    : 0.1f * -Vector2.UnitY.RotatedBy(MathHelper.TwoPi / max * i); //looks controlled during mutant p1 skip
                float ai0 = fightIsOver ? -Main.player[NPC.target].whoAmI - 1 : NPC.whoAmI; //player -1 necessary for edge case of player 0
                float ai1 = vel.Length() / Main.rand.Next(fightIsOver ? 90 : 150, 180); //window in which they begin homing in
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<MutantHeal>(), heal, 0f, Main.myPlayer, ai0, ai1);
            }
        }

        private void EModeSpecialEffects(NPC npc)
        {
            if (WorldSavingSystem.EternityMode)
            {
                //because this breaks the background???
                if (Main.GameModeInfo.IsJourneyMode && CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().Enabled)
                    CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().SetPowerInfo(false);
                //if (!SkyManager.Instance["FargosPhantasmMode:MutantSpecialSky"].IsActive())
                //SkyManager.Instance.Activate("FargosPhantasmMode:MutantSpecialSky");
                if (!SkyManager.Instance["FargosPhantasmMode:MutantSky3"].IsActive())
                    SkyManager.Instance.Activate("FargosPhantasmMode:MutantSky3");

                if (npc.ModNPC is MutantBoss mutant)
                {
                    //mutant.Music = MusicLoader.GetMusicSlot("FargosPhantasmMode/Assets/Music/ChamberofShackles");
                    
                    if (ModLoader.TryGetMod("FargowiltasMusic", out Mod musicMod))
                    {
                        if (WorldSavingSystem.MasochistModeReal && musicMod.Version >= Version.Parse("0.1.1"))
                            mutant.Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Storia");
                        else
                            mutant.Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/rePrologue");
                    }
                    
                }

            }
        }

        private void TryMasoP3Theme(NPC npc)
        {
            if (npc.ModNPC is MutantBoss mutant)
            {
                
                if (WorldSavingSystem.MasochistModeReal && ModLoader.TryGetMod("FargowiltasMusic", out Mod musicMod) && musicMod.Version >= Version.Parse("0.1.1.3"))
                {
                    mutant.Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/StoriaShort");
                }
                
                //mutant.Music = MusicLoader.GetMusicSlot("FargosPhantasmMode/Assets/Music/PianoHistoryShort");
            }
        }

        private void FancyFireballs(NPC NPC, int repeats)
        {
            float modifier = 0;
            for (int i = 0; i < repeats; i++)
                modifier = MathHelper.Lerp(modifier, 1f, 0.08f);

            float distance = 1600 * (1f - modifier);
            float rotation = MathHelper.TwoPi * modifier;
            const int max = 6;
            for (int i = 0; i < max; i++)
            {
                int d = Dust.NewDust(NPC.Center + distance * Vector2.UnitX.RotatedBy(rotation + MathHelper.TwoPi / max * i), 0, 0, FargoSoulsUtil.AprilFools ? DustID.SolarFlare : DustID.Vortex, NPC.velocity.X * 0.3f, NPC.velocity.Y * 0.3f, newColor: Color.White);
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 6f - 4f * modifier;
            }
        }

        private void EdgyBossText(NPC NPC, string text)
        {
            if (Main.zenithWorld) //edgy boss text
            {
                Color color = Color.Cyan;
                FargoSoulsUtil.PrintText(text, color);
                CombatText.NewText(NPC.Hitbox, color, text, true);
            }
        }

        private const int ObnoxiousQuoteCount = 71;
        private const string GFBLocPath = $"Mods.FargowiltasSouls.NPCs.MutantBoss.GFBText.";

        private string RandomObnoxiousQuote() => Language.GetTextValue($"{GFBLocPath}Random{Main.rand.Next(ObnoxiousQuoteCount)}");

        private string GFBQuote(int num) => Language.GetTextValue($"{GFBLocPath}Quote{num}");
        private void SpawnWillJavelin(NPC npc, Vector2 spawnPos, int max, float offset, float omiga = MathF.Tau / 280, int delay = 0)
        {
            SoundEngine.PlaySound(SoundID.Item92, npc.Center);
            int type = ModContent.ProjectileType<WillJavelin3>();
            for (int i = 0; i < max; i++)
            {
                float angle = offset + (float)Math.PI * 2 / max * i;
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + 450 * Vector2.UnitX.RotatedBy(angle), Vector2.Zero,
                    type, npc.defDamage / 4, 0f, Main.myPlayer, omiga, angle + (float)Math.PI, ai2: -delay);
            }
        }
        private void SpawnCursedFlamesWall(NPC npc, Vector2 spawnCenter, float Angle = MathHelper.PiOver2)
        {
            SoundEngine.PlaySound(SoundID.Item34, spawnCenter);

            const int offset = 800;
            const int speed = 14;
            Vector2 unit = Vector2.UnitX.RotatedBy(Angle);
            if (FargoSoulsUtil.HostCheck)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnCenter + unit * offset, unit * -speed, ModContent.ProjectileType<MutantCursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnCenter + unit * offset / 2, unit * speed, ModContent.ProjectileType<MutantCursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnCenter + unit * -offset / 2, unit * -speed, ModContent.ProjectileType<MutantCursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnCenter + unit * -offset, unit * speed, ModContent.ProjectileType<MutantCursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
            }
        }
        #endregion 方法移植
        /*
        public override bool CheckDead(NPC NPC)
        {
            if (NPC.ai[0] == -7)
            {
                return true;
            }

            NPC.life = 1;
            NPC.active = true;
            if (FargoSoulsUtil.HostCheck && NPC.ai[0] > -1)
            {
                NPC.ai[0] = WorldSavingSystem.EternityMode ? NPC.ai[0] >= 10 ? -1 : 10 : -6;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                NPC.localAI[0] = 0;
                NPC.localAI[1] = 0;
                NPC.localAI[2] = 0;
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;
                FargoSoulsUtil.ClearAllProjectiles(2, NPC.whoAmI, NPC.ai[0] < 0);
                EdgyBossText(NPC, GFBQuote(34));
            }
            return false;
        }

        public override void OnKill(NPC NPC)
        {
            base.OnKill(NPC);

            if (WorldSavingSystem.MasochistModeReal || (!playerInvulTriggered && WorldSavingSystem.EternityMode))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.Hitbox, ModContent.ItemType<BrokenSpearhead>());
            }

            if (WorldSavingSystem.EternityMode)
            {
                if (Main.LocalPlayer.active)
                {
                    if (!Main.LocalPlayer.FargoSouls().Toggler.CanPlayMaso && Main.netMode != NetmodeID.Server)
                        Main.NewText(Language.GetTextValue($"Mods.{Mod.Name}.Message.MasochistModeUnlocked"), new Color(51, 255, 191, 0));
                    Main.LocalPlayer.FargoSouls().Toggler.CanPlayMaso = true;
                }
                WorldSavingSystem.CanPlayMaso = true;
            }

            WorldSavingSystem.SkipMutantP1 = 0;

            NPC.SetEventFlagCleared(ref WorldSavingSystem.downedMutant, -1);
        }
        */
        public override bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Vector2 position = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);
            Rectangle rectangle = NPC.frame;
            Vector2 origin2 = rectangle.Size() / 2f;

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.EntitySpriteDraw(texture2D13, position, new Rectangle?(rectangle), NPC.GetAlpha(drawColor), NPC.rotation, origin2, NPC.scale, effects, 0);

            Vector2 auraPosition = AuraCenter;
            if (ShouldDrawAura)
                DrawAura(NPC, spriteBatch, auraPosition, AuraScale);

            return false;
        }

        public void DrawAura(NPC npc, SpriteBatch spriteBatch, Vector2 position, float auraScale)
        {
            Color outerColor = FargoSoulsUtil.AprilFools ? Color.Red : Color.CadetBlue;
            outerColor.A = 0;

            Color darkColor = outerColor;
            Color mediumColor = Color.Lerp(outerColor, Color.White, 0.75f);
            Color lightColor2 = Color.Lerp(outerColor, Color.White, 0.5f);

            Vector2 auraPos = position;
            float radius = 2000f * auraScale;
            var target = Main.LocalPlayer;
            var blackTile = TextureAssets.MagicPixel;
            var diagonalNoise = FargosTextureRegistry.WavyNoise;
            if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                return;
            var maxOpacity = npc.Opacity;

            ManagedShader borderShader = ShaderManager.GetShader("FargowiltasSouls.MutantP1Aura");
            borderShader.TrySetParameter("colorMult", 7.35f);
            borderShader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
            borderShader.TrySetParameter("radius", radius);
            borderShader.TrySetParameter("anchorPoint", auraPos);
            borderShader.TrySetParameter("screenPosition", Main.screenPosition);
            borderShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            borderShader.TrySetParameter("playerPosition", target.Center);
            borderShader.TrySetParameter("maxOpacity", maxOpacity);
            borderShader.TrySetParameter("darkColor", darkColor.ToVector4());
            borderShader.TrySetParameter("midColor", mediumColor.ToVector4());
            borderShader.TrySetParameter("lightColor", lightColor2.ToVector4());

            spriteBatch.GraphicsDevice.Textures[1] = diagonalNoise.Value;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, borderShader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
            Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
            spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            //spriteBatch.Draw(FargosTextureRegistry.SoftEdgeRing.Value, position, null, outerColor * 0.7f, 0f, FargosTextureRegistry.SoftEdgeRing.Value.Size() * 0.5f, 9.2f * auraScale, SpriteEffects.None, 0f);
        }

        public static void ArenaAura(Vector2 center, float distance, bool reverse = false, int dustid = -1, Color color = default, params int[] buffs)
        {
            Player p = Main.LocalPlayer;

            if (buffs.Length == 0 || buffs[0] < 0)
                return;

            //works because buffs are client side anyway :ech:
            float range = center.Distance(p.Center);
            if (p.active && !p.dead && !p.ghost && (reverse ? range > distance && range < Math.Max(3000f, distance * 2) : range < distance))
            {
                foreach (int buff in buffs)
                {
                    FargoSoulsUtil.AddDebuffFixedDuration(p, buff, 2);
                }
            }
        }

    }
}