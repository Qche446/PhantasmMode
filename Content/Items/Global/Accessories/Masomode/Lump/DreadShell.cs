using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Lump
{
    public class DreadShellOverride : PModeGlobalMasoItem<DreadShell>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.statDefense += 5;
            player.endurance += 0.05f;
        }
    }
}
