using FargosPhantasmMode.Content.Bosses.VanillaEternity.Destroyer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Sky
{
    public class DestroyerFlashSky : CustomSky
    {
        public int StormWeaverHeadIndex = -1;

        public override void Update(GameTime gameTime)
        {
            int weaverType = NPCID.TheDestroyer;
            if (StormWeaverHeadIndex >= 0 && Main.npc[StormWeaverHeadIndex].active && Main.npc[StormWeaverHeadIndex].type == weaverType)
                return;

            StormWeaverHeadIndex = NPC.FindFirstNPC(weaverType);
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth < float.MaxValue || !Main.npc.IndexInRange(StormWeaverHeadIndex))
                return;
            PHDestroyer globalNPC = Main.npc[StormWeaverHeadIndex].GetGlobalNPC<PHDestroyer>();
            if (globalNPC == null)
                return;
            // Draw lightning in the background based on TextureAssets.MagicPixel.
            // It is a long, white vertical strip that exists for some reason.
            // This lightning effect is achieved by expanding this to fit the entire background and then drawing it as a distinct element.
            Texture2D white = TextureAssets.MagicPixel.Value;
            float lightningFlashPower = globalNPC.lightning;
            Vector2 scale = new Vector2(Main.screenWidth * 1.1f / white.Width, Main.screenHeight * 1.1f / white.Height);
            Vector2 screenCenter = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            Color drawColor = Color.White * MathHelper.Lerp(0f, 0.88f, lightningFlashPower);
            Vector2 origin = white.Size() * 0.5f;

            for (int i = 0; i < 2; i++)
                spriteBatch.Draw(white, screenCenter, null, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        public override void Reset() { }

        public override void Activate(Vector2 position, params object[] args) { }

        public override void Deactivate(params object[] args) { }

        public override bool IsActive() => StormWeaverHeadIndex != -1 && !Main.gameMenu;
    }
    public class DestroyerBackgroundScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override bool IsSceneEffectActive(Player player) => NPC.AnyNPCs(NPCID.TheDestroyer);

        public override void SpecialVisuals(Player player, bool isActive)
        {
            if (SkyManager.Instance["FargosPhantasmMode:DestroyerFlashSky"] != null && isActive != SkyManager.Instance["FargosPhantasmMode:DestroyerFlashSky"].IsActive())
            {
                if (isActive)
                    SkyManager.Instance.Activate("FargosPhantasmMode:DestroyerFlashSky", player.Center);
                else
                    SkyManager.Instance.Deactivate("FargosPhantasmMode:DestroyerFlashSky");
            }
        }
    }
}
