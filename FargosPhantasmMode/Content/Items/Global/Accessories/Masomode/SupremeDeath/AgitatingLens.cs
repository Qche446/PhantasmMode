using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Projectiles.Masomode;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.SupremeDeath
{
    public class AgitatingLensOverride : PModeGlobalMasoItem<AgitatingLens>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.moveSpeed += player.statLife <= player.statLifeMax2 / 2f ? 0.3f : 0.1f;
        }
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<AgitatingLensEffect>().PostUpdateEquips, ReplaceScycle);
        }
        private static void ReplaceScycle(Action<AgitatingLensEffect, Player> orig, AgitatingLensEffect self, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (modPlayer.AgitatingLensCD++ > 30)
            {
                modPlayer.AgitatingLensCD = 0;
                if ((Math.Abs(player.velocity.X) >= 5 || Math.Abs(player.velocity.Y) >= 5) && player.whoAmI == Main.myPlayer)
                {
                    int projType = PModeWorldSavingSystem.PhantasmMode ? ModContent.ProjectileType<PHBloodScytheFriendly>() : ModContent.ProjectileType<BloodScytheFriendly>();
                    int damage = 18;
                    if (modPlayer.SupremeDeathbringerFairy)
                        damage *= 2;
                    if (modPlayer.MasochistSoul)
                        damage *= 2;
                    damage = (int)(damage * player.ActualClassDamage(DamageClass.Magic));
                    Projectile.NewProjectile(self.GetSource_EffectItem(player), player.Center, player.velocity * 0.1f, projType, damage, 5f, player.whoAmI);
                }
            }
        }
        /*
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
        */
    }
}
