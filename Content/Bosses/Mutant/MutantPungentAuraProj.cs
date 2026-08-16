using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using Luminance.Common.DataStructures;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class MutantPungentAuraProj : PungentAuraProj, IProjOwnedByBoss<MutantBoss>
    {
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.scale = 1f;
            Projectile.timeLeft = 2000;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            if (!Projectile.owner.IsWithinBounds(Main.maxPlayers))
            {
                Projectile.Kill();
                return;
            }
            Player player = Main.LocalPlayer;
            Projectile.velocity = 8 * Projectile.SafeDirectionTo(player.Center);
            if (player.Distance(Projectile.Center) < 10)
            {
                Projectile.velocity = Vector2.Zero;
            }
            Projectile.ai[0] = 132;//半径
            int index = -1;
            for (int i = 0; i <= Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == ModContent.NPCType<MutantBoss>() && Main.npc[i].active)
                {
                    index = i;
                }
            }
            if (index == -1 || Main.npc[index].ai[0] != 50)//紫砂
            {
                Projectile.Kill();
            }

            const float distance = 128;//碰撞检测用

            foreach (Player n in Main.player.Where(n => n.active))
            {
                if (Vector2.Distance(Projectile.Center, FargoSoulsUtil.ClosestPointInHitbox(n.Hitbox, Projectile.Center)) < distance)
                {
                    n.AddBuff(ModContent.BuffType<PungentGazeBuff>(), 180);
                    n.AddBuff(ModContent.BuffType<SmiteBuff>(), 180);
                    n.AddBuff(ModContent.BuffType<MarkedforDeathBuff>(), 180);
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!Projectile.owner.IsWithinBounds(Main.maxNPCs))
            {
                Projectile.Kill();
                return false;
            }
            

            Color darkColor = Color.YellowGreen;
            Color mediumColor = Color.OrangeRed;
            Color lightColor2 = Color.Lerp(Color.IndianRed, Color.White, 0.35f);

            Vector2 auraPos = Projectile.Center;
            float radius = Projectile.ai[0];
            var target = Main.LocalPlayer;
            var blackTile = TextureAssets.MagicPixel;
            var diagonalNoise = FargosTextureRegistry.HoneycombNoise;
            if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                return false;
            var maxOpacity = Projectile.Opacity;

            ManagedShader borderShader = ShaderManager.GetShader("FargowiltasSouls.WoFAuraShader");
            borderShader.TrySetParameter("colorMult", 7.35f);
            borderShader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
            borderShader.TrySetParameter("radius", radius);
            borderShader.TrySetParameter("anchorPoint", auraPos);
            borderShader.TrySetParameter("screenPosition", Main.screenPosition);
            borderShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            borderShader.TrySetParameter("playerPosition", target.Center);
            borderShader.TrySetParameter("maxOpacity", maxOpacity);
            borderShader.TrySetParameter("darkColor", darkColor.ToVector4());
            borderShader.TrySetParameter("midColor", mediumColor.ToVector4());
            borderShader.TrySetParameter("lightColor", lightColor2.ToVector4());
            borderShader.TrySetParameter("opacityAmp", 1f);

            Main.spriteBatch.GraphicsDevice.Textures[1] = diagonalNoise.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, borderShader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
            Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
            Main.spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
