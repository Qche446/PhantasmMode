using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using JetBrains.Annotations;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Earth
{
    public class Titanium : PModeGlobalEnchant<TitaniumEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<TitaniumRitualEffect>(item);
        }
    }
    public class TitaniumRitualEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<EarthHeader>();
        public override int ToggleItemType => ModContent.ItemType<TitaniumEnchant>();
        public override bool ExtraAttackEffect => true;
        public override void PostUpdateEquips(Player player)
        {
            if (!player.HasEffect<TitaniumRitualEffect>())
                return;
            var modplayer = player.GetModPlayer<EarthPlayer>();
            if (modplayer.TiChargeTime > 0)
            {
                modplayer.TiChargeTime--;
                if (modplayer.PrepareForTi)
                    modplayer.TiEnergy += player.HasEffect<EarthForceEffect>() ? 1 : 1;
            }
            //Main.NewText(modplayer.TiChargeTime);
            if (modplayer.TiEnergy >= modplayer.MaxTiEnergy)
            {
                modplayer.TiEnergy = 0;
                bool HF = player.ForceEffect<TitaniumRitualEffect>();
                int N = HF ? 6 : 4;
                float R = 90;
                for (int i = 1; i <= N; i++)
                {
                    float max = 8;
                    int damage = player.HasEffect<EarthForceEffect>() ? 300 : HF ? 40 : 20;
                    damage = (int)(player.ActualClassDamage(DamageClass.Melee) * damage);
                    for (int j = 0; j < max; j++)
                    {
                        //Main.NewText("成功生成");
                        int projType = ModContent.ProjectileType<TiRitualFragmentsProj>();
                        Vector2 spawnPos = player.Center + R * i * (j * MathHelper.TwoPi / max).ToRotationVector2();
                        int p = Projectile.NewProjectile(player.GetSource_EffectItem<TitaniumRitualEffect>(), player.Center, Vector2.Zero, projType, damage, 1, player.whoAmI, i * R, Main.rand.Next(0, 13), i % 2 == 0 ? 1 : -1);
                        //Main.projectile[p].DamageType = DamageClass.Generic;
                    }
                }

            }
            if (player.whoAmI == Main.myPlayer)
                CooldownBarManager.Activate("TitaniumRitualCharge", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Enchantments/TitaniumEnchant").Value, new(245, 245, 245),
                () => modplayer.TiEnergy / modplayer.MaxTiEnergy, activeFunction: player.HasEffect<TitaniumRitualEffect>, displayAtFull: true);
        }
        public override void OnHitNPCEither(Player player, NPC target, NPC.HitInfo hitInfo, DamageClass damageClass, int baseDamage, Projectile projectile, Item item)
        {
            var modplayer = player.GetModPlayer<EarthPlayer>();
            if (player.HasEffect<TitaniumRitualEffect>() && modplayer.PrepareForTi)
            {
                //Main.NewText(2);
                modplayer.TiChargeTime = 15;
            }
        }
    }
    public class TiRitualFragmentsProj : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.TitaniumStormShard;
        public ref float Radius => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = Main.projFrames[ProjectileID.TitaniumStormShard];
            //ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            //ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.scale = 0.8f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ArmorPenetration = 20;
        }
        public override void AI()
        {
            Player py = Main.player[Projectile.owner];
            var Earthpy = py.GetModPlayer<EarthPlayer>();
            List<Projectile> list = Earthpy.TiList.Where(p => p.ai[0] == Radius).ToList();
            if (!py.active || py == null || list.Count <= 0)
            {
                Projectile.Kill();
                return;
            }
            Projectile.rotation = (Projectile.Center - py.Center).ToRotation();
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = list.IndexOf(Projectile) * MathHelper.TwoPi / list.Count + Radius / 180f;
            }
            float angle = list.IndexOf(Projectile) * MathHelper.TwoPi / list.Count;
            float offangle = MathF.PI * 2f * ((float)Main.GameUpdateCount % 50) / 50;
            Projectile.Center = py.Center + Vector2.UnitX.RotatedBy(Projectile.localAI[0] + offangle * Projectile.ai[2]) * Radius;
            Projectile.frame = (int)Projectile.ai[1];
        }
        public override bool ShouldUpdatePosition() => false;
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 3; i++) 
            {
                Dust d = Dust.NewDustDirect(Projectile.Center, 8, 8, DustID.SilverCoin, Alpha: 100);
                d.velocity = 3 * Main.rand.NextVector2Unit();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[Projectile.type].Value;
            int num = TextureAssets.Projectile[Projectile.type].Value.Width / Main.projFrames[Projectile.type];
            int y = num * Projectile.frame;
            Rectangle rectangle = new Rectangle(y, 0, num, value.Height);
            Vector2 origin = rectangle.Size() / 2f;
            SpriteEffects effects = ((base.Projectile.spriteDirection <= 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            /*
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Vector2 vector = Projectile.oldPos[i];
                float rotation = Projectile.oldRot[i];
                Color color = Color.White;
                color.A = 50;
                //Main.spriteBatch.Draw(value, vector + Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), rectangle, color, rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            */
            Main.EntitySpriteDraw(value, base.Projectile.Center - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, Color.White, base.Projectile.rotation, origin, base.Projectile.scale,effects);
            return false;
        }
    }
}
