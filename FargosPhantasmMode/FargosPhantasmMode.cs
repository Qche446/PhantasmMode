using FargosPhantasmMode.Assets.ExtraTextures;
using FargosPhantasmMode.Content.Render;
using FargosPhantasmMode.Core.Systems;
using Monochrome.Core.Graphics.Shaders;
using FargosPhantasmMode.Common.DamageRules;
using Monochrome.Core.Graphics.RenderTargets;
using Monochrome.Core.Graphics;
using Monochrome.Common.MonoUtil;


namespace FargosPhantasmMode
{
    [MonoShaderScope]
    public class FargosPhantasmMode : Mod
    {
        internal static FargosPhantasmMode Instance;
        public static MonoManagedRenderTarget Rt;
        public static Mod FargoMod;
        public override void Load()
        {
            ModLoader.TryGetMod("FargowiltasSouls", out FargoMod);

            //注册网络同步相关
            PModeNet.Register();

            //注册元素倍率
            PModeDamageRules.Register();

            Rt = MonoRt.ScreenSized();

            Instance = this;
        }

        /// <summary>
        /// 订阅渲染钩子。不能放在 <see cref="Load"/> 里：Monochrome 要等它自己的
        /// PostSetupContent 才扫着色器，而加载画面在主线程上会先命中这个钩子，
        /// 那时注册表还是空的。Monochrome 排在 FPM 之前，走到这里 FPM 的着色器已经注册好了。
        /// </summary>
        public override void PostSetupContent()
        {
            On_FilterManager.EndCapture += FilterManager_EndCapture;
        }
        public override void Unload()
        {
            On_FilterManager.EndCapture -= FilterManager_EndCapture;
            Rt?.Retire();
            Rt = null;
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
            // 取不到就跳过这层着色，不要抛：热重载等场合注册表可能暂时没有它。
            if (MonoShaderManager.TryGet("FargosPhantasmMode.BigTentacle", out var shader) && shader is not null)
            {
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                gd.Textures[1] = PhantasmTextureRegistry.UniverseNoise.Value;
                shader.TrySetParameter("color", new Color(54, 255, 236));//102, 26, 179���ϣ�  54��255��236(��)
                shader.TrySetParameter("m", 0.62f);
                shader.TrySetParameter("n", 0.01f);
                shader.Apply();
                sb.Draw(Rt, Vector2.Zero, Color.White);
                sb.End();
            }
            #endregion

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
    }
}
