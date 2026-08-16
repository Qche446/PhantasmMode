
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace FargosPhantasmMode.Core.Config
{
    public class BaseConfig : ModConfig
    {
        public static BaseConfig Instance;
        public override void OnLoaded()
        {
            Instance = this;
        }
        public override ConfigScope Mode => ConfigScope.ClientSide;
        [DefaultValue(true)]
        public bool ExtraCoolDownBar;
    }
}
