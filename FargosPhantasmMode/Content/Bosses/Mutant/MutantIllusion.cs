using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantIllusion : MutantIllusion, IProjOwnedByBoss<MutantBoss>
    {
        //四柱用
        public override void AI()
        {
            NPC nPC = FargoSoulsUtil.NPCExists(base.NPC.ai[0], ModContent.NPCType<MutantBoss>());
            if (nPC == null || nPC.ai[0] < 18f || nPC.ai[0] > 19f || nPC.life <= 1)
            {
                base.NPC.life = 0;
                base.NPC.HitEffect();
                base.NPC.SimpleStrikeNPC(int.MaxValue, 0, crit: false, 0f, null, damageVariation: false, 0f, noPlayerInteraction: true);
                base.NPC.active = false;
                for (int i = 0; i < 40; i++)
                {
                    int num = Dust.NewDust(base.NPC.position, base.NPC.width, base.NPC.height, 5);
                    Main.dust[num].velocity *= 2.5f;
                    Main.dust[num].scale += 0.5f;
                }

                for (int j = 0; j < 20; j++)
                {
                    int num2 = Dust.NewDust(base.NPC.position, base.NPC.width, base.NPC.height, DustID.Vortex, 0f, 0f, 0, default(Color), 2f);
                    Main.dust[num2].noGravity = true;
                    Main.dust[num2].noLight = true;
                    Main.dust[num2].velocity *= 9f;
                }

                return;
            }

            base.NPC.target = nPC.target;
            base.NPC.damage = nPC.damage;
            base.NPC.defDamage = nPC.damage;
            base.NPC.frame.Y = nPC.frame.Y;
            if (base.NPC.HasValidTarget)
            {
                Vector2 center = Main.player[nPC.target].Center;
                Vector2 vector = center - nPC.Center;
                base.NPC.Center = center;
                base.NPC.position.X += vector.X * base.NPC.ai[1];
                base.NPC.position.Y += vector.Y * base.NPC.ai[2];
                base.NPC.direction = (base.NPC.spriteDirection = ((base.NPC.position.X < Main.player[base.NPC.target].position.X) ? 1 : (-1)));
            }
            else
            {
                base.NPC.Center = nPC.Center;
            }

            if ((base.NPC.ai[3] -= 1f) == 0f)
            {
                int num3 = ((!(base.NPC.ai[1] < 0f)) ? ((base.NPC.ai[2] < 0f) ? 1 : 2) : 0);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(nPC.GetSource_FromThis(), base.NPC.Center, Vector2.UnitY * -5f, ModContent.ProjectileType<PHMutantPillar>(), FargoSoulsUtil.ScaledProjectileDamage(nPC.damage, 1.33333337f), 0f, Main.myPlayer, num3, base.NPC.whoAmI);
                }
            }

            if (Main.getGoodWorld && (base.NPC.localAI[0] += 1f) > 6f)
            {
                base.NPC.localAI[0] = 0f;
                base.NPC.AI();
            }
        }
    }
}
