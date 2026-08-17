using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Bionomic
{
    public class SandsofTimeOverride : PModeGlobalMasoItem<SandsofTime>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<FallingSandsEffect>(item);
        }
    }
    public class FallingSandsEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<BionomicHeader>();
        public override int ToggleItemType => ModContent.ItemType<SandsofTime>();
        public override bool ExtraAttackEffect => true;
        public int Timer = 0;
        public override void PostUpdateEquips(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                IEnumerable<NPC> targetnpc = Main.npc.Where(p => p.Alive() && !p.friendly && Main.GameUpdateCount % 30 == 0 && (p.Center - Main.LocalPlayer.Center).Length() <= 1200);
                if (targetnpc.Any() && Main.GameUpdateCount % 20 == 0)
                {
                    int num = 0;
                    foreach (NPC p in targetnpc)
                    {
                        for (int x = -3; x < 3; x += 2)
                        {
                            bool flag = false;
                            for (int y = 0; y < 50; y++)
                            {
                                Vector2 projPos = p.Center + Vector2.UnitX * x * 16 + Vector2.UnitY * -y * 16;
                                Point tile = projPos.ToTileCoordinates();
                                Point tileUnder = projPos.ToTileCoordinates() + (Vector2.UnitY * 1).ToPoint();
                                int damage = Main.hardMode ? 50 : 20;
                                if (player.FargoSouls().MasochistSoul) damage *= 20;
                                if (WorldGen.SolidTile(tile) && !WorldGen.SolidTile(tileUnder))
                                {
                                    if ((p.type == ModContent.NPCType<CursedCoffin>() || p.type == ModContent.NPCType<CursedSpirit>()) && Main.zenithWorld)
                                    {
                                        damage *= 10;
                                    }
                                    int proj = Projectile.NewProjectile(GetSource_EffectItem(player), projPos, Vector2.Zero,
                                        ModContent.ProjectileType<FallingSandstone>(), (int)(player.ActualClassDamage(DamageClass.Generic) * damage), 0f, Main.myPlayer, Main.rand.Next(40, 60));
                                    Main.projectile[proj].friendly = true;
                                    Main.projectile[proj].hostile = false;
                                    flag = true;
                                    num++;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                int damage = Main.hardMode ? 40 : 10;
                                if (player.FargoSouls().MasochistSoul) damage *= 40;
                                if ((p.type == ModContent.NPCType<CursedCoffin>() || p.type == ModContent.NPCType<CursedSpirit>()) && Main.zenithWorld)
                                {
                                    damage *= 15;
                                }
                                Vector2 projPos = p.Center + Vector2.UnitX * x * 16 + Vector2.UnitY * -40 * 16;
                                int proj = Projectile.NewProjectile(GetSource_EffectItem(player), projPos, Vector2.Zero,
                                    ModContent.ProjectileType<FallingSandstone>(), (int)(player.ActualClassDamage(DamageClass.Generic) * damage), 0f, Main.myPlayer, Main.rand.Next(40, 60));
                                Main.projectile[proj].friendly = true;
                                Main.projectile[proj].hostile = false;
                                num++;
                            }
                        }
                        if (num >= 9)
                            break;
                    }
                }
            }
        }
    }
}
