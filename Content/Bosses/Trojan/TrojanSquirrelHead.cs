using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Content.Bosses.TrojanSquirrel;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class P_TrojanSquirrelHead : PModeNPCBehaviour
    {
        public NPC body;
        public bool Ghost = false;
        public override int NPCType => ModContent.NPCType<TrojanSquirrelHead>();
        public override void SetDefaults(NPC npc)
        {
            npc.lifeMax = Convert.ToInt32(1.2f * npc.lifeMax);
            if (Main.getGoodWorld && npc.scale > 1)
                npc.scale -= 0.6f;
        }
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) => !Ghost;
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(npc.scale);
            binaryWriter.Write(body is NPC ? body.whoAmI : -1);
            bitWriter.WriteBit(Ghost);
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            npc.scale = binaryReader.ReadSingle();
            body = FargoSoulsUtil.NPCExists(binaryReader.ReadInt32());
            Ghost = bitReader.ReadBit();
        }
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
            var P_Sq = body.GetGlobalNPC<P_TrojanSquirrel>();
            npc.target = body.target;
            if (!P_Sq.FinalPhase) 
            {
                npc.velocity = Vector2.Zero;
                npc.direction = npc.spriteDirection = body.direction;
                npc.Center = body.Bottom + new Vector2(42f * npc.direction, -153f) * body.scale;
            }
            HeadAI(npc);
            return false;
        }
        private void HeadAI(NPC npc)
        {
            switch ((int)npc.ai[0])
            {
                case 0: PrepareForAttack(npc); break;
                case 1: AcornSpray(npc); break;
                case 2: SquirrelBarrage(npc); break;
                case 3: Final_Wait(npc); break;
                case 4: Final_AcornSpray(npc); break;
                case 5: Final_SquirrelBarrage(npc); break;
            }
        }
        private void PrepareForAttack(NPC npc)
        {
            if (body.ai[0] == 0 && body.localAI[0] <= 0)
            {
                npc.ai[1] += 1.5f;

                if (body.dontTakeDamage)
                    npc.ai[1] += 1f;

                int threshold = 240;

                //structured like this so body gets priority first
                int stallPoint = threshold - 30;
                if (npc.ai[1] > stallPoint)
                {
                    //TrojanSquirrel squirrel = body.As<TrojanSquirrel>();
                    var sq = body.GetGlobalNPC<P_TrojanSquirrel>();
                    if (sq.arms != null && sq.arms.ai[0] != 0f) //wait if other part is attacking
                        npc.ai[1] = stallPoint;
                }

                if (npc.ai[1] > threshold && Math.Abs(body.velocity.Y) < 0.05f)
                {
                    npc.ai[0] = 1 + npc.ai[2];
                    npc.ai[1] = 0;
                    npc.ai[2] = npc.ai[2] == 0 ? 1 : 0;
                    npc.netUpdate = true;
                }
            }
        }
        private void AcornSpray(NPC npc)
        {
            float prog = npc.ai[1] / (Main.getGoodWorld ? 160f : 210f);
            if (npc.ai[3] >= 30)
            {
                npc.ai[3] = 0;
                Vector2 pos = npc.Center;
                pos.X += 22 * npc.direction; 
                pos.Y += 22;

                const float gravity = 0.2f;
                float time = 45f;
                Vector2 distance = Main.player[npc.target].Center - pos;
                distance.X += (distance.X > 0 ? 1 : -1) * MathHelper.SmoothStep(0, 50, prog);
                if (Math.Abs(distance.X) < 200)
                    distance.X = 200 * (distance.X > 0 ? 1 : -1);
                distance.X /= time;
                distance.Y = distance.Y / time - 0.5f * gravity * time;
                float max = MathHelper.SmoothStep(5f, 15f, prog);
                for (int i = 0; i < max; i++)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        SoundEngine.PlaySound(FargosSoundRegistry.TrojanCannon, pos);
                        float detal = MathHelper.SmoothStep(0.5f, 2.5f, prog);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), pos, distance + Main.rand.NextVector2Square(-detal, detal),
                            ModContent.ProjectileType<TrojanAcorn>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    }
                }
            }
            npc.ai[3] += MathHelper.SmoothStep(0.8f, 2f, prog);
            if (++npc.ai[1] > 210)
            {
                npc.ai[0] = 0;
                npc.ai[1] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        private void SquirrelBarrage(NPC npc)
        {
            if (npc.ai[1] == 90)
            {
                NPC arms = body.GetGlobalNPC<P_TrojanSquirrel>().arms;
                if (arms != null && arms.ai[0] != 2)
                {
                    arms.ai[0] = 2;
                    arms.ai[1] = 0;
                    arms.netUpdate = true;
                }
            }
            npc.ai[1]++;
            int start = 90;
            int end = 210;
            body.velocity.X *= 0.95f;
            if (npc.ai[1] % 4 == 0)
            {
                ShootSquirrelAt(npc, body.Center + Main.rand.NextVector2Circular(180, 180));

                if (npc.ai[1] > start)
                {
                    float ratio = (npc.ai[1] - start) / (end - start);
                    //if (ratio > 0.8f)
                        //ratio = 0.8f;
                    Vector2 target = new(npc.Center.X, Main.player[npc.target].Center.Y);
                    target.X += Math.Sign(npc.direction) * (550f + 1800f * (1f - ratio));

                    ShootSquirrelAt(npc, target);
                }
            }
            if (npc.ai[1] > end)
            {
                npc.ai[0] = 0;
                npc.ai[1] = 0;
                npc.netUpdate = true;
            }
        }
        private void Final_Wait(NPC npc)
        {
            Player player = Main.player[npc.target];
            Vector2 targetPos = player.Center;
            var P_Sq = body.GetGlobalNPC<P_TrojanSquirrel>();
            npc.direction = npc.spriteDirection = player.Center.X > npc.Center.X ? 1 : -1;
            if (P_Sq.arms.ai[0] == 4)
            {
                targetPos.X += /*player.velocity.X * 45f*/0;
                targetPos.Y -= 200;
                float muti = 0.5f;
                if (npc.Distance(player.Center) < 80)
                    muti = 0.8f;
                if (npc.Distance(targetPos) > 50)
                    Movement(npc, targetPos, muti, 24f);
                
            }
            else
            {
                targetPos.X += npc.Center.X < player.Center.X ? -200 : 200;
                targetPos.Y -= 200;
                if (npc.Distance(targetPos) > 50)
                    Movement(npc, targetPos, 0.25f, 32f);
            }
            int threshold = 60;
            int stallPoint = threshold - 10;
            if (npc.ai[1] > stallPoint)
            {
                var sq = body.GetGlobalNPC<P_TrojanSquirrel>();
                if (sq.arms != null && sq.arms.ai[0] != 3f) 
                    npc.ai[1] = stallPoint;
            }
            if (++npc.ai[1] > threshold && Math.Abs(body.velocity.Y) < 0.05f && body.localAI[0] <= 0)
            {
                npc.ai[0] = 4 + npc.ai[2];
                npc.ai[1] = 0;
                npc.ai[2] = npc.ai[2] == 0 ? 1 : 0;
                npc.netUpdate = true;
            }
        }
        private void Final_AcornSpray(NPC npc)
        {
            body.velocity.X *= 0.99f;
            Player player = Main.player[npc.target];
            Vector2 targetPos = player.Center;
            targetPos.X += npc.Center.X < player.Center.X ? -400 : 400;
            targetPos.Y -= 120;
            npc.direction = npc.spriteDirection = player.Center.X > npc.Center.X ? 1 : -1;
            if (npc.Distance(targetPos) > 50)
                Movement(npc, targetPos, 0.25f, 32f);
            float prog = npc.ai[1] / (Main.getGoodWorld ? 160f : 210f);
            if (npc.ai[3] >= 30)
            {
                npc.ai[3] = 0;
                Vector2 pos = npc.Center;
                pos.X += 22 * npc.direction;
                pos.Y -= 33;
                const float gravity = 0.2f;
                float time = 45f;
                Vector2 distance = Main.player[npc.target].Center - pos;
                distance.X += (distance.X > 0 ? 1 : -1) * MathHelper.SmoothStep(0, 50, prog);
                if (Math.Abs(distance.X) < 200)
                    distance.X = 200 * (distance.X > 0 ? 1 : -1);
                distance.X /= time;
                distance.Y = distance.Y / time - 0.5f * gravity * time;
                float max = MathHelper.SmoothStep(5f, 15f, prog);
                for (int i = 0; i < max; i++)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        SoundEngine.PlaySound(FargosSoundRegistry.TrojanCannon, pos);
                        float detal = MathHelper.SmoothStep(0.5f, 2.2f, prog);
                        Projectile.NewProjectile(npc.GetSource_FromThis(), pos, distance + Main.rand.NextVector2Square(-detal, detal),
                            ModContent.ProjectileType<TrojanAcorn>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                    }
                }
            }
            npc.ai[3] += MathHelper.SmoothStep(0.8f, 2f, prog);
            if (++npc.ai[1] > 180)
            {
                npc.ai[0] = 3;
                npc.ai[1] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        private void Final_SquirrelBarrage(NPC npc)
        {
            int start = 60;
            int end = 210;
            Player player = Main.player[npc.target];
            Vector2 targetPos = player.Center;
            targetPos.Y -= 400;
            if (npc.ai[1] < start / 2)
            {
                if (npc.Distance(targetPos) > 50)
                    Movement(npc, targetPos, 0.6f, 32f);
                npc.direction = npc.spriteDirection = player.Center.X > npc.Center.X ? 1 : -1;
            }
            else
                npc.velocity *= 0.95f;
            if (npc.ai[1] == start / 2 && FargoSoulsUtil.HostCheck)
            {
                int max = Main.rand.Next(6, 10);
                for(int i = 0; i < max; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + 50 * Main.rand.NextVector2Unit(), 5 * Main.rand.NextVector2Unit(), ModContent.ProjectileType<SqBomb>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0, Main.myPlayer,
                        end - start / 2 + i * 10 - max * 5, i * 30 - max * 15, ai2: npc.target);
                }
            }
            if (npc.ai[1] % 4 == 0)
            {
                //ShootSquirrelAt(npc, body.Center + Main.rand.NextVector2Circular(180, 180));
                if (npc.ai[1] > start)
                {
                    float ratio = (npc.ai[1] - start) / (end - start);
                    //if (ratio > 0.8f)
                    //ratio = 0.8f;
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 target = new Vector2(npc.Center.X, player.Center.Y) + i * (550f + 1800f * (1f - ratio)) * Vector2.UnitX;
                        ShootSquirrelAt(npc, target);
                    }
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 target = new Vector2(npc.Center.X, player.Center.Y) + i * (550f + 1200f * (1f - ratio)) * Vector2.UnitX;
                        ShootSquirrelAt(npc, target);
                    }
                }
            }
            if (++npc.ai[1] > end)
            {
                npc.ai[0] = 3;
                npc.ai[1] = 0;
                npc.netUpdate = true;
            }
        }
        private void ShootSquirrelAt(NPC npc, Vector2 target)
        {
            float gravity = 0.6f;
            const float origTime = 75;
            float time = origTime - 15;
            if (body.dontTakeDamage)
                time -= 15;
            gravity *= origTime / time;

            Vector2 distance = target - npc.Center;// + player.velocity * 30f;
            distance.X += Main.rand.NextFloat(-96, 96);
            distance.X /= time;
            distance.Y = distance.Y / time - 0.5f * gravity * time;

            distance.X += Math.Min(4f, Math.Abs(npc.velocity.X)) * Math.Sign(npc.velocity.X);

            SoundEngine.PlaySound(SoundID.Item1, npc.Center);

            if (FargoSoulsUtil.HostCheck)
            {
                float ai1 = time + Main.rand.Next(-10, 11) - 1;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, distance,
                    ModContent.ProjectileType<TrojanSquirrelProj>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, gravity, ai1);
            }
        }
        private static void Movement(NPC npc, Vector2 targetPos, float speedModifier, float cap = 12f)
        {
            if (npc.Center.X < targetPos.X)
            {
                npc.velocity.X += speedModifier;
                if (npc.velocity.X < 0)
                    npc.velocity.X += speedModifier * 2;
            }
            else
            {
                npc.velocity.X -= speedModifier;
                if (npc.velocity.X > 0)
                    npc.velocity.X -= speedModifier * 2;
            }
            if (npc.Center.Y < targetPos.Y)
            {
                npc.velocity.Y += speedModifier;
                if (npc.velocity.Y < 0)
                    npc.velocity.Y += speedModifier * 2;
            }
            else
            {
                npc.velocity.Y -= speedModifier;
                if (npc.velocity.Y > 0)
                    npc.velocity.Y -= speedModifier * 2;
            }
            if (Math.Abs(npc.velocity.X) > cap)
                npc.velocity.X = cap * Math.Sign(npc.velocity.X);
            if (Math.Abs(npc.velocity.Y) > cap)
                npc.velocity.Y = cap * Math.Sign(npc.velocity.Y);
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
