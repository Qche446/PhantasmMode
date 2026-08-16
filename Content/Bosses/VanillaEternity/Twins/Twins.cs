using FargosPhantasmMode.Content.Bossbar;
using FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins.P_Retinazer;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// 激光眼
    /// </summary>
    public class P_Retinazer : PModeNPCBehaviour, IPTwins
    {
        #region 不常修改
        public static readonly SoundStyle DeathrayFire = new SoundStyle("FargosPhantasmMode/Assets/Sounds/DeathrayFire")
        {
            Volume = 2f,          // 音量 (0.0f 到 1.0f)
            PitchVariance = 0.3f,   // 音高随机变化范围，增加声音自然度
            MaxInstances = 1,       // 最多同时存在的实例数，防止声音叠加
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
        };

        public static readonly SoundStyle LensEject = new SoundStyle("FargosPhantasmMode/Assets/Sounds/LensEject");

        public float AuraOpacity = 0;

        public float AuraRadius = 1;

        public bool DroppedSummon;

        public TwinsAtt AIState { get; set; }

        public bool Ghost { get; set; } = false;

        public bool Ignite { get; set; } = false;

        public int IgniteTimer { get; set; } = 0;

        public int OrbColor => MechElectricOrb.Yellow;

        public int Phase { get; set; } = 1;

        public int Phaseinit { get; set; } = 1;

        public override int NPCType => NPCID.Retinazer;
        public override bool SafePreAI(NPC npc)
        {
            EModeGlobalNPC.retiBoss = npc.whoAmI;
            NPC bro = FargoSoulsUtil.NPCExists(EModeGlobalNPC.spazBoss, NPCID.Spazmatism);
            if (bro != null)
            {
                P_Spazmatism Spaz = bro.GetGlobalNPC<P_Spazmatism>();
                Spaz.Phase = Phase;
                PhaseCheck(npc, bro);
            }
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();
            Player player = Main.player[npc.target];

            if (!AliveCheck(npc, player))
                return false;
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
            PHTwinsAI(npc, player);
            ManangeAura(npc);
            ManageAuraRadius(npc);
            EModeUtils.DropSummon(npc, "MechEye", NPC.downedMechBoss2, ref DroppedSummon, Main.hardMode);
            return false;
        }

        public override void SetDefaults(NPC npc)
        {

        }
        public override void StopEmodeAI(NPC npc) => npc.GetGlobalNPC<Retinazer>().RunEmodeAI = false;
        #endregion
        public static readonly List<TwinsAtt> phase1 = [
            TwinsAtt.NormalShoot,
            TwinsAtt.FlankingShoot
            ];
        public static readonly List<TwinsAtt> phase2 = [
            TwinsAtt.NormalShoot,
            TwinsAtt.FlankingShoot,
            TwinsAtt.PolyRing,
            TwinsAtt.FlankingShoot,
            TwinsAtt.NormalShoot,
            TwinsAtt.CurvedDeathRay,
            ];
        public static readonly List<TwinsAtt> phase3 = [
            TwinsAtt.LocatedShoot,
            TwinsAtt.Final_PolyRing,
            TwinsAtt.BulletHell_Open,
            TwinsAtt.BulletHell_End,
            TwinsAtt.Final_Deathray,
            ];
        public List<TwinsAtt> Phase1 => phase1;
        public List<TwinsAtt> Phase2 => phase2;
        public List<TwinsAtt> Phase3 => phase3;
        public static void PHTwinsAI(NPC npc, Player player)
        {
            //Main.NewText(NPCID.Sets.TrailingMode[npc.type]);
            ManageIgnite(npc);
            IPTwins self = GetIPTwins(npc);
            switch (self.AIState)
            {
                case TwinsAtt.PhaseChange1st: PhaseChange1st(npc); break;
                case TwinsAtt.NormalShoot: NormalShoot(npc, player); break;
                case TwinsAtt.FlankingShoot: FlankingShoot(npc, player); break;
                case TwinsAtt.CurFireDash: CurFireDash(npc, player); break;
                case TwinsAtt.LegFireDash: LegFireDash(npc, player); break;
                case TwinsAtt.P1_BreathedFire: P1_BreathedFire(npc, player); break;

                case TwinsAtt.PhaseChange2nd: PhaseChange2nd(npc); break;
                //case TwinsAtt.SineShoot: SineShoot(npc, player); break;
                case TwinsAtt.PolyRing: PolyRing(npc, player); break;
                case TwinsAtt.CurvedDeathRay: CurvedDeathRay(npc, player); break;
                case TwinsAtt.P2_BreathedFire: P2_BreathedFire(npc, player); break;
                case TwinsAtt.RollingShoot: RollingShoot(npc); break;

                case TwinsAtt.PhaseChange3rd: PhaseChange3rd(npc); break;
                case TwinsAtt.LocatedShoot: LocatedShoot(npc, player); break;

                case TwinsAtt.Final_PolyRing: Final_PolyRing(npc, player); break;
                case TwinsAtt.BulletHell_Open: BulletHell_Open(npc, player); break;
                case TwinsAtt.BulletHell_End: BulletHell_End(npc, player); break;
                case TwinsAtt.Final_Deathray: Final_Deathray(npc, player); break;

                case TwinsAtt.FireRotate: FireRotate(npc, player);  break;
                case TwinsAtt.Final_LegFireDash: Final_LegFireDash(npc, player); break;
                case TwinsAtt.Final_CurFireDashBreathed: Final_CurFireDashBreathed(npc, player); break;
                case TwinsAtt.Final_Embers: Final_Embers(npc, player); break;
                default: break;
            }
        }
        #region 新ai方法
        public static void BulletHell_End(NPC npc, Player player)
        {
            int chaseDuration = 480;
            //IPTwins self = GetIPTwins(npc);
            npc.velocity *= 0.85f;
            RotateTowards(npc, player.Center);
            if (npc.ai[1] == 20)
                npc.localAI[0] = (player.Center - ShootPos(npc)).ToRotation();
            Vector2 vel = npc.localAI[0].ToRotationVector2();
            float progress = npc.ai[1] / 240f;
            npc.ai[2] += MathHelper.SmoothStep(1f, 5.5f, progress);
            float maxspeed = MathHelper.SmoothStep(8, 12, progress);
            float inter = MathHelper.SmoothStep(10, 3, progress);
            npc.ai[3]++;
            if (npc.ai[2] > 20 && npc.ai[1] > 20)
            {
                npc.ai[2] = 0;
                int projType = ModContent.ProjectileType<MechElectricOrb>();

                for (int i = -1; i <= 1; i += 2)
                {
                    float max = 4;
                    float Minister = MathHelper.TwoPi / max;
                    for (int j = 0; j < max; j++)
                    {
                        int orbColor = (j - 1.5f) > 0 ? MechElectricOrb.Yellow : MechElectricOrb.Green;
                        double interangle = i * (1 + Math.Sin(npc.ai[1] * MathHelper.Pi / 90 + j * Minister)) * MathHelper.Pi / 2f;
                        if (Math.Abs(interangle) > inter * MathF.PI / 180f && FargoSoulsUtil.HostCheck)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                                maxspeed * vel.RotatedBy(interangle), projType, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f,
                                Main.myPlayer, 0, ai2: orbColor);
                        }
                    }

                    /*
                    double interangle2 = i * (1 - Math.Sin(npc.ai[1] * MathHelper.Pi / 90)) * MathHelper.Pi / 4f;
                    if (Math.Abs(interangle2) > 5 * MathF.PI / 180f)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                        maxspeed * vel.RotatedBy(interangle2), projType, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f,
                        Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                    }
                    */
                }
                SpawnElectricSpark(npc, vel);
                npc.velocity -= 0.8f * vel;
            }
            if (++npc.ai[1] > chaseDuration + 30)
            {
                ChooseAttack(npc);
            }
        }

        public static void BulletHell_Open(NPC npc, Player player)
        {
            //IPTwins self = GetIPTwins(npc);
            int chaseDuration = 180;

            //int flagY = npc.Center.Y > player.Center.Y ? 1 : -1;
            TwinMove(npc, player.Center - 200 * Vector2.UnitY, 2);
            npc.velocity *= 0.8f;
            RotateTowards(npc, player.Center);
            //float progress = npc.ai[1] / (float)chaseDuration;
            npc.ai[2] += MathHelper.SmoothStep(1, 6, npc.ai[1] / 120f);

            if (npc.ai[2] > 20 && npc.ai[1] <= chaseDuration)
            {
                npc.ai[2] = 0f;
                Vector2 vel = npc.SafeDirectionTo(player.Center);
                //int laserType = ModContent.ProjectileType<MechElectricOrb>();
                float spreadAngle = 0;
                if (npc.ai[1] >= 60)
                {
                    spreadAngle = MathHelper.SmoothStep(2, 0.4f, (npc.ai[1] - 60f) / 120f);
                }
                int spread = npc.ai[1] <= 120 ? 1 : 2;

                Vector2 spawnPos = ShootPos(npc);
                for (int i = -spread; i <= spread; i++)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 shotVel2 = vel.RotatedBy(MathHelper.PiOver2 * spreadAngle * i);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, shotVel2, ModContent.ProjectileType<DarkStarAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer);
                        SpawnElectricSpark(npc, vel);
                    }
                }
                npc.velocity -= 0.5f * vel;
            }
            if (++npc.ai[1] >= chaseDuration + 30)
                ChooseAttack(npc);
        }

        public static void CurFireDash(NPC npc, Player player)
        {
            npc.TryGetGlobalNPC<P_Spazmatism>(out P_Spazmatism self);
            bool ignite = self.Ignite;
            int chaseDuration = 400;
            float modifier = npc.GetLifePercent() * npc.GetLifePercent();
            npc.ai[2] += 1f;
            if (npc.ai[2] <= 30)
            {
                npc.ai[2] -= modifier / 1.5f;
                npc.ai[2] -= Main.getGoodWorld ? 0.15f : 0.25f;
                RotateTowards(npc, player.Center, 0.12f);
                if (npc.ai[2] <= 20)
                    npc.velocity *= 0.94f;
                if (npc.velocity.Length() < 0.1f)
                    npc.velocity = Vector2.Zero;
            }
            else if (npc.ai[2] <= 80)
            {
                if (npc.localAI[0] == 0)
                {
                    npc.localAI[0] = 1;
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                    float chargeSpeed = MathHelper.Lerp(21, 16, modifier);
                    npc.velocity = chargeSpeed * npc.SafeDirectionTo(player.Center);
                    npc.netUpdate = true;
                }
                npc.rotation = npc.velocity.ToRotation() - 1.57f;
                if (npc.HasValidTarget && ++npc.ai[3] > 2) //cursed flamethrower when dashing
                {
                    npc.ai[3] = 0;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int projtype = ignite ? ModContent.ProjectileType<ShadowFlame>() : ProjectileID.EyeFire;
                        float dashTime = 50f;
                        Vector2 spawnPos = ignite ? ShootPos(npc) : npc.Center;
                        float extension = MathF.Sin(MathF.PI * (npc.ai[2] - 30) / dashTime);
                        if (extension < 0)
                            extension = 0;
                        float speed = extension * 0.55f;
                        float rotationVariance = 9f * extension * 0.75f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, speed * npc.velocity.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-rotationVariance, rotationVariance))), projtype, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    }
                }
                if (npc.HasValidTarget)
                {
                    Vector2 toTarget = npc.DirectionTo(Main.player[npc.target].Center);
                    npc.velocity += toTarget * 0.27f;
                    npc.velocity = npc.velocity.RotateTowards(toTarget.ToRotation(), 0.007f);
                }
            }
            if (npc.ai[2] >= 80) // 冲刺结束
            {
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                if (npc.ai[1] >= chaseDuration)
                    ChooseAttack(npc);

            }
            ++npc.ai[1];
        }

        public static void CurvedDeathRay(NPC npc, Player player)
        {
            #region 主逻辑
            //float rotationInterval = 1.05f * 2f * (float)Math.PI * 1.2f / 4f / 60f;
            if (npc.ai[1] == 0)
            {
                npc.localAI[0] = Main.player[npc.target].Center.X - npc.Center.X < 0 ? 1 : -1;
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
                SoundEngine.PlaySound(FargosSoundRegistry.TwinsWarning with { Volume = 4f }, npc.Center);

                NPC bro = FargoSoulsUtil.NPCExists(EModeGlobalNPC.spazBoss, NPCID.Spazmatism);
                if (bro != null)
                {
                    bro.ai[1] = bro.ai[2] = bro.ai[3] = bro.localAI[0] = bro.localAI[1] = bro.localAI[2] = bro.localAI[3] = 0;
                    IPTwins Ibro = GetIPTwins(bro);
                    Ibro.AIState = TwinsAtt.RollingShoot;
                }

                npc.netUpdate = true;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                NetSync(npc);
            }
            if (npc.ai[1] == 30 && FargoSoulsUtil.HostCheck)
            {
                float num = 4;
                for (int i = 0; i < num; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / num),
                        ModContent.ProjectileType<TwinCurvedLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0, Main.myPlayer,
                        npc.whoAmI, 180, 1.2f * npc.localAI[0]);
                }
                if (Main.getGoodWorld)
                {
                    for (int i = 0; i < num; i++)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / num),
                            ModContent.ProjectileType<TwinCurvedLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0, Main.myPlayer,
                            npc.whoAmI, 360, 1.2f * npc.localAI[0]);
                    }
                }
                npc.netUpdate = true;
            }
            if (npc.ai[1] <= 150f)
            {
                Vector2 pos = player.Center + player.DirectionTo(npc.Center) * 250;
                npc.velocity = FargoSoulsUtil.SmartAccel(npc.Center, pos, npc.velocity, 0.9f, 0.9f);

                npc.velocity *= 1f - npc.ai[1] / 120f;
                npc.localAI[1] = 0f;

                if (npc.ai[1] == 150f)
                {
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(FargosSoundRegistry.TwinsDeathray with { Volume = 2f }, npc.Center);
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
            }

            if (++npc.ai[1] > 450)
            {
                ChooseAttack(npc);
                NetSync(npc);
            }
            #endregion
        }

        public static void Final_CurFireDashBreathed(NPC npc, Player player)
        {
            npc.TryGetGlobalNPC<P_Spazmatism>(out P_Spazmatism self);
            bool ignite = self.Ignite;
            //int chaseDuration = 400;
            //float modifier = npc.GetLifePercent() * npc.GetLifePercent();
            npc.localAI[3]++;
            float progress = npc.localAI[3] / 300f;
            npc.ai[2] += MathHelper.SmoothStep(1f, 2.2f, progress);
            if (npc.ai[2] <= 20)
            {
                //npc.ai[2] -= 0.15f;
                RotateTowards(npc, player.Center, 0.12f);
                if (npc.ai[2] <= 20)
                    npc.velocity *= 0.94f;
                if (npc.velocity.Length() < 0.1f)
                    npc.velocity = Vector2.Zero;
            }
            else if (npc.ai[2] <= 60)
            {
                if (npc.localAI[0] == 0)
                {
                    npc.localAI[0] = 1;
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                    float chargeSpeed = MathHelper.SmoothStep(30, 40f, progress);
                    npc.velocity = chargeSpeed * npc.SafeDirectionTo(player.Center);
                    npc.rotation = npc.velocity.ToRotation() - 1.57f;
                    ScreenShakeSystem.StartShake(3f);
                    npc.netUpdate = true;
                }

                if (npc.HasValidTarget && ++npc.ai[3] > 2) //cursed flamethrower when dashing
                {
                    npc.ai[3] = 0;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int projtype = ignite ? ModContent.ProjectileType<ShadowFlame>() : ProjectileID.EyeFire;
                        float dashTime = 40f;
                        Vector2 spawnPos = ignite ? ShootPos(npc) : npc.Center;
                        float extension = MathF.Sin(MathF.PI * (npc.ai[2] - 20) / dashTime);
                        if (extension < 0)
                            extension = 0;
                        float speed2 = extension * 0.55f;
                        float rotationVariance = 9f * extension * 0.75f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, speed2 * npc.velocity.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-rotationVariance, rotationVariance))), projtype, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    }
                }
                if (npc.HasValidTarget)
                {
                    Vector2 toTarget = npc.DirectionTo(Main.player[npc.target].Center);
                    npc.velocity += toTarget * MathHelper.SmoothStep(0.3f, 0.4f, progress);
                    npc.velocity = npc.velocity.RotateTowards(toTarget.ToRotation(), 0.007f);
                }
            }
            Vector2 targetPos = player.Center;// + 600 * player.SafeDirectionTo(npc.Center);
            float omiga = 0.012f;
            float distanceToTarget = npc.Distance(targetPos);
            float aof = Math.Abs(MathHelper.WrapAngle(npc.rotation + MathHelper.PiOver2 - (player.Center - npc.Center).ToRotation()));
            int soi = Math.Sign(MathHelper.WrapAngle(npc.rotation + MathHelper.PiOver2 - (player.Center - npc.Center).ToRotation()));
            if (aof > 0.15f * MathF.PI) omiga += 0.01f;
            if (aof > 0.25f * MathF.PI) omiga += 0.01f;
            if (aof > 0.35f * MathF.PI) omiga += 0.01f;
            if (distanceToTarget < 300f) omiga += 0.004f;
            if (distanceToTarget > 800f) omiga *= 0.6f;
            RotateTowards(npc, player.Center, omiga);
            //npc.ai[2] += 1f;
            npc.localAI[2] += 1.2f;
            if (npc.localAI[2] > 22f)
            {
                npc.localAI[2] = 0f;
                SoundEngine.PlaySound(SoundID.Item34, npc.position);
            }

            // 弹幕发射计时器
            if (npc.ai[2] > 20)
            {
                //float prece = npc.GetLifePercent();
                npc.localAI[1] += 1f;

                if (npc.localAI[1] > 1f)
                {
                    float projectileSpeed = MathHelper.SmoothStep(2, 30, (npc.ai[2] - 20) / 40f);
                    int projectileDamage = FargoSoulsUtil.ScaledProjectileDamage(npc.damage);
                    Vector2 vel = projectileSpeed * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        for (int i = 0; i < 3; i++)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel * Main.rand.NextFloat(0.6f, 1f), ModContent.ProjectileType<ShadowFlame>(), projectileDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel, ModContent.ProjectileType<DarkStarAcc>(), projectileDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel.RotatedBy(soi * MathHelper.Pi / 3f), ModContent.ProjectileType<DarkStarAcc>(), projectileDamage, 0f, Main.myPlayer);
                        //Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel.RotatedBy(soi * MathHelper.Pi / 1.5f), ModContent.ProjectileType<DarkStarAcc>(), projectileDamage, 0f, Main.myPlayer);
                    }
                }
            }

            if (npc.ai[2] >= 60) // 冲刺结束
            {
                ++npc.ai[1];
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                if (npc.ai[1] >= 23)
                    ChooseAttack(npc);
            }

        }

        public static void Final_Deathray(NPC npc, Player player)
        {
            if (npc.ai[1] % 15 == 0 && npc.ai[1] < 420 && npc.ai[1] >= 60)
            {
                float speed = 25f * Math.Min((npc.ai[1] - 60) / 60f, 1f);
                int timeLeft = (int)(speed / 27 * 90f);
                float AngOff = MathHelper.Pi / 16f;
                if (npc.ai[1] >= 150)
                    AngOff *= MathHelper.SmoothStep(2, 0.5f, (npc.ai[1] - 150f) / 180f);
                if (timeLeft > 60 && FargoSoulsUtil.HostCheck)
                {
                    IPTwins pTwins = GetIPTwins(npc);
                    int projType = ModContent.ProjectileType<DarkStarSplit>();
                    float num = 13;
                    for (float i = -num + 1; i < num; i++)
                    {
                        float baseRotation = MathHelper.Pi / 5f + Math.Abs(i) * AngOff;
                        if (i < 0)
                            baseRotation *= -1;
                        Vector2 vel = speed * npc.SafeDirectionTo(player.Center).RotatedBy(baseRotation);
                        Projectile p = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), ShootPos(npc),
                            vel, projType,
                            FargoSoulsUtil.ScaledProjectileDamage(npc.damage),
                            0, Main.myPlayer, Math.Min(40, timeLeft), 0, pTwins.OrbColor);
                        if (p.active)
                            p.timeLeft = timeLeft;
                    }
                }
            }
            if (npc.ai[1] >= 420)
            {
                npc.rotation += MathHelper.SmoothStep(0.6f, 0, (npc.ai[1] - 420f) / 30f);
            }
            #region 主逻辑
            //float rotationInterval = 1.05f * 2f * (float)Math.PI * 1.2f / 4f / 60f;
            if (npc.ai[1] == 0)
            {
                npc.localAI[0] = Main.player[npc.target].Center.X - npc.Center.X < 0 ? 1 : -1;
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
                SoundEngine.PlaySound(FargosSoundRegistry.TwinsWarning with { Volume = 4f }, npc.Center);
                npc.netUpdate = true;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                NetSync(npc);
            }
            if (npc.ai[1] == 30 && FargoSoulsUtil.HostCheck)
            {
                float num = 4;
                for (int i = 0; i < num; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / num),
                        ModContent.ProjectileType<TwinCurvedLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0, Main.myPlayer,
                        npc.whoAmI, 180, 1.5f * npc.localAI[0]);
                }
                for (int i = 0; i < num; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / num),
                        ModContent.ProjectileType<TwinCurvedLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0, Main.myPlayer,
                        npc.whoAmI, -180, 1.5f * npc.localAI[0]);
                }
                for (int i = 0; i < num; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / num),
                        ModContent.ProjectileType<TwinCurvedLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0, Main.myPlayer,
                        npc.whoAmI, 360, 1.5f * npc.localAI[0]);
                }
                npc.netUpdate = true;
            }
            if (npc.ai[1] <= 150f)
            {
                Vector2 pos = player.Center + player.DirectionTo(npc.Center) * 250;
                npc.velocity = FargoSoulsUtil.SmartAccel(npc.Center, pos, npc.velocity, 0.9f, 0.9f);

                npc.velocity *= 1f - npc.ai[1] / 120f;
                npc.localAI[1] = 0f;

                if (npc.ai[1] == 150f)
                {
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(FargosSoundRegistry.TwinsDeathray with { Volume = 2f }, npc.Center);
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
            }

            if (++npc.ai[1] > 450)
            {
                npc.defense -= 20;
                ChooseAttack(npc);
                NetSync(npc);
            }
            #endregion
        }

        public static void Final_Embers(NPC npc, Player player)
        {
            if (npc.ai[1] == 0)
            {
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
            }
            if (npc.ai[1] < 60)
            {
                if (npc.ai[1] == 0)
                {
                    npc.localAI[0] = (npc.Center - player.Center).ToRotation();
                }
                Vector2 detalvec = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * Main.rand.NextFloat(0, 1) + npc.localAI[0]);
                Vector2 desired = player.Center + 360 * detalvec;
                TwinMove(npc, desired, 15, 0.6f, 4);
                RotateTowards(npc, desired);
                /*
                float offect = MathHelper.Pi / 3;
                if (npc.ai[1] % 2 == 0)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + offect),
                        ModContent.ProjectileType<SpazmatismGlowLine>(), 0, 0f, Main.myPlayer, 0, npc.whoAmI, offect);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 - offect),
                        ModContent.ProjectileType<SpazmatismGlowLine>(), 0, 0f, Main.myPlayer, 0, npc.whoAmI, -offect);
                }
                */
            }

            else
            {
                npc.velocity *= 0.9f;
            }
            if (npc.ai[1] == 90)
            {
                if (!Main.dedServ)
                    SoundEngine.PlaySound(DeathrayFire);

                //SoundEngine.PlaySound(FargosSoundRegistry.TwinsDeathray with { Volume = 2f }, npc.Center);
            }
            if (npc.ai[1] >= 120)
            {
                if (npc.ai[1] == 120)
                {
                    npc.localAI[1] = -Math.Sign(MathHelper.WrapAngle(npc.rotation + MathHelper.PiOver2 - (player.Center - npc.Center).ToRotation()));
                }
                int scycletime = 90;
                float omiga = MathHelper.Pi * (npc.ai[1] - 120) / (60 * scycletime);
                omiga = Math.Clamp(omiga, 0, MathHelper.Pi / scycletime);
                npc.rotation += omiga * npc.localAI[1];
                if (FargoSoulsUtil.HostCheck)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                            30 * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + MathHelper.ToRadians(Main.rand.NextFloat(-(npc.ai[1] - 120) / 24f, (npc.ai[1] - 120) / 24f))),
                            ModContent.ProjectileType<ShadowFlame>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer);
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center,
                            30 * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2 + MathHelper.ToRadians(Main.rand.NextFloat(-(npc.ai[1] - 120) / 24f, (npc.ai[1] - 120) / 24f))),
                            ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer);
                    }
                    if (npc.ai[1] >= 120 & npc.ai[1] % 10 == 0)
                    {
                        for (int i = 3; i <= 9; i++)
                        {
                            Vector2 target = 100 * i * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2) + npc.Center;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), target,
                                Vector2.Normalize(player.Center - target), ModContent.ProjectileType<DarkStarSpaz>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f,
                                Main.myPlayer, npc.target, ai2: MechElectricOrb.Green);
                        }
                    }
                }
                ScreenShakeSystem.StartShake(3f);
                npc.velocity -= 0.4f * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
            }
            if (++npc.ai[1] > 120 + 360)
            {
                npc.defense -= 20;
                ChooseAttack(npc);
            }
        }

        public static void Final_LegFireDash(NPC npc, Player player)
        {
            npc.TryGetGlobalNPC<P_Spazmatism>(out P_Spazmatism self);
            bool ignite = self.Ignite;
            int chaseDuration = 360;

            npc.ai[2] += MathHelper.SmoothStep(1, 2, npc.ai[1] / 240f);
            if (npc.ai[2] <= 40)
            {
                npc.ai[2] -= 0.2f;
                RotateTowards(npc, player.Center, 0.12f);
                if (npc.ai[2] <= 40)
                    npc.velocity *= 0.94f;
                if (npc.velocity.Length() < 0.1f)
                    npc.velocity = Vector2.Zero;
            }
            else if (npc.ai[2] <= 90)
            {
                if (npc.localAI[0] == 0)
                {
                    npc.localAI[0] = 1;
                    npc.localAI[2] = (player.Center - npc.Center).ToRotation();
                }
                if (npc.ai[2] >= 40 && npc.localAI[0] == 1)
                {
                    npc.localAI[0] = 2;
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                    float maxspeed = MathHelper.SmoothStep(30, 50, npc.ai[1] / 240f);
                    float chargeSpeed = maxspeed;
                    npc.velocity = chargeSpeed * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                    npc.netUpdate = true;
                }
                if (npc.ai[2] >= 40)
                    npc.rotation = npc.velocity.ToRotation() - 1.57f;
                else
                    RotateTowards(npc, npc.Center + Vector2.UnitX.RotatedBy(npc.localAI[2]), 0.12f);
                if (npc.HasValidTarget && ++npc.ai[3] > 2 && npc.ai[2] >= 40) //cursed flamethrower when dashing
                {
                    npc.ai[3] = 0;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int projtype = Main.rand.NextBool() ? ModContent.ProjectileType<ShadowFlame>() : ProjectileID.EyeFire;
                        float speed = 0.7f;
                        float rotationVariance = 0;
                        Vector2 spawnPos = ignite ? ShootPos(npc) : npc.Center;
                        float progress = (npc.ai[2] - 40f) / 40f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, progress * speed * npc.velocity.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-rotationVariance, rotationVariance))), projtype, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer);
                    }
                }
            }
            if (npc.ai[2] >= 90) // 冲刺结束
            {
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                if (npc.ai[1] >= chaseDuration)
                    ChooseAttack(npc);
            }
            ++npc.ai[1];
        }

        public static void Final_PolyRing(NPC npc, Player player)
        {
            int chaseDuration = 640;
            //IPTwins re = GetIPTwins(npc);
            npc.velocity *= 0;

            if (npc.ai[1] % 10 == 0 && npc.ai[1] < chaseDuration && FargoSoulsUtil.HostCheck && npc.ai[1] >= 40)
            {
                int max = 8;
                int projType = ModContent.ProjectileType<DarkStarPolyline>();
                for (int i = 0; i < max; i++)
                {
                    Vector2 vel = Vector2.UnitX.RotatedBy((i + max * npc.ai[2] / 120f) * MathHelper.TwoPi / max);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                        10 * vel, projType, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1f, Main.myPlayer,
                        0, 1, MechElectricOrb.Yellow);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                        10 * vel, projType, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1f, Main.myPlayer,
                        0, -1, MechElectricOrb.Yellow);
                    int projType2 = ModContent.ProjectileType<MechElectricOrb>();
                    if (npc.ai[1] % 20 == 0)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                            10 * Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / max - 4 * MathHelper.Pi * npc.ai[2] / 180f), projType2, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer,
                            0, ai2: MechElectricOrb.Yellow);
                    SpawnElectricSpark(npc, vel);
                }
                npc.ai[2]++;
            }
            if (npc.ai[1] >= chaseDuration)
            {
                RotateTowards(npc, player.Center);
                Vector2 targetPos = player.Center - npc.SafeDirectionTo(player.Center) * 450;
                float dis = npc.Distance(targetPos);
                if (dis > 800)
                    TwinMove(npc, targetPos, 6);
                else if (dis > 400)
                    TwinMove(npc, targetPos, 6, 0.3f);
                else if (dis < 100)
                    TwinMove(npc, targetPos, 6, 0.4f);
            }
            if (++npc.ai[1] > chaseDuration + 30)
            {
                ChooseAttack(npc);
            }
        }

        public static void FireRotate(NPC npc, Player player)
        {
            const float prepTime = 360;
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
            npc.localAI[0] += npc.localAI[2] * MathHelper.TwoPi / 120;
            if (npc.ai[1] <= prepTime)
            {
                npc.velocity = (desiredPos - npc.Center) * (npc.ai[1] / 150f + 0.5f);
                npc.rotation = npc.SafeDirectionTo(player.Center).ToRotation() + npc.localAI[2] * MathHelper.PiOver2 + MathHelper.PiOver2;
            }
            if (npc.velocity.Length() > 28)
            {
                npc.velocity = Vector2.Normalize(npc.velocity) * 28;
            }
            if (npc.ai[1] % 2 == 0 && npc.ai[1] > 30 && FargoSoulsUtil.HostCheck)
            {
                Vector2 vel = Vector2.Normalize(npc.velocity);
                for (float i = -0.25f; i <= 0.25f; i += 0.25f)
                {
                    int k = Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel.RotatedBy(i * MathF.PI), ModContent.ProjectileType<DarkStarAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                        Main.myPlayer, npc.target, ai2: pT.OrbColor);
                    Main.projectile[k].timeLeft = 180;
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), 1.5f * npc.SafeDirectionTo(player.Center), ModContent.ProjectileType<DarkStarAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                    Main.myPlayer, npc.target, ai2: pT.OrbColor);
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 10 * vel.RotatedByRandom(0.05f * MathF.PI), ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1, Main.myPlayer);
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 10 * vel.RotatedByRandom(0.05f * MathF.PI), ModContent.ProjectileType<ShadowFlame>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1, Main.myPlayer);
                SpawnElectricSpark(npc, vel);
            }
            if (npc.ai[1] >= prepTime)
            {
                npc.velocity *= 0.95f;
                RotateTowards(npc, player.Center);
            }
            if (++npc.ai[1] >= prepTime)
            {
                ChooseAttack(npc);
            }
        }

        public static void FlankingShoot(NPC npc, Player player)
        {
            IPTwins self = GetIPTwins(npc);
            int chaseDuration = self.Phase == 1 ? 180 : 216;
            int flagX = npc.OnRightSideOf(player) ? 1 : -1;
            TwinMove(npc, player.Center + 340 * flagX * Vector2.UnitX, 12f, 0.2f, 1.5f);
            RotateTowards(npc, player.Center);
            npc.ai[2] += 1.5f;
            float prece = npc.GetLifePercent();
            if (prece < 0.75)
                npc.ai[2] += 0.5f;
            if (prece < 0.5)
                npc.ai[2] += Main.getGoodWorld ? 1f : 0.75f;
            if (prece < 0.25)
                npc.ai[2] += 1f;
            if (prece < 0.1)
                npc.ai[2] += 1f;
            if (npc.ai[2] > (self.Ignite ? 40f : 55f))
            {
                npc.ai[2] = 0f;
                Vector2 vel = -player.SafeDirectionTo(ShootPos(npc));
                //int laserDamage = FargoSoulsUtil.ScaledProjectileDamage(npc.damage);
                int laserType = ModContent.ProjectileType<MechElectricOrb>();
                float spreadAngle = 0.45f;
                int spread = 0;
                Vector2 spawnPos = ShootPos(npc);
                if (FargoSoulsUtil.HostCheck)
                {
                    if (self.Ignite)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, vel, ModContent.ProjectileType<DarkStarTwins>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, npc.target);
                    }
                    else
                    {
                        for (int i = -spread; i <= spread; i++)
                        {
                            if (i == 0 && spread != 0)
                                continue;
                            Vector2 shotVel2 = 20 * vel.RotatedBy(MathHelper.PiOver2 * spreadAngle * i);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, shotVel2, laserType, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                            SpawnElectricSpark(npc, vel);
                        }
                    }
                }
                npc.velocity -= vel;
            }
            if (++npc.ai[1] >= chaseDuration)
                ChooseAttack(npc);
        }

        public static void LegFireDash(NPC npc, Player player)
        {
            npc.TryGetGlobalNPC<P_Spazmatism>(out P_Spazmatism self);
            bool ignite = self.Ignite;
            int chaseDuration = 400;
            float modifier = npc.GetLifePercent() * npc.GetLifePercent();
            npc.ai[2] += Main.getGoodWorld ? 1.7f : 1.4f;
            npc.ai[2] += ignite ? Main.getGoodWorld ? 0.35f : 0.1f : 0;
            if (npc.ai[2] <= 40)
            {
                npc.ai[2] -= modifier * (Main.getGoodWorld ? 0.8f : 0.6f);
                npc.ai[2] -= 0.1f;
                RotateTowards(npc, player.Center, 0.12f);
                if (npc.ai[2] <= 40)
                    npc.velocity *= 0.89f;
                if (npc.velocity.Length() < 0.1f)
                    npc.velocity = Vector2.Zero;
            }
            else if (npc.ai[2] <= 90)
            {
                if (npc.localAI[0] == 0)
                {
                    npc.localAI[0] = 1;
                    npc.localAI[2] = (player.Center - npc.Center).ToRotation();
                }
                if (npc.ai[2] >= 40 && npc.localAI[0] == 1)
                {
                    npc.localAI[0] = 2;
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                    float maxspeed = Main.getGoodWorld ? 26 : 19;
                    float chargeSpeed = MathHelper.Lerp(maxspeed, 14, modifier);
                    npc.velocity = chargeSpeed * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                    npc.netUpdate = true;
                }
                if (npc.ai[2] >= 40)
                    npc.rotation = npc.velocity.ToRotation() - 1.57f;
                else
                    RotateTowards(npc, npc.Center + Vector2.UnitX.RotatedBy(npc.localAI[2]), 0.12f);
                if (npc.HasValidTarget && ++npc.ai[3] > 2 && npc.ai[2] >= 40) //cursed flamethrower when dashing
                {
                    npc.ai[3] = 0;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int projtype = ignite ? ModContent.ProjectileType<ShadowFlame>() : ProjectileID.EyeFire;
                        float speed = (1f - 0.5f * modifier) * 0.6f;
                        float rotationVariance = self.Phase <= 1 ? 9f * modifier / 2f : 0;
                        Vector2 spawnPos = ignite ? ShootPos(npc) : npc.Center;
                        float progress = Math.Clamp((npc.ai[2] - 40f) / 20f, 0, 1);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, progress * speed * npc.velocity.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-rotationVariance, rotationVariance))), projtype, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer);
                    }
                }
            }
            if (npc.ai[2] >= 90) // 冲刺结束
            {
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                if (npc.ai[1] >= chaseDuration)
                    ChooseAttack(npc);
            }
            ++npc.ai[1];

        }

        public static void LocatedShoot(NPC npc, Player player)
        {
            int chaseDuration = 360;
            int waittime = 40;
            //npc.dontTakeDamage = false;
            IPTwins self = GetIPTwins(npc);
            Vector2 desired = player.Center - 500 * Vector2.UnitY.RotatedBy(npc.ai[2] * MathHelper.Pi / 6);
            TwinMove(npc, desired, 40, 4f, 4);
            RotateTowards(npc, player.Center);
            if (npc.ai[1] >= waittime)
            {
                int inter = 15;
                if (npc.ai[1] % inter == 0 && npc.ai[1] > waittime)
                {
                    Vector2 vel = -player.SafeDirectionTo(ShootPos(npc));
                    if (FargoSoulsUtil.HostCheck)
                    {
                        for (int i = -2; i <= 2; i++)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                                20 * vel.RotatedBy(i * MathHelper.Pi / 6), ModContent.ProjectileType<DarkStar>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f,
                                Main.myPlayer, ai2: self.OrbColor);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                                30 * vel.RotatedBy(i * MathHelper.Pi / 6), ModContent.ProjectileType<DarkStar>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f,
                                Main.myPlayer, ai2: self.OrbColor);
                            SpawnElectricSpark(npc, vel);
                        }
                        for (float i = 0.7f; i <= 1.6f; i += 0.2f)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                                i * vel.RotatedBy(j * MathHelper.Pi / 3), ModContent.ProjectileType<MechElectricOrbAcc>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f,
                                Main.myPlayer, ai2: self.OrbColor);
                            }
                        }
                    }
                    npc.ai[2]++;
                    npc.velocity -= vel;
                }
            }
            if (++npc.ai[1] >= chaseDuration + waittime + 30)
            {
                ChooseAttack(npc);
            }
        }

        public static void NormalShoot(NPC npc, Player player)
        {
            IPTwins self = GetIPTwins(npc);
            int chaseDuration = self.Phase == 1 ? 320 : 384;
            //npc.dontTakeDamage = false;
            //int flagY = npc.Center.Y > player.Center.Y ? 1 : -1;
            TwinMove(npc, player.Center - 550 * Vector2.UnitY, 9, 0.17f, 1);
            RotateTowards(npc, player.Center);
            npc.ai[2] += 1f;
            float prece = npc.GetLifePercent();
            if (prece < 0.75)
                npc.ai[2] += 1f;
            if (prece < 0.5)
                npc.ai[2] += Main.getGoodWorld ? 1.5f : 1;
            if (prece < 0.25)
                npc.ai[2] += Main.getGoodWorld ? 1.5f : 1;
            if (prece < 0.1)
                npc.ai[2] += 1f;

            if (npc.ai[2] > (self.Ignite ? 100f : 170f))
            {
                npc.ai[2] = 0f;
                npc.ai[3]++;
                Vector2 vel = -player.SafeDirectionTo(ShootPos(npc)) * (Main.getGoodWorld ? 1.1f : 1);
                int laserType = ModContent.ProjectileType<MechElectricOrb>();
                float spreadAngle = 0.5f;
                int spread = Main.getGoodWorld && npc.ai[3] % 2 == 0 ? 1 : 0;
                Vector2 spawnPos = ShootPos(npc);
                if (FargoSoulsUtil.HostCheck)
                {
                    if (self.Ignite)
                    {
                        for (int i = -spread; i <= spread; i++)
                        {
                            Vector2 shotVel2 = 20 * vel.RotatedBy(MathHelper.PiOver2 * spreadAngle * i);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, shotVel2, ModContent.ProjectileType<DarkStar>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, npc.target);
                            SpawnElectricSpark(npc, vel);
                        }
                        //Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, vel, ModContent.ProjectileType<DarkStarTwins>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, npc.target);
                    }
                    else
                    {
                        for (int i = -spread; i <= spread; i++)
                        {
                            Vector2 shotVel2 = 20 * vel.RotatedBy(MathHelper.PiOver2 * spreadAngle * i);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, shotVel2, laserType, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer, npc.target, ai2: MechElectricOrb.Yellow);
                            SpawnElectricSpark(npc, vel);
                        }
                    }
                }
                npc.velocity -= vel;
            }
            if (++npc.ai[1] >= chaseDuration)
                ChooseAttack(npc);
        }

        public static void P1_BreathedFire(NPC npc, Player player)
        {
            //npc.dontTakeDamage = false;
            IPTwins self = GetIPTwins(npc);
            int chaseDuration = 300;
            int flagX = npc.OnRightSideOf(player) ? 1 : -1;
            Vector2 targetPos = player.Center + 180 * flagX * Vector2.UnitX; //+ 240 * player.SafeDirectionTo(npc.Center);
            float speed = 4;
            float acc = 0.22f;
            float omiga = 0.015f;
            float aof = Math.Abs(MathHelper.WrapAngle(npc.rotation + MathHelper.PiOver2 - (player.Center - npc.Center).ToRotation()));
            float distanceToTarget = npc.Distance(targetPos);
            if (distanceToTarget > 300f) speed += 0.5f;
            if (distanceToTarget > 400f) speed += 0.5f;
            if (distanceToTarget > 500f) speed += 0.55f;
            if (distanceToTarget > 600f) speed += 0.55f;
            if (distanceToTarget > 700f) speed += 0.6f;
            if (distanceToTarget > 800f) speed += 0.6f;
            //if (distanceToTarget < 240f) acc += 0.22f;
            if (aof > 0.15f * MathF.PI) omiga += 0.008f;
            if (aof > 0.25f * MathF.PI) omiga += 0.01f;
            if (aof > 0.35f * MathF.PI) omiga += 0.01f;
            if (distanceToTarget < 200f) omiga *= 0.6f;
            TwinMove(npc, targetPos, speed, acc);
            RotateTowards(npc, player.Center, omiga);
            npc.ai[2] += 1f;
            npc.localAI[2] += 1f;
            if (npc.localAI[2] > 22f)
            {
                npc.localAI[2] = 0f;
                SoundEngine.PlaySound(SoundID.Item34, npc.position);
            }

            // 弹幕发射计时器
            if (FargoSoulsUtil.HostCheck && npc.ai[2] > 30)
            {
                float prece = npc.GetLifePercent();
                npc.localAI[1] += 1f;
                // 血量越低，发射速度越快
                if (prece < 0.75) npc.localAI[1] += 1f;
                if (prece < 0.5) npc.localAI[1] += 1f;
                if (prece < 0.25) npc.localAI[1] += 1f;
                if (prece < 0.1) npc.localAI[1] += 2f;

                if (npc.localAI[1] > 4f)
                {
                    npc.localAI[1] = 0f;
                    float projectileSpeed = MathHelper.SmoothStep(0, 14, (npc.ai[2] - 30) / 60f);
                    int projectileDamage = FargoSoulsUtil.ScaledProjectileDamage(npc.damage);
                    int projectileType = ProjectileID.EyeFire; // 魔焰眼诅咒焰
                    Vector2 vel = projectileSpeed * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
                    Vector2 spawnPos = self.Ignite ? ShootPos(npc) : npc.Center;
                    if (self.Ignite)
                    {
                        if (FargoSoulsUtil.HostCheck)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, vel * Main.rand.NextFloat(0.6f, 1f), ModContent.ProjectileType<ShadowFlame>(), projectileDamage, 0f, Main.myPlayer);
                        npc.localAI[1] = 2;
                    }
                    else if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, vel * Main.rand.NextFloat(0.6f, 1f), projectileType, projectileDamage, 0f, Main.myPlayer);
                }
            }
            if (npc.ai[2] >= chaseDuration) // 追逐时间到，切换为冲刺状态
            {
                ChooseAttack(npc);
            }
        }

        public static void P2_BreathedFire(NPC npc, Player player)
        {
            IPTwins self = GetIPTwins(npc);
            int chaseDuration = 300;
            if (npc.ai[2] == 0 && FargoSoulsUtil.HostCheck)
            {
                int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, NPCID.MoonLordCore);
                Main.projectile[p].scale *= 1.5f;
            }
            //int flagX = npc.OnRightSideOf(player) ? 1 : -1;
            Vector2 targetPos = player.Center;// + 600 * player.SafeDirectionTo(npc.Center);
            float speed = 4;
            float acc = 0.22f;
            float omiga = 0.012f;
            float distanceToTarget = npc.Distance(targetPos);
            float aof = Math.Abs(MathHelper.WrapAngle(npc.rotation + MathHelper.PiOver2 - (player.Center - npc.Center).ToRotation()));
            if (distanceToTarget > 300f) speed += 0.5f;
            if (distanceToTarget > 400f) speed += 0.5f;
            if (distanceToTarget > 500f) speed += 0.55f;
            if (distanceToTarget > 600f) speed += 0.55f;
            if (distanceToTarget > 700f) speed += 0.6f;
            if (distanceToTarget > 800f) speed += 0.6f;
            //if (distanceToTarget < 500f) acc += 0.33f;
            if (aof > 0.15f * MathF.PI) omiga += 0.01f;
            if (aof > 0.25f * MathF.PI) omiga += 0.01f;
            if (aof > 0.35f * MathF.PI) omiga += 0.01f;
            if (distanceToTarget < 300f) omiga += 0.004f;
            if (distanceToTarget > 800f) omiga *= 0.6f;
            TwinMove(npc, targetPos, speed, acc);
            RotateTowards(npc, player.Center, omiga);
            npc.ai[2] += 1f;
            npc.localAI[2] += 1f;
            if (npc.localAI[2] > 22f)
            {
                npc.localAI[2] = 0f;
                SoundEngine.PlaySound(SoundID.Item34, npc.position);
            }

            // 弹幕发射计时器
            if (npc.ai[2] > 60)
            {
                float prece = npc.GetLifePercent();
                npc.localAI[1] += 1f;
                if (prece < 0.5) npc.localAI[1] += 0.5f;
                if (prece < 0.25) npc.localAI[1] += 0.5f;
                if (prece < 0.1) npc.localAI[1] += 1f;

                if (npc.localAI[1] > 4f)
                {
                    float projectileSpeed = MathHelper.SmoothStep(0, 30, (npc.ai[2] - 60) / 60f);
                    int projectileDamage = FargoSoulsUtil.ScaledProjectileDamage(npc.damage);
                    Vector2 vel = projectileSpeed * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (self.Ignite)
                        {
                            for (int i = 0; i < 3; i++)
                                Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel * Main.rand.NextFloat(0.6f, 1f), ModContent.ProjectileType<ShadowFlame>(), projectileDamage, 0f, Main.myPlayer);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel, ModContent.ProjectileType<DarkStarAcc>(), projectileDamage, 0f, Main.myPlayer);
                        }
                        else
                        {
                            for (int i = 0; i < 3; i++)
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel * Main.rand.NextFloat(0.6f, 1f), ProjectileID.EyeFire, projectileDamage, 0f, Main.myPlayer);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), vel, ModContent.ProjectileType<MechElectricOrbAcc>(), projectileDamage, 0f, Main.myPlayer, ai2: MechElectricOrb.Green);
                        }
                    }
                }
            }
            if (npc.ai[2] >= chaseDuration) // 追逐时间到，切换为冲刺状态
            {
                ChooseAttack(npc);
            }
        }

        public static void PhaseChange1st(NPC npc)
        {
            npc.TryGetGlobalNPC<P_Retinazer>(out P_Retinazer reti);
            npc.velocity *= 0.98f;
            if (npc.ai[1] == 0)
                npc.dontTakeDamage = true;
            if (npc.velocity.Length() < 0.1f)
                npc.velocity = Vector2.Zero;
            if (npc.ai[1] < 60)
            {
                npc.ai[2] += 0.018f;
                if (npc.ai[2] > 1.08f)
                    npc.ai[2] = 1.08f;

            }
            else if (npc.ai[1] < 120)
            {
                npc.ai[0] = 2f;
                npc.ai[2] -= 0.018f;
                if (npc.ai[2] < 0f)
                    npc.ai[2] = 0f;
                if (npc.ai[1] == 60)
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
                if (npc.ai[1] > 60 && reti != null)
                {
                    float progress = (npc.ai[1] - 60f) / 60f;
                    //reti.AuraRadius = MathHelper.SmoothStep(1, 1500, progress);
                    reti.AuraRadius = 1500 * FargoSoulsUtil.SineInOut(progress);
                }
            }
            npc.rotation += npc.ai[2];
            if (++npc.ai[1] == 120)
            {
                npc.dontTakeDamage = false;
                npc.HitSound = SoundID.NPCHit4;
                ChooseAttack(npc);
                npc.netUpdate = true;
            }
        }
        //
        public static void PhaseChange2nd(NPC npc)
        {
            //IPTwins self = GetIPTwins(npc);
            npc.velocity *= 0.95f;
            if (npc.ai[1] == 0)
                npc.dontTakeDamage = true;
            if (npc.velocity.Length() < 0.1f)
                npc.velocity = Vector2.Zero;
            int whoisbro = npc.type == NPCID.Retinazer ? EModeGlobalNPC.spazBoss : EModeGlobalNPC.retiBoss;
            NPC bro = FargoSoulsUtil.NPCExists(whoisbro, NPCID.Retinazer, NPCID.Spazmatism);
            if (bro != null)
            {
                Vector2 target = bro.Center + bro.SafeDirectionTo(npc.Center) * 100;
                npc.velocity = FargoSoulsUtil.SmartAccel(npc.Center, target, npc.velocity, 0.2f, 0.2f);
            }
            if (npc.ai[1] < 60)
            {
                npc.ai[2] += 0.018f;
                if (npc.ai[2] > 1.08f)
                    npc.ai[2] = 1.08f;

            }
            else if (npc.ai[1] < 120)
            {
                npc.ai[0] = 2f;
                npc.ai[2] -= 0.018f;
                if (npc.ai[2] < 0f)
                    npc.ai[2] = 0f;
                if (npc.ai[1] == 60)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (npc.type == NPCID.Retinazer)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
                        else
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, NPCID.MoonLordCore);
                    }
                    SoundEngine.PlaySound(3, (int)npc.position.X, (int)npc.position.Y);
                    for (int i = 0; i < 20; i++)
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                }
            }
            npc.rotation += npc.ai[2];
            if (++npc.ai[1] >= 120)
            {
                npc.dontTakeDamage = false;
                ChooseAttack(npc);
            }
        }
        //
        public static void PhaseChange3rd(NPC npc)
        {
            if (npc.ai[1] == 0)
                npc.dontTakeDamage = true;
            npc.velocity *= 0.95f;
            if (npc.ai[1] == 30 && FargoSoulsUtil.HostCheck)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsFireBackground>(), 0, 0, Main.myPlayer, npc.whoAmI, npc.type);
            }
            if (npc.ai[1] > 30)
            {
                int heal = (int)(npc.lifeMax / 90 * Main.rand.NextFloat(1f, 1.5f));
                float maxlifepre = npc.type == NPCID.Retinazer ? 0.5f : 0.4f;
                if (npc.life > maxlifepre * npc.lifeMax)
                    npc.life = (int)(maxlifepre * npc.lifeMax);
                else
                    npc.life += heal;
                CombatText.NewText(npc.Hitbox, CombatText.HealLife, heal);
            }
            if (++npc.ai[1] > 90)
            {
                npc.dontTakeDamage = false;
                ChooseAttack(npc);
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                ScreenShakeSystem.StartShake(20f);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsWave>(), 0, 0, Main.myPlayer, npc.type, 0, 20);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsWave>(), 0, 0, Main.myPlayer, npc.type, 0, 16);
                }
            }
        }

        public static void PolyRing(NPC npc, Player player)
        {
            int chaseDuration = 390;
            IPTwins re = GetIPTwins(npc);
            npc.velocity *= 0.80f;
            if (npc.ai[1] == 0 && FargoSoulsUtil.HostCheck)
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
            if (npc.ai[1] % 10 == 0 && npc.ai[1] > 30 && npc.ai[1] < chaseDuration)
            {
                int max = Main.getGoodWorld ? 5 : 4;
                int projType = re.Ignite ? ModContent.ProjectileType<DarkStarPolyline>() : ModContent.ProjectileType<MechElectricOrbPolyline>();
                for (int i = 0; i < max; i++)
                {
                    Vector2 vel = Vector2.UnitX.RotatedBy((i + max * npc.ai[2] / 120f) * MathHelper.TwoPi / max);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc),
                        10 * vel, projType, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1f, Main.myPlayer,
                        0, 1, MechElectricOrb.Yellow);
                        SpawnElectricSpark(npc, vel);
                    }
                }
                npc.ai[2]++;
            }
            if (npc.ai[1] >= chaseDuration)
            {
                RotateTowards(npc, player.Center);
                Vector2 targetPos = player.Center - npc.SafeDirectionTo(player.Center) * 450;
                float dis = npc.Distance(targetPos);
                if (dis > 800)
                    TwinMove(npc, targetPos, 6);
                else if (dis > 400)
                    TwinMove(npc, targetPos, 6, 0.3f);
                else if (dis < 100)
                    TwinMove(npc, targetPos, 6, 0.4f);
            }
            if (++npc.ai[1] > chaseDuration + 30)
            {
                ChooseAttack(npc);
            }
        }
        /*
        public static void CurvedDeathRay(NPC npc, Player player)
        {
            IPTwins self = GetIPTwins(npc);
            npc.velocity *= 0.80f;
            if (npc.ai[1] == 0)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
                float num = 4;
                for (int i = 0; i < num; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), ShootPos(npc), Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / num), 
                        ModContent.ProjectileType<TwinCurvedLaser>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0, Main.myPlayer,
                        npc.whoAmI);
                }
            }
            if (++npc.ai[1] > 400)
                ChooseAttack(npc);
        }
        */
        /*
        public static void FlamesSlash(NPC npc, Player player)
        {
            IPTwins self = GetIPTwins(npc);
            npc.velocity *= 0.8f;
            if (npc.ai[1] == 0)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, NPCID.MoonLordCore);
            }
            float progress = npc.ai[1] / 120f;
            float speed = MathHelper.SmoothStep(0, 100, progress);
            Vector2 vel = speed * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
            Vector2 spawnPos = self.Ignite ? ShootPos(npc) : npc.Center;
            int projType = self.Ignite ? ModContent.ProjectileType<ShadowFlame>() : ProjectileID.EyeFire;
            for (int i = 0; i < 10; i++)
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, vel * Main.rand.NextFloat(0.6f, 1f), projType, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer);
            if (npc.ai[1] >= 120)
            {
                if (npc.ai[1] == 120)
                {
                    npc.localAI[0] = Math.Sign(vel.AngleDifference(player.Center - npc.Center));
                    if (npc.localAI[0] == 0)
                        npc.localAI[0] = 1;
                    npc.localAI[1] = npc.rotation;
                }
                float progress2 = (npc.ai[1] - 120f) / 60f;
                float angle = 2 * MathF.PI / 3f;
                npc.rotation = npc.localAI[1] + npc.localAI[0] * angle * (float)Math.Pow(progress2, 3f);
            }
            if (++npc.ai[1] > 180)
                ChooseAttack(npc);
        }
        */
        public static void RollingShoot(NPC npc)
        {
            NPC bro = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            if (bro == null)
            {
                ChooseAttack(npc);
                return;
            } 
            Vector2 target = bro.Center + bro.SafeDirectionTo(npc.Center) * 100;
            npc.velocity = (target - npc.Center) / 60f;

            int FlameWheelCount = 3; 

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
                npc.ai[2] += 0;

                if (timeLeft > 5 && FargoSoulsUtil.HostCheck)
                {
                    IPTwins pTwins = GetIPTwins(npc);
                    int projType = pTwins.Ignite ? ModContent.ProjectileType<DarkStar>() : ModContent.ProjectileType<MechElectricOrb>();
                    
                    for (int i = 0; i < FlameWheelCount; i++)
                    {
                        Projectile p = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center,
                            (baseRotation + MathHelper.TwoPi / FlameWheelCount * i).ToRotationVector2() * speed, projType,
                            FargoSoulsUtil.ScaledProjectileDamage(npc.damage),
                            0, Main.myPlayer, ai0: 80f,  ai2: pTwins.OrbColor);
                        if (p.active)
                        {
                            p.timeLeft = timeLeft;
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
        #endregion
        #region 废弃AI方法
        /*
        public static void P1_FireDash(NPC npc, Player player)
        {
            npc.TryGetGlobalNPC<P_Spazmatism>(out P_Spazmatism self);
            bool ignite = self.Ignite;
            int chaseDuration = 360;
            float modifier = npc.GetLifePercent() * npc.GetLifePercent();
            npc.ai[2] += ignite ? 1.6f : 1;
            if (npc.ai[2] <= 30)
            {
                npc.ai[2] -= modifier / 1.5f;
                npc.ai[2] -= 0.05f;
                RotateTowards(npc, player.Center, 0.12f);
                if (npc.ai[2] <= 20)
                    npc.velocity *= 0.94f;
                if (npc.velocity.Length() < 0.1f)
                    npc.velocity = Vector2.Zero;
            }
            else if (npc.ai[2] <= 80)
            {
                if (npc.localAI[0] == 0)
                {
                    npc.localAI[0] = 1;
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                    float chargeSpeed = MathHelper.Lerp(ignite ? 20 : 22, ignite ? 14 : 17, modifier);
                    npc.velocity = chargeSpeed * npc.SafeDirectionTo(player.Center);
                }
                npc.rotation = npc.velocity.ToRotation() - 1.57f;
                if (npc.HasValidTarget && ++npc.ai[3] > 2) //cursed flamethrower when dashing
                {
                    npc.ai[3] = 0;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (ignite)
                        {
                            float speed = (1f - 0.5f * modifier) * 1.2f;
                            float rotationVariance = 9f * modifier / 1.5f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed * npc.velocity.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-rotationVariance, rotationVariance))), ModContent.ProjectileType<ShadowFlame>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0f, Main.myPlayer);
                        }
                        else
                        {
                            float dashTime = 50f;
                            float extension = MathF.Sin(MathF.PI * (npc.ai[2] - 30) / dashTime);
                            if (extension < 0)
                                extension = 0;
                            float speed = extension * 0.55f;
                            float rotationVariance = 9f * extension * 0.75f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed * npc.velocity.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-rotationVariance, rotationVariance))), ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                        }
                    }
                }
                if (npc.HasValidTarget && !ignite)
                {
                    Vector2 toTarget = npc.DirectionTo(Main.player[npc.target].Center);
                    npc.velocity += toTarget * 0.28f;
                    npc.velocity = npc.velocity.RotateTowards(toTarget.ToRotation(), 0.007f);
                }
            }
            if (npc.ai[2] >= 80) // 冲刺结束
            {
                npc.ai[2] = ignite ? 10 : 0;
                npc.localAI[0] = 0;
                if (npc.ai[1] >= chaseDuration)
                    ChooseAttack(npc);
            }
            ++npc.ai[1];
        }
        */
        /*
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
        */
        #endregion

        #region 辅助方法
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

        public static bool Checkdead(NPC npc)
        {
            bool isRe = npc.type == NPCID.Retinazer;
            NPC bro = FargoSoulsUtil.NPCExists(isRe ? EModeGlobalNPC.spazBoss : EModeGlobalNPC.retiBoss, isRe ? NPCID.Spazmatism : NPCID.Retinazer);
            // Main.NewText(bro == null);
            if (bro == null)
                return true;
            IPTwins Ibro = GetIPTwins(bro);
            IPTwins self = GetIPTwins(npc);
            if (self.Ghost)
            {
                npc.netUpdate = bro.netUpdate = true;
                return true;
            }
            if (Ibro.Ghost)
            {
                if (!Main.getGoodWorld)
                    bro.dontTakeDamage = false;
                else
                {
                    if (Ibro.AIState == TwinsAtt.RollingShoot || Ibro.AIState == TwinsAtt.CurvedDeathRay)
                    {
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            Projectile proj = Main.projectile[i];
                            if (proj.type == ModContent.ProjectileType<TwinCurvedLaser>() && proj.active)
                                proj.Kill();
                        }
                    }
                    Ibro.Ghost = false;
                    Ibro.AIState = TwinsAtt.PhaseChange3rd;
                    Ibro.Phase = 3;
                    Ibro.Phaseinit = 0;
                    bro.ai[1] = bro.ai[2] = bro.ai[3] = bro.localAI[0] = bro.localAI[1] = bro.localAI[2] = bro.localAI[3] = 0;
                }
                npc.netUpdate = bro.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                if (bro.netSpam > 10)
                    bro.netSpam = 10;
                return true;
            }

            npc.AddBuff(BuffID.CursedInferno, 999999);
            npc.AddBuff(BuffID.Ichor, 999999);
            bro.AddBuff(BuffID.CursedInferno, 999999);
            bro.AddBuff(BuffID.Ichor, 999999);
            if (true)
            {
                self.Ghost = true;
                npc.dontTakeDamage = true;
                npc.life = 1;
                self.Ignite = false;
                self.IgniteTimer += 9999999;
                FargoSoulsUtil.PrintLocalization($"Mods.FargowiltasSouls.NPCs.EMode.TwinsEndure", new Color(175, 75, 255), npc.FullName);
                npc.netUpdate = true;
            }

            Ibro.Ignite = false;
            Ibro.IgniteTimer += 9999999;
            /*
            if (Main.getGoodWorld)
            {
                if (Ibro.Phase <= 2 && self.Phase <= 2)
                {
                    npc.ai[1] = npc.ai[2] = npc.ai[3] = npc.localAI[0] = npc.localAI[1] = npc.localAI[2] = npc.localAI[3] = 0;
                    bro.ai[1] = bro.ai[2] = bro.ai[3] = bro.localAI[0] = bro.localAI[1] = bro.localAI[2] = bro.localAI[3] = 0;
                    self.Phase = 3;
                    self.AIState = TwinsAtt.PhaseChange3rd;
                    Ibro.Phase = 3;
                    Ibro.AIState = TwinsAtt.PhaseChange3rd;
                    //Ibro.IgniteTimer += 9999999;
                    //bro.life = 1;
                    npc.dontTakeDamage = true;
                    bro.dontTakeDamage = true;
                }
            }
            */
            npc.netUpdate = bro.netUpdate = true;
            if (npc.netSpam > 10)
                npc.netSpam = 10;
            if (bro.netSpam > 10)
                bro.netSpam = 10;
            return false;
        }

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

        public static void ManageIgnite(NPC npc)
        {
            IPTwins self = GetIPTwins(npc);
            int min = (int)MathHelper.Lerp(400, 560, npc.GetLifePercent());
            int max = (int)MathHelper.Lerp(720, 960, npc.GetLifePercent());
            if (self.Ignite == false)
            {
                if (self.IgniteTimer > max || (self.IgniteTimer >= min && (self.IgniteTimer - min) % 30 == 0 && Main.rand.NextBool(3)))
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (npc.type == NPCID.Retinazer)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
                        else
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, NPCID.MoonLordCore);
                    }
                    
                    self.Ignite = true;
                    self.IgniteTimer = Math.Max(max, self.IgniteTimer);
                    npc.netUpdate = true;
                    //if (self.Phase <= 1)
                    //npc.ai[2] = -(int)MathHelper.Lerp(0, 40, npc.GetLifePercent() * npc.GetLifePercent());
                }
                self.IgniteTimer++;
            }
            else
            {
                if (self.IgniteTimer <= 0 && self.Phase <= 2)
                {
                    self.Ignite = false;
                    npc.netUpdate = true;
                }
                self.IgniteTimer--;
                for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, npc.type == NPCID.Retinazer ? DustID.GemTopaz : DustID.GemEmerald, 0f, 0f, 0, default, 1.8f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 5f;
                }
                //npc.AddBuff(BuffID.Ichor, 2);
                //npc.AddBuff(BuffID.CursedInferno, 2);
            }
        }

        public static bool PhaseCheck(NPC npc, NPC bro)
        {
            GetTwins(npc, bro, out IPTwins Reti, out IPTwins Spaz);
            if ((npc.life < npc.lifeMax * 0.70f || bro.life < bro.lifeMax * 0.70f) && Reti.Phase == 1 && Spaz.Phase == 1)
            {
                Reti.Phase = Spaz.Phase = 2;
                Reti.AIState = Spaz.AIState = TwinsAtt.PhaseChange2nd;
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
        public static void RotateTowards(NPC npc, Vector2 targetPos, float turnSpeed = 0.18f) => P_EyeOfCthulhu.RotateTowards(npc, targetPos, turnSpeed);
        public static Vector2 ShootPos(NPC npc) => npc.Center + (npc.width - 24) * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);

        public static void SpawnElectricSpark(NPC npc, Vector2 vel)
        {
            if (FargoSoulsUtil.HostCheck)
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
        }

        public static bool TryWatchHarmlessly(NPC npc)
        {
            if (npc.HasValidTarget)
            {
                const float PI = (float)Math.PI;
                if (npc.rotation > PI)
                    npc.rotation -= 2 * PI;
                if (npc.rotation < -PI)
                    npc.rotation += 2 * PI;

                float targetRotation = npc.SafeDirectionTo(Main.player[npc.target].Center).ToRotation() - PI / 2;
                if (targetRotation > PI)
                    targetRotation -= 2 * PI;
                if (targetRotation < -PI)
                    targetRotation += 2 * PI;
                npc.rotation = MathHelper.Lerp(npc.rotation, targetRotation, 0.07f);

                return false;
            }
            return true;
        }

        public static void TwinMove(NPC npc, Vector2 targetPos, float speed = 12.5f, float accel = 0.22f, float decelMult = 2f)
        {
            Vector2 target = speed * npc.SafeDirectionTo(targetPos);
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
        public void ManangeAura(NPC npc)
        {
            if (AIState != TwinsAtt.PhaseChange1st)
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

        private void ManageAuraRadius(NPC npc)
        {
            if (AIState != TwinsAtt.PhaseChange1st)
            {
                if ((AIState == TwinsAtt.CurvedDeathRay && npc.ai[1] <= 390) || (AIState == TwinsAtt.Final_Deathray && npc.ai[1] <= 390))
                {
                    float minR = Main.getGoodWorld ? 800: 900;
                    AuraRadius -= 4;
                    if (AuraRadius < minR)
                        AuraRadius = minR;
                }
                else
                {
                    AuraRadius += 3;
                    if (AuraRadius > 1500)
                        AuraRadius = 1500;
                }
            }
        }
        #endregion
        #region 重写方法
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (AIState == TwinsAtt.LocatedShoot)
                return false;
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (AIState == TwinsAtt.CurvedDeathRay)
                modifiers.FinalDamage /= 2;
            base.ModifyIncomingHit(npc, ref modifiers);
        }

        public override bool CheckDead(NPC npc) => Checkdead(npc);

        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            //if (!Ignite)
            return base.GetAlpha(npc, drawColor);
            //return new Color(255, drawColor.G / 2, drawColor.B / 2);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);
            AuraRadius = binaryReader.ReadSingle();
            AuraOpacity = binaryReader.ReadSingle();
            Phaseinit = binaryReader.Read7BitEncodedInt();
            Phase = binaryReader.Read7BitEncodedInt();
            AIState = (TwinsAtt)binaryReader.Read7BitEncodedInt();
            Ignite = bitReader.ReadBit();
            IgniteTimer = binaryReader.Read7BitEncodedInt();
            Ghost = bitReader.ReadBit();
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);
            binaryWriter.Write(AuraRadius);
            binaryWriter.Write(AuraOpacity);
            binaryWriter.Write7BitEncodedInt(Phaseinit);
            binaryWriter.Write7BitEncodedInt(Phase);
            binaryWriter.Write7BitEncodedInt((int)AIState);
            bitWriter.WriteBit(Ignite);
            binaryWriter.Write7BitEncodedInt(IgniteTimer);
            bitWriter.WriteBit(Ghost);
        }
        #endregion
        #region 绘制
        public void DrawAura(NPC npc, SpriteBatch spriteBatch, Vector2 position)
        {
            if (AuraOpacity < 1f)
                AuraOpacity += 0.01f;
            if (!Main.dedServ)
            {
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
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 AuraPosition = npc.Center;
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
        #endregion
    }
    /// <summary>
    /// 魔焰眼
    /// </summary>
    public class P_Spazmatism : PModeNPCBehaviour, IPTwins
    {
        #region 不常修改
        public TwinsAtt AIState { get; set; }

        public bool Ghost { get; set; } = false;

        public bool Ignite { get; set; } = false;

        public int IgniteTimer { get; set; } = 0;

        public int OrbColor => MechElectricOrb.Green;

        public int Phase { get; set; } = 1;

        public int Phaseinit { get; set; } = 1;

        public override int NPCType => NPCID.Spazmatism;
        public override bool SafePreAI(NPC npc)
        {
            EModeGlobalNPC.spazBoss = npc.whoAmI;
            NPC bro = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            if (bro != null)
            {
                P_Retinazer Reti = bro.GetGlobalNPC<P_Retinazer>();
                Phase = Reti.Phase;
                PhaseCheck(npc, bro);
            }
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                if (bro != null)
                    npc.target = bro.target;
                else
                    npc.TargetClosest();
            }

            Player player = Main.player[npc.target];

            if (!AliveCheck(npc, bro, player))
                return false;
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
            PHTwinsAI(npc, player);
            return false;
        }

        public override void SetDefaults(NPC npc)
        {
        }
        public override void StopEmodeAI(NPC npc)
        {
            npc.GetGlobalNPC<Spazmatism>().RunEmodeAI = false;
        }
        #endregion
        public static readonly List<TwinsAtt> phase1 = [
            TwinsAtt.CurFireDash,
            TwinsAtt.P1_BreathedFire,
            TwinsAtt.LegFireDash,
            TwinsAtt.P1_BreathedFire,
            ];
        
        public static readonly List<TwinsAtt> phase2 = [
            TwinsAtt.CurFireDash,
            TwinsAtt.LegFireDash,
            TwinsAtt.CurFireDash,
            TwinsAtt.P2_BreathedFire,
            TwinsAtt.LegFireDash,
            TwinsAtt.CurFireDash,
            TwinsAtt.LegFireDash,
            TwinsAtt.P2_BreathedFire,
            //TwinsAtt.FlamesSlash,
            ];
        public static readonly List<TwinsAtt> phase3 = [
            TwinsAtt.LocatedShoot,
            TwinsAtt.FireRotate,
            TwinsAtt.Final_LegFireDash,
            TwinsAtt.Final_CurFireDashBreathed,
            TwinsAtt.Final_Embers,
            ];
        public List<TwinsAtt> Phase1 => phase1;
        public List<TwinsAtt> Phase2 => phase2;
        public List<TwinsAtt> Phase3 => phase3;
        #region AI方法

        #endregion
        #region 辅助方法
        public static bool AliveCheck(NPC npc, NPC bro, Player player)
        {
            bool length = Vector2.Distance(npc.Center, player.Center) > 5000f && bro == null;
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
        #endregion
        #region 重写方法
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (AIState == TwinsAtt.LocatedShoot || AIState == TwinsAtt.Final_CurFireDashBreathed)
                return false;
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (AIState == TwinsAtt.RollingShoot)
                modifiers.FinalDamage /= 2;
            base.ModifyIncomingHit(npc, ref modifiers);
        }

        public override bool CheckDead(NPC npc) => Checkdead(npc);

        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            //if (!Ignite)
            return base.GetAlpha(npc, drawColor);
            //return new Color(drawColor.R / 2, 255, drawColor.B / 2);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);
            Phaseinit = binaryReader.Read7BitEncodedInt();
            Phase = binaryReader.Read7BitEncodedInt();
            AIState = (TwinsAtt)binaryReader.Read7BitEncodedInt();
            Ignite = bitReader.ReadBit();
            IgniteTimer = binaryReader.Read7BitEncodedInt();
            Ghost = bitReader.ReadBit();
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);
            binaryWriter.Write7BitEncodedInt(Phaseinit);
            binaryWriter.Write7BitEncodedInt(Phase);
            binaryWriter.Write7BitEncodedInt((int)AIState);
            bitWriter.WriteBit(Ignite);
            binaryWriter.Write7BitEncodedInt(IgniteTimer);
            bitWriter.WriteBit(Ghost);
        }
        #endregion
    }
}
