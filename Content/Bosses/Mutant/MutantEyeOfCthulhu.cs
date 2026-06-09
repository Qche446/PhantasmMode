using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Luminance.Common.Utilities;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantEyeOfCthulhu : MutantEyeOfCthulhu
    {
        public override void AI()
        {
            #region 定位
            Player player = FargoSoulsUtil.PlayerExists(base.Projectile.ai[0]);
            if (player == null)
            {
                base.Projectile.Kill();
                return;
            }

            if (!spawned)
            {
                spawned = true;
                SoundEngine.PlaySound(in SoundID.ForceRoarPitched, base.Projectile.Center);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), base.Projectile.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, -1f, 4f);
                }
            }

            if ((base.Projectile.ai[1] += 1f) < 120f)
            {
                base.Projectile.alpha -= 8;
                if (base.Projectile.alpha < 0)
                {
                    base.Projectile.alpha = 0;
                }

                base.Projectile.position += player.velocity / 2f;
                float num = base.Projectile.ai[1] * 1.5f / 120f;
                if (num < 0.25f)
                {
                    num = 0.25f;
                }

                if (num > 1f)
                {
                    num = 1f;
                }

                Vector2 vector = player.Center + base.Projectile.DirectionFrom(player.Center).RotatedBy(MathHelper.ToRadians(20f)) * 700f * num;
                float num2 = 0.6f;
                if (base.Projectile.Center.X < vector.X)
                {
                    base.Projectile.velocity.X += num2;
                    if (base.Projectile.velocity.X < 0f)
                    {
                        base.Projectile.velocity.X += num2 * 2f;
                    }
                }
                else
                {
                    base.Projectile.velocity.X -= num2;
                    if (base.Projectile.velocity.X > 0f)
                    {
                        base.Projectile.velocity.X -= num2 * 2f;
                    }
                }

                if (base.Projectile.Center.Y < vector.Y)
                {
                    base.Projectile.velocity.Y += num2;
                    if (base.Projectile.velocity.Y < 0f)
                    {
                        base.Projectile.velocity.Y += num2 * 2f;
                    }
                }
                else
                {
                    base.Projectile.velocity.Y -= num2;
                    if (base.Projectile.velocity.Y > 0f)
                    {
                        base.Projectile.velocity.Y -= num2 * 2f;
                    }
                }

                if (Math.Abs(base.Projectile.velocity.X) > 24f)
                {
                    base.Projectile.velocity.X = 24 * Math.Sign(base.Projectile.velocity.X);
                }

                if (Math.Abs(base.Projectile.velocity.Y) > 24f)
                {
                    base.Projectile.velocity.Y = 24 * Math.Sign(base.Projectile.velocity.Y);
                }

                base.Projectile.rotation = base.Projectile.SafeDirectionTo(player.Center).ToRotation() - MathF.PI / 2f;
            }
            else if (base.Projectile.ai[1] == 120f)
            {
                base.Projectile.localAI[0] = player.Center.X;
                base.Projectile.localAI[1] = player.Center.Y;
                base.Projectile.Center = player.Center + base.Projectile.DirectionFrom(player.Center) * 700f;
                base.Projectile.velocity = Vector2.Zero;
                base.Projectile.netUpdate = true;
            }
            #endregion
            else if (base.Projectile.ai[1] == 121f)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    SpawnProjectile(base.Projectile.Center - base.Projectile.velocity / 2f);
                    float ai = 0.025f;
                    float ai2 = Luminance.Common.Utilities.Utilities.SafeDirectionTo(destination: new Vector2(base.Projectile.localAI[0], base.Projectile.localAI[1]), entity: base.Projectile).ToRotation();
                    
                    int num3 = Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), base.Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MutantScythe2>(), base.Projectile.damage, 0f, Main.myPlayer, ai, ai2);
                    if (num3 != Main.maxProjectiles)
                    {
                        Main.projectile[num3].timeLeft = base.Projectile.timeLeft + 180 + 30;
                    }

                    if (WorldSavingSystem.MasochistModeReal)
                    {
                        num3 = Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), base.Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MutantScythe2>(), base.Projectile.damage, 0f, Main.myPlayer, ai, ai2);
                        if (num3 != Main.maxProjectiles)
                        {
                            Main.projectile[num3].timeLeft = base.Projectile.timeLeft + 180 + 30 + 150;
                        }
                    }
                }

                base.Projectile.velocity = 120f * base.Projectile.SafeDirectionTo(new Vector2(base.Projectile.localAI[0], base.Projectile.localAI[1])).RotatedBy(MathHelper.ToRadians(22.5f));
                base.Projectile.netUpdate = true;
                SoundEngine.PlaySound(in SoundID.ForceRoarPitched, base.Projectile.Center);
            }
            else if (base.Projectile.ai[1] < 131.666672f)
            {
                base.Projectile.rotation = base.Projectile.velocity.ToRotation() - MathF.PI / 2f;
                if (FargoSoulsUtil.HostCheck)
                {
                    SpawnProjectile(base.Projectile.Center);
                    SpawnProjectile(base.Projectile.Center - base.Projectile.velocity / 2f);
                }
            }
            else
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    SpawnProjectile(base.Projectile.Center);
                    SpawnProjectile(base.Projectile.Center - base.Projectile.velocity / 2f);
                }

                base.Projectile.ai[1] = 120f;
            }

            if (++base.Projectile.frameCounter > 6)
            {
                base.Projectile.frameCounter = 0;
                if (++base.Projectile.frame >= Main.projFrames[base.Projectile.type])
                {
                    base.Projectile.frame = 0;
                }
            }

            if (base.Projectile.frame < 3)
            {
                base.Projectile.frame = 3;
            }

            void SpawnProjectile(Vector2 position)
            {
                float ai3 = 0.03f;
                Vector2 destination2 = new Vector2(base.Projectile.localAI[0], base.Projectile.localAI[1]);
                destination2 += 180f * base.Projectile.SafeDirectionTo(destination2).RotatedBy(MathHelper.Pi / 2);
                float ai4 = base.Projectile.SafeDirectionTo(destination2).ToRotation();
                int num4 = Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), position, Vector2.Zero, ModContent.ProjectileType<MutantScythe1>(), base.Projectile.damage, 0f, Main.myPlayer, ai3, ai4);
                if (num4 != Main.maxProjectiles)
                {
                    Main.projectile[num4].timeLeft = base.Projectile.timeLeft + 180 + 30 + 150;
                    if (WorldSavingSystem.MasochistModeReal)
                    {
                        Main.projectile[num4].timeLeft -= 30;
                    }
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(163, 15);
                target.FargoSouls().MaxLifeReduction += 100;
                target.AddBuff(ModContent.BuffType<OceanicMaulBuff>(), 5400);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 120);
                target.AddBuff(ModContent.BuffType<BerserkedBuff>(), 120);
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
            }
        }
    }
}
