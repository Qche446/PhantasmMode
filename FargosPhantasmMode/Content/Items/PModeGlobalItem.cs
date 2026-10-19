using FargosPhantasmMode.Common;
using FargosPhantasmMode.Common.ChatTag;
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
using Monochrome.Common.MonoUtil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI.Chat;

namespace FargosPhantasmMode.Content.Items
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
            AddBuffInfomation(item, tooltips);
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
        private void AddBuffInfomation(Item item, List<TooltipLine> tooltips)
        {
            var buffIdsInTooltip = new HashSet<int>();
            foreach (var tooltip in tooltips)
            {
                var snippets = ChatManager.ParseMessage(tooltip.Text, Color.White);
                foreach (var snippet in snippets)
                {
                    if (snippet is PModeBuffTag.Snippet buffSnippet)
                    {
                        buffIdsInTooltip.Add(buffSnippet.BuffId);
                    }
                }
            }
            if (buffIdsInTooltip.Count > 0)
            {
                bool showTheTip = false;
                bool foundDebuff = false;
                foreach (int buffId in buffIdsInTooltip)
                {
                    string tooltipKey = "";
                    if (buffId < BuffID.Count)
                    {
                        tooltipKey = $"Mods.Terraria.Buffs.{BuffID.Search.GetName(buffId)}.ItemTooltip";
                    }
                    else
                    {
                        //模组buff
                        var modBuff = BuffLoader.GetBuff(buffId);
                        tooltipKey = $"Mods.{modBuff.Mod.Name}.Buffs.{modBuff.Name}.ItemTooltip";
                    }

                    if (!Language.Exists(tooltipKey))
                    {
                        continue;
                    }

                    var text = Language.GetTextValue(tooltipKey);
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    foundDebuff = true;
                    if (!PlayerInput.Triggers.Current.SmartCursor)
                    {
                        showTheTip = true;
                        break;
                    }

                    tooltips.Add(new TooltipLine(Mod, "FPM:AltExpandTooltip" + buffId, $"[pbuff:{buffId}]\n{text}"));
                }

                if (showTheTip)
                {
                    var str = PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus["SmartCursor"].First().ToString();
                    tooltips.Add((new TooltipLine(Mod, "FPM:AltExpandTooltip", Language.GetTextValue("Mods.FargosPhantasmMode.UI.AltExpand").Replace("{0}", str))));
                    tooltips[^1].OverrideColor = new Color(170, 170, 170);
                }
                else if (foundDebuff)
                {
                    foreach (var item1 in tooltips)
                    {
                        if (item1.Name.Contains("Tooltip") && !item1.Name.Contains("AltExpandTooltip"))
                            item1.Hide();
                    }
                }
            }
        }
    }
}
