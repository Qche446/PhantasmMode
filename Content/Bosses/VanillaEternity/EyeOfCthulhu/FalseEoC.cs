using FargowiltasSouls.Core;
using FargowiltasSouls;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using FargowiltasSouls.Core.Systems;
using System.IO;
using FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu;
using System.Threading;
using Luminance.Core.Graphics;
using FargowiltasSouls.Core.Globals;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    /// <summary>
    /// ai[0]待机时间,传入负的，ai[1]传入移动类型MoveType，ai[2]传入最大时间
    /// </summary>
    public class FalseEoC : ModProjectile
    {
        const string EoCName = "NPC_4";
        public override string Texture => $"FargowiltasSouls/Assets/ExtraTextures/Resprites/{EoCName}";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Sky Dragon's Fury");
            Main.projFrames[Projectile.type] = Main.npcFrameCount[NPCID.EyeofCthulhu];

            ProjectileID.Sets.TrailCacheLength[Type] = 15;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.aiStyle = -1;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60 * 60;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.FargoSouls().DeletionImmuneRank = 2;
            Projectile.hide = true;
            //Projectile.extraUpdates = 1;
        }

        public override bool? CanDamage() => false;
        public ref float Timer => ref Projectile.ai[0];
        public ref float moveType => ref Projectile.ai[1];
        public ref float maxTime => ref Projectile.ai[2];
        public enum MoveType
        {
            Straight,
            Brokenline,
            Triangle,
            Square,
            Hexagon,
            Octagonal,
            Arc,
            Round
        }

        public override void AI()
        {
            #region 杂项
            if (Projectile.frame < 2)
                Projectile.frame = 3;
            if (++Projectile.frameCounter > 4)
            {
                if (++Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 3;
            }
            int preTime = (int)Projectile.ai[0];
            if (Projectile.localAI[0] == 0 || Projectile.localAI[1] == 0)
            {
                Projectile.localAI[0] = Projectile.velocity.X;
                Projectile.localAI[1] = Projectile.velocity.Y;
                Projectile.velocity = Vector2.Zero;
            }
            
            if ((!Main.dayTime || Main.zenithWorld || Main.remixWorld))
            {
                
            }
            else //despawn and retarget
            {
                Projectile.Kill();
            }
            #endregion
            Timer++;
            
            if (Timer > maxTime)
                Projectile.Kill();
            
            switch ((MoveType)moveType)
            {
                case MoveType.Straight: StraightAI(); break;
                case MoveType.Brokenline: BrokenlineAI(); break;
                case MoveType.Triangle: TriangleAI(); break;
                case MoveType.Square: SquareAI(); break;
                case MoveType.Hexagon: HexagonAI(); break;
                case MoveType.Octagonal: OctagonalAI(); break;
                case MoveType.Round: RoundAI(); break;
                default: break;
            }
            if (Timer == 0)
            {
                Projectile.velocity = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
                Projectile.hide = false;
            }
            if (Timer > 0)
            {
                Projectile.alpha = 125;
                for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
                if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.eyeBoss, NPCID.EyeofCthulhu))
                {
                    NPC npc = Main.npc[EModeGlobalNPC.eyeBoss];
                    if (npc.GetGlobalNPC<P_EyeOfCthulhu>().DeathTimer >= 0)
                    {
                        Projectile.Kill();
                    }
                    if (Projectile.localAI[2] != 0)//可由克眼传参
                    {
                        int num = 3;
                        if ((MoveType)moveType == MoveType.Round)
                            num = 2;
                        Vector2 targetPos = new Vector2(npc.localAI[0], npc.localAI[1]);
                        Vector2 mainVel = 4f * Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2);
                        if ((MoveType)moveType == MoveType.Round)
                        {
                            Vector2 dir = Projectile.SafeDirectionTo(targetPos).RotatedBy(MathHelper.PiOver2);
                            mainVel = 4 * Projectile.SafeDirectionTo(targetPos + 100 * dir);
                        }
                        if (Timer % num == num - 2)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), Projectile.Center, mainVel, ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, ai1: npc.whoAmI, ai2: 1);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), Projectile.Center, mainVel.RotatedBy(MathHelper.Pi), ModContent.ProjectileType<MoonScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, ai1: npc.whoAmI, ai2: 1);
                        }
                    }
                }
                else
                    Projectile.Kill();
            }
            
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }
        private void StraightAI()
        {

        } 
        private void BrokenlineAI()
        {
            if (Timer % 15 == 0 && Timer > 0)
            {
                float angle = (Timer % 30 == 1 ? 1 : -1) * 150 * MathF.PI / 180f;
                Projectile.velocity = Projectile.velocity.RotatedBy(angle);
                Projectile.rotation += angle;
                Projectile.netUpdate = true;
            }
        }
        private void TriangleAI()
        {
            if (Timer % 15 == 0 && Timer > 0)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(2 * MathF.PI / 3f);
                Projectile.rotation += 2 * MathF.PI / 3f;
                Projectile.netUpdate = true;
            }
        }
        private void SquareAI()
        {
            if (Timer % 15 == 0 && Timer > 0)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(MathF.PI / 2f);
                Projectile.rotation += MathF.PI / 2f;
                Projectile.netUpdate = true;
            }
        }
        private void HexagonAI()
        {
            if (Timer % 15 == 0 && Timer > 0)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(2 * MathF.PI / 3f);
                Projectile.rotation += 2 * MathF.PI / 3f;
                Projectile.netUpdate = true;
            }
        }
        private void OctagonalAI()
        {
            if (Timer % 15 == 0 && Timer > 0)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(3 * MathF.PI / 4f);
                Projectile.rotation += 3 * MathF.PI / 4f;
                Projectile.netUpdate = true;
            }
        }
        private void RoundAI()
        {
            if (Timer > 0)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(80f / 600f);//80 / 800
                Projectile.rotation += 2f / 15f;
                if (Timer % 10 == 0)
                    Projectile.netUpdate = true;
            }
        }
        public override void SendExtraAI(BinaryWriter binaryWriter)
        {
            base.SendExtraAI(binaryWriter);
            binaryWriter.Write(Projectile.localAI[0]);
            binaryWriter.Write(Projectile.localAI[1]);
            binaryWriter.Write(Projectile.localAI[2]);
        }
        public override void ReceiveExtraAI(BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(binaryReader);
            Projectile.localAI[0] = binaryReader.ReadSingle();
            Projectile.localAI[1] = binaryReader.ReadSingle();
            Projectile.localAI[2] = binaryReader.ReadSingle();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
            Texture2D tex = TextureAssets.Npc[NPCID.EyeofCthulhu].Value;
            int sizeY = tex.Height / Main.projFrames[Type]; //ypos of lower right corner of sprite to draw
            int frameY = Projectile.frame * sizeY;
            Rectangle rectangle = new(0, frameY, tex.Width, sizeY);
            Vector2 origin = rectangle.Size() / 2f;
            SpriteEffects spriteEffects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Color baseColor = recolor ? Color.Cyan : Color.Red;
            Color color = baseColor with { A = 0 } * 0.13f;

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = color * 0.75f;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(tex, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27,
                    num165, origin, Projectile.scale, spriteEffects, 0);
            }

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(color),
                    Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            return false;
        }
    }
}
