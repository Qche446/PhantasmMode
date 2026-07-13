using FargosPhantasmMode.Content.Buffs.Global;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Globals;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs
{
    public class NanoErosionBuff : ModBuff 
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            base.Update(npc, ref buffIndex);
        }
    }
    public class NanoErosionDotEnhance : ModSystem
    {
        public override void Load()
        {
            MethodInfo method1 = typeof(FargoSoulsGlobalNPC).GetMethod("DoTMultiplier", BindingFlags.Static | BindingFlags.Public);
            MonoModHooks.Modify(method1, ILNanoErosionDotEnhance);
        }
        private void ILNanoErosionDotEnhance(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<NPC, Player, float>>((npc, player) =>
            {
                float multiplier = 1f;
                bool hasBuff = npc.HasBuff(ModContent.BuffType<NanoErosionBuff>());
                bool hasHypothermia = npc.GetGlobalNPC<GlobalBuffNPC>().Hypothermia;
                if (npc.lifeRegen >= 0)
                    return multiplier;

                if (player.HasEffect<OrichalcumEffect>())
                    multiplier += OrichalcumEffect.OriDotModifier(npc, player.FargoSouls()) - 1;

                if (npc.FargoSouls().MagicalCurse)
                {
                    if (hasBuff || hasHypothermia)
                    {
                        multiplier *= 2;
                    }
                    else
                    {
                        multiplier += 1;
                    }
                }
                //half as effective if daybreak applied
                if (npc.daybreak && multiplier > 1 && (!hasBuff) && (!hasHypothermia))
                    multiplier -= (multiplier - 1) / 2;
                multiplier *= hasBuff ? 1.2f : 1f;
                multiplier *= hasHypothermia ? 1.05f : 1;
                return multiplier;
            });
            c.Emit(OpCodes.Ret);
        }
    }
}
