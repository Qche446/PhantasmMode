using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Lump
{
    public class SkullCharmOverride : PModeGlobalMasoItem<SkullCharm>
    {

    }
    public class ShadowflamesFriendlyOverride : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(projectile, target, hit, damageDone);
            bool ph = PModeWorldSavingSystem.PhantasmMode;
            if (projectile.type == ModContent.ProjectileType<ShadowflamesFriendly>() && ph)
            {
                target.AddBuff(BuffID.ShadowFlame, 30);
            }
            if (ph && projectile.type == ModContent.ProjectileType<PhantasmalDeathrayPungent>())
            {
                target.AddBuff(BuffID.OnFire3, 180);
                target.AddBuff(BuffID.ShadowFlame, 180);
                target.AddBuff(ModContent.BuffType<SmiteBuff>(), 60);
            }
        }
    }
}
