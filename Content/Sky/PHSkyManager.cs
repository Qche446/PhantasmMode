using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Effects;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Sky
{
    public class PHSkyManager : ModSystem
    {
        public override void Load()
        {
            if (!Main.dedServ)
            {
                SkyManager.Instance["FargosPhantasmMode:AbomSky"] = new AbomSky();
                SkyManager.Instance["FargosPhantasmMode:DestroyerFlashSky"] = new DestroyerFlashSky();
                SkyManager.Instance["FargosPhantasmMode:MutantSky3"] = new MutantSky3();
                SkyManager.Instance["FargosPhantasmMode:AbomSky2"] = new AbomSky2();
            }
        }
    }
}
