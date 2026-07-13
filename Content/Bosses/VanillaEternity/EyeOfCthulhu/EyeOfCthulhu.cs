using FargosPhantasmMode.Content.Bossbar;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Bosses.Champions.Will;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    /// <summary>
    /// AIState控制状态机，ai[0]与原版形态联系，只在转阶段使用,ai[1]计时器,ai[2]可当参数,ai[3] 为阶段数,
    /// other为可用参数, ///localAI[3]作为特殊弹幕显隐标志(1为隐，0为显)
    /// </summary>
    public class P_EyeOfCthulhu : PModeNPCBehaviour
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.EyeofCthulhu);

        public bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
        public bool DroppedSummon;
        public int TeleportDirection;
        public float AIState = 0;
        public int DeathTimer = -1;
        public int LastAIState = 0;
        public int Last2AIState = 0;
        public int HyperTime = 0;
        public int P3AttackChange = 0;
        public override void SetDefaults(NPC npc)
        {
            npc.BossBar = ModContent.GetInstance<PhantasmBossBar>();
        }
        public override void StopEmodeAI(NPC npc)
        {
            npc.GetGlobalNPC<EyeofCthulhu>().RunEmodeAI = false;
        }
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (npc.alpha > 120)
                return false;
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }
        
        public override bool SafePreAI(NPC npc)
        {
            EModeGlobalNPC.eyeBoss = npc.whoAmI;
            
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            //ftw特性(祛华):根据某个神秘参数加速boss和boss弹幕更新
            if (Main.getGoodWorld)
            {
                Color light = Lighting.GetColor(npc.Center.ToTileCoordinates());
                float modifier = (light.R + light.G + light.B) / 765f;
                modifier *= (light.R + light.G + light.B) / 765f;
                modifier *= 1 - npc.alpha / 255;
                modifier *= 1 - npc.alpha / 255;
                modifier *= 0.5f * Math.Abs(Main.moonPhase - 4f) / 4f + 0.5f;
                //Main.NewText(modifier);
                bool RestrictedLight = modifier < 0.5f;
                //if (RestrictedLight)
                    //Main.NewText("Hyper");
                int[] EoCProj = [
                    ModContent.ProjectileType<BloodScythe>(),
                    ModContent.ProjectileType<MoonScythe>(),
                    ModContent.ProjectileType<FalseEoC>(),
                    ModContent.ProjectileType<MoonBolt>(),
                    ModContent.ProjectileType<MoonlightTrail>(),
                    ModContent.ProjectileType<EoCTpTelegraph>(),
                    ModContent.ProjectileType<SuperEoCTpTelegraph>()
                ];
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = FargoSoulsUtil.ProjectileExists(i, EoCProj);
                    if (proj != null)
                    {
                        if (RestrictedLight && HyperTime == 4)
                            proj.extraUpdates = 1;
                        else
                            proj.extraUpdates = 0;
                    }
                }
                if (RestrictedLight)
                    npc.Center += 0.2f * npc.velocity;
                if (++HyperTime >= 5)
                {
                    HyperTime = 0;
                    if (RestrictedLight)
                    {
                        SafePreAI(npc);
                    }
                }
            }
            
            if (npc.alpha > 50 && !Main.getGoodWorld)
                Lighting.AddLight(npc.Center, 0.75f, 1.35f, 1.5f);
            npc.dontTakeDamage = npc.alpha > 100;
            Player player = Main.player[npc.target];
            if (npc.ai[3] == 3)
                npc.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 5);
            //死亡动画
            /*
            if (DeathTimer >= 0)
            {
                DeathAnimation(npc, player);
                if (++DeathTimer >= 180) // 300帧后真正死亡
                {

                    npc.life = 0;
                    npc.dontTakeDamage = false;
                    npc.checkDead();
                }
            }
            else
            {
                PHEyeofCthulhuAI(npc, player);
            }
            */
            PHEyeofCthulhuAI(npc, player);
            return false;
        }
        //状态机
        public void PHEyeofCthulhuAI(NPC npc, Player player)
        {
            if (!AliveCheck(npc, player))
                return;
            PhaseCheck(npc);
            
            switch (AIState)
            {
                case -3: PhaseChange3rd(npc, player); break;
                case -2: PhaseChange2nd(npc); break;
                case -1: PhaseChange1st(npc); break;
                //P1
                case 0: FourCornersWait(npc, player); break;
                case 1: NormalDash(npc, player); break;
                case 2: MoonShoot(npc, player); break;
                //P2
                case 3: Phase2Wait(npc, player); break;
                case 4: NormalFastDash(npc, player); break;
                case 5: NormalTpDash(npc, player); break;
                //P3
                case 6: Phase2Wait(npc, player); break;
                case 7: P3FastDash(npc, player); break;

                case 8: FastTpDashs(npc, player); break;
                case 9: Restraint_Triangle(npc, player); break;
                case 10: Restraint_Square(npc, player); break;
                case 11: Restraint_Hexagon(npc, player); break;
                case 12: Restraint_Octagonal(npc, player); break;
                case 13: Restraight_Round(npc, player); break;
                case 14:FaintVisible_FourRowsScythe(npc, player); break;
                case 15: FaintVisible_RoundScythe(npc, player); break;


                case 24: ChooseNextAttack(npc); break;
                case 25: SuperTpDash(npc, player); break;
                case 26: P3MoonShootDash(npc, player); break;
            }

            EModeUtils.DropSummon(npc, "SuspiciousEye", NPC.downedBoss1, ref DroppedSummon);
        }
        #region AI方法
        private void PhaseChange3rd(NPC npc, Player player)//P3转P4
        {
            //f (npc.ai[1] == 0)
                //npc.velocity = 6 * Main.rand.NextVector2Unit();
            npc.velocity *= 0.98f;
            npc.alpha += 4;
            npc.dontTakeDamage = true;
            RotateTowards(npc, player.Center, 0.08f);
            /*
            for (int i = 0; i < 8; i++)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }
            */
            if (npc.alpha > 255)
            {
                npc.alpha = 255;
            }
            if (++npc.ai[1] >= 60)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.HasValidTarget ? Main.player[npc.target].Center : npc.Center);
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
                ChooseNext(npc);
                npc.defense -= 30;
                npc.dontTakeDamage = false;
                //npc.alpha = 0;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.localAI[2] = 0;
                npc.localAI[3] = 0;
                npc.netUpdate = true;
            }
        }
        private void PhaseChange2nd(NPC npc)//P2转P3
        {
            npc.velocity *= 0.96f;
            npc.dontTakeDamage = true;
            if (npc.velocity.Length() < 0.1f)
                npc.velocity = Vector2.Zero;
            if (npc.ai[1] < 60)
            {
                npc.ai[2] += 0.012f;
                if (npc.ai[2] > 0.72f)
                    npc.ai[2] = 0.72f;
            }
            else if (npc.ai[1] < 120)
            {
                npc.ai[0] = 2f;
                npc.ai[2] -= 0.012f;
                if (npc.ai[2] < 0f)
                    npc.ai[2] = 0f;
                if (npc.ai[1] == 60)
                {
                    SoundEngine.PlaySound(3, (int)npc.position.X, (int)npc.position.Y);
                    for (int i = 0; i < 20; i++)
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Vortex, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                }
            }
            npc.rotation += npc.ai[2];
            if (++npc.ai[1] == 120)
            {
                npc.dontTakeDamage = false;
                AIState = 25;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.netUpdate = true;
            }
        }
        private void PhaseChange1st(NPC npc)//P1转P2
        {
            npc.velocity *= 0.96f;
            npc.dontTakeDamage = true;
            if (npc.velocity.Length() < 0.1f)
                npc.velocity = Vector2.Zero;
            if (npc.ai[1] < 60)
            {
                npc.ai[2] += 0.012f;
                if (npc.ai[2] > 0.72f)
                    npc.ai[2] = 0.72f;
            }
            else if (npc.ai[1] < 120)
            {
                npc.ai[0] = 2f;
                npc.ai[2] -= 0.012f;
                if (npc.ai[2] < 0f)
                    npc.ai[2] = 0f;
                if (npc.ai[1] == 60)
                {
                    SoundEngine.PlaySound(3, (int)npc.position.X, (int)npc.position.Y);
                    for (int i = 0; i < 2; i++)
                    {
                        Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 8);
                        Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 7);
                        Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 6);
                    }
                    for (int i = 0; i < 20; i++)
                        Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                }
            }
            npc.rotation += npc.ai[2];
            if (++npc.ai[1] == 120)
            {
                npc.dontTakeDamage = false;
                AIState = 3;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.netUpdate = true;
            }
        }
        private void FourCornersWait(NPC npc, Player player)//四角等待+射月镰
        {
            int flagX = Math.Sign(npc.Center.X - player.Center.X);
            int flagY = Math.Sign(npc.Center.Y - player.Center.Y);

            Vector2 direct = npc.SafeDirectionTo(player.Center);
            RotateTowards(npc, player.Center, 0.03f);
            Vector2 targetCenter = player.Center + 300 * flagY * Vector2.UnitY + flagX * 300 * Vector2.UnitX;
            bool up = (targetCenter - npc.Center).Length() > 800;
            float speed = up ? 15f : 7.5f;
            float accel = up ? 0.36f : 0.18f;
            Movement(npc, targetCenter, speed, accel);
            if (npc.ai[1] % 60 == 0)
            {
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.ServantofCthulhu);
                for (float i = 1; i < 5; i += 1.5f)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, i * direct, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
                npc.netUpdate = true;
            }
            if (++npc.ai[1] > 180)
            {
                AIState += 1f;
                npc.ai[1] = 0;
                npc.netUpdate = true;
            }
        }
        private void NormalDash(NPC npc, Player player)//常态三连冲
        {
            Vector2 direction = npc.SafeDirectionTo(player.Center);
            if (npc.ai[1] == 0)
            {
                npc.rotation = direction.ToRotation() - MathHelper.PiOver2;
                float chargeSpeed = 12f;
                npc.velocity = chargeSpeed * direction * (0.4f * npc.ai[2] + 1f);
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 60);
            }
            if (npc.ai[1] > 40)
            {
                RotateTowards(npc, player.Center, 0.08f);
                npc.velocity *= 0.9556f;

                if (Math.Abs(npc.velocity.X) < 0.1)
                    npc.velocity.X = 0f;
                if (Math.Abs(npc.velocity.Y) < 0.1)
                    npc.velocity.Y = 0f;
            }
            else // 冲刺方向跟随速度
            {
                Vector2 vel = 1.5f * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
                if (npc.ai[1] <= 70 && npc.ai[1] % 8 == 0)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
            }
            if (++npc.ai[1] > 85)
            {
                npc.ai[2] += 1f;
                npc.ai[1] = 0f;
                if (npc.ai[2] >= 3)
                {
                    AIState = 2f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                }
            }
        }
        private void MoonShoot(NPC npc, Player player)//眼状散射月矢
        {
            if (npc.ai[1] < 60)
            {
                npc.velocity *= 0.96f;
                RotateTowards(npc, player.Center, 0.08f);
                FancyFireballs(npc, (int)npc.ai[1]);
            }
            else
            {
                float i = npc.ai[1] - 60f;
                for (float j = -1; j <= 1; j += 2)
                {
                    double angle = i * MathHelper.TwoPi / 20 * j;
                    Vector2 EllipseVel = new (150f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 300f * (float)Math.Sin(angle));
                    EllipseVel *= (j + 2f) / 2f;
                    Vector2 vel = EllipseVel.RotatedBy(npc.rotation + MathHelper.PiOver2) / 10f;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                }
            }
            if (++npc.ai[1] > 90)
            {
                AIState = 0;
                npc.ai[1] = 0;
            }
        }
        private void Phase2Wait(NPC npc, Player player)//P2P3挂机
        {
            if (npc.alpha > 0)
                npc.alpha -= 3;
            if (npc.alpha < 0)
                npc.alpha = 0;
            for (int i = 0; i < 3; i++)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }
            float speed = 6f;
            float acceleration = 0.07f;
            //Vector2 target = npc.SafeDirectionTo(player.Center - 120f * Vector2.UnitY);
            float distance = (player.Center - 120f * Vector2.UnitY - npc.Center).Length();
            //对速度加速度的修正
            if (distance > 400f)
            {
                speed += 2f;
                acceleration += 0.1f;
                if (distance > 600f)
                {
                    speed += 2f;
                    acceleration += 0.1f;
                    if (distance > 800f)
                    {
                        speed += 1f;
                        acceleration += 0.05f;
                        if (distance > 1200)
                        {
                            speed += 5;
                            acceleration += 0.2f;
                        }
                    }
                }
            }
            if (Main.getGoodWorld)
            {
                speed += 1f;
                acceleration += 0.1f;
            }
            Movement(npc, player.Center - 12 * Vector2.UnitY, speed, acceleration);
            RotateTowards(npc, player.Center, 0.18f);
            if (++npc.ai[1] > 90)
            {
                if (npc.ai[3] >= 2)
                {
                    if (P3AttackChange % 2 == 0)
                        AIState = 7;
                    else
                        AIState = 26;
                }
                else
                    AIState += 1;
                P3AttackChange += 1;
                npc.ai[1] = 0;
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
            }
        }
        private void NormalFastDash(NPC npc, Player player)//P2高速冲刺+ 环形月矢镰刀
        {
            if (npc.ai[1] == 0)
            {
                float predictDistance = 20f;
                Vector2 targetDelta = player.Center - npc.Center;
                // 根据玩家速度预测提前量
                float playerSpeedFactor = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y) / 4f;

                float predictionMultiplier = 10f - playerSpeedFactor;
                if (predictionMultiplier < 5f)
                    predictionMultiplier = 5f;
                if (predictionMultiplier > 15f)
                    predictionMultiplier = 15f;

                predictionMultiplier *= 4f;
                predictDistance *= 1.3f;

                targetDelta.X -= player.velocity.X * predictionMultiplier;
                targetDelta.Y -= player.velocity.Y * predictionMultiplier / 4f;

                // 添加随机误差
                targetDelta.X *= 1f + Main.rand.Next(-10, 11) * 0.01f;
                targetDelta.Y *= 1f + Main.rand.Next(-10, 11) * 0.01f;

                float dirLength = targetDelta.Length();
                float originalDirLength = dirLength;
                dirLength = predictDistance / dirLength;
                npc.velocity.X = targetDelta.X * dirLength;
                npc.velocity.Y = targetDelta.Y * dirLength;
                // 添加随机偏移
                npc.velocity.X += Main.rand.Next(-20, 21) * 0.1f;
                npc.velocity.Y += Main.rand.Next(-20, 21) * 0.1f;
                if (originalDirLength < 100f) // 距离较近时交换 X/Y 方向以避免太直
                {
                    if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                    {
                        float x = Math.Abs(npc.velocity.X);
                        float y = Math.Abs(npc.velocity.Y);
                        if (npc.Center.X > player.Center.X)
                            y *= -1f;
                        if (npc.Center.Y > player.Center.Y)
                            x *= -1f;
                        npc.velocity.X = y;
                        npc.velocity.Y = x;
                    }
                }
                else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y)) // 较远时求平均调整
                {
                    float avg = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                    float x = avg;
                    float y = avg;
                    if (npc.Center.X > player.Center.X)
                        y *= -1f;
                    if (npc.Center.Y > player.Center.Y)
                        x *= -1f;
                    npc.velocity.X = y;
                    npc.velocity.Y = x;
                }
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                if (npc.ai[2] == 0)
                    npc.localAI[0] = 6 + Main.rand.Next(1, 4);//次数
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                FargoSoulsUtil.XWay(8, npc.GetSource_FromThis(), npc.Center, ModContent.ProjectileType<BloodScythe>(), 1.5f, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0);
                for (int i = 0; i < 8; i++)
                {
                    for (float j = -1; j <= 1; j += 2)
                    {
                        double angle = i * MathHelper.TwoPi / 8 * j;
                        Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                        EllipseVel *= (j + 2f) / 2f;
                        Vector2 vel = EllipseVel / 15f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                    }
                }
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
            }
            if (npc.ai[1] == 20 && Vector2.Distance(npc.position, player.position) < 200f)
                npc.ai[1] -= 1f;
            if (npc.ai[1] > 20)
            {
                npc.velocity *= 0.95f;
                if (Math.Abs(npc.velocity.X) < 0.1)
                    npc.velocity.X = 0f;
                if (Math.Abs(npc.velocity.Y) < 0.1)
                    npc.velocity.Y = 0f;
                RotateTowards(npc, player.Center, 0.22f);
            }
            if (++npc.ai[1] > 33)
            {
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                npc.ai[2]++;
                npc.ai[1] = 0;
                if (npc.ai[2] >= npc.localAI[0])
                {
                    AIState += 1;
                    npc.ai[1] = 0;
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.netUpdate = true;
                }
            }
        }
        private void NormalTpDash(NPC npc, Player player)//tp冲刺
        {
            if (npc.ai[1] == 0)
            {
                Vector2 distance = Main.player[npc.target].Center - npc.Center;
                if (distance.X == 0) 
                    distance.X = 1;
                const int Xmax = 700; 
                const int Xmin = 550; 
                if (Math.Abs(distance.X) > Xmax)
                    distance.X = Xmax * Math.Sign(distance.X);
                else if (Math.Abs(distance.X) < Xmin)
                    distance.X = Xmin * Math.Sign(distance.X);

                TeleportDirection = Main.rand.NextBool() ? 1 : -1;
                if (TeleportDirection == 0)
                    TeleportDirection = Main.rand.NextBool() ? 1 : -1; //first dash picks side towards player

                distance.X = Math.Abs(distance.X) * TeleportDirection;

                if (distance.Y > 0) //ensure to teleport above
                    distance.Y *= -1;

                const int Ymax = 400; // 1.6.1 note: was 450 before
                const int Ymin = 150; // 1.6.1 note: was 150 before
                if (Math.Abs(distance.Y) > Ymax)
                    distance.Y = Ymax * Math.Sign(distance.Y);
                if (Math.Abs(distance.Y) < Ymin)
                    distance.Y = Ymin * Math.Sign(distance.Y);

                distance.X += Main.rand.NextFloat(-50, 50);
                distance.Y += Main.rand.NextFloat(-200, 200); //randomness otherwise pattern basically becomes static
                npc.localAI[0] = distance.X + player.Center.X;
                npc.localAI[1] = distance.Y + player.Center.Y;
                Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), Vector2.Zero, ModContent.ProjectileType<EoCTpTelegraph>(), 0, 0, Main.myPlayer, 120, npc.whoAmI);
                npc.netUpdate = true;
            }
            if (npc.ai[1] < 120)
            {
                float speed = 6f;
                float acceleration = 0.07f;
                //Vector2 target = npc.SafeDirectionTo(player.Center - 120f * Vector2.UnitY);
                float distance = (player.Center - 120f * Vector2.UnitY - npc.Center).Length();
                //对速度加速度的修正
                if (distance > 400f)
                {
                    speed += 2f;
                    acceleration += 0.1f;
                    if (distance > 600f)
                    {
                        speed += 2f;
                        acceleration += 0.1f;
                        if (distance > 800f)
                        {
                            speed += 1f;
                            acceleration += 0.05f;
                        }
                    }
                }
                if (Main.getGoodWorld)
                {
                    speed += 1f;
                    acceleration += 0.1f;
                }
                Movement(npc, player.Center - 12 * Vector2.UnitY, speed, acceleration);
                npc.velocity *= 0.95f;
                RotateTowards(npc, player.Center, 0.10f);
                npc.alpha += 4;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 4f;
                    }
                }
            }
            if (npc.ai[1] == 90)
            {
                npc.localAI[2] = (player.Center - new Vector2(npc.localAI[0], npc.localAI[1])).ToRotation();
                npc.rotation = npc.localAI[2] - MathHelper.PiOver2;
                Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), 48 * npc.localAI[2].ToRotationVector2(), ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -1, (float)FalseEoC.MoveType.Straight, 60);
            }
            if (npc.ai[1] == 120)
            {
                npc.Center = new Vector2(npc.localAI[0], npc.localAI[1]);
                ReleaseDust(npc, 500);
                ScreenShakeSystem.StartShake(10);
                npc.velocity = 72 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                //npc.velocity = 72 * npc.SafeDirectionTo(player.Center);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                npc.netUpdate = true;
            }
            if (npc.ai[1]> 120)
            {
                npc.velocity *= 0.975f;
                if (npc.alpha > 0)
                    npc.alpha = 0;
                for (int i = 0; i < 8; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
                if (npc.ai[1] % 3 == 0)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                    //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 2 * Main.rand.NextVector2Unit(), ModContent.ProjectileType<MoonFireProj>(), 0, 0, Main.myPlayer);
                }
                //ShootBackMoonBolt(npc, 1);
                //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, player.Center.X, player.Center.Y, 90);
                //RotateTowards(npc, player.Center, 0.22f);
            }
            if (++npc.ai[1] > 150)
            {
                AIState = 3f;
                npc.ai[1] = 0f;
                npc.ai[2] = 0f;
                npc.localAI[0] = 0f;
                npc.localAI[1] = 0f;
                npc.localAI[2] = 0f;
                npc.netUpdate = true;
            }
        }
        private void P3FastDash(NPC npc, Player player)//P3阴间冲刺
        {
            if (npc.ai[1] == 0)
            {
                float predictDistance = 20f;
                Vector2 targetDelta = player.Center - npc.Center;
                // 根据玩家速度预测提前量
                float playerSpeedFactor = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y) / 4f;

                float predictionMultiplier = 10f - playerSpeedFactor;
                if (predictionMultiplier < 5f)
                    predictionMultiplier = 5f;
                if (predictionMultiplier > 15f)
                    predictionMultiplier = 15f;

                predictionMultiplier *= 5f;
                predictDistance *= 1.6f;

                targetDelta.X -= player.velocity.X * predictionMultiplier;
                targetDelta.Y -= player.velocity.Y * predictionMultiplier / 4f;

                // 添加随机误差
                targetDelta.X *= 1f + Main.rand.Next(-10, 11) * 0.01f;
                targetDelta.Y *= 1f + Main.rand.Next(-10, 11) * 0.01f;

                float dirLength = targetDelta.Length();
                float originalDirLength = dirLength;
                dirLength = predictDistance / dirLength;
                npc.velocity.X = targetDelta.X * dirLength;
                npc.velocity.Y = targetDelta.Y * dirLength;
                // 添加随机偏移
                npc.velocity.X += Main.rand.Next(-20, 21) * 0.1f;
                npc.velocity.Y += Main.rand.Next(-20, 21) * 0.1f;
                if (originalDirLength < 100f) // 距离较近时交换 X/Y 方向以避免太直
                {
                    if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                    {
                        float x = Math.Abs(npc.velocity.X);
                        float y = Math.Abs(npc.velocity.Y);
                        if (npc.Center.X > player.Center.X)
                            y *= -1f;
                        if (npc.Center.Y > player.Center.Y)
                            x *= -1f;
                        npc.velocity.X = y;
                        npc.velocity.Y = x;
                    }
                }
                else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y)) // 较远时求平均调整
                {
                    float avg = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                    float x = avg;
                    float y = avg;
                    if (npc.Center.X > player.Center.X)
                        y *= -1f;
                    if (npc.Center.Y > player.Center.Y)
                        x *= -1f;
                    npc.velocity.X = y;
                    npc.velocity.Y = x;
                }
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                if (npc.ai[2] == 0)
                    npc.localAI[0] = 8 + Main.rand.Next(1, 7);//次数
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                FargoSoulsUtil.XWay(8, npc.GetSource_FromThis(), npc.Center, ModContent.ProjectileType<BloodScythe>(), 1.5f, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0);
                for (int i = 0; i < 8; i++)
                {
                    for (float j = -1; j <= 1; j += 2)
                    {
                        double angle = i * MathHelper.TwoPi / 8 * j;
                        Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                        EllipseVel *= (j + 3f) / 2f;
                        Vector2 vel = EllipseVel / 30f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, player.Center.X, player.Center.Y, 40);
                        j += 2;
                    }
                }
                for (int i = 0; i < 8; i++)
                {
                    for (float j = -1; j <= 1; j += 2)
                    {
                        j += 2;
                        double angle = i * MathHelper.TwoPi / 8 * j;
                        Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                        EllipseVel *= (j + 2f) / 3f;
                        Vector2 vel = EllipseVel / 10f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                    }
                }
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
            }
            if (npc.ai[1] == 15 && Vector2.Distance(npc.position, player.position) < 200f)
                npc.ai[1] -= 1f;
            if (npc.ai[1] > 15)
            {
                npc.velocity *= 0.95f;
                if (Math.Abs(npc.velocity.X) < 0.1)
                    npc.velocity.X = 0f;
                if (Math.Abs(npc.velocity.Y) < 0.1)
                    npc.velocity.Y = 0f;
                RotateTowards(npc, player.Center, 0.22f);
            }
            if (++npc.ai[1] > 28)
            {
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                npc.ai[2]++;
                if (npc.ai[2] < npc.localAI[0])
                    npc.ai[1] = 0;
                if (npc.ai[2] >= npc.localAI[0] && npc.ai[1] > 28 + 30)
                {
                    ChooseNext(npc);
                    npc.ai[1] = 0;
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.netUpdate = true;
                }
            }
        }
        private void FastTpDashs(NPC npc,Player player)//高频tp冲刺
        {
            int intervel = 80; 
            if (npc.ai[1] == 0)
            {
                if (npc.ai[2] == 0)
                    npc.ai[2] = Main.rand.Next(4, 7);
                Vector2 distance = Main.rand.Next(400, 701) * Main.rand.NextVector2Unit();

                npc.localAI[0] = distance.X + player.Center.X;
                npc.localAI[1] = distance.Y + player.Center.Y;
                Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), Vector2.Zero, ModContent.ProjectileType<EoCTpTelegraph>(),
                    -1, 0, Main.myPlayer, intervel, npc.whoAmI);
                npc.netUpdate = true;
            }
            if (npc.ai[1] < intervel && npc.ai[1] > 0.75f * (float)intervel)
            {
                float speed = 6f;
                float acceleration = 0.07f;
                //Vector2 target = npc.SafeDirectionTo(player.Center - 120f * Vector2.UnitY);
                float distance = (player.Center - 120f * Vector2.UnitY - npc.Center).Length();
                //对速度加速度的修正
                if (distance > 400f)
                {
                    speed += 2f;
                    acceleration += 0.1f;
                    if (distance > 600f)
                    {
                        speed += 2f;
                        acceleration += 0.1f;
                        if (distance > 800f)
                        {
                            speed += 1f;
                            acceleration += 0.05f;
                        }
                    }
                }
                if (Main.getGoodWorld)
                {
                    speed += 1f;
                    acceleration += 0.1f;
                }
                Movement(npc, player.Center - 12 * Vector2.UnitY, speed, acceleration);
                npc.velocity *= 0.95f;
                RotateTowards(npc, player.Center, 0.10f);
                npc.alpha += 4;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            if (npc.ai[1] < 3 * (float)intervel / 5f)
            {
                npc.velocity *= 0.97f;
                //npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                npc.alpha += 6;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 4f;
                    }
                }
                if (npc.ai[1] % 2 == 0)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                    
                }
                if (npc.ai[1] % 4 == 0)
                {
                    //int flag = Math.Sign(npc.SafeDirectionTo(player.Center).ToRotation() - npc.velocity.ToRotation());
                    //flag = Main.rand.NextBool() ? 1 : -1;
                    //Vector2 vel = npc.velocity.SafeNormalize(Vector2.Zero).RotatedBy(flag * MathHelper.PiOver2);
                    //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 20 * npc.velocity.SafeNormalize(Vector2.Zero).RotatedBy(+MathHelper.PiOver2), ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, player.Center.X, player.Center.Y, 40);
                    //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 20 * npc.velocity.SafeNormalize(Vector2.Zero).RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, player.Center.X, player.Center.Y, 40);
                }
            }
            if (npc.ai[1] == 0.75f * intervel)
            {
                npc.localAI[2] = (player.Center - new Vector2(npc.localAI[0], npc.localAI[1])).ToRotation();
                npc.rotation = npc.localAI[2] - MathHelper.PiOver2;
                Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), 48 * npc.localAI[2].ToRotationVector2(), ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -1, (float)FalseEoC.MoveType.Straight, 60);
            }
            if (npc.ai[1] == intervel)
            {
                npc.Center = new Vector2(npc.localAI[0], npc.localAI[1]);
                ReleaseDust(npc, 500);
                ScreenShakeSystem.StartShake(10);
                npc.velocity = 72 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                //npc.velocity = 72 * npc.SafeDirectionTo(player.Center);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 dir = npc.SafeDirectionTo(player.Center).RotatedBy(MathHelper.PiOver2);

                    for (float j = -1; j <= 1; j += 1)
                    {
                        Vector2 target = player.Center + j * 600* dir;
                        double angle = i * MathHelper.TwoPi / 8;
                        Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                        EllipseVel *= 2f;
                        Vector2 vel = EllipseVel.RotatedBy(npc.rotation + MathHelper.PiOver2) / 20f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, target.X, target.Y, 40);
                    }
                }
                for (int i = 0; i < 8; i++)
                {
                    for (float j = -1; j <= 1; j += 2)
                    {
                        double angle = i * MathHelper.TwoPi / 8 * j;
                        Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                        EllipseVel *= (j + 2f) / 4f;
                        Vector2 vel = EllipseVel.RotatedBy(npc.rotation + MathHelper.PiOver2) / 10f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                    }
                }
                npc.netUpdate = true;
            }
            if (++npc.ai[1] > intervel)
            {
                npc.velocity *= 0.97f;
                if (npc.alpha > 0)
                    npc.alpha = 0;
                for (int i = 0; i < 8; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }

                //ShootBackMoonBolt(npc, 1);
                //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, player.Center.X, player.Center.Y, 90);
                //RotateTowards(npc, player.Center, 0.22f);
                if (npc.ai[2] > 0)
                {
                    npc.ai[2] -= 1;
                }
                if (npc.ai[2] != 0)
                {
                    npc.ai[1] = 0;
                }
                npc.localAI[0] = 0f;
                npc.localAI[1] = 0f;
                npc.localAI[2] = 0f;
                if (npc.ai[2] != 0)
                    npc.netUpdate = true;
                if (npc.ai[2] <= 0 && npc.ai[1] > intervel + 30)
                {
                    RecordLast();
                    ChooseNext(npc);
                    npc.ai[1] = 0;
                    npc.ai[2] = 0f;
                    npc.localAI[0] = 0f;
                    npc.localAI[1] = 0f;
                    npc.localAI[2] = 0f;
                    npc.netUpdate = true;
                }
            }
        }
        private void Restraint_Triangle(NPC npc, Player player)//三角拘束
        {
            if (npc.ai[1] == 0)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Triangle;
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 150 * MathF.PI / 180f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 150 * MathF.PI / 180f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -50, (int)movetype, 6 * 15);
                npc.netUpdate = true;
            }
            if (npc.ai[1] % 15 == 0 && npc.ai[1] <= 5 * 15 && npc.ai[1] > 0)
            {
                npc.localAI[2] += 120 * MathF.PI / 180f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 150 * MathF.PI / 180f);
                npc.netUpdate = true;
            }
            if ((npc.ai[1] - 80) % 15 == 0 && npc.ai[1] >= 80 && npc.ai[1] <= 80 + 15 * 6)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                ReleaseDust(npc, 100);
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                if (npc.ai[1] == 80)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (npc.ai[1] > 80 && npc.ai[1] < 80 + 6 * 15)
            {
                if (npc.ai[1] % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 75);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }
            if (npc.ai[1] > 80 + 6 * 15)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 3;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            npc.alpha += 5;
            if (npc.alpha > 255)
                npc.alpha = 255;
            if (++npc.ai[1] > 80 + 15 * 6 + 20 - (npc.ai[3] == 3 ? 10 : 0))
            {
                RecordLast();
                AIState = 24;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.localAI[2] = 0;
                npc.netUpdate = true;
            }
        }
        private void Restraint_Square(NPC npc, Player player)//方形拘束
        {
            if (npc.ai[1] == 0)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Square;
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 707 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 135 * MathF.PI / 180f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 135 * MathF.PI / 180f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 66.67f * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -50, (int)movetype, 8 * 15);
                npc.netUpdate = true;
            }
            if (npc.ai[1] % 15 == 0 && npc.ai[1] <= 8 * 15 && npc.ai[1] > 0)
            {
                npc.localAI[2] += 90 * MathF.PI / 180f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 707 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 135 * MathF.PI / 180f);
                npc.netUpdate = true;
            }
            if ((npc.ai[1] - 80) % 15 == 1 && npc.ai[1] >= 80 + 1 && npc.ai[1] <= 80 + 15 * 8 + 1)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                ReleaseDust(npc, 100);
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                float speed = npc.velocity.Length();
                Vector2 veldir = npc.velocity.SafeNormalize(Vector2.Zero);
                if (speed > 66.7f)
                    npc.velocity = veldir * 66.7f;
                if (npc.ai[1] == 80 + 1)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (npc.ai[1] > 80 && npc.ai[1] < 80 + 8 * 15)
            {
                if (npc.ai[1] % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 75);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }
            if (npc.ai[1] > 80 + 8 * 15)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 3;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            npc.alpha += 5;
            if (npc.alpha > 255)
                npc.alpha = 255;
            if (++npc.ai[1] > 80 + 15 * 8 + 40 - (npc.ai[3] == 3 ? 30 : 0))
            {
                RecordLast();
                AIState = 24;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.localAI[2] = 0;
                npc.netUpdate = true;
            }
        }
        private void Restraint_Hexagon(NPC npc, Player player)//六芒星拘束
        {
            FalseEoC.MoveType movetype = FalseEoC.MoveType.Hexagon;
            if (npc.ai[1] == 0)
            {
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 150 * MathF.PI / 180f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 150 * MathF.PI / 180f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -50, (int)movetype, 3 * 15);
                npc.netUpdate = true;
            }
            if (npc.ai[1] % 15 == 0 && npc.ai[1] <= 5 * 15 && npc.ai[1] > 0)
            {
                npc.localAI[2] += 120 * MathF.PI / 180f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                if (npc.ai[1] % 45 == 0)
                {
                    npc.localAI[2] -= 60 * MathF.PI / 180f;
                    spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                    Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 150 * MathF.PI / 180f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -50, (int)movetype, 3 * 15);
                }    
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 150 * MathF.PI / 180f);
                npc.netUpdate = true;
            }
            if ((npc.ai[1] - 80) % 15 == 0 && npc.ai[1] >= 80 && npc.ai[1] <= 80 + 15 * 6)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                ReleaseDust(npc, 100);
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                if (npc.ai[1] == 80)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (npc.ai[1] > 80 && npc.ai[1] < 80 + 6 * 15)
            {
                if (npc.ai[1] % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 75);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }
            if (npc.ai[1] > 80 + 6 * 15)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 3;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            npc.alpha += 5;
            if (npc.alpha > 255)
                npc.alpha = 255;
            if (++npc.ai[1] > 80 + 15 * 6 + 20 - (npc.ai[3] == 3 ? 10 : 0))
            {
                RecordLast();
                AIState = 24;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.localAI[2] = 0;
                npc.netUpdate = true;
            }
        }
        private void Restraint_Octagonal(NPC npc, Player player)//八芒星拘束
        {
            if (npc.ai[1] == 0)
            {
                npc.ai[2] = Main.rand.Next(0, 3);
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Octagonal;
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 157.5f * MathF.PI / 180f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 157.5f * MathF.PI / 180f);

                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -50, (int)movetype, 15 * 8);
                npc.netUpdate = true;
            }
            if (npc.ai[1] % 15 == 0 && npc.ai[1] <= 7 * 15 && npc.ai[1] > 0)
            {
                npc.localAI[2] += 135 * MathF.PI / 180f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 157.5f * MathF.PI / 180f);
                npc.netUpdate = true;
            }
            if ((npc.ai[1] - 80) % 15 == 0 && npc.ai[1] >= 80 && npc.ai[1] <= 80 + 8 * 15)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                ReleaseDust(npc, 100);
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                if (npc.ai[1] == 80)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (npc.ai[1] > 80 && npc.ai[1] < 80 + 15 * 8)
            {
                if (npc.ai[1] % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 100);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }
                    
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }
            if (npc.ai[1] > 80 + 8 * 15)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 3;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            npc.alpha += 5;
            if (npc.alpha > 255)
                npc.alpha = 255;
            if (++npc.ai[1] > 80 + 8 * 15 + 40 - (npc.ai[3] == 3 ? 30 : 0))
            {
                RecordLast();
                AIState = 24;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.localAI[2] = 0;
                npc.netUpdate = true;
            }
        }
        private void Restraight_Round(NPC npc,Player player)//圆形拘束
        {
            int r = 600;
            if (npc.ai[1] == 0)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Round;
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + r * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 90f * MathF.PI / 180f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 90 * MathF.PI / 180f);

                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -50, (int)movetype, 50 * 2);
                npc.netUpdate = true;
            }
            if (npc.ai[1] % 10 == 0 && npc.ai[1] <= 50 * 2 && npc.ai[1] > 0)
            {
                npc.localAI[2] += 15f * 0.1f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + r * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, npc.localAI[2] + 90 * MathF.PI / 180f);
                npc.netUpdate = true;
            }
            if ((npc.ai[1] - 80) % 10 == 0 && npc.ai[1] >= 80 && npc.ai[1] <= 80 + 50 * 2)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                ReleaseDust(npc, 100);
                if ((npc.ai[1] - 80) % 20 == 0)
                    SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                if (npc.ai[1] == 80)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (npc.ai[1] > 80 && npc.ai[1] < 80 + 50 * 2)
            {
                if (npc.ai[1] % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 65);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }

                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }
            
            if (npc.ai[1] > 80 + 50 * 2)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 6;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            else if (npc.ai[1] >= 80)
            {
                npc.velocity = npc.velocity.RotatedBy(2f / 15f);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
            }
            else
            {
                npc.alpha += 4;
                if (npc.alpha > 255)
                    npc.alpha = 255;
            }
            if (++npc.ai[1] > 80 + 50 + 50 + 10 - (npc.ai[3] == 3 ? 0 : 0))
            {
                RecordLast();
                AIState = 24;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.localAI[2] = 0;
                npc.netUpdate = true;
            }
        }
        private void FaintVisible_FourRowsScythe(NPC npc, Player player)//四排月镰
        {
            if (npc.ai[1] == 0 || npc.ai[1] == 60)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Straight;
                //Vector2 dir = -npc.SafeDirectionTo(player.Center);
                if (npc.ai[1] == 0)
                {
                    npc.localAI[0] = player.Center.X + player.velocity.X;
                    npc.localAI[1] = player.Center.Y + player.velocity.Y;
                }
                //npc.localAI[2] = dir.ToRotation();
                npc.localAI[2] = Main.rand.NextFloat(0, MathHelper.TwoPi);
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * MathHelper.PiOver2 + MathHelper.PiOver4 + npc.localAI[2];
                    Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 1000 * Vector2.UnitX.RotatedBy(angle);
                    Vector2 vel = Vector2.UnitX.RotatedBy(angle + 135 * MathF.PI / 180f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, vel.ToRotation());
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -50, (int)movetype, 25);
                }
                
                //Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                   // 80, npc.whoAmI, npc.localAI[2] + 135 * MathF.PI / 180f);
                npc.netUpdate = true;
            }
            npc.localAI[2] = Main.rand.NextFloat(0, MathHelper.TwoPi);
            if (npc.ai[1] == 80 || npc.ai[1] == 80 + 60)
            {
                //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                //ReleaseDust(npc, 100);
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                ScreenShakeSystem.StartShake(5);
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Straight;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * MathHelper.PiOver2 + MathHelper.PiOver4 + npc.localAI[2];
                    Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 1000 * Vector2.UnitX.RotatedBy(angle);
                    Vector2 vel = Vector2.UnitX.RotatedBy(angle + 135 * MathF.PI / 180f);
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -1, (int)movetype, 15);
                    Main.projectile[p].localAI[2] = 1;//启用发射弹幕
                }
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                npc.netUpdate = true;
            }
            if (npc.ai[1] < 80 + 140)
            {
                npc.velocity *= 0.94f;
                npc.alpha += 5;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            /*
            if (npc.ai[1] == 80 + 120 || npc.ai[1] == 80 + 120 + 60)
            {
                npc.localAI[3] = 1;
            }
            if (npc.ai[1] == 80 + 155)
                npc.localAI[3] = 0;
            */
            if (++npc.ai[1] > 80 + 120 - (npc.ai[3] == 3 ? 20 : 0))
            {
                RecordLast();
                AIState = 24;
                //npc.alpha = 0;
                npc.ai[2] = 0;
                npc.ai[1] = 0;
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.localAI[2] = 0;
                npc.localAI[3] = 0;
                npc.netUpdate = true;
            }
        }
        private void FaintVisible_RoundScythe(NPC npc, Player player)//圆形月镰
        {
            if (npc.ai[1] == 0 || npc.ai[1] == 60)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Round;
                //Vector2 dir = -npc.SafeDirectionTo(player.Center);
                if (npc.ai[1] == 0)
                {
                    npc.localAI[0] = player.Center.X + player.velocity.X;
                    npc.localAI[1] = player.Center.Y + player.velocity.Y;
                }
                //npc.localAI[2] = dir.ToRotation();
                npc.localAI[2] = Main.rand.NextFloat(0, MathHelper.TwoPi);
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * MathHelper.PiOver2 + MathHelper.PiOver4 + npc.localAI[2];
                    Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 600 * Vector2.UnitX.RotatedBy(angle);
                    Vector2 vel = Vector2.UnitX.RotatedBy(angle + 90 * MathF.PI / 180f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    80, npc.whoAmI, vel.ToRotation());
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -50, (int)movetype, 24);//一半
                }
                npc.netUpdate = true;
            }
            //npc.localAI[2] = Main.rand.NextFloat(0, MathHelper.TwoPi);
            if (npc.ai[1] == 80 || npc.ai[1] == 80 + 60)
            {
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                ScreenShakeSystem.StartShake(5);
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Round;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * MathHelper.PiOver2 + MathHelper.PiOver4 + npc.localAI[2];
                    Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 600 * Vector2.UnitX.RotatedBy(angle);
                    Vector2 vel = Vector2.UnitX.RotatedBy(angle + 90 * MathF.PI / 180f);
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -1, (int)movetype, 24);
                    Main.projectile[p].localAI[2] = 1;//启用发射弹幕
                }
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                npc.netUpdate = true;
            }
            if (npc.ai[1] < 80 + 40 + 60)
            {
                npc.velocity *= 0.94f;
                npc.alpha += 6;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 4f;
                    }
                }
            }
            /*
            if (npc.ai[1] == 90 + 90 || npc.ai[1] == 90 + 90 + 60)
            {
                npc.localAI[3] = 1;
            }
            if (npc.ai[1] == 80 + 150)
                npc.localAI[3] = 0;
            */
            if (++npc.ai[1] > 80 + 110 + (npc.ai[3] >= 3 ? 0 : 10))
            {
                RecordLast();
                AIState = 24;
                npc.alpha = 0;
                npc.ai[2] = 0;
                npc.ai[1] = 0;
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.localAI[2] = 0;
                npc.localAI[3] = 0;
                npc.netUpdate = true;
            }
        }
        private void ChooseNextAttack(NPC npc)
        {
            if (npc.ai[3] != 3)
                AIState = 25;
            else
                ChooseNext(npc);
        }
        private void SuperTpDash(NPC npc, Player player)//超级tp冲
        {
            if (npc.ai[1] == 0)
            {
                Vector2 distance = player.Center + 350 * Main.rand.NextVector2Unit();

                distance.X += Main.rand.NextFloat(-50, 50);
                distance.Y += Main.rand.NextFloat(-200, 200); //randomness otherwise pattern basically becomes static
                npc.localAI[0] = distance.X;
                npc.localAI[1] = distance.Y;
                Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), Vector2.Zero, ModContent.ProjectileType<EoCTpTelegraph>(), 0, 0, Main.myPlayer, 120, npc.whoAmI);
                npc.netUpdate = true;
            }
            if (npc.ai[1] < 120)
            {
                float speed = 6f;
                float acceleration = 0.07f;
                //Vector2 target = npc.SafeDirectionTo(player.Center - 120f * Vector2.UnitY);
                float distance = (player.Center - 120f * Vector2.UnitY - npc.Center).Length();
                //对速度加速度的修正
                if (distance > 400f)
                {
                    speed += 2f;
                    acceleration += 0.1f;
                    if (distance > 600f)
                    {
                        speed += 2f;
                        acceleration += 0.1f;
                        if (distance > 800f)
                        {
                            speed += 1f;
                            acceleration += 0.05f;
                        }
                    }
                }
                if (Main.getGoodWorld)
                {
                    speed += 1f;
                    acceleration += 0.1f;
                }
                Movement(npc, player.Center - 12 * Vector2.UnitY, speed, acceleration);
                npc.velocity *= 0.95f;
                RotateTowards(npc, player.Center, 0.10f);
                npc.alpha += 4;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 4f;
                    }
                }
            }
            if (npc.ai[1] == 100)
            {
                npc.localAI[2] = (player.Center - new Vector2(npc.localAI[0], npc.localAI[1])).ToRotation();
                npc.rotation = npc.localAI[2] - MathHelper.PiOver2;
                Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), 48 * npc.localAI[2].ToRotationVector2(), ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -1, (float)FalseEoC.MoveType.Straight, 60);
            }
            if (npc.ai[1] == 120)
            {
                npc.Center = new Vector2(npc.localAI[0], npc.localAI[1]);
                ReleaseDust(npc, 500);
                ScreenShakeSystem.StartShake(10);
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.velocity = 72 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                //npc.velocity = 72 * npc.SafeDirectionTo(player.Center);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                npc.netUpdate = true;
            }
            if (npc.ai[1] > 120)
            {
                npc.velocity *= 0.975f;
                if (npc.alpha > 0)
                    npc.alpha = 0;
                for (int i = 0; i < 8; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
                if (npc.ai[1] % 3 == 0)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                    //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 2 * Main.rand.NextVector2Unit(), ModContent.ProjectileType<MoonFireProj>(), 0, 0, Main.myPlayer);
                }
                ShootBackMoonBolt(npc, 1);
                //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, player.Center.X, player.Center.Y, 90);
                //RotateTowards(npc, player.Center, 0.22f);
            }
            if (++npc.ai[1] > 150)
            {
                AIState = 6f;
                npc.ai[1] = 0f;
                npc.ai[2] = 0f;
                npc.localAI[0] = 0f;
                npc.localAI[1] = 0f;
                npc.localAI[2] = 0f;
                npc.netUpdate = true;
            }
        }
        private void P3MoonShootDash(NPC npc, Player player)//冲刺散射
        {
            if (npc.ai[1] == 0)
            {
                #region 原版
                float predictDistance = 20f;
                Vector2 targetDelta = player.Center - npc.Center;
                // 根据玩家速度预测提前量
                float playerSpeedFactor = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y) / 4f;

                float predictionMultiplier = 10f - playerSpeedFactor;
                if (predictionMultiplier < 5f)
                    predictionMultiplier = 5f;
                if (predictionMultiplier > 15f)
                    predictionMultiplier = 15f;
                predictionMultiplier *= 3.5f;
                predictDistance *= 1.3f;
                targetDelta.X -= player.velocity.X * predictionMultiplier;
                targetDelta.Y -= player.velocity.Y * predictionMultiplier / 4f;
                // 添加随机误差
                targetDelta.X *= 1f + Main.rand.Next(-10, 11) * 0.01f;
                targetDelta.Y *= 1f + Main.rand.Next(-10, 11) * 0.01f;

                float dirLength = targetDelta.Length();
                float originalDirLength = dirLength;
                dirLength = predictDistance / dirLength;
                npc.velocity.X = targetDelta.X * dirLength;
                npc.velocity.Y = targetDelta.Y * dirLength;
                // 添加随机偏移
                npc.velocity.X += Main.rand.Next(-20, 21) * 0.1f;
                npc.velocity.Y += Main.rand.Next(-20, 21) * 0.1f;
                if (originalDirLength < 100f) // 距离较近时交换 X/Y 方向以避免太直
                {
                    if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                    {
                        float x = Math.Abs(npc.velocity.X);
                        float y = Math.Abs(npc.velocity.Y);
                        if (npc.Center.X > player.Center.X)
                            y *= -1f;
                        if (npc.Center.Y > player.Center.Y)
                            x *= -1f;
                        npc.velocity.X = y;
                        npc.velocity.Y = x;
                    }
                }
                else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y)) // 较远时求平均调整
                {
                    float avg = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                    float x = avg;
                    float y = avg;
                    if (npc.Center.X > player.Center.X)
                        y *= -1f;
                    if (npc.Center.Y > player.Center.Y)
                        x *= -1f;
                    npc.velocity.X = y;
                    npc.velocity.Y = x;
                }
                #endregion
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                if (npc.ai[2] == 0)
                    npc.localAI[0] = 5 + Main.rand.Next(0, 3);//次数
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                FargoSoulsUtil.XWay(8, npc.GetSource_FromThis(), npc.Center, ModContent.ProjectileType<BloodScythe>(), 1.5f, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0);
                /*
                for (int i = 0; i < 8; i++)
                {
                    for (float j = -1; j <= 1; j += 2)
                    {
                        double angle = i * MathHelper.TwoPi / 8 * j;
                        Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                        EllipseVel *= (j + 2f) / 2f;
                        Vector2 vel = EllipseVel / 10f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                    }
                }
                */
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
            }
            if (npc.ai[1] == 30 && Vector2.Distance(npc.position, player.position) < 200f)
                npc.ai[1] -= 1f;
            if (npc.ai[1] > 30 && npc.ai[1] <= 30 + 20)
            {
                npc.localAI[1] += 0.03f;
                if (npc.localAI[1] > 0.6f)
                    npc.localAI[1] = 0.6f;
            }
            if (npc.ai[1] > 30 + 20 && npc.ai[1] <= 30 + 40)
            {
                npc.localAI[1] -= 0.03f;
                if (npc.localAI[1] < 0)
                    npc.localAI[1] = 0;
            }
            if (npc.ai[1] == 30)
                npc.localAI[2] = npc.SafeDirectionTo(player.Center).ToRotation();
            if (npc.ai[1] > 30)
            {
                npc.velocity *= 0f;
                if (Math.Abs(npc.velocity.X) < 0.1)
                    npc.velocity.X = 0f;
                if (Math.Abs(npc.velocity.Y) < 0.1)
                    npc.velocity.Y = 0f;
                npc.rotation += npc.localAI[1];
                float i = npc.ai[1] - 60f;
                for (float j = -1; j <= 1; j += 2)
                {
                    double angle = i * MathHelper.TwoPi / 20 * j;
                    Vector2 EllipseVel = new(150f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 300f * (float)Math.Sin(angle));
                    EllipseVel *= (j + 2f) / 2f;
                    Vector2 vel = EllipseVel.RotatedBy(npc.localAI[2]) / 10f;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                }
            }
            if (++npc.ai[1] > 30 + 40)
            {
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                npc.ai[2]++;
                npc.ai[1] = 0;
                npc.localAI[1] = 0;
                if (npc.ai[2] >= npc.localAI[0])
                {
                    ChooseNext(npc);
                    npc.ai[1] = 0;
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.localAI[1] = 0;
                    npc.netUpdate = true;
                }
            }
        }
        private void DeathAnimation(NPC npc)//死亡动画
        {
            npc.dontTakeDamage = true;
            npc.alpha += 3;
            if (npc.alpha > 150)
                npc.alpha = 150;
            
            Particle p;
            float scaleMult;
            int screenshake = 3;
            npc.velocity *= 0.92f; // 水平减速
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 mutantEyePos = npc.Center + new Vector2(-5f, -12f); // Mutant眼睛位置
            if (DeathTimer == 1)
            {
                FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
            }
            // 生成灰尘效果
            if (Main.rand.NextBool(5))
            {
                SoundEngine.PlaySound(npc.HitSound, npc.Center);
            }
            bool recolor = WorldSavingSystem.EternityMode;
            for (int i = 0; i < 8; i++)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }

            // 在特定时间点增加屏幕震动
            if (DeathTimer == 50 || DeathTimer == 120 || DeathTimer == 150)
            {
                screenshake += 2;
                FargoSoulsUtil.ScreenshakeRumble(screenshake);
                SoundEngine.PlaySound(FargosSoundRegistry.MutantSword with { Volume = 0.6f }, npc.Center);
            }

            // 初始充能阶段（60-149帧）
            if (DeathTimer >= 60 && DeathTimer < 150)
            {
                Vector2 pos = npc.Center + 5 * Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi);
                scaleMult = (DeathTimer - 60) / 23f; // 随时间增大的比例
                p = new SparkParticle(pos, Vector2.UnitX.RotatedBy((pos - npc.Center).ToRotation()),
                    Color.Teal, scaleMult * 0.1f, 10);
                p.Spawn();
                /*
                if (DeathTimer % 20 == 0)
                {
                    Vector2 spawnPos = player.Center + Main.rand.Next(300, 401) * Main.rand.NextVector2Unit();
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                    30, npc.whoAmI, Main.rand.NextFloat(0, 6.28f));
                }
                */
            }

            // 主要爆炸阶段（270-300帧）
            if (DeathTimer >= 150)
            {
                // 眼睛发光效果
                scaleMult = (DeathTimer - 150) / 10f;
                p = new SparkParticle(mutantEyePos, Vector2.UnitY, Color.Teal, 1.5f, 120);
                p.Scale *= scaleMult;
                p.Spawn();
                p = new SparkParticle(mutantEyePos, Vector2.UnitX, Color.Teal, 1.5f, 120);
                p.Scale *= scaleMult;
                p.Spawn();

                // 周期性爆炸效果
                if (DeathTimer % 5 == 0)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 spawnPos = npc.position + new Vector2(Main.rand.Next(npc.width), Main.rand.Next(npc.height));
                        int type = ModContent.ProjectileType<MutantBombSmall>();
                        Projectile proj = Projectile.NewProjectileDirect(npc.GetSource_FromAI(), spawnPos,
                            Vector2.Zero, type, 0, 0f, Main.myPlayer);
                        proj.scale *= 0.43f * scaleMult; // 根据时间调整爆炸规模
                    }
                    SoundEngine.PlaySound(SoundID.Item14, npc.Center); // 爆炸音效
                    FargoSoulsUtil.ScreenshakeRumble((DeathTimer - 150) / 15f); // 屏幕震动
                    
                }
            }

            // 最终爆炸（298帧）
            if (DeathTimer == 178)
            {
                FargoSoulsUtil.ScreenshakeRumble(7f); // 强烈屏幕震动
                SoundEngine.PlaySound(FargosSoundRegistry.MutantKSKill, npc.Center); // 终结音效
                for (double i = 0; i < 40; i++)
                {
                    double angle = i * MathHelper.PiOver2 / 10;
                    Vector2 target = npc.Center + new Vector2(150f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 300f * (float)Math.Sin(angle));
                    Vector2 targetV = (npc.Center.X - target.X) * Vector2.UnitX / 1500;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), target, targetV, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
                for (double i = 0; i < 60; i++)
                {
                    double angle = i * MathHelper.PiOver2 / 15;
                    Vector2 target = npc.Center + new Vector2(225f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 450f * (float)Math.Sin(angle));
                    Vector2 targetV = (npc.Center.X - target.X) * Vector2.UnitX / 1500;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), target, targetV, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
                for (double i = 0; i < 24; i++)
                {
                    double angle = i * MathHelper.PiOver2 / 6;
                    Vector2 Center = npc.Center + new Vector2(150f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 75f * (float)Math.Sin(angle));
                    Vector2 targetV = (npc.Center.X - Center.X) * Vector2.UnitX / 1500;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), Center, targetV, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
                Main.bloodMoon = false;
                npc.localAI[3] = 1;
            }
        }

        #endregion
        #region 辅助方法
        
        private static void FancyFireballs(NPC npc, int repeats)
        {
            float modifier = 0;
            for (int i = 0; i < repeats; i++)
                modifier = MathHelper.Lerp(modifier, 1f, 0.08f);

            float distance = 1400 * (1f - modifier);
            float rotation = MathHelper.TwoPi * modifier;
            const int max = 6;
            for (int i = 0; i < max; i++)
            {
                int d = Dust.NewDust(npc.Center + distance * Vector2.UnitX.RotatedBy(rotation + MathHelper.TwoPi / max * i), 0, 0, DustID.SnowSpray, npc.velocity.X * 0.3f, npc.velocity.Y * 0.3f, 150);
                int p = Dust.NewDust(npc.Center + distance * Vector2.UnitX.RotatedBy(-rotation + MathHelper.TwoPi / max * i), 0, 0, DustID.Vortex, npc.velocity.X * 0.3f, npc.velocity.Y * 0.3f, 150);
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 1.5f - 0.8f * modifier;
                Main.dust[p].noGravity = true;
                Main.dust[p].scale = 1.5f - 0.8f * modifier;
            }
        }
        private static void ReleaseDust(NPC npc, int num = 2)
        {
            for (int i = 0; i < num; i++)
            {
                int randdistance = Main.rand.Next(200, 600);
                float randangle = Main.rand.NextFloat(0, 2 * MathF.PI);
                Vector2 vel = randdistance * Vector2.UnitX.RotatedBy(randangle) / 10;
                int d = Dust.NewDust(npc.Center, 0, 0, DustID.SnowSpray, vel.X, vel.Y, 150);
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = Main.rand.NextFloat(1.2f, 1.5f);
            }
            for (int i = 0; i < num; i++)
            {
                int randdistance = Main.rand.Next(50, 600);
                float randangle = Main.rand.NextFloat(0, 2 * MathF.PI);
                Vector2 vel = randdistance * Vector2.UnitX.RotatedBy(randangle) / 5;
                Vector2 spawnPos = npc.Center + vel / 10;
                int p = Dust.NewDust(spawnPos, 0, 0, DustID.Vortex, vel.X, vel.Y, 150);
                Main.dust[p].noGravity = true;
                Main.dust[p].scale = Main.rand.NextFloat(1.2f, 1.5f);
            }
        }
        private static void ShootBackMoonBolt(NPC npc, int num)
        {
            for (int i = 0; i < num; i++)
            {
                float angle = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
                Vector2 vel = npc.velocity.RotatedBy(angle);
                Vector2 targetPos = npc.Center - 10 * npc.velocity;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, targetPos.X, targetPos.Y, 40);
            }
        }
        private void RecordLast()
        {
            Last2AIState = LastAIState;
            LastAIState = (int)AIState;
        }
        private void ChooseNext(NPC npc)
        {
            
            if (npc.ai[3] == 3)
            {
                int num = Main.rand.Next(9, 16);
                while (num == LastAIState || num == Last2AIState)
                    num = Main.rand.Next(9, 16);
                AIState = num;
                
            }
            else
            {
                int num = Main.rand.NextBool(2) ? 8 : Main.rand.Next(9, 16);
                while (num == LastAIState || num == Last2AIState)
                    num = Main.rand.NextBool(2) ? 8 : Main.rand.Next(9, 16);
                AIState = num;
            }
        }
        public static void Movement(NPC npc, Vector2 targetPos, float speed = 7.5f, float acceleration = 0.18f)
        {
            Vector2 target = speed * npc.SafeDirectionTo(targetPos);
            // 向期望速度加速
            if (npc.velocity.X < target.X)
            {
                npc.velocity.X += acceleration;
                if (npc.velocity.X < 0f && target.X > 0f)
                    npc.velocity.X += acceleration;
            }
            else if (npc.velocity.X > target.X)
            {
                npc.velocity.X -= acceleration;
                if (npc.velocity.X > 0f && target.X < 0f)
                    npc.velocity.X -= acceleration;
            }
            if (npc.velocity.Y < target.Y)
            {
                npc.velocity.Y += acceleration;
                if (npc.velocity.Y < 0f && target.Y > 0f)
                    npc.velocity.Y += acceleration;
            }
            else if (npc.velocity.Y > target.Y)
            {
                npc.velocity.Y -= acceleration;
                if (npc.velocity.Y > 0f && target.Y < 0f)
                    npc.velocity.Y -= acceleration;
            }

        }
        /// <summary>
        /// 通常turnSpeed参考取值为:常态0.03，normaldashed 0.08，往后0.08，0.12，0.22等
        /// </summary>
        public static void RotateTowards(NPC npc, Vector2 targetPos, float turnSpeed)
        {
            Vector2 direction = targetPos - npc.Center;
            float targetRotation = direction.ToRotation() - MathHelper.PiOver2; // 克苏鲁之眼需要+90度
            targetRotation = MathHelper.WrapAngle(targetRotation);
            float currentRotation = MathHelper.WrapAngle(npc.rotation);
            //float rotationDiff = MathHelper.WrapAngle(targetRotation - currentRotation);
            /*原法转向方法，现弃用
            if (Math.Abs(rotationDiff) > 0.01f)
            {
                if (rotationDiff > 0)
                {
                    npc.currentRotation += turnSpeed;
                    if (rotationDiff < turnSpeed * 2)
                        npc.currentRotation = currentRotation + rotationDiff * 0.5f;
                }
                else
                {
                    npc.currentRotation -= turnSpeed;
                    if (rotationDiff > -turnSpeed * 2)
                        npc.currentRotation = currentRotation + rotationDiff * 0.5f;
                }
                if (limitRotation)
                {
                    float maxRotationChange = turnSpeed * 3;
                    float actualChange = MathHelper.WrapAngle(npc.currentRotation - currentRotation);
                    if (Math.Abs(actualChange) > maxRotationChange)
                    {
                        npc.currentRotation = currentRotation + maxRotationChange * Math.Sign(actualChange);
                    }
                }
            }
            */
            if (currentRotation < targetRotation)
            {
                if (targetRotation - currentRotation > Math.PI)
                    currentRotation -= turnSpeed;
                else
                    currentRotation += turnSpeed;
            }
            else if (currentRotation > targetRotation)
            {
                if (currentRotation - targetRotation > Math.PI)
                    currentRotation += turnSpeed;
                else
                    currentRotation -= turnSpeed;
            }
            if (currentRotation > targetRotation - turnSpeed && currentRotation < targetRotation + turnSpeed)
                currentRotation = targetRotation;
            currentRotation = MathHelper.WrapAngle(currentRotation);
            npc.rotation = currentRotation;
        }
        private bool PhaseCheck(NPC npc)
        {
            if (npc.life < npc.lifeMax * 0.15f && npc.ai[3] == 2)
            {
                FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);
                AIState = -3;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.ai[3] = 3;
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.localAI[2] = 0;
                npc.localAI[3] = 0;
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                return false;
            }
            else if (npc.life < npc.lifeMax * 0.5f && npc.ai[3] == 1)
            {
                AIState = -2;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.ai[3] = 2;
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                return false;
            }
            else if (npc.life < npc.lifeMax * 0.8f && npc.ai[3] == 0)
            {
                npc.defense = 0;
                AIState = -1;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.ai[3] = 1;
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                return false;
            }
            return true;
        }
        private static bool AliveCheck(NPC npc, Player player)
        {
            bool length = npc.ai[3] != 3 && Vector2.Distance(npc.Center, player.Center) > 5000f;
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
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);
            binaryWriter.Write7BitEncodedInt(TeleportDirection);
            binaryWriter.Write7BitEncodedInt(LastAIState);
            binaryWriter.Write7BitEncodedInt(Last2AIState);
            binaryWriter.Write7BitEncodedInt(HyperTime);
            binaryWriter.Write7BitEncodedInt(P3AttackChange);
            binaryWriter.Write(AIState);
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);
            TeleportDirection = binaryReader.Read7BitEncodedInt();
            LastAIState = binaryReader.Read7BitEncodedInt();
            Last2AIState = binaryReader.Read7BitEncodedInt();
            HyperTime = binaryReader.Read7BitEncodedInt();
            P3AttackChange = binaryReader.Read7BitEncodedInt();
            AIState = binaryReader.ReadSingle();
        }
        #endregion
        public override bool CheckDead(NPC npc)
        {
            /*
            if (DeathTimer != -1)
                return true;
            npc.life = 1; 
            npc.active = true;
            DeathTimer++;
            npc.dontTakeDamage = true; 
            npc.netUpdate = true; 

            return false;
            */
            return true;
        }
    }
}
