using FargosPhantasmMode.Content.Buffs.Global;
using FargosPhantasmMode.Content.Items;
using FargosPhantasmMode.Content.Items.Global;
using FargowiltasSouls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles
{
    public class PModeGlobalProj : GlobalProjectile
    {
        public bool PoisonAttribute;
        public bool FireAttribute;
        public bool IceAttribute;
        public bool OrdinaryAttributes;
        public int GrazeCD = 0;
        public override bool InstancePerEntity => true;
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            var Pbn = target.GetGlobalNPC<PModeGlobalBuffNPC>();
            List<bool> Attributes = [PoisonAttribute, FireAttribute, IceAttribute];
            List<float> Multiplier = [Pbn.PosionMultiplier, Pbn.FireMultiplier, Pbn.IceMultiplier];
            float result = 1f;
            for (int i = 0; i < Attributes.Count; i++)
            {
                if ((OrdinaryAttributes || Attributes[i]) && Multiplier[i] > result)
                    result = Multiplier[i];
            }
            modifiers.FinalDamage *= result;
        }
        public override void PostAI(Projectile projectile)
        {
            if (projectile.hostile && projectile.damage > 0 && projectile.aiStyle != ProjAIStyleID.FallingTile && --GrazeCD < 0)
            {
                GrazeCD = 6; //don't check per tick ech
                Player py = Main.LocalPlayer;
                if (py.active && !py.dead)
                {
                    if (py.FargoSouls().Graze && !py.immune && py.hurtCooldowns[0] <= 0 && py.hurtCooldowns[1] <= 0)
                    {
                        var fproj = projectile.FargoSouls();
                        if (ProjectileLoader.CanDamage(projectile) != false && ProjectileLoader.CanHitPlayer(projectile, py) && fproj.GrazeCheck(projectile))
                        {
                            GrazeCD = 30 * projectile.MaxUpdates;
                            ShadowveilHeart.OnGraze(py);

                        }
                    }
                }
            }
        }
    }
}
