using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Systems;
using Monochrome.Common.MonoUtil;
using Monochrome.Core.Graphics.Trails;
using Terraria.GameContent;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class MutantGoldenShower : ModProjectile, IMonoOwnedProj<MutantBoss>
    {
        public override string Texture => "Terraria/Images/Projectile_288";
        private MonoTrail trail;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.aiStyle = -1;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.hostile = true;
        }
        public override bool? CanCutTiles() => false;
        public override void AI()
        {
            Projectile.extraUpdates = (int)Projectile.ai[1];
            if (Projectile.localAI[1] == 0)
            {
                Projectile.localAI[1] = 1;
                SoundEngine.PlaySound(SoundID.Item17, Projectile.Center);
            }
            if (--Projectile.ai[0] < 0)
                Projectile.tileCollide = true;

            if (Projectile.localAI[0] == 0)
            {
                Projectile.velocity.X += 0.5f * Projectile.ai[2];
                Projectile.rotation = Projectile.velocity.ToRotation() + (float)Math.PI / 2f;
            }
            trail ??= new()
            {
                WidthFunction = t => WidthFunction(1 - t),
                ColorFunction = t => ColorFunction(1 - t),
                PointsToCreate = 5,
                Texture = FargosTextureRegistry.FadedStreak.Value,
                UseUnscaledMatrix = true,
            };
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Ichor, 900);
            target.AddBuff(BuffID.OnFire, 300);
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 600);
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            base.Projectile.velocity = Vector2.Zero;
            if (base.Projectile.timeLeft > 10)
            {
                base.Projectile.timeLeft = 10;
            }

            return false;
        }
        public float WidthFunction(float completionRatio)
        {
            float value = base.Projectile.scale * (float)base.Projectile.width * 1.3f;
            return MathHelper.SmoothStep(value, 3.5f, completionRatio);
        }

        public static Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(new Color(250, 250, 0), Color.Transparent, completionRatio) * 0.7f;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            int num = TextureAssets.Projectile[base.Projectile.type].Value.Height / Main.projFrames[base.Projectile.type];
            int y = num * base.Projectile.frame;
            Rectangle rectangle = new Rectangle(0, y, value.Width, num);
            Vector2 origin = rectangle.Size() / 2f;
            Main.EntitySpriteDraw(value, base.Projectile.Center - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, Color.White, base.Projectile.rotation, origin, base.Projectile.scale, SpriteEffects.None);
            trail?.DrawFrom(Projectile.oldPos);
            return false;
        }
    }
}
