using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Globals;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantSlimeSpike : MutantSlimeSpike
    {
        public override void AI()
        {
            Vector2 acc = 0.1f * Vector2.UnitX.RotatedBy(MathHelper.Pi * Projectile.ai[1] / 180f);
            Projectile.localAI[1]++;
            Projectile.velocity += acc;
            Vector2 vel = Vector2.Normalize(Projectile.velocity);
            if (Projectile.velocity.Length() > 7)
            {
                Projectile.velocity -= 0.03f * vel;
            }
            //speed = Projectile.velocity.Length();
            #region 初始
            base.Projectile.frame = (int)base.Projectile.ai[2];
            base.Projectile.rotation = base.Projectile.velocity.ToRotation() - MathF.PI / 2f;
            if (base.Projectile.localAI[0] == 0f)
            {
                base.Projectile.localAI[0] += Main.rand.Next(1, 4);
            }
            if (base.Projectile.timeLeft % base.Projectile.MaxUpdates == 0 && ++base.Projectile.frameCounter >= 6)
            {
                base.Projectile.frameCounter = 0;
                if (++base.Projectile.frame >= Main.projFrames[base.Projectile.type])
                {
                    base.Projectile.frame = 0;
                }
            }
            if ((base.Projectile.localAI[1] += 1f) > 10f && FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<MutantBoss>()) && Math.Sign(base.Projectile.Center.Y - Main.npc[EModeGlobalNPC.mutantBoss].Center.Y) == Math.Sign(base.Projectile.velocity.Y) && base.Projectile.Distance(Main.npc[EModeGlobalNPC.mutantBoss].Center) > 1200f + base.Projectile.ai[0])
            {
                base.Projectile.timeLeft = 0;
            }
            #endregion
        }
    }
}
