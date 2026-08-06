using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Bionomic;
using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Chalice;
using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Circuitry;
using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist;
using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Pure;
using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.SupremeDeath;
using FargosPhantasmMode.Content.Render;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Souls
{
    public class MasochistSoulOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<MasochistSoul>() && lateInstantiation;
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.MasochistSoul.Base"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
                var extraLine2 = new TooltipLine(Mod, "PHAddTooltipsExtra", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.MasochistSoul.Extra"));
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
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                //告死
                player.AddEffect<NecroSpinSpeedEffect>(item);

                //电路
                player.AddEffect<FusedLensMechElectricOrbEffect>(item);
                player.AddEffect<ReinforcedPlatingNanoErosionEffect>(item);
                //纯净心
                player.AddEffect<GuttedHeartAura>(item);
                player.AddEffect<FlawlessEffect>(item);
                //生态球
                player.FargoSouls().WyvernBallsCD++;
                player.AddEffect<ClippedWingsAttackEffect>(item);
                player.AddEffect<FrostBurn2AttackEffect>(item);
                player.AddEffect<ShadowFlameAttackEffect>(item);
                player.AddEffect<FallingSandsEffect>(item);
                player.FargoSouls().NymphsPerfumeCD -= player.FargoSouls().MasochistSoul ? 10 : 1;
                player.manaCost -= 0.05f;
                player.GetDamage(DamageClass.Magic) += 0.05f;
                //血肉团
                player.statDefense += 4;
                player.endurance += 0.04f;
                //苍翠恶兆
                player.AddEffect<IvyVenomAttackEffect>(item);
                player.statDefense += 5;
                player.AddEffect<CultistMinionEffect>(item);
                //大师心
                player.AddEffect<OceanicMaulAttackEffect>(item);
                player.wingTimeMax = 999999;
                player.wingTime = player.wingTimeMax;
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                //告死
                player.AddEffect<PlatformFallthroughEffect>(item);
                //大师心
                player.buffImmune[BuffID.VortexDebuff] = true;
            }
        }

        public override void UpdateVanity(Item item, Player player)
        {
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                //告死
                player.AddEffect<PlatformFallthroughEffect>(item);
                //大师心
                player.buffImmune[BuffID.VortexDebuff] = true;
            }  
        }
    }
}
