using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Projectiles.Masomode;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// ai0 : whoaii, ai1 :movetype, ai2 : Color
    /// </summary>
    public class TwinFakeLaser : MechElectricOrb, IPixelatedPrimitiveRenderer
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/MechElectricOrb";
        public const int Normal = 0;
        public const int Track = 1;
        public const int Poly = 2;
        public int MoveType { get => (int)Projectile.ai[1];}
        public float Timer{ get => (int)Projectile.localAI[2]; set => Projectile.localAI[2] = value; }
        private Vector2[] oldPosi = new Vector2[30];
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = 1200;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 99;
        }
        public override void AI()
        {
            NPC npc = FargoSoulsUtil.NPCExists((int)Projectile.ai[0], NPCID.Retinazer, NPCID.Spazmatism);
            if (npc == null || npc.active == false) 
                return;
            Player player = Main.player[npc.target];
            switch (MoveType)
            {
                case Normal:
                    break;
                case Track:
                    if (Timer < 300)
                    {
                        Vector2 dir = player.Center - Projectile.Center;
                        float length = dir.Length();
                        Vector2 acc =  20 * dir / length / length;
                        Projectile.velocity += acc;
                    }
                    break;
                case Poly:
                    break;
            }
            if (Timer % 50 == 0)
            {
                for (int i = oldPosi.Length - 1; i > 0; i--)
                {
                    oldPosi[i] = oldPosi[i - 1];
                }
                oldPosi[0] = Projectile.Center;
            }
            Timer++;
            /*
            for (int j = -3; j <= 3; j++)
            {
                Vector2 particleVel = (Projectile.velocity * 1.1f).RotatedBy(MathHelper.PiOver2 * 0.075f * j)
                    .RotatedByRandom(MathHelper.PiOver2 * 0.04f) * Main.rand.NextFloat(0.8f, 1.2f);
                Particle p = new SparkParticle(P_Retinazer.ShootPos(npc),
                    particleVel, npc.type == NPCID.Retinazer ? Color.Yellow : Color.Green, Main.rand.NextFloat(0.7f, 1f), 40);
                p.Spawn();
            }
            */
            if (Projectile.IsFinalExtraUpdate())
                base.AI();
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Ichor, 300);
            target.AddBuff(BuffID.Burning, 120);
            target.AddBuff(BuffID.OnFire, 120);
        }
        public override bool PreDraw(ref Color lightColor) => false;
        public float WidthFunction(float ratio) => Projectile.width * Projectile.scale;
        public static Color ColorFunction(float ratio)
        {
            Color color = Color.Red;
            color.A = 0;
            return color * (1f - ratio);
        }
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            if (Projectile.IsFinalExtraUpdate())
            {
                ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.RetinazerDeathray");
                // 设置着色器参数
                shader.TrySetParameter("mainColor", new Color(240, 220, 240, 0));
                FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedThinGlowStreak.Value);
                shader.TrySetParameter("stretchAmount", 0.5);
                shader.TrySetParameter("scrollSpeed", 4f);
                shader.TrySetParameter("uColorFadeScaler", 0.8f);
                shader.TrySetParameter("useFadeIn", true);

                // 以像素化方式渲染轨迹
                PrimitiveRenderer.RenderTrail(oldPosi, new(WidthFunction, ColorFunction, Pixelate: true, Shader: shader), 60);
            }
        }
    }
    
}
