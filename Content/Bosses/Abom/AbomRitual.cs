using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static FargosPhantasmMode.Common.IDelegateStateMachine;

namespace FargosPhantasmMode.Content.Bosses.Abom
{
    public class AbomRitual : BaseArena
    {
        public override string Texture => "FargowiltasSouls/Content/Bosses/AbomBoss/AbomDeathScythe";

        private const float realRotation = MathHelper.Pi / 180f;
        public float VisualScale = 0f;

        // 新增：拖尾系统
        private List<Vector2>[] scytheTrailPositions;
        private List<float>[] scytheTrailRotations;
        private const int TrailLength = 8; // 拖尾长度

        public AbomRitual() : base(realRotation, 1400f, ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>(), 87, visualCount: 64) { }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;

            // 初始化拖尾系统
            InitializeTrails();
        }

        private void InitializeTrails()
        {
            int scytheCount = 54;
            scytheTrailPositions = new List<Vector2>[scytheCount];
            scytheTrailRotations = new List<float>[scytheCount];

            for (int i = 0; i < scytheCount; i++)
            {
                scytheTrailPositions[i] = new List<Vector2>();
                scytheTrailRotations[i] = new List<float>();
            }
        }

        protected override void Movement(NPC npc)
        {
            npc.TryGetGlobalNPC<AbomBossOverride>(out AbomBossOverride abom);
            if (!AbomBossOverride.RitualCanNotMove.Contains(abom.AIState))
            {
                Projectile.velocity = npc.Center - Projectile.Center;
                Projectile.velocity /= abom.AIState == abom.ChooseStrongAttack ? 10f : 40f; //snaps to abom faster when preparing for p2 attack

                rotationPerTick = realRotation;
            }
            else //remains still in higher AIs
            {
                Projectile.velocity = Vector2.Zero;

                rotationPerTick = -realRotation / 10f; //denote arena isn't moving
            }
        }

        public override void AI()
        {
            base.AI();
            NPC npc = FargoSoulsUtil.NPCExists(Projectile.ai[1], npcType);
            Projectile.rotation += 0.015f;
            if (Projectile.Opacity < 0.5f && npc != null)
                Projectile.Opacity = 0.5f;
            if (VisualScale < 1)
                VisualScale += 0.01f;

            // 更新拖尾系统
            UpdateTrails();

            // 生成紫色主题粒子效果
            SpawnPurpleParticles();

            if (!WorldSavingSystem.MasochistModeReal)
            {
                Player player = Main.LocalPlayer;
                if (player.active && !player.dead && !player.ghost)
                {
                    float distance = player.Distance(Projectile.Center);
                    if (distance > threshold && distance < threshold * 1.4f)
                    {
                        player.AddBuff(BuffID.Bleeding, 240);
                        if (WorldSavingSystem.EternityMode)
                            player.AddBuff(ModContent.BuffType<AbomFangBuff>(), 240);
                    }
                }
            }
        }

        // 新增：更新拖尾系统
        private void UpdateTrails()
        {
            int scytheCount = 54;
            float radius = threshold * VisualScale;

            for (int i = 0; i < scytheCount; i++)
            {
                float angle = MathHelper.TwoPi * i / scytheCount + Projectile.rotation;
                Vector2 position = Projectile.Center + angle.ToRotationVector2() * radius;
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

        // 新增：生成紫色主题粒子效果
        private void SpawnPurpleParticles()
        {
            int scytheCount = 54;
            float radius = threshold * VisualScale;

            for (int i = 0; i < scytheCount; i++)
            {
                if (Main.rand.NextBool(8)) // 33% 几率每帧生成粒子
                {
                    float angle = MathHelper.TwoPi * i / scytheCount + Projectile.rotation;
                    Vector2 position = Projectile.Center + angle.ToRotationVector2() * radius;

                    // 生成主粒子 - 紫水晶
                    int dust = Dust.NewDust(position, 0, 0, DustID.GemAmethyst, 0f, 0f, 0, default, 2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = Vector2.Zero;

                    // 生成次级粒子 - 紫色火焰
                    if (Main.rand.NextBool(2))
                    {
                        int smallDust = Dust.NewDust(position, 0, 0, DustID.Shadowflame, 0f, 0f, 0, default, 1.2f);
                        Main.dust[smallDust].noGravity = true;
                        Main.dust[smallDust].velocity = Main.rand.NextVector2Circular(2f, 2f);
                    }

                    // 偶尔生成特效粒子 - 紫色火花
                    if (Main.rand.NextBool(10))
                    {
                        int effectDust = Dust.NewDust(position, 0, 0, DustID.PurpleTorch, 0f, 0f, 0, default, 1.5f);
                        Main.dust[effectDust].noGravity = true;
                        Main.dust[effectDust].velocity = Main.rand.NextVector2Circular(3f, 3f);
                    }

                    // 偶尔生成紫色魔法粒子
                    if (Main.rand.NextBool(15))
                    {
                        int magicDust = Dust.NewDust(position, 0, 0, DustID.MagicMirror, 0f, 0f, 0, default, 1.3f);
                        Main.dust[magicDust].noGravity = true;
                        Main.dust[magicDust].velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
                    }
                }
            }
        }

        public override bool CanHitPlayer(Player target)
        {
            return WorldSavingSystem.MasochistModeReal ? base.CanHitPlayer(target) : false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            base.OnHitPlayer(target, info);

            target.AddBuff(BuffID.Bleeding, 240);
            if (WorldSavingSystem.EternityMode)
                target.AddBuff(ModContent.BuffType<AbomFangBuff>(), 240);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Projectile.hide)
                behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/AbomBoss/AbomDeathScythe").Value;

            // 计算仪式圆环的参数
            float radius = threshold * VisualScale;
            int scytheCount = 54;
            float scale = 2f;

            // 切换到加算混合模式，使效果更明亮
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 绘制紫色主题拖尾效果
            DrawPurpleTrails(texture, radius, scytheCount, scale);

            // 绘制主镰刀 - 使用紫色调
            for (int i = 0; i < scytheCount; i++)
            {
                float angle = MathHelper.TwoPi * i / scytheCount + Projectile.rotation;
                Vector2 position = Projectile.Center + angle.ToRotationVector2() * radius;

                // 计算镰刀的旋转角度（指向圆心）
                float scytheRotation = 120 * angle + MathHelper.PiOver2;

                // 计算帧矩形
                Rectangle frame = texture.Bounds;
                Vector2 origin = frame.Size() / 2f;

                // 使用紫色调绘制主镰刀
                Color purpleColor = Color.Lerp(Color.Purple, Color.Violet, 0.3f) * Projectile.Opacity;
                Main.EntitySpriteDraw(texture, position - Main.screenPosition, frame,
                    purpleColor, scytheRotation, origin, scale, SpriteEffects.None, 0);
            }

            // 切换回默认混合模式
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        // 新增：绘制紫色主题拖尾效果
        private void DrawPurpleTrails(Texture2D texture, float radius, int scytheCount, float baseScale)
        {
            Rectangle frame = texture.Bounds;
            Vector2 origin = frame.Size() / 2f;

            for (int i = 0; i < scytheCount; i++)
            {
                // 绘制拖尾
                for (int j = 0; j < scytheTrailPositions[i].Count; j++)
                {
                    if (j >= scytheTrailPositions[i].Count || j >= scytheTrailRotations[i].Count)
                        continue;

                    Vector2 trailPosition = scytheTrailPositions[i][j];
                    float trailRotation = scytheTrailRotations[i][j];

                    // 计算拖尾的透明度和缩放
                    float trailOpacity = (float)(TrailLength - j) / TrailLength * Projectile.Opacity * 0.7f;
                    float trailScale = baseScale * (0.5f + 0.5f * (float)(TrailLength - j) / TrailLength);

                    // 使用紫色渐变颜色（从紫罗兰色到深紫色）
                    Color trailColor = Color.Lerp(Color.Violet, Color.Purple, j / (float)TrailLength) * trailOpacity;

                    Main.EntitySpriteDraw(texture, trailPosition - Main.screenPosition, frame,
                        trailColor, trailRotation, origin, trailScale, SpriteEffects.None, 0);
                }
            }
        }
    }
}