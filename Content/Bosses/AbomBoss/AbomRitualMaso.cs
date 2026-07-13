using FargowiltasSouls.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss
{
    public class AbomRitualMaso : BaseArena
    {
        private const float realRotation = -MathHelper.Pi / 180f;

        // 新增：拖尾系统
        private List<Vector2>[] scytheTrailPositions;
        private List<float>[] scytheTrailRotations;
        private const int TrailLength = 6;
        private const int ScytheCount = 36;

        public AbomRitualMaso() : base(realRotation, 1100f, ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>(), 87) { }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Abominationn Seal");
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            CooldownSlot = ImmunityCooldownID.Bosses;

            // 初始化拖尾系统
            InitializeTrails();
        }

        private void InitializeTrails()
        {
            scytheTrailPositions = new List<Vector2>[ScytheCount];
            scytheTrailRotations = new List<float>[ScytheCount];

            for (int i = 0; i < ScytheCount; i++)
            {
                scytheTrailPositions[i] = new List<Vector2>();
                scytheTrailRotations[i] = new List<float>();
            }
        }

        public int Timer = 0;
        public static int StartTime = 90;

        protected override void Movement(NPC npc)
        {
            Projectile.velocity = npc.Center - Projectile.Center;
            if (npc.ai[0] != 8) //snaps directly to abom when preparing for p2 attack
                Projectile.velocity /= 40f;

            rotationPerTick = realRotation;
        }

        public override void AI()
        {
            base.AI();
            Projectile.rotation -= 0.015f;
            if (Timer < StartTime)
            {
                Timer++;
                threshold = MathHelper.Lerp(2000f, 1100f, (float)Timer / StartTime);
            }

            // 更新拖尾系统
            UpdateTrails();

            // 生成金色主题粒子效果
            SpawnGoldenParticles();
        }

        // 更新拖尾系统
        private void UpdateTrails()
        {
            float radius = threshold;

            for (int i = 0; i < ScytheCount; i++)
            {
                float angle = MathHelper.TwoPi * i / ScytheCount + Projectile.rotation;
                Vector2 position = Projectile.Center + new Vector2(radius, 0).RotatedBy(angle);
                float scytheRotation = 120 * angle + MathHelper.PiOver2;

                // 添加当前位置到拖尾
                scytheTrailPositions[i].Insert(0, position);
                scytheTrailRotations[i].Insert(0, scytheRotation);

                // 限制拖尾长度
                if (scytheTrailPositions[i].Count > TrailLength)
                {
                    scytheTrailPositions[i].RemoveAt(TrailLength);
                    scytheTrailRotations[i].RemoveAt(TrailLength);
                }
            }
        }

        // 生成金色主题粒子效果
        private void SpawnGoldenParticles()
        {
            float radius = threshold;

            for (int i = 0; i < ScytheCount; i++)
            {
                if (Main.rand.NextBool(8)) // 25% 几率每帧生成粒子
                {
                    float angle = MathHelper.TwoPi * i / ScytheCount + Projectile.rotation;
                    Vector2 position = Projectile.Center + new Vector2(radius, 0).RotatedBy(angle);

                    // 金色主题粒子
                    int dust = Dust.NewDust(position, 0, 0, DustID.GoldCoin, 0f, 0f, 0, default, 1.8f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = Vector2.Zero;

                    // 次级金色粒子
                    if (Main.rand.NextBool(3))
                    {
                        int smallDust = Dust.NewDust(position, 0, 0, DustID.Gold, 0f, 0f, 0, default, 1.2f);
                        Main.dust[smallDust].noGravity = true;
                        Main.dust[smallDust].velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
                    }

                    // 偶尔生成闪光粒子
                    if (Main.rand.NextBool(8))
                    {
                        int flashDust = Dust.NewDust(position, 0, 0, DustID.YellowStarDust, 0f, 0f, 0, default, 1.5f);
                        Main.dust[flashDust].noGravity = true;
                        Main.dust[flashDust].velocity = Main.rand.NextVector2Circular(2f, 2f);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sickleTexture = ModContent.Request<Texture2D>("Terraria/Images/Projectile_274").Value;
            Rectangle frame = new(0, 0, sickleTexture.Width, sickleTexture.Height);
            Vector2 origin = frame.Size() / 2f;

            // 切换到加算混合模式
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 绘制拖尾效果
            DrawGoldenTrails(sickleTexture, frame, origin);

            // 最大亮度金色（接近白色但保持金色色调）
            Color maxBrightGold = new Color(255, 255, 0, 0) * Projectile.Opacity;

            // 32个超大镰刀组成基础环
            for (int i = 0; i < ScytheCount; i++)
            {
                float angle = MathHelper.TwoPi * i / ScytheCount + Projectile.rotation;
                Vector2 position = Projectile.Center + new Vector2(threshold, 0).RotatedBy(angle);

                Main.EntitySpriteDraw(sickleTexture,
                    position - Main.screenPosition,
                    frame,
                    maxBrightGold,
                    120 * angle + MathHelper.PiOver2,
                    origin,
                    2.0f, // 最大尺寸
                    SpriteEffects.None,
                    0);
            }

            // 切换回默认混合模式
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        // 绘制金色主题拖尾
        private void DrawGoldenTrails(Texture2D texture, Rectangle frame, Vector2 origin)
        {
            for (int i = 0; i < ScytheCount; i++)
            {
                // 绘制拖尾
                for (int j = 0; j < scytheTrailPositions[i].Count; j++)
                {
                    if (j >= scytheTrailPositions[i].Count || j >= scytheTrailRotations[i].Count)
                        continue;

                    Vector2 trailPosition = scytheTrailPositions[i][j];
                    float trailRotation = scytheTrailRotations[i][j];

                    // 计算拖尾的透明度和缩放
                    float trailOpacity = (float)(TrailLength - j) / TrailLength * Projectile.Opacity * 0.6f;
                    float trailScale = 2.0f * (0.4f + 0.6f * (float)(TrailLength - j) / TrailLength);

                    // 金色拖尾颜色（从亮金色到暗金色）
                    Color trailColor = Color.Lerp(Color.Gold, Color.DarkGoldenrod, j / (float)TrailLength) * trailOpacity;

                    Main.EntitySpriteDraw(texture, trailPosition - Main.screenPosition, frame,
                        trailColor, trailRotation, origin, trailScale, SpriteEffects.None, 0);
                }
            }
        }
    }
}