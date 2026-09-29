using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using Luminance.Common.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.InfoDisplays
{
    public class InfoModPlayer : ModPlayer
    {
        public bool ShowGrazeCount = false;
        public bool BossAliveLastFrame = false;
        public int GrazeCount = 0;
        public bool ShowDotsDps = false;
        public int targetIndex = -1;
        public Queue<int> dotDamagePond = new(64);
        public void RegisterDotDamage(int damage)
        {
            if (dotDamagePond.Count >= 60)
                dotDamagePond.Dequeue();
            dotDamagePond.Enqueue(damage);
            //Main.NewText(damage);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target != null && target.active)
                targetIndex = target.whoAmI;
        }
        public override void PostUpdateEquips()
        {
            if (Utilities.AnyBosses())
            {
                if (!BossAliveLastFrame)
                {
                    BossAliveLastFrame = true;
                    GrazeCount = 0;
                }
            }
            else
                BossAliveLastFrame = false;
        }
        public override void ResetInfoAccessories()
        {
            ShowGrazeCount = false;
            ShowDotsDps = false;
            if (targetIndex >= 0 && targetIndex < Main.npc.Length)
                if (!Main.npc[targetIndex].active || Main.npc[targetIndex] == null)
                    targetIndex = -1;
        }
        public override void RefreshInfoAccessoriesFromTeamPlayers(Player otherPlayer)
        {
            var infop = otherPlayer.GetModPlayer<InfoModPlayer>();
            if (infop.ShowGrazeCount)
                ShowGrazeCount = true;
            if (infop.ShowDotsDps)
                ShowDotsDps = true;
        }
    }
}
