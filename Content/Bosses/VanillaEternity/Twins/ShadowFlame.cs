using FargosPhantasmMode.Common.MetaBalls;
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
            for (int i = 0; i < 1; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemAmethyst, 0f, 0f, 0, default, 1.8f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.98f;
            }
            base.AI();
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
            //target.AddBuff(BuffID.CursedInferno, 180);
            target.AddBuff(ModContent.BuffType<OiledBuff>(), 300);
        }
    }
}
