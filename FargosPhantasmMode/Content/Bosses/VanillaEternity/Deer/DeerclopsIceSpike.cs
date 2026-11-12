using FargowiltasSouls.Content.Buffs.Masomode;
using Terraria.GameContent;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Deer;

public class DeerclopsIceSpike : ModProjectile
{
    const float Mutl = 0.76f;
    //ai[2]分裂样式
    public int Style => Convert.ToInt32(Projectile.ai[2]);
    /// <summary>不分裂</summary>
    public const int SplitNone = 0;
    /// <summary>指向原方向的三叉分裂</summary>
    public const int SplitFork = 1;
    /// <summary>靠向水平方向(插值0.75f)的三叉分裂</summary>
    public const int SplitFork_Flat1 = 2;
    /// <summary>指向水平方向的三叉分裂</summary>
    public const int SplitFork_Flat2 = 3;
    /// <summary>右侧连续线性出刺</summary>
    public const int SplitComb_R = 4;
    /// <summary>左侧连续线性出刺</summary>
    public const int SplitComb_L = 5;
    /// <summary>两侧连续线性出刺</summary>
    public const int SplitComb_All = 6;
    public const int SplitRotate_L = 7;
    public const int SplitRotate_R = 8;
    public const int SplitRotate_None = 9;
    //Deer SpikeTime1
    public const int D1_1 = 20;
    public const int D1_2 = 21;
    public const int D1_3 = 22;
    public const int D1_3_1 = 23;
    public const int D1_3_2 = 24;
    //SpikeTime2
    public const int D2_1 = 30;
    public const int D2_2 = 31;
    public const int D2_2_1 = 32;
    public const int D2_2_2 = 33;
    public const int D2_2_3 = 34;
    public const int D2_3_1 = 35;
    public const int D2_3_2 = 36;
    public const int D2_3_3 = 37;
    public const int Dp2_1 = 40;
    public const int Dp2_2 = 41;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeerclopsIceSpike;

    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 32;
        Projectile.hostile = true;
        Projectile.alpha = 255;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.coldDamage = true;
    }

    //ai[0]生长帧(可为负,越负存在越久), ai[1]体积倍率, ai[2]分裂样式
    public override void AI()
    {
        bool growing = Projectile.ai[0] < 10f;
        bool fading = Projectile.ai[0] >= 10f;
        bool dying = Projectile.ai[0] >= 20f;
        Projectile.ai[0] += 1f;
        if (Projectile.localAI[0] == 0f)
        {
            Projectile.localAI[0] = 1f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frame = Main.rand.Next(5);
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.Cloud, Projectile.velocity * 0.75f * MathHelper.Lerp(0.2f, 0.7f, Main.rand.NextFloat()));
                dust.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                dust.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
            }
            for (int j = 0; j < 5; j++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.Cloud, Main.rand.NextVector2Circular(2f, 2f) + Projectile.velocity * 0.75f * MathHelper.Lerp(0.2f, 0.5f, Main.rand.NextFloat()));
                dust.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                dust.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
                dust.fadeIn = 1f;
            }
            SoundEngine.PlaySound(SoundID.DeerclopsIceAttack, Projectile.Center);
        }
        Projectile.localAI[1] += 1f;
        //额外的分裂逻辑
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            switch (Style)
            {
                default: break;
                case SplitFork: 
                    ThreeSplit(Projectile.velocity, MonoUtil.Pi * 0.325f, 1.35f, static _ => SplitNone);
                    break;
                case SplitFork_Flat1: 
                    ThreeSplit(Vector2.Lerp(dir, Vector2.UnitX * Math.Sign(dir.X), 0.75f), MonoUtil.PiOver6, 1.35f, static _ => SplitNone);
                    break;
                case SplitFork_Flat2: 
                    ThreeSplit(Vector2.UnitX * Math.Sign(dir.X), MonoUtil.PiOver6, 1.35f, static _ => SplitNone);
                    break;
                case SplitComb_R:
                    CombSplit(false, true);
                    break;
                case SplitComb_L:
                    CombSplit(true, false);
                    break;
                case SplitComb_All:
                    CombSplit(true, true);
                    break;
                case SplitRotate_L:
                    Rotate(1.3f, -MonoUtil.PiOver4);
                    break;
                case SplitRotate_R:
                    Rotate(1.3f, MonoUtil.PiOver4);
                    break;
                case SplitRotate_None:
                    Rotate(1.3f, 0f);
                    break;
                //开鹿
                //IceSpikeTime == 0
                case D1_1:
                    ThreeSplit(Vector2.UnitX * Math.Sign(dir.X), MonoUtil.Pi * 0.325f, 1.35f, static _ => D1_2);
                    break;
                case D1_2:
                    ThreeSplit(Vector2.Lerp(dir, Vector2.UnitX * Math.Sign(dir.X), 0.75f), MonoUtil.PiOver6, 1.3f, 
                        static i => i switch { -1 => D1_3_1, 1 => D1_3_2, _ => SplitNone });
                    break;
                case D1_3_1:
                    Rotate(1.3f, MonoUtil.PiOver3, SplitComb_L);
                    break;
                case D1_3_2:
                    Rotate(1.3f, -MonoUtil.PiOver3, SplitComb_R);
                    break;
                //IceSpikeTime == 2
                case D2_1:
                    Mirror(1.3f, static i => i switch { -1 => D2_2_1, 0 => D2_2_2, 1 => D2_2_3, _ => SplitNone });
                    break;
                case D2_2_1:
                    ThreeSplit(Vector2.UnitX * Math.Sign(dir.X), MonoUtil.PiOver6, 1.35f, static _ => SplitNone);
                    break;
                case D2_2_2:
                    ThreeSplit(Vector2.Lerp(dir, Vector2.UnitY * Math.Sign(dir.Y), 0.75f), MonoUtil.PiOver6, 1.35f, AngleChoose(1));
                    break;
                case D2_2_3:
                    ThreeSplit(dir, MonoUtil.Pi * 0.325f, 1.35f, static _ => SplitComb_All);
                    break;
                case Dp2_1:
                    Mirror(1.3f, static _ => SplitFork_Flat2);
                    break;
            }
        }
        if (growing)
        {
            Projectile.Opacity += 0.1f;
            Projectile.scale = Projectile.Opacity * Projectile.ai[1];
        }
        if (fading)
            Projectile.Opacity -= 0.2f;
        if (dying)
            Projectile.Kill();
    }
    #region ai辅助方法
    private void ThreeSplit(Vector2 centerDirect, float angleOffset, float scale, Func<int, int> nextAi2)
    {
        if (Projectile.localAI[1] == 2)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            for (int i = -1; i <= 1; i++)
            {
                Vector2 vel = centerDirect.RotatedBy(i * angleOffset).SafeNormalize(-Vector2.UnitY);
                float ai2 = nextAi2(i);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + dir * 200f * Projectile.ai[1] * Mutl, vel, Type, Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.ai[0], scale, ai2);
            }
        }
    }
    private void CombSplit(bool canLeft, bool canRight, int trailnextAi2 = SplitNone)
    {
        if (Projectile.localAI[1] <= 20f)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            const float spacing = 16f;
            int total = (int)(200f * Projectile.ai[1] / spacing);
            if (total > 0)
            {
                int spawned = (int)Projectile.localAI[2];
                float interval = 20f / total;
                while (spawned < total && Projectile.localAI[1] >= spawned * interval)
                {
                    int left = 1;
                    int right = -1;
                    if (canLeft)
                        left = -1;
                    if (canRight)
                        right = 1;
                    for (int j = left; j <= right; j += 2)
                    {
                        float Angle = MathHelper.PiOver2 - spawned * 0.7f * (MathHelper.PiOver4 / total);
                        Angle *= j;
                        int ai2 = SplitNone;
                        if (spawned == total - 1)
                        {
                            ai2 = trailnextAi2;
                        }
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + dir * (spawned * spacing),
                            dir.RotatedBy(Angle), Type, Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.ai[0], 0.1f + Main.rand.NextFloat() * 0.1f + spawned * 1.1f / total, ai2);
                    }
                    spawned++;
                }
                Projectile.localAI[2] = spawned;
            }
        }
    }
    private void Rotate(float scale, float angle, int nextAi2 = SplitNone)
    {
        if (Projectile.localAI[1] == 2)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + dir * 200f * Projectile.ai[1] * Mutl, dir.RotatedBy(angle), Type, Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.ai[0], scale, nextAi2);
        }
    }
    private void Mirror(float scale, Func<int, int> nextAi2)
    {
        if (Projectile.localAI[1] == 2)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + dir * 200f * Projectile.ai[1] * Mutl, new Vector2(-dir.X, dir.Y), Type, Projectile.damage, Projectile.knockBack, Projectile.owner,
                Projectile.ai[0], scale, nextAi2(-1));
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + dir * 200f * Projectile.ai[1] * Mutl, dir, Type, Projectile.damage, Projectile.knockBack, Projectile.owner, 
                Projectile.ai[0], scale, nextAi2(0));
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, new Vector2(-dir.X, dir.Y), Type, Projectile.damage, Projectile.knockBack, Projectile.owner,
                Projectile.ai[0], scale, nextAi2(1));
        }
    }
    /// <summary>
    /// -1为内夹，1为外绽
    /// </summary>
    private static Func<int, int> AngleChoose(int style)
    {
        return style switch
        {
            1 => i => i switch { -1 => SplitRotate_L, 0 => SplitRotate_None, 1 => SplitRotate_R, _ => SplitNone },
            -1 => i => i switch { -1 => SplitRotate_R, 0 => SplitRotate_None, 1 => SplitRotate_L, _ => SplitNone },
            _ => i => SplitNone
        };
    }
    #endregion

    public override bool ShouldUpdatePosition() => false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collisionPoint = 0f;
        Vector2 tip = Projectile.Center + Projectile.velocity.SafeNormalize(-Vector2.UnitY) * 200f * Projectile.scale;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, tip, 22f * Projectile.scale, ref collisionPoint);
    }

    public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.Black, 0.25f);

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
        Rectangle frame = texture.Frame(1, 5, 0, Projectile.frame);
        Vector2 origin = new(16f, frame.Height / 2f);
        Vector2 scale = new(Projectile.scale);
        scale.Y *= Utils.GetLerpValue(30f, 25f, Projectile.ai[0], true);
        Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);

        Vector4 lightVec = lightColor.ToVector4();
        Vector4 shadeVec = new Color(67, 17, 17).ToVector4() * lightVec;
        Main.EntitySpriteDraw(TextureAssets.Extra[ExtrasID.SharpTears].Value, drawPos - Projectile.velocity * Projectile.scale * 0.5f, null, Projectile.GetAlpha(new Color(shadeVec.X, shadeVec.Y, shadeVec.Z, shadeVec.W)), Projectile.rotation + MathHelper.PiOver2, TextureAssets.Extra[ExtrasID.SharpTears].Value.Size() / 2f, Projectile.scale * 0.9f, SpriteEffects.None);

        Color glow = Projectile.GetAlpha(Color.White) * Utils.Remap(Projectile.ai[0], 0f, 20f, 0.5f, 0f);
        glow.A = 0;
        for (int i = 0; i < 4; i++)
            Main.EntitySpriteDraw(texture, drawPos + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2 * i) * 2f * scale, frame, glow, Projectile.rotation, origin, scale, SpriteEffects.None);

        Main.EntitySpriteDraw(texture, drawPos, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, scale, SpriteEffects.None);
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        for (float t = 0f; t < 1f; t += 0.25f)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 16f) * Projectile.scale + Projectile.velocity.SafeNormalize(Vector2.UnitY) * t * 200f * Projectile.scale, DustID.Cloud, Main.rand.NextVector2Circular(3f, 3f));
            dust.velocity.Y += -0.3f;
            dust.velocity += Projectile.velocity * 0.2f;
            dust.scale = 1f;
            dust.alpha = 100;
        }
    }
    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
    {
        target.longInvince = true;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(BuffID.Frostburn, 90);
        target.AddBuff(ModContent.BuffType<MarkedforDeathBuff>(), 900);
        target.AddBuff(ModContent.BuffType<HypothermiaBuff>(), 1200);
    }
}
