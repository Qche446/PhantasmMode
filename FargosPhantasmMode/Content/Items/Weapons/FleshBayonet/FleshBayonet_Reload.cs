using FargosPhantasmMode.Common;
using FargosPhantasmMode.Common.Particles;
using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls;
using Microsoft.CodeAnalysis;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Content.Items.Weapons
{
    public class FleshBayonet_Reload : ModProjectile
    {
        public override string Texture => FleshBayonet.TexturePath + "FleshBayonet";
        private Vector2 offset;
        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.scale = 1;
            //Projectile.width = Projectile.height = (int)(Projectile.scale * 60);
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = 30;
            Projectile.aiStyle = -1;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.ownerHitCheck = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;
        public override void AI()
        {
            var py = FargoSoulsUtil.PlayerExists((int)Projectile.ai[0]);
            if (py == null || py.dead || !py.active)
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.ai[2] == 0)
            {
                Projectile.ai[2]++;
                offset = Projectile.Center - py.Center;
                new FargoSparkParticle(Projectile.Center, Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.4f), Color.Red, 0.5f, 15, true, Color.Red).Spawn();
            }
            Projectile.Center = py.Center + offset;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.direction = py.direction;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation;
            SpriteEffects effect = SpriteEffects.None;
            if (Projectile.direction > 0)
            {
                drawPosition += new Vector2(20, 0).RotatedBy(rotation);
                rotation -= MathHelper.PiOver4 * 3;
                effect = SpriteEffects.FlipVertically;
            }
            else
            {
                drawPosition += new Vector2(20, 0).RotatedBy(rotation);
                rotation += MathHelper.PiOver4 * 3;
                effect = SpriteEffects.None;
            }
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), rotation, origin, Projectile.scale, effect, 0);
            return false;
        }
    }
}
