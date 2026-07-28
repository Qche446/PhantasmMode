using FargosPhantasmMode.Content.Buffs.Global;
using FargosPhantasmMode.Content.Items.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles
{
    public class PModeGlobalProj : GlobalProjectile
    {
        public bool PoisonAttribute;
        public bool FireAttribute;
        public bool IceAttribute;
        public bool OrdinaryAttributes;
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
    }
}
