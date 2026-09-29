using Fargowiltas.Projectiles;
using FargowiltasSouls.Content.NPCs;
using FargowiltasSouls;
using FargowiltasSouls.Content.UI;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Reflection;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using FargosPhantasmMode.Core.Systems;
using Microsoft.Xna.Framework.Graphics;
using FargowiltasSouls.Content.Items;
using Microsoft.Xna.Framework.Input;
using Terraria.UI;
using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.UI
{
    public class PModeDifficultySelectionMenu : ModSystem
    {
        public const int BackHeight = 100;
        public const int newBackWidth = 204;
        public override void Load()
        {
            var ins = ModContent.GetInstance<DifficultySelectionMenu>();
            //给难度选择加上境妄
            PhanUtil.AddHooks(ins.UpdateElements, AddPhantasm);
            //调整难度选择界面大小，不知道为什么没用
            //PhanUtil.AddHooks(ins.OnInitialize, AdjustBackScale);
            PhanUtil.AddHooks(MasoDifficultyOption.EnableMasochist, AddStopPhantasm);

            MethodInfo m3 = typeof(UIOncomingMutant).GetMethod("DrawSelf", BindingFlags.NonPublic | BindingFlags.Instance);
            MonoModHooks.Add(m3, UIMutantFixed);//调整突变体图标以适应境妄
        }
        private static void AddPhantasm(Action<DifficultySelectionMenu> orig, DifficultySelectionMenu self)
        {
            self.BackPanel.RemoveAllChildren();

            var title = new UIText(Language.GetTextValue("Mods.FargowiltasSouls.UI.SelectDifficulty"));
            title.Left.Set(-60, 0.5f);
            title.Top.Set(5, 0);
            self.BackPanel.Append(title);
            float halfWidth = 20;

            self.AddNewOption(new VanillaDifficultyOption(), -3 * halfWidth);
            self.AddNewOption(new EternityDifficultyOption(), -halfWidth);
            self.AddNewOption(new MasoDifficultyOption(), halfWidth);

            self.AddNewOption(new PhantasmDifficultyOption(), 3 * halfWidth);
        }
        private static void AdjustBackScale(Action<DifficultySelectionMenu> orig, DifficultySelectionMenu self)
        {
            Vector2 offset = new(-newBackWidth / 2, -BackHeight / 2);

            self.BackPanel = new UIPanel();
            self.BackPanel.Left.Set(offset.X, 0.5f);
            self.BackPanel.Top.Set(offset.Y, 0.5f);
            self.BackPanel.Width.Set(newBackWidth, 0);
            self.BackPanel.Height.Set(BackHeight, 0);
            self.BackPanel.PaddingLeft = self.BackPanel.PaddingRight = self.BackPanel.PaddingTop = self.BackPanel.PaddingBottom = 0;
            self.BackPanel.BackgroundColor = new Color(29, 33, 70) * 0.7f;

            self.Append(self.BackPanel);
            
        }
        private static void AddStopPhantasm(Action orig)
        {
            orig.Invoke();
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                PModeWorldSavingSystem.CanPlayPhantasm = false;
            }
            else
            {
                if (PModeWorldSavingSystem.CanPlayPhantasm)
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Difficulty" + "Deactivate") with { Volume = 0.5f });

                var netMessage = FargosPhantasmMode.Instance.GetPacket();
                netMessage.Write((byte)FargosPhantasmMode.PacketID.ActivePhamtasmMode);
                netMessage.Write((byte)Main.LocalPlayer.whoAmI);
                netMessage.Write((byte)2); // 0 = disable emode
                netMessage.Send();
            }
        }
        private static void UIMutantFixed(Action<UIOncomingMutant, SpriteBatch> orig, UIOncomingMutant self, SpriteBatch spriteBatch)
        {
            CalculatedStyle style = self.GetDimensions();
            // Logic
            if (self.IsMouseHovering && !self.dragging)
            {
                Vector2 textPosition = Main.MouseScreen + new Vector2(21, 21);
                string TextPhan = Language.GetTextValue("Mods.FargosPhantasmMode.UI.OpenPhanState");
                string text = PModeWorldSavingSystem.PhantasmMode ? TextPhan : WorldSavingSystem.MasochistModeReal ? self.TextMaso : WorldSavingSystem.EternityMode ? self.TextEMode : self.TextDisabled;
                if (PModeWorldSavingSystem.PhantasmMode)
                    text = $"[c/2FD6FF:{text}]";
                else if (WorldSavingSystem.MasochistModeReal)
                    text = $"[c/33ffbe:{text}]";
                else if (WorldSavingSystem.EternityMode)
                    text = $"[c/00FFFF:{text}]";

                if (Masochist.CanToggleEternity())
                    text += $"\n[c/787878:{self.TextRightClick}]";


                if (Main.keyState.IsKeyDown(Keys.LeftShift))
                {
                    string PhdifText = Language.GetTextValue("Mods.FargosPhantasmMode.UI.ExpandedFeatures");
                    string difText = PModeWorldSavingSystem.PhantasmMode ? PhdifText : WorldSavingSystem.MasochistModeReal ? self.TextExpandedMaso : self.TextExpandedEternity;
                    text += $"\n{difText}";
                    if (!PModeWorldSavingSystem.PhantasmMode)
                        text += $"\n{self.TextExpandedFeatures}";
                    if (WorldSavingSystem.MasochistModeReal && Main.netMode != NetmodeID.SinglePlayer)
                        text += $"\n{self.TextMasoMultiplayer}";
                }
                else
                    text += $"\n[c/787878:{self.TextHoldShift}]";

                Utils.DrawBorderString(
                    spriteBatch,
                    text,
                    textPosition,
                    Color.White);
            }

            // Drawing
            var texture = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/UI/PhantasmIcon", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Vector2 position = style.Position();
            if (WorldSavingSystem.EternityMode)
            {
                if (PModeWorldSavingSystem.PhantasmMode)
                {
                    spriteBatch.Draw(texture, position, texture.Bounds, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
                }
                else
                {
                    spriteBatch.Draw(self.Texture, position + new Vector2(2), self.Texture.Bounds, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    if (WorldSavingSystem.MasochistModeReal)
                    {
                        spriteBatch.Draw(self.AuraTexture, position, self.AuraTexture.Bounds, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
                    }
                }
            }
            else
            {
                spriteBatch.Draw(self.EmptyTexture, position + new Vector2(2), self.Texture.Bounds, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
        }
    }
}
