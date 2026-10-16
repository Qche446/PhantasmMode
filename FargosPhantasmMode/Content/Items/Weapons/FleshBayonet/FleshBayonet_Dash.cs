using FargosPhantasmMode.Common;
using FargosPhantasmMode.Common.Particles;
using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Buffs.Masomode;
using Monochrome.Common.MonoUtil;
using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Trails;

namespace FargosPhantasmMode.Content.Items.Weapons
{
    public class FleshBayonet_Dash : FleshBayonet_Reload
    {
        public override bool? CanDamage() => true;
        public override bool ShouldUpdatePosition() => true;
        public Player Owner => Main.player[Projectile.owner];
        public float LungeProgression => Utils.GetLerpValue(0f, 20, Time, true);
        public const int CooldownTime = 30;
        public ref float Time => ref Projectile.ai[0];
        private MonoTrail trail;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.timeLeft = 20 + CooldownTime;
        }
        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity * (float)(1f - Math.Pow(LungeProgression, 5D));
        public override void AI()
        {
            if (Owner.Phantasm().MouseRight || Owner.dead || !Owner.active || Owner.noItems)
            {
                Projectile.Kill();
                Owner.velocity = (Owner.velocity * 0.3f).ClampLength(0, 11f);
                return;
            }

            if (Time == 1f)
                SoundEngine.PlaySound(SoundID.AbigailAttack, Projectile.Center);

            // Stick to the owner.
            Projectile.Center = Owner.MountedCenter;

            float angularVelocity = MonoUtil.Pi * MathF.Pow(LungeProgression, 3f) * 0.024f;
            float currentRotation = Projectile.velocity.ToRotation();
            float idealRotation = Owner.MountedCenter.AngleTo(Main.MouseWorld);
            Projectile.velocity = currentRotation.AngleTowards(idealRotation, angularVelocity).ToRotationVector2();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Owner.heldProj = Projectile.whoAmI;

            Owner.fallStart = (int)(Owner.position.Y / 16f);

            if (LungeProgression < 1f)
            {
                float velocityPower = Utils.Remap(MathF.Sin(MathF.PI * MonoUtil.Saturate(LungeProgression)), 0f, 1f, 0.25f, 1f);
                Vector2 newVelocity = Projectile.velocity * 50 * (0.09f + 0.91f * velocityPower);
                Owner.velocity = newVelocity;
                Owner.Phantasm().LungingDown = true;

                if (Main.rand.NextBool(2))
                {
                    Vector2 energySpawnPosition = Owner.Center + Main.rand.NextVector2Circular(45f, 45f) - Owner.velocity * 2f;
                    Vector2 energyVelocity = -Owner.velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * Main.rand.NextFloat(6f, 8.75f);
                    new FargoSparkParticle(energySpawnPosition, energyVelocity, Color.Red, 0.4f, 45, bloomColor: Color.DarkRed)
                    .Spawn();
                }
            }

            Time++;
            trail ??= new()
            {
                Space = MonoGraphicsSpace.World,
                PointsToCreate = 40,
                UseUnscaledMatrix = true,
                ColorFunction = t => Color.Lerp(Color.Transparent, Color.Red, t) * 0.5f,
                WidthFunction = t => MathHelper.SmoothStep(Projectile.width * Projectile.scale * 0.4f, 3f, 1 - t),
                Offset = Projectile.Size * 0.5f,
                Texture = FargosTextureRegistry.FadedStreak.Value
            };
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Owner.velocity = Owner.SafeDirectionTo(target.Center) * -18f;
            Projectile.Kill();
            int time1 = 5;
            int time2 = 8;
            if (Projectile.ai[2] != 0)
            {
                time1 = 10;
                time2 = 16;
                target.Phantasm().CrushCumulative += 50;
            }
            target.AddBuff(ModContent.BuffType<MutantNibbleBuff>(), time1 * 60);
            target.AddBuff(ModContent.BuffType<PrayerofTaintedBlood>(), time2 * 60);
        
            target.Phantasm().CrushCumulative += 50;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);
            if (Time <= 20 )
                trail?.DrawFrom(Projectile.oldPos);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            trail = null;
            Owner.Phantasm().LungingDown = false;
        }
    }
}
