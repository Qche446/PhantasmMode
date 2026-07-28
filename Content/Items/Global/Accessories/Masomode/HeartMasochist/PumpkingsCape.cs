using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist
{
    public class PumpkingsCapeOverride : PModeGlobalMasoItem<PumpkingsCape>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<FargoSoulsPlayer>().RaisedShieldEffects, RottingDistanceEnhance);
        }
        private static void RottingDistanceEnhance(Action<FargoSoulsPlayer> orig, FargoSoulsPlayer self)
        {
            FargoSoulsPlayer fp = self;
            Player py = fp.Player;
            bool maso = PModeChangeApply;
            bool silverEffect = py.HasEffect<SilverEffect>();
            bool dreadEffect = py.HasEffect<DreadShellEffect>();
            bool pumpkingEffect = py.HasEffect<PumpkingsCapeEffect>();
            if (dreadEffect)
            {
                if (!fp.MasochistSoul)
                    fp.DreadShellVulnerabilityTimer = 60;
            }

            if (pumpkingEffect) //strong aura effect
            {
                float distance = maso ? 600f : 300f;
                for (int i = 0; i < Main.maxNPCs; i++)
                    if (Main.npc[i].active && !Main.npc[i].friendly && Main.npc[i].Distance(py.Center) < distance)
                        Main.npc[i].AddBuff(ModContent.BuffType<RottingBuff>(), 600);

                for (int i = 0; i < (maso ? 40 : 20); i++)
                {
                    Vector2 offset = new();
                    double angle = Main.rand.NextDouble() * 2d * Math.PI;
                    offset.X += (float)(Math.Sin(angle) * distance);
                    offset.Y += (float)(Math.Cos(angle) * distance);
                    Dust dust = Main.dust[Dust.NewDust(py.Center + offset - new Vector2(4, 4), 0, 0, DustID.Ice_Pink, 0, 0, 100, Color.White, 1f)];
                    dust.velocity = py.velocity;
                    if (Main.rand.NextBool(3))
                        dust.velocity += Vector2.Normalize(offset) * -(maso ? 8f : 5f);
                    dust.noGravity = true;
                }
            }
            //maso下取消举盾减速
            if ((dreadEffect || pumpkingEffect) && !silverEffect && !maso)
            {
                py.velocity.X *= 0.85f;
                if (py.velocity.Y < 0)
                    py.velocity.Y *= 0.85f;
            }
            int cooldown = FargoSoulsPlayer.ShieldCooldown(py);

            if (fp.shieldCD < cooldown)
                fp.shieldCD = cooldown;
        }
    }
}
