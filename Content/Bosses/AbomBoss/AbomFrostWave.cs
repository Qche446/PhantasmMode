using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss
{
    public class AbomFrostWave : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_348";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Frost Wave");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8; // 设置拖尾缓存长度
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2; // 设置拖尾模式
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 1;
            AIType = ProjectileID.FrostWave;
            Projectile.hostile = true;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 240;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            

            // 添加寒霜粒子效果
            if (Main.rand.NextBool(6))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Frost, 0f, 0f, 100, default, 1.5f);
                dust.noGravity = true;
                dust.velocity *= 0.5f;
                dust.velocity += Projectile.velocity * 0.2f;
            }

            // 添加冰晶粒子效果
            if (Main.rand.NextBool(10))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Ice, 0f, 0f, 100, new Color(150, 200, 255), 1f);
                dust.noGravity = true;
                dust.velocity = Projectile.velocity * 0.3f + Main.rand.NextVector2Circular(1f, 1f);
            }

        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Bleeding, 240);
            if (WorldSavingSystem.EternityMode)
                target.AddBuff(ModContent.BuffType<AbomFangBuff>(), 240);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(150, 200, 255, Projectile.alpha); // 调整为寒霜蓝色调
        }

        // 添加拖尾绘制
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
            int y3 = num156 * Projectile.frame;
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            // 绘制拖尾
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float fade = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color drawColor = new Color(100, 150, 255, 100) * fade * 0.5f;
                float scale = Projectile.scale * (0.5f + 0.5f * fade);
                Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + origin2 + new Vector2(0f, Projectile.gfxOffY);
                Main.EntitySpriteDraw(texture2D13, drawPos, rectangle, drawColor, Projectile.oldRot[i], origin2, scale, SpriteEffects.None, 0);
            }

            // 绘制主体
            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                new Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
        /*
        // 添加发光效果
        public override void PostDraw(Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
            int y3 = num156 * Projectile.frame;
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            // 使用叠加混合模式绘制发光效果
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            // 发光层
            Color glowColor = new Color(100, 150, 255, 100) * 0.7f;
            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                new Rectangle?(rectangle), glowColor, Projectile.rotation, origin2, Projectile.scale * 1.2f, SpriteEffects.None, 0);

            // 恢复默认混合模式
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
        */
    }
}