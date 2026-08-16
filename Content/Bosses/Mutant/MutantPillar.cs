using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.Champions.Cosmos;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.DataStructures;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantPillar : MutantPillar, IProjOwnedByBoss<MutantBoss>
    {
        public override void OnKill(int timeLeft)
        {
            if (Main.LocalPlayer.active && !Main.dedServ)
            {
                ScreenShakeSystem.StartShake(10f, MathF.PI * 2f, null, 1f / 3f);
            }

            SoundEngine.PlaySound(in SoundID.Item92, base.Projectile.Center);
            int type = (int)base.Projectile.ai[0] switch
            {
                0 => 242,
                1 => 127,
                2 => 229,
                _ => 135,
            };
            for (int i = 0; i < 80; i++)
            {
                Dust dust = Main.dust[Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, type)];
                dust.velocity *= 10f;
                dust.fadeIn = 1f;
                dust.scale = 1f + Main.rand.NextFloat() + (float)Main.rand.Next(4) * 0.3f;
                if (!Main.rand.NextBool(3))
                {
                    dust.noGravity = true;
                    dust.velocity *= 3f;
                    dust.scale *= 2f;
                }
            }

            if (!FargoSoulsUtil.HostCheck)
            {
                return;
            }

            int timeLeft2 = 240;
            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<MutantBoss>()) && Main.npc[EModeGlobalNPC.mutantBoss].ai[0] == 19f)
            {
                timeLeft2 = (int)Main.npc[EModeGlobalNPC.mutantBoss].localAI[0];
            }

            float num = WorldSavingSystem.MasochistModeReal ? 4.5f : 3.5f;
            for (int j = 0; j < 3; j++)
            {
                Vector2 spinningpoint = num * (j + 0.5f) * Projectile.SafeDirectionTo(Main.LocalPlayer.Center);
                for (int k = 0; k < 24; k++)
                {
                    int num2 = Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), base.Projectile.Center, spinningpoint.RotatedBy(MathF.PI / 12f * (float)k), ModContent.ProjectileType<PHMutantFragment>(), base.Projectile.damage / 2, 0f, Main.myPlayer, base.Projectile.ai[0]);
                    if (num2 != Main.maxProjectiles)
                    {
                        Main.projectile[num2].timeLeft = timeLeft2;
                    }
                }
            }
            if (Projectile.ai[0] == 1)//日耀
            {
                for (int j = 0; j < 6; j++)
                {
                    Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, 12f * Projectile.SafeDirectionTo(Main.LocalPlayer.Center).RotatedBy(Projectile.rotation + j * MathHelper.TwoPi / 6),
                        ProjectileID.CultistBossFireBall, Projectile.damage, 0f, Main.myPlayer);
                } 
            }
            else if (Projectile.ai[0] == 2)//星璇
            {
                const int max = 6;
                for (int i = 0; i < max; i++)
                {
                    Vector2 dir = Projectile.SafeDirectionTo(Main.LocalPlayer.Center).RotatedBy(2 * (float)Math.PI / max * i);
                    Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, 3 * dir * Projectile.width / 120f, ModContent.ProjectileType<LightningVortexHostile>(), //ModContent.ProjectileType<CosmosLightning>(),
                        Projectile.damage, 0, Main.myPlayer, 1f, dir.ToRotation());
                }
            }
            else if (Projectile.ai[0] == 3)//星尘
            {
                /*
                for (int j = -1; j <= 1; j++) //to both sides
                {
                    if (j == 0)
                        continue;

                    const int gap = 30;
                    const int max = 15;
                    const int individualOffset = 8;
                    Vector2 baseVel = Projectile.SafeDirectionTo(Main.LocalPlayer.Center).RotatedBy(MathHelper.ToRadians(gap) * j);
                    for (int k = 0; k < max; k++) //a fan of blazes
                    {
                        Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, 6f * baseVel.RotatedBy(MathHelper.ToRadians(individualOffset) * j * k),
                            ModContent.ProjectileType<CosmosNebulaBlaze>(), Projectile.damage, 0f, Main.myPlayer, 0.009f);
                    }
                }
                */
            }
            else//星云
            {
                for (int j = -1; j <= 1; j++) //to both sides
                {
                    if (j == 0)
                        continue;

                    const int gap = 45;
                    const int max = 12;
                    const int individualOffset = 8;
                    Vector2 baseVel = Projectile.SafeDirectionTo(Main.LocalPlayer.Center).RotatedBy(MathHelper.ToRadians(gap) * j);
                    for (int k = 0; k < max; k++) //a fan of blazes
                    {
                        Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, 6f * baseVel.RotatedBy(MathHelper.ToRadians(individualOffset) * j * k),
                            ModContent.ProjectileType<CosmosNebulaBlaze>(), Projectile.damage, 0f, Main.myPlayer, 0.009f);
                    }
                }
            }
        }
    }
    public class PHMutantFragment : MutantFragment, IProjOwnedByBoss<MutantBoss>
    {
        public override void AI()
        {
            Projectile.velocity *= 0.985f;
            Projectile.rotation += Projectile.velocity.X / 30f;
            Projectile.frame = (int)Projectile.ai[0];
            if (Main.rand.NextBool(15))
            {
                var type = (int)Projectile.ai[0] switch
                {
                    0 => 242,
                    1 => 127,
                    2 => 229,
                    _ => 135,
                };
                Dust dust = Main.dust[Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, 0f, 0f, 0, new Color(), 1f)];
                dust.velocity *= 4f;
                dust.fadeIn = 1f;
                dust.scale = 1f + Main.rand.NextFloat() + Main.rand.Next(4) * 0.3f;
                dust.noGravity = true;
            }

            if (ritualID == -1) //identify the ritual CLIENT SIDE
            {
                ritualID = -2; //if cant find it, give up and dont try every tick

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<PHMutantRitual>())
                    {
                        ritualID = i;
                        break;
                    }
                }
            }

            Projectile ritual = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>());
            if (ritual != null && Projectile.Distance(ritual.Center) > 1200f) //despawn faster
                Projectile.timeLeft = 0;
        }
    }
}
