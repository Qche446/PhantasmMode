using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.SupremeDeath;
using FargosPhantasmMode.Content.Projectiles.Masomode;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist
{
    public class BetsysHeartOverride : PModeGlobalMasoItem<BetsysHeart>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<FargoSoulsPlayer>().SpecialDashKey, SpecialDashFixed);
        }
        private static void SpecialDashFixed(Action<FargoSoulsPlayer, int> orig, FargoSoulsPlayer self, int type)
        {
            Player py = Main.LocalPlayer;
            FargoSoulsPlayer fp = py.FargoSouls();
            bool maso = PModeChangeApply;
            if (fp.SpecialDashCD <= 0)
            {
                fp.SpecialDashCD = 5 * 60;

                if (py.whoAmI == Main.myPlayer)
                {
                    py.RemoveAllGrapplingHooks();

                    /*Player.controlLeft = false;
                    Player.controlRight = false;
                    Player.controlJump = false;
                    Player.controlDown = false;*/
                    py.controlUseItem = false;
                    py.controlUseTile = false;
                    py.controlHook = false;
                    //Player.controlMount = false;

                    py.itemAnimation = 0;
                    py.itemTime = 0;
                    py.reuseDelay = 0;

                    if (py.HasEffect<BetsyDashEffect>() && type == 2)
                    {
                        Vector2 vel = py.SafeDirectionTo(Main.MouseWorld) * 25;
                        Projectile.NewProjectile(py.GetSource_Accessory(fp.BetsysHeartItem), py.Center, vel, ModContent.ProjectileType<BetsyDash>(), (int)(100 * py.ActualClassDamage(DamageClass.Melee)), 6f, py.whoAmI);
                        if (maso)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                int damage = 60;
                                if (py.FargoSouls().MasochistSoul)
                                {
                                    damage *= 10;
                                }
                                else if (py.FargoSouls().MasochistHeart)
                                {
                                    damage *= 2;
                                }
                                Projectile.NewProjectile(py.GetSource_Accessory(fp.BetsysHeartItem), py.Center, Main.rand.NextFloat(1.4f, 2.6f) * vel.RotatedByRandom(MathHelper.Pi / 8f), ModContent.ProjectileType<BetsysHeartPhoenix>(), (int)(damage * py.ActualClassDamage(DamageClass.Melee)), 6f, py.whoAmI);
                            }
                        }
                        py.immune = true;
                        py.immuneTime = Math.Max(py.immuneTime, 2);
                        py.hurtCooldowns[0] = Math.Max(py.hurtCooldowns[0], 2);
                        py.hurtCooldowns[1] = Math.Max(py.hurtCooldowns[1], 2);

                        CooldownBarManager.Activate("SpecialDashCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Masomode/BetsysHeart").Value, Color.OrangeRed,
                            () => 1 - fp.SpecialDashCD / (5f * 60f), activeFunction: () => fp.BetsysHeartItem != null);
                    }
                    else if (py.HasEffect<SpecialDashEffect>() && type == 0)
                    {
                        fp.SpecialDashCD += 60;

                        Vector2 vel = py.SafeDirectionTo(Main.MouseWorld) * 20;
                        int p = Projectile.NewProjectile(py.GetSource_Accessory(fp.QueenStingerItem), py.Center, vel, ModContent.ProjectileType<BeeDash>(), (int)(44 * py.ActualClassDamage(DamageClass.Melee)), 6f, py.whoAmI);
                        if (maso)
                        {
                            var bp = py.GetModPlayer<BeeDashTimerPlayer>();
                            bp.CheckNohitTimer = 20;
                            bp.TrueDashTime = 20;
                        }
                        CooldownBarManager.Activate("SpecialDashCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Masomode/QueenStinger").Value, Color.Yellow,
                            () => 1 - fp.SpecialDashCD / (6f * 60f), activeFunction: () => fp.QueenStingerItem != null);
                    }

                    py.AddBuff(ModContent.BuffType<BetsyDashBuff>(), 20);
                }
            }
        }
        /*
        private void ILSpecialDash(ILContext il)
        {
            ILCursor c = new(il);
            c.Goto(0);
            c.RemoveRange(c.Instrs.Count);
            il.Body.ExceptionHandlers.Clear();
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<int>>((type) =>
            {
                Player py = Main.LocalPlayer;
                FargoSoulsPlayer fp = py.FargoSouls();
                bool maso = WorldSavingSystem.masochistModeReal;
                if (fp.SpecialDashCD <= 0)
                {
                    fp.SpecialDashCD = 5 * 60;

                    if (py.whoAmI == Main.myPlayer)
                    {
                        py.RemoveAllGrapplingHooks();

                        py.controlUseItem = false;
                        py.controlUseTile = false;
                        py.controlHook = false;
                        //Player.controlMount = false;

                        py.itemAnimation = 0;
                        py.itemTime = 0;
                        py.reuseDelay = 0;

                        if (py.HasEffect<BetsyDashEffect>() && type == 2)
                        {
                            Vector2 vel = py.SafeDirectionTo(Main.MouseWorld) * 25;
                            Projectile.NewProjectile(py.GetSource_Accessory(fp.BetsysHeartItem), py.Center, vel, ModContent.ProjectileType<BetsyDash>(), (int)(100 * py.ActualClassDamage(DamageClass.Melee)), 6f, py.whoAmI);
                            if (maso)
                            {
                                for (int i = 0; i < 12; i++)
                                {
                                    int damage = 60;
                                    if (py.FargoSouls().MasochistSoul)
                                    {
                                        damage *= 10;
                                    }
                                    else if (py.FargoSouls().MasochistHeart)
                                    {
                                        damage *= 2;
                                    }
                                    Projectile.NewProjectile(py.GetSource_Accessory(fp.BetsysHeartItem), py.Center, Main.rand.NextFloat(1.4f, 2.6f) * vel.RotatedByRandom(MathHelper.Pi / 8f), ModContent.ProjectileType<BetsysHeartPhoenix>(), (int)(damage * py.ActualClassDamage(DamageClass.Melee)), 6f, py.whoAmI);
                                }
                            }
                            py.immune = true;
                            py.immuneTime = Math.Max(py.immuneTime, 2);
                            py.hurtCooldowns[0] = Math.Max(py.hurtCooldowns[0], 2);
                            py.hurtCooldowns[1] = Math.Max(py.hurtCooldowns[1], 2);

                            CooldownBarManager.Activate("SpecialDashCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Masomode/BetsysHeart").Value, Color.OrangeRed,
                                () => 1 - fp.SpecialDashCD / (5f * 60f), activeFunction: () => fp.BetsysHeartItem != null);
                        }
                        else if (py.HasEffect<SpecialDashEffect>() && type == 0)
                        {
                            fp.SpecialDashCD += 60;

                            Vector2 vel = py.SafeDirectionTo(Main.MouseWorld) * 20;
                            int p = Projectile.NewProjectile(py.GetSource_Accessory(fp.QueenStingerItem), py.Center, vel, ModContent.ProjectileType<BeeDash>(), (int)(44 * py.ActualClassDamage(DamageClass.Melee)), 6f, py.whoAmI);
                            if (maso)
                            {
                                var bp = py.GetModPlayer<BeeDashTimerPlayer>();
                                bp.CheckNohitTimer = 20;
                                bp.TrueDashTime = 20;
                            }
                            CooldownBarManager.Activate("SpecialDashCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Masomode/QueenStinger").Value, Color.Yellow,
                                () => 1 - fp.SpecialDashCD / (6f * 60f), activeFunction: () => fp.QueenStingerItem != null);
                        }

                        py.AddBuff(ModContent.BuffType<BetsyDashBuff>(), 20);
                    }
                }
            });
            c.Emit(OpCodes.Ret);
        }
        */
    }

}
