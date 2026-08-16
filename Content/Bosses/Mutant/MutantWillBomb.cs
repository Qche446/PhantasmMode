using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.Champions.Will;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.DataStructures;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    /// <summary>
    /// ai0控制初始生成激光的角偏，ai1=whomi，ai2控制旋转方向（正负号）
    /// 激光个数固定为4，弹幕总生存时间120帧，旋转速度从零线性增加，120帧内旋转完90°
    /// </summary>
    public class MutantWillBomb : WillBomb, IProjOwnedByBoss<MutantBoss>
    {
        float speed = 0;
        public override string Texture => "FargowiltasSouls/Content/Bosses/Champions/Will/WillBomb";
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            CooldownSlot = 1;

            Projectile.FargoSouls().DeletionImmuneRank = 1;
        }
        public override void AI()
        {
            if (Projectile.localAI[1] == 0)
            {
                speed = Projectile.velocity.Length();
            }
            Vector2 reduction = Vector2.Normalize(Projectile.velocity) * speed / 59f;
            Projectile.velocity -= reduction;

            Projectile.rotation += Projectile.velocity.Length() * 0.03f * Math.Sign(Projectile.velocity.X);

            if (++Projectile.localAI[1] == 59)
            {
                SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);

                if (Main.LocalPlayer.active)
                    ScreenShakeSystem.StartShake(10, shakeStrengthDissipationIncrement: 10f / 30);

                if (FargoSoulsUtil.HostCheck)
                {
                    const int max = 4; // 激光个数固定为4
                    float angleOffset = Projectile.ai[0]; // ai[0]控制初始生成激光的角偏
                    float rotationSign = Math.Sign(Projectile.ai[2]); // ai[2]控制旋转方向（正负号）
                    if (rotationSign == 0)
                        rotationSign = 1;

                    for (int i = 0; i < max; i++)
                    {
                        // 4束激光均匀分布（间隔90°），加上ai[0]角偏和朝向玩家的方向
                        float baseAngle = MathHelper.PiOver2 * i;
                        float totalAngle = baseAngle + angleOffset + Projectile.SafeDirectionTo(Main.LocalPlayer.Center).ToRotation();
                        Vector2 vel = Vector2.UnitX.RotatedBy(totalAngle);
                        Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, vel,
                            ModContent.ProjectileType<MutantWillDeathray>(), Projectile.damage, 0f, Main.myPlayer, rotationSign, Projectile.ai[1]);//ai[0]=旋转方向, ai[1]=whoami
                    }
                }

                Projectile.position = Projectile.Center;
                Projectile.width = 250;
                Projectile.height = 250;
                Projectile.Center = Projectile.position;

                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                Projectile.Kill();
            }
        }
        private new void SpawnSphereRing(int max, float speed, int damage, float rotationModifier)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float rotation = 2f * (float)Math.PI / max;
            Vector2 vel = Vector2.UnitY * speed;
            int type = ModContent.ProjectileType<WillTyphoon>();
            for (int i = 0; i < max; i++)
            {
                vel = vel.RotatedBy(rotation);
                Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, vel, type, damage, 0f, Main.myPlayer, rotationModifier, speed);
            }
            SoundEngine.PlaySound(SoundID.Item84, Projectile.Center);
        }
        public override void OnKill(int timeLeft)
        {


            for (int index1 = 0; index1 < 20; ++index1)
            {
                int index2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemTopaz, 0f, 0f, 100, new Color(), 3f);
                Main.dust[index2].noGravity = true;
                Main.dust[index2].velocity *= 12f;
                Main.dust[index2].noLight = true;

                int index3 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemTopaz, 0f, 0f, 100, new Color(), 2f);
                Main.dust[index3].velocity *= 9f;
                Main.dust[index3].noGravity = true;
                Main.dust[index3].noLight = true;

                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemTopaz, 0f, 0f, 100, default, 4.5f);
                Main.dust[d].velocity *= Main.rand.NextFloat(9f, 12f);
                Main.dust[d].position = Projectile.Center;
            }

            for (int i = 0; i < 50; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 4f);
                Main.dust[dust].scale *= Main.rand.NextFloat(1, 2.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Main.dust[dust].velocity.RotatedByRandom(MathHelper.ToRadians(40)) * 6f;
                Main.dust[dust].velocity *= 4f;

                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 4f);
                Main.dust[dust].velocity *= 8f;
            }

            float scaleFactor9 = 2.5f;
            for (int j = 0; j < 20; j++)
            {
                int gore = Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position + new Vector2(Main.rand.Next(Projectile.width), Main.rand.Next(Projectile.height)), Vector2.Zero, Main.rand.Next(61, 64), scaleFactor9);
                Main.gore[gore].velocity.Y += 2f;
                Main.gore[gore].velocity *= 6f;
            }
        }
    }
    public class MutantWillDeathray : BaseDeathray, IProjOwnedByBoss<MutantBoss>
    {
        public override string Texture => "FargowiltasSouls/Content/Bosses/Champions/Will/WillDeathray";
        float omiga = 0;
        float omegaSpeed = 0; // 当前角速度，从零线性增加
        public MutantWillDeathray()
        : base(120f) // 总生存时间120帧
        {
        }
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.projFrames[base.Projectile.type] = 5;
        }

        public override bool? CanDamage()
        {
            return base.Projectile.scale == 1f;
        }

        public override void AI()
        {
            Vector2? vector = null;
            if (base.Projectile.velocity.HasNaNs() || base.Projectile.velocity == Vector2.Zero)
            {
                base.Projectile.velocity = -Vector2.UnitY;
            }

            NPC nPC = FargoSoulsUtil.NPCExists(base.Projectile.ai[1], ModContent.NPCType<MutantBoss>());
            if (nPC == null || nPC.ai[0] < 0)
            {
                base.Projectile.Kill();
                return;
            }

            if (base.Projectile.velocity.HasNaNs() || base.Projectile.velocity == Vector2.Zero)
            {
                base.Projectile.velocity = -Vector2.UnitY;
            }

            if (base.Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(in SoundID.Zombie104, base.Projectile.Center);
            }

            float num = 1f;
            base.Projectile.localAI[0] += 1f;
            if (base.Projectile.localAI[0] >= maxTime)
            {
                base.Projectile.Kill();
                return;
            }

            base.Projectile.scale = (float)Math.Sin(base.Projectile.localAI[0] * MathF.PI / maxTime) * 2.5f * num;
            if (base.Projectile.scale > num)
            {
                base.Projectile.scale = num;
            }

            float num2 = base.Projectile.velocity.ToRotation() - MathF.PI / 2f;

            // 旋转速度从零线性增加，120帧内完成90°旋转
            // 角加速度 = π/2 / Σ(i for i=1..120) = π/2 / 7260
            float angularAccel = MathHelper.PiOver2 / 7260f;
            omegaSpeed += angularAccel;
            float rotDir = Math.Sign(Projectile.ai[0]);
            if (rotDir == 0)
                rotDir = 1;
            omiga += omegaSpeed * rotDir;
            num2 += omegaSpeed * rotDir;
            base.Projectile.rotation = num2;
            num2 += MathF.PI / 2f;
            base.Projectile.velocity = num2.ToRotationVector2();
            float num3 = 3f;
            _ = base.Projectile.width;
            _ = base.Projectile.Center;
            if (vector.HasValue)
            {
                _ = vector.Value;
            }

            float[] array = new float[(int)num3];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 3000f;
            }

            float num4 = 0f;
            for (int j = 0; j < array.Length; j++)
            {
                num4 += array[j];
            }

            num4 /= num3;
            float amount = 0.5f;
            base.Projectile.localAI[1] = MathHelper.Lerp(base.Projectile.localAI[1], num4, amount);
            Vector2 vector2 = base.Projectile.Center + base.Projectile.velocity * (base.Projectile.localAI[1] - 14f);
            for (int k = 0; k < 2; k++)
            {
                float num5 = base.Projectile.velocity.ToRotation() + (Main.rand.NextBool(2) ? (-1f) : 1f) * (MathF.PI / 2f);
                float num6 = (float)Main.rand.NextDouble() * 2f + 2f;
                Vector2 vector3 = new Vector2((float)Math.Cos(num5) * num6, (float)Math.Sin(num5) * num6);
                int num7 = Dust.NewDust(vector2, 0, 0, DustID.CopperCoin, vector3.X, vector3.Y);
                Main.dust[num7].noGravity = true;
                Main.dust[num7].scale = 1.7f;
            }

            if (Main.rand.NextBool(5))
            {
                Vector2 vector4 = base.Projectile.velocity.RotatedBy(1.5707963705062866) * ((float)Main.rand.NextDouble() - 0.5f) * base.Projectile.width;
                int num8 = Dust.NewDust(vector2 + vector4 - Vector2.One * 4f, 8, 8, DustID.CopperCoin, 0f, 0f, 100, default(Color), 1.5f);
                Main.dust[num8].velocity *= 0.5f;
                Main.dust[num8].velocity.Y = 0f - Math.Abs(Main.dust[num8].velocity.Y);
            }

            base.Projectile.position -= base.Projectile.velocity;
            if (++base.Projectile.frameCounter > 2)
            {
                base.Projectile.frameCounter = 0;
                if (++base.Projectile.frame >= Main.projFrames[base.Projectile.type])
                {
                    base.Projectile.frame = 0;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<DefenselessBuff>(), 300);
                target.AddBuff(ModContent.BuffType<MidasBuff>(), 300);
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 600);
            }

            target.AddBuff(BuffID.Bleeding, 300);
        }

        public float WidthFunction(float _)
        {
            return (float)base.Projectile.width * base.Projectile.scale * 3f;
        }

        public static Color ColorFunction(float _)
        {
            return new Color(253, 254, 32, 100);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (base.Projectile.velocity == Vector2.Zero)
            {
                return false;
            }

            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.WillDeathray");
            Vector2 value = base.Projectile.Center + base.Projectile.velocity.SafeNormalize(Vector2.UnitY) * drawDistance;
            Vector2 value2 = base.Projectile.Center - base.Projectile.velocity * 150f;
            Vector2[] array = new Vector2[8];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Vector2.Lerp(value2, value, (float)i / ((float)array.Length - 1f));
            }

            Color color = new Color(252, 252, 192, 100);
            shader.TrySetParameter("mainColor", color);
            ModContent.Request<Texture2D>("FargowiltasSouls/Assets/ExtraTextures/Trails/WillStreak").Value.SetTexture1();
            PrimitiveRenderer.RenderTrail(array, new PrimitiveSettings(WidthFunction, ColorFunction, null, Smoothen: true, Pixelate: false, shader), 30);
            return false;
        }
    }
}
