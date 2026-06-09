using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using FargowiltasSouls.Content.Buffs.Masomode;
namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Destroyer
{
    public class DestroyerDeathray : BaseDeathray
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Deathrays/PhantasmalDeathrayWOF";
        public DestroyerDeathray() : base(300, 0f, 1f, 3000, 15, BaseDeathray.TextureSheeting.Horizontal) { }
        private Vector2 spawnPos;
        public bool fadeStart = false;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.scale = 0.1f;
        }
        public override void AI()
        {
            // --- 绑定 NPC 位置逻辑 (新增) ---
            int ownerIndex = (int)Projectile.ai[0];
            bool hasValidOwner = ownerIndex >= 0 && Main.npc[ownerIndex].active && Main.npc[ownerIndex].type == NPCID.TheDestroyerBody;
            if (hasValidOwner)
            {
                Projectile.Center = Main.npc[ownerIndex].Center;
            }
            Vector2? vector78 = null;
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = -Vector2.UnitY;
            }
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = -Vector2.UnitY;
            }
            if (Projectile.localAI[0] == 0f)
            {
                if (!Main.dedServ)
                    SoundEngine.PlaySound(FargosSoundRegistry.GenericDeathray, Projectile.Center);
            }
            else //vibrate beam
            {
                Projectile.Center = spawnPos + Main.rand.NextVector2Circular(5, 5);
            }

            float num801 = 5f;
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] >= maxTime)
            {
                Projectile.Kill();
                return;
            }
            Projectile.scale = (float)Math.Sin(Projectile.localAI[0] * 3.14159274f / maxTime) * num801 * 6f;
            if (Projectile.scale > num801)
            {
                Projectile.scale = num801;
            }
            float num805 = 3f;
            float num806 = Projectile.width;
            Vector2 samplingPoint = Projectile.Center;
            if (vector78.HasValue)
            {
                samplingPoint = vector78.Value;
            }
            float[] array3 = new float[(int)num805];
            //Collision.LaserScan(samplingPoint, Projectile.velocity, num806 * Projectile.scale, 3000f, array3);
            for (int i = 0; i < array3.Length; i++)
                array3[i] = 3000f;
            float num807 = 0f;
            int num3;
            for (int num808 = 0; num808 < array3.Length; num808 = num3 + 1)
            {
                num807 += array3[num808];
                num3 = num808;
            }
            num807 /= num805;
            float amount = 0.5f;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], num807, amount);
            Vector2 vector79 = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14f);
            for (int num809 = 0; num809 < 2; num809 = num3 + 1)
            {
                float num810 = Projectile.velocity.ToRotation() + (Main.rand.NextBool(2) ? -1f : 1f) * 1.57079637f;
                float num811 = (float)Main.rand.NextDouble() * 2f + 2f;
                Vector2 vector80 = new((float)Math.Cos((double)num810) * num811, (float)Math.Sin((double)num810) * num811);
                int num812 = Dust.NewDust(vector79, 0, 0, DustID.Electric, vector80.X, vector80.Y, 0, default, 1f);
                Main.dust[num812].noGravity = true;
                Main.dust[num812].scale = 1.7f;
                num3 = num809;
            }
            if (Main.rand.NextBool(5))
            {
                Vector2 value29 = Projectile.velocity.RotatedBy(1.5707963705062866, default) * ((float)Main.rand.NextDouble() - 0.5f) * (float)Projectile.width;
                int num813 = Dust.NewDust(vector79 + value29 - Vector2.One * 4f, 8, 8, DustID.CopperCoin, 0f, 0f, 100, default, 1.5f);
                Dust dust = Main.dust[num813];
                dust.velocity *= 0.5f;
                Main.dust[num813].velocity.Y = -Math.Abs(Main.dust[num813].velocity.Y);
            }

            Projectile.position -= Projectile.velocity;
            Projectile.rotation = Projectile.velocity.ToRotation() - 1.57079637f;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(BuffID.Electrified, 60);
                target.AddBuff(ModContent.BuffType<LightningRodBuff>(), 600);
            }
        }
        public float WidthFunction(float _)
        {
            return (float)base.Projectile.width * base.Projectile.scale * 2f;
        }
        public static Color ColorFunction(float _)
        {
            return new Color(0, 150, 255, 100);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
            {
                return false;
            }
            Vector2 laserEnd = Projectile.Center + Utils.SafeNormalize(Projectile.velocity, Vector2.UnitY) * drawDistance;
            Vector2 initialDrawPoint = Projectile.Center - Projectile.velocity * 150f;

            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
            {
                baseDrawPoints[i] = Vector2.Lerp(initialDrawPoint, laserEnd, (float)i / ((float)baseDrawPoints.Length - 1f));
            }

            Color brightColor = new Color(0, 150, 255, 100);

            // 获取着色器
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.WillBigDeathray");
            shader.TrySetParameter("mainColor", brightColor);
            Texture2D fademap = ModContent.Request<Texture2D>("FargowiltasSouls/Assets/ExtraTextures/Trails/WillStreak").Value;
            FargoSoulsUtil.SetTexture1(fademap);
            PrimitiveRenderer.RenderTrail(baseDrawPoints, new(WidthFunction, ColorFunction, Shader: shader), 30);


            return false;


        }
    }
}
