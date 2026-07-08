using FargosPhantasmMode.Assets.ExtraTextures;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Bosses.Champions.Will;
using FargowiltasSouls;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    public class MoonlightTrail : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.BeforeNPCs;
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        public override void SetDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.scale = 1f;
            Projectile.timeLeft = 180;

            Projectile.hide = true;
        }
        public ref float NPCID => ref Projectile.ai[0];
        public ref float Duration => ref Projectile.ai[1];
        public ref float Timer => ref Projectile.ai[2];
        public override void AI()
        {
            NPC npc = Main.npc[(int)Projectile.ai[0]];
            if (npc.type == Terraria.ID.NPCID.EyeofCthulhu)
            {
                Projectile.Center = npc.Center + Offset(npc);
                Projectile.velocity = npc.velocity;
            }
            else
            {
                Projectile.Kill();
                return;
            }
            Timer++;
            if (Timer > Duration)
            {
                Projectile.Kill();
                return;
            }
            if (Timer > Duration - 10)
            {
                Projectile.Opacity -= (1f / 8);
            }

        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Projectile.hide)
                behindNPCs.Add(index);
        }
        public static Vector2 Offset(NPC npc) => (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * 25;
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public float WidthFunction(float completionRatio)
        {
            float baseWidth = Projectile.scale * 36 * 3.0f;
            return MathHelper.SmoothStep(baseWidth, baseWidth * 0.3f, completionRatio) * Projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            float threshold = 0.1f;
            float opacity = 1f;
            if (completionRatio < threshold)
                opacity *= MathF.Pow(completionRatio / threshold, 2);

            float threshold2 = 0.78f;
            if (completionRatio > 0.78f)
                opacity *= MathF.Pow(1 - ((completionRatio - threshold2) / (1 - threshold2)), 2);
            return Color.Lerp(new Color(41, 243, 243), Color.Transparent, completionRatio) * 0.4f * opacity * Projectile.Opacity ;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, _ => Projectile.Size * 0.5f, Pixelate: true, Shader: shader), 60);
        }
    }
}
