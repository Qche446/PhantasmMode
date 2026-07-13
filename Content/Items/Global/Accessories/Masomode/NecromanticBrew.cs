using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class NecromanticBrewOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<NecromanticBrew>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.NecromanticBrew"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (item.type == ModContent.ItemType<NecromanticBrew>() && WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<NecroSpinSpeedEffect>(item);
                if (ModContent.GetInstance<NecroSpinSpeedEffect>().speed == 0.5f)
                    ModContent.GetInstance<NecroSpinSpeedEffect>().speed = 0.3f;
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class NecroSpinSpeedEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<SupremeFairyHeader>();
        public override int ToggleItemType => ModContent.ItemType<NecromanticBrew>();
        public float speed = 0.3f;
        public override void PostUpdateEquips(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
                speed = 0.5f;
        }
    }
    public class SkeletronArmRShadowFlame : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(projectile, target, hit, damageDone);
            if (projectile.type == ModContent.ProjectileType<SkeletronArmR>() && WorldSavingSystem.masochistModeReal)
            {
                target.AddBuff(BuffID.ShadowFlame, 120);
            }
        }
    }
    public class NecroSpinSpeed : ModSystem
    {
        public override void Load()
        {
            MethodInfo method1 = typeof(FargoSoulsPlayer).GetMethod("PostUpdateEquips", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(method1, ILNecroSpinSpeed);
        }
        private void ILNecroSpinSpeed(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.3f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return ModContent.GetInstance<NecroSpinSpeedEffect>().speed;
            });
        }
    }
}
