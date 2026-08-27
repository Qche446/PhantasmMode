using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Assets.Particles;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Content.Bosses.TrojanSquirrel;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Items.Summons;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Content.Bosses.Trojan
{
    public class P_TrojanSquirrel : PModeNPCBehaviour
    {
        private const float BaseWalkSpeed = 4f;
        public NPC head;
        public bool HeadGhost => head?.GetGlobalNPC<P_TrojanSquirrelHead>().Ghost ?? false;
        public NPC arms;
        public bool ArmsGhost => arms?.GetGlobalNPC<P_TrojanSquirrelArms>().Ghost ?? false;
        public bool FinalPhase => HeadGhost && ArmsGhost;
        public bool Translated = false;
        bool hasplayedbreaksound;
        public bool Jumping = false;
        public override int NPCType => ModContent.NPCType<TrojanSquirrel>();
        #region 杂
        public override void SetDefaults(NPC npc)
        {
            npc.lifeMax = Convert.ToInt32(1.2f * npc.lifeMax);
            if (Main.getGoodWorld && npc.scale > 1)
                npc.scale -= 0.6f;
        }
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);
            binaryWriter.Write(npc.scale);
            binaryWriter.Write(head is NPC ? head.whoAmI : -1);
            binaryWriter.Write(arms is NPC ? arms.whoAmI : -1);
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);
            npc.scale = binaryReader.ReadSingle();
            head = FargoSoulsUtil.NPCExists(binaryReader.ReadInt32());
            arms = FargoSoulsUtil.NPCExists(binaryReader.ReadInt32());
        }
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            LeadingConditionRule rule = new(new Conditions.NotExpert());
            rule.OnSuccess(ItemDropRule.OneFromOptions(1, BaseForce.EnchantsIn<TimberForce>()));
            npcLoot.Add(rule);
        }
        #endregion
        public override void OnFirstTick(NPC npc)
        {
            npc.TargetClosest(false);
            if (FargoSoulsUtil.HostCheck)
            {
                head = FargoSoulsUtil.NPCExists(FargoSoulsUtil.NewNPCEasy(npc.GetSource_FromThis(), npc.Center, ModContent.NPCType<TrojanSquirrelHead>(), npc.whoAmI, target: npc.target));
                arms = FargoSoulsUtil.NPCExists(FargoSoulsUtil.NewNPCEasy(npc.GetSource_FromThis(), npc.Center, ModContent.NPCType<TrojanSquirrelArms>(), npc.whoAmI, target: npc.target));
            }
            if (!WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.TrojanSquirrel] && FargoSoulsUtil.HostCheck)
                Item.NewItem(npc.GetSource_Loot(), Main.player[npc.target].Hitbox, ModContent.ItemType<SquirrelCoatofArms>());
            npc.ai[0] = 1f;
            npc.ai[3] = 1f;
            for (int i = 0; i < 80; i++)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Smoke, npc.velocity.X, npc.velocity.Y, 50, default, 4f);
                Main.dust[d].velocity.Y -= 1.5f;
                Main.dust[d].velocity *= 1.5f;
                Main.dust[d].noGravity = true;
            }
            FargoSoulsUtil.GrossVanillaDodgeDust(npc);
        }
        public override bool SafePreAI(NPC npc)
        {
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();
            Player player = Main.player[npc.target];
            npc.direction = npc.spriteDirection = npc.Center.X < player.Center.X ? 1 : -1;
            bool despawn = false;
            switch (Convert.ToInt32(npc.ai[0]))
            {
                case 0: WalkorDash(npc, player, ref despawn); break;
                case 1: PrepareAttack(npc, player); break;
                case 2: JumpAttack(npc, player); break;
                default:
                    npc.ai[0] = 0;
                    goto case 0;
            }
            if (despawn)
            {
                if (npc.timeLeft > 60)
                    npc.timeLeft = 60;
            }
            else
            {
                if (npc.timeLeft < 600)
                    npc.timeLeft = 600;
            }
            ManageExtraThing(npc);
            SmokeVisuals(npc);
            return false;
        }
        private void WalkorDash(NPC npc, Player player, ref bool despawn)
        {
            Vector2 target = player.Bottom - Vector2.UnitY;
            if (npc.localAI[0] > 0) //doing running attack
            {
                npc.localAI[0] -= 1f;

                if (npc.localAI[0] % 10 == 0) //hermes boot clouds
                {
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Challengers/Trojan/TrojanFootstep") with { Variants = [1, 2, 3], Volume = 0.5f }, npc.Bottom);
                    Vector2 vel = (-npc.velocity).RotatedByRandom(MathHelper.Pi / 11f);
                    vel /= 2;
                    Gore gore = Gore.NewGoreDirect(player.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 10, vel, Main.rand.Next(11, 14), Scale: Main.rand.NextFloat(1.5f, 2f));
                    gore.timeLeft /= 2;
                }

                float distance = npc.Center.X - target.X;
                bool passedTarget = Math.Sign(distance) == npc.localAI[1];
                if (passedTarget && Math.Abs(distance) > 160)
                    npc.localAI[0] = 0f;

                target = new Vector2(npc.Center.X + 256f * npc.localAI[1], target.Y);

                if (npc.localAI[0] == 0f)
                    npc.TargetClosest(false);

                if (npc.localAI[0] % 3 == 0 && FargoSoulsUtil.HostCheck && Main.getGoodWorld)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Top.X, npc.Top.Y, Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-3),
                        Main.rand.Next(326, 329), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    if (p != Main.maxProjectiles)
                        Main.projectile[p].timeLeft = 60;
                }
            }
            else if (!npc.HasValidTarget || npc.Distance(player.Center) > 2400)
            {
                target = npc.Center + new Vector2(256f * Math.Sign(npc.Center.X - player.Center.X), -128);
                npc.TargetClosest(false);
                despawn = true;
            }

            if (Math.Abs(npc.velocity.Y) < 0.05f && npc.localAI[3] >= 2)
            {
                if (npc.localAI[3] == 2)
                {
                    npc.localAI[3] = 0f;
                }
                else
                {
                    npc.localAI[3] -= 1;
                    npc.ai[0] = 1f;
                    npc.ai[3] = 1f;
                }
                SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Challengers/Trojan/TrojanJump") with { Variants = [1, 2] }, npc.Bottom);

                //ExplodeAttack(npc);
            }

            bool goFast = despawn || npc.localAI[0] > 0;
            Movement(npc, target, goFast);

            if (arms != null && (npc.localAI[3] == -1 || npc.localAI[3] == 1)) //from arms
                npc.direction = npc.spriteDirection = (int)npc.localAI[3];

            bool canDoAttacks = WorldSavingSystem.EternityMode && !goFast;
            if (canDoAttacks) //decide next action
            {
                float increment = 1f;
                if (HeadGhost)
                    increment += 0.5f;
                if (ArmsGhost)
                    increment += 0.5f;
                increment += 1f;
                if (npc.dontTakeDamage)
                    increment /= 2;

                if (target.Y > npc.Top.Y)
                    npc.ai[1] += increment;
                else
                    npc.ai[2] += increment;
                if (arms.ai[0] == 4)
                    npc.ai[1] += 2;
                if (head.ai[0] == 5)
                    npc.ai[2] += 2;
                if (Math.Abs(npc.velocity.Y) < 0.05f)
                {
                    //its structured like this to ensure body picks the right attack for the situation after being delayed by head/arms
                    bool canProceed = !(head != null && head.ai[0] != 0) && !(arms != null && arms.ai[0] != 0);
                    if (FinalPhase) canProceed = (head == null || head.ai[0] == 3) && (arms == null || arms.ai[0] == 3);
                    int threshold = 300;
                    if (npc.ai[1] > threshold)
                    {
                        //冲刺
                        if (canProceed || (arms.ai[0] == 4 && arms.ai[1] > 120))
                        {
                            npc.ai[0] = 1f;
                            npc.ai[1] = 0f;
                            //npc.ai[2] = 0f;
                            npc.ai[3] = 0f;
                            npc.localAI[0] = 0f;
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.ai[1] -= 10f;
                        }
                    }

                    if (npc.ai[2] > threshold)
                    {
                        //跳
                        if (canProceed || (head.ai[0] == 5 && head.ai[1] > 60))
                        {
                            npc.ai[0] = 1f;
                            //npc.ai[1] = 0f;
                            npc.ai[2] = 0f;
                            npc.ai[3] = 1f;
                            npc.localAI[0] = 0f;
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.ai[2] -= 10f;
                        }
                    }
                }
            }
        }
        private void PrepareAttack(NPC npc, Player player)
        {
            npc.velocity.X = 0;

            TileCollision(npc, player.Bottom.Y - 1 > npc.Bottom.Y, Math.Abs(player.Center.X - npc.Center.X) < npc.width / 2 && npc.Bottom.Y < player.Bottom.Y - 1);

            int threshold = 85;
            if (HeadGhost)
                threshold -= 15;
            if (ArmsGhost)
                threshold -= 15;
            if (FinalPhase)
                threshold -= 5;
            threshold -= 20;
            if (npc.ai[3] != 0f) //telegraphing jump
            {
                int dir = npc.localAI[0] % 2 == 0 ? 1 : -1;
                int maxShake = 8;
                float shake = dir * maxShake * (npc.localAI[0] / threshold);
                npc.position.X += shake;
            }

            if (++npc.localAI[0] > threshold)
            {
                npc.localAI[0] = 0f;
                npc.netUpdate = true;

                if (npc.ai[3] == 0f)
                {
                    npc.ai[0] = 0f;

                    npc.localAI[0] = 300f;
                    npc.localAI[1] = Math.Sign(player.Center.X - npc.Center.X);
                    npc.localAI[2] = player.Center.X;
                }
                else
                {
                    npc.ai[0] = 2f;
                }
            }
        }
        private void JumpAttack(NPC npc, Player player)
        {
            const float gravity = 0.4f;
            float time = ArmsGhost ? 60f : 90f;

            if (npc.localAI[0]++ == 0)
            {
                Vector2 distance = player.Top - npc.Bottom;

                if (ArmsGhost)
                {
                    distance.X += npc.width * Math.Sign(player.Center.X - npc.Center.X);

                    if (npc.localAI[3] < 2)
                    {
                        npc.localAI[3] = 2; //flag to stomp again on landing
                        if (head == null)
                            npc.localAI[3] += 2; //flag to do more stomps
                    }

                    //ExplodeAttack(npc);
                }

                distance.X /= time;
                distance.Y = distance.Y / time - 0.5f * gravity * time;
                npc.velocity = distance;

                npc.netUpdate = true;

                if (!ArmsGhost)
                {
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Challengers/Trojan/TrojanJump") with { Variants = [1, 2] }, npc.Bottom);
                }
                else
                {
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Challengers/Trojan/TrojanJumpExplosive") with { Variants = [1, 2] }, npc.Bottom);
                }


                for (int i = 0; i < 4; i++)
                {
                    int side = i % 2 == 0 ? 1 : -1;
                    float speed = Main.rand.NextFloat(4, 6);
                    Vector2 vel = (Vector2.UnitX * side * speed).RotatedByRandom(MathHelper.Pi / 11);
                    Gore gore = Gore.NewGoreDirect(npc.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 10, vel, Main.rand.Next(11, 14), Scale: 2f);
                }

                Jumping = true;
            }
            else
            {
                npc.velocity.Y += gravity;
            }

            if (npc.localAI[0] > time)
            {
                npc.TargetClosest(false);

                npc.velocity.X = Utils.Clamp(npc.velocity.X, -20, 20);
                npc.velocity.Y = Utils.Clamp(npc.velocity.Y, -10, 10);

                npc.ai[0] = 0f;
                npc.localAI[0] = 0f;
                npc.netUpdate = true;
            }
        }
        private void ManageExtraThing(NPC npc)
        {
            if (HeadGhost)
            {
                Vector2 pos = npc.Top;
                pos.X += 2f * 16f * npc.direction;
                pos.Y -= 8f;

                int width = 4 * 16;
                int height = 2 * 16;

                pos.X -= width / 2f;
                pos.Y -= height / 2f;

                /*for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(pos, width, height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y, 50, default, 2.5f);
                    Main.dust[d].velocity.Y -= 1.5f;
                    Main.dust[d].velocity *= 1.5f;
                    Main.dust[d].noGravity = true;
                }*/

                if (Main.rand.NextBool(3))
                {
                    int d = Dust.NewDust(pos, width, height, DustID.Torch, npc.velocity.X * 0.4f, npc.velocity.Y * 0.4f, 100, default, 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity.Y -= 3f;
                    Main.dust[d].velocity *= 1.5f;
                }
            }
            head = FargoSoulsUtil.NPCExists(head.whoAmI, ModContent.NPCType<TrojanSquirrelHead>());

            if (ArmsGhost)
            {
                Vector2 pos = npc.Center;
                pos.X -= 16f * npc.direction;
                pos.Y -= 3f * 16f;

                int width = 2 * 16;
                int height = 2 * 16;

                pos.X -= width / 2f;
                pos.Y -= height / 2f;

                /*for (int i = 0; i < 2; i++)
                {
                    int d = Dust.NewDust(pos, width, height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y, 50, default, 1.5f);
                    Main.dust[d].noGravity = true;
                }*/

                /*if (Main.rand.NextBool(6))
                {
                    int d2 = Dust.NewDust(pos, width, height, DustID.Torch, NPC.velocity.X * 0.4f, NPC.velocity.Y * 0.4f, 100, default, 3f);
                    Main.dust[d2].noGravity = true;
                }*/
            }
            arms = FargoSoulsUtil.NPCExists(arms.whoAmI, ModContent.NPCType<TrojanSquirrelArms>());

            /*if (NPC.life < NPC.lifeMax / 2 && Main.rand.NextBool(3))
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y, 50, default, 4f);
                Main.dust[d].velocity.Y -= 1.5f;
                Main.dust[d].velocity *= 1.5f;
                Main.dust[d].noGravity = true;
            }*/
            if (!Translated && FinalPhase)
            {
                Translated = true;
                head.ai[0] = arms.ai[0] = 3;
                head.ai[1] = head.ai[3] = 0;
                arms.ai[1] = arms.ai[3] = 0;
                npc.netUpdate = head.netUpdate = arms.netUpdate = true;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    var proj = Main.projectile[i];
                    if (proj.active && proj.type == ModContent.ProjectileType<TrojanHook>())
                        proj.Kill();
                }
            }
            bool wasImmune = npc.dontTakeDamage;
            npc.dontTakeDamage = (!HeadGhost || !ArmsGhost);

            if (wasImmune != npc.dontTakeDamage)
            {
                for (int i = 0; i < 6; i++)
                    ExplodeDust(npc, npc.position + new Vector2(Main.rand.Next(npc.width), Main.rand.Next(npc.height)));
            }
            // to prevent a bug where he played the sound again.
            if (hasplayedbreaksound == false && (ArmsGhost && HeadGhost))
            {
                SoundEngine.PlaySound(FargosSoundRegistry.TrojanLegsDeath, npc.Center);
                hasplayedbreaksound = true;
            }
        }
        #region 辅助方法
        private static void ExplodeAttack(NPC npc)
        {
            if (FargoSoulsUtil.HostCheck)
            {
                float offsetX = npc.width;
                const float offsetY = 65;
                int max = WorldSavingSystem.MasochistModeReal ? 4 : 2;
                for (int i = -max; i <= max; i++)
                {
                    Projectile p = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Bottom + new Vector2(offsetX * i, -offsetY), Vector2.Zero, ProjectileID.DD2ExplosiveTrapT3Explosion, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer);
                    if (p != null)
                    {
                        p.friendly = false;
                        p.hostile = true;
                        p.netUpdate = true;
                    }
                }

            }
        }
        private void ManageFTWEatWood(NPC npc)
        {
            if (WorldSavingSystem.MasochistModeReal && Main.getGoodWorld && FargoSoulsUtil.HostCheck)
            {
                int[] edibleTiles =
                [
                    TileID.WoodBlock,
                    TileID.AshWood,
                    TileID.BorealWood,
                    TileID.DynastyWood,
                    TileID.LivingWood,
                    TileID.PalmWood,
                    TileID.SpookyWood,
                    TileID.Ebonwood,
                    TileID.Pearlwood,
                    TileID.Shadewood,
                    TileID.Trees,
                    TileID.TreeAsh,
                    TileID.ChristmasTree,
                    TileID.PalmTree,
                    TileID.PineTree,
                    TileID.VanityTreeSakura,
                    TileID.VanityTreeYellowWillow,
                    TileID.LivingMahoganyLeaves
                ];
                for (float x = npc.position.X; x < npc.BottomRight.X; x += 16)
                {
                    for (float y = npc.position.Y; y < npc.BottomRight.Y; y += 16)
                    {
                        Tile tile = Framing.GetTileSafely(new Vector2(x, y));
                        if (tile != null && edibleTiles.Contains(tile.TileType))
                        {
                            int xCoord = (int)x / 16;
                            int yCoord = (int)y / 16;
                            WorldGen.KillTile(xCoord, yCoord, noItem: true);
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendTileSquare(-1, xCoord, yCoord, 1);

                            npc.scale += 0.01f;
                            npc.netUpdate = true;
                            if (head is NPC)
                            {
                                head.scale += 0.01f;
                                head.netUpdate = true;
                            }
                            if (arms is NPC)
                            {
                                arms.scale += 0.01f;
                                arms.netUpdate = true;
                            }
                        }
                    }
                }
            }
        }
        private void SmokeVisuals(NPC npc)
        {
            int headsmokedir = 0;
            int armsmokedir = 0;

            if (npc.direction == 1)
            {
                headsmokedir = 50;
                armsmokedir = 5;
            }

            if (npc.direction == -1)
            {
                headsmokedir = 5;
                armsmokedir = 25;
            }
            Vector2 headsmokepos = npc.Center + new Vector2(headsmokedir, -35);

            Vector2 armsmokepos = npc.Center + new Vector2(armsmokedir, -35);

            int smokeAmount = 1;

            if (head == null)
            {
                smokeAmount = 5;
            }

            if (head == null && arms == null)
            {
                smokeAmount = 3;
            }

            if (head == null && arms == null && npc.life == npc.lifeMax / 2)
            {
                smokeAmount = 1;
            }

            if (Main.rand.NextBool(smokeAmount) && head == null)
            {
                Particle p = new SmokeParticle(headsmokepos, new Vector2(0, Main.rand.Next(-10, -5)), Color.Gray, 50, 1f, 0.05f);
                p.Spawn();
            }

            if (Main.rand.NextBool(3) && arms == null && head != null)
            {
                Particle p = new SmokeParticle(armsmokepos, new Vector2(0, Main.rand.Next(-10, -5)), Color.Gray, 50, 0.5f, 0.05f);
                p.Spawn();
                //Particle p2 = new SmokeParticle(armsmokepos * -1, new Vector2(0, Main.rand.Next(-10, -5)), Color.Gray, 50, 0.5f, 0.05f);
                //p2.Spawn();
            }
        }

        private static void ExplodeDust(NPC npc, Vector2 center)
        {
            const int width = 32;
            const int height = 32;

            Vector2 pos = center - new Vector2(width, height) / 2f;

            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(pos, width, height, DustID.Smoke, 0f, 0f, 100, default, 3f);
                Main.dust[dust].velocity *= 1.4f;
            }

            for (int i = 0; i < 15; i++)
            {
                int dust = Dust.NewDust(pos, width, height, DustID.Torch, 0f, 0f, 100, default, 3.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 7f;

                dust = Dust.NewDust(pos, width, height, DustID.Torch, 0f, 0f, 100, default, 1.5f);
                Main.dust[dust].velocity *= 3f;
            }

            float scaleFactor9 = 0.5f;
            for (int j = 0; j < 3; j++)
            {
                int gore = Gore.NewGore(npc.GetSource_FromThis(), center, default, Main.rand.Next(61, 64));
                Main.gore[gore].velocity *= scaleFactor9;
                Main.gore[gore].velocity.X += 1f;
                Main.gore[gore].velocity.Y += 1f;
            }
        }
        private void Movement(NPC npc, Vector2 target, bool goFast = false)
        {
            npc.direction = npc.spriteDirection = npc.Center.X < target.X ? 1 : -1;
            if (Math.Abs(target.X - npc.Center.X) < npc.width / 2)
            {
                npc.velocity.X *= 0.9f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;
            }
            else
            {
                float maxwalkSpeed = BaseWalkSpeed;
                if (HeadGhost)
                    maxwalkSpeed *= 1.25f;
                if (ArmsGhost)
                    maxwalkSpeed *= 1.25f;
                if (goFast)
                    maxwalkSpeed *= 3f;
                if (npc.dontTakeDamage && !FinalPhase)
                    maxwalkSpeed *= 0.75f;
                //ph、change
                maxwalkSpeed *= Main.getGoodWorld ? 1.5f : 1.2f;
                int walkModifier = 20;
                if (FinalPhase)
                    walkModifier = 40;
                //if (npc.Distance(Main.player[npc.target].Center) < 100 && FinalPhase && goFast)
                   // walkModifier = 60;
                if (npc.direction > 0)
                    npc.velocity.X = (npc.velocity.X * walkModifier + maxwalkSpeed) / (walkModifier + 1);
                else
                    npc.velocity.X = (npc.velocity.X * walkModifier - maxwalkSpeed) / (walkModifier + 1);
            }
            TileCollision(npc, target.Y > npc.Bottom.Y, Math.Abs(target.X - npc.Center.X) < npc.width / 2 && npc.Bottom.Y < target.Y);
        }
        private void TileCollision(NPC npc, bool fallthrough = false, bool dropDown = false)
        {
            bool onPlatforms = false;
            for (int i = (int)npc.position.X; i <= npc.position.X + npc.width; i += 16)
            {
                if (Framing.GetTileSafely(new Vector2(i, npc.Bottom.Y + 2)).TileType == TileID.Platforms)
                {
                    onPlatforms = true;
                    break;
                }
            }
            bool onCollision = Collision.SolidCollision(npc.position, npc.width, npc.height);
            if (dropDown)
            {
                npc.velocity.Y += 0.5f;
            }
            else if (onCollision || onPlatforms && !fallthrough)
            {
                if (npc.velocity.Y > 0f)
                    npc.velocity.Y = 0f;

                if (npc.velocity.Y > -0.2f)
                    npc.velocity.Y -= 0.025f;
                else
                    npc.velocity.Y -= 0.2f;

                if (npc.velocity.Y < -4f)
                    npc.velocity.Y = -4f;

                if (Jumping) //landing effects
                {
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Challengers/Trojan/TrojanJump") with { Variants = [1, 2] }, npc.Bottom);
                    for (int i = 0; i < 4; i++)
                    {
                        int side = i % 2 == 0 ? 1 : -1;
                        float speed = Main.rand.NextFloat(4, 6);
                        Vector2 vel = (Vector2.UnitX * side * speed).RotatedByRandom(MathHelper.Pi / 11);
                        Gore gore = Gore.NewGoreDirect(npc.GetSource_FromThis(), npc.Bottom - Vector2.UnitY * 10, vel, Main.rand.Next(11, 14), Scale: 2f);
                    }
                    Jumping = false;
                }
            }
            else
            {
                if (npc.velocity.Y < 0f)
                    npc.velocity.Y = 0f;

                if (npc.velocity.Y < 0.1f)
                    npc.velocity.Y += 0.025f;
                else
                    npc.velocity.Y += 0.5f;
            }

            if (npc.velocity.Y > 10f)
                npc.velocity.Y = 10f;

            Player target = Main.player[npc.target];
            //anti-leaving soon when terrain above you
            if (onCollision && target != null && target.active && !target.dead && target.Center.Y > npc.Center.Y + npc.height * 1.5f && Math.Abs(target.Center.X - npc.Center.X) < 400)
            {
                npc.velocity.Y += 3f;
            }
        }
        #endregion
    }
}
