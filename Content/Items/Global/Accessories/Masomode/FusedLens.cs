using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using FargowiltasSouls.Core.Toggler;
using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.ModPlayers;
using System;
using FargosPhantasmMode.Content.Projectiles.Masomode;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class FusedLensOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<FusedLens>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.FusedLens"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<FusedLensMechElectricOrbEffect>(item);
            }
            base.UpdateAccessory(item, player, hideVisual);
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
            if (player.whoAmI == Main.myPlayer)
            {
                FargoSoulsPlayer modPlayer = player.FargoSouls();
                int currentOrbs = player.ownedProjectileCounts[ModContent.ProjectileType<FusedLensMechElectricOrb>()];
                int damage = FargoSoulsUtil.HighestDamageTypeScaling(player, 30);
                int max = 2;
                bool dubiouscircuitry = modPlayer.DubiousCircuitry;
                bool masosoul = modPlayer.MasochistSoul;

                if (masosoul)
                {
                    max = 8;
                    damage *= 6;
                }
                else if (dubiouscircuitry) //可疑电路
                {
                    max = 4;
                    damage = FargoSoulsUtil.HighestDamageTypeScaling(player, 40);
                }

                //spawn for first time
                if (currentOrbs == 0 && Timer >= TimerCD)
                {
                    float rotation = 2f * (float)Math.PI / max;

                    for (int i = 0; i < max; i++)
                    {
                        Vector2 spawnPos = player.Center + new Vector2(60, 0f).RotatedBy(rotation * i);
                        Vector2 vel = (spawnPos - player.Center).RotatedBy(MathHelper.PiOver2);
                        int p = Projectile.NewProjectile(player.GetSource_Misc(""), spawnPos, vel / 4, ModContent.ProjectileType<FusedLensMechElectricOrb>(), damage, 10f, player.whoAmI, 0, ai2 : (i + 2) % 4);
                        Main.projectile[p].FargoSouls().CanSplit = false;
                    }
                    Timer = 0;
                }
                
                Timer++;
            }
        }
    }
}
