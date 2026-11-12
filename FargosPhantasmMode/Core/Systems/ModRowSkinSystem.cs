using FargosPhantasmMode.Content.UI;
using Monochrome.Core.UI;

namespace FargosPhantasmMode.Core.Systems
{
    /// <summary>把 FPM 那一行的皮肤挂到 Monochrome 的行皮肤注册表上。</summary>
    public class ModRowSkinSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            MonoRowSkins.Register(PhantasmModRowSkin.ModName, PhantasmModRowSkin.Draw, PhantasmModRowSkin.DrawName);
        }

        public override void Unload()
        {
            MonoRowSkins.Unregister(PhantasmModRowSkin.ModName);
        }
    }
}
