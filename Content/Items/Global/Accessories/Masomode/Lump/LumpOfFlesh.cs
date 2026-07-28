using FargosPhantasmMode.Content.Render;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Lump
{
    public class LumpOfFleshOverride : PModeGlobalMasoItem<LumpOfFlesh>
    {
        public override bool IsAssembly => true;
        public override void PHExtraTooltipDraw(DrawableTooltipLine line, ref int yOffset)
        {
            TextRender.BurnDraw(line, 0.1f, new Vector2(0, 0.2f), Color.Gray, Color.Red, Color.Blue, Color.GhostWhite);
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.statDefense += 4;
            player.endurance += 0.04f;
        }
    }
}
