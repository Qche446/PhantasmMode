using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static FargosPhantasmMode.Core.Systems.PModeWorldSavingSystem;
using static FargowiltasSouls.Core.Systems.WorldSavingSystem;
using Luminance.Common.Utilities;

namespace FargosPhantasmMode.Core.Systems
{
    public class WorldUpdateSystem : ModSystem
    {
        public override void PostUpdateWorld()
        {
            // 世界级开关只由权威端改，客户端从 MonoNet 收。原先客户端也自己算一遍，再被整份 WorldData 覆盖。
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (!PhantasmMode && EternityMode && MasochistModeReal && CanPlayPhantasm && !Utilities.AnyBosses())
            {
                // 赋值本身就把新值发出去了（值变才发），播报写在字段的 OnChanged 里，两端各播一次。
                PhantasmMode = true;
            }

            if (PhantasmMode && !(MasochistModeReal && CanPlayPhantasm))
                PhantasmMode = false;

            if (!MasochistModeReal)
                CanPlayPhantasm = false;
        }
    }
}
