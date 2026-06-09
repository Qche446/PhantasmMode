using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.Lifelight;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Boss;
using Luminance.Common.Utilities;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class MutantLifeNuke : LifeNuke
    {
        public override void SetDefaults()
        {
            base.Projectile.width = 24;
            base.Projectile.height = 24;
            base.Projectile.aiStyle = -1;
            base.Projectile.hostile = true;
            base.Projectile.penetrate = 1;
            base.Projectile.tileCollide = false;
            base.Projectile.ignoreWater = true;
            base.Projectile.timeLeft = 60;
            base.Projectile.scale = 1f;
            base.Projectile.Opacity = 0.5f;
        }
        public override string Texture => "FargowiltasSouls/Content/Bosses/Lifelight/LifeNuke";
        public override void AI()
        {

            if (Projectile.timeLeft < 20)
            {
                float interpolant = 1f - (Projectile.timeLeft / 20f);
                Projectile.position -= Projectile.velocity * interpolant;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 3f, 0.1f);
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 1f, 0.1f);
            }

            Projectile.rotation += Projectile.velocity.Length() * 0.075f * Math.Sign(Projectile.velocity.X);
            Projectile.alpha = (int)(150 * Math.Sin(++Projectile.localAI[0] / 3));


            /*
            for (int i = 0; i < 4; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch, Scale: 3f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.5f;
            }
            */
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<SmiteBuff>(), 60 * 3);
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 600);
            }
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            int max = (int)Projectile.ai[0];
            for (int i = 0; i < max; i++)
            {
                float rad = MathHelper.TwoPi / max * i + Projectile.velocity.ToRotation();
                int damage = Projectile.damage;
                int knockBack = 3;
                float speed = 0.4f;
                if (Projectile.ai[2] != 0)
                    speed *= Projectile.ai[2];
                Vector2 vector = Projectile.velocity.RotatedBy(rad) * speed;
                if (FargoSoulsUtil.HostCheck)
                {
                    int type = ModContent.ProjectileType<MutantLifeProjSmall>();
                    float ai0 = 0;
                    float ai1 = 0;
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vector, type, damage, knockBack, Main.myPlayer, ai0, ai1);
                    if (p != Main.maxProjectiles)
                    {
                        Main.projectile[p].hostile = true;
                        Main.projectile[p].friendly = false;
                    }
                }
            }


            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.GemDiamond, 0f, 0f, 100, default, 3f);
                Main.dust[dust].velocity *= 1.4f;
                Main.dust[dust].noGravity = true;
            }

            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width,
                    Projectile.height, DustID.GemDiamond, 0f, 0f, 100, default, 3.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 7f;
                dust = Dust.NewDust(Projectile.position, Projectile.width,
                    Projectile.height, DustID.GemDiamond, 0f, 0f, 100, default, 1.5f);
                Main.dust[dust].velocity *= 3f;
            }

            float scaleFactor9 = 0.5f;
            for (int j = 0; j < 4; j++)
            {
                int gore = Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.Center,
                    default,
                    Main.rand.Next(61, 64));

                Main.gore[gore].velocity *= scaleFactor9;
                Main.gore[gore].velocity += new Vector2(1f, 1f).RotatedBy(MathHelper.TwoPi / 4 * j);
            }
        }
    }
    public class MutantLifeProjSmall : LifeProjSmall
    {
        int ritualID = -1;
        public override string Texture => "FargowiltasSouls/Content/Bosses/Lifelight/LifeProjSmall";
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathF.PI / 2f;
            if ((Projectile.ai[0] += 1f) > 600f)
            {
                Projectile.Kill();
            }

            if (Projectile.ai[0] == 45f || Projectile.ai[0] == 90f)
            {
                if (Projectile.ai[1] == 3f)
                {
                    Projectile.velocity = Projectile.velocity.RotatedBy(-Math.PI / 3.0);
                }

                if (Projectile.ai[1] == 4f)
                {
                    Projectile.velocity = Projectile.velocity.RotatedBy(Math.PI / 3.0);
                }
            }

            if (!Projectile.tileCollide && Projectile.ai[0] > (float)(60 * Projectile.MaxUpdates) && Projectile.ai[1] < 3f)
            {
                Tile tileSafely = Framing.GetTileSafely(Projectile.Center);
                if (!tileSafely.HasUnactuatedTile || !Main.tileSolid[tileSafely.TileType] || Main.tileSolidTop[tileSafely.TileType])
                {
                    Projectile.tileCollide = true;
                }
            }
            if (ritualID == -1) //identify the ritual CLIENT SIDE
            {
                ritualID = -2; //if cant find it, give up and dont try every tick

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<PHMutantRitual>())
                    {
                        ritualID = i;
                        break;
                    }
                }
            }

            Projectile ritual = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>());
            if (ritual != null && Projectile.Distance(ritual.Center) > 1200f) //despawn faster
                Projectile.timeLeft = 0;
        }
    }
}
