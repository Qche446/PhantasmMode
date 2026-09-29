using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Sky
{
    public class PhantasmModeMenuScreen : ModMenu
    {
        bool forgor = false;

        public override int Music => MusicLoader.GetMusicSlot("FargosPhantasmMode/Assets/Music/Sketchbook");
        public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<MainMenuBackgroundSky>();

        public override string DisplayName => Language.GetTextValue("Mods.FargosPhantasmMode.UI.MainMenu");

        public override void OnSelected()
        {
            ((MainMenuBackgroundSky)MenuBackgroundStyle).fadeIn = 0;
            forgor = Main.rand.NextBool(100);
        }
    }
}
