using Fargowiltas.Common.Configs;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles.Masomode
{
    public class BetsysHeartPhoenix : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_706";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            Main.projFrames[Projectile.type] = Main.projFrames[ProjectileID.DD2PhoenixBowShot];
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.alpha = 100;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.penetrate = 2;
            CooldownSlot = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.CritChance = (int)FargoSoulsUtil.HighestCritChance(Main.player[Projectile.owner]);
            
            Player player = Main.player[Projectile.owner];
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            Projectile.netUpdate = true;
            Projectile.localAI[2]++;
            if (player.whoAmI == Main.myPlayer && (player.dead || !player.HasEffect<BetsyDashEffect>()))
            {
                Projectile.Kill();
                return;
            }

            int type = FargoSoulsUtil.FindClosestHostileNPC(Projectile.Center, 1800, true);
            float distance = (Projectile.Center - player.Center).Length();
            if (type > 0 && Projectile.localAI[2] >= 10)//有敌人时
            {
                if (Main.npc[(int)type].active)
                {
                    NPC npc = Main.npc[(int)type];
                    Vector2 vectorToIdlePosition = npc.Center - Projectile.Center;
                    Projectile.velocity = 0.98f * Projectile.velocity + 0.02f * vectorToIdlePosition;
                    FusedLensMechElectricOrb.MechElectricMovement(Projectile, npc.Center, 0, 20);
                }
            }
            else//常态挂机减速
            {
                Projectile.velocity *= 0.995f;
            }

            #region 特效
            int dustID = DustID.Torch;
            if (Projectile.alpha <= 0) //vanilla display code
            {
                /*for (int i = 0; i < 2; ++i)
                {
                    if (!Main.rand.NextBool(4))
                    {
                        Dust dust = Dust.NewDustDirect(Projectile.Center - Projectile.Size / 4f, Projectile.width / 2, Projectile.height / 2,
                            Utils.SelectRandom(Main.rand, new int[3] { 6, 31, 31 }), 0.0f, 0.0f, 0, default, 1f);
                        dust.noGravity = true;
                        dust.velocity *= 2.3f;
                        dust.fadeIn = 1.5f;
                        dust.noLight = true;
                    }
                }*/
                Vector2 vector2_1 = 16f * new Vector2(0f, (float)Math.Cos(Projectile.frameCounter * 6.28318548202515 / 40.0 - (float)Math.PI / 2)).RotatedBy(Projectile.rotation);
                Vector2 vector2_2 = Vector2.Normalize(Projectile.velocity);

                Dust dust1 = Dust.NewDustDirect(Projectile.Center - Projectile.Size / 4f, Projectile.width / 2, Projectile.height / 2, dustID, 0.0f, 0.0f, 0, default, 1f);
                dust1.noGravity = true;
                dust1.position = Projectile.Center + vector2_1;
                dust1.velocity = Vector2.Zero;
                dust1.fadeIn = 1.4f;
                dust1.scale = 1.15f;
                dust1.noLight = true;
                dust1.position += Projectile.velocity * 1.2f;
                dust1.velocity += vector2_2 * 2f;

                Dust dust2 = Dust.NewDustDirect(Projectile.Center - Projectile.Size / 4f, Projectile.width / 2, Projectile.height / 2, dustID, 0.0f, 0.0f, 0, default, 1f);
                dust2.noGravity = true;
                dust2.position = Projectile.Center + vector2_1;
                dust2.velocity = Vector2.Zero;
                dust2.fadeIn = 1.4f;
                dust2.scale = 1.15f;
                dust2.noLight = true;
                dust2.position += Projectile.velocity * 0.5f;
                dust2.position += Projectile.velocity * 1.2f;
                dust2.velocity += vector2_2 * 2f;
            }
            int num9 = Projectile.frameCounter + 1;
            Projectile.frameCounter = num9;
            if (num9 >= 40)
                Projectile.frameCounter = 0;
            Projectile.frame = Projectile.frameCounter / 5;
            if (Projectile.alpha > 0)
            {
                Projectile.alpha = Projectile.alpha - 55;
                if (Projectile.alpha < 0)
                {
                    Projectile.alpha = 0;
                    /*float num1 = 16f;
                    for (int index1 = 0; index1 < num1; ++index1)
                    {
                        Vector2 vector2 = -Vector2.UnitY.RotatedBy(index1 * (6.28318548202515 / num1)) * new Vector2(1f, 4f);
                        vector2 = vector2.RotatedBy(Projectile.velocity.ToRotation());
                        int index2 = Dust.NewDust(Projectile.Center, 0, 0, dustID, 0.0f, 0.0f, 0, default, 1f);
                        Main.dust[index2].scale = 1.5f;
                        Main.dust[index2].noLight = true;
                        Main.dust[index2].noGravity = true;
                        Main.dust[index2].position = Projectile.Center + vector2;
                        Main.dust[index2].velocity = Main.dust[index2].velocity * 4f + Projectile.velocity * 0.3f;
                    }*/
                }
            }
            DelegateMethods.v3_1 = new Vector3(1f, 0.6f, 0.2f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * 4f, 40f, DelegateMethods.CastLightOpen);

            Projectile.direction = Projectile.spriteDirection = Projectile.velocity.X < 0 ? -1 : 1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.direction < 0)
                Projectile.rotation += (float)Math.PI;
            #endregion
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White * Projectile.Opacity * ModContent.GetInstance<FargoClientConfig>().TransparentFriendlyProjectiles;
        }
        public override void OnKill(int timeLeft)
        {
            const int num226 = 12;
            for (int i = 0; i < num226; i++)
            {
                Vector2 vector6 = Vector2.UnitX.RotatedBy(Projectile.rotation) * 6f;
                vector6 = vector6.RotatedBy((i - (num226 / 2 - 1)) * 6.28318548f / num226, default) + Projectile.Center;
                Vector2 vector7 = vector6 - Projectile.Center;
                int num228 = Dust.NewDust(vector6 + vector7, 0, 0, DustID.FlameBurst, 0f, 0f, 0, default, 1.5f);
                Main.dust[num228].noGravity = true;
                Main.dust[num228].velocity = vector7;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = Color.White * Projectile.Opacity * 0.75f * 0.5f * ModContent.GetInstance<FargoClientConfig>().TransparentFriendlyProjectiles;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, Projectile.scale, effects, 0);
            }

            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, effects, 0);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.BetsysCurse, 300);
            target.AddBuff(BuffID.OnFire3, 300);
            base.OnHitNPC(target, hit, damageDone);
        }
    }
}