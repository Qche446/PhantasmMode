using FargowiltasSouls;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    /// <summary>
    /// ai[0]传持续时间，ai[1]传whoami
    /// </summary>
    public class EoCTpTelegraph : ModProjectile
    {
        // Kills the projectile above 0, so set it to a negative value.
        public ref float Timer => ref Projectile.ai[0];

        // The .whoAmI of the parent npc.
        public ref float ParentIndex => ref Projectile.ai[1];

        public override string Texture => "FargowiltasSouls/Assets/Effects/LifeStar";

        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.aiStyle = -1;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
            //writer.Write(Projectile.localAI[2]);
            base.SendExtraAI(writer);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.Read();
            Projectile.localAI[1] = reader.Read();
            //Projectile.localAI[2] = reader.Read();
            base.ReceiveExtraAI(reader);
        }
        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = Math.Abs(Timer);
                Projectile.netUpdate = true;
            }

            if (Timer < 0f)
                Projectile.Kill();

            // Ramp up the scale and rotation over time
            float ratio = 1f - Math.Abs(Timer) / Projectile.localAI[0];
            if (ratio > 0.5f)
                Projectile.localAI[1] = ratio > 0.65f ? 1 : ((ratio - 0.5f) / 0.15f);
            float trueratio = ratio > 0.15f ? 1 : (ratio / 0.15f);
            float rampupVfx = (float)Math.Sin(MathHelper.PiOver2 * trueratio);
            float otherVfx = (float)Math.Sin(MathHelper.PiOver2 * Projectile.localAI[1]);
            Projectile.scale = 0.1f + 0.6f * rampupVfx + 1.4f * otherVfx;
            Projectile.scale *= Main.rand.NextFloat(0.8f, 1.2f);
            //Projectile.rotation = 2f * MathHelper.TwoPi * rampupVfx;

            NPC parent = FargoSoulsUtil.NPCExists(ParentIndex);
            // Stick to a position set by lifelight.
            if (parent != null)
                Projectile.Center = new Vector2(parent.localAI[0], parent.localAI[1]);

            Timer--;
        }

        // Telegraphs should not deal damage.
        public override bool? CanDamage() => false;

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.Teal;
            color.A = 100;
            return color;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 origin = texture.Size() / 2f;

            for (int i = 0; i < 3; i++)
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation + MathHelper.PiOver4, origin, 0.5f * Projectile.scale, effects, 0);
            }  
            return false;
        }
    }

    /// <summary>
    /// ai[0]持续时间，ai[1]传自己，ai[2]传发射角度
    /// </summary>
    public class SuperEoCTpTelegraph : ModProjectile
    {
        // Kills the projectile above 0, so set it to a negative value.
        public ref float Timer => ref Projectile.ai[0];

        // The .whoAmI of the parent npc.
        public ref float ParentIndex => ref Projectile.ai[1];

        public override string Texture => "FargowiltasSouls/Assets/Effects/LifeStar";

        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.aiStyle = -1;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
            //writer.Write(Projectile.localAI[2]);
            base.SendExtraAI(writer);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.Read();
            Projectile.localAI[1] = reader.Read();
            //Projectile.localAI[2] = reader.Read();
            base.ReceiveExtraAI(reader);
        }
        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = Math.Abs(Timer);
                Projectile.netUpdate = true;
            }
            NPC parent = FargoSoulsUtil.NPCExists(ParentIndex);
            if (parent == null || parent.active == false)
                Projectile.Kill();
            
            // Ramp up the scale and rotation over time
            float ratio = 1f - Math.Abs(Timer) / Projectile.localAI[0];
            if (ratio > 0.5f)
                Projectile.localAI[1] = ratio > 0.65f ? 1 : ((ratio - 0.5f) / 0.15f);
            float trueratio = ratio > 0.15f ? 1 : (ratio / 0.15f);
            float rampupVfx = (float)Math.Sin(MathHelper.PiOver2 * trueratio);
            float otherVfx = (float)Math.Sin(MathHelper.PiOver2 * Projectile.localAI[1]);
            Projectile.scale = 0.1f + 0.6f * rampupVfx + 1.4f * otherVfx;
            Projectile.scale *= Main.rand.NextFloat(0.8f, 1.2f);
            if (Timer < 0f)
            {
                parent.Center = Projectile.Center;
                parent.velocity = 80 * Vector2.UnitX.RotatedBy(Projectile.ai[2]);
                parent.rotation = parent.velocity.ToRotation() - MathHelper.PiOver2;
                Projectile.Kill();
            }
            Timer--;

        }

        // Telegraphs should not deal damage.
        public override bool? CanDamage() => false;

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.Teal;
            color.A = 100;
            return color;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 origin = texture.Size() / 2f;

            for (int i = 0; i < 3; i++)
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation + MathHelper.PiOver4, origin, 0.5f * Projectile.scale, effects, 0);
            }
            return false;
        }
    }
}
