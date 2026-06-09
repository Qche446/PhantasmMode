using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Reflection;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;
using Terraria.ID;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class DeerclawpsOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<Deerclawps>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.Deerclawps"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
    }
    public class IceDeerclopsIceSpike : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.type == ProjectileID.DeerclopsIceSpike && WorldSavingSystem.masochistModeReal)
            {
                target.AddBuff(BuffID.Frostburn, 180);
            }
            base.OnHitNPC(projectile, target, hit, damageDone);
        }
    }
    public class ILDeerclawpsEffect : ModSystem
    {
        public override void Load()
        {
            MethodInfo method1 = typeof(DeerclawpsEffect).GetMethod("DeerclawpsAttack", BindingFlags.Static | BindingFlags.Public);
            MonoModHooks.Modify(method1, ILDeerclawps);
            MethodInfo method2 = typeof(DeerclawpsDive).GetMethod("DeerclawpsLandingSpikes",BindingFlags.Static | BindingFlags.Public);
            MonoModHooks.Modify(method2, ILDeerclawps2);
        }
        private void ILDeerclawps(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<Player, Vector2>>((player,pos) =>
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    if (!WorldSavingSystem.masochistModeReal)
                    {
                        Vector2 vel = 16f * -Vector2.UnitY.RotatedByRandom(MathHelper.ToRadians(30));

                        int dam = 32;
                        int type = ProjectileID.DeerclopsIceSpike;
                        float ai0 = -15f;
                        float ai1 = Main.rand.NextFloat(0.5f, 1f);
                        if (player.FargoSouls().LumpOfFlesh)
                        {
                            dam = 48;
                            type = ProjectileID.SharpTears;
                            ai0 *= 2f;
                            ai1 += 0.5f;
                        }
                        dam = (int)(dam * player.ActualClassDamage(DamageClass.Melee));

                        if (player.velocity.Y == 0)
                            Projectile.NewProjectile(player.GetSource_EffectItem<DeerclawpsEffect>(), pos, vel, type, dam, 4f, Main.myPlayer, ai0, ai1);
                        else
                        {
                            int npcID = FargoSoulsUtil.FindClosestHostileNPC(pos, 300, true, true);
                            if (!npcID.IsWithinBounds(Main.maxNPCs))
                                return;
                            NPC npc = Main.npc[npcID];
                            if (!npc.Alive())
                                return;
                            vel = pos.DirectionTo(npc.Center) * vel.Length();
                            Projectile.NewProjectile(player.GetSource_EffectItem<DeerclawpsEffect>(), pos, vel.RotatedByRandom(MathHelper.PiOver2 * 0.3f), type, dam, 4f, Main.myPlayer, ai0, ai1 / 2);

                        }
                    }//原版
                    else
                    {
                        Vector2 vel = 16f * -Vector2.UnitY.RotatedByRandom(MathHelper.ToRadians(30));

                        int dam = 48;
                        int type = ProjectileID.DeerclopsIceSpike;
                        float ai0 = -15f;
                        float ai1 = Main.rand.NextFloat(0.5f, 1f);
                        float currentscale = 1.2f;
                        if (player.FargoSouls().LumpOfFlesh)
                        {
                            dam = 64;
                            //type = ProjectileID.SharpTears;
                            ai0 *= 2f;
                            ai1 += 0.5f;
                            currentscale = 1.6f;
                        }
                        dam = (int)(dam * player.ActualClassDamage(DamageClass.Melee));
                        int p = Projectile.NewProjectile(player.GetSource_EffectItem<DeerclawpsEffect>(), pos, vel, type, dam, 4f, Main.myPlayer, ai0, ai1);
                        Main.projectile[p].scale *= currentscale;

                        int npcID = FargoSoulsUtil.FindClosestHostileNPC(pos, 300, true, true);
                        if (npcID.IsWithinBounds(Main.maxNPCs))
                        {
                            NPC npc = Main.npc[npcID];
                            if (npc.Alive())
                            {
                                vel = pos.DirectionTo(npc.Center) * vel.Length();
                                int q = Projectile.NewProjectile(player.GetSource_EffectItem<DeerclawpsEffect>(), pos, vel.RotatedByRandom(MathHelper.PiOver2 * 0.3f), type, dam, 4f, Main.myPlayer, ai0, ai1 / 2);
                                Main.projectile[q].scale *= currentscale;
                            }
                        }
                    }//改版
                }
            });
            c.Emit(OpCodes.Ret);
        }
        private void ILDeerclawps2(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<Player, Vector2>>((player, pos) =>
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    if (!WorldSavingSystem.masochistModeReal)
                    {
                        const int max = 4;
                        for (int i = -max; i <= max; i++)
                        {
                            Vector2 vel = 16f * -Vector2.UnitY.RotatedBy(MathHelper.PiOver2 / max * i).RotatedByRandom(MathHelper.ToRadians(10));

                            int dam = 32;
                            int type = ProjectileID.DeerclopsIceSpike;
                            float ai0 = -15f;
                            float ai1 = Main.rand.NextFloat(0.5f, 1f);
                            if (player.FargoSouls().LumpOfFlesh)
                            {
                                dam = 48;
                                type = ProjectileID.SharpTears;
                                ai0 *= 2f;
                                ai1 += 0.5f;
                            }
                            dam = (int)(dam * player.ActualClassDamage(DamageClass.Melee));

                            Projectile.NewProjectile(player.GetSource_EffectItem<DeerclawpsEffect>(), pos, vel, type, dam, 4f, Main.myPlayer, ai0, ai1);
                        }
                    }//原版
                    else
                    {
                        const int max = 4;
                        for (int i = -max; i <= max; i++)
                        {
                            Vector2 vel = 16f * -Vector2.UnitY.RotatedBy(MathHelper.PiOver2 / max * i).RotatedByRandom(MathHelper.ToRadians(10));

                            int dam = 48;
                            int type = ProjectileID.DeerclopsIceSpike;
                            float ai0 = -15f;
                            float ai1 = Main.rand.NextFloat(0.5f, 1f);
                            float currentscale = 1.2f;
                            if (player.FargoSouls().LumpOfFlesh)
                            {
                                dam = 64;
                                //type = ProjectileID.SharpTears;
                                ai0 *= 2f;
                                ai1 += 0.5f;
                                currentscale = 1.6f;
                            }
                            dam = (int)(dam * player.ActualClassDamage(DamageClass.Melee));

                            int p = Projectile.NewProjectile(player.GetSource_EffectItem<DeerclawpsEffect>(), pos, vel, type, dam, 4f, Main.myPlayer, ai0, ai1);
                            Main.projectile[p].scale *= currentscale;
                        }
                    }//改版
                }
            });
            c.Emit(OpCodes.Ret);
        }
    }
}
