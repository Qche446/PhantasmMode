using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Lump
{
    public class PungentEyeballOverride : PModeGlobalMasoItem<PungentEyeball>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<PungentEyeballCursor>().PostUpdateEquips, PungentGazeSpeed);
            PhanUtil.AddILHooks(ModContent.GetInstance<FargoSoulsGlobalNPC>().ModifyIncomingHit, ILPungentGazeEx);
        }
        private static void PungentGazeSpeed(Action<PungentEyeballCursor, Player> orig, PungentEyeballCursor self, Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                const float distance = 16 * 5;
                foreach (NPC n in Main.npc.Where(n => n.active && !n.dontTakeDamage && n.lifeMax > 5 && !n.friendly))
                {
                    if (Vector2.Distance(Main.MouseWorld, FargoSoulsUtil.ClosestPointInHitbox(n.Hitbox, Main.MouseWorld)) < distance)
                    {
                        n.AddBuff(ModContent.BuffType<PungentGazeBuff>(), PModeChangeApply ? 3 : 2, true);
                    }
                }

                int visualProj = ModContent.ProjectileType<PungentAuraProj>();
                if (player.ownedProjectileCounts[visualProj] <= 0)
                {
                    Projectile.NewProjectile(self.GetSource_EffectItem(player), player.Center, Vector2.Zero, visualProj, 0, 0, Main.myPlayer);
                }
            }
        }
        private void ILPungentGazeEx(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.15f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() =>
            {
                return Main.LocalPlayer.FargoSouls().LumpOfFlesh && PModeChangeApply ? 0.2f : 0.15f;
            });
        }
    }
}
