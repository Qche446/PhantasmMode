using Terraria;
using Microsoft.Xna.Framework;
using FargosPhantasmMode.Assets.ExtraTextures;
using FargosPhantasmMode.Content.Render;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using System.IO;
using System;
using FargowiltasSouls;
using FargowiltasSouls.Core.Systems;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using static Terraria.GameContent.Creative.CreativePowers;
using Luminance.Common.Utilities;
using FargosPhantasmMode.Core.Systems;
using Monochrome.Core.Graphics.Shaders;


namespace FargosPhantasmMode
{
    // 声明本模组的着色器归 Monochrome 管：加载期进注册表，/monoshader reload 会把编好的 .fxc 写回源目录。
    [MonoShaderScope]
    public class FargosPhantasmMode : Mod
    {
        internal static FargosPhantasmMode Instance;
        public static ManagedRenderTarget Rt;
        public static Mod FargoMod;
        public override void Load()
        {
            ModLoader.TryGetMod("FargowiltasSouls", out FargoMod);

            // 请求的注册与服务端处理。放在 Load 里是因为它必须在任何世界开始之前就绪。
            PModeNet.Register();

            On_FilterManager.EndCapture += FilterManager_EndCapture;
            Rt = new ManagedRenderTarget(true,
                (width, heigth) => new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight));

            Instance = this;
        }
        public override void Unload()
        {
            On_FilterManager.EndCapture -= FilterManager_EndCapture;
        }
        private void FilterManager_EndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;

            #region ��UI����֮��
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
            shader.TrySetParameter("color", new Color(54, 255, 236));//102, 26, 179���ϣ�  54��255��236(��)
            shader.TrySetParameter("m", 0.62f);
            shader.TrySetParameter("n", 0.01f);
            shader.Apply();
            sb.Draw(Rt, Vector2.Zero, Color.White);
            sb.End();
            #endregion

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
    }
}
