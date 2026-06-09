using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls;
using FargowiltasSouls.Common.Graphics.Particles;
using Luminance.Core.Graphics;
using FargowiltasSouls.Content.Projectiles.Masomode;
using System.Collections.Generic;
namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Destroyer
{
    public class Lightning : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.CultistBossLightningOrbArc}";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30; // ★ 改为30
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        public float colorlerp;
        public bool playedsound = false;
        public float startSpeed;
        public int BranchDepth => (int)Projectile.localAI[2];
        public int BranchCount
        {
            get => (int)Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 0.5f;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.alpha = 100;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 120 * (Projectile.extraUpdates + 1);
            Projectile.penetrate = 1;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 20;
            Projectile.FargoSouls().noInteractionWithNPCImmunityFrames = true;
        }
        public override void AI()
        {
            if (Main.rand.NextBool(5))
            {
                Particle spark = new SparkParticle(Projectile.Center + Vector2.UnitY * (100 - Main.rand.Next(30, 300)), -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2 * 0.2f) * Main.rand.NextFloat(3, 13), Color.Cyan, Main.rand.NextFloat(0.3f, 0.7f), Main.rand.Next(10, 25));
                spark.Spawn();
            }
            if (!playedsound)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f, Pitch = -0.5f }, Projectile.Center);
                playedsound = true;
                startSpeed = Projectile.velocity.Length();
                if (startSpeed < 35f)
                {
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 35f;
                    startSpeed = 35f;
                }
                if (BranchDepth > 0)
                {
                    Projectile.localAI[0] = Main.rand.NextFloat(-0.5f, 0.5f);
                }
            }
            if (Projectile.velocity != Vector2.Zero && BranchDepth < 2 && Main.rand.NextBool(2) && BranchCount < 4)
            {
                // ★ 分支角度偏移放宽到 ±1.3 弧度（约 74.5°）
                float branchAngleOffset = Main.rand.NextFloat(-1.3f, 1.3f);
                Vector2 branchDir = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(branchAngleOffset);
                float branchSpeed = Math.Max(startSpeed * Main.rand.NextFloat(0.6f, 0.9f), 20f);
                int damage = Projectile.damage / 2;
                if (damage < 1) damage = 1;
                float branchInitialAngle = branchDir.ToRotation();
                int branchTimeLeft = 30 * (Projectile.extraUpdates + 1);
                int proj = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    branchDir * branchSpeed,
                    Projectile.type,
                    damage,
                    Projectile.knockBack / 2f,
                    Projectile.owner,
                    branchInitialAngle,
                    Main.rand.Next(1000),
                    BranchCount + 1
                );
                if (proj != Main.maxProjectiles)
                {
                    Projectile branchProj = Main.projectile[proj];
                    branchProj.localAI[2] = BranchDepth + 1;
                    branchProj.timeLeft = branchTimeLeft;
                    branchProj.localAI[0] = Main.rand.NextFloat(-0.5f, 0.5f);
                }
                BranchCount++;
            }
            // ★ 核心飞行逻辑（稳定弯曲，无静止）
            if (Projectile.velocity != Vector2.Zero && Projectile.velocity.Length() >= 3f)
            {
                float speed = Projectile.velocity.Length();
                float maxTurn = 0.15f + 0.02f * (speed / startSpeed);
                float turn = Main.rand.NextFloat(-maxTurn, maxTurn);
                Vector2 newDir = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(turn);
                Projectile.velocity = newDir * speed;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            else if (Projectile.velocity == Vector2.Zero)
            {
                // 撞墙静止粒子（可保留原代码）
                if (Projectile.frameCounter >= Projectile.extraUpdates * 2)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, 0f, 0f, 0, default, 1f);
                    }
                }
            }
        }
        // 以下方法保持不变：Colliding, OnKill, OnHitNPC, GetAlpha, PreDraw（下面提供新版），OnTileCollide
        // ...
        public override void OnKill(int timeLeft)
        {
            
            base.OnKill(timeLeft);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item62);
            if (FargoSoulsUtil.HostCheck && Main.getGoodWorld)
            {
                Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<LightningExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                if (target.type == NPCID.TheDestroyer ||target.type == NPCID.TheDestroyerBody ||target.type == NPCID.TheDestroyerTail && Main.rand.NextBool(2))
                {
                    int p =Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center,
                        Main.rand.NextFloat(4,8) * Vector2.UnitY, ModContent.ProjectileType<MechElectricOrb>(), Projectile.damage, 0f,
                        Main.myPlayer,ai2: MechElectricOrb.Blue);
                    Main.projectile[p].timeLeft -= 120;
                }
            }
            Projectile.friendly = false;
            Projectile.hostile = false;
            target.AddBuff(BuffID.Electrified, 120);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.friendly = false;
            Projectile.hostile = false;
            target.AddBuff(BuffID.Electrified, 120);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Rectangle rectangle = texture2D13.Bounds;
            Vector2 origin2 = rectangle.Size() / 2f;
            Color baseColor = Projectile.GetAlpha(lightColor);
            int trailLength = ProjectileID.Sets.TrailCacheLength[Projectile.type]; // 30
            for (int i = 1; i < trailLength; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Projectile.oldPos[i])
                    continue;
                Vector2 offset = Projectile.oldPos[i - 1] - Projectile.oldPos[i];
                int length = (int)offset.Length();
                // ★ 自定义衰减：让尾端更持久
                float alphaFactor = 0.9f - (i / (float)trailLength) * 0.4f; // 0.6 ~ 0.2
                float scaleFactor = 0.9f - (i / (float)trailLength) * 0.6f; // 0.9 ~ 0.3
                //Color drawColor = baseColor * alphaFactor;
                Color drawColor = Color.CadetBlue * alphaFactor;
                float drawScale = Projectile.scale * scaleFactor;
                offset.Normalize();
                const int step = 3;
                for (int j = 0; j < length; j += step)
                {
                    Vector2 pos = Projectile.oldPos[i] + offset * j;
                    Main.EntitySpriteDraw(texture2D13,
                        pos + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY),
                        new Rectangle?(rectangle),
                        drawColor,
                        Projectile.rotation,
                        origin2,
                        drawScale,
                        SpriteEffects.FlipHorizontally,
                        0);
                }
            }
            return false;
        }
        
        // 其他方法保留原样，包括 OnTileCollide 等
    }
}