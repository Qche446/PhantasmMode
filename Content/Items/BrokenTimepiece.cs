using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Minions;
using FargowiltasSouls.Content.Items;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Items.Consumables;
using FargowiltasSouls.Content.Items.Materials;
using FargowiltasSouls.Content.Items.Placables;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static FargowiltasSouls.FargoSoulsSets;

namespace FargosPhantasmMode.Content.Items
{
    public class BrokenTimepiece : SoulsItem
    {
        public override bool Eternity => true;
        
        public override List<AccessoryEffect> ActiveSkillTooltips =>
            [AccessoryEffectLoader.GetEffect<TimeTrickKeyEffect>()];
        
        public override void SetStaticDefaults()
        {
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Gray;
            Item.value = Item.sellPrice(0, 0, 47, 0);
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.AddEffect<TimeTrickKeyEffect>(Item);
            player.buffImmune[BuffID.Slow] = true;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
            .AddIngredient(ModContent.ItemType<Masochist>(),1)
            .AddIngredient(ItemID.CopperBar,5)
            .AddIngredient(ItemID.SunplateBlock,5)

            .AddTile(TileID.DemonAltar)
            .Register();
        }
    }
    public class TimeTrickKeyEffect : AccessoryEffect
    {
        public override Header ToggleHeader => null;
        public override bool ActiveSkill => true;
        public override int ToggleItemType => ModContent.ItemType<BrokenTimepiece>();
        public override void ActiveSkillJustPressed(Player player, bool stunned)
        {
            if (stunned)
                return;
            player.AddBuff(ModContent.BuffType<TimeGodsTrickBuff>(), 600);
            SoundEngine.PlaySound(SoundID.Item4, player.Center);
            for (int index1 = 0; index1 < 50; ++index1)
            {
                int index2 = Dust.NewDust(player.position, player.width, player.height, Main.rand.NextBool() ? 107 : 157, 0f, 0f, 0, new Color(), 3f);
                Main.dust[index2].noGravity = true;
                Main.dust[index2].velocity *= 8f;
            }
        }
    }
}
