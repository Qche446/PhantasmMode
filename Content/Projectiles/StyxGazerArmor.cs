using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Bosses.AbomBoss;
using FargowiltasSouls.Content.Buffs.Masomode;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
        public override bool PreDraw(ref Color lightColor)
        {
            DrawStyxGazerDeathray(Projectile, drawDistance, _ => Projectile.width * base.Projectile.scale * 1.25f);
            return false;
        }
        public static void DrawStyxGazerDeathray(Projectile projectile, float drawDistance, PrimitiveSettings.VertexWidthFunction widthFunction, bool drawHandle = true, bool fadeStart = false)
        {
            if (projectile.velocity == Vector2.Zero)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.StyxGazerShader");
            Texture2D value = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/AbomBoss/AbomSword").Value;
            Vector2 vector = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 vector2 = vector * projectile.scale * value.Height;
            if (!drawHandle)
            {
                vector2 = vector;
            }

            Vector2 vector3 = vector * -176f * projectile.scale;
            Vector2 vector4 = projectile.Center + vector2 * 2f + vector3;
            Vector2 value2 = vector4 + vector * drawDistance;
            Vector2 value3 = vector4;
            Vector2[] array = new Vector2[8];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Vector2.Lerp(value3, value2, (float)i / ((float)array.Length - 1f));
            }

            Color color = AbomSword.midColor;
            shader.TrySetParameter("mainColor", color);
            shader.TrySetParameter("fadeStart", fadeStart);
            Texture2D value4 = FargosTextureRegistry.WillStreak.Value;
            value4.SetTexture1();
            for (int j = 0; j < 2; j++)
            {
                PrimitiveSettings settings = new PrimitiveSettings(widthFunction, ColorFunction, null, Smoothen: true, Pixelate: false, shader);
                PrimitiveRenderer.RenderTrail(array, settings, 30);
            }

            if (drawHandle)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                for (int k = 0; k < 12; k++)
                {
                    Vector2 vector5 = (MathF.PI * 2f * (float)k / 12f).ToRotationVector2() * 6f;
                    Color color2 = AbomSword.darkColor;
                    Main.EntitySpriteDraw(value, projectile.Center + vector2 + vector5 - Main.screenPosition + new Vector2(0f, projectile.gfxOffY), null, color2, vector.ToRotation() + MathF.PI / 2f, Vector2.UnitX * value.Width / 2f, projectile.scale, SpriteEffects.None);
                }

                Main.spriteBatch.ResetToDefault();
                Main.EntitySpriteDraw(value, projectile.Center + vector2 - Main.screenPosition + new Vector2(0f, projectile.gfxOffY), null, AbomSword.lightColor, vector.ToRotation() + MathF.PI / 2f, Vector2.UnitX * value.Width / 2f, projectile.scale, SpriteEffects.None);
            }
            else
            {
                Main.spriteBatch.ResetToDefault();
            }
        }
    }
}