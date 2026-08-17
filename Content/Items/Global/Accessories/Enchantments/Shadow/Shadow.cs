using FargosPhantasmMode.Common;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class Shadow : PModeGlobalEnchant<ShadowEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<ShadowBalls>().PostUpdateEquips, ShadowBallFixed);
            PhanUtil.AddILHooks(ModContent.GetInstance<FargoSoulsGlobalProjectile>().PreAI, ShadowAdjustForce);
        }
        private static void ShadowBallFixed(Action<ShadowBalls, Player> prig, ShadowBalls self, Player player)
        {
            if (!self.HasEffectEnchant(player) && !PModeWorldSavingSystem.PhantasmMode)
                return;
            if (PModeChangeApply && player.FargoSouls().TerrariaSoul)
                return;
            if (player.whoAmI == Main.myPlayer)
            {
                FargoSoulsPlayer modPlayer = player.FargoSouls();
                int currentOrbs = player.ownedProjectileCounts[ModContent.ProjectileType<ShadowEnchantOrb>()];

                int max = 2;
                bool ancientShadow = modPlayer.AncientShadowEnchantActive;
                bool forceEffect = modPlayer.ForceEffect<ShadowEnchant>();

                if (modPlayer.TerrariaSoul || player.HasEffect<ShadowForceEffect>())
                {
                    max = 5;
                }
                else if (forceEffect && ancientShadow) //ancient shadow force
                {
                    max = 4;
                }
                else if (ancientShadow || (forceEffect)) //ancient shadow or normal shadow force
                {
                    max = 3;
                }

                //spawn for first time
                if (currentOrbs == 0)
                {
                    float rotation = 2f * (float)Math.PI / max;

                    for (int i = 0; i < max; i++)
                    {
                        Vector2 spawnPos = player.Center + new Vector2(60, 0f).RotatedBy(rotation * i);
                        int p = Projectile.NewProjectile(player.GetSource_Misc(""), spawnPos, Vector2.Zero, ModContent.ProjectileType<ShadowEnchantOrb>(), 0, 10f, player.whoAmI, 0, rotation * i);
                        Main.projectile[p].FargoSouls().CanSplit = false;
                    }
                }
                //equipped somwthing that allows for more or less, respawn, only once every 10 seconds to prevent exploit
                else if ((currentOrbs < max && modPlayer.ShadowOrbRespawnTimer <= 0) || currentOrbs > max)
                {
                    modPlayer.ShadowOrbRespawnTimer = 60 * 10;

                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile proj = Main.projectile[i];

                        if (proj.active && proj.type == ModContent.ProjectileType<ShadowEnchantOrb>() && proj.owner == player.whoAmI)
                        {
                            proj.Kill();
                        }
                    }

                    float rotation = 2f * (float)Math.PI / max;

                    for (int i = 0; i < max; i++)
                    {
                        Vector2 spawnPos = player.Center + new Vector2(60, 0f).RotatedBy(rotation * i);
                        int p = Projectile.NewProjectile(self.GetSource_EffectItem(player), spawnPos, Vector2.Zero, ModContent.ProjectileType<ShadowEnchantOrb>(), 0, 10f, player.whoAmI, 0, rotation * i);
                        Main.projectile[p].FargoSouls().CanSplit = false;
                    }
                }
                modPlayer.ShadowOrbRespawnTimer--;
            }
        }
        private static void ShadowAdjustForce(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(50)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() => PModeChangeApply && Main.LocalPlayer.HasEffect<ShadowForceEffect>() ? 160 : 50);
        }
    }
}
