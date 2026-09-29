using FargosPhantasmMode.Common;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Lump
{
    public class DeerclawpsOverride : PModeGlobalMasoItem<Deerclawps>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(DeerclawpsEffect.DeerclawpsAttack, DeerclawpsFixed);
            PhanUtil.AddHooks(DeerclawpsDive.DeerclawpsLandingSpikes, DeerclawpsLandingFixed);
        }
        private static void DeerclawpsFixed(Action<Player, Vector2> orig, Player player, Vector2 pos)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (!PModeChangeApply)
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
        }
        private static void DeerclawpsLandingFixed(Action<Player, Vector2> orig, Player player, Vector2 pos)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (!PModeChangeApply)
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
        }
       
    }
    public class IceDeerclopsIceSpike : GlobalProjectile
    {
        public override GlobalProjectile NewInstance(Projectile target) => PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.type == ProjectileID.DeerclopsIceSpike && PModeWorldSavingSystem.PhantasmMode)
            {
                target.AddBuff(BuffID.Frostburn, 180);
            }
            base.OnHitNPC(projectile, target, hit, damageDone);
        }
    }
}
