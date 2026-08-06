using FargowiltasSouls;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class ShadowPlayer : ModPlayer
    {
        public bool CanReduceMonkCD = true;
        public bool ActualReduceMonoCD = false;
        public override void PostUpdateEquips()
        {
            if (CanReduceMonkCD && ActualReduceMonoCD)
            {
                CanReduceMonkCD = false;
                ActualReduceMonoCD = false;
                Player.FargoSouls().DashCD -= 45;
                Player.dashDelay -= 45;
            }
            ActualReduceMonoCD = false;
            if (Player.dashDelay <= 0 || Player.FargoSouls().DashCD <= 0)
                CanReduceMonkCD = true;
        }
    }
}
