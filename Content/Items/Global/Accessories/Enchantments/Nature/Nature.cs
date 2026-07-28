using FargosPhantasmMode.Common;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using FargosPhantasmMode.Content.Projectiles;
using FargowiltasSouls.Assets.ExtraTextures;
using Luminance.Common.Utilities;
using System.Runtime.InteropServices.Marshalling;
using FargowiltasSouls.Content.Buffs.Souls;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature
{
    public class Nature : PModeGlobalEnchant<NatureForce>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<JungleEnhanceEffect>(item);
            //player.AddEffect<FrostSnowEffect>(item);
            player.AddEffect<MoltenBombEffect>(item);
            player.AddEffect<ShroomiteEffect>(item);
            player.AddEffect<NatureTrailEffect>(item);
            if (PModeChangeApply)
            {
                float timeLeft = 0;
                if (player.HasBuff(ModContent.BuffType<CrimsonRegenBuff>()))
                {
                    for (int i = 0; i < player.buffType.Length; i++)
                    {
                        if (player.buffType[i] == ModContent.BuffType<CrimsonRegenBuff>())
                        {
                            timeLeft = player.buffTime[i];
                        }
                    }
                }
                player.GetDamage(DamageClass.Generic) += 0.3f * timeLeft / 900f;
                player.endurance += 0.3f * timeLeft / 900f;
            }
        }
    }
    public class NatureTrailEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<NatureHeader>();
        public override int ToggleItemType => ModContent.ItemType<NatureForce>();
        public override bool ExtraAttackEffect => true;
    }
    public class NatureTrailProj : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public float radius = 32;
        public bool StartFaded = false;
        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.AfterProjectiles;
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.scale = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.FargoSouls().CanSplit = false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i += 4)
            {
                if (Utilities.CircularHitboxCollision(Projectile.oldPos[i], WidthFunction(i / (float)Projectile.oldPos.Length), targetHitbox))
                    return true;
            }
            return false;
        }
        public override void AI()
        {
            Projectile proj = Main.projectile[(int)Projectile.ai[0]];
            Player player = Main.player[Projectile.owner];
            float damage = 120;
            Projectile.damage = (int)(player.ActualClassDamage(DamageClass.Magic) * damage);
            //Projectile.localNPCHitCooldown = 30;
            if (StartFaded)
            {
                Projectile.Opacity -= 0.1f;
            }
            else if (Projectile.timeLeft < 30)
                Projectile.timeLeft = 30;
            if (!player.HasEffect<NatureTrailEffect>() && !StartFaded)
            {
                Projectile.Opacity -= 0.1f;
            }
            else if (Projectile.Opacity < 1 && proj != null && proj.active && proj.damage != 0 && !StartFaded)
            {
                Projectile.Opacity += 0.1f;
            }
            if (Projectile.Opacity <= 0)
                Projectile.Kill();
            if (proj == null || proj.active == false || proj.damage == 0)
            {
                StartFaded = true;
            }
            else if (!StartFaded)
            {
                Projectile.scale = proj.scale;
                Projectile.velocity = proj.velocity;
                Projectile.Center = proj.Center;
            }
            
        }
        public float WidthFunction(float completionRatio)
        {
            float baseWidth = Projectile.scale * 8 * 3.0f;
            float result = 0;
            if (completionRatio < 0.2f)
            {
                result = MathHelper.SmoothStep(0.3f * baseWidth, baseWidth, 5 * completionRatio);
            }
            else
            {
                result = MathHelper.SmoothStep(baseWidth, 0.3f * baseWidth, (completionRatio - 0.2f) / 0.8f);
            }
            return result * Projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color value = Color.Lerp(Color.DeepSkyBlue, Color.ForestGreen, 1 - completionRatio);
            return Color.Lerp(value, value * 0.5f, completionRatio) * 0.6f;
        }
        public override bool? CanHitNPC(NPC target)
        {
            Projectile.GetGlobalProjectile<PModeGlobalProj>().IceAttribute = true;
            return true;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.AddBuff(BuffID.Frostburn, 240);
            target.AddBuff(BuffID.Frostburn2, 240);
        }
        public override bool PreDraw(ref Color lightColor) => false;
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, _ => Projectile.Size * 0.5f, Pixelate: true, Shader: shader), 60);
        }
    }
}
