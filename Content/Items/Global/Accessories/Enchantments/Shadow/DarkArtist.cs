using FargosPhantasmMode.Common;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Net.Mime.MediaTypeNames;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class DarkArtist : PModeGlobalEnchant<DarkArtistEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<ApprenticeSupport>().PostUpdateEquips, ApprenticeFixed);
        }
        private static void ApprenticeFixed(Action<ApprenticeSupport, Player> orig, ApprenticeSupport self, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            bool forceEffect = modPlayer.ForceEffect<ApprenticeEnchant>();
            //update item cd
            if (modPlayer.ApprenticeItemCD > 0)
            {
                modPlayer.ApprenticeItemCD--;
            }

            if (player.controlUseItem)
            {

                if (player.controlUseItem)
                {
                    Item item = player.HeldItem;

                    //non weapons and weapons with no ammo begone
                    if (item.damage <= 0 || !player.HasAmmo(item) || (item.mana > 0 && player.statMana < item.mana)
                        || item.createTile != -1 || item.createWall != -1 || item.ammo != AmmoID.None || item.hammer != 0 || item.pick != 0 || item.axe != 0) return;

                    int startingSlot = 0;

                    //first we need to find what slot the current weapon is
                    for (int j = 0; j < 10; j++) //hotbar
                    {
                        Item item2 = player.inventory[j];

                        if (item2.type == item.type)
                        {
                            startingSlot = j;
                            break;
                        }
                    }

                    int weaponsUsed = 0;

                    //then go from there and find the next weapon to fire
                    for (int j = startingSlot; j < 10; j++) //hotbar
                    {
                        Item item2 = player.inventory[j];

                        if (item2 != null && item2.damage > 0 && item2.shoot > ProjectileID.None && item2.ammo <= 0 && item.type != item2.type && !item2.channel)
                        {
                            if (!player.HasAmmo(item2) || (item2.mana > 0 && player.statMana < item2.mana) || item2.sentry || ContentSamples.ProjectilesByType[item2.shoot].minion || ApprenticeSupport.Blacklist.Contains(item2.type))
                                continue;

                            weaponsUsed++;
                            if (weaponsUsed > 1)
                                break;

                            int itemCD = modPlayer.ApprenticeItemCD;

                            if (itemCD > 0)
                                continue;

                            if (!PlayerLoader.CanUseItem(player, item2) || !ItemLoader.CanUseItem(item2, player))
                                continue;

                            Vector2 pos = new(player.Center.X + Main.rand.Next(-50, 50), player.Center.Y + Main.rand.Next(-50, 50));
                            Vector2 velocity = Vector2.Normalize(Main.MouseWorld - pos);

                            int projToShoot = item2.shoot;
                            float speed = item2.shootSpeed;
                            int damage = player.GetWeaponDamage(item2);
                            float KnockBack = item2.knockBack;

                            int itemtime = player.itemTime;
                            int itemtimemax = player.itemTimeMax;
                            ApprenticeSupport.shootMethod.Invoke(player, [player.whoAmI, item2, damage]);

                            player.itemTime = itemtime;
                            player.itemTimeMax = itemtimemax;
                            int divisor = 7;
                            if (modPlayer.DarkArtistEnchantActive && forceEffect)
                            {
                                divisor = 3;
                            }
                            else if (modPlayer.DarkArtistEnchantActive || forceEffect)
                            {
                                divisor = 3;
                            }

                            if (!self.HasEffectEnchant(player))
                                divisor = PModeChangeApply ? 3 : 10;

                            modPlayer.ApprenticeItemCD = item2.useAnimation * divisor;

                            if (item2.mana > 0)
                            {
                                if (player.CheckMana(item2.mana / 2, true, false))
                                {
                                    player.manaRegenDelay = 300;
                                }
                            }
                            if (item2.consumable)
                            {
                                item2.stack--;
                            }
                            break;
                        }
                    }
                }
            }
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<DarkArtistEffect>(item);
        }
    }
    public class DarkArtistEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<ShadowHeader>();
        public override int ToggleItemType => ModContent.ItemType<DarkArtistEnchant>();
        public override bool MinionEffect => true;
        public override void PostUpdateEquips(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<LightningRingMinion>()] < 1)
            {
                FargoSoulsUtil.NewSummonProjectile(player.GetSource_EffectItem<DarkArtistEffect>(), player.Center, Vector2.Zero, ModContent.ProjectileType<LightningRingMinion>(),
                    30, 0, player.whoAmI);
            }
        }
    }
    public class LightningRingMinion : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DD2LightningAuraT3;
        public float Radius = 240;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = Main.projFrames[ProjectileID.DD2LightningAuraT3];
        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.timeLeft = 900;
            Projectile.localNPCHitCooldown = 5;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.sentry = true;
            Projectile.netImportant = true;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (player.whoAmI == Main.myPlayer && (player.dead || !player.HasEffect<DarkArtistEffect>()))
            {
                Projectile.Kill();
                return;
            }
            Projectile.netUpdate = true; // Please sync ech
            Projectile.Center = player.Center - Radius * Vector2.UnitY;
            bool isF = player.ForceEffect<DarkArtistEffect>();
            Projectile.damage = (int)(player.ActualClassDamage(DamageClass.Summon) * 40f);
            Radius = isF ? 480 : 240;
            Projectile.damage *= isF ? 1 : 1;
            Projectile.damage *= player.HasEffect<ShadowForceEffect>() ? 2 : 1;

            if (Projectile.timeLeft < 30)
                Projectile.timeLeft = 30;
            if (++Projectile.frameCounter >= 8)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= Main.projFrames[ProjectileID.DD2LightningAuraT3])
                {
                    Projectile.frame = 0;
                }
            }
            DelegateMethods.v3_1 = new Vector3(0.2f, 0.7f, 1f);
            Utils.PlotTileLine(Projectile.Center + Vector2.UnitX * -Radius, Projectile.Center + Vector2.UnitX * Radius, 2 * Radius, DelegateMethods.CastLightOpen);
            Vector2 vector2 = new Vector2(Projectile.Top.X, Projectile.position.Y + Radius);
            for (int j = 0; j < (isF ? 32 : 16); j++)
            {
                if (!Main.rand.NextBool(6))
                {
                    continue;
                }
                Vector2 vector3 = Main.rand.NextVector2Unit();
                if (!(Math.Abs(vector3.X) < 0.06f))
                {
                    Vector2 targetPosition = Projectile.Center + Radius * vector3 + Radius * Vector2.UnitY;
                    if (!WorldGen.SolidTile((int)targetPosition.X / 16, (int)targetPosition.Y / 16) && Projectile.AI_137_CanHit(targetPosition))
                    {
                        Dust dust = Dust.NewDustDirect(targetPosition, 0, 0, DustID.Electric, 0f, 0f, 100);
                        dust.position = targetPosition;
                        dust.velocity = (vector2 - dust.position).SafeNormalize(Vector2.Zero);
                        dust.scale = 0.7f;
                        dust.fadeIn = 1f;
                        dust.noGravity = true;
                        dust.noLight = true;
                    }
                }
            }
            for (int l = 0; l < 12; l++)
            {
                if (Main.rand.NextBool(10))
                {
                    Dust dust3 = Dust.NewDustDirect(Projectile.Center + Main.rand.NextFloat(0, 2 * Radius) * Vector2.UnitY, 16, Projectile.height / 2 - (int)Radius, DustID.Electric, 0f, 0f, 100);
                    dust3.velocity *= 0.6f;
                    dust3.velocity += Vector2.UnitY * -2f;
                    dust3.scale = 0.7f;
                    dust3.noGravity = true;
                    dust3.noLight = true;
                }
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularHitboxCollision(Projectile.Center + new Vector2(0, Radius), Radius, targetHitbox);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.AddBuff(BuffID.Electrified, 240);
            modifiers.DefenseEffectiveness *= 0;
        }
        public override void OnKill(int timeLeft)
        {
            const int num226 = 12;
            for (int i = 0; i < num226; i++)
            {
                Vector2 vector6 = Vector2.UnitX.RotatedBy(Projectile.rotation) * 6f;
                vector6 = vector6.RotatedBy((i - (num226 / 2 - 1)) * 6.28318548f / num226, default) + Projectile.Center;
                Vector2 vector7 = vector6 - Projectile.Center;
                int num228 = Dust.NewDust(vector6 + vector7, 0, 0, DustID.Electric, 0f, 0f, 0, default, 1.5f);
                Main.dust[num228].noGravity = true;
                Main.dust[num228].velocity = vector7;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[Projectile.type].Value;
            int num = TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
            int y = num * Projectile.frame;
            Rectangle rectangle = new(0, y, value.Width, num);
            Vector2 origin = rectangle.Size() / 2f;
            Vector2 vector = Projectile.rotation.ToRotationVector2() * (value.Width - Projectile.width) / 2f;

            vector = Vector2.Zero;
            SpriteEffects effects = ((Projectile.spriteDirection <= 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

            float alphaMultiplier = 1;
            Color drawColor = lightColor * alphaMultiplier;
            Main.EntitySpriteDraw(value, Projectile.Center + vector - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), rectangle, Projectile.GetAlpha(drawColor), Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
    }
    public class BallistaMinion : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DD2BallistraTowerT3;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = Main.projFrames[ProjectileID.DD2BallistraTowerT3];
        }
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 54;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.minion = true;
            Projectile.timeLeft = 900;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.manualDirectionChange = true;
            Projectile.sentry = true;
            Projectile.netImportant = true;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (player.whoAmI == Main.myPlayer && (player.dead || !player.HasEffect<DarkArtistEffect>()))
            {
                Projectile.Kill();
                return;
            }
            Projectile.netUpdate = true; // Please sync ech
            Projectile.Center = player.Center - 200 * Vector2.UnitY;
            bool isF = player.ForceEffect<DarkArtistEffect>();

        }
        public override bool? CanHitNPC(NPC target) => false;
        public override bool ShouldUpdatePosition() => false;
    }
    public class FlameburstMinionGlobal : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ModContent.ProjectileType<FlameburstMinion>();
        public override GlobalProjectile NewInstance(Projectile target) => PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        public override bool PreAI(Projectile proj)
        {
            bool PH = PModeWorldSavingSystem.PhantasmMode;
            Player player = Main.player[proj.owner];

            if (player.whoAmI == Main.myPlayer && (player.dead || !player.FargoSouls().DarkArtistEnchantActive || !player.HasEffect<DarkArtistMinion>()))
            {
                proj.Kill();
                return false;
            }

            proj.netUpdate = true; // Please sync ech

            //pulsation mumbo jumbo
            proj.position.X = (int)proj.position.X;
            proj.position.Y = (int)proj.position.Y;
            float num395 = Main.mouseTextColor / 200f - 0.35f;
            num395 *= 0.2f;
            proj.scale = num395 + 0.95f;

            //charging above the player
            if (proj.ai[0] == 0)
            {
                //float above player
                proj.position.X = player.Center.X - proj.width / 2;
                proj.position.Y = player.Center.Y - proj.height / 2 + player.gfxOffY - 50f;

                //rotate towards and face mouse
                const float rotationModifier = 0.08f;

                if (player.whoAmI == Main.myPlayer)
                {
                    if (Main.MouseWorld.X > proj.Center.X)
                    {
                        proj.spriteDirection = 1;

                        proj.rotation = proj.rotation.AngleLerp(
                            (new Vector2(Main.MouseWorld.X, Main.MouseWorld.Y) - proj.Center).ToRotation(), rotationModifier);
                    }
                    else
                    {
                        proj.spriteDirection = -1;

                        //absolute fuckery so it faces the right direction
                        Vector2 target = new Vector2(Main.MouseWorld.X - (Main.MouseWorld.X - proj.Center.X) * 2, Main.MouseWorld.Y - (Main.MouseWorld.Y - proj.Center.Y) * 2) - proj.Center;

                        proj.rotation = proj.rotation.AngleLerp(target.ToRotation(), rotationModifier);
                    }
                }



                //attack as sentry 
                int attackRate = 60;
                proj.ai[1] += 1f;
                if (PH && player.ForceEffect<DarkArtistEffect>())
                    proj.ai[1]++;
                if (player.controlUseItem && proj.ai[1] >= attackRate)
                {
                    Vector2 velocity = Vector2.Normalize(Main.MouseWorld - proj.Center) * 10;
                    int damage = PH && player.ForceEffect<DarkArtistEffect>() ? 144 : 72;
                    damage *= PH && player.HasEffect<ShadowForceEffect>() ? 2 : 1;
                    int p = Projectile.NewProjectile(proj.GetSource_FromThis(), proj.Center, velocity, ModContent.ProjectileType<MegaFlameburst>(), (int)(damage * player.ActualClassDamage(DamageClass.Magic)), 4, proj.owner, proj.whoAmI);
                    SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot, proj.Center);

                    proj.ai[1] = 0f;
                }
            }
            return false;
        }
    }
}
