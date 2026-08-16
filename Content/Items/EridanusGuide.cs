using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items;
using FargowiltasSouls.Content.Items.Armor;
using FargowiltasSouls.Content.Items.Materials;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items
{
    public class EridanusGuide : SoulsItem
    {
        public override string Texture => "FargowiltasSouls/Content/Items/Materials/Eridanium";
        int drawTimer = 0;
        public override void SetStaticDefaults()
        {
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.ItemNoGravity[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.accessory = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Gray;
            Item.value = Item.sellPrice(0, 10, 0, 0);
        }
        public override void UpdateInfoAccessory(Player player)
        {
            player.AddEffect<EridanusGuideEffect>(Item);
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.AddEffect<EridanusGuideEffect>(Item);
        }
        public override void UpdateVanity(Player player)
        {
            player.AddEffect<EridanusGuideEffect>(Item);
        }
        public override void AddRecipes()
        {
            CreateRecipe()
            .AddIngredient(ModContent.ItemType<Eridanium>(), 5)
            .AddIngredient(ItemID.FragmentSolar, 1)
            .AddIngredient(ItemID.FragmentVortex, 1)
            .AddIngredient(ItemID.FragmentNebula, 1)
            .AddIngredient(ItemID.FragmentStardust, 1)
            .AddTile(ModContent.Find<ModTile>("Fargowiltas", "CrucibleCosmosSheet"))
            .Register();
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            PhanUtil.DrawItemGlow(Item, position, PhanUtil.CosmoColor(), Color.White, ref drawTimer);
            drawTimer++;
            return true;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            PhanUtil.DrawItemGlow(Item, Item.Center - Main.screenPosition, PhanUtil.CosmoColor(), Color.White, ref drawTimer);
            drawTimer++;
            return true;
        }
    }
    public class EridanusGuideEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<CosmoHeader>();
        public override int ToggleItemType => ModContent.ItemType<EridanusGuide>();
        public override void PostUpdateEquips(Player player)
        {
            var fp = player.FargoSouls();
            if (!fp.EridanusEmpower || !Main.mouseItem.IsAir)
                return;
            int style = fp.EridanusTimer / EridanusHat.ClassDuration;
            if (fp.EridanusTimer % EridanusHat.ClassDuration == 5)
            {
                int index = player.selectedItem;
                for (int i = 0; i < 11; i++)
                {
                    int endindex = index + i;
                    if (endindex >= 10)
                        endindex -= 10;
                    DamageClass damageClass = style switch
                    {
                        1 => DamageClass.Ranged,
                        2 => DamageClass.Magic,
                        3 => DamageClass.Summon,
                        _ => DamageClass.Melee
                    };
                    Item targetitem = player.inventory[endindex];
                    bool isTargetclass = style switch
                    {
                        0 => targetitem.DamageType == DamageClass.Melee || targetitem.DamageType == DamageClass.MeleeNoSpeed,
                        3 => targetitem.DamageType == DamageClass.SummonMeleeSpeed || targetitem.DamageType == DamageClass.Summon,
                        _ => targetitem.DamageType == damageClass
                    };
                    if (isTargetclass)
                    {
                        index = endindex;
                        break;
                    }
                }
                //Main.NewText(style.ToString() + player.inventory[index].Name);
                player.selectedItem = index;
            }
        }
    }
}
