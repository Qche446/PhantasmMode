using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Content.Bosses.Champions.Timber;
using FargowiltasSouls.Content.Bosses.TrojanSquirrel;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Luminance.Core.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monochrome.Core.Net;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Content.Bosses.Trojan
{
    public class P_TrojanSquirrelArms : PModeNPCBehaviour
    {
        public NPC body;
        public bool Ghost = false;
        public LoopedSoundInstance Loop;
        int looptimer;
        public override int NPCType => ModContent.NPCType<TrojanSquirrelArms>();
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) => !Ghost;
        public override void SetDefaults(NPC npc)
        {
            npc.lifeMax = Convert.ToInt32(1.2f * npc.lifeMax);
            if (Main.getGoodWorld && npc.scale > 1)
                npc.scale -= 0.6f;
        }
        /// <summary>
        /// 表里没有基类那四格 <c>localAI</c>：原来的实现就没调 <c>base.SendExtraAI</c>，迁移保持原样。
        /// </summary>
        private static readonly MonoNetFields<NPC> Fields =
            MonoNet.Fields<NPC>("fpm.trojanSquirrelArms")
                .Float("scale", static npc => npc.scale, static (npc, value) => npc.scale = value)
                .Int("body", static npc => Index(Self(npc).body), static (npc, value) => Self(npc).body = FargoSoulsUtil.NPCExists(value))
                .Bool("ghost", static npc => Self(npc).Ghost, static (npc, value) => Self(npc).Ghost = value);

        private static P_TrojanSquirrelArms Self(NPC npc) => npc.GetGlobalNPC<P_TrojanSquirrelArms>();

        private static int Index(NPC npc) => npc is null ? -1 : npc.whoAmI;

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) => Fields.Write(npc, binaryWriter);

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) => Fields.Read(npc, binaryReader);
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (source is EntitySource_Parent parent && parent.Entity is NPC sourceNPC)
                body = sourceNPC;
        }
        public override bool SafePreAI(NPC npc)
        {
            if (body != null)
                body = FargoSoulsUtil.NPCExists(body.whoAmI, ModContent.NPCType<TrojanSquirrel>());

            if (body == null)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    npc.life = 0;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                    npc.active = false;
                }
                return false;
            }
            npc.velocity = Vector2.Zero;
            npc.target = body.target;
            npc.direction = npc.spriteDirection = body.direction;
            var P_Sq = body.GetGlobalNPC<P_TrojanSquirrel>();
            if (!P_Sq.FinalPhase)
                npc.Center = body.Bottom + new Vector2(18f * npc.direction, -105f) * body.scale;
            else
            {
                npc.direction = npc.spriteDirection = P_Sq.head.direction;
                npc.Center = P_Sq.head.Center + new Vector2(-12f * npc.direction, -8) * body.scale;
            }
            if (npc.ai[0] != 1 && Loop?.HasLoopSoundBeenStarted == true)
            {
                looptimer = 0;
                Loop?.Stop();
            }
            ArmsAI(npc);
            return false;
        }
        public void ArmsAI(NPC npc)
        {
            switch ((int)npc.ai[0])
            {
                case 0: PrepareForAttack(npc); break;
                case 1: Chains(npc); break;
                case 2: SnowBalls(npc); break;
                case 3: Final_Wait(npc); break;
                case 4: Final_SnowBalls(npc); break;
            }
        }
        private void PrepareForAttack(NPC npc)
        {
            if (body.ai[0] == 0 && body.localAI[0] <= 0)
            {
                npc.ai[1] += WorldSavingSystem.EternityMode ? 1.5f : 1f;

                if (body.dontTakeDamage)
                    npc.ai[1] += 1f;

                int threshold = 360;

                //structured like this so body gets priority first
                int stallPoint = threshold - 30;
                if (npc.ai[1] > stallPoint)
                {
                    //TrojanSquirrel squirrel = body.As<TrojanSquirrel>();
                    var sq = body.GetGlobalNPC<P_TrojanSquirrel>();
                    if (sq.head != null && sq.head.ai[0] != 0f) //wait if other part is attacking
                        npc.ai[1] = stallPoint;
                }

                if (npc.ai[1] > threshold && Math.Abs(body.velocity.Y) < 0.05f)
                {
                    //dont attack unless player is in 90 degree cone in front of squrrl
                    float baseAngle = npc.direction > 0 ? 0f : MathHelper.Pi;
                    if (Math.Abs(MathHelper.WrapAngle(npc.SafeDirectionTo(Main.player[npc.target].Center).ToRotation() - baseAngle)) > MathHelper.PiOver4)
                    {
                        npc.ai[1] = stallPoint;
                    }
                    else
                    {
                        npc.ai[0] = 1 + npc.ai[2];
                        npc.ai[1] = 0;
                        if (Main.expertMode)
                            npc.ai[2] = npc.ai[2] == 0 ? 1 : 0;
                        npc.netUpdate = true;

                        body.localAI[3] = Math.Sign(body.SafeDirectionTo(Main.player[body.target].Center).X);
                        body.netUpdate = true;
                    }
                }
            }
        }
        private void Chains(NPC npc)
        {
            if (++looptimer >= 90)
            {
                Loop ??= LoopedSoundManager.CreateNew(FargosSoundRegistry.TrojanHookLoop with { Volume = 0.5f }, () =>
                {
                    return npc == null || !npc.active || npc.ai[0] != 1;
                });

                Loop?.Update(npc.Center);

                if (Loop?.HasBeenStopped == true && Loop?.HasLoopSoundBeenStarted == true)
                {
                    Loop?.Restart();
                }
            }

            int start = 30;
            int end = 180;

            int teabagInterval = start / 3;

            if (npc.ai[1] < start) //better for animation
            {
                body.velocity.X *= 0.9f;
                if (npc.ai[1] <= 1)
                    SoundEngine.PlaySound(FargosSoundRegistry.TrojanHookTelegraph, npc.Center);
            }

            npc.ai[1]++;

            //to help animate body
            npc.ai[3] = npc.ai[1] < start && npc.ai[1] % teabagInterval < teabagInterval / 2 ? 1 : 0;

            if (npc.ai[1] > start && npc.ai[1] < end && npc.ai[1] % (Main.getGoodWorld ? 20 : 35) == 0)
            {
                Vector2 pos = GetShootPos(npc);
                float baseAngle = npc.direction > 0 ? 0f : MathHelper.Pi;
                float angle = npc.SafeDirectionTo(Main.player[npc.target].Center).ToRotation();
                if (Math.Abs(MathHelper.WrapAngle(angle - baseAngle)) > MathHelper.PiOver2)
                    angle = MathHelper.PiOver2 * Math.Sign(angle);

                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), pos, 8f * angle.ToRotationVector2(), ModContent.ProjectileType<TrojanHook>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    float max = Main.rand.Next(5, 9);
                    if (Main.getGoodWorld)
                    { 
                        for (int i = 0; i < max; i++)
                        {
                            float extravelx = (i / max) * (Main.player[npc.target].Center.X - npc.Top.X) / 50f;
                            int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Top, new Vector2(Main.rand.NextFloat(-5, 5) + extravelx, Main.rand.NextFloat(-3)),
                                        Main.rand.Next(326, 329), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                            if (p != Main.maxProjectiles)
                                Main.projectile[p].timeLeft = 60;
                        }
                    }
                }
            }

            if (npc.ai[1] > 170 && FargoSoulsUtil.HostCheck && Main.LocalPlayer.ownedProjectileCounts[ModContent.ProjectileType<TrojanHook>()] <= 0)
            {
                npc.ai[0] = 0;
                npc.ai[1] = 0;
                npc.netUpdate = true;

                body.localAI[3] = 0;
                body.netUpdate = true;
            }
        }
        private void SnowBalls(NPC npc)
        {
            npc.ai[1]++;

            int start = 70;
            int end = 340;
            if (WorldSavingSystem.EternityMode)
            {
                start -= 30;
                end -= 30;
            }
            if (WorldSavingSystem.MasochistModeReal)
                end -= 60;

            body.velocity.X *= 0.98f;

            if (npc.ai[1] == 10)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = GetShootPos(npc);
                    SoundEngine.PlaySound(FargosSoundRegistry.TrojanGunStartup, pos);
                    for (int j = 0; j < 20; j++)
                    {
                        int d = Dust.NewDust(pos, 0, 0, DustID.SnowBlock, Scale: 3f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 4f;
                        Main.dust[d].velocity.X += npc.direction * Main.rand.NextFloat(6f, 24f);
                    }
                }
            }

            if (npc.ai[1] > start && npc.ai[1] % 4 == 0)
            {
                SoundEngine.PlaySound(FargosSoundRegistry.Minigun, GetShootPos(npc));
                if (npc.ai[1] % 8 == 0)
                {
                    Vector2 pos = GetShootPos(npc);

                    SoundEngine.PlaySound(FargosSoundRegistry.TrojanSnowball, pos);

                    float ratio = (npc.ai[1] - start) / (end - start);

                    Vector2 target = npc.Center;
                    target.X += Math.Sign(npc.direction) * (WorldSavingSystem.EternityMode ? 1800f : 1200f) * ratio; //gradually targets further and further
                                                                                                                     //target.Y -= 8 * 16;
                    target += Main.rand.NextVector2Circular(16, 16);
                    const float gravity = 0.5f;
                    float time = 45f;
                    Vector2 distance = target - pos;
                    distance.X /= time;
                    distance.Y = distance.Y / time - 0.5f * gravity * time;
                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), pos, distance, ModContent.ProjectileType<TrojanSnowball>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, gravity);
                }
                npc.ai[1] += npc.ai[1] > end / 3 ? npc.ai[1] > end * (2 / 3) ? 3 : 1 : 0;
            }

            if (npc.ai[1] > end)
            {
                npc.ai[0] = 0;
                npc.ai[1] = 0;
                npc.netUpdate = true;

                body.localAI[3] = 0;
                body.netUpdate = true;
            }
        }
        private void Final_Wait(NPC npc)
        {
            int threshold = 90;
            int stallPoint = threshold - 15;
            if (npc.ai[1] > stallPoint)
            {
                var sq = body.GetGlobalNPC<P_TrojanSquirrel>();
                if (sq.head != null && sq.head.ai[0] != 3f)
                    npc.ai[1] = stallPoint;
            }
            if (++npc.ai[1] > threshold && Math.Abs(body.velocity.Y) < 0.05f && body.localAI[0] <= 0)
            {
                npc.ai[0] = 4 ;
                npc.ai[1] = 0;
                npc.ai[2] = npc.ai[2] == 0 ? 1 : 0;
                npc.netUpdate = true;
            }
        }
        private void Final_SnowBalls(NPC npc)
        {
            //if (Main.getGoodWorld) body.velocity.X *= 0.98f;
            if (++npc.ai[2] > 5 && npc.ai[1] < 420)
            {
                npc.ai[2] = 0;

                if (FargoSoulsUtil.HostCheck)
                {
                    int damage = npc.ai[1] > 120 ? FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage) : 0;
                    int alpha = npc.ai[1] > 120 ? 0 : 150;
                    for (int i = -2; i <= 2; i++)
                    {
                        Vector2 speed = new(5f * i, -20f);
                        int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed, ModContent.ProjectileType<TrojanSnowball>(), damage, 0f, Main.myPlayer, 0.5f);
                        Main.projectile[p].alpha = alpha;
                    }
                }
            }

            if (++npc.ai[1] > 510)
            {
                npc.TargetClosest();
                npc.ai[0] = 3;
                npc.ai[1] = 0;
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        public override bool CheckDead(NPC npc)
        {
            if (!Ghost)
            {
                Ghost = true;
                npc.dontTakeDamage = true;
                npc.life = 1;
                return false;
            }
            return true;
        }
        private static Vector2 GetShootPos(NPC NPC)
        {
            NPC.localAI[0] = NPC.localAI[0] == 0 ? 1 : 0;

            Vector2 pos = NPC.Bottom;
            pos.X += NPC.width / 2f * NPC.direction;
            pos.Y -= 16 * NPC.scale;
            pos.X -= (NPC.localAI[0] == 0 ? 10 : 48) * NPC.direction * NPC.scale;
            return pos;
        }
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (body == null)
                return false;

            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;
            Rectangle rectangle = npc.frame;
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = drawColor;
            color26 = npc.GetAlpha(color26);

            SpriteEffects effects = npc.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            bool final = body.GetGlobalNPC<P_TrojanSquirrel>().FinalPhase;
            bool Trail = (body.ai[0] == 0 && body.localAI[0] > 0) || final;
            if (Trail)
            {
                for (int i = 0; i < NPCID.Sets.TrailCacheLength[npc.type]; i++) //math.min to safeguard against uncached trail
                {
                    float oldrot = npc.oldRot[i];
                    Vector2 oldCenter = body.oldPos[i] + body.Size / 2;
                    if (final)
                        oldCenter = npc.oldPos[i] + npc.Size / 2;
                    DrawData oldGlow = new(texture2D13, oldCenter - screenPos + new Vector2(0f, npc.gfxOffY - 53 * body.scale), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26 * (0.5f / i), oldrot, origin2, npc.scale, effects, 0);
                    GameShaders.Misc["LCWingShader"].UseColor(Color.Blue).UseSecondaryColor(Color.Black);
                    GameShaders.Misc["LCWingShader"].Apply(oldGlow);
                    oldGlow.Draw(spriteBatch);
                }
            }


            Vector2 center = body.Center;
            if (final)
                center = npc.Center;
            Main.EntitySpriteDraw(texture2D13, center - screenPos + new Vector2(0f, npc.gfxOffY - 53 * body.scale), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26, npc.rotation, origin2, npc.scale, effects, 0);

            return false;
        }
    }
}
