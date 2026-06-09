using FargowiltasSouls.Content.Items.Summons;
using FargowiltasSouls.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using FargosPhantasmMode;
using Terraria.ID;
using FargosPhantasmMode.Content.Bosses.AbomBoss;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using FargowiltasSouls.Core.Toggler;
using Terraria.GameContent;


namespace FargosPhantasmMode
{
    public class FargosPhantasmMode : Mod
    {
        
        internal static FargosPhantasmMode Instance;
        internal bool LoadedNewSprites;
        internal struct TextureBuffer
        {
            public static readonly Dictionary<int, Asset<Texture2D>> NPC = [];
            public static readonly Dictionary<int, Asset<Texture2D>> NPCHeadBoss = [];
            public static readonly Dictionary<int, Asset<Texture2D>> Gore = [];
            public static readonly Dictionary<int, Asset<Texture2D>> Golem = [];
            public static readonly Dictionary<int, Asset<Texture2D>> Dest = [];
            public static readonly Dictionary<int, Asset<Texture2D>> GlowMask = [];
            public static readonly Dictionary<int, Asset<Texture2D>> Extra = [];
            public static readonly Dictionary<int, Asset<Texture2D>> Projectile = [];
            public static Asset<Texture2D> Ninja = null;
            public static Asset<Texture2D> Probe = null;
            public static Asset<Texture2D> BoneArm = null;
            public static Asset<Texture2D> BoneArm2 = null;
            public static Asset<Texture2D> BoneLaser = null;
            public static Asset<Texture2D> BoneEyes = null;
            public static Asset<Texture2D> Chain12 = null;
            public static Asset<Texture2D> Chain26 = null;
            public static Asset<Texture2D> Chain27 = null;
            public static Asset<Texture2D> Wof = null;
            public static Asset<Texture2D> EyeLaser = null;
        }
        public override void Load()
        {
            Instance = this;
        }
        public override void Unload()
        {
            static void RestoreSprites(Dictionary<int, Asset<Texture2D>> buffer, Asset<Texture2D>[] original)
            {
                foreach (KeyValuePair<int, Asset<Texture2D>> pair in buffer)
                    original[pair.Key] = pair.Value;

                buffer.Clear();
            }

            RestoreSprites(TextureBuffer.NPC, TextureAssets.Npc);
            RestoreSprites(TextureBuffer.NPCHeadBoss, TextureAssets.NpcHeadBoss);
            RestoreSprites(TextureBuffer.Gore, TextureAssets.Gore);
            RestoreSprites(TextureBuffer.Golem, TextureAssets.Golem);
            RestoreSprites(TextureBuffer.Dest, TextureAssets.Dest);
            RestoreSprites(TextureBuffer.GlowMask, TextureAssets.GlowMask);
            RestoreSprites(TextureBuffer.Extra, TextureAssets.Extra);
            RestoreSprites(TextureBuffer.Projectile, TextureAssets.Projectile);

            if (TextureBuffer.Ninja != null)
                TextureAssets.Ninja = TextureBuffer.Ninja;
            if (TextureBuffer.Probe != null)
                TextureAssets.Probe = TextureBuffer.Probe;
            if (TextureBuffer.BoneArm != null)
                TextureAssets.BoneArm = TextureBuffer.BoneArm;
            if (TextureBuffer.BoneArm2 != null)
                TextureAssets.BoneArm2 = TextureBuffer.BoneArm2;
            if (TextureBuffer.BoneLaser != null)
                TextureAssets.BoneLaser = TextureBuffer.BoneLaser;
            if (TextureBuffer.BoneEyes != null)
                TextureAssets.BoneEyes = TextureBuffer.BoneEyes;
            if (TextureBuffer.Chain12 != null)
                TextureAssets.Chain12 = TextureBuffer.Chain12;
            if (TextureBuffer.Chain26 != null)
                TextureAssets.Chain26 = TextureBuffer.Chain26;
            if (TextureBuffer.Chain27 != null)
                TextureAssets.Chain27 = TextureBuffer.Chain27;
            if (TextureBuffer.Wof != null)
                TextureAssets.Wof = TextureBuffer.Wof;

            ToggleLoader.Unload();
        }
        
    }
}
