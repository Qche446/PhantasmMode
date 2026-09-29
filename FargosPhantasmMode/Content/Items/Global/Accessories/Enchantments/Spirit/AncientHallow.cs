using FargosPhantasmMode.Common;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Minions;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using System.Reflection;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit
{
    public class AncientHallow : PModeGlobalEnchant<AncientHallowEnchant>
    {
        public override void Load()
        {
            // HallowSword.MousePos is private without Publicizer; no hook is installed.
            MonoModHooks.Add(typeof(HallowSword).GetMethod("MousePos", BindingFlags.Instance | BindingFlags.NonPublic), HallowSword_MousePos);
        }
        private static Vector2 HallowSword_MousePos(Func<HallowSword, Player, Vector2> orig, HallowSword self, Player player)
        {
            Vector2 result = orig.Invoke(self, player);
            if (PModeChangeApply)
                result = Main.MouseWorld;
            return result;
        }
    }
}
