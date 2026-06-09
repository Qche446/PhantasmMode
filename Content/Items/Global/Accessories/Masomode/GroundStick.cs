using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using FargowiltasSouls.Core.Toggler;
using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.ModPlayers;
using System;
using FargosPhantasmMode.Content.Projectiles.Masomode;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;

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
