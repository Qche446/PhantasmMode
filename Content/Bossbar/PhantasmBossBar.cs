using Microsoft.Xna.Framework.Graphics;
using FargosPhantasmMode.Content.Render;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.GameContent;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using System;
using ReLogic.Graphics;
using FargowiltasSouls.Content.Bosses.MutantBoss;

namespace FargosPhantasmMode.Content.Bossbar
{
    public class PhantasmBossBar : ModBossBar
    {
        public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
        {
            (Texture2D barTexture, Vector2 barCenter, _, _, Color iconColor, float life, float lifeMax, float shield, float shieldMax, float iconScale, bool showText, Vector2 textOffset) = drawParams;
            float lifeRatio = Luminance.Common.Utilities.Utilities.Saturate(life / lifeMax);
            int headTextureIndex = NPCID.Sets.BossHeadTextures[npc.type];
            if (headTextureIndex == -1)
            {
                NPCLoader.BossHeadSlot(npc, ref headTextureIndex);
                if (headTextureIndex == -1)
                    return false;
            }
            Texture2D iconTexture = TextureAssets.NpcHeadBoss[headTextureIndex].Value;
            Rectangle iconFrame = iconTexture.Frame();
            Point barSize = new(456, 22);
            Point topLeftOffset = new(32, 24);
            int frameCount = 6;
            //Rectangle bgFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 3);
            //Color bgColor = Color.White * 0.2f;

            int scale = barSize.X;
            //scale -= scale % 2;
            Rectangle barFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 2);
            barFrame.X += topLeftOffset.X;
            barFrame.Y += topLeftOffset.Y;
            barFrame.Width = 2;
            barFrame.Height = barSize.Y;

            int shieldScale = (int)(barSize.X * shield / shieldMax);
            shieldScale -= shieldScale % 2;

            Rectangle barShieldFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 5);
            barShieldFrame.X += topLeftOffset.X;
            barShieldFrame.Y += topLeftOffset.Y;
            barShieldFrame.Width = 2;
            barShieldFrame.Height = barSize.Y;

            Rectangle tipShieldFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 4);
            tipShieldFrame.X += topLeftOffset.X;
            tipShieldFrame.Y += topLeftOffset.Y;
            tipShieldFrame.Width = 2;
            tipShieldFrame.Height = barSize.Y;

            Rectangle barPosition = Utils.CenteredRectangle(barCenter, barSize.ToVector2());
           // Main.NewText(barPosition);
            
            Vector2 barTopLeft = barPosition.TopLeft();
            Vector2 topLeft = barTopLeft - topLeftOffset.ToVector2();
            //Main.NewText(topLeft);

            // Background.
            //spriteBatch.Draw(barTexture, topLeft, bgFrame, bgColor, 0f, Vector2.Zero, 1f, 0, 0f);

            Main.spriteBatch.PrepareForShaders(null, true);
            DrawBar(npc, barTexture, barTopLeft, barFrame, scale, lifeRatio);
            Main.spriteBatch.ResetToDefaultUI();

            // Bar itself (shield).
            if (shield > 0f)
            {
                Vector2 stretchScale = new(shieldScale / barFrame.Width, 1f);
                spriteBatch.Draw(barTexture, barTopLeft, barShieldFrame, Color.White, 0f, Vector2.Zero, stretchScale, 0, 0f);
                spriteBatch.Draw(barTexture, barTopLeft + new Vector2(shieldScale - 2, 0f), tipShieldFrame, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
            }
            /*
            // Frame.
            Rectangle frameFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 0);
            spriteBatch.Draw(barTexture, topLeft, frameFrame, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
            */


            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            // Icon.
            Vector2 iconOffset = new(4f, 20f);
            Vector2 iconSize = new(26f, 28f);
            Vector2 iconPosition = iconOffset + iconSize * 0.5f;
            spriteBatch.Draw(iconTexture, topLeft + iconPosition, iconFrame, iconColor, 0f, iconFrame.Size() / 2f, iconScale, 0, 0f);
            
            
            /*
            if (Main.drawsCountedForFPS % 60 == 0)
                LightningPartiRe.SpawnParticle(topLeft + iconPosition, topLeft + iconPosition + new Vector2(0, -200), 10, 20);
            LightningPartiRe.AllDraw(spriteBatch);
            */
            //LightningPartiRe.UpdateParticle();

            // Health text.
            textOffset += new Vector2(0, 25);
            if (BigProgressBarSystem.ShowText && showText)
            {
                if (shield > 0f)
                    DrawHealthText(spriteBatch, barPosition, textOffset, shield, shieldMax);
                else
                    DrawHealthText(spriteBatch, barPosition, textOffset, life, lifeMax);
            }
            Vector2 namePosition = textOffset + new Vector2(-200, 0);
            //Boss Name
            DrawBossName(npc, spriteBatch, barPosition, namePosition, Color.Transparent);
            Main.spriteBatch.ResetToDefaultUI();
            return false;
        }
        private static void DrawBar(NPC npc, Texture2D barTexture, Vector2 barTopLeft, Rectangle barFrame, float scale, float lifeRatio)
        {
            Point topLeftOffset = new(32, 24);
            Vector2 topLeft = barTopLeft - topLeftOffset.ToVector2();
            Vector2 iconOffset = new(4f, 20f);
            Vector2 iconSize = new(26f, 28f);
            Vector2 iconPosition = iconOffset + iconSize * 0.5f;
            if (npc.type == NPCID.EyeofCthulhu)
            {
                BossBarRender.DrawDoubleColorPulse(barTexture, barTopLeft, barFrame, scale, lifeRatio, Color.Teal, Color.Teal, 0);
            }
            if (npc.type == ModContent.NPCType<MutantBoss>())//突
            {
                BossBarRender.DrawDoubleColorPulse(barTexture, barTopLeft, barFrame, scale, lifeRatio, Color.Aqua, Color.Blue, 3);
                FirePartiRe.Particle p = new FirePartiRe.Particle
                {
                    Position = topLeft + iconPosition + new Vector2(-20, -2) + Main.rand.NextVector2Unit() * 3 /*+ Main.rand.Next(60) * Vector2.UnitX*/,
                    Velocity = 1 * Main.rand.NextVector2Unit() + 12 * Vector2.UnitX,
                    Scale = 1.3f,
                    Alpha = 255,
                    active = true
                };
                FirePartiRe.SpawnParticle(p);
            }
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
            Utils.DrawBorderStringFourWay(spriteBatch, value, healthtext, vector.X, vector.Y, Color.White, Color.Transparent, vector2 / 2f, 0.80f);
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
        /*
        private static void DrawIcon(NPC npc, Texture2D iconTexture, Vector2 Position, Rectangle sourceRectangle, Color iconColor, float iconScale)
        {
            Vector2 iconOffset = new(4f, 20f);
            Vector2 iconSize = new(26f, 28f);
            Vector2 iconPosition = iconOffset + iconSize * 0.5f;
            Main.spriteBatch.Draw(iconTexture, topLeft + iconPosition, iconFrame, iconColor, 0f, iconFrame.Size() / 2f, iconScale, 0, 0f);
        }
        */
    }
}
