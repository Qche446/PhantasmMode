using FargosPhantasmMode.Content.Projectiles;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Pure
{
    public class GuttedHeartOverride : PModeGlobalMasoItem<GuttedHeart>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<GuttedHeartAura>(item);
        }
    }
    public class GuttedHeartAura : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<PureHeartHeader>();
        public override int ToggleItemType => ModContent.ItemType<GuttedHeart>();
        public override bool ExtraAttackEffect => true;
        public float Timer = 0;
        bool flag = true;
        public override void PostUpdateEquips(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                bool pure = player.FargoSouls().PureHeart;
                int visualProj = ModContent.ProjectileType<GuttedHeartAuraProj>();

                if (Timer >= 60 && flag)
                {
                    Projectile.NewProjectile(GetSource_EffectItem(player), player.Center, Vector2.Zero, visualProj, 1, 0, Main.myPlayer, ai2: pure ? 16 : 12);
                    flag = false;
                }
                if (!pure)
                    Lighting.AddLight((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f), 0.65f, 0.4f, 0.1f);
                if (++Timer >= (player.FargoSouls().PureHeart ? 180 : 240))
                {
                    flag = true;
                    Timer = 0;
                }
            }
        }

    }
}
