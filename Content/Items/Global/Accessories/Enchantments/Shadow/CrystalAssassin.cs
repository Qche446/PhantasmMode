using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class CrystalAssassin : PModeGlobalEnchant<CrystalAssassinEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddILHooks(CrystalAssassinDash.CrystalDash, CrystalDashFixed);
        }
        private static void CrystalDashFixed(ILContext il)
        {
            ILCursor c = new (il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(22f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() => PModeChangeApply ? 30f : 22f);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(60)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() => PModeChangeApply ? 40 : 60);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(30)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() => PModeChangeApply ? 20 : 30);
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            //辅助功能罢了
            if (!player.HasEffectEnchant<CrystalAssassinDash>())
                return;
            var modplayer = player.FargoSouls();
            if (player.whoAmI == Main.myPlayer)
                CooldownBarManager.Activate("CrystalDashFirstStrikeCD", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Enchantments/CrystalAssassinEnchant").Value, new(140, 38, 242),
                () => 1 - (float)modplayer.CrystalDashFirstStrikeCD / (player.ForceEffect<CrystalDiagonalDash>() ? 300f : 600f), activeFunction: player.HasEffectEnchant<CrystalAssassinDash>, displayAtFull: true);
        }
    }
}
