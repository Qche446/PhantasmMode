using FargosPhantasmMode.Common;
using FargosPhantasmMode.Common.Particles;
using FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins;
using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Globals;
using Monochrome.Common.MonoUtil;
using Monochrome.Content.Status;
using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Particles;
using Monochrome.Core.Graphics.Particles.Builtin;
using Monochrome.Core.Graphics.Trails;

namespace FargosPhantasmMode.Content.Items.Weapons
{
    public class RendingSpiral_White : DarkStar
    {
        private int Time;
        private MonoTrail trail;
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.alpha = 50;
            //Projectile.light = 1f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.extraUpdates = 2;
            Projectile.penetrate = -1;
        }
        public override void AI()
        {
            base.AI();
            trail ??= new MonoTrail(20)
            {
                Space = MonoGraphicsSpace.World,
                MinDistance = 0f,
                PointsToCreate = 40,
                UseUnscaledMatrix = true,
                WidthFunction = t => MathHelper.SmoothStep(base.Projectile.scale * base.Projectile.width, 3.5f, 1f - t),
                ColorFunction = t => Color.Lerp(Color.White, Color.Transparent, 1 - t) * 0.6f,
                Texture = FargosTextureRegistry.FadedStreak.Value,
                Offset = 0.5f * Projectile.Size
            };
            Time++;
            double width = 36;
            width *= Math.Sin(Time * MathHelper.TwoPi / 40f);
            Vector2 pr = Projectile.velocity.SafeNormalize(Vector2.UnitX) * (float)width;
            MonoDust.At(Projectile.Center + pr, DustID.WhiteTorch, Vector2.Zero, noGravity: true, scale: 2f);
            MonoDust.At(Projectile.Center - pr, DustID.WhiteTorch, Vector2.Zero, noGravity: true, scale: 2f);
            width = 56;
            width *= Math.Sin(Time * MathHelper.TwoPi / 40f + MathHelper.PiOver2);
            pr = Vector2.Normalize(Projectile.velocity).Perp() * (float)width;
            MonoDust.At(Projectile.Center + pr, DustID.HallowedTorch, Vector2.Zero, noGravity: true, scale: 2f);
            MonoDust.At(Projectile.Center - pr, DustID.HallowedTorch, Vector2.Zero, noGravity: true, scale: 2f);
            new FargoSparkParticle(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX).Perp() * Main.rand.NextFloat(-20, 20),
                    Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.6f) * Main.rand.NextFloat(0.1f, 0.6f),
                    Color.White, 0.4f, 45, bloomColor: Color.White)
                    .Spawn();
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<PhSublimationBuff>(), 5 * 60);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.AddBuff(ModContent.BuffType<PhSublimationBuff>(), 5 * 60);
            int hunterMark = HunterMark.Level(target.MonoBuffs().Data<HunterMark>());
            modifiers.FinalDamage *= 1f + hunterMark * 2f;
            target.Take(ModContent.BuffType<HunterMark>());
            SoundEngine.PlaySound(in SoundID.Item14, base.Projectile.position);
            for (int i = 0; i < 20; i++)
            {
                new MonoStreak()
                {
                    Position = target.Center,
                    Velocity = Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.1f, 0.6f),
                    Thickness = 4f,
                    LengthScale = 1.5f,
                    MinLength = 16f,
                    Drag = 0.96f,
                    Color = Color.White,
                    Lifetime = 45
                }.Spawn();
            }
        }
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, lightColor.A - Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glow = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Bosses/VanillaEternity/Twins/DarkStar_Glow", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            int rect1 = glow.Height / Main.projFrames[Projectile.type];
            int rect2 = rect1 * Projectile.frame;
            Rectangle glowrectangle = new(0, rect2, glow.Width, rect1);
            Vector2 gloworigin2 = glowrectangle.Size() / 2f;
            Color glowcolor = Color.Lerp(new Color(255, 255, 255, 150), Color.Transparent, 0.6f);
            Vector2 drawCenter = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 14;

            Main.EntitySpriteDraw(glow, drawCenter - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle),//create small, non transparent trail texture
                   Projectile.GetAlpha(lightColor), Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, Projectile.scale / 2, SpriteEffects.None, 0);

            for (int i = 0; i < 3; i++) //create multiple transparent trail textures ahead of the projectile
            {
                Vector2 drawCenter2 = drawCenter + (Projectile.velocity.SafeNormalize(Vector2.UnitX) * 12).RotatedBy(MathHelper.Pi / 5 - i * MathHelper.Pi / 5); //use a normalized version of the projectile's velocity to offset it at different angles
                drawCenter2 -= Projectile.velocity.SafeNormalize(Vector2.UnitX) * 12; //then move it backwards
                Main.EntitySpriteDraw(glow, drawCenter2 - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle),
                    glowcolor, Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, Projectile.scale, SpriteEffects.None, 0);
            }

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++) //reused betsy fireball scaling trail thing
            {

                Color color27 = glowcolor;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                float scale = Projectile.scale * (ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i] - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 14;
                Main.EntitySpriteDraw(glow, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle), color27,
                    Projectile.velocity.ToRotation() + MathHelper.PiOver2, gloworigin2, scale, SpriteEffects.None, 0);
            }
            trail?.DrawFrom(Projectile.oldPos);
            return false;
        }
        public override void PostDraw(Color lightColor)
        {
            
        }
    }
}
