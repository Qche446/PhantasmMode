using FargosPhantasmMode.Common;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Circuitry
{
    public class GroundStickOverride : PModeGlobalMasoItem<GroundStick>
    {
        public override void Load()
        {
            PhanUtil.AddILHooks(ModContent.GetInstance<GroundStickDR>().ProjectileDamageDR, ILGroundStickDR);
        }
        private void ILGroundStickDR(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.5f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() =>
            {
                return PModeChangeApply ? 0.8f : 0.5f;
            });
        }
    }
}
