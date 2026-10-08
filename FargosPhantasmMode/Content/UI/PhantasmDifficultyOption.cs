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

            // 多人下先响一声：这两项都是已经同步下来的世界状态，客户端读得到。
            // 单机原本不响这一声，保持原样。
            if (Main.netMode != NetmodeID.SinglePlayer && (Main.GameMode != GameModeID.Master || !WorldSavingSystem.ShouldBeEternityMode))
                SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Difficulty" + "Maso") with { Volume = 1f });

            // 单机与多人同一条路：请求在权威端复核后执行，单机时"权威端"就是本机。
            PModeNet.SetDifficulty.Send(3);

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
