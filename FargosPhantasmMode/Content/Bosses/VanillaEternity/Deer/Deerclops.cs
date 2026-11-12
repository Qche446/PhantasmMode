using FargosPhantasmMode.Common;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Graphics.CameraModifiers;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Deer;

public class P_Deerclops : PModeNPCBehaviour
{
    public byte Phase = 1;
    public int State { get => (int)Npc.ai[0]; set => Npc.ai[0] = value; }
    public ref float Timer => ref Npc.ai[1];
    public ref float IceSpikeTime => ref Npc.localAI[1];
    public override int NPCType => NPCID.Deerclops;
    public bool DroppedSummon;
    public bool haltMovement;
    public int SpikesMade;
    public ref int BerserkSpeedupTimer => ref fdeer.BerserkSpeedupTimer;
    public ref int TeleportTimer => ref fdeer.TeleportTimer;
    public ref int WalkingSpeedUpTimer => ref fdeer.WalkingSpeedUpTimer;
    public ref int ForceDespawnTimer => ref fdeer.ForceDespawnTimer;
    public ref int LockDirection => ref fdeer.LockDirection;
    //决定双向冰刺后是否放激光（phase3）或者雪球（phase2）
    public ref bool DoLaserAttack => ref fdeer.DoLaserAttack;
    public int IceSpikesAi1
    {
        get
        {
            int ai1 = -20;
            if (State == 4)
            {
                ai1 -= 30;
                if (Phase >= 2)
                    ai1 -= 30;
                if (Phase >= 3)
                    ai1 -= 120;
            }
            return ai1;
        }
    }
    const int TeleportThreshold = 800;
    const int MaxBerserkTime = 600;
    public NPC Npc;
    public Deerclops fdeer;
    public Player Player => Main.player[Npc.target];
    public override void OnFirstTick(NPC npc) => npc.GetGlobalNPC<Deerclops>().RunEmodeAI = false;
    public override void OnSpawn(NPC npc, IEntitySource source) { Npc = npc; fdeer = npc.GetGlobalNPC<Deerclops>(); }
    public override bool SafePreAI(NPC npc)
    {
        NPC.deerclopsBoss = npc.whoAmI;
        EModeGlobalNPC.deerBoss = npc.whoAmI;
        if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            npc.TargetClosest();
        //领域
        if (Main.LocalPlayer.active && !Main.LocalPlayer.ghost && !Main.LocalPlayer.dead && npc.Distance(Main.LocalPlayer.Center) < 1000f)
        {
            Main.LocalPlayer.AddBuff(ModContent.BuffType<LowGroundBuff>(), 2);
            Main.LocalPlayer.buffImmune[BuffID.Frozen] = true;
            Main.LocalPlayer.buffImmune[BuffID.Slow] = true;
        }
        if (npc.ai[0] != 0)
        {
            npc.alpha -= 10;
            if (npc.alpha < 0)
                npc.alpha = 0;
        }
        if (!Main.IsItDay())
            Lighting.AddLight(npc.Center, TorchID.Red);

        bool Faded = npc.Distance(Player.Center) >= 450f;
        npc.localAI[3] = MathHelper.Clamp(npc.localAI[3] + Faded.ToDirectionInt(), 0f, 30f);
        npc.dontTakeDamage = npc.localAI[3] >= 30;
        float num2 = Utils.Remap(npc.localAI[3], 0f, 30f, 0f, 1f);
        if (num2 > 0f)
        {
            float num3 = Main.rand.NextFloat() * num2 * 3f;
            while (num3 > 0f)
            {
                num3 -= 1f;
                Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Asphalt, 0f, -3f, 0, default, 1.4f).noGravity = true;
            }
        }
        if (npc.homeTileX == -1 && npc.homeTileY == -1)
        {
            Point point = npc.Bottom.ToTileCoordinates();
            npc.homeTileX = point.X;
            npc.homeTileY = point.Y;
            npc.netUpdate = true;
            npc.timeLeft = 86400;
        }
        switch (State)
        {
            case -1: npc.localAI[3] = -10;
                break;
            //追击+决策
            case 0: Pursue(npc); break;
            //前方冰刺
            case 1: FrontIceSpike(npc); break;
            //前方雪球抛射
            case 2: RubbleUp(npc); break;
            //怒了一下，用于转阶段等
            case 3: JustRoar(npc); break;
            //两边冰刺
            case 4: BothSideIceSpike(npc); break;
            //暗影手惩罚
            case 5: HandsPunish(npc); break;
            case 6: break;
            case 7: break;
            case 8: WaitForLeave(npc); break;
        }
        Rectangle target = Player.Hitbox;
        (bool Grounded, bool FrontFree, bool Straddles, bool AboveFeet) foot = ProbeFoot(npc, target);
        MoveHorizontal(npc, target.Center.X, MoveSpeed(npc), haltMovement);
        ApplyGravity(npc, target.Center.X, foot);
        haltMovement = false;
        EModeUtils.DropSummon(npc, "DeerThing2", NPC.downedDeerclops, ref DroppedSummon);
        return false;
    }
    private void Pursue(NPC npc)
    {
        Timer++;
        //原法的神秘加速逻辑
        if (++WalkingSpeedUpTimer > 900) 
            WalkingSpeedUpTimer = 900;
        npc.position.X += npc.velocity.X * Math.Max(0, WalkingSpeedUpTimer - 90) / 90f;
        if (TeleportTimer < TeleportThreshold)
        {
            if (Phase >= 3)
                npc.position.X += npc.velocity.X;
            if (npc.velocity.Y == 0)
            {
                if (Phase >= 2)
                    npc.position.X += npc.velocity.X;
                if (BerserkSpeedupTimer > 0)
                    npc.position.X += npc.velocity.X * 4f * BerserkSpeedupTimer / MaxBerserkTime;
            }
        }
        if (Phase >= 2)
        {
            if (Phase < 3 && npc.life < npc.lifeMax * .33)
            {
                npc.ai[0] = 3;
                npc.ai[1] = 0;
                npc.netUpdate = true;
                return;
            }

            if (TeleportTimer > TeleportThreshold)
            {
                WalkingSpeedUpTimer = 0;

                npc.velocity.X *= 0.9f;
                npc.dontTakeDamage = true;
                npc.localAI[1] = 0; //reset walls attack counter

                if (Phase >= 2 && Main.LocalPlayer.active && !Main.LocalPlayer.ghost && !Main.LocalPlayer.dead && npc.Distance(Main.LocalPlayer.Center) < 1600)
                {
                    FargoSoulsUtil.AddDebuffFixedDuration(Main.LocalPlayer, BuffID.Darkness, 2);
                    FargoSoulsUtil.AddDebuffFixedDuration(Main.LocalPlayer, BuffID.Blackout, 2);
                }

                if (npc.alpha == 0)
                {
                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    foreach (Projectile p in Main.ActiveProjectiles)
                    {
                        if (p.TypeAlive(ProjectileID.DeerclopsRangedProjectile))
                            p.Kill();
                    }
                    Deerclops.SpawnFreezeHands(npc, Main.player[npc.target]);
                }

                npc.alpha += 5;
                if (WorldSavingSystem.SwarmActive)
                    npc.alpha -= 2;
                if (npc.alpha > 255)
                {
                    npc.alpha = 255;

                    npc.localAI[3] = 30;

                    if (npc.HasPlayerTarget) //teleport
                    {
                        float distance = 16 * 14 * Math.Sign(npc.Center.X - Main.player[npc.target].Center.X);
                        distance *= -1f; //alternate back and forth

                        if (TeleportTimer == TeleportThreshold + 10) //introduce randomness
                        {
                            if (Main.rand.NextBool())
                                distance *= -1f;

                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);

                            DoLaserAttack = !DoLaserAttack; //guarantee he alternates wall attacks at some point in the fight
                            npc.netUpdate = true;
                        }

                        npc.Bottom = Main.player[npc.target].Bottom + distance * Vector2.UnitX;

                        npc.direction = Math.Sign(Main.player[npc.target].Center.X - npc.Center.X);
                        npc.velocity.X = 3.4f * npc.direction;
                        npc.velocity.Y = 0;

                        int addedThreshold = 180;
                        if (Phase >= 3)
                            addedThreshold -= 30;
                        if (WorldSavingSystem.MasochistModeReal)
                            addedThreshold -= 30;

                        if (TeleportTimer > TeleportThreshold + addedThreshold)
                        {
                            TeleportTimer = 0;
                            npc.velocity.X = 0;
                            npc.ai[0] = 4;
                            npc.ai[1] = 0;
                            (npc).netUpdate = true;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                        }
                    }
                }
                else
                {
                    TeleportTimer = TeleportThreshold;

                    if (npc.localAI[3] > 0)
                        npc.localAI[3] -= 3; //remove visual effect
                }

                return;
            }
        }
        else if (npc.GetLifePercent() < .66f) 
        {
            State = 3;
            Timer = 0;
            npc.netUpdate = true;
        }
        //Vanilla 决策状态
        if (ShouldRunAway(true))
        {
            State = 8;
            Timer = 0;
            IceSpikeTime = 0;
            npc.netUpdate = true;
            return;
        }
        Vector2 vector = npc.Bottom + new Vector2(0f, -32f);
        Vector2 vector2 = Player.Hitbox.ClosestPointInRect(vector);
        Vector2 vector3 = vector2 - vector;
        float dx = Math.Abs(vector3.X);
        (vector2 - npc.Center).Length();
        float num15 = 0.6f;
        //基本位于同一水平面
        bool flag4 = dx >= Math.Abs(vector3.Y) * num15 || vector3.Length() < 48f;
        //目标大致齐平
        bool flag5 = vector3.Y <= 100 + Player.height && vector3.Y >= -200f;
        void NextState(int nextState)
        {
            npc.velocity.X = 0;
            State = nextState;
            Timer = 0;
            npc.netUpdate = true;
        }
        //两侧冰刺
        if (dx < 120 && flag5 && npc.velocity.Y == 0 && IceSpikeTime >= 2)
        {
            NextState(4);
            IceSpikeTime = 0f;
            SpikesMade = 0;
            return;
        }
        //前方冰刺
        if (Math.Abs(vector3.X) < 120f && flag5 && npc.velocity.Y == 0f && flag4)
        {
            NextState(1);
            //SpikesMade = 0;
            IceSpikeTime += 1f;
            return;
        }
        //等太久，抛射雪球
        if (npc.velocity.Y == 0f && npc.velocity.X != 0f && Timer >= 240f)
        {
            NextState(2);
            IceSpikeTime = 0f;
            SpikesMade = 0;
            return;
        }
        //召唤暗影手
        if (npc.velocity.Y == 0f && npc.velocity.X == 0f && Timer >= 90)
        {
            NextState(5);
            IceSpikeTime = 0f;
            return;
        }
        bool flag9 = !Main.player[npc.target].buffImmune[BuffID.Slow] && Main.player[npc.target].FindBuffIndex(BuffID.Slow) == -1;
        //吼了一下
        if (npc.velocity.Y == 0f && flag9 && Math.Abs(vector3.X) > 100f && Timer >= 120)
        {
            NextState(3);
            IceSpikeTime = 0f;
            return;
        }
    }
    private void FrontIceSpike(NPC npc)
    {
        WalkingSpeedUpTimer = 0;
        haltMovement = true;
        MakeSpikesForward(npc);
        if (Timer >= 30)
        {
            Timer += 0.5f;
            npc.frameCounter += 0.5f;
        }
        if (++Timer >= 80)
        {
            State = 0;
            Timer = 0;
            SpikesMade = 0;
            npc.netUpdate = true;
        }
    }
    private void RubbleUp(NPC npc)
    {
        const int total = 20;
        const int startFrame = 32;
        Timer++;
        if (Timer == startFrame - 20)
            SoundEngine.PlaySound(SoundID.DeerclopsScream, npc.Center);
        if (Timer == startFrame)
            SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack, npc.Center);
        haltMovement = true;
        Point source = npc.Top.ToTileCoordinates();
        source.X += npc.direction * 3;
        source.Y -= 10;
        if (Timer < startFrame && !Main.dedServ)
        {
            //你法的落石点的雪尘预警
            for (int count = 0; count < 2; count++)
            {
                int offset = Main.rand.Next(1, 60 - startFrame);
                int x = source.X + offset * npc.direction;
                int y = FindSpikeY(x, source.Y + Main.rand.Next(0, 35) - 12);
                Vector2 pos = new(x * 16 + 8, y * 16 - 8);
                pos.X += Main.rand.NextFloat(-30f, 30f);
                int d = Dust.NewDust(pos, 0, 0, DustID.SnowSpray);
                Main.dust[d].velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -1f));
            }
        }
        if (PhanUtil.HostCheck && Timer >= startFrame && SpikesMade < total)
        {
            if (SpikesMade == 0)
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(npc.Center, new Vector2(0f, -1f), 20f, 6f, 30, 1000f, "Deerclops"));
            ShootRubbleUp(npc, source, SpikesMade);
            SpikesMade++;
        }
        if (Timer >= 60)
        {
            State = 0;
            Timer = 0;
            SpikesMade = 0;
            npc.netUpdate = true;
        }
    }
    private void JustRoar(NPC npc)
    {
        WalkingSpeedUpTimer = 0;
        if (Phase >= 2)
        {
            IceSpikeTime = 0f;
            npc.localAI[3] = 0;
            if (Timer >= 30)
            {
                Main.dayTime = false;
                Main.time = 16200;
            }
        }
        else if (Phase < 2 && npc.GetLifePercent() < .66f)
        {
            Phase = 2;
            npc.netUpdate = true;
        }
        if (Phase >= 3)
        {
            BerserkSpeedupTimer = MaxBerserkTime;
            if (!Main.dedServ)
                FargoSoulsUtil.ScreenshakeRumble(6);
            if (npc.ai[1] > 30) //突脸这一块
            {
                if (npc.HasValidTarget)
                    npc.position = Vector2.Lerp(npc.position, Main.player[npc.target].Center - 450 * Vector2.UnitY, 0.2f);
            }
        }
        else if (npc.GetLifePercent() < .33f)
        {
            Phase = 3;
            npc.netUpdate = true;
        }
        if (Timer == 30f)
        {
            SoundEngine.PlaySound(SoundID.DeerclopsScream, npc.Center);
        }
        haltMovement = true;
        if ((int)Timer % 4 == 0 && Timer >= 28f)
        {
            PunchCameraModifier modifier5 = new PunchCameraModifier(npc.Center, (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 20f, 6f, 20, 1000f, "Deerclops");
            Main.instance.CameraModifiers.Add(modifier5);
            if (Main.netMode != NetmodeID.Server)
            {
                Player player = Main.LocalPlayer;
                int num17 = 32;
                int timeToAdd = 720;
                if (!player.dead && player.active && player.FindBuffIndex(num17) == -1 && (player.Center - npc.Center).Length() < 800f && !player.creativeGodMode)
                {
                    player.AddBuff(num17, timeToAdd);
                }
            }
        }
        if (++Timer >= 60f)
        {
            State = 0;
            Timer = 0f;
            npc.netUpdate = true;
        }
    }
    private void BothSideIceSpike(NPC npc)
    {
        WalkingSpeedUpTimer = 0;
        haltMovement = true;
        int cooldown = 100;
        if (Phase >= 3)
            cooldown *= 2;
        if (TeleportTimer > TeleportThreshold - cooldown)
            TeleportTimer = TeleportThreshold - cooldown;
        if (Timer == 0)
        {
            LockDirection = npc.direction;
            if (Phase >= 2)
            {
                if (npc.alpha == 0) //i.e. dont randomize when coming out of tp
                    DoLaserAttack = Main.rand.NextBool();
                npc.netUpdate = true;
                if (PhanUtil.HostCheck && DoLaserAttack)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
                }
            }
            if (PhanUtil.HostCheck)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.DD2OgreSmash, 0, 0f, Main.myPlayer);
            }
        }
        npc.direction = LockDirection;
        Vector2 eye = npc.Center + new Vector2(64 * npc.direction, -24f) * npc.scale;
        MakeSpikesBothSides(npc);
        if (Timer < 35)
            Timer++;
        Timer++;
        if (DoLaserAttack && Timer >= 70)
        {
            //激光
            if (Phase >= 3)
            {
                const float baseIncrement = 0.33f;
                float increment = baseIncrement;
                if (Timer == 70)
                {
                    float time = (90 - 70) / baseIncrement - 5;
                    time *= 5; 
                    float rotation = MathHelper.Pi / time * -npc.direction;

                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), eye, Vector2.UnitY, ModContent.ProjectileType<DeerclopsDeathray>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 2f), 0f, Main.myPlayer, rotation, time);
                }

                Timer += increment; 

                if (Timer < 90)
                    return; 
            }
            //雪球
            else
            {
                Timer += 0.33f;

                if (Timer >= 89)
                {
                    State = 2; 
                    Timer = 0;
                    npc.frameCounter = 0;
                    npc.netUpdate = true;
                    return;
                }
            }

            if (Timer < 90)
                return; 
        }
        if (Timer >= 90f)
        {
            State = 0;
            Timer = 0f;
            SpikesMade = 0;
            npc.netUpdate = true;
        }
    }
    private void HandsPunish(NPC npc)
    {
        if (Timer == 30f)
        {
            SoundEngine.PlaySound(in SoundID.DeerclopsScream, npc.Center);
        }
        haltMovement = true;
        if ((int)Timer % 4 == 0 && Timer >= 28f)
        {
            PunchCameraModifier modifier = new PunchCameraModifier(npc.Center, (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 20f, 6f, 20, 1000f, "Deerclops");
            Main.instance.CameraModifiers.Add(modifier);
        }
        if (Timer == 30f)
        {
            if (PhanUtil.HostCheck)
            {
                for (int i = 0; i < 6; i++)
                {
                    Projectile.RandomizeInsanityShadowFor(Player, isHostile: true, out var spawnposition, out var spawnvelocity, out var ai, out var ai2);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawnposition, spawnvelocity, ProjectileID.InsanityShadowHostile, 15, 0f, Main.myPlayer, ai, ai2);
                }
                if (npc.HasValidTarget && Math.Abs(npc.Center.X - Player.Center.X) < 16 * 3 && Player.Bottom.Y < npc.Top.Y - 16 * 5)
                {
                    Deerclops.SpawnFreezeHands(npc, Player);
                }
            }
        }
        if (++Timer >= 60f)
        {
            State = 0;
            Timer = 0f;
            npc.netUpdate = true;
        }
    }
    private void WaitForLeave(NPC npc)
    {
        ++Timer;
        if (Timer <= 180)
        {
            if (!ShouldRunAway(false))
            {
                State = 0;
                Timer = 0;
                IceSpikeTime = 0;
                npc.netUpdate = true;
                return;
            }
        }
        else
        {
            haltMovement = true;
            if (Timer == 210)
            {
                SoundEngine.PlaySound(in SoundID.DeerclopsScream, npc.Center);
            }
            if ((int)Timer % 4 == 0 && Timer >= 28 + 180)
            {
                PunchCameraModifier modifier2 = new PunchCameraModifier(npc.Center, (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 20f, 6f, 20, 1000f, "Deerclops");
                Main.instance.CameraModifiers.Add(modifier2);
            }
            if (Timer >= 180 + 40)
            {
                npc.life = -1;
                npc.HitEffect();
                npc.active = false;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, -1f);
                }
                return;
            }
        }
    }
    #region 辅助方法
    private static float MoveSpeed(NPC npc) => 3.5f + 1f * (1f - (float)npc.life / npc.lifeMax);
    private static void MoveHorizontal(NPC npc, float targetX, float speed, bool haltMovement)
    {
        float dx = targetX - npc.Center.X;
        if (Math.Abs(dx) < 80f || haltMovement)
        {
            npc.velocity.X *= 0.9f;
            if (npc.velocity.X > -0.1f && npc.velocity.X < 0.1f)
                npc.velocity.X = 0f;
            return;
        }
        npc.velocity.X = MathHelper.Lerp(npc.velocity.X, Math.Sign(dx) * speed, 1f / 4f);
    }
    private static (bool Grounded, bool FrontFree, bool Straddles, bool AboveFeet) ProbeFoot(NPC npc, Rectangle target)
    {
        Vector2 feet = new(npc.Center.X - 20f, npc.Bottom.Y - 20f);
        bool acceptTop = npc.Bottom.Y >= target.Top;
        return (
            Collision.SolidCollision(feet, 40, 20, acceptTop),
            !Collision.SolidCollision(feet + new Vector2(40f * npc.direction, 0f), 16, 80, acceptTop),
            feet.X < target.X && feet.X + 40f > target.Right,
            feet.Y + 20f < target.Bottom - 16f);
    }
    private static void ApplyGravity(NPC npc, float targetX, (bool Grounded, bool FrontFree, bool Straddles, bool AboveFeet) foot)
    {
        if (foot.Grounded)
            npc.localAI[0] = 0f;
        if ((foot.Straddles || Math.Abs(targetX - npc.Center.X) < 80f) && foot.AboveFeet)
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.8f, 0.001f, 16f);
        else if (foot.Grounded)
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.4f, -8f, 0f);
        else if (npc.velocity.Y == 0f && foot.FrontFree)
        {
            npc.velocity.Y = -8f;
            npc.localAI[0] = 1f;
        }
        else
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.4f, -8f, 16f);
    }
    private bool ShouldRunAway(bool isChasing)
    {
        Player player = Main.player[Npc.target];
        if (!player.active || player.dead || Npc.Distance(player.Center) >= 2000 || !isChasing)
        {
            Npc.TargetClosest();
            player = Main.player[Npc.target];
            if (!player.active || player.dead || Npc.Distance(player.Center) >= 2000 || !isChasing)
                return true;
        }
        return false;
    }
    private void MakeSpikesForward(NPC npc)
    {
        const int burstStart = 36;
        const int perBurst = 4;
        const int total = 20;
        if (!PhanUtil.HostCheck || SpikesMade >= total || Timer < burstStart + SpikesMade)
            return;
        if (SpikesMade == 0)
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(npc.Center, new Vector2(0f, 1f), 20f, 6f, 30, 1000f, "Deerclops"));
        Point foot = npc.Bottom.ToTileCoordinates();
        foot.X += npc.direction * 3;
        for (int i = SpikesMade; i < SpikesMade + perBurst && i < total; i++)
        {
            int x = foot.X + i * npc.direction;
            int y = FindSpikeY(x, foot.Y);
            if (!WorldGen.ActiveAndWalkableTile(x, y))
                continue;
            Vector2 velocity = new Vector2(0f, -1f).RotatedBy(i * npc.direction * 0.7f * (MathHelper.PiOver4 / total));
            
            int ai2 = i == total - 1 ? (Phase >= 2 ? DeerclopsIceSpike.D1_1 : DeerclopsIceSpike.D1_2) : DeerclopsIceSpike.SplitNone;
            Projectile.NewProjectile(npc.GetSource_FromAI(), new Vector2(x * 16 + 8, y * 16 - 8), velocity, PProjID.DeerclopsIceSpike, 13, 0f, Main.myPlayer, IceSpikesAi1, 0.1f + Main.rand.NextFloat() * 0.1f + i * 1.1f / total, ai2);
        }
        SpikesMade += perBurst;
    }
    private static void ShootRubbleUp(NPC npc, Point source, int whichOne)
    {
        const int total = 20;
        int x = source.X + whichOne * npc.direction;
        for (int i = 0; i < 35; i++)
        {
            int y = source.Y + i;
            if (!WorldGen.SolidTile(x, y))
                continue;
            Vector2 velocity = new Vector2(0f, -1f).RotatedBy(whichOne * npc.direction * 0.7f * (MathHelper.PiOver4 / total));
            velocity *= 8f + Main.rand.NextFloat() * 8f;
            Projectile.NewProjectile(npc.GetSource_FromAI(), new Vector2(x * 16 + 8, y * 16 - 8), velocity, PProjID.DeerclopsRangedProjectile, 18, 0f, Main.myPlayer, 0f, 6 + Main.rand.Next(6), 25);
            break;
        }
    }
    private void MakeSpikesBothSides(NPC npc)
    {
        const int burstStart = 56;
        const int spacing = 2;
        const int total = 15;
        if (!PhanUtil.HostCheck || SpikesMade >= total || Timer < burstStart + SpikesMade * spacing)
            return;
        if (SpikesMade == 0)
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(npc.Center, new Vector2(0f, 1f), 20f, 6f, 30, 1000f, "Deerclops"));
        Point foot = npc.Bottom.ToTileCoordinates();
        for (int side = -1; side <= 1; side += 2)
        {
            int x = foot.X + SpikesMade * side;
            int y = FindSpikeY(x, foot.Y);
            if (!WorldGen.ActiveAndWalkableTile(x, y))
                continue;
            Vector2 velocity = new Vector2(0f, -1f).RotatedBy(-SpikesMade * side * 0.7f * (MathHelper.PiOver4 / total));
            int ai2 = SpikesMade == total - 1 ? (Phase >= 2 ? DeerclopsIceSpike.D2_1 : DeerclopsIceSpike.Dp2_1) : DeerclopsIceSpike.SplitNone;
            Projectile.NewProjectile(npc.GetSource_FromAI(), new Vector2(x * 16 + 8, y * 16 - 8), velocity, PProjID.DeerclopsIceSpike, 13, 0f, Main.myPlayer, IceSpikesAi1, 0.1f + Main.rand.NextFloat() * 0.1f + SpikesMade * 1.1f / total, ai2);
        }
        SpikesMade++;
    }
    private static int FindSpikeY(int x, int startY)
    {
        int y = startY;
        for (int i = 0; i < 20 && y > 10; i++)
        {
            if (!WorldGen.SolidTile(x, y))
                break;
            y--;
        }
        for (int i = 0; i < 20 && y < Main.maxTilesY - 10; i++)
        {
            if (WorldGen.ActiveAndWalkableTile(x, y))
                break;
            y++;
        }
        return y;
    }
    #endregion
}
