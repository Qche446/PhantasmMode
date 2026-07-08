using FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs.VanillaEnemies.FrostMoon;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using JetBrains.Annotations;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default.Patreon;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;
using static FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins.P_Retinazer;
using static FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins.P_Spazmatism;
using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// 激光眼
    /// </summary>
    public class P_Retinazer : PModeNPCBehaviour, IPTwins
    {
        #region 不常修改
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.Retinazer);
        public bool DroppedSummon;
        public float AuraRadius = 1;
        public float AuraOpacity = 0;
        public TwinsAtt AIState { get; set; }
        public int Phase { get; set; } = 1;
        public int Phaseinit { get; set; } = 1;
        public bool Ignite { get; set; } = false;
        public bool isDeathray { get; set; } = false;
        public int OrbColor => MechElectricOrb.Yellow;
        public static readonly SoundStyle LensEject = new SoundStyle("FargosPhantasmMode/Assets/Sounds/LensEject");
        public static readonly SoundStyle DeathrayFire = new SoundStyle("FargosPhantasmMode/Assets/Sounds/DeathrayFire")
        {
            Volume = 2f,          // 音量 (0.0f 到 1.0f)
            PitchVariance = 0.3f,   // 音高随机变化范围，增加声音自然度
            MaxInstances = 1,       // 最多同时存在的实例数，防止声音叠加
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
        };
        public override void StopEmodeAI(NPC npc)
        {
            npc.GetGlobalNPC<Retinazer>().RunEmodeAI = false;
        }
        public override bool SafePreAI(NPC npc)
        {
            EModeGlobalNPC.retiBoss = npc.whoAmI;
            NPC bro = FargoSoulsUtil.NPCExists(EModeGlobalNPC.spazBoss, NPCID.Spazmatism);
            if (bro == null)
                return false;
            P_Spazmatism Spaz = bro.GetGlobalNPC<P_Spazmatism>();
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();
            Player player = Main.player[npc.target];
            //ShootPos = npc.Center + (npc.width - 24) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
            Spaz.Phase = Phase;
            if (bro == null && Phase == 3)
                CheckDead(npc);

            if (!AliveCheck(npc, player))
                return false;
            PhaseCheck(npc, bro);
            if (Phase >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Ichor, 0f, 0f, 0, default, 1.8f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 5f;
                }
            }//粒子效果
            PHTwinsAI(npc, player, bro);
            ManangeAura(npc);
            ManageAuraRadius();
            EModeUtils.DropSummon(npc, "MechEye", NPC.downedMechBoss2, ref DroppedSummon, Main.hardMode);
            return false;
        }
        #endregion
        public List<TwinsAtt> Phase1 => [

            ];
        public List<TwinsAtt> Phase2 => [

            ];
        public List<TwinsAtt> Phase3 => [

            ];
        public static void PHTwinsAI(NPC npc, Player player, NPC bro)
        {
            IPTwins self = GetIPTwins(npc);
            switch (self.AIState)
            {
                case TwinsAtt.PhaseChange1st: PhaseChange1st(npc); break;


                default: break;
            }
        }
        #region 新ai方法
        public static void NormalShoot(NPC npc, Player player, NPC bro)
        {
            if (npc.localAI[1] >= (npc.ai[1] == 0 ? 170 : 50)) //hijacking vanilla laser code
            {
                Vector2 vel = npc.SafeDirectionTo(player.Center);
                Vector2 shotVel = vel * 20;
                int type = ModContent.ProjectileType<MechElectricOrb>();
                float spreadAngle = 0.4f;
                int spread = 0; // 1;
                if (npc.ai[1] == 0 && Main.getGoodWorld)
                {
                    spread = 1;
                }
                for (int i = -spread; i <= spread; i++)
                {
                    if (i == 0 && spread != 0)
                        continue;
                    Vector2 shotVel2 = shotVel.RotatedBy(MathHelper.PiOver2 * spreadAngle * i);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + (npc.width - 24) * vel, shotVel2, type, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                    for (int j = -3; j <= 3; j++)
                    {
                        Vector2 particleVel = (shotVel2 * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j).RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                        Particle p = new ElectricSpark(npc.Center + (npc.width - 24) * vel - shotVel2, particleVel, Color.Yellow, Main.rand.NextFloat(0.7f, 1f), 20);
                        p.Spawn();
                    }
                }
                if (npc.ai[1] == 0)
                    shotVel *= 2;
                npc.velocity -= shotVel / 7f;
            }
        }
        #endregion
        #region 废弃AI方法
        public static void PhaseChange1st(NPC npc)
        { 
            npc.TryGetGlobalNPC<P_Retinazer>(out P_Retinazer reti);
            npc.velocity *= 0.98f;
            npc.dontTakeDamage = true;
            if (npc.velocity.Length() < 0.1f)
                npc.velocity = Vector2.Zero;
            if (npc.ai[1] < 90)
            {
                npc.ai[2] += 0.012f;
                if (npc.ai[2] > 1.08f)
                    npc.ai[2] = 1.08f;
                
            }
            else if (npc.ai[1] < 180)
            {
                npc.ai[0] = 2f;
                npc.ai[2] -= 0.012f;
                if (npc.ai[2] < 0f)
                    npc.ai[2] = 0f;
                if (npc.ai[1] == 90)
                {
                    SoundEngine.PlaySound(3, (int)npc.position.X, (int)npc.position.Y);
                    for (int i = 0; i < 2; i++)
                    {
                        Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.5f, Main.rand.Next(-30, 31) * 0.5f), npc.type == NPCID.Retinazer ? 143 : 144);
                        Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.5f, Main.rand.Next(-30, 31) * 0.5f), 7);
                        Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.5f, Main.rand.Next(-30, 31) * 0.5f), 6);
                    }
                    for (int i = 0; i < 20; i++)
                        Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                }
                if (npc.ai[1] > 90 && reti != null)
                {
                    float progress = (npc.ai[1] - 90f) / 90f;
                    //reti.AuraRadius = MathHelper.SmoothStep(1, 1500, progress);
                    reti.AuraRadius = 1500 * FargoSoulsUtil.SineInOut(progress);
                }
            }
            npc.rotation += npc.ai[2];
            if (++npc.ai[1] == 180)
            {
                npc.dontTakeDamage = false;
                ChooseAttack(npc);
                npc.netUpdate = true;
            }
        }
        public static void PhaseChange2nd(NPC npc, NPC bro)
        {
            if (npc.ai[1] <= 30)
            {
                npc.dontTakeDamage = true;
                npc.velocity *= MathHelper.Lerp(1, 0, npc.ai[1] / 30f);
                RotateTowards(npc, bro.Center, 0.12f);
            }
            else if (npc.ai[1] <= 80)
            {
                int heal = (int)(npc.lifeMax / 90 * Main.rand.NextFloat(1f, 1.5f));
                if (npc.life > 0.40f * npc.lifeMax)
                    npc.life = (int)(0.40f * npc.lifeMax);
                else
                    npc.life += heal;
                CombatText.NewText(npc.Hitbox, CombatText.HealLife, heal);
            }
            if (npc.ai[1] == 60)
            {
                Vector2 dir = bro.Center - npc.Center;
                npc.velocity = 1.5f * dir / 10f;
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
            }
            if (npc.ai[1] > 60)
            {
                npc.velocity *= 0.92f;
                if (CollisionDetector.Intersects(npc.Hitbox, bro.Hitbox))
                {
                    npc.ai[2] = 1;
                    bro.ai[2] = 1;
                    npc.velocity *= 0;
                    bro.velocity *= 0;
                    if (npc.type == NPCID.Retinazer)
                    {
                        Main.NewText(npc.ai[2] + bro.ai[2]);
                        //Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsFireBackground>(), 0, 0, Main.myPlayer, ai0: npc.whoAmI, 0);
                        //SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                        //ScreenShakeSystem.StartShake(20f);
                        //Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsWave>(), 0, 0, Main.myPlayer, ai1: 0, ai2: 20);
                    }
                    else
                    {
                        //Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsWave>(), 0, 0, Main.myPlayer, ai1: 0, ai2: 16);
                    }

                }
            }
            if (++npc.ai[1] > 60)
            {
                if (npc.ai[2] == 1 && bro.ai[2] == 1)
                {
                    npc.dontTakeDamage = false;
                    ChooseAttack(npc);
                    npc.netUpdate = true;
                }
            }
        }
        public static void ChangeDash(NPC npc, NPC bro)
        {
            float WaitTime = 50;
            float turnSpeed = 0.12f;
            float SlowTime = 30;
            IPTwins pT = GetIPTwins(npc);
            if (npc.ai[1] == 0)
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + TwinsEyeFlash.Offset(npc), Vector2.Zero, ModContent.ProjectileType<TwinsEyeFlash>(), 0, 0, Main.myPlayer, npc.whoAmI);
            if (pT.Phase == 2)
            {
                WaitTime = 30;
                turnSpeed = 0.15f;
                SlowTime = 30;
            }
            if (pT.Phase == 3)
            {
                WaitTime = 20;
                turnSpeed = 0.18f;
                SlowTime = 20;
            }
            if (npc.ai[1] < WaitTime)
            {
                RotateTowards(npc, bro.Center, turnSpeed);
                npc.velocity *= 0.96f;
            }
            if (npc.ai[1] == WaitTime)
            {
                Vector2 dir = bro.Center - npc.Center;
                if (dir.Length() < 600)
                    npc.velocity = 1.2f * 500 * dir.SafeNormalize(Vector2.Zero) / 10f;
                else
                    npc.velocity = 1.2f * dir / 10f;
            }
            if (npc.ai[1] > WaitTime)
            {
                npc.velocity *= 0.9f;
            }
            if (++npc.ai[1] > WaitTime + SlowTime)
            {
                ChooseAttack(npc);
                npc.netUpdate = true;
            }
        }
        public static void TurnAndWait(NPC npc, Player player, int WaitTime)
        {
            npc.velocity *= 0.95f;
            float speed = WaitTime < 30 ? 0.22f : 0.15f;
            RotateTowards(npc, player.Center, speed);
            if (++npc.ai[1] > WaitTime)
                ChooseAttack(npc);
        }
        public static void DistanceShoot(NPC npc, Player player, NPC bro)
        {
            RotateTowards(npc, player.Center);
            Vector2 targetPos = player.Center - npc.SafeDirectionTo(player.Center) * 450;
            float dis = npc.Distance(targetPos);
            if (dis > 800)
                TwinMove(npc, targetPos, 0.9f, 4);
            else if (dis > 400)
                TwinMove(npc, targetPos, 0.6f);
            else if (dis < 100)
                TwinMove(npc, targetPos, 0.4f);

            int intervel = (npc.GetLifePercent() < 0.7f || bro.GetLifePercent() < 0.7f) ? 15 : 25;
            if (npc.ai[1] % intervel == 0 && npc.ai[1] > 30)
            {
                Vector2 vel = npc.SafeDirectionTo(player.Center) * 15f;
                if (npc.type == NPCID.Retinazer)
                {
                    if (npc.ai[1] % (2 * intervel) == 0)
                    {
                        for (float i = -1; i <= 1f; i += 1f)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel.RotatedBy(0.5f * i * MathHelper.PiOver2), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer,
                                npc.target, ai2: MechElectricOrb.Yellow);
                        }
                    }
                    else
                    {
                        for (float i = -1.5f; i <= 1.5f; i += 1f)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel.RotatedBy(0.8f * i * MathHelper.PiOver2), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer,
                                npc.target, ai2: MechElectricOrb.Yellow);
                        }
                    }
                }
                else
                {
                    for (float i = 1; i <= 1.3f; i += 0.2f)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), i * vel / 15f, ModContent.ProjectileType<MechElectricOrbSpaz>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer,
                            npc.target, ai2: MechElectricOrb.Green);
                    }
                }
                SpawnElectricSpark(npc, vel);
                npc.velocity -= Main.rand.NextFloat(0.3f, 0.6f) * vel;
                npc.netUpdate = true;
            }
            if (++npc.ai[1] > 300)
            {
                ChooseAttack(npc);
            }
        }
        public static void LocateAndWaitDash(NPC npc, Player player, NPC bro)
        {
            IPTwins npcpT = GetIPTwins(npc);
            IPTwins bropT = GetIPTwins(bro);
            bool inState = (npcpT.AIState == TwinsAtt.LocateAndWaitDash || npcpT.AIState == TwinsAtt.RotatedAndWaitDash) || (bropT.AIState == TwinsAtt.LocateAndWaitDash || bropT.AIState == TwinsAtt.RotatedAndWaitDash);
            float prepTime = 70;
            if (npc.ai[1] == 0)
            {
                npc.localAI[0] = player.DirectionTo(npc.Center).ToRotation();
                int dir = Main.rand.NextBool() ? 1 : -1;
                float bro_Angle = player.DirectionTo(bro.Center).ToRotation();
                dir *= MathF.Sign(npc.localAI[0] - bro_Angle);
                npc.localAI[0] += dir * MathHelper.PiOver2 * (0.25f + Main.rand.NextFloat(0.2f));
                npc.netUpdate = true;
            }

            // lock on to spot next to player
            npc.localAI[1] = 450f;
            Vector2 desiredPos = player.Center + npc.localAI[0].ToRotationVector2() * npc.localAI[1];

            if (npc.ai[1] < prepTime)
            {
                TwinMove(npc, desiredPos, 0.6f, 4f);
                RotateTowards(npc, player.Center, 0.11f);
            }

            bool waitForbro = bro.ai[1] == npc.ai[1];
            Vector2 broDesiredPos = player.Center + bro.localAI[0].ToRotationVector2() * npc.localAI[1];
            if (bro.Distance(broDesiredPos) <= 300)
                waitForbro = false;

            if (npc.ai[1] < prepTime - 30 && (npc.Distance(desiredPos) > 500 || waitForbro))
                npc.ai[1]--;
            int flashDelay = 25;
            if (npc.ai[1] < prepTime - flashDelay && bro.ai[1] < npc.ai[1] && inState)
                npc.ai[1] = bro.ai[1];
            if (npc.ai[1] == prepTime - flashDelay)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + TwinsEyeFlash.Offset(npc), Vector2.Zero, ModContent.ProjectileType<TwinsEyeFlash>(), 0, 0, Main.myPlayer, npc.whoAmI);
                }
            }
            if (npc.ai[1] == prepTime)
                npc.velocity = npc.DirectionTo(player.Center) * 40;
            if (npc.ai[1] >= prepTime && npc.ai[1] % 2 == 0)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                    3f * Vector2.Normalize(npc.velocity), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                    Main.myPlayer, npc.target, ai2: npc.type == NPCID.Retinazer ? MechElectricOrb.Yellow : MechElectricOrb.Green);
            }
            if (npc.ai[1] >= prepTime + 20)
            {
                npc.velocity *= 0.95f;
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            }
            if (++npc.ai[1] >= prepTime + 30)
            {
                ChooseAttack(npc);
            }
        }
        public static void RotatedAndWaitDash(NPC npc, Player player)
        {
            IPTwins pT = GetIPTwins(npc);
            float prepTime = 70;
            if (npc.ai[1] == 0)
            {
                npc.localAI[0] = player.DirectionTo(npc.Center).ToRotation();
                npc.localAI[1] = (npc.Center - player.Center).Length();
                npc.localAI[1] = Math.Clamp(npc.localAI[1], 350, 600);
                npc.localAI[2] = Main.rand.NextBool() ? 1 : -1;
                npc.netUpdate = true;
            }
            
            // lock on to spot next to player
            Vector2 desiredPos = player.Center + npc.localAI[0].ToRotationVector2() * npc.localAI[1];
            npc.localAI[0] += npc.localAI[2] * MathHelper.TwoPi / 90;
            if (npc.ai[1] < prepTime + 30)
            {
                npc.velocity = (desiredPos - npc.Center) * (npc.ai[1] / (prepTime + 30f));
                npc.rotation = npc.SafeDirectionTo(player.Center).ToRotation() + npc.localAI[2] * MathHelper.PiOver2 + MathHelper.PiOver2;
            }
            if (npc.velocity.Length() > 28)
            {
                npc.velocity = Vector2.Normalize(npc.velocity) * 28;
            }
            if (npc.ai[1] % 4 == 0 && npc.ai[1] <= prepTime + 30)
            {
                Vector2 vel = 2 * Vector2.Normalize(npc.velocity);
                Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                    vel, ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                    Main.myPlayer, npc.target, ai2: pT.OrbColor);
            }
            if (npc.ai[1] >= prepTime + 20)
            {
                npc.velocity *= 0.95f;
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            }
            if (++npc.ai[1] >= prepTime + 30)
            {
                ChooseAttack(npc);
            }
        }
        //
        public static void P2DistanceShoot(NPC npc, Player player, bool isLocked)
        {
            if (isLocked)
            {
                RotateTowards(npc, player.Center);
                Vector2 desired = player.Center + 500 * Vector2.UnitY.RotatedBy(npc.ai[2] * MathHelper.Pi / 6) * (npc.type == NPCID.Retinazer ? -1 : 1);
                TwinMove(npc, desired, 3f, 4);
            }
            else
            {
                RotateTowards(npc, player.Center);
                Vector2 targetPos = player.Center - npc.SafeDirectionTo(player.Center) * 450;
                float dis = npc.Distance(targetPos);
                if (dis > 800)
                    TwinMove(npc, targetPos, 0.9f, 4);
                else if (dis > 400)
                    TwinMove(npc, targetPos, 0.6f);
                else if (dis < 100)
                    TwinMove(npc, targetPos, 0.4f);
            }
            int inter = 25;
            if (npc.ai[1] % inter == 0 && npc.ai[1] > 30)
            {
                Vector2 vel = npc.SafeDirectionTo(player.Center);
                if (npc.type == NPCID.Retinazer)
                {
                    for (int i = -2; i <= 2; i++)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), 25 * vel.RotatedBy(i * MathHelper.Pi / 6), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f,
                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), 35 * vel.RotatedBy(i * MathHelper.Pi / 6), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f,
                            Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                    }
                }
                else
                {
                    for (float i = 0.7f; i <= 1.3f; i += 0.2f)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), i * vel.RotatedBy(j * MathHelper.Pi / 3), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                        }
                    }
                }
                SpawnElectricSpark(npc, vel);
                float num = isLocked ? 15: Main.rand.NextFloat(15f, 21f);
                npc.velocity -= num * vel;
                npc.ai[2]++;
            }
            if (++npc.ai[1] > 300)
            {
                ChooseAttack(npc);
            }
        }
        public static void CurvedLaserLocked(NPC npc, Player player)
        {
            Vector2 dir = npc.SafeDirectionTo(player.Center);
            RotateTowards(npc, player.Center);
            Vector2 targetPos = player.Center - dir * 450;
            float dis = npc.Distance(targetPos);
            if (dis > 800)
                TwinMove(npc, targetPos, 0.9f, 4);
            else if (dis > 400)
                TwinMove(npc, targetPos, 0.6f, 3);
            else if (dis > 50)
                TwinMove(npc, targetPos);
            npc.velocity *= 0;
            if (npc.ai[1] % 20 == 0 && npc.ai[1] >= 30)
            {
                float offsetAngle = 0.4f * MathF.PI;
                for (int i = -1; i <= 1; i += 2)
                {
                    for (float j = 1; j <= 1.1f; j += 0.2f)  
                    {
                        Vector2 vel = dir.RotatedBy(i * offsetAngle);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel, ModContent.ProjectileType<TwinCurvedLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1, Main.myPlayer,
                            npc.whoAmI, 40);
                    }
                }
                npc.velocity -= dir;
                npc.netUpdate = true;
            }
            if (++npc.ai[1] > 360)
                ChooseAttack(npc);
        }
        public static void CursedFireWheel(NPC npc, Player player)
        {
            ref float flagX = ref npc.localAI[0];
            ref float flagY = ref npc.localAI[1];
            if (npc.ai[1] == 0)
            {
                flagX = Math.Sign(npc.Center.X - player.Center.X);
                flagY = Math.Sign(npc.Center.Y - player.Center.Y);
            }
            Vector2 targetPos = player.Center + new Vector2(800 * flagX, 800 * flagY);
            TwinMove(npc, targetPos, 3f, 4);
            RotateTowards(npc, player.Center);
            if (npc.ai[1] % 60 == 0)
            {
                Vector2 vel = 8 * npc.SafeDirectionTo(player.Center).RotatedByRandom(MathHelper.Pi / 12);
                Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel, ModContent.ProjectileType<CursedFireWheel>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1, Main.myPlayer,
                    vel.ToRotation(), 2f, 0);
                if (Main.rand.NextBool())
                    flagX *= -1;
                else
                    flagY *= -1;
                npc.velocity -= 5 * vel;
                SpawnElectricSpark(npc, vel);
                npc.netUpdate = true;
            }
            if (++npc.ai[1] > 360)
                ChooseAttack(npc);
        }
        public static void PredictiveLaser(NPC npc, Player player)
        {
            if (npc.ai[1] == 0)
                npc.ai[2] = npc.SafeDirectionTo(player.Center).ToRotation() + MathF.PI * Main.rand.NextFloat(-0.6f, 0.6f) + MathF.PI;
            Vector2 desired = player.Center + 500 * Vector2.UnitX.RotatedBy(npc.ai[2]);
            TwinMove(npc, desired, 2f, 4);
            Vector2 target = player.Center + 80 * player.velocity;
            if (npc.ai[1] % 30 < 15 || npc.ai[1] <= 40)
            {
                npc.localAI[0] = target.X;
                npc.localAI[1] = target.Y;
            }
            Vector2 Ntarget = new Vector2(npc.localAI[0], npc.localAI[1]);
            RotateTowards(npc, Ntarget, 0.16f);
            int offset = Math.Sign((player.Center - npc.Center).AngleDifference(Ntarget - npc.Center));
            //if (npc.ai[1] % 40 <= 30 || npc.ai[1] <= 40)
                //JunengAnimation(npc);
            if (npc.ai[1] % 30 == 20 && npc.ai[1] > 40)
            {
                Vector2 vel = npc.SafeDirectionTo(Ntarget);
                for (int i = -1; i <= 1; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), 1.5f * vel.RotatedBy(i * 0.05f * MathF.PI), ModContent.ProjectileType<TwinFakeLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1, Main.myPlayer,
                    npc.whoAmI, TwinFakeLaser.Normal, MechElectricOrb.Red);
                }
                npc.velocity -= 10 * vel;
            }
            if (npc.ai[1] % 30 >= 25 && npc.ai[1] % 30 <= 30 && npc.ai[1] > 40)
                npc.velocity *= 0.94f;
            if (++npc.ai[1] > 300)
                ChooseAttack(npc);
        }
        public static void FireRotated(NPC npc, Player player)
        {
            const float prepTime = 300;
            IPTwins pT = GetIPTwins(npc);
            if (npc.ai[1] == 0)
            {
                npc.localAI[0] = player.DirectionTo(npc.Center).ToRotation();
                npc.localAI[1] = (npc.Center - player.Center).Length();
                npc.localAI[1] = Math.Clamp(npc.localAI[1], 350, 600);
                npc.localAI[2] = Main.rand.NextBool() ? 1 : -1;
                npc.netUpdate = true;
            }

            // lock on to spot next to player
            Vector2 desiredPos = player.Center + npc.localAI[0].ToRotationVector2() * npc.localAI[1];
            npc.localAI[0] += npc.localAI[2] * MathHelper.TwoPi / 100;
            if (npc.ai[1] <= prepTime)
            {
                npc.velocity = (desiredPos - npc.Center) * (npc.ai[1] / 150f + 0.5f);
                npc.rotation = npc.SafeDirectionTo(player.Center).ToRotation() + npc.localAI[2] * MathHelper.PiOver2 + MathHelper.PiOver2;
            }
            if (npc.velocity.Length() > 28)
            {
                npc.velocity = Vector2.Normalize(npc.velocity) * 28;
            }
            if (npc.ai[1] % 5 == 0 && npc.ai[1] > 80)
            {
                Vector2 vel = Vector2.Normalize(npc.velocity);
                Projectile p = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), ShootPos(npc), vel, ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                    Main.myPlayer, npc.target, ai2: pT.OrbColor);
                Projectile q = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), ShootPos(npc), vel.RotatedBy(-0.2f * MathF.PI), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                    Main.myPlayer, npc.target, ai2: pT.OrbColor);
                p.timeLeft = q.timeLeft = 180;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 10 * vel.RotatedByRandom(0.05f * MathF.PI), ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1, Main.myPlayer);
                SpawnElectricSpark(npc, vel);
            }
            if (npc.ai[1] >= prepTime)
            {
                npc.velocity *= 0.95f;
                RotateTowards(npc, player.Center);
            }
            if (++npc.ai[1] >= 300)
            {
                ChooseAttack(npc);
            }
        }
        public static void Deathray(NPC npc, Player player)
        {
            #region 主逻辑
            float rotationInterval = 1.05f * 2f * (float)Math.PI * 1.2f / 4f / 60f;
            if (npc.ai[1] == 0)
            {
                npc.rotation = npc.Center.X < Main.player[npc.target].Center.X ? 0 : (float)Math.PI;
                npc.rotation -= (float)Math.PI / 2;

                npc.ai[3] = -npc.rotation;
                if (--npc.ai[2] > 295f)
                    npc.ai[2] = 295f;
                npc.localAI[0] = Main.player[npc.target].Center.X - npc.Center.X < 0 ? 1 : -1;

                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);

                SoundEngine.PlaySound(FargosSoundRegistry.TwinsWarning with { Volume = 4f }, npc.Center);
                npc.netUpdate = true;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                NetSync(npc);
            }
            if (npc.ai[1] == 30f)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowLine>(), 0, 0f, Main.myPlayer, 9f, npc.whoAmI);
            }
            if (npc.ai[1] <= 150f)
            {
                Vector2 pos = player.Center + player.DirectionTo(npc.Center) * 250;
                npc.velocity = FargoSoulsUtil.SmartAccel(npc.Center, pos, npc.velocity, 0.9f, 0.9f);

                npc.velocity *= 1f - npc.ai[1] / 120f;
                npc.localAI[1] = 0f;
                //if (--npc.ai[2] > 295f) npc.ai[2] = 295f;
                npc.ai[3] -= npc.ai[1] / 120f * rotationInterval * npc.localAI[0];
                npc.rotation = -npc.ai[3];
                if (npc.ai[1] == 150f)
                {
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(FargosSoundRegistry.TwinsDeathray with { Volume = 2f }, npc.Center);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 speed = Vector2.UnitX.RotatedBy(npc.rotation);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed, ModContent.ProjectileType<RetinazerDeathray>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 3), 0f, Main.myPlayer, 0f, npc.whoAmI);
                    }
                    npc.netUpdate = true;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                    NetSync(npc);
                }
            }
            else if (npc.ai[1] <= 390f)
            {
                npc.velocity = Vector2.Zero;
                npc.localAI[1] = 0f;
                //if (--npc.ai[2] > 295f) npc.ai[2] = 295f;
                npc.ai[3] -= rotationInterval * npc.localAI[0];
                npc.rotation = -npc.ai[3];

                if (npc.ai[1] == 390)
                {
                    npc.netUpdate = true;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                    NetSync(npc);
                }
            }
            else if (npc.ai[1] < 450f)
            {
                npc.velocity = Vector2.Zero;
                npc.ai[3] -= rotationInterval * npc.localAI[0];
                npc.rotation = -npc.ai[3];
            }
            
            if (++npc.ai[1] > 450)
            {
                ChooseAttack(npc);
                NetSync(npc);
            }
            #endregion
        }
        public static void RollingShoot(NPC npc, NPC bro)
        {
            Vector2 target = bro.Center + bro.SafeDirectionTo(npc.Center) * 100;
            npc.velocity = (target - npc.Center) / 60f;

            int FlameWheelCount = 2; // 基础2个
            
            if (Main.getGoodWorld)
                FlameWheelCount++;
            if (npc.ai[1] > 30 && npc.ai[1] < 420)
            {
                npc.rotation += MathHelper.SmoothStep(0, 0.6f, (npc.ai[1] - 30f) / 30f);
            }
            if (npc.ai[1] < 30)
            {
                npc.rotation = npc.SafeDirectionTo(bro.Center).ToRotation() - MathHelper.PiOver2;
                npc.ai[2] = npc.rotation;
            }
            else if (npc.ai[1] % 15 == 0 && npc.ai[1] < 420 && npc.ai[1] >= 30)
            {
                float speed = 11f * Math.Min((npc.ai[1] - 30) / 120f, 1f);
                int timeLeft = (int)(speed / 12f * 90f); 
                float baseRotation = -bro.localAI[0] * npc.ai[1] / 60f + npc.ai[2];
                npc.ai[2] += 0.15f * MathF.PI;

                if (timeLeft > 5) 
                {
                    IPTwins pTwins= GetIPTwins(npc);
                    for (int i = 0; i < FlameWheelCount; i++)
                    {
                        Projectile p = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center,
                            (baseRotation + MathHelper.TwoPi / FlameWheelCount * i).ToRotationVector2() * speed,
                            ModContent.ProjectileType<MechElectricOrb>(),
                            FargoSoulsUtil.ScaledProjectileDamage(npc.damage),
                            0f, Main.myPlayer, ai2: pTwins.OrbColor);
                        Projectile q = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center,
                            (-baseRotation + MathHelper.TwoPi / FlameWheelCount * i).ToRotationVector2() * speed,
                            ModContent.ProjectileType<MechElectricOrb>(),
                            FargoSoulsUtil.ScaledProjectileDamage(npc.damage),
                            0f, Main.myPlayer, ai2: pTwins.OrbColor);
                        if (p.active && q.active)
                        {
                            p.timeLeft = timeLeft;
                            q.timeLeft = timeLeft;
                        }
                    }
                }
            }
            if (npc.ai[1] >= 420)
            {
                npc.rotation += MathHelper.SmoothStep(0.6f, 0, (npc.ai[1] - 420f) / 30f);
            }
            if (++npc.ai[1] > 450)
                ChooseAttack(npc);
        }
        public static void PolyRing(NPC npc, Player player)
        {
            IPTwins re = GetIPTwins(npc);
            npc.velocity *= 0.80f;
            if (npc.ai[1] % 10 == 0 && npc.ai[1] > 30 && npc.ai[1] < 390)
            {
                int max = 8;
                for (int i = 0; i < max; i++)
                {
                    Vector2 vel = Vector2.UnitX.RotatedBy((i + npc.ai[2] / 20f) * MathHelper.TwoPi / max);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                        10 * vel, ModContent.ProjectileType<MechElectricOrbPolyline>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1f, Main.myPlayer,
                        0, 1, MechElectricOrb.Yellow);
                    if (Main.getGoodWorld && false)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                        10 * vel, ModContent.ProjectileType<MechElectricOrbPolyline>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1f, Main.myPlayer, 
                        0, -1, MechElectricOrb.Yellow);
                    }
                    if (re.Phase >= 3)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                        16 * Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / max + 3 * npc.ai[2]), ModContent.ProjectileType<MechElectricOrb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
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
                npc.ai[2]++;
            }
            if (npc.ai[1] >= 330)
            {
                RotateTowards(npc, player.Center);
                Vector2 targetPos = player.Center - npc.SafeDirectionTo(player.Center) * 450;
                float dis = npc.Distance(targetPos);
                if (dis > 800)
                    TwinMove(npc, targetPos, 0.9f, 4);
                else if (dis > 400)
                    TwinMove(npc, targetPos, 0.6f);
                else if (dis < 100)
                    TwinMove(npc, targetPos, 0.4f);
            }
            if (++npc.ai[1] > 420)
            {
                ChooseAttack(npc);
            }
        }
        public static void LocatedDash(NPC npc, Player player, NPC bro)
        {
            if (npc.ai[1] % 70 == 0)
            {
                float r = Main.rand.Next(600, 751);
                npc.ai[2] = -1;
                Vector2 dir2 = npc.ai[2] * npc.SafeDirectionTo(player.Center);
                float a = dir2.ToRotation() + MathF.PI * Main.rand.NextFloat(-0.2f, 0.2f);
                npc.localAI[0] = r * Vector2.UnitX.RotatedBy(a).X;
                npc.localAI[1] = r * Vector2.UnitX.RotatedBy(a).Y;
                npc.netUpdate = true;
            }
            //Vector2 target = new Vector2(npc.localAI[0], npc.localAI[1]);
            if (npc.ai[1] % 70 < 40)
            {
                JunengAnimation(npc);
                RotateTowards(npc, player.Center);
                Vector2 desired = new Vector2(npc.localAI[0], npc.localAI[1]) + player.Center;
                TwinMove(npc, desired, 0.6f, 4);
                if (npc.velocity.Length() > 24)
                    npc.velocity = 24 * Vector2.Normalize(npc.velocity);
            }
            else if (npc.ai[1] % 70 == 40)
            {
                //Vector2 dir = npc.SafeDirectionTo(player.Center);
                npc.localAI[1] = (player.Center - npc.Center).ToRotation();
                npc.netUpdate = true;
                //npc.velocity = 35 * dir;
            }
            else
            {
                float progress = (npc.ai[1] % 70 - 40) / 30f;
                Vector2 vel = npc.localAI[1].ToRotationVector2();
                npc.velocity = vel * MathHelper.Lerp(2, 80, 1f - Math.Abs(5 * progress / 3f - 1f));
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel.RotatedByRandom(0.04f * MathF.PI) * MathHelper.SmoothStep(2, 20, progress), 
                    ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1, Main.myPlayer);
            }
            if (++npc.ai[1] > 420)
                ChooseAttack(npc);
        }
        public static void Sweeping(NPC npc, Player player, NPC bro)
        {
        }
        public static void SimpleShoot(NPC npc, Player player)
        {
            if (npc.ai[1] == 0)
            {
                npc.localAI[0] = player.SafeDirectionTo(npc.Center).ToRotation();
                npc.netUpdate = true;
            }
            Vector2 desired = player.Center + 550 * (npc.localAI[0].ToRotationVector2());
            TwinMove(npc, desired, 0.3f, 3);
            RotateTowards(npc, player.Center);
            Vector2 vel = npc.SafeDirectionTo(player.Center);
            int intervel = 20;
            if (npc.ai[1] > 80)
                intervel = 10;
            if (npc.ai[1] > 140)
                intervel = 5;
            if (npc.ai[1] % intervel == 0 && npc.ai[1] > 20 && npc.ai[1] <= 300)
            {
                for (int i = -2; i <= 2; i++)
                {
                    if (npc.type == NPCID.Retinazer && i == 0)
                        i++;
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                        3 * vel.RotatedBy(i * MathHelper.Pi / 5), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                        Main.myPlayer, npc.target, ai2: npc.type == NPCID.Retinazer ? MechElectricOrb.Yellow : MechElectricOrb.Green);
                    Main.projectile[p].timeLeft = 150;
                }
                SpawnElectricSpark(npc, vel);
                npc.velocity -= 2f * vel;
            }
            if (++npc.ai[1] > 240)
            {
                ChooseAttack(npc);
            }
        }
        public static void HugeFire(NPC npc, Player player, NPC bro)
        {
            //Spaz删去激光，发射诅咒焰
            if (npc.ai[1] > 150 && npc.ai[1] < 390)
            {
                float progress = (npc.ai[1] - 150) / 30f;
                if (npc.ai[1] > 360)
                    progress = (390 - npc.ai[1]) / 30f;
                float speed = MathHelper.SmoothStep(0, 30, progress);
                for (int i = 0; i < 5; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), Main.rand.NextFloat(0.8f * speed, speed) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + MathHelper.ToRadians(Main.rand.NextFloat(-5, 5))),
                        ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer);
                }
                ScreenShakeSystem.StartShake(3f);
            }
            #region 主逻辑
            float rotationInterval = 1.05f * 2f * (float)Math.PI * 1.2f / 4f / 60f;
            if (npc.ai[1] == 0)
            {
                npc.rotation = npc.Center.X < Main.player[npc.target].Center.X ? 0 : (float)Math.PI;
                npc.rotation -= (float)Math.PI / 2;

                npc.ai[3] = -npc.rotation;
                if (--npc.ai[2] > 295f)
                    npc.ai[2] = 295f;
                npc.localAI[0] = Main.player[npc.target].Center.X - npc.Center.X < 0 ? 1 : -1;

                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(bro.GetSource_FromThis(), bro.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, bro.whoAmI, bro.type);

                SoundEngine.PlaySound(LensEject with { Volume = 4f }, npc.Center);
                npc.netUpdate = true;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                NetSync(npc);
            }
            if (npc.ai[1] == 30f)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<SpazmatismGlowLine>(), 0, 0f, Main.myPlayer, 
                    120f, npc.whoAmI, 0.01f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<SpazmatismGlowLine>(), 0, 0f, Main.myPlayer,
                    120f, npc.whoAmI, -0.01f);
            }
            if (npc.ai[1] <= 150f)
            {
                Vector2 pos = player.Center + player.DirectionTo(npc.Center) * 250;
                npc.velocity = FargoSoulsUtil.SmartAccel(npc.Center, pos, npc.velocity, 0.9f, 0.9f);

                npc.velocity *= 1f - npc.ai[1] / 120f;
                npc.localAI[1] = 0f;
                //if (--npc.ai[2] > 295f) npc.ai[2] = 295f;
                npc.ai[3] -= npc.ai[1] / 120f * rotationInterval * npc.localAI[0];
                npc.rotation = -npc.ai[3];
                if (npc.ai[1] == 150f)
                {
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(DeathrayFire with { Volume = 2f }, npc.Center);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        //Vector2 speed = Vector2.UnitX.RotatedBy(npc.rotation);
                        //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed, ModContent.ProjectileType<RetinazerDeathray>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 3), 0f, Main.myPlayer, 0f, npc.whoAmI);
                    }
                    npc.netUpdate = true;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                    NetSync(npc);
                }
            }
            else if (npc.ai[1] <= 390f)
            {
                npc.velocity = Vector2.Zero;
                npc.localAI[1] = 0f;
                //if (--npc.ai[2] > 295f) npc.ai[2] = 295f;
                npc.ai[3] -= rotationInterval * npc.localAI[0];
                npc.rotation = -npc.ai[3];

                if (npc.ai[1] == 390)
                {
                    npc.netUpdate = true;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                    NetSync(npc);
                }
            }
            else if (npc.ai[1] < 450f)
            {
                npc.velocity = Vector2.Zero;
                npc.ai[3] -= rotationInterval * npc.localAI[0];
                npc.rotation = -npc.ai[3];
            }

            if (++npc.ai[1] > 450)
            {
                ChooseAttack(npc);
                NetSync(npc);
            }
            #endregion
        }
        

        #endregion

        #region 辅助方法
        public static void ChooseAttack(NPC npc)
        {
            npc.ai[1] = npc.ai[2] = npc.ai[3] = npc.localAI[0] = npc.localAI[1] = npc.localAI[2] = npc.localAI[3] = 0;
            IPTwins pT = GetIPTwins(npc);
            List<List<TwinsAtt>> PhaseList = [pT.Phase1, pT.Phase2, pT.Phase3];
            if (pT.Phaseinit > PhaseList[pT.Phase - 1].Count - 1)
                pT.Phaseinit = 0;
            pT.AIState = PhaseList[pT.Phase - 1][pT.Phaseinit];
            pT.Phaseinit++;
            npc.netUpdate = true;
        }
        public static bool PhaseCheck(NPC npc, NPC bro)
        {
            GetTwins(npc, bro, out IPTwins Reti, out IPTwins Spaz);
            if ((npc.life < npc.lifeMax * 0.66f || bro.life < bro.lifeMax * 0.66f) && Reti.Phase == 1 && Spaz.Phase == 1)
            {
                Reti.Phase = Spaz.Phase = 2;
                Reti.AIState = Spaz.AIState = TwinsAtt.PhaseChange1st;
                Reti.Phaseinit = Spaz.Phaseinit = 0;
                npc.ai[1] = npc.ai[2] = npc.ai[3] = npc.localAI[0] = npc.localAI[1] = npc.localAI[2] = npc.localAI[3] = 0;
                bro.ai[1] = bro.ai[2] = bro.ai[3] = bro.localAI[0] = bro.localAI[1] = bro.localAI[2] = bro.localAI[3] = 0;
                npc.netUpdate = bro.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                if (bro.netSpam > 10)
                    bro.netSpam = 10;
                return false;
            }
            return true;
        }
        public static bool AliveCheck(NPC npc, Player player)
        {
            bool length = Vector2.Distance(npc.Center, player.Center) > 5000f;
            if (!player.active || player.dead || length || Main.IsItDay())
            {
                npc.TargetClosest();
                player = Main.player[npc.target];
                if (!player.active || player.dead || length || Main.IsItDay())
                {
                    npc.EncourageDespawn(10);
                    npc.velocity.Y -= 0.04f;
                    return false;
                }
            }
            return true;
        }
        public static void RotateTowards(NPC npc, Vector2 targetPos, float turnSpeed = 0.18f) => P_EyeOfCthulhu.RotateTowards(npc, targetPos, turnSpeed);
        public static void TwinMove(NPC npc, Vector2 targetPos, float accel = 0.22f, float decelMult = 2f)
        {
            Vector2 target = targetPos - npc.Center;
            if (npc.velocity.X < target.X)
            {
                npc.velocity.X += accel;
                if (npc.velocity.X < 0f && target.X > 0f)
                    npc.velocity.X += accel * decelMult;
            }
            else if (npc.velocity.X > target.X)
            {
                npc.velocity.X -= accel;
                if (npc.velocity.X > 0f && target.X < 0f)
                    npc.velocity.X -= accel * decelMult;
            }
            if (npc.velocity.Y < target.Y)
            {
                npc.velocity.Y += accel;
                if (npc.velocity.Y < 0f && target.Y > 0f)
                    npc.velocity.Y += accel * decelMult;
            }
            else if (npc.velocity.Y > target.Y)
            {
                npc.velocity.Y -= accel;
                if (npc.velocity.Y > 0f && target.Y < 0f)
                    npc.velocity.Y -= accel * decelMult;
            }
        }
        public static Vector2 ShootPos(NPC npc) => npc.Center + (npc.width - 24) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
        public static IPTwins GetIPTwins(NPC npc)
        {
            if (npc.type == NPCID.Retinazer)
                return npc.GetGlobalNPC<P_Retinazer>();
            if (npc.type == NPCID.Spazmatism)
                return npc.GetGlobalNPC<P_Spazmatism>();
            return null;
        }
        public static void GetTwins(NPC npc, NPC bro, out IPTwins Reti, out IPTwins Spaz)
        {
            NPC retiNpc = npc.type == NPCID.Retinazer ? npc : bro;
            NPC spazNpc = npc.type == NPCID.Spazmatism ? npc : bro;
            Reti = retiNpc.GetGlobalNPC<P_Retinazer>();
            Spaz = spazNpc.GetGlobalNPC<P_Spazmatism>();
        }
        public static void SpawnElectricSpark(NPC npc, Vector2 vel)
        {
            for (int j = -3; j <= 3; j++)
            {
                Vector2 particleVel = (vel * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                    .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                Particle p = new ElectricSpark(ShootPos(npc),
                    particleVel, npc.type == NPCID.Retinazer ? Color.Yellow : Color.Green, Main.rand.NextFloat(0.7f, 1f), 20);
                p.Spawn();
            }
        }
        public static void JunengAnimation(NPC npc, int num = 3)
        {
            int maxr = 400;
            for (int i = 0; i < num; i++)
            {
                int r = Main.rand.Next(maxr + 1);
                Vector2 vec = r * Main.rand.NextVector2Unit();
                Vector2 spawn = ShootPos(npc) + vec;
                Color c = npc.type == NPCID.Retinazer ? Color.Red : Color.Green;
                SparkParticle spark = new SparkParticle(spawn, -0.04f * vec, c, 0.5f, 20);
                spark.Spawn();
                //FireMetaBall fm = ModContent.GetInstance<FireMetaBall>();
                //fm.CreateParticle(ShootPos(npc), vec, 60f);
            }
            
        }
        private void ManageAuraRadius()
        {
            if (Phase >= 2 && AIState != TwinsAtt.PhaseChange1st)
            {
                if (AIState == TwinsAtt.Deathray)
                {
                    AuraRadius -= 5;
                    if (AuraRadius < 600)
                        AuraRadius = 600;
                }
                else
                {
                    AuraRadius += 5;
                    if (AuraRadius > 1500)
                        AuraRadius = 1500;
                }
            }
        }
        public void ManangeAura(NPC npc)
        {
            if (Phase >= 2 && AIState != TwinsAtt.PhaseChange1st)
            {
                EModeGlobalNPC.Aura(npc, AuraRadius, true, -1, default, ModContent.BuffType<OiledBuff>());
                float threshold = AuraRadius;

                Player localPlayer = Main.LocalPlayer;
                float distance = localPlayer.Distance(npc.Center);
                if (localPlayer.active && !localPlayer.dead && !localPlayer.ghost) //pull into arena
                {
                    if (distance > threshold && distance < threshold * 4f)
                    {
                        if (distance > threshold * 2f)
                        {
                            localPlayer.Incapacitate();
                            localPlayer.velocity.X = 0f;
                            localPlayer.velocity.Y = -0.4f;
                        }

                        Vector2 movement = npc.Center - localPlayer.Center;
                        float difference = movement.Length() - threshold;
                        movement.Normalize();
                        movement *= difference < 30f ? difference : 30f;
                        localPlayer.position += movement;
                    }
                }
            }
        }

        #endregion
        #region 重写方法
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);
            binaryWriter.Write(AuraRadius);
            binaryWriter.Write(AuraOpacity);
            binaryWriter.Write7BitEncodedInt(Phaseinit);
            binaryWriter.Write7BitEncodedInt(Phase);
            binaryWriter.Write7BitEncodedInt((int)AIState);
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);
            AuraRadius = binaryReader.ReadSingle();
            AuraOpacity = binaryReader.ReadSingle();
            Phaseinit = binaryReader.Read7BitEncodedInt();
            Phase = binaryReader.Read7BitEncodedInt();
            AIState = (TwinsAtt)binaryReader.Read7BitEncodedInt();
        }
        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            if (npc.ai[0] < 2)
                return base.GetAlpha(npc, drawColor);
            return new Color(255, drawColor.G / 2, drawColor.B / 2);
        }
        public override bool CheckDead(NPC npc)
        {
            NPC bro = FargoSoulsUtil.NPCExists(EModeGlobalNPC.spazBoss, NPCID.Spazmatism);
            if (bro == null)
                return true;
            P_Spazmatism Spaz = bro.GetGlobalNPC<P_Spazmatism>();
            if (Phase <= 2 || Spaz.Phase <= 2)
            {
                npc.ai[1] = npc.ai[2] = npc.ai[3] = npc.localAI[0] = npc.localAI[1] = npc.localAI[2] = npc.localAI[3] = 0;
                bro.ai[1] = bro.ai[2] = bro.ai[3] = bro.localAI[0] = bro.localAI[1] = bro.localAI[2] = bro.localAI[3] = 0;
                Phase = Spaz.Phase = 3;
                AIState = Spaz.AIState = TwinsAtt.PhaseChange2nd;
                npc.life = bro.life = 1;
                npc.dontTakeDamage = bro.dontTakeDamage = true;
                npc.netUpdate = bro.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                if (bro.netSpam > 10)
                    bro.netSpam = 10;

                return false;
            }
            if (Phase >= 3 && Spaz.Phase >= 3 && bro.life == 1 && bro.dontTakeDamage && AIState != TwinsAtt.PhaseChange2nd && Spaz.AIState != TwinsAtt.PhaseChange2nd)
                return true;
            else
            {
                npc.life = 1;
                npc.dontTakeDamage = true;
                npc.netUpdate = true;
                return false;
            }
        }
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (false)
                return false;
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }
        #endregion
        #region 绘制
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 AuraPosition = npc.Center;
            if (Phase >= 2)
                DrawAura(npc, spriteBatch, AuraPosition);

            Vector2 offset = ShootPos(npc);
            Vector2 position = npc.Center + offset;
            Texture2D flare = MiscTexturesRegistry.BloomFlare.Value;
            float flarescale = Main.rand.NextFloat(0.1f, 0.15f);


            if (npc.GetGlobalNPC<Retinazer>().DeathrayState == 2)
            {
                Main.spriteBatch.Draw(flare, position - Main.screenPosition, null, Color.Red with { A = 0 }, Main.GlobalTimeWrappedHourly * -2f, flare.Size() * 0.5f, flarescale, 0, 0f);
                Main.spriteBatch.Draw(flare, position - Main.screenPosition, null, Color.Red with { A = 0 }, Main.GlobalTimeWrappedHourly * 2f, flare.Size() * 0.5f, flarescale, 0, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

            }
            return true;
        }
        public void DrawAura(NPC npc, SpriteBatch spriteBatch, Vector2 position)
        {
            if (AuraOpacity < 1f)
                AuraOpacity += 0.01f;

            Color darkColor = Color.DarkRed;
            Color mediumColor = Color.Red;
            Color lightColor2 = Color.Lerp(Color.IndianRed, Color.White, 0.35f);
            Vector2 auraPos = position;
            float radius = AuraRadius;
            var blackTile = TextureAssets.MagicPixel;
            var diagonalNoise = FargosTextureRegistry.Techno1Noise;
            if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                return;
            var maxOpacity = npc.Opacity * AuraOpacity;

            ManagedShader borderShader = ShaderManager.GetShader("FargowiltasSouls.TwinsAuraShader");
            borderShader.TrySetParameter("colorMult", 7.35f);
            borderShader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
            borderShader.TrySetParameter("radius", radius);
            borderShader.TrySetParameter("anchorPoint", auraPos);
            borderShader.TrySetParameter("screenPosition", Main.screenPosition);
            borderShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
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
        }
        #endregion
    }
    /// <summary>
    /// 魔焰眼
    /// </summary>
    public class P_Spazmatism : PModeNPCBehaviour, IPTwins
    {
        #region 不常修改
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.Spazmatism);
        public TwinsAtt AIState { get; set; }
        public int Phase { get; set; } = 1;
        public int Phaseinit { get; set; } = 1;
        public bool Ignite { get; set; } = false;
        public bool isDeathray { get; set; } = false;
        public int OrbColor => MechElectricOrb.Green;
        public override void StopEmodeAI(NPC npc)
        {
            npc.GetGlobalNPC<Spazmatism>().RunEmodeAI = false;
        }
        public override bool SafePreAI(NPC npc)
        {
            EModeGlobalNPC.spazBoss = npc.whoAmI;
            NPC bro = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            if (bro == null)
                return false;
            P_Retinazer Reti = bro.GetGlobalNPC<P_Retinazer>();
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.target = bro.target;
            Player player = Main.player[npc.target];
            //ShootPos = npc.Center + (npc.width - 24) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
            Phase = Reti.Phase;

            if (bro == null && Phase == 3)
                CheckDead(npc);

            if (!AliveCheck(npc, player))
                return false;
            PhaseCheck(npc, bro);
            if (Phase >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, 0f, 0f, 0, default, 1.8f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 6f;
                }
            }//粒子效果
            PHTwinsAI(npc, player, bro);
            return false;
        }
        #endregion
        public List<TwinsAtt> Phase1 => [

            ];
        
        public List<TwinsAtt> Phase2 => [

            ];
        public List<TwinsAtt> Phase3 => [

            ];
        #region AI方法

        #endregion
        #region 辅助方法

        #endregion
        #region 重写方法
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);

            binaryWriter.Write(npc.localAI[0]);
            binaryWriter.Write(npc.localAI[1]);
            binaryWriter.Write(npc.localAI[2]);
            binaryWriter.Write(npc.localAI[3]);
            //binaryWriter.Write7BitEncodedInt(TeleportDirection);
            //binaryWriter.Write7BitEncodedInt(LastAIState);
            //binaryWriter.Write7BitEncodedInt(Last2AIState);
            //binaryWriter.Write7BitEncodedInt(HyperTime);
            //binaryWriter.Write7BitEncodedInt(P3AttackChange);
            binaryWriter.Write(Phase);
            binaryWriter.Write(Phaseinit);
            binaryWriter.Write((int)AIState);
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);
            npc.localAI[0] = binaryReader.ReadSingle();
            npc.localAI[1] = binaryReader.ReadSingle();
            npc.localAI[2] = binaryReader.ReadSingle();
            npc.localAI[3] = binaryReader.ReadSingle();
            //TeleportDirection = binaryReader.Read7BitEncodedInt();
            //LastAIState = binaryReader.Read7BitEncodedInt();
            //Last2AIState = binaryReader.Read7BitEncodedInt();
            //HyperTime = binaryReader.Read7BitEncodedInt();
            //3AttackChange = binaryReader.Read7BitEncodedInt();
            Phaseinit = binaryReader.Read7BitEncodedInt();
            Phase = binaryReader.Read7BitEncodedInt();
            AIState = (TwinsAtt)binaryReader.ReadSingle();
        }
        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            if (npc.ai[0] < 2)
                return base.GetAlpha(npc, drawColor);
            return new Color(drawColor.R / 2, 255, drawColor.B / 2);
        }
        public override bool CheckDead(NPC npc)
        {
            NPC bro = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            if (bro == null)
                return true;
            P_Retinazer Reti = bro.GetGlobalNPC<P_Retinazer>();
            if (Phase <= 2 || Reti.Phase <= 2)
            {
                npc.ai[1] = npc.ai[2] = npc.ai[3] = npc.localAI[0] = npc.localAI[1] = npc.localAI[2] = npc.localAI[3] = 0;
                bro.ai[1] = bro.ai[2] = bro.ai[3] = bro.localAI[0] = bro.localAI[1] = bro.localAI[2] = bro.localAI[3] = 0;
                Phase = Reti.Phase = 3;
                AIState = Reti.AIState = TwinsAtt.PhaseChange2nd;
                npc.life = bro.life = 1;
                npc.dontTakeDamage = bro.dontTakeDamage = true;
                npc.netUpdate = bro.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                if (bro.netSpam > 10)
                    bro.netSpam = 10;

                return false;
            }
            if (Phase >= 3 && Reti.Phase >= 3 && bro.life == 1 && bro.dontTakeDamage && AIState != TwinsAtt.PhaseChange2nd && Reti.AIState != TwinsAtt.PhaseChange2nd)
                return true;
            else
            {
                npc.life = 1;
                npc.dontTakeDamage = true;
                npc.netUpdate = true;
                return false;
            }
        }
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (false)
                return false;
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }
        #endregion
    }
}
