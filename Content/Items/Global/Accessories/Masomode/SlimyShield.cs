using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles.BossWeapons;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class SlimyShieldOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<SlimyShield>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.SlimyShield"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateVanity(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<SlimyShield>() && WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<PlatformFallthroughEffect>(item);
            }
            base.UpdateVanity(item, player);
        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<SlimyShield>() && WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<PlatformFallthroughEffect>(item);
            }
            base.UpdateInventory(item, player);
        }
    }
    public class SlimeBallOverride : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(projectile, target, hit, damageDone);
            if (projectile.type == ModContent.ProjectileType<SlimeBall>() && WorldSavingSystem.masochistModeReal)
            {
                target.AddBuff(ModContent.BuffType<FlamesoftheUniverseBuff>(), 60);
            }
        }
    }
}
