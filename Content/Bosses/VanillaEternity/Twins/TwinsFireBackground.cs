using System;
using System.IO;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls.Assets.ExtraTextures;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// ai[0] = whoami,ai[1] = npc.type
    /// </summary>
    public class TwinsFireBackground : ModProjectile
    {
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;

        //ref float NPCID => ref Projectile.ai[0];


        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Terraria.ID.ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;
        }

        public override void SetDefaults()
        {
            Terraria.ID.ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 2;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            int npcID = (int)Projectile.ai[0];
            if (FargoSoulsUtil.NPCExists(npcID, NPCID.Retinazer, NPCID.Spazmatism) == null)
            {
                Deplete();
                return;
            }
            NPC npc = Main.npc[npcID];
            if (!npc.Alive())
            {
                Deplete();
                return;
            }
            else
            {
                Projectile.timeLeft = 60 * 9999;
                Projectile.Center = Main.LocalPlayer.Center;
                float opacity = 10 * (1f - 0.3f * npc.GetLifePercent());
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, opacity, 0.001f);
            }
        }
        public void Deplete()
        {
            Projectile.Opacity -= 0.05f;
            if (Projectile.Opacity <= 0f)
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 auraPos = Main.LocalPlayer.Center;
            float radius = Main.screenWidth * 1.2f / 2;
            var target = Main.LocalPlayer;
            var blackTile = TextureAssets.MagicPixel;
            var diagonalNoise = FargosTextureRegistry.Techno1Noise;
            var maxOpacity = Projectile.Opacity * 0.2f;

            if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                return false;

            ManagedShader shader = ShaderManager.GetShader("FargosPhantasmMode.TwinsBackgroundShader");
            shader.TrySetParameter("isRetinazer", (int)Projectile.ai[1] == NPCID.Retinazer);
            shader.TrySetParameter("colorMult", 7.35f);
            shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
            shader.TrySetParameter("radius", radius);
            shader.TrySetParameter("anchorPoint", auraPos);
            shader.TrySetParameter("screenPosition", Main.screenPosition);
            shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            shader.TrySetParameter("playerPosition", target.Center);
            shader.TrySetParameter("maxOpacity", maxOpacity);


            Main.spriteBatch.GraphicsDevice.Textures[1] = diagonalNoise.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, shader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
            Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
            Main.spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
