using FargowiltasSouls.Content.Bosses.MutantBoss;
using Terraria.Audio;
using Terraria.ID;
using Terraria;
using Microsoft.Xna.Framework;
using Luminance.Common.Utilities;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantMark2 : MutantMark2
    {
        public override void AI()
        {
            if (base.Projectile.localAI[0] == 0f)
            {
                base.Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(in SoundID.Item84, base.Projectile.Center);
            }

            if ((base.Projectile.ai[0] -= 1f) == 0f)
            {
                base.Projectile.netUpdate = true;
                base.Projectile.velocity = Vector2.Zero;
            }

            if ((base.Projectile.ai[1] -= 1f) == 0f)
            {
                base.Projectile.netUpdate = true;
                Player player = Main.player[Player.FindClosest(base.Projectile.position, base.Projectile.width, base.Projectile.height)];
                base.Projectile.velocity = base.Projectile.SafeDirectionTo(player.Center) * 10f;
                SoundEngine.PlaySound(in SoundID.Item84, base.Projectile.Center);
            }
        }
    }
}
