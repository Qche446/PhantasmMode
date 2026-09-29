using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// 双子眼弯曲假激光（弹幕静止，曲线由虚拟尖端的历史位置构成）
    /// ai0 : 所属 NPC 的 whoAmI
    /// ai1 : 角度（角度制）
    /// ai2 : 方向
    /// </summary>
    public class TwinCurvedLaser : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public List<Vector2> trailPos = [];
        public bool active = false;
        protected readonly int grazeCD = 5;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.timeLeft = 360;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 100;
            Projectile.FargoSouls().GrazeCheck =
                Projectile =>
                {
                    if (active)
                    {
                        for (int i = 0; i < trailPos.Count; i++)
                        {
                            float pr = 0.8f * (Projectile.width * Projectile.scale);
                            float gr = Main.LocalPlayer.FargoSouls().GrazeRadius;
                            if (Main.LocalPlayer.Center.Distance(trailPos[i]) <= pr + gr)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                };
        }
        public override bool ShouldUpdatePosition() => false;
        public override void AI()
        {
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
                Projectile.velocity = -Vector2.UnitY;

            NPC retinazer = FargoSoulsUtil.NPCExists(Projectile.ai[0], NPCID.Retinazer);
            if (retinazer != null)
            {
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

            if (Projectile.localAI[0] < 120)
            {
                //Projectile.scale = 2;
                float omiga = Projectile.ai[2] * MathHelper.Lerp(MathF.PI / 240f, 0, Math.Clamp((Projectile.localAI[0]) / 60f, 0, 1));
                Projectile.velocity = Projectile.velocity.RotatedBy(omiga);
                Projectile.scale = MathHelper.Lerp(0, 2, Projectile.localAI[0] / 120f);
                active = false;
                trailPos = [];
                Laser(Projectile.ai[1], false, false);
            }
            else if(Projectile.localAI[0] < 300)
            {
                active = true;
                float omiga = Projectile.ai[2] * MathHelper.Lerp(0, MathF.PI / 240f, Math.Clamp((Projectile.localAI[0] - 120f) / 60f, 0, 1));
                trailPos = [];
                Laser(Projectile.ai[1], false, true);
                Projectile.velocity = Projectile.velocity.RotatedBy(omiga);
            }
            else
            {
                float omiga = Projectile.ai[2] * MathHelper.Lerp(0, MathF.PI / 240f, (-Projectile.localAI[0] + 360f) / 60f);
                Projectile.scale = MathHelper.Lerp(0, 2, (-Projectile.localAI[0] + 360f) / 60f);
                trailPos = [];
                Laser(Projectile.ai[1], false, true);
                Projectile.velocity = Projectile.velocity.RotatedBy(omiga);
            }
            if (Projectile.FargoSouls().GrazeCD > grazeCD)
                Projectile.FargoSouls().GrazeCD = grazeCD;
            //Laser(MathHelper.Pi / 3f, true, true);
            // 更新存在时间，到达限制则消散
            Projectile.localAI[0] += 1f;

        }
        public void Laser(float targetRotation, bool dust = false, bool someWhatDust = false)
        {
            for (int i = 0; i <= 100; i++)
            {
                //float mult = 0f;
                float lerpMult = (float)i / 100f;
                //float sin = (float)Math.Sin(MathHelper.ToRadians(Projectile.ai[0] * 10f + (float)(i * 3)));
                Vector2 unitTrue = new Vector2(32f, 0f).RotatedBy(Projectile.velocity.ToRotation() + MathHelper.ToRadians(targetRotation * lerpMult));
                if (i <= 0)
                {
                    Vector2 position = Projectile.Center + unitTrue * i;
                    trailPos.Add(position);
                    if ((dust && i % 3 == 0) || (someWhatDust && Main.rand.NextBool(360)))
                    {
                        Dust dust2 = Dust.NewDustPerfect(position, DustID.GemRuby);
                        dust2.noGravity = true;
                        dust2.velocity *= 0.3f;
                        dust2.scale *= 2.4f;
                        dust2.fadeIn = 0.1f;
                        dust2.alpha = 100;
                        dust2.color = ((!Main.rand.NextBool(4)) ? new Color(255, 233, 2, 50) : new Color(220, 95, 210, 50));
                        dust2.velocity += Main.rand.NextVector2Circular(3f, 3f);
                    }
                }
                else
                {
                    Vector2 position = trailPos[i - 1] + unitTrue;
                    trailPos.Add(position);
                    if ((dust && i % 3 == 0) || (someWhatDust && Main.rand.NextBool(360)))
                    {
                        Dust dust2 = Dust.NewDustPerfect(position, DustID.GemRuby);
                        dust2.noGravity = true;
                        dust2.velocity *= 0.3f;
                        dust2.scale *= 2.4f;
                        dust2.fadeIn = 0.1f;
                        dust2.alpha = 100;
                        dust2.color = ((!Main.rand.NextBool(4)) ? new Color(255, 233, 2, 50) : new Color(220, 95, 210, 50));
                        dust2.velocity += Main.rand.NextVector2Circular(3f, 3f);
                    }
                }
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!active)
                return false;
            //int width = (int)(Projectile.width * Projectile.scale);
            //Rectangle rect;
            for (int i = 0; i < trailPos.Count; i++)
            {
                float width = 0.8f * (Projectile.width * Projectile.scale);
                //int length = 14;
                //rect = new Rectangle((int)trailPos[i].X, (int)trailPos[i].Y, (int)trailPos[i + 1].X - (int)trailPos[i].X, (int)trailPos[i + 1].Y - (int)trailPos[i].Y);
                if (Utilities.CircularHitboxCollision(trailPos[i], width, targetHitbox))
                {
                    return true;
                }
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

        public override bool PreDraw(ref Color lightColor)
        {
            /*
            SpriteBatch spriteBatch = Main.spriteBatch;
            for (int i = 0; i < trailPos.Count - 1; i++)
            {
                spriteBatch.DrawBloomLine(trailPos[i], trailPos[i + 1], Color.Green, 32);
            }
            */
            return false;
        }
        public float WidthFunction(float ratio) => Projectile.width * Projectile.scale * (!active ? 0.1f : 1);
        public Color ColorFunction(float ratio)
        {
            Color color = Color.Red;
            color.A = 0;
            return color * (!active ? 0.5f : 1) * Projectile.scale;
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
            PrimitiveRenderer.RenderTrail(trailPos, new(WidthFunction, ColorFunction, Pixelate: true, Shader: shader));
        }
    }
}
