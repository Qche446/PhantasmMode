using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Earth;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class ShadowGlobalProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public override GlobalProjectile NewInstance(Projectile target) => PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            Player py = Main.player[proj.owner];
            if (proj.type == ModContent.ProjectileType<MonkDashSlash>() && source is EntitySource_Parent parent && parent.Entity is Projectile)
            {
                py.GetModPlayer<ShadowPlayer>().ActualReduceMonoCD = true;
            }
            if (py.HasEffect<NinjaAttackSpeedEffect>() && (source is EntitySource_ItemUse_WithAmmo || source is EntitySource_ItemUse))
            {
                bool hasForce = py.ForceEffect<NinjaAttackSpeedEffect>();
                float muti = hasForce ? 0.333f : 0.5f;
                proj.damage = (int)(proj.damage * muti);
            }

            //处理精金分裂
            FargoSoulsPlayer modPlayer = py.FargoSouls();

            bool canAdaSplit = AdamantiteEffect.CanBeAffected(proj, py);
            if (py.HasEffect<AdamantiteEffect>() && canAdaSplit && !proj.FargoSouls().Adamantite)
            {
                if (AdamantiteProjSplit.AdamIgnoreItems.Contains(modPlayer.Player.HeldItem.type))
                    return;
                modPlayer.HeldItemAdamantiteValid = true;
                //projectile.velocity = projectile.velocity.RotateRandom(MathHelper.ToRadians(splitDegreeAngle));
                proj.FargoSouls().Adamantite = true;
            }

            bool CanSplit = proj.FargoSouls().CanSplit;
            if (py.HasEffect<AdamantiteProjSplit>()
                && FargoSoulsUtil.OnSpawnEnchCanAffectProjectile(proj, false)
                && CanSplit && Array.IndexOf(AdamantiteProjSplit.NoSplit, proj.type) <= -1
                && proj.aiStyle != ProjAIStyleID.Spear)
            {
                if (proj.owner == Main.myPlayer
                    && (FargoSoulsUtil.IsProjSourceItemUseReal(proj, source)
                    || source is EntitySource_Parent parent1 && parent1.Entity is Projectile sourceProj && (sourceProj.aiStyle == ProjAIStyleID.Spear || sourceProj.minion || sourceProj.sentry || ProjectileID.Sets.IsAWhip[sourceProj.type] && !ProjectileID.Sets.IsAWhip[proj.type])))
                {
                    //apen is inherited from proj to proj
                    proj.ArmorPenetration += proj.damage / 2;

                    AdamantiteProjSplit.AdamantiteSplit(proj, modPlayer, (int)(32));
                }

                //AdamModifier = modPlayer.EarthForce ? 3 : 2;
                //AdamModifier = modPlayer.ForceEffect(modPlayer.AdamantiteItem.type) ? 3 : 2;
            }
        }
        public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {

        }
    }
}
