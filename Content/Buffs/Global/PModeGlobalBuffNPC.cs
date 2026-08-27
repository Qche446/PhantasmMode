using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins;
using FargosPhantasmMode.Content.Items.Accessories;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Life;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Armor;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Buffs.Global
{
    public class PModeGlobalBuffNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public static bool PModeChangeApply => PModeWorldSavingSystem.PhantasmMode;
        public bool DotCrit = false;
        public bool IvyVenom = false;
        public bool Neurotoxin = false;
        public bool Hypothermia = false;
        public bool NanoErosion = false;
        
        public bool Sublimation = false;
        public bool HallowFlame = false;
        public int HallowFlameLevel = 0;

        public float PosionMultiplier = 1f;
        public float FireMultiplier = 1f;
        public float IceMultiplier = 1f;

        public bool HeartBroken = false;
        private bool hasApplied = false;
        private int originalLifeMax = 0;
        public override void Load()
        {
            //跳过原法dot处理内容
            PhanUtil.AddHooks(FargoSoulsGlobalNPC.DoTMultiplier, SkipFargosDotMultiplier);
            //好痛苦...请评价我吧
            On_NPC.UpdateNPC_BuffApplyDOTs += UpdateNPC_BuffApplyDOTs;
        }

        private void UpdateNPC_BuffApplyDOTs(On_NPC.orig_UpdateNPC_BuffApplyDOTs orig, NPC npc)
        {
            void DamageOverTime(int badLifeRegen, bool affectLifeRegenCount = true)
            {
                if (npc.lifeRegen > 0 && affectLifeRegenCount)
                    npc.lifeRegen = 0;

                npc.lifeRegen -= badLifeRegen;
            }
            if (npc.dontTakeDamage)
            {
                return;
            }
            int num = npc.lifeRegenExpectedLossPerSecond;
            if (npc.poisoned)
            {
                DamageOverTime(12);
            }
            if (npc.onFire)
            {
                DamageOverTime(8);
            }
            if (npc.onFire3)
            {
                DamageOverTime(30);
                if (num < 5)
                {
                    num = 5;
                }
            }
            if (npc.onFrostBurn)
            {
                DamageOverTime(16);
                if (num < 2)
                {
                    num = 2;
                }
            }
            if (npc.onFrostBurn2)
            {
                DamageOverTime(50);
                if (num < 10)
                {
                    num = 10;
                }
            }
            if (npc.onFire2)
            {
                DamageOverTime(48);
                if (num < 10)
                {
                    num = 10;
                }
            }
            if (npc.venom)
            {
                DamageOverTime(60);
                if (num < 15)
                {
                    num = 15;
                }
            }
            if (npc.shadowFlame)
            {
                DamageOverTime(30);
                if (num < 5)
                {
                    num = 5;
                }
            }
            if (npc.oiled && (npc.onFire || npc.onFire2 || npc.onFire3 || npc.onFrostBurn || npc.onFrostBurn2 || npc.shadowFlame))
            {
                DamageOverTime(50);
                if (num < 10)
                {
                    num = 10;
                }
            }
            if (npc.javelined)
            {
                int num2 = 0;
                int num3 = 1;
                for (int i = 0; i < 1000; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ProjectileID.BoneJavelin && Main.projectile[i].ai[0] == 1f && Main.projectile[i].ai[1] == npc.whoAmI)
                    {
                        num2++;
                    }
                }
                DamageOverTime(num2 * 2 * 3);
                if (num < num2 * 3 / num3)
                {
                    num = num2 * 3 / num3;
                }
            }
            if (npc.tentacleSpiked)
            {
                int num4 = 0;
                int num5 = 1;
                for (int j = 0; j < 1000; j++)
                {
                    if (Main.projectile[j].active && Main.projectile[j].type == ProjectileID.TentacleSpike && Main.projectile[j].ai[0] == 1f && Main.projectile[j].ai[1] == (float)npc.whoAmI)
                    {
                        num4++;
                    }
                }
                DamageOverTime(num4 * 2 * 3);
                if (num < num4 * 3 / num5)
                {
                    num = num4 * 3 / num5;
                }
            }
            if (npc.bloodButchered)
            {
                int num6 = 0;
                int num7 = 1;
                for (int k = 0; k < 1000; k++)
                {
                    if (Main.projectile[k].active && Main.projectile[k].type == ProjectileID.BloodButcherer && Main.projectile[k].ai[0] == 1f && Main.projectile[k].ai[1] == (float)npc.whoAmI)
                    {
                        num6++;
                    }
                }
                DamageOverTime(num6 * 2 * 4);
                if (num < num6 * 4 / num7)
                {
                    num = num6 * 4 / num7;
                }
            }
            if (npc.daybreak)
            {
                int num8 = 0;
                int num9 = 4;
                for (int l = 0; l < 1000; l++)
                {
                    if (Main.projectile[l].active && Main.projectile[l].type == ProjectileID.Daybreak && Main.projectile[l].ai[0] == 1f && Main.projectile[l].ai[1] == (float)npc.whoAmI)
                    {
                        num8++;
                    }
                }
                if (num8 == 0)
                {
                    num8 = 1;
                }
                DamageOverTime(num8 * 2 * 100);
                if (num < num8 * 100 / num9)
                {
                    num = num8 * 100 / num9;
                }
            }
            if (npc.celled)
            {
                int num10 = 0;
                for (int m = 0; m < 1000; m++)
                {
                    if (Main.projectile[m].active && Main.projectile[m].type == ProjectileID.StardustCellMinionShot && Main.projectile[m].ai[0] == 1f && Main.projectile[m].ai[1] == npc.whoAmI)
                    {
                        num10++;
                    }
                }
                DamageOverTime(num10 * 2 * 20);
                if (num < num10 * 20)
                {
                    num = num10 * 20 / 2;
                }
            }
            if (npc.dryadBane)
            {
                int num11 = 4;
                float num12 = 1f;
                if (NPC.downedBoss1)
                {
                    num12 += 0.1f;
                }
                if (NPC.downedBoss2)
                {
                    num12 += 0.1f;
                }
                if (NPC.downedBoss3)
                {
                    num12 += 0.1f;
                }
                if (NPC.downedQueenBee)
                {
                    num12 += 0.1f;
                }
                if (Main.hardMode)
                {
                    num12 += 0.4f;
                }
                if (NPC.downedMechBoss1)
                {
                    num12 += 0.15f;
                }
                if (NPC.downedMechBoss2)
                {
                    num12 += 0.15f;
                }
                if (NPC.downedMechBoss3)
                {
                    num12 += 0.15f;
                }
                if (NPC.downedPlantBoss)
                {
                    num12 += 0.15f;
                }
                if (NPC.downedGolemBoss)
                {
                    num12 += 0.15f;
                }
                if (NPC.downedAncientCultist)
                {
                    num12 += 0.15f;
                }
                if (Main.expertMode)
                {
                    num12 *= Main.GameModeInfo.TownNPCDamageMultiplier;
                }
                num11 = (int)((float)num11 * num12);
                DamageOverTime(2 * num11);
                if (num < num11)
                {
                    num = num11 / 3;
                }
            }
            if (npc.soulDrain && npc.realLife == -1)
            {
                DamageOverTime(50);
                if (num < 5)
                {
                    num = 5;
                }
            }
            NPCLoader.UpdateLifeRegen(npc, ref num);
            Player py = Main.LocalPlayer;
            var pp = py.GetModPlayer<PModeBuffPlayer>();
            float dotMultiplier = DoTMultiplier(npc, py);
            if (dotMultiplier != 1 && npc.lifeRegen < 0)
            {
                npc.lifeRegen = (int)(npc.lifeRegen * dotMultiplier);
                num = (int)(num * dotMultiplier);
            }
            if (npc.lifeRegen <= -240 && num < 2)
            {
                num = 2;
            }
            float speed = npc.lifeRegen;
            if (py.HasEffect<GermDotEffect>() && npc.lifeRegen < 0)
                speed *= 1 + 0.5f * (py.FargoSouls().AttackSpeed - 1);
            npc.lifeRegenCount += (int)speed;
            while (npc.lifeRegenCount >= 120)
            {
                npc.lifeRegenCount -= 120;
                if (!npc.immortal)
                {
                    if (npc.life < npc.lifeMax)
                    {
                        npc.life++;
                    }
                    if (npc.life > npc.lifeMax)
                    {
                        npc.life = npc.lifeMax;
                    }
                }
            }
            if (num > 0)
            {
                while (npc.lifeRegenCount <= -120 * num)
                {
                    npc.lifeRegenCount += 120 * num;
                    int num13 = npc.whoAmI;
                    if (npc.realLife >= 0)
                    {
                        num13 = npc.realLife;
                    }
                    bool docrit = pp.DotCrit && PhanUtil.FloatBool(Main.LocalPlayer.ActualClassCrit(DamageClass.Generic) / 100f);
                    int muti = docrit ? 2 : 1;
                    Color textcolor = docrit ? CombatText.DamagedFriendlyCrit : CombatText.LifeRegenNegative;
                    if (!Main.npc[num13].immortal)
                    {
                        Main.npc[num13].life -= num * muti;
                    }
                    CombatText.NewText(new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height), textcolor, num * muti, dramatic: false, dot: true);
                    if (Main.npc[num13].life > 0 || Main.npc[num13].immortal)
                    {
                        continue;
                    }
                    Main.npc[num13].life = 1;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        //Main.npc[num13].StrikeNPCNoInteraction(9999, 0f, 0);
                        NPC.HitInfo hit = new()
                        {
                            Crit = false,
                            Knockback = 0f,
                            HitDirection = 0,
                            InstantKill = true,
                        };
                        object[] arg = [hit];
                        PhanUtil.SetLegacyStrike.Invoke(null, arg);
                        Main.npc[num13].StrikeNPC(hit, false, true);
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, num13, 9999f);
                        }
                    }
                }
                return;
            }
            while (npc.lifeRegenCount <= -120)
            {
                npc.lifeRegenCount += 120;
                int num14 = npc.whoAmI;
                if (npc.realLife >= 0)
                {
                    num14 = npc.realLife;
                }
                bool docrit = pp.DotCrit && PhanUtil.FloatBool(Main.LocalPlayer.ActualClassCrit(DamageClass.Generic) / 100f);
                int muti = docrit ? 2 : 1;
                Color textcolor = docrit ? CombatText.DamagedFriendlyCrit : CombatText.LifeRegenNegative;
                if (!Main.npc[num14].immortal)
                {
                    Main.npc[num14].life -= muti;
                }
                CombatText.NewText(new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height), textcolor, muti, dramatic: false, dot: true);
                if (Main.npc[num14].life > 0 || Main.npc[num14].immortal)
                {
                    continue;
                }
                Main.npc[num14].life = 1;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.HitInfo hit = new()
                    {
                        Crit = false,
                        Knockback = 0f,
                        HitDirection = 0,
                        InstantKill = true,
                    };
                    object[] arg = [hit];
                    PhanUtil.SetLegacyStrike.Invoke(null, arg);
                    Main.npc[num14].StrikeNPC(hit, false, true);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, num14, 9999f);
                    }
                }
            }
        }

        public static float SkipFargosDotMultiplier(Func<NPC, Player, float> orig, NPC npc, Player player) => 1f;
        public override void ResetEffects(NPC npc)
        {
            DotCrit = false;
            IvyVenom = false;
            Neurotoxin = false;
            Hypothermia = false;
            NanoErosion = false;
            Sublimation = false;
            if (!HallowFlame)
                HallowFlameLevel = 0;
            else if (HallowFlameLevel < 1)
                HallowFlameLevel = 1;
            HallowFlame = false;

            PosionMultiplier = 1f;
            FireMultiplier = 1f;
            IceMultiplier = 1f;

            float healthPrecentage = npc.GetLifePercent();
            if (HeartBroken)
            {
                if (!hasApplied)
                {
                    originalLifeMax = npc.lifeMax;
                    int reduction = (int)(npc.lifeMax * 0.15f);
                    if (reduction < 1) reduction = 1;
                    npc.lifeMax -= reduction;

                    npc.life = (int)(healthPrecentage * npc.lifeMax);
                    hasApplied = true;
                }
            }
            else
            {
                if (hasApplied)
                {
                    npc.lifeMax = originalLifeMax;

                    npc.life = (int)(healthPrecentage * npc.lifeMax);

                    hasApplied = false;
                    originalLifeMax = 0;
                }
            }
            HeartBroken = false;
        }
        public override void AI(NPC npc)
        {
            var fn = npc.FargoSouls();
            if (fn.Infested)
                PosionMultiplier += 0.1f;
            if (npc.poisoned)
                PosionMultiplier += 0.1f;
            if (fn.LeadPoison)
                PosionMultiplier += 0.1f;
            if (fn.OriPoison)
                PosionMultiplier += 0.1f;
            if (npc.venom)
                PosionMultiplier += 0.15f;
            if (IvyVenom)
                PosionMultiplier += 0.1f;
            if (Neurotoxin)
                PosionMultiplier += 0.2f;
            if (fn.Rotting)
                PosionMultiplier += 0.15f;
            if (npc.onFire)
                FireMultiplier += 0.1f;
            if (npc.onFire2)
                FireMultiplier += 0.2f;
            if (npc.onFire3)
                FireMultiplier += 0.12f;
            if (npc.shadowFlame)
                FireMultiplier += 0.12f;
            if (npc.betsysCurse)
                FireMultiplier += 0.1f;
            if (npc.daybreak)
                FireMultiplier += 0.15f;
            if (fn.FlamesoftheUniverse)
                FireMultiplier += 0.2f;
            if (fn.SolarFlare)
                FireMultiplier += 0.2f;
            if (npc.onFrostBurn)
                IceMultiplier += 0.15f;
            if (npc.onFrostBurn2)
                IceMultiplier += 0.15f;
            if (Hypothermia)
                IceMultiplier += 0.2f;
            if (fn.TimeFrozen)
                IceMultiplier += 0.2f;
        }
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            //FargoSoulsPlayer fp= py.FargoSouls();
            FargoSoulsGlobalNPC fgn = npc.FargoSouls();
            void DamageOverTime(int badLifeRegen, bool affectLifeRegenCount = true)
            {
                if (npc.lifeRegen > 0 && affectLifeRegenCount)
                    npc.lifeRegen = 0;

                npc.lifeRegen -= badLifeRegen;
            }
            if (IvyVenom)//常春藤15dps
            {
                DamageOverTime(30);
                if (damage < 3)
                    damage = 3;
            }
            if (Neurotoxin)//神经160dps
            {
                DamageOverTime(320);
                if (damage < 32)
                    damage = 32;
            }
            if (Hypothermia)//失温200dps
            {
                DamageOverTime(400);
                if (damage < 40)
                    damage = 40;
            }
            if (fgn.OceanicMaul)//海洋重击100dps
            {
                DamageOverTime(200);
                if (damage < 20)
                    damage = 20;
            }
            if (Sublimation)//升华25dps
            {
                DamageOverTime(50);
                if (damage < 5)
                    damage = 5;
            }
            if (HallowFlame)//圣炎 20 * level dps
            {
                int a = Main.LocalPlayer.ForceEffect<HallowFlameEffect>() ? 8 : 4;
                DamageOverTime(a * 10 * HallowFlameLevel);
                if (damage < a * HallowFlameLevel)
                    damage = a * HallowFlameLevel;
            }
        }
        public static float DoTMultiplier(NPC npc, Player player)
        {
            float multiplier = 1;
            bool hasNanoErosion = npc.GetGlobalNPC<PModeGlobalBuffNPC>().NanoErosion;
            bool hasHypothermia = npc.GetGlobalNPC<PModeGlobalBuffNPC>().Hypothermia;
            if (player.HasEffect<OrichalcumEffect>())
                multiplier += OrichalcumEffect.OriDotModifier(npc, player.FargoSouls()) - 1;
            if (PModeChangeApply && player.ForceEffect<OrichalcumEffect>())
                multiplier += 0.5f;

            if (npc.FargoSouls().MagicalCurse)
            {
                if (hasNanoErosion || hasHypothermia)
                {
                    multiplier *= 2;
                }
                else
                {
                    multiplier += 1;
                }
            }
            if (npc.daybreak && multiplier > 1 && (!hasNanoErosion) && (!hasHypothermia))
                multiplier -= (multiplier - 1) / 2;
            multiplier *= hasNanoErosion ? 1.2f : 1f;
            multiplier *= hasHypothermia ? 1.05f : 1;
            if (player.HasEffect<GermDotEffect>())
            {
                multiplier *= ModContent.GetInstance<GermDotEffect>().TheCost + 1;
            }
            return multiplier;
        }
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (Sublimation)
            {
                if (Main.rand.NextBool(4))
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.PortalBolt, npc.velocity.X * 0.4f, npc.velocity.Y * 0.4f, 0, new Color(220, 255, 220), 2.5f);
                    Main.dust[d].velocity.Y -= 1;
                    Main.dust[d].velocity *= 1.5f;
                    Main.dust[d].noGravity = true;
                }
            }
            if (HallowFlame)
            {
                for (int i = 0; i < MathHelper.Min(HallowFlameLevel, 4); i++)
                {
                    if (Main.rand.NextBool(4))
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.HallowedTorch, npc.velocity.X * 0.4f, npc.velocity.Y * 0.4f, 0, new Color(220, 255, 220), 2.5f);
                        Main.dust[d].velocity.Y -= 1;
                        Main.dust[d].velocity *= 1.5f;
                        Main.dust[d].noGravity = true;
                    }
                }
            }
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.GetGlobalNPC<FargoSoulsGlobalNPC>().OceanicMaul)
                modifiers.Defense.Flat -= 20;
            if (Sublimation)
                modifiers.Defense.Flat -= 15;
            if (HallowFlame)
            {
                modifiers.Defense.Flat -= 4 * HallowFlameLevel;
                float a = Main.LocalPlayer.FargoSouls().MutantPresence ? 0.005f : 0.01f;
                modifiers.FinalDamage *= 1f + a * HallowFlameLevel;
            }
            if (Hypothermia)
            {
                modifiers.FinalDamage *= 1.05f;
            }
        }
        public override void OnKill(NPC npc)
        {
            hasApplied = false;
            originalLifeMax = 0;
        }
    }
}
