using FargosPhantasmMode.Content.Projectiles.Masomode;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Circuitry
{
    public class FusedLensOverride : PModeGlobalMasoItem<FusedLens>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<FusedLensMechElectricOrbEffect>(item);
        }
    }
    public class FusedLensMechElectricOrbEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<DubiousHeader>();
        public override int ToggleItemType => ModContent.ItemType<FusedLens>();
        public override bool ExtraAttackEffect => true;
        public int Timer = 0;
        public int TimerCD = 300;
        public override void PostUpdateEquips(Player player)
        {
            if (player.whoAmI == Main.myPlayer && !player.FargoSouls().MutantDesperation)
            {
                FargoSoulsPlayer modPlayer = player.FargoSouls();
                int currentOrbs = player.ownedProjectileCounts[ModContent.ProjectileType<FusedLensMechElectricOrb>()] + player.ownedProjectileCounts[ModContent.ProjectileType<FusedLensDarkStar>()];
                int damage = 40;
                int max = 2;
                bool dubiouscircuitry = modPlayer.DubiousCircuitry;
                bool masosoul = modPlayer.MasochistSoul;

                if (masosoul)
                {
                    max = 8;
                    damage *= 15;
                }
                else if (dubiouscircuitry) //可疑电路
                {
                    max = 4;
                    damage = 60;
                }

                //spawn for first time
                if (currentOrbs == 0 && Timer >= TimerCD)
                {
                    float rotation = 2f * (float)Math.PI / max;

                    for (int i = 0; i < max; i++)
                    {
                        Vector2 spawnPos = player.Center + new Vector2(60, 0f).RotatedBy(rotation * i);
                        Vector2 vel = (spawnPos - player.Center).RotatedBy(MathHelper.PiOver2);
                        int projType = player.ichor || player.onFire2 ? ModContent.ProjectileType<FusedLensDarkStar>() : ModContent.ProjectileType<FusedLensMechElectricOrb>();
                        int p = Projectile.NewProjectile(player.GetSource_FromThis(), spawnPos, vel / 4, projType, damage, 10f, player.whoAmI, 0, ai2: (i + 2) % 4);
                        Main.projectile[p].FargoSouls().CanSplit = false;
                    }
                    Timer = 0;
                }

                Timer++;
            }
        }
    }
}
