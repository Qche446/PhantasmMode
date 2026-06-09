using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Reflection;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;
using Terraria.ID;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Content.Buffs.Masomode;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class GelicWingsOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<GelicWings>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.GelicWings"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
    }
    public class GelicWingSpikeEffect : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.type == ModContent.ProjectileType<GelicWingSpike>() && WorldSavingSystem.masochistModeReal)
            {
                target.AddBuff(ModContent.BuffType<FlamesoftheUniverseBuff>(), 60);
                target.AddBuff(BuffID.Oiled, 240);
            }
            base.OnHitNPC(projectile, target, hit, damageDone);
        }
    }
}
