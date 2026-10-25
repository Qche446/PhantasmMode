using FargosPhantasmMode.Common;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.WorldGeneration;
using Monochrome.Common.MonoUtil;
using Monochrome.Core.Graphics.Screen;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Content.Bosses.Coffin;

public class P_CursedSpirit : PModeNPCBehaviour
{
    public override int NPCType => ModContent.NPCType<CursedSpirit>();
    public CursedSpirit spirit;
    public NPC owner;
    public Player Player => Main.player[owner.target];
    private int Frame = 0;
    public ref float Owner => ref spirit.Owner;
    public ref float Timer => ref spirit.Timer;
    public ref float State => ref spirit.State;
    public ref float AI3 => ref spirit.AI3;
    public ref float StartupFadein => ref spirit.StartupFadein;
    public bool RotateToVelocity = true;
    public override void SetDefaults(NPC npc)
    {
        npc.damage = 25;//原来为35
    }
    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        spirit = npc.As<CursedSpirit>();
        owner = FargoSoulsUtil.NPCExists(Owner, ModContent.NPCType<CursedCoffin>());
    }
    public override bool SafePreAI(NPC npc)
    {
        if (!owner.TypeAlive<CursedCoffin>() && FargoSoulsUtil.HostCheck)
        {
            npc.StrikeInstantKill();
            return false;
        }
        if (StartupFadein < 10)
        {
            StartupFadein++;
            npc.Opacity = 0;
        }
        else if (StartupFadein == 10)
        {
            npc.Opacity = 1;
            StartupFadein++;
        }
        if (FargoSoulsUtil.HostCheck)
        {
            npc.lifeMax = owner.lifeMax = Math.Min(npc.lifeMax, owner.lifeMax);
            npc.life = owner.life = Math.Min(npc.life, owner.life);
        }
        RotateToVelocity = true;
        npc.dontTakeDamage = npc.scale < 0.5f;
        npc.noTileCollide = true;
        if (!(owner.target.IsWithinBounds(Main.maxPlayers) && Main.player[owner.target] is Player player && player.Alive()))
            return false;
        CursedCoffin coffin = owner.As<CursedCoffin>();
        P_CursedCoffin pcoffin = owner.GetGlobalNPC<P_CursedCoffin>();
        bool newState = (float)pcoffin.Fsm.Current != State;

        switch (pcoffin.Fsm.Current)
        {
            case P_CursedCoffin.State.HoveringForSlam:
                NewStateTran(newState);
                HoveringForSlam(npc);
                break;
            case P_CursedCoffin.State.SlamWShockwave:
                NewStateTran(newState);
                SlamWShockwave(npc);
                break;
            case P_CursedCoffin.State.WavyShotCircle:
                NewStateTran(newState);
                WavyShotCircle(npc);
                break;
            case P_CursedCoffin.State.WavyShotSlam:
                NewStateTran(newState);
                WavyShotSlam(npc);
                break;
            case P_CursedCoffin.State.Jormungandr:
                NewStateTran(newState);
                Jormungandr(npc);
                break;
            case P_CursedCoffin.State.LateralSprint:
                NewStateTran(newState);
                LateralSprint(npc);
                break;
            case P_CursedCoffin.State.CircleRound:
                NewStateTran(newState);
                CircleRound(npc);
                break;
            case P_CursedCoffin.State.PhaseTransition1st:
                {
                    npc.Center = owner.Center;
                    npc.scale = 0.2f;
                    npc.Opacity = 0f; //这一阶段半灵还没登场
                }
                break;
            case P_CursedCoffin.State.PhaseTransition2nd:
                NewStateTran(newState);
                PhaseTransition2nd(npc);
                break;
            default: 
                break;
        }
        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (!proj.Hitbox.Intersects(npc.Hitbox))
                continue;
            if (proj.type == FProjID.CoffinWaveShot && proj.ai[2] == 0f && pcoffin.Fsm.Current == P_CursedCoffin.State.CircleRound)
                proj.Kill();
            else if (proj.type == ModContent.ProjectileType<FallingSandstone>() && proj.hostile)
            {
                SoundEngine.PlaySound(in SoundID.Dig, proj.Center);
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(proj.position, proj.width, proj.height, DustID.SandstormInABottle, Main.rand.NextFloat(2f, 4f), Main.rand.NextFloat(2f, 4f), 0, default, Main.rand.NextFloat(0.7f, 1.5f));
                proj.Kill();
            }
        }

        State = (float)pcoffin.Fsm.Current;
        if (RotateToVelocity)
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        //Main.NewText(npc.damage);
        //阻止原spirit无接触伤害
        spirit.BittenPlayer = -1;
        return false;
    }
    private void HoveringForSlam(NPC npc)//扔鬼魂
    {
        if (npc.Opacity > 0.9f)
            npc.Opacity = 0.9f;
        LerpOpacity(0.4f);
        LerpScale(0.6f);
        Vector2 desiredPos = owner.Center - Vector2.UnitY * owner.height;
        Movement(desiredPos, 0.1f, Math.Max(25, owner.velocity.Length()), owner.velocity.Length(), 0.08f, 20);
        if (npc.Distance(desiredPos) < owner.height * 0.75f)
        {
            const int shotTime = 20;
            if (Timer % shotTime == shotTime - 1)
            {
                SoundEngine.PlaySound(CursedCoffin.SoulShotSFX, npc.Center);
                if (PhanUtil.HostCheck)
                {
                    Vector2 vel = -Vector2.UnitY.RotatedBy(MathF.Tau * 0.14f * Math.Sin(MathF.Tau * (Timer + Main.rand.Next(20)) / 53)) * 4;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, FProjID.CoffinDarkSouls, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.8f), 0f, Main.myPlayer, npc.whoAmI, 0.18f);
                }
            }
            Timer++;
        }
    }
    private void SlamWShockwave(NPC npc)//砸地：定位蓄力时撒灵魂，下砸时缩进棺材
    {
        Vector2 center = CoffinArena.FightCenter;
        P_CursedCoffin pcoffin = owner.GetGlobalNPC<P_CursedCoffin>();
        float chargeEnd = 40f + (pcoffin.Phase >= 2 ? 45f : 20f); //棺材定位 40 帧 + 蓄力
        Timer++;
        if (Timer == 1)
            npc.localAI[3] = pcoffin.AI3; //记下棺材的砸地计数，用来抓每一次落地
        if (pcoffin.AI3 != npc.localAI[3]) //棺材刚砸到地：从身下喷一圈会往上飘的灵魂
        {
            npc.localAI[3] = pcoffin.AI3;
            SoundEngine.PlaySound(CursedCoffin.SoulShotSFX, npc.Center);
            if (PhanUtil.HostCheck)
            {
                for (int i = -3; i <= 3; i++)
                {
                    if (i == 0)
                        continue;
                    Vector2 fanVel = Vector2.UnitY.RotatedBy(i * 0.25f) * (6f + Math.Abs(i)); //朝下喷开，负加速度让它们再飘回去
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Bottom, fanVel, FProjID.CoffinDarkSouls,
                        FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.whoAmI, -0.135f);
                }
            }
        }
        //下砸与砸后休息：缩进棺材里跟着走
        if (pcoffin.Timer > chargeEnd || pcoffin.Timer < 0)
        {
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(owner.Center) * Math.Min(Math.Max(20f, owner.velocity.Length()), npc.Distance(owner.Center)), 0.2f);
            LerpOpacity(0.15f);
            LerpScale(0.4f);
            return;
        }
        //定位与蓄力：浮在棺材上方，蓄力剩 20 帧时收手
        LerpOpacity(1.1f);
        LerpScale(1f);
        RotateToVelocity = false;
        Movement(CoffinArena.ClampWithinArena(owner.Center - Vector2.UnitY * owner.height, npc), 0.12f, 18f, 6f, 0.1f, 30f);
        npc.rotation = MathHelper.Lerp(npc.rotation, npc.SafeDirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2, 0.1f);
        Vector2 target = new(pcoffin.AI1, pcoffin.AI2);
        if (pcoffin.Timer <= 1f || pcoffin.Timer > chargeEnd - 20f || owner.Distance(target) > 30f || pcoffin.Timer % 2 != 0)
            return;
        //上半平面随机方向；落点落在棺材那一条（棺材宽度再放宽一大截）就重摇
        float gravity = 0.2f;
        float groundY = center.Y + CoffinArena.VectorHeight / 2f;
        Vector2 vel = Vector2.Zero;
        for (int i = 0; i < 8; i++)
        {
            vel = Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2)) * Main.rand.NextFloat(6f, 9f);
            float t = (-vel.Y + MathF.Sqrt(vel.Y * vel.Y - 2 * gravity * (npc.Center.Y - groundY))) / gravity; //落到场地底面的用时
            if (Math.Abs(npc.Center.X + vel.X * t - owner.Center.X) > owner.width + 40f)
                break;
        }
        SoundEngine.PlaySound(CursedCoffin.SoulShotSFX, npc.Center);
        if (PhanUtil.HostCheck)
            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, FProjID.CoffinDarkSouls, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.whoAmI, gravity);
    }
    private void WavyShotCircle(NPC npc)//丢垃圾
    {
        // 0 飞往反侧上空 / 1 停到第 60 帧 / 2 横向漂移
        Vector2 center = CoffinArena.FightCenter;
        Timer++;
        switch ((int)AI3)
        {
            case 0:
                if (Timer == 1)
                {
                    float side = Math.Sign(Player.Center.X - center.X); //玩家在中心哪一侧
                    if (side == 0)
                        side = 1;
                    npc.localAI[1] = center.X - side * Main.rand.NextFloat(120f, 400f); //反侧的随机 X
                    npc.localAI[2] = center.Y - 205f; //贴着房间上沿的高度
                    npc.netUpdate = true;
                }
                LerpOpacity(1.1f);
                LerpScale(1f);
                Vector2 lockPos = new(npc.localAI[1], npc.localAI[2]);
                Movement(lockPos, 0.12f, 18f, 6f, 0.1f, 40f);
                if (npc.Distance(lockPos) < 30f || Timer >= 60)
                    AI3 = 1;
                break;
            case 1:
                RotateToVelocity = false;
                npc.velocity *= 0.9f;
                npc.rotation = MathHelper.Lerp(npc.rotation, npc.SafeDirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2, 0.1f);
                if (Timer >= 60)
                {
                    float dir = Math.Sign(Player.Center.X - npc.Center.X); //初始朝向玩家
                    npc.localAI[1] = dir == 0 ? -1f : dir;
                    AI3 = 2;
                    Timer = 0;
                    npc.netUpdate = true;
                }
                break;
            case 2:
                float moveDir = npc.localAI[1] == 0 ? -1f : npc.localAI[1];
                float bound = CoffinArena.VectorWidth / 2f - npc.width / 2f - 20f; //贴墙前掉头
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, moveDir * 6f, 0.08f);
                npc.velocity.Y = (npc.localAI[2] - npc.Center.Y) * 0.08f;
                if (moveDir * (npc.Center.X - center.X) >= bound)
                {
                    npc.localAI[1] = -moveDir;
                    npc.netUpdate = true;
                }
                if (Timer % 30 == 29)
                {
                    npc.localAI[3] = Main.rand.Next(3) switch
                    {
                        1 => 5f, //花瓶
                        2 => 6f, //骨头
                        _ => Main.rand.Next(5) //宝石
                    };
                    npc.netUpdate = true;
                }
                if (Timer % 30 == 0)
                {
                    SoundEngine.PlaySound((int)npc.localAI[3] switch
                    {
                        5 => SoundID.Item106,
                        6 => SoundID.NPCHit2,
                        _ => SoundID.Item101
                    }, npc.Center);
                    if (PhanUtil.HostCheck)
                    {
                        Vector2 vel = new(Main.rand.NextFloat(-0.6f, 0.6f), 0f);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + Vector2.UnitY * (npc.height / 3f), vel,
                            FProjID.CoffinRandomStuff, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.8f), 0f, Main.myPlayer, npc.localAI[3]);
                    }
                }
                break;
        }
    }
    private void WavyShotSlam(NPC npc)//砸地
    {
        Vector2 center = CoffinArena.FightCenter;
        Timer++;
        switch ((int)AI3)
        {
            case 0:
                if (Timer == 1)
                {
                    npc.localAI[1] = owner.Center.X; //棺材正上方的 X
                    npc.localAI[2] = center.Y - 214f; //场地内沿允许的最高处
                    npc.netUpdate = true;
                }
                Vector2 ceilingPos = new(npc.localAI[1], npc.localAI[2]);
                Movement(ceilingPos, 0.15f, 20f, 6f, 0.12f, 30f);
                if (npc.Distance(ceilingPos) < 25f || Timer >= 60)
                {
                    AI3 = 1;
                    Timer = 0;
                }
                break;
            case 1:
                RotateToVelocity = false;
                npc.velocity *= 0.9f;
                npc.rotation = MathHelper.Lerp(npc.rotation, npc.SafeDirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2, 0.15f);
                owner.GetGlobalNPC<P_CursedCoffin>().SpawnParticle(1f - Utils.GetLerpValue(0f, 60f, Timer, true), npc.Center);
                if (Timer >= 60)
                {
                    npc.velocity = npc.SafeDirectionTo(Player.Center) * 4f;
                    AI3 = 2;
                    Timer = 0;
                    npc.netUpdate = true;
                }
                break;
            case 2:
                npc.velocity += npc.velocity.SafeNormalize(Vector2.Zero) * 0.5f;
                if (npc.velocity.Length() > 12f)
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * 12f;
                if ((Collision.SolidTiles(npc.position + npc.velocity, npc.width, npc.height) && Timer >= 20) || Timer >= 180)
                {
                    npc.velocity = -npc.velocity.SafeNormalize(Vector2.Zero) * 5f; //撞完往反方向弹开一小段
                    AI3 = 3;
                    Timer = 0;
                }
                break;
            case 3:
                RotateToVelocity = false;
                npc.velocity *= 0.9f;
                break;
        }
    }
    private void Jormungandr(NPC npc)//靠墙蓄力，反复撞玩家
    {
        Vector2 center = CoffinArena.FightCenter;
        Timer++;
        switch ((int)AI3)
        {
            case 0:
                if (Timer == 1)
                {
                    float side = Math.Sign(Player.Center.X - center.X); //玩家在中心哪一侧
                    if (side == 0)
                        side = 1;
                    npc.localAI[1] = center.X - side * (CoffinArena.VectorWidth / 2f - npc.width / 2f); //反侧墙边
                    npc.netUpdate = true;
                }
                Vector2 wallPos = new(npc.localAI[1], center.Y);
                LerpOpacity(1.1f);
                LerpScale(1f);
                RotateToVelocity = false;
                npc.rotation = MathHelper.Lerp(npc.rotation, npc.SafeDirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2, 0.1f);
                if (npc.Distance(wallPos) < 25f)
                    npc.velocity *= 0.9f; //到位后原地等满这 60 帧
                else
                    Movement(wallPos, 0.15f, 20f, 6f, 0.12f, 30f);
                if (Timer >= 60)
                {
                    AI3 = 1;
                    Timer = 0;
                }
                break;
            case 1:
                RotateToVelocity = false;
                npc.velocity *= 0.9f;
                npc.rotation = MathHelper.Lerp(npc.rotation, npc.SafeDirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2, 0.15f);
                owner.GetGlobalNPC<P_CursedCoffin>().SpawnParticle(1f - Utils.GetLerpValue(0f, 20f, Timer, true), npc.Center);
                if (Timer >= 20)
                {
                    npc.velocity = npc.SafeDirectionTo(Player.Center) * 4f;
                    AI3 = 2;
                    Timer = 0;
                    npc.netUpdate = true;
                }
                break;
            case 2:
                npc.velocity += npc.velocity.SafeNormalize(Vector2.Zero) * 0.8f;
                if (npc.velocity.Length() > 18f)
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * 18f;
                if ((Timer >= 20 && Collision.SolidTiles(npc.position + npc.velocity, npc.width / 2, npc.height / 2)) || Timer >= 180)
                {
                    npc.velocity = -npc.velocity.SafeNormalize(Vector2.Zero) * 5f; //撞完往反方向弹开一小段
                    AI3 = 3;
                    Timer = 0;
                }
                break;
            case 3:
                RotateToVelocity = false;
                npc.velocity *= 0.9f;
                if (Timer >= 30)
                {
                    AI3 = 1;
                    Timer = 0;
                }
                break;
        }
    }
    private void LateralSprint(NPC npc)//横向冲刺：藏在棺材里，撞墙时弹出反打
    {
        // 0 藏棺材 / 1 弹出减速 / 2 蓄力 40 / 3 冲玩家 / 4 弹开后回自己这侧的天花板角落 / 5 短蓄力 20 / 6 水平横冲另一半场
        Vector2 center = CoffinArena.FightCenter;
        P_CursedCoffin pcoffin = owner.GetGlobalNPC<P_CursedCoffin>();
        float stage = pcoffin.AI3; //棺材这一招的阶段
        Timer++;
        //只有第一次撞墙之前藏着；之后哪怕棺材开始第二轮也不再进棺材
        if (owner.localAI[3] == 0f && stage <= 3f)
        {
            AI3 = 0;
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(owner.Center) * Math.Min(Math.Max(40f, owner.velocity.Length()), npc.Distance(owner.Center)), 0.2f);
            LerpOpacity(0.15f);
            LerpScale(0.4f);
            return;
        }
        switch ((int)AI3)
        {
            case 0: //等棺材撞墙
                if (stage == 4f)
                {
                    AI3 = 1;
                    Timer = 0;
                    npc.netUpdate = true;
                }
                break;
            case 1: //弹出减速，落到反侧
                LerpOpacity(1.1f);
                LerpScale(1f);
                Vector2 ejectPos = new(center.X - pcoffin.AI2 * 240f, center.Y - 80f); //反侧半个半场宽，略高于中心
                npc.velocity = (ejectPos - npc.Center) * (2f / (21f - Timer)); //每帧收掉剩余距离的一部分，20 帧正好停住
                if (Timer >= 20)
                {
                    AI3 = 2;
                    Timer = 0;
                }
                break;
            case 2: //蓄力 40 帧
                Charge(npc, 40f);
                if (Timer >= 40)
                {
                    npc.velocity = npc.SafeDirectionTo(Player.Center) * 4f;
                    AI3 = 3;
                    Timer = 0;
                    npc.netUpdate = true;
                }
                break;
            case 3: //冲向玩家，撞墙后弹开一小段
                npc.velocity += npc.velocity.SafeNormalize(Vector2.Zero) * 0.5f;
                if (npc.velocity.Length() > 12f)
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * 12f;
                if (Collision.SolidTiles(npc.position + npc.velocity, npc.width, npc.height) || Timer >= 180)
                {
                    npc.velocity = -npc.velocity.SafeNormalize(Vector2.Zero) * 5f;
                    AI3 = 4;
                    Timer = 0;
                    SoundEngine.PlaySound(SoundID.Item14, npc.Center);
                    MonoScreenEffectsSystem.StartShakeByAmplitude(10);
                    if (Main.getGoodWorld && false) //传奇模式：整场落沙
                    {
                        pcoffin.HalfDropSand(1, -20, true);
                        pcoffin.HalfDropSand(-1, -20, true);
                    }
                }
                break;
            case 4: //先把弹回的余速度放掉，再去自己这一侧的天花板角落
                LerpOpacity(1.1f);
                LerpScale(1f);
                if (Timer <= 12)
                {
                    npc.velocity *= 0.88f;
                    break;
                }
                if (Timer == 13)
                {
                    float side = -Math.Sign(Player.Center.X - npc.Center.X);
                    if (side == 0)
                        side = 1;
                    npc.localAI[1] = center.X + side * (CoffinArena.VectorWidth / 2f - npc.width / 2f - 20); //同侧墙边
                    npc.localAI[2] = center.Y - 175f; //贴天花板
                    npc.netUpdate = true;
                }
                Vector2 corner = new(npc.localAI[1], npc.localAI[2]);
                float dist = npc.Distance(corner);
                RotateToVelocity = false;
                npc.rotation = MathHelper.Lerp(npc.rotation, npc.SafeDirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2, 0.1f);
                if (dist >= 30f && Timer < 90)
                {
                    Movement(corner, 0.15f, 18f, 6f, 0.12f, 60f);
                    break;
                }
                npc.Center = corner; //贴死在角上，不然带着余速会直接冲出房间
                npc.velocity = Vector2.Zero;
                AI3 = 5;
                Timer = 0;
                break;
            case 5: //短蓄力 20 帧，然后朝另一半场横冲
                Charge(npc, 20f);
                if (Timer >= 20)
                {
                    float side = Math.Sign(npc.Center.X - center.X);
                    if (side == 0)
                        side = -1;
                    npc.velocity = new Vector2(-side * 10f, 0f);
                    AI3 = 6;
                    Timer = 0;
                    npc.netUpdate = true;
                }
                break;
            case 6: //水平横冲，边飞边撒弹
                npc.velocity.X += Math.Sign(npc.velocity.X) * 0.5f;
                if (Math.Abs(npc.velocity.X) > 20f)
                    npc.velocity.X = Math.Sign(npc.velocity.X) * 20f;
                npc.velocity.Y *= 0.9f;
                if (Timer % 3 == 0 && PhanUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + 10 * (Timer * 0.2f).ToRotationVector2(), Vector2.Zero, PProjID.P_CoffinSpiritWaveShot, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.8f), 0f, Main.myPlayer, 0f, Player.whoAmI);
                if ((Collision.SolidTiles(npc.position + npc.velocity, npc.width, npc.height) && Timer >= 20) || Timer >= 180)
                {
                    npc.velocity = -npc.velocity.SafeNormalize(Vector2.Zero) * 5f;
                    AI3 = 2;
                    Timer = 0;
                }
                break;
        }
    }
    private void Charge(NPC npc, float duration)//蓄力：刹住、转向玩家、往里吸粒子
    {
        RotateToVelocity = false;
        LerpOpacity(1f);
        LerpScale(1f);
        npc.velocity *= 0.9f;
        npc.rotation = MathHelper.Lerp(npc.rotation, npc.SafeDirectionTo(Player.Center).ToRotation() + MathHelper.PiOver2, 0.15f);
        owner.GetGlobalNPC<P_CursedCoffin>().SpawnParticle(1f - Utils.GetLerpValue(0f, duration, Timer, true), npc.Center);
    }
    private void PhaseTransition2nd(NPC npc)//转阶段2：现身，浮到棺材上方当绕圈半径
    {
        Vector2 center = CoffinArena.FightCenter;
        Timer++;
        if (Timer == 1)
        {
            float groundDist = center.Y + CoffinArena.VectorHeight / 2f - owner.Center.Y; //棺材离地高度
            npc.localAI[2] = Math.Clamp(groundDist - 80f, 60f, 160f); //绕圈半径
            npc.localAI[1] = -MathHelper.PiOver2; //起手在正上方
            npc.localAI[3] = 0.025f; //角速度
            npc.netUpdate = true;
        }
        LerpOpacity(1f);
        LerpScale(1f);
        Movement(owner.Center - Vector2.UnitY * npc.localAI[2], 0.15f, 20f, 6f, 0.12f, 30f);
    }
    private void CircleRound(NPC npc)//尾杀绕圈
    {
        Timer++;
        if (npc.localAI[2] <= 0f) //没经过 PhaseTransition2nd 时的兜底
            npc.localAI[2] = 150f;
        if (AI3 == 0f && Timer == 1) //起手角度取当前所在方位
            npc.localAI[1] = (npc.Center - owner.Center).ToRotation();
        switch ((int)AI3)
        {
            case 0:
                if (Timer >= 120)
                {
                    AI3 = 1;
                    Timer = 0;
                }
                break;
            case 1:
                if (Timer >= 60)
                {
                    AI3 = 2;
                    Timer = 0;
                }
                break;
            case 2:
                owner.GetGlobalNPC<P_CursedCoffin>().SpawnParticle(1f - Utils.GetLerpValue(0f, 60f, Timer, true), npc.Center);
                if (Timer >= 60)
                {
                    SoundEngine.PlaySound(P_CursedCoffin.BigShotSFX, npc.Center);
                    MonoScreenEffectsSystem.StartShakeByAmplitude(10);
                    if (PhanUtil.HostCheck)
                    {
                        for (int i = 0; i < 60; i++)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Main.rand.NextFloat(MathF.Tau).ToRotationVector2() * Main.rand.NextFloat(5f, 12f),
                                FProjID.CoffinWaveShot, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 0f, 0f, 1f); //ai2=1 标记这发是半灵的
                        }
                    }
                    AI3 = 3;
                    Timer = 0;
                }
                else if (Timer % 20 == 1 && PhanUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), owner.GetGlobalNPC<P_CursedCoffin>().MaskCenter, Vector2.Zero, PProjID.CoffinShockWave, 0, 0, Main.myPlayer, 10, 100);
                break;
            case 3:
                if (Timer >= 120)
                {
                    AI3 = 1;
                    Timer = 0;
                }
                break;
        }
        //绕圈：角速度逐渐爬到上限
        float maxOmiga = MathHelper.Pi / 120f;
        maxOmiga *= Main.getGoodWorld ? 1.2f : 1;
        npc.localAI[3] = Math.Min(npc.localAI[3] + 0.0004f, maxOmiga);
        npc.localAI[1] += npc.localAI[3];
        npc.velocity = owner.Center + npc.localAI[1].ToRotationVector2() * npc.localAI[2] - npc.Center; //直接把下个位置当速度，朝向自动贴切线
    }
    private void NewStateTran(bool newstate)
    {
        if (newstate)
        {
            Timer = 0;
            AI3 = 0;
            spirit.NPC.netUpdate = true;
        }
    }
    public static bool TryApplyRelease(int spiritIndex, int victimWhoAmI)
    {
        if (spiritIndex >= Main.maxNPCs || victimWhoAmI >= Main.maxPlayers)
            return false;

        NPC npc = Main.npc[spiritIndex];
        if (!npc.active || npc.ModNPC is not CursedSpirit cursedSpirit)
            return false;

        Player victim = Main.player[victimWhoAmI];
        cursedSpirit.BittenPlayer = -1;
        cursedSpirit.BiteTimer = -90;
        npc.velocity = -MonoUtil.SafeDirectionTo(npc, victim.Center) * 12f;
        victim.immune = true;
        victim.immuneTime = Math.Max(victim.immuneTime, 30);
        victim.hurtCooldowns[0] = Math.Max(victim.hurtCooldowns[0], 30);
        victim.hurtCooldowns[1] = Math.Max(victim.hurtCooldowns[1], 30);
        npc.netUpdate = true;
        cursedSpirit.Timer = 0f;
        cursedSpirit.AI3 = 0f;
        return true;
    }
    void Movement(Vector2 pos, float accel = 0.03f, float maxSpeed = 20, float lowspeed = 5, float decel = 0.03f, float slowdown = 30)
    {
        NPC npc = spirit.NPC;
        if (npc.Distance(pos) > slowdown)
        {
            npc.velocity = Vector2.Lerp(npc.velocity, (pos - npc.Center).SafeNormalize(Vector2.Zero) * maxSpeed, accel);
        }
        else
        {
            npc.velocity = Vector2.Lerp(npc.velocity, (pos - npc.Center).SafeNormalize(Vector2.Zero) * lowspeed, decel);
        }
    }
    void LerpOpacity(float target, float speed = 0.15f)
    {
        spirit.NPC.Opacity = (float)Utils.Lerp(spirit.NPC.Opacity, target, speed);
    }
    void LerpScale(float target, float speed = 0.15f) => spirit.NPC.scale = (float)Utils.Lerp(spirit.NPC.scale, target, speed);
}
