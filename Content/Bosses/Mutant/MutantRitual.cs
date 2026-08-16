using FargowiltasSouls.Content.Bosses.MutantBoss;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Terraria;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantRitual : MutantRitual
    {
        protected override void Movement(NPC npc)
        {
            //int[] unmovingArenaStates =
            //{
            //    19, //pillars
            //    13, 14, 15, //predictive dash
            //    21, 22, 23 //direct dash
            //};

            float targetRotation;
            //if (unmovingArenaStates.Contains((int)npc.ai[0])) //be stationary
            if (npc.ai[0] == 19 || npc.ai[0] == 50 || npc.ai[0] == 28) //pillars 和 血肉强
            {
                Projectile.velocity = Vector2.Zero;

                targetRotation = -realRotation / 2; //denote arena isn't moving
            }
            else if (npc.ai[0] == 49) //golem
            {
                if (npc.HasValidTarget && npc.ai[1] < 30) //snap it to player at start
                {
                    Projectile.velocity = (Main.player[npc.target].Center - Projectile.Center) / 10f;

                    targetRotation = realRotation;
                }
                else
                {
                    Projectile.velocity = Vector2.Zero;

                    targetRotation = -realRotation / 2; //denote arena isn't moving
                }
            }
            else
            {
                Projectile.velocity = npc.Center - Projectile.Center;
                if (npc.ai[0] == 36)
                    Projectile.velocity /= 20f; //much faster for slime rain
                else if (npc.ai[0] == 22 || npc.ai[0] == 23 || npc.ai[0] == 25)
                    Projectile.velocity /= 40f; //move faster for direct dash, predictive throw
                else
                    Projectile.velocity /= 60f;

                targetRotation = realRotation;
            }

            const float increment = realRotation / 40;
            if (rotationPerTick < targetRotation)
            {
                rotationPerTick += increment;
                if (rotationPerTick > targetRotation)
                    rotationPerTick = targetRotation;
            }
            else if (rotationPerTick > targetRotation)
            {
                rotationPerTick -= increment;
                if (rotationPerTick < targetRotation)
                    rotationPerTick = targetRotation;
            }

            MutantDead = npc.ai[0] <= -6;
        }
    }
}
