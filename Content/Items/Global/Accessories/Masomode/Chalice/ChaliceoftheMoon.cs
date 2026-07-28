using FargosPhantasmMode.Content.Render;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Chalice
{
    public class ChaliceoftheMoonOverride : PModeGlobalMasoItem<ChaliceoftheMoon>
    {
        public override bool IsAssembly => true;
        public override void PHExtraTooltipDraw(DrawableTooltipLine line, ref int yOffset)
        {
            TextRender.BurnDraw(line, 0.4f, new Vector2(0, -1f), Color.Gray, Color.Cornsilk, Color.DarkBlue, Color.Silver);
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            //魔法球茎
            player.AddEffect<IvyVenomAttackEffect>(item);
            //蜥蜴盒
            player.statDefense += 5;
            //天界符文
            player.AddEffect<CultistMinionEffect>(item);
        }
    }
}
