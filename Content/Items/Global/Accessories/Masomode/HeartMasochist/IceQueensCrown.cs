using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist
{
    public class IceQueensCrownOverride : PModeGlobalMasoItem<IceQueensCrown>
    {

    }
    public class CirnoBombOverride : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.type == ModContent.ProjectileType<CirnoBomb>();
        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (PModeWorldSavingSystem.PhantasmMode)
            {
                if (projectile.ai[0] == 1)
                {
                    Player player = Main.player[projectile.owner];

                    int freezeRange = 16 * 150;
                    int HypothermiaDuration = 600;
                    if (player.FargoSouls().MasochistSoul || player.FargoSouls().MasochistHeart)
                    {
                        HypothermiaDuration *= 2;
                    }
                    //int slowDuration = freezeDuration + 180;

                    foreach (NPC n in Main.npc.Where(n => n.active && !n.friendly && n.damage > 0 && player.Distance(FargoSoulsUtil.ClosestPointInHitbox(n, player.Center)) < freezeRange && !n.dontTakeDamage && !n.buffImmune[ModContent.BuffType<HypothermiaBuff>()]))
                    {
                        n.AddBuff(ModContent.BuffType<HypothermiaBuff>(), HypothermiaDuration);
                    }
                }
            }
        }
    }
}
