using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Abom
{
    public class AbomRitualFTW : BaseArena
    {
        public override string Texture => "FargowiltasSouls/Content/Bosses/AbomBoss/AbomDeathScythe";

        private const float realRotation = MathHelper.Pi / 150f;
        private int fadeInTimer = 0;
        private const int fadeInTime = 60; // 60帧渐显时间

        // 新增：拖尾系统
        private List<Vector2>[] scytheTrailPositions;
        private List<float>[] scytheTrailRotations;
        private const int TrailLength = 8;
        private const int ScytheCount = 32;

        public AbomRitualFTW() : base(realRotation, 900f, ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>(), 87) { }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Abominationn Seal");
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            CooldownSlot = ImmunityCooldownID.TileContactDamage;

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
            Projectile.rotation += 0.015f;

            // 渐显效果计时器
            if (fadeInTimer < fadeInTime)
                fadeInTimer++;

            // 更新拖尾系统
            UpdateTrails();

            // 生成橙色主题粒子效果
            SpawnOrangeParticles();
        }

        // 更新拖尾系统
        private void UpdateTrails()
        {
            float radius = threshold;
            float fadeProgress = fadeInTimer / (float)fadeInTime;

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

        // 生成橙色主题粒子效果
        private void SpawnOrangeParticles()
        {
            float radius = threshold;
            float fadeProgress = fadeInTimer / (float)fadeInTime;

            for (int i = 0; i < ScytheCount; i++)
            {
                if (Main.rand.NextBool(8)) // 25% 几率每帧生成粒子
                {
                    float angle = MathHelper.TwoPi * i / ScytheCount + Projectile.rotation;
                    Vector2 position = Projectile.Center + new Vector2(radius, 0).RotatedBy(angle);

                    // 橙色主题粒子 - 使用火炬粒子
                    int dust = Dust.NewDust(position, 0, 0, DustID.Torch, 0f, 0f, 0, default, 1.6f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = Vector2.Zero;

                    // 次级橙色粒子 - 使用火焰粒子
                    if (Main.rand.NextBool(3))
                    {
                        int smallDust = Dust.NewDust(position, 0, 0, DustID.GreenTorch, 0f, 0f, 0, default, 1.3f); // DustID.Flames = 61
                        Main.dust[smallDust].noGravity = true;
                        Main.dust[smallDust].velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
                    }

                    // 偶尔生成特效火焰粒子
                    if (Main.rand.NextBool(8))
                    {
                        int fireDust = Dust.NewDust(position, 0, 0, DustID.FlameBurst, 0f, 0f, 0, default, 1.4f);
                        Main.dust[fireDust].noGravity = true;
                        Main.dust[fireDust].velocity = Main.rand.NextVector2Circular(2f, 2f);
                    }
                }
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 0, 0) * Projectile.Opacity * (targetPlayer == Main.myPlayer ? 1f : 0.15f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Bleeding, 240);
            if (WorldSavingSystem.EternityMode)
                target.AddBuff(ModContent.BuffType<AbomFangBuff>(), 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 获取原版死神镰刀纹理
            Texture2D sickleTexture = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/AbomBoss/AbomDeathScythe").Value;
            Rectangle frame = new(0, 0, sickleTexture.Width, sickleTexture.Height);
            Vector2 origin = frame.Size() / 2f;

            // 计算渐显进度 (0到1)
            float fadeProgress = fadeInTimer / (float)fadeInTime;

            // 切换到加算混合模式
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 绘制橙色主题拖尾
            DrawOrangeTrails(sickleTexture, frame, origin, fadeProgress);

            // 亮橙色，随着渐显进度调整透明度
            Color brightOrange = new Color(255, 255, 0, (int)(220 * fadeProgress)) * Projectile.Opacity;

            // 基础环 - 28个镰刀
            for (int i = 0; i < ScytheCount; i++)
            {
                float angle = MathHelper.TwoPi * i / ScytheCount + Projectile.rotation;
                Vector2 position = Projectile.Center + new Vector2(threshold, 0).RotatedBy(angle);

                // 尺寸也随着渐显进度增加
                float scale = 2.0f * fadeProgress;

                Main.EntitySpriteDraw(sickleTexture,
                    position - Main.screenPosition,
                    frame,
                    brightOrange,
                    120 * angle + MathHelper.PiOver2, // 指向圆周切线
                    origin,
                    scale,
                    SpriteEffects.None,
                    0);
            }

            // 切换回默认混合模式
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        // 绘制橙色主题拖尾
        private void DrawOrangeTrails(Texture2D texture, Rectangle frame, Vector2 origin, float fadeProgress)
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

                    // 计算拖尾的透明度和缩放（考虑渐显进度）
                    float trailOpacity = (float)(TrailLength - j) / TrailLength * Projectile.Opacity * 0.5f * fadeProgress;
                    float trailScale = 2.0f * fadeProgress * (0.4f + 0.6f * (float)(TrailLength - j) / TrailLength);

                    // 橙色拖尾颜色（从亮橙色到暗橙色）
                    Color trailColor = Color.Lerp(Color.Orange, Color.DarkOrange, j / (float)TrailLength) * trailOpacity;

                    Main.EntitySpriteDraw(texture, trailPosition - Main.screenPosition, frame,
                        trailColor, trailRotation, origin, trailScale, SpriteEffects.None, 0);
                }
            }
        }
    }
}