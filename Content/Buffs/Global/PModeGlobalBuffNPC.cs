using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
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
        }
        public static float SkipFargosDotMultiplier(Func<NPC, Player, float> orig, NPC npc, Player player) => 1f;
        public override void ResetEffects(NPC npc)
        {
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
            Player py = Main.LocalPlayer;
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
            float dotMultiplier = DoTMultiplier(npc, py);
            if (dotMultiplier != 1 && npc.lifeRegen < 0)
            {
                npc.lifeRegen = (int)(npc.lifeRegen * dotMultiplier);
                damage = (int)(damage * dotMultiplier);
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
                float a = Main.LocalPlayer.FargoSouls().MutantPresence ? 0.008f : 0.02f;
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
