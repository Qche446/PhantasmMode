using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.Items.Summons;
using FargowiltasSouls.Content.WorldGeneration;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Monochrome.Common.MonoUtil;
using Monochrome.Common.MonoUtil.Mathematics.Statistics;
using Monochrome.Content.Ai;
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
        RandomStuff,//扔垃圾
        CircleRound,//指鬼魂环绕棺材
        Count,//数量
    }
    private readonly List<State> Attacks =
    [
        State.HoveringForSlam,
        State.WavyShotCircle,
        State.Jormungandr,
        State.RandomStuff,
        State.CircleRound,
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
        random = new(MonoRng.Derive(0, (ulong)npc.type, (ulong)npc.whoAmI, 0));
    }
    public override bool SafePreAI(NPC npc)
    {
        npc.defense = npc.defDefense;
        if (Main.npc.Any(p => p.TypeAlive<CursedSpirit>()))
            npc.defense += 15;
        if (Fsm != null && Fsm.Current != State.RandomStuff)
            npc.rotation = 0;
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
            {
                npc.velocity.Y += 0.2f;
            }
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
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 30f, vel, ModContent.ProjectileType<CoffinSlamShockwave>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.6f, 2), 1f, Main.myPlayer);
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
                        coffin.Music = 81;
                    }
                    //砸地后反弹速度
                    npc.velocity.Y -= 9f;
                    if (!Main.dedServ)
                    {
                        ScreenShakeSystem.StartShake(10f, (float)Math.PI * 2f, null, 0.5f);
                    }
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
            Movement(desiredPos, 0.1f, 10, 5, 0.08f, 20);
        }
    }
    [MonoBehavior<State>(State.SlamWShockwave)]
    public void SlamWShockwave()//释放cjb的砸地
    {
        NPC npc = coffin.NPC;
        npc.noTileCollide = npc.Bottom.Y + npc.velocity.Y < Player.Bottom.Y - 16;

    }
    [MonoBehavior<State>(State.PhaseTransition1st)]
    public void PhaseTransition1st()
    {

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
    public void Movement(Vector2 pos, float accel = 0.03f, float maxSpeed = 20, float lowspeed = 5, float decel = 0.03f, float slowdown = 30)
        => coffin.Movement(pos, accel, maxSpeed, lowspeed, decel, slowdown);
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
}

