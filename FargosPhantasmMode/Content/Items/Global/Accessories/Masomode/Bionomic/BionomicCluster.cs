using FargosPhantasmMode.Content.Render;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Bionomic
{
    public class BionomicClusterOverride : PModeGlobalMasoItem<BionomicCluster>
    {
        public override bool IsAssembly => true;
        public override void PHExtraTooltipDraw(DrawableTooltipLine line, ref int yOffset)
        {
            TextRender.BurnDraw(line, 0.4f, new Vector2(0, -0.5f), Color.Gray, Color.ForestGreen, Color.IndianRed, Color.Purple);
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            //飞龙之羽
            player.FargoSouls().WyvernBallsCD++;
            player.wingTimeMax += 30;
            player.AddEffect<ClippedWingsAttackEffect>(item);
            //冰霜之握
            player.AddEffect<FrostBurn2AttackEffect>(item);
            //诅咒袋子
            player.AddEffect<ShadowFlameAttackEffect>(item);
            //时之沙
            player.AddEffect<FallingSandsEffect>(item);
            //宁芙香水
            player.FargoSouls().NymphsPerfumeCD -= player.FargoSouls().MasochistSoul ? 10 : 1;
            //蒂姆迷药
            player.manaCost -= 0.05f;
            //神秘头骨
            player.GetDamage(DamageClass.Magic) += 0.05f;
        }
    }
}
