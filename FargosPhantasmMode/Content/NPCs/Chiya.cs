using FargosPhantasmMode.Core.Systems;
using Fargowiltas.Content.Biomes;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;
namespace FargosPhantasmMode.Content.NPCs;

[AutoloadHead]
public class Chiya : ModNPC
{
    public override bool IsLoadingEnabled(Mod mod) => false;
    public override ITownNPCProfile TownNPCProfile() => new ChiyaProfile();
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = 23;
        NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
        NPCID.Sets.AttackFrameCount[NPC.type] = 4;
        NPCID.Sets.DangerDetectRange[NPC.type] = 700;
        NPCID.Sets.AttackType[NPC.type] = -1;
        NPCID.Sets.AttackTime[NPC.type] = 30;
        NPCID.Sets.AttackAverageChance[NPC.type] = 5;
        NPCID.Sets.HatOffsetY[NPC.type] = 2;
        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new() { Velocity = -1f, Direction = -1 };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
        NPC.Happiness.SetBiomeAffection<OceanBiome>(AffectionLevel.Love);
        NPC.Happiness.SetBiomeAffection<SkyBiome>(AffectionLevel.Like);
        NPC.Happiness.SetBiomeAffection<SnowBiome>(AffectionLevel.Like);
        NPC.Happiness.SetBiomeAffection<JungleBiome>(AffectionLevel.Dislike);
        NPC.Happiness.SetBiomeAffection<DesertBiome>(AffectionLevel.Hate);
        NPC.Happiness.SetNPCAffection(NPCID.PartyGirl, AffectionLevel.Like);
        NPC.Happiness.SetNPCAffection(NPCID.Guide, AffectionLevel.Dislike);
    }
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange([BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean, 
            new FlavorTextBestiaryInfoElement("Mods.FargosPhantasmMode.Bestiary.Chiya")]);
    }
    public override void SetDefaults()
    {
        NPC.townNPC = true;
        NPC.friendly = true;
        NPC.width = 40;
        NPC.height = 40;
        NPC.aiStyle = NPCAIStyleID.Passive;
        NPC.damage = 10;
        NPC.defense = NPC.downedMoonlord ? 50 : 15;
        NPC.lifeMax = NPC.downedPlantBoss ? 600 : Main.hardMode ? 480 : 400;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.knockBackResist = 0.5f;
        AnimationType = NPCID.Angler;
        NPC.buffImmune[BuffID.Suffocation] = true;
        NPC.dontTakeDamage = true;
    }
    public override bool CanTownNPCSpawn(int numTownNPCs)
    {
        if (!PModeWorldSavingSystem.PhantasmMode || true)
            return false;
        if (numTownNPCs >= 3 && NPC.downedBoss1)
        {
            return true;
        }
        return false;
    }
    public override bool CanGoToStatue(bool toKingStatue) => !toKingStatue;
    public override List<string> SetNPCNameList() => ["Chiya", "Chiyo", "Twila", "Nameless"];
    public override string GetChat()
    {
        return base.GetChat();
    }
    public static string ChiyaChat(string key, params object[] args) => Language.GetTextValue($"Mods.FargosPhantasmMode.NPCs.Chiya.Chat.{key}", args);
    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = Language.GetTextValue("LegacyInterface.28");
        button2 = Language.GetTextValue("Mods.FargosPhantasmMode.NPCs.Chiya.HelpButton");
    }
    public const string Shop = "Shop";
    public override void OnChatButtonClicked(bool firstButton, ref string shop)
    {
        if (firstButton)
            shop = Shop;
        /*else
            Main.npcChatText = ;*/
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        int offset = -5;
        Texture2D texture = (Texture2D)TownNPCProfile().GetTextureNPCShouldUse(NPC);
        Rectangle rectangle = NPC.frame;
        Vector2 origin2 = rectangle.Size() / 2f;
        SpriteEffects effects = NPC.IsShimmerVariant ? NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (NPC.IsShimmerVariant)
            offset = -3;

        if (!NPC.IsABestiaryIconDummy)
        {
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY) + new Vector2(0, offset), new Microsoft.Xna.Framework.Rectangle?(rectangle), NPC.GetAlpha(drawColor), NPC.rotation, origin2, NPC.scale, effects, 0);
            return false;
        }
        else
            return true;
    }
    public override void AddShops()
    {
        var npcShop = new NPCShop(Type, Shop)
        ;

        npcShop.Register();
    }
    public override void HitEffect(NPC.HitInfo hit)
    {
        if (NPC.life <= 0)
        {
            for (int k = 0; k < 24; k++)
            {
                int did = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0, 0, 0, default, 0.8f);
                Main.dust[did].velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0, 4);
            }

        }
        else
        {
            for (int k = 0; k < hit.Damage / NPC.lifeMax * 50.0; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection, -1f, 0, default, 0.6f);
            }
        }
    }
}
public class ChiyaProfile : ITownNPCProfile
{
    public readonly static string ChiyaString = "FargosPhantasmMode/Content/NPCs/Chiya";
    public int GetHeadTextureIndex(NPC npc) => ModContent.GetModHeadSlot(ChiyaString + "_Head");

    public string GetNameForVariant(NPC npc) => npc.getNewNPCName();

    public Asset<Texture2D> GetTextureNPCShouldUse(NPC npc)
    {
        if (npc.IsABestiaryIconDummy)
            return ModContent.Request<Texture2D>(ChiyaString);
        /*
        if (npc.IsShimmerVariant)
        {
            return ModContent.Request<Texture2D>("Fargowiltas/NPCs/Deviantt_Shimmer");
        }
        */
        if (npc.direction == -1)
            return ModContent.Request<Texture2D>(ChiyaString);
        else
            return ModContent.Request<Texture2D>(ChiyaString + "Right");
    }

    public int RollVariation() => 0;
}
