using FargosPhantasmMode.Common;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Souls;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class Monk : PModeGlobalEnchant<MonkEnchant>
    {
        public override void Load()
        {
            //PhanUtil.AddHooks(MonkDashEffect.MonkDash, MonoDashEnhance);
        }
        private static void MonoDashEnhance(Action<Player, int> orig, Player player, int direction)
        {
            orig.Invoke(player, direction);
        }
    }
}
