using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.Champions.Earth;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class MutantCrystalBomb : CrystalBomb, IProjOwnedByBoss<MutantBoss>
    {
        int Timer = 0;
        const int waittime = 30;
        public override string Texture => "FargowiltasSouls/Content/Bosses/Champions/Earth/CrystalBomb";
        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = Main.rand.NextBool(2) ? 1f : -1f;
                Projectile.rotation = Main.rand.NextFloat((float)Math.PI * 2);
                Projectile.hide = false;
            }

            if (--Projectile.localAI[1] < 0)
            {
                Projectile.localAI[1] = 60;
                SoundEngine.PlaySound(SoundID.Item27, Projectile.position);
            }

            Projectile.alpha -= 10;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            if (Projectile.alpha > 255)
                Projectile.alpha = 255;

            Projectile.rotation += (float)Math.PI / 40f * Projectile.localAI[0];

            Lighting.AddLight(Projectile.Center, 0.3f, 0.75f, 0.9f);

            int index3 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.NorthPole, 0.0f, 0.0f, 100, Color.Transparent, 1f);
            Main.dust[index3].noGravity = true;

            //Projectile.velocity *= 1.03f

            if (++Timer > waittime)
            {
                Projectile.velocity *= 0.95f;
            }
            if (Timer > waittime + 20)
            {
                Projectile.Kill();
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.EternityMode)
                target.AddBuff(BuffID.Chilled, 180);
            target.AddBuff(BuffID.Frostburn, 180);
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 600);
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.position);

            for (int index1 = 0; index1 < 40; ++index1)
            {
                int index2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueCrystalShard, 0f, 0f, 0, default, 1f);
                Main.dust[index2].noGravity = true;
                Main.dust[index2].velocity *= 1.5f;
                Main.dust[index2].scale *= 0.9f;
            }

            if (FargoSoulsUtil.HostCheck)
            {
                for (int index = 0; index < 5; ++index)
                {
                    Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, Main.rand.NextVector2Circular(12f, 12f),
                        ModContent.ProjectileType<MutantCrystalBombShard>(), Projectile.damage, 0f, Projectile.owner);
                }
            }
        }
    }
    public class MutantCrystalBombShard : CrystalBombShard, IProjOwnedByBoss<MutantBoss>
    {
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.QueenSlimeMinionBlueSpike);
            AIType = ProjectileID.QueenSlimeMinionBlueSpike;
            Projectile.scale *= 1f;
            Projectile.timeLeft = 600;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.EternityMode)
                target.AddBuff(BuffID.Chilled, 180);
            target.AddBuff(BuffID.Frostburn, 180);
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 600);
            }
        }
    }
}
