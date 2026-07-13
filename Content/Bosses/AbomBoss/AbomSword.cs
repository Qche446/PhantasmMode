using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss
{
    public class AbomSword : AbomSpecialDeathray
    {
        public AbomSword() : base(300) { }

        public int counter;
        public bool spawnedHandle;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.FargoSouls().DeletionImmuneRank = 2;
            Projectile.extraUpdates = 1;
            Projectile.netImportant = true;
        }
        public override void AI()
        {
            base.AI();

            Vector2? vector78 = null;
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = -Vector2.UnitY;
            }
            NPC abom = FargoSoulsUtil.NPCExists(Projectile.ai[1], ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>());
            if (abom == null)
            {
                Projectile.Kill();
                return;
            }
            else
            {
                Projectile.Center = abom.Center;
            }
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = -Vector2.UnitY;
            }
            if (Projectile.localAI[0] == 0f)
            {
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(FargosSoundRegistry.StyxGazer with { Volume = 1.5f }, Projectile.Center);
                    SoundEngine.PlaySound(FargosSoundRegistry.GenericDeathray, Projectile.Center);
                }
            }
            float num801 = 1f;
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] >= maxTime)
            {
                Projectile.Kill();
                return;
            }
            Projectile.scale = (float)Math.Sin(Projectile.localAI[0] * MathF.PI / maxTime) * num801 * 3;
            if (Projectile.scale > num801)
            {
                Projectile.scale = num801;
            }
            float rotation = Projectile.velocity.ToRotation();
            if ((abom.velocity != Vector2.Zero || abom.ai[0] == 19 || abom.ai[0] == 29 || abom.ai[0] == 31) && abom.ai[0] != 20 && abom.ai[0] !=30 && abom.ai[0] !=32)
            {
                rotation += Projectile.ai[0] / Projectile.MaxUpdates;
            }

            Projectile.rotation = rotation - 1.57079637f;
            Projectile.velocity = rotation.ToRotationVector2();
            float num805 = 3f;
            float num806 = Projectile.width;
            Vector2 samplingPoint = Projectile.Center;
            if (vector78.HasValue)
            {
                samplingPoint = vector78.Value;
            }
            float[] array3 = new float[(int)num805];
            //Collision.LaserScan(samplingPoint, Projectile.velocity, num806 * Projectile.scale, 3000f, array3);
            for (int i = 0; i < array3.Length; i++)
                array3[i] = 3000f;
            float num807 = 0f;
            int num3;
            for (int num808 = 0; num808 < array3.Length; num808 = num3 + 1)
            {
                num807 += array3[num808];
                num3 = num808;
            }
            num807 /= num805;
            float amount = 0.5f;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], num807, amount);
            if (Projectile.localAI[0] % 2 == 0)
            {
                /*Vector2 vector79 = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14f);
                for (int num809 = 0; num809 < 2; num809 = num3 + 1)
                {
                    float num810 = Projectile.velocity.ToRotation() + ((Main.rand.Next(2) == 1) ? -1f : 1f) * 1.57079637f;
                    float num811 = (float)Main.rand.NextDouble() * 2f + 2f;
                    Vector2 vector80 = new Vector2((float)Math.Cos((double)num810) * num811, (float)Math.Sin((double)num810) * num811);
                    int num812 = Dust.NewDust(vector79, 0, 0, 244, vector80.X, vector80.Y, 0, default(Color), 1f);
                    Main.dust[num812].noGravity = true;
                    Main.dust[num812].scale = 1.7f;
                    num3 = num809;
                }
                if (Main.rand.NextBool(5))
                {
                    Vector2 value29 = Projectile.velocity.RotatedBy(1.5707963705062866, default(Vector2)) * ((float)Main.rand.NextDouble() - 0.5f) * (float)Projectile.width;
                    int num813 = Dust.NewDust(vector79 + value29 - Vector2.One * 4f, 8, 8, 244, 0f, 0f, 100, default(Color), 1.5f);
                    Dust dust = Main.dust[num813];
                    dust.velocity *= 0.5f;
                    Main.dust[num813].velocity.Y = -Math.Abs(Main.dust[num813].velocity.Y);
                }*/
                //DelegateMethods.v3_1 = new Vector3(0.3f, 0.65f, 0.7f);
                //Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], (float)Projectile.width * Projectile.scale, new Utils.PerLinePoint(DelegateMethods.CastLight));

                if (abom.velocity != Vector2.Zero)
                {
                    if (--counter < 0)
                    {
                        counter = 5;
                        if (FargoSoulsUtil.HostCheck) //spawn bonus projs
                        {
                            Vector2 spawnPos = Projectile.Center;
                            Vector2 vel = Projectile.velocity.RotatedBy(Math.PI / 2 * Math.Sign(Projectile.ai[0]));
                            const int max = 15;
                            for (int i = 1; i <= max; i++)
                            {
                                spawnPos += Projectile.velocity * 3000f / max;
                                Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), spawnPos, vel, ModContent.ProjectileType<AbomSickle2>(), Projectile.damage, 0f, Projectile.owner);
                            }
                        }
                    }
                    // spark visuals
                    const float sparks = 60;
                    for (int i = 2; i < sparks; i++)
                    {
                        if (Main.rand.NextBool(6))
                        {
                            float lerper = i + Main.rand.NextFloat(-0.7f, 0.7f);
                            Vector2 spawnPos = Projectile.Center + lerper * Projectile.velocity * 3000f / sparks;
                            Vector2 vel = Projectile.velocity.RotatedBy(Math.PI / 2 * -Math.Sign(Projectile.ai[0]));
                            vel *= Main.rand.NextFloat(6, 12f);
                            vel = vel.RotatedByRandom(MathHelper.PiOver2 * 0.3f);
                            Particle p = new RectangleParticle(spawnPos, vel, Color.OrangeRed, Main.rand.NextFloat(0.2f, 0.4f), Main.rand.Next(20, 40), true, true, Color.Yellow);
                            p.Spawn();
                        }
                    }
                    const float smoke = 20;
                    for (int i = 2; i < smoke; i++)
                    {
                        if (Main.rand.NextBool(10))
                        {
                            float lerper = i + Main.rand.NextFloat(-0.7f, 0.7f);
                            Vector2 spawnPos = Projectile.Center + lerper * Projectile.velocity * 3000f / smoke;
                            Vector2 vel = Projectile.velocity.RotatedBy(Math.PI / 2 * -Math.Sign(Projectile.ai[0]));
                            vel *= Main.rand.NextFloat(2f, 4f);
                            vel = vel.RotatedByRandom(MathHelper.PiOver2 * 0.3f);

                            int index = Gore.NewGore(Projectile.GetSource_FromThis(), spawnPos, vel, Main.rand.Next(61, 64), 1f);
                            Main.gore[index].scale *= Main.rand.NextFloat(0.6f, 0.9f);
                            Main.gore[index].rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                        }
                    }
                }
                /*
                for (int i = 0; i < 15; i++)
                {
                    int d = Dust.NewDust(Projectile.position + Projectile.velocity * Main.rand.NextFloat(2000), Projectile.width, Projectile.height, DustID.GemTopaz, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 4f;
                }
                */
            }

            if (!spawnedHandle)
            {
                spawnedHandle = true;
                if (FargoSoulsUtil.HostCheck)
                {
                    //Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<AbomSwordHandle>(), Projectile.damage, Projectile.knockBack, Projectile.owner, (float)Math.PI / 2, Projectile.identity);
                    //Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<AbomSwordHandle>(), Projectile.damage, Projectile.knockBack, Projectile.owner, -(float)Math.PI / 2, Projectile.identity);
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.velocity.X = target.Center.X < Main.npc[(int)Projectile.ai[1]].Center.X ? -15f : 15f;
            target.velocity.Y = -10f;

            Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), target.Center + Main.rand.NextVector2Circular(100, 100), Vector2.Zero, ModContent.ProjectileType<AbomBlast>(), 0, 0f, Projectile.owner);

            target.AddBuff(BuffID.Bleeding, 240);
            if (WorldSavingSystem.EternityMode)
                target.AddBuff(ModContent.BuffType<AbomFangBuff>(), 240);
        }

        public float WidthFunction(float _) => Projectile.width * Projectile.scale * 2;
        public static Color FromDecimal(double r, double g, double b, double a) => new((int)(r * 255), (int)(g * 255), (int)(b * 255), (int)(a * 255)); 
        public static readonly Color darkColor = FromDecimal(0.75, 0.36, 0.08, 1);
        public static readonly Color midColor = FromDecimal(0.96, 0.60, 0.09, 1);
        public static readonly Color lightColor = FromDecimal(0.98, 0.95, 0.79, 1);

        public static Color ColorFunction(float _) => darkColor;

        public override bool PreDraw(ref Color lightColor)
        {
            DrawStyxGazerDeathray(Projectile, drawDistance, WidthFunction);
            return false;
        }
        public static void DrawStyxGazerDeathray(Projectile projectile, float drawDistance, PrimitiveSettings.VertexWidthFunction widthFunction, bool drawHandle = true, bool fadeStart = false)
        {
            // This should never happen, but just in case.
            if (projectile.velocity == Vector2.Zero)
                return;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.StyxGazerShader");
            Texture2D hiltTexture = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/AbomBoss/AbomSword").Value;

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 offset = direction * projectile.scale * hiltTexture.Height;
            if (!drawHandle)
                offset = direction;

            // Get the laser positions.
            Vector2 laserStartOffset = direction * -176 * projectile.scale;
            Vector2 laserStart = projectile.Center + offset * 2 + laserStartOffset;
            Vector2 laserEnd = laserStart + direction * drawDistance;

            // Create 8 points that span across the draw distance from the projectile center.

            // This allows the drawing to be pushed back, which is needed due to the shader fading in at the start to avoid
            // sharp lines.
            Vector2 initialDrawPoint = laserStart;
            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(initialDrawPoint, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // Set shader parameters. This one takes a fademap and a color.

            // The laser should fade to this in the middle.
            Color brightColor = midColor;
            shader.TrySetParameter("mainColor", brightColor);
            shader.TrySetParameter("fadeStart", fadeStart);

            // GameShaders.Misc["FargoswiltasSouls:MutantDeathray"].UseImage1(); cannot be used due to only accepting vanilla paths.
            Texture2D fademap = FargosTextureRegistry.MagmaStreak.Value;
            FargoSoulsUtil.SetTexture1(fademap);
            for (int j = 0; j < 2; j++)
            {
                PrimitiveSettings primSettings = new(widthFunction, ColorFunction, Shader: shader);
                PrimitiveRenderer.RenderTrail(baseDrawPoints, primSettings, 30);
                /*
                for (int i = 0; i < baseDrawPoints.Length / 2; i++)
                {
                    Vector2 temp = baseDrawPoints[i];
                    int swap = baseDrawPoints.Length - 1 - i;
                    baseDrawPoints[i] = baseDrawPoints[swap];
                    baseDrawPoints[swap] = temp;
                }
                PrimitiveRenderer.RenderTrail(baseDrawPoints, primSettings, 30);
                */
            }

            if (drawHandle)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);

                // glow
                for (int j = 0; j < 12; j++)
                {
                    Vector2 afterimageOffset = (MathHelper.TwoPi * j / 12f).ToRotationVector2() * 6f;
                    Color glowColor = darkColor;

                    Main.EntitySpriteDraw(hiltTexture, projectile.Center + offset + afterimageOffset - Main.screenPosition + new Vector2(0f, projectile.gfxOffY), null, glowColor,
                        direction.ToRotation() + MathHelper.PiOver2, Vector2.UnitX * hiltTexture.Width / 2, projectile.scale, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ResetToDefault();

                Main.EntitySpriteDraw(hiltTexture, projectile.Center + offset - Main.screenPosition + new Vector2(0f, projectile.gfxOffY), null, lightColor,
                    direction.ToRotation() + MathHelper.PiOver2, Vector2.UnitX * hiltTexture.Width / 2, projectile.scale, SpriteEffects.None, 0);
            }
            else
            {
                Main.spriteBatch.ResetToDefault();
            }
        }
    }
}