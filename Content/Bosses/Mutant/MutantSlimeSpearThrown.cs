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
    public class PHMutantSlimeSpearThrown : MutantSpearThrown, IProjOwnedByBoss<MutantBoss>
    {
        //改版投矛
        public override void AI()
        {
            if (--Projectile.localAI[0] < 0)
            {
                if (WorldSavingSystem.MasochistModeReal)
                {
                    Projectile.localAI[0] = 4;

                    if (Projectile.ai[1] == 0 && FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MutantSphereSmall>(), Projectile.damage, 0f, Projectile.owner, Projectile.ai[0]);
                }
            }
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(FargosSoundRegistry.PenetratorThrow, Projectile.Center);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(135f);

            scaletimer++;
        }
    }
}
