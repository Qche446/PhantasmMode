using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs.Global
{
    public class PModeBuffPlayer : ModPlayer
    {
        public bool Sublimation = false;
        public static bool PModeChangdeApply => PModeWorldSavingSystem.PhantasmMode;
        public override void ResetEffects()
        {
            Sublimation = false;
        }
        public override void UpdateBadLifeRegen()
        {
            float dotMu = 1;
            if (Player.FargoSouls().Oiled)
                dotMu *= 1.5f;
            void DamageOverTime(int badLifeRegen, bool affectLifeRegenCount = false)
            {
                if (Player.lifeRegen > 0)
                    Player.lifeRegen = 0;

                if (affectLifeRegenCount && Player.lifeRegenCount > 0)
                    Player.lifeRegen = 0;

                Player.lifeRegenTime = 0;
                Player.lifeRegen -= (int)(badLifeRegen * dotMu);
            }
            if (Sublimation)
                DamageOverTime(50, false);
            if (PModeChangdeApply && Player.ForceEffect<OrichalcumEffect>() && Player.lifeRegen < 0 && !Player.HasEffect<EarthForceEffect>())
                Player.lifeRegen = (int)(Player.lifeRegen * 1.2f);
            if (Player.HasEffect<LeadEffect>() && Player.lifeRegen < 0)
            {
                float mul = Player.ForceEffect<LeadEffect>() ? 0.6f : 0.8f;
                if (!PModeChangdeApply)
                    mul -= 0.2f;
                Player.lifeRegen = (int)(Player.lifeRegen * mul);
            }
            if (PModeChangdeApply && Player.HasEffect<PalladiumHealing>() && Player.HasBuff(BuffID.RapidHealing))
            {
                int ReduceNum = Player.ForceEffect<PalladiumHealing>() ? 18 : 8;
                if (Player.lifeRegen < -ReduceNum)
                    Player.lifeRegen += ReduceNum;
                else if (Player.lifeRegen < 0)
                    Player.lifeRegen = 0;
            }

        }
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (Sublimation)
            {
                if (Main.rand.NextBool(4))
                {
                    int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.PortalBolt, Player.velocity.X * 0.4f, Player.velocity.Y * 0.4f, 0, new Color(220, 255, 220), 2.5f);
                    Main.dust[d].velocity.Y -= 1;
                    Main.dust[d].velocity *= 1.5f;
                    Main.dust[d].noGravity = true;
                }
            }
        }
    }
}
