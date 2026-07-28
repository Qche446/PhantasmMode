using FargowiltasSouls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using static FargosPhantasmMode.Core.Systems.PModeWorldSavingSystem;
using static FargowiltasSouls.Core.Systems.WorldSavingSystem;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;

namespace FargosPhantasmMode.Core.Systems
{
    public class WorldUpdateSystem : ModSystem
    {
        public override void PostUpdateWorld()
        {
            if (!PhantasmMode && EternityMode && MasochistModeReal && CanPlayPhantasm && !Utilities.AnyBosses())
            {
                PhantasmMode = true;
                FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.UI.PhantasmOn", new Color(51, 255, 191, 0));
                if (Main.getGoodWorld)
                    FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.UI.PhantasmFTWWarning", new Color(51, 255, 191, 0));
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/DifficultyMaso") with { Volume = 0.5f }, Main.LocalPlayer.Center);
            }
            if (PhantasmMode && !(MasochistModeReal && CanPlayPhantasm))
            {
                PhantasmMode = false;
                FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.UI.PhantasmOff", new Color(51, 255, 191, 0));
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/DifficultyDeactivate"), Main.LocalPlayer.Center);
            }
            if (!MasochistModeReal)
                CanPlayPhantasm = false;
        }
    }
}
