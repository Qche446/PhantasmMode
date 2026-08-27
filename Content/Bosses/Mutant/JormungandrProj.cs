using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    // ai[0] is the target player and ai[1] is the lifetime, in frames.
    // ai[2] is the movement phase and is never used to select a texture.
    public class JormungandrHead : MutantDestroyerHead
    {
        private const int DefaultLifetime = 600;
        private const int TrackFrames = 105;
        private const int ChargeFrames = 45;
        private const int DashFrames = 30;
        private const float TrackSpeed = 13f;
        private const float FarTrackSpeed = 26f;
        private const float DashSpeed = 30f;

        public override string Texture => FargoSoulsUtil.AprilFools
            ? "FargowiltasSouls/Content/Bosses/MutantBoss/MutantDestroyerHead_April"
            : "FargowiltasSouls/Assets/ExtraTextures/Resprites/NPC_134";

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.scale = 1.8f;
            Projectile.timeLeft = 6000;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            base.OnSpawn(source);
            int lifetime = (int)Projectile.ai[1];
            if (lifetime <= 0)
                lifetime = DefaultLifetime;
            lifetime = Utils.Clamp(lifetime, 1, 6000);
            Projectile.ai[1] = lifetime;
            Projectile.timeLeft = lifetime;
            Projectile.ai[2] = 0f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;

            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
                return;

            Player target = Main.player[targetIndex];
            if (!target.active || target.dead)
                return;

            int phase = (int)++Projectile.ai[2] % (TrackFrames + ChargeFrames + DashFrames);
            float distance = Projectile.Distance(target.Center);
            float distanceFactor = MathHelper.Clamp((distance - 400f) / 1400f, 0f, 1f);

            if (phase < TrackFrames)
            {
                float speed = MathHelper.Lerp(TrackSpeed, FarTrackSpeed, distanceFactor);
                float turnStrength = MathHelper.Lerp(0.14f, 0.30f, distanceFactor);
                Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center + target.velocity * 12f) * speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, turnStrength);
            }
            else if (phase < TrackFrames + ChargeFrames)
            {
                int chargeTimer = phase - TrackFrames;
                Projectile.velocity *= 0.92f;
                SpawnChargeDust(chargeTimer);
            }
            else
            {
                float dashTurnStrength = MathHelper.Lerp(0.03f, 0.12f, distanceFactor);
                Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center + target.velocity * 10f) * DashSpeed;
                if (phase == TrackFrames + ChargeFrames)
                {
                    Projectile.velocity = desiredVelocity;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, dashTurnStrength);
                }
            }

            if (phase == 0)
                Projectile.netUpdate = true;
        }

        private void SpawnChargeDust(int chargeTimer)
        {
            if (Main.dedServ || chargeTimer % 3 != 0)
                return;

            int dustCount = chargeTimer >= ChargeFrames / 2 ? 12 : 6;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(Projectile.width * 0.4f, Projectile.height * 0.4f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GemSapphire,
                    offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 3f) - Projectile.velocity * 0.05f,
                    100, default, Main.rand.NextFloat(1.1f, 1.7f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            JormungandrDraw.Draw(Projectile, ref lightColor);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch,
                    -Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f, 100, default, 2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 2f;
                dust = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.RedTorch,
                    -Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f, 100);
                Main.dust[dust].velocity *= 2f;
            }
            SoundEngine.PlaySound(SoundID.NPCDeath14, Projectile.Center);
        }
    }

    public class JormungandrBody : MutantDestroyerBody
    {
        public override string Texture => base.Texture;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.scale = 1.3f;
            Projectile.timeLeft = 6000;
        }

        public override void AI()
        {
            if ((int)Main.time % 120 == 0)
                Projectile.netUpdate = true;

            int parentIndex = FargoSoulsUtil.GetProjectileByIdentity(Projectile.owner, (int)Projectile.ai[0],
                Projectile.type, ModContent.ProjectileType<JormungandrHead>(), ModContent.ProjectileType<JormungandrBody>());
            if (parentIndex < 0 || !Main.projectile[parentIndex].active)
                return;

            Projectile parent = Main.projectile[parentIndex];
            parent.localAI[0] = Projectile.localAI[0] + 1f;
            if (parent.type == ModContent.ProjectileType<JormungandrHead>())
                parent.localAI[1] = Projectile.identity;
            FollowParent(parent);
        }

        private void FollowParent(Projectile parent)
        {
            Projectile.timeLeft = parent.timeLeft;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 42);
            Projectile.velocity = Vector2.Zero;
            Vector2 offset = parent.Center - Projectile.Center;
            if (parent.rotation != Projectile.rotation)
                offset = offset.RotatedBy(MathHelper.WrapAngle(parent.rotation - Projectile.rotation) * 0.1f);
            Projectile.rotation = offset.ToRotation() + MathHelper.PiOver2;
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = (int)(30f * Projectile.scale);
            Projectile.Center = Projectile.position;
            if (offset != Vector2.Zero)
                Projectile.Center = parent.Center - Vector2.Normalize(offset) * JormungandrConstants.SegmentSpacing;
            Projectile.spriteDirection = offset.X > 0f ? 1 : -1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            JormungandrDraw.Draw(Projectile, ref lightColor);
            return false;
        }
    }

    public class JormungandrTail : MutantDestroyerTail
    {
        public override string Texture => base.Texture;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.scale = 1.3f;
        }

        public override void AI()
        {
            if ((int)Main.time % 120 == 0)
                Projectile.netUpdate = true;

            int parentIndex = FargoSoulsUtil.GetProjectileByIdentity(Projectile.owner, (int)Projectile.ai[0],
                ModContent.ProjectileType<JormungandrBody>());
            if (parentIndex < 0 || !Main.projectile[parentIndex].active)
                return;

            Projectile parent = Main.projectile[parentIndex];
            parent.localAI[0] = Projectile.localAI[0] + 1f;
            Projectile.timeLeft = parent.timeLeft;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 42);
            Projectile.velocity = Vector2.Zero;
            Vector2 offset = parent.Center - Projectile.Center;
            if (parent.rotation != Projectile.rotation)
                offset = offset.RotatedBy(MathHelper.WrapAngle(parent.rotation - Projectile.rotation) * 0.1f);
            Projectile.rotation = offset.ToRotation() + MathHelper.PiOver2;
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = (int)(30f * Projectile.scale);
            Projectile.Center = Projectile.position;
            if (offset != Vector2.Zero)
                Projectile.Center = parent.Center - Vector2.Normalize(offset) * JormungandrConstants.SegmentSpacing;
            Projectile.spriteDirection = offset.X > 0f ? 1 : -1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            JormungandrDraw.Draw(Projectile, ref lightColor);
            return false;
        }
    }

    internal static class JormungandrDraw
    {
        public static void Draw(Projectile projectile, ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[projectile.type];
            Rectangle frame = new(0, frameHeight * projectile.frame, texture.Width, frameHeight);
            SpriteEffects effects = projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY), frame,
                projectile.GetAlpha(Color.White), projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f),
                projectile.scale, effects);
        }
    }

    internal static class JormungandrConstants
    {
        public const float SegmentSpacing = 48f;
    }
}
