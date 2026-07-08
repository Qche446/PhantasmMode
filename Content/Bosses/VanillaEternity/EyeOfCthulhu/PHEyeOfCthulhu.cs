using System;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using Terraria.ID;
using Terraria;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ModLoader;
using System.Runtime.InteropServices;
using Terraria.WorldBuilding;
using Microsoft.Xna.Framework.Graphics;
using FargowiltasSouls.Assets.ExtraTextures;
using Luminance.Core.Graphics;
using Terraria.GameContent;
using FargosPhantasmMode.Content.Bosses.EyeOfCthulhu;
using FargowiltasSouls.Content.Projectiles.BossWeapons;
using FargosPhantasmMode.Content.NPCs;
using System.IO;
using Terraria.ModLoader.IO;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Bosses.MutantBoss;
namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    /*
    internal class PHEyeOfCthulhu : EyeofCthulhu
    {
        
        bool InitializeAI = false;//判断是否需要初始化
        private const float angle = -MathHelper.Pi / 2;
        int auraDistance = 0;
        int flag3 = 0;
        float omiga = 0;
        Vector2 targetCenter = Vector2.Zero;
        Vector2 Direct = Vector2.Zero;
        public int DeathTimer = -1;
        public int ScytheSpawnTimer;

        public override bool SafePreAI(NPC npc)
        {
            if (DeathTimer >= 0)
            {
                DeathAnimation(npc);
                if (++DeathTimer >= 180) // 300帧后真正死亡
                {
                    npc.life = 0;
                    npc.dontTakeDamage = false;
                    npc.checkDead();
                }
            }
            npc.aiStyle = -1;
            PHEyeofCthulhuAI(npc);
            return false;
        }
        public bool PHEyeofCthulhuAI(NPC npc)
        {
            if (InitializeAI == false)
            {
                InitializeCustomAI(npc);
                InitializeAI = true;
            }
            EModeGlobalNPC.eyeBoss = npc.whoAmI;
            npc.dontTakeDamage = npc.alpha > 50 || npc.localAI[0] < 0 || npc.localAI[0] == 8;
            if (npc.alpha > 50)
                Lighting.AddLight(npc.Center, 0.75f, 1.35f, 1.5f);
            npc.TargetClosest();
            Player player = Main.player[npc.target];
            float distance = (player.Center - npc.Center).Length();
            if (npc.localAI[2] >= 1 && auraDistance > 1100)
            {
                if ((player.Center - npc.Center).Length() > auraDistance)
                {
                    player.velocity += (1 + (player.Center - npc.Center).Length() - auraDistance) * Vector2.Normalize(npc.Center - player.Center);
                }
            }//仪式圈限制
            if (ScytheSpawnTimer > 0)
            {
                if (ScytheSpawnTimer % (IsInFinalPhase ? 2 : 6) == 0 && FargoSoulsUtil.HostCheck)
                {
                    if (IsInFinalPhase && !WorldSavingSystem.MasochistModeReal)
                    {
                        int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                        if (p != Main.maxProjectiles)
                            Main.projectile[p].timeLeft = 75;
                    }
                    else
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                    }
                }
                ScytheSpawnTimer--;
            }//为ScytheSpawnTimer赋值让本体发射月镰
            switch (npc.localAI[0])
            {
                case -2://p2进p3
                    if (!AliveCheck(npc, player))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        npc.alpha = 186;
                        Lighting.AddLight(npc.Center, 0.75f, 1.35f, 1.5f);
                        npc.velocity *= 0.96f;
                        for (int i = 0; i < 3; i++)
                        {
                            int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].noLight = true;
                            Main.dust[d].velocity *= 4f;
                        }
                        if (npc.localAI[1] <= 60)
                        {
                            if (npc.localAI[1] == 60)
                            {
                                npc.ai[0] = 3;
                                npc.ai[1] = 1;
                                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                            }
                            omiga = npc.localAI[1] * 0.015f;
                        }
                        else if (npc.localAI[1] <= 120 && npc.localAI[1] > 60)
                        {
                            omiga = (120 - npc.localAI[1]) * 0.015f;
                        }
                        npc.rotation += omiga;
                        if (npc.localAI[1] == 120)
                        {
                            for (int i = 0; i < 128; i++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 3 * Vector2.UnitX.RotatedBy(i * MathHelper.PiOver4 / 16), ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, 0, npc.whoAmI, Main.rand.NextBool(Main.zenithWorld ? 2 : 3) ? 0 : 1);

                            }
                        }
                        if (npc.localAI[1] > 120)
                        {
                            RotateTowards(npc, player.Center, 0.08f);
                            targetCenter = player.Center - 300 * Vector2.UnitY;
                            Movement(npc, targetCenter, 0.2f);
                        }
                        if (npc.localAI[1] == 210)
                        {
                            npc.localAI[3] = 0;
                            Main.bloodMoon = true;
                        }
                        if (npc.localAI[1] >= 269)
                        {
                            npc.alpha = (int)(331 - npc.localAI[1]) * 3;
                        }
                        if (++npc.localAI[1] > 330)
                        {
                            npc.alpha = 0;
                            Main.bloodMoon = false;
                            npc.localAI[0] = 5;
                            npc.localAI[1] = 0;
                            npc.localAI[2] = 2;//进入p3
                            npc.localAI[3] = 1;
                        }
                    }
                    break;
                case -1://p1进p2
                    if (!AliveCheck(npc, player))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        npc.alpha = 180;
                        Lighting.AddLight(npc.Center, 0.75f, 1.35f, 1.5f);
                        npc.velocity *= 0.96f;
                        for (int i = 0; i < 3; i++)
                        {
                            int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].noLight = true;
                            Main.dust[d].velocity *= 4f;
                        }
                        if (npc.localAI[1] >= 60)
                        {
                            FancyFireballs(npc, (int)npc.localAI[1] - 60);
                        }
                        if (npc.localAI[1] == 120)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<EoCRitual>(), 0, 0f, Main.myPlayer, 0f, npc.whoAmI);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(i * MathHelper.PiOver4 / 2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                            }
                            npc.localAI[2] = 1;//p2标志
                        }
                        if (npc.localAI[1] > 120)
                        {
                            //auraDistance = (int)(1200 * (900 - (npc.localAI[1] - 150f) * (npc.localAI[1] - 150f)) / 900) ;
                        }
                        if (++npc.localAI[1] >= 150)
                        {
                            npc.alpha = 0;
                            npc.localAI[0] = 2;//进入p2攻击方式
                            npc.localAI[1] = 0;
                        }
                    }
                    break;
                case 0://悬停到玩家头上
                    if (!AliveCheck(npc, player) || Phase2Check(npc))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int flagX, flagY = 0;
                        flagX = Math.Sign(npc.Center.X - player.Center.X);
                        flagY = Math.Sign(npc.Center.Y - player.Center.Y);

                        Vector2 direct = Vector2.Normalize(player.Center - npc.Center);
                        RotateTowards(npc, player.Center, 0.03f);
                        targetCenter = player.Center + 300 * flagY * Vector2.UnitY + flagX * 300 * Vector2.UnitX;
                        Movement(npc, targetCenter, 0.2f);
                        if (npc.localAI[1] % 60 == 0)
                        {
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.ServantofCthulhu);
                            for (float i = 1; i < 5; i += 1.5f)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, i * direct, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                            }
                        }

                        if (++npc.localAI[1] > 180)
                        {
                            npc.localAI[0]++;
                            npc.localAI[1] = 0;
                        }
                    }
                    break;
                case 1://旋转后冲刺连续三次
                    if (!AliveCheck(npc, player) || Phase2Check(npc))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 direct = npc.SafeDirectionTo(player.Center);
                        Vector2 direct2 = Vector2.UnitX.RotatedBy(npc.rotation);
                        if (npc.localAI[1] < 270)
                        {
                            if (npc.localAI[1] % 90 == 0)
                            {
                                npc.velocity = (100f * MathHelper.TwoPi / 40f) * direct;
                                ScytheSpawnTimer = 40;
                                for (int i = 0; i < 8; i++)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(i * MathHelper.PiOver4), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                                }
                            }
                            npc.rotation = npc.velocity.ToRotation() + angle;//设置朝向
                            if (npc.localAI[1] % 90 < 40)
                            {
                                npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi / 40f);
                            }
                            else if (npc.localAI[1] % 90 == 40)
                            {
                                ScytheSpawnTimer = 50;
                            }
                            else if (npc.localAI[1] % 90 < 90)
                            {
                                float speed = npc.velocity.Length();
                                npc.velocity += npc.SafeDirectionTo(Main.player[npc.target].Center) * 0.4f;
                                npc.velocity = Vector2.Normalize(npc.velocity) * speed;
                            }
                        }

                        if (++npc.localAI[1] >= 270)
                        {
                            npc.localAI[0]--;
                            npc.localAI[1] = 0;
                        }
                    }
                    break;
                case 2://p2开始的行为
                    if (!AliveCheck(npc, player) || Phase3Check(npc))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int flagY = 0;
                        flagY = Math.Sign(npc.Center.Y - player.Center.Y);
                        Vector2 direct = Vector2.Normalize(player.Center - npc.Center);
                        RotateTowards(npc, player.Center, 0.08f);
                        targetCenter = player.Center + player.SafeDirectionTo(npc.Center) * 400;
                        if (npc.Distance(targetCenter) > 30)
                            Movement(npc, targetCenter, 0.2f);
                        if (npc.localAI[1] % 90 == 45)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(i * MathHelper.PiOver4 / 2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                            }
                        }
                        if (++npc.localAI[1] > 180)
                        {
                            npc.localAI[0]++;
                            npc.localAI[1] = 0;
                        }
                    }
                    break;
                case 3://冲刺连续三次
                    if (!AliveCheck(npc, player) || Phase3Check(npc))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 direct = npc.SafeDirectionTo(player.Center);
                        if (npc.localAI[1] < 180)
                        {
                            if (npc.localAI[1] % 60 == 10)
                            {
                                npc.velocity = ((100f + npc.localAI[1] / 4) * MathHelper.TwoPi / 60f) * direct;
                                ScytheSpawnTimer = 60;
                                for (int i = 0; i < 8; i++)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitX.RotatedBy(i * MathHelper.PiOver4), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                                }
                            }
                            if (npc.localAI[1] % 60 > 10 && npc.localAI[1] % 60 < 40)
                            {
                                npc.rotation = npc.velocity.ToRotation() + angle;
                            }
                            else
                            {
                                RotateTowards(npc, player.Center, 0.4f);
                            }

                        }
                        if (npc.localAI[1] > 150)
                        {
                            npc.alpha = (int)((npc.localAI[1] - 150) * 8.5f);
                            Lighting.AddLight(npc.Center, 0.75f, 1.35f, 1.5f);
                            for (int i = 0; i < 3; i++)
                            {
                                int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                                Main.dust[d].noGravity = true;
                                Main.dust[d].noLight = true;
                                Main.dust[d].velocity *= 4f;
                            }
                        }


                        if (++npc.localAI[1] > 180)
                        {
                            npc.localAI[0]++;
                            npc.localAI[1] = 0;
                        }
                    }
                    break;
                case 4://绕圈
                    if (!AliveCheck(npc, player) || Phase3Check(npc))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (npc.localAI[1] < 85)
                        {
                            npc.alpha = (int)(255 - 3 * npc.localAI[1]);
                            Lighting.AddLight(npc.Center, 0.75f, 1.35f, 1.5f);
                            for (int i = 0; i < 3; i++)
                            {
                                int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                                Main.dust[d].noGravity = true;
                                Main.dust[d].noLight = true;
                                Main.dust[d].velocity *= 4f;
                            }
                        }
                        Direct = npc.SafeDirectionTo(player.Center);
                        targetCenter = player.Center;

                        RotateTowards(npc, Direct.RotatedBy(npc.localAI[1] * MathHelper.TwoPi / 60 + angle), 0.08f);
                        npc.rotation = Direct.ToRotation() - MathHelper.Pi;//抵消了
                        distance = distance < 300 ? 300 : distance;
                        Vector2 vec = targetCenter - (distance + 1.5f) * Direct.RotatedBy(MathHelper.TwoPi / 60);

                        npc.Center = 0.9f * vec + 0.1f * npc.Center;
                        if (npc.localAI[1] % 5 == 0)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(targetCenter - npc.Center), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                        }

                        if (++npc.localAI[1] > 120)
                        {
                            npc.localAI[0] = 2;
                            npc.localAI[1] = 0;
                        }
                    }
                    break;
                case 5://p3开始,连冲
                    if (!AliveCheck(npc, player) || Phase4Check(npc) || Phase5Check(npc))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int max = 16;//分散镰刀数目
                        max = Main.getGoodWorld || Main.zenithWorld ? 20 : 16;
                        npc.velocity *= 0.985f;
                        if (npc.localAI[1] >= 30) //不是时悬停
                        {
                            if (npc.localAI[1] % 30 == 0)
                            {
                                targetCenter = player.Center;
                            }
                            if (npc.localAI[1] % 30 == 20)
                            {
                                npc.velocity = 36 * npc.SafeDirectionTo(targetCenter);
                                ScytheSpawnTimer = 25;
                                npc.velocity.X = Math.Abs(npc.velocity.X) > 25f ? 25 * Math.Sign(npc.velocity.X) : npc.velocity.X;
                                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                                for (int i = 0; i < max; i++)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 3 * Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / max), ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, 0, npc.whoAmI, flag3);
                                }
                            }
                            if (npc.localAI[1] % 30 >= 20 || npc.localAI[1] % 30 < 10)
                            {
                                npc.rotation = npc.velocity.ToRotation() + angle;
                            }
                            else
                            {
                                npc.alpha -= npc.alpha > 6 ? 6 : 0;
                                RotateTowards(npc, player.Center, 0.5f);
                            }
                        }
                        else
                        {
                            RotateTowards(npc, player.Center, 0.2f);
                            targetCenter = player.Center - 300 * Vector2.UnitY;
                            Movement(npc, targetCenter, 0.15f);
                        }
                        npc.velocity += 0.2f * npc.SafeDirectionTo(player.Center);
                        if (++npc.localAI[1] > 270)
                        {
                            npc.localAI[0] = 6;
                            npc.localAI[1] = 0;
                        }
                    }
                    break;
                case 6://环形封锁
                    if (!AliveCheck(npc, player) || Phase4Check(npc) || Phase5Check(npc))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (npc.localAI[1] == 0)
                            flag3 = flag3 == 0 ? 1 : 0;
                        int flagX, flagY = 0;
                        flagX = Math.Sign(npc.Center.X - player.Center.X);
                        flagY = Math.Sign(npc.Center.Y - player.Center.Y);
                        RotateTowards(npc, player.Center, 0.03f);
                        targetCenter = player.Center + 400 * flagY * Vector2.UnitY + flagX * 400 * Vector2.UnitX;
                        Movement(npc, targetCenter, 0.3f);
                        Vector2 Direct = npc.SafeDirectionTo(player.Center);
                        if (npc.localAI[1] == 60)
                        {
                            for (int i = -20; i <= 20; i++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), player.Center + 500 * Direct.RotatedBy(i * MathHelper.Pi / 20), -1.5f * Direct, ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, 0, npc.whoAmI, flag3 == 1 || i % 2 == 0 ? 1 : 0);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), player.Center + 700 * Direct.RotatedBy(i * MathHelper.Pi / 20), -1.5f * Direct, ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, 0, npc.whoAmI, flag3 == 1 || i % 2 == 0 ? 1 : 0);
                                if (Main.getGoodWorld || Main.zenithWorld)
                                {
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), player.Center + 600 * Direct.RotatedBy(i * MathHelper.Pi / 20), -1.5f * Direct, ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, 0, npc.whoAmI, i % 2 != 0 ? 1 : 0);
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), player.Center + 800 * Direct.RotatedBy(i * MathHelper.Pi / 20), -1.5f * Direct, ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, 0, npc.whoAmI, i % 2 != 0 ? 1 : 0);
                                }

                            }
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched, targetCenter);
                        }
                        if (npc.localAI[1] == 120)
                        {
                            Main.bloodMoon = true;
                            npc.localAI[3] = 0;
                        }
                        if (npc.localAI[1] == 270)
                        {
                            Main.bloodMoon = false;
                            npc.localAI[3] = 1;
                        }
                        if (++npc.localAI[1] >= 300)
                        {
                            npc.localAI[0] = 7;
                            npc.localAI[1] = 0;
                            npc.alpha = 0;
                        }
                    }
                    break;
                case 7://虚化冲刺
                    if (!AliveCheck(npc, player) || Phase4Check(npc) || Phase5Check(npc))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int flagX = 0;
                        flagX = Math.Sign(npc.Center.X - player.Center.X);
                        npc.velocity *= npc.localAI[1] < 180 ? 0.985f : 0.99f;
                        if (npc.localAI[1] <= 50)
                        {
                            npc.alpha += npc.localAI[1] <= 50 ? 5 : 0;
                            for (int i = 0; i < 3; i++)
                            {
                                int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                                Main.dust[d].noGravity = true;
                                Main.dust[d].noLight = true;
                                Main.dust[d].velocity *= 4f;
                            }
                        }
                        if (npc.localAI[1] == 50)
                        {
                            npc.Center = player.Center - 1300 * flagX * Vector2.UnitX;
                        }
                        if (npc.localAI[3] == 60)
                        {
                            npc.velocity -= 80 * Vector2.UnitX * flagX;
                            SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                        }
                        ScytheSpawnTimer += npc.localAI[1] == 60 ? 100 : 0;
                        npc.velocity += npc.localAI[1] > 60 && npc.localAI[1] < 180 ? 0.8f * npc.SafeDirectionTo(player.Center) : Vector2.Zero;
                        if (npc.localAI[1] > 60 && npc.localAI[1] <= 110)
                        {
                            npc.alpha -= 5;
                            for (int i = 0; i < 3; i++)
                            {
                                int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                                Main.dust[d].noGravity = true;
                                Main.dust[d].noLight = true;
                                Main.dust[d].velocity *= 4f;
                            }
                        }
                        RotateTowards(npc, player.Center, 0.08f);
                        if (ScytheSpawnTimer > 0 && ScytheSpawnTimer % 3 == 0)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 2 * Vector2.Normalize(npc.velocity.RotatedBy(MathHelper.Pi / 2)), ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, 0, npc.whoAmI, ScytheSpawnTimer % 6 == 0 ? 0 : 1);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 2 * Vector2.Normalize(npc.velocity.RotatedBy(-MathHelper.Pi / 2)), ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, 0, npc.whoAmI, ScytheSpawnTimer % 6 == 0 ? 0 : 1);
                        }
                        if (npc.localAI[1] > 140 && npc.localAI[1] < 230)
                        {
                            Main.bloodMoon = true;
                            npc.localAI[3] = 0;
                        }
                        if (++npc.localAI[1] > 230)
                        {
                            Main.bloodMoon = false;
                            npc.localAI[3] = 1;
                            npc.localAI[0] = 5;
                            npc.localAI[1] = 0;
                        }
                    }
                    break;
                case 8://狂视调律（伪）
                    if (!AliveCheck(npc, player) || Phase5Check(npc))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        npc.velocity *= 0.90f;
                        int flagY;
                        flagY = Math.Sign(npc.Center.Y - player.Center.Y);
                        if (npc.localAI[1] == 20)
                        {
                            Main.bloodMoon = false;
                            npc.localAI[3] = 1;
                        }
                        if (npc.localAI[1] < 60)
                        {
                            omiga = npc.localAI[1] * 0.015f;
                            npc.alpha = 4 * (int)npc.localAI[1];
                        }
                        else if (npc.localAI[1] > 1200)
                        {
                            omiga = (1320 - npc.localAI[1]) * 0.0075f;
                            npc.alpha = 2 * (int)(1320 - npc.localAI[1]);
                        }
                        npc.rotation += omiga;
                        if (npc.localAI[1] < 720)
                        {
                            if (npc.localAI[1] % 120 == 0)
                            {
                                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 1300, (int)npc.Center.Y + flagY * Main.rand.Next(0, 300), ModContent.NPCType<TrueEyeNPC>(), 0, MathHelper.PiOver2, MathHelper.Pi, npc.whoAmI, 1);
                                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 1300, (int)npc.Center.Y + flagY * Main.rand.Next(0, 300), ModContent.NPCType<TrueEyeNPC>(), 0, -MathHelper.PiOver2, MathHelper.Pi, npc.whoAmI, 1);
                                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 1300, (int)npc.Center.Y + flagY * Main.rand.Next(0, 300), ModContent.NPCType<TrueEyeNPC>(), 0, MathHelper.PiOver2, 0, npc.whoAmI, -1);
                                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 1300, (int)npc.Center.Y + flagY * Main.rand.Next(0, 300), ModContent.NPCType<TrueEyeNPC>(), 0, -MathHelper.PiOver2, 0, npc.whoAmI, -1);
                            }
                        }
                        if (npc.localAI[1] < 1320)
                        {
                            if (npc.localAI[1] > 180)
                            {
                                if (npc.localAI[1] % 180 == 120)
                                {
                                    Main.bloodMoon = true;
                                    npc.localAI[3] = 0;
                                }
                                if (npc.localAI[1] % 180 == 20)
                                {
                                    Main.bloodMoon = false;
                                    npc.localAI[3] = 1;
                                }
                            }
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].noLight = true;
                            Main.dust[d].velocity *= 4f;
                        }
                        if (++npc.localAI[1] > 1320)
                        {
                            npc.localAI[1] = 0;
                            npc.localAI[0] = 7;
                            npc.localAI[3] = 1;
                            Main.bloodMoon = false;
                            FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);
                        }
                    }
                    break;
                case 9://原版尾杀
                    if (!AliveCheck(npc, player))
                        break;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        if (++npc.localAI[1] % 10 == 0 && npc.localAI[1] > 68)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Main.rand.NextFloat(2.5f, 3.5f) * Vector2.UnitX.RotatedBy(i * MathHelper.PiOver4 + Main.rand.NextFloat(0, MathHelper.PiOver4)), ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, 0, npc.whoAmI, 1);
                            }
                        }
                        /*
                        if (AITime == 1) 
                        {
                            AITimer = 70;
                        }
                        *//*
                    }
                    break;
            }
            EModeUtils.DropSummon(npc, "SuspiciousEye", NPC.downedBoss1, ref DroppedSummon);
            return true;
        }
        private void InitializeCustomAI(NPC npc)
        {
            npc.ai[0] = 0; //当前攻击case
            npc.ai[1] = 0; // 攻击状态
            npc.ai[2] = 0; // 攻击计时
            npc.localAI[0] = 0;
            npc.localAI[1] = 0;
            npc.localAI[2] = 0;
            npc.localAI[3] = 1;

            npc.TargetClosest();
            if (npc.timeLeft < 30)
                npc.timeLeft = 30;
            ScytheSpawnTimer = 0;

        }

        void FancyFireballs(NPC npc, int repeats)
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
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 6f - 4f * modifier;
            }
        }
        private void Movement(NPC npc, Vector2 targetPos, float speedModifier, bool fastX = true)
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
            if (Math.Abs(npc.velocity.X) > 24)
                npc.velocity.X = 24 * Math.Sign(npc.velocity.X);
            if (Math.Abs(npc.velocity.Y) > 24)
                npc.velocity.Y = 24 * Math.Sign(npc.velocity.Y);
        }
        private void RotateTowards(NPC npc, Vector2 targetPos, float turnSpeed, bool limitRotation = true)
        {
            Vector2 direction = targetPos - npc.Center;
            float targetRotation = direction.ToRotation() - MathHelper.PiOver2; // 克苏鲁之眼需要+90度
            float currentRotation = MathHelper.WrapAngle(npc.rotation);
            float rotationDiff = MathHelper.WrapAngle(targetRotation - currentRotation);
            if (Math.Abs(rotationDiff) > 0.01f)
            {
                if (rotationDiff > 0)
                {
                    npc.rotation += turnSpeed;
                    if (rotationDiff < turnSpeed * 2)
                        npc.rotation = currentRotation + rotationDiff * 0.5f;
                }
                else
                {
                    npc.rotation -= turnSpeed;
                    if (rotationDiff > -turnSpeed * 2)
                        npc.rotation = currentRotation + rotationDiff * 0.5f;
                }
                if (limitRotation)
                {
                    float maxRotationChange = turnSpeed * 3;
                    float actualChange = MathHelper.WrapAngle(npc.rotation - currentRotation);
                    if (Math.Abs(actualChange) > maxRotationChange)
                    {
                        npc.rotation = currentRotation + maxRotationChange * Math.Sign(actualChange);
                    }
                }
            }
            npc.rotation = MathHelper.WrapAngle(npc.rotation);
        }
        private bool Phase2Check(NPC npc)
        {
            /*
            if (npc.localAI[2] >= 1)
                return false;
            *//*
            if (npc.life < npc.lifeMax * 0.8f && Main.expertMode)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    npc.ai[0] = 0;
                    npc.ai[1] = 0;
                    npc.ai[2] = 0;
                    npc.localAI[0] = -1;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 0;//未进入p2
                    npc.netUpdate = true;
                }
                return true;
            }
            return false;
        }
        private bool Phase3Check(NPC npc)
        {
            if (npc.life < npc.lifeMax * 0.6 && Main.expertMode)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    npc.ai[0] = 0;
                    npc.ai[1] = 0;
                    npc.ai[2] = 0;
                    npc.localAI[0] = -2;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 1;//未进入p3
                    npc.netUpdate = true;
                }
                return true;
            }
            return false;
        }
        private bool Phase4Check(NPC npc)
        {
            if (npc.life < npc.lifeMax * 0.16 && Main.expertMode && npc.localAI[2] == 2)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);
                    npc.localAI[0] = 8;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 3;
                    npc.netUpdate = true;
                }
                return true;
            }
            return false;
        }
        private bool Phase5Check(NPC npc)
        {
            if (npc.life < npc.lifeMax * 0.1 && Main.expertMode)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);
                    npc.localAI[0] = 9;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 3;
                    npc.localAI[3] = 0;
                    Main.bloodMoon = true;
                    npc.netUpdate = true;
                }
                return true;
            }
            return false;
        }
        private bool AliveCheck(NPC npc, Player player)
        {
            if (!player.active || player.dead || Vector2.Distance(npc.Center, player.Center) > 5000f || Main.dayTime)
            {
                npc.TargetClosest();
                player = Main.player[npc.target];
                if (!player.active || player.dead || Vector2.Distance(npc.Center, player.Center) > 5000f || Main.dayTime)
                {
                    if (npc.timeLeft > 60)
                        npc.timeLeft = 60;
                    npc.velocity.Y -= 1f;
                    if (npc.timeLeft == 1)
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            NetMessage.SendData(MessageID.WorldData);
                        }
                    }
                    return false;
                }
            }
            return true;
        }
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            /*
            if (WorldSavingSystem.EternityMode && npc.localAI[2] >=1) //aura
            {
                Color outerColor = Color.Blue;
                outerColor.A = 0;

                Color darkColor = outerColor;
                Color mediumColor = Color.Lerp(outerColor, Color.White, 0.75f);
                Color lightColor2 = Color.Lerp(outerColor, Color.White, 0.5f);

                Vector2 auraPos = npc.Center;
                float radius = auraDistance;
                var target = Main.LocalPlayer;
                var blackTile = TextureAssets.MagicPixel;
                var diagonalNoise = FargosTextureRegistry.DottedNoise;
                if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                    return false;
                var maxOpacity = npc.Opacity;

                ManagedShader borderShader = ShaderManager.GetShader("FargowiltasSouls.NatureAuraShader");
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
            }
            *//*
            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);

        }
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);

            binaryWriter.Write7BitEncodedInt(AITimer);
            binaryWriter.Write7BitEncodedInt(ScytheSpawnTimer);
            binaryWriter.Write7BitEncodedInt(FinalPhaseDashCD);
            binaryWriter.Write7BitEncodedInt(FinalPhaseDashStageDuration);
            binaryWriter.Write7BitEncodedInt(FinalPhaseAttackCounter);
            binaryWriter.Write7BitEncodedInt(TeleportDirection);

            bitWriter.WriteBit(IsInFinalPhase);
            bitWriter.WriteBit(FinalPhaseBerserkDashesComplete);
            bitWriter.WriteBit(FinalPhaseDashHorizSpeedSet);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);

            AITimer = binaryReader.Read7BitEncodedInt();
            ScytheSpawnTimer = binaryReader.Read7BitEncodedInt();
            FinalPhaseDashCD = binaryReader.Read7BitEncodedInt();
            FinalPhaseDashStageDuration = binaryReader.Read7BitEncodedInt();
            FinalPhaseAttackCounter = binaryReader.Read7BitEncodedInt();
            TeleportDirection = binaryReader.Read7BitEncodedInt();

            IsInFinalPhase = bitReader.ReadBit();
            FinalPhaseBerserkDashesComplete = bitReader.ReadBit();
            FinalPhaseDashHorizSpeedSet = bitReader.ReadBit();


        }
        public override bool CheckDead(NPC npc)
        {
            if (DeathTimer != -1)
                return true;
            npc.life = 1; // 将生命值设为1防止真正死亡
            npc.active = true;
            DeathTimer++; // 开始死亡计时器
            npc.dontTakeDamage = true; // 无敌状态
            npc.netUpdate = true; // 网络同步

            return false;
        }
        public void DeathAnimation(NPC npc)
        {
            npc.dontTakeDamage = true;
            npc.alpha = 200;
            Particle p;
            float scaleMult;
            int screenshake = 3;
            npc.velocity *= 0.86f; // 水平减速
            Vector2 mutantEyePos = npc.Center + new Vector2(-5f, -12f); // Mutant眼睛位置
            if (DeathTimer == 1)
            {
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
            }
            // 生成灰尘效果
            if (Main.rand.NextBool(5))
            {
                SoundEngine.PlaySound(npc.HitSound, npc.Center);
            }
            bool recolor =  WorldSavingSystem.EternityMode;
            Dust.NewDust(npc.TopLeft, npc.width, npc.height, DustID.t_Slime);

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
                    targetCenter = npc.Center + new Vector2(150f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 300f * (float)Math.Sin(angle));
                    Vector2 targetV = (npc.Center.X - targetCenter.X) * Vector2.UnitX / 1500;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), targetCenter, targetV, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
                for (double i = 0; i < 60; i++)
                {
                    double angle = i * MathHelper.PiOver2 / 15;
                    targetCenter = npc.Center + new Vector2(225f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 450f * (float)Math.Sin(angle));
                    Vector2 targetV = (npc.Center.X - targetCenter.X) * Vector2.UnitX / 1500;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), targetCenter, targetV, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
                for (double i = 0; i < 24; i++)
                {
                    double angle = i * MathHelper.PiOver2 / 6;
                    targetCenter = npc.Center + new Vector2(150f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 75f * (float)Math.Sin(angle));
                    Vector2 targetV = (npc.Center.X - targetCenter.X) * Vector2.UnitX / 1500;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), targetCenter, targetV, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
                Main.bloodMoon = false;
                npc.localAI[3] = 1;
            }
        }
    }
    /*
    public class PHEyeOfCthulhuModSystem : ModSystem
    {
        private delegate bool orig_SafePreAI(NPC npc);
        public override void Load()
        {
            //ApplyOnEdits();
            ApplyILEdits();
        }
        /*
        private void ApplyOnEdits()
        {

            MethodInfo targetMethod = typeof(EyeofCthulhu).GetMethod("SafePreAI", BindingFlags.Static | BindingFlags.Public);
            MonoModHooks.Add(targetMethod, OnEoCFinal);
        }
        private bool OnEoCFinal(orig_SafePreAI orig,NPC npc)
        {
            
            return orig(npc);//调用基方法
        }
     */
    /*
        private void ApplyILEdits()
        {
            // First, get the MethodInfo of the method you want to apply the IL patch to.
            MethodInfo targetMethod = typeof(EyeofCthulhu).GetMethod("SafePreAI", BindingFlags.Instance | BindingFlags.Public);

            // Call MonoModHooks.Modify using the target method and your patch method.
            MonoModHooks.Modify(targetMethod, ILEoCAI);
        }
        private void ILEoCAI(ILContext il)
        {
            ILCursor c = new(il);
            ILCursor d = new(il);
            ILCursor e = new(il);
            ILCursor f = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(20)))
                throw new Exception("IL edit failed!");
            if (!d.TryGotoNext(MoveType.After, i => i.MatchLdcI4(40)))
                throw new Exception("IL edit failed!");
            if (!e.TryGotoNext(MoveType.After, i => i.MatchLdcI4(-100)))
                throw new Exception("IL edit failed!");
            if (!f.TryGotoNext(MoveType.After, i => i.MatchLdcI4(100)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
            d.Emit(OpCodes.Pop);
            d.Emit(OpCodes.Ldc_I4, 70);
            e.Emit(OpCodes.Pop);
            e.Emit(OpCodes.Ldc_I4, -1);
            f.Emit(OpCodes.Pop);
            f.Emit(OpCodes.Ldc_I4, 1);
        }
    }
    */
}