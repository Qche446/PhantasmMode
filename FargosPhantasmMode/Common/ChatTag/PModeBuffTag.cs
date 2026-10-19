using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using ReLogic.Graphics;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace FargosPhantasmMode.Common.ChatTag
{
    public class PModeBuffTag : ITagHandler, ILoadable
    {
        public string[] TagName { get; } = ["pbuff"];
        public void Load(Mod mod)
        {
            ChatManager.Register<PModeBuffTag>(TagName);
        }

        public TextSnippet Parse(string text, Color baseColor = default, string options = null)
        {
            if (int.TryParse(text, out int buffId) && buffId >= 0 && buffId < BuffLoader.BuffCount)
                return new Snippet(buffId);

            if (BuffID.Search.TryGetId(text, out buffId))
                return new Snippet(buffId);
            return new TextSnippet(text);
        }

        public void Unload()
        {

        }
        public sealed class Snippet(int buffId) : TextSnippet
        {
            private const float IconSize = 26f;
            public int BuffId => buffId;
            public bool DrawIcon => true;
            public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = new Vector2(), Color color = new Color(), float scale = 1)
            {
                size = new Vector2(GetStringLength(FontAssets.MouseText.Value), IconSize);

                if (!justCheckingString && (color.R != 0 || color.G != 0 || color.B != 0))
                {
                    if (DrawIcon)
                    {
                        if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                        {
                            var texture = TextureAssets.Buff[BuffId];
                            spriteBatch.Draw(texture.Value, new Rectangle((int)position.X, (int)position.Y - 2, (int)IconSize, (int)IconSize), null, Color.White);
                        }

                        position.X += IconSize;
                    }

                    var buffColor = GetBuffColor(BuffId);
                    var name = $"{(DrawIcon ? " " : "")}{Lang.GetBuffName(buffId)}";
                    ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, name, position, buffColor, 0f, Vector2.Zero, new Vector2(scale));
                }
                return true;
            }

            public override float GetStringLength(DynamicSpriteFont font)
            {
                float iconSize = !DrawIcon ? 0f : IconSize + font.MeasureString(" ").X;
                float size = iconSize + font.MeasureString(Lang.GetBuffName(buffId)).X;
                return size * Scale;
            }
        }
        public static Color GetBuffColor(int buffId)
        {
            Color def = new Color(230, 202, 250);
            if (buffId == ModContent.BuffType<FlamesoftheUniverseBuff>())
            {
                return PhanUtil.CosmoColor();
            }
            if (redBuff.Contains(buffId))
            {
                return Color.Red;
            }
            if (orangeBuff.Contains(buffId))
            {
                return Color.Orange;
            }
            if (yellowBuff.Contains(buffId))
            {
                return Color.Yellow;
            }
            if (greenBuff.Contains(buffId))
            {
                return Color.Green;
            }
            if (cyanBuff.Contains(buffId))
            {
                return Color.Cyan;
            }
            if (blueBuff.Contains(buffId))
            {
                return Color.Blue;
            }
            if (purpleBuff.Contains(buffId))
            {
                return Color.Purple;
            }
            if (garyBuff.Contains(buffId))
            {
                return Color.Gray;
            }
            if (whitebuff.Contains(buffId))
            {
                return Color.White;
            }
            return def;
        }
        public static readonly List<int> redBuff = [BuffID.OnFire3, PBuffID.PungentGazeBuff, PBuffID.BerserkedBuff, PBuffID.BulletReload, PBuffID.PrayerofTaintedBlood,
            PBuffID.HeartBrokenBuff, PBuffID.NanoErosionBuff];
        public static readonly List<int> orangeBuff = [BuffID.RapidHealing, PBuffID.SmiteBuff, BuffID.OnFire, BuffID.Ichor, PBuffID.HallowFlameBuff, BuffID.Daybreak];
        public static readonly List<int> yellowBuff = [BuffID.Honey, PBuffID.InfestedBuff];
        public static readonly List<int> greenBuff = [BuffID.CursedInferno, BuffID.Poisoned, PBuffID.IvyVenomBuff];
        public static readonly List<int> cyanBuff = [PBuffID.CurseoftheMoonBuff, PBuffID.MutantFangBuff];
        public static readonly List<int> blueBuff = [BuffID.Frostburn, BuffID.Frostburn2, BuffID.Frozen, PBuffID.OceanicMaulBuff];
        public static readonly List<int> purpleBuff = [BuffID.Venom, PBuffID.HunterMark, BuffID.ShadowFlame];
        public static readonly List<int> garyBuff = [PBuffID.NeurotoxinBuff, PBuffID.MutantNibbleBuff, PBuffID.FractureBuff];
        public static readonly List<int> whitebuff = [PBuffID.PhSublimationBuff, PBuffID.TimeGodsTrickBuff, PBuffID.FlawlessBuff];
    }
}
