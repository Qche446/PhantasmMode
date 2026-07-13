using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.PlayerDrawLayers;
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

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class PrecisionSealOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<PrecisionSeal>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.PrecisionSeal"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
    }
    public class PrecisionSealHurtboxModSystem : ModSystem
    {
        public override void Load()
        {
            MethodInfo method = typeof(FargoSoulsPlayer).GetMethod("GetPrecisionHurtbox", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(method, ILHurtbox);
            MethodInfo method2 = typeof(PrecisionHurtboxDrawLayer).GetMethod("Draw", BindingFlags.Instance | BindingFlags.NonPublic);
            MonoModHooks.Modify(method2, ILHurtboxDraw);
        }
        public static void ILHurtbox(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.EmitDelegate<Func<Rectangle>>(() =>
            {
                float multiplier = WorldSavingSystem.masochistModeReal ? 0.5f : 1;
                Rectangle hurtbox = Main.LocalPlayer.Hitbox;
                hurtbox.X += hurtbox.Width / 2;
                hurtbox.Y += hurtbox.Height / 2;
                hurtbox.Width = (int)(multiplier * Math.Min(hurtbox.Width, hurtbox.Height));
                hurtbox.Height = Math.Min(hurtbox.Width, hurtbox.Height);
                hurtbox.X -= hurtbox.Width / 2;
                hurtbox.Y -= hurtbox.Height / 2;
                return hurtbox;
            });
            c.Emit(OpCodes.Ret);
        }
        public static void ILHurtboxDraw(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(1)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return WorldSavingSystem.masochistModeReal ? 0.5f : 1;
            });
        }
    }
}
