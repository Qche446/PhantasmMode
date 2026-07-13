using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class DarkenedHeartOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<DarkenedHeart>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.DarkenedHeart"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
    }
    public class TinyEaterDamageEffect : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.type == ProjectileID.TinyEater;


        int HeartItemType = -1;
        bool fromEnch => HeartItemType != -1;
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (!projectile.owner.IsWithinBounds(Main.maxPlayers))
                return;
            Player player = Main.player[projectile.owner];
            Item heartItem = player.FargoSouls().DarkenedHeartItem;
            if (player != null && heartItem != null && player.active && source is EntitySource_ItemUse itemSource && itemSource.Item.type == heartItem.type)
            {
                HeartItemType = heartItem.type;
            }
        }
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(projectile, target, hit, damageDone);
            if (projectile.type == ProjectileID.TinyEater && WorldSavingSystem.masochistModeReal)
            {
                if (fromEnch)
                {
                    float PrecentageDamage = 0.1f / 100f;
                    if ((int)(PrecentageDamage * target.lifeMax) < target.life)
                    {
                        target.life -= (int)(PrecentageDamage * target.lifeMax);
                    }
                    else
                    {
                        target.life -= target.life - 1;
                    }
                    CombatText.NewText(target.Hitbox, Color.Aqua, (int)(PrecentageDamage * target.lifeMax));
                }
            } 
        }
    }
}
