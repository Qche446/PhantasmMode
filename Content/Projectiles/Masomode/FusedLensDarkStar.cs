using FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins;
using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Circuitry;
using Fargowiltas.Common.Configs;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles.Masomode
{
    public class FusedLensDarkStar : DarkStar
    {
        public ref float ColorAI => ref base.Projectile.ai[2];
        public float ColorType
        {
            get
            {
                return ColorAI;
            }
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = 18000;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.scale *= 0.8f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.CritChance = 100;
        }
        public override void AI()
        {
            Projectile.CritChance = (int)FargoSoulsUtil.HighestCritChance(Main.player[Projectile.owner]);
            Player player = Main.player[Projectile.owner];
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            Projectile.netUpdate = true;
            Projectile.ai[0]++;

            if (player.whoAmI == Main.myPlayer && (player.dead || !player.HasEffect<FusedLensMechElectricOrbEffect>()))
            {
                Projectile.Kill();
                return;
            }

            int type = FargoSoulsUtil.FindClosestHostileNPC(Projectile.Center, 1200, true);
            float distance = (Projectile.Center - player.Center).Length();
            if (type > 0 && Projectile.ai[0] >= 60)//有敌人时
            {
                if (Main.npc[(int)type].active)
                {
                    NPC npc = Main.npc[(int)type];
                    Vector2 vectorToIdlePosition = npc.Center - Projectile.Center;
                    Projectile.velocity = 0.98f * Projectile.velocity + 0.02f * vectorToIdlePosition;
                    FusedLensMechElectricOrb.MechElectricMovement(Projectile, npc.Center, 0, 30);
                }
            }
            else//常态挂机绕玩家伪简谐震动
            {
                if (Projectile.owner == Main.myPlayer && distance > 10)
                {
                    FusedLensMechElectricOrb.MechElectricMovement(Projectile, player.Center, 0.01f, 30);
                }
            }
            base.AI();
            Projectile.Opacity = ModContent.GetInstance<FargoClientConfig>().TransparentFriendlyProjectiles;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            float colorType = ColorType;
            if (colorType != 1f)
            {
                if (colorType != 3f)
                {
                    if (colorType == 2f)
                    {
                        target.AddBuff(BuffID.Ichor, 180);
                    }
                    else
                    {
                        target.AddBuff(ModContent.BuffType<NanoErosionBuff>(), 180);
                    }
                }
                else
                {
                    target.AddBuff(BuffID.CursedInferno, 180);
                }
            }
            else
            {
                target.AddBuff(BuffID.Electrified, 180);
                target.AddBuff(ModContent.BuffType<LightningRodBuff>(), 180);
            }
            target.AddBuff(BuffID.Oiled, 180);
        }
    }
}
