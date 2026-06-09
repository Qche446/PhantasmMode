using FargowiltasSouls;
using FargowiltasSouls.Content.Projectiles.Masomode;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using System.Reflection.Metadata;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class MutantWOFReticle : WOFReticle
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/WOFReticle";
        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
                Projectile.localAI[0] = Main.rand.NextBool() ? 1 : -1;

            if (++Projectile.ai[0] < 60)
            {
                Projectile.alpha -= 5;
                if (Projectile.alpha < 0) //fade in
                    Projectile.alpha = 0;

                int modifier = Math.Min(40, (int)Projectile.ai[0]);
                Projectile.scale = 4f - 3f / 40 * modifier; //start big, shrink down

                /*Projectile.Center = Main.npc[ai0].Center;
                Projectile.velocity = Main.player[Main.npc[ai0].target].Center - Projectile.Center;
                Projectile.velocity = Projectile.velocity / 60 * modifier; //move from npc to player*/
                Projectile.rotation = (float)Math.PI * 2f / 55 * modifier * Projectile.localAI[0];

                if (Projectile.ai[0] % 30 == 0)
                {
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/ReticleBeep"), Projectile.Center);
                }
            }
            else //if (Projectile.ai[0] < 145)
            {
                additive -= 7;
                if (additive < 0)
                    additive = 0;

                Projectile.alpha += 20;
                if (Projectile.alpha > 255) //fade out
                {
                    Projectile.alpha = 255;
                    Projectile.Kill();
                    return;
                }

                Projectile.scale = 4f - 3f * Projectile.Opacity; //scale back up

                //if (Projectile.ai[0] == 130 && FargoSoulsUtil.HostCheck) Projectile.NewProjectile(Projectile.InheritSource(Projectile), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, -1, -13);

                if (Projectile.ai[0] % 6 == 0 && Projectile.localAI[1]++ < 3)
                {
                    int ritualID = -1;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<PHMutantRitual>())
                        {
                            ritualID = i;
                            break;
                        }
                    }
                    Projectile ritual = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>());
                    Vector2 centerPoint = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>()) == null ? Projectile.Center : ritual.Center;
                    float angle = MathHelper.ToRadians(Main.rand.Next(-15, 16) + Projectile.ai[1]);
                    Vector2 basevel = Vector2.UnitY.RotatedBy(angle) * 20f;//base长度20
                    Vector2 spawnPos = Projectile.Center;
                    while ((spawnPos - centerPoint).Length() < 1200)
                        spawnPos += basevel;

                    Vector2 vel = Main.rand.NextFloat(0.8f, 1.2f) * (Projectile.Center - spawnPos) / 90;
                    if (vel.Length() < 10f)
                        vel = Vector2.Normalize(vel) * Main.rand.NextFloat(10f, 15f);
                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), spawnPos, vel, ModContent.ProjectileType<MutantWOFChain>(), Projectile.damage, 0f, Main.myPlayer);

                    FargoSoulsUtil.ScreenshakeRumble(4);

                    SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.5f }, Projectile.Center);

                    Projectile.localAI[0] *= -1;
                }
            }
        }
    }
    public class MutantWOFChain : WOFChain
    {
        bool startcheck = false;
        public override string Texture => "Terraria/Images/NPC_115";
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.aiStyle = -1;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1800;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            //CooldownSlot = 1;

            Projectile.extraUpdates = 2;
        }
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.1f, 0.5f, 0.7f);

            if (Projectile.timeLeft <= 30 || Projectile.ai[2] == 1)
            {
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 0f, 0.05f);
                if (Projectile.Opacity < 0.1f)
                {
                    Projectile.Kill();
                    return;
                }
            }

            if (Projectile.ai[0] == 0)
            {
                Projectile.ai[0] = 1;
                Projectile.localAI[0] = Projectile.Center.X;
                Projectile.localAI[1] = Projectile.Center.Y;
                //Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value = Main.npcTexture[NPCID.TheHungry];
            }

            if (Projectile.velocity != Vector2.Zero && Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemSapphire, Projectile.velocity.X * 0.4f, Projectile.velocity.Y * 0.4f, 114, default, 2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 1.8f;
                Main.dust[dust].velocity.Y -= 0.5f;
            }

            int ritualID = -1;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<PHMutantRitual>())
                {
                    ritualID = i;
                    break;
                }
            }
            Projectile ritual = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>());
            Vector2 centerPoint = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>()) == null ? Projectile.Center : ritual.Center;

            if (!startcheck && (Projectile.Center - centerPoint).Length() < 1200)
                startcheck = true;
            if (startcheck && (Projectile.Center - centerPoint).Length() > 1200)
            {
                Projectile.position -= Projectile.velocity * 2f;
                Projectile.velocity = Vector2.Zero;
            }
            //stop moving at vertical limits of underworld


            if (BittenPlayer != -1)
            {

                Player victim = Main.player[BittenPlayer];
                if (victim.active && !victim.ghost && !victim.dead
                    && (Projectile.Distance(victim.Center) < 160 || victim.whoAmI != Main.myPlayer)
                    && victim.FargoSouls().MashCounter < 20)
                {
                    victim.AddBuff(ModContent.BuffType<GrabbedBuff>(), 2);
                    victim.velocity = Vector2.Zero;
                    Projectile.Center = victim.Center;
                }
                else
                {
                    BittenPlayer = -1;
                    Projectile.netUpdate = true;
                }
            }
            NPC npc = Main.npc[0];
            for (int i = 0; i <= Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<MutantBoss>())
                {
                    npc = Main.npc[i];
                }
            }
            if (npc.ai[0] != 50)
            {
                Projectile.Kill();
            }
            if (Projectile.velocity != Vector2.Zero)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();

                if (++Projectile.frameCounter > 6 * (Projectile.extraUpdates + 1))
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame >= Main.projFrames[Projectile.type])
                        Projectile.frame = 0;
                }
            }
        }
    }
}
