using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FargosPhantasmMode.Common;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Items.Summons;
using FargowiltasSouls.Content.WorldGeneration;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.StateMachines;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Content.Bosses.Coffin
{
    public partial class P_CursedCoffin : PModeNPCBehaviour
    {
        public CursedCoffin coffin;

        public const int RandomStuffOpenTime = 60;

        private float DrawcodeOpacity = 0f;

        private byte Phase = 1;

        private bool ExtraTrail = false;

        private static readonly Color glowColor;

        public ref float ForceGrabPunish => ref coffin.ForceGrabPunish;

        public ref float AI2 => ref coffin.AI2;

        public ref float AI3 => ref coffin.AI3;

        public ref float AttackCounter => ref coffin.AttackCounter;

        private byte LastAttackChoice { get; set; }

        public float Timer
        {
            get
            {
                return (StateMachine.StateStack.Count != 0) ? StateMachine.CurrentState.Time : 0;
            }
            set
            {
                if (StateMachine.StateStack.Count != 0)
                {
                    StateMachine.CurrentState.Time = (int)value;
                }
            }
        }

        public ref int MashTimer => ref coffin.MashTimer;

        public ref int Frame => ref coffin.Frame;

        public ref Vector2 LockVector1 => ref coffin.LockVector1;

        public Vector2 MaskCenter => coffin.MaskCenter();

        public static Color GlowColor => glowColor;

        public override int NPCType => ModContent.NPCType<CursedCoffin>();
        public override void SetDefaults(NPC npc)
        {
            if (!Main.getGoodWorld)
            {
                npc.damage *= (int)(1.1f * (float)npc.lifeMax);
                npc.lifeMax *= (int)(1.1f * (float)npc.lifeMax);
            }
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(LastAttackChoice);
            binaryWriter.Write(Phase);
            binaryWriter.Write(Timer);
            binaryWriter.WriteVector2(LockVector1);
            List<EntityAIState<BehaviorStates>> stateStack = [.. (StateMachine?.StateStack ?? new Stack<EntityAIState<BehaviorStates>>())];
            binaryWriter.Write(stateStack.Count);
            for (int i = stateStack.Count - 1; i >= 0; i--)
            {
                binaryWriter.Write((byte)stateStack[i].Identifier);
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            LastAttackChoice = binaryReader.ReadByte();
            Phase = binaryReader.ReadByte();
            Timer = binaryReader.ReadSingle();
            LockVector1 = binaryReader.ReadVector2();
            StateMachine.StateStack.Clear();
            int stateStackCount = binaryReader.ReadInt32();
            for (int i = 0; i < stateStackCount; i++)
            {
                StateMachine.StateStack.Push(StateMachine.StateRegistry[(BehaviorStates)binaryReader.ReadByte()]);
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.IsABestiaryIconDummy)
            {
                if (Main.getGoodWorld)
                {
                    Texture2D whitecoffin = ModContent.Request<Texture2D>(npc.ModNPC.Texture + "_FTW").Value;
                    spriteBatch.Draw(whitecoffin, npc.position - screenPos, null, npc.GetAlpha(drawColor), 0f, Vector2.Zero, npc.scale, SpriteEffects.None, 0f);
                    return false;
                }
                return true;
            }
            if (DrawcodeOpacity < 1f)
            {
                DrawcodeOpacity += 0.025f;
            }
            Texture2D bodytexture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPos = npc.Center - screenPos;
            SpriteEffects spriteEffects = ((npc.direction != 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            Vector2 origin = new Vector2(bodytexture.Width / 2, bodytexture.Height / 2 / Main.npcFrameCount[npc.type]);
            for (int i = 0; i < (ExtraTrail ? NPCID.Sets.TrailCacheLength[npc.type] : (NPCID.Sets.TrailCacheLength[npc.type] / 4)); i++)
            {
                Vector2 value4 = npc.oldPos[i];
                int oldFrame = Frame;
                DrawData oldGlow = new DrawData(sourceRect: new Rectangle(0, oldFrame * bodytexture.Height / Main.npcFrameCount[npc.type], bodytexture.Width, bodytexture.Height / Main.npcFrameCount[npc.type]), texture: bodytexture, position: value4 + npc.Size / 2f - screenPos + new Vector2(0f, npc.gfxOffY), color: GlowColor * (0.5f / (float)i) * DrawcodeOpacity, rotation: npc.rotation, origin: origin, scale: npc.scale, effect: spriteEffects);
                GameShaders.Misc["LCWingShader"].UseColor(Color.Blue).UseSecondaryColor(Color.Black);
                GameShaders.Misc["LCWingShader"].Apply(oldGlow);
                oldGlow.Draw(spriteBatch);
            }
            bool spirit = Main.npc.Any((NPC p) => FargoExtensionMethods.TypeAlive<CursedSpirit>(p));
            if (!spirit)
            {
                for (int j = 0; j < 12; j++)
                {
                    float spinOffset = (float)Main.GameUpdateCount * 0.001f * (float)j % 12f;
                    float magnitude = 3f + (float)(j % 5) * 3f * MathF.Sin((float)Main.GameUpdateCount * ((float)Math.PI * 2f) / (10f + ((float)j - 6f) * 28f));
                    Vector2 afterimageOffset = ((float)Math.PI * 2f * ((float)j + spinOffset) / 12f).ToRotationVector2() * magnitude * npc.scale;
                    Color glowColor = GlowColor;
                    spriteBatch.Draw(bodytexture, drawPos + afterimageOffset, npc.frame, glowColor * DrawcodeOpacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }
            spriteBatch.Draw(bodytexture, drawPos, npc.frame, drawColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            if (!spirit)
            {
                float shakeFactor = 1f;
                if (StateMachine.StateStack.Count != 0 && StateMachine.CurrentState.Identifier == BehaviorStates.PhaseTransition)
                {
                    shakeFactor = 3f + 5f * (Timer / 60f);
                }
                Texture2D glowTexture = ModContent.Request<Texture2D>(npc.ModNPC.Texture + "_MaskGlow", AssetRequestMode.ImmediateLoad).Value;
                Color glowColor2 = GlowColor;
                int glowTimer = (int)(Main.GlobalTimeWrappedHourly * 60f) % 60;
                DrawData oldGlow2 = new DrawData(glowTexture, drawPos + Main.rand.NextVector2Circular(shakeFactor, shakeFactor), npc.frame, glowColor2 * DrawcodeOpacity * (0.75f + 0.25f * MathF.Sin((float)Math.PI * 2f * (float)glowTimer / 60f)), npc.rotation, new Vector2(bodytexture.Width / 2, bodytexture.Height / 2 / Main.npcFrameCount[npc.type]), npc.scale, spriteEffects);
                GameShaders.Misc["LCWingShader"].UseColor(Color.Purple).UseSecondaryColor(Color.Black);
                GameShaders.Misc["LCWingShader"].Apply(oldGlow2);
                oldGlow2.Draw(spriteBatch);
            }
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.spriteDirection = npc.direction;
            npc.frame.Y = frameHeight * Frame;
        }
    }
}