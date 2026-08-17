using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.Champions.Terra;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Life
{
    public class Cactus : PModeGlobalEnchant<CactusEnchant>
    {
    }
    public class CactusNeedleGlobalNPC : GlobalNPC
    {
        public static bool PModeChangeApply => PModeWorldSavingSystem.PhantasmMode;
        public override GlobalNPC NewInstance(NPC target) => PModeChangeApply ? base.NewInstance(target) : null;
        public override bool InstancePerEntity => true;
        public int CactusDropCD = 0;
        public static List<int> blacklist = [NPCID.TheDestroyerBody, NPCID.TheDestroyerTail, ModContent.NPCType<TerraChampionBody>()];
        public override void AI(NPC npc)
        {
            if (npc.FargoSouls().Needled && PModeChangeApply)
            {
                if (++CactusDropCD > 5 * 60)
                {
                    CactusDropCD = 0;
                    Player player = Main.LocalPlayer;
                    CactusDropItem(player, npc);
                    npc.FargoSouls().Needled = false;
                }
            }
            /*
            if (!Main.LocalPlayer.HasEffect<CactusEffect>())
                npc.FargoSouls().Needled = false;
            */
        }
        public override void OnKill(NPC npc)
        {
            if (npc.FargoSouls().Needled && PModeChangeApply)
            {
                CactusDropItem(Main.LocalPlayer, npc);
                npc.FargoSouls().Needled = false;
            }
        }
        public static void CactusDropItem(Player player, NPC npc)
        {
            if (player.HasEffect<CactusEffect>())
            {
                bool isHeart = Main.rand.NextBool();
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Item.NewItem(player.GetSource_OnHit(npc), npc.Hitbox, isHeart ? ItemID.Heart : ItemID.Star);
                }
                else if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    var heart = isHeart ? FargowiltasSouls.FargowiltasSouls.PacketID.RequestPerfumeHeart : FargowiltasSouls.FargowiltasSouls.PacketID.RequestPearlwoodStar;
                    var netMessage = FargosPhantasmMode.FargoMod.GetPacket();
                    netMessage.Write((byte)heart);
                    netMessage.Write((byte)player.whoAmI);
                    netMessage.Write((byte)npc.whoAmI);
                    netMessage.Send();
                }
            }
        }
    }
    public class CactusNeedleGlobalProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public static bool PModeChangeApply => PModeWorldSavingSystem.PhantasmMode;
        public override GlobalProjectile NewInstance(Projectile target) => PModeChangeApply ? base.NewInstance(target) : null;
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ModContent.ProjectileType<CactusNeedle>();
        
        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            Player player = Main.player[proj.owner];
            if (player.HasEffect<CactusEffect>() && PModeChangeApply)
            {
                bool hasenhance = player.ForceEffect<CactusEffect>();
                proj.damage *= player.HasEffect<LifeForceEffect>() ? 5 : 1;
                proj.timeLeft = hasenhance ? 120 : 60;
                proj.extraUpdates = hasenhance ? 3 : 1;
            }
            base.OnSpawn(proj, source);
        }
    }
}
