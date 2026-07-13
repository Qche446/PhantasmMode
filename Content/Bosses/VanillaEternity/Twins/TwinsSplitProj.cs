using FargowiltasSouls;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// ai[0]=生存时间
    /// ai[1]=分裂次数
    /// </summary>
    public class MechElectricOrbSplit : MechElectricOrb
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/MechElectricOrb";
        public override void AI()
        {
           //Main.NewText(1);
            base.AI();
            if (++Projectile.localAI[0] >= Projectile.ai[0] && Projectile.ai[1] >= 0)
            {
                Projectile.ai[1]--;
                for (int i = -1; i <= 1; i += 2)
                {
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedBy(i * MathF.PI / 3f),
                        ModContent.ProjectileType<MechElectricOrbSplit>(), Projectile.damage, 0, Main.myPlayer, Projectile.ai[0], Projectile.ai[1], Projectile.ai[2]);
                    if (Main.projectile[p].timeLeft > 300)
                        Main.projectile[p].timeLeft = 300;
                }
                Projectile.Kill();
            }
            NPC npc = FargoSoulsUtil.NPCExists(EModeGlobalNPC.retiBoss, NPCID.Retinazer);
            if (npc != null)
            {
                npc.TryGetGlobalNPC<P_Retinazer>(out P_Retinazer re);
                if (Projectile.Distance(npc.Center) > re.AuraRadius && Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
            }
        }
    }
    /// <summary>
    /// ai[0]=生存时间
    /// ai[1]=分裂次数
    /// </summary>
    public class DarkStarSplit: DarkStar
    {
        public override void AI()
        {
            //Main.NewText(2);
            base.AI();
            if (++Projectile.localAI[2] >= Projectile.ai[0] && Projectile.ai[1] >= 0)
            {
                Projectile.ai[1]--;
                for (int i = -1; i <= 1; i += 2)
                {
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedBy(i * MathF.PI / 3f),
                        ModContent.ProjectileType<DarkStarSplit>(), Projectile.damage, 0, Main.myPlayer, Projectile.ai[0], Projectile.ai[1]);
                    if (Main.projectile[p].timeLeft > 300)
                        Main.projectile[p].timeLeft = 300;
                }
                Projectile.Kill();
            }
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
