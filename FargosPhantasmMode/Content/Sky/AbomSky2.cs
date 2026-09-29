using FargosPhantasmMode.Assets.ExtraTextures;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Sky
{
    public class AbomSky2 : CustomSky
    {
        private bool isActive;
        private float intensity;
        private int abomBossType = -1;
        Texture2D skyTexture;
        ManagedShader shader;
        Texture2D noiseTexture;

        public override void OnLoad()
        {
            // 获取我们自己的AbomBoss类型
            abomBossType = ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>();
        }

        public override void Update(GameTime gameTime)
        {
            // 检查我们自己的AbomBoss是否存活
            if (NPC.AnyNPCs(abomBossType))
            {
                this.intensity += 0.01f;
                if (this.intensity > 1f)
                {
                    this.intensity = 1f;
                    return;
                }
            }
            else
            {
                this.intensity -= 0.01f;
                if (this.intensity < 0f)
                {
                    this.intensity = 0f;
                    this.Deactivate();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0f && minDepth < 0f)
            {
                skyTexture = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/AbomSky", AssetRequestMode.ImmediateLoad).Value;
                float opacity = 0.5f;
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.Transform);
                spriteBatch.Draw(skyTexture,
                    new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * opacity * 1.5f);
                spriteBatch.End();

                // 设置着色器参数
                noiseTexture = PhantasmTextureRegistry.FireNoise2.Value;
                var blackTile = TextureAssets.MagicPixel;
                shader = ShaderManager.GetShader("FargosPhantasmMode.AbomSkyEffect");
                Main.spriteBatch.GraphicsDevice.Textures[1] = noiseTexture;
                shader.TrySetParameter("maxOpacity", this.intensity * 0.6f);
                shader.TrySetParameter("time", (float)Main.GlobalTimeWrappedHourly);
                shader.TrySetParameter("verticalPower", this.intensity * 2f);
                shader.TrySetParameter("screenPosition", Main.screenPosition);
                shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());

                shader.TrySetParameter("flowDir1", new Vector2(8, 5.4f).RotatedByRandom(0f));
                shader.TrySetParameter("flowDir2", new Vector2(2f, 8));
                shader.TrySetParameter("flowDir3", new Vector2(-8, 4.5f));

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
                Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);

                spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);

            }
        }


        public override float GetCloudAlpha()
        {
            return 1f - this.intensity;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            this.isActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            this.isActive = false;
        }

        public override void Reset()
        {
            this.isActive = false;
            this.intensity = 0f;
        }

        public override bool IsActive()
        {
            return this.isActive;
        }

        public override Color OnTileColor(Color inColor)
        {
            return new Color(Vector4.Lerp(new Vector4(1f, 0.9f, 0.6f, 1f), inColor.ToVector4(), 1f - this.intensity));
        }
    }
}
