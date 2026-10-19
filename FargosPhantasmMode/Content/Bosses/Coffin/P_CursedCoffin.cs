using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using Monochrome.Common.MonoUtil;
using Monochrome.Common.MonoUtil.Mathematics.Statistics;
using Monochrome.Core.Net;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace FargosPhantasmMode.Content.Bosses.Coffin;

public partial class P_CursedCoffin : PModeNPCBehaviour
{
    public MonoRandom random;
    public override int NPCType => ModContent.NPCType<CursedCoffin>();
    public ref float Timer => ref coffin.NPC.ai[0];
    public ref float AI1 => ref coffin.ForceGrabPunish;
    public ref float AI2 => ref coffin.AI2;
    public ref float AI3 => ref coffin.AI3;
    public Vector2 MaskCenter => coffin.MaskCenter();
    private static readonly Color glowColor = Color.Purple with { A = 0 };//new(224, 196, 252, 0);
    public static Color GlowColor => Main.npc.Any(n => n.TypeAlive<CursedCoffin>() && n.As<CursedCoffin>().Enraged) ? Color.Lerp(Color.Red, glowColor, 0.75f) : glowColor;
    public bool ExtraTrail;
    public float DrawcodeOpacity = 0f;
    public ref int Frame => ref coffin.Frame;
    /*
    private static readonly MonoNetFields<NPC> Fields = MonoNet.Fields<NPC>("fpm.coffin")
        .Byte("lac1", static n => (byte)Self(n).LastAttackChoice1, static (n, value) => Self(n).LastAttackChoice1 = (State)value)
        .Byte("lac2", static n => (byte)Self(n).LastAttackChoice2, static (n, value) => Self(n).LastAttackChoice2 = (State)value)
        .Byte("phase", static n => Self(n).Phase, static (n, value) => Self(n).Phase = value);
    */
    private static readonly MonoNetFields<P_CursedCoffin> Fields2 = MonoNet.Fields<P_CursedCoffin>("fpm.pcoffin")
        .Byte("lac1", static n => (byte)n.LastAttackChoice1, static (n, value) => n.LastAttackChoice1 = (State)value)
        .Byte("lac2", static n => (byte)n.LastAttackChoice2, static (n, value) => n.LastAttackChoice2 = (State)value)
        .Byte("phase", static n => n.Phase, static (n, value) => n.Phase = value);
    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (npc.IsABestiaryIconDummy)
        {
            if (Main.getGoodWorld)
            {
                Texture2D whitecoffin = ModContent.Request<Texture2D>(coffin.Texture + "_FTW").Value;
                spriteBatch.Draw(whitecoffin, npc.position - screenPos, null, npc.GetAlpha(drawColor), 0f, Vector2.Zero, npc.scale, SpriteEffects.None, 0f);
                return false;
            }
            return true;
        }

        if (DrawcodeOpacity < 1f)
            DrawcodeOpacity += 0.025f;

        Texture2D bodytexture = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;
        Vector2 drawPos = npc.Center - screenPos;
        SpriteEffects spriteEffects = npc.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Vector2 origin = new Vector2(bodytexture.Width / 2, bodytexture.Height / 2 / Main.npcFrameCount[npc.type]);

        for (int i = 0; i < (ExtraTrail ? NPCID.Sets.TrailCacheLength[npc.type] : NPCID.Sets.TrailCacheLength[npc.type] / 4); i++)
        {
            Vector2 value4 = npc.oldPos[i];
            int oldFrame = Frame;
            Rectangle oldRectangle = new(0, oldFrame * bodytexture.Height / Main.npcFrameCount[npc.type], bodytexture.Width, bodytexture.Height / Main.npcFrameCount[npc.type]);
            DrawData oldGlow = new(bodytexture, value4 + npc.Size / 2f - screenPos + new Vector2(0, npc.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(oldRectangle), GlowColor * (0.5f / i) * DrawcodeOpacity, npc.rotation, origin, npc.scale, spriteEffects, 0);
            GameShaders.Misc["LCWingShader"].UseColor(Color.Blue).UseSecondaryColor(Color.Black);
            GameShaders.Misc["LCWingShader"].Apply(oldGlow);
            oldGlow.Draw(spriteBatch);
        }
        bool spirit = Main.npc.Any(p => p.TypeAlive<CursedSpirit>());
        if (!spirit)
        {
            for (int j = 0; j < 12; j++)
            {
                float spinOffset = (Main.GameUpdateCount * 0.001f * j) % 12;
                float magnitude = 3f + ((j % 5) * 3f * MathF.Sin(Main.GameUpdateCount * MathHelper.TwoPi / (10 + ((j - 6f) * 28f))));
                Vector2 afterimageOffset = (MathHelper.TwoPi * (j + spinOffset) / 12f).ToRotationVector2() * magnitude * npc.scale;
                Color glowColor = GlowColor;

                spriteBatch.Draw(bodytexture, drawPos + afterimageOffset, npc.frame, glowColor * DrawcodeOpacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            }
        }

        spriteBatch.Draw(bodytexture, drawPos, npc.frame, drawColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);

        if (!spirit)
        {
            float shakeFactor = 1;
            if (Fsm.Current == State.PhaseTransition1st || Fsm.Current == State.PhaseTransition2nd)
                shakeFactor = 3 + 5 * (Timer / 60);
            Texture2D glowTexture = ModContent.Request<Texture2D>(coffin.Texture + "_MaskGlow", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Color glowColor = GlowColor;
            int glowTimer = (int)(Main.GlobalTimeWrappedHourly * 60) % 60;
            DrawData oldGlow = new(glowTexture, drawPos + Main.rand.NextVector2Circular(shakeFactor, shakeFactor), npc.frame, glowColor * DrawcodeOpacity * (0.75f + 0.25f * MathF.Sin(MathF.Tau * glowTimer / 60f)), npc.rotation, new Vector2(bodytexture.Width / 2, bodytexture.Height / 2 / Main.npcFrameCount[npc.type]), npc.scale, spriteEffects, 0);
            GameShaders.Misc["LCWingShader"].UseColor(Color.Purple).UseSecondaryColor(Color.Black);
            GameShaders.Misc["LCWingShader"].Apply(oldGlow);
            oldGlow.Draw(spriteBatch);
        }

        return false;
    }
}
