using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Buffs.Souls;
using System.Linq;
using FargowiltasSouls;
using FargosPhantasmMode.Content.Buffs;
using static System.Net.Mime.MediaTypeNames;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class IceQueensCrownOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<IceQueensCrown>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.IceQueensCrown"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class CirnoBombOverride : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.type == ModContent.ProjectileType<CirnoBomb>();
        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                if (projectile.ai[0] == 1)
                {
                    Player player = Main.player[projectile.owner];

                    int freezeRange = 16 * 150;
                    int HypothermiaDuration = 600;
                    if (player.FargoSouls().MasochistSoul || player.FargoSouls().MasochistHeart)
                    {
                        HypothermiaDuration *= 2;
                    }
                    //int slowDuration = freezeDuration + 180;

                    foreach (NPC n in Main.npc.Where(n => n.active && !n.friendly && n.damage > 0 && player.Distance(FargoSoulsUtil.ClosestPointInHitbox(n, player.Center)) < freezeRange && !n.dontTakeDamage && !n.buffImmune[ModContent.BuffType<HypothermiaBuff>()]))
                    {
                        n.AddBuff(ModContent.BuffType<HypothermiaBuff>(), HypothermiaDuration);
                    }
                }
            } 
        }
    }
}
