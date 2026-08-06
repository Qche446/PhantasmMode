using Fargowiltas.Projectiles;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.Champions.Will;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Will
{
    public class Will : PModeGlobalEnchant<WillForce>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<WillJavelinEffect>(item);
        }
    }
    public class WillJavelinEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<WillHeader>();
        public override int ToggleItemType => ModContent.ItemType<WillForce>();
        public override bool ExtraAttackEffect => true;
        public override bool MutantsPresenceAffects => true;
        public override void PostUpdateEquips(Player player)
        {
            var wp = player.GetModPlayer<WillPlayer>();
            if (wp.WillJavelinCD <= 0)
            {
                foreach(NPC npc in Main.npc.Where(n => n.Distance(player.Center) < 1000 && n.active && !n.friendly && !n.townNPC && !n.dontTakeDamage))
                {
                    bool F = player.FargoSouls().GoldShell;
                    int max = F ? 12 : 6;
                    float offset = Main.rand.NextFloat((float)Math.PI * 2);
                    float omiga = MathF.Tau / 280;
                    int delay = F ? 15 : 30;
                    float damage = F ? 640 : 1000;
                    damage *= player.ActualClassDamage(DamageClass.Generic);
                    int type = ModContent.ProjectileType<WillJavelin3>();
                    for (int i = 0; i < max; i++)
                    {
                        float angle = offset + (float)Math.PI * 2 / max * i;
                        var p = Projectile.NewProjectileDirect(GetSource_EffectItem(player), npc.Center + 450 * Vector2.UnitX.RotatedBy(angle), Vector2.Zero,
                            type, (int)damage, 0f, Main.myPlayer, omiga, angle + (float)Math.PI, ai2: -delay);
                        p.hostile = false;
                        p.friendly = true;
                        p.CritChance = (int)(player.GetCritChance(DamageClass.Generic));
                    }
                    wp.WillJavelinCD = F ? 30 : 60;
                    break;
                }
            }
        }
    }
}
