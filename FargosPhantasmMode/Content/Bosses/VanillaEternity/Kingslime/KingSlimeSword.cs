using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Buffs.Masomode;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monochrome.Core.Net;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Kingslime
{
    //ai0传史王whoami
    public class KingSlimeSword : ModProjectile, IPixelatedPrimitiveRenderer
    {
        //state0初始，state1瞄准，state2运动， state3 静止，停止本体绘制，开始展开领域
        ref float State => ref Projectile.localAI[0];
        ref float Timer => ref Projectile.localAI[1];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 25;
            Projectile.height = 25;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
            Projectile.hostile = true;
        }
        public override bool CanHitPlayer(Player target) => State == 2;
        /// <summary>本类的 <c>State</c> / <c>Timer</c> 就是 <c>localAI[0]</c> / <c>[1]</c>，写读由字段表统一。</summary>
        private static readonly MonoNetFields<Projectile> Fields =
            MonoNet.Fields<Projectile>("fpm.kingSlimeSword")
                .Float("state", static p => p.localAI[0], static (p, value) => p.localAI[0] = value)
                .Float("timer", static p => p.localAI[1], static (p, value) => p.localAI[1] = value);

        public override void SendExtraAI(BinaryWriter writer) => Fields.Write(Projectile, writer);

        public override void ReceiveExtraAI(BinaryReader reader) => Fields.Read(Projectile, reader);
        public override void AI()
        {
            NPC slimeboss = FargoSoulsUtil.NPCExists(Projectile.ai[0], NPCID.KingSlime);
            if (slimeboss == null || !slimeboss.active)
            {
                Projectile.Kill();
                return;
            }
            else if (Projectile.timeLeft < 30)
                Projectile.timeLeft = 30;
            if (State != 1)
            {
                Projectile.Center = slimeboss.Center + new Vector2(0, -300);
                Projectile.rotation = 3 * MathHelper.PiOver2;
            }
            /*
            if (++Projectile.frameCounter > 6)
            {
                if (++Projectile.frame >= Main.projFrames[Type])
                    Projectile.frame = 0;
                Projectile.frameCounter = 0;
            }
            */
        }
        /// <summary>
        /// 死亡时撒尘埃。纯表现，各端本地撒（原来包在 HostCheck 里，纯客户端看不到）。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                int randdistance = Main.rand.Next(200, 600);
                float randangle = Main.rand.NextFloat(0, 2 * MathF.PI);
                Vector2 vel = randdistance * Vector2.UnitX.RotatedBy(randangle) / 10;
                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.TintableDust, vel.X, vel.Y, 150, new Color(0, 80, 255, 80));
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = Main.rand.NextFloat(1.2f, 1.5f);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int sizeY = texture.Height / Main.projFrames[Type]; //ypos of lower right corner of sprite to draw
            int sizeX = texture.Width;
            int frameY = Projectile.frame * sizeY;
            int frameX = 0;
            Rectangle rectangle = new(frameX, frameY, sizeX, sizeY);
            Vector2 origin = rectangle.Size() / 2f;
            SpriteEffects spriteEffects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                float modifier = (float)(ProjectileID.Sets.TrailCacheLength[Type] - i) / ProjectileID.Sets.TrailCacheLength[Type];
                Main.EntitySpriteDraw(texture, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Color.White * modifier * modifier, num165, origin, Projectile.scale, spriteEffects, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            return false;
        }
        public float WidthFunction(float completionRatio)
        {
            float baseWidth = Projectile.scale * Projectile.width * 1.3f;
            return MathHelper.SmoothStep(baseWidth, 3.5f, completionRatio);
        }
        public Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(FargowiltasSouls.FargowiltasSouls.EModeColor(), Color.Transparent, completionRatio) * 0.6f;
        }
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, Pixelate: true, Shader: shader), 20);
        }
    }
}
