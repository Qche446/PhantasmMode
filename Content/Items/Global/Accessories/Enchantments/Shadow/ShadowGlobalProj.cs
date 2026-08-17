using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class ShadowGlobalProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool NinjaAttack;
        public override GlobalProjectile NewInstance(Projectile target) => PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            if (proj.type == ModContent.ProjectileType<MonkDashSlash>() && source is EntitySource_Parent parent && parent.Entity is Projectile)
            {
                Player py = Main.player[proj.owner];
                py.GetModPlayer<ShadowPlayer>().ActualReduceMonoCD = true;
            }
            if (source is EntitySource_ItemUse_WithAmmo || source is EntitySource_ItemUse)
            {
                NinjaAttack = true;
            }
        }
        public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            Player py = Main.player[proj.owner];
            if (py.HasEffect<NinjaAttackSpeedEffect>())
            {
                bool hasForce = py.ForceEffect<NinjaAttackSpeedEffect>();
                if (proj.whoAmI == py.heldProj || NinjaAttack)
                {
                    modifiers.FinalDamage *= hasForce ? 0.3f : 0.45f;
                }
            }
        }
    }
}
