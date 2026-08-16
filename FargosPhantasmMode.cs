using Terraria;
using Microsoft.Xna.Framework;
using FargosPhantasmMode.Assets.ExtraTextures;
using FargosPhantasmMode.Content.Render;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using System.IO;
using System;
using FargowiltasSouls;
using FargowiltasSouls.Core.Systems;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using static Terraria.GameContent.Creative.CreativePowers;
using Luminance.Common.Utilities;
using Terraria.ModLoader.Default.Patreon;
using FargosPhantasmMode.Core.Systems;
using System.Collections.Generic;
using FargosPhantasmMode.Common;


namespace FargosPhantasmMode
{
    public class FargosPhantasmMode : Mod
    {
        internal static FargosPhantasmMode Instance;
        public static ManagedRenderTarget Rt;
        public static Mod FargoMod;
        public override void Load()
        {
            ModLoader.TryGetMod("FargowiltasSouls", out FargoMod);
            On_FilterManager.EndCapture += FilterManager_EndCapture;
            Rt = new ManagedRenderTarget(true,
                (width, heigth) => new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight));

            Instance = this;
        }
        public override void Unload()
        {
            On_FilterManager.EndCapture -= FilterManager_EndCapture;
        }
        private void FilterManager_EndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;

            #region °∞UI”Ó÷Ê÷Æª°±
            gd.SetRenderTarget(Main.screenTargetSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();


            gd.SetRenderTarget(Rt);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            Texture2D tex = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Dusts/CosmicFlame").Value;
            FirePartiRe.AllDraw(sb, tex);
            FirePartiRe.UpdateParticle();
            //LightningPartiRe.AllDraw(sb);
            LightningPartiRe.UpdateParticle();
            sb.End();

            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            ManagedShader shader = ShaderManager.GetShader("FargosPhantasmMode.BigTentacle");
            gd.Textures[1] = PhantasmTextureRegistry.UniverseNoise.Value;
            shader.TrySetParameter("color", new Color(54, 255, 236));//102, 26, 179£®◊œ£©  54£¨255£¨236(«‡)
            shader.TrySetParameter("m", 0.62f);
            shader.TrySetParameter("n", 0.01f);
            shader.Apply();
            sb.Draw(Rt, Vector2.Zero, Color.White);
            sb.End();
            #endregion

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
        internal enum PacketID : byte
        {
            ActivePhamtasmMode,
        }
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte data = reader.ReadByte();
            if (Enum.IsDefined(typeof(PacketID), data))
            {
                switch ((PacketID)data)
                {
                    case PacketID.ActivePhamtasmMode:
                        {
                            Player player = FargoSoulsUtil.PlayerExists(reader.ReadByte());
                            int diff = reader.ReadByte();
                            if (Main.netMode == NetmodeID.Server)
                            {
                                string toggle = diff switch
                                {
                                    3 => "Phantasm",
                                    2 => "Master",
                                    1 => "Expert",
                                    0 => "None",
                                    _ => "None"
                                };
                                if (diff != 0)
                                {
                                    bool changed = false;
                                    if (Main.GameModeInfo.IsJourneyMode)
                                    {
                                        float value = diff >= 2 ? 1f : 0.66f;
                                        var slider = CreativePowerManager.Instance.GetPower<DifficultySliderPower>();
                                        typeof(CreativePowers.DifficultySliderPower).GetMethod("SetValueKeyboardForced", Utilities.UniversalBindingFlags).Invoke(slider, [value]);
                                    }
                                    else
                                    {
                                        switch (diff)
                                        {
                                            case 1:
                                                if (Main.GameMode != GameModeID.Expert)
                                                    changed = true;
                                                Main.GameMode = GameModeID.Expert;
                                                break;
                                            case 2:
                                                if (Main.GameMode != GameModeID.Master)
                                                    changed = true;
                                                Main.GameMode = GameModeID.Master;
                                                break;
                                            case 3:
                                                if (Main.GameMode != GameModeID.Master)
                                                    changed = true;
                                                Main.GameMode = GameModeID.Master;
                                                break;
                                        }
                                    }
                                    if (changed)
                                        FargoSoulsUtil.PrintLocalization($"Mods.Fargowiltas.Items.ModeToggle.{toggle}", new Color(175, 75, 255));
                                }

                                WorldSavingSystem.ShouldBeEternityMode = diff != 0;
                                PModeWorldSavingSystem.CanPlayPhantasm = diff == 3;
                                if (diff != 0)
                                {
                                    WorldSavingSystem.SpawnedDevi = true;
                                }

                                NetMessage.SendData(MessageID.WorldData); //sync world
                            }
                            else
                            {
                                string mode;
                                float volume = 0.5f;

                                switch (diff)
                                {
                                    case 1:
                                        mode = "Emode";
                                        break;
                                    case 2:
                                        mode = "Maso";
                                        break;
                                    case 3:
                                        mode = "Phantasm";
                                        break;
                                    default:
                                        mode = "Deactivate";
                                        volume = 1;
                                        break;
                                }
                                if (diff != 3)
                                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Difficulty" + mode) with { Volume = volume });
                                else
                                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Difficulty" + "Maso") with { Volume = volume });
                            }
                        }
                        break;
                }
            }
        }
    }
}
