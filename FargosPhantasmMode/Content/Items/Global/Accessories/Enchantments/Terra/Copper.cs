using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Terra
{
    public class Copper : PModeGlobalEnchant<CopperEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(CopperEffect.CopperProc, CopperProcEnhance);
        }
        private static void CopperProcEnhance(Action<Player, NPC> orig, Player player, NPC target)
        {
            if (!player.HasEffectEnchant<CopperEffect>())
                return;
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (modPlayer.CopperProcCD <= 0)
            {
                bool forceEffect = modPlayer.ForceEffect<CopperEnchant>();
                target.AddBuff(BuffID.Electrified, 180);

                int dmg = 60;
                int arcs = 5;
                int cdLength = 60 * 4;

                if (forceEffect)
                {
                    dmg = 250;
                    arcs = 8;
                }
                if (PModeChangeApply)
                    arcs += 1;
                int damage = FargoSoulsUtil.HighestDamageTypeScaling(modPlayer.Player, dmg);

                Projectile.NewProjectile(player.GetSource_EffectItem<CopperEffect>(), player.Center, player.DirectionTo(target.Center) * 20, ModContent.ProjectileType<CopperLightning>(),
                    damage, 0f, modPlayer.Player.whoAmI, player.DirectionTo(target.Center).ToRotation(), damage, ai2: arcs);

                modPlayer.CopperProcCD = cdLength;
            }
        }
    }
}
