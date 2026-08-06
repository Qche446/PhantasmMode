using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Will
{
    public class WillPlayer : ModPlayer
    {
        public int WillJavelinCD = 0;
        public override void ResetEffects()
        {
            if (WillJavelinCD > 0)
                WillJavelinCD--;
        }
    }
}
