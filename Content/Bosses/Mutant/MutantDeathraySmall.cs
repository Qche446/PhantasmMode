using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantDeathraySmall : MutantDeathraySmall
    {
        //是预警线阿(由Mark1生成）
        public override void SetStaticDefaults()
        {
            maxTime = 60;
        }
        public override void AI()
        {
            Vector2? vector = null;
            if (base.Projectile.velocity.HasNaNs() || base.Projectile.velocity == Vector2.Zero)
            {
                base.Projectile.velocity = -Vector2.UnitY;
            }

            if (base.Projectile.velocity.HasNaNs() || base.Projectile.velocity == Vector2.Zero)
            {
                base.Projectile.velocity = -Vector2.UnitY;
            }

            float num = 0.3f;
            base.Projectile.localAI[0] += 1f;
            if (base.Projectile.localAI[0] >= maxTime)
            {
                base.Projectile.Kill();
                return;
            }

            base.Projectile.scale = (float)Math.Sin(base.Projectile.localAI[0] * MathF.PI / maxTime) * 0.6f * num;
            if (base.Projectile.scale > num)
            {
                base.Projectile.scale = num;
            }

            float num2 = 3f;
            _ = base.Projectile.width;
            _ = base.Projectile.Center;
            if (vector.HasValue)
            {
                _ = vector.Value;
            }

            float[] array = new float[(int)num2];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 3000f;
            }

            float num3 = 0f;
            for (int j = 0; j < array.Length; j++)
            {
                num3 += array[j];
            }

            num3 /= num2;
            float amount = 0.5f;
            base.Projectile.localAI[1] = MathHelper.Lerp(base.Projectile.localAI[1], num3, amount);
            Vector2 vector2 = base.Projectile.Center + base.Projectile.velocity * (base.Projectile.localAI[1] - 14f);
            for (int k = 0; k < 2; k++)
            {
                float num4 = base.Projectile.velocity.ToRotation() + (Main.rand.NextBool(2) ? (-1f) : 1f) * (MathF.PI / 2f);
                float num5 = (float)Main.rand.NextDouble() * 2f + 2f;
                Vector2 vector3 = new Vector2((float)Math.Cos(num4) * num5, (float)Math.Sin(num4) * num5);
                int num6 = Dust.NewDust(vector2, 0, 0, 244, vector3.X, vector3.Y);
                Main.dust[num6].noGravity = true;
                Main.dust[num6].scale = 1.7f;
            }

            if (Main.rand.NextBool(5))
            {
                Vector2 vector4 = base.Projectile.velocity.RotatedBy(1.5707963705062866) * ((float)Main.rand.NextDouble() - 0.5f) * base.Projectile.width;
                int num7 = Dust.NewDust(vector2 + vector4 - Vector2.One * 4f, 8, 8, 244, 0f, 0f, 100, default(Color), 1.5f);
                Main.dust[num7].velocity *= 0.5f;
                Main.dust[num7].velocity.Y = 0f - Math.Abs(Main.dust[num7].velocity.Y);
            }

            base.Projectile.position -= base.Projectile.velocity;
            base.Projectile.rotation = base.Projectile.velocity.ToRotation() - MathF.PI / 2f;
        }
    }
}
