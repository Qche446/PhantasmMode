using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Projectiles;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.BossWeapons;
using FargowiltasSouls.Core.ModPlayers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static FargowiltasSouls.Content.Items.Accessories.Enchantments.TungstenEffect;
using static Terraria.ModLoader.ModContent;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Terra
{
    public class Tungsten : PModeGlobalEnchant<TungstenEnchant>
    {
        public static float SizeMult => PModeChangeApply ? Main.LocalPlayer.ForceEffect<TungstenEffect>() ? 2.5f : 1.75f : TungstenEffect.SizeMult;
        private readonly static List<int> AlwaysAffectProjExtraList = [ProjectileID.DD2SquireSonicBoom];
        private readonly static List<int> NerfedProjExtraList = [ProjectileType<StyxGazerArmor>(), ProjectileType<StyxSickle>()];
        public override void Load()
        {
            PhanUtil.AddHooks(TungstenIncreaseWeaponSize, PModeTungstenIncreaseWeaponSize);
            PhanUtil.AddILHooks(TungstenIncreaseProjSize, ILTungstenEffect2);

            PhanUtil.AddHooks(TungstenAlwaysAffectProj, AlwaysAffectProjExtra);
            PhanUtil.AddHooks(TungstenNerfedProj, NerfedProjExtra);
        }
        private static bool AlwaysAffectProjExtra(Func<Projectile, bool> orig, Projectile proj)
        {
            return orig.Invoke(proj) || (PModeChangeApply && AlwaysAffectProjExtraList.Contains(proj.type));
        }
        private static bool NerfedProjExtra(Func<Projectile, bool> orig, Projectile proj)
        {
            return orig.Invoke(proj) || (PModeChangeApply && NerfedProjExtraList.Contains(proj.type));
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
        public static void AddProjType(ref List<int> list, params int[] type)
        {
            foreach(int i in type)
            {
                if (!list.Contains(i))
                    list.Add(i);
            }
        }
        public static void RemoveProjType(ref List<int> list, params int[] type)
        {
            foreach (int i in type)
            {
                list.Remove(i);
            }
        }
    }
}
