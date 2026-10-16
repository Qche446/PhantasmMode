using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit;
using FargosPhantasmMode.Content.Rarities;
using FargosPhantasmMode.Content.Render;
using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using Terraria.Localization;
using Monochrome.Common.MonoUtil;

namespace FargosPhantasmMode.Content.Items.Global
{
    public class PModeGlobalItem : GlobalItem
    {
        public bool Phantasm = false;
        public override bool InstancePerEntity => true;
        public override void Load()
        {
            //PhanUtil.AddILHooks(EModeGlobalItem.EModePrefixChanges, ILPrefixChanges);
            MethodInfo method = typeof(EModeGlobalItem).GetMethod("EModePrefixChanges", BindingFlags.Static | BindingFlags.NonPublic);
            MonoModHooks.Modify(method, ILPrefixChanges);
            PhanUtil.AddILHooks(ModContent.GetInstance<EModeGlobalItem>().ModifyTooltips, ILPrefixChanges);
        }

        const float PModeViolentBaseAttackSpeed = 0.01f;
        private void ILPrefixChanges(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.005f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return PModeWorldSavingSystem.PhantasmMode ? PModeViolentBaseAttackSpeed : 0.005f;
            });
        }
        public override void OnConsumeItem(Item item, Player player)
        {
            if (!PModeWorldSavingSystem.PhantasmMode)
                return;
            if (item.healLife > 0 && item.potion)
            {
                if (player.HasEffect<HallowFlameEffect>())
                {
                    player.AddBuff(ModContent.BuffType<HallowFlameBuff>(), 45 * 60);
                }
            }
        }
        public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            //忍者物品减伤
            if (player.HasEffect<NinjaAttackSpeedEffect>())
            {
                bool hasForce = player.ForceEffect<NinjaAttackSpeedEffect>();
                if (item == player.HeldItem || item == player.HeldMouseItem())
                {
                    modifiers.FinalDamage *= hasForce ? 0.333f : 0.5f;
                }
            }
        }
        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            item.MonoTag().Clear();
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (Phantasm)
                tooltips.Add(new TooltipLine(Mod, $"{Mod.Name}:Phantasm", Language.GetTextValue("Mods.FargosPhantasmMode.Items.Extra.Phantasm")));
        }
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (Phantasm)
            {
                if (line.Name == $"{Mod.Name}:Phantasm")
                    return !PhantomTextRender.Draw(line, PhantomTextRender.Cyan, PhantomTextRender.Blue);
                if (line.Name == "ItemName")
                {
                    if (item.ModItem is IPhantomRender pr)
                        return !PhantomTextRender.Draw(line, pr.Color1, pr.Color2);
                    else
                        return !PhantomTextRender.Draw(line, Color.Aqua, Color.WhiteSmoke);
                }

            }
            //稀有度
            if (line.Name == "ItemName")
            {
                if (item.rare == ModContent.RarityType<SupremeDeathRarity>())
                    return !SupremeDeathRarity.RenderTooltip(line);
                if (item.rare == ModContent.RarityType<MechRarity>())
                    return !MechRarity.RenderTooltip(line);
               
            }
            return true;

        }
    }
}
