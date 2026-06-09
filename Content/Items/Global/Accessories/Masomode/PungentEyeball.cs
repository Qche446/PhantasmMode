using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;
using System.Reflection;
using MonoMod.Cil;
using System;
using System.Text.RegularExpressions;
using Mono.Cecil.Cil;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using System.Linq;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Globals;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class PungentEyeballOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<PungentEyeball>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.PungentEyeball"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
    }
    public class PungentGazePlus : ModSystem
    {
        public override void Load()
        {
            MethodInfo method1 = typeof(PungentEyeballCursor).GetMethod("PostUpdateEquips", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(method1, ILPungentGazeSpeed);
            MethodInfo method2 = typeof(FargoSoulsGlobalNPC).GetMethod("ModifyIncomingHit", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(method2, ILPungentGazeEx);
        }
        private void ILPungentGazeSpeed(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<Player>>(player =>
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    const float distance = 16 * 5;
                    bool maso = WorldSavingSystem.masochistModeReal;
                    foreach (NPC n in Main.npc.Where(n => n.active && !n.dontTakeDamage && n.lifeMax > 5 && !n.friendly))
                    {
                        if (Vector2.Distance(Main.MouseWorld, FargoSoulsUtil.ClosestPointInHitbox(n.Hitbox, Main.MouseWorld)) < distance)
                        {
                            n.AddBuff(ModContent.BuffType<PungentGazeBuff>(), maso ? 3 : 2, true);
                        }
                    }

                    int visualProj = ModContent.ProjectileType<PungentAuraProj>();
                    if (player.ownedProjectileCounts[visualProj] <= 0)
                    {
                        Projectile.NewProjectile(ModContent.GetInstance<PungentEyeballCursor>().GetSource_EffectItem(player), player.Center, Vector2.Zero, visualProj, 0, 0, Main.myPlayer);
                    }
                }
            });
            c.Emit(OpCodes.Ret);
        }
        private void ILPungentGazeEx(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.15f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return Main.LocalPlayer.FargoSouls().LumpOfFlesh && WorldSavingSystem.masochistModeReal ? 0.2f : 0.15f;
            });
        }
    }
}
