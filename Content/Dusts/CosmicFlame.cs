using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Dusts
{
    public class CosmicFlame : ModDust
    {
        //public override string Texture => "FargowiltasSouls/Assets/ExtraTextures/AdditiveTextures/BloomParticle";
        public override void OnSpawn(Dust dust)
        {
            dust.alpha = 255;
            dust.noLight = true;
            dust.noGravity = true;
            base.OnSpawn(dust);
        }
        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            dust.scale -= 0.03f;
            dust.velocity *= 0.99f;
            if (dust.scale <= 0)
                dust.active = false;
            return false;
        }
        public static void DrawAll(SpriteBatch sb)
        {
            foreach (Dust d in Main.dust)
            {
                if (d.type == ModContent.DustType<CosmicFlame>() && d.active)
                {
                    Texture2D tex = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Dusts/CosmicFlame").Value;
                    sb.Draw(tex, d.position - Main.screenPosition, null, Color.White, 0, tex.Size() / 2, d.scale, SpriteEffects.None, 0);
                }
            }
        }
    }
}
