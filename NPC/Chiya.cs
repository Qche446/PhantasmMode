namespace FargosPhantasmMode.NPCs
{
    /*
    internal class Chiya : ModNPC
    {
        //[AutoloadHead]
        public override void SetStaticDefaults()
        {

            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 700;
            NPCID.Sets.AttackType[NPC.type] = 0;
            NPCID.Sets.AttackTime[NPC.type] = 30;
            NPCID.Sets.AttackAverageChance[NPC.type] = 5;
            NPCID.Sets.HatOffsetY[NPC.type] = 2;

        }
        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 40;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 10;
            NPC.defense = NPC.downedMoonlord ? 50 : 15;
            NPC.lifeMax = NPC.downedMoonlord ? 600 : Main.hardMode ? 600 : 400;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.Guide;
            NPC.buffImmune[BuffID.Suffocation] = true;
        }
        public override bool CanTownNPCSpawn(int numTownNPCs)// tModPorter Suggestion: Copy the implementation of NPC.SpawnAllowed_Merchant in vanilla if you to count money, and be sure to set a flag when unlocked, so you don't count every tick.
        {
            if (numTownNPCs >= 3 && NPC.downedBoss1)
            {
                return true;
            }
            return false;
        }
        public override List<string> SetNPCNameList()
        {
            string[] names = ["Chiya", "Chiyo", "Qche", "Twila", "Nameless"];
            return new List<string>(names);
        }
        public override void SetChatButtons(ref string button, ref string button2)
        {
            //翻译“商店文本”
            button = Language.GetTextValue("LegacyInterface.28");
        }
        public const string Shop = "Shop";
        public override void OnChatButtonClicked(bool firstButton, ref string shop)
        {
            //如果按下第一个按钮，则开启商店
            if (firstButton)
            {
                shop = Shop;
            }
            //在if之后可以写第二个按钮的作用，自己想想能加点啥
        }
        public override void AddShops()
        {
            var npcShop = new NPCShop(Type, Shop)
                .Add(new Item(ItemType<WoodEnchant>()) { shopCustomPrice = Item.buyPrice(copper: 10000) })
                .Add(new Item(ItemType<WeatherBalloon>()) { shopCustomPrice = Item.buyPrice(copper: 20000) })
                .Add(new Item(ItemType<Anemometer>()) { shopCustomPrice = Item.buyPrice(copper: 30000) })
                .Add(new Item(ItemType<ForbiddenScarab>()) { shopCustomPrice = Item.buyPrice(copper: 30000) })
                .Add(new Item(ItemType<SlimyBarometer>()) { shopCustomPrice = Item.buyPrice(copper: 40000) })
                .Add(new Item(ItemID.BloodMoonStarter) { shopCustomPrice = Item.buyPrice(copper: 50000) })
                .Add(new Item(ItemID.GoblinBattleStandard) { shopCustomPrice = Item.buyPrice(copper: 60000) })
                .Add(new Item(ItemType<MatsuriLantern>()) { shopCustomPrice = Item.buyPrice(copper: 100000) }, new Condition("Mods.Fargowiltas.Conditions.BossDown", () => FargoWorld.DownedBools["boss"]))
                .Add(new Item(ItemID.SnowGlobe) { shopCustomPrice = Item.buyPrice(copper: 150000) }, Condition.Hardmode)
                .Add(new Item(ItemID.PirateMap) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedPirates)
                .Add(new Item(ItemType<PlunderedBooty>()) { shopCustomPrice = Item.buyPrice(copper: 150000) }, new Condition("Mods.Fargowiltas.Conditions.DutchmanDown", () => NPC.downedPirates && FargoWorld.DownedBools["flyingDutchman"]))
                .Add(new Item(ItemID.SolarTablet) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedMechBossAny)
                .Add(new Item(ItemType<ForbiddenTome>()) { shopCustomPrice = Item.buyPrice(copper: 50000) }, new Condition("Mods.Fargowiltas.Conditions.MageDown", () => FargoWorld.DownedBools["darkMage"] || NPC.downedMechBossAny))
                .Add(new Item(ItemType<BatteredClub>()) { shopCustomPrice = Item.buyPrice(copper: 150000) }, new Condition("Mods.Fargowiltas.Conditions.OgreDown", () => FargoWorld.DownedBools["ogre"] || NPC.downedGolemBoss))
                .Add(new Item(ItemType<BetsyEgg>()) { shopCustomPrice = Item.buyPrice(copper: 400000) }, new Condition("Mods.Fargowiltas.Conditions.BetsyDown", () => FargoWorld.DownedBools["betsy"]))
                .Add(new Item(ItemID.PumpkinMoonMedallion) { shopCustomPrice = Item.buyPrice(copper: 500000) }, Condition.DownedPumpking)
                 .Add(new Item(ItemType<HeadofMan>()) { shopCustomPrice = Item.buyPrice(copper: 200000) }, new Condition("Mods.Fargowiltas.Conditions.HorsemanDown", () => FargoWorld.DownedBools["headlessHorseman"]))
                 .Add(new Item(ItemType<SpookyBranch>()) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedMourningWood)
                 .Add(new Item(ItemType<SuspiciousLookingScythe>()) { shopCustomPrice = Item.buyPrice(copper: 300000) }, Condition.DownedPumpking)
                 .Add(new Item(ItemID.NaughtyPresent) { shopCustomPrice = Item.buyPrice(copper: 500000) }, Condition.DownedIceQueen)
                 .Add(new Item(ItemType<FestiveOrnament>()) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedEverscream)
                 .Add(new Item(ItemType<NaughtyList>()) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedSantaNK1)
                 .Add(new Item(ItemType<IceKingsRemains>()) { shopCustomPrice = Item.buyPrice(copper: 300000) }, Condition.DownedIceQueen)
                 .Add(new Item(ItemType<RunawayProbe>()) { shopCustomPrice = Item.buyPrice(copper: 500000) }, Condition.DownedGolem)
                 .Add(new Item(ItemType<MartianMemoryStick>()) { shopCustomPrice = Item.buyPrice(copper: 300000) }, Condition.DownedMartians)
                 .Add(new Item(ItemType<PillarSummon>()) { shopCustomPrice = Item.buyPrice(copper: 750000) }, new Condition("Mods.Fargowiltas.Conditions.PillarsDown", () => NPC.downedTowers))
                 .Add(new Item(ItemType<AbominationnScythe>()) { shopCustomPrice = Item.buyPrice(copper: 50000) }, new Condition("Mods.Fargowiltas.Conditions.PillarsDown", () => NPC.downedTowers))
                .Add(new Item(ItemType<SiblingPylon>()), Condition.HappyEnoughToSellPylons, Condition.NpcIsPresent(NPCType<Mutant>()), Condition.NpcIsPresent(NPCType<Deviantt>()))

            ;

            npcShop.Register();
        }
    }
    */
}
