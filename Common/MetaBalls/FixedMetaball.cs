using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Common.MetaBalls
{
    public abstract class FixedMetaball<T> : MetaballType where T : FixedMetaball<T>
    {
        public override bool ShouldRender => ActiveParticleCount >= 1;
        public override bool DrawnManually => true;
        public override bool LayerIsFixedToScreen(int layerIndex) => true;
        public override void Load()
        {
            LuMetaBallDrawfixed.NewMetaBallRtEvent += FixedDraw;
        }
        public override void Unload()
        {
            LuMetaBallDrawfixed.NewMetaBallRtEvent -= FixedDraw;
        }
        public virtual void FixedDraw()
        {
            T instance = ModContent.GetInstance<T>();
            if (instance.ShouldRender)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < instance.LayerTargets.Count; i++)
                {
                    instance.PrepareShaderForTarget(i);
                    Main.spriteBatch.Draw(instance.LayerTargets[i], Main.screenLastPosition - Main.screenPosition, Color.White);
                }
                Main.spriteBatch.End();
            }
        }
    }
}
