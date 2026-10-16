using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Content.Items.Weapons;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using Monochrome.Content.Status;

namespace FargosPhantasmMode.Common.DamageRules
{
    /// <summary>
    /// jw的易伤减伤buff伤害规则
    /// </summary>
    public sealed class PModeBuffDamageRule : MonoDamageRule
    {
        /// <summary>圣炎的易伤系数。</summary>
        private const float HallowFlameRate = 0.01f;

        /// <summary>目标就是突变体时圣炎的易伤系数。</summary>
        private const float HallowFlameMutantRate = 0.005f;

        /// <inheritdoc/>
        public override string Name => "状态受击修正";

        /// <inheritdoc/>
        public override MonoDamageStage Stage => MonoDamageStage.Defense;

        /// <inheritdoc/>
        public override bool AppliesTo(in MonoDamageInfo info) => !info.AgainstPlayer;

        /// <inheritdoc/>
        public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values)
        {
            if (!MonoDamageSource.TryTargetNpc(info, out NPC npc))
                return;

            var buffs = npc.Phantasm();
            MonoBuffSet set = npc.MonoBuffs();
            // 海洋重击：削护甲。
            if (npc.FargoSouls().OceanicMaul)
                values.DefenseFlat -= 20f;

            // 升华：削护甲。
            if (buffs.Sublimation)
                values.DefenseFlat -= 15f;

            // 失温：易伤。
            if (buffs.Hypothermia)
                values.DamageMul *= 1.05f;

            // 圣炎：按层数削护甲并易伤。
            int hallowFlame = HallowFlameBuff.Level(set.Data<HallowFlameBuff>());
            if (hallowFlame > 0)
            {
                float rate = npc.type == ModContent.NPCType<MutantBoss>() ? HallowFlameMutantRate : HallowFlameRate;
                values.DefenseFlat -= 4f * hallowFlame;
                values.DamageMul *= 1f + rate * hallowFlame;
            }
            //猎杀标记
            int hunterMark = HunterMark.Level(set.Data<HunterMark>());
            if (hunterMark > 0)
            {
                values.ReductionScale *= 0.8f;
            }
            //击溃，取消所有防御能力
            if (buffs.CrushBuff)
            {
                values.ReductionScale *= 0;
                values.DefenseMul *= 0;
            }
            //if (info.SourceKind == MonoDamageSourceKind.Projectile && info.SourceIndex == ModContent.ProjectileType<RendingSpiral_White>() && hunterMark > 0)
                //values.DamageMul *= hunterMark * 2f;
        }
    }
}
