using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Projectiles.Masomode;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Kingslime
{
    public class KSGlobalProj : GlobalProjectile
    {
        public static readonly List<int> KSProj = [ModContent.ProjectileType<SlimeSpike2>(), ModContent.ProjectileType<SlimeSpike>(), ModContent.ProjectileType<SlimeBallHostile>()];
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => KSProj.Contains(entity.type) && lateInstantiation;
        public override GlobalProjectile NewInstance(Projectile target) => PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        public override void AI(Projectile proj)
        {
            float maxspeed = Main.getGoodWorld ? 11 : 10;
            if (proj.velocity.Y > maxspeed)
                proj.velocity.Y = maxspeed;
        }
        public override void OnHitPlayer(Projectile proj, Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<FractureBuff>(), 60 * 3);
        }
    }
}
