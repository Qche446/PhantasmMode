using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Items;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments
{
    public abstract class PModeGlobalEnchant<T> : GlobalItem where T : SoulsItem
    {
        public static bool PModeChangeApply => PModeWorldSavingSystem.PhantasmMode;
        //public int EnchantID => ModContent.ItemType<T>();
        //public string EnchantName => typeof(T).Name;
        public sealed override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == ModContent.ItemType<T>() && lateInstantiation;
        public virtual void SafeModifyTooltips(Item item, List<TooltipLine> tooltips) { }
        public virtual void SafeUpdateAccessory(Item item, Player player, bool hideVisual) { }
        public virtual void SafeUpdateInPack(Item item, Player player) { }
        public virtual void SafeUpdateVanity(Item item, Player player) { }
        public sealed override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (PModeChangeApply)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue($"Mods.FargosPhantasmMode.Enchantments.{typeof(T).Name}"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
                SafeModifyTooltips(item, tooltips);
            }
        }
        public sealed override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (PModeChangeApply)
            {
                SafeUpdateInPack(item, player);
                SafeUpdateAccessory(item, player, hideVisual);
            }
        }
        public sealed override void UpdateInventory(Item item, Player player)
        {
            if (PModeChangeApply)
            {
                SafeUpdateInPack(item, player);
            }
        }
        public sealed override void UpdateVanity(Item item, Player player)
        {
            if (PModeChangeApply)
            {
                SafeUpdateInPack(item, player);
                SafeUpdateVanity(item, player);
            }
        }
    }
}
