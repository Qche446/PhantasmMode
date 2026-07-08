using FargosPhantasmMode.Content.Render;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bossbar
{
    /*
    public class PhantasmBossBarStyle : ModBossBarStyle
    {
        
        public override string DisplayName => "Phantasm";
        public override bool PreventDraw => true;
        Texture2D barTexture = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Bossbar/PhantasmBossBar").Value;
        public override void Draw(SpriteBatch spriteBatch, IBigProgressBar currentBar, BigProgressBarInfo info)
        {
            NPC npc = Main.npc[info.npcIndexToAimAt];
            if (!npc.boss)
                return;
            #region restore 数据
            Vector2 barCenter = new Vector2(1032, 1111) + new Vector2(0, 0 );
            Color iconColor = Color.White;
            float life = npc.life;
            float lifeMax = npc.lifeMax;
            float iconScale = 1;
            bool showText = true;
            Vector2 textOffset = new (0, 0);
            int frameCount = 6;
            float lifeRatio = Luminance.Common.Utilities.Utilities.Saturate(life / lifeMax);
            Point barSize = new(456, 22);
            Point topLeftOffset = new(32, 24);
            Rectangle barPosition = Utils.CenteredRectangle(barCenter, barSize.ToVector2());
            Vector2 barTopLeft = barPosition.TopLeft();
            Vector2 topLeft = barTopLeft - topLeftOffset.ToVector2();
            Rectangle barFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 2);
            barFrame.X += topLeftOffset.X;
            barFrame.Y += topLeftOffset.Y;
            barFrame.Width = 2;
            barFrame.Height = barSize.Y;
            int scale = barSize.X;
            #endregion
            if (info.npcIndexToAimAt == -1)
            {
                NPCLoader.BossHeadSlot(npc, ref info.npcIndexToAimAt);
                if (info.npcIndexToAimAt == -1)
                    return;
            }
            Texture2D iconTexture = TextureAssets.NpcHeadBoss[info.npcIndexToAimAt].Value;
            Rectangle iconFrame = iconTexture.Frame();
            //healthbar
            Main.spriteBatch.PrepareForShaders(null, true);
            BossBarRender.DrawDoubleColorPulse(barTexture, barTopLeft, barFrame, scale, lifeRatio, Color.Aqua, Color.Blue, 3);
            Main.spriteBatch.ResetToDefaultUI();

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            // Icon.
            Vector2 iconOffset = new(4f, 20f);
            Vector2 iconSize = new(26f, 28f);
            Vector2 iconPosition = iconOffset + iconSize * 0.5f;
            spriteBatch.Draw(iconTexture, topLeft + iconPosition, iconFrame, iconColor, 0f, iconFrame.Size() / 2f, iconScale, 0, 0f);
            FirePartiRe.Particle p = new FirePartiRe.Particle
            {
                Position = topLeft + iconPosition + new Vector2(-20, -2) + Main.rand.NextVector2Unit() * 3,
                Velocity = 1 * Main.rand.NextVector2Unit() + 12 * Vector2.UnitX,
                Scale = 1.3f,
                Alpha = 255,
                active = true
            };
            FirePartiRe.SpawnParticle(p);
           
            

            //Health text.
            textOffset += new Vector2(0, 25);
            if (BigProgressBarSystem.ShowText && showText)
            {
                DrawHealthText(spriteBatch, barPosition, textOffset, life, lifeMax);
            }
            Vector2 namePosition = textOffset + new Vector2(-200, 0);
            //Boss Name
            DrawBossName(npc, spriteBatch, barPosition, namePosition, Color.Transparent);
            Main.spriteBatch.ResetToDefaultUI();
        }
        


        private static void DrawHealthText(SpriteBatch spriteBatch, Rectangle area, Vector2 textOffset, float current, float max)
        {
            DynamicSpriteFont value = FontAssets.MouseText.Value;
            Vector2 vector = area.Center.ToVector2() + textOffset;
            vector.Y += 1f;

            double lifePercentage = Math.Round(100 * current / max, 2);
            string strliferatio = lifePercentage.ToString();

            if (lifePercentage % 1 == 0)
            {
                strliferatio += ".00";
            }
            else if (lifePercentage * 10 % 1 == 0)
            {
                strliferatio += "0";
            }
            strliferatio += "%";
            string healthtext = $"{(int)current}/{(int)max}   " + strliferatio;
            Vector2 vector2 = value.MeasureString(healthtext);
            //DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, value, healthtext, vector, Color.White, 0f, vector2 / 2f, 0.75f, (SpriteEffects)0, 0f);
            Utils.DrawBorderStringFourWay(spriteBatch, value, healthtext, vector.X, vector.Y, Color.White, Color.Transparent, vector2 / 2f, 0.85f);
        }
        private static void DrawBossName(NPC npc, SpriteBatch sb, Rectangle area, Vector2 textOffset, Color color)
        {
            string bossName = npc.FullName;
            Vector2 vector = area.Center.ToVector2() + textOffset;
            vector.Y += 1f;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 vector2 = font.MeasureString(bossName);
            Utils.DrawBorderStringFourWay(sb, font, bossName, vector.X, vector.Y, Color.White, color, vector2 / 2f, 0.9f);
        }
    }
    */
}
