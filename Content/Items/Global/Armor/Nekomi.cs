using FargosPhantasmMode.Common;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Armor;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Global.Armor
{
    public class NekomiOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation) => lateInstantiation && (item.type == ModContent.ItemType<NekomiHood>() || item.type == ModContent.ItemType<NekomiHoodie>() || item.type == ModContent.ItemType<NekomiLeggings>());
        public override void Load()
        {
            PhanUtil.AddHooks(NekomiHood.NekomiSetBonusKey, NekomiBonusFixed);
        }
        private static void NekomiBonusFixed(Action<Player> orig, Player player)
        {
            if (!PModeWorldSavingSystem.PhantasmMode)
            {
                orig.Invoke(player);
                return;
            }
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (modPlayer.NekomiSet && player.whoAmI == Main.myPlayer)
            {
                bool superAttack = modPlayer.NekomiAttackReadyTimer > 0;
                if (superAttack)
                {
                    int baseDamage = NekomiDamage();
                    int p = FargoSoulsUtil.NewSummonProjectile(player.GetSource_Misc(""), player.Center, Vector2.Zero, ModContent.ProjectileType<NekomiDevi>(), baseDamage, 16f, player.whoAmI);
                    if (NPC.downedMoonlord && PModeWorldSavingSystem.PhantasmMode) Main.projectile[p].scale *= 2;
                    SoundEngine.PlaySound(SoundID.Item43, player.Center);
                    modPlayer.NekomiMeter = 0;
                    modPlayer.NekomiAttackReadyTimer = 0;
                }
                else
                {
                    int hearts = (int)((double)modPlayer.NekomiMeter / NekomiHood.MAX_METER * NekomiHood.MAX_HEARTS);
                    for (int i = 0; i < hearts; i++)
                    {
                        Vector2 offset = -150f * Vector2.UnitY.RotatedBy(MathHelper.TwoPi / hearts * i);
                        Vector2 spawnPos = player.Center + offset;
                        const float speed = 12;
                        Vector2 vel = speed * player.DirectionFrom(spawnPos);
                        int baseHeartDamage = 17;
                        const float ai1 = 150 / speed;
                        FargoSoulsUtil.NewSummonProjectile(player.GetSource_Misc(""), spawnPos, vel, ModContent.ProjectileType<FriendHeart>(), baseHeartDamage, 3f, player.whoAmI, -1, ai1);
                    }

                    if (hearts > 0)
                        modPlayer.NekomiMeter = 0;
                }
            }
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            int baseDamage = 660;
            if (!Main.hardMode)
                baseDamage /= 2;
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                baseDamage = NekomiDamage();
            }
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Armor.Nekomi") + baseDamage + "(" + 666 + ")")
                {
                    OverrideColor = Color.Aqua // 可选：设置颜色
                };
                tooltips.Add(extraLine);
            }
        }
        public static int NekomiDamage()
        {
            int baseDamage = 333;
            if (Main.hardMode) baseDamage = 666;
            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3) baseDamage = 999;
            if (NPC.downedPlantBoss) baseDamage = 1200;
            if (NPC.downedGolemBoss) baseDamage = 2000;
            if (NPC.downedMoonlord) baseDamage = 6000;
            if (WorldSavingSystem.downedAbom) baseDamage = 10000;
            return baseDamage;
        }
    }
}