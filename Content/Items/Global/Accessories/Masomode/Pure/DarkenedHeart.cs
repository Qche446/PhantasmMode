using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Pure
{
    public class DarkenedHeartOverride : PModeGlobalMasoItem<DarkenedHeart>
    {

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
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitNPC(projectile, target, ref modifiers);
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                if (fromEnch)
                {
                    float PrecentageDamage = 0.1f / 100f;
                    int CaseDamage = (int)(PrecentageDamage * target.lifeMax);
                    if (CaseDamage > 1000)
                        CaseDamage = 1000;
                    modifiers.FlatBonusDamage += CaseDamage;
                    //modifiers.HideCombatText();
                    //CombatText.NewText(target.Hitbox, Color.Aqua, CaseDamage);
                }
            }
        }
    }
}
