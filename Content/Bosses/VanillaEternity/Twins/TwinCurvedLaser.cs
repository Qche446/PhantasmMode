using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// 双子眼弯曲假激光（弹幕静止，曲线由虚拟尖端的历史位置构成）
    /// ai0 : 所属 NPC 的 whoAmI
    /// ai1 : maxTime
    /// ai2 : 颜色索引（保留）
    /// </summary>
    public class TwinCurvedLaser : BaseDeathray, IPixelatedPrimitiveRenderer
    {
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public TwinCurvedLaser() : base(120) { }
        public Vector2[] CollisionPoint = new Vector2[40];
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Projectile.hide = true;
            Projectile.tileCollide = false;
            Projectile.width = 8;
            Projectile.height = 8;
            base.SetDefaults();
        }
        public override void AI()
        {
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
                Projectile.velocity = -Vector2.UnitY;

            NPC retinazer = FargoSoulsUtil.NPCExists(Projectile.ai[0], NPCID.Retinazer);
            if (retinazer != null)
            {
                // 从 npc 的中心加上基于其旋转的偏移量，得到射线的起始点
                Projectile.Center = P_Retinazer.ShootPos(retinazer);
            }
            else
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
                Projectile.velocity = -Vector2.UnitY;

            FargoSoulsUtil.ScreenshakeRumble(2);
            Projectile.scale *= 0.9f;
            if (Projectile.localAI[0] == 0)
                Projectile.localAI[1] = Projectile.velocity.ToRotation();
            CollisionPoint = PhanUtil.GetArcPoints(Projectile.Center, 40 * (Projectile.localAI[1].ToRotationVector2()), Main.player[retinazer.target].Center, 2f, 40);
            // 更新存在时间，到达限制则消散
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] >= Projectile.ai[1] || Projectile.scale < 0.1f)
            {
                Projectile.Kill();
                return;
            }
            // 让射线方向垂直于激光眼的正面（rotation）
            // 射线旋转 = 激光眼的旋转角度（即与面垂直的方向）
            float parentRotation = retinazer.rotation;
            //Projectile.rotation = parentRotation;
            parentRotation += MathHelper.PiOver2; // velocity 为垂直于正面的方向（射线发射方向）
            //Projectile.velocity = parentRotation.ToRotationVector2();
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < CollisionPoint.Length - 1; i++) 
            {
                int width = (int)(Projectile.width * Projectile.scale);
                Vector2 length = CollisionPoint[i + 1] - CollisionPoint[i];
                float rotation = length.ToRotation();
                Rectangle rect = new Rectangle((int)CollisionPoint[i].X, (int)CollisionPoint[i].Y, width, (int)length.Length());
                if (CollisionDetector.Intersects(new RotatedRectangle(rect, rotation), targetHitbox))
                    return true;
            }
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Burning, 150);
            target.AddBuff(BuffID.OnFire, 300);
            target.AddBuff(BuffID.Ichor, 300);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Projectile.hide)
                behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public float WidthFunction(float ratio) => Projectile.width * Projectile.scale;
        public static Color ColorFunction(float ratio)
        {
            Color color = Color.Red;
            color.A = 0;
            return color * (1f );
        }
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            if (Projectile.hide)
                return;

            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.RetinazerDeathray");

            // 设置着色器参数
            shader.TrySetParameter("mainColor", new Color(240, 220, 240, 0));
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedThinGlowStreak.Value);
            shader.TrySetParameter("stretchAmount", 0.5);
            shader.TrySetParameter("scrollSpeed", 4f);
            shader.TrySetParameter("uColorFadeScaler", 0.8f);
            shader.TrySetParameter("useFadeIn", true);

            // 以像素化方式渲染轨迹
            PrimitiveRenderer.RenderTrail(CollisionPoint, new(WidthFunction, ColorFunction, Pixelate: true, Shader: shader));
        }
    }
}
