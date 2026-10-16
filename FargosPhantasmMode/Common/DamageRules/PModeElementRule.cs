
namespace FargosPhantasmMode.Common.DamageRules
{
    /// <summary>
    /// 元素属性倍率。
    /// <para>
    /// 原先这段算法在 <c>PModeGlobalItem</c> 与 <c>PModeGlobalProj</c> 里各写了一遍：
    /// 从四个布尔字段里取出这一击带的属性，与目标身上对应属性的倍率取最大，再乘进 <c>FinalDamage</c>。
    /// 现在属性以伤害标签的形式挂在弹幕或物品上，倍率只算一次，命中追踪里能看到它改了多少。
    /// </para>
    /// <para>
    /// 目标侧的倍率来源不变，仍是 <c>PModeGlobalBuffNPC</c> 按身上负面状态累加出来的那三个字段。
    /// </para>
    /// </summary>
    public sealed class PModeElementRule : MonoDamageRule
    {
        /// <summary>毒、火、冰三种属性全带。原先 <c>OrdinaryAttributes</c> 的效果就是给这一击挂满三个标签。</summary>
        public const ulong AllAttributes = MonoDamageTag.Poison | MonoDamageTag.Fire | MonoDamageTag.Ice;

        /// <inheritdoc/>
        public override MonoDamageStage Stage => MonoDamageStage.PercentMul;

        /// <inheritdoc/>
        public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values)
        {
            if (!MonoDamageSource.TryTargetNpc(info, out NPC npc))
                return;

            var target = npc.Phantasm();

            // 三个属性取最大，与迁移前的循环等价：倍率只会从 1 往上加，所以起点是 1，
            // 没带标签的属性不参与。
            float result = 1f;
            if (info.HasTag(MonoDamageTag.Poison) && target.PosionMultiplier > result)
                result = target.PosionMultiplier;
            if (info.HasTag(MonoDamageTag.Fire) && target.FireMultiplier > result)
                result = target.FireMultiplier;
            if (info.HasTag(MonoDamageTag.Ice) && target.IceMultiplier > result)
                result = target.IceMultiplier;

            values.DamageMul *= result;
        }
    }
}
