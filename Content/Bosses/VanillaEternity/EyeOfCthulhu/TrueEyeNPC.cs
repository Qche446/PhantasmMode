using FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu;
using FargowiltasSouls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    public class TrueEyeNPC : ModNPC
    {
        // 发射方向（从传入参数获取）
        private Vector2 shootDirection = Vector2.Zero;

        // 发射相关变量
        private int shootTimer = 0;
        private const int ShootInterval = 12; // 每30帧发射一次
        private const float ShootSpeed = 6f; // 发射速度

        // 关联的克眼NPC索引（从传入参数获取）
        private int linkedEoCIndex = -1;

        // 是否启用弹幕状态检查
        private bool enableProjectileStateCheck = false;

        public override string Texture => "Terraria/Images/NPC_" + 400;//克苏鲁真眼的id

        public override void SetStaticDefaults()
        {
            // 使用原版真眼的帧数
            Main.npcFrameCount[Type] = Main.npcFrameCount[400];

            // 可选：添加一些特性
            NPCID.Sets.NeedsExpertScaling[Type] = false;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;
        }

        public override void SetDefaults()
        {
            // 克隆原版真眼的属性
            NPC.CloneDefaults(400);

            // 自定义设置
            NPC.width = 120;  // 调整大小
            NPC.height = 120;
            NPC.lifeMax = 1000;
            NPC.defense = 10;
            NPC.damage = 0; // 本NPC不直接造成伤害
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.dontTakeDamage = true;
            NPC.timeLeft = 600;

            // AI样式设置为-1，我们将自定义AI
            NPC.aiStyle = -1;

            // 设置初始值
            shootTimer = 0;
        }

        public override void OnSpawn(IEntitySource source)
        {

            if (NPC.ai[0] != null )
            {
                shootDirection = Vector2.UnitX.RotatedBy(NPC.ai[0]);

                // 归一化方向向量
                if (shootDirection != Vector2.Zero)
                {
                    shootDirection.Normalize();
                }
            }

            // 读取关联的克眼索引
            linkedEoCIndex = (int)NPC.ai[2];

            // 随机决定是否启用弹幕状态检查
            enableProjectileStateCheck = Main.rand.NextBool(2);

            // 播放生成音效
            SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

            // 生成粒子效果
            for (int i = 0; i < 20; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    NPC.position,
                    NPC.width,
                    NPC.height,
                    DustID.Vortex,
                    Main.rand.NextFloat(-2f, 2f),
                    Main.rand.NextFloat(-2f, 2f),
                    0, default, 1.5f
                );
                dust.noGravity = true;
            }
        }

        public override void AI()
        {
            // 基础动画和存在时间控制
            NPC.localAI[3]++; // 使用ai[3]作为计时器
            NPC.velocity = Main.npc[linkedEoCIndex].localAI[3] == 1 ? 6 * Vector2.UnitX.RotatedBy(NPC.ai[1]) : Vector2.Zero; //控制移动速度
            if (Main.npc[linkedEoCIndex].localAI[0] != 8 || Main.npc[linkedEoCIndex] == null || (NPC.Distance(Main.npc[linkedEoCIndex].Center) > 1400f && NPC.Distance(Main.npc[linkedEoCIndex].Center) < 1500))
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    NPC.life = 0;
                    NPC.HitEffect();
                    NPC.checkDead();
                    NPC.active = false;
                }
                return;
            }
            // 发射弹幕逻辑
            shootTimer += Main.npc[linkedEoCIndex].localAI[3] == 1 ? 1 : 0;

            if (shootTimer >= ShootInterval && shootDirection != Vector2.Zero && Main.npc[linkedEoCIndex].localAI[3] == 1)
            {
                ShootMoonScythe();
                shootTimer = 0;

                // 可选：每次发射后微调方向
                AdjustShootDirection();
            }

            // 粒子效果
            SpawnParticles();
        }

        private void ShootMoonScythe()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            Vector2 shootPosition = NPC.Center;

            // 计算发射速度
            Vector2 velocity = shootDirection * ShootSpeed;
            int ai2 = enableProjectileStateCheck ? 1 : 0;

            // 生成弹幕
            int proj = Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                shootPosition,
                velocity * (Main.rand.NextBool(4) && Main.getGoodWorld ? Main.rand.NextFloat(0.5f,1.8f) :1),
                ModContent.ProjectileType<MoonScythe2>(),
                FargoSoulsUtil.ScaledProjectileDamage(Main.npc[linkedEoCIndex].defDamage), // 伤害值
                1f, // 击退值
                Main.myPlayer,
                ai0: 0f,    // 让弹幕随机选择贴图
                ai1: linkedEoCIndex, // 关联的克眼索引
                ai2: NPC.ai[3]    // 是否检查克眼状态
            );
            // 可选：播放发射音效
            if (proj >= 0)
            {
                SoundEngine.PlaySound(SoundID.Item71, NPC.Center);

                // 发射时的粒子效果
                for (int i = 0; i < 10; i++)
                {
                    Dust dust = Dust.NewDustDirect(
                        shootPosition - new Vector2(5, 5),
                        10,
                        10,
                        DustID.Vortex,
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f),
                        0, default, 1.2f
                    );
                    dust.noGravity = true;
                    dust.velocity = velocity * 0.5f;
                }
            }
        }

        private void AdjustShootDirection()
        {
            // 轻微随机调整方向，使发射方向不会完全固定
            float adjustAngle = 0;
            shootDirection = shootDirection.RotatedBy(adjustAngle);
        }

        private void SpawnParticles()
        {
            // 持续生成粒子效果
            if (Main.rand.NextBool(3))
            {
                Vector2 particlePos = NPC.Center + Main.rand.NextVector2Circular(NPC.width * 0.5f, NPC.height * 0.5f);

                Dust dust = Dust.NewDustDirect(
                    particlePos,
                    0,
                    0,
                    DustID.Vortex,
                    0f, 0f, 150, default, 0.8f
                );
                dust.noGravity = true;
                dust.velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);
                dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            // 使用原版真眼的帧动画逻辑
            NPC.frameCounter++;

            // 原版真眼有8帧，动画帧速
            float frameSpeed = 6f; // 每帧显示6帧

            if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;

                if (NPC.frame.Y >= Main.npcFrameCount[NPC.type] * frameHeight)
                {
                    NPC.frame.Y = 0;
                }
            }

            // 或者可以简化：使用预定义的帧索引
            // 原版真眼通常使用帧0-7循环
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 获取原版真眼的贴图
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;

            // 计算绘制位置
            Vector2 drawPos = NPC.Center - screenPos;

            // 获取当前帧
            Rectangle frame = NPC.frame;

            // 计算原点（中心）
            Vector2 origin = frame.Size() / 2f;

            // 根据alpha值调整绘制颜色
            Color color = NPC.GetAlpha(drawColor);

            // 绘制NPC
            Main.EntitySpriteDraw(
                texture,
                drawPos,
                frame,
                color,
                NPC.rotation,
                origin,
                NPC.scale,
                SpriteEffects.None,
                0
            );

            // 绘制发光效果（可选）
            if (NPC.alpha < 200)
            {
                Color glowColor = Color.Lerp(Color.Cyan, Color.Purple, (float)Main.timeForVisualEffects * 0.01f % 1f);
                glowColor *= 0.5f * (1f - NPC.alpha / 255f);

                Main.EntitySpriteDraw(
                    texture,
                    drawPos,
                    frame,
                    glowColor,
                    NPC.rotation,
                    origin,
                    NPC.scale * 1.1f,
                    SpriteEffects.None,
                    0
                );
            }

            return false; // 跳过默认绘制
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            // 受到伤害时的效果
            if (NPC.life <= 0)
            {
                // 死亡时的粒子效果
                for (int i = 0; i < 30; i++)
                {
                    Dust dust = Dust.NewDustDirect(
                        NPC.position,
                        NPC.width,
                        NPC.height,
                        DustID.Vortex,
                        Main.rand.NextFloat(-5f, 5f),
                        Main.rand.NextFloat(-5f, 5f),
                        0, default, 1.5f
                    );
                    dust.noGravity = true;
                }

                // 播放音效
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.8f }, NPC.Center);
            }
        }

        public override bool CheckActive()
        {
            // 当透明度达到255时，NPC自动消失
            if (NPC.alpha >= 255)
            {
                return false; // 标记为不活动
            }

            return true; // 保持活动
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            // 这个NPC不会直接碰撞伤害玩家
            return false;
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            // 可以被物品攻击
            return false;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            // 可以被弹幕攻击
            return false;
        }
    }
}