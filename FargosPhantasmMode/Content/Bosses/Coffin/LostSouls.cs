using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.WorldGeneration;
using Monochrome.Core.Graphics;
using System.Collections.Generic;

namespace FargosPhantasmMode.Content.Bosses.Coffin;

/// <summary>
/// 棺材放出的迷失灵魂。ai0 = 横扫高度（世界 Y），ai1 = 外侧落点抖动（px）。
/// 轨迹：上冒并放大 → 飞出本侧房间外 → 掉头横扫对侧 → 淡出；棺材消失则整批逃逸，靠近 CursedSpirit 会被吸走。
/// 只有外飞与横扫两段带伤害，放大过程与被吸收过程都不造成伤害。
/// </summary>
public class LostSouls : ModProjectile
{
    public override string Texture => "FargowiltasSouls/Content/Bosses/CursedCoffin/CoffinDarkSouls";
    const int TrailLength = 10;

    const int StateEmerge = 0;
    const int StateOutward = 1;
    const int StateSweep = 2;
    const int StateAbsorb = 3;
    const int StateFade = 4;
    const int StateFlee = 5;

    const int GrowTime = 24;
    const float EmergeRise = 120f;
    const int EmergeMaxTime = 60;
    const float OutwardMargin = 260f;
    const float TurnLead = 80f;
    const float OutwardTurnRate = 0.045f;
    const float SweepTurnRate = 0.12f;
    const float OutwardSpeed = 8f;
    const float SweepSpeed = 6f;
    const int FadeTime = 20;
    const float AbsorbRadius = 200f;
    const int AbsorbTime = 20;
    const float FleeSpeed = 22f;
    const float FleeAccel = 0.8f;
    const float FleeKillDistance = 1400f;

    static int coffinType;
    static int spiritType;

    ref float SweepY => ref Projectile.ai[0];
    ref float OutwardJitter => ref Projectile.ai[1];
    ref float StateTimer => ref Projectile.localAI[0];
    ref float Side => ref Projectile.localAI[1];
    ref float State => ref Projectile.localAI[2];

    public int[] oldFrame = new int[TrailLength];

    public override void SetStaticDefaults()
    {
        coffinType = ModContent.NPCType<CursedCoffin>();
        spiritType = ModContent.NPCType<CursedSpirit>();
        ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        Main.projFrames[Type] = 4;
    }
    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 30;
        Projectile.aiStyle = -1;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.scale = 0.2f;
        Projectile.light = 1;
        Projectile.timeLeft = 60 * 8;
        Projectile.hide = true;
    }
    public override void AI()
    {
        if (Side == 0f)
            Side = DetermineSide();

        if (State != StateFlee && !NPC.AnyNPCs(coffinType))
            SetState(StateFlee);

        if ((State == StateOutward || State == StateSweep) && TryFindSpirit(Projectile.Center, AbsorbRadius, out _))
            SetState(StateAbsorb);

        switch ((int)State)
        {
            case StateEmerge: Emerge(); break;
            case StateOutward: Outward(); break;
            case StateSweep: Sweep(); break;
            case StateAbsorb: Absorb(); break;
            case StateFade: Fade(); break;
            case StateFlee: Flee(); break;
        }

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        AnimateFrames();
        StateTimer++;
    }
    public override bool? CanDamage() => State == StateOutward || State == StateSweep;
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(FBuffID.ShadowflameBuff, 60 * 4);
    }

    #region 分段行为
    void Emerge()
    {
        float progress = Utils.GetLerpValue(0f, GrowTime, StateTimer, true);
        Projectile.scale = MathHelper.Lerp(0.2f, 1f, progress);

        bool risen = Projectile.Center.Y < CoffinArena.FightCenter.Y - EmergeRise;
        if ((risen && StateTimer >= GrowTime) || StateTimer >= EmergeMaxTime)
            SetState(StateOutward);
    }
    void Outward()
    {
        float halfWidth = CoffinArena.VectorWidth / 2f;
        Vector2 target = new(CoffinArena.FightCenter.X + Side * (halfWidth + OutwardMargin + OutwardJitter), SweepY);
        SteerTowards(target, OutwardSpeed, OutwardTurnRate);

        //提前 TurnLead 开始回头，让 U 形弯的顶点落在落点带附近
        if (Side * (Projectile.Center.X - target.X) > -TurnLead)
            SetState(StateSweep);
    }
    void Sweep()
    {
        float halfWidth = CoffinArena.VectorWidth / 2f;
        float oppositeBand = CoffinArena.FightCenter.X - Side * (halfWidth + OutwardMargin);
        SteerTowards(new Vector2(oppositeBand - Side * 400f, SweepY), SweepSpeed, SweepTurnRate);

        if (Side * (Projectile.Center.X - oppositeBand) < -60f)
            SetState(StateFade);
    }
    void Absorb()
    {
        Vector2 target = Projectile.Center;
        if (TryFindSpirit(Projectile.Center, 2000f, out NPC spirit))
            target = spirit.Center;

        Vector2 toTarget = target - Projectile.Center;
        if (toTarget.Length() > 4f)
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, toTarget.SafeNormalize(-Vector2.UnitY) * 10f, 0.2f);

        float progress = Utils.GetLerpValue(0f, AbsorbTime, StateTimer, true);
        Projectile.scale = MathHelper.Lerp(1f, 0f, progress);
        Projectile.Opacity = 1f - progress;
        if (StateTimer >= AbsorbTime)
            Projectile.Kill();
    }
    void Fade()
    {
        float progress = Utils.GetLerpValue(0f, FadeTime, StateTimer, true);
        Projectile.Opacity = 1f - progress;
        Projectile.scale = MathHelper.Lerp(1f, 0.85f, progress);
        Projectile.velocity *= 0.99f;
        if (StateTimer >= FadeTime)
            Projectile.Kill();
    }
    void Flee()
    {
        Vector2 away = (Projectile.Center - CoffinArena.FightCenter).SafeNormalize(-Vector2.UnitY);
        float speed = MathHelper.Min(Projectile.velocity.Length() + FleeAccel, FleeSpeed);
        float rotation = Projectile.velocity.ToRotation().AngleTowards(away.ToRotation(), 0.15f);
        Projectile.velocity = rotation.ToRotationVector2() * speed;

        float distance = Projectile.Distance(CoffinArena.FightCenter);
        Projectile.Opacity = Utils.GetLerpValue(FleeKillDistance, FleeKillDistance - 400f, distance, true);
        if (distance > FleeKillDistance)
            Projectile.Kill();
    }
    #endregion

    void SteerTowards(Vector2 target, float speed, float turnRate)
    {
        Vector2 direction = (target - Projectile.Center).SafeNormalize(-Vector2.UnitY);
        float rotation = Projectile.velocity.ToRotation().AngleTowards(direction.ToRotation(), turnRate);
        Projectile.velocity = rotation.ToRotationVector2() * MathHelper.Lerp(Projectile.velocity.Length(), speed, 0.08f);
    }
    void SetState(int next)
    {
        State = next;
        StateTimer = 0f;
    }
    float DetermineSide()
    {
        float side = Math.Sign(Projectile.velocity.X);
        if (side == 0f)
            side = Math.Sign(Projectile.Center.X - CoffinArena.FightCenter.X);
        if (side == 0f)
            side = Projectile.whoAmI % 2 == 0 ? -1f : 1f;
        return side;
    }
    static bool TryFindSpirit(Vector2 position, float maxDistance, out NPC result)
    {
        result = null;
        float best = maxDistance;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!npc.active || npc.type != spiritType)
                continue;
            float distance = npc.Distance(position);
            if (distance < best)
            {
                best = distance;
                result = npc;
            }
        }
        return result != null;
    }
    void AnimateFrames()
    {
        for (int i = oldFrame.Length - 1; i > 0; i--)
            oldFrame[i] = oldFrame[i - 1];
        oldFrame[0] = Projectile.frame;

        if (++Projectile.frameCounter >= 6)
        {
            Projectile.frameCounter = 0;
            Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
        }
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        if (Projectile.hide)
            behindNPCs.Add(index);
    }
    public static readonly Color GlowColor = new(224, 196, 252, 0);
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
        int frameHeight = texture.Height / Main.projFrames[Projectile.type];
        int frameY = frameHeight * Projectile.frame;
        Rectangle rectangle = new(0, frameY, texture.Width, frameHeight);
        Vector2 origin = rectangle.Size() / 2f;
        Vector2 drawOffset = Projectile.rotation.ToRotationVector2() * (texture.Width - Projectile.width) / 2;

        SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Main.spriteBatch.UseBlendState(BlendState.Additive);
        for (int i = 0; i < TrailLength; i++)
        {
            //残影颜色是自己拼的，不经过 GetAlpha，透明度要在这里补
            Color trailColor = GlowColor with { A = 255 };
            trailColor *= (TrailLength - i) / (float)TrailLength * Projectile.Opacity;

            int oldFrameY = frameHeight * oldFrame[i];
            Rectangle oldRectangle = new(0, oldFrameY, texture.Width, frameHeight);
            Vector2 position = Projectile.oldPos[i] + drawOffset + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);
            Main.EntitySpriteDraw(texture, position, oldRectangle, trailColor, Projectile.oldRot[i], origin, Projectile.scale, effects, 0);
        }
        Main.spriteBatch.ResetToDefault();

        Main.EntitySpriteDraw(texture, Projectile.Center + drawOffset - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), rectangle, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);
        return false;
    }
}
