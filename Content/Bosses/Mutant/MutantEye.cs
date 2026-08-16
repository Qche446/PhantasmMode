using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantEye : MutantEye, IProjOwnedByBoss<MutantBoss>
    {
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 0;
            CooldownSlot = 1;
            //dont let others inherit this behaviour
            DieOutsideArena = Projectile.type == ModContent.ProjectileType<PHMutantEye>();
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + (float)Math.PI / 2;

            if (Projectile.localAI[0] < ProjectileID.Sets.TrailCacheLength[Projectile.type])
            {
                Projectile.localAI[0] += 0.1f;
            }
            else
                Projectile.localAI[0] = ProjectileID.Sets.TrailCacheLength[Projectile.type];

            Projectile.localAI[1] += 0.25f;

            if (DieOutsideArena)
            {
                if (ritualID == -1) //identify the ritual CLIENT SIDE
                {
                    ritualID = -2; //if cant find it, give up and dont try every tick

                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<PHMutantRitual>())
                        {
                            ritualID = i;
                            break;
                        }
                    }
                }

                Projectile ritual = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<PHMutantRitual>());
                if (ritual != null && Projectile.Distance(ritual.Center) > 1200f) //despawn faster
                    Projectile.timeLeft = 0;
            }
        }
    }
    public class PHMutantEyeHoming : PHMutantEye, IProjOwnedByBoss<MutantBoss>
    {
        public override string Texture => FargoSoulsUtil.AprilFools ?
            "FargowiltasSouls/Content/Bosses/MutantBoss/MutantEye_April" :
            "Terraria/Images/Projectile_452";

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = 900;
        }

        public override void AI()
        {
            const int endHomingTime = -600;

            float maxSpeed = WorldSavingSystem.MasochistModeReal ? 15f : 10f;

            bool stopAttacking = false;

            NPC npc = FargoSoulsUtil.NPCExists(EModeGlobalNPC.mutantBoss, ModContent.NPCType<MutantBoss>());
            int[] spearSpinAIs = [4, 5, 6, 13, 14, 15, 21, 22, 23];
            if ((npc == null || !spearSpinAIs.Contains((int)npc.ai[0]))
                && !(WorldSavingSystem.MasochistModeReal && npc.ai[0] > 10)
                && !Main.getGoodWorld)
            {
                Projectile.ai[1] = endHomingTime; //for deceleration
                stopAttacking = true;
            }

            Projectile.ai[1]--;

            Player p = FargoSoulsUtil.PlayerExists(npc == null ? Projectile.ai[0] : npc.target);
            if (stopAttacking || Projectile.ai[1] > 0 && p != null && Projectile.Distance(p.Center) < 240)
            {
                if (p != null)
                {
                    double angle = Projectile.DirectionFrom(p.Center).ToRotation() - Projectile.velocity.ToRotation();
                    if (angle > Math.PI)
                        angle -= 2.0 * Math.PI;
                    if (angle < -Math.PI)
                        angle += 2.0 * Math.PI;

                    Projectile.velocity = Projectile.velocity.RotatedBy(angle * 0.05);
                }

                if (Projectile.timeLeft > 180)
                    Projectile.timeLeft = 180;
            }
            else if (Projectile.ai[1] < 0 && Projectile.ai[1] > endHomingTime)
            {
                if (p != null)
                {
                    float homingMaxSpeed = maxSpeed;
                    if (npc != null && (npc.ai[0] == 21 || npc.ai[0] == 22 || npc.ai[0] == 23))
                        homingMaxSpeed *= 2f;
                    if (Projectile.velocity.Length() < homingMaxSpeed)
                        Projectile.velocity *= 1.02f;

                    Vector2 target = p.Center;
                    float deactivateHomingRange = WorldSavingSystem.MasochistModeReal ? 360 : 480;
                    if (Projectile.Distance(target) > deactivateHomingRange)
                    {
                        Vector2 distance = target - Projectile.Center;

                        double angle = distance.ToRotation() - Projectile.velocity.ToRotation();
                        if (angle > Math.PI)
                            angle -= 2.0 * Math.PI;
                        if (angle < -Math.PI)
                            angle += 2.0 * Math.PI;

                        Projectile.velocity = Projectile.velocity.RotatedBy(angle * 0.1);
                    }
                    else
                    {
                        Projectile.ai[1] = endHomingTime;
                    }
                }
            }

            if (Projectile.ai[1] < endHomingTime && !Main.getGoodWorld)
            {
                if (Projectile.velocity.Length() > maxSpeed)
                    Projectile.velocity *= 0.96f;
            }

            base.AI();
        }
    }
    public class PHMutantEyeWavy : PHMutantEye, IProjOwnedByBoss<MutantBoss>
    {
        public override string Texture => FargoSoulsUtil.AprilFools ?
            "FargowiltasSouls/Content/Bosses/MutantBoss/MutantEye_April" :
            "Terraria/Images/Projectile_452";

        public override int TrailAdditive => 150;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = 180;
            Projectile.FargoSouls().TimeFreezeImmune = true;
            CooldownSlot = 0;
        }

        private float Amplitude => Projectile.ai[0];
        private float Period => Projectile.ai[1];
        private float Counter => Projectile.localAI[1] * 4;

        public float oldRot;

        public override void AI()
        {
            NPC mutant = FargoSoulsUtil.NPCExists(EModeGlobalNPC.mutantBoss);
            if (mutant != null && (mutant.ai[0] == -5f || mutant.ai[0] == -7f))
            {
                float targetRotation = mutant.ai[3];

                float speed = Projectile.velocity.Length();
                float rotation = targetRotation + (float)Math.PI / 4 * (float)Math.Sin(2 * (float)Math.PI * Counter / Period) * Amplitude;
                Projectile.velocity = speed * rotation.ToRotationVector2();

                if (oldRot != 0)
                {
                    Vector2 oldCenter = Projectile.Center;
                    Projectile.Center = mutant.Center + (Projectile.Center - mutant.Center).RotatedBy(targetRotation - oldRot);

                    Vector2 diff = Projectile.Center - oldCenter;
                    for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
                    {
                        Projectile.oldPos[i] += diff;
                    }
                }

                oldRot = targetRotation;
            }
            else
            {
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0] += 0.1f;

            base.AI();
        }

        public override void OnKill(int timeleft)
        {
            //prevents base dust from forming
        }
    }
}
