using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow;
using FargosPhantasmMode.Content.Projectiles;
using Fargowiltas.Projectiles;
using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature
{
    public class Snow : PModeGlobalEnchant<SnowEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<SnowTrailEffect>(item);
            player.AddEffect<TimeFrozenRitualEffect>(item);
        }
        public override void SafeUpdateVanity(Item item, Player player)
        {
            player.AddEffect<SnowTrailEffect>(item);
        }
    }
    public class SnowTrailEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<NatureHeader>();
        public override int ToggleItemType => ModContent.ItemType<SnowEnchant>();
        public override bool ExtraAttackEffect => true;
        public override void PostUpdateMiscEffects(Player player)
        {
            int type = ModContent.ProjectileType<SnowTrailProj>();
            int num = player.ownedProjectileCounts[type];
            if (num < 1)
                Projectile.NewProjectile(player.GetSource_FromThis(), player.Center, Vector2.Zero, type, 0, 2, Main.myPlayer);
        }
    }
    public class TimeFrozenRitualEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<NatureHeader>();
        public override int ToggleItemType => ModContent.ItemType<SnowEnchant>();
        public override bool ExtraAttackEffect => true;
        public override void PostUpdateMiscEffects(Player player)
        {
            int type = ModContent.ProjectileType<TimeFrozenRitualProj>();
            int num = player.ownedProjectileCounts[type];
            if (num < 1)
                Projectile.NewProjectile(player.GetSource_EffectItem<TimeFrozenRitualEffect>(), player.Center, Vector2.Zero, type, 0, 2, Main.myPlayer);
        }
    }
    public class SnowTrailProj : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public float radius = 64;
        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.AfterProjectiles;
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 18000;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.scale = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.FargoSouls().CanSplit = false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Utilities.CircularHitboxCollision(Projectile.Center, radius, targetHitbox))
            {
                return true;
            }
            for (int i = 0; i < Projectile.oldPos.Length; i += 4)
            {
                if (Utilities.CircularHitboxCollision(Projectile.oldPos[i], WidthFunction(i / (float)Projectile.oldPos.Length), targetHitbox))
                    return true;
            }
            return false;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            bool hasForce = player.ForceEffect<SnowTrailEffect>();
            bool hasNature = player.HasEffect<NatureEffect>();
            int slot = player.FindAccessorySlot(ModContent.ItemType<SnowEnchant>());
            //Main.NewText(slot);
            radius = hasForce ? 128 : 64;
            radius *= hasNature ? 2 : 1;
            Projectile.localNPCHitCooldown = hasForce ? 10 : 20;
            float damage = hasForce ? 80 : 10;
            damage *= hasNature ? 3 : 1;
            if (slot == -1)
                damage = 0;
            Projectile.damage = (int)(player.ActualClassDamage(DamageClass.Magic) * damage);
            if (!player.HasEffect<SnowTrailEffect>())
            {
                Projectile.Opacity -= 0.1f;
            }
            else if(Projectile.Opacity < 1)
            {
                Projectile.Opacity += 0.1f;
            }
            if (Projectile.Opacity <= 0)
                Projectile.Kill();
            Projectile.Center = player.Center;
            /*
            if (slot == -1)
                return;
            for (int i = 0; i < (hasForce ? 10 : 5); i++)
            {
                Vector2 offset = new();
                double angle = Main.rand.NextDouble() * 2d * Math.PI;
                offset.X += (float)(Math.Sin(angle) * radius);
                offset.Y += (float)(Math.Cos(angle) * radius);
                Dust dust = Main.dust[Dust.NewDust(player.Center + offset - new Vector2(4, 4), 0, 0, DustID.GemSapphire, 0, 0, 150, Color.White, 0.8f)];
                dust.velocity = player.velocity;
                if (Main.rand.NextBool(3))
                    dust.velocity += Vector2.Normalize(offset) * -(hasForce ? 8f : 4f);
                dust.noGravity = true;
            }
            foreach(NPC npc in Main.npc.Where(n => n.active && !n.HasBuff(ModContent.BuffType<TimeFrozenBuff>())))
            {
                npc.AddBuff(ModContent.BuffType<TimeFrozenBuff>(), 2);
            }
            foreach(Projectile proj in Main.projectile.Where(p => p.active && !(p.minion && !ProjectileID.Sets.MinionShot[p.type]) && !p.FargoSouls().TimeFreezeImmune && p.FargoSouls().TimeFrozen == 0))
            {
                proj.FargoSouls().TimeFrozen = 2;
            }
            */
        }
        public float WidthFunction(float completionRatio)
        {
            Player player = Main.player[Projectile.owner];
            bool hasForce = player.ForceEffect<SnowTrailEffect>();
            float baseWidth = Projectile.scale * 16 * 3.0f;
            baseWidth *= hasForce ? 0.8f : 0.5f;
            return MathHelper.SmoothStep(baseWidth, baseWidth * 0.3f, completionRatio) * Projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color value = Color.Lerp(Color.DeepSkyBlue, Color.White, 1 - completionRatio);
            return Color.Lerp(value, value * 0.5f, completionRatio);
        }
        public override bool? CanHitNPC(NPC target)
        {
            Projectile.GetGlobalProjectile<PModeGlobalProj>().IceAttribute = true;
            return !target.townNPC;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.AddBuff(BuffID.Frostburn, 180);
            Player player = Main.player[Projectile.owner];
            bool hasForce = player.ForceEffect<SnowTrailEffect>();
            if (hasForce)
            {
                target.AddBuff(BuffID.Frostburn2, 180);
            }
        }
        public override bool PreDraw(ref Color lightColor) => false;
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, _ => Projectile.Size * 0.5f, Pixelate: true, Shader: shader), 100);
        }
    }
    public class TimeFrozenRitualProj : ModProjectile
    {
        public float radius = 64;
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 18000;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.scale = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.FargoSouls().CanSplit = false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => Utilities.CircularHitboxCollision(Projectile.Center, radius, targetHitbox);

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            bool hasForce = player.ForceEffect<TimeFrozenRitualEffect>();
            bool hasNature = player.HasEffect<NatureEffect>();
            //Main.NewText(slot);
            radius = hasForce ? 128 : 64;
            radius *= hasNature ? 2 : 1;
            Projectile.localNPCHitCooldown = hasForce ? 10 : 20;
            float damage = hasForce ? 80 : 10;
            damage *= hasNature ? 3 : 1;
            Projectile.damage = (int)(player.ActualClassDamage(DamageClass.Magic) * damage);
            if (!player.HasEffect<TimeFrozenRitualEffect>())
            {
                Projectile.Kill();
            }
            Projectile.Center = player.Center;
            for (int i = 0; i < (hasNature ? 40 : (hasForce ? 20 : 10)); i++)
            {
                Vector2 offset = new();
                double angle = Main.rand.NextDouble() * 2d * Math.PI;
                offset.X += (float)(Math.Sin(angle) * radius);
                offset.Y += (float)(Math.Cos(angle) * radius);
                Dust dust = Main.dust[Dust.NewDust(player.Center + offset - new Vector2(4, 4), 0, 0, DustID.GemSapphire, 0, 0, 150, Color.White, 0.8f)];
                dust.velocity = player.velocity;
                if (Main.rand.NextBool(3))
                    dust.velocity += Vector2.Normalize(offset) * -(hasForce ? 8f : 4f);
                dust.noGravity = true;
            }
            foreach (NPC npc in Main.npc.Where(n => n.active && !n.HasBuff(ModContent.BuffType<TimeFrozenBuff>()) && 
                Utilities.CircularHitboxCollision(Projectile.Center, radius, n.Hitbox)))
            {
                npc.AddBuff(ModContent.BuffType<TimeFrozenBuff>(), 3);
            }
            foreach (Projectile proj in Main.projectile.Where(p => Utilities.CircularHitboxCollision(Projectile.Center, radius, p.Hitbox) && p.active && !(p.minion && !ProjectileID.Sets.MinionShot[p.type]) && !p.FargoSouls().TimeFreezeImmune && p.FargoSouls().TimeFrozen == 0))
            {
                proj.FargoSouls().TimeFrozen = 3;
            }
        }
        public override bool? CanHitNPC(NPC target)
        {
            Projectile.GetGlobalProjectile<PModeGlobalProj>().IceAttribute = true;
            return true;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.AddBuff(BuffID.Frostburn, 180);
            Player player = Main.player[Projectile.owner];
            bool hasForce = player.ForceEffect<SnowTrailEffect>();
            if (hasForce)
            {
                target.AddBuff(BuffID.Frostburn2, 180);
            }
        }
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
