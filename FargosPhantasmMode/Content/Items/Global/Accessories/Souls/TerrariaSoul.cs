using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Earth;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Life;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Will;
using FargosPhantasmMode.Content.Render;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Items.Accessories.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Souls
{
    public class TerrariaSoulOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<TerrariaSoul>() && lateInstantiation;
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Enchantments.TerrariaSoul.Base"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
                var extraLine2 = new TooltipLine(Mod, "PHAddTooltipsExtra", Language.GetTextValue("Mods.FargosPhantasmMode.Enchantments.TerrariaSoul.Extra"));
                tooltips.Add(extraLine2);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                if (line.Name == "PHAddTooltipsExtra")
                {
                    TextRender.BurnDraw(line, 0.4f, new Vector2(0, -0.5f), Color.GhostWhite, Color.Red, Color.IndianRed, Color.White);
                    return false;
                }
            }
            return true;
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (!PModeWorldSavingSystem.PhantasmMode)
                return;
            var fp = player.FargoSouls();
            TerraInPack(item, player);
            //大地
            player.AddEffect<AdamantiteProjSplit>(item);
            //player.AddEffect<CobaltJumpEnhance>(item);
            Earth.ReduceEarthMyTimer(player);
            //自然
            player.AddEffect<JungleEnhanceEffect>(item);
            player.AddEffect<MoltenBombEffect>(item);
            player.AddEffect<ShroomiteEffect>(item);
            player.AddEffect<NatureTrailEffect>(item);
            Crimson.CrimsonRevenge(player);
            //暗影
            fp.AncientShadowEnchantActive = true;
            player.AddEffect<DarkArtistMinion>(item);
            player.AddEffect<DarkArtistEffect>(item);
            player.AddEffect<NinjaAttackSpeedEffect>(item);
            //心灵
            AncientHallowEnchant.AddEffects(player, item);
            player.AddEffect<SpectreAttackEffect>(item);
            player.AddEffect<SpectreOnHitEffect>(item);
            player.AddEffect<HallowFlameEffect>(item);
            player.AddEffect<TikiMinLimitEffect>(item);
            //泰拉
            if (player.HasEffect<TerraLightningEffect>() && fp.TerraProcCD > 0)
                fp.TerraProcCD--;
            //森林
            //意志
            player.AddEffect<WillJavelinEffect>(item);
        }
        public static void TerraInPack(Item item, Player player)
        {
            if (!PModeWorldSavingSystem.PhantasmMode)
                return;
            player.AddEffect<PumpkinFeedEffect>(item);
        }
        public override void UpdateVanity(Item item, Player player)
        {
            TerraInPack(item, player);
        }
        public override void UpdateInventory(Item item, Player player)
        {
            TerraInPack(item, player);
        }
    }
}
