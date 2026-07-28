using FargosPhantasmMode.Common;
using FargosPhantasmMode.Core.Systems;
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

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.SupremeDeath
{
    public class NecromanticBrewOverride : PModeGlobalMasoItem<NecromanticBrew>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<NecroSpinSpeedEffect>(item);
        }
        public override void Load()
        {
            PhanUtil.AddILHooks(ModContent.GetInstance<FargoSoulsPlayer>().PostUpdateEquips, ILNecroSpinSpeed);
        }
        private void ILNecroSpinSpeed(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.3f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() =>
            {
                return Main.LocalPlayer.HasEffect<NecroSpinSpeedEffect>() ? 0.5f : 0.3f;
            });
        }
    }
    public class NecroSpinSpeedEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<SupremeFairyHeader>();
        public override int ToggleItemType => ModContent.ItemType<NecromanticBrew>();
    }
    public class SkeletronArmRShadowFlame : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(projectile, target, hit, damageDone);
            if (projectile.type == ModContent.ProjectileType<SkeletronArmR>() && PModeWorldSavingSystem.PhantasmMode)
            {
                target.AddBuff(BuffID.ShadowFlame, 120);
            }
        }
    }
}
