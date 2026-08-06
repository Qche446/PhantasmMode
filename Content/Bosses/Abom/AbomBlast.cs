using FargowiltasSouls.Content.Projectiles.BossWeapons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Abom
{
    public class AbomBlast : PhantasmalBlast
    {
        public override string Texture => "FargowiltasSouls/Content/Bosses/AbomBoss/AbomBlast";

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 300);
            target.AddBuff(ModContent.BuffType<FargowiltasSouls.Content.Buffs.Masomode.MutantNibbleBuff>(), 300);
        }
    }
}

