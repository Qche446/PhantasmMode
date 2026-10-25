using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.CursedCoffin;

namespace FargosPhantasmMode.Content.Bosses.Coffin;
//ai0加速度，ai1为加速角度
public class P_CoffinDarkSouls : CoffinDarkSouls
{
    public override string Texture => "FargowiltasSouls/Content/Bosses/CursedCoffin/CoffinDarkSouls";
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

        if (Projectile.localAI[0] < 25)
        {
            Projectile.localAI[0]++;
            Projectile.scale = MathHelper.Lerp(0.15f, 1, Projectile.localAI[0] / 25);
        }

        for (int i = 0; i < oldFrame.Length; i++)
        {
            int j = oldFrame.Length - i - 1;
            if (j > 0)
                oldFrame[j] = oldFrame[j - 1];
            else
                oldFrame[j] = Projectile.frame;
        }

        Projectile.Animate(6);

        if (Projectile.ai[1] != 0)
            Projectile.velocity += Projectile.ai[1].ToRotationVector2() * Projectile.ai[0];
        if (Projectile.timeLeft <= 20)
        {
            Projectile.Opacity -= 1 / 20f;
            Projectile.scale -= 1 / 20f;
        }
    }
}
