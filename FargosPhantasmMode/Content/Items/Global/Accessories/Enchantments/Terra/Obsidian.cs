using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Terra
{
    public class Obsidian : PModeGlobalEnchant<ObsidianEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<ObsidianProcEffect>().OnHitNPCEither, OnHitNPCEitherFixed);
        }
        private static void OnHitNPCEitherFixed(Action<ObsidianProcEffect, Player, NPC, NPC.HitInfo, DamageClass, int, Projectile, Item> orig, ObsidianProcEffect self, Player player, NPC target, NPC.HitInfo hitInfo, DamageClass damageClass, int baseDamage, Projectile projectile, Item item)
        {
            if (!PModeChangeApply)
            {
                orig?.Invoke(self, player, target, hitInfo, damageClass, baseDamage, projectile, item);
                return;
            }
            if (!self.HasEffectEnchant(player))
                return;
            if (player.FargoSouls().ObsidianCD == 0)
            {
                float explosionDamage = baseDamage;
                FargoSoulsPlayer modPlayer = player.FargoSouls();
                bool force = player.ForceEffect<ObsidianProcEffect>();
                float softcapMult = force ? 4f : 1f;

                if (force) // this section is just imitating the previous version but cleaner
                {
                    explosionDamage *= 2f; // technically meant to result to 1.3f but we'll see
                }

                if (explosionDamage > 50f * softcapMult)
                    explosionDamage = ((100f * softcapMult) + explosionDamage) / 2.5f;

                Projectile.NewProjectile(self.GetSource_EffectItem(player), target.Center, Vector2.Zero, ModContent.ProjectileType<ObsidianExplosion>(), (int)explosionDamage, 0, player.whoAmI);

                modPlayer.ObsidianCD = 50;
            }
        }

    }
}
