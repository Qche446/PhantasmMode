using FargowiltasSouls;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantSpearThrown : MutantSpearThrown, IProjOwnedByBoss<MutantBoss>
    {
        //改版投矛
        public override void AI()
        {
            if (--Projectile.localAI[0] < 0)
            {
                Projectile.localAI[0] = 3;

                for (int i = -1; i <= 1; i += 2)
                {
                    if (FargoSoulsUtil.HostCheck && scaletimer <= 48)
                    {
                        Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, 16f / 2f * Vector2.Normalize(Projectile.velocity).RotatedBy(MathHelper.PiOver2 * i), ModContent.ProjectileType<PHMutantSphereSmall>(), Projectile.damage, 0f, Main.myPlayer, Projectile.owner,scaletimer);
                    }
                }
            }
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(WorldSavingSystem.masochistModeReal ? FargosSoundRegistry.PenetratorExplosion : FargosSoundRegistry.PenetratorThrow, Projectile.Center);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(135f);

            scaletimer++;
        }
    }

}
