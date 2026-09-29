using FargosPhantasmMode.Common.MetaBalls;
using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    public class ShadowFlame : ModProjectile
    {
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public override void SetDefaults() 
        {
            Projectile.CloneDefaults(ProjectileID.EyeFire);
            //Projectile.width *= 2;
            //Projectile.height *= 2;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 60;
        }
        public override void AI()
        {
            if (Main.rand.NextBool(3))
            {
                Vector2 vector = Vector2.Normalize(base.Projectile.velocity.RotatedByRandom(0.62831854820251465));
                float num = Math.Max(4f, base.Projectile.velocity.Length() / 2f);
                CosmicFireMetaBall cosmicFireMetaBall = ModContent.GetInstance<CosmicFireMetaBall>();
                cosmicFireMetaBall.CreateParticle(Projectile.Center, num * vector, 60);
            }
            base.AI();
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<PhSublimationBuff>(), 180);
            //target.AddBuff(BuffID.CursedInferno, 180);
            target.AddBuff(ModContent.BuffType<OiledBuff>(), 360);
        }
    }
}
