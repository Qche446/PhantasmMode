using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using FargosPhantasmMode.Assets.ExtraTextures;
using FargosPhantasmMode.Content.Render;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class BionomicClusterOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<BionomicCluster>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.BionomicCluster.Base"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
                var extraLine2 = new TooltipLine(Mod, "PHAddTooltipsExtra", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.BionomicCluster.Extra"));
                tooltips.Add(extraLine2);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                if (line.Name == "PHAddTooltipsExtra")
                {
                    TextRender.BurnDraw(line, 0.4f, new Vector2(0, -0.5f), Color.Gray, Color.ForestGreen, Color.IndianRed, Color.Purple);
                    return false;
                }
            }
            return true;
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                //飞龙之羽
                player.FargoSouls().WyvernBallsCD++;
                player.AddEffect<ClippedWingsAttackEffect>(item);
                //冰霜之握
                player.AddEffect<FrostBurn2AttackEffect>(item);
                //诅咒袋子
                player.AddEffect<ShadowFlameAttackEffect>(item);
                //时之沙
                player.AddEffect<FallingSandsEffect>(item);
                //宁芙香水
                player.FargoSouls().NymphsPerfumeCD -= player.FargoSouls().MasochistSoul ? 10 : 1;
                //蒂姆迷药
                player.manaCost -= 0.05f;
                //神秘头骨
                player.GetDamage(DamageClass.Magic) += 0.05f;
            }  
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
}
