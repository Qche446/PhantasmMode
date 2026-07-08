using FargowiltasSouls;
using FargowiltasSouls.Core.Globals;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// ai0 = 咒火角度，ai[1] = 个数, ai[2] = 计时器
    /// </summary>
    public class CursedFireWheel : ModProjectile
    {
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public ref float Count => ref Projectile.ai[1];
        public ref float Timer => ref Projectile.ai[2];
        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.scale = 1f;
            Projectile.timeLeft = 300;
            Projectile.hide = false;
        }
        public override void AI()
        {
            NPC npc = FargoSoulsUtil.NPCExists(EModeGlobalNPC.spazBoss, NPCID.Spazmatism);
            if (npc == null)
                return;
            if (Timer % 2 == 0)
            {
                for (int i = 0; i < Count; i++)
                {
                    float angle = MathHelper.TwoPi / Count * i + Projectile.ai[0];
                    float speed = MathHelper.SmoothStep(0f, 10f, Timer / 45f) + Main.rand.NextFloat(-1f, 1f);
                    for (int j = 0; j < 3; j++)
                    {
                        Vector2 vel = (angle).ToRotationVector2() * speed;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), Projectile.Center, vel * Main.rand.NextFloat(0.5f, 1f), ProjectileID.EyeFire, Projectile.damage, Projectile.knockBack, Main.myPlayer);
                    }
                }
            }
            Projectile.ai[0] += MathHelper.SmoothStep(0, MathF.PI / 90f, Timer / 45f);
            Timer++;
        }
        public override bool CanHitPlayer(Player target) => false;
    }
}
