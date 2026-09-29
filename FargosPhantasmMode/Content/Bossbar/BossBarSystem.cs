using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bossbar
{
    public class BossBarSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            BossBarRegistry.Initialize();
            BossBarRegistry.RegisterAllBossBar();
        }
        public override void Unload()
        {
            BossBarRegistry.Unload();
        }
    }
}
