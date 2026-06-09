using Terraria;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using FargowiltasSouls.Content.Bosses.AbomBoss;

namespace FargosPhantasmMode.Content.Projectiles
{
    public class StyxGazerArmor : FargowiltasSouls.Content.Projectiles.BossWeapons.StyxGazer
    {
        public override void AI()
        {
            Projectile.damage = FargoSoulsUtil.HighestDamageTypeScaling(Main.player[Projectile.owner], 666);
            Projectile.CritChance = (int)FargoSoulsUtil.HighestCritChance(Main.player[Projectile.owner]);

            base.AI();

            Main.player[Projectile.owner].itemTime = 0;
            Main.player[Projectile.owner].itemAnimation = 0;
            if (Main.player[Projectile.owner].reuseDelay < 17)
                Main.player[Projectile.owner].reuseDelay = 17;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.localNPCImmunity[target.whoAmI] >= 15)
                return false;
            return null;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.Projectile.localNPCImmunity[target.whoAmI]++;
            if (base.Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(base.Projectile.GetSource_FromThis(), target.Center + Main.rand.NextVector2Circular(100f, 100f), Vector2.Zero, ModContent.ProjectileType<AbomBlast>(), 0, 0f, base.Projectile.owner);
            }
            target.AddBuff(153, 300);
            target.AddBuff(ModContent.BuffType<MutantNibbleBuff>(), 300);
        }
    }
}