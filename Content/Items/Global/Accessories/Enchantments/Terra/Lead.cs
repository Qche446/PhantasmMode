using FargosPhantasmMode.Common;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Terra
{
    public class Lead : PModeGlobalEnchant<LeadEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(LeadEffect.ProcessLeadEffectLifeRegen, StopLeadEffect);
        }
        //在其他地方处理前模式
        private static void StopLeadEffect(Action<Player> orig, Player player)
        {
        }
    }
}
