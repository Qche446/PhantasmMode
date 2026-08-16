using FargowiltasSouls.Content.Items.BossBags;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using SteelSeries.GameSense;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bossbar
{
    public static class BossBarRender
    {
        public static int BarLength => 600;
        public static int BarHeight => 20;
        public static int TextOffset => 15;
        public static void DrawCustomBossBar(SpriteBatch sb, NPC npc, Vector2 center)
        {
            if (npc == null)
                return;
            //Vector2 center = new(Main.screenWidth / 2f, Main.screenHeight - 70);
            Rectangle barRectangle = Utils.CenteredRectangle(center, new (BarLength, BarHeight));
            sb.PrepareForShaders(null, true);
            var barconfig = BossBarRegistry.GetBossBarConfig(npc.type);
            barconfig?.drawBossBarMethod?.Invoke(sb, npc, barRectangle);
            int Shield = barconfig.Shield?.Invoke(npc) ?? 0;
            int MaxShield = barconfig.MaxShield?.Invoke(npc) ?? 0;
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            if (barconfig.HasShield && Shield != 0)
                DrawShieldText(sb, Shield, MaxShield, barRectangle);
            else
                DrawHealthText(sb, npc, barRectangle);
            DrawBossName(sb, npc, barRectangle);
            //DrawBossIcon(sb, npc, barRectangle);
            Main.spriteBatch.ResetToDefaultUI();
        }
        private static void DrawBossIcon(SpriteBatch sb, NPC npc, Rectangle barrectangle)
        {
            int headTextureIndex = NPCID.Sets.BossHeadTextures[npc.type];
            if (headTextureIndex == -1)
            {
                NPCLoader.BossHeadSlot(npc, ref headTextureIndex);
                if (headTextureIndex == -1)
                    return;
            }
            Texture2D iconTexture = TextureAssets.NpcHeadBoss[headTextureIndex].Value;
            Vector2 iconCenter = barrectangle.Center.ToVector2() + new Vector2(0, -TextOffset - 5);
            Vector2 iconSize = new(26f, 28f);
            Rectangle iconrectangle = Utils.CenteredRectangle(iconCenter, iconSize);
            sb.Draw(iconTexture, iconCenter, null, Color.White, 0, iconSize / 2, 1, 0, 0);
        }
        private static void DrawBossName(SpriteBatch sb, NPC npc, Rectangle area)
        {
            string bossName = npc.FullName;
            Vector2 vector = area.Center.ToVector2();
            vector.X -= 190;
            vector.Y += TextOffset;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 vector2 = font.MeasureString(bossName);
            Utils.DrawBorderStringFourWay(sb, font, bossName, vector.X, vector.Y, Color.White, Color.Transparent, vector2 / 2f, 0.9f);
        }
        private static void DrawHealthText(SpriteBatch spriteBatch, NPC npc, Rectangle area)
        {
            DynamicSpriteFont value = FontAssets.MouseText.Value;
            Vector2 vector = area.Center.ToVector2();
            vector.X += 10;
            vector.Y += TextOffset;
            long totalLife = npc.life;
            long totalMaxlife = npc.lifeMax;
            if (npc.type == NPCID.EaterofWorldsHead && TryGetEaterOfWorldsChainLife(npc, out long life, out long maxLife))
            {
                totalLife = life;
                totalMaxlife = maxLife;
            }
            double lifePercentage = Math.Round(100 * totalLife / (double)totalMaxlife, 2);
            string strliferatio = lifePercentage.ToString();
            if (lifePercentage % 1 == 0)
                strliferatio += ".00";
            else if (lifePercentage * 10 % 1 == 0)
                strliferatio += "0";
            strliferatio += "%";
            string healthtext = $"{totalLife}/{totalMaxlife}   " + strliferatio;
            Vector2 vector2 = value.MeasureString(healthtext);
            Utils.DrawBorderStringFourWay(spriteBatch, value, healthtext, vector.X, vector.Y, Color.White, Color.Transparent, vector2 / 2f, 0.80f);
        }
        private static void DrawShieldText(SpriteBatch sb, int shield, int maxShield, Rectangle area)
        {
            DynamicSpriteFont value = FontAssets.MouseText.Value;
            Vector2 vector = area.Center.ToVector2();
            vector.X += 10;
            vector.Y += TextOffset;
            string healthtext = $"{shield} / {maxShield}";
            Vector2 vector2 = value.MeasureString(healthtext);
            Utils.DrawBorderStringFourWay(sb, value, healthtext, vector.X, vector.Y, Color.White, Color.Transparent, vector2 / 2f, 0.80f);
        }
        public static bool TryGetEaterOfWorldsChainLife(NPC head, out long life, out long maxLife)
        {
            life = 0L;
            maxLife = 0L;

            if (head is null || !head.active || head.type != NPCID.EaterofWorldsHead)
            {
                return false;
            }
            Span<byte> visited = stackalloc byte[Main.maxNPCs];
            visited.Clear();

            NPC segment = head;

            for (int count = 0; count < Main.maxNPCs; count++)
            {
                int index = segment.whoAmI;

                if ((uint)index >= (uint)Main.maxNPCs || visited[index] != 0)
                    break;

                visited[index] = 1;
                life += Math.Max(segment.life, 0);
                maxLife += Math.Max(segment.lifeMax, 0);

                if (segment.type == NPCID.EaterofWorldsTail)
                    break;

                int nextIndex = (int)segment.ai[0];
                if ((uint)nextIndex >= (uint)Main.maxNPCs)
                    break;

                NPC next = Main.npc[nextIndex];

                if (!next.active ||
                    (next.type != NPCID.EaterofWorldsBody &&
                     next.type != NPCID.EaterofWorldsTail) ||
                    (int)next.ai[1] != segment.whoAmI)
                {
                    break;
                }
                segment = next;
            }

            return maxLife > 0L;
        }
    }
}
