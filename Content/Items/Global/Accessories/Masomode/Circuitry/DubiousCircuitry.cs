using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Render;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Circuitry
{
    public class DubiousCircuitryOverride : PModeGlobalMasoItem<DubiousCircuitry>
    {
        public override bool IsAssembly => true;
        public override void PHExtraTooltipDraw(DrawableTooltipLine line, ref int yOffset)
        {
            TextRender.BurnDraw(line, 0.2f, new Vector2(0.2f, 0), PhanUtil.MechColor(), PhanUtil.MechColor(80), PhanUtil.MechColor(160), PhanUtil.MechColor(240));
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<FusedLensMechElectricOrbEffect>(item);
            player.AddEffect<ReinforcedPlatingNanoErosionEffect>(item);
        }
    }
}
