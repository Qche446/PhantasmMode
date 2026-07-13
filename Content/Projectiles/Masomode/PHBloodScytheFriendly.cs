using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles.Masomode
{
    public class PHBloodScytheFriendly : BloodScytheFriendly//, IPixelatedPrimitiveRenderer
    {
        public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
        {
            if (randomize == 0)
            {
                randomize += Main.rand.Next(1, 4);
                Projectile.netUpdate = true;
            }
            Texture2D texture = WorldSavingSystem.masochistModeReal ?
                ModContent.Request<Texture2D>("FargowiltasSouls/Content/Projectiles/Masomode/BloodScythe" + randomize).Value :
                ModContent.Request<Texture2D>("FargowiltasSouls/Content/Projectiles/Masomode/BloodScytheVanilla" + randomize).Value;
            if (WorldSavingSystem.masochistModeReal)
            {
                Texture2D glowTexture = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Projectiles/GlowRing").Value;

                Vector2 glowDrawPosition = Projectile.Center + Projectile.velocity / 10f;
                glowDrawPosition += Main.rand.NextVector2Circular(5, 5);

                Main.EntitySpriteDraw(glowTexture, glowDrawPosition - Main.screenPosition, null,
                    Microsoft.Xna.Framework.Color.DarkRed, Projectile.rotation, glowTexture.Size() * 0.5f,
                    Projectile.scale * 0.8f, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Microsoft.Xna.Framework.Color.DarkRed, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo info, int damageDone)
        {
            if (WorldSavingSystem.MasochistModeReal)
            {
                target.AddBuff(ModContent.BuffType<BerserkedBuff>(), 120);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 120);
            }
        }
    }
}
