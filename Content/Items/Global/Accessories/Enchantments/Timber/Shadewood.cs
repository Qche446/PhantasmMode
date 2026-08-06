using FargosPhantasmMode.Common;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using System;
using Terraria;
using static FargowiltasSouls.Content.Items.Accessories.Forces.TimberForce;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Timber
{
    public class Shadewood : PModeGlobalEnchant<ShadewoodEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ShadewoodEffect.ShadewoodProc, ShadewoodProcEnhance);
        }
        private static void ShadewoodProcEnhance(Action<Player, NPC, Projectile> orig, Player player, NPC target, Projectile projectile)
        {
            if (PModeChangeApply)
            {
                FargoSoulsPlayer modPlayer = player.FargoSouls();
                bool forceEffect = modPlayer.ForceEffect<ShadewoodEnchant>();
                int dmg = 16;

                if (forceEffect)
                    dmg *= 3;
                if (player.HasEffect<TimberEffect>())
                    dmg *= 2;
                if (target.HasBuff(ModContent.BuffType<SuperBleedBuff>()) && modPlayer.ShadewoodCD == 0 && (projectile == null || projectile.type != ModContent.ProjectileType<SuperBlood>()) && player.whoAmI == Main.myPlayer)
                {
                    modPlayer.ShadewoodCD = 12;
                    int max = 2;
                    for (int i = 0; i < max; i++)
                    {
                        Projectile.NewProjectile(player.GetSource_EffectItem<ShadewoodEffect>(), target.Center.X, target.Center.Y - 20, 0f + Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5), ModContent.ProjectileType<SuperBlood>(), (int)(dmg * player.ActualClassDamage(DamageClass.Melee)), 0f, Main.myPlayer);
                    }

                    if (forceEffect)
                    {
                        target.AddBuff(BuffID.Ichor, 120);
                    }
                }
            }
            else
                orig.Invoke(player, target, projectile);
        }
    }
}
