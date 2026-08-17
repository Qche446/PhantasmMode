using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Content.Buffs.Global;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static FargowiltasSouls.Content.Items.Accessories.Forces.TimberForce;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Timber
{
    public class TimberGlobalProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public int PalmTreeTimer = 0;
        public override GlobalProjectile NewInstance(Projectile target) => PModeWorldSavingSystem.PhantasmMode ? NewInstance(target) : null;
        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            Player player = Main.player[proj.owner];
            if (proj.type == ModContent.ProjectileType<SuperBlood>() && player.HasEffect<ShadewoodEffect>())
            {
                proj.localNPCHitCooldown = 20;
            }
        }
        public override void AI(Projectile proj)
        {
            Player player = Main.player[proj.owner];
            var modPlayer = player.FargoSouls();
            if (proj.type == ModContent.ProjectileType<EbonwoodAuraProj>() && player.HasEffect<EbonwoodEffect>())
            {
                bool f = player.ForceEffect<EbonwoodEffect>();
                int dist = ShadewoodEffect.Range(player, f);
                List<int> ignore = [BuffID.Tipsy, BuffID.Sunflower, BuffID.Campfire, BuffID.PotionSickness, BuffID.ManaSickness, BuffID.WaterCandle, ModContent.BuffType<FlawlessBuff>()];
                IEnumerable bufftype = player.buffType.Where(t => Main.debuff[t] && !ignore.Contains(t));
                foreach (NPC npc in Main.npc.Where(n => n.active && !n.friendly && n.lifeMax > 10 && !n.dontTakeDamage && (n.damage > 0 || n.defDamage > 0)))
                {
                    Vector2 npcComparePoint = FargoSoulsUtil.ClosestPointInHitbox(npc, player.Center);
                    if (player.Distance(npcComparePoint) < dist && Collision.CanHitLine(player.Center, 0, 0, npcComparePoint, 0, 0))
                    {
                        foreach (int buff in bufftype)
                        {
                            npc.AddBuff(buff, f ? 720 : 360);
                            if (buff == ModContent.BuffType<HallowFlameBuff>())
                                npc.GetGlobalNPC<PModeGlobalBuffNPC>().HallowFlameLevel = player.GetModPlayer<PModeBuffPlayer>().HallowFlameLevel;
                        }
                    }
                }
            }
            if (proj.type == ModContent.ProjectileType<PalmTreeSentry>() && player.HasEffect<PalmwoodEffect>())
            {
                PalmTreeTimer++;
            }
        }
        public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[proj.owner];
            var modPlayer = player.FargoSouls();
            if (proj.type == ProjectileID.SnowBallFriendly && player.HasEffect<BorealEffect>())
            {
                if (player.HasEffect<TimberEffect>())
                    modPlayer.BorealCD -= 9;
                else
                    modPlayer.BorealCD -= modPlayer.ForceEffect(ModContent.ItemType<BorealWoodEnchant>()) ? 2 : 10;

            }
        }
    }
}
