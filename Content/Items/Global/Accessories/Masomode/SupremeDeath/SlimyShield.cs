using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles.BossWeapons;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.SupremeDeath
{
    public class SlimyShieldOverride : PModeGlobalMasoItem<SlimyShield>
    {
        public override void SafeUpdateInPack(Item item, Player player)
        {
            player.AddEffect<PlatformFallthroughEffect>(item);
        }
    }
    public class SlimeBallOverride : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(projectile, target, hit, damageDone);
            if (projectile.type == ModContent.ProjectileType<SlimeBall>() && PModeWorldSavingSystem.PhantasmMode)
            {
                target.AddBuff(ModContent.BuffType<FlamesoftheUniverseBuff>(), 60);
            }
        }
    }
}
