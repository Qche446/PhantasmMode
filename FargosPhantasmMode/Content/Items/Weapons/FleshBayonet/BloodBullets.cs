using FargosPhantasmMode.Common;
namespace FargosPhantasmMode.Content.Items.Weapons
{
    public class BloodBullets : ModProjectile
    {
        public byte MoveType => (byte)Projectile.ai[2];
        public const byte Single = 0;
        public const byte Throughout = 1;
        public const byte Diffusion = 2;
        private ref float Timer => ref Projectile.localAI[0];
        public Vector2 TargetPos => new(Projectile.ai[0], Projectile.ai[1]);
        public bool CanSplit => Projectile.localAI[2] == 0;
        public override bool ShouldUpdatePosition() => CanSplit;
        public override string Texture => "Terraria/Images/Projectile_756";
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 45;
            Projectile.penetrate = 1;
            Projectile.alpha = 0;
            Projectile.scale = 0.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
        }
        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
            {
                if (CanSplit)
                {
                    if (MoveType == 1)
                    {
                        Projectile.penetrate = -1;
                        Projectile.timeLeft = 90;
                        Projectile.scale = 0.6f;
                        Projectile.position = Projectile.Center;
                        Projectile.width = Projectile.height = 90;
                        Projectile.Center = Projectile.position;
                    }
                    else
                        Projectile.timeLeft = (int)((TargetPos - Projectile.Center).Length() / Projectile.velocity.Length());
                }
                else
                {
                    Projectile.Opacity = 0;
                    SoundEngine.PlaySound(SoundID.Item60, Projectile.Center);
                }
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);
            }
            /*
            if (Main.rand.NextBool(8))
            {
                Vector2 energySpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(90f, 90f) + Projectile.velocity * 2f;
                Vector2 energyVelocity = -Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.direction) * Main.rand.NextFloat(6f, 8.75f);
                new FargoSparkParticle(energySpawnPosition, energyVelocity, Color.Red, 0.4f, 45, bloomColor: Color.DarkRed)
                .Spawn();
            }
            */
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.localAI[0]++;
            Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.1f, 0.1f) * Projectile.scale);
            if (CanSplit)
                return;
            if (Timer < 20)
            {
                Projectile.Opacity += 0.1f;
                Projectile.scale = Projectile.Opacity * 0.6f;
                Dust dust3 = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 16f), DustID.Blood, Projectile.velocity * MathHelper.Lerp(0.2f, 0.5f, Main.rand.NextFloat()));
                dust3.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                dust3.velocity *= 0.5f;
                dust3.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
            }
            else if (Timer >= 30)
            {
                Projectile.Opacity -= 0.2f;
            }
            else if (Timer >= 35)
                Projectile.Kill();
        }
        public override void OnKill(int timeLeft)
        {
            /*
            for (float num17 = 0f; num17 < 1f; num17 += 0.025f)
            {
                Dust dust15 = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 16f) * Projectile.scale + Projectile.velocity.SafeNormalize(Vector2.UnitY) * num17 * 200f * Projectile.scale, DustID.Blood, Main.rand.NextVector2Circular(3f, 3f));
                dust15.velocity.Y += -0.3f;
                Dust dust16 = dust15;
                Dust dust3 = dust16;
                dust3.velocity += Projectile.velocity * 0.2f;
                dust15.scale = 1f;
                dust15.alpha = 100;
            }
            */
            //MonoShot.Ring(Projectile.GetSource_FromThis(), Projectile.Center, 3, Type, (int)(Projectile.damage * 0.5f), 0, Projectile.rotation, Projectile.knockBack, Main.myPlayer, ai2: 1);
            if (!CanSplit)
                return;
            Projectile.rotation = Main.rand.NextFloat() * MathHelper.TwoPi;
            for (int i = 0; i < 3; i++)
            {
                Vector2 dir = (Projectile.rotation + i * MathHelper.TwoPi / 3f).ToRotationVector2();
                var p = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center + 30 * dir, dir, Type, (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner);
                p.localAI[2] = 1;
                p.penetrate = 4;
                p.Phantasm().CaseCrush += 1;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!CanSplit || MoveType != 1)
                return;
            Projectile.rotation = Main.rand.NextFloat() * MathHelper.TwoPi;
            for (int i = 0; i < 3; i++)
            {
                Vector2 dir = (Projectile.rotation + i * MathHelper.TwoPi / 3f).ToRotationVector2();
                var p = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center + 30 * dir, dir, Type, (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner);
                p.localAI[2] = 1;
                p.penetrate = 4;
                p.Phantasm().CaseCrush += 1;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;
            SpriteEffects effects = SpriteEffects.None;
            Color color26 = lightColor;
            color26 = Projectile.GetAlpha(color26) * Projectile.Opacity;
            
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = color26 * 0.75f;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, Projectile.scale, effects, 0);
            }
            
            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26, Projectile.rotation, origin2, Projectile.scale, effects, 0);
            return false;
        }
    }
}
