using FargowiltasSouls.Content.Bosses.MutantBoss;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    /// <summary>
    /// 这个是4片叶绿水晶的铁处女弹幕，会高速旋转.ai0决定发射上下,ai1决定旋转方向(左负右正),ai2决定初始角偏移
    /// </summary>
    public class IronVirgin : MutantMark2
    {
        public override void AI()
        {
            Projectile.hostile = false;
            if (base.Projectile.localAI[0] == 0f)
            {
                base.Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(in SoundID.Item84, base.Projectile.Center);
                int max = 4;
                const float distance = 60f;
                float rotation = MathHelper.TwoPi / max;
                for (int i = 0; i < max; i++)
                {
                    float myRot = rotation * i + Projectile.ai[2];
                    Vector2 spawnPos = Projectile.Center + new Vector2(distance, 0f).RotatedBy(myRot);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<PHMutantCrystalLeaf>(), Projectile.damage, 0f, Main.myPlayer, Projectile.whoAmI, myRot, Projectile.ai[1]);
                }

            }

            if (++Projectile.localAI[1] == 60f)
            {
                base.Projectile.netUpdate = true;
                //Player player = Main.player[Player.FindClosest(base.Projectile.position, base.Projectile.width, base.Projectile.height)];
                base.Projectile.velocity = Vector2.UnitY * 10f * Projectile.ai[0];
                SoundEngine.PlaySound(in SoundID.Item84, base.Projectile.Center);
            }
        }
    }
}
