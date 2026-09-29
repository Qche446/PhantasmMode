using FargowiltasSouls.Content.Projectiles.Minions;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Trojan
{
    public class SqBomb : KamikazeSquirrel
    {
        private const int DefaultHopTime = 180;
        private const int FuseTime = 60;
        private const int HopInterval = 36;
        private const float Gravity = 0.35f;
        private const float HopSpeed = 15f;
        private const float HopRadius = 220f;
        private const int ExplosionSize = 124;
        private bool detonated;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
        }
        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = Projectile.height = 30;
            Projectile.timeLeft = 2;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        // ai[0] = hopping duration; ai[1] = elapsed phase time; ai[2] = target player index.
        public override bool? CanDamage() => detonated;

        public override void AI()
        {
            Projectile.timeLeft = 2;
            Player target = GetTarget();
            if (target == null)
            {
                Projectile.Kill();
                return;
            }

            float hopTime = Projectile.ai[0] > 0f ? Projectile.ai[0] : DefaultHopTime;
            Projectile.ai[1]++;
            if (Projectile.ai[1] <= hopTime)
                HopAround(target);
            else
            {
                Projectile.velocity *= 0.86f;
                if (Projectile.velocity.LengthSquared() < 0.04f)
                    Projectile.velocity = Vector2.Zero;
                Projectile.localAI[2]++;
                if (Projectile.localAI[2] >= FuseTime)
                {
                    Projectile.Kill();
                    return;
                }
            }

            Projectile.rotation += Projectile.velocity.X * 0.04f;
            UpdateFrame();
        }

        private Player GetTarget()
        {
            int targetIndex = (int)Projectile.ai[2];
            if (targetIndex >= 0 && targetIndex < Main.maxPlayers)
            {
                Player target = Main.player[targetIndex];
                if (target.active && !target.dead)
                    return target;
            }

            int closestIndex = Player.FindClosest(Projectile.Center, 0, 0);
            if (closestIndex >= 0 && closestIndex < Main.maxPlayers)
            {
                Player closest = Main.player[closestIndex];
                if (closest.active && !closest.dead)
                {
                    Projectile.ai[2] = closest.whoAmI;
                    Projectile.netUpdate = true;
                    return closest;
                }
            }
            return null;
        }

        private void HopAround(Player target)
        {
            Projectile.localAI[0] += 0.075f;
            Vector2 desired = target.Center + Projectile.localAI[0].ToRotationVector2() * HopRadius;
            if (Projectile.velocity.Y == 0f || Projectile.localAI[1] <= 0f)
            {
                Vector2 horizontal = desired - Projectile.Center;
                horizontal.Y = 0f;
                if (horizontal.LengthSquared() > 1f)
                    horizontal.Normalize();
                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, horizontal.X * 18f, 0.8f);
                Projectile.velocity.Y = HopSpeed * (target.Center.Y - Projectile.Center.Y) / 120f;
                Projectile.localAI[1] = HopInterval;
                Projectile.netUpdate = true;
            }
            else
            {
                Projectile.localAI[1]--;
                Projectile.velocity.Y += Gravity;
                Projectile.velocity.X *= 0.99f;
            }
            Projectile.direction = Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
        }

        private void UpdateFrame()
        {
            if (Projectile.velocity.X == 0f)
            {
                Projectile.frame = 0;
                return;
            }
            Projectile.frameCounter += (int)Math.Abs(Projectile.velocity.X);
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (oldVelocity.Y > 0f)
                Projectile.velocity.Y = -HopSpeed;
            else
                Projectile.velocity.Y = oldVelocity.Y;
            Projectile.velocity.X = oldVelocity.X * 0.8f;
            Projectile.netUpdate = true;
            return false;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            int targetIndex = (int)Projectile.ai[2];
            if (targetIndex >= 0 && targetIndex < Main.maxPlayers)
            {
                Player target = Main.player[targetIndex];
                fallThrough = target.active && !target.dead && target.Center.Y > Projectile.Bottom.Y;
            }

            return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
        }

        public override void OnKill(int timeLeft)
        {
            if (detonated)
                return;
            detonated = true;
            Projectile.tileCollide = false;
            Vector2 explosionCenter = Projectile.Center;
            Projectile.Resize(ExplosionSize, ExplosionSize);
            Projectile.Center = explosionCenter;

            for (int i = 0; i < 24; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.6f);
                Main.dust[dust].velocity *= 1.4f;
            }
            for (int i = 0; i < 12; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 2.4f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 4f;
            }
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);
            if (Projectile.owner == Main.myPlayer)
                Projectile.Damage();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);

            float hopTime = Projectile.ai[0] > 0f ? Projectile.ai[0] : DefaultHopTime;
            if (Projectile.ai[1] > hopTime)
            {
                Texture2D glow = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/MutantBoss/MutantSpearAimGlow", AssetRequestMode.ImmediateLoad).Value;
                float progress = MathHelper.Clamp(Projectile.localAI[2] / FuseTime, 0f, 1f);
                float radiusScale = 100f * progress / glow.Width;
                Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, glow.Bounds, Color.OrangeRed * (0.25f + 0.5f * progress), 0f, glow.Bounds.Size() * 0.5f, radiusScale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
