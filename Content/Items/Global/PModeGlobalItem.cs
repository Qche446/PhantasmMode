using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Buffs.Global;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items;
using FargowiltasSouls.Core.Systems;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global
{
    public class PModeGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;
        public bool PoisonAttribute;
        public bool FireAttribute;
        public bool IceAttribute;
        public bool OrdinaryAttributes;
        public override void Load()
        {
            /*
            MethodInfo eModePrefixChanges = typeof(EModeGlobalItem).GetMethod("EModePrefixChanges", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo modifyTooltips = typeof(EModeGlobalItem).GetMethod("ModifyTooltips", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(eModePrefixChanges, ILPrefixChanges);
            MonoModHooks.Modify(modifyTooltips, ILPrefixChanges);
            */
            PhanUtil.AddILHooks(EModeGlobalItem.EModePrefixChanges, ILPrefixChanges);
            PhanUtil.AddILHooks(ModContent.GetInstance<EModeGlobalItem>().ModifyTooltips, ILPrefixChanges);
        }

        const float PModeViolentBaseAttackSpeed = 0.01f;
        private void ILPrefixChanges(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(EModeGlobalItem.newViolentBaseAttackSpeed)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return PModeWorldSavingSystem.PhantasmMode ? PModeViolentBaseAttackSpeed : EModeGlobalItem.newViolentBaseAttackSpeed;
            });
        }
        public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            //属性附加（）
            if (item.IsWeapon())
            {
                var Pgm = item.GetGlobalItem<PModeGlobalItem>();
                var Pbn = target.GetGlobalNPC<PModeGlobalBuffNPC>();
                List<bool> Attributes = [Pgm.PoisonAttribute, Pgm.FireAttribute, Pgm.IceAttribute];
                List<float> Multiplier = [Pbn.PosionMultiplier, Pbn.FireMultiplier, Pbn.IceMultiplier];
                float result = 1f;
                for (int i = 0; i < Attributes.Count; i++)
                {
                    if ((OrdinaryAttributes || Attributes[i]) && Multiplier[i] > result)
                        result = Multiplier[i];
                }
                modifiers.FinalDamage *= result;
            }
        }
        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(item, player, target, hit, damageDone);
            PoisonAttribute = false;
            FireAttribute = false;
            IceAttribute = false;
            OrdinaryAttributes = false;
        }
    }
}
