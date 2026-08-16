using FargowiltasSouls.Content.Bosses.MutantBoss;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using Luminance.Common.DataStructures;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    /// <summary>
    /// ai0 whoami,ai1时间,ai2光环颜色0123456红橙黄绿青蓝紫
    /// </summary>
    public class MutantSpear : MutantSpearSpin, IProjOwnedByBoss<MutantBoss>
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/BossWeapons/Penetrator";
        public Color GlowColor { get => (int)Projectile.ai[2] switch
        {
            0 => Color.Red, 1 => Color.Orange, 2 => Color.Yellow, 3 => Color.Green, 4 => Color.Teal, 5 => Color.Blue, 6 => Color.Purple, _ => Color.White,
        };
        }
        public override void AI()
        {
            if (base.Projectile.localAI[1] == 0f)
            {
                base.Projectile.localAI[1] = ((!Main.rand.NextBool()) ? 1 : (-1));
                base.Projectile.timeLeft = (int)base.Projectile.ai[1];
            }

            NPC nPC = Main.npc[(int)base.Projectile.ai[0]];
            if (nPC.active && nPC.type == ModContent.NPCType<MutantBoss>())
            {
                base.Projectile.Center = nPC.Center;
                direction = nPC.direction;
                base.Projectile.rotation += 0.4586267f * base.Projectile.localAI[1];
                if (base.Projectile.timeLeft % 20 == 0)
                {
                    SoundEngine.PlaySound(in SoundID.Item1, base.Projectile.Center);
                }
                base.Projectile.alpha = 0;
            }
            else
            {
                base.Projectile.Kill();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            int num = TextureAssets.Projectile[base.Projectile.type].Value.Height / Main.projFrames[base.Projectile.type];
            int y = num * base.Projectile.frame;
            Rectangle rectangle = new Rectangle(0, y, value.Width, num);
            Vector2 origin = rectangle.Size() / 2f;
            Color newColor = lightColor;
            newColor = base.Projectile.GetAlpha(newColor);
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[base.Projectile.type]; i++)
            {
                Color color = newColor * 0.5f;
                color *= (float)(ProjectileID.Sets.TrailCacheLength[base.Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[base.Projectile.type];
                Vector2 vector = base.Projectile.oldPos[i];
                float rotation = base.Projectile.oldRot[i];
                Main.EntitySpriteDraw(value, vector + base.Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, color, rotation, origin, base.Projectile.scale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(value, base.Projectile.Center - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, base.Projectile.GetAlpha(lightColor), base.Projectile.rotation, origin, base.Projectile.scale, SpriteEffects.None);
            if (base.Projectile.ai[1] > 0f)
            {
                Texture2D value2 = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/MutantBoss/MutantSpearAimGlow", AssetRequestMode.ImmediateLoad).Value;
                float num2 = (float)base.Projectile.timeLeft / base.Projectile.ai[1];
                Color color2 = GlowColor;

                color2 *= 1f - num2;
                float scale = base.Projectile.scale * 8f * num2;
                Main.EntitySpriteDraw(value2, base.Projectile.Center - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), value2.Bounds, color2, 0f, value2.Bounds.Size() / 2f, scale, SpriteEffects.None);
            }
            return false;
        }
    }
    public class MutantBiggestSting : MutantSpear, IProjOwnedByBoss<MutantBoss>
    {
        public override string Texture => "FargowiltasSouls/Content/Items/Weapons/FinalUpgrades/TheBiggestSting";
        public override void AI()
        {
            if (base.Projectile.localAI[1] == 0f)
            {
                base.Projectile.localAI[1] = ((!Main.rand.NextBool()) ? 1 : (-1));
                base.Projectile.timeLeft = (int)base.Projectile.ai[1];
            }

            NPC nPC = Main.npc[(int)base.Projectile.ai[0]];
            if (nPC.active && nPC.type == ModContent.NPCType<MutantBoss>())
            {
                base.Projectile.Center = nPC.Center;
                direction = nPC.direction;
                base.Projectile.rotation += 0.4586267f * base.Projectile.localAI[1];
                if (base.Projectile.timeLeft % 20 == 0)
                {
                    SoundEngine.PlaySound(in SoundID.Item1, base.Projectile.Center);
                }
                base.Projectile.alpha = 0;
            }
            else
            {
                base.Projectile.Kill();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return base.PreDraw(ref lightColor);
        }
    }
}
