using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantSpearDash : MutantSpearDash
    {
        public override void AI()
        {
            if (base.Projectile.localAI[1] == 0f)
            {
                base.Projectile.localAI[1] = 1f;
                if (!WorldSavingSystem.masochistModeReal)
                {
                    if (base.Projectile.ai[1] != -2f)
                    {
                        SoundEngine.PlaySound(in FargosSoundRegistry.PenetratorThrow, base.Projectile.Center);
                    }

                    if (base.Projectile.ai[1] == -2f)
                    {
                        SoundEngine.PlaySound(in FargosSoundRegistry.PenetratorExplosion, base.Projectile.Center);
                    }
                }
                else
                {
                    SoundEngine.PlaySound(in FargosSoundRegistry.PenetratorExplosion, base.Projectile.Center);
                }
            }

            NPC nPC = Main.npc[(int)base.Projectile.ai[0]];
            if (nPC.active && nPC.type == ModContent.NPCType<MutantBoss>() && (nPC.ai[0] == 6f || nPC.ai[0] == 15f || nPC.ai[0] == 23f))
            {
                base.Projectile.velocity = Vector2.Normalize(nPC.velocity);
                base.Projectile.position -= base.Projectile.velocity;
                base.Projectile.rotation = nPC.velocity.ToRotation() + MathHelper.ToRadians(135f);
                base.Projectile.Center = nPC.Center + nPC.velocity;
                if ((base.Projectile.ai[1] <= 0f || WorldSavingSystem.MasochistModeReal) && (base.Projectile.localAI[0] -= 1f) < 0f)
                {
                    if (base.Projectile.ai[1] == -2f)
                    {
                        base.Projectile.localAI[0] = 1f;
                        for (int i = -1; i <= 1; i += 2)
                        {
                            if (FargoSoulsUtil.HostCheck)
                            {
                                int num = Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), base.Projectile.Center, 16f * Vector2.Normalize(nPC.velocity).RotatedBy(MathF.PI / 2f * (float)i), ModContent.ProjectileType<PHMutantSphereSmall>(), base.Projectile.damage, 0f, base.Projectile.owner, 0f);
                            }
                        }
                    }
                    else if (WorldSavingSystem.MasochistModeReal)
                    {
                        base.Projectile.localAI[0] = 2f;
                        for (int j = -1; j <= 1; j += 2)
                        {
                            if (FargoSoulsUtil.HostCheck)
                            {
                                int num2 = Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), base.Projectile.Center, 8f * Vector2.Normalize(nPC.velocity).RotatedBy(MathF.PI / 2f * (float)j), ModContent.ProjectileType<PHMutantSphereSmall>(), base.Projectile.damage, 0f, base.Projectile.owner, 0f);
                               
                            }
                        }
                    }
                    else
                    {
                        base.Projectile.localAI[0] = 2f;
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Projectile.NewProjectile(Terraria.Entity.InheritSource(base.Projectile), base.Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MutantSphereSmall>(), base.Projectile.damage, 0f, base.Projectile.owner, nPC.target);
                        }
                    }
                }
            }
            else
            {
                base.Projectile.Kill();
            }

            scaletimer += 1f;
        }
    }
}
