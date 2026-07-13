using FargowiltasSouls.Content.Items.Accessories.Masomode;
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
    public class GroundStickOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<GroundStick>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.GroundStick"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
    }
    public class GroundStickDRSysyem : ModSystem
    {
        public override void Load()
        {
            MethodInfo method = typeof(GroundStickDR).GetMethod("ProjectileDamageDR", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(method, ILGroundStickDR);
        }
        private void ILGroundStickDR(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.5f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return WorldSavingSystem.masochistModeReal ? 0.8f : 0.5f;
            });
        }
    }
}
