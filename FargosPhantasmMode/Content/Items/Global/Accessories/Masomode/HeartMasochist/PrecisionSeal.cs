using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.PlayerDrawLayers;
using FargowiltasSouls.Core.AccessoryEffectSystem;
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

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist
{
    public class PrecisionSealOverride : PModeGlobalMasoItem<PrecisionSeal>
    {
        public override void Load()
        {
            MethodInfo method2 = typeof(PrecisionHurtboxDrawLayer).GetMethod("Draw", BindingFlags.Instance | BindingFlags.NonPublic);
            MonoModHooks.Modify(method2, ILHurtboxDraw);
        }
        public static void ILHurtboxDraw(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(1)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() =>
            {
                return PModeChangeApply ? 0.5f : 1;
            });
        }
    }
    public class PrecisionSealPlayer : ModPlayer
    {
        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (PModeWorldSavingSystem.PhantasmMode && Player.HasEffect<PrecisionSealHurtbox>() && !proj.Colliding(proj.Hitbox, GetPrecisionHurtbox()))
                return false;
            return true;
        }
        public static Rectangle GetPrecisionHurtbox()
        {
            float multiplier = PModeWorldSavingSystem.PhantasmMode ? 0.5f : 1;
            Rectangle hurtbox = Main.LocalPlayer.Hitbox;
            hurtbox.X += hurtbox.Width / 2;
            hurtbox.Y += hurtbox.Height / 2;
            hurtbox.Width = (int)(multiplier * Math.Min(hurtbox.Width, hurtbox.Height));
            hurtbox.Height = Math.Min(hurtbox.Width, hurtbox.Height);
            hurtbox.X -= hurtbox.Width / 2;
            hurtbox.Y -= hurtbox.Height / 2;
            return hurtbox;
        }
    }
}
