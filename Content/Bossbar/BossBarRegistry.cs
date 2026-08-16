using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Render;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Bosses.AbomBoss;
using FargowiltasSouls.Content.Bosses.BanishedBaron;
using FargowiltasSouls.Content.Bosses.Champions.Earth;
using FargowiltasSouls.Content.Bosses.Champions.Nature;
using FargowiltasSouls.Content.Bosses.Champions.Terra;
using FargowiltasSouls.Content.Bosses.Champions.Timber;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.Bosses.DeviBoss;
using FargowiltasSouls.Content.Bosses.Lifelight;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Bosses.TrojanSquirrel;
using static FargowiltasSouls.FargowiltasSouls;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using FargowiltasSouls.Content.Bosses.Champions.Life;
using FargowiltasSouls.Content.Bosses.Champions.Shadow;
using FargowiltasSouls.Content.Bosses.Champions.Spirit;
using FargowiltasSouls.Content.Bosses.Champions.Will;
using FargowiltasSouls.Content.Bosses.Champions.Cosmos;
using Terraria.DataStructures;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs.VanillaEnemies.LunarEvents.Solar;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs.VanillaEnemies.LunarEvents.Vortex;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs.VanillaEnemies.LunarEvents.Nebula;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs.VanillaEnemies.LunarEvents.Stardust;
using System.Linq;

namespace FargosPhantasmMode.Content.Bossbar
{
    public static class BossBarRegistry
    {
        public delegate void DrawBossBarMethod(SpriteBatch sb, NPC npc, Rectangle rectangle);
        private static Dictionary<int, BossBarConfig> bossBarRegistered;
        public static bool HasCustomBossBar(int npcType) => bossBarRegistered?.ContainsKey(npcType) ?? false;
        public static BossBarConfig GetBossBarConfig(int npcType)
        {
            if (!bossBarRegistered.ContainsKey(npcType) || bossBarRegistered == null)
                return BaseBarStyle(Color.Red, Color.White, 1);
            return bossBarRegistered.GetValueOrDefault(npcType);
        }
        public static void Initialize() => bossBarRegistered = [];
        public static void Unload() => bossBarRegistered = null;
        public static void RegisterAllBossBar()
        {
            RegisterVanillaBossBars();
        }
        private static void RegisterVanillaBossBars()
        {
            bossBarRegistered = new()
            {
                [NPCType<TrojanSquirrel>()] = BaseBarStyle(new Color(166, 126, 16), new Color(161, 141, 87)),
                [NPCID.KingSlime] = BaseBarStyle(Color.Blue, Color.AliceBlue, 1),
                [NPCID.EyeofCthulhu] = BaseBarStyle(Color.Teal, Color.Teal, 2),
                [NPCType<CursedCoffin>()] = BaseBarStyle(Color.Purple, Color.Yellow, 1),
                [NPCID.EaterofWorldsHead] = new BossBarConfig((sb, npc, rec) =>
                {
                    if (BossBarRender.TryGetEaterOfWorldsChainLife(npc, out long life, out long maxLife))
                    {
                        DoubleColorPulse(sb, npc, rec, () => Color.Purple, () => new Color(144, 144, 144), (float)life / (float)maxLife, 3);
                    }
                }),
                [NPCID.BrainofCthulhu] = BaseBarStyle(Color.Red, Color.DeepPink, 3, true, 
                    npc => Main.npc.Count(n => n.type == NPCID.Creeper && n.active), 
                    n => NPC.GetBrainOfCthuluCreepersCount()),
                [NPCID.QueenBee] = BaseBarStyle(Color.Yellow, new Color(102, 170, 39), 1),
                [NPCID.SkeletronHead] = BaseBarStyle(Color.White, Color.WhiteSmoke, 2),
                [NPCID.Deerclops] = BaseBarStyle(new Color(112, 132, 255), Color.AliceBlue, 1),
                [NPCType<DeviBoss>()] = BaseBarStyle(() => Color.Pink, EModeColor, 3),
                [NPCID.WallofFlesh] = BaseBarStyle(Color.Red, Color.Yellow, 6),
                [NPCID.QueenSlimeBoss] = BaseBarStyle(Color.LightPink, new Color(192, 64, 255), 1),
                [NPCType<BanishedBaron>()] = BaseBarStyle(Color.Pink, Color.Gold, 2),
                [NPCID.Retinazer] = BaseBarStyle(PhanUtil.MechColor, () => Color.Yellow, 3),
                [NPCID.Spazmatism] = BaseBarStyle(PhanUtil.MechColor, () => Color.Green, 3),
                [NPCID.SkeletronPrime] = BaseBarStyle(PhanUtil.MechColor, () => Color.Red, 3),
                [NPCID.TheDestroyer] = BaseBarStyle(PhanUtil.MechColor, () => Color.Blue, 3),
                [NPCType<LifeChallenger>()] = BaseBarStyle(Color.Gold, Color.LightGoldenrodYellow, 2),
                [NPCID.Plantera] = BaseBarStyle(Color.Green, new Color(64, 255, 166), 4),
                [NPCID.Golem] = BaseBarStyle(new Color(156, 143, 76), Color.IndianRed, 5),
                [NPCID.DD2Betsy] = BaseBarStyle(Color.Black, Color.Purple, 2),
                [NPCID.DukeFishron] = BaseBarStyle(Color.Blue, Color.Aqua, 5),
                [NPCID.HallowBoss] = BaseBarStyle(Color.Black, Color.LightGoldenrodYellow, 5),
                [NPCID.CultistBoss] = BaseBarStyle(PhanUtil.CosmoColor, () => Color.DarkGray, 4),
                [NPCID.LunarTowerSolar] = BaseBarStyle(PhanUtil.CosmoColor, () => Color.OrangeRed, 1, true, n => n.GetGlobalNPC<LunarTowerSolar>().ShieldStrength, n => NPC.LunarShieldPowerMax),
                [NPCID.LunarTowerVortex] = BaseBarStyle(PhanUtil.CosmoColor, () => Color.AliceBlue, 1, true, n => n.GetGlobalNPC<LunarTowerVortex>().ShieldStrength, n => NPC.LunarShieldPowerMax),
                [NPCID.LunarTowerNebula] = BaseBarStyle(PhanUtil.CosmoColor, () => Color.Purple, 1, true, n => n.GetGlobalNPC<LunarTowerNebula>().ShieldStrength, n => NPC.LunarShieldPowerMax),
                [NPCID.LunarTowerStardust] = BaseBarStyle(PhanUtil.CosmoColor, () => Color.Blue, 1, true, n => n.GetGlobalNPC<LunarTowerStardust>().ShieldStrength, n => NPC.LunarShieldPowerMax),
                [NPCID.MoonLordCore] = BaseBarStyle(PhanUtil.CosmoColor, PhanUtil.CosmoColor, 8),
                [NPCType<TimberChampion>()] = BaseBarStyle(EModeColor, () => new Color(166, 126, 16), 3),
                [NPCType<TerraChampion>()] = BaseBarStyle(EModeColor, () => new Color(159, 159, 159), 3),
                [NPCType<EarthChampion>()] = BaseBarStyle(EModeColor, () => new Color(255, 115, 64), 3),
                [NPCType<NatureChampion>()] = BaseBarStyle(EModeColor, () => Color.AliceBlue, 3),
                [NPCType<LifeChampion>()] = BaseBarStyle(EModeColor, () => Color.LightGoldenrodYellow, 3),
                [NPCType<ShadowChampion>()] = BaseBarStyle(EModeColor, () => Color.DarkViolet, 3, true, 
                    npc => Main.npc.Count(n => n.type == NPCType<ShadowOrbNPC>() && n.active && n.ai[0] == npc.whoAmI && !n.dontTakeDamage), 
                    npc => Main.npc.Count(n => n.type == NPCType<ShadowOrbNPC>() && n.active && n.ai[0] == npc.whoAmI)),
                [NPCType<SpiritChampion>()] = BaseBarStyle(EModeColor, () => Color.Black, 3),
                [NPCType<WillChampion>()] = BaseBarStyle(EModeColor, () => Color.Gold, 3),
                [NPCType<CosmosChampion>()] = BaseBarStyle(() => Color.Purple, PhanUtil.CosmoColor, 5),
                [NPCType<AbomBoss>()] = BaseBarStyle(PhanUtil.CosmoColor, EModeColor, 6),
                [NPCType<MutantBoss>()] = new(MutantBossBar),
            };

        }
        public static BossBarConfig BaseBarStyle(Func<Color> color1, Func<Color> color2, float omiga = 0, bool HasShield = false, Func<NPC, int> Shield = null, Func<NPC, int> MaxShield = null) => new ((sb, npc, rectangle) => DoubleColorPulse(sb, npc, rectangle, color1, color2, npc.GetLifePercent(), omiga), HasShield, Shield, MaxShield);
        public static BossBarConfig BaseBarStyle(Color color1, Color color2, float omiga = 0, bool HasShield = false, Func<NPC, int> Shield = null, Func<NPC, int> MaxShield = null) => new ((sb, npc, rectangle) => DoubleColorPulse(sb, npc, rectangle, () => color1, () => color2, npc.GetLifePercent(), omiga), HasShield, Shield, MaxShield);
        private static void DoubleColorPulse(SpriteBatch sb, NPC npc, Rectangle rectangle, Func<Color> color1, Func<Color> color2, float lifeRatio, float omiga)
        {
            ManagedShader healthBarShader = ShaderManager.GetShader("FargosPhantasmMode.BossBarShader");
            Texture2D noise = FargosTextureRegistry.WavyNoise.Value;
            sb.GraphicsDevice.Textures[1] = noise;

            healthBarShader.TrySetParameter("lifeRatio", lifeRatio);
            healthBarShader.TrySetParameter("color1", color1?.Invoke());
            healthBarShader.TrySetParameter("color2", color2?.Invoke());
            healthBarShader.TrySetParameter("omiga", omiga);
            healthBarShader.Apply();
            sb.Draw(MiscTexturesRegistry.InvisiblePixel.Value, rectangle, null, Color.White);
        }
        private static void MutantBossBar(SpriteBatch sb, NPC npc, Rectangle rectangle)
        {
            DoubleColorPulse(sb, npc, rectangle, () => Color.Aqua, () => Color.Blue, npc.GetLifePercent(), 6);
            FirePartiRe.Particle p = new FirePartiRe.Particle
            {
                Position = rectangle.Center.ToVector2() + new Vector2(-300, 0) + Main.rand.NextVector2Unit() * 3 /*+ Main.rand.Next(60) * Vector2.UnitX*/,
                Velocity = 1 * Main.rand.NextVector2Unit() + 12 * Vector2.UnitX,
                Scale = 1.3f,
                Alpha = 255,
                active = true
            };
            FirePartiRe.SpawnParticle(p);
        }
    }
}
