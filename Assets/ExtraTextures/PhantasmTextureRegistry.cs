using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Assets.ExtraTextures
{
    public class PhantasmTextureRegistry
    {
        public static Asset<Texture2D> FireNoise => ModContent.Request<Texture2D>("FargosPhantasmMode/Assets/ExtraTextures/Noise/FireNoise");
        public static Asset<Texture2D> FireNoise2 => ModContent.Request<Texture2D>("FargosPhantasmMode/Assets/ExtraTextures/Noise/FireNoise2");
        //public static Asset<Texture2D> Neurons2 => ModContent.Request<Texture2D>("FargosPhantasmMode/Assets/ExtraTextures/Noise/Neurons2");
        //public static Asset<Texture2D> SparkNoise => ModContent.Request<Texture2D>("FargosPhantasmMode/Assets/ExtraTextures/Noise/SparkNoise");
        public static Asset<Texture2D> UniverseNoise => ModContent.Request<Texture2D>("FargosPhantasmMode/Assets/ExtraTextures/Noise/UniverseNoise");

    }
}
