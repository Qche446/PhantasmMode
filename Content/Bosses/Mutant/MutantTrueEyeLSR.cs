using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.DataStructures;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantTrueEyeL : MutantTrueEyeL, IProjOwnedByBoss<MutantBoss>
    {
        public override void AI()
        {
            Player player = Main.player[(int)base.Projectile.ai[0]];
            base.Projectile.localAI[0] += 1f;
            switch ((int)base.Projectile.ai[1])
            {
                case 0:
                    {
                        Vector2 vector = player.Center - base.Projectile.Center + new Vector2(0f, -300f);
                        if (vector != Vector2.Zero)
                        {
                            vector.Normalize();
                            vector *= 24f;
                            base.Projectile.velocity.X = (base.Projectile.velocity.X * 29f + vector.X) / 30f;
                            base.Projectile.velocity.Y = (base.Projectile.velocity.Y * 29f + vector.Y) / 30f;
                        }

                        if (base.Projectile.Distance(player.Center) < 150f)
                        {
                            if (base.Projectile.Center.X < player.Center.X)
                            {
                                base.Projectile.velocity.X -= 0.25f;
                            }
                            else
                            {
                                base.Projectile.velocity.X += 0.25f;
                            }

                            if (base.Projectile.Center.Y < player.Center.Y)
                            {
                                base.Projectile.velocity.Y -= 0.25f;
                            }
                            else
                            {
                                base.Projectile.velocity.Y += 0.25f;
                            }
                        }

                        if (base.Projectile.localAI[0] > 120f)
                        {
                            base.Projectile.localAI[0] = 0f;
                            base.Projectile.ai[1] += 1f;
                            base.Projectile.netUpdate = true;
                        }

                        break;
                    }
                case 1:
                    base.Projectile.velocity *= 0.95f;
                    if (base.Projectile.velocity.Length() < 1f)
                    {
                        base.Projectile.velocity = Vector2.Zero;
                        base.Projectile.localAI[0] = 0f;
                        base.Projectile.ai[1] += 1f;
                        base.Projectile.netUpdate = true;
                    }

                    break;
                case 2:
                    if (base.Projectile.localAI[0] == 1f)
                    {
                        float num2 = MathF.PI / 135f;
                        if (base.Projectile.Center.X < player.Center.X)
                        {
                            num2 *= -1f;
                        }

                        localAI0 -= num2 * 60f;
                        Vector2 velocity = -Vector2.UnitX.RotatedBy(localAI0);
                        if (FargoSoulsUtil.HostCheck)
                        {
                            int p = Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), base.Projectile.Center - Vector2.UnitY * 12f, velocity, ModContent.ProjectileType<PHMutantTrueEyeDeathray>(), base.Projectile.damage, 0f, base.Projectile.owner, num2, base.Projectile.whoAmI);
                        }

                        localai1 = num2;
                        base.Projectile.netUpdate = true;
                    }
                    else if (base.Projectile.localAI[0] > 90f)
                    {
                        base.Projectile.localAI[0] = 0f;
                        base.Projectile.ai[1] += 1f;
                    }
                    else
                    {
                        localAI0 += localai1;
                    }

                    break;
                default:
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            int num = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.IceTorch, 0f, 0f, 0, default(Color), 3f);
                            Main.dust[num].noGravity = true;
                            Main.dust[num].noLight = true;
                            Main.dust[num].velocity *= 8f;
                        }

                        SoundEngine.PlaySound(in SoundID.Zombie102, base.Projectile.Center);
                        base.Projectile.Kill();
                        break;
                    }
            }

            if ((double)base.Projectile.rotation > 3.14159274101257)
            {
                base.Projectile.rotation = base.Projectile.rotation - 6.283185f;
            }

            base.Projectile.rotation = (((double)base.Projectile.rotation <= -0.005 || (double)base.Projectile.rotation >= 0.005) ? (base.Projectile.rotation * 0.96f) : 0f);
            if (++base.Projectile.frameCounter >= 4)
            {
                base.Projectile.frameCounter = 0;
                if (++base.Projectile.frame >= Main.projFrames[base.Projectile.type])
                {
                    base.Projectile.frame = 0;
                }
            }

            if (base.Projectile.ai[1] != 2f)
            {
                UpdatePupil();
            }
        }
    }
    public class PHMutantTrueEyeDeathray : BaseDeathray, IPixelatedPrimitiveRenderer, IProjOwnedByBoss<MutantBoss>
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Deathrays/" + (FargoSoulsUtil.AprilFools ? "PhantasmalDeathray" : "PhantasmalDeathrayML");

        public PHMutantTrueEyeDeathray()
            : base(90f)
        {
        }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override bool CanHitPlayer(Player target)
        {
            return target.hurtCooldowns[1] == 0;
        }

        public override void AI()
        {
            ScreenShakeSystem.StartShake(3f);
            base.Projectile.hide = false;
            Vector2? vector = null;
            if (base.Projectile.velocity.HasNaNs() || base.Projectile.velocity == Vector2.Zero)
            {
                base.Projectile.velocity = -Vector2.UnitY;
            }

            if (base.Projectile.velocity.HasNaNs() || base.Projectile.velocity == Vector2.Zero)
            {
                base.Projectile.velocity = -Vector2.UnitY;
            }

            if (base.Projectile.localAI[0] == 0f)
            {
                SoundStyle style = SoundID.Zombie104 with
                {
                    Volume = 0.5f
                };
                SoundEngine.PlaySound(in style, base.Projectile.Center);
            }

            float num = 0.4f;
            base.Projectile.localAI[0] += 1f;
            if (base.Projectile.localAI[0] >= maxTime)
            {
                base.Projectile.Kill();
                return;
            }

            base.Projectile.scale = (float)Math.Sin(base.Projectile.localAI[0] * MathF.PI / maxTime) * 10f * num;
            if (base.Projectile.scale > num)
            {
                base.Projectile.scale = num;
            }

            float num2 = base.Projectile.velocity.ToRotation();
            num2 += base.Projectile.ai[0];
            base.Projectile.rotation = num2 - MathF.PI / 2f;
            base.Projectile.velocity = num2.ToRotationVector2();
            float num3 = 3f;
            float num4 = base.Projectile.width;
            Vector2 samplingPoint = base.Projectile.Center;
            if (vector.HasValue)
            {
                samplingPoint = vector.Value;
            }

            float[] array = new float[(int)num3];
            Collision.LaserScan(samplingPoint, base.Projectile.velocity, 5 * num4 * base.Projectile.scale, 3000f, array);
            float num5 = 0f;
            for (int i = 0; i < array.Length; i++)
            {
                num5 += array[i];
            }

            num5 /= num3;
            float amount = 0.5f;
            base.Projectile.localAI[1] = MathHelper.Lerp(base.Projectile.localAI[1], num5, amount);
            Vector2 vector2 = base.Projectile.Center + base.Projectile.velocity * (base.Projectile.localAI[1] - 14f);
            for (int j = 0; j < 2; j++)
            {
                float num6 = base.Projectile.velocity.ToRotation() + (Main.rand.NextBool(2) ? (-1f) : 1f) * (MathF.PI / 2f);
                float num7 = (float)Main.rand.NextDouble() * 2f + 2f;
                Vector2 vector3 = new Vector2((float)Math.Cos(num6) * num7, (float)Math.Sin(num6) * num7);
                int num8 = Dust.NewDust(vector2, 0, 0, DustID.CopperCoin, vector3.X, vector3.Y);
                Main.dust[num8].noGravity = true;
                Main.dust[num8].scale = 1.7f;
            }

            if (Main.rand.NextBool(5))
            {
                Vector2 vector4 = base.Projectile.velocity.RotatedBy(1.5707963705062866) * ((float)Main.rand.NextDouble() - 0.5f) * base.Projectile.width;
                int num9 = Dust.NewDust(vector2 + vector4 - Vector2.One * 4f, 8, 8, DustID.CopperCoin, 0f, 0f, 100, default(Color), 1.5f);
                Main.dust[num9].velocity *= 0.5f;
                Main.dust[num9].velocity.Y = 0f - Math.Abs(Main.dust[num9].velocity.Y);
            }

            DelegateMethods.v3_1 = new Vector3(0.3f, 0.65f, 0.7f);
            Utils.PlotTileLine(base.Projectile.Center, base.Projectile.Center + base.Projectile.velocity * base.Projectile.localAI[1], (float)base.Projectile.width * base.Projectile.scale, DelegateMethods.CastLight);
            base.Projectile.position -= base.Projectile.velocity;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.EternityMode)
            {
                target.FargoSouls().MaxLifeReduction += 100;
                target.AddBuff(ModContent.BuffType<OceanicMaulBuff>(), 5400);
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
            }

            target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 360);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public float WidthFunction(float trailInterpolant)
        {
            return 5 * (float)base.Projectile.width * base.Projectile.scale * 1.3f;
        }

        public static Color ColorFunction(float trailInterpolant)
        {
            Color result = (FargoSoulsUtil.AprilFools ? Color.Red : Color.Cyan);
            result.A = 100;
            return result;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            if (!base.Projectile.hide)
            {
                ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.GenericDeathray");
                Vector2 value = base.Projectile.Center + base.Projectile.velocity.SafeNormalize(Vector2.UnitY) * drawDistance * 1.1f;
                Vector2 center = base.Projectile.Center;
                Vector2[] array = new Vector2[8];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = Vector2.Lerp(center, value, (float)i / ((float)array.Length - 1f));
                }

                FargosTextureRegistry.MutantStreak.Value.SetTexture1();
                shader.TrySetParameter("mainColor", FargoSoulsUtil.AprilFools ? new Color(253, 252, 183, 100) : new Color(183, 252, 253, 100));
                shader.TrySetParameter("stretchAmount", 3);
                shader.TrySetParameter("scrollSpeed", 2f);
                shader.TrySetParameter("uColorFadeScaler", 1f);
                shader.TrySetParameter("useFadeIn", true);
                PrimitiveRenderer.RenderTrail(array, new PrimitiveSettings(WidthFunction, ColorFunction, null, Smoothen: true, Pixelate: true, shader), 20);
            }
        }
    }
}
