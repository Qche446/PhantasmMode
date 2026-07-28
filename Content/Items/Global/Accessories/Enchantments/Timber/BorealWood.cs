using FargosPhantasmMode.Common;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static FargowiltasSouls.Content.Items.Accessories.Forces.TimberForce;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Timber
{
    public class BorealWood : PModeGlobalEnchant<BorealWoodEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (!player.HasEffectEnchant<BorealEffect>())
                return;
            var modplayer = player.FargoSouls();
            Texture2D tex = TextureAssets.Item[ModContent.ItemType<BorealWoodEnchant>()].Value;
            if (player.whoAmI == Main.myPlayer)
                CooldownBarManager.Activate("BorealCD", tex, new(182, 139, 38),
                () => 1 - (float)modplayer.BorealCD / (player.ForceEffect<BorealEffect>() ? 30f: 60f), activeFunction: player.HasEffectEnchant<BorealEffect>, displayAtFull: true);
        }
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<BorealEffect>().BorealSnowballs, BorealSnowballsFixed);
        }
        private static void BorealSnowballsFixed(Action<BorealEffect, Player, int> orig, BorealEffect self, Player player, int baseDamage)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (modPlayer.BorealCD <= 0 && player.whoAmI == Main.myPlayer)
            {
                Item item = self.EffectItem(player);
                bool forceEffect = modPlayer.ForceEffect(item.type);
                Item heldItem = player.HeldItem;
                modPlayer.BorealCD = forceEffect ? 30 : 60;
                if (player.HasEffect<TimberEffect>())
                    modPlayer.BorealCD = 90;

                Vector2 vel = Vector2.Normalize(Main.MouseWorld - player.Center) * 20f;
                float snowballDamage = baseDamage / 2;
                if (PModeChangeApply)
                {
                    snowballDamage = baseDamage * (forceEffect ? 0.9f : 0.8f);
                }
                if (!player.HasEffect<TimberEffect>() && heldItem != null && heldItem.IsWeaponWithDamageClass())
                {
                    snowballDamage *= player.ActualClassDamage(DamageClass.Ranged);
                    float softcapMult = forceEffect ? 10f : 1f;
                    if (PModeChangeApply)
                    {
                        if (snowballDamage > (50f * softcapMult)) // diminishing returns above 15 snowballDamage for non wiz, 100 for wiz (post-deflation numbers; current numbers are higher)
                            snowballDamage = (float)Math.Round(((25f * softcapMult) + snowballDamage) / 1.5f);
                    }
                    else
                    {
                        if (snowballDamage > (25f * softcapMult)) // diminishing returns above 15 snowballDamage for non wiz, 100 for wiz (post-deflation numbers; current numbers are higher)
                            snowballDamage = (float)Math.Round(((50f * softcapMult) + snowballDamage) / 3f);
                    }
                }
                if (player.HasEffect<TimberEffect>())
                    snowballDamage = 400;
                int p = Projectile.NewProjectile(player.GetSource_Accessory(item), player.Center, vel, ProjectileID.SnowBallFriendly, (int)snowballDamage, 1, Main.myPlayer);

                int numSnowballs = forceEffect ? 7 : 3;
                if (p != Main.maxProjectiles)
                    FargoSoulsGlobalProjectile.SplitProj(Main.projectile[p], numSnowballs, MathHelper.Pi / 10, 1);
            }
        }
    }
    public class SnowBallFriendlyOverride : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        //public Item item;
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ProjectileID.SnowBallFriendly;
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[projectile.owner];
            var modPlayer = player.FargoSouls();
            //modPlayer.BorealCD -= 2;
            if (player.HasEffect<BorealEffect>() && PModeWorldSavingSystem.PhantasmMode)
            {
                if (player.HasEffect<TimberEffect>())
                    modPlayer.BorealCD -= 9;
                else
                    modPlayer.BorealCD -= modPlayer.ForceEffect(ModContent.ItemType<BorealWoodEnchant>()) ? (Main.rand.NextBool() ? 2 : 3) : 10;
                
            }
            //Main.NewText(modPlayer.BorealCD);
        }
    }
}
