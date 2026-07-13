using FargosPhantasmMode.Assets.ExtraTextures;
using FargosPhantasmMode.Content.Render;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;


namespace FargosPhantasmMode
{
    public class FargosPhantasmMode : Mod
    {
        internal static FargosPhantasmMode Instance;
        public static ManagedRenderTarget Rt;
        public override void Load()
        {
            On_FilterManager.EndCapture += FilterManager_EndCapture;
            Rt = new ManagedRenderTarget(true,
                (width, heigth) => new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight));

            Instance = this;
        }
        public override void Unload()
        {
            /*
            #region Sprites
            static void RestoreSprites(Dictionary<int, Asset<Texture2D>> buffer, Asset<Texture2D>[] original)
            {
                foreach (KeyValuePair<int, Asset<Texture2D>> pair in buffer)
                    original[pair.Key] = pair.Value;

                buffer.Clear();
            }

            RestoreSprites(TextureBuffer.NPC, TextureAssets.Npc);
            RestoreSprites(TextureBuffer.NPCHeadBoss, TextureAssets.NpcHeadBoss);
            RestoreSprites(TextureBuffer.Gore, TextureAssets.Gore);
            RestoreSprites(TextureBuffer.Golem, TextureAssets.Golem);
            RestoreSprites(TextureBuffer.Dest, TextureAssets.Dest);
            RestoreSprites(TextureBuffer.GlowMask, TextureAssets.GlowMask);
            RestoreSprites(TextureBuffer.Extra, TextureAssets.Extra);
            RestoreSprites(TextureBuffer.Projectile, TextureAssets.Projectile);

            if (TextureBuffer.Ninja != null)
                TextureAssets.Ninja = TextureBuffer.Ninja;
            if (TextureBuffer.Probe != null)
                TextureAssets.Probe = TextureBuffer.Probe;
            if (TextureBuffer.BoneArm != null)
                TextureAssets.BoneArm = TextureBuffer.BoneArm;
            if (TextureBuffer.BoneArm2 != null)
                TextureAssets.BoneArm2 = TextureBuffer.BoneArm2;
            if (TextureBuffer.BoneLaser != null)
                TextureAssets.BoneLaser = TextureBuffer.BoneLaser;
            if (TextureBuffer.BoneEyes != null)
                TextureAssets.BoneEyes = TextureBuffer.BoneEyes;
            if (TextureBuffer.Chain12 != null)
                TextureAssets.Chain12 = TextureBuffer.Chain12;
            if (TextureBuffer.Chain26 != null)
                TextureAssets.Chain26 = TextureBuffer.Chain26;
            if (TextureBuffer.Chain27 != null)
                TextureAssets.Chain27 = TextureBuffer.Chain27;
            if (TextureBuffer.Wof != null)
                TextureAssets.Wof = TextureBuffer.Wof;

            ToggleLoader.Unload();
            #endregion
            */
            On_FilterManager.EndCapture -= FilterManager_EndCapture;
        }
        private void FilterManager_EndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;

            #region ¡°UIÓîÖæÖ®»ð¡±
            gd.SetRenderTarget(Main.screenTargetSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();


            gd.SetRenderTarget(Rt);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            Texture2D tex = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Dusts/CosmicFlame").Value;
            FirePartiRe.AllDraw(sb, tex);
            FirePartiRe.UpdateParticle();
            //LightningPartiRe.AllDraw(sb);
            LightningPartiRe.UpdateParticle();
            sb.End();

            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            ManagedShader shader = ShaderManager.GetShader("FargosPhantasmMode.BigTentacle");
            gd.Textures[1] = PhantasmTextureRegistry.UniverseNoise.Value;
            shader.TrySetParameter("color", new Color(54, 255, 236));//102, 26, 179£¨×Ï£©  54£¬255£¬236(Çà)
            shader.TrySetParameter("m", 0.62f);
            shader.TrySetParameter("n", 0.01f);
            shader.Apply("Tentacle");
            sb.Draw(Rt, Vector2.Zero, Color.White);
            sb.End();
            #endregion

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
    }
}
