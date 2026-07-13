using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
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
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments
{
    public class TungstenEnchantOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<TungstenEnchant>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Enchantments.TungstenEnchant"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
    }
    public class Tungsten : ModSystem
    {
        public override void Load()
        {
            MethodInfo method1 = typeof(TungstenEffect).GetMethod("TungstenIncreaseWeaponSize", BindingFlags.Static | BindingFlags.Public);
            MonoModHooks.Modify(method1, ILTungstenEffect1);
            MethodInfo method2 = typeof(TungstenEffect).GetMethod("TungstenIncreaseProjSize", BindingFlags.Static | BindingFlags.Public);
            MonoModHooks.Modify(method2, ILTungstenEffect2);
        }
        private void ILTungstenEffect1(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<FargoSoulsPlayer,float>>(modplayer =>
            {
                return Main.LocalPlayer.ForceEffect<TungstenEffect>() ? 2.5f : 1.75f;
            });
            c.Emit(OpCodes.Ret);
        }
        private void ILTungstenEffect2(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.5f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return Main.LocalPlayer.ForceEffect<TungstenEffect>() ? 1.5f : 0.75f;
            });
        }
    }
}
