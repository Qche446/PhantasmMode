using FargowiltasSouls;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// ai[0]未知,ai[1]决定初始偏转方像(1, -1)，ai[2]决定颜色
    /// </summary>
    public class MechElectricOrbPolyline : MechElectricOrb
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/MechElectricOrb";
        Vector2 direct = Vector2.Zero;
        float timer = 0;
        int turntimer = 0;
        Vector2 oldvel = Vector2.Zero;
        
        public override void AI()
        {
            if (timer == 0)
            {
                oldvel = Projectile.velocity;
            }
            if (++timer >= 40 && turntimer < 6)
            {
                Projectile.velocity *= 0.96f;
            }
            if (timer % 40 == 0 && timer >= 40) 
            {
                float detalangle = Projectile.ai[1] * (turntimer == 0 ? MathHelper.PiOver4 : MathHelper.PiOver2);
                Projectile.ai[1] = Projectile.ai[1] == -1 ? 1 : -1;
                Projectile.velocity = 2.5f * oldvel.Length() * Vector2.Normalize(Projectile.velocity).RotatedBy(detalangle);
                turntimer++;
            }
            NPC npc = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            if (npc != null)
            {
                npc.TryGetGlobalNPC<P_Retinazer>(out P_Retinazer re);
                if (Projectile.Distance(npc.Center) > re.AuraRadius && Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
            }
             
            
            base.AI();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int sizeY = texture.Height / Main.projFrames[Type]; 
            int sizeX = texture.Width / 4; // 纹理被分为4列（对应4种颜色类型）

            // 根据当前帧和颜色类型计算要绘制的纹理区域
            int frameY = Projectile.frame * sizeY; 
            int frameX = (int)ColorType * sizeX; 

            // 定义要绘制的矩形区域（纹理的一部分）
            Rectangle rectangle = new(frameX, frameY, sizeX, sizeY);
            Vector2 origin = rectangle.Size() / 2f; // 绘制原点（中心点）

            // 确定精灵效果（根据弹幕方向决定是否水平翻转）
            SpriteEffects spriteEffects = Projectile.spriteDirection > 0 ?
                SpriteEffects.None : SpriteEffects.FlipHorizontally;
            // 2. 预警时间内的位置调整（35帧的蓄力效果）
            float telegraphTime = 35; // 预警总时长（35帧，约0.58秒）

            // Projectile.localAI[2] 用作预警计时器
            if (++Projectile.localAI[2] < telegraphTime)
            {
                //Projectile.position -= Projectile.velocity * (1 - (Projectile.localAI[2] / telegraphTime));
            }
            Color color = ColorType switch
            {
                Blue => Color.Teal,     // 蓝色 -> 青色
                Green => Color.Green,   // 绿色 -> 绿色
                Yellow => Color.Yellow, // 黄色 -> 黄色
                _ => Color.Red          // 默认（红色）-> 红色
            };
            // 4. 绘制轨迹拖尾效果
            for (float i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i += 0.33f)
            {
                Color oldColor = color;
                oldColor.A = 50; // 设置透明度为50（半透明）

                // 计算轨迹衰减因子：越旧的轨迹越透明
                float modifier = (float)(ProjectileID.Sets.TrailCacheLength[Type] - i) /
                                 ProjectileID.Sets.TrailCacheLength[Type];
                oldColor *= modifier; // 应用衰减

                // 计算轨迹大小：基础大小的一半 + 根据衰减调整的另一半
                float scale = (Projectile.scale / 2) + (Projectile.scale * modifier / 2);

                // 获取前一个轨迹点的索引（用于插值）
                int max0 = (int)i - 1;
                if (max0 < 0) // 跳过第一个点（没有前一个点）
                    continue;

                // 在两个轨迹点之间进行线性插值，使轨迹更平滑
                // i % 1 获取小数部分，用于在两个整数索引间插值
                Vector2 oldPos = Vector2.Lerp(Projectile.oldPos[(int)i],
                    Projectile.oldPos[max0], 1 - i % 1) + (origin / 2);

                // 使用前一个点的旋转角度
                float oldRot = Projectile.oldRot[max0];

                // 绘制轨迹点
                Main.EntitySpriteDraw(texture, oldPos - Main.screenPosition +
                    new Vector2(0f, Projectile.gfxOffY), rectangle, oldColor,
                    oldRot, origin, scale, spriteEffects, 0);
            }
            // 5. 绘制预警线（核心功能）
            // 使用原版Extra[178]纹理，这是一个细长的直线纹理
            Asset<Texture2D> line = TextureAssets.Extra[178];
            float opacity = 0.55f; // 预警线透明度

            // 绘制预警线的参数详解：
            Main.EntitySpriteDraw(
                line.Value, // 纹理：细长的直线

                // 位置：弹幕当前位置（已减去屏幕位置和Y轴偏移）
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                null,
                color * opacity,
                // 通过速度向量的角度确定旋转，使预警线指向弹幕飞行方向
                Projectile.velocity.ToRotation() + Projectile.ai[1] * (turntimer == 0 ? MathHelper.PiOver4 : MathHelper.PiOver2),

                new Vector2(0, line.Height() * 0.5f),
                new Vector2(0.33f, Projectile.scale * 5),

                SpriteEffects.None 
            );
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                rectangle, Color.White, // 使用纯白色（无着色）
                Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            return false;
        }
    }
    public class DarkStarPolyline : DarkStar
    {
        float timer = 0;
        int turntimer = 0;
        Vector2 oldvel = Vector2.Zero;
        public override void AI()
        {
            if (timer == 0)
            {
                oldvel = Projectile.velocity;
            }
            if (++timer >= 40 && turntimer < 6)
            {
                Projectile.velocity *= 0.96f;
            }
            if (timer % 40 == 0 && timer >= 40)
            {
                float detalangle = Projectile.ai[1] * (turntimer == 0 ? MathHelper.PiOver4 : MathHelper.PiOver2);
                Projectile.ai[1] = Projectile.ai[1] == -1 ? 1 : -1;
                Projectile.velocity = 2.5f * oldvel.Length() * Vector2.Normalize(Projectile.velocity).RotatedBy(detalangle);
                turntimer++;
            }
            NPC npc = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            if (npc != null)
            {
                npc.TryGetGlobalNPC<P_Retinazer>(out P_Retinazer re);
                if (Projectile.Distance(npc.Center) > re.AuraRadius && Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
            }
            base.AI();
        }
    }
}
