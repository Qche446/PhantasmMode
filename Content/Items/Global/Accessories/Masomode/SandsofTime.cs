using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls;
using System.Linq;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class SandsofTimeOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
           => entity.type == ModContent.ItemType<SandsofTime>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.SandsofTime"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
                player.AddEffect<FallingSandsEffect>(item);
            base.UpdateAccessory(item, player, hideVisual);
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
                                        damage *= 20;
                                    }
                                    int proj = Projectile.NewProjectile(GetSource_EffectItem(player), projPos, Vector2.Zero,
                                        ModContent.ProjectileType<FallingSandstone>(), damage, 0f, Main.myPlayer, Main.rand.Next(40, 60));
                                    Main.projectile[proj].friendly = true;
                                    Main.projectile[proj].hostile = false;
                                    flag = true;
                                    break;
                                } 
                            }
                            if (!flag)
                            {
                                int damage = Main.hardMode ? 50 : 20;
                                if (player.FargoSouls().MasochistSoul) damage *= 20;
                                if ((p.type == ModContent.NPCType<CursedCoffin>() || p.type == ModContent.NPCType<CursedSpirit>()) && Main.zenithWorld)
                                {
                                    damage *= 10;
                                }
                                Vector2 projPos = p.Center + Vector2.UnitX * x * 16 + Vector2.UnitY * -40 * 16;
                                int proj = Projectile.NewProjectile(GetSource_EffectItem(player), projPos, Vector2.Zero,
                                    ModContent.ProjectileType<FallingSandstone>(), damage, 0f, Main.myPlayer, Main.rand.Next(40, 60));
                                Main.projectile[proj].friendly = true;
                                Main.projectile[proj].hostile = false;
                            }
                        }
                    }
                }
            }
        }
    }
}
