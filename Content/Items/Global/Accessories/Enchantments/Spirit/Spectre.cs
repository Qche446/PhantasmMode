using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit
{
    public class Spectre : PModeGlobalEnchant<SpectreEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(SpectreEffect.SpectreRevive, SpectreReviveFixed);
        }
        private static void SpectreReviveFixed(Action<Player> orig, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            bool spiritForce = modPlayer.ForceEffects.Contains(ModContent.ItemType<SpiritForce>());

            int spiritDamage = 160;
            if (modPlayer.ForceEffect<SpectreEnchant>())
            {
                spiritDamage = 320;
            }
            spiritDamage = (int)(spiritDamage * player.ActualClassDamage(DamageClass.Magic));

            static Projectile[] XWay(int num, IEntitySource spawnSource, Vector2 pos, int type, float speed, int damage, float knockback, int player)
            {
                Projectile[] projs = new Projectile[num];
                double spread = 2 * Math.PI / num;
                for (int i = 0; i < num; i++)
                    projs[i] = FargoSoulsUtil.NewProjectileDirectSafe(spawnSource, pos, new Vector2(speed, speed).RotatedBy(spread * i), type, damage, knockback, player);
                return projs;
            }

            void Revive(int healAmount, int reviveCooldown)
            {
                player.statLife = healAmount;
                player.HealEffect(healAmount);

                player.immune = true;
                player.immuneTime = 120;
                player.hurtCooldowns[0] = 120;
                player.hurtCooldowns[1] = 120;

                int max = player.buffType.Length;
                for (int i = 0; i < max; i++)
                {
                    int timeLeft = player.buffTime[i];
                    if (timeLeft <= 0)
                        continue;

                    int buffType = player.buffType[i];
                    if (buffType <= 0)
                        continue;

                    if (timeLeft > 5
                        && Main.debuff[buffType]
                        && !Main.buffNoTimeDisplay[buffType]
                        && !BuffID.Sets.NurseCannotRemoveDebuff[buffType])
                    {
                        player.DelBuff(i);

                        i--;
                        max--; //just in case, to prevent being stuck here forever
                    }
                }

                if ((PModeChangeApply || player.HasEffectEnchant<SpectreEffect>()) && !modPlayer.TerrariaSoul && !modPlayer.Eternity)
                    modPlayer.SpectreGhostTime = Utilities.SecondsToFrames(5f);

                string text = Language.GetTextValue($"Mods.{FargosPhantasmMode.FargoMod.Name}.Message.Revived");
                CombatText.NewText(player.Hitbox, Color.LightCyan, text, true);
                Main.NewText(text, Color.LightCyan);

                player.AddBuff(ModContent.BuffType<FossilReviveCDBuff>(), reviveCooldown);
            };

            if (modPlayer.Eternity)
            {
                Revive(player.statLifeMax2 / 2 > 300 ? player.statLifeMax2 / 2 : 300, 10800);
                //if (player.HasEffect<SpectreOnHitEffect>())
                //   XWay(30, player.GetSource_Misc("FossilEnchant"), player.Center, ModContent.ProjectileType<FossilBone>(), 15, spiritDamage, 0, player.whoAmI);
            }
            else if (modPlayer.TerrariaSoul)
            {
                Revive(300, 14400);
                //if (player.HasEffect<SpectreOnHitEffect>())
                //    XWay(25, player.GetSource_Misc("FossilEnchant"), player.Center, ModContent.ProjectileType<FossilBone>(), 15, spiritDamage, 0, player.whoAmI);
            }
            else
            {
                bool forceEffect = modPlayer.ForceEffect<SpectreEnchant>();
                Revive(forceEffect ? 200 : 100, 18000);
                if (player.HasEffect<SpectreOnHitEffect>())
                    XWay(forceEffect ? 20 : 10, player.GetSource_EffectItem<SpectreEffect>(), player.Center, ModContent.ProjectileType<SpectreSpirit>(), 15, spiritDamage, 0, player.whoAmI);
            }
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<SpectreAttackEffect>(item);
        }
    }
    public class SpectreAttackEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<SpiritHeader>();
        public override int ToggleItemType => ModContent.ItemType<SpectreEnchant>();
        public override bool ExtraAttackEffect => true;
        public override bool MutantsPresenceAffects => true;
        public override void OnHitNPCEither(Player player, NPC target, NPC.HitInfo hitInfo, DamageClass damageClass, int baseDamage, Projectile projectile, Item item)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (!target.immortal && modPlayer.SpectreCD <= 0 && Main.rand.NextBool())
            {
                bool spectreForceEffect = modPlayer.ForceEffect<SpectreEnchant>();
                if (projectile == null)
                {
                    //forced orb spawn reeeee
                    float num = 4f;
                    float speedX = Main.rand.Next(-100, 101);
                    float speedY = Main.rand.Next(-100, 101);
                    float num2 = (float)Math.Sqrt((double)(speedX * speedX + speedY * speedY));
                    num2 = num / num2;
                    speedX *= num2;
                    speedY *= num2;
                    Projectile p = FargoSoulsUtil.NewProjectileDirectSafe(GetSource_EffectItem(player), target.position, new Vector2(speedX, speedY), ProjectileID.SpectreWrath, hitInfo.Damage / 2, 0, player.whoAmI, target.whoAmI);

                    if ((spectreForceEffect || (hitInfo.Crit && Main.rand.NextBool(5))) && p != null)
                    {
                        SpectreHeal(player, target, p);
                        modPlayer.SpectreCD = spectreForceEffect ? 10 : 30;
                    }
                }
                else if (projectile.type != ProjectileID.SpectreWrath)
                {
                    SpectreHurt(projectile);

                    if (spectreForceEffect || (hitInfo.Crit && Main.rand.NextBool(5)))
                        SpectreHeal(player, target, projectile);

                    modPlayer.SpectreCD = spectreForceEffect ? 10 : 30;
                }
            }
        }
        public void SpectreHeal(Player player, NPC npc, Projectile proj)
        {
            if (npc.canGhostHeal && !player.moonLeech)
            {
                float num = 0.2f;
                num -= proj.numHits * 0.05f;
                if (num <= 0f)
                {
                    return;
                }
                float num2 = proj.damage * num;
                if ((int)num2 <= 0)
                {
                    return;
                }
                if (Main.player[Main.myPlayer].lifeSteal <= 0f)
                {
                    return;
                }
                Main.player[Main.myPlayer].lifeSteal -= num2 * 5; //original damage

                float num3 = 0f;
                int num4 = proj.owner;
                for (int i = 0; i < 255; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead && ((!Main.player[proj.owner].hostile && !Main.player[i].hostile) || Main.player[proj.owner].team == Main.player[i].team))
                    {
                        float num5 = Math.Abs(Main.player[i].position.X + (Main.player[i].width / 2) - proj.position.X + (proj.width / 2)) + Math.Abs(Main.player[i].position.Y + (Main.player[i].height / 2) - proj.position.Y + (proj.height / 2));
                        if (num5 < 1200f && (Main.player[i].statLifeMax2 - Main.player[i].statLife) > num3)
                        {
                            num3 = Main.player[i].statLifeMax2 - Main.player[i].statLife;
                            num4 = i;
                        }
                    }
                }
                Projectile.NewProjectile(proj.GetSource_FromThis(), proj.position.X, proj.position.Y, 0f, 0f, ProjectileID.SpiritHeal, 0, 0f, proj.owner, num4, num2);
            }
        }
        public void SpectreHurt(Projectile proj)
        {
            int num = proj.damage / 2;
            if (proj.damage / 2 <= 1)
            {
                return;
            }
            int num2 = 1000;
            if (Main.LocalPlayer.ghostDmg > (float)num2)
            {
                return;
            }

            Main.LocalPlayer.ghostDmg += (float)num;
            int[] array = new int[200];
            int num3 = 0;
            int num4 = 0;
            for (int i = 0; i < 200; i++)
            {
                if (Main.npc[i].CanBeChasedBy(this, false))
                {
                    float num5 = Math.Abs(Main.npc[i].position.X + (Main.npc[i].width / 2) - proj.position.X + (proj.width / 2)) + Math.Abs(Main.npc[i].position.Y + (Main.npc[i].height / 2) - proj.position.Y + (proj.height / 2));
                    if (num5 < 800f)
                    {
                        if (Collision.CanHit(proj.position, 1, 1, Main.npc[i].position, Main.npc[i].width, Main.npc[i].height) && num5 > 50f)
                        {
                            array[num4] = i;
                            num4++;
                        }
                        else if (num4 == 0)
                        {
                            array[num3] = i;
                            num3++;
                        }
                    }
                }
            }
            if (num3 == 0 && num4 == 0)
            {
                return;
            }
            int num6;
            if (num4 > 0)
            {
                num6 = array[Main.rand.Next(num4)];
            }
            else
            {
                num6 = array[Main.rand.Next(num3)];
            }
            float num7 = 4f;
            float num8 = Main.rand.Next(-100, 101);
            float num9 = Main.rand.Next(-100, 101);
            float num10 = (float)Math.Sqrt((double)(num8 * num8 + num9 * num9));
            num10 = num7 / num10;
            num8 *= num10;
            num9 *= num10;
            Projectile.NewProjectile(proj.GetSource_FromThis(), proj.position.X, proj.position.Y, num8, num9, ProjectileID.SpectreWrath, num, 0f, proj.owner, (float)num6, 0);
        }
    }
}
