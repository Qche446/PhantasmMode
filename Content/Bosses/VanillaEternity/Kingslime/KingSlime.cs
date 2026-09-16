using FargosPhantasmMode.Common;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Kingslime
{
    public class P_KingSlime : PModeNPCBehaviour
    {
        public int SpikeRainCounter; 
        public bool LandingAttackReady;
        public bool CurrentlyJumping;
        public float JumpTimer = 0;
        const int SpecialJumpTime = 60 * 15;
        public int SpecialJumpWindupTimer;
        const int SummonWaves = 5;
        public float SummonCounter = SummonWaves - 1;
        public bool SpecialJumping = false;
        public bool SuperSpecialJump = true;
        public float SuperSpecialJumpWindupTimer = 30;
        public int KSSwordIndex = -1;
        public int SFTpCounter = 0;
        public override int NPCType => NPCID.KingSlime;
        public override void OnFirstTick(NPC npc) => npc.GetGlobalNPC<KingSlime>().RunEmodeAI = false;
        public override void SetDefaults(NPC npc)
        {
            if (!Main.getGoodWorld)
                npc.lifeMax = (int)(1.2f * npc.lifeMax);
            else
                npc.lifeMax = (int)(1.1f * npc.lifeMax);
        }
        public override bool SafePreAI(NPC npc)
        {
            var Eslime = npc.GetGlobalNPC<KingSlime>();
            if (Eslime.DeathTimer >= 0)
            {
                Eslime.DeathAnimation(npc);
                if (++Eslime.DeathTimer >= 300)
                {
                    npc.life = 0;
                    npc.dontTakeDamage = false;
                    npc.checkDead();
                }
                return false;
            }
            EModeGlobalNPC.slimeBoss = npc.whoAmI;
            ref float teleportTimer = ref npc.ai[2];
            Player player = Main.player[npc.target];

            /*
            if (JumpTimer < SpecialJumpTime)
            {
                JumpTimer += Math.Min(2 - npc.GetLifePercent(), SpecialJumpTime - JumpTimer);
            }
            */
            if (teleportTimer >= 145 && teleportTimer < 150) //ai[2]为传送计时器， 原法利用传送计数器到达一半时拦截计时执行大跳
            {
                if (JumpTimer < SpecialJumpTime)
                    JumpTimer = SpecialJumpTime;
                teleportTimer = 145;
            }
            if (npc.GetLifePercent() < SummonCounter / SummonWaves)
            {
                const int Slimes = 6;
                if (FargoSoulsUtil.HostCheck)
                {
                    for (int i = 0; i < Slimes; i++)
                    {
                        int x = (int)(npc.position.X + Main.rand.NextFloat(npc.width - 32));
                        int y = (int)(npc.position.Y + Main.rand.NextFloat(npc.height - 32));
                        int type = ModContent.NPCType<SlimeSwarm>();
                        int slime = NPC.NewNPC(npc.GetSource_FromThis(), x, y, type);
                        if (slime.IsWithinBounds(Main.maxNPCs))
                        {
                            Main.npc[slime].SetDefaults(type);
                            Main.npc[slime].velocity.X = Main.rand.NextFloat(-15, 16) * 0.1f;
                            Main.npc[slime].velocity.Y = Main.rand.NextFloat(-30, -15) * 0.3f;
                            if (npc.HasValidTarget)
                            {
                                Main.npc[slime].ai[0] = Math.Sign(player.Center.X - npc.Center.X);
                                Main.npc[slime].velocity.X = Main.rand.NextFloat(10, 16) * 0.4f * -npc.HorizontalDirectionTo(player.Center);
                            }
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, slime);
                            }
                        }
                    }
                }
                SoundEngine.PlaySound(SoundID.Item167, npc.Center);
                SummonCounter--;
            }

            npc.position.X += npc.velocity.X * MathHelper.SmoothStep(0, 0.15f, 1 - npc.GetLifePercent());
            // Attack that happens when landing
            if (LandingAttackReady)
            {
                if (npc.velocity.Y == 0f)
                {
                    LandingAttackReady = false;
                    if (JumpTimer >= SpecialJumpTime && !SpecialJumping && npc.ai[1] < 5)
                    {
                        SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/VanillaEternity/KingSlime/KSCharge"), npc.Center);
                        Particle p = new ExpandingBloomParticle(npc.Center, Vector2.Zero, Color.Blue, Vector2.One, Vector2.One * 60, 40, true, Color.Transparent);
                        SpecialJumping = true;
                        SpecialJumpWindupTimer = 60;
                        p.Spawn();
                    }
                    else
                    {
                        if (SpecialJumping)
                        {
                            JumpTimer = 0;
                            SpecialJumping = false;
                            teleportTimer = 150; //continue teleport timer
                        }
                        else
                        {
                            if (FargoSoulsUtil.HostCheck && npc.ai[1] != 7)
                            {
                                /*
                                if (WorldSavingSystem.MasochistModeReal)
                                {
                                    for (int i = 0; i < 30; i++) //spike spray
                                    {
                                        Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.Center.X + Main.rand.Next(-5, 5), npc.Center.Y - 15),
                                            new Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-8, -5)),
                                            ProjectileID.SpikedSlimeSpike, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                    }
                                }
                                */

                                //凝胶盾skill
                                if (npc.HasValidTarget)
                                {
                                    SoundEngine.PlaySound(SoundID.Item21, player.Center);
                                    int num = 3;
                                    int GapX = 150;
                                    if (FargoSoulsUtil.HostCheck)
                                    {
                                        for (int i = -num; i <= num; i++)
                                        {
                                            Vector2 spawn = player.Center;
                                            spawn.X += i * GapX / num;
                                            spawn.Y -= 800 + Math.Abs(i) * GapX / num;
                                            Vector2 speed = player.Center - spawn;
                                            speed.Normalize();
                                            speed *= 5f;
                                            speed = speed.RotatedByRandom(MathHelper.ToRadians(4));
                                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawn, speed, ModContent.ProjectileType<SlimeBallHostile>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0f, Main.myPlayer);
                                        }
                                    }
                                }
                                
                            }
                        }
                    }

                }
            }
            else if (npc.velocity.Y > 0)
            {
                // If they're in the air, flag that the landing attack should be used next time they land
                LandingAttackReady = true;
            }

            if (npc.velocity.Y < 0) // Jumping up
            {
                if (!CurrentlyJumping) // Once per jump...
                {
                    CurrentlyJumping = true;


                    if (SpecialJumping) //special jump
                    {
                        npc.velocity.Y = -18;
                        int direction = Math.Sign(player.Center.X - npc.Center.X);
                        int pastPlayer = 1000;
                        Vector2 desiredDestination = player.Center + (Vector2.UnitX * pastPlayer * direction);

                        //funny highschool physics math
                        float jumpTime = Math.Abs(2 * npc.velocity.Y / npc.gravity);
                        npc.velocity.X = (desiredDestination.X - npc.Center.X) / jumpTime;
                        SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/VanillaEternity/KingSlime/KSJump"), npc.Center);
                    }
                    else
                    {
                        if (npc.HasValidTarget)
                        {
                            // If player is well above me, jump higher
                            if (player.Center.Y < npc.position.Y + npc.height - 240)
                            {
                                npc.velocity.Y *= 1.5f;
                                //shootSpikes = true;
                            }

                            //jump longer when player is further than threshold, scaling with distance up to cap
                            const int XThreshold = 0;
                            float xDif = Math.Abs(player.Center.X - npc.Center.X);
                            if (xDif > XThreshold)
                            {
                                float modifier = xDif - XThreshold;
                                modifier /= 700f;
                                modifier *= modifier;
                                modifier += 1;
                                modifier = MathHelper.Clamp(modifier, 1, 3);
                                npc.velocity.X *= modifier;
                                npc.velocity.Y *= Math.Min((float)Math.Cbrt(modifier), 1.5f);

                                // Flat addition
                                npc.velocity.X += Math.Sign(npc.velocity.X) * 2.25f;
                            }

                        }
                        if (npc.ai[1] != 0 && FargoSoulsUtil.HostCheck && npc.ai[1] != 7)
                        {
                            const float gravity = 0.15f;
                            float time = 90f;
                            Vector2 distance = player.Center - npc.Center + player.velocity * 30f;
                            distance.X /= time;
                            distance.Y = distance.Y / time - 0.5f * gravity * time;
                            for (int i = 0; i < 15; i++)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, distance + Main.rand.NextVector2Square(-1f, 1f),
                                    ModContent.ProjectileType<SlimeSpike>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                            }
                        }
                    }
                }
            }
            else
            {
                CurrentlyJumping = false;
            }

            if (npc.velocity.Y == 0) //on ground
            {
                if (SpecialJumpWindupTimer > 0)
                {
                    npc.ai[0] = -999; // no jumping until this is done
                    SpecialJumpWindupTimer--;
                    if (SpecialJumpWindupTimer == 0)
                        npc.ai[0] = -1; // ok now you can jump
                }

            }
            else //midair
            {
                if (SpecialJumping) //special jump
                {
                    JumpTimer++;
                    const int ProjTime = 5;
                    if (Math.Sign(npc.velocity.X) != Math.Sign(npc.DirectionTo(player.Center).X) && Math.Abs(npc.Center.X - player.Center.X) > 250 && npc.velocity.Y > 0)
                    {
                        npc.velocity.X /= 5;
                        SpecialJumping = false;
                        JumpTimer = 0;
                        teleportTimer = 150; //continue teleport timer
                    }

                    else if (JumpTimer % ProjTime < 1 && (JumpTimer % (ProjTime * 3) > 1 || WorldSavingSystem.MasochistModeReal))
                    {
                        SoundEngine.PlaySound(SoundID.Item17, npc.Center);
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Vector2 spawnPos = npc.Bottom;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero,
                                ModContent.ProjectileType<SlimeSpike2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0f, Main.myPlayer);
                        }
                    }
                }
            }


            if (npc.life < npc.lifeMax * .66f && npc.HasValidTarget && !SpecialJumping)
            {
                if (--SpikeRainCounter < 0) // Spike rain
                {
                    SpikeRainCounter = 240;

                    if (FargoSoulsUtil.HostCheck)
                    {
                        const int Gap = 110;
                        Vector2 spawnPos = player.Center + (Vector2.UnitX * Main.rand.Next(-Gap / 2, Gap / 2));
                        for (int i = -12; i <= 12; i++)
                        {
                            Vector2 spikePos = spawnPos;
                            spikePos.X += Gap * i;
                            spikePos.Y -= 500;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), spikePos, (0f) * Vector2.UnitY,
                                ModContent.ProjectileType<SlimeSpike2>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0f, Main.myPlayer);
                        }
                    }
                }
            }
            //传送之时
            if (npc.ai[1] == 5) 
            {
                if (npc.HasPlayerTarget && npc.ai[0] == 1) 
                    npc.localAI[2] = player.Center.Y;
                if (SuperSpecialJump)
                    npc.localAI[2] = player.Center.Y - 500;
                Vector2 tpPos = new(npc.localAI[1], npc.localAI[2]);
                for (int i = 0; i < 10; i++)
                {
                    int d = Dust.NewDust(tpPos, npc.width, npc.height / 2, DustID.t_Slime, 0, 0, 75, new Color(78, 136, 255, 80), 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity.Y -= 3f;
                    Main.dust[d].velocity *= 3f;
                }
                if (npc.ai[0] == 1)
                {
                    if (SFTpCounter > 0)
                    {
                        Particle p = new ExpandingBloomParticle(tpPos, Vector2.Zero, Color.Blue, Vector2.One, Vector2.One * 60, 40, true, Color.Transparent);
                        p.Spawn();

                    }
                    npc.velocity *= 0;
                    for (int i = 0; i < 3; i++)
                        ReleaseDustTp(npc.Center, tpPos, 20);
                    float teleportThreshold = 120;
                    if (npc.life < npc.lifeMax * .5f)
                        teleportThreshold -= 20;
                    KSGuiderSpawn(npc, tpPos, npc.Center, 2, teleportThreshold);
                }
            }
            UnifiedKingSlimeAI(npc, player);
            EModeUtils.DropSummon(npc, "SlimyCrown", NPC.downedSlimeKing, ref Eslime.DroppedSummon);
            return false;
        }
        private void UnifiedKingSlimeAI(NPC npc, Player player)
        {
            float movementScale = 1f;
            float goodWorldScale = 1f;
            bool justInitialized = false;
            bool teleporting = false;
            bool hidden = false;
            float goodWorldScaleBase = 2f;

            if (Main.getGoodWorld)
            {
                goodWorldScaleBase -= 1f - (float)npc.life / npc.lifeMax;
                goodWorldScale *= goodWorldScaleBase;
            }

            npc.aiAction = 0;

            // localAI[3] is the one-time initialization marker.
            if (npc.localAI[3] == 0f)
            {
                npc.localAI[3] = 1f;
                justInitialized = true;
                if (PhanUtil.HostCheck)
                {
                    npc.ai[0] = -100f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
            }

            const float maxTargetDistance = 5000f;
            if (player.dead || Vector2.Distance(npc.Center, player.Center) > maxTargetDistance)
            {
                npc.TargetClosest();
                player = Main.player[npc.target];

                if (player.dead || Vector2.Distance(npc.Center, player.Center) > maxTargetDistance)
                {
                    npc.EncourageDespawn(10);
                    npc.direction = player.Center.X < npc.Center.X ? 1 : -1;

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[1] != 5f)
                    {
                        npc.netUpdate = true;
                        npc.ai[2] = 0f;
                        npc.ai[0] = 0f;
                        npc.ai[1] = 5f;
                        npc.localAI[1] = Main.maxTilesX * 16;
                        npc.localAI[2] = Main.maxTilesY * 16;
                    }
                }
            }
            int tpCD = 300;
            //原版传送定位机制
            if (!player.dead && npc.timeLeft > 10 && npc.ai[2] >= tpCD && npc.ai[1] < 5f && npc.velocity.Y == 0f)
            {
                npc.ai[2] = 0f;
                npc.ai[0] = 0f;
                npc.ai[1] = 5f;

                if (PhanUtil.HostCheck)
                {
                    npc.TargetClosest(faceTarget: false);
                    player = Main.player[npc.target];
                    Point npcTilePosition = npc.Center.ToTileCoordinates();
                    Point playerTilePosition = player.Center.ToTileCoordinates();
                    Vector2 playerOffset = player.Center - npc.Center;

                    const int searchRadius = 10;
                    const int currentNpcRadius = 0;
                    const int playerSafeRadius = 7;
                    int searchAttempts = 0;
                    bool foundLandingPosition = false;

                    if (npc.localAI[0] >= 360f || playerOffset.Length() > 2000f)
                    {
                        if (npc.localAI[0] >= 360f)
                            npc.localAI[0] = 360f;

                        foundLandingPosition = true;
                        searchAttempts = 100;
                    }

                    while (!foundLandingPosition && searchAttempts < 100)
                    {
                        searchAttempts++;
                        int candidateTileX = Main.rand.Next(playerTilePosition.X - searchRadius, playerTilePosition.X + searchRadius + 1);
                        int candidateTileY = Main.rand.Next(playerTilePosition.Y - searchRadius, playerTilePosition.Y + 1);

                        bool insidePlayerSafeArea = candidateTileY >= playerTilePosition.Y - playerSafeRadius
                            && candidateTileY <= playerTilePosition.Y + playerSafeRadius
                            && candidateTileX >= playerTilePosition.X - playerSafeRadius
                            && candidateTileX <= playerTilePosition.X + playerSafeRadius;
                        bool insideNpcPosition = candidateTileY >= npcTilePosition.Y - currentNpcRadius
                            && candidateTileY <= npcTilePosition.Y + currentNpcRadius
                            && candidateTileX >= npcTilePosition.X - currentNpcRadius
                            && candidateTileX <= npcTilePosition.X + currentNpcRadius;

                        if (insidePlayerSafeArea || insideNpcPosition || Main.tile[candidateTileX, candidateTileY].HasTile)
                            continue;

                        int landingTileY = candidateTileY;
                        int tilesToGround = 0;
                        if (Main.tile[candidateTileX, landingTileY].HasTile
                            && Main.tileSolid[Main.tile[candidateTileX, landingTileY].TileType]
                            && !Main.tileSolidTop[Main.tile[candidateTileX, landingTileY].TileType])
                        {
                            tilesToGround = 1;
                        }
                        else
                        {
                            for (; tilesToGround < 150 && landingTileY + tilesToGround < Main.maxTilesY; tilesToGround++)
                            {
                                int tileY = landingTileY + tilesToGround;
                                if (Main.tile[candidateTileX, tileY].HasTile
                                    && Main.tileSolid[Main.tile[candidateTileX, tileY].TileType]
                                    && !Main.tileSolidTop[Main.tile[candidateTileX, tileY].TileType])
                                {
                                    tilesToGround--;
                                    break;
                                }
                            }
                        }

                        candidateTileY += tilesToGround;
                        bool validLandingPosition = !(Main.tile[candidateTileX, candidateTileY].LiquidType == LiquidID.Lava
                            && Main.tile[candidateTileX, candidateTileY].LiquidAmount > 0)
                            && Collision.CanHitLine(npc.Center, 0, 0, player.Center, 0, 0);

                        if (validLandingPosition)
                        {
                            npc.localAI[1] = candidateTileX * 16 + 8;
                            npc.localAI[2] = candidateTileY * 16 + 16;
                            foundLandingPosition = true;
                            break;
                        }
                    }

                    if (searchAttempts >= 100)
                    {
                        Vector2 playerBottom = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)].Bottom;
                        npc.localAI[1] = playerBottom.X;
                        npc.localAI[2] = playerBottom.Y;
                    }
                }
            }

            bool lineOfSightBlocked = !Collision.CanHitLine(npc.Center, 0, 0, player.Center, 0, 0);
            bool verticalGapTooLarge = Math.Abs(npc.Top.Y - player.Bottom.Y) > 160f;
            if (lineOfSightBlocked || verticalGapTooLarge)
            {
                npc.ai[2]++;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    npc.localAI[0]++;
            }
            else if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.localAI[0]--;
                if (npc.localAI[0] < 0f)
                    npc.localAI[0] = 0f;
            }

            if (npc.timeLeft < 10 && (npc.ai[0] != 0f || npc.ai[1] != 0f))
            {
                npc.ai[0] = 0f;
                npc.ai[1] = 0f;
                npc.netUpdate = true;
                teleporting = false;
            }
            float teleportThreshold = 120;
            if (npc.life < npc.lifeMax * .5f)
                teleportThreshold -= 20;
            switch ((int)npc.ai[1])
            {
                //传送缩小
                case 5:
                    teleporting = true;
                    npc.aiAction = 1;
                    npc.ai[0]++;
                    npc.ai[3]++;
                    movementScale = MathHelper.Clamp((teleportThreshold - npc.ai[0]) / teleportThreshold, 0f, 1f);
                    movementScale = 0.5f + movementScale * 0.5f;
                    if (npc.ai[0] >= teleportThreshold)
                        hidden = true;
                    if (npc.ai[0] == teleportThreshold)
                        Gore.NewGore(npc.GetSource_FromThis(), npc.Center + new Vector2(-40f, -npc.height / 2), npc.velocity, 734);
                    //本体传送
                    if (npc.ai[0] >= teleportThreshold)
                    {
                        npc.Bottom = new Vector2(npc.localAI[1], npc.localAI[2]);
                        if (SFTpCounter <= 0 && npc.ai[3] >= teleportThreshold - 1)
                        {
                            npc.ai[1] = 6f;
                            npc.ai[0] = 0f;
                            if (SuperSpecialJump)
                            {
                                SFTpCounter = 3;
                                if (npc.life < npc.lifeMax * .5f)
                                    SFTpCounter++;
                            }
                            npc.ai[3] = 0f;
                            npc.velocity *= 0;
                        }
                        if (npc.ai[3] >= teleportThreshold)
                        {
                            npc.ai[3] = 0;
                            SFTpCounter--;
                            Vector2 targetPos = GetNextTpTarget(npc, player);
                            targetPos.X -= npc.width / 2;
                            npc.localAI[1] = targetPos.X;
                            npc.localAI[2] = targetPos.Y;
                            int length = 10;
                            int gap = 10;
                            if (Main.getGoodWorld)
                                gap = 15;
                            float liferatio = npc.GetLifePercent();
                            if (liferatio < 0.5f)
                                length += 20;
                            if (liferatio < 0.33f)
                                length += 30;
                            if (PhanUtil.HostCheck)
                            {
                                for (int i = 0; i <= length; i += gap)
                                {
                                    KSGuiderSpawn(npc, targetPos, npc.Center, 3, teleportThreshold + i);
                                    if (Main.getGoodWorld)
                                        KSGuiderSpawn(npc, npc.Center, targetPos, 3, teleportThreshold + i);
                                }
                            }
                            if (SFTpCounter == 0 && (Main.tile[targetPos.ToTileCoordinates()].HasTile || targetPos.Y - player.Center.Y > 250))
                            {
                                SFTpCounter++;
                            }
                            for (int i = 0; i < 3; i++)
                                ReleaseDustTp(npc.Center, new Vector2(npc.localAI[1], npc.localAI[2]), 20);
                            if (Main.getGoodWorld)
                                SpikeRainCounter -= 240;
                            else
                                SpikeRainCounter = 180;
                        }
                        npc.netUpdate = true;
                    }
                    else if (SpikeRainCounter > 0)
                        SpikeRainCounter++;
                    /*
                    if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[0] >= 1.5f * teleportThreshold)
                    {
                        npc.ai[1] = 6f;
                        npc.ai[0] = 0f;
                    }
                    */
                    if (!hidden)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            int dust = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, DustID.TintableDust,
                                npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                            Main.dust[dust].noGravity = true;
                            Main.dust[dust].velocity *= 0.5f;
                        }
                    }
                    break;
                //到点放大
                case 6:
                    teleporting = true;
                    npc.aiAction = 0;
                    npc.ai[0]++;
                    movementScale = MathHelper.Clamp(npc.ai[0] / 30f, 0f, 1f);
                    movementScale = 0.5f + movementScale * 0.5f;
                    if (SuperSpecialJump)
                    {
                        npc.velocity *= 0;
                        if (Main.rand.NextBool(2))
                        {
                            float distance = 0;
                            while (!Main.tile[(npc.Bottom + distance * Vector2.UnitY).ToTileCoordinates()].HasTile && distance <= 800)
                            {
                                distance += 16;
                            }
                            for (float i = 0; i < distance; i += 15)
                            {
                                int dust = Dust.NewDust(npc.Bottom + new Vector2(npc.width / 2, i), 3, 3, DustID.t_Slime, 0f, 0f, 150, new Color(78, 136, 255, 80), 1.5f);
                                Main.dust[dust].velocity = -Main.rand.NextFloat(1, 6f) * Vector2.UnitX;
                                Main.dust[dust].noGravity = true;
                                int dust2 = Dust.NewDust(npc.Bottom + new Vector2(-npc.width / 2, i), 3, 3, DustID.GemSapphire, 0f, 0f, 150, new Color(78, 136, 255, 80), 1.5f);
                                Main.dust[dust2].velocity = Main.rand.NextFloat(1, 6f) * Vector2.UnitX;
                                Main.dust[dust2].noGravity = true;
                            }
                        }
                    }
                    if (npc.ai[0] >= 30f && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (!SuperSpecialJump)
                        {
                            npc.ai[1] = 0f;
                            SuperSpecialJump = true;
                        }
                        else
                        {
                            npc.ai[1]++;
                            SuperSpecialJump = false;
                        }
                        npc.ai[0] = 0f;
                        npc.netUpdate = true;
                        npc.TargetClosest();
                    }
                    if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[0] >= 60f)
                    {
                        if (!SuperSpecialJump)
                        {
                            npc.ai[1] = 0f;
                            SuperSpecialJump = true;
                        }
                        else
                        {
                            npc.ai[1]++;
                            SuperSpecialJump = false;
                        }
                        npc.ai[0] = 0f;
                        npc.TargetClosest();
                    }

                    for (int i = 0; i < 10; i++)
                    {
                        int dust = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, DustID.TintableDust,
                            npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 2f;
                    }

                    break;
                case 7:
                    teleporting = false;
                    npc.aiAction = 0;
                    npc.ai[3]++;
                    npc.velocity.X = 0;
                    const float Waittime = 60;
                    npc.ai[0] = -999;
                    SpikeRainCounter = 180;
                    if (npc.ai[3] < SuperSpecialJumpWindupTimer)
                        npc.velocity.Y = -5f * MathHelper.Clamp(1 - npc.ai[3] / SuperSpecialJumpWindupTimer, 0, 1);
                    else if (npc.velocity.Y > 0f && !Main.tile[(npc.Bottom + 1.2f * npc.velocity).ToTileCoordinates()].HasTile)
                    {
                        npc.velocity.Y *= 1.02f;
                        npc.position.Y += npc.velocity.Y * 1.2f;
                    }
                    if (npc.velocity.Y == 0 && npc.ai[3] < SuperSpecialJumpWindupTimer + Waittime)
                        npc.ai[3] = SuperSpecialJumpWindupTimer + Waittime;
                    if (npc.ai[3] == SuperSpecialJumpWindupTimer + Waittime)
                    {
                        SoundEngine.PlaySound(SoundID.Item21, player.Center);
                        ScreenShakeSystem.StartShake(10);
                        if (PhanUtil.HostCheck && npc.HasValidTarget)
                        {
                            //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<KSShockWave>(), 0, 0, Main.myPlayer, 60, 30);
                            Vector2 basePos = player.Center - 700 * Vector2.UnitY;
                            float baseGapX = 100 * 2;
                            ProjSpawn(0, 0);
                            float liferatio = npc.GetLifePercent();
                            if (liferatio < 0.5f)
                                ProjSpawn(baseGapX, 1.8f * baseGapX);
                            if (liferatio < 0.33f)
                            {
                                ProjSpawn(0, 3.6f * baseGapX);
                                if (Main.getGoodWorld)
                                    ProjSpawn(-baseGapX, 5.4f * baseGapX);
                            }
                            void ProjSpawn(float detalX, float detalY)
                            {
                                for (int i = -5; i <= 5; i++)
                                {
                                    SpawnConicalSlimeSpikes(npc, basePos + (detalX + 2 * i * baseGapX) * Vector2.UnitX - detalY * Vector2.UnitY, MathHelper.Pi / 3f, (int)detalY / 15 + 5);
                                }
                            }
                        }
                    }
                    if (npc.ai[3] > SuperSpecialJumpWindupTimer + 2 * Waittime && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.ai[1] = npc.ai[3] = 0;
                        npc.TargetClosest();
                        npc.ai[0] = -120;
                    }
                    if (npc.ai[3] > SuperSpecialJumpWindupTimer + 3.5f * Waittime && Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        npc.ai[1] = npc.ai[3] = 0;
                        npc.TargetClosest();
                        npc.ai[0] = -120;
                    }
                    break;
                default:
                    break;
            }
            //FargoSoulsUtil.PrintAI(npc);
            npc.dontTakeDamage = npc.hide = hidden;

            if (npc.velocity.Y == 0f)
            {
                npc.velocity.X *= 0.8f;
                if (npc.velocity.X > -0.1f && npc.velocity.X < 0.1f)
                    npc.velocity.X = 0f;

                if (!teleporting)
                {
                    npc.ai[0] += 2f;
                    if (npc.life < npc.lifeMax * 0.8)
                        npc.ai[0] += 1f;
                    if (npc.life < npc.lifeMax * 0.6)
                        npc.ai[0] += 1f;
                    if (npc.life < npc.lifeMax * 0.4)
                        npc.ai[0] += 2f;
                    if (npc.life < npc.lifeMax * 0.2)
                        npc.ai[0] += 3f;
                    if (npc.life < npc.lifeMax * 0.1)
                        npc.ai[0] += 4f;

                    if (npc.ai[0] >= 0f)
                    {
                        npc.netUpdate = true;
                        npc.TargetClosest();
                        if (npc.ai[1] == 3f)
                        {
                            npc.velocity.Y = -13f;
                            npc.velocity.X += 3.5f * npc.direction;
                            npc.ai[0] = -200f;
                            npc.ai[1] = 0f;
                        }
                        else if (npc.ai[1] == 2f)
                        {
                            npc.velocity.Y = -6f;
                            npc.velocity.X += 4.5f * npc.direction;
                            npc.ai[0] = -120f;
                            npc.ai[1] += 1f;
                        }
                        else if (npc.ai[1] != 7)
                        {
                            npc.velocity.Y = -8f;
                            npc.velocity.X += 4f * npc.direction;
                            npc.ai[0] = -120f;
                            npc.ai[1] += 1f;
                        }
                    }
                    else if (npc.ai[0] >= -30f)
                    {
                        npc.aiAction = 1;
                    }
                }
            }
            else if (npc.target < 255)
            {
                float airAccelerationLimit = Main.getGoodWorld ? 6f : 3f;
                if ((npc.direction == 1 && npc.velocity.X < airAccelerationLimit)
                    || (npc.direction == -1 && npc.velocity.X > -airAccelerationLimit))
                {
                    if ((npc.direction == -1 && npc.velocity.X < 0.1f)
                        || (npc.direction == 1 && npc.velocity.X > -0.1f))
                    {
                        npc.velocity.X += 0.2f * npc.direction;
                    }
                    else
                    {
                        npc.velocity.X *= 0.93f;
                    }
                }
            }

            int movementDust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.TintableDust, npc.velocity.X, npc.velocity.Y,
                255, new Color(0, 80, 255, 80), npc.scale * 1.2f);
            Main.dust[movementDust].noGravity = true;
            Main.dust[movementDust].velocity *= 0.5f;

            if (npc.life <= 0)
                return;

            float targetScale = (float)npc.life / npc.lifeMax;
            targetScale = targetScale * 0.5f + 0.75f;
            targetScale *= movementScale;
            targetScale *= goodWorldScale;

            if (targetScale != npc.scale || justInitialized)
            {
                npc.position.X += npc.width / 2;
                npc.position.Y += npc.height;
                npc.scale = targetScale;
                npc.width = (int)(98f * npc.scale);
                npc.height = (int)(92f * npc.scale);
                npc.position.X -= npc.width / 2f;
                npc.position.Y -= npc.height;
            }
            /*
            if (PhanUtil.HostCheck)
            {
                int minionSpawnThreshold = (int)(npc.lifeMax * 0.05);
                if (npc.life + minionSpawnThreshold < npc.ai[3])
                {
                    npc.ai[3] = npc.life;
                    int minionCount = Main.rand.Next(1, 4);
                    for (int i = 0; i < minionCount; i++)
                    {
                        int spawnX = (int)(npc.position.X + Main.rand.Next(npc.width - 32));
                        int spawnY = (int)(npc.position.Y + Main.rand.Next(npc.height - 32));
                        int minionType = Main.expertMode && Main.rand.NextBool(4) ? 535 : 1;
                        int minionIndex = NPC.NewNPC(npc.GetSource_FromAI(), spawnX, spawnY, minionType);
                        Main.npc[minionIndex].SetDefaults(minionType);
                        Main.npc[minionIndex].velocity.X = Main.rand.Next(-15, 16) * 0.1f;
                        Main.npc[minionIndex].velocity.Y = Main.rand.Next(-30, 1) * 0.1f;
                        Main.npc[minionIndex].ai[0] = -1000 * Main.rand.Next(3);
                        Main.npc[minionIndex].ai[1] = 0f;

                        if (Main.netMode == NetmodeID.Server && minionIndex < 200)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, minionIndex);
                    }
                    Main.NewText("spawned");
                }
            }
            */
        }
        public override void SafeModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.type == ProjectileID.ThornChakram)
                modifiers.FinalDamage *= 0.75f;
            base.SafeModifyHitByProjectile(npc, projectile, ref modifiers);
        }
        private static void ReleaseDustTp(Vector2 start, Vector2 end, int gap)
        {
            Vector2 direction = end - start;
            float distance = direction.Length();
            direction.Normalize();
            for (float i = 0; i < distance; i += gap)
            {
                Vector2 position = start + direction * i;
                int dust = Dust.NewDust(position, 3, 3, DustID.GemSapphire, 0f, 0f, 150, new Color(78, 136, 255, 80), 2.5f);
                Main.dust[dust].velocity = Main.rand.NextFloat(0, 2f) * direction;
                Main.dust[dust].noGravity = true;
            }
        }
        private static Vector2 GetNextTpTarget(NPC npc, Player player)
        {
            float targANgle = npc.DirectionTo(player.Center).ToRotation();
            targANgle += Main.rand.NextFloat(MathHelper.Pi / 8f, 2.5f * MathHelper.Pi / 8f) * (Main.rand.NextBool() ? 1 : -1);
            return player.Center + targANgle.ToRotationVector2() * Main.rand.NextFloat(600, 700f);
        }
        private static void KSGuiderSpawn(NPC npc, Vector2 targetPos, Vector2 start, int num, float teleportThreshold)
        {
            Vector2 targetDirection = targetPos - start;
            if (targetDirection.Length() < 200f || !PhanUtil.HostCheck)
                return;

            targetDirection.Normalize();
            int guiderType = ModContent.ProjectileType<KSGuider>();
            var source = npc.GetSource_FromThis();
            int damage = FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6);
            for (int i = -num; i <= num; i++)
            {
                float rAngle = i * MathHelper.Pi / 5f;
                if (Math.Abs(i) <= 1)
                    rAngle *= 1.2f;
                else if (Math.Abs(i) >= 3)
                    rAngle *= 0.85f;
                Vector2 velocity = targetDirection.RotatedBy(rAngle);
                Projectile.NewProjectile(source, start, velocity, guiderType, damage, 0f, Main.myPlayer,
                    targetPos.X, targetPos.Y, teleportThreshold);
            }
        }
        private static void SpawnConicalSlimeSpikes(NPC npc, Vector2 spawnPos, float angle, int extraTimeleft)
        {
            int num = 3;
            int GapX = 100;
            int projType = ModContent.ProjectileType<SlimeBallHostile>();
            var source = npc.GetSource_FromThis();
            int damage = FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6);
            for (int i = -num; i <= num; i++)
            {
                Vector2 spawn = spawnPos;
                spawn.X += i * GapX / num;
                spawn.Y += (float)Math.Tan(angle) * Math.Abs(i) * GapX / num;
                Vector2 speed = Vector2.UnitY * 0;
                speed *= 1f;
                if (Main.getGoodWorld)
                    speed = speed.RotatedByRandom(MathHelper.ToRadians(2));
                int p = Projectile.NewProjectile(source, spawn, speed, projType, damage, 0f, Main.myPlayer);
                Main.projectile[p].timeLeft += extraTimeleft;
            }
        }
    }
}
