using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.InfoDisplays
{
    public class GrazeInfoDisplay : InfoDisplay
    {
        public static LocalizedText CurrentGrazeCount { get; private set; }
        public static LocalizedText NoGrazeCount { get; private set; }

        public override void SetStaticDefaults()
        {
            CurrentGrazeCount = this.GetLocalization("HasGrazeCount");
            NoGrazeCount = this.GetLocalization("NoGrazeCount");
        }
        public override bool Active() => Main.LocalPlayer.GetModPlayer<InfoModPlayer>().ShowGrazeCount;
        public override string DisplayValue(ref Color displayColor, ref Color displayShadowColor)
        {
            Player py = Main.LocalPlayer;
            var ip = py.GetModPlayer<InfoModPlayer>();
            bool noinfo = ip.GrazeCount == 0;
            if (noinfo)
                displayColor = Color.Gray;
            else
                displayColor = Color.White;
            return noinfo ? NoGrazeCount.Value : CurrentGrazeCount.Format(ip.GrazeCount);
        }
    }
}
