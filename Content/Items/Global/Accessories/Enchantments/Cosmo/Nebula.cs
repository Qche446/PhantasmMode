using FargosPhantasmMode.Common;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Cosmo
{
    public class Nebula : PModeGlobalEnchant<NebulaEnchant>
    {
        public override void Load()
        {
            /*
            MethodInfo FargoProjOnSpawn = typeof(FargoSoulsGlobalProjectile).GetMethod("OnSpawn", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(FargoProjOnSpawn, NebulaFixed);
            */
            PhanUtil.AddILHooks(ModContent.GetInstance<FargoSoulsGlobalProjectile>().OnSpawn, NebulaFixed);
        }
        private static void NebulaFixed(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdloc(23)))
                throw new Exception("IL edit failed!");
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(0)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() => PModeChangeApply ? 1 : 0);
            
        }
    }
}
