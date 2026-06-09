using FargowiltasSouls;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Luminance.Common.Utilities;
using FargowiltasSouls.Content.Bosses.MutantBoss;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    /// <summary>
    /// ai0=whoami，ai1控制颜色(0 为红，1为蓝)
    /// </summary>
    public class MutantFlash : ModProjectile
    {
        public override string Texture => FargoSoulsUtil.EmptyTexture;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.scale = 0.8f;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 1f)
            {
                Projectile.localAI[1] = Main.rand.NextBool() ? -1 : 1;
                SoundEngine.PlaySound(in SoundID.MaxMana, Projectile.Center);
            }

            Projectile.rotation += Projectile.localAI[1] * (MathF.PI * 2f) / 90f;
            if ((Projectile.localAI[0] += 1f) > 10f)
            {
                Projectile.alpha += 5;
                Projectile.scale -= 0.025f;
            }

            NPC nPC = Main.npc[(int)Projectile.ai[0]];
            if (nPC.TypeAlive(ModContent.NPCType<MutantBoss>()))
            {
                Projectile.Center = nPC.Center + Offset(nPC);
                Projectile.velocity = nPC.velocity;
            }

            if ((double)Projectile.scale <= 0.05)
            {
                Projectile.Kill();
            }
        }

        public static Vector2 Offset(NPC npc)
        {
            return -Vector2.UnitY * npc.width * 0.45f;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                int num = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 8, 0f, 0f, 0, default(Color), 1.5f);
                Main.dust[num].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = (int)Projectile.ai[1] switch
            {
                0 => Color.IndianRed,
                1 => Color.Blue,
                _ => Color.Red,
            };

            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D value = ModContent.Request<Texture2D>("FargowiltasSouls/Assets/Effects/LifeStar", AssetRequestMode.ImmediateLoad).Value;
            Rectangle value2 = new Rectangle(0, 0, value.Width, value.Height);
            float num = Projectile.scale * Main.rand.NextFloat(1.5f, 3f);
            Vector2 origin = new Vector2((float)(value.Width / 2) + num, (float)(value.Height / 2) + num);
            Main.spriteBatch.Draw(value, Projectile.Center - Main.screenPosition, value2, color * Projectile.Opacity, Projectile.rotation, origin, num, SpriteEffects.None, 0f);
            DrawData drawData = new DrawData(value, Projectile.Center - Main.screenPosition, value2, color * Projectile.Opacity, Projectile.rotation, origin, num, SpriteEffects.None);
            GameShaders.Misc["LCWingShader"].UseColor(color * Projectile.Opacity).UseSecondaryColor(color * Projectile.Opacity);
            GameShaders.Misc["LCWingShader"].Apply();
            drawData.Draw(Main.spriteBatch);
            Main.spriteBatch.ResetToDefault();
            return false;
        }
    }
}
