using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.ModPlayers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Terra
{
    public class Tungsten : PModeGlobalEnchant<TungstenEnchant>
    {
        public static float SizeMult => PModeChangeApply ? Main.LocalPlayer.ForceEffect<TungstenEffect>() ? 2.5f : 1.75f : TungstenEffect.SizeMult;
        public override void Load()
        {
            PhanUtil.AddHooks(TungstenEffect.TungstenIncreaseWeaponSize, PModeTungstenIncreaseWeaponSize);
            PhanUtil.AddILHooks(TungstenEffect.TungstenIncreaseProjSize, ILTungstenEffect2);
        }
        private static float PModeTungstenIncreaseWeaponSize(Func<FargoSoulsPlayer, float> orig, FargoSoulsPlayer fp) => SizeMult;
        private void ILTungstenEffect2(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.5f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() => SizeMult - 1f);
        }
    }
}
