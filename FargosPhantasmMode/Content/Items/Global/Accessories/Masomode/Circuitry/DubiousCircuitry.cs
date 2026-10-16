using FargosPhantasmMode.Content.Rarities;
using FargosPhantasmMode.Content.Render;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Circuitry
{
    public class DubiousCircuitryOverride : PModeGlobalMasoItem<DubiousCircuitry>
    {
        public override bool IsAssembly => true;
        public override void SetDefaults(Item entity)
        {
            entity.rare = ModContent.RarityType<MechRarity>();
        }
        public override void PHExtraTooltipDraw(DrawableTooltipLine line, ref int yOffset)
        {
            MechTextRender.Draw(line, MechTextRender.ColorRed, MechTextRender.ColorGreen, MechTextRender.ColorYellow, MechTextRender.ColorBlue);
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<FusedLensMechElectricOrbEffect>(item);
            player.AddEffect<ReinforcedPlatingNanoErosionEffect>(item);
        }
    }
}
