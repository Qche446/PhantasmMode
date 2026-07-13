using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantSansHead : MutantSansHead
    {
        public override void AI()
        {
            if (base.Projectile.localAI[0] == 0f)
            {
                base.Projectile.localAI[0] = 1f;
                base.Projectile.rotation = Projectile.ai[2] * MathHelper.Pi / 180f + MathHelper.Pi;
            }

            if ((base.Projectile.ai[0] -= 1f) == 0f)
            {
                base.Projectile.velocity = Vector2.Zero;
                base.Projectile.netUpdate = true;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), base.Projectile.Center, Vector2.UnitY.RotatedBy(Projectile.rotation), ModContent.ProjectileType<PHMutantSansBeam>(), base.Projectile.damage, base.Projectile.knockBack, base.Projectile.owner, 0f, base.Projectile.identity);
                }
            }
            else if (base.Projectile.ai[0] < -170f)
            {
                Projectile.velocity *= 1.025f;
            }
            else if (base.Projectile.ai[0] < -50f)
            {
                Projectile.velocity = -Projectile.ai[1] * Vector2.UnitX.RotatedBy(Projectile.rotation);
            }

            base.Projectile.frame = 1;
        }
    }
    public class PHMutantSansBeam : MutantSansBeam
    {
        public override void AI()
        {
            base.Projectile.alpha = 0;
            Vector2? vector = null;
            if (base.Projectile.velocity.HasNaNs() || base.Projectile.velocity == Vector2.Zero)
            {
                base.Projectile.velocity = -Vector2.UnitY;
            }

            Projectile projectile = FargoSoulsUtil.ProjectileExists(FargoSoulsUtil.GetProjectileByIdentity(base.Projectile.owner, base.Projectile.ai[1]), ModContent.ProjectileType<PHMutantSansHead>());
            if (projectile != null)
            {
                base.Projectile.Center = projectile.Center + base.Projectile.velocity * 16f * 3f;
                if (base.Projectile.velocity.HasNaNs() || base.Projectile.velocity == Vector2.Zero)
                {
                    base.Projectile.velocity = -Vector2.UnitY;
                }

                if (base.Projectile.localAI[0] == 0f && !Main.dedServ)
                {
                    SoundStyle style = new SoundStyle("FargowiltasSouls/Assets/Sounds/VanillaEternity/Golem/GolemBeam");
                    SoundEngine.PlaySound(in style, base.Projectile.Center);
                }

                float scale = 1.3f;
                base.Projectile.localAI[0] += 1f;
                if (base.Projectile.localAI[0] >= maxTime)
                {
                    base.Projectile.Kill();
                    return;
                }

                base.Projectile.scale = scale;
                float num = base.Projectile.velocity.ToRotation();
                base.Projectile.rotation = num - MathF.PI / 2f;
                base.Projectile.velocity = num.ToRotationVector2();
                float num2 = 3f;
                _ = base.Projectile.width;
                _ = base.Projectile.Center;
                if (vector.HasValue)
                {
                    _ = vector.Value;
                }

                float[] array = new float[(int)num2];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = 1800f;
                }

                float num3 = 0f;
                for (int j = 0; j < array.Length; j++)
                {
                    num3 += array[j];
                }

                num3 /= num2;
                if (!(base.Projectile.localAI[0] <= 50f))
                {
                    return;
                }

                float value = Math.Max(num3, 320f);
                base.Projectile.localAI[1] = MathHelper.Lerp(0f, value, base.Projectile.localAI[0] / 50f);
                if (++base.Projectile.frameCounter > 3)
                {
                    base.Projectile.frameCounter = 0;
                    if (++base.Projectile.frame >= Main.projFrames[base.Projectile.type])
                    {
                        base.Projectile.frame = 0;
                    }
                }
            }
            else
            {
                base.Projectile.Kill();
            }
        }
    }
}
