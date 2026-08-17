using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Weapons.BossDrops;
using FargowiltasSouls.Content.Items.Weapons.Challengers;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.UI;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Earth
{
    public class Adamantite : PModeGlobalEnchant<AdamantiteEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<AdamantiteProjSplit>(item);
            //player.AddEffect<AdamantiteEffect>(item);
        }
        public override void Load()
        {
            PhanUtil.AddHooks(AdamantiteEffect.CalcAdamantiteAttackSpeed, CalcAdamantiteAttackSpeedFixed);
            PhanUtil.AddHooks(ModContent.GetInstance<AdamantiteEffect>().PostUpdateEquips, PostUpdateEquipsFixed);
            PhanUtil.AddHooks(AdamantiteEffect.AdamantiteSplit, AdamantiteSplitFixed);
        }
        private void CalcAdamantiteAttackSpeedFixed(Action<Player, Item> orig, Player player, Item item)
        {
            if (!player.HasEffectEnchant<AdamantiteEffect>() && !PModeChangeApply)
                return;
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            if (!(item.DamageType != DamageClass.Default && item.pick == 0 && item.axe == 0 && item.hammer == 0 && item.type != ModContent.ItemType<PrismaRegalia>()))
                return;
            if (item.shoot <= ProjectileID.None)
                return;
            if (!modPlayer.HeldItemAdamantiteValid)
                return;
            float maxSpeed = player.ForceEffect<AdamantiteEffect>() ? 0.5f : 0.3f;

            if (ProjectileID.Sets.CultistIsResistantTo[item.shoot])
                maxSpeed /= 2;

            float ratio = Math.Max((float)modPlayer.AdamantiteSpread / AdamantiteEffect.SpreadCap, 0);
            modPlayer.AttackSpeed += maxSpeed * ratio;

            if (player.whoAmI == Main.myPlayer)
                CooldownBarManager.Activate("AdamantiteEnchantCharge", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Enchantments/AdamantiteEnchant").Value, new(221, 85, 125),
                () => (float)player.FargoSouls().AdamantiteSpread / AdamantiteEffect.SpreadCap, activeFunction: player.HasEffect<AdamantiteEffect>, displayAtFull: true);
        }
        private static void PostUpdateEquipsFixed(Action<AdamantiteEffect, Player> orig, AdamantiteEffect self, Player player)
        {
            if (!self.HasEffectEnchant(player) && !PModeChangeApply)
                return;
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (player.HeldItem != null && player.HeldItem.IsWeapon())
            {
                if (modPlayer.AdamantiteItem != player.HeldItem)
                {
                    modPlayer.HeldItemAdamantiteValid = false;
                    modPlayer.AdamantiteSpread = 0;
                }
                modPlayer.AdamantiteItem = player.HeldItem;
            }

            int adaCap = (int)AdamantiteEffect.SpreadCap;

            const float incSeconds = 10;
            const float decSeconds = 1.5f;
            if (modPlayer.WeaponUseTimer > 0)
                modPlayer.AdamantiteSpread += (adaCap / 60f) / incSeconds; //ada spread change per frame, based on total amount of seconds to reach cap
            else
                modPlayer.AdamantiteSpread -= (adaCap / 60f) / decSeconds;

            if (modPlayer.AdamantiteSpread < 0)
                modPlayer.AdamantiteSpread = 0;

            if (modPlayer.AdamantiteSpread > adaCap)
                modPlayer.AdamantiteSpread = adaCap;
        }
        private static void AdamantiteSplitFixed(Action<Projectile, FargoSoulsPlayer, int> orig, Projectile projectile, FargoSoulsPlayer modPlayer, int splitDegreeAngle)
        {
            if (!modPlayer.Player.HasEffectEnchant<AdamantiteEffect>() && !PModeChangeApply)
                return;
            if (AdamantiteEffect.AdamIgnoreItems.Contains(modPlayer.Player.HeldItem.type))
                return;
            modPlayer.HeldItemAdamantiteValid = true;
            projectile.velocity = projectile.velocity.RotateRandom(MathHelper.ToRadians(splitDegreeAngle));
            projectile.FargoSouls().Adamantite = true;
        }
    }
    public class AdamantiteProjSplit : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<EarthHeader>();
        public override int ToggleItemType => ModContent.ItemType<AdamantiteEnchant>();
        public override bool ExtraAttackEffect => true;
        public const float SpreadCap = 17; // spread cap in DEGREES
        static int[] AdamIgnoreItems = new int[]
        {
            ItemID.NightsEdge,
            ItemID.TrueNightsEdge,
            ItemID.Excalibur,
            ItemID.TrueExcalibur,
            ItemID.TerraBlade,
            ModContent.ItemType<DecrepitAirstrikeRemote>()
        };
        public override void PostUpdateEquips(Player player)
        {

        }
        public static void AdamantiteSplit(Projectile projectile, FargoSoulsPlayer modPlayer, int splitDegreeAngle)
        {
            bool hasForce = Main.LocalPlayer.ForceEffect<AdamantiteProjSplit>();
            if (AdamIgnoreItems.Contains(modPlayer.Player.HeldItem.type))
            {
                return;
            }
            foreach (Projectile p in FargoSoulsGlobalProjectile.SplitProj(projectile, 3, MathHelper.ToRadians(splitDegreeAngle), hasForce ? 1f / 3 : 1f / 2))
            {
                if (p != null && p.active)
                {
                    p.FargoSouls().HuntressProj = projectile.FargoSouls().HuntressProj;
                }
            }

            if (!hasForce)
            {
                projectile.type = ProjectileID.None;
                projectile.timeLeft = 0;
                projectile.active = false;
            }
            else
            {
                projectile.damage = (int)(projectile.damage / 3f);
            }
        }
    }
    public class AdamantiteGlobalProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public override GlobalProjectile NewInstance(Projectile target) => PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        public int AdamModifier;
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            Player player = Main.player[projectile.owner];
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            bool canAdaSplit = AdamantiteEffect.CanBeAffected(projectile, player);
            if (player.HasEffect<AdamantiteEffect>() && canAdaSplit && !projectile.FargoSouls().Adamantite)
            {
                if (AdamantiteEffect.AdamIgnoreItems.Contains(modPlayer.Player.HeldItem.type))
                    return;
                modPlayer.HeldItemAdamantiteValid = true;
                //projectile.velocity = projectile.velocity.RotateRandom(MathHelper.ToRadians(splitDegreeAngle));
                projectile.FargoSouls().Adamantite = true;
            }

            bool CanSplit = projectile.FargoSouls().CanSplit;
            if (player.HasEffect<AdamantiteProjSplit>()
                && FargoSoulsUtil.OnSpawnEnchCanAffectProjectile(projectile, false)
                && CanSplit && Array.IndexOf(NoSplit, projectile.type) <= -1
                && projectile.aiStyle != ProjAIStyleID.Spear)
            {
                if (projectile.owner == Main.myPlayer
                    && (FargoSoulsUtil.IsProjSourceItemUseReal(projectile, source)
                    || source is EntitySource_Parent parent && parent.Entity is Projectile sourceProj && (sourceProj.aiStyle == ProjAIStyleID.Spear || sourceProj.minion || sourceProj.sentry || ProjectileID.Sets.IsAWhip[sourceProj.type] && !ProjectileID.Sets.IsAWhip[projectile.type])))
                {
                    //apen is inherited from proj to proj
                    projectile.ArmorPenetration += projectile.damage / 2;

                    AdamantiteProjSplit.AdamantiteSplit(projectile, modPlayer, (int)(32));
                }

                //AdamModifier = modPlayer.EarthForce ? 3 : 2;
                //AdamModifier = modPlayer.ForceEffect(modPlayer.AdamantiteItem.type) ? 3 : 2;
            }
            
        }
        public static int[] NoSplit => new int[] {
            ProjectileID.SandnadoFriendly,
            ProjectileID.LastPrism,
            ProjectileID.LastPrismLaser,
            ProjectileID.BabySpider,
            ProjectileID.Phantasm,
            ProjectileID.VortexBeater,
            ProjectileID.ChargedBlasterCannon,
            ProjectileID.WireKite,
            ProjectileID.DD2PhoenixBow,
            ProjectileID.LaserMachinegun,
            ProjectileID.PiercingStarlight,
            ProjectileID.Celeb2Weapon,
            ProjectileID.Xenopopper
        };
    }
}
