using Monochrome.Common.MonoUtil;

namespace FargosPhantasmMode.Content.Bosses.Coffin;
/// <summary>
/// 半灵冲刺时撒的弹幕：沿用 CoffinWaveShot 的贴图与拖尾，先原地悬一小会儿，再朝玩家逐渐加速撞出去。
/// ai0 加速方向，ai1 目标玩家，ai2 计时
/// </summary>
public class P_CoffinSpiritWaveShot : P_CoffinWaveShot
{
    ref float Aim => ref Projectile.ai[0];
    ref float Target => ref Projectile.ai[1];
    ref float ChargeTimer => ref Projectile.ai[2];
    public override void SetDefaults()
    {
        base.SetDefaults();
        Projectile.timeLeft = 60 * 5;
        Projectile.scale = 1.25f;
    }
    public override void AI()
    {
        if (Projectile.localAI[0] < 12) //出场放大
        {
            Projectile.localAI[0]++;
            Projectile.scale = MathHelper.Lerp(0, 1, Projectile.localAI[0] / 12);
        }
        ChargeTimer++;
        if (ChargeTimer < 20) //先悬在原地
            Projectile.velocity = Vector2.Zero;
        else
        {
            if (ChargeTimer == 20)
                Aim = Projectile.Center.SafeDirectionTo(Main.player[(int)Target].Center).ToRotation();
            Projectile.velocity = Aim.ToRotationVector2() * Math.Min(Projectile.velocity.Length() + 0.5f, 16f);
        }
        EnsureTrail();
    }
}
