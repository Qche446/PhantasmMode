using FargosPhantasmMode.Content.Items.Accessories;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Core.Players
{
    public partial class PmodePlayer : ModPlayer
    {
        public static bool PModeChangdeApply => PModeWorldSavingSystem.PhantasmMode;
        public bool DotCrit = false;
        public bool DotCanDie = false;
        public bool MouseRight => Player.altFunctionUse == 2;
        public bool LungingDown = false;
        public override void ResetEffects()
        {
            DotCrit = false;
            DotCanDie = false;
            BuffReset();
        }
        public override void PostUpdateEquips()
        {
            BuffPostUpdateEquips();
        }
        public override void PostUpdateMiscEffects()
        {
            if (LungingDown)
                Player.maxFallSpeed = 80;
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
            if (PrayerofTaintedBlood)
                DamageOverTime(25, false);


            if (PModeChangdeApply && Player.ForceEffect<OrichalcumEffect>() && Player.lifeRegen < 0 && !Player.HasEffect<EarthForceEffect>())
                Player.lifeRegen = (int)(Player.lifeRegen * 1.2f);
            if (Player.HasEffect<GermDotEffect>() && Player.lifeRegen < 0)
                Player.lifeRegen = (int)(Player.lifeRegen * 1.5f);
            if (Player.HasEffect<LeadEffect>() && Player.lifeRegen < 0)
            {
                float mul = Player.ForceEffect<LeadEffect>() ? 0.4f : 0.6f;
                if (PModeChangdeApply)
                    mul += 0.1f;
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

            if (Player.statLife < 5 && Player.lifeRegen < 0 && !DotCanDie)
                Player.lifeRegen = 0;

        }
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            DrawBuffEffect(drawInfo, ref r, ref g, ref b, ref a, ref fullBright);
        }
    }
}
