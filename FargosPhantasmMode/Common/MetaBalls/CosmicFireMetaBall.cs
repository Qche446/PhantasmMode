using FargosPhantasmMode.Assets.ExtraTextures;
using FargosPhantasmMode.Common.Particles;
using Monochrome.Core.Graphics.Metaballs;
using Monochrome.Core.Graphics.Shaders;

namespace FargosPhantasmMode.Common.MetaBalls
{
    /// <summary>
    /// 幽火融合球：一颗会减速、收缩的紫色火点，同一层的球彼此粘连。
    /// <para>
    /// 写场用 FSS 粒子图集里的 <c>MetaballBase</c> 软圆（与粒子同一套取图方式，见
    /// <see cref="FargoParticleAtlas"/>）；合成不走内置那把边着色器，而是自己覆写
    /// <see cref="PrepareShaderForTarget"/>：用 <c>FargosPhantasmMode.BigTentacle</c>，
    /// 把宇宙噪声当材质、<c>color</c> / <c>m</c> / <c>n</c> 决定火焰的色调与形态。
    /// </para>
    /// <para>
    /// 写场的贴图绑在 s0（由绘制方自动绑上，就是这一层的累积目标），所以噪声从 s1 起绑。
    /// </para>
    /// </summary>
    public class CosmicFireMetaBall : MonoMetaballType
    {
        private const string FlameShaderName = "FargosPhantasmMode.BigTentacle";

        private static bool warnedMissingShader;

        /// <summary>写场用的软圆：FSS 粒子图集里的 <c>MetaballBase</c>。</summary>
        public override Func<Texture2D> FieldTexture => () => FargoParticleAtlas.Texture;

        /// <summary>上面那张图里要用的一块。</summary>
        public override Rectangle? FieldTextureFrame => FargoParticleAtlas.Frame("MetaballBase");

        /// <summary>
        /// 层数由这里决定（一张累积目标）。合成被 <see cref="PrepareShaderForTarget"/> 接管了，
        /// 所以这张材质其实不会被采样，但层数还是得有。
        /// </summary>
        public override Func<Texture2D>[] LayerTextures =>
        [
            () => ModContent.Request<Texture2D>("FargowiltasSouls/Assets/Textures/Metaballs/DarkBluePixel").Value
        ];

        /// <summary>贴着轮廓那一圈的颜色。</summary>
        public override Color EdgeColor => Color.Purple;

        /// <summary>幽火的材质吃镜头视差，所以不固定贴屏幕。</summary>
        public override bool LayerIsFixedToScreen(int layerIndex) => false;

        /// <summary>缩到很小就消失。</summary>
        public override bool ShouldKillParticle(MonoMetaballInstance particle) => particle.Size <= 4f;

        /// <summary>会减速、会收缩。</summary>
        public override void UpdateParticle(MonoMetaballInstance particle)
        {
            particle.Velocity *= 0.99f;
            particle.Size *= 0.97f;
        }

        /// <summary>
        /// 这一层自己的合成：<c>BigTentacle</c> 拿融合场当遮罩，用宇宙噪声与色调画成火焰。
        /// 着色器热重载/未注册时安静跳过，不抛。
        /// </summary>
        public override void PrepareShaderForTarget(int layerIndex)
        {
            if (!MonoShaderManager.TryGet(FlameShaderName, out MonoShader shader) || shader is null)
            {
                if (!warnedMissingShader)
                {
                    warnedMissingShader = true;
                    Mod.Logger.Warn($"着色器 {FlameShaderName} 尚未注册，幽火融合球暂时不画材质。");
                }
                return;
            }
            var texture2D = PhantasmTextureRegistry.UniverseNoise;
            shader.SetParameter("color", new Color(57, 10, 110))//102, 26, 179（紫）  54, 255, 236（青）
                  .SetParameter("m", 0.62f)
                  .SetParameter("n", 0.01f)
                  .SetTexture(PhantasmTextureRegistry.UniverseNoise, 1, SamplerState.LinearWrap)
                  .SetTexture(texture2D.Value, 1, SamplerState.LinearWrap)
                  .Apply();
        }
    }
}
