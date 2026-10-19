using FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins;
using Monochrome.Common.MonoUtil.Damage;
using Monochrome.Content.Damage;
using Monochrome.Content.Damage.Rules;

using Monochrome.Common.MonoUtil;
using FargosPhantasmMode.Content.Bosses.Coffin;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
namespace FargosPhantasmMode.Common.DamageRules
{
    /// <summary>
    /// 把 FPM 的伤害规则登记进 Monochrome 的规则表。在 <c>FargosPhantasmMode.Load</c> 里调一次。
    /// <para>
    /// 这里只做登记，规则本身在同目录的各个文件里。命中时规则表按段号与次序跑，每条规则在追踪里
    /// 各占一行——这是把它们从各自的钩子搬过来的主要收益。
    /// </para>
    /// </summary>
    public static class PModeDamageRules
    {
        /// <summary>登记全部规则。</summary>
        public static void Register()
        {
            MonoDamage.Register(new PModeElementRule());

            // 史莱姆王与克苏鲁之眼：对荆棘旋刃的减伤。
            MonoDamage.Register(new MonoConditionalReductionRule(0.25f,
                when: IsThornChakram, targetType: NPCID.KingSlime));

            MonoDamage.Register(new MonoConditionalReductionRule(0.15f,
                when: IsThornChakram, targetType: NPCID.EyeofCthulhu));

            //棺材
            MonoDamage.Register(new MonoConditionalReductionRule(0.75f, 
                when: CoffinPhaseChange, 
                targetType: ModContent.NPCType<CursedCoffin>()));

            // 双子：死亡射线与滚动射击期间各自减伤一半。
            MonoDamage.Register(new MonoConditionalReductionRule(0.5f,
                when: static info => TwinsInState<P_Retinazer>(info, TwinsAtt.CurvedDeathRay),
                targetType: NPCID.Retinazer));

            MonoDamage.Register(new MonoConditionalReductionRule(0.5f,
                when: static info => TwinsInState<P_Spazmatism>(info, TwinsAtt.RollingShoot),
                targetType: NPCID.Spazmatism));
            // 状态带来的破甲与易伤都在这一条里，加新效果直接改 PModeBuffDamageRule。
            MonoDamage.Register(new PModeBuffDamageRule());
        }

        /// <summary>来源是不是荆棘旋刃。</summary>
        private static bool IsThornChakram(MonoDamageInfo info)
            => MonoDamageSource.TrySourceProjectile(info, out Projectile projectile)
               && projectile.type == ProjectileID.ThornChakram;

        /// <summary>目标双子是不是处在指定状态。</summary>
        private static bool TwinsInState<T>(MonoDamageInfo info, TwinsAtt state)
            where T : GlobalNPC, IPTwins
            => MonoDamageSource.TryTargetNpc(info, out NPC npc)
               && npc.GetGlobalNPC<T>() is IPTwins twins
               && twins.AIState == state;

        ///棺材是否在阶段转换状态
        private static bool CoffinPhaseChange(MonoDamageInfo info)
            => MonoDamageSource.TryTargetNpc(info, out NPC npc) && npc.TryGetGlobalNPC<P_CursedCoffin>(out var p) &&
            (p.Fsm.Current == P_CursedCoffin.State.PhaseTransition1st || p.Fsm.Current == P_CursedCoffin.State.PhaseTransition2nd);
    }
}
