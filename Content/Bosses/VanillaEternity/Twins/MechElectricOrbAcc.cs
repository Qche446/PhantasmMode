using FargowiltasSouls;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using Terraria;
using Terraria.ID;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    public class MechElectricOrbAcc : MechElectricOrb
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/MechElectricOrb";
        public override void AI()
        {
            base.AI();
            if (++Projectile.ai[1] < 75) //straight accel
                Projectile.velocity *= Main.getGoodWorld ? 1.07f : 1.06f;
            NPC npc = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            if (npc != null)
            {
                npc.TryGetGlobalNPC<P_Retinazer>(out P_Retinazer re);
                if (Projectile.Distance(npc.Center) > re.AuraRadius && Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
            }
        }
    }
    public class DarkStarAcc : DarkStar
    {
        public override void AI()
        {
            base.AI();
            if (++Projectile.ai[1] < 75) //straight accel
                Projectile.velocity *= Main.getGoodWorld ? 1.07f : 1.06f;
            NPC npc = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            if (npc != null)
            {
                npc.TryGetGlobalNPC<P_Retinazer>(out P_Retinazer re);
                if (Projectile.Distance(npc.Center) > re.AuraRadius && Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
            }
        }
    }
}
