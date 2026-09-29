using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Circuitry;
using Fargowiltas.Common.Configs;
using FargowiltasSouls;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles.Masomode
{
    public class FusedLensMechElectricOrb : ModProjectile
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/MechElectricOrb";
        public static readonly SoundStyle ShotSound = FargosSoundRegistry.ElectricOrbShot with
        {
            PitchVariance = 0.3f,
            Volume = 7f
        };

        public const int Red = 2;

        public const int Blue = 3;

        public const int Yellow = 0;

        public const int Green = 1;

        public bool lastSecondAccel;

        public ref float ColorAI => ref base.Projectile.ai[2];

        public float ColorType
        {
            get
            {
                return ColorAI;
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[base.Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[base.Projectile.type] = 2;
            Main.projFrames[base.Type] = 10;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.aiStyle = -1;
            Projectile.alpha = 50;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 18000;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.scale *= 0.8f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.CritChance = 100;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
            {
                return true;
            }

            Rectangle rectangle = projHitbox;
            rectangle.X = (int)base.Projectile.oldPosition.X;
            rectangle.Y = (int)base.Projectile.oldPosition.Y;
            if (rectangle.Intersects(targetHitbox))
            {
                return true;
            }

            return false;
        }

        public override void AI()
        {
            Projectile.CritChance = (int)FargoSoulsUtil.HighestCritChance(Main.player[Projectile.owner]);
            Player player = Main.player[Projectile.owner];
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            Projectile.netUpdate = true;
            Projectile.ai[0]++;

            if (player.whoAmI == Main.myPlayer && (player.dead || !player.HasEffect<FusedLensMechElectricOrbEffect>()))
            {
                Projectile.Kill();
                return;
            }
            
            int type = FargoSoulsUtil.FindClosestHostileNPC(Projectile.Center, 1200, true);
            float distance = (Projectile.Center - player.Center).Length();
            if (type > 0 && Projectile.ai[0] >= 60)//有敌人时
            {
                if (Main.npc[(int)type].active)
                {
                    NPC npc = Main.npc[(int)type];
                    Vector2 vectorToIdlePosition = npc.Center - Projectile.Center;
                    Projectile.velocity = 0.98f * Projectile.velocity + 0.02f * vectorToIdlePosition;
                    MechElectricMovement(Projectile, npc.Center, 0, 30);
                }
            }
            else//常态挂机绕玩家伪简谐震动
            {
                if (Projectile.owner == Main.myPlayer && distance > 10)
                {
                    MechElectricMovement(Projectile, player.Center, 0.01f, 30);
                }
            }
            Projectile.Opacity = ModContent.GetInstance<FargoClientConfig>().TransparentFriendlyProjectiles;
            #region 原AI部分
            if (++base.Projectile.frameCounter > 6)
            {
                if (++base.Projectile.frame >= Main.projFrames[base.Type])
                {
                    base.Projectile.frame = 0;
                }

                base.Projectile.frameCounter = 0;
            }

            if (base.Projectile.localAI[1] == 0f)
            {
                SoundStyle style = ShotSound with
                {
                    Volume = 0.3f,
                    MaxInstances = 4
                };
                SoundEngine.PlaySound(in style, base.Projectile.position);
                base.Projectile.localAI[1] = 1f;
                lastSecondAccel = base.Projectile.type == ModContent.ProjectileType<MechElectricOrb>();
            }

            if (base.Projectile.localAI[0] == 0f)
            {
                base.Projectile.localAI[0] = (Main.rand.NextBool() ? 1 : (-1));
            }

            base.Projectile.Opacity = 1f;
            base.Projectile.rotation += MathF.PI / 20f * base.Projectile.localAI[0];
            float colorType = ColorType;
            Color color = ((colorType == 1f) ? Color.Teal : ((colorType == 3f) ? Color.Green : ((colorType != 2f) ? Color.Red : Color.Yellow)));
            Color color2 = color;
            if (Main.rand.NextBool(6))
            {
                Vector2 vector = Vector2.Normalize(-base.Projectile.velocity.RotatedByRandom(0.62831854820251465));
                float num = Math.Max(4f, base.Projectile.velocity.Length() / 2f);
                new ElectricSpark(base.Projectile.Center, vector * num, color2 * 0.7f, Main.rand.NextFloat(0.7f, 1f), 20).Spawn();
            }

            float r = (float)(int)color2.R / 255f;
            float g = (float)(int)color2.G / 255f;
            float b = (float)(int)color2.B / 255f;
            Lighting.AddLight(base.Projectile.Center, r, g, b);
            if (lastSecondAccel && base.Projectile.ai[0] == -1f && (base.Projectile.ai[1] -= 1f) < 0f)
            {
                base.Projectile.velocity *= 1.03f;
            }

            float num2 = base.Projectile.velocity.Length() / (float)(base.Projectile.width * 3);
            if (num2 > 1f)
            {
                base.Projectile.velocity /= num2;
            }
            #endregion
        }
        public static void MechElectricMovement(Projectile projectile, Vector2 targetPos, float k, float maxspeed)
        {
            Vector2 vel = targetPos - projectile.Center;
            projectile.velocity += k * vel;
            Vector2 vel2 = projectile.velocity != Vector2.Zero ? Vector2.Normalize(projectile.velocity) : Vector2.Zero;
            float speed = projectile.velocity.Length();
            if (speed > maxspeed)
            {
                projectile.velocity = maxspeed * vel2;
            }

        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            float colorType = ColorType;
            if (colorType != 1f)
            {
                if (colorType != 3f)
                {
                    if (colorType == 2f)
                    {
                        target.AddBuff(BuffID.Ichor, 180);
                    }
                    else
                    {
                        target.AddBuff(ModContent.BuffType<NanoErosionBuff>(), 180);
                    }
                }
                else
                {
                    target.AddBuff(BuffID.CursedInferno, 180);
                }
            }
            else
            {
                target.AddBuff(BuffID.Electrified, 180);
                target.AddBuff(ModContent.BuffType<LightningRodBuff>(), 180);
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(in SoundID.Item10, base.Projectile.position);
            float colorType = ColorType;
            short num = (short)((colorType == 1f) ? 59 : ((colorType == 3f) ? 61 : ((colorType != 2f) ? 60 : 64)));
            int type = num;
            Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, type, base.Projectile.velocity.X * 0.1f, base.Projectile.velocity.Y * 0.1f, 150, default(Color), 1.2f);
            if (!Main.dedServ)
            {
                Gore.NewGore(base.Projectile.GetSource_FromThis(), base.Projectile.position, new Vector2(base.Projectile.velocity.X * 0.05f, base.Projectile.velocity.Y * 0.05f), Main.rand.Next(16, 18));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[base.Type].Value;
            int num = value.Height / Main.projFrames[base.Type];
            int num2 = value.Width / 4;
            int y = base.Projectile.frame * num;
            int x = (int)ColorType * num2;
            Rectangle rectangle = new Rectangle(x, y, num2, num);
            Vector2 vector = rectangle.Size() / 2f;
            SpriteEffects effects = ((base.Projectile.spriteDirection <= 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            for (float num3 = 0f; num3 < (float)ProjectileID.Sets.TrailCacheLength[base.Projectile.type]; num3 += 0.33f)
            {
                float colorType = ColorType;
                Color color = ((colorType == 1f) ? Color.Teal : ((colorType == 3f) ? Color.Green : ((colorType != 2f) ? Color.Red : Color.Yellow)));
                Color color2 = color;
                color2.A = 50;
                float num4 = ((float)ProjectileID.Sets.TrailCacheLength[base.Type] - num3) / (float)ProjectileID.Sets.TrailCacheLength[base.Type];
                color2 *= num4;
                float scale = base.Projectile.scale / 2f + base.Projectile.scale * num4 / 2f;
                int num5 = (int)num3 - 1;
                if (num5 >= 0)
                {
                    Vector2 vector2 = Vector2.Lerp(base.Projectile.oldPos[(int)num3], base.Projectile.oldPos[num5], 1f - num3 % 1f) + vector / 2f;
                    float rotation = base.Projectile.oldRot[num5];
                    Main.EntitySpriteDraw(value, vector2 - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, color2, rotation, vector, scale, effects);
                }
            }

            Main.EntitySpriteDraw(value, base.Projectile.Center - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, Color.White, base.Projectile.rotation, vector, base.Projectile.scale, effects);
            return false;
        }
    }
}
