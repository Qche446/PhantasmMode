using FargosPhantasmMode.Content.Projectiles;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Luminance.Common.Utilities;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Content.Patreon.DevAesthetic;
using FargosPhantasmMode.Core.Systems;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature
{
    public class NatureGlobalProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool StopCheck = false;
        public int SnowCD = 0;
        //public bool HasSnowTrail = false;
        public bool CanSpawnSnow = false;
        public static List<int> ignoreProj => [
            ModContent.ProjectileType<ShroomiteShroom>(),
            ProjectileID.NorthPoleSnowflake,
            ModContent.ProjectileType<NatureTrailProj>(),
            ];
        public override GlobalProjectile NewInstance(Projectile target)
        {
            return PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        }
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            Player player = Main.LocalPlayer;
            if (player.HasEffect<JungleEnhanceEffect>())
            {
                bool HasForce = Main.LocalPlayer.ForceEffect<JungleEnhanceEffect>();
                if (projectile.type == ProjectileID.IvyWhip)
                    projectile.extraUpdates += 1;
                List<int> weaponProj = [ProjectileID.BladeOfGrass, ProjectileID.ThornChakram, ProjectileID.ThornWhip, ProjectileID.JungleYoyo];
                if (weaponProj.Contains(projectile.type))
                {
                    projectile.GetGlobalProjectile<PModeGlobalProj>().PoisonAttribute = true;
                    //projectile.velocity *= 2;
                }
                if (projectile.type == ProjectileID.ThornWhip)
                {
                    projectile.WhipSettings.RangeMultiplier *= HasForce ? 2.5f : 1.5f;
                }
                if (projectile.type == ProjectileID.JungleYoyo)
                {
                    projectile.extraUpdates += 1;
                }
                if (projectile.type == ProjectileID.ThornChakram || projectile.type == ProjectileID.JungleYoyo)
                    projectile.extraUpdates += 1;
                if (projectile.type == ProjectileID.PoisonDartBlowgun)
                {
                    projectile.extraUpdates += 1;
                    projectile.penetrate += HasForce ? 2 : 1;
                }
            }
            if (player.HasEffect<FrostSnowEffect>() && source is EntitySource_Parent p && !(p.Entity is Projectile) && projectile.damage != 0 && projectile.friendly && !ignoreProj.Contains(projectile.type))
            {
                CanSpawnSnow = true;
            }
            if (player.HasEffect<ShroomiteEffect>())
            {
                List<int> proj = [ProjectileID.TinyEater, ModContent.ProjectileType<DevRocket>()];
                if (projectile.penetrate != -1 && !proj.Contains(projectile.type))
                {
                    bool HasForce = Main.LocalPlayer.ForceEffect<ShroomiteEffect>();
                    projectile.penetrate += HasForce ? 2 : 1;
                    projectile.maxPenetrate += HasForce ? 2 : 1;
                    projectile.damage = (int)(projectile.damage * (HasForce ? 0.4f : 0.55f));
                    if (!projectile.usesLocalNPCImmunity)
                    {
                        projectile.usesLocalNPCImmunity = true;
                        projectile.localNPCHitCooldown = 40;
                    }
                }
            }
            int num = player.ownedProjectileCounts[ModContent.ProjectileType<NatureTrailProj>()];
            if (player.HasEffect<NatureTrailEffect>() && source is EntitySource_Parent s && !(s.Entity is Projectile) && projectile.active && projectile.damage != 0 && projectile.friendly && !ignoreProj.Contains(projectile.type) && num < 80)
            {
                Projectile.NewProjectile(player.GetSource_FromThis(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<NatureTrailProj>(), 0, 2, Main.myPlayer, projectile.whoAmI);
            }
        }
        public override bool PreAI(Projectile projectile)
        {
            if (Main.LocalPlayer.HasEffect<FrostSnowEffect>() && CanSpawnSnow)
            {
                //Player player = Main.player[projectile.owner];
                bool HasForce = Main.LocalPlayer.ForceEffect<FrostSnowEffect>();
                int TotalSnowCD = 30;
                float Maxdamage = HasForce ? 60 : 15;
                Maxdamage = Main.LocalPlayer.HasEffect<NatureEffect>() ? 120 : Maxdamage;
                float damage = projectile.damage * 0.5f;
                if (damage > Maxdamage) 
                    damage = Maxdamage;
                damage *= Main.LocalPlayer.ActualClassDamage(DamageClass.Melee);
                if (++SnowCD > TotalSnowCD)
                {
                    SnowCD = 0;
                    if (projectile.type != ProjectileID.NorthPoleSnowflake)
                    {
                        int p = Projectile.NewProjectile(Main.LocalPlayer.GetSource_EffectItem<FrostSnowEffect>(), projectile.Center, Vector2.Zero, ProjectileID.NorthPoleSnowflake, (int)damage, projectile.knockBack * 0.55f, Main.myPlayer, 0f, Main.rand.Next(3));
                        Main.projectile[p].DamageType = DamageClass.Melee;
                        Main.projectile[p].timeLeft = 180;
                    }
                }
                if (projectile.type != ProjectileID.NorthPoleSnowflake && false)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        int num462 = Dust.NewDust(projectile.position, 16, 16, DustID.SnowflakeIce, projectile.oldVelocity.X, projectile.oldVelocity.Y, 100, default, 1.2f);
                        Main.dust[num462].noGravity = true;
                        Dust dust170 = Main.dust[num462];
                        Dust dust3 = dust170;
                        dust3.velocity *= 0.5f;
                    }
                }
            }
            return true;
        }
        public override void GrapplePullSpeed(Projectile projectile, Player player, ref float speed)
        {
            if (player.HasEffect<JungleEnhanceEffect>() && projectile.type == ProjectileID.IvyWhip)
                speed *= 1.5f;
        }
        public override void GrappleRetreatSpeed(Projectile projectile, Player player, ref float speed)
        {
            if (player.HasEffect<JungleEnhanceEffect>() && projectile.type == ProjectileID.IvyWhip)
                speed *= 2f;
        }
    }
}
