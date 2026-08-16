using FargosPhantasmMode.Content.Projectiles.Masomode;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.Champions.Cosmos;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Cosmo
{
    public class CosmoGlobalProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool Vortexed = false;
        public float VortexedCD = 0;
        public bool CosmoExtraAttack = true;
        public int MoonNum = 0;
        public override GlobalProjectile NewInstance(Projectile target)
        {
            return PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        }
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            Player player = Main.LocalPlayer;
            if (player.HasEffect<VortexProjGravity>())
            {
                Vortexed = true;
            }
        }
        public override bool PreAI(Projectile proj)
        {
            Player py = Main.LocalPlayer;
            if (proj.type == ModContent.ProjectileType<CosmosForceMoon>() && py.HasEffect<CosmoMoonEnhanceEffect>())
            {
                if (proj.ai[1] == 0 && proj.active)
                {
                    int MoonNum = py.ownedProjectileCounts[proj.type];
                }
            }
            if (!py.HasEffect<VortexProjGravity>() || !Vortexed)
                return true;
            if (VortexedCD <= 0 && !proj.hostile && proj.friendly && proj.damage != 0)
            {
                float r = 1200f;
                foreach (NPC npc in Main.npc.Where(n => n.active && n != null && n.Distance(py.Center) < r && !n.friendly && n.Distance(proj.Center) < 400 && n.Distance(proj.Center) > 2))
                {
                    proj.velocity += 0.6f * proj.SafeDirectionTo(npc.Center);
                }
            }
            else if (VortexedCD > 0)
                VortexedCD--;
            return true;
        }
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            VortexedCD = 40;
            Player player = Main.player[projectile.owner];
            if (!player.FargoSouls().TerrariaSoul && projectile.type == ModContent.ProjectileType<CosmosForceMoon>() && CosmoExtraAttack && player.HasEffect<CosmoMoonEnhanceEffect>())
            {
                CosmoExtraAttack = false;
                if (projectile.owner == Main.myPlayer)
                {
                    bool wizBoost = MoonNum >= 4;
                    switch ((int)projectile.ai[2])
                    {
                        //日耀
                        case 0:
                            {
                                int multiplier = wizBoost ? 2 : 1;
                                int damage = 3200 * multiplier / 3;
                                int speed = wizBoost ? 17 : 13;

                                Projectile.NewProjectile(player.GetSource_EffectItem<CosmosMoonEffect>(), player.Center, Vector2.Zero, ModContent.ProjectileType<P_SolarEnchFlare>(), (int)(damage * player.ActualClassDamage(DamageClass.Melee)), 1f, player.whoAmI, ai2: speed);
                            }
                            break;
                        //星璇
                        case 1:
                            {
                                int dmg = 9750 / 3;
                                if (wizBoost)
                                    dmg = 18000;
                                dmg /= 2;
                                Vector2 velocity = player.DirectionTo(target.Center);
                                int damage = (int)(dmg * player.ActualClassDamage(DamageClass.Ranged));
                                FargoSoulsUtil.NewProjectileDirectSafe(player.GetSource_EffectItem<CosmosMoonEffect>(), player.Center, velocity, ModContent.ProjectileType<VortexLaser>(), damage, 0f, player.whoAmI, 1f);
                            }
                            break;
                        //星云
                        case 2:
                            {
                                int damage = Math.Max(1200, projectile.damage) / 3;
                                damage = (int)MathHelper.Clamp(damage, 0, 3000);
                                if (wizBoost)
                                    damage = (int)(damage * 1.66667f);

                                Projectile.NewProjectile(player.GetSource_FromThis(), player.Center, projectile.velocity, ModContent.ProjectileType<NebulaShot>(), (int)(damage * player.ActualClassDamage(DamageClass.Magic)), 1f, player.whoAmI, 0);
                            }
                            break;
                        //星辰 => 流星
                        case 3:
                            {
                                target.GetGlobalNPC<MeteorTargetGlobalNPC>().MeteorHitCD = wizBoost ? 120 : 60;
                            }
                            break;
                    }
                }
            }
        }
    }
}
