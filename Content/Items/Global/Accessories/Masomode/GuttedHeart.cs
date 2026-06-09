using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls;
using FargosPhantasmMode.Content.Projectiles;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class GuttedHeartOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<GuttedHeart>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.GuttedHeart"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (item.type == ModContent.ItemType<GuttedHeart>() && WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<GuttedHeartAura>(item);
            }  
            base.UpdateAccessory(item, player, hideVisual);
        }

    }
    public class GuttedHeartAura : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<PureHeartHeader>();
        public override int ToggleItemType => ModContent.ItemType<GuttedHeart>();
        public override bool ExtraAttackEffect => true;
        public float Timer = 0;
        bool flag = true;
        public override void PostUpdateEquips(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                bool pure = player.FargoSouls().PureHeart;
                int visualProj = ModContent.ProjectileType<GuttedHeartAuraProj>();
                
                if (ModContent.GetInstance<GuttedHeartAura>().Timer >= 60 && flag)
                {
                    Projectile.NewProjectile(GetSource_EffectItem(player), player.Center, Vector2.Zero, visualProj, 1, 0, Main.myPlayer, ai2: pure ? 16 : 12);
                    flag = false;
                }
                if (!pure)
                    Lighting.AddLight((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f), 0.65f, 0.4f, 0.1f);
                if (++ModContent.GetInstance<GuttedHeartAura>().Timer >= (player.FargoSouls().PureHeart ? 180 : 240))
                {
                    flag = true;
                    ModContent.GetInstance<GuttedHeartAura>().Timer = 0;
                }
            }
        }

    }
}
