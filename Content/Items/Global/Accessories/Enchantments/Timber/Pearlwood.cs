using FargosPhantasmMode.Common;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using System;
using Terraria;
using FargowiltasSouls.Core.AccessoryEffectSystem;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Timber
{
    public class Pearlwood : PModeGlobalEnchant<PearlwoodEnchant>
    {
        public override void Load()
        {
            //PhanUtil.AddHooks(PearlwoodEffect.PearlwoodCritReroll, PearlwoodDamageVariationReroll);
            On_NPC.HitModifiers.GetDamage += HitModifiers_GetDamageReRoll;
        }
        public override void Unload()
        {
            On_NPC.HitModifiers.GetDamage -= HitModifiers_GetDamageReRoll;
        }
        private int HitModifiers_GetDamageReRoll(On_NPC.HitModifiers.orig_GetDamage orig, ref NPC.HitModifiers self, float baseDamage, bool crit, bool damageVariation, float luck)
        {
            crit = self._critOverride ?? crit;
            if (self.SuperArmor)
            {
                float dmg = 1f;
                if (crit)
                {
                    dmg *= self.CritDamage.Additive * self.CritDamage.Multiplicative;
                }
                return Math.Clamp((int)dmg, 1, Math.Min(self._damageLimit, 4));
            }
            float damage = self.SourceDamage.ApplyTo(baseDamage);
            damage += self.FlatBonusDamage.Value + self.ScalingBonusDamage.Value * damage;
            damage *= self.TargetDamageMultiplier.Value;
            int variationPercent = Utils.Clamp((int)Math.Round((float)Main.DefaultDamageVariationPercent * self.DamageVariationScale.Value), 0, 100);
            if (damageVariation && variationPercent > 0)
            {
                if (PModeChangeApply && Main.LocalPlayer.HasEffect<PearlwoodEffect>())
                {
                    int rerolls = Main.LocalPlayer.ForceEffect<PearlwoodEffect>() ? 2 : 1;
                    float bestdamage = Main.DamageVar(damage, variationPercent, luck);
                    for (int i = 0; i < rerolls; i++)
                    {
                        float insda = PhanUtil.ApplyVariance(damage, variationPercent);
                        bestdamage = Math.Max(bestdamage, insda);
                    }
                    damage = bestdamage;
                }
                else
                    damage = Main.DamageVar(damage, variationPercent, luck);
            }

            float num = Math.Max(self.Defense.ApplyTo(0f), 0f);
            float armorPenetration = num * Math.Clamp(self.ScalingArmorPenetration.Value, 0f, 1f) + self.ArmorPenetration.Value;
            float damageReduction = Math.Max(num - armorPenetration, 0f) * self.DefenseEffectiveness.Value;
            damage = Math.Max(damage - damageReduction, 1f);
            damage = (crit ? self.CritDamage : self.NonCritDamage).ApplyTo(damage);
            return Math.Clamp((int)self.FinalDamage.ApplyTo(damage), 1, self._damageLimit);
        }
    }
}
