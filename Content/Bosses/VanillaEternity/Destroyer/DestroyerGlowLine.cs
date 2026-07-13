using FargowiltasSouls;
using FargowiltasSouls.Content.Projectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;


namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    public class DestroyerGlowLine : GlowLine
    {
        float angle = 0;
        new int counter = 0;
        public override string Texture => "FargowiltasSouls/Content/Projectiles/GlowLine";
        public override void SetDefaults()
        {
            Projectile.width = 6;
            base.SetDefaults();
        }
        public override void AI()
        {
            if (angle == 0)
                angle = Projectile.ai[2];
            int maxTime = 120;
            float alphaModifier = 10;
            Projectile.ai[2] -= angle / (maxTime-1);
            color = Color.Blue;
            NPC npc = FargoSoulsUtil.NPCExists(Projectile.ai[1], NPCID.TheDestroyer);
            if (npc != null)
            {
                Vector2 offset = new Vector2(npc.width - 24, 0).RotatedBy(npc.rotation -1.57079637);
                Projectile.Center = npc.Center + offset;
                Projectile.rotation = npc.rotation - MathHelper.PiOver2 + Projectile.ai[2];
            }
            else
            {
                Projectile.Kill();
                return;
            }
            if (++counter > maxTime)
            {
                Projectile.Kill();
                return;
            }

            if (alphaModifier >= 0)
            {
                Projectile.alpha = 255 - (int)(255 * Math.Sin(Math.PI / maxTime * counter) * alphaModifier);
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;
            }

            color.A = 0;
        }
    }
}
