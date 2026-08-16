using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantSphereRingP2 : MutantSphereRing, IProjOwnedByBoss<MutantBoss>
    {/*
        float Angle = 0;
        Vector2 direct = Vector2.Zero;
        int flag = 1;
        int turntimer = 0;
        float speed = 14;
        float timer = 0;*/
        double flag = 0;

        //—— 特殊阶段（同轨迹、变速度）——
        public bool specialPhase;   // 阶段标志，由静态方法 EnterSpecialPhase 置位
        private bool pathReady;     // 轨迹预计算完成
        private Vector2[] path;     // 进入时预计算的剩余规范轨迹（path[0] = 进入时位置）
        private float advance;      // 弧长参数，tick 单位（1 tick = originalSpeed 像素）
        private float speedMul;     // 当前速度倍率，0 → MaxMul
        private int phaseTimer;     // 阶段内计时（静止 / 加速用）
        private readonly int HoldFrames = 30;   // 静止帧数（可调）
        private const float Accel = 0.03f;   // 每帧倍率增量（可调）
        private const float MaxMul = 3f;     // 最大倍率（可调）
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(specialPhase);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            specialPhase = reader.ReadBoolean();
        }
        /// <summary>
        /// 让所有存活的 PHMutantSphereRing 进入特殊阶段：保持原轨迹，先静止再加速到最大速度。
        /// MutantBoss 在技能中途调用。幂等：重复调用无副作用。
        /// </summary>
        public static void EnterSpecialPhase()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].ModProjectile is PHMutantSphereRingP2 ring)
                    ring.specialPhase = true;
                Main.projectile[i].netUpdate = true;
            }
        }

        /// <summary>
        /// 与实时移动公式完全一致的逐帧方向步进（快照预计算与实时阶段共用，杜绝轨迹漂移）。
        /// </summary>
        private static Vector2 StepDir(Vector2 dir, float ai0, float ai1, ref double vt, ref double fl)
        {
            vt += 1f;
            fl += 1f;
            double num = fl;
            if (vt % 60 < 20 && vt > 40)
            {
                fl -= 2f;
                num *= -1;
            }
            return Vector2.Normalize(dir.RotatedBy(ai1 / (Math.PI * 2.0 * ai0 * num)));
        }

        public override void SetDefaults()
        {
            base.Projectile.width = 40;
            base.Projectile.height = 40;
            base.Projectile.hostile = true;
            base.Projectile.ignoreWater = true;
            base.Projectile.tileCollide = false;
            base.Projectile.timeLeft = 480;
            base.Projectile.alpha = 200;
            base.CooldownSlot = 1;
            DieOutsideArena = true;
            base.Projectile.FargoSouls().TimeFreezeImmune = WorldSavingSystem.MasochistModeReal && FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<MutantBoss>()) && Main.npc[EModeGlobalNPC.mutantBoss].ai[0] == -5f;
        }
        public override void AI()
        {
            if (!spawned)
            {
                spawned = true;
                originalSpeed = Projectile.velocity.Length();
            }
            if (specialPhase)
            {
                SpecialPhaseAI();
            }
            else
            {
                Projectile.localAI[0] += 1f;
                flag += 1f;
                double num = flag;
                if (Projectile.localAI[0] % 60 < 20 && Projectile.localAI[0] > 40)
                {
                    flag -= 2f;
                    num *= -1;
                }
                Projectile.velocity = originalSpeed * Vector2.Normalize(Projectile.velocity).RotatedBy(Projectile.ai[1] / (Math.PI * 2.0 * Projectile.ai[0] * num));
            }
            #region 其他
            if (base.Projectile.alpha > 0)
            {
                base.Projectile.alpha -= 20;
                if (base.Projectile.alpha < 0)
                {
                    base.Projectile.alpha = 0;
                }
            }

            base.Projectile.scale = 1f - (float)base.Projectile.alpha / 255f;
            if (++base.Projectile.frameCounter >= 6)
            {
                base.Projectile.frameCounter = 0;
                if (++base.Projectile.frame > 1)
                {
                    base.Projectile.frame = 0;
                }
            }

            if (DieOutsideArena)
            {
                if (ritualID == -1)
                {
                    ritualID = -2;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<PHMutantRitual>())
                        {
                            ritualID = i;
                            break;
                        }
                    }
                }

                Projectile projectile = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>());
                if (projectile != null && base.Projectile.Distance(projectile.Center) > 1200f)
                {
                    base.Projectile.timeLeft = 0;
                }
            }

            TryTimeStop();
            #endregion
        }

        /// <summary>
        /// 特殊阶段：首次进入时快照当前状态、预计算剩余规范轨迹；
        /// 之后每帧沿轨迹按弧长参数 advance 采样，速度曲线为 静止 → 加速 → 维持最大。
        /// 到达轨迹终点后保留末速度沿切线直线飞出，直到 timeLeft 归零消失（不会卡在终点）。
        /// 进入后 localAI[0]/flag 冻结，不再走实时公式。
        /// </summary>
        private void SpecialPhaseAI()
        {
            if (!pathReady)
            {
                pathReady = true;
                phaseTimer = 0;
                speedMul = 0f;
                advance = 0f;
                double vt = Projectile.localAI[0];
                double fl = flag;
                Vector2 dir = Vector2.Normalize(Projectile.velocity);
                int steps = Projectile.timeLeft;
                path = new Vector2[steps + 1];
                path[0] = Projectile.Center;
                for (int i = 1; i <= steps; i++)
                {
                    dir = StepDir(dir, Projectile.ai[0], Projectile.ai[1], ref vt, ref fl);
                    path[i] = path[i - 1] + originalSpeed * dir;
                }
            }

            //速度曲线：静止 HoldFrames 帧 → 线性加速至 MaxMul → 维持
            phaseTimer++;
            speedMul = phaseTimer <= HoldFrames ? 0f : Math.Min(speedMul + Accel, MaxMul);

            //已到达/越过轨迹终点：保留当前速度直线飞出，不再采样轨迹，
            //等 timeLeft 自然归零后消失——否则会一直卡在终点（原实现把位置钳在末点）。
            if (advance >= path.Length - 1)
                return;

            advance += speedMul;

            //本帧恰好越过终点：沿终点切线以当前倍率飞出，下一帧起被上方 return 保留该速度
            if (advance >= path.Length - 1)
            {
                Vector2 tan = path[^1] - path[^2];
                if (tan.LengthSquared() < 0.01f)
                    tan = Vector2.UnitY;
                tan.Normalize();
                Projectile.velocity = tan * (speedMul * originalSpeed);
                return;
            }

            int i0 = (int)advance;
            int i1 = Math.Min(i0 + 1, path.Length - 1);
            float frac = Math.Min(advance - i0, 1f);
            Vector2 target = Vector2.Lerp(path[i0], path[i1], frac);
            Vector2 tan2 = path[i1] - path[i0];
            if (tan2.LengthSquared() < 0.01f)
                tan2 = Vector2.UnitY;
            tan2.Normalize();

            //移动沿用 velocity 驱动模型：velocity = 本帧实际位移，引擎每帧将其加到 position
            Vector2 disp = target - Projectile.Center;
            if (disp.LengthSquared() < 0.01f)
                disp = tan2 * 0.05f; //静止时给微小速度，防 PreDraw 的 Normalize(velocity)=NaN
            Projectile.velocity = disp;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            /*
            #region 额外绘制
            Texture2D texture = TextureAssets.Projectile[ModContent.ProjectileType<MutantSphereRing>()].Value;
            int sizeY = texture.Height / Main.projFrames[ModContent.ProjectileType<MutantSphereRing>()];
            int sizeX = texture.Width;

            int frameY = Projectile.frame * sizeY;
            int frameX = sizeX;

            Rectangle rectangle = new(frameX, frameY, sizeX, sizeY);
            Vector2 origin = rectangle.Size() / 2f;

            SpriteEffects spriteEffects = Projectile.spriteDirection > 0 ?
                SpriteEffects.None : SpriteEffects.FlipHorizontally;


            Color color = Color.Aqua;
            // 4. 绘制轨迹拖尾效果
            for (float i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i += 0.33f)
            {
                Color oldColor = color;
                oldColor.A = 50;

                float modifier = (float)(ProjectileID.Sets.TrailCacheLength[ModContent.ProjectileType<MutantSphereRing>()] - i) /
                                 ProjectileID.Sets.TrailCacheLength[ModContent.ProjectileType<MutantSphereRing>()];
                oldColor *= modifier;

                float scale = (Projectile.scale / 1) + (Projectile.scale * modifier / 2);

                int max0 = (int)i - 1;
                if (max0 < 0)
                    continue;

                Vector2 oldPos = Vector2.Lerp(Projectile.oldPos[(int)i],
                    Projectile.oldPos[max0], 1 - i % 1) + (origin / 2);

                // 使用前一个点的旋转角度
                float oldRot = Projectile.oldRot[max0];
                Main.EntitySpriteDraw(texture, oldPos - Main.screenPosition +
                    new Vector2(0f, Projectile.gfxOffY), rectangle, oldColor,
                    oldRot, origin, scale, spriteEffects, 0);
            }

            Asset<Texture2D> line = TextureAssets.Extra[178];
            float opacity = 0.55f; // 预警线透明度

            Main.EntitySpriteDraw(
                line.Value, // 纹理：细长的直线
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                null,
                color * opacity,
            // 通过速度向量的角度确定旋转，使预警线指向弹幕飞行方向
                Angle,

                new Vector2(0, line.Height() * 0.5f),
                new Vector2(0.33f, Projectile.scale * 5),

                SpriteEffects.None
            );
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                rectangle, Color.White,
                Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            #endregion
            */
            #region 原始绘制
            Texture2D value = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/MutantBoss/MutantSphereGlow", AssetRequestMode.ImmediateLoad).Value;
            int height = value.Height;
            int y = 0;
            Rectangle rectangle = new Rectangle(0, y, value.Width, height);
            Vector2 origin = rectangle.Size() / 2f;
            Color color = Color.Lerp(FargoSoulsUtil.AprilFools ? Color.Red : new Color(196, 247, 255, 0), Color.Transparent, 0.9f);
            color *= base.Projectile.Opacity;
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[base.Projectile.type]; i++)
            {
                Color color2 = color;
                color2 *= (float)(ProjectileID.Sets.TrailCacheLength[base.Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[base.Projectile.type];
                float num = base.Projectile.scale * (float)(ProjectileID.Sets.TrailCacheLength[base.Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[base.Projectile.type];
                Vector2 vector = base.Projectile.oldPos[i] - Vector2.Normalize(base.Projectile.velocity) * i * 6f;
                Main.EntitySpriteDraw(value, vector + base.Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, color2, base.Projectile.velocity.ToRotation() + MathF.PI / 2f, origin, num * 1.5f, SpriteEffects.None);
            }

            color = Color.Lerp(new Color(255, 255, 255, 0), Color.Transparent, 0.85f);
            Main.EntitySpriteDraw(value, base.Projectile.position + base.Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, color, base.Projectile.velocity.ToRotation() + MathF.PI / 2f, origin, base.Projectile.scale * 1.5f, SpriteEffects.None);
            return false;
            #endregion
        }

    }
    public class PHMutantSphereRingP1 : MutantSphereRing, IProjOwnedByBoss<MutantBoss>
    {
        //—— 特殊阶段（同轨迹、变速度）——
        public bool specialPhase;   // 阶段标志，由静态方法 EnterSpecialPhase 置位
        private bool pathReady;     // 轨迹预计算完成
        private Vector2[] path;     // 进入时预计算的剩余规范轨迹（path[0] = 进入时位置）
        private float advance;      // 弧长参数，tick 单位（1 tick = originalSpeed 像素）
        private float speedMul;     // 当前速度倍率，0 → MaxMul
        private int phaseTimer;     // 阶段内计时（静止 / 加速用）
        private readonly int HoldFrames = 10;   // 静止帧数（可调）
        private const float Accel = 0.035f;   // 每帧倍率增量（可调）
        private const float MaxMul = 3f;     // 最大倍率（可调）
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(specialPhase);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            specialPhase = reader.ReadBoolean();
        }
        /// <summary>
        /// 让所有存活的 PHMutantSphereRing 进入特殊阶段：保持原轨迹，先静止再加速到最大速度。
        /// MutantBoss 在技能中途调用。幂等：重复调用无副作用。
        /// </summary>
        public static void EnterSpecialPhase()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].ModProjectile is PHMutantSphereRingP1 ring)
                    ring.specialPhase = true;
                Main.projectile[i].netUpdate = true;
            }
        }
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 480;
            Projectile.alpha = 200;
            CooldownSlot = 1;
            DieOutsideArena = true;
            base.Projectile.FargoSouls().TimeFreezeImmune = WorldSavingSystem.MasochistModeReal && FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<MutantBoss>()) && Main.npc[EModeGlobalNPC.mutantBoss].ai[0] == -5f;
        }
        public override void AI()
        {
            if (!spawned)
            {
                spawned = true;
                originalSpeed = Projectile.velocity.Length();
            }
            if (specialPhase)
            {
                SpecialPhaseAI();
            }
            else
            {
                Projectile.localAI[0] += 1f;
                double num = Projectile.localAI[0];
                Projectile.velocity = originalSpeed * Vector2.Normalize(Projectile.velocity).RotatedBy(Projectile.ai[1] / (Math.PI * 2.0 * Projectile.ai[0] * num));
            }
            #region 其他
            if (base.Projectile.alpha > 0)
            {
                base.Projectile.alpha -= 20;
                if (base.Projectile.alpha < 0)
                {
                    base.Projectile.alpha = 0;
                }
            }

            base.Projectile.scale = 1f - (float)base.Projectile.alpha / 255f;
            if (++base.Projectile.frameCounter >= 6)
            {
                base.Projectile.frameCounter = 0;
                if (++base.Projectile.frame > 1)
                {
                    base.Projectile.frame = 0;
                }
            }

            if (DieOutsideArena)
            {
                if (ritualID == -1)
                {
                    ritualID = -2;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<PHMutantRitual>())
                        {
                            ritualID = i;
                            break;
                        }
                    }
                }

                Projectile projectile = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>());
                if (projectile != null && base.Projectile.Distance(projectile.Center) > 1200f)
                {
                    base.Projectile.timeLeft = 0;
                }
            }

            TryTimeStop();
            #endregion
        }
        private void SpecialPhaseAI()
        {
            if (!pathReady)
            {
                pathReady = true;
                phaseTimer = 0;
                speedMul = 0f;
                advance = 0f;
                double vt = Projectile.localAI[0];
                Vector2 dir = Vector2.Normalize(Projectile.velocity);
                int steps = Projectile.timeLeft;
                path = new Vector2[steps + 1];
                path[0] = Projectile.Center;
                for (int i = 1; i <= steps; i++)
                {
                    dir = StepDir(dir, Projectile.ai[0], Projectile.ai[1], ref vt);
                    path[i] = path[i - 1] + originalSpeed * dir;
                }
            }

            //速度曲线：静止 HoldFrames 帧 → 线性加速至 MaxMul → 维持
            phaseTimer++;
            speedMul = phaseTimer <= HoldFrames ? 0f : Math.Min(speedMul + Accel, MaxMul);

            //已到达/越过轨迹终点：保留当前速度直线飞出，不再采样轨迹，
            //等 timeLeft 自然归零后消失——否则会一直卡在终点（原实现把位置钳在末点）。
            if (advance >= path.Length - 1)
                return;

            advance += speedMul;

            //本帧恰好越过终点：沿终点切线以当前倍率飞出，下一帧起被上方 return 保留该速度
            if (advance >= path.Length - 1)
            {
                Vector2 tan = path[^1] - path[^2];
                if (tan.LengthSquared() < 0.01f)
                    tan = Vector2.UnitY;
                tan.Normalize();
                Projectile.velocity = tan * (speedMul * originalSpeed);
                return;
            }

            int i0 = (int)advance;
            int i1 = Math.Min(i0 + 1, path.Length - 1);
            float frac = Math.Min(advance - i0, 1f);
            Vector2 target = Vector2.Lerp(path[i0], path[i1], frac);
            Vector2 tan2 = path[i1] - path[i0];
            if (tan2.LengthSquared() < 0.01f)
                tan2 = Vector2.UnitY;
            tan2.Normalize();

            //移动沿用 velocity 驱动模型：velocity = 本帧实际位移，引擎每帧将其加到 position
            Vector2 disp = target - Projectile.Center;
            if (disp.LengthSquared() < 0.01f)
                disp = tan2 * 0.05f; //静止时给微小速度，防 PreDraw 的 Normalize(velocity)=NaN
            Projectile.velocity = disp;
        }
        private static Vector2 StepDir(Vector2 dir, float ai0, float ai1, ref double vt)
        {
            vt += 1f;
            double num = vt;
            return Vector2.Normalize(dir.RotatedBy(ai1 / (Math.PI * 2.0 * ai0 * num)));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            #region 原始绘制
            Texture2D value = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/MutantBoss/MutantSphereGlow", AssetRequestMode.ImmediateLoad).Value;
            int height = value.Height;
            int y = 0;
            Rectangle rectangle = new Rectangle(0, y, value.Width, height);
            Vector2 origin = rectangle.Size() / 2f;
            Color ac = FargoSoulsUtil.AprilFools ? Color.Red : new Color(196, 247, 255, 0);
            float prog = 0.9f;
            if (specialPhase)
            {
                ac = FargoSoulsUtil.AprilFools ? Color.Purple : new Color(138, 177, 255, 0);
                prog = 0.4f;
            }
            Color color = Color.Lerp(ac, Color.Transparent, prog);
            color *= base.Projectile.Opacity;
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[base.Projectile.type]; i++)
            {
                Color color2 = color;
                color2 *= (float)(ProjectileID.Sets.TrailCacheLength[base.Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[base.Projectile.type];
                float num = base.Projectile.scale * (float)(ProjectileID.Sets.TrailCacheLength[base.Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[base.Projectile.type];
                Vector2 vector = base.Projectile.oldPos[i] - Vector2.Normalize(base.Projectile.velocity) * i * 6f;
                Main.EntitySpriteDraw(value, vector + base.Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, color2, base.Projectile.velocity.ToRotation() + MathF.PI / 2f, origin, num * 1.5f, SpriteEffects.None);
            }

            color = Color.Lerp(new Color(255, 255, 255, 0), Color.Transparent, 0.85f);
            Main.EntitySpriteDraw(value, base.Projectile.position + base.Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, color, base.Projectile.velocity.ToRotation() + MathF.PI / 2f, origin, base.Projectile.scale * 1.5f, SpriteEffects.None);
            return false;
            #endregion
        }
    }
}
