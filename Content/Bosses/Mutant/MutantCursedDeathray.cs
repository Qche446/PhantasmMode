using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class MutantCursedDeathray : BaseDeathray, IProjOwnedByBoss<MutantBoss>
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Deathrays/CursedDeathray";
        public MutantCursedDeathray() : base(180, 0.5f) { } // 改为180帧
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }
        public override void AI()
        {
            // 重写后的AI：绑定MutantBoss，水平光柱，从下方2400移至600
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
                Projectile.velocity = Vector2.UnitX;
            // 查找MutantBoss
            NPC npc = FargoSoulsUtil.NPCExists(Projectile.ai[0], ModContent.NPCType<MutantBoss>());
            if (npc == null)
            {
                Projectile.Kill();
                return;
            }
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] >= maxTime)
            {
                Projectile.Kill();
                return;
            }
            // 从2400线性移动到600
            float startDist = 2400f;
            float endDist = 600f;
            float progress = Projectile.localAI[0] / maxTime;
            float currentDist = MathHelper.Lerp(startDist, endDist, progress);
            // 光柱中心位于NPC正下方currentDist像素处
            Projectile.Center = new Vector2(npc.Center.X - 1500, npc.Center.Y + currentDist);
            // 固定水平向右的速度
            Projectile.velocity = Vector2.UnitX;

            if (Projectile.localAI[0] % 4 == 0)
            {
                Vector2 spawnPos = new(npc.Center.X, npc.Center.Y + currentDist);

                SoundEngine.PlaySound(SoundID.Item34, spawnPos);

                const int offsetX = 800;
                const int speed = 14;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Vector2.UnitX * offsetX, Vector2.UnitX * -speed, ModContent.ProjectileType<MutantCursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Vector2.UnitX * offsetX / 2, Vector2.UnitX * speed, ModContent.ProjectileType<MutantCursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Vector2.UnitX * -offsetX / 2, Vector2.UnitX * -speed, ModContent.ProjectileType<MutantCursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Vector2.UnitX * -offsetX, Vector2.UnitX * speed, ModContent.ProjectileType<MutantCursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                }
            }


            // 缩放：淡入淡出效果
            float num801 = 0.2f;
            Projectile.scale = (float)Math.Sin(Projectile.localAI[0] * Math.PI / maxTime) * 10f * num801;
            if (Projectile.scale > num801)
                Projectile.scale = num801;
            // 旋转对准速度方向
            float num804 = Projectile.velocity.ToRotation();
            Projectile.rotation = num804 - MathHelper.PiOver2;
            Projectile.velocity = num804.ToRotationVector2();
            // 激光长度计算（固定3000）
            float num805 = 3f;
            float num806 = Projectile.width;
            Vector2 samplingPoint = Projectile.Center;
            float[] array3 = new float[(int)num805];
            for (int i = 0; i < array3.Length; i++)
                array3[i] = 3000f;
            float num807 = 0f;
            int num3;
            for (int num808 = 0; num808 < array3.Length; num808 = num3 + 1)
            {
                num807 += array3[num808];
                num3 = num808;
            }
            num807 /= num805;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], num807, 0.5f);
            Vector2 vector79 = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14f);
            // 粒子特效
            for (int num809 = 0; num809 < 2; num809 = num3 + 1)
            {
                float num810 = Projectile.velocity.ToRotation() + (Main.rand.NextBool(2) ? -1f : 1f) * 1.57079637f;
                float num811 = (float)Main.rand.NextDouble() * 2f + 2f;
                Vector2 vector80 = new((float)Math.Cos(num810) * num811, (float)Math.Sin(num810) * num811);
                int num812 = Dust.NewDust(vector79, 0, 0, DustID.CopperCoin, vector80.X, vector80.Y, 0, default, 1f);
                Main.dust[num812].noGravity = true;
                Main.dust[num812].scale = 1.7f;
                num3 = num809;
            }
            if (Main.rand.NextBool(5))
            {
                Vector2 value29 = Projectile.velocity.RotatedBy(1.5707963705062866, default) * ((float)Main.rand.NextDouble() - 0.5f) * Projectile.width;
                int num813 = Dust.NewDust(vector79 + value29 - Vector2.One * 4f, 8, 8, DustID.CopperCoin, 0f, 0f, 100, default, 1.5f);
                Dust dust = Main.dust[num813];
                dust.velocity *= 0.5f;
                Main.dust[num813].velocity.Y = -Math.Abs(Main.dust[num813].velocity.Y);
            }
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;//隐藏
        }
    }
    public class MutantCursedFlamethrower : ModProjectile, IProjOwnedByBoss<MutantBoss>
    {
        public override string Texture => "Terraria/Images/Projectile_101";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Eye Fire");
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.EyeFire); //has 4 updates per tick
            AIType = ProjectileID.EyeFire;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.tileCollide = false;
            Projectile.width = 20;
            Projectile.height = 400;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(6))
                target.AddBuff(BuffID.CursedInferno, 480, true);
            else if (Main.rand.NextBool(4))
                target.AddBuff(BuffID.CursedInferno, 480, true);
            else if (Main.rand.NextBool())
                target.AddBuff(BuffID.CursedInferno, 480, true);

            target.AddBuff(BuffID.OnFire, 300);
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 600);
            }
            
            /*target.AddBuff(ModContent.BuffType<ClippedWings>(), 180);
            target.AddBuff(ModContent.BuffType<Crippled>(), 60);*/
        }
    }
}
