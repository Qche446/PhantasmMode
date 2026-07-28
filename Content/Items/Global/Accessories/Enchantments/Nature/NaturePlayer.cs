using FargowiltasSouls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.BackupIO;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature
{
    public class NaturePlayer : ModPlayer
    {
        public bool HasSporeCloudShoot = false;
        public int SporeCloudCD = 0;
        public override void Load()
        {
            On_Player.ItemCheck_CheckCanUse += ThornChakramNoLimit;
        }
        public override void Unload()
        {
            On_Player.ItemCheck_CheckCanUse -= ThornChakramNoLimit;
        }

        private bool ThornChakramNoLimit(On_Player.orig_ItemCheck_CheckCanUse orig, Terraria.Player self, Item sItem)
        {
            bool flag = orig.Invoke(self, sItem);
            if (self.HasEffect<JungleEnhanceEffect>() && sItem.shoot == ProjectileID.ThornChakram)
            {
                flag = true;
            }
            return flag;
        }

        public override void ResetEffects()
        {
            HasSporeCloudShoot = false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (HasSporeCloudShoot && SporeCloudCD > 10)
            {
                if (this.Player.whoAmI == Main.myPlayer)
                {
                    foreach (Projectile p in FargoSoulsUtil.XWay(8, Player.GetSource_FromThis(), target.Center, ProjectileID.SporeCloud, Main.rand.Next(1, 5), FargoSoulsUtil.HighestDamageTypeScaling(Player, 20), 0f))
                    {
                        if (p == null)
                            continue;
                        p.usesIDStaticNPCImmunity = true;
                        p.idStaticNPCHitCooldown = 10;
                        p.FargoSouls().noInteractionWithNPCImmunityFrames = true;
                        p.extraUpdates += 1;
                        p.DamageType = DamageClass.Default;
                    }
                    if (hit.DamageType == DamageClass.Magic)
                        target.AddBuff(BuffID.Poisoned, 300);
                }
                SporeCloudCD = 0;
            }
        }
    }
}
