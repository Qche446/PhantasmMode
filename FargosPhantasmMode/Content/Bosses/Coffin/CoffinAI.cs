using FargosPhantasmMode.Common;
using FargosPhantasmMode.Common.Particles;
using FargowiltasSouls;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.Items.Summons;
using FargowiltasSouls.Content.WorldGeneration;
using FargowiltasSouls.Core.Systems;
using Monochrome.Common.MonoUtil;
using Monochrome.Common.MonoUtil.Mathematics.Statistics;
using Monochrome.Content.Ai;
using Monochrome.Core;
using Monochrome.Core.Graphics.Screen;
using System.Collections.Generic;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Content.Bosses.Coffin;
public partial class P_CursedCoffin
{
    #region 字段，枚举，数组
    public static readonly SoundStyle PhaseTransitionSFX = FargosSoundRegistry.CoffinPhaseTransition;
    public static readonly SoundStyle SlamSFX = FargosSoundRegistry.CoffinSlam with { Volume = 0.5f, PitchVariance = 0.3f };
    public static readonly SoundStyle SpiritDroneSFX = FargosSoundRegistry.CoffinSpiritDrone with { MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.2f };
    public static readonly SoundStyle BigShotSFX = FargosSoundRegistry.CoffinBigShot with { Volume = 0.6f, PitchVariance = 0.3f };
    public static readonly SoundStyle ShotSFX = FargosSoundRegistry.CoffinShot with { Volume = 0.3f, PitchVariance = 0.3f };
    public static readonly SoundStyle SoulShotSFX = FargosSoundRegistry.CoffinSoulShot with { Volume = 0.3f, PitchVariance = 0.3f };
    public static readonly SoundStyle HandChargeSFX = FargosSoundRegistry.CoffinHandCharge;
    public enum State : byte
    {
        Opening,//开幕式
        PhaseTransition1st,
        PhaseTransition2nd,
        HoveringForSlam,//悬空，等待砸地
        SlamWShockwave,//砸地，释放cjb
        WavyShotCircle,//发射诅咒弹
        WavyShotSlam,//发射诅咒弹后的砸地
        Jormungandr,//耶梦加得(不是)
        LateralSprint,//横向冲刺
        //RandomStuff,//扔垃圾
        CircleRound,//指鬼魂环绕棺材
        Count,//数量
    }
    //只用于p1p2招式选取，将三阶段全程保留的CircleRound去除
    private readonly List<State> Attacks =
    [
        State.HoveringForSlam,
        State.WavyShotCircle,
        State.Jormungandr,
        //State.RandomStuff,
        State.LateralSprint,
        //State.CircleRound,
    ];
    public CursedCoffin coffin;
    public Player Player => Main.player[coffin.NPC.target];
    public byte Phase = 1;
    private State LastAttackChoice1 = State.Opening;
    private State LastAttackChoice2 = State.Opening;
    #endregion
    #region 重写
    public override void OnSpawn(NPC npc, IEntitySource source) 
    {
        coffin = npc.As<CursedCoffin>();
        random = new(MonoRng.Derive(0, (ulong)npc.type, (ulong)npc.whoAmI, (ulong)MonoTime.Tick));
    }
    public override bool SafePreAI(NPC npc)
    {
        npc.defense = npc.defDefense;
        if (Phase >= 2)
            npc.defense += 15;
        if (Phase >= 3)
            npc.defense += 15;
        if (Fsm != null && Fsm.Current != State.LateralSprint && Fsm.Current != State.Jormungandr)
            npc.rotation = 0;
        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (proj.type == ModContent.ProjectileType<FallingSandstone>() && proj.hostile && proj.Hitbox.Intersects(npc.Hitbox))
            {
                SoundEngine.PlaySound(in SoundID.Dig, proj.Center);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 position = proj.position;
                    int width = proj.width;
                    int height = proj.height;
                    float speedX = Main.rand.NextFloat(2f, 4f);
                    float speedY = Main.rand.NextFloat(2f, 4f);
                    float scale = Main.rand.NextFloat(0.7f, 1.5f);
                    Dust.NewDust(position, width, height, DustID.SandstormInABottle, speedX, speedY, 0, default, scale);
                }
                proj.Kill();
            }
        }
        CrowdedPlayer(npc);
        ManageArena();
        if (!Targeting())
            return false;
        npc.timeLeft = 60;
        npc.Opacity = 1;

        Fsm.Update();
        Timer++;
        return false;
    }
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) => Fields2.Write(Self(npc), binaryWriter);
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) => Fields2.Read(Self(npc), binaryReader);
    #endregion
    #region 状态绑定的AI方法
    [MonoBehavior<State>(State.Opening)]
    public void Opening()//开幕式
    {
        NPC npc = coffin.NPC;
        if (Timer == 2 && !WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.CursedCoffin] && FargoSoulsUtil.HostCheck)
            Item.NewItem(npc.GetSource_Loot(), Main.player[npc.target].Hitbox, ModContent.ItemType<CoffinSummon>());
        if (Timer >= 0f)
        {
            ExtraTrail = true;
            //开局下砸
            if (Math.Abs(npc.velocity.Y) < 22f)
                npc.velocity.Y += 0.2f;
            if ((Timer > 5f && npc.velocity.Y > 0f) || Timer > 120f)
            {
                npc.noTileCollide = false;
                if (npc.velocity.Y <= 1f)
                {
                    SoundEngine.PlaySound(in SoundID.Item14, npc.Center);
                    SoundEngine.PlaySound(in CursedCoffin.SlamSFX, npc.Center);
                    ExtraTrail = false;
                    //砸中地面后计时120后进入正式攻击状态
                    Timer = -120f;
                    if (PhanUtil.HostCheck)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 vel = Vector2.UnitX * i * 3f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 30f, vel, FProjID.CoffinSlamShockwave, FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.6f, 2), 1f, Main.myPlayer);
                        }
                    }
                    if (ModLoader.TryGetMod("FargowiltasMusic", out var musicMod) && musicMod.Version >= Version.Parse("0.1.6"))
                    {
                        coffin.Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/ShiftingSands");
                        if (Main.musicFade[coffin.Music] < 0.5f)
                        {
                            Main.musicFade[coffin.Music] = 0.5f;
                        }
                    }
                    else
                    {
                        coffin.Music = MusicID.OtherworldlyBoss1;
                    }
                    //砸地后反弹速度
                    npc.velocity.Y -= 9f;
                    MonoScreenEffectsSystem.StartShakeByAmplitude(10);
                    int dir = Math.Sign(Player.Center.X - CoffinArena.FightCenter.X);
                    HalfDropSand(dir);
                    if (Main.getGoodWorld)
                    {
                        HalfDropSand(-dir);
                    }
                }
            }
        }
        //120tick等待时间，减速
        if (Timer < 0f)
        {
            if (npc.velocity.Y < 0f)
                npc.velocity.Y += 0.2f;
            else
                npc.velocity.Y = 0f;
        }
    }
    [MonoBehavior<State>(State.HoveringForSlam)]
    public void HoveringForSlam()//悬停，接砸地
    {
        NPC npc = coffin.NPC;
        const float WaveAmpX = 200;
        const float WaveAmpY = 30;
        const float XHalfPeriod = 60 * 1.5f;
        const float YHalfPeriod = 60 * 0.75f;
        HoverSound();
        if (Timer == 1)
        {
            float xOffset = Utils.Clamp(npc.Center.X - Player.Center.X, -WaveAmpX, WaveAmpX);
            AI1 = MathF.Asin(xOffset / WaveAmpX);
            AI2 = Main.rand.Next(160, 220);
            if (!Main.npc.Any(p => p.TypeAlive<CursedSpirit>()))
                AI2 -= 105;
        }
        if (Timer < npc.ai[2] && Timer >= 0)
        {
            npc.noTileCollide = true;
            float desiredX = WaveAmpX * MathF.Sin(AI1 + MathF.PI * (Timer / XHalfPeriod));
            float desiredY = -350;
            Vector2 desiredPos = Player.Center + desiredX * Vector2.UnitX + desiredY * Vector2.UnitY;
            desiredPos = CoffinArena.ClampWithinArena(desiredPos, npc);
            desiredPos.Y += 50;
            desiredPos.Y += WaveAmpY * MathF.Sin(MathF.PI * (Timer / YHalfPeriod));
            Movement(desiredPos, 0.1f, 10, 0.08f);
        }
    }
    [MonoBehavior<State>(State.SlamWShockwave)]
    public void SlamWShockwave()//释放cjb的砸地, P1 2次， P2 4次
    {
        NPC npc = coffin.NPC;
        ref float Counter = ref AI3;
        int LocateTime = 40;
        int relaxTime = 20;
        int ChargeTime = Phase >= 2 ? 45 : 20;
        int totalImpact = Phase >= 2 ? 3 : 1;
        npc.noTileCollide = npc.Bottom.Y + npc.velocity.Y < Player.Bottom.Y - 16;
        if (Timer < 0)
        {
            if (Math.Abs(npc.Center.X - CoffinArena.FightCenter.X) > (CoffinArena.Width * 8) - npc.width / 2)
                if (npc.velocity.X.Sign(true) != npc.HorizontalDirectionTo(CoffinArena.FightCenter))
                    npc.velocity.X = 0;
            if (Timer == -60)
                npc.velocity.Y -= 9;
            if (Timer > -60 && npc.velocity.Y < 0)
                npc.velocity.Y += 0.2f;
        }
        else if (Timer == 1)
        {
            Vector2 desiredPos = Player.Center - 350 * Vector2.UnitY;
            desiredPos = CoffinArena.ClampWithinArena(desiredPos, npc);
            desiredPos.Y += 0;
            AI1 = desiredPos.X;
            AI2 = desiredPos.Y;
        }
        //定位瞄准
        else if (Timer <= LocateTime)
        {
            Vector2 targetPos = new(AI1, AI2);
            Movement(targetPos, 0.12f, 30, 0.18f);
            if ((npc.Center - targetPos).Length() < 5)
            {
                npc.velocity *= 0.95f;
                if (Timer < LocateTime - 10)
                    Timer = LocateTime - 10;
            }
        }
        //蓄力动画，减速
        else if (Timer <= LocateTime + ChargeTime)
        {
            float progress = 1f - (Timer - LocateTime) / (float)ChargeTime;
            SpawnParticle(progress);
            npc.velocity *= 0.92f;
        }
        //砸地
        else if (Timer <= 2000 + relaxTime)
        {
            //先查落地：引擎会在触地那一帧把 Y 速度清零，所以这里必须在加重力之前判
            if (npc.velocity.Y == 0 && !npc.noTileCollide && Timer <= 2000)
            {
                SoundEngine.PlaySound(SoundID.Item14, npc.Center);
                SoundEngine.PlaySound(SlamSFX, npc.Center);
                MonoScreenEffectsSystem.StartShakeByAmplitude(10);
                ExtraTrail = false;
                Counter++;
                if (PhanUtil.HostCheck)
                {
                    Vector2 vel = Vector2.UnitX * 3;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 30, vel, FProjID.CoffinSlamShockwave, FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 1f), 1f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 30, -vel, FProjID.CoffinSlamShockwave, FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 1f), 1f, Main.myPlayer);
                    int dir = Math.Sign(Player.Center.X - CoffinArena.FightCenter.X);
                    HalfDropSand(dir, -20, false);
                    if (Main.getGoodWorld)
                    {
                        HalfDropSand(-dir, -20, false);
                    }
                }
                if (Counter <= totalImpact)
                {
                    Timer = 2000;
                    //npc.velocity.Y -= 10;
                }
                //撞完后
                else
                {
                    Timer = -80;
                    npc.velocity *= 0;
                }
            }
            npc.velocity.X *= 0.95f;
            npc.velocity.Y += 0.5f;
            if (npc.velocity.Y > 15)
                npc.velocity.Y = 15;
        }
        //休息时刻
        else
        {
            Timer = 0;
            npc.velocity *= 0;
            npc.velocity.Y -= 5;
        }
        ExtraTrail = true;

    }
    [MonoBehavior<State>(State.WavyShotCircle)]
    public void WavyShotCircle()//开花诅咒弹
    {
        int TelegraphTime = Phase >= 2 ? 80 : 60;
        int ShotDelay = 20;
        NPC npc = coffin.NPC;
        float progress = 1 - (Timer / TelegraphTime);
        Vector2 desiredPos = CoffinArena.FightCenter;
        Movement(desiredPos, 0.1f, 14, 0.08f);
        float dist = npc.Distance(desiredPos);
        if (Timer == 1)
            AI2 = random.NextFloat(MathF.Tau);
        if (dist > 50)
            Timer = -1;
        if (Timer < TelegraphTime && Timer > 0)
        {
            SpawnParticle(progress);
        }
        else if ((Timer - TelegraphTime) % 3 == 0 && Timer.IsInRange(TelegraphTime, TelegraphTime + ShotDelay))
        {
            SoundEngine.PlaySound(BigShotSFX, MaskCenter);
            int shots = Main.getGoodWorld ? 9 : 7;
            if (PhanUtil.HostCheck)
            {
                for (int i = 0; i < shots; i++)
                {
                    float rot = AI2 + MathF.Tau * ((float)i / shots);
                    Vector2 vel = rot.ToRotationVector2() * 6;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), MaskCenter, vel, PProjID.P_CoffinWaveShot, FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 1f, Main.myPlayer, 30, 6);
                }
            }
        }
        else if (Timer > TelegraphTime + ShotDelay + 15 && AI3 < (Phase >= 2 ? 4 : 2)) // + endlag
        {
            AI3++;
            Timer = 0;
        }
    }
    [MonoBehavior<State>(State.WavyShotSlam)]
    public void WavyShotSlam()//开花诅咒弹后的砸地
    {
        NPC npc = coffin.NPC;
        npc.noTileCollide = false;
        if (Timer == 20)
            npc.velocity.Y += 13f;
        if (Timer >= 0)
        {
            if (npc.velocity.Y == 0)
            {
                npc.velocity.X = 0;
                SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.5f }, npc.Center);
                SoundEngine.PlaySound(SlamSFX, npc.Center);
                Timer = -120;
                int dir = Math.Sign(Player.Center.X - CoffinArena.FightCenter.X);
                int leniencyTime = -20;

                MonoScreenEffectsSystem.StartShakeByAmplitude(10);
                HalfDropSand(dir, leniencyTime, true);
                if (Main.getGoodWorld)
                    HalfDropSand(-dir, leniencyTime, true);
                if (FargoSoulsUtil.HostCheck)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 vel = Vector2.UnitX * i * 3;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 30, vel, ModContent.ProjectileType<CoffinSlamShockwave>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 1f), 1f, Main.myPlayer);
                    }
                }
                //依旧反弹
                npc.velocity.Y -= 9f;
                return;
            }
            npc.velocity.Y += 0.2f;
            if (npc.velocity.Y > 0)
                npc.velocity.Y += 0.32f;
            if (npc.velocity.Y > 15)
                npc.velocity.Y = 15;
            ExtraTrail = true;
        }
        else
        {
            if (npc.velocity.Y < 0)
                npc.velocity.Y += 0.2f;
            if (npc.velocity.Y > 0)
                npc.velocity.Y = 0;
        }
    }
    [MonoBehavior<State>(State.Jormungandr)]
    public void Jormungandr()//棺材放鬼魂，半灵撞人
    {
        //const int TotalTime = 60 + 4 * 60;
        NPC npc = coffin.NPC;
        Vector2 desiredPos = CoffinArena.FightCenter;
        desiredPos.Y -= 40;
        Movement(desiredPos, 0.1f, 14, 0.08f);
        float dist = npc.Distance(desiredPos);
        int frameTime = (int)MathF.Floor(60 / Main.npcFrameCount[coffin.Type]);
        //控制帧图
        if (Timer.IsInRange(0, 60))
        {
            if (++npc.frameCounter % frameTime == frameTime - 1)
                if (Frame < Main.npcFrameCount[coffin.Type] - 1)
                    Frame++;
            npc.rotation = MathHelper.SmoothStep(0, MathHelper.PiOver2, Timer / 60f);
        }
        if (Timer < 0)
        {
            npc.velocity *= 0.96f;
            if (++npc.frameCounter % 30 == 29 && Frame > 0)
                Frame--;
            npc.rotation *= 0.95f;
        }
        if (dist >= 50 && Timer > 60)
            Timer = 60;
        //释放灵魂
        if (Timer > 60)
        {
            int Interval = Main.getGoodWorld ? 8 : 10;
            if (Timer <= 60 + 6 * 60 && (Timer - 60) % Interval == 0)
            {
                int index = (int)(Timer / Interval) - 1;
                int side = index % 2 == 0 ? -1 : 1;
                float sweepY = CoffinArena.FightCenter.Y/* + MathHelper.Lerp(-210f, 200f, (index + 0.5f) / 80f)*/ + random.NextFloat(-300f, 300f);   // 全房间均布
                Vector2 velocity = new(side * random.NextFloat(2.5f, 4.5f), -random.NextFloat(5.5f, 7f));
                if (PhanUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), MaskCenter + random.NextVector2InCircle(24), velocity,
                        PProjID.LostSouls, FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.8f), 0f, Main.myPlayer,
                        sweepY, random.NextFloat(-80f, 80f));
                }
                SoundEngine.PlaySound(SoulShotSFX, MaskCenter);
            }
            //释放完后再等4秒转阶段
            if (Timer > 60 + 6 * 60)
                Timer = -2 * 60;
        }
    }
    [MonoBehavior<State>(State.LateralSprint)]
    public void LateralSprint()//横向dash
    {
        const int StageLock = 0, StageAim = 1, StageCharge = 2, StageSprint = 3, StageImpact = 4, StageRecover = 5;

        NPC npc = coffin.NPC;
        Vector2 center = CoffinArena.FightCenter;
        float arenaHalfWidth = CoffinArena.VectorWidth / 2f;
        float coffinHalfWidth = npc.height / 2f;    //旋转 90° 后，半宽来自身高

        //定位阶段
        if (AI3 == StageLock && Timer == 1)
        {
            if (npc.localAI[3] == 0)
            {
                float offset = Player.Center.X - center.X;
                AI2 = offset == 0 ? -1 : Math.Sign(offset);
            }
            else
            {
                float offset = npc.Center.X - center.X;
                AI2 = offset == 0 ? (AI2 == 0 ? -1 : AI2) : -Math.Sign(offset);
            }
            npc.localAI[2] = MathHelper.Clamp(Player.Center.Y, center.Y - 185f, center.Y + 185f); //车道高度：抬高一点，但仍能碰到站地面的玩家
            npc.noTileCollide = true;
            npc.netUpdate = true;
        }
        float dir = AI2 == 0 ? -1 : AI2;
        Vector2 lockPos = new(center.X - dir * (arenaHalfWidth - coffinHalfWidth - 24f), npc.localAI[2]);

        switch ((int)AI3)
        {
            case StageLock:
                ExtraTrail = false;
                npc.rotation = 0f;
                Movement(lockPos, 0.12f, 20, 0.12f);
                if ((npc.Distance(lockPos) < 30f && (Timer >= 30 || Phase < 2)) || Timer >= 90)
                {
                    AI3 = StageAim;
                    Timer = 0;
                    SoundEngine.PlaySound(HandChargeSFX, MaskCenter);
                }
                break;
            case StageAim:
                npc.velocity *= 0.9f;
                npc.velocity.Y += (lockPos.Y - npc.Center.Y) * 0.05f;
                npc.direction = dir >= 0 ? 1 : -1;
                npc.rotation = MathHelper.SmoothStep(0f, dir * (MathHelper.PiOver2 - 0.2f), Utils.GetLerpValue(0f, 20f, Timer, true)); //留 0.2 弧度倾角
                if (Timer >= 30)
                {
                    AI3 = StageCharge;
                    Timer = 0;
                }
                break;
            case StageCharge:
                ExtraTrail = true;
                npc.velocity *= 0.9f;
                SpawnParticle(1f - Utils.GetLerpValue(0f, 60f, Timer, true));
                if (Timer >= 60)
                {
                    SoundEngine.PlaySound(BigShotSFX, MaskCenter);
                    npc.velocity.X = dir * 6f;
                    AI3 = StageSprint;
                    Timer = 0;
                }
                break;
            case StageSprint:
                ExtraTrail = true;
                npc.velocity.Y *= 0.85f;
                npc.velocity.X += dir * 1f;
                if (Math.Abs(npc.velocity.X) > 28f)
                    npc.velocity.X = dir * 28f;
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Sand, -dir * 4f, 0f, 0, default, 1.4f);
                    dust.noGravity = true;
                }
                if (dir * (npc.Center.X - center.X) >= arenaHalfWidth - coffinHalfWidth - 1f || Timer >= 90)
                {
                    AI3 = StageImpact;
                    Timer = 0;
                }
                break;
            case StageImpact:
                ExtraTrail = true;
                if (Timer == 1)
                {
                    npc.velocity = new(-8 * dir.Sign(true), 0);
                    SoundEngine.PlaySound(SlamSFX, npc.Center);
                    MonoScreenEffectsSystem.StartShakeByAmplitude(10);
                    if (PhanUtil.HostCheck)
                    {
                        SpawnWallShockwaves(npc, dir);
                        HalfDropSand(dir.Sign(true), -20, true);
                        if (Main.getGoodWorld)
                            HalfDropSand(-dir.Sign(true), -20, true);
                    }
                }
                npc.velocity *= 0.8f;
                if (Timer >= 60)
                {
                    AI3 = StageRecover;
                    Timer = 0;
                }
                break;
            case StageRecover:
                ExtraTrail = true;
                npc.rotation *= 0.85f; //回位途中转正
                Vector2 returnPos = new(center.X + dir * 240f, center.Y); //半个半场宽
                Movement(returnPos, 0.12f, 18, 0.12f);
                if (npc.Distance(returnPos) < 25f || Timer >= (Phase >= 2 ? 110 : 60))
                {
                    if (npc.localAI[3] + 1 < (Phase >= 2 ? 3 : 2)) //两轮：第一轮站玩家反侧，第二轮站撞完墙的这一侧
                    {
                        npc.localAI[3]++;
                        AI3 = StageLock;
                        Timer = 0;
                    }
                    else
                    {
                        npc.localAI[3] = 0;
                        ExtraTrail = false;
                        Timer = -1;
                    }
                }
                break;
        }
    }
    [MonoBehavior<State>(State.CircleRound)]
    public void CircleRound()//尾杀，无限循环
    {
        NPC npc = coffin.NPC;
        Vector2 desiredPos = CoffinArena.FightCenter;
        Movement(desiredPos, 0.1f, 14, 0.08f);
        float dist = npc.Distance(desiredPos);
        if (dist > 50)
            Timer = -1;
        foreach (Projectile proj in Main.ActiveProjectiles) //挡掉半灵打的弹幕
        {
            if (proj.type == FProjID.CoffinWaveShot && proj.ai[2] == 1f && proj.Hitbox.Intersects(npc.Hitbox))
                proj.Kill();
        }
        if (Timer > 60)
        {
            if (AI1 < 60)
            {
                if (AI1 % 20 == 0 && PhanUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), MaskCenter, Vector2.Zero, PProjID.CoffinShockWave, 0, 0, Main.myPlayer, 10, 100);
                SpawnParticle(1 - AI1 / 60f);
            }
            else if (AI1 == 60)
            {
                MonoScreenEffectsSystem.StartShakeByAmplitude(10);
                SoundEngine.PlaySound(BigShotSFX, MaskCenter);
                for (int i = 0; i < 60; i++)
                {
                    if (PhanUtil.HostCheck)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), MaskCenter, random.NextVector2OnCircle(10) * random.NextFloat(0.5f, 1.2f), FProjID.CoffinWaveShot,
                            FargoSoulsUtil.ScaledProjectileDamage(npc.damage), 0, Main.myPlayer);
                    }
                }
                AI1 = 0;
                Timer = -120;
                return;
            }
            AI1++;
        }
    }
    [MonoBehavior<State>(State.PhaseTransition1st)]
    public void PhaseTransition1st()//转阶段1st
    {
        HoverSound();
        NPC npc = coffin.NPC;
        const int TransTime = 90;
        npc.velocity = (CoffinArena.FightCenter - npc.Center) * 0.05f;
        npc.rotation = Main.rand.NextFloat(MathF.Tau * 0.06f * (Timer / TransTime));
        SoundEngine.PlaySound(SpiritDroneSFX, npc.Center);
        npc.HitSound = SoundID.NPCHit4;
        npc.netUpdate = true;
        if (Phase < 2)
            Phase = 2;
    }
    [MonoBehavior<State>(State.PhaseTransition2nd)]
    public void PhaseTransition2nd()//转阶段2nd
    {
        HoverSound();
        NPC npc = coffin.NPC;
        Vector2 desiredPos = CoffinArena.FightCenter;
        Movement(desiredPos, 0.1f, 14, 0.08f);
        npc.rotation *= 0.85f;
        if (Phase < 3)
            Phase = 3;
    }
    #endregion
    //挤人
    public static void CrowdedPlayer(NPC npc)
    {
        Player localPlayer = Main.LocalPlayer;
        Vector2 nextCenter = localPlayer.Center + localPlayer.velocity;
        Rectangle nextFrameHitbox = new((int)(nextCenter.X - localPlayer.Hitbox.Width / 2), (int)(nextCenter.Y - localPlayer.Hitbox.Height / 2), localPlayer.Hitbox.Width, localPlayer.Hitbox.Height);
        if (nextFrameHitbox.Intersects(npc.Hitbox))
        {
            if (!localPlayer.Hitbox.Intersects(npc.Hitbox))
            {
                localPlayer.velocity.X /= 2;
                localPlayer.position.X -= Math.Sign(localPlayer.Center.X - npc.Center.X) * 8;
            }
            localPlayer.velocity -= localPlayer.DirectionTo(npc.Center);
        }
    }
    //场地处理
    public static void ManageArena()
    {
        Player localPlayer = Main.LocalPlayer;
        Vector2 arenaCenter = CoffinArena.FightCenter;
        float distanceX = Math.Abs(localPlayer.Center.X - arenaCenter.X);
        float threshold = CoffinArena.VectorWidth / 2f;
        int DustType = DustID.Sand;
        if (localPlayer.active && !localPlayer.dead && !localPlayer.ghost) //pull into arena
        {
            if (distanceX > threshold && distanceX < threshold * 4f)
            {

                Vector2 movement = Vector2.UnitX * (arenaCenter.X - localPlayer.Center.X);
                float difference = movement.Length() - threshold;
                movement.Normalize();
                movement *= difference < 17f ? difference : 17f;
                localPlayer.position += movement;

                for (int i = 0; i < 10; i++)
                {
                    int d = Dust.NewDust(localPlayer.position, localPlayer.width, localPlayer.height, DustType, 0f, 0f, 0, default, 1.25f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 5f;
                }
            }
        }

        for (int i = -1; i <= 1; i += 2) // Sand walls
        {
            float posX = arenaCenter.X + i * threshold;
            for (int y = 0; y <= CoffinArena.Height * 2; y++)
            {
                float posY = arenaCenter.Y + (CoffinArena.VectorHeight / 2f) - (8f * y);
                if (Main.rand.NextBool(3) && !Main.tile[(int)posX / 16, (int)posY / 16].HasUnactuatedTile)
                {
                    int d = Dust.NewDust((posX - 4f) * Vector2.UnitX + (posY - 8f) * Vector2.UnitY, 8, 16, DustType, 0f, 0f, 0, default, 1.25f);
                    Main.dust[d].noGravity = true;
                }
            }
        }
    }
    public void HoverSound() => coffin.HoverSound();
    public void Movement(Vector2 pos, float accel = 0.03f, float maxSpeed = 20, float decel = 0.03f)
        => coffin.Movement(pos, accel, maxSpeed, 5, decel, 20);
    public bool Targeting() => coffin.Targeting();
    private static P_CursedCoffin Self(NPC npc) => npc.GetGlobalNPC<P_CursedCoffin>();
    public void HalfDropSand(int dir, int leniencyTime = -20, bool FromWall = true)
    {
        NPC npc = coffin.NPC;
        Vector2 center = CoffinArena.FightCenter;
        if (!FargoSoulsUtil.HostCheck)
            return;
        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.DD2OgreSmash, 0, 0f, Main.myPlayer);
        for (int i = -1; i < 20; i++)
        {
            Vector2 projPos = center + dir * Vector2.UnitX * (CoffinArena.Width * 8) * (i / 20f);
            Point tile = projPos.ToTileCoordinates();
            for (int safety = 0; safety < 100; safety++)
            {
                if (Main.tile[tile.X, tile.Y].HasUnactuatedTile && WorldGen.SolidTile(tile))
                {
                    break;
                }
                tile.Y--;
            }
            projPos = tile.ToWorldCoordinates();
            projPos.X += Main.rand.NextFloat(-10f, 10f);
            projPos.Y += Main.rand.NextFloat(-3f, 4f);
            int fromWall = 20 - i;
            if (i == -1)
            {
                projPos.X = CoffinArena.FightCenter.X + dir * (CoffinArena.Width * 8f - 24f);
                fromWall = 0;
            }
            Projectile.NewProjectile(npc.GetSource_FromThis(), projPos, Vector2.Zero, ModContent.ProjectileType<FallingSandstone>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.75f, 2), 0f, Main.myPlayer, leniencyTime + (FromWall ? (int)(fromWall * 1.5f) : 0) + Main.rand.Next(60, 80));
        }
    }
    public void SpawnParticle(float progress, Vector2? center = null)
    {
        Vector2 anchor = center ?? MaskCenter;
        Vector2 sparkDir = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi);
        float sparkDistance = (120 * progress) * Main.rand.NextFloat(0.6f, 1.3f);
        Vector2 sparkCenter = anchor + sparkDir * sparkDistance * 2;
        float sparkTime = 15;
        Vector2 sparkVel = (anchor - sparkCenter) / sparkTime;
        float sparkScale = 2f - progress * 1.2f;
        new FargoSparkParticle(sparkCenter, sparkVel, GlowColor, sparkScale, (int)sparkTime).Spawn();
    }
    private static void SpawnWallShockwaves(NPC npc, float dir)
    {
        int type = PProjID.CoffinWallShockwave;
        float wallX = CoffinArena.FightCenter.X + dir * (CoffinArena.VectorWidth / 2f - 20f);
        int damage = FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.9f, 2);
        for (int i = -1; i <= 1; i += 2)
        {
            Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(wallX, npc.Center.Y), Vector2.UnitY * i * 9f,
                type, damage, 1f, Main.myPlayer, dir);
        }
    }
}

