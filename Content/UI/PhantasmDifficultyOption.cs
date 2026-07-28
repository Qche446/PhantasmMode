using FargosPhantasmMode.Core.Systems;
using Fargowiltas.Projectiles;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items;
using FargowiltasSouls.Content.NPCs;
using FargowiltasSouls.Content.UI;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.GameContent.Creative.CreativePowers;

namespace FargosPhantasmMode.Content.UI
{
    public class PhantasmDifficultyOption : DifficultyOption
    {
        public const string LocPath = "Mods.FargosPhantasmMode.UI.";
        public override string NameKey => LocPath + "Phantasm";
        
        public static void EnablePhantasm()
        {
            if (!Masochist.CanToggleEternity())
                return;
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                bool changed = false;
                if (Main.GameModeInfo.IsJourneyMode)
                {
                    var slider = CreativePowerManager.Instance.GetPower<DifficultySliderPower>();
                    DifficultySelectionMenu.JourneyMode_SetValue.Invoke(slider, [1f]);
                }
                else
                {
                    if (Main.GameMode != GameModeID.Master)
                        changed = true;
                    Main.GameMode = GameModeID.Master;
                }
                if (changed)
                    FargoSoulsUtil.PrintLocalization("Mods.Fargowiltas.Items.ModeToggle.Master", new Color(175, 75, 255));

                WorldSavingSystem.ShouldBeEternityMode = true;
                PModeWorldSavingSystem.CanPlayPhantasm = true;
            }
            else
            {
                if (Main.GameMode != GameModeID.Master || !WorldSavingSystem.ShouldBeEternityMode)
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Difficulty" + "Maso") with { Volume = 1f });

                var netMessage = FargosPhantasmMode.Instance.GetPacket();
                netMessage.Write((byte)FargosPhantasmMode.PacketID.ActivePhamtasmMode);
                netMessage.Write((byte)Main.LocalPlayer.whoAmI);
                netMessage.Write((byte)3); // 2 = set to emode
                netMessage.Send();
            }
            int deviType = ModContent.NPCType<UnconsciousDeviantt>();
            if (!WorldSavingSystem.SpawnedDevi && !NPC.AnyNPCs(deviType))
            {
                WorldSavingSystem.SpawnedDevi = true;

                Vector2 spawnPos = (Main.zenithWorld || Main.remixWorld) ? Main.LocalPlayer.Center : Main.LocalPlayer.Center - 1000 * Vector2.UnitY;
                Projectile.NewProjectile(Main.LocalPlayer.GetSource_Misc(""), spawnPos, Vector2.Zero, ModContent.ProjectileType<SpawnProj>(), 0, 0, Main.myPlayer, deviType);

                FargoSoulsUtil.PrintLocalization("Announcement.HasAwoken", new Color(175, 75, 255), Language.GetTextValue("Mods.Fargowiltas.NPCs.Deviantt.DisplayName"));
            }
        }

        public override void OnClicked()
        {
            EnablePhantasm();
        }

        public override string TooltipText()
        {
            string text = Language.GetTextValue($"{LocPath}PhantasmOption");
            text += $"\n{Language.GetTextValue($"{LocPath}ExpandedFeatures")}";
            if (Main.netMode != NetmodeID.SinglePlayer)
                text += $"\n{Language.GetTextValue($"{LocPath}MasochistMultiplayer")}";
            return text;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 position, float scale)
        {
            var texture = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/UI/PhantasmIcon", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Vector2 center = position + new Vector2(Width.Pixels / 2, Height.Pixels / 2) - scale * texture.Size() / 2;
            spriteBatch.Draw(texture, center, texture.Bounds, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }
}
