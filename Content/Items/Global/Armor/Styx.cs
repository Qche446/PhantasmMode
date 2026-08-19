using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Projectiles;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Armor;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Global.Armor
{
    public class StyxCrownOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation) => lateInstantiation && (item.type == ModContent.ItemType<StyxChestplate>() || item.type == ModContent.ItemType<StyxCrown>() || item.type == ModContent.ItemType<StyxLeggings>());
        public override void Load()
        {
            PhanUtil.AddHooks(StyxCrown.StyxSetBonusKey, StyxBonusFixed);
        }
        private static void StyxBonusFixed(Action<Player> orig, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (modPlayer.StyxSet && player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[ModContent.ProjectileType<StyxGazerArmor>()] <= 0)
            {
                int scytheType = ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.StyxArmorScythe>();
                bool superAttack = modPlayer.StyxAttackReadyTimer > 0;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].friendly && Main.projectile[i].type == scytheType && Main.projectile[i].owner == player.whoAmI)
                    {
                        if (!superAttack)
                        {
                            Projectile.NewProjectile(Main.projectile[i].GetSource_FromThis(), Main.projectile[i].Center, Vector2.Normalize(Main.projectile[i].velocity) * 24f, ModContent.ProjectileType<FargowiltasSouls.Content.Projectiles.StyxArmorScythe2>(),
                                Main.projectile[i].damage, Main.projectile[i].knockBack, player.whoAmI, -1, -1);
                        }

                        Main.projectile[i].Kill();
                    }
                }
                if (superAttack)
                {
                    Vector2 speed = Vector2.Normalize(Main.MouseWorld - player.Center);
                    bool flip = speed.X < 0;
                    speed = speed.RotatedBy(MathHelper.PiOver2 * (flip ? 1 : -1));
                    Projectile.NewProjectile(player.GetSource_Misc(""), player.Center, speed, ModContent.ProjectileType<StyxGazerArmor>(), 0, 14f, player.whoAmI, MathHelper.Pi / 120 * (flip ? -1 : 1));
                    if (PModeWorldSavingSystem.PhantasmMode)
                    {
                        Projectile.NewProjectile(player.GetSource_Misc(""), player.Center, -speed, ModContent.ProjectileType<StyxGazerArmor>(), 0, 14f, player.whoAmI, MathHelper.Pi / 120 * (flip ? -1 : 1));
                    }
                    player.controlUseItem = false; //this kills other heldprojs
                    player.releaseUseItem = true;
                    modPlayer.StyxAttackReadyTimer = 0;
                }
            }
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            int damage = FargoSoulsUtil.HighestDamageTypeScaling(Main.LocalPlayer, 666);
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Armor.Styx", damage, 666))
                {
                    OverrideColor = Color.Aqua // 可选：设置颜色
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
    }
}

