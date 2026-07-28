using FargosPhantasmMode.Content.Render;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.SupremeDeath
{
    public class SupremeDeathbringerFairyOverride : PModeGlobalMasoItem<SupremeDeathbringerFairy>
    {
        public override bool IsAssembly => true;
        public override void PHExtraTooltipDraw(DrawableTooltipLine line, ref int yOffset)
        {
            TextRender.BurnDraw(line, 0.2f, new Vector2(0, -0.5f), Color.Gray, Color.Blue, Color.Aqua, Color.Purple);
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            //躁动晶状体
            player.moveSpeed += player.statLife <= player.statLifeMax2 / 2f ? 0.3f : 0.1f;
            //蜂后毒刺
            player.AddBuff(BuffID.Honey, 2);
            //player.honey = true;
            //死灵密酿
            player.AddEffect<NecroSpinSpeedEffect>(item);
        }
        public override void SafeUpdateInPack(Item item, Player player)
        {
            player.AddEffect<PlatformFallthroughEffect>(item);
            player.AddBuff(BuffID.Honey, 2);
        }
    }
}
