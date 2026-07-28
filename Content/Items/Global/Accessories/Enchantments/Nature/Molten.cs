using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature
{
    public class Molten : PModeGlobalEnchant<MoltenEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<MoltenBombEffect>(item);
        }
    }
    public class MoltenBombEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<NatureHeader>();
        public override int ToggleItemType => ModContent.ItemType<MoltenEnchant>();
        public override bool ExtraAttackEffect => true;
        public override void ModifyHitByNPC(Player player, NPC npc, ref Player.HurtModifiers modifiers)
            => MoltenBomb(player, npc, ref modifiers);
        public override void ModifyHitByProjectile(Player player, Projectile projectile, ref Player.HurtModifiers modifiers)
            => MoltenBomb(player, projectile, ref modifiers);

        public static void MoltenBomb(Player player, Entity entity, ref Player.HurtModifiers modifiers)
        {
            bool HasForce = player.ForceEffect<MoltenBombEffect>();
            int damage = HasForce ? 160 : 50;
            damage *= player.HasEffect<NatureEffect>() ? 3 : 1;
            //Vector2 vel = entity.SafeDirectionTo(player.Center);
            player.noKnockback = false;
            modifiers.Knockback *= 5;
            //player.velocity += 2 * Vector2.UnitX;
            var proj = Projectile.NewProjectileDirect(player.GetSource_EffectItem<MoltenBombEffect>(), player.Center + new Vector2(-75, -70), Vector2.Zero, ProjectileID.Volcano, damage, 2, Main.myPlayer);
            proj.usesLocalNPCImmunity = true;
            proj.localNPCHitCooldown = 5;
            proj.scale *= HasForce ? 9 : 6;
            proj.width *= HasForce ? 3 : 2;
            proj.height *= HasForce ? 3 : 2;
        }
    }
}
