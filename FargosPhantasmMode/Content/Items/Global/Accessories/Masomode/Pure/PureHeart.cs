using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Content.Render;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Pure
{
    public class PureHeartOverride : PModeGlobalMasoItem<PureHeart>
    {
        public override bool IsAssembly => true;
        public override void PHExtraTooltipDraw(DrawableTooltipLine line, ref int yOffset)
            => TextRender.BurnDraw(line, 0.4f, new Vector2(0, -0.5f), Color.Gray, Color.ForestGreen, Color.IndianRed, Color.Purple);
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<GuttedHeartAura>(item);
            player.AddEffect<FlawlessEffect>(item);
        }
    }
    public class FlawlessEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<PureHeartHeader>();
        public override int ToggleItemType => ModContent.ItemType<PureHeart>();

        public override void PostUpdateEquips(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<FlawlessPlayer>();
                if (++modPlayer.FlawlessTimer >= 60 * 15)
                {
                    modPlayer.FlawlessTimer = 60 * 15;
                    player.AddBuff(ModContent.BuffType<FlawlessBuff>(), 2);
                }
            }
        }
    }
}
