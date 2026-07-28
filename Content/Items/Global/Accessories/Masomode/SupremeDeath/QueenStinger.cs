using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.SupremeDeath
{
    public class QueenStingerOverride : PModeGlobalMasoItem<QueenStinger>
    {
        public override void SafeUpdateInPack(Item item, Player player)
        {
            player.AddBuff(BuffID.Honey, 2);
        }
    }
    public class BeeDashTimerPlayer : ModPlayer
    {
        public int CheckNohitTimer = 0;
        public int TrueDashTime = 0;
        public override void PostUpdateEquips()
        {
            Player py = Main.LocalPlayer;
            FargoSoulsPlayer fp = py.FargoSouls();
            if (CheckNohitTimer == 1 && PModeWorldSavingSystem.PhantasmMode)
                fp.SpecialDashCD -= 3 * 60;
            if (CheckNohitTimer > 0)
                CheckNohitTimer--;
            if (TrueDashTime > 0)
                TrueDashTime--;
        }
        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (PModeWorldSavingSystem.PhantasmMode && TrueDashTime > 0 && !proj.Colliding(proj.Hitbox, PrecisionSealPlayer.GetPrecisionHurtbox()))
                return false;
            return true;
        }
        public override void OnHurt(Player.HurtInfo info)
        {
            CheckNohitTimer = 0;
        }
    }
}
