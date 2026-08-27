using FargosPhantasmMode.Content.Items.Accessories;
using FargowiltasSouls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.InfoDisplays
{
    public class DotInfoDisplay : InfoDisplay
    {
        public static LocalizedText CurrentDotDps { get; private set; }
        public static LocalizedText NoDotDps { get; private set; }

        public override void SetStaticDefaults()
        {
            CurrentDotDps = this.GetLocalization("HasDotDamage");
            NoDotDps = this.GetLocalization("NoDotDamage");
        }
        public override bool Active() => Main.LocalPlayer.GetModPlayer<InfoModPlayer>().ShowDotsDps;
        public override string DisplayValue(ref Color displayColor, ref Color displayShadowColor)
        {
            Player py = Main.LocalPlayer;
            var ip = py.GetModPlayer<InfoModPlayer>();
            bool noinfo = ip.targetIndex == -1;
            if (!noinfo)
                noinfo = Main.npc[ip.targetIndex].lifeRegen >= 0;
            string result = NoDotDps.Value;
            if (noinfo)
                displayColor = Color.Gray;
            else
            {
                displayColor = Color.White;
                float speed = Main.npc[ip.targetIndex].lifeRegen;
                if (py.HasEffect<GermDotEffect>() && speed < 0)
                    speed *= 1 + 0.5f * (py.FargoSouls().AttackSpeed - 1);
                result = CurrentDotDps.Format(Math.Round(Math.Abs(speed), 1));
            }
            return result;
        }
    }
}
