using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    public class MoonScythe : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public ref float randomize => ref Projectile.ai[0];
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/BloodScytheVanilla1";

        // 添加字段记录是否处于无害模式
        private bool _harmlessMode = false;

        public bool recolor => true && WorldSavingSystem.EternityMode && !Main.bloodMoon;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.DemonSickle);
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            CooldownSlot = 1;

            randomize = 0;
        }

        public override void AI()
        {
            // 检查克眼NPC的状态
            if (Projectile.ai[2] == 1)
            {
                CheckEoCState();
            }

            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
            }

            Projectile.rotation += 0.8f;

            if (++Projectile.localAI[1] > 30 && Projectile.localAI[1] < 90)
                Projectile.velocity *= 1.016f;

            // 根据模式调整粒子效果
            Vector2 offset = new Vector2(0, -20).RotatedBy(Projectile.rotation);
            offset = offset.RotatedByRandom(MathHelper.Pi / 6);

            // 在无害模式下减少粒子生成频率
            if (!_harmlessMode || Main.rand.NextBool(3))
            {
                int d = Dust.NewDust(Projectile.Center, 0, 0, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 150);
                Main.dust[d].position += offset;
                float velrando = Main.rand.Next(20, 31) / 10;
                Main.dust[d].velocity = Projectile.velocity / velrando;
                Main.dust[d].noGravity = true;

                // 在无害模式下降低粒子尺寸
                Main.dust[d].scale = _harmlessMode ? 0.6f : 1.2f;

                if (!Projectile.active)
                {
                    Main.dust[d].scale = 0f;
                }
            }

            if (Projectile.timeLeft < 180)
                Projectile.tileCollide = true;
        }

        private void CheckEoCState()
        {
            // 尝试获取克眼NPC
            // 如果Projectile.ai[1]存储了克眼的索引，则使用它
            int eocIndex = -1;

            // 方法1: 如果Projectile.ai[1]存储了NPC索引
            if (Projectile.ai[1] > 0)
            {
                eocIndex = (int)Projectile.ai[1];
            }
            // 方法2: 遍历查找克眼NPC
            else
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && npc.type == NPCID.EyeofCthulhu)
                    {
                        eocIndex = i;
                        break;
                    }
                }
            }

            // 检查克眼NPC的localAI[3]
            if (eocIndex >= 0 && eocIndex < Main.maxNPCs)
            {
                NPC eoc = Main.npc[eocIndex];
                if (eoc.active)
                {
                    // 根据克眼localAI[3]的值设置无害模式
                    _harmlessMode = eoc.localAI[3] != 0f;

                    // 在无害模式下降低弹幕透明度
                    if (_harmlessMode && Projectile.alpha < 200)
                    {
                        Projectile.alpha = 200;
                    }
                    else if (!_harmlessMode && Projectile.alpha > 0)
                    {
                        Projectile.alpha = 0;
                    }
                }
            }
        }

        public float WidthFunction(float completionRatio)
        {
            float baseWidth = Projectile.scale * Projectile.width * 1.3f;
            return MathHelper.SmoothStep(baseWidth, 3.5f, completionRatio);
        }

        public Color ColorFunction(float completionRatio)
        {
            // 在无害模式下降低拖尾颜色强度
            float alphaMultiplier = _harmlessMode ? 0.3f : 0.7f;
            return Color.Lerp(recolor ? Color.Teal : Color.DarkRed, Color.Transparent, completionRatio) * alphaMultiplier;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            // 在无害模式下不绘制像素化拖尾
            
            if (_harmlessMode)
                return;
            
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, _ => Projectile.Size * 0.5f, Pixelate: true, Shader: shader), 25);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // 在无害模式下降低整体透明度
            Color baseColor = Color.White;
            if (_harmlessMode)
                baseColor *= 0.3f;

            return baseColor;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (randomize == 0)
            {
                randomize += Main.rand.Next(1, 4);
                Projectile.netUpdate = true;
            }

            Texture2D texture = recolor ?
                ModContent.Request<Texture2D>("FargowiltasSouls/Content/Projectiles/Masomode/BloodScythe" + randomize).Value :
                ModContent.Request<Texture2D>("FargowiltasSouls/Content/Projectiles/Masomode/BloodScytheVanilla" + randomize).Value;

            // 在无害模式下不绘制发光环
            if (!_harmlessMode)
            {
                Texture2D glowTexture = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Projectiles/GlowRing").Value;

                Vector2 glowDrawPosition = Projectile.Center + Projectile.velocity / 10f;
                glowDrawPosition += Main.rand.NextVector2Circular(5, 5);

                Main.EntitySpriteDraw(glowTexture, glowDrawPosition - Main.screenPosition, null,
                    recolor ? Color.Teal : Color.DarkRed, Projectile.rotation, glowTexture.Size() * 0.5f,
                    Projectile.scale * 0.8f, SpriteEffects.None, 0);
            }

            // 在无害模式下使用更暗的颜色
            Color drawColor = lightColor;
            if (_harmlessMode)
            {
                drawColor *= 0.3f;
            }

            FargoSoulsUtil.GenericProjectileDraw(Projectile, drawColor, texture: texture);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            // 在无害模式下减少击杀时的粒子数量
            int particleCount = _harmlessMode ? 1 : 4;

            for (int i = 0; i < particleCount; i++)
            {
                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.SnowSpray, 0f, 0f, 150);
                Main.dust[d].velocity = Main.rand.NextVector2Circular(5, 5);
                Main.dust[d].noGravity = true;

                // 在无害模式下降低粒子尺寸
                Main.dust[d].scale = _harmlessMode ? 1f : 2f;
                Main.dust[d].color = Color.White;
            }
            base.OnKill(timeLeft);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // 在无害模式下不施加任何debuff
            if (_harmlessMode)
                return;

            if (WorldSavingSystem.MasochistModeReal)
            {
                target.AddBuff(ModContent.BuffType<ShadowflameBuff>(), 300);
                target.AddBuff(BuffID.Bleeding, 600);
                target.AddBuff(BuffID.Obstructed, 15);
            }

            target.AddBuff(ModContent.BuffType<BerserkedBuff>(), 120);
            target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 120);
        }

        // 添加方法覆盖CanDamage，在无害模式下返回false
        public override bool? CanDamage()
        {
            return _harmlessMode ? false : base.CanDamage();
        }
    }
}