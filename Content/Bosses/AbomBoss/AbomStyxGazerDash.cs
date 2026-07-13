using FargowiltasSouls;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss
{
    public class AbomStyxGazerDash : AbomStyxGazer
    {
        public override string Texture => "FargowiltasSouls/Content/Items/Weapons/FinalUpgrades/StyxGazer";
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = maxTime;
        }
        public override void AI()
        {
            Projectile.hide = false; //to avoid edge case tick 1 wackiness

            //the important part
            NPC npc = FargoSoulsUtil.NPCExists(Projectile.ai[0], ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>());
            if (npc != null)
            {
                if (npc.ai[0] == 0) Projectile.extraUpdates = 1;

                if (Projectile.localAI[0] == 0)
                    Projectile.localAI[1] = Projectile.ai[1] / maxTime; //do this first

                Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[1]);
                Projectile.ai[1] -= Projectile.localAI[1];
                Projectile.Center = npc.Center + new Vector2(60, 60).RotatedBy(Projectile.velocity.ToRotation() - MathHelper.PiOver4) * Projectile.scale;
            }
            else
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;

                /*Vector2 basePos = Projectile.Center - Projectile.velocity * 141 / 2 * Projectile.scale;
                for (int i = 0; i < 40; i++)
                {
                    int d = Dust.NewDust(basePos + Projectile.velocity * Main.rand.NextFloat(127) * Projectile.scale, 0, 0, 87, Scale: 3f);
                    Main.dust[d].velocity *= 4.5f;
                    Main.dust[d].noGravity = true;
                }*/

                SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
            }

            /*if (Projectile.timeLeft == maxTime - 20)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    int p = Player.FindClosest(Projectile.Center, 0, 0);
                    if (p != -1)
                    {
                        Vector2 vel = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * 30f;
                        int max = 8;
                        for (int i = 0; i < max; i++)
                        {
                            Projectile.NewProjectile(Projectile.InheritSource(Projectile), Projectile.Center, vel.RotatedBy(MathHelper.TwoPi / max * i), ModContent.ProjectileType<AbomSickle3>(), Projectile.damage, Projectile.knockBack, Projectile.owner, p);
                        }
                    }
                }
            }*/

            Projectile.Opacity = (float)Math.Min(1, (2 - Projectile.extraUpdates) * Math.Sin(Math.PI * (maxTime - Projectile.timeLeft) / maxTime));

            Projectile.direction = Projectile.spriteDirection = Math.Sign(Projectile.ai[1]);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(Projectile.direction < 0 ? 135 : 45);
            //Main.NewText(MathHelper.ToDegrees(Projectile.velocity.ToRotation()) + " " + MathHelper.ToDegrees(Projectile.ai[1]));
        }
    }
}