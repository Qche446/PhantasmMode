using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;
using FargowiltasSouls.Core.ModPlayers;
using MonoMod.Cil;
using System.Reflection;
using System;
using Mono.Cecil.Cil;
using FargowiltasSouls.Content.Buffs;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.UI.Elements;
using Microsoft.Xna.Framework.Graphics;
using FargowiltasSouls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Luminance.Common.Utilities;
using FargosPhantasmMode.Content.Projectiles.Masomode;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class BetsysHeartOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<BetsysHeart>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.BetsysHeart"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
            {

            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class SpecialDashModSysyem : ModSystem
    {
        public override void Load()
        {
            MethodInfo method = typeof(FargoSoulsPlayer).GetMethod("SpecialDashKey", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(method, ILSpecialDash);
        }
        private void ILSpecialDash(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<int>>((type) =>
            {
                Player py = Main.LocalPlayer;
                FargoSoulsPlayer fp = py.FargoSouls();
                bool maso = WorldSavingSystem.masochistModeReal;
                if (fp.SpecialDashCD <= 0)
                {
                    fp.SpecialDashCD = 5 * 60;

                    if (py.whoAmI == Main.myPlayer)
                    {
                        py.RemoveAllGrapplingHooks();

                        /*Player.controlLeft = false;
                        Player.controlRight = false;
                        Player.controlJump = false;
                        Player.controlDown = false;*/
                        py.controlUseItem = false;
                        py.controlUseTile = false;
                        py.controlHook = false;
                        //Player.controlMount = false;

                        py.itemAnimation = 0;
                        py.itemTime = 0;
                        py.reuseDelay = 0;

                        if (py.HasEffect<BetsyDashEffect>() && type == 2)
                        {
                            Vector2 vel = py.SafeDirectionTo(Main.MouseWorld) * 25;
                            Projectile.NewProjectile(py.GetSource_Accessory(fp.BetsysHeartItem), py.Center, vel, ModContent.ProjectileType<BetsyDash>(), (int)(100 * py.ActualClassDamage(DamageClass.Melee)), 6f, py.whoAmI);
                            if (maso)
                            {
                                for (int i = 0; i < 12; i++)
                                {
                                    int damage = 60;
                                    if (py.FargoSouls().MasochistSoul)
                                    {
                                        damage *= 10;
                                    }
                                    else if (py.FargoSouls().MasochistHeart)
                                    {
                                        damage *= 2;
                                    }
                                    Projectile.NewProjectile(py.GetSource_Accessory(fp.BetsysHeartItem), py.Center,Main.rand.NextFloat(1.4f,2.6f) * vel.RotatedByRandom(MathHelper.Pi / 8f), ModContent.ProjectileType<BetsysHeartPhoenix>(), (int)(damage * py.ActualClassDamage(DamageClass.Melee)), 6f, py.whoAmI);
                                }
                            }
                            py.immune = true;
                            py.immuneTime = Math.Max(py.immuneTime, 2);
                            py.hurtCooldowns[0] = Math.Max(py.hurtCooldowns[0], 2);
                            py.hurtCooldowns[1] = Math.Max(py.hurtCooldowns[1], 2);

                            CooldownBarManager.Activate("SpecialDashCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Masomode/BetsysHeart").Value, Color.OrangeRed,
                                () => 1 - (float)fp.SpecialDashCD / (5f * 60f), activeFunction: () => fp.BetsysHeartItem != null);
                        }
                        else if (py.HasEffect<SpecialDashEffect>() && type == 0)
                        {
                            fp.SpecialDashCD += 60;

                            Vector2 vel = py.SafeDirectionTo(Main.MouseWorld) * 20;
                            Projectile.NewProjectile(py.GetSource_Accessory(fp.QueenStingerItem), py.Center, vel, ModContent.ProjectileType<BeeDash>(), (int)(44 * py.ActualClassDamage(DamageClass.Melee)), 6f, py.whoAmI);

                            CooldownBarManager.Activate("SpecialDashCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Masomode/QueenStinger").Value, Color.Yellow,
                                () => 1 - (float)fp.SpecialDashCD / (5f * 60f), activeFunction: () => fp.QueenStingerItem != null);
                        }

                        py.AddBuff(ModContent.BuffType<BetsyDashBuff>(), 20);
                    }
                }
            });
            c.Emit(OpCodes.Ret);
        }
    }
}
