using FargosPhantasmMode.Assets.ExtraTextures;
using FargowiltasSouls.Assets.ExtraTextures;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Common.MetaBalls
{
    public class CosmicFireMetaBall : FixedMetaball 
    {
        public override bool ShouldRender => ActiveParticleCount >= 1;
        public override Func<Texture2D>[] LayerTextures => new Func<Texture2D>[1] 
        {
            () => ModContent.Request<Texture2D>("FargowiltasSouls/Assets/Textures/Metaballs/DarkBluePixel").Value 
        };
        public override Color EdgeColor => Color.Purple;
        public override bool DrawnManually => true;
        public override string MetaballAtlasTextureToUse => "FargowiltasSouls.MetaballBase";
        public override bool LayerIsFixedToScreen(int layerIndex) => false;
        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 4f;
        public override void UpdateParticle(MetaballInstance particle)
        {
            particle.Velocity *= 0.99f;
            particle.Size *= 0.97f;
        }
        public override void PrepareShaderForTarget(int layerIndex)
        {
            ManagedShader shader = ShaderManager.GetShader("FargosPhantasmMode.BigTentacle");
            var texture2D = PhantasmTextureRegistry.UniverseNoise;
            shader.TrySetParameter("color", new Color(57, 10, 110));//102, 26, 179（紫）  54，255，236(青)
            shader.TrySetParameter("m", 0.62f);
            shader.TrySetParameter("n", 0.01f);
            shader.SetTexture(texture2D.Value, 1, SamplerState.LinearWrap);
            shader.Apply();
            
            //shader.Apply();
        }
    }
}
