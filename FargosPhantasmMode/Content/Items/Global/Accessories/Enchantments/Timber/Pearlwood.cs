using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using System;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Timber
{
    /// <summary>
    /// 珍珠木附魔：一次命中多掷几次伤害浮动，取最大的那一掷。
    /// <para>
    /// 做法是把 <c>orig</c> 多调几次再取最大值。每次调用就是一次独立的浮动，而整条管线——暴击覆盖、
    /// 防御、穿透、伤害上限——仍由原版自己算，这里不需要知道它们中的任何一个。
    /// </para>
    /// <para>
    /// 原先这里抄了一份 <c>GetDamage</c> 的实现，并用反射读它的私有伤害上限字段来做收尾夹取。
    /// 那份抄本会随原版改动而失真，反射字段名一旦变了则静默失效。
    /// </para>
    /// </summary>
    public class Pearlwood : PModeGlobalEnchant<PearlwoodEnchant>
    {
        public override void Load()
        {
            On_NPC.HitModifiers.GetDamage += HitModifiers_GetDamageReRoll;
        }
        public override void Unload()
        {
            On_NPC.HitModifiers.GetDamage -= HitModifiers_GetDamageReRoll;
        }
        private int HitModifiers_GetDamageReRoll(On_NPC.HitModifiers.orig_GetDamage orig, ref NPC.HitModifiers self, float baseDamage, bool crit, bool damageVariation, float luck)
        {
            int result = orig.Invoke(ref self, baseDamage, crit, damageVariation, luck);

            if (!PModeChangeApply || !damageVariation || !Main.LocalPlayer.HasEffect<PearlwoodEffect>())
                return result;

            int rerolls = Main.LocalPlayer.ForceEffect<PearlwoodEffect>() ? 3 : 1;
            for (int i = 0; i < rerolls; i++)
                result = Math.Max(result, orig.Invoke(ref self, baseDamage, crit, damageVariation, luck));

            return result;
        }
    }
}
