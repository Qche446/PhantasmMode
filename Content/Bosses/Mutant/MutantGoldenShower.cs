using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.DataStructures;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class MutantGoldenShower : GoldenShowerWOF, IProjOwnedByBoss<MutantBoss>
    {
        public override void SetDefaults()
        {
            base.Projectile.width = 15;
            base.Projectile.height = 15;
            base.Projectile.aiStyle = -1;
            base.Projectile.alpha = 255;
            base.Projectile.tileCollide = false;
            base.Projectile.ignoreWater = true;
            base.Projectile.timeLeft = 120;
            base.Projectile.hostile = true;
        }
        public override void AI()
        {
            Projectile.extraUpdates = (int)Projectile.ai[1];
            if (Projectile.localAI[1] == 0)
            {
                Projectile.localAI[1] = 1;
                SoundEngine.PlaySound(SoundID.Item17, Projectile.Center);
            }

            /*for (int i = 0; i < 2; i++) //vanilla dusts
            {
                for (int j = 0; j < 2; ++j)
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 170, 0.0f, 0.0f, 100, default, 0.75f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.1f;
                    Main.dust[d].velocity += Projectile.velocity * 0.5f;
                    Main.dust[d].position -= Projectile.velocity / 3 * j;
                }
                if (Main.rand.NextBool(8))
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 170, 0.0f, 0.0f, 100, default, 0.325f);
                    Main.dust[d].velocity *= 0.25f;
                    Main.dust[d].velocity += Projectile.velocity * 0.5f;
                }
            }*/

            if (--Projectile.ai[0] < 0)
                Projectile.tileCollide = true;

            if (Projectile.localAI[0] == 0)
            {
                Projectile.velocity.X += 0.5f * Projectile.ai[2];
                Projectile.rotation = Projectile.velocity.ToRotation() + (float)Math.PI / 2f;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Ichor, 900);
            target.AddBuff(BuffID.OnFire, 300);
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 600);
            }
        }
    }
}
