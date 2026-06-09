using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using Terraria.ID;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.ModPlayers;
using MonoMod.Cil;
using System.Reflection;
using System;
using Mono.Cecil.Cil;
using FargowiltasSouls;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using FargosPhantasmMode.Assets.ExtraTextures;
using FargosPhantasmMode.Content.Render;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class HeartoftheMasochistOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<HeartoftheMasochist>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var baseLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.HeartoftheMasochist.Base"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(baseLine);
                var extraLine = new TooltipLine(Mod, "PHAddTooltipsExtra", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.HeartoftheMasochist.Extra"));
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                if (line.Name == "PHAddTooltipsExtra")
                {
                    TextRender.BurnDraw(line, 0.5f, new Vector2(0.2f, -0.5f), new Color(40, 5, 0), new Color(220, 80, 0), new Color(255, 180, 20), new Color(255, 240, 150));
                    //TextRender.FlameParticleManager.SpawnFlameParticles(line, scale: 1f, intensity: 0.9f);
                    return false;
                }
            }
            return true;
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                //南瓜王:格挡相关腐败圈
                //冰炸弹：失温症
                //飞碟控制器：免疫扭曲
                //龙心：额外弹幕
                //突变抗体
                player.AddEffect<OceanicMaulAttackEffect>(item);
                player.FargoSouls().QueenStingerItem = item;
                //玲珑：0.8缩判
                //月抛杯子
                player.wingTimeMax = 999999;
                player.wingTime = player.wingTimeMax;
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (WorldSavingSystem.masochistModeReal)
                player.buffImmune[BuffID.VortexDebuff] = true;
        }

        public override void UpdateVanity(Item item, Player player)
        {
            if (WorldSavingSystem.masochistModeReal)
                player.buffImmune[BuffID.VortexDebuff] = true;
        }
    }
    public class DeactivatedMinionEffectModSystem : ModSystem
    {
        public override void Load()
        {
            MethodInfo method1 = typeof(FargoSoulsPlayer).GetMethod("PostUpdateMiscEffects", BindingFlags.Instance | BindingFlags.Public);
            MonoModHooks.Modify(method1, ILDeactivatedMinionEffect);
        }
        private void ILDeactivatedMinionEffect(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.01f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<float>>(() =>
            {
                return WorldSavingSystem.masochistModeReal ? 0.03f : 0.01f;
            });
        }
    }
}
