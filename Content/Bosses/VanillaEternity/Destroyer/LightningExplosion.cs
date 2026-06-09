using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Destroyer;

public class LightningExplosion : CobaltExplosion
{
    public override string Texture => "FargowiltasSouls/Content/Projectiles/Souls/CobaltExplosion";

    public override void SetDefaults()
    {
        base.SetDefaults();
        base.Projectile.friendly = true;
        base.Projectile.hostile = true;
        base.Projectile.DamageType = DamageClass.Default;
        base.Projectile.scale = 2f;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return base.Projectile.Distance(FargoSoulsUtil.ClosestPointInHitbox(targetHitbox, base.Projectile.Center)) < (float)projHitbox.Width * 0.9f / 2f;
    }

    public override bool CanHitPlayer(Player target)
    {
        
        return base.CanHitPlayer(target);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (!target.townNPC)
        {
            modifiers.SourceDamage *= 1f;
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(144, 120);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(144, 120);
    }
}