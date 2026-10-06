using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.Items.Summons;
using FargowiltasSouls.Content.WorldGeneration;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.StateMachines;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using System.Collections.Generic;
using System.Linq;
using Monochrome.Common.MonoUtil;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Content.Bosses.Coffin
{
    public partial class P_CursedCoffin
    {
        public enum BehaviorStates : byte
        {
            Opening,
            PhaseTransition,
            StunPunish,
            SpiritGrabPunish,
            HoveringForSlam,
            SlamWShockwave,
            WavyShotCircle,
            WavyShotSlam,
            GrabbyHands,
            RandomStuff,
            YouCantEscape,
            Count
        }
        private readonly List<BehaviorStates> Attacks =
        [
            BehaviorStates.HoveringForSlam,
            BehaviorStates.WavyShotCircle,
            BehaviorStates.GrabbyHands,
            BehaviorStates.RandomStuff
        ];
        public Player Player => Main.player[coffin.NPC.target];
        #region AI
        public override void OnFirstTick(NPC npc)
        {
            coffin = Utilities.As<CursedCoffin>(npc);
        }
        public override bool SafePreAI(NPC npc)
        {
            npc.defense = npc.defDefense;
            if (Main.npc.Any((NPC p) => FargoExtensionMethods.TypeAlive<CursedSpirit>(p)))
            {
                npc.defense += 15;
            }
            if (StateMachine != null && StateMachine.CurrentState != null && StateMachine.CurrentState.Identifier != BehaviorStates.RandomStuff)
            {
                npc.rotation = 0f;
            }
            Projectile[] projectile = Main.projectile;
            foreach (Projectile proj in projectile)
            {
                if (proj.type == ModContent.ProjectileType<FallingSandstone>() && proj.hostile && proj.active && proj.Hitbox.Intersects(npc.Hitbox))
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
                        Dust.NewDust(position, width, height, DustID.SandstormInABottle, speedX, speedY, 0, default(Color), scale);
                    }
                    proj.Kill();
                }
            }
            Player localPlayer = Main.LocalPlayer;
            Vector2 nextCenter = localPlayer.Center + localPlayer.velocity;
            if (new Rectangle((int)(nextCenter.X - (float)(localPlayer.Hitbox.Width / 2)), (int)(nextCenter.Y - (float)(localPlayer.Hitbox.Height / 2)), localPlayer.Hitbox.Width, localPlayer.Hitbox.Height).Intersects(npc.Hitbox))
            {
                if (!localPlayer.Hitbox.Intersects(npc.Hitbox))
                {
                    localPlayer.velocity.X /= 2f;
                    localPlayer.position.X -= Math.Sign(localPlayer.Center.X - npc.Center.X) * 8;
                }
                localPlayer.velocity -= localPlayer.DirectionTo(npc.Center);
            }
            Vector2 arenaCenter = CoffinArena.FightCenter;
            float distanceX = Math.Abs(localPlayer.Center.X - arenaCenter.X);
            float threshold = (float)CoffinArena.VectorWidth / 2f;
            int DustType = 32;
            if (localPlayer.active && !localPlayer.dead && !localPlayer.ghost && distanceX > threshold && distanceX < threshold * 4f)
            {
                Vector2 movement = Vector2.UnitX * (arenaCenter.X - localPlayer.Center.X);
                float difference = movement.Length() - threshold;
                movement.Normalize();
                movement *= ((difference < 17f) ? difference : 17f);
                localPlayer.position += movement;
                for (int i2 = 0; i2 < 10; i2++)
                {
                    int d = Dust.NewDust(localPlayer.position, localPlayer.width, localPlayer.height, DustType, 0f, 0f, 0, default(Color), 1.25f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 5f;
                }
            }
            for (int i3 = -1; i3 <= 1; i3 += 2)
            {
                float posX = arenaCenter.X + (float)i3 * threshold;
                for (int y = 0; y <= CoffinArena.Height * 2; y++)
                {
                    float posY = arenaCenter.Y + (float)CoffinArena.VectorHeight / 2f - 8f * (float)y;
                    if (Main.rand.NextBool(3) && !Main.tile[(int)posX / 16, (int)posY / 16].HasUnactuatedTile)
                    {
                        int d2 = Dust.NewDust((posX - 4f) * Vector2.UnitX + (posY - 8f) * Vector2.UnitY, 8, 16, DustType, 0f, 0f, 0, default(Color), 1.25f);
                        Main.dust[d2].noGravity = true;
                    }
                }
                
            }
            if (!coffin.Targeting())
            {
                return false;
            }
            npc.timeLeft = 60;
            npc.Opacity = 1f;
            StateMachine.PerformBehaviors();
            StateMachine.PerformStateTransitionCheck();
            if (StateMachine.StateStack.Count > 0)
            {
                Timer++;
            }
            return false;
        }
        #endregion
        #region state
        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.Opening)]
        public void Opening()
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            if (Timer == 2f && !WorldSavingSystem.DownedBoss[11] && PhanUtil.HostCheck)
            {
                Item.NewItem(npc.GetSource_Loot(), Main.player[npc.target].Hitbox, ModContent.ItemType<CoffinSummon>());
            }
            if (Timer >= 0f)
            {
                ExtraTrail = true;
                if (Math.Abs(npc.velocity.Y) < 22f)
                {
                    npc.velocity.Y += 0.2f;
                }
                if ((Timer > 5f && npc.Bottom.Y >= LockVector1.Y && npc.velocity.Y > 0f) || Timer > 120f)
                {
                    npc.noTileCollide = false;
                    if (npc.velocity.Y <= 1f)
                    {
                        SoundEngine.PlaySound(in SoundID.Item14, npc.Center);
                        SoundEngine.PlaySound(in CursedCoffin.SlamSFX, npc.Center);
                        ExtraTrail = false;
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
                            ((ModNPC)(object)coffin).Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/ShiftingSands");
                            if (Main.musicFade[((ModNPC)(object)coffin).Music] < 0.5f)
                            {
                                Main.musicFade[((ModNPC)(object)coffin).Music] = 0.5f;
                            }
                        }
                        else
                        {
                            ((ModNPC)(object)coffin).Music = 81;
                        }
                        npc.velocity.Y -= 9f;
                        if (!Main.dedServ)
                        {
                            ScreenShakeSystem.StartShake(10f, (float)Math.PI * 2f, (Vector2?)null, 0.5f);
                        }
                        int dir = Math.Sign(Player.Center.X - CoffinArena.FightCenter.X);
                        HalfDropSand(dir);
                        if (Main.getGoodWorld)
                        {
                            HalfDropSand(-dir);
                        }
                    }
                }
                if (npc.Center.Y >= LockVector1.Y + 800f)
                {
                    npc.velocity = Vector2.Zero;
                }
            }
            if (Timer < 0f)
            {
                if (npc.velocity.Y < 0f)
                {
                    npc.velocity.Y += 0.2f;
                }
                if (npc.velocity.Y.IsInRange(0f, 0.2f))
                {
                    npc.velocity.Y = 0f;
                }
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.PhaseTransition)]
        public void PhaseTransition()
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            coffin.HoverSound();
            npc.velocity = (CoffinArena.FightCenter - npc.Center) * 0.05f;
            npc.rotation = Main.rand.NextFloat((float)Math.PI * 3f / 25f * (Timer / 90f));
            SoundEngine.PlaySound(in CursedCoffin.SpiritDroneSFX, npc.Center);
            npc.HitSound = SoundID.NPCHit4;
            npc.netUpdate = true;
            if (Phase < 2)
            {
                Phase = 2;
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.StunPunish)]
        public void StunPunish()
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            if (Phase >= 3)
            {
                Phase = 2;
            }
            npc.velocity *= 0.95f;
            if (Timer < 20f)
            {
                if ((npc.frameCounter += 1.0) % 4.0 == 3.0 && Frame < Main.npcFrameCount[((ModNPC)(object)coffin).Type] - 1)
                {
                    Frame++;
                }
            }
            else if (Timer == 20f)
            {
                IEnumerable<Player> stunned = Main.player.Where((Player p) => FargoExtensionMethods.Alive(p) && p.HasBuff(BuffID.Dazed));
                if (stunned.Any())
                {
                    SoundEngine.PlaySound(in CursedCoffin.ShotSFX, npc.Center);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        foreach (Player player in stunned)
                        {
                            Vector2 dir = npc.DirectionTo(player.Center);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, dir * 1f, ModContent.ProjectileType<CoffinHand>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.5f, 2), 1f, Main.myPlayer, npc.whoAmI, 22f, player.whoAmI);
                        }
                    }
                }
                SoundEngine.PlaySound(in CursedCoffin.ShotSFX, npc.Center);
            }
            else if ((npc.frameCounter += 1.0) % 60.0 == 59.0)
            {
                Frame--;
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.YouCantEscape)]
        public void YouCantEscape()
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            if (Phase >= 3)
            {
                Phase = 2;
            }
            npc.velocity *= 0.95f;
            if (Timer < 20f)
            {
                if ((npc.frameCounter += 1.0) % 4.0 == 3.0 && Frame < Main.npcFrameCount[((ModNPC)(object)coffin).Type] - 1)
                {
                    Frame++;
                }
            }
            else
            {
                if (Timer == 20f)
                {
                    IEnumerable<Player> outsideArena = Main.player.Where((Player p) => FargoExtensionMethods.Alive(p) && !CoffinArena.Rectangle.Contains(p.Center.ToTileCoordinates()));
                    if (!outsideArena.Any())
                    {
                        return;
                    }
                    SoundEngine.PlaySound(in CursedCoffin.ShotSFX, npc.Center);
                    if (!FargoSoulsUtil.HostCheck)
                    {
                        return;
                    }
                    {
                        foreach (Player player in outsideArena)
                        {
                            Vector2 dir = npc.rotation.ToRotationVector2();
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, dir * 4f, ModContent.ProjectileType<CoffinHand>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.5f, 2), 1f, Main.myPlayer, npc.whoAmI, 44f, player.whoAmI);
                        }
                        return;
                    }
                }
                if ((npc.frameCounter += 1.0) % 30.0 == 29.0)
                {
                    Frame--;
                }
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.SpiritGrabPunish)]
        public void SpiritGrabPunish()
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            if (Phase >= 3)
            {
                Phase = 2;
            }
            ref float initialDir = ref AI2;
            ref float initialDist = ref AI3;
            coffin.HoverSound();
            if ((npc.frameCounter += 1.0) % 10.0 == 9.0 && Frame > 0)
            {
                Frame--;
            }
            if (Timer <= 1f)
            {
                initialDir = Utilities.SafeDirectionTo((Entity)Player, npc.Center).ToRotation();
                initialDist = npc.Distance(Player.Center);
            }
            if (Timer <= 70f)
            {
                float progress = Timer / 70f;
                float distance = MathHelper.Lerp(initialDist, 350f, progress);
                Vector2 direction = Vector2.Lerp(initialDir.ToRotationVector2(), -Vector2.UnitY, progress);
                Vector2 desiredPos = Player.Center + distance * direction;
                npc.velocity = desiredPos - npc.Center;
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.HoveringForSlam)]
        public void HoveringForSlam()
        {
            Vector2 desiredPos = CoffinArena.FightCenter;
            ref float Xsign = ref AI2;
            ref float RandomTimer = ref AI3;
            NPC npc = ((ModNPC)(object)coffin).NPC;
            coffin.HoverSound();
            if (Timer == 1f)
            {
                Xsign = (desiredPos.X - Player.Center.X).Sign();
                RandomTimer = 240f;
            }
            if (Timer < RandomTimer && Timer >= 0f)
            {
                npc.noTileCollide = false;
                if (Timer < 60f)
                {
                    coffin.Movement(desiredPos + new Vector2(desiredPos.X + 0.9f * Xsign * (float)CoffinArena.VectorWidth / 2f, Player.Center.Y), 0.1f, 14f, 5f, 0.08f, 20f);
                }
                else if (Timer == 60f)
                {
                    npc.velocity = -8f * Xsign * Vector2.UnitX;
                }
                else if (npc.velocity.X < 0.2f && Timer < AI3 - 60f)
                {
                    Timer = AI3 - 60f;
                }
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.SlamWShockwave)]
        public void SlamWShockwave()
        {
            ref float Counter = ref AI2;
            NPC npc = ((ModNPC)(object)coffin).NPC;
            npc.noTileCollide = npc.Bottom.Y + npc.velocity.Y < Player.Bottom.Y - 16f;
            if (Timer >= 0f)
            {
                npc.velocity.X *= 0.97f;
                float speedUp = ((Counter == 2f) ? 0.3f : 0.17f);
                npc.velocity.X += (float)Math.Sign(Player.Center.X - npc.Center.X) * speedUp;
                if (npc.velocity.Y >= 0f && Counter == 0f)
                {
                    Counter = 1f;
                }
                if (npc.velocity.Y == 0f && Counter > 0f && !npc.noTileCollide && Timer > 5f)
                {
                    SoundEngine.PlaySound(in SoundID.Item14, npc.Center);
                    SoundEngine.PlaySound(in CursedCoffin.SlamSFX, npc.Center);
                    ExtraTrail = false;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 vel = Vector2.UnitX * i * 3f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 30f, vel, ModContent.ProjectileType<CoffinSlamShockwave>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 1f, 2), 1f, Main.myPlayer);
                        }
                    }
                    if (Counter < 2f)
                    {
                        Counter = 2f;
                        Timer = 0f;
                        npc.velocity.Y = -10f;
                    }
                    else
                    {
                        int endlag = 80;
                        Timer = -endlag;
                        npc.velocity.X = 0f;
                    }
                    return;
                }
                npc.velocity.Y += 0.175f;
                if (npc.velocity.Y > 0f)
                {
                    npc.velocity.Y += 0.32f;
                }
                if (npc.velocity.Y > 15f)
                {
                    npc.velocity.Y = 15f;
                }
                ExtraTrail = true;
                if (npc.Center.Y >= LockVector1.Y + 1000f)
                {
                    npc.velocity = Vector2.Zero;
                }
            }
            if (Math.Abs(npc.Center.X - CoffinArena.FightCenter.X) > (float)(CoffinArena.Width * 8 - npc.width / 2) && (float)Utilities.NonZeroSign(npc.velocity.X) != Utilities.HorizontalDirectionTo((Entity)npc, CoffinArena.FightCenter))
            {
                npc.velocity.X = 0f;
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.WavyShotCircle)]
        public void WavyShotCircle()
        {
            //IL_0137: Unknown result type (might be due to invalid IL or missing references)
            //IL_013e: Expected O, but got Unknown
            int TelegraphTime = (WorldSavingSystem.MasochistModeReal ? 60 : 70);
            float progress = 1f - Timer / (float)TelegraphTime;
            Vector2 maskCenter = MaskCenter;
            NPC npc = ((ModNPC)(object)coffin).NPC;
            Vector2 desiredPos = CoffinArena.FightCenter;
            coffin.Movement(desiredPos, 0.1f, 14f, 5f, 0.08f, 20f);
            float dist = npc.Distance(desiredPos);
            if (dist > 50f)
            {
                Timer = -1f;
            }
            if (Timer < (float)TelegraphTime && Timer > 0f)
            {
                Vector2 sparkDir = Vector2.UnitX.RotatedByRandom(6.2831854820251465);
                float sparkDistance = 120f * progress * Main.rand.NextFloat(0.6f, 1.3f);
                Vector2 sparkCenter = maskCenter + sparkDir * sparkDistance * 2f;
                float sparkTime = 15f;
                Vector2 sparkVel = (maskCenter - sparkCenter) / sparkTime;
                float sparkScale = 2f - progress * 1.2f;
                Particle spark = (Particle)new SparkParticle(sparkCenter, sparkVel, GlowColor, sparkScale, (int)sparkTime, true, (Color?)null);
                spark.Spawn();
            }
            else if (Timer == (float)TelegraphTime)
            {
                SoundEngine.PlaySound(in CursedCoffin.BigShotSFX, maskCenter);
                int shots = ((!Main.expertMode) ? 5 : ((!WorldSavingSystem.EternityMode) ? 7 : (WorldSavingSystem.MasochistModeReal ? 11 : 9)));
                if (FargoSoulsUtil.HostCheck)
                {
                    float baseRot = Main.rand.NextFloat((float)Math.PI * 2f);
                    for (int i = 0; i < shots; i++)
                    {
                        float rot = baseRot + (float)Math.PI * 2f * ((float)i / (float)shots);
                        Vector2 vel = rot.ToRotationVector2() * 4f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), maskCenter, vel, ModContent.ProjectileType<CoffinWaveShot>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 1f, 2), 1f, Main.myPlayer);
                    }
                }
            }
            else if (Timer > (float)(TelegraphTime + 15) && AI3 < 1f && WorldSavingSystem.MasochistModeReal)
            {
                AI3 = 1f;
                Timer = 0f;
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.WavyShotSlam)]
        public void WavyShotSlam()
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            npc.noTileCollide = false;
            if (Timer == 20f)
            {
                npc.velocity.Y += 13f;
            }
            if (!(Timer >= 0f))
            {
                return;
            }
            if (npc.velocity.Y == 0f)
            {
                npc.velocity.X = 0f;
                SoundStyle style = SoundID.Item14 with
                {
                    Pitch = -0.5f
                };
                SoundEngine.PlaySound(in style, npc.Center);
                SoundEngine.PlaySound(in CursedCoffin.SlamSFX, npc.Center);
                Timer = -180f;
                int dir = Math.Sign(Player.Center.X - CoffinArena.FightCenter.X);
                int leniencyTime = (WorldSavingSystem.MasochistModeReal ? (-20) : (WorldSavingSystem.EternityMode ? 10 : (Main.expertMode ? 20 : 30)));
                Vector2 center = CoffinArena.FightCenter;
                if (!Main.dedServ)
                {
                    ScreenShakeSystem.StartShake(10f, (float)Math.PI * 2f, (Vector2?)null, 0.5f);
                }
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.DD2OgreSmash, 0, 0f, Main.myPlayer);
                }
                for (int i = -1; i < 20; i++)
                {
                    Vector2 projPos = center + dir * Vector2.UnitX * (CoffinArena.Width * 8) * ((float)i / 20f);
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
                        projPos.X = CoffinArena.FightCenter.X + (float)dir * ((float)CoffinArena.Width * 8f - 24f);
                        fromWall = 0;
                    }
                    Projectile.NewProjectile(npc.GetSource_FromThis(), projPos, Vector2.Zero, ModContent.ProjectileType<FallingSandstone>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 1f, 2), 0f, Main.myPlayer, leniencyTime + (int)((float)fromWall * 1.5f) + Main.rand.Next(60, 80));
                }
                if (FargoSoulsUtil.HostCheck && WorldSavingSystem.MasochistModeReal)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        Vector2 vel = Vector2.UnitX * j * 3f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 30f, vel, ModContent.ProjectileType<CoffinSlamShockwave>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 1f, 2), 1f, Main.myPlayer);
                    }
                }
            }
            else
            {
                npc.velocity.Y += 0.2f;
                if (npc.velocity.Y > 0f)
                {
                    npc.velocity.Y += 0.32f;
                }
                if (npc.velocity.Y > 15f)
                {
                    npc.velocity.Y = 15f;
                }
                ExtraTrail = true;
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.GrabbyHands)]
        public void GrabbyHands()
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            npc.noTileCollide = true;
            coffin.HoverSound();
            Vector2 offset = -Vector2.UnitY * 300f + Vector2.UnitX * Math.Sign(npc.Center.X - Player.Center.X) * 200f;
            Vector2 desiredPos = Player.Center + offset;
            desiredPos = CoffinArena.ClampWithinArena(desiredPos, (Entity)npc);
            coffin.Movement(desiredPos, 0.1f, 10f, 5f, 0.08f, 20f);
            if (Timer == 2f)
            {
                AI3 = Main.rand.Next(90, 103);
                npc.netUpdate = true;
            }
            if (Timer > 2f && Timer == AI3)
            {
                foreach (Projectile hand in Main.projectile.Where((Projectile projectile) => FargoExtensionMethods.TypeAlive<CoffinHand>(projectile) && projectile.ai[0] == (float)npc.whoAmI && projectile.ai[1] == 1f))
                {
                    SoundEngine.PlaySound(in CursedCoffin.HandChargeSFX, hand.Center);
                    hand.ai[1] = 10f;
                    hand.netUpdate = true;
                }
            }
            if (Timer < 40f)
            {
                if ((npc.frameCounter += 1.0) % 4.0 == 3.0 && Frame < Main.npcFrameCount[((ModNPC)(object)coffin).Type] - 1)
                {
                    Frame++;
                }
            }
            else if (Timer == 40f)
            {
                SoundEngine.PlaySound(in CursedCoffin.ShotSFX, npc.Center);
                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 dir = npc.rotation.ToRotationVector2();
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, dir * 4f, ModContent.ProjectileType<CoffinHand>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.5f, 2), 1f, Main.myPlayer, npc.whoAmI, 0.98f);
                    p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, dir * 4f, ModContent.ProjectileType<CoffinHand>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.5f, 2), 1f, Main.myPlayer, npc.whoAmI, -0.98f);
                    p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, (npc.rotation + (float)Math.PI / 2f).ToRotationVector2() * 4f, ModContent.ProjectileType<CoffinHand>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.5f, 2), 1f, Main.myPlayer, npc.whoAmI, Main.rand.NextBool() ? 1.5f : (-1.5f));
                }
            }
            else
            {
                int delay = 40;
                if ((npc.frameCounter += 1.0) % (double)delay == (double)(delay - 1) && Frame > 0)
                {
                    Frame--;
                }
            }
        }

        [AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>(BehaviorStates.RandomStuff)]
        public void RandomStuff()
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            ref float RandomProj = ref AI3;
            npc.noTileCollide = true;
            coffin.HoverSound();
            Vector2 dir = CalculateAngle();
            int frameTime = (int)MathF.Floor(60 / Main.npcFrameCount[((ModNPC)(object)coffin).Type]);
            if (Timer < 60f)
            {
                Position();
                if ((npc.frameCounter += 1.0) % (double)frameTime == (double)(frameTime - 1) && Frame < Main.npcFrameCount[((ModNPC)(object)coffin).Type] - 1)
                {
                    Frame++;
                }
            }
            else if (Timer < 370f && Timer >= 60f)
            {
                Position();
                npc.rotation = Vector2.Lerp(npc.rotation.ToRotationVector2(), dir, Timer / 35f).ToRotation();
                npc.velocity.X *= 0.7f;
                int shotTime = (WorldSavingSystem.MasochistModeReal ? 20 : 24);
                shotTime = ((Phase >= 2) ? (shotTime + 5) : (shotTime - 10));
                if (Timer % (float)shotTime == 0f)
                {
                    int num = Main.rand.Next(3);
                    if (1 == 0)
                    {
                    }
                    int num2 = num switch
                    {
                        1 => 5,
                        2 => 6,
                        _ => Main.rand.Next(5),
                    };
                    if (1 == 0)
                    {
                    }
                    RandomProj = num2;
                    npc.netUpdate = true;
                }
                if (Timer % (float)shotTime != (float)(shotTime - 1))
                {
                    return;
                }
                float num3 = RandomProj;
                if (1 == 0)
                {
                }
                SoundStyle soundStyle = ((num3 == 5f) ? SoundID.Item106 : ((num3 != 6f) ? SoundID.Item101 : SoundID.NPCHit2));
                if (1 == 0)
                {
                }
                SoundStyle sound = soundStyle;
                SoundEngine.PlaySound(in sound, npc.Center);
                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 vel = dir;
                    float bound = 0.25f;
                    float rotationBound = 0.025f;
                    if (Phase < 2)
                    {
                        bound = 0.3f;
                        rotationBound = 0.029f;
                    }
                    vel *= Main.rand.NextFloat(1f - bound, 1f + bound);
                    dir = dir.RotatedByRandom((float)Math.PI / 2f * rotationBound);
                    Vector2 offsetDir = Vector2.Normalize(dir);
                    Vector2 posOffset = offsetDir.RotatedBy(1.5707963705062866) * Main.rand.NextFloat(-npc.height / 3, npc.height / 3);
                    posOffset -= offsetDir * 10f;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + posOffset, vel, ModContent.ProjectileType<CoffinRandomStuff>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 1f, 2), 1f, Main.myPlayer, RandomProj);
                }
            }
            else
            {
                npc.velocity *= 0.96f;
                if ((npc.frameCounter += 1.0) % 30.0 == 29.0 && Frame > 0)
                {
                    Frame--;
                }
                npc.rotation *= 0.95f;
            }
            Vector2 CalculateAngle()
            {
                ref float RandomProj2 = ref AI3;
                float gravity = CoffinRandomStuff.Gravity(AI3);
                float xDif = Player.Center.X - npc.Center.X;
                float yDif = Player.Center.Y - npc.Center.Y;
                float velY = -10f;
                float arcTop = yDif - 290f;
                do
                {
                    arcTop -= 10f;
                    if (yDif < 0f && (0f - arcTop) * gravity >= 0f)
                    {
                        float newVelY = (0f - MathF.Sqrt((0f - arcTop) * gravity)) / 1.5f;
                        if (newVelY < velY)
                        {
                            velY = newVelY;
                        }
                    }
                }
                while (MathF.Pow(velY / gravity, 2f) + 2f * yDif / gravity < 0f);
                float sqrtNum = MathF.Pow(velY / gravity, 2f) + 2f * yDif / gravity;
                if (sqrtNum < 0f)
                {
                    sqrtNum = 0f;
                }
                float t = (0f - velY) / gravity + MathF.Sqrt(sqrtNum);
                float velX = xDif / t;
                return velX * Vector2.UnitX + velY * Vector2.UnitY;
            }
            void Position()
            {
                npc.rotation = Vector2.Lerp(npc.rotation.ToRotationVector2(), dir, 0.25f).ToRotation();
                float angle = npc.rotation % ((float)Math.PI * 2f);
                float incline = MathF.Abs(MathF.Sin(angle));
                float angledHeight = (int)(MathHelper.Lerp(npc.height, npc.width, incline) * npc.scale);
                Vector2 desiredPos = CoffinArena.FightCenter + Vector2.UnitY * ((float)(CoffinArena.Height * 8) - angledHeight) + Vector2.UnitX * Math.Sign(npc.Center.X - Player.Center.X) * ((float)(CoffinArena.Width * 8) - (float)npc.width * 1.5f);
                CoffinArena.ClampWithinArena(desiredPos, (Entity)npc);
                coffin.Movement(desiredPos, 0.1f, 20f, 5f, 0.08f, 20f);
            }
        }
        #endregion
        public void HalfDropSand(int dir, int leniencyTime = -20, bool FromWall = true)
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            Vector2 center = CoffinArena.FightCenter;
            if (FargoSoulsUtil.HostCheck)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.DD2OgreSmash, 0, 0f, Main.myPlayer);
            }
            for (int i = -1; i < 20; i++)
            {
                Vector2 projPos = center + dir * Vector2.UnitX * (CoffinArena.Width * 8) * ((float)i / 20f);
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
                    projPos.X = CoffinArena.FightCenter.X + (float)dir * ((float)CoffinArena.Width * 8f - 24f);
                    fromWall = 0;
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), projPos, Vector2.Zero, ModContent.ProjectileType<FallingSandstone>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 0.75f, 2), 0f, Main.myPlayer, leniencyTime + (FromWall ? ((int)((float)fromWall * 1.5f)) : 0) + Main.rand.Next(60, 80));
            }
        }
    }
}
