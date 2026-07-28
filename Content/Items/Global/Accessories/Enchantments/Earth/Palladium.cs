using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using System;
using System.Reflection;
using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using Luminance.Common.Utilities;
using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Earth
{
    public class Palladium : PModeGlobalEnchant<PalladiumEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<PalladiumHealing>().OnHitNPCEither, OnHitNPCEitherFixed);
        }
        public static void OnHitNPCEitherFixed(Action<PalladiumHealing, Player, NPC, NPC.HitInfo, DamageClass, int, Projectile, Item> orig, PalladiumHealing self, Player player, NPC target, NPC.HitInfo hitInfo, DamageClass damageClass, int baseDamage, Projectile projectile, Item item)
        {
            if (!self.HasEffectEnchant(player) && !PModeChangeApply)
                return;

            if (!player.onHitRegen)
            {
                player.AddBuff(BuffID.RapidHealing, Utilities.SecondsToFrames(3)); //heal time based on damage dealt, capped at 5sec
            }
        }
    }
}
