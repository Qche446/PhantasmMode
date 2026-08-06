using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Timber
{
    public class PalmWood : PModeGlobalEnchant<PalmWoodEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(PalmwoodEffect.ActivatePalmwoodSentry, PalmwoodEffectFixed);
        }
        private static void PalmwoodEffectFixed(Action<Player> orig, Player player)
        {
            if (player.HasEffect<PalmwoodEffect>() && player.HasEffectEnchant<PalmwoodEffect>())
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    FargoSoulsPlayer modPlayer = player.FargoSouls();
                    bool forceEffect = modPlayer.ForceEffect<PalmWoodEnchant>();

                    Vector2 mouse = Main.MouseWorld;

                    int maxSpawn = player.maxTurrets;

                    List<Projectile> PalmTree = Main.projectile.Where(p => p.active && p.type == ModContent.ProjectileType<PalmTreeSentry>() && p.owner == player.whoAmI).ToList();
                    if (PalmTree.Count >= maxSpawn)
                    {
                        int time = 0;
                        int index = 0;
                        foreach (Projectile proj in PalmTree) 
                        {
                            if (proj.GetGlobalProjectile<TimberGlobalProj>().PalmTreeTimer > time)
                            {
                                index = PalmTree.IndexOf(proj);
                                time = proj.GetGlobalProjectile<TimberGlobalProj>().PalmTreeTimer;
                            }
                        }
                        PalmTree[index].Kill();
                    }

                    Vector2 offset = forceEffect ? (-40 * Vector2.UnitX) + (-120 * Vector2.UnitY) : (-41 * Vector2.UnitY);
                    FargoSoulsUtil.NewSummonProjectile(player.GetSource_EffectItem<PalmwoodEffect>(), mouse + offset, Vector2.Zero, ModContent.ProjectileType<PalmTreeSentry>(), forceEffect ? 95 : 14, 0f, player.whoAmI);
                }
            }
        }
    }
}
