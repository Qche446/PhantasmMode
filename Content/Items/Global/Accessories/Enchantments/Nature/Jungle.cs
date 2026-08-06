using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature
{
    public class Jungle : PModeGlobalEnchant<JungleEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<JungleEnhanceEffect>(item);
        }
    }
    public class JungleEnhanceEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<NatureHeader>();
        public override int ToggleItemType => ModContent.ItemType<JungleEnchant>();
        
    }
    public class JungleEnhanceGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;
        int drawTimer = 0;
        public static bool Apply => Main.LocalPlayer.HasEffect<JungleEnhanceEffect>();
        public static bool HasEnhance => Main.LocalPlayer.ForceEffect<JungleEnhanceEffect>() || Main.LocalPlayer.FargoSouls().ChlorophyteEnchantActive;
        public static bool HasNature => Main.LocalPlayer.HasEffect<NatureEffect>();
        public static List<int> JungleItem => [
            ItemID.JungleHat, ItemID.JungleShirt, ItemID.JunglePants,//套装
            ItemID.ThornChakram,//荆棘旋刃
            ItemID.IvyWhip,//荆棘钩爪
            ItemID.BladeofGrass,//草剑
            //ItemID.JungleRose,//丛林玫瑰
            ItemID.JungleYoyo,//亚马逊悠悠球
            ItemID.ThornWhip,//荆鞭
            ItemID.PoisonDart//毒镖
            ];
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!Apply) return;
            switch (item.type)
            {
                case ItemID.BladeofGrass:
                    Addtooltips(tooltips, "BladeofGrass", HasEnhance ? 3f : 1.2f, HasNature ? 16 : HasEnhance ? 2.4f : 1.1f);
                    break;
                case ItemID.ThornChakram:
                    Addtooltips(tooltips, "ThornChakram", HasNature ? 20 : HasEnhance ? 2.6f : 1.1f);
                    break;
                case ItemID.IvyWhip:
                    Addtooltips(tooltips, "IvyWhip"/*, HasEnhance ? 1.6f : 1.3f*/);
                    break;
                case ItemID.JungleYoyo:
                    Addtooltips(tooltips, "JungleYoyo", HasEnhance ? 2.4f : 1.1f);
                    break;
                case ItemID.ThornWhip:
                    Addtooltips(tooltips, "ThornWhip", HasNature ? 18 : HasEnhance ? 2.2f : 1.1f, HasEnhance ? 2.5f : 1.5f);
                    break;
                case ItemID.PoisonDart:
                    Addtooltips(tooltips, "PoisonDart", HasEnhance ? 1.8f : 1.2f, HasEnhance ? 2 : 1);
                    break;
                default: break;
            }
        }
        public void Addtooltips(List<TooltipLine> tooltips, string str, params object[] args)
        {
            var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue($"Mods.FargosPhantasmMode.SpecialEnhance.Jungle.{str}", args))
            {
                OverrideColor = Color.LightGreen
            };
            tooltips.Add(extraLine);
        }
        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (head.type == ItemID.JungleHat && body.type == ItemID.JungleShirt && legs.type == ItemID.JunglePants)
            {
                return "SporeCloudShoot";
            }
            return base.IsArmorSet(head, body, legs);
        }
        public override void UpdateArmorSet(Player player, string set)
        {
            if (!Apply) return;
            player.setBonus = Language.GetTextValue("Mods.FargosPhantasmMode.Armor.Jungle");
            player.GetModPlayer<NaturePlayer>().HasSporeCloudShoot = true;
            player.manaCost -= 0.14f;
            if (player.GetModPlayer<NaturePlayer>().SporeCloudCD < 20)
                player.GetModPlayer<NaturePlayer>().SporeCloudCD++;
            base.UpdateArmorSet(player, set);
        }
        public override void ModifyItemScale(Item item, Player player, ref float scale)
        {
            if (!Apply) return;
            if (item.type == ItemID.BladeofGrass)
            {
                scale *= HasEnhance ? 3f : 1.2f;
            }
        }
        public override bool? CanHitNPC(Item item, Player player, NPC target)
        {
            if (!Apply) return base.CanHitNPC(item, player, target);
            if (JungleItem.Contains(item.type) && item.IsWeapon())
            {
                item.GetGlobalItem<PModeGlobalItem>().PoisonAttribute = true;
            }
            return base.CanHitNPC(item, player, target);
        }
        public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (!Apply) return;
            if (item.type == ItemID.ThornChakram)
            {
                velocity *= HasEnhance ? 1.6f : 1.1f;
            }
        }
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (!Apply) return;
            if (JungleItem.Contains(item.type) && item.IsWeapon())
            {
                if (item.type == ItemID.ThornWhip)
                    damage *= HasNature ? 18 : HasEnhance ? 2.2f : 1.1f;
                if (item.type == ItemID.PoisonDart)
                    damage *= HasEnhance ? 1.8f : 1.2f;
                if (item.type == ItemID.BladeofGrass)
                    damage *= HasNature ? 16 : HasEnhance ? 2.4f : 1.1f;
                if (item.type == ItemID.ThornChakram)
                    damage *= HasNature ? 20 : HasEnhance ? 2.6f : 1.1f;
            }
        }
        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Apply && JungleItem.Contains(item.type))
            {
                for (int j = 0; j < 12; j++)
                {
                    Vector2 afterimageOffset = (MathHelper.TwoPi * j / 12f).ToRotationVector2() * 1f;
                    float modifier = 0.5f + ((float)Math.Sin(drawTimer / 30f) / 6);
                    Color glowColor = Color.Lerp(Color.Blue with { A = 0 }, Color.LightGreen with { A = 0 }, modifier) * 0.5f;

                    Texture2D texture = Terraria.GameContent.TextureAssets.Item[item.type].Value;
                    Main.EntitySpriteDraw(texture, position + afterimageOffset, null, glowColor, 0, texture.Size() * 0.5f, item.scale * (item.type == ItemID.BladeofGrass ? 0.7f : 1f), SpriteEffects.None, 0f);
                }
            }
            drawTimer++;
            return base.PreDrawInInventory(item, spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        
    }

}
