using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.Systems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using FargowiltasSouls;
using Microsoft.Xna.Framework;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Terraria.Localization;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.ModPlayers;
using FargosPhantasmMode.Content.Projectiles.Masomode;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class AgitatingLensOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<AgitatingLens>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.AgitatingLens"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (item.type == ModContent.ItemType<AgitatingLens>() && WorldSavingSystem.masochistModeReal)
            {
                player.moveSpeed += (player.statLife <= player.statLifeMax2 / 2f) ? 0.3f : 0.1f;
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class AgitatingLensScycle : ModSystem
    {
        public override void Load()
        {
            MethodInfo method1 = typeof(AgitatingLensEffect).GetMethod("PostUpdateEquips", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(method1,ILAgitatingLensScycle);
        }
        private void ILAgitatingLensScycle(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<Player>>(player =>
            {
                FargoSoulsPlayer modPlayer = player.FargoSouls();
                if (modPlayer.AgitatingLensCD++ > 30)
                {
                    modPlayer.AgitatingLensCD = 0;
                    if ((Math.Abs(player.velocity.X) >= 5 || Math.Abs(player.velocity.Y) >= 5) && player.whoAmI == Main.myPlayer)
                    {
                        int damage = 18;
                        if (modPlayer.SupremeDeathbringerFairy)
                            damage *= 2;
                        if (modPlayer.MasochistSoul)
                            damage *= 2;
                        damage = (int)(damage * player.ActualClassDamage(DamageClass.Magic));
                        Projectile.NewProjectile(ModContent.GetInstance<AgitatingLensEffect>().GetSource_EffectItem(player), player.Center, player.velocity * 0.1f, ModContent.ProjectileType<PHBloodScytheFriendly>(), damage, 5f, player.whoAmI);
                    }
                }
            });
            c.Emit(OpCodes.Ret);
        }
    }
}
