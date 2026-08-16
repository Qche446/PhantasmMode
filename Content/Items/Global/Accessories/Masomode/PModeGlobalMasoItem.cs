using FargosPhantasmMode.Content.Render;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Items;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public abstract class PModeGlobalMasoItem<T> : GlobalItem where T : SoulsItem
    {
        public static bool PModeChangeApply => PModeWorldSavingSystem.PhantasmMode;
        public virtual bool IsAssembly { get => false;}
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
                string BaseText = $"Mods.{Mod.Name}.Masomode.{typeof(T).Name}";
                string extraText = BaseText;
                if (IsAssembly)
                {
                    BaseText += ".Base";
                    extraText += ".Extra";
                }
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue(BaseText))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
                if (IsAssembly)
                {
                    var extraLine2 = new TooltipLine(Mod, "PHAddTooltipsExtra", Language.GetTextValue(extraText));
                    tooltips.Add(extraLine2);
                }
                SafeModifyTooltips(item, tooltips);
            }
        }
        public virtual void PHExtraTooltipDraw(DrawableTooltipLine line, ref int yOffset) { }
        public sealed override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (PModeChangeApply && IsAssembly)
            {
                if (line.Name == "PHAddTooltipsExtra")
                {
                    PHExtraTooltipDraw(line, ref yOffset);
                    return false;
                }
            }
            return true;
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
        public sealed override void UpdateInfoAccessory(Item item, Player player)
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
