using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using Terraria.DataStructures;
using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Cosmo
{
    public class Meteor : PModeGlobalEnchant<MeteorEnchant>
    {
        public const int MeteorCD = 15 * 60;
        public override void Load()
        {
            MethodInfo m = PhanUtil.GetMethodInfo(ModContent.GetInstance<MeteorEffect>().OnHitNPCEither);
            MonoModHooks.Add(m, MeteorEnhance);
        }
        public static void MeteorEnhance(Action<MeteorEffect, Player, NPC, NPC.HitInfo, DamageClass, int, Projectile, Item> orig, MeteorEffect self, Player player, NPC target, NPC.HitInfo hitInfo, DamageClass damageClass, int baseDamage, Projectile projectile, Item item)
        {
            if (PModeChangeApply)
            {
                if (player.whoAmI != Main.myPlayer)
                    return;
                FargoSoulsPlayer modPlayer = player.FargoSouls();
                CooldownBarManager.Activate("MeteorEnchantCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Enchantments/MeteorEnchant").Value, Color.Lerp(MeteorEnchant.NameColor, Color.OrangeRed, 0.75f),
                        () => 1f - player.FargoSouls().MeteorCD / (float)MeteorCD, activeFunction: player.HasEffect<MeteorEffect>, displayAtFull: true);
                if (modPlayer.MeteorCD > 0)
                    return;
                bool forceEffect = modPlayer.ForceEffect<MeteorEnchant>();
                //int damage = forceEffect ? 400 : 70;
                modPlayer.MeteorCD = MeteorCD;
                target.GetGlobalNPC<MeteorTargetGlobalNPC>().MeteorHitCD = forceEffect ? 120 : 60;
            }
            else
            {
                orig.Invoke(self, player, target, hitInfo, damageClass, baseDamage, projectile, item);
            }
        }
    }
    public class MeteorTargetGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public bool IsMeteorTarget = false;
        public int MeteorHitCD = -1;
        public override void ResetEffects(NPC npc)
        {
            if (MeteorHitCD >= 0)
                MeteorHitCD--;
        }
        public override bool PreAI(NPC npc)
        {
            if (MeteorHitCD >= 0 && MeteorHitCD % 8 == 0)
            {
                Player player = Main.LocalPlayer;
                Vector2 pos = new(npc.Center.X + Main.rand.NextFloat(-320, 320), npc.Center.Y - 1000);
                Vector2 vel = new(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(8, 12));
                Vector2 predictive = Main.rand.NextFloat(10f, 30f) * npc.velocity;
                pos.X += predictive.X;
                Vector2 targetPos = npc.Center;
                if (pos.Y < targetPos.Y)
                {
                    vel = FargoSoulsUtil.PredictiveAim(pos, targetPos, npc.velocity / 3, 12f);
                }
                SoundEngine.PlaySound(FargosSoundRegistry.ThrowShort, pos);
                int force = player.HasEffect<CosmoForceEffect>() || player.ForceEffect<MeteorEffect>() ? 1 : 0;
                int damage = player.HasEffect<CosmoForceEffect>() ? 400 : player.ForceEffect<MeteorEffect>() ? 70 : 20;
                IEntitySource entitySource = null;
                if (player.HasEffect<CosmoForceEffect>())
                    entitySource = player.GetSource_EffectItem<CosmosMoonEffect>();
                else if (player.HasEffect<MeteorEffect>())
                    entitySource = player.GetSource_EffectItem<MeteorEffect>();
                if (entitySource == null)
                    return true;
                Projectile.NewProjectile(entitySource, pos, vel, ModContent.ProjectileType<MeteorEnchantMeatball>(), (int)(damage * player.ActualClassDamage(DamageClass.Magic)), 0.5f, player.whoAmI, 0);
            }
            return true;
        }
    }
}
