using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bossbar
{
    public class PhantasmBossBarStyle : ModBossBarStyle
    {
        public override string DisplayName => "Phantasm";
        public override bool PreventDraw => true;
        public override void Draw(SpriteBatch spriteBatch, IBigProgressBar currentBar, BigProgressBarInfo info)
        {
            DrawMultipleBossBars(spriteBatch);
        }
        private static void DrawMultipleBossBars(SpriteBatch spriteBatch)
        {
            List<BossBarData> bosses = MultiBossBarSystem.GetActiveBosses();
            if (bosses.Count == 0)
            {
                return;
            }
            int baseY = Main.screenHeight - 45;
            for (int i = 0; i < bosses.Count; i++)
            {
                NPC boss = bosses[i].GetNPC();
                if (boss != null && boss.active)
                {
                    int yPos = baseY - i * 50;
                    int xPos = Main.screenWidth / 2;
                    BossBarRender.DrawCustomBossBar(spriteBatch, boss, new(xPos, yPos));
                }
            }
        }
    }
}
