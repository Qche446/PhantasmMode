using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class SkullCharmOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<SkullCharm>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.SkullCharm"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }

    }
    public class ShadowflamesFriendlyOverride : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(projectile, target, hit, damageDone);
            if (projectile.type == ModContent.ProjectileType<ShadowflamesFriendly>() && WorldSavingSystem.masochistModeReal)
            {
                target.AddBuff(BuffID.ShadowFlame, 30);
            }
            if(WorldSavingSystem.masochistModeReal && projectile.type == ModContent.ProjectileType<PhantasmalDeathrayPungent>())
            {
                target.AddBuff(BuffID.OnFire3, 180);
                target.AddBuff(BuffID.ShadowFlame, 180);
                target.AddBuff(ModContent.BuffType<SmiteBuff>(), 60);
            }
        }
    }
}
