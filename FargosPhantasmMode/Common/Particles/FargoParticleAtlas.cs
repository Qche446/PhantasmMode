using System.Collections.Generic;
using System.Text.Json;
using ReLogic.Content;

namespace FargosPhantasmMode.Common.Particles
{
    /// <summary>
    /// 取 FSS 的粒子图集（<c>FargowiltasSouls/Assets/Atlases/ParticleAtlas</c>）以及其中各张贴图的子矩形。
    /// <para>
    /// FPM 的粒子贴图都从这张图集里取，不另外存一份；矩形是读图集自带的 json 得来的，
    /// 所以 FSS 重新打包图集之后这里会跟着变，不必手改常量。解析失败时退回整张图集
    /// （贴图仍能画出来，只是会成为整图）。
    /// </para>
    /// </summary>
    public static class FargoParticleAtlas
    {
        private const string AtlasAssetPath = "FargowiltasSouls/Assets/Atlases/ParticleAtlas";
        private const string JsonFilePath = "Assets/Atlases/ParticleAtlas.json";

        private static Texture2D atlasTexture;
        private static Dictionary<string, Rectangle> frames;
        private static bool attempted;

        /// <summary>图集纹理；FSS 不在场或尚未加载好时为 <see langword="null"/>。</summary>
        public static Texture2D Texture
        {
            get
            {
                EnsureLoaded();
                return atlasTexture;
            }
        }

        /// <summary>取某张贴图在图集里的子矩形；取不到时返回整张图集的矩形。</summary>
        /// <param name="name">图集里的贴图名，例如 <c>SparkParticle</c>。</param>
        public static Rectangle Frame(string name)
        {
            EnsureLoaded();
            if (frames is not null && frames.TryGetValue(name, out Rectangle frame))
                return frame;
            return atlasTexture?.Bounds ?? Rectangle.Empty;
        }

        private static void EnsureLoaded()
        {
            if (attempted)
                return;
            attempted = true;

            if (!ModLoader.TryGetMod("FargowiltasSouls", out Mod fargo))
                return;

            try
            {
                atlasTexture = ModContent.Request<Texture2D>(AtlasAssetPath, AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                return;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(fargo.GetFileBytes(JsonFilePath));
                Dictionary<string, Rectangle> parsed = [];
                foreach (JsonProperty entry in document.RootElement.GetProperty("frames").EnumerateObject())
                {
                    JsonElement frame = entry.Value.GetProperty("frame");
                    parsed[entry.Name] = new Rectangle(
                        frame.GetProperty("x").GetInt32(),
                        frame.GetProperty("y").GetInt32(),
                        frame.GetProperty("w").GetInt32(),
                        frame.GetProperty("h").GetInt32());
                }
                frames = parsed;
            }
            catch
            {
                // 图集解析不了就按整图用，画面会大一圈但不会崩。
            }
        }
    }
}
