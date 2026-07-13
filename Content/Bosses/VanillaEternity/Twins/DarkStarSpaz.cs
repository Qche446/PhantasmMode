using FargowiltasSouls;
using FargowiltasSouls.Core.Globals;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    public class DarkStarSpaz : DarkStar
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            base.Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            base.AI();
            if ((base.Projectile.ai[1] += 1f) < 75f)
            {
                base.Projectile.velocity *= 1.06f;
            }

            Player player = FargoSoulsUtil.PlayerExists(base.Projectile.ai[0]);
            if (player == null)
            {
                return;
            }

            float num = base.Projectile.velocity.ToRotation();
            Vector2 v = player.Center - base.Projectile.Center;
            float num2 = v.ToRotation();
            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.spazBoss, 126) && Math.Abs(MathHelper.WrapAngle(num2 - num)) < MathF.PI / 2f)
            {
                float num3 = (Main.npc[EModeGlobalNPC.spazBoss].Distance(player.Center) - 600f) / 1200f;
                num3 *= num3;
                if (num3 < 0f)
                {
                    num3 = 0f;
                }

                if (num3 > 1f)
                {
                    num3 = 1f;
                }

                float amount = 0.8f * num3;
                base.Projectile.velocity = new Vector2(base.Projectile.velocity.Length(), 0f).RotatedBy(num.AngleLerp(num2, amount));
            }
        }
    }
}
