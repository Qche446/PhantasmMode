using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Reflection;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls;
using System.Linq;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Souls;
using Terraria.ID;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class PumpkingsCapeOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
           => entity.type == ModContent.ItemType<PumpkingsCape>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.PumpkingsCape"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class RottingDistanceEnhanceModSysyem : ModSystem
    {
        public override void Load()
        {
            MethodInfo method = typeof(FargoSoulsPlayer).GetMethod("RaisedShieldEffects",BindingFlags.Instance | BindingFlags.NonPublic);
            MonoModHooks.Modify(method, ILRottingDistanceEnhance);
        }
        private void ILRottingDistanceEnhance(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.EmitDelegate<Action>(() =>
            {
                Player py = Main.LocalPlayer;
                FargoSoulsPlayer fp = py.FargoSouls();
                bool maso = WorldSavingSystem.masochistModeReal;
                bool silverEffect = py.HasEffect<SilverEffect>();
                bool dreadEffect = py.HasEffect<DreadShellEffect>();
                bool pumpkingEffect = py.HasEffect<PumpkingsCapeEffect>();
                if (dreadEffect)
                {
                    if (!fp.MasochistSoul)
                        fp.DreadShellVulnerabilityTimer = 60;
                }

                if (pumpkingEffect) //strong aura effect
                {
                    float distance = maso ? 600f : 300f;
                    for (int i = 0; i < Main.maxNPCs; i++)
                        if (Main.npc[i].active && !Main.npc[i].friendly && Main.npc[i].Distance(py.Center) < distance)
                            Main.npc[i].AddBuff(ModContent.BuffType<RottingBuff>(), 600);

                    for (int i = 0; i < (maso ? 40 : 20); i++)
                    {
                        Vector2 offset = new();
                        double angle = Main.rand.NextDouble() * 2d * Math.PI;
                        offset.X += (float)(Math.Sin(angle) * distance);
                        offset.Y += (float)(Math.Cos(angle) * distance);
                        Dust dust = Main.dust[Dust.NewDust(py.Center + offset - new Vector2(4, 4), 0, 0, DustID.Ice_Pink, 0, 0, 100, Color.White, 1f)];
                        dust.velocity = py.velocity;
                        if (Main.rand.NextBool(3))
                            dust.velocity += Vector2.Normalize(offset) * -(maso ? 8f : 5f);
                        dust.noGravity = true;
                    }
                }
                //maso下取消举盾减速
                if ((dreadEffect || pumpkingEffect) && !silverEffect && !WorldSavingSystem.masochistModeReal)
                {
                    py.velocity.X *= 0.85f;
                    if (py.velocity.Y < 0)
                        py.velocity.Y *= 0.85f;
                }
                int cooldown = FargoSoulsPlayer.ShieldCooldown(py);

                if (fp.shieldCD < cooldown)
                    fp.shieldCD = cooldown;
            });
            c.Emit(OpCodes.Ret);
        }
    }
}
