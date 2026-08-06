using FargosPhantasmMode.Common;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Minions;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit
{
    public class AncientHallow : PModeGlobalEnchant<AncientHallowEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<HallowSword>().MousePos, HallowSword_MousePos);
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
