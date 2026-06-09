using Terraria;
using Terraria.ModLoader;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Armor;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using System.Reflection;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;
using static Terraria.Player;
using FargosPhantasmMode.Content.Projectiles;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Content.Projectiles.Minions;
using Terraria.Audio;
using Terraria.ID;
using System.Collections.Generic;
using Terraria.Localization;
namespace FargosPhantasmMode.Content.Items.Global.Armor
{
    public class NekomiOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            int baseDamage = FargoSoulsUtil.HighestDamageTypeScaling(Main.LocalPlayer, 666);
            if (!Main.hardMode)
                baseDamage /= 2;
            if (WorldSavingSystem.masochistModeReal)
            {
                if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3) baseDamage *= 2;
                if (NPC.downedPlantBoss) baseDamage = (int)(2f * baseDamage);
                if (NPC.downedGolemBoss) baseDamage = (int)(2f * baseDamage);
                if (NPC.downedMoonlord) baseDamage = (int)(2.5f * baseDamage);
                if (WorldSavingSystem.downedAbom) baseDamage = (int)(1f * baseDamage);
            }
            if (WorldSavingSystem.masochistModeReal && (item.type == ModContent.ItemType<NekomiHood>() || item.type == ModContent.ItemType<NekomiHoodie>() || item.type == ModContent.ItemType<NekomiLeggings>()))
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Armor.Nekomi") + baseDamage + "(" + 666 + ")")
                {
                    OverrideColor = Color.Aqua // 可选：设置颜色
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
    }
    public class NekomiSetBonusKeyOverride : ModSystem
    {
        public override void Load()
        {
            MethodInfo targetMethod1 = typeof(NekomiHood).GetMethod("NekomiSetBonusKey", BindingFlags.Static | BindingFlags.Public);
            MonoModHooks.Modify(targetMethod1, ILNekomiBonus);
        }
        private void ILNekomiBonus(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            // 推入静态方法的参数：ldarg_0
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>((player) =>
            {
                FargoSoulsPlayer modPlayer = player.FargoSouls();
                if (modPlayer.NekomiSet && player.whoAmI == Main.myPlayer)
                {
                    bool superAttack = modPlayer.NekomiAttackReadyTimer > 0;
                    if (superAttack)
                    {
                        int baseDamage = FargoSoulsUtil.HighestDamageTypeScaling(player, 666);
                        if (!Main.hardMode)
                            baseDamage /= 2;
                        if (WorldSavingSystem.masochistModeReal)
                        {
                            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3) baseDamage *= 2;
                            if (NPC.downedPlantBoss) baseDamage = (int)(2f * baseDamage);
                            if (NPC.downedGolemBoss) baseDamage = (int)(1.2f * baseDamage);
                            if (NPC.downedMoonlord) baseDamage = (int)(4f * baseDamage);
                            if (WorldSavingSystem.downedAbom) baseDamage = (int)(1f * baseDamage);
                        }
                        int p = FargoSoulsUtil.NewSummonProjectile(player.GetSource_Misc(""), player.Center, Vector2.Zero, ModContent.ProjectileType<NekomiDevi>(), baseDamage, 16f, player.whoAmI);
                        if (NPC.downedMoonlord && WorldSavingSystem.masochistModeReal) Main.projectile[p].scale *= 2;
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
            });
            c.Emit(OpCodes.Ret);
        }
    }
}