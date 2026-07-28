using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Render;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist
{
    public class HeartoftheMasochistOverride : PModeGlobalMasoItem<HeartoftheMasochist>
    {
        public override bool IsAssembly => true;
        public override void PHExtraTooltipDraw(DrawableTooltipLine line, ref int yOffset)
        {
            TextRender.BurnDraw(line, 0.5f, new Vector2(0.2f, -0.5f), new Color(40, 5, 0), new Color(220, 80, 0), new Color(255, 180, 20), new Color(255, 240, 150)); 
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
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
        public override void SafeUpdateInPack(Item item, Player player)
        {
            player.buffImmune[BuffID.VortexDebuff] = true;
        }
        public override void Load()
        {
            PhanUtil.AddILHooks(ModContent.GetInstance<FargoSoulsPlayer>().PostUpdateMiscEffects, ILDeactivatedMinionEffect);
        }
        private void ILDeactivatedMinionEffect(ILContext il)
        {
            ILCursor c = new(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0.01f)))
                throw new Exception("IL edit failed!");
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() =>
            {
                return PModeChangeApply ? 0.03f : 0.01f;
            });
        }
    }
}
