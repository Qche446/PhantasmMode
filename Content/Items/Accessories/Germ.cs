using FargosPhantasmMode.Content.Buffs.Global;
using FargosPhantasmMode.Content.InfoDisplays;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items;
using FargowiltasSouls.Content.Items.Accessories.Expert;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Accessories
{
    public class Germ : ModItem
    {
        public override void SetStaticDefaults()
        {
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.ItemNoGravity[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.accessory = true;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.sellPrice(0, 1, 0, 0);
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var pp = player.GetModPlayer<PModeBuffPlayer>();
            pp.DotCanDie = true;
            pp.DotCrit = true;
            player.AddEffect<GermDotEffect>(Item);
            player.GetModPlayer<InfoModPlayer>().ShowDotsDps = true;
        }
        public override void UpdateInfoAccessory(Player player)
        {
            player.GetModPlayer<InfoModPlayer>().ShowDotsDps = true;
        }
        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<InfoModPlayer>().ShowDotsDps = true;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
            .AddIngredient<Masochist>(1)
            .AddIngredient<AccursedAnkh>(1)
            .AddIngredient(ItemID.JungleRose)
            .AddIngredient(ItemID.NaturesGift)
            .AddTile(TileID.Bottles)
            .Register();
        }
    }
    public class GermDotEffect : AccessoryEffect
    {
        public override Header ToggleHeader => null;
        public float TheCost = 0;
        public static readonly List<DamageClass> AllDamageClass = [DamageClass.Melee, DamageClass.MeleeNoSpeed, DamageClass.Ranged, DamageClass.Generic, DamageClass.Magic, DamageClass.MagicSummonHybrid, DamageClass.Summon, DamageClass.SummonMeleeSpeed, DamageClass.Throwing];
        public override void PostUpdateMiscEffects(Player player)
        {
            TheCost = player.ActualClassDamage(DamageClass.Generic) - 1;
            player.GetDamage(DamageClass.Generic) -= TheCost;
        }
    }
}
